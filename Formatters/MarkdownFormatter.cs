#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace NotionTaskSync.Formatters;

using System;
using System.Collections.Generic;
using System.Text;
using NotionTaskSync.Domain.Models;
using Microsoft.Extensions.Logging;

/// <summary>
/// Formats tasks as Markdown for documentation and export purposes.
/// Creates structured, human-readable Markdown files suitable for version control.
/// Enables easy integration with documentation systems and git-based workflows.
/// </summary>
public class MarkdownFormatter
{
    private readonly ILogger<MarkdownFormatter> _logger;

    public MarkdownFormatter(ILogger<MarkdownFormatter> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Formats a single task as a Markdown section.
    /// </summary>
    public string FormatTask(Task task)
    {
        try
        {
            var sb = new StringBuilder();

            // Header
            var statusEmoji = GetStatusEmoji(task.Status);
            sb.AppendLine($"## {statusEmoji} {EscapeMarkdown(task.Title)}");
            sb.AppendLine();

            // Metadata
            sb.AppendLine("| Property | Value |");
            sb.AppendLine("|----------|-------|");
            sb.AppendLine($"| Status | `{task.Status}` |");
            sb.AppendLine($"| Priority | {task.Priority} |");
            sb.AppendLine($"| Created | {task.CreatedAt:g} |");
            sb.AppendLine($"| Updated | {task.UpdatedAt:g} |");

            if (task.DueDate.HasValue)
                sb.AppendLine($"| Due Date | {task.DueDate.Value:g} |");

            if (!string.IsNullOrEmpty(task.AssignedTo))
                sb.AppendLine($"| Assigned To | {EscapeMarkdown(task.AssignedTo)} |");

            if (!string.IsNullOrEmpty(task.Tags))
                sb.AppendLine($"| Tags | {EscapeMarkdown(task.Tags)} |");

            sb.AppendLine();

            // Description
            if (!string.IsNullOrEmpty(task.Description))
            {
                sb.AppendLine("### Description");
                sb.AppendLine(EscapeMarkdown(task.Description));
                sb.AppendLine();
            }

            // IDs
            if (!string.IsNullOrEmpty(task.NotionPageId))
                sb.AppendLine($"> **Notion ID:** `{task.NotionPageId}`");

            sb.AppendLine($"> **Local ID:** `{task.Id}`");
            sb.AppendLine();

            return sb.ToString();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to format task {TaskId} as Markdown", task.Id);
            throw;
        }
    }

    /// <summary>
    /// Formats a collection of tasks as a Markdown document.
    /// </summary>
    public string FormatTasks(List<Task> tasks, string title = "Tasks")
    {
        try
        {
            var sb = new StringBuilder();

            // Document header
            sb.AppendLine($"# {EscapeMarkdown(title)}");
            sb.AppendLine();
            sb.AppendLine($"Generated: {DateTime.UtcNow:g}");
            sb.AppendLine($"Total Tasks: {tasks.Count}");
            sb.AppendLine();

            // Summary statistics
            var completed = tasks.FindAll(t => t.Status == TaskStatus.Done).Count;
            var blocked = tasks.FindAll(t => t.Status == TaskStatus.Blocked).Count;
            var inProgress = tasks.FindAll(t => t.Status == TaskStatus.InProgress).Count;

            sb.AppendLine("## Summary");
            sb.AppendLine($"- **Total:** {tasks.Count}");
            sb.AppendLine($"- **Completed:** {completed}");
            sb.AppendLine($"- **In Progress:** {inProgress}");
            sb.AppendLine($"- **Blocked:** {blocked}");
            sb.AppendLine();

            // Table of contents
            sb.AppendLine("## Contents");
            sb.AppendLine();
            foreach (var task in tasks)
            {
                var statusEmoji = GetStatusEmoji(task.Status);
                sb.AppendLine($"- {statusEmoji} [{EscapeMarkdown(task.Title)}](#{task.Id})");
            }
            sb.AppendLine();

            // Detailed tasks
            sb.AppendLine("---");
            sb.AppendLine();

            foreach (var task in tasks)
            {
                sb.Append(FormatTask(task));
                sb.AppendLine("---");
                sb.AppendLine();
            }

            return sb.ToString();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to format {TaskCount} tasks as Markdown", tasks.Count);
            throw;
        }
    }

    /// <summary>
    /// Escapes special Markdown characters in text.
    /// </summary>
    private string EscapeMarkdown(string? text)
    {
        if (string.IsNullOrEmpty(text))
            return "";

        // Escape special markdown characters
        return text
            .Replace("\\", "\\\\")
            .Replace("[", "\\[")
            .Replace("]", "\\]")
            .Replace("*", "\\*")
            .Replace("_", "\\_")
            .Replace("`", "\\`")
            .Replace("#", "\\#")
            .Replace("+", "\\+")
            .Replace("-", "\\-");
    }

    /// <summary>
    /// Gets an emoji representation of a task status.
    /// Used in tables and lists for visual distinction.
    /// </summary>
    private string GetStatusEmoji(TaskStatus status)
    {
        return status switch
        {
            TaskStatus.Done => "✅",
            TaskStatus.InProgress => "🔄",
            TaskStatus.Blocked => "⛔",
            TaskStatus.Archived => "📦",
            _ => "⭕"
        };
    }

    /// <summary>
    /// Creates a Markdown table from a list of tasks.
    /// Useful for quick overview and summaries.
    /// </summary>
    public string FormatTasksAsTable(List<Task> tasks)
    {
        var sb = new StringBuilder();

        sb.AppendLine("| Status | Title | Priority | Due Date | Assigned |");
        sb.AppendLine("|--------|-------|----------|----------|----------|");

        foreach (var task in tasks)
        {
            var status = GetStatusEmoji(task.Status);
            var title = EscapeMarkdown(task.Title).Truncate(30);
            var dueDate = task.DueDate?.ToString("d") ?? "-";
            var assignedTo = string.IsNullOrEmpty(task.AssignedTo) ? "-" : task.AssignedTo;

            sb.AppendLine($"| {status} | {title} | {task.Priority} | {dueDate} | {assignedTo} |");
        }

        return sb.ToString();
    }
}

/// <summary>
/// Extension method for truncating in Markdown formatter.
/// </summary>
internal static class TruncateExtension
{
    internal static string Truncate(this string str, int maxLength)
    {
        if (str.Length <= maxLength)
            return str;
        return str.Substring(0, maxLength - 3) + "...";
    }
}
