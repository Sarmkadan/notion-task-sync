#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace NotionTaskSync.Domain.Models;

using System;
using System.Collections.Generic;
using System.Globalization;

/// <summary>
/// Provides validation helpers for <see cref="Task"/> entities.
/// Validates all public members to ensure data integrity before persistence or sync operations.
/// </summary>
public static class TaskValidation
{
    /// <summary>
    /// Validates the specified task and returns a list of human-readable validation errors.
    /// </summary>
    /// <param name="value">The task to validate.</param>
    /// <returns>An empty list if valid; otherwise, a list of error messages.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null.</exception>
    public static IReadOnlyList<string> ValidateTask(this Task? value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var errors = new List<string>();

        // Validate Id
        if (value.Id == Guid.Empty)
        {
            errors.Add("Task.Id must be a non-empty GUID.");
        }

        // Validate Title (required, max 500 chars)
        if (string.IsNullOrWhiteSpace(value.Title))
        {
            errors.Add("Task.Title is required and cannot be null, empty, or whitespace.");
        }
        else if (value.Title.Length > 500)
        {
            errors.Add("Task.Title must be 500 characters or less.");
        }

        // Validate Description (optional, max 5000 chars)
        if (value.Description is not null && value.Description.Length > 5000)
        {
            errors.Add("Task.Description must be 5000 characters or less when provided.");
        }

        // Validate NotionPageId (optional, format validation)
        if (value.NotionPageId is not null)
        {
            if (string.IsNullOrWhiteSpace(value.NotionPageId))
            {
                errors.Add("Task.NotionPageId cannot be whitespace when provided.");
            }
            else if (value.NotionPageId.Length > 200)
            {
                errors.Add("Task.NotionPageId must be 200 characters or less when provided.");
            }
        }

        // Validate LocalFilePath (optional, format validation)
        if (value.LocalFilePath is not null)
        {
            if (string.IsNullOrWhiteSpace(value.LocalFilePath))
            {
                errors.Add("Task.LocalFilePath cannot be whitespace when provided.");
            }
            else if (value.LocalFilePath.Length > 2048)
            {
                errors.Add("Task.LocalFilePath must be 2048 characters or less when provided.");
            }
        }

        // Validate Status
        // Note: TaskStatus is an enum, so any value is technically valid
        // But we validate the business logic: Completed tasks should have CompletedAt set
        if (value.Status == TaskStatus.Done && !value.CompletedAt.HasValue)
        {
            errors.Add("Task.Status is 'Done' but Task.CompletedAt is not set.");
        }

        // Validate Priority (0-100 range)
        if (value.Priority < 0 || value.Priority > 100)
        {
            errors.Add("Task.Priority must be between 0 and 100 inclusive.");
        }

        // Validate CreatedAt (must be a valid date, not default)
        if (value.CreatedAt == default)
        {
            errors.Add("Task.CreatedAt must be set to a valid DateTime.");
        }
        else if (value.CreatedAt.Kind != DateTimeKind.Utc)
        {
            errors.Add("Task.CreatedAt must be in UTC timezone.");
        }

        // Validate UpdatedAt (must be a valid date, not default, should be >= CreatedAt)
        if (value.UpdatedAt == default)
        {
            errors.Add("Task.UpdatedAt must be set to a valid DateTime.");
        }
        else if (value.UpdatedAt.Kind != DateTimeKind.Utc)
        {
            errors.Add("Task.UpdatedAt must be in UTC timezone.");
        }
        else if (value.UpdatedAt < value.CreatedAt)
        {
            errors.Add("Task.UpdatedAt cannot be earlier than Task.CreatedAt.");
        }

        // Validate DueDate (must be >= CreatedAt when set)
        if (value.DueDate.HasValue)
        {
            if (value.DueDate.Value.Kind != DateTimeKind.Utc)
            {
                errors.Add("Task.DueDate must be in UTC timezone when provided.");
            }
            else if (value.DueDate.Value < value.CreatedAt)
            {
                errors.Add("Task.DueDate cannot be earlier than Task.CreatedAt.");
            }
        }

        // Validate CompletedAt (must be >= CreatedAt when set)
        if (value.CompletedAt.HasValue)
        {
            if (value.CompletedAt.Value.Kind != DateTimeKind.Utc)
            {
                errors.Add("Task.CompletedAt must be in UTC timezone when provided.");
            }
            else if (value.CompletedAt.Value < value.CreatedAt)
            {
                errors.Add("Task.CompletedAt cannot be earlier than Task.CreatedAt.");
            }
        }

        // Validate AssignedTo (optional, max 200 chars)
        if (value.AssignedTo is not null)
        {
            if (string.IsNullOrWhiteSpace(value.AssignedTo))
            {
                errors.Add("Task.AssignedTo cannot be whitespace when provided.");
            }
            else if (value.AssignedTo.Length > 200)
            {
                errors.Add("Task.AssignedTo must be 200 characters or less when provided.");
            }
        }

        // Validate Tags (optional, max 1000 chars)
        if (value.Tags is not null)
        {
            if (string.IsNullOrWhiteSpace(value.Tags))
            {
                errors.Add("Task.Tags cannot be whitespace when provided.");
            }
            else if (value.Tags.Length > 1000)
            {
                errors.Add("Task.Tags must be 1000 characters or less when provided.");
            }
        }

        // Validate IsDeleted / DeletedAt consistency
        if (value.IsDeleted)
        {
            if (!value.DeletedAt.HasValue)
            {
                errors.Add("Task.IsDeleted is true but Task.DeletedAt is not set.");
            }
            else if (value.DeletedAt.Value.Kind != DateTimeKind.Utc)
            {
                errors.Add("Task.DeletedAt must be in UTC timezone when provided.");
            }
            else if (value.DeletedAt.Value < value.CreatedAt)
            {
                errors.Add("Task.DeletedAt cannot be earlier than Task.CreatedAt.");
            }
        }
        else
        {
            if (value.DeletedAt.HasValue)
            {
                errors.Add("Task.IsDeleted is false but Task.DeletedAt is set. Set IsDeleted to true or clear DeletedAt.");
            }
        }

        // Validate Status/DueDate/CompletedAt consistency
        switch (value.Status)
        {
            case TaskStatus.Done:
                if (!value.CompletedAt.HasValue)
                {
                    errors.Add("Task.Status is 'Done' but Task.CompletedAt is not set.");
                }
                break;

            case TaskStatus.Blocked:
                // Blocked tasks can have DueDate but should typically have one
                if (value.DueDate.HasValue && value.DueDate.Value < DateTime.UtcNow)
                {
                    errors.Add("Task.Status is 'Blocked' but Task.DueDate is in the past.");
                }
                break;

            case TaskStatus.Archived:
                if (!value.IsDeleted)
                {
                    errors.Add("Task.Status is 'Archived' but Task.IsDeleted is false. Archived tasks should be marked as deleted.");
                }
                break;
        }

        return errors.AsReadOnly();
    }

    /// <summary>
    /// Determines whether the specified task is valid.
    /// </summary>
    /// <param name="value">The task to check.</param>
    /// <returns>True if valid; otherwise, false.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null.</exception>
    public static bool IsValid(this Task? value)
    {
        return value is not null && value.ValidateTask().Count == 0;
    }

    /// <summary>
    /// Ensures that the specified task is valid, throwing an <see cref="ArgumentException"/>
    /// with detailed validation messages if it is not.
    /// </summary>
    /// <param name="value">The task to validate.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when the task is invalid, containing detailed error messages.</exception>
    public static void EnsureValid(this Task? value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var errors = value.ValidateTask();
        if (errors.Count == 0)
        {
            return;
        }

        throw new ArgumentException(
            $"Task validation failed:{Environment.NewLine}{string.Join(Environment.NewLine, errors)}");
    }
}
