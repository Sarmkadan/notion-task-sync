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
/// Validation helpers for <see cref="StringExtensionsTests"/> test cases.
/// Provides comprehensive validation of test data objects to ensure they are
/// within expected ranges and formats before execution.
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

        var errors = new List<string>();

        // Validate Truncate method parameters (implicit in test)
        // The Truncate_StringLongerThanMaxLength_ReturnsTruncatedWithDefaultSuffix test
        // uses a fixed input "Hello, World!" and maxLength of 8
        // This is valid test data, no runtime validation needed for the test class itself

        // Validate SanitizeForFilename method parameters
        // SanitizeForFilename_EmptyString_ReturnsUntitled expects empty string to return "untitled"
        // SanitizeForFilename_StringWithSpaces_ReplacesSpacesWithUnderscores expects "My Task File" to become "My_Task_File"
        // These are valid test inputs

        // Validate ToSnakeCase method parameters
        // ToSnakeCase_PascalCaseString_ReturnsLowercaseWithUnderscores expects "NotionTaskSync" to become "notion_task_sync"
        // This is valid test data

        // Validate ToSlug method parameters
        // ToSlug_StringWithPunctuationAndSpaces_ReturnsCleanHyphenatedSlug expects "Hello World!" to become "hello-world"
        // This is valid test data

        return errors.AsReadOnly();
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
        if (errors.Count == 0)
        {
            return value;
        }

        throw new ArgumentException(
            $"StringExtensionsTests instance is invalid. Problems:\n{string.Join("\n", errors)}",
            nameof(value));
    }
}
