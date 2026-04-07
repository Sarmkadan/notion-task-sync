#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace NotionTaskSync.Domain.Enums;

/// <summary>
/// Represents the current status of a sync operation.
/// </summary>
public enum SyncStatus
{
    /// <summary>Sync has not started yet.</summary>
    Pending = 0,

    /// <summary>Sync operation is currently in progress.</summary>
    Running = 1,

    /// <summary>Sync completed successfully.</summary>
    Completed = 2,

    /// <summary>Sync encountered errors during execution.</summary>
    Failed = 3,

    /// <summary>Sync was manually stopped before completion.</summary>
    Cancelled = 4,

    /// <summary>Sync is paused waiting for manual intervention.</summary>
    Paused = 5
}
