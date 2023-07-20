#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace NotionTaskSync.Commands;

using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Threading.Tasks;

/// <summary>
/// Extension methods for ConfigureCommand providing additional functionality
/// for configuration validation, settings inspection, and file operations.
/// </summary>
public static class ConfigureCommandExtensions
{
    /// <summary>
    /// Validates that the current configuration can be loaded from appsettings.json
    /// </summary>
    /// <param name="command">The ConfigureCommand instance</param>
    /// <returns>True if configuration is valid and can be loaded, false otherwise</returns>
    public static bool ValidateConfigurationFile(this ConfigureCommand command)
    {
        try
        {
            var appSettingsPath = "appsettings.json";
            if (!File.Exists(appSettingsPath))
            {
                return false;
            }

            var content = File.ReadAllText(appSettingsPath);
            return !string.IsNullOrWhiteSpace(content);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Gets the current Notion API key from configuration
    /// </summary>
    /// <param name="command">The ConfigureCommand instance</param>
    /// <returns>The API key if available, null otherwise</returns>
    public static string? GetApiKey(this ConfigureCommand command)
    {
        try
        {
            var appSettingsPath = "appsettings.json";
            if (!File.Exists(appSettingsPath))
            {
                return null;
            }

            var content = File.ReadAllText(appSettingsPath);
            if (string.IsNullOrWhiteSpace(content))
            {
                return null;
            }

            // Simple parsing to extract API key
            var keyStart = content.IndexOf("\"ApiKey\":");
            if (keyStart >= 0)
            {
                var valueStart = content.IndexOf('"', keyStart + 9) + 1;
                var valueEnd = content.IndexOf('"', valueStart);
                if (valueEnd > valueStart)
                {
                    return content[valueStart..valueEnd];
                }
            }

            return null;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Gets the current database ID from configuration
    /// </summary>
    /// <param name="command">The ConfigureCommand instance</param>
    /// <returns>The database ID if available, null otherwise</returns>
    public static string? GetDatabaseId(this ConfigureCommand command)
    {
        try
        {
            var appSettingsPath = "appsettings.json";
            if (!File.Exists(appSettingsPath))
            {
                return null;
            }

            var content = File.ReadAllText(appSettingsPath);
            if (string.IsNullOrWhiteSpace(content))
            {
                return null;
            }

            // Simple parsing to extract database ID
            var dbStart = content.IndexOf("\"DatabaseIds\":");
            if (dbStart >= 0)
            {
                var arrayStart = content.IndexOf('[', dbStart);
                var arrayEnd = content.IndexOf(']', arrayStart);
                if (arrayStart > 0 && arrayEnd > arrayStart)
                {
                    var contentBetween = content[arrayStart..arrayEnd];
                    var quoteStart = contentBetween.IndexOf('"');
                    var quoteEnd = contentBetween.LastIndexOf('"');
                    if (quoteStart >= 0 && quoteEnd > quoteStart)
                    {
                        return contentBetween[(quoteStart + 1)..quoteEnd];
                    }
                }
            }

            return null;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Gets the current sync interval from configuration
    /// </summary>
    /// <param name="command">The ConfigureCommand instance</param>
    /// <returns>The sync interval in seconds if available, 300 (default) otherwise</returns>
    public static int GetSyncIntervalSeconds(this ConfigureCommand command)
    {
        try
        {
            var appSettingsPath = "appsettings.json";
            if (!File.Exists(appSettingsPath))
            {
                return 300;
            }

            var content = File.ReadAllText(appSettingsPath);
            if (string.IsNullOrWhiteSpace(content))
            {
                return 300;
            }

            // Simple parsing to extract sync interval
            var intervalStart = content.IndexOf("\"DefaultSyncIntervalSeconds\":");
            if (intervalStart >= 0)
            {
                var valueStart = intervalStart + 32; // length of "\"DefaultSyncIntervalSeconds\":"
                var valueEnd = content.IndexOfAny(new[] { ',', '}', '\n', '\r' }, valueStart);
                if (valueEnd > valueStart && int.TryParse(content[valueStart..valueEnd], out var interval))
                {
                    return interval;
                }
            }

            return 300;
        }
        catch
        {
            return 300;
        }
    }

    /// <summary>
    /// Gets the current conflict strategy from configuration
    /// </summary>
    /// <param name="command">The ConfigureCommand instance</param>
    /// <returns>The conflict strategy if available, "last-write" (default) otherwise</returns>
    public static string GetConflictStrategy(this ConfigureCommand command)
    {
        try
        {
            var appSettingsPath = "appsettings.json";
            if (!File.Exists(appSettingsPath))
            {
                return "last-write";
            }

            var content = File.ReadAllText(appSettingsPath);
            if (string.IsNullOrWhiteSpace(content))
            {
                return "last-write";
            }

            // Simple parsing to extract conflict strategy
            var strategyStart = content.IndexOf("\"DefaultConflictStrategy\":");
            if (strategyStart >= 0)
            {
                var valueStart = strategyStart + 27; // length of "\"DefaultConflictStrategy\":"
                var valueEnd = content.IndexOfAny(new[] { ',', '}', '\n', '\r' }, valueStart);
                if (valueEnd > valueStart)
                {
                    var strategy = content[valueStart..valueEnd].Trim(' ', '"', '\n', '\r');
                    if (!string.IsNullOrWhiteSpace(strategy))
                    {
                        return strategy;
                    }
                }
            }

            return "last-write";
        }
        catch
        {
            return "last-write";
        }
    }
}