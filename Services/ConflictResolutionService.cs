#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace NotionTaskSync.Services;

using NotionTaskSync.Domain.Models;
using NotionTaskSync.Data.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Resolves conflicts detected during sync operations using configured strategies.
/// Supports last-write-wins, local-wins, notion-wins, and manual resolution modes.
/// </summary>
public class ConflictResolutionService
{
    private readonly IChangeLogRepository _changeLogRepository;

    public ConflictResolutionService(IChangeLogRepository changeLogRepository)
    {
        _changeLogRepository = changeLogRepository;
    }

    /// <summary>
    /// Resolves all detected conflicts using the specified strategy.
    /// When local changes are discarded, a warning entry is written to the change log
    /// so edits are never silently lost.
    /// </summary>
    public virtual async Task<List<ConflictResolution>> ResolveConflictsAsync(
        List<ConflictResolution> conflicts,
        ConflictResolutionStrategy strategy,
        Dictionary<string, ConflictResolutionStrategy>? fieldStrategies = null)
    {
        var resolutions = new List<ConflictResolution>();

        foreach (var conflict in conflicts)
        {
            var hadLocalValue = !string.IsNullOrEmpty(conflict.LocalValue);

            // Per-field strategy overrides the global strategy when configured
            var effectiveStrategy = fieldStrategies is not null
                && !string.IsNullOrEmpty(conflict.PropertyName)
                && fieldStrategies.TryGetValue(conflict.PropertyName, out var fieldStrategy)
                    ? fieldStrategy
                    : strategy;

            var resolution = effectiveStrategy switch
            {
                ConflictResolutionStrategy.LastWrite => ResolveByLastWrite(conflict),
                ConflictResolutionStrategy.LocalWins => ResolveLocalWins(conflict),
                ConflictResolutionStrategy.NotionWins => ResolveNotionWins(conflict),
                ConflictResolutionStrategy.Manual => ResolveManual(conflict),
                _ => ResolveByLastWrite(conflict)
            };

            // Log a warning whenever a local edit is discarded so it can be audited or recovered
            if (hadLocalValue
                && resolution.Status == ResolutionStatus.Resolved
                && resolution.ResolutionMethod != ResolutionMethod.LocalWins
                && resolution.ResolvedValue != resolution.LocalValue)
            {
                await LogDiscardedLocalChangeAsync(resolution);
            }

            resolutions.Add(resolution);
        }

        return resolutions;
    }

    /// <summary>
    /// Writes a change-log entry recording the discarded local value so it can be
    /// audited, recovered, or surfaced in a future UI.
    /// </summary>
    private async System.Threading.Tasks.Task LogDiscardedLocalChangeAsync(ConflictResolution resolution)
    {
        var logEntry = new ChangeLog
        {
            Id = Guid.NewGuid(),
            TaskId = resolution.TaskId,
            ChangeType = "LocalChangeDiscarded",
            Source = ChangeSource.System,
            Timestamp = DateTime.UtcNow,
            OldValue = resolution.LocalValue,
            NewValue = resolution.ResolvedValue,
            PropertyName = resolution.PropertyName,
            IsConflict = true,
            ConflictResolutionStrategy = resolution.ResolutionMethod.ToString(),
            Description = $"Local change discarded during conflict resolution. " +
                          $"Strategy: {resolution.ResolutionMethod}. " +
                          $"Discarded value: \"{resolution.LocalValue}\". " +
                          $"Applied value: \"{resolution.ResolvedValue}\". " +
                          $"Notes: {resolution.ResolutionNotes}"
        };

        await _changeLogRepository.AddAsync(logEntry);
    }

