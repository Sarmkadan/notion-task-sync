#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace NotionTaskSync.Services;

using System;
using System.Collections.Generic;
using System.Globalization;

/// <summary>
/// Provides validation helpers for <see cref="NotionApiService"/> instances.
/// Validates constructor arguments and runtime configuration state.
/// </summary>
internal static class NotionApiServiceValidation
{
    /// <summary>
    /// Validates the specified <see cref="NotionApiService"/> instance.
    /// </summary>
    /// <param name="value">The service instance to validate.</param>
    /// <returns>An immutable list of human-readable validation problems; empty if valid.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is <see langword="null"/>.</exception>
    public static IReadOnlyList<string> Validate(this NotionApiService? value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var problems = new List<string>();

        // Validate API key state (can be null for anonymous access, but should be reasonable if provided)
        if (!string.IsNullOrWhiteSpace(value._apiKey))
        {
            if (value._apiKey.Length < 32)
            {
                problems.Add("The API key is shorter than 32 characters, which is suspiciously short for a Notion API key.");
            }
            else if (value._apiKey.Length > 256)
            {
                problems.Add("The API key is longer than 256 characters, which is suspiciously long.");
            }
        }

        // Validate HttpClient state (can be null if injected externally)
        if (value._httpClient is null)
        {
            problems.Add("The internal HttpClient instance is null.");
        }
        else
        {
            // HttpClient should have reasonable timeout settings
            if (value._httpClient.Timeout.TotalSeconds < 5)
            {
                problems.Add("The HttpClient timeout is too short (< 5 seconds).");
            }
            else if (value._httpClient.Timeout.TotalSeconds > 120)
            {
                problems.Add("The HttpClient timeout is too long (> 120 seconds).");
            }
        }

        return problems.AsReadOnly();
    }

    /// <summary>
    /// Determines whether the specified <see cref="NotionApiService"/> instance is valid.
    /// </summary>
    /// <param name="value">The service instance to check.</param>
    /// <returns><see langword="true"/> if valid; otherwise, <see langword="false"/>.</returns>
    public static bool IsValid(this NotionApiService? value)
    {
        return value is not null && Validate(value).Count == 0;
    }

    /// <summary>
    /// Ensures that the specified <see cref="NotionApiService"/> instance is valid.
    /// Throws an <see cref="ArgumentException"/> with a detailed message listing all validation problems if invalid.
    /// </summary>
    /// <param name="value">The service instance to validate.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">Thrown when the service instance is invalid, with a message listing all problems.</exception>
    public static void EnsureValid(this NotionApiService? value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var problems = Validate(value);
        if (problems.Count == 0)
        {
            return;
        }

        var errorMessage = string.Join(
            $"{Environment.NewLine}  - ",
            problems);

        throw new ArgumentException(
            $"The NotionApiService instance is invalid. Problems:{Environment.NewLine}  - {errorMessage}{Environment.NewLine}Parameter name: {nameof(value)}");
    }
}