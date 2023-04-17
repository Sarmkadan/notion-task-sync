// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace NotionTaskSync.Utils;

using NotionTaskSync.Constants;
using System;
using System.Text.RegularExpressions;

/// <summary>
/// Provides validation utilities for common data types and patterns.
/// </summary>
public static class ValidationHelper
{
    /// <summary>
    /// Validates that a string is a valid Notion page or database ID format.
    /// </summary>
    public static bool IsValidNotionId(string? id)
    {
        if (string.IsNullOrEmpty(id))
            return false;

        // Notion IDs are typically 36 characters with dashes (UUID format)
        return id.Length == AppConstants.MaxPageIdLength &&
               Regex.IsMatch(id, @"^[a-f0-9\-]{36}$");
    }

    /// <summary>
    /// Validates that a string is a valid email address.
    /// </summary>
    public static bool IsValidEmail(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return false;

        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Validates that a file path is within acceptable bounds.
    /// </summary>
    public static bool IsValidFilePath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return false;

        try
        {
            var fullPath = Path.GetFullPath(path);
            return !string.IsNullOrEmpty(fullPath);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Validates that a string represents a valid directory path.
    /// </summary>
    public static bool IsValidDirectoryPath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return false;

        try
        {
            var fullPath = Path.GetFullPath(path);
            return !string.IsNullOrEmpty(fullPath);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Validates that an API key has the correct format.
    /// </summary>
    public static bool IsValidApiKey(string? apiKey)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
            return false;

        // Basic check: API key should be at least 20 characters
        return apiKey.Length >= 20;
    }

    /// <summary>
    /// Validates that a task priority is within acceptable range.
    /// </summary>
    public static bool IsValidPriority(int priority)
    {
        return priority >= 0 && priority <= 100;
    }

    /// <summary>
    /// Validates that an integer is within a specified range.
    /// </summary>
    public static bool IsInRange(int value, int min, int max)
    {
        return value >= min && value <= max;
    }

    /// <summary>
    /// Validates that a string length is within bounds.
    /// </summary>
    public static bool IsLengthValid(string? value, int minLength, int maxLength)
    {
        if (string.IsNullOrEmpty(value))
            return minLength == 0;

        return value.Length >= minLength && value.Length <= maxLength;
    }

    /// <summary>
    /// Sanitizes a string by removing potentially harmful characters.
    /// </summary>
    public static string SanitizeString(string? input)
    {
        if (string.IsNullOrEmpty(input))
            return string.Empty;

        // Remove control characters and trim whitespace
        var sanitized = Regex.Replace(input, @"[\x00-\x1F\x7F]", string.Empty);
        return sanitized.Trim();
    }

    /// <summary>
    /// Validates that a name follows identifier naming conventions.
    /// </summary>
    public static bool IsValidIdentifierName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return false;

        // Must start with letter or underscore, contain only alphanumerics and underscores
        return Regex.IsMatch(name, @"^[a-zA-Z_][a-zA-Z0-9_]*$");
    }

    /// <summary>
    /// Validates that a URL is properly formatted.
    /// </summary>
    public static bool IsValidUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return false;

        try
        {
            var uri = new Uri(url);
            return uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps;
        }
        catch
        {
            return false;
        }
    }
}
