#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace NotionTaskSync.Data.Mappers;

using NotionTaskSync.Domain.Models;
using System;

/// <summary>
/// Maps between Task entities and their transfer objects.
/// Handles serialization, deserialization, and data transformation.
/// </summary>
public static class TaskMapper
{
    /// <summary>
    /// Creates a task from a Notion page.
    /// </summary>
    public static Task MapFromNotionPage(NotionPage page)
    {
        if (!page.Validate())
            throw new ArgumentException("Invalid Notion page provided");

        var task = new Task
        {
            Id = Guid.NewGuid(),
            Title = page.Title,
            NotionPageId = page.PageId,
            CreatedAt = page.CreatedTime,
            UpdatedAt = page.LastEditedTime,
            IsDeleted = page.Archived
        };

        // Extract description if available
        if (page.Properties is not null && page.Properties.ContainsKey("Description"))
        {
            task.Description = page.Properties["Description"]?.ToString();
        }

        // Extract status if available
        if (page.Properties is not null && page.Properties.ContainsKey("Status"))
        {
            var status = page.Properties["Status"]?.ToString();
            if (Enum.TryParse<TaskStatus>(status, out var parsedStatus))
                task.Status = parsedStatus;
        }

        // Extract due date if available
        if (page.Properties is not null && page.Properties.ContainsKey("DueDate"))
        {
            if (DateTime.TryParse(page.Properties["DueDate"]?.ToString(), out var dueDate))
                task.DueDate = dueDate;
        }

        // Extract priority if available
        if (page.Properties is not null && page.Properties.ContainsKey("Priority"))
        {
            if (int.TryParse(page.Properties["Priority"]?.ToString(), out var priority))
                task.Priority = priority;
        }

        // Extract assigned to if available
        if (page.Properties is not null && page.Properties.ContainsKey("AssignedTo"))
        {
            task.AssignedTo = page.Properties["AssignedTo"]?.ToString();
        }

        return task;
    }

    /// <summary>
    /// Updates a task from a Notion page, preserving local-only fields.
    /// </summary>
    public static void UpdateTaskFromPage(Task task, NotionPage page)
    {
        if (!page.Validate())
            throw new ArgumentException("Invalid Notion page provided");

        task.Title = page.Title;
        task.UpdatedAt = page.LastEditedTime;
        task.IsDeleted = page.Archived;

        if (page.Properties is not null)
        {
            if (page.Properties.ContainsKey("Description"))
                task.Description = page.Properties["Description"]?.ToString();

            if (page.Properties.ContainsKey("Status"))
            {
                var status = page.Properties["Status"]?.ToString();
                if (Enum.TryParse<TaskStatus>(status, out var parsedStatus))
                    task.Status = parsedStatus;
            }

            if (page.Properties.ContainsKey("Priority"))
            {
                if (int.TryParse(page.Properties["Priority"]?.ToString(), out var priority))
                    task.Priority = priority;
            }

            if (page.Properties.ContainsKey("DueDate"))
            {
                if (DateTime.TryParse(page.Properties["DueDate"]?.ToString(), out var dueDate))
                    task.DueDate = dueDate;
            }

            if (page.Properties.ContainsKey("AssignedTo"))
                task.AssignedTo = page.Properties["AssignedTo"]?.ToString();
        }
    }

    /// <summary>
    /// Converts a task to a Notion page representation.
    /// </summary>
    public static NotionPage MapToNotionPage(Task task, string databaseId)
    {
        if (!task.Validate())
            throw new ArgumentException("Invalid task provided");

        var page = new NotionPage(
            pageId: task.NotionPageId ?? Guid.NewGuid().ToString(),
            databaseId: databaseId,
            title: task.Title
        );

        page.CreatedTime = task.CreatedAt;
        page.LastEditedTime = task.UpdatedAt;
        page.Archived = task.IsDeleted;

        page.Properties = new()
        {
            ["Title"] = task.Title,
            ["Description"] = task.Description,
            ["Status"] = task.Status.ToString(),
            ["Priority"] = task.Priority,
            ["DueDate"] = task.DueDate?.ToString("O"),
            ["AssignedTo"] = task.AssignedTo,
            ["Tags"] = task.Tags,
            ["CreatedAt"] = task.CreatedAt.ToString("O"),
            ["UpdatedAt"] = task.UpdatedAt.ToString("O")
        };

        return page;
    }

    /// <summary>
    /// Creates a DTO representation of a task for API responses.
    /// </summary>
    public static TaskDto MapToDto(Task task)
    {
        return new TaskDto
        {
            Id = task.Id,
            Title = task.Title,
            Description = task.Description,
            Status = task.Status.ToString(),
            Priority = task.Priority,
            CreatedAt = task.CreatedAt,
            UpdatedAt = task.UpdatedAt,
            DueDate = task.DueDate,
            AssignedTo = task.AssignedTo,
            NotionPageId = task.NotionPageId,
            IsDeleted = task.IsDeleted
        };
    }
}

/// <summary>
/// DTO for transferring task data over APIs or to external systems.
/// </summary>
public class TaskDto
{
    public Guid Id { get; set; }
    public string? Title { get; set; }
    public string? Description { get; set; }
    public string? Status { get; set; }
    public int Priority { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? DueDate { get; set; }
    public string? AssignedTo { get; set; }
    public string? NotionPageId { get; set; }
    public bool IsDeleted { get; set; }
}
