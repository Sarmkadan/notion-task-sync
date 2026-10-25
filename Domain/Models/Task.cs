#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace NotionTaskSync.Domain.Models;

using System;
using System.ComponentModel.DataAnnotations;

/// <summary>
/// Represents a task entity that can be synced between Notion and local files.
/// Tracks both local and remote identifiers, status, and metadata.
/// </summary>
public sealed class Task
{
    /// <summary>
    /// Unique identifier of the task within the local store.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Required human-readable title of the task. Must not be empty or whitespace and is limited to 500 characters.
    /// </summary>
    [Required]
    [StringLength(500)]
    public required string Title { get; set; }

    /// <summary>
    /// Optional free-text description of the task, limited to 5000 characters.
    /// </summary>
    [StringLength(5000)]
    public string? Description { get; set; }

    /// <summary>
    /// Identifier of the linked Notion page, or <c>null</c> if the task has not been mapped to Notion.
    /// </summary>
    public string? NotionPageId { get; set; }

    /// <summary>
    /// Path of the local file that represents this task, or <c>null</c> if no local file is associated.
    /// </summary>
    public string? LocalFilePath { get; set; }

    /// <summary>
    /// Current workflow status of the task. Defaults to <see cref="TaskStatus.Todo"/>.
    /// </summary>
    public TaskStatus Status { get; set; } = TaskStatus.Todo;

    /// <summary>
    /// Priority of the task, an integer in the inclusive range 0 to 100.
    /// </summary>
    [Range(0, 100)]
    public int Priority { get; set; }

    /// <summary>
    /// UTC timestamp of when the task was created. Set to the current UTC time on construction.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// UTC timestamp of the last modification. Refreshed by <see cref="UpdateTimestamp"/> and the state-changing methods.
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Optional due date of the task. When set, it must not be earlier than <see cref="CreatedAt"/> to pass <see cref="Validate"/>.
    /// </summary>
    public DateTime? DueDate { get; set; }

    /// <summary>
    /// UTC timestamp of when the task was completed, or <c>null</c> if it has not been completed.
    /// Set by <see cref="Complete"/>; must not be earlier than <see cref="CreatedAt"/> to pass <see cref="Validate"/>.
    /// </summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// Optional identifier of the person or entity the task is assigned to.
    /// </summary>
    public string? AssignedTo { get; set; }

    /// <summary>
    /// Optional tags associated with the task, stored as a single string. The tag delimiter is not defined by this model.
    /// </summary>
    public string? Tags { get; set; }

    /// <summary>
    /// Soft-delete flag. <c>true</c> once the task has been marked as deleted via <see cref="MarkAsDeleted"/>; the record is kept.
    /// </summary>
    public bool IsDeleted { get; set; }

    /// <summary>
    /// UTC timestamp of when the task was soft-deleted, or <c>null</c> if it has not been deleted.
    /// </summary>
    public DateTime? DeletedAt { get; set; }

    /// <summary>
    /// Validates the task entity ensuring required fields are populated correctly.
    /// </summary>
    public bool Validate()
    {
        if (string.IsNullOrWhiteSpace(Title) || Title.Length > 500)
            return false;

        if (Priority < 0 || Priority > 100)
            return false;

        if (DueDate.HasValue && DueDate.Value < CreatedAt)
            return false;

        if (CompletedAt.HasValue && CompletedAt.Value < CreatedAt)
            return false;

        return true;
    }

    /// <summary>
    /// Updates the task's UpdatedAt timestamp and validates the changes.
    /// </summary>
    public void UpdateTimestamp()
    {
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Marks the task as completed with current timestamp.
    /// </summary>
    public void Complete()
    {
        Status = TaskStatus.Done;
        CompletedAt = DateTime.UtcNow;
        UpdateTimestamp();
    }

    /// <summary>
    /// Marks the task as deleted without permanent removal.
    /// </summary>
    public void MarkAsDeleted()
    {
        IsDeleted = true;
        DeletedAt = DateTime.UtcNow;
        UpdateTimestamp();
    }

    /// <summary>
    /// Creates a copy of the task with a new identity for duplication purposes.
    /// </summary>
    public Task Clone()
    {
        return new Task
        {
            Id = Guid.NewGuid(),
            Title = Title,
            Description = Description,
            Status = Status,
            Priority = Priority,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            DueDate = DueDate,
            AssignedTo = AssignedTo,
            Tags = Tags,
            IsDeleted = false
        };
    }
}

public enum TaskStatus
{
    Todo = 0,
    InProgress = 1,
    Done = 2,
    Blocked = 3,
    Archived = 4
}
