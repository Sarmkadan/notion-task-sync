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
public class Task
{
    public Guid Id { get; set; }

    [Required]
    [StringLength(500)]
    public required string Title { get; set; }

    [StringLength(5000)]
    public string? Description { get; set; }

    public string? NotionPageId { get; set; }

    public string? LocalFilePath { get; set; }

    public TaskStatus Status { get; set; } = TaskStatus.Todo;

    [Range(0, 100)]
    public int Priority { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? DueDate { get; set; }

    public DateTime? CompletedAt { get; set; }

    public string? AssignedTo { get; set; }

    public string? Tags { get; set; }

    public bool IsDeleted { get; set; }

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
