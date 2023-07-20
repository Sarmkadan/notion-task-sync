#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace NotionTaskSync.Domain.Models;

using System;
using System.Text;

/// <summary>
/// Extension methods for ChangeLog providing additional functionality for audit trail operations.
/// </summary>
public static class ChangeLogExtensions
{
    /// <summary>
    /// Creates a detailed audit trail entry string for logging and reporting purposes.
    /// Includes all relevant change information in a human-readable format.
    /// </summary>
    /// <param name="changeLog">The change log entry to format</param>
    /// <param name="includeValues">Whether to include old/new value comparisons</param>
    /// <returns>Formatted audit string</returns>
    public static string ToAuditString(this ChangeLog changeLog, bool includeValues = true)
    {
        if (changeLog == null)
            throw new ArgumentNullException(nameof(changeLog));

        var sb = new StringBuilder();
        sb.AppendLine($"Change Log Entry: {changeLog.Id}");
        sb.AppendLine($"  Task ID: {changeLog.TaskId}");
        sb.AppendLine($"  Type: {changeLog.ChangeType}");
        sb.AppendLine($"  Source: {changeLog.Source}");
        sb.AppendLine($"  Timestamp: {changeLog.Timestamp:yyyy-MM-dd HH:mm:ss UTC}");

        if (!string.IsNullOrEmpty(changeLog.UserEmail))
            sb.AppendLine($"  User: {changeLog.UserEmail}");

        if (!string.IsNullOrEmpty(changeLog.PropertyName))
            sb.AppendLine($"  Property: {changeLog.PropertyName}");

        if (includeValues && !string.IsNullOrEmpty(changeLog.OldValue) && !string.IsNullOrEmpty(changeLog.NewValue))
        {
            sb.AppendLine($"  Old Value: {changeLog.OldValue}");
            sb.AppendLine($"  New Value: {changeLog.NewValue}");
        }

        if (!string.IsNullOrEmpty(changeLog.Description))
            sb.AppendLine($"  Description: {changeLog.Description}");

        if (changeLog.IsConflict)
        {
            sb.AppendLine("  CONFLICT STATUS:");
            sb.AppendLine($"    Resolved: {changeLog.ConflictResolutionStrategy ?? "Unknown"}");
        }

        return sb.ToString().Trim();
    }

    /// <summary>
    /// Determines if this change represents a property modification.
    /// Useful for filtering change logs to only property changes.
    /// </summary>
    /// <param name="changeLog">The change log entry to check</param>
    /// <returns>True if this is a property change, false otherwise</returns>
    public static bool IsPropertyChange(this ChangeLog changeLog)
    {
        if (changeLog == null)
            throw new ArgumentNullException(nameof(changeLog));

        return !string.IsNullOrEmpty(changeLog.PropertyName);
    }

    /// <summary>
    /// Creates a simplified change description suitable for UI display or notifications.
    /// </summary>
    /// <param name="changeLog">The change log entry</param>
    /// <param name="maxLength">Maximum length of the resulting string</param>
    /// <returns>Simplified change description</returns>
    public static string ToDisplayString(this ChangeLog changeLog, int maxLength = 100)
    {
        if (changeLog == null)
            throw new ArgumentNullException(nameof(changeLog));

        var summary = changeLog.GetSummary();

        if (changeLog.IsConflict)
            summary = "⚠️ " + summary + " (Conflict: " + (changeLog.ConflictResolutionStrategy ?? "Pending") + ")";

        if (summary.Length > maxLength)
            summary = summary.Substring(0, maxLength - 3) + "...";

        return summary;
    }

    /// <summary>
    /// Determines if this change was made by a user (has UserEmail populated).
    /// Useful for distinguishing system changes from user-initiated changes.
    /// </summary>
    /// <param name="changeLog">The change log entry to check</param>
    /// <returns>True if this change was made by a user</returns>
    public static bool IsUserInitiated(this ChangeLog changeLog)
    {
        if (changeLog == null)
            throw new ArgumentNullException(nameof(changeLog));

        return !string.IsNullOrEmpty(changeLog.UserEmail);
    }
}