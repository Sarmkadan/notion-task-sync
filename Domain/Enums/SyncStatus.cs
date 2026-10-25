#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace NotionTaskSync.Domain.Enums;

/// <summary>
/// Represents the current status of a sync operation.
/// </summary>
/// <remarks>
/// The numeric values are stable and may be persisted or compared across releases; do not renumber members.
/// </remarks>
public enum SyncStatus
{
    /// <summary>
    /// The sync operation has been created but has not started yet.
    /// </summary>
    Pending = 0,

    /// <summary>
    /// The sync operation is currently in progress.
    /// </summary>
    Running = 1,

    /// <summary>
    /// The sync operation finished successfully.
    /// </summary>
    Completed = 2,

    /// <summary>
    /// The sync operation finished with errors; see the accompanying error message and details.
    /// </summary>
    Failed = 3,

    /// <summary>
    /// The sync operation was manually stopped before it completed.
    /// </summary>
    Cancelled = 4,

    /// <summary>
    /// The sync operation is suspended and waiting for manual intervention before it can continue.
    /// </summary>
    Paused = 5
}
