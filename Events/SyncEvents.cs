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
/// Published when a sync is initiated.
/// </summary>
public class SyncStartedEvent : ApplicationEvent
{
    public string SyncConfigId { get; set; } = string.Empty;
    public string DatabaseId { get; set; } = string.Empty;
    public DateTime StartTime { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Sync operation completed event.
/// Published when sync finishes successfully or with errors.
/// </summary>
public class SyncCompletedEvent : ApplicationEvent
{
    public string SyncConfigId { get; set; } = string.Empty;
    public int TasksProcessed { get; set; }
    public int ChangesDetected { get; set; }
    public int ConflictsResolved { get; set; }
    public TimeSpan Duration { get; set; }
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Conflict detected event.
/// Published when a sync conflict is discovered between local and remote.
/// </summary>
public class ConflictDetectedEvent : ApplicationEvent
{
    public Guid TaskId { get; set; }
    public string TaskTitle { get; set; } = string.Empty;
    public DateTime LocalModifiedAt { get; set; }
    public DateTime RemoteModifiedAt { get; set; }
    public string ConflictType { get; set; } = string.Empty; // e.g., "StatusMismatch", "PropertyMismatch"
    public Dictionary<string, object> LocalValues { get; set; } = new();
    public Dictionary<string, object> RemoteValues { get; set; } = new();
}

/// <summary>
/// Change detected event.
/// Published when a change is identified in local or remote storage.
/// </summary>
public class ChangeDetectedEvent : ApplicationEvent
{
    public Guid TaskId { get; set; }
    public string ChangeType { get; set; } = string.Empty; // "Created", "Updated", "Deleted"
    public string Source { get; set; } = string.Empty; // "Local" or "Remote"
    public DateTime ChangedAt { get; set; }
    public Dictionary<string, object?> ChangedProperties { get; set; } = new();
}

/// <summary>
/// Task synchronized event.
/// Published when a single task has been successfully synced.
/// </summary>
public class TaskSynchronizedEvent : ApplicationEvent
{
    public Guid TaskId { get; set; }
    public string TaskTitle { get; set; } = string.Empty;
    public string SyncDirection { get; set; } = string.Empty; // "LocalToRemote", "RemoteToLocal", "Bidirectional"
    public bool Successful { get; set; }
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Backup created event.
/// Published when a backup of tasks is created before sync.
/// </summary>
public class BackupCreatedEvent : ApplicationEvent
{
    public string BackupPath { get; set; } = string.Empty;
    public int TaskCount { get; set; }
    public long FileSizeBytes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Rate limit approached event.
/// Published when API rate limit is getting close.
/// </summary>
public class RateLimitWarningEvent : ApplicationEvent
{
    public string ApiService { get; set; } = string.Empty; // "Notion", "Google", etc.
    public int RequestsRemaining { get; set; }
    public int RequestLimit { get; set; }
    public DateTime ResetTime { get; set; }
}

/// <summary>
/// Sync configuration changed event.
/// Published when sync configuration is updated.
/// </summary>
public class ConfigurationChangedEvent : ApplicationEvent
{
    public string ConfigId { get; set; } = string.Empty;
    public string FieldName { get; set; } = string.Empty;
    public object? OldValue { get; set; }
    public object? NewValue { get; set; }
}

/// <summary>
/// Validation failed event.
/// Published when data validation fails during sync.
/// </summary>
public class ValidationFailedEvent : ApplicationEvent
{
    public Guid? TaskId { get; set; }
    public string ValidationType { get; set; } = string.Empty;
    public List<string> ErrorMessages { get; set; } = new();
    public Dictionary<string, object?> InvalidData { get; set; } = new();
}
