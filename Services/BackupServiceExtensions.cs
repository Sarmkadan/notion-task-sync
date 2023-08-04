#nullable enable

namespace NotionTaskSync.Services;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

/// <summary>
/// Extension methods for <see cref="BackupService"/> providing additional backup management functionality.
/// </summary>
public static class BackupServiceExtensions
{
    /// <summary>
    /// Creates a backup with automatic label based on current date/time.
    /// </summary>
    /// <param name="backupService">The backup service instance.</param>
    /// <returns>Information about the created backup.</returns>
    public static async Task<BackupInfo> CreateDailyBackupAsync(this BackupService backupService)
    {
        var label = $"daily_{DateTime.UtcNow:yyyyMMdd}";
        return await backupService.CreateBackupAsync(label);
    }

    /// <summary>
    /// Finds the most recent backup by creation date.
    /// </summary>
    /// <param name="backupService">The backup service instance.</param>
    /// <returns>The most recent backup, or null if no backups exist.</returns>
    public static BackupInfo? GetLatestBackup(this BackupService backupService)
    {
        return backupService.GetAvailableBackups().FirstOrDefault();
    }

    /// <summary>
    /// Checks if a backup with the specified label exists.
    /// </summary>
    /// <param name="backupService">The backup service instance.</param>
    /// <param name="label">The label to search for.</param>
    /// <returns>True if a backup with the label exists; otherwise, false.</returns>
    public static bool HasBackupWithLabel(this BackupService backupService, string label)
    {
        return backupService.GetAvailableBackups()
            .Any(b => string.Equals(b.Label, label, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Gets the total number of backup files across all backups.
    /// </summary>
    /// <param name="backupService">The backup service instance.</param>
    /// <returns>The total number of files in all backups.</returns>
    public static long GetTotalFileCount(this BackupService backupService)
    {
        return backupService.GetAvailableBackups()
            .Sum(b => (long)b.FileCount);
    }

    /// <summary>
    /// Gets the total age of all backups combined.
    /// </summary>
    /// <param name="backupService">The backup service instance.</param>
    /// <returns>The total age of all backups as a TimeSpan.</returns>
    public static TimeSpan GetTotalAge(this BackupService backupService)
    {
        var backups = backupService.GetAvailableBackups();
        if (backups.Count == 0)
        {
            return TimeSpan.Zero;
        }

        var oldest = backups.Min(b => b.CreatedAt);
        var newest = backups.Max(b => b.CreatedAt);
        return newest - oldest;
    }

    /// <summary>
    /// Gets backups filtered by label pattern.
    /// </summary>
    /// <param name="backupService">The backup service instance.</param>
    /// <param name="labelPattern">The label pattern to match (supports wildcards).</param>
    /// <returns>Filtered list of backups matching the pattern.</returns>
    public static List<BackupInfo> GetBackupsByLabelPattern(this BackupService backupService, string labelPattern)
    {
        var backups = backupService.GetAvailableBackups();

        if (string.IsNullOrWhiteSpace(labelPattern) || labelPattern == "*")
        {
            return backups;
        }

        // Simple wildcard matching
        var pattern = labelPattern
            .Replace(".", "\\.")
            .Replace("*", ".*")
            .Replace("?", ".?");

        var regex = new System.Text.RegularExpressions.Regex(pattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        return backups
            .Where(b => b.Label != null && regex.IsMatch(b.Label))
            .OrderByDescending(b => b.CreatedAt)
            .ToList();
    }

    /// <summary>
    /// Gets backups created within the specified time range.
    /// </summary>
    /// <param name="backupService">The backup service instance.</param>
    /// <param name="startTime">The start of the time range (inclusive).</param>
    /// <param name="endTime">The end of the time range (inclusive).</param>
    /// <returns>Backups created within the specified time range.</returns>
    public static List<BackupInfo> GetBackupsInRange(this BackupService backupService, DateTime startTime, DateTime endTime)
    {
        return backupService.GetAvailableBackups()
            .Where(b => b.CreatedAt >= startTime && b.CreatedAt <= endTime)
            .OrderBy(b => b.CreatedAt)
            .ToList();
    }

    /// <summary>
    /// Gets the oldest backup.
    /// </summary>
    /// <param name="backupService">The backup service instance.</param>
    /// <returns>The oldest backup, or null if no backups exist.</returns>
    public static BackupInfo? GetOldestBackup(this BackupService backupService)
    {
        return backupService.GetAvailableBackups()
            .OrderBy(b => b.CreatedAt)
            .FirstOrDefault();
    }

    /// <summary>
    /// Checks if any backups exist.
    /// </summary>
    /// <param name="backupService">The backup service instance.</param>
    /// <returns>True if backups exist; otherwise, false.</returns>
    public static bool HasBackups(this BackupService backupService)
    {
        return backupService.GetAvailableBackups().Count > 0;
    }

    /// <summary>
    /// Gets the backup with the specified ID.
    /// </summary>
    /// <param name="backupService">The backup service instance.</param>
    /// <param name="id">The backup ID to find.</param>
    /// <returns>The backup with the specified ID, or null if not found.</returns>
    public static BackupInfo? GetBackupById(this BackupService backupService, Guid id)
    {
        return backupService.GetAvailableBackups()
            .FirstOrDefault(b => b.Id == id);
    }

    /// <summary>
    /// Gets backups sorted by file count (descending).
    /// </summary>
    /// <param name="backupService">The backup service instance.</param>
    /// <returns>Backups sorted by file count in descending order.</returns>
    public static List<BackupInfo> GetBackupsByFileCountDescending(this BackupService backupService)
    {
        return backupService.GetAvailableBackups()
            .OrderByDescending(b => b.FileCount)
            .ToList();
    }

    /// <summary>
    /// Gets backups sorted by age (oldest first).
    /// </summary>
    /// <param name="backupService">The backup service instance.</param>
    /// <returns>Backups sorted by age in ascending order (oldest first).</returns>
    public static List<BackupInfo> GetBackupsByAgeAscending(this BackupService backupService)
    {
        return backupService.GetAvailableBackups()
            .OrderBy(b => b.CreatedAt)
            .ToList();
    }
}
