#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace NotionTaskSync.Utils;

using System;
using System.Linq;
using System.Text.RegularExpressions;

/// <summary>
/// Extension methods for string manipulation and validation.
/// Centralizes common string operations used throughout the sync pipeline.
/// Improves code readability and reduces duplication.
/// </summary>
public static class StringExtensions
{
    /// <summary>
    /// Truncates a string to a maximum length with optional ellipsis suffix.
    /// Used for displaying long titles or descriptions in truncated form.
    /// </summary>
    /// <param name="str">The string to truncate.</param>
    /// <param name="maxLength">The maximum length of the resulting string.</param>
    /// <param name="suffix">The suffix to append when truncating. Defaults to "...".</param>
    /// <returns>The truncated string, or the original string if it's shorter than maxLength.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="str"/> is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="maxLength"/> is less than 0.</exception>
    public static string Truncate(this string str, int maxLength, string suffix = "...")
    {
        ArgumentNullException.ThrowIfNull(str);
        ArgumentOutOfRangeException.ThrowIfNegative(maxLength);

        if (str.Length <= maxLength)
            return str;

        var truncatedLength = Math.Max(0, maxLength - suffix.Length);
        return str[..Math.Min(truncatedLength, str.Length)] + suffix;
    }

    /// <summary>
    /// Sanitizes a string by removing or replacing invalid filesystem characters.
    /// Critical for converting task titles to valid file names.
    /// </summary>
    /// <param name="str">The string to sanitize.</param>
    /// <returns>A sanitized filename-safe string. Returns "untitled" if input is null or whitespace.</returns>
    public static string SanitizeForFilename(this string str)
    {
        if (string.IsNullOrWhiteSpace(str))
            return "untitled";

        // Remove or replace invalid filename characters
        var invalidChars = System.IO.Path.GetInvalidFileNameChars();
        var sanitized = new string(str
            .Where(c => !invalidChars.Contains(c))
            .ToArray());

        // Replace spaces with underscores for consistency
        sanitized = sanitized.Replace(" ", "_");

        // Collapse consecutive underscores
        while (sanitized.Contains("__"))
            sanitized = sanitized.Replace("__", "_");

        return sanitized.Length > 0 ? sanitized : "untitled";
    }