    /// <summary>
    /// Resolves conflict by keeping the value with the newer modification timestamp.
    /// Uses <see cref="ConflictResolution.LocalModifiedAt"/> and
    /// <see cref="ConflictResolution.NotionModifiedAt"/> when available.
    /// Resolution notes record what was discarded so the decision is fully auditable.
    /// </summary>
    private ConflictResolution ResolveByLastWrite(ConflictResolution conflict)
    {
        var localTimestamp = conflict.LocalModifiedAt ?? DateTime.MinValue;
        var notionTimestamp = conflict.NotionModifiedAt ?? DateTime.MinValue;

        if (notionTimestamp >= localTimestamp)
        {
            conflict.Resolve(
                conflict.NotionValue ?? string.Empty,
                ResolutionMethod.LastWrite,
                $"Resolved using last-write-wins: Notion value is newer " +
                $"(local: {localTimestamp:O}, notion: {notionTimestamp:O}). " +
                $"Discarded local value: \"{conflict.LocalValue}\"");
        }
        else
        {
            conflict.Resolve(
                conflict.LocalValue ?? string.Empty,
                ResolutionMethod.LastWrite,
                $"Resolved using last-write-wins: Local value is newer " +
                $"(local: {localTimestamp:O}, notion: {notionTimestamp:O}). " +
                $"Discarded Notion value: \"{conflict.NotionValue}\"");
        }

        return conflict;
    }

    /// <summary>
    /// Resolves conflict by always preferring the local value.
    /// </summary>
    private ConflictResolution ResolveLocalWins(ConflictResolution conflict)
    {
        conflict.Resolve(conflict.LocalValue ?? string.Empty, ResolutionMethod.LocalWins,
            "Resolved using local-wins strategy: Local value preferred");

        return conflict;
    }

    /// <summary>
    /// Resolves conflict by always preferring the Notion value.
    /// </summary>
    private ConflictResolution ResolveNotionWins(ConflictResolution conflict)
    {
        conflict.Resolve(conflict.NotionValue ?? string.Empty, ResolutionMethod.NotionWins,
            "Resolved using notion-wins strategy: Notion value preferred");

        return conflict;
    }

    /// <summary>
    /// Marks conflict for manual review as it cannot be automatically resolved.
    /// </summary>
    private ConflictResolution ResolveManual(ConflictResolution conflict)
    {
        conflict.MarkForManualReview(
            $"Conflict requires manual resolution: {conflict.GetConflictSummary()}");

        return conflict;
    }

    /// <summary>
    /// Manually resolves a pending conflict with a specific value and method.
    /// </summary>
    public async Task<ConflictResolution> ManuallyResolveAsync(
        Guid conflictId,
        string resolvedValue,
        ResolutionMethod method,
        string? notes = null)
    {
        // Would fetch the conflict from storage in real implementation
        var conflict = new ConflictResolution
        {
            Id = conflictId,
            TaskId = Guid.Empty,
            Status = ResolutionStatus.Pending
        };

        conflict.Resolve(resolvedValue, method, notes);
        conflict.ResolvedBy = "system";

        return conflict;
    }

    /// <summary>
    /// Attempts to merge conflicting values when both have changed.
    /// Works best for list-like or additive properties.
    /// </summary>
    public ConflictResolution MergeConflicts(ConflictResolution conflict)
    {
        if (string.IsNullOrEmpty(conflict.LocalValue) || string.IsNullOrEmpty(conflict.NotionValue))
        {
            // If one is empty, prefer the non-empty value
            var mergedValue = string.IsNullOrEmpty(conflict.LocalValue)
                ? conflict.NotionValue
                : conflict.LocalValue;

            conflict.Resolve(mergedValue ?? string.Empty, ResolutionMethod.Merged,
                "Merged by selecting non-empty value");
        }
        else if (conflict.LocalValue == conflict.NotionValue)
        {
            // If both values are the same, use either value
            conflict.Resolve(conflict.LocalValue, ResolutionMethod.Merged,
                "Merged: both sides had identical values");
        }
        else
        {
            // Hotfix: Both sides modified the same property with different values
            // This should be marked for manual review rather than auto-merged
            conflict.MarkForManualReview(
                $"Conflict: both sides modified {conflict.PropertyName ?? "property"} with different values. Manual review required.");
        }

        return conflict;
    }

