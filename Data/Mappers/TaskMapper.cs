#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace NotionTaskSync.Data.Mappers;

using NotionTaskSync.Domain.Models;
using System;

/// <summary>
/// Maps between <c>Task</c> entities, Notion pages, and <see cref="TaskDto"/> transfer objects.
/// Handles serialization, deserialization, and data transformation.
/// </summary>
/// <remarks>
/// Notion page properties are read by well-known key (see the private constants in this class).
/// Missing keys leave the corresponding task field unchanged; values that cannot be parsed
/// into their target type are ignored rather than raising an error.
/// </remarks>
public static class TaskMapper
{
    // Notion property names
    private const string TitleKey = "Title";
    private const string DescriptionKey = "Description";
    private const string StatusKey = "Status";
    private const string DueDateKey = "DueDate";
    private const string PriorityKey = "Priority";
    private const string AssignedToKey = "AssignedTo";
    private const string TagsKey = "Tags";
    private const string CreatedAtKey = "CreatedAt";
    private const string UpdatedAtKey = "UpdatedAt";

    // Error messages
    private const string InvalidNotionPageMessage = "Invalid Notion page provided";
    private const string InvalidTaskMessage = "Invalid task provided";

    // Date/time serialization format (round-trip)
    private const string IsoDateTimeFormat = "O";

    /// <summary>
    /// Creates a new task from a Notion page.
    /// </summary>
    /// <remarks>
    /// The returned task gets a newly generated <c>Id</c>. Title, Notion page ID, timestamps, and
    /// archived state are taken from the page. Description, Status, DueDate, Priority, and AssignedTo
    /// are set only when the matching property is present on the page.
    /// </remarks>
    /// <param name="page">The Notion page to map. Must pass <c>NotionPage.Validate()</c>.</param>
    /// <returns>A new <see cref="Task"/> populated from the page.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="page"/> is <c>null</c>.</exception>
    /// <exception cref="ArgumentException">The page fails validation.</exception>
    public static Task MapFromNotionPage(NotionPage page)
    {
        ArgumentNullException.ThrowIfNull(page);

        if (!page.Validate())
            throw new ArgumentException(InvalidNotionPageMessage);

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
        if (page.Properties is not null && page.Properties.ContainsKey(DescriptionKey))
        {
            task.Description = page.Properties[DescriptionKey]?.ToString();
        }

        // Extract status if available
        if (page.Properties is not null && page.Properties.ContainsKey(StatusKey))
        {
            var status = page.Properties[StatusKey]?.ToString();
            if (Enum.TryParse<TaskStatus>(status, out var parsedStatus))
                task.Status = parsedStatus;
        }

        // Extract due date if available
        if (page.Properties is not null && page.Properties.ContainsKey(DueDateKey))
        {
            if (DateTime.TryParse(page.Properties[DueDateKey]?.ToString(), out var dueDate))
                task.DueDate = dueDate;
        }

        // Extract priority if available
        if (page.Properties is not null && page.Properties.ContainsKey(PriorityKey))
        {
            if (int.TryParse(page.Properties[PriorityKey]?.ToString(), out var priority))
                task.Priority = priority;
        }

        // Extract assigned to if available
        if (page.Properties is not null && page.Properties.ContainsKey(AssignedToKey))
        {
            task.AssignedTo = page.Properties[AssignedToKey]?.ToString();
        }

        return task;
    }

