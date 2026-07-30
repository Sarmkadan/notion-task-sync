#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace NotionTaskSync.Events;

using System;

/// <summary>
/// Extension methods for <see cref="SyncCompletedEvent"/> providing concise
/// summaries and detailed reports.
/// </summary>
public static class SyncCompletedEventExtensions
{
    /// <summary>
    /// Produces a one‑line summary of the sync result.
    /// </summary>
    /// <param name="evt">The sync completed event.</param>
    /// <returns>A single line string summarizing the event.</returns>
    public static string ToOneLineSummary(this SyncCompletedEvent evt)
    {
        if (evt == null) throw new ArgumentNullException(nameof(evt));

        var status = evt.Success ? "Success" : "Failed";
        var error = string.IsNullOrWhiteSpace(evt.ErrorMessage) ? "" : $" Error: {evt.ErrorMessage}";
        return $"{evt.Timestamp:O} | {evt.Duration.TotalMilliseconds}ms | {evt.TasksProcessed} tasks | {evt.ChangesDetected} changes | {evt.ConflictsResolved} conflicts | {status}{error}";
    }

    /// <summary>
    /// Produces a detailed multi‑line report of the sync result.
    /// </summary>
    /// <param name="evt">The sync completed event.</param>
    /// <returns>A multi‑line string containing all relevant details.</returns>
    public static string ToDetailedReport(this SyncCompletedEvent evt)
    {
        if (evt == null) throw new ArgumentNullException(nameof(evt));

        var status = evt.Success ? "Success" : "Failed";
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"Event ID: {evt.EventId}");
        sb.AppendLine($"Timestamp: {evt.Timestamp:O}");
        sb.AppendLine($"Duration: {evt.Duration.TotalMilliseconds} ms");
        sb.AppendLine($"Status: {status}");
        sb.AppendLine($"Tasks Processed: {evt.TasksProcessed}");
        sb.AppendLine($"Changes Detected: {evt.ChangesDetected}");
        sb.AppendLine($"Conflicts Resolved: {evt.ConflictsResolved}");
        if (!string.IsNullOrWhiteSpace(evt.ErrorMessage))
        {
            sb.AppendLine($"Error Message: {evt.ErrorMessage}");
        }
        return sb.ToString().TrimEnd();
    }
}
