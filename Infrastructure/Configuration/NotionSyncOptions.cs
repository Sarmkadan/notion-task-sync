#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace NotionTaskSync.Infrastructure.Configuration;

using System;
using NotionTaskSync.Domain.Models;

/// <summary>
/// Startup options for the Notion sync, bound from the <c>NotionSync</c> configuration section.
/// Checked by <see cref="NotionSyncOptionsValidator"/> when registered via <c>AddNotionTaskSync</c>.
/// </summary>
public sealed class NotionSyncOptions
{
    /// <summary>
    /// Name of the configuration section these options are bound from.
    /// </summary>
    public const string SectionName = "NotionSync";

    /// <summary>
    /// Gets or sets the Notion integration token. Required; must not be blank.
    /// </summary>
    public string? ApiToken { get; set; }

    /// <summary>
    /// Gets or sets the Notion database ID to sync. Required; a UUID with or without dashes.
    /// </summary>
    public string? DatabaseId { get; set; }

    /// <summary>
    /// Gets or sets the interval between sync cycles. Must be at least one minute.
    /// </summary>
    public TimeSpan SyncInterval { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Gets or sets the conflict resolution strategy name (<c>LastWrite</c>, <c>LocalWins</c>,
    /// <c>NotionWins</c> or <c>Manual</c>). Matched case-insensitively.
    /// </summary>
    public string ConflictStrategy { get; set; } = nameof(ConflictResolutionStrategy.LastWrite);

    /// <summary>
    /// Gets or sets the local directory where task files are stored. Required; must be an absolute path.
    /// </summary>
    public string? LocalStorePath { get; set; }
}
