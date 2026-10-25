#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

namespace NotionTaskSync.Infrastructure.Configuration;

using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Application-wide settings loaded from appsettings.json.
/// Contains paths, logging configuration, and sync defaults.
/// </summary>
/// <remarks>
/// Bound from the <c>AppSettings</c> configuration section. Environment variables override values
/// from appsettings.json. Numeric ranges enforced by <see cref="Validate"/> are noted on each property.
/// </remarks>
public sealed class AppSettings
{
    /// <summary>
    /// Gets or sets the root directory for local task files.
    /// Must not be null, empty, or whitespace when <see cref="Validate"/> is called.
    /// Defaults to <c>./tasks</c>.
    /// </summary>
    public string LocalTasksDirectory { get; set; } = "./tasks";

    /// <summary>
    /// Gets or sets the logging level (Debug, Information, Warning, Error, Critical).
    /// Defaults to <c>Information</c>. The value is stored as a string and is not checked by <see cref="Validate"/>.
    /// </summary>
    public string LogLevel { get; set; } = "Information";

    /// <summary>
    /// Gets or sets whether to enable console logging output.
    /// Defaults to <c>true</c>.
    /// </summary>
    public bool EnableConsoleLogging { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether the application should run in dry-run mode.
    /// When <c>true</c>, mutation calls to the Notion API are skipped and only planned operations
    /// are logged. Defaults to <c>false</c>.
    /// </summary>
    public bool DryRun { get; set; } = false;

    /// <summary>
    /// Gets or sets the log file path (if file logging is enabled).
    /// <c>null</c> (the default) means no log file is written.
    /// </summary>
    public string? LogFilePath { get; set; }

    /// <summary>
    /// Gets or sets the default sync interval in seconds.
    /// Must be between 5 and 3600 inclusive when <see cref="Validate"/> is called. Defaults to <c>300</c>.
    /// </summary>
    public int DefaultSyncIntervalSeconds { get; set; } = 300;

    /// <summary>
    /// Gets or sets the default conflict resolution strategy.
    /// Defaults to <c>LastWrite</c>. The value is stored as a string and is not checked by <see cref="Validate"/>.
    /// </summary>
    public string DefaultConflictStrategy { get; set; } = "LastWrite";

    /// <summary>
    /// Gets or sets maximum number of concurrent sync operations.
    /// Must be between 1 and 10 inclusive when <see cref="Validate"/> is called. Defaults to <c>1</c>.
    /// </summary>
    public int MaxConcurrentSyncs { get; set; } = 1;

    /// <summary>
    /// Gets or sets whether to enable change tracking.
    /// Defaults to <c>true</c>.
    /// </summary>
    public bool EnableChangeTracking { get; set; } = true;

    /// <summary>
    /// Gets or sets the maximum number of retries for failed operations.
    /// Must be between 0 and 10 inclusive when <see cref="Validate"/> is called. Defaults to <c>3</c>.
    /// </summary>
    public int MaxRetries { get; set; } = 3;

    /// <summary>
    /// Gets or sets the timeout for API requests in seconds.
    /// Must be between 5 and 300 inclusive when <see cref="Validate"/> is called. Defaults to <c>30</c>.
    /// </summary>
    public int ApiTimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Gets or sets the backup directory for local tasks.
    /// <c>null</c> (the default) means no backup directory is configured.
    /// </summary>
    public string? BackupDirectory { get; set; }

    /// <summary>
    /// Gets or sets whether to create automatic backups.
    /// Defaults to <c>true</c>.
    /// </summary>
    public bool EnableAutoBackup { get; set; } = true;

    /// <summary>
    /// Gets or sets the backup frequency in hours.
    /// Defaults to <c>24</c>. The value is not checked by <see cref="Validate"/>.
    /// </summary>
    public int BackupFrequencyHours { get; set; } = 24;

    /// <summary>
    /// Gets or sets the maximum number of backup files to keep.
    /// Must be between 1 and 1000 inclusive when <see cref="Validate"/> is called. Defaults to <c>10</c>.
    /// </summary>
    public int MaxBackupFiles { get; set; } = 10;

    /// <summary>
    /// Gets or sets the application version.
    /// Defaults to <c>1.0.0</c>.
    /// </summary>
    public string Version { get; set; } = "1.0.0";

    /// <summary>
    /// Gets or sets the environment (Development, Staging, Production).
    /// Defaults to <c>Development</c>. The value is stored as a string and is not checked by <see cref="Validate"/>.
    /// </summary>
    public string Environment { get; set; } = "Development";

    /// <summary>
    /// Gets or sets custom sync configurations by name.
    /// The key is the profile name; the value is an untyped object. Use the
    /// <c>GetSyncProfile</c> extension method to read a profile as a specific type.
    /// </summary>
    public Dictionary<string, object> SyncProfiles { get; set; } = new();

    /// <summary>
    /// Gets or sets the list of task statuses to include in sync (empty = all statuses).
    /// Valid values are: "Todo", "InProgress", "Done", "Blocked", "Archived".
    /// Matching is case-insensitive. Invalid values cause <see cref="Validate"/> to throw.
    /// </summary>
    public List<string> IncludedStatuses { get; set; } = new();

    /// <summary>
    /// Validates the settings, checking that the local tasks directory is set, that numeric values are
    /// within their allowed ranges, and that every entry in <see cref="IncludedStatuses"/> is a known status.
    /// </summary>
    /// <returns>
    /// <c>true</c> if all checks pass; <c>false</c> if the directory is blank or a numeric value is out of range.
    /// </returns>
    /// <exception cref="ArgumentException">Thrown when IncludedStatuses contains invalid status values.</exception>
    public bool Validate()
    {
        if (string.IsNullOrWhiteSpace(LocalTasksDirectory))
            return false;

        if (DefaultSyncIntervalSeconds < 5 || DefaultSyncIntervalSeconds > 3600)
            return false;

        if (MaxConcurrentSyncs < 1 || MaxConcurrentSyncs > 10)
            return false;

        if (MaxRetries < 0 || MaxRetries > 10)
            return false;

        if (ApiTimeoutSeconds < 5 || ApiTimeoutSeconds > 300)
            return false;

        if (MaxBackupFiles < 1 || MaxBackupFiles > 1000)
            return false;

        // Validate IncludedStatuses
        ValidateIncludedStatuses();

        return true;
    }

    /// <summary>
    /// Validates that all status values in IncludedStatuses are one of the known task statuses
    /// (Todo, InProgress, Done, Blocked, Archived), compared case-insensitively.
    /// </summary>
    /// <exception cref="ArgumentException">Thrown when IncludedStatuses contains invalid status values.</exception>
    private void ValidateIncludedStatuses()
    {
        if (IncludedStatuses == null || IncludedStatuses.Count == 0)
            return;

        var validStatuses = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Todo", "InProgress", "Done", "Blocked", "Archived"
        };

        var invalidStatuses = IncludedStatuses
            .Where(status => !validStatuses.Contains(status))
            .ToList();

        if (invalidStatuses.Count > 0)
        {
            throw new ArgumentException(
                $"Invalid status values in IncludedStatuses: {string.Join(", ", invalidStatuses)}. " +
                $"Valid status values are: Todo, InProgress, Done, Blocked, Archived.");
        }
    }

    /// <summary>
    /// Returns a summary of the current settings for logging.
    /// Includes the local tasks directory, log level, sync interval, and environment. Other settings are omitted.
    /// </summary>
    /// <returns>A single-line string describing the key settings.</returns>
    public override string ToString()
    {
        return $"AppSettings(LocalDir={LocalTasksDirectory}, LogLevel={LogLevel}, " +
               $"SyncInterval={DefaultSyncIntervalSeconds}s, Environment={Environment})";
    }
}
