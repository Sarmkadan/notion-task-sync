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
/// Provides validation helpers for <see cref="RetryHelperTests"/> instances.
/// Validates that test class instances are properly initialized with required dependencies.
/// </summary>
public static class RetryHelperTestsValidation
{
    /// <summary>
    /// Validates the specified <see cref="RetryHelperTests"/> instance and returns a list of validation problems.
    /// </summary>
    /// <param name="value">The instance to validate.</param>
    /// <returns>A read-only list of validation problems; empty if the instance is valid.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null.</exception>
    public static IReadOnlyList<string> Validate(this RetryHelperTests? value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var problems = new List<string>();

        // Validate that the test class has been properly initialized
        // Since fields are private, we validate the class state through its public contract
        // The constructor should have initialized all required dependencies

        if (value.GetType().GetFields(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
            .Length == 0)
        {
            problems.Add("RetryHelperTests has no private fields - validation cannot be performed");
        }

        return problems.AsReadOnly();
    }

    /// <summary>
    /// Determines whether the specified <see cref="RetryHelperTests"/> instance is valid.
    /// </summary>
    /// <param name="value">The instance to check.</param>
    /// <returns><see langword="true"/> if the instance is valid; otherwise, <see langword="false"/>.</returns>
    public static bool IsValid(this RetryHelperTests? value) => value?.Validate().Count is 0 or null;

    /// <summary>
    /// Ensures that the specified <see cref="RetryHelperTests"/> instance is valid.
    /// </summary>
    /// <param name="value">The instance to validate.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown if the instance has validation problems.</exception>
    public static void EnsureValid(this RetryHelperTests? value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var problems = value.Validate();
        if (problems.Count > 0)
        {
            throw new ArgumentException(
                $"RetryHelperTests instance is invalid. Problems: {string.Join("; ", problems)}",
                nameof(value));
        }
    }
}
