#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace NotionTaskSync.Services;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using NotionTaskSync.Domain.Models;

/// <summary>
/// Provides validation helpers for <see cref="ConflictDiffService"/> instances.
/// Validates constructor arguments, method parameters, and service state to ensure
/// <see cref="ConflictDiffService"/> can operate correctly.
/// </summary>
public static class ConflictDiffServiceValidation
{
    /// <summary>
    /// Validates the specified <see cref="ConflictDiffService"/> instance.
    /// </summary>
    /// <param name="value">The service instance to validate.</param>
    /// <returns>A list of human-readable validation problems; empty if the instance is valid.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null.</exception>
    public static IReadOnlyList<string> Validate(this ConflictDiffService? value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var errors = new List<string>();

        // ConflictDiffService only has a constructor that takes ILogger<ConflictDiffService>
        // No other state to validate beyond the logger being non-null (already validated by constructor)
        // The logger is validated in the constructor itself

        return errors.AsReadOnly();
    }

    /// <summary>
    /// Determines whether the specified <see cref="ConflictDiffService"/> instance is valid.
    /// </summary>
    /// <param name="value">The service instance to check.</param>
    /// <returns><see langword="true"/> if the instance is valid; otherwise, <see langword="false"/>.</returns>
    public static bool IsValid(this ConflictDiffService? value)
    {
        return value is not null && value.Validate().Count == 0;
    }

    /// <summary>
    /// Ensures that the specified <see cref="ConflictDiffService"/> instance is valid.
    /// </summary>
    /// <param name="value">The service instance to validate.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown if the instance is invalid, containing a list of validation problems.</exception>
    public static void EnsureValid(this ConflictDiffService? value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var errors = value.Validate();
        if (errors.Count == 0)
            return;

        throw new ArgumentException(
            $"ConflictDiffService instance is invalid:{Environment.NewLine}{string.Join(Environment.NewLine, errors)}");
    }

    /// <summary>
    /// Validates a <see cref="ConflictResolution"/> instance for use with <see cref="ConflictDiffService.GenerateDiffAsync"/>.
    /// </summary>
    /// <param name="conflict">The conflict to validate.</param>
    /// <returns>A list of human-readable validation problems; empty if the conflict is valid.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="conflict"/> is null.</exception>
    public static IReadOnlyList<string> Validate(this ConflictResolution conflict)
    {
        ArgumentNullException.ThrowIfNull(conflict);

        var errors = new List<string>();

        if (conflict.Id == Guid.Empty)
            errors.Add("ConflictResolution.Id must be a non-empty GUID.");

        if (string.IsNullOrWhiteSpace(conflict.PropertyName))
            errors.Add("ConflictResolution.PropertyName must not be null or whitespace.");

        if (conflict.LocalValue is null)
            errors.Add("ConflictResolution.LocalValue must not be null.");

        if (conflict.NotionValue is null)
            errors.Add("ConflictResolution.NotionValue must not be null.");

        if (conflict.DetectedAt == default)
            errors.Add("ConflictResolution.DetectedAt must be a non-default DateTime.");

        return errors.AsReadOnly();
    }

    /// <summary>
    /// Validates parameters for <see cref="ConflictDiffService.GenerateDiffForPropertyAsync"/>.
    /// </summary>
    /// <param name="localValue">The local value to compare.</param>
    /// <param name="notionValue">The Notion value to compare.</param>
    /// <param name="propertyName">The name of the property being compared.</param>
    /// <param name="conflictId">The conflict identifier.</param>
    /// <returns>A list of human-readable validation problems; empty if all parameters are valid.</returns>
    public static IReadOnlyList<string> Validate(
        string? localValue,
        string? notionValue,
        string propertyName,
        Guid conflictId = default)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(propertyName))
            errors.Add("propertyName must not be null or whitespace.");

        if (conflictId == Guid.Empty)
            errors.Add("conflictId should be a non-empty GUID for traceability.");

        return errors.AsReadOnly();
    }

    /// <summary>
    /// Validates a <see cref="ConflictDiffResult"/> instance for use with <see cref="ConflictDiffService.RenderAsTextAsync"/>.
    /// </summary>
    /// <param name="diff">The diff result to validate.</param>
    /// <returns>A list of human-readable validation problems; empty if the diff is valid.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="diff"/> is null.</exception>
    public static IReadOnlyList<string> Validate(this ConflictDiffResult diff)
    {
        ArgumentNullException.ThrowIfNull(diff);

        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(diff.PropertyName))
            errors.Add("ConflictDiffResult.PropertyName must not be null or whitespace.");

        if (diff.GeneratedAt == default)
            errors.Add("ConflictDiffResult.GeneratedAt must be a non-default DateTime.");

        if (diff.Lines is null)
            errors.Add("ConflictDiffResult.Lines must not be null.");

        return errors.AsReadOnly();
    }

    /// <summary>
    /// Validates parameters for <see cref="ConflictDiffService.GenerateBatchDiffsAsync"/>.
    /// </summary>
    /// <param name="conflicts">The collection of conflicts to process.</param>
    /// <returns>A list of human-readable validation problems; empty if the collection is valid.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="conflicts"/> is null.</exception>
    public static IReadOnlyList<string> Validate(this IReadOnlyList<ConflictResolution> conflicts)
    {
        ArgumentNullException.ThrowIfNull(conflicts);

        var errors = new List<string>();

        if (conflicts.Count == 0)
            errors.Add("conflicts collection must not be empty.");

        for (int i = 0; i < conflicts.Count; i++)
        {
            var conflict = conflicts[i];
            if (conflict is null)
            {
                errors.Add($"conflicts[{i}] must not be null.");
                continue;
            }

            var conflictErrors = ConflictDiffServiceValidation.Validate(conflict);
            if (conflictErrors.Count > 0)
            {
                errors.AddRange(conflictErrors.Select(e => $"conflicts[{i}]: {e}"));
            }
        }

        return errors.AsReadOnly();
    }
}