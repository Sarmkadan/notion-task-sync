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
public sealed class AppSettings
{
    /// <summary>
    /// Gets or sets the root directory for local task files.
    /// </summary>
    public string LocalTasksDirectory { get; set; } = "./tasks";

    /// <summary>
    /// Gets or sets the logging level (Debug, Information, Warning, Error, Critical).
    /// </summary>
    public string LogLevel { get; set; } = "Information";

    /// <summary>
    /// Gets or sets whether to enable console logging output.
    /// </summary>
    public bool EnableConsoleLogging { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether the application should run in dry‑run mode.
    /// When true, mutation calls to the Notion API are skipped and only planned operations
    /// are logged.
    /// </summary>
    public bool DryRun { get; set; } = false;

    /// <summary>
    /// Gets or sets the log file path (if file logging is enabled).
    /// </summary>
    public string? LogFilePath { get; set; }

    /// <summary>
    /// Gets or sets the default sync interval in seconds.
    /// </summary>
    public int DefaultSyncIntervalSeconds { get; set; } = 300;

    /// <summary>
    /// Gets or sets the default conflict resolution strategy.
    /// </summary>
    public string DefaultConflictStrategy { get; set; } = "LastWrite";

    /// <summary>
    /// Gets or sets maximum number of concurrent sync operations.
    /// </summary>
    public int MaxConcurrentSyncs { get; set; } = 1;

    /// <summary>
    /// Gets or sets whether to enable change tracking.
    /// </summary>
    public bool EnableChangeTracking { get; set; } = true;

    /// <summary>
    /// Gets or sets the maximum number of retries for failed operations.
    /// </summary>
    public int MaxRetries { get; set; } = 3;

    /// <summary>
    /// Gets or sets the timeout for API requests in seconds.
    /// </summary>
    public int ApiTimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Gets or sets the backup directory for local tasks.
    /// </summary>
    public string? BackupDirectory { get; set; }

    /// <summary>
    /// Gets or sets whether to create automatic backups.
    /// </summary>
    public bool EnableAutoBackup { get; set; } = true;

    /// <summary>
    /// Gets or sets the backup frequency in hours.
    /// </summary>
    public int BackupFrequencyHours { get; set; } = 24;

    /// <summary>
    /// Gets or sets the maximum number of backup files to keep.
    /// </summary>
    public int MaxBackupFiles { get; set; } = 10;

    /// <summary>
    /// Gets or sets the application version.
    /// </summary>
    public string Version { get; set; } = "1.0.0";

    /// <summary>
    /// Gets or sets the environment (Development, Staging, Production).
    /// </summary>
    public string Environment { get; set; } = "Development";

    /// <summary>
    /// Gets or sets custom sync configurations by name.
    /// </summary>
    public Dictionary<string, object> SyncProfiles { get; set; } = new();

    /// <summary>
    /// Gets or sets the list of task statuses to include in sync (empty = all statuses).
    /// Valid values are: "Todo", "InProgress", "Done", "Blocked", "Archived".
    /// </summary>
    public List<string> IncludedStatuses { get; set; } = new();

    /// <summary>
    /// Validates the settings ensuring all paths are configured correctly and status values are valid.
    /// </summary>
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
    /// Validates that all status values in IncludedStatuses are valid TaskStatus enum values.
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
    /// </summary>
    public override string ToString()
    {
        return $"AppSettings(LocalDir={LocalTasksDirectory}, LogLevel={LogLevel}, " +
               $"SyncInterval={DefaultSyncIntervalSeconds}s, Environment={Environment})";
    }
}