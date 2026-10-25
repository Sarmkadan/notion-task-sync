#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace NotionTaskSync.Events;

using System;
using System.Collections.Generic;
using NotionTaskSync.Domain.Models;

/// <summary>
/// Sync operation started event.
/// Raised once at the beginning of a sync cycle, before any tasks are read or compared
/// (published by <c>SyncWorker</c>).
/// </summary>
public class SyncStartedEvent : ApplicationEvent
{
    /// <summary>Identifier of the sync configuration that is being run.</summary>
    public string SyncConfigId { get; set; } = string.Empty;

    /// <summary>Notion database that the sync cycle targets.</summary>
    public string DatabaseId { get; set; } = string.Empty;

    /// <summary>UTC time at which the sync cycle started.</summary>
    public DateTime StartTime { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Sync operation completed event.
/// Raised once when a sync cycle ends, whether it finished successfully or with errors
/// (published by <c>SyncWorker</c>). This is the terminal event for a sync cycle; see
/// <c>SyncEventExtensions.IsTerminal</c>.
/// </summary>
public class SyncCompletedEvent : ApplicationEvent
{
    /// <summary>Identifier of the sync configuration that was run.</summary>
    public string SyncConfigId { get; set; } = string.Empty;

    /// <summary>Number of tasks processed during the cycle.</summary>
    public int TasksProcessed { get; set; }

    /// <summary>Number of local or remote changes detected during the cycle.</summary>
    public int ChangesDetected { get; set; }

    /// <summary>Number of conflicts resolved during the cycle.</summary>
    public int ConflictsResolved { get; set; }

    /// <summary>Wall-clock time the cycle took.</summary>
    public TimeSpan Duration { get; set; }

    /// <summary><c>true</c> if the cycle completed without errors; otherwise <c>false</c>.</summary>
    public bool Success { get; set; }

    /// <summary>Error message when the cycle failed; <c>null</c> on success.</summary>
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Conflict detected event.
/// Raised when the local and remote versions of the same task have diverged in a way that
/// cannot be merged automatically, before the conflict is resolved.
/// </summary>
public class ConflictDetectedEvent : ApplicationEvent
{
    /// <summary>Identifier of the conflicting task.</summary>
    public Guid TaskId { get; set; }

    /// <summary>Title of the conflicting task, for display and logging.</summary>
    public string TaskTitle { get; set; } = string.Empty;

    /// <summary>Last modification time of the local copy.</summary>
    public DateTime LocalModifiedAt { get; set; }

    /// <summary>Last modification time of the remote (Notion) copy.</summary>
    public DateTime RemoteModifiedAt { get; set; }

    /// <summary>Kind of conflict, e.g. <c>"StatusMismatch"</c> or <c>"PropertyMismatch"</c>.</summary>
    public string ConflictType { get; set; } = string.Empty; // e.g., "StatusMismatch", "PropertyMismatch"

    /// <summary>Conflicting property values from the local side, keyed by property name.</summary>
    public Dictionary<string, object> LocalValues { get; set; } = new();

    /// <summary>Conflicting property values from the remote side, keyed by property name.</summary>
    public Dictionary<string, object> RemoteValues { get; set; } = new();
}

/// <summary>
/// Change detected event.
/// Raised when a create, update or delete is identified in local or remote storage. For
/// remote changes it is raised by the webhook handler when Notion reports a page event.
/// </summary>
public class ChangeDetectedEvent : ApplicationEvent
{
    /// <summary>Identifier of the task that changed.</summary>
    public Guid TaskId { get; set; }

    /// <summary>Kind of change: <c>"Created"</c>, <c>"Updated"</c> or <c>"Deleted"</c>.</summary>
    public string ChangeType { get; set; } = string.Empty; // "Created", "Updated", "Deleted"

    /// <summary>Where the change was found: <c>"Local"</c> or <c>"Remote"</c>.</summary>
    public new string Source { get; set; } = string.Empty; // "Local" or "Remote"

    /// <summary>Time at which the change occurred.</summary>
    public DateTime ChangedAt { get; set; }

    /// <summary>Changed property values, keyed by property name; <c>null</c> values are allowed.</summary>
    public Dictionary<string, object?> ChangedProperties { get; set; } = new();
}

/// <summary>
/// Task synchronized event.
/// Raised when a single task's synchronization attempt finishes, whether it succeeded or
/// failed. Check <see cref="Successful"/> to tell the two apart.
/// </summary>
public class TaskSynchronizedEvent : ApplicationEvent
{
    /// <summary>Identifier of the task that was synchronized.</summary>
    public Guid TaskId { get; set; }

    /// <summary>Title of the task, for display and logging.</summary>
    public string TaskTitle { get; set; } = string.Empty;

    /// <summary>Direction of the sync: <c>"LocalToRemote"</c>, <c>"RemoteToLocal"</c> or <c>"Bidirectional"</c>.</summary>
    public string SyncDirection { get; set; } = string.Empty; // "LocalToRemote", "RemoteToLocal", "Bidirectional"

    /// <summary><c>true</c> if the task was synchronized without errors; otherwise <c>false</c>.</summary>
    public bool Successful { get; set; }

    /// <summary>Error message when the task failed to synchronize; <c>null</c> on success.</summary>
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Backup created event.
/// Raised after a backup of the task files has been written.
/// </summary>
public class BackupCreatedEvent : ApplicationEvent
{
    /// <summary>Path of the backup file or directory that was written.</summary>
    public string BackupPath { get; set; } = string.Empty;

    /// <summary>Number of tasks included in the backup.</summary>
    public int TaskCount { get; set; }

    /// <summary>Size of the backup in bytes.</summary>
    public long FileSizeBytes { get; set; }

    /// <summary>UTC time at which the backup was created.</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Rate limit approached event.
/// Raised when the number of requests left in the current API rate-limit window drops
/// low enough to warrant a warning (published by <c>RateLimitingMiddleware</c>).
/// </summary>
public class RateLimitWarningEvent : ApplicationEvent
{
    /// <summary>Name of the API the limit applies to, e.g. <c>"Notion"</c> or <c>"Google"</c>.</summary>
    public string ApiService { get; set; } = string.Empty; // "Notion", "Google", etc.

    /// <summary>Requests still available in the current window; never negative.</summary>
    public int RequestsRemaining { get; set; }

    /// <summary>Total requests allowed per window.</summary>
    public int RequestLimit { get; set; }

    /// <summary>UTC time at which the current window resets.</summary>
    public DateTime ResetTime { get; set; }
}

/// <summary>
/// Sync configuration changed event.
/// Raised each time a single field of a sync configuration is updated, with the old and
/// new values.
/// </summary>
public class ConfigurationChangedEvent : ApplicationEvent
{
    /// <summary>Identifier of the configuration that was changed.</summary>
    public string ConfigId { get; set; } = string.Empty;

    /// <summary>Name of the field that changed.</summary>
    public string FieldName { get; set; } = string.Empty;

    /// <summary>Value of the field before the change; <c>null</c> if it was unset.</summary>
    public object? OldValue { get; set; }

    /// <summary>Value of the field after the change; <c>null</c> if it was cleared.</summary>
    public object? NewValue { get; set; }
}

/// <summary>
/// Validation failed event.
/// Raised when data fails a validation check during sync. It carries the error messages
/// and the offending data.
/// </summary>
public class ValidationFailedEvent : ApplicationEvent
{
    /// <summary>Identifier of the task that failed validation, or <c>null</c> if the failure is not tied to one task.</summary>
    public Guid? TaskId { get; set; }

    /// <summary>Kind of validation that failed.</summary>
    public string ValidationType { get; set; } = string.Empty;

    /// <summary>Human-readable descriptions of each validation error.</summary>
    public List<string> ErrorMessages { get; set; } = new();

    /// <summary>Data that failed validation, keyed by property name.</summary>
    public Dictionary<string, object?> InvalidData { get; set; } = new();
}
