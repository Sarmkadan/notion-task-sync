#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace NotionTaskSync.Domain.Models;

using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Provides useful extension methods for the Task entity to enhance productivity
/// and simplify common operations when working with task collections.
/// </summary>
public static class TaskExtensions
{
    /// <summary>
    /// Determines whether the task is overdue based on its DueDate and current status.
    /// </summary>
    /// <param name="task">The task to check</param>
    /// <returns>True if the task is overdue; otherwise, false</returns>
    public static bool IsOverdue(this Task task)
    {
        if (task.DueDate == null || task.Status == TaskStatus.Done || task.Status == TaskStatus.Archived)
            return false;

        return task.DueDate.Value < DateTime.UtcNow.Date;
    }

    /// <summary>
    /// Determines whether the task is due today.
    /// </summary>
    /// <param name="task">The task to check</param>
    /// <returns>True if the task is due today; otherwise, false</returns>
    public static bool IsDueToday(this Task task)
    {
        if (task.DueDate == null)
            return false;

        var today = DateTime.UtcNow.Date;
        return task.DueDate.Value.Date == today;
    }

    /// <summary>
    /// Determines whether the task is high priority (priority >= 80).
    /// </summary>
    /// <param name="task">The task to check</param>
    /// <returns>True if the task is high priority; otherwise, false</returns>
    public static bool IsHighPriority(this Task task)
    {
        return task.Priority >= 80;
    }

    /// <summary>
    /// Determines whether the task is blocked or has a blocked status.
    /// </summary>
    /// <param name="task">The task to check</param>
    /// <returns>True if the task is blocked; otherwise, false</returns>
    public static bool IsBlocked(this Task task)
    {
        return task.Status == TaskStatus.Blocked;
    }

    /// <summary>
    /// Gets the age of the task in days since creation.
    /// </summary>
    /// <param name="task">The task to calculate age for</param>
    /// <returns>Number of days since task was created</returns>
    public static int GetAgeInDays(this Task task)
    {
        return (int)(DateTime.UtcNow.Date - task.CreatedAt.Date).TotalDays;
    }

    /// <summary>
    /// Determines whether the task is recent (created within last 7 days).
    /// </summary>
    /// <param name="task">The task to check</param>
    /// <returns>True if the task is recent; otherwise, false</returns>
    public static bool IsRecent(this Task task)
    {
        return (DateTime.UtcNow.Date - task.CreatedAt.Date).TotalDays <= 7;
    }

    /// <summary>
    /// Gets the task's priority level as a descriptive string.
    /// </summary>
    /// <param name="task">The task to get priority for</param>
    /// <returns>Priority level description (Critical, High, Medium, Low)</returns>
    public static string GetPriorityLevel(this Task task)
    {
        return task.Priority switch
        {
            >= 90 => "Critical",
            >= 80 => "High",
            >= 60 => "Medium",
            _ => "Low"
        };
    }

    /// <summary>
    /// Determines whether the task has any tags.
    /// </summary>
    /// <param name="task">The task to check</param>
    /// <returns>True if the task has tags; otherwise, false</returns>
    public static bool HasTags(this Task task)
    {
        return !string.IsNullOrWhiteSpace(task.Tags);
    }

    /// <summary>
    /// Gets the tags as a collection of strings split by comma.
    /// </summary>
    /// <param name="task">The task to get tags from</param>
    /// <returns>Collection of tag strings</returns>
    public static IEnumerable<string> GetTagList(this Task task)
    {
        if (string.IsNullOrWhiteSpace(task.Tags))
            return Array.Empty<string>();

        return task.Tags.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(t => t.Trim())
            .Where(t => !string.IsNullOrWhiteSpace(t));
    }

    /// <summary>
    /// Determines whether the task matches the specified tag.
    /// </summary>
    /// <param name="task">The task to check</param>
    /// <param name="tag">The tag to match</param>
    /// <returns>True if the task has the specified tag; otherwise, false</returns>
    public static bool HasTag(this Task task, string tag)
    {
        if (string.IsNullOrWhiteSpace(task.Tags) || string.IsNullOrWhiteSpace(tag))
            return false;

        var tags = task.GetTagList();
        return tags.Contains(tag.Trim(), StringComparer.OrdinalIgnoreCase);
    }
}
