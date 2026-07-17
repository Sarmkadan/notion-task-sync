using System;
using System.Collections.Generic;
using System.Globalization;

namespace NotionTaskSync.Infrastructure.Configuration;

/// <summary>
/// Provides extension methods for <see cref="AppSettings"/>.
/// </summary>
public static class AppSettingsExtensions
{
    /// <summary>
    /// Safely retrieves a sync profile by its name.
    /// </summary>
    /// <typeparam name="T">The expected type of the sync profile.</typeparam>
    /// <param name="settings">The <see cref="AppSettings"/> instance.</param>
    /// <param name="profileName">The name of the sync profile.</param>
    /// <returns>The sync profile object if found and of the correct type; otherwise, null.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="settings"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="profileName"/> is null or empty.</exception>
    public static T? GetSyncProfile<T>(this AppSettings settings, string profileName)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentException.ThrowIfNullOrEmpty(profileName);

        return settings.SyncProfiles.TryGetValue(profileName, out var profile) && profile is T typedProfile
            ? typedProfile
            : default;
    }

    /// <summary>
    /// Checks if auto-backup is correctly configured (enabled and directory specified).
    /// </summary>
    /// <param name="settings">The <see cref="AppSettings"/> instance.</param>
    /// <returns>True if auto-backup is enabled and the directory is configured; otherwise, false.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="settings"/> is null.</exception>
    public static bool IsBackupConfigured(this AppSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        return settings.EnableAutoBackup && !string.IsNullOrWhiteSpace(settings.BackupDirectory);
    }

    /// <summary>
    /// Retrieves the effective log level, returning "Information" if not explicitly set.
    /// </summary>
    /// <param name="settings">The <see cref="AppSettings"/> instance.</param>
    /// <returns>The log level.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="settings"/> is null.</exception>
    public static string GetEffectiveLogLevel(this AppSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        return string.IsNullOrWhiteSpace(settings.LogLevel) ? "Information" : settings.LogLevel;
    }

    /// <summary>
    /// Formats the API timeout as a string for display purposes, ensuring invariant culture.
    /// </summary>
    /// <param name="settings">The <see cref="AppSettings"/> instance.</param>
    /// <returns>The formatted API timeout string.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="settings"/> is null.</exception>
    public static string GetFormattedApiTimeout(this AppSettings settings) =>
        settings.ApiTimeoutSeconds.ToString(CultureInfo.InvariantCulture);
}