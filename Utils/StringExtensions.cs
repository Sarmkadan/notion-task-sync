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
    /// Determines if a string is null, empty, or contains only whitespace.
    /// Preferred over string.IsNullOrWhiteSpace for fluent syntax.
    /// </summary>
    public static bool IsNullOrEmpty(this string? str)
    {
        return string.IsNullOrWhiteSpace(str);
    }

    /// <summary>
    /// Determines if a string has meaningful content (not null, empty, or whitespace).
    /// Logical opposite of IsNullOrEmpty for better readability.
    /// </summary>
    public static bool HasContent(this string? str)
    {
        return !string.IsNullOrWhiteSpace(str);
    }

    /// <summary>
    /// Truncates a string to a maximum length with optional ellipsis suffix.
    /// Used for displaying long titles or descriptions in truncated form.
    /// </summary>
    public static string Truncate(this string str, int maxLength, string suffix = "...")
    {
        if (str == null || str.Length <= maxLength)
            return str;

        return str.Substring(0, maxLength - suffix.Length) + suffix;
    }

    /// <summary>
    /// Sanitizes a string by removing or replacing invalid filesystem characters.
    /// Critical for converting task titles to valid file names.
    /// </summary>
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
    public static bool IsValidGuid(this string str)
    {
        return Guid.TryParse(str, out _);
    }

    /// <summary>
    /// Converts a string to PascalCase (first letter uppercase, word boundaries at capitals).
    /// Useful for converting API responses to proper naming conventions.
    /// </summary>
    public static string ToPascalCase(this string str)
    {
        if (string.IsNullOrWhiteSpace(str))
            return str;

        var words = str.Split(' ', '-', '_');
        return string.Concat(words.Select(w => char.ToUpper(w[0]) + w.Substring(1).ToLower()));
    }

    /// <summary>
    /// Converts a string to snake_case (lowercase with underscores).
    /// Standard format for configuration keys and database field names.
    /// </summary>
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
    public static string AfterLast(this string str, string delimiter)
    {
        if (string.IsNullOrEmpty(str) || string.IsNullOrEmpty(delimiter))
            return str;

        var index = str.LastIndexOf(delimiter);
        return index >= 0 ? str.Substring(index + delimiter.Length) : str;
    }

    /// <summary>
    /// Extracts the portion of a string before the last occurrence of a delimiter.
    /// Complements AfterLast for splitting strings at boundaries.
    /// </summary>
    public static string BeforeLast(this string str, string delimiter)
    {
        if (string.IsNullOrEmpty(str) || string.IsNullOrEmpty(delimiter))
            return str;

        var index = str.LastIndexOf(delimiter);
        return index >= 0 ? str.Substring(0, index) : str;
    }

    /// <summary>
    /// Determines if a string contains another string case-insensitively.
    /// More explicit than Contains(StringComparison.OrdinalIgnoreCase).
    /// </summary>
    public static bool ContainsIgnoreCase(this string str, string value)
    {
        if (string.IsNullOrEmpty(str) || string.IsNullOrEmpty(value))
            return false;

        return str.Contains(value, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Normalizes line endings to a consistent format (Unix style \n).
    /// Prevents line-ending-based change detection false positives.
    /// </summary>
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