    /// <summary>
    /// Updates a task from a Notion page, preserving local-only fields.
    /// </summary>
    /// <remarks>
    /// Title, UpdatedAt, and IsDeleted are always overwritten from the page. Description, Status,
    /// Priority, DueDate, and AssignedTo are overwritten only when the matching property is present.
    /// The task's <c>Id</c>, <c>CreatedAt</c>, and <c>NotionPageId</c> are not changed.
    /// </remarks>
    /// <param name="task">The task to update in place.</param>
    /// <param name="page">The Notion page supplying the new values. Must pass <c>NotionPage.Validate()</c>.</param>
    /// <exception cref="ArgumentNullException"><paramref name="task"/> or <paramref name="page"/> is <c>null</c>.</exception>
    /// <exception cref="ArgumentException">The page fails validation.</exception>
    public static void UpdateTaskFromPage(Task task, NotionPage page)
    {
        ArgumentNullException.ThrowIfNull(task);
        ArgumentNullException.ThrowIfNull(page);

        if (!page.Validate())
            throw new ArgumentException(InvalidNotionPageMessage);

        task.Title = page.Title;
        task.UpdatedAt = page.LastEditedTime;
        task.IsDeleted = page.Archived;

        if (page.Properties is not null)
        {
            if (page.Properties.ContainsKey(DescriptionKey))
                task.Description = page.Properties[DescriptionKey]?.ToString();

            if (page.Properties.ContainsKey(StatusKey))
            {
                var status = page.Properties[StatusKey]?.ToString();
                if (Enum.TryParse<TaskStatus>(status, out var parsedStatus))
                    task.Status = parsedStatus;
            }

            if (page.Properties.ContainsKey(PriorityKey))
            {
                if (int.TryParse(page.Properties[PriorityKey]?.ToString(), out var priority))
                    task.Priority = priority;
            }

            if (page.Properties.ContainsKey(DueDateKey))
            {
                if (DateTime.TryParse(page.Properties[DueDateKey]?.ToString(), out var dueDate))
                    task.DueDate = dueDate;
            }

            if (page.Properties.ContainsKey(AssignedToKey))
                task.AssignedTo = page.Properties[AssignedToKey]?.ToString();
        }
    }

    /// <summary>
    /// Converts a task to a Notion page representation.
    /// </summary>
    /// <remarks>
    /// The page ID is the task's <c>NotionPageId</c>, or a newly generated GUID string when that is unset.
    /// The page's properties dictionary is replaced with Title, Description, Status, Priority, DueDate,
    /// AssignedTo, Tags, CreatedAt, and UpdatedAt. Dates are written in round-trip ("O") format.
    /// </remarks>
    /// <param name="task">The task to convert. Must pass <c>Task.Validate()</c>.</param>
    /// <param name="databaseId">The Notion database the page belongs to.</param>
    /// <returns>A <see cref="NotionPage"/> representing the task.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="task"/> or <paramref name="databaseId"/> is <c>null</c>.</exception>
    /// <exception cref="ArgumentException">The task fails validation.</exception>
    public static NotionPage MapToNotionPage(Task task, string databaseId)
    {
        ArgumentNullException.ThrowIfNull(task);
        ArgumentNullException.ThrowIfNull(databaseId);

        if (!task.Validate())
            throw new ArgumentException(InvalidTaskMessage);

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
            [TitleKey] = task.Title,
            [DescriptionKey] = task.Description,
            [StatusKey] = task.Status.ToString(),
            [PriorityKey] = task.Priority,
            [DueDateKey] = task.DueDate?.ToString(IsoDateTimeFormat),
            [AssignedToKey] = task.AssignedTo,
            [TagsKey] = task.Tags,
            [CreatedAtKey] = task.CreatedAt.ToString(IsoDateTimeFormat),
            [UpdatedAtKey] = task.UpdatedAt.ToString(IsoDateTimeFormat)
        };

        return page;
    }

    /// <summary>
    /// Creates a DTO representation of a task for API responses.
    /// </summary>
    /// <remarks>
    /// Status is converted to its string name. All other fields are copied as-is.
    /// </remarks>
    /// <param name="task">The task to copy from.</param>
    /// <returns>A new <see cref="TaskDto"/> with the task's values.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="task"/> is <c>null</c>.</exception>
    public static TaskDto MapToDto(Task task)
    {
        ArgumentNullException.ThrowIfNull(task);

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
