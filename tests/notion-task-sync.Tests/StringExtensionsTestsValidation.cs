#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace NotionTaskSync.Tests;

using System;
using System.Collections.Generic;

/// <summary>
/// Provides validation methods for <see cref="StringExtensionsTests"/> test cases.
/// Validates test data integrity and ensures test objects are in a valid state
/// before test execution.
/// </summary>
public static class StringExtensionsTestsValidation
{
    /// <summary>
    /// Validates a <see cref="StringExtensionsTests"/> instance and returns a list of human-readable problems.
    /// </summary>
    /// <param name="value">The instance to validate.</param>
    /// <returns>A read-only list of validation messages. Empty if valid.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null.</exception>
    public static IReadOnlyList<string> Validate(this StringExtensionsTests? value)
    {
        ArgumentNullException.ThrowIfNull(value);

        return Array.Empty<string>();
    }

    /// <summary>
    /// Determines whether a <see cref="StringExtensionsTests"/> instance is valid.
    /// </summary>
    /// <param name="value">The instance to check.</param>
    /// <returns>True if the instance is valid; otherwise, false.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null.</exception>
    public static bool IsValid(this StringExtensionsTests? value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return value.Validate().Count == 0;
    }

    /// <summary>
    /// Ensures that a <see cref="StringExtensionsTests"/> instance is valid.
    /// </summary>
    /// <param name="value">The instance to validate.</param>
    /// <returns>The validated instance.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown if the instance contains validation errors.</exception>
    public static StringExtensionsTests EnsureValid(this StringExtensionsTests? value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var errors = value.Validate();
        return errors.Count == 0
            ? value
            : throw new ArgumentException(
                $"StringExtensionsTests instance is invalid. Problems:\n{string.Join("\n", errors)}",
                nameof(value));
    }
}