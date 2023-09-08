#nullable enable

namespace NotionTaskSync.Tests;

using System;
using System.Collections.Generic;
using System.Globalization;

/// <summary>
/// Provides validation helpers for <see cref="SyncServiceTests"/> instances.
/// Validates null/empty strings, out-of-range numbers, default dates, and other constraints
/// based on the semantic meaning of each member.
/// </summary>
public static class SyncServiceTestsValidation
{
    /// <summary>
    /// Validates the specified <see cref="SyncServiceTests"/> instance.
    /// </summary>
    /// <param name="value">The instance to validate.</param>
    /// <returns>A list of human-readable validation problems; empty if valid.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null.</exception>
    public static IReadOnlyList<string> Validate(this SyncServiceTests value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var problems = new List<string>();

        // No instance members to validate on SyncServiceTests itself
        // All validation is for method parameters/config objects within the test methods

        return problems.AsReadOnly();
    }

    /// <summary>
    /// Determines whether the specified <see cref="SyncServiceTests"/> instance is valid.
    /// </summary>
    /// <param name="value">The instance to check.</param>
    /// <returns><see langword="true"/> if valid; otherwise, <see langword="false"/>.</returns>
    public static bool IsValid(this SyncServiceTests value) => Validate(value).Count == 0;

    /// <summary>
    /// Ensures that the specified <see cref="SyncServiceTests"/> instance is valid.
    /// </summary>
    /// <param name="value">The instance to validate.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown if <paramref name="value"/> is invalid, containing all validation problems.</exception>
    public static void EnsureValid(this SyncServiceTests value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var problems = Validate(value);
        if (problems.Count > 0)
        {
            throw new ArgumentException(
                $"SyncServiceTests instance is invalid:{Environment.NewLine}{string.Join(Environment.NewLine, problems)}");
        }
    }
}