#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

namespace NotionTaskSync.Services;

/// <summary>
/// Defines strategies for resolving conflicts between local and remote (Notion) values.
/// </summary>
public enum ConflictStrategy
{
    /// <summary>
    /// Prefer the local value over the remote value.
    /// </summary>
    PreferLocal = 0,

    /// <summary>
    /// Prefer the remote (Notion) value over the local value.
    /// </summary>
    PreferRemote = 1,

    /// <summary>
    /// Use the newest value based on modification timestamps (last-write-wins).
    /// </summary>
    Newest = 2
}