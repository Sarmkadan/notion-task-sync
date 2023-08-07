#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace NotionTaskSync.Services;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NotionTaskSync.Domain.Enums;

/// <summary>
/// Extension methods for <see cref="SyncService.SyncResult"/> providing additional functionality
/// for sync operations analysis, reporting, and status monitoring.
/// </summary>
public static class SyncServiceExtensions
{
    /// <summary>
    /// Determines if the sync operation was successful.
    /// </summary>
    /// <param name="result">The sync result instance.</param>
    /// <returns>True if the sync completed successfully; otherwise, false.</returns>
    public static bool IsSuccessful(this SyncService.SyncResult result)
    {
        return result.Status == SyncStatus.Completed && result.ErrorMessage is null;
    }

    /// <summary>
    /// Gets the duration of the sync operation.
    /// </summary>
    /// <param name="result">The sync result instance.</param>
    /// <returns>The time span representing the sync duration, or null if not completed.</returns>
    public static TimeSpan? GetDuration(this SyncService.SyncResult result)
    {
        return result.Duration;
    }

    /// <summary>
    /// Calculates the total number of changes detected across both systems.
    /// </summary>
    /// <param name="result">The sync result instance.</param>
    /// <returns>The total number of changes detected (local + Notion).</returns>
    public static int GetTotalChangesDetected(this SyncService.SyncResult result)
    {
        return result.LocalChangesDetected + result.NotionChangesDetected;
    }

    /// <summary>
    /// Gets a summary of sync statistics as a formatted string.
    /// </summary>
    /// <param name="result">The sync result instance.</param>
    /// <returns>A formatted string containing key sync statistics.</returns>
    public static string GetSummary(this SyncService.SyncResult result)
    {
        var durationSec = result.Duration.HasValue ? result.Duration.Value.TotalSeconds.ToString("F1") : "?";

        return $"Sync completed: {result.Created} created, {result.Updated} updated, {result.Deleted} deleted, " +
               $"{result.Unchanged} unchanged, {result.ConflictsDetected} conflicted ({durationSec}s)";
    }

    /// <summary>
    /// Determines if there are any pending conflicts that require manual review.
    /// </summary>
    /// <param name="result">The sync result instance.</param>
    /// <returns>True if conflicts are pending review; otherwise, false.</returns>
    public static bool HasPendingConflicts(this SyncService.SyncResult result)
    {
        return result.ConflictsPendingReview > 0;
    }

    /// <summary>
    /// Calculates the sync completion percentage based on detected changes.
    /// </summary>
    /// <param name="result">The sync result instance.</param>
    /// <returns>A percentage value (0-100) representing sync completion, or 0 if no changes detected.</returns>
    public static int GetCompletionPercentage(this SyncService.SyncResult result)
    {
        var totalChanges = result.GetTotalChangesDetected();

        if (totalChanges == 0)
        {
            return 100; // No changes means fully synced
        }

        var resolvedConflicts = result.ConflictsResolved;
        var totalResolved = resolvedConflicts + (totalChanges - result.ConflictsDetected);

        // Calculate percentage based on resolved operations vs total changes
        var percentage = (int)Math.Round((double)totalResolved / totalChanges * 100);
        return Math.Clamp(percentage, 0, 100);
    }

    /// <summary>
    /// Gets the error message if the sync failed.
    /// </summary>
    /// <param name="result">The sync result instance.</param>
    /// <returns>The error message if sync failed; otherwise, null.</returns>
    public static string? GetErrorMessage(this SyncService.SyncResult result)
    {
        return result.Status == SyncStatus.Failed ? result.ErrorMessage : null;
    }

    /// <summary>
    /// Determines if significant changes were detected during the sync.
    /// </summary>
    /// <param name="result">The sync result instance.</param>
    /// <param name="threshold">The minimum number of changes to consider as significant (default: 10).</param>
    /// <returns>True if changes exceed the threshold; otherwise, false.</returns>
    public static bool HasSignificantChanges(this SyncService.SyncResult result, int threshold = 10)
    {
        return result.GetTotalChangesDetected() >= threshold;
    }

    /// <summary>
    /// Gets a formatted string with detailed sync statistics.
    /// </summary>
    /// <param name="result">The sync result instance.</param>
    /// <returns>A formatted string containing detailed sync statistics.</returns>
    public static string GetDetailedSummary(this SyncService.SyncResult result)
    {
        var durationText = result.Duration.HasValue ? $"{result.Duration.Value.TotalSeconds:F1}s" : "pending";

        return $"Sync Summary [Config: {result.ConfigId}]\n" +
               $"----------------------------------------\n" +
               $"Status: {result.Status}\n" +
               $"Duration: {durationText}\n" +
               $"Tasks: {result.LocalTaskCount} local, {result.NotionPageCount} Notion\n" +
               $"Changes: {result.GetTotalChangesDetected()} total ({result.LocalChangesDetected} local, {result.NotionChangesDetected} Notion)\n" +
               $"Conflicts: {result.ConflictsDetected} detected, {result.ConflictsResolved} resolved, {result.ConflictsPendingReview} pending\n" +
               $"Operations: {result.Created} created, {result.Updated} updated, {result.Deleted} deleted\n" +
               $"{(result.Status == SyncStatus.Failed ? $"Error: {result.ErrorMessage}" : "")}";
    }

    /// <summary>
    /// Filters a list of sync results to only include successful operations.
    /// </summary>
    /// <param name="results">The list of sync results.</param>
    /// <returns>A filtered list containing only successful sync results.</returns>
    public static IEnumerable<SyncService.SyncResult> WhereSuccessful(this IEnumerable<SyncService.SyncResult> results)
    {
        return results.Where(r => r.IsSuccessful());
    }

    /// <summary>
    /// Filters a list of sync results to only include failed operations.
    /// </summary>
    /// <param name="results">The list of sync results.</param>
    /// <returns>A filtered list containing only failed sync results.</returns>
    public static IEnumerable<SyncService.SyncResult> WhereFailed(this IEnumerable<SyncService.SyncResult> results)
    {
        return results.Where(r => r.Status == SyncStatus.Failed);
    }

    /// <summary>
    /// Orders sync results by completion time (most recent first).
    /// </summary>
    /// <param name="results">The list of sync results.</param>
    /// <returns>An ordered list of sync results.</returns>
    public static IOrderedEnumerable<SyncService.SyncResult> OrderByCompletion(this IEnumerable<SyncService.SyncResult> results)
    {
        return results.OrderByDescending(r => r.CompletedAt ?? r.StartedAt);
    }

    /// <summary>
    /// Gets the most recent sync result from a collection.
    /// </summary>
    /// <param name="results">The list of sync results.</param>
    /// <returns>The most recent sync result, or null if the collection is empty.</returns>
    public static SyncService.SyncResult? GetMostRecent(this IEnumerable<SyncService.SyncResult> results)
    {
        return results.OrderByCompletion().FirstOrDefault();
    }
}