#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace NotionTaskSync.Events;

using System;

/// <summary>
/// Extension methods for <see cref="ApplicationEvent"/> and its derived types.
/// Provides convenient helpers for logging, duration calculation, and terminal event detection.
/// </summary>
public static class SyncEventExtensions
{
    /// <summary>
    /// Returns the duration of a completed sync event.
    /// For <see cref="SyncCompletedEvent"/> this is the <see cref="SyncCompletedEvent.Duration"/> property.
    /// For all other event types the result is <c>null</c>.
    /// </summary>
    /// <param name="evt">The event instance.</param>
    /// <returns>The duration if available; otherwise <c>null</c>.</returns>
    public static TimeSpan? Duration(this ApplicationEvent evt)
    {
        return evt switch
        {
            SyncCompletedEvent completed => completed.Duration,
            _ => null
        };
    }

    /// <summary>
    /// Produces a one‑line, human‑readable summary of the event suitable for logging.
    /// The format varies per event type to include the most relevant information.
    /// </summary>
    /// <param name="evt">The event instance.</param>
    /// <returns>A concise log string.</returns>
    public static string ToLogString(this ApplicationEvent evt)
    {
        return evt switch
        {
            SyncStartedEvent started => $"SyncStarted: Config={started.SyncConfigId}, DB={started.DatabaseId}, Start={started.StartTime:O}",
            SyncCompletedEvent completed => $"SyncCompleted: Config={completed.SyncConfigId}, Tasks={completed.TasksProcessed}, Changes={completed.ChangesDetected}, Conflicts={completed.ConflictsResolved}, Duration={completed.Duration}, Success={completed.Success}, Error={completed.ErrorMessage ?? "none"}",
            ConflictDetectedEvent conflict => $"ConflictDetected: Task={conflict.TaskId}, Title={conflict.TaskTitle}, Type={conflict.ConflictType}, Local={conflict.LocalModifiedAt:O}, Remote={conflict.RemoteModifiedAt:O}",
            ChangeDetectedEvent change => $"ChangeDetected: Task={change.TaskId}, Type={change.ChangeType}, Source={change.Source}, At={change.ChangedAt:O}",
            TaskSynchronizedEvent synced => $"TaskSynchronized: Task={synced.TaskId}, Title={synced.TaskTitle}, Direction={synced.SyncDirection}, Success={synced.Successful}, Error={synced.ErrorMessage ?? "none"}",
            BackupCreatedEvent backup => $"BackupCreated: Path={backup.BackupPath}, Count={backup.TaskCount}, Size={backup.FileSizeBytes} bytes, Created={backup.CreatedAt:O}",
            RateLimitWarningEvent rateLimit => $"RateLimitWarning: Service={rateLimit.ApiService}, Remaining={rateLimit.RequestsRemaining}, Limit={rateLimit.RequestLimit}, Reset={rateLimit.ResetTime:O}",
            ConfigurationChangedEvent configChanged => $"ConfigurationChanged: Config={configChanged.ConfigId}, Field={configChanged.FieldName}, Old={configChanged.OldValue ?? "null"}, New={configChanged.NewValue ?? "null"}",
            ValidationFailedEvent validation => $"ValidationFailed: Task={validation.TaskId?.ToString() ?? "null"}, Type={validation.ValidationType}, Errors=[{string.Join(",", validation.ErrorMessages)}]",
            _ => $"{evt.GetType().Name}"
        };
    }

    /// <summary>
    /// Indicates whether the event marks the end of a sync run.
    /// Currently only <see cref="SyncCompletedEvent"/> is considered terminal.
    /// </summary>
    /// <param name="evt">The event instance.</param>
    /// <returns><c>true</c> if the event ends a sync run; otherwise <c>false</c>.</returns>
    public static bool IsTerminal(this ApplicationEvent evt)
    {
        return evt is SyncCompletedEvent;
    }
}
