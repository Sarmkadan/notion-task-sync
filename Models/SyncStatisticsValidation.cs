#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace NotionTaskSync.Models;

using System;
using System.Collections.Generic;
using System.Globalization;

/// <summary>
/// Provides validation helpers for <see cref="SyncStatistics"/> instances.
/// Ensures statistics are in valid states before use in reporting and analysis.
/// </summary>
public static class SyncStatisticsValidation
{
    /// <summary>
    /// Validates a <see cref="SyncStatistics"/> instance and returns a list of human-readable problems.
    /// </summary>
    /// <param name="value">The statistics to validate.</param>
    /// <returns>An empty list if valid; otherwise, a list of validation error messages.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null.</exception>
    public static IReadOnlyList<string> Validate(this SyncStatistics value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var errors = new List<string>();

        // Validate counter fields
        if (value.TotalSyncs < 0)
        {
            errors.Add($"TotalSyncs must be non-negative, but was {value.TotalSyncs}.");
        }

        if (value.SuccessfulSyncs < 0)
        {
            errors.Add($"SuccessfulSyncs must be non-negative, but was {value.SuccessfulSyncs}.");
        }

        if (value.FailedSyncs < 0)
        {
            errors.Add($"FailedSyncs must be non-negative, but was {value.FailedSyncs}.");
        }

        if (value.TotalTasksSynced < 0)
        {
            errors.Add($"TotalTasksSynced must be non-negative, but was {value.TotalTasksSynced}.");
        }

        if (value.TotalConflicts < 0)
        {
            errors.Add($"TotalConflicts must be non-negative, but was {value.TotalConflicts}.");
        }

        if (value.ResolvedConflicts < 0)
        {
            errors.Add($"ResolvedConflicts must be non-negative, but was {value.ResolvedConflicts}.");
        }

        // Validate derived relationships
        if (value.SuccessfulSyncs + value.FailedSyncs != value.TotalSyncs)
        {
            errors.Add(
                $"SuccessfulSyncs + FailedSyncs ({value.SuccessfulSyncs} + {value.FailedSyncs}) " +
                $"must equal TotalSyncs ({value.TotalSyncs}).");
        }

        if (value.ResolvedConflicts > value.TotalConflicts)
        {
            errors.Add(
                $"ResolvedConflicts ({value.ResolvedConflicts}) cannot exceed TotalConflicts ({value.TotalConflicts}).");
        }

        // Validate LastResetAt (should not be default DateTime)
        if (value.LastResetAt == default)
        {
            errors.Add("LastResetAt must be set to a valid DateTime, but was default.");
        }

        // Validate Operations collection
        if (value.Operations is null)
        {
            errors.Add("Operations collection must not be null.");
        }
        else
        {
            if (value.Operations.Count > 100)
            {
                errors.Add("Operations collection must contain at most 100 items, but had " +
                         $"{value.Operations.Count} items.");
            }

            foreach (var (index, operation) in value.Operations.Index())
            {
                if (operation is null)
                {
                    errors.Add($"Operations[{index}] must not be null.");
                    continue;
                }

                if (operation.DurationMs < 0)
                {
                    errors.Add($"Operations[{index}].DurationMs must be non-negative, but was {operation.DurationMs}.");
                }

                if (operation.TasksProcessed < 0)
                {
                    errors.Add($"Operations[{index}].TasksProcessed must be non-negative, but was {operation.TasksProcessed}.");
                }

                if (operation.ChangesDetected < 0)
                {
                    errors.Add($"Operations[{index}].ChangesDetected must be non-negative, but was {operation.ChangesDetected}.");
                }

                if (operation.ConflictsDetected < 0)
                {
                    errors.Add($"Operations[{index}].ConflictsDetected must be non-negative, but was {operation.ConflictsDetected}.");
                }

                if (operation.ConflictsResolved < 0)
                {
                    errors.Add($"Operations[{index}].ConflictsResolved must be non-negative, but was {operation.ConflictsResolved}.");
                }

                if (operation.ConflictsResolved > operation.ConflictsDetected)
                {
                    errors.Add(
                        $"Operations[{index}].ConflictsResolved ({operation.ConflictsResolved}) " +
                        $"cannot exceed ConflictsDetected ({operation.ConflictsDetected}).");
                }

                if (operation.Timestamp == default)
                {
                    errors.Add($"Operations[{index}].Timestamp must be set to a valid DateTime, but was default.");
                }
            }
        }

        return errors.AsReadOnly();
    }

    /// <summary>
    /// Determines whether the specified <see cref="SyncStatistics"/> instance is valid.
    /// </summary>
    /// <param name="value">The statistics to check.</param>
    /// <returns>True if valid; otherwise, false.</returns>
    public static bool IsValid(this SyncStatistics value)
    {
        return value.Validate().Count == 0;
    }

    /// <summary>
    /// Ensures that the specified <see cref="SyncStatistics"/> instance is valid.
    /// </summary>
    /// <param name="value">The statistics to validate.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="value"/> is invalid, containing a list of validation errors.</exception>
    public static void EnsureValid(this SyncStatistics value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var errors = value.Validate();
        if (errors.Count == 0)
        {
            return;
        }

        throw new ArgumentException(
            $"SyncStatistics validation failed:{Environment.NewLine}" +
            string.Join(Environment.NewLine, errors),
            nameof(value));
    }
}