#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace NotionTaskSync.Commands;

using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

/// <summary>
/// Extension methods for <see cref="ConfigureCommand"/> providing additional functionality
/// for configuration validation, settings inspection, and file operations.
/// </summary>
public static class ConfigureCommandExtensions
{
    /// <summary>
    /// Validates that the current configuration can be loaded from appsettings.json
    /// </summary>
    /// <param name="command">The <see cref="ConfigureCommand"/> instance</param>
    /// <exception cref="ArgumentNullException"><paramref name="command"/> is <see langword="null"/></exception>
    /// <returns>True if configuration is valid and can be loaded, false otherwise</returns>
    public static bool ValidateConfigurationFile(this ConfigureCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);

        try
        {
            const string appSettingsPath = "appsettings.json";
            return File.Exists(appSettingsPath) && !string.IsNullOrWhiteSpace(File.ReadAllText(appSettingsPath));
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Gets the current Notion API key from configuration
    /// </summary>
    /// <param name="command">The <see cref="ConfigureCommand"/> instance</param>
    /// <exception cref="ArgumentNullException"><paramref name="command"/> is <see langword="null"/></exception>
    /// <returns>The API key if available, null otherwise</returns>
    public static string? GetApiKey(this ConfigureCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);

        try
        {
            const string appSettingsPath = "appsettings.json";
            if (!File.Exists(appSettingsPath))
            {
                return null;
            }

            var content = File.ReadAllText(appSettingsPath);
            if (string.IsNullOrWhiteSpace(content))
            {
                return null;
            }

            using var document = JsonDocument.Parse(content);
            if (document.RootElement.TryGetProperty("NotionApi", out var notionApi) &&
                notionApi.TryGetProperty("ApiKey", out var apiKey))
            {
                return apiKey.GetString();
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
    /// <param name="command">The <see cref="ConfigureCommand"/> instance</param>
    /// <exception cref="ArgumentNullException"><paramref name="command"/> is <see langword="null"/></exception>
    /// <returns>The database ID if available, null otherwise</returns>
    public static string? GetDatabaseId(this ConfigureCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);

        try
        {
            const string appSettingsPath = "appsettings.json";
            if (!File.Exists(appSettingsPath))
            {
                return null;
            }

            var content = File.ReadAllText(appSettingsPath);
            if (string.IsNullOrWhiteSpace(content))
            {
                return null;
            }

            using var document = JsonDocument.Parse(content);
            if (document.RootElement.TryGetProperty("NotionApi", out var notionApi) &&
                notionApi.TryGetProperty("DatabaseIds", out var databaseIds) &&
                databaseIds.GetArrayLength() > 0)
            {
                return databaseIds[0].GetString();
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
    /// <param name="command">The <see cref="ConfigureCommand"/> instance</param>
    /// <exception cref="ArgumentNullException"><paramref name="command"/> is <see langword="null"/></exception>
    /// <returns>The sync interval in seconds if available, 300 (default) otherwise</returns>
    public static int GetSyncIntervalSeconds(this ConfigureCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);

        try
        {
            const string appSettingsPath = "appsettings.json";
            if (!File.Exists(appSettingsPath))
            {
                return 300;
            }

            var content = File.ReadAllText(appSettingsPath);
            if (string.IsNullOrWhiteSpace(content))
            {
                return 300;
            }

            using var document = JsonDocument.Parse(content);
            if (document.RootElement.TryGetProperty("AppSettings", out var appSettings) &&
                appSettings.TryGetProperty("DefaultSyncIntervalSeconds", out var interval) &&
                interval.TryGetInt32(out var seconds))
            {
                return seconds;
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
    /// <param name="command">The <see cref="ConfigureCommand"/> instance</param>
    /// <exception cref="ArgumentNullException"><paramref name="command"/> is <see langword="null"/></exception>
    /// <returns>The conflict strategy if available, "last-write" (default) otherwise</returns>
    public static string GetConflictStrategy(this ConfigureCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);

        try
        {
            const string appSettingsPath = "appsettings.json";
            if (!File.Exists(appSettingsPath))
            {
                return "last-write";
            }

            var content = File.ReadAllText(appSettingsPath);
            if (string.IsNullOrWhiteSpace(content))
            {
                return "last-write";
            }

            using var document = JsonDocument.Parse(content);
            if (document.RootElement.TryGetProperty("AppSettings", out var appSettings) &&
                appSettings.TryGetProperty("DefaultConflictStrategy", out var strategy))
            {
                var strategyValue = strategy.GetString();
                return !string.IsNullOrWhiteSpace(strategyValue) ? strategyValue : "last-write";
            }

            return "last-write";
        }
        catch
        {
            return "last-write";
        }
    }
}