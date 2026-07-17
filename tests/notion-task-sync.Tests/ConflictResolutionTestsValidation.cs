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

        // Validate Status-specific requirements using pattern matching
        switch (value.Status)
        {
            case ResolutionStatus.Resolved when string.IsNullOrEmpty(value.ResolvedValue):
                problems.Add("ResolvedValue must be set when Status is Resolved");
                break;

            case ResolutionStatus.Resolved when value.ResolutionMethod == ResolutionMethod.LastWrite &&
                !value.ResolvedAt.HasValue:
                problems.Add("ResolvedAt must be set when using LastWrite resolution method");
                break;
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
        if (value.ResolutionMethod == ResolutionMethod.LastWrite &&
            !value.LocalModifiedAt.HasValue && !value.NotionModifiedAt.HasValue)
        {
            problems.Add("At least one of LocalModifiedAt or NotionModifiedAt must be set for LastWrite resolution");
        }

        // Validate string properties for appropriate lengths using expression-bodied members
        problems.AddRange(ValidateStringLength(value.PropertyName, 200, nameof(value.PropertyName)));
        problems.AddRange(ValidateStringLength(value.LocalValue, 1000, nameof(value.LocalValue)));
        problems.AddRange(ValidateStringLength(value.NotionValue, 1000, nameof(value.NotionValue)));
        problems.AddRange(ValidateStringLength(value.ResolvedValue, 1000, nameof(value.ResolvedValue)));
        problems.AddRange(ValidateStringLength(value.ResolutionNotes, 500, nameof(value.ResolutionNotes)));
        problems.AddRange(ValidateStringLength(value.ResolvedBy, 256, nameof(value.ResolvedBy)));

        return problems.AsReadOnly();
    }

    /// <summary>
    /// Validates that a string property does not exceed the specified maximum length.
    /// </summary>
    /// <param name="value">The string value to validate.</param>
    /// <param name="maxLength">The maximum allowed length.</param>
    /// <param name="propertyName">The name of the property for error messages.</param>
    /// <returns>A list of validation problems; empty if valid.</returns>
    private static IEnumerable<string> ValidateStringLength(string? value, int maxLength, string propertyName)
    {
        if (!string.IsNullOrEmpty(value) && value.Length > maxLength)
        {
            yield return $"{{propertyName}} exceeds maximum length of {{maxLength}} characters";
        }
    }

    /// <summary>
    /// Determines whether the specified ConflictResolution instance is valid.
    /// </summary>
    /// <param name="value">The ConflictResolution instance to check.</param>
    /// <returns>true if valid; otherwise, false.</returns>
    /// <exception cref="ArgumentNullException">Thrown if value is null.</exception>
    public static bool IsValid(this ConflictResolution value)
    {
        ArgumentNullException.ThrowIfNull(value);
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
                $"ConflictResolution is not valid. Problems: {string.Join(", ", problems)}",
                nameof(value));
        }
    }
}