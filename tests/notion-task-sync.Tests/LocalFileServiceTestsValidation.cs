#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace NotionTaskSync.Tests;

using NotionTaskSync.Domain.Models;
using System;
using System.Collections.Generic;
using System.Globalization;

/// <summary>
/// Provides validation helpers for <see cref="Task"/> entities used in LocalFileService tests.
/// </summary>
public static class LocalFileServiceTestsValidation
{
    /// <summary>
    /// Validates a <see cref="Task"/> entity and returns a list of human-readable validation problems.
    /// </summary>
    /// <param name="value">The task to validate.</param>
    /// <returns>An immutable list of validation problems; empty if the task is valid.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null.</exception>
    public static IReadOnlyList<string> ValidateProblems(this Task value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var problems = new List<string>();

        // Validate Id
        if (value.Id == Guid.Empty)
        {
            problems.Add("Task.Id cannot be empty (Guid.Empty).");
        }

        // Validate Title
        if (string.IsNullOrWhiteSpace(value.Title))
        {
            problems.Add("Task.Title cannot be null, empty, or whitespace.");
        }
        else if (value.Title.Length > 500)
        {
            problems.Add("Task.Title length exceeds maximum of 500 characters.");
        }

        // Validate Description length
        if (value.Description?.Length > 5000)
        {
            problems.Add("Task.Description length exceeds maximum of 5000 characters.");
        }

        // Validate Status is a defined enum value
        if (!Enum.IsDefined(typeof(TaskStatus), value.Status))
        {
            problems.Add("Task.Status is not a valid TaskStatus enum value.");
        }

        // Validate Priority range
        if (value.Priority < 0 || value.Priority > 100)
        {
            problems.Add("Task.Priority must be between 0 and 100 inclusive.");
        }

        // Validate CreatedAt
        if (value.CreatedAt == default)
        {
            problems.Add("Task.CreatedAt cannot be the default DateTime value.");
        }

        // Validate UpdatedAt
        if (value.UpdatedAt == default)
        {
            problems.Add("Task.UpdatedAt cannot be the default DateTime value.");
        }

        // Validate DueDate (if set)
        if (value.DueDate.HasValue)
        {
            if (value.DueDate.Value == default)
            {
                problems.Add("Task.DueDate cannot be the default DateTime value when set.");
            }
            else if (value.DueDate.Value < value.CreatedAt)
            {
                problems.Add("Task.DueDate cannot be earlier than Task.CreatedAt.");
            }
        }

        // Validate CompletedAt (if set)
        if (value.CompletedAt.HasValue)
        {
            if (value.CompletedAt.Value == default)
            {
                problems.Add("Task.CompletedAt cannot be the default DateTime value when set.");
            }
            else if (value.CompletedAt.Value < value.CreatedAt)
            {
                problems.Add("Task.CompletedAt cannot be earlier than Task.CreatedAt.");
            }
        }

        // Validate DeletedAt (if set)
        if (value.DeletedAt.HasValue)
        {
            if (value.DeletedAt.Value == default)
            {
                problems.Add("Task.DeletedAt cannot be the default DateTime value when set.");
            }
        }

        // Validate LocalFilePath (if set)
        if (value.LocalFilePath is not null && string.IsNullOrWhiteSpace(value.LocalFilePath))
        {
            problems.Add("Task.LocalFilePath cannot be empty or whitespace when set.");
        }

        // Validate AssignedTo length
        if (value.AssignedTo?.Length > 500)
        {
            problems.Add("Task.AssignedTo length exceeds maximum of 500 characters.");
        }

        // Validate Tags length
        if (value.Tags?.Length > 500)
        {
            problems.Add("Task.Tags length exceeds maximum of 500 characters.");
        }

        // Validate NotionPageId length
        if (value.NotionPageId?.Length > 500)
        {
            problems.Add("Task.NotionPageId length exceeds maximum of 500 characters.");
        }

        return problems.AsReadOnly();
    }

    /// <summary>
    /// Determines whether the specified <see cref="Task"/> is valid.
    /// </summary>
    /// <param name="value">The task to check.</param>
    /// <returns><see langword="true"/> if the task is valid; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null.</exception>
    public static bool IsValid(this Task value)
    {
        return value.ValidateProblems().Count == 0;
    }

    /// <summary>
    /// Ensures that the specified <see cref="Task"/> is valid, throwing an <see cref="ArgumentException"/>
    /// with a detailed message listing all validation problems if it is not.
    /// </summary>
    /// <param name="value">The task to validate.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when the task fails validation, containing a list of all problems.</exception>
    public static void EnsureValid(this Task value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var problems = value.ValidateProblems();

        if (problems.Count == 0)
        {
            return;
        }

        throw new ArgumentException(
            $"Task validation failed with {problems.Count} problem(s):{Environment.NewLine}" +
            $"- {string.Join($"{Environment.NewLine}- ", problems)}",
            nameof(value),
            new InvalidOperationException(string.Join("; ", problems)));
    }
}
