// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace NotionTaskSync.Models;

using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Aggregates statistics about sync operations for monitoring and reporting.
/// Tracks success rates, performance metrics, and error patterns.
/// Used for generating health reports and identifying performance bottlenecks.
/// </summary>
public class SyncStatistics
{
    /// <summary>
    /// Total number of sync operations performed.
    /// </summary>
    public int TotalSyncs { get; set; }

    /// <summary>
    /// Number of successful sync operations.
    /// </summary>
    public int SuccessfulSyncs { get; set; }

    /// <summary>
    /// Number of failed sync operations.
    /// </summary>
    public int FailedSyncs { get; set; }

    /// <summary>
    /// Total tasks synced across all operations.
    /// </summary>
    public int TotalTasksSynced { get; set; }

    /// <summary>
    /// Total conflicts detected across all operations.
    /// </summary>
    public int TotalConflicts { get; set; }

    /// <summary>
    /// Total conflicts successfully resolved.
    /// </summary>
    public int ResolvedConflicts { get; set; }

    /// <summary>
    /// Collection of sync operation results for individual tracking.
    /// </summary>
    public List<SyncOperationSnapshot> Operations { get; set; } = new();

    /// <summary>
    /// Timestamp when statistics were last reset.
    /// </summary>
    public DateTime LastResetAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets the success rate as a percentage (0-100).
    /// </summary>
    public double SuccessRate =>
        TotalSyncs > 0 ? (SuccessfulSyncs * 100.0 / TotalSyncs) : 0;

    /// <summary>
    /// Gets the average duration of sync operations.
    /// </summary>
    public TimeSpan AverageSyncDuration
    {
        get
        {
            if (Operations.Count == 0)
                return TimeSpan.Zero;

            var avgMs = Operations.Average(o => o.DurationMs);
            return TimeSpan.FromMilliseconds(avgMs);
        }
    }

    /// <summary>
    /// Gets the conflict resolution rate as a percentage.
    /// </summary>
    public double ConflictResolutionRate =>
        TotalConflicts > 0 ? (ResolvedConflicts * 100.0 / TotalConflicts) : 0;

    /// <summary>
    /// Records a new sync operation result.
    /// </summary>
    public void RecordOperation(SyncOperationSnapshot snapshot)
    {
        Operations.Add(snapshot);
        TotalSyncs++;

        if (snapshot.Successful)
            SuccessfulSyncs++;
        else
            FailedSyncs++;

        TotalTasksSynced += snapshot.TasksProcessed;
        TotalConflicts += snapshot.ConflictsDetected;
        ResolvedConflicts += snapshot.ConflictsResolved;

        // Keep only last 100 operations to prevent unbounded growth
        if (Operations.Count > 100)
        {
            Operations.RemoveRange(0, Operations.Count - 100);
        }
    }

    /// <summary>
    /// Resets all statistics to initial state.
    /// </summary>
    public void Reset()
    {
        TotalSyncs = 0;
        SuccessfulSyncs = 0;
        FailedSyncs = 0;
        TotalTasksSynced = 0;
        TotalConflicts = 0;
        ResolvedConflicts = 0;
        Operations.Clear();
        LastResetAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Gets a summary of statistics as a formatted string.
    /// </summary>
    public override string ToString()
    {
        return $@"Sync Statistics:
  Total Operations: {TotalSyncs}
  Successful: {SuccessfulSyncs} ({SuccessRate:F1}%)
  Failed: {FailedSyncs}
  Total Tasks Synced: {TotalTasksSynced}
  Average Duration: {AverageSyncDuration:mm\\:ss}
  Conflicts Detected: {TotalConflicts}
  Conflicts Resolved: {ResolvedConflicts} ({ConflictResolutionRate:F1}%)
  Last Reset: {LastResetAt:g}";
    }
}

/// <summary>
/// Snapshot of a single sync operation result.
/// Allows historical analysis of sync performance.
/// </summary>
public class SyncOperationSnapshot
{
    /// <summary>
    /// When the sync operation occurred.
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// How long the sync took in milliseconds.
    /// </summary>
    public long DurationMs { get; set; }

    /// <summary>
    /// Whether the operation completed successfully.
    /// </summary>
    public bool Successful { get; set; }

    /// <summary>
    /// Number of tasks processed in this operation.
    /// </summary>
    public int TasksProcessed { get; set; }

    /// <summary>
    /// Number of changes detected.
    /// </summary>
    public int ChangesDetected { get; set; }

    /// <summary>
    /// Number of conflicts encountered.
    /// </summary>
    public int ConflictsDetected { get; set; }

    /// <summary>
    /// Number of conflicts resolved.
    /// </summary>
    public int ConflictsResolved { get; set; }

    /// <summary>
    /// Error message if operation failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Creates a snapshot from a sync result.
    /// </summary>
    public static SyncOperationSnapshot FromSyncResult(Services.SyncService.SyncResult result)
    {
        return new SyncOperationSnapshot
        {
            Timestamp = result.StartedAt,
            DurationMs = (long)(result.Duration?.TotalMilliseconds ?? 0),
            Successful = result.Status == Domain.Enums.SyncStatus.Completed,
            TasksProcessed = result.LocalTaskCount,
            ChangesDetected = result.LocalChangesDetected + result.NotionChangesDetected,
            ConflictsDetected = result.ConflictsDetected,
            ConflictsResolved = result.ConflictsResolved,
            ErrorMessage = result.ErrorMessage
        };
    }
}
