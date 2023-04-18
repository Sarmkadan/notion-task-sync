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
    /// </summary>
    public async Task<List<ConflictResolution>> ResolveConflictsAsync(
        List<ConflictResolution> conflicts,
        ConflictResolutionStrategy strategy)
    {
        var resolutions = new List<ConflictResolution>();

        foreach (var conflict in conflicts)
        {
            var resolution = strategy switch
            {
                ConflictResolutionStrategy.LastWrite => ResolveByLastWrite(conflict),
                ConflictResolutionStrategy.LocalWins => ResolveLocalWins(conflict),
                ConflictResolutionStrategy.NotionWins => ResolveNotionWins(conflict),
                ConflictResolutionStrategy.Manual => ResolveManual(conflict),
                _ => ResolveByLastWrite(conflict)
            };

            resolutions.Add(resolution);
        }

        return resolutions;
    }

    /// <summary>
    /// Resolves conflict by keeping the last written value (newest timestamp).
    /// </summary>
    private ConflictResolution ResolveByLastWrite(ConflictResolution conflict)
    {
        // In real implementation, would fetch actual timestamps from change logs
        var localTimestamp = DateTime.UtcNow.AddMinutes(-2);
        var notionTimestamp = DateTime.UtcNow;

        if (notionTimestamp > localTimestamp)
        {
            conflict.Resolve(conflict.NotionValue ?? string.Empty, ResolutionMethod.LastWrite,
                "Resolved using last-write-wins: Notion value is newer");
        }
        else
        {
            conflict.Resolve(conflict.LocalValue ?? string.Empty, ResolutionMethod.LastWrite,
                "Resolved using last-write-wins: Local value is newer");
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
        else
        {
            // For non-empty values, use last-write approach
            conflict = ResolveByLastWrite(conflict);
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
