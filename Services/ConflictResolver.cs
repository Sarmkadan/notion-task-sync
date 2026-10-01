#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace NotionTaskSync.Services;

using NotionTaskSync.Domain.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Applies conflict resolution strategies to individual property-level conflicts.
/// Works alongside <see cref="ConflictResolutionService"/> as the per-item resolution engine,
/// supporting last-write-wins, local-priority, notion-priority, and manual strategies.
/// </summary>
public sealed class ConflictResolver
{
    private readonly ILogger<ConflictResolver> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConflictResolver"/> class.
    /// </summary>
    /// <param name="logger">Logger for resolution diagnostics.</param>
    public ConflictResolver(ILogger<ConflictResolver> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Resolves a single conflict using the specified resolution method.
    /// Mutates the <see cref="ConflictResolution"/> in place, setting
    /// <see cref="ConflictResolution.ResolvedValue"/>, <see cref="ConflictResolution.Status"/>, and timestamps.
    /// </summary>
    /// <param name="conflict">The conflict to resolve.</param>
    /// <param name="method">The strategy to apply.</param>
    /// <returns>The resolved value that was chosen.</returns>
    public string? Resolve(ConflictResolution conflict, ResolutionMethod method)
    {
        ArgumentNullException.ThrowIfNull(conflict);

        _logger.LogDebug("Resolving conflict {ConflictId} for task {TaskId}, property '{Property}' with strategy {Method}",
            conflict.Id, conflict.TaskId, conflict.PropertyName, method);

        conflict.ResolutionMethod = method;
        conflict.ResolvedValue = method switch
        {
            ResolutionMethod.LocalWins => conflict.LocalValue,
            ResolutionMethod.NotionWins => conflict.NotionValue,
            ResolutionMethod.LastWrite => PickLastWrite(conflict),
            ResolutionMethod.Manual => null, // left for manual resolution
            _ => conflict.NotionValue, // safe default
        };

        if (method != ResolutionMethod.Manual)
        {
            conflict.Status = ResolutionStatus.Resolved;
            conflict.ResolvedAt = DateTime.UtcNow;
        }

        _logger.LogInformation("Conflict {ConflictId} resolved: chose '{ResolvedValue}' via {Method}",
            conflict.Id, conflict.ResolvedValue, method);

        return conflict.ResolvedValue;
    }

    /// <summary>
    /// Resolves a batch of conflicts using the same strategy.
    /// </summary>
    /// <param name="conflicts">Conflicts to resolve.</param>
    /// <param name="method">Strategy to apply uniformly.</param>
    /// <returns>Number of conflicts that were successfully resolved (excludes <see cref="ResolutionMethod.Manual"/>).</returns>
    public int ResolveBatch(IEnumerable<ConflictResolution> conflicts, ResolutionMethod method)
    {
        ArgumentNullException.ThrowIfNull(conflicts);

        int resolved = 0;
        foreach (var conflict in conflicts)
        {
            Resolve(conflict, method);
            if (conflict.Status == ResolutionStatus.Resolved)
                resolved++;
        }

        _logger.LogInformation("Batch resolution complete: {Resolved} conflicts resolved via {Method}", resolved, method);
        return resolved;
    }

    /// <summary>
    /// Determines the appropriate resolution method based on conflict type and configuration.
    /// </summary>
    /// <param name="conflictType">The type of conflict detected.</param>
    /// <param name="defaultStrategy">The default strategy from configuration.</param>
    /// <returns>The resolution method to use.</returns>
    public ResolutionMethod DetermineStrategy(ConflictType conflictType, string defaultStrategy)
    {
        // Delete conflicts always require manual review
        if (conflictType == ConflictType.DeletionConflict)
            return ResolutionMethod.Manual;

        return defaultStrategy?.ToUpperInvariant() switch
        {
            "LOCALWINS" or "LOCAL" => ResolutionMethod.LocalWins,
            "NOTIONWINS" or "NOTION" => ResolutionMethod.NotionWins,
            "LASTWRITE" or "LASTWRITEWINS" => ResolutionMethod.LastWrite,
            "MANUAL" => ResolutionMethod.Manual,
            _ => ResolutionMethod.LastWrite,
        };
    }

    /// <summary>
    /// Returns all unresolved (pending) conflicts from a list.
    /// </summary>
    /// <param name="conflicts">Source conflict list.</param>
    /// <returns>Only the conflicts whose status is <see cref="ResolutionStatus.Pending"/>.</returns>
    public IReadOnlyList<ConflictResolution> GetPending(IEnumerable<ConflictResolution> conflicts)
    {
        ArgumentNullException.ThrowIfNull(conflicts);
        return conflicts.Where(c => c.Status == ResolutionStatus.Pending).ToList();
    }

    private string? PickLastWrite(ConflictResolution conflict)
    {
        if (conflict.LocalModifiedAt is null)
            return conflict.NotionValue;

        if (conflict.DetectedAt <= conflict.LocalModifiedAt.Value)
            return conflict.LocalValue;

        return conflict.NotionValue;
    }
}
