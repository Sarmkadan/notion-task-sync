#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace NotionTaskSync.Tests;

using System;
using System.Collections.Generic;
using System.Globalization;

/// <summary>
/// Provides validation helpers for <see cref="NotionApiServiceTests"/> instances.
/// </summary>
public static class NotionApiServiceTestsValidation
{
    /// <summary>
    /// Validates the specified <see cref="NotionApiServiceTests"/> instance.
    /// </summary>
    /// <param name="value">The instance to validate.</param>
    /// <returns>A list of validation problems; empty if the instance is valid.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null.</exception>
    public static IReadOnlyList<string> Validate(this NotionApiServiceTests? value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var problems = new List<string>();

        // NotionApiServiceTests is a test fixture class with no public instance state
        // that would require validation. All validation is handled at the method level
        // in the actual NotionApiService methods being tested.

        return problems.AsReadOnly();
    }

    /// <summary>
    /// Determines whether the specified <see cref="NotionApiServiceTests"/> instance is valid.
    /// </summary>
    /// <param name="value">The instance to check.</param>
    /// <returns><see langword="true"/> if the instance is valid; otherwise, <see langword="false"/>.</returns>
    public static bool IsValid(this NotionApiServiceTests? value)
    {
        return value?.Validate().Count == 0;
    }

    /// <summary>
    /// Ensures that the specified <see cref="NotionApiServiceTests"/> instance is valid.
    /// </summary>
    /// <param name="value">The instance to validate.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown if the instance has validation problems.</exception>
    public static void EnsureValid(this NotionApiServiceTests? value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var problems = value.Validate();
        if (problems.Count > 0)
        {
            throw new ArgumentException(
                $"NotionApiServiceTests instance is invalid. Problems: {string.Join(", ", problems)}",
                nameof(value));
        }
    }
}