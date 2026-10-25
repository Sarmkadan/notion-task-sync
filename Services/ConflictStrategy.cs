#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

namespace NotionTaskSync.Services;

/// <summary>
/// Defines strategies for resolving conflicts between local and remote (Notion) values.
/// </summary>
/// <remarks>
/// The strategy is passed to <see cref="ConflictResolutionService.ResolveWith"/>. Any value outside the
/// defined members falls back to <see cref="Newest"/>.
/// </remarks>
public enum ConflictStrategy
{
    /// <summary>
    /// Prefer the local value over the remote value. The conflict is resolved as <c>LocalWins</c>.
    /// </summary>
    PreferLocal = 0,

    /// <summary>
    /// Prefer the remote (Notion) value over the local value. The conflict is resolved as <c>NotionWins</c>.
    /// </summary>
    PreferRemote = 1,

    /// <summary>
    /// Use the newest value based on modification timestamps (last-write-wins).
    /// This is also the fallback for unrecognized strategy values.
    /// </summary>
    Newest = 2
}