    /// <summary>
    /// Gets all pending conflicts requiring manual review.
    /// </summary>
    public List<ConflictResolution> GetPendingConflicts(List<ConflictResolution> allConflicts)
    {
        return allConflicts.Where(c => c.IsPending()).ToList();
    }

    /// <summary>
    /// Gets the resolution statistics for a batch of conflicts.
    /// </summary>
    public ConflictResolutionStats GetResolutionStats(List<ConflictResolution> conflicts)
    {
        return new ConflictResolutionStats
        {
            TotalConflicts = conflicts.Count,
            ResolvedCount = conflicts.Count(c => c.Status == ResolutionStatus.Resolved),
            PendingReviewCount = conflicts.Count(c => c.Status == ResolutionStatus.PendingReview),
            AbandonedCount = conflicts.Count(c => c.Status == ResolutionStatus.Abandoned),
            ResolutionRate = conflicts.Count > 0
                ? (double)conflicts.Count(c => c.Status == ResolutionStatus.Resolved) / conflicts.Count
                : 0
        };
    }

    /// <summary>
    /// Resolves a single conflict using the specified strategy.
    /// </summary>
    /// <param name="conflict">The conflict to resolve.</param>
    /// <param name="strategy">The strategy to use for resolution.</param>
    /// <returns>The resolved conflict.</returns>
    public ConflictResolution ResolveWith(ConflictResolution conflict, ConflictStrategy strategy)
    {
        ArgumentNullException.ThrowIfNull(conflict);

        return strategy switch
        {
            ConflictStrategy.PreferLocal => ResolveWithPreferLocal(conflict),
            ConflictStrategy.PreferRemote => ResolveWithPreferRemote(conflict),
            ConflictStrategy.Newest => ResolveWithNewest(conflict),
            _ => ResolveWithNewest(conflict)
        };
    }

    /// <summary>
    /// Resolves conflict by preferring the local value.
    /// </summary>
    private ConflictResolution ResolveWithPreferLocal(ConflictResolution conflict)
    {
        conflict.Resolve(
            conflict.LocalValue ?? string.Empty,
            ResolutionMethod.LocalWins,
            "Resolved using PreferLocal strategy: Local value preferred");

        return conflict;
    }

    /// <summary>
    /// Resolves conflict by preferring the remote (Notion) value.
    /// </summary>
    private ConflictResolution ResolveWithPreferRemote(ConflictResolution conflict)
    {
        conflict.Resolve(
            conflict.NotionValue ?? string.Empty,
            ResolutionMethod.NotionWins,
            "Resolved using PreferRemote strategy: Notion value preferred");

        return conflict;
    }

    /// <summary>
    /// Resolves conflict by using the newest value based on modification timestamps.
    /// </summary>
    private ConflictResolution ResolveWithNewest(ConflictResolution conflict)
    {
        var localTimestamp = conflict.LocalModifiedAt ?? DateTime.MinValue;
        var notionTimestamp = conflict.NotionModifiedAt ?? DateTime.MinValue;

        if (notionTimestamp >= localTimestamp)
        {
            conflict.Resolve(
                conflict.NotionValue ?? string.Empty,
                ResolutionMethod.LastWrite,
                $"Resolved using Newest strategy: Notion value is newer " +
                $"(local: {localTimestamp:O}, notion: {notionTimestamp:O}). " +
                $"Discarded local value: \"{conflict.LocalValue}\"");
        }
        else
        {
            conflict.Resolve(
                conflict.LocalValue ?? string.Empty,
                ResolutionMethod.LastWrite,
                $"Resolved using Newest strategy: Local value is newer " +
                $"(local: {localTimestamp:O}, notion: {notionTimestamp:O}). " +
                $"Discarded Notion value: \"{conflict.NotionValue}\"");
        }

        return conflict;
    }
}

/// <summary>
/// Statistics about conflict resolution results.
/// </summary>
public class ConflictResolutionStats
{
    public int TotalConflicts { get; set; }
    public int ResolvedCount { get; set; }
    public int PendingReviewCount { get; set; }
    public int AbandonedCount { get; set; }
    public double ResolutionRate { get; set; }
}
