#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace NotionTaskSync.Tests;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using NotionTaskSync.Domain.Models;

/// <summary>
/// Validation helpers for ConflictResolution tests to ensure test data integrity.
/// </summary>
public static class ConflictResolutionTestsValidation
{
    /// <summary>
    /// Validates a ConflictResolution instance and returns a list of human-readable problems.
    /// </summary>
    /// <param name="value">The ConflictResolution instance to validate.</param>
    /// <returns>A list of validation problems; empty if valid.</returns>
    /// <exception cref="ArgumentNullException">Thrown if value is null.</exception>
    public static IReadOnlyList<string> ValidateConflictResolution(this ConflictResolution value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var problems = new List<string>();

        // Validate TaskId
        if (value.TaskId == Guid.Empty)
        {
            problems.Add("TaskId cannot be empty");
        }

        // Validate ConflictType
        if (value.ConflictType == ConflictType.Unknown)
        {
            problems.Add("ConflictType cannot be Unknown");
        }

        // Validate Status-specific requirements
        if (value.Status == ResolutionStatus.Resolved)
        {
            if (string.IsNullOrEmpty(value.ResolvedValue))
            {
                problems.Add("ResolvedValue must be set when Status is Resolved");
            }

            if (value.ResolutionMethod == ResolutionMethod.LastWrite &&
                !value.ResolvedAt.HasValue)
            {
                problems.Add("ResolvedAt must be set when using LastWrite resolution method");
            }
        }

        // Validate DetectedAt (should not be default/MinValue)
        if (value.DetectedAt == default)
        {
            problems.Add("DetectedAt must be set to a valid date");
        }

        // Validate ResolvedAt (if set, should not be default)
        if (value.ResolvedAt.HasValue && value.ResolvedAt.Value == default)
        {
            problems.Add("ResolvedAt must be a valid date if set");
        }

        // Validate LocalModifiedAt/NotionModifiedAt for LastWrite strategy
        if (value.ResolutionMethod == ResolutionMethod.LastWrite)
        {
            if (!value.LocalModifiedAt.HasValue && !value.NotionModifiedAt.HasValue)
            {
                problems.Add("At least one of LocalModifiedAt or NotionModifiedAt must be set for LastWrite resolution");
            }
        }

        // Validate string properties for appropriate lengths
        if (!string.IsNullOrEmpty(value.PropertyName) && value.PropertyName.Length > 200)
        {
            problems.Add("PropertyName exceeds maximum length of 200 characters");
        }

        if (!string.IsNullOrEmpty(value.LocalValue) && value.LocalValue.Length > 1000)
        {
            problems.Add("LocalValue exceeds maximum length of 1000 characters");
        }

        if (!string.IsNullOrEmpty(value.NotionValue) && value.NotionValue.Length > 1000)
        {
            problems.Add("NotionValue exceeds maximum length of 1000 characters");
        }

        if (!string.IsNullOrEmpty(value.ResolvedValue) && value.ResolvedValue.Length > 1000)
        {
            problems.Add("ResolvedValue exceeds maximum length of 1000 characters");
        }

        if (!string.IsNullOrEmpty(value.ResolutionNotes) && value.ResolutionNotes.Length > 500)
        {
            problems.Add("ResolutionNotes exceeds maximum length of 500 characters");
        }

        if (!string.IsNullOrEmpty(value.ResolvedBy) && value.ResolvedBy.Length > 256)
        {
            problems.Add("ResolvedBy exceeds maximum length of 256 characters");
        }

        return problems.AsReadOnly();
    }

    /// <summary>
    /// Determines whether the specified ConflictResolution instance is valid.
    /// </summary>
    /// <param name="value">The ConflictResolution instance to check.</param>
    /// <returns>true if valid; otherwise, false.</returns>
    public static bool IsValid(this ConflictResolution value)
    {
        return value.ValidateConflictResolution().Count == 0;
    }

    /// <summary>
    /// Ensures that the specified ConflictResolution instance is valid.
    /// </summary>
    /// <param name="value">The ConflictResolution instance to validate.</param>
    /// <exception cref="ArgumentNullException">Thrown if value is null.</exception>
    /// <exception cref="ArgumentException">Thrown if value is not valid, containing a list of problems.</exception>
    public static void EnsureValid(this ConflictResolution value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var problems = value.ValidateConflictResolution();
        if (problems.Count > 0)
        {
            throw new ArgumentException(
                $"ConflictResolution is not valid. Problems: {string.Join(", ", problems)}");
        }
    }
}