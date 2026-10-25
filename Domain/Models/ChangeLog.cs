#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace NotionTaskSync.Domain.Models;

using System;
using System.ComponentModel.DataAnnotations;

/// <summary>
/// Records changes made to tasks for audit trail and conflict detection.
/// Enables tracking of what changed, when, and from which source.
/// </summary>
public class ChangeLog
{
    /// <summary>
    /// Unique identifier of this change log entry.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Identifier of the task this change applies to. Must not be <see cref="Guid.Empty"/>.
    /// </summary>
    [Required]
    public required Guid TaskId { get; set; }

    /// <summary>
    /// Kind of change that occurred, such as Created, Updated, Deleted, or Synced.
    /// Maximum length is 100 characters.
    /// </summary>
    [Required]
    [StringLength(100)]
    public required string ChangeType { get; set; }

    /// <summary>
    /// Name of the task property that changed, if the change applies to a single property.
    /// Maximum length is 200 characters.
    /// </summary>
    [StringLength(200)]
    public string? PropertyName { get; set; }

    /// <summary>
    /// Value of the property before the change. Maximum length is 1000 characters.
    /// </summary>
    [StringLength(1000)]
    public string? OldValue { get; set; }

    /// <summary>
    /// Value of the property after the change. Maximum length is 1000 characters.
    /// </summary>
    [StringLength(1000)]
    public string? NewValue { get; set; }

    /// <summary>
    /// Side that produced the change: local files, Notion, or the system itself.
    /// </summary>
    public ChangeSource Source { get; set; }

    /// <summary>
    /// UTC time the change was recorded. Defaults to the time of creation.
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Email address of the user associated with the change, if known. Maximum length is 256 characters.
    /// </summary>
    [StringLength(256)]
    public string? UserEmail { get; set; }

    /// <summary>
    /// Free-text description of the change. Maximum length is 500 characters.
    /// </summary>
    [StringLength(500)]
    public string? Description { get; set; }

    /// <summary>
    /// Whether this change conflicts with another change and requires resolution.
    /// Set by <see cref="MarkAsConflict(string)"/>.
    /// </summary>
    public bool IsConflict { get; set; }

    /// <summary>
    /// Name of the strategy used to resolve the conflict, or <c>null</c> if none has been assigned.
    /// Set by <see cref="MarkAsConflict(string)"/>.
    /// </summary>
    public string? ConflictResolutionStrategy { get; set; }

    /// <summary>
    /// Validates the change log entry ensuring required fields are populated.
    /// </summary>
    public bool Validate()
    {
        if (TaskId == Guid.Empty)
            return false;

        if (string.IsNullOrWhiteSpace(ChangeType) || ChangeType.Length > 100)
            return false;

        if (Timestamp > DateTime.UtcNow)
            return false;

        return true;
    }

    /// <summary>
    /// Creates a summary string of the change for logging purposes.
    /// </summary>
    public string GetSummary()
    {
        var summary = $"{ChangeType} from {Source}";

        if (!string.IsNullOrEmpty(PropertyName))
            summary += $": {PropertyName}";

        if (!string.IsNullOrEmpty(OldValue) && !string.IsNullOrEmpty(NewValue))
            summary += $" ({OldValue} -> {NewValue})";

        return summary;
    }

    /// <summary>
    /// Marks the change log entry as a conflict requiring resolution.
    /// </summary>
    public void MarkAsConflict(string resolutionStrategy)
    {
        IsConflict = true;
        ConflictResolutionStrategy = resolutionStrategy;
    }

    /// <summary>
    /// Determines if this change occurred within a specified time window.
    /// Useful for grouping related changes.
    /// </summary>
    public bool IsWithinTimeWindow(TimeSpan window)
    {
        return DateTime.UtcNow - Timestamp <= window;
    }
}

public enum ChangeSource
{
    Local = 0,
    Notion = 1,
    System = 2
}