    /// <summary>
    /// Validates if a string is a valid email address format.
    /// Used for assignee validation in sync operations.
    /// </summary>
    /// <param name="str">The string to validate.</param>
    /// <returns>True if the string is a valid email address; otherwise, false.</returns>
    public static bool IsValidEmail(this string str)
    {
        if (string.IsNullOrWhiteSpace(str))
            return false;

        try
        {
            var addr = new System.Net.Mail.MailAddress(str);
            return addr.Address == str;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Validates if a string matches a UUID/GUID format.
    /// Used to verify Notion page IDs and other database identifiers.
    /// </summary>
    /// <param name="str">The string to validate.</param>
    /// <returns>True if the string is a valid GUID/UUID; otherwise, false.</returns>
    public static bool IsValidGuid(this string str)
    {
        return Guid.TryParse(str, out _);
    }

    /// <summary>
    /// Converts a string to PascalCase (first letter uppercase, word boundaries at capitals).
    /// Useful for converting API responses to proper naming conventions.
    /// </summary>
    /// <param name="str">The string to convert.</param>
    /// <returns>The PascalCase string, or the original string if conversion is not possible.</returns>
    public static string ToPascalCase(this string str)
    {
        if (string.IsNullOrWhiteSpace(str))
            return str;

        var words = str.Split(new[] {' ', '-', '_'}, StringSplitOptions.RemoveEmptyEntries);
        return string.Concat(words.Select(w =>
            w.Length > 0
                ? char.ToUpperInvariant(w[0]) + w[1..].ToLowerInvariant()
                : string.Empty));
    }

    /// <summary>
    /// Converts a string to snake_case (lowercase with underscores).
    /// Standard format for configuration keys and database field names.
    /// </summary>
    /// <param name="str">The string to convert.</param>
    /// <returns>The snake_case string, or the original string if it's null or whitespace.</returns>
    public static string ToSnakeCase(this string str)
    {
        if (string.IsNullOrWhiteSpace(str))
            return str;

        // Insert underscore before uppercase letters preceded by lowercase
        var snaked = Regex.Replace(str, "([a-z0-9])([A-Z])", "$1_$2");
        // Replace spaces and hyphens with underscores
        snaked = snaked.Replace(" ", "_").Replace("-", "_");
        // Convert to lowercase
        return snaked.ToLowerInvariant();
    }

    /// <summary>
    /// Extracts the portion of a string after the last occurrence of a delimiter.
    /// Useful for path manipulation and extracting file names from full paths.
    /// </summary>
    /// <param name="str">The source string.</param>
    /// <param name="delimiter">The delimiter to search for.</param>
    /// <returns>The substring after the last delimiter, or the original string if delimiter is not found.</returns>
    public static string AfterLast(this string str, string delimiter)
    {
        ArgumentNullException.ThrowIfNull(str);
        ArgumentNullException.ThrowIfNull(delimiter);

        var index = str.LastIndexOf(delimiter, StringComparison.Ordinal);
        return index >= 0 ? str[(index + delimiter.Length)..] : str;
    }

    /// <summary>
    /// Extracts the portion of a string before the last occurrence of a delimiter.
    /// Complements AfterLast for splitting strings at boundaries.
    /// </summary>
    /// <param name="str">The source string.</param>
    /// <param name="delimiter">The delimiter to search for.</param>
    /// <returns>The substring before the last delimiter, or the original string if delimiter is not found.</returns>
    public static string BeforeLast(this string str, string delimiter)
    {
        ArgumentNullException.ThrowIfNull(str);
        ArgumentNullException.ThrowIfNull(delimiter);

        var index = str.LastIndexOf(delimiter, StringComparison.Ordinal);
        return index >= 0 ? str[..index] : str;
    }

    /// <summary>
    /// Determines if a string contains another string case-insensitively.
    /// More explicit than Contains(StringComparison.OrdinalIgnoreCase).
    /// </summary>
    /// <param name="str">The source string.</param>
    /// <param name="value">The value to search for.</param>
    /// <returns>True if the value is found case-insensitively; otherwise, false.</returns>
    public static bool ContainsIgnoreCase(this string str, string value)
    {
        ArgumentNullException.ThrowIfNull(str);
        ArgumentNullException.ThrowIfNull(value);

        return str.Contains(value, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Normalizes line endings to a consistent format (Unix style \n).
    /// Prevents line-ending-based change detection false positives.
    /// </summary>
    /// <param name="str">The string with potentially mixed line endings.</param>
    /// <returns>A string with normalized line endings, or the original string if it's null or empty.</returns>
    public static string NormalizeLineEndings(this string str)
    {
        if (string.IsNullOrEmpty(str))
            return str;

        return str.Replace("\r\n", "\n").Replace("\r", "\n");
    }

    /// <summary>
    /// Generates a slug from a string suitable for URLs or identifiers.
    /// Combines lowercasing, removing special characters, and replacing spaces with hyphens.
    /// </summary>
    /// <param name="str">The string to convert to a slug.</param>
    /// <returns>A URL-safe slug string. Returns "untitled" if input is null or whitespace.</returns>
    public static string ToSlug(this string str)
    {
        if (string.IsNullOrWhiteSpace(str))
            return "untitled";

        // Convert to lowercase
        var slug = str.ToLowerInvariant();

        // Remove invalid URL characters
        slug = Regex.Replace(slug, @"[^a-z0-9\s\-]", "");

        // Replace spaces with hyphens
        slug = Regex.Replace(slug, @"\s+", "-");

        // Collapse consecutive hyphens
        slug = Regex.Replace(slug, @"-+", "-");

        // Trim hyphens from ends
        return slug.Trim('-');
    }
}