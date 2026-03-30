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
    public Guid Id { get; set; }

    [Required]
    public required Guid TaskId { get; set; }

    [Required]
    [StringLength(100)]
    public required string ChangeType { get; set; } // Created, Updated, Deleted, Synced

    [StringLength(200)]
    public string? PropertyName { get; set; }

    [StringLength(1000)]
    public string? OldValue { get; set; }

    [StringLength(1000)]
    public string? NewValue { get; set; }

    public ChangeSource Source { get; set; } // Local or Notion

    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    [StringLength(256)]
    public string? UserEmail { get; set; }

    [StringLength(500)]
    public string? Description { get; set; }

    public bool IsConflict { get; set; }

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
