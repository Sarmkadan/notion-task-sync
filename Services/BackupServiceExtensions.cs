#nullable enable

namespace NotionTaskSync.Services;

using System;
using System.Collections.Generic;
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
    /// <exception cref="ArgumentNullException"><paramref name="backupService"/> is <see langword="null"/>.</exception>
    /// <returns>Information about the created backup.</returns>
    public static async Task<BackupInfo> CreateDailyBackupAsync(this BackupService backupService)
    {
        ArgumentNullException.ThrowIfNull(backupService);

        var label = $"daily_{DateTime.UtcNow:yyyyMMdd}";
        return await backupService.CreateBackupAsync(label).ConfigureAwait(false);
    }

    /// <summary>
    /// Finds the most recent backup by creation date.
    /// </summary>
    /// <param name="backupService">The backup service instance.</param>
    /// <exception cref="ArgumentNullException"><paramref name="backupService"/> is <see langword="null"/>.</exception>
    /// <returns>The most recent backup, or <see langword="null"/> if no backups exist.</returns>
    public static BackupInfo? GetLatestBackup(this BackupService backupService)
    {
        ArgumentNullException.ThrowIfNull(backupService);

        return backupService.GetAvailableBackups().FirstOrDefault();
    }

    /// <summary>
    /// Checks if a backup with the specified label exists.
    /// </summary>
    /// <param name="backupService">The backup service instance.</param>
    /// <param name="label">The label to search for.</param>
    /// <exception cref="ArgumentNullException"><paramref name="backupService"/> or <paramref name="label"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="label"/> is empty or consists only of whitespace.</exception>
    /// <returns>True if a backup with the label exists; otherwise, false.</returns>
    public static bool HasBackupWithLabel(this BackupService backupService, string label)
    {
        ArgumentNullException.ThrowIfNull(backupService);
        ArgumentNullException.ThrowIfNull(label);
        ArgumentException.ThrowIfNullOrWhiteSpace(label);

        return backupService.GetAvailableBackups()
            .Any(b => string.Equals(b.Label, label, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Gets the total number of backup files across all backups.
    /// </summary>
    /// <param name="backupService">The backup service instance.</param>
    /// <exception cref="ArgumentNullException"><paramref name="backupService"/> is <see langword="null"/>.</exception>
    /// <returns>The total number of files in all backups.</returns>
    public static long GetTotalFileCount(this BackupService backupService)
    {
        ArgumentNullException.ThrowIfNull(backupService);

        return backupService.GetAvailableBackups()
            .Sum(b => (long)b.FileCount);
    }

    /// <summary>
    /// Gets the total age of all backups combined.
    /// </summary>
    /// <param name="backupService">The backup service instance.</param>
    /// <exception cref="ArgumentNullException"><paramref name="backupService"/> is <see langword="null"/>.</exception>
    /// <returns>The total age of all backups as a <see cref="TimeSpan"/>.</returns>
    public static TimeSpan GetTotalAge(this BackupService backupService)
    {
        ArgumentNullException.ThrowIfNull(backupService);

        var backups = backupService.GetAvailableBackups();
        return backups.Count == 0
            ? TimeSpan.Zero
            : backups.Max(b => b.CreatedAt) - backups.Min(b => b.CreatedAt);
    }

    /// <summary>
    /// Gets backups filtered by label pattern.
    /// </summary>
    /// <remarks>
    /// Supports simple wildcard patterns:
    /// <list type="bullet">
    /// <item><c>*</c> matches any sequence of characters</item>
    /// <item><c>?</c> matches any single character</item>
    /// <item>Escaped with backslash for literal matching</item>
    /// </list>
    /// </remarks>
    /// <param name="backupService">The backup service instance.</param>
    /// <param name="labelPattern">The label pattern to match (supports wildcards).</param>
    /// <exception cref="ArgumentNullException"><paramref name="backupService"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="labelPattern"/> is empty or consists only of whitespace.</exception>
    /// <returns>Filtered list of backups matching the pattern, ordered by creation date (newest first).</returns>
    public static List<BackupInfo> GetBackupsByLabelPattern(this BackupService backupService, string labelPattern)
    {
        ArgumentNullException.ThrowIfNull(backupService);
        ArgumentException.ThrowIfNullOrWhiteSpace(labelPattern);

        var backups = backupService.GetAvailableBackups();

        if (labelPattern == "*")
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
            .Where(b => b.Label is not null && regex.IsMatch(b.Label))
            .OrderByDescending(b => b.CreatedAt)
            .ToList();
    }

    /// <summary>
    /// Gets backups created within the specified time range.
    /// </summary>
    /// <param name="backupService">The backup service instance.</param>
    /// <param name="startTime">The start of the time range (inclusive).</param>
    /// <param name="endTime">The end of the time range (inclusive).</param>
    /// <exception cref="ArgumentNullException"><paramref name="backupService"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="startTime"/> is after <paramref name="endTime"/>.</exception>
    /// <returns>Backups created within the specified time range, ordered by creation date (oldest first).</returns>
    public static List<BackupInfo> GetBackupsInRange(this BackupService backupService, DateTime startTime, DateTime endTime)
    {
        ArgumentNullException.ThrowIfNull(backupService);

        if (startTime > endTime)
        {
            throw new ArgumentOutOfRangeException(nameof(startTime), "Start time cannot be after end time.");
        }

        return backupService.GetAvailableBackups()
            .Where(b => b.CreatedAt >= startTime && b.CreatedAt <= endTime)
            .OrderBy(b => b.CreatedAt)
            .ToList();
    }

    /// <summary>
    /// Gets the oldest backup.
    /// </summary>
    /// <param name="backupService">The backup service instance.</param>
    /// <exception cref="ArgumentNullException"><paramref name="backupService"/> is <see langword="null"/>.</exception>
    /// <returns>The oldest backup, or <see langword="null"/> if no backups exist.</returns>
    public static BackupInfo? GetOldestBackup(this BackupService backupService)
    {
        ArgumentNullException.ThrowIfNull(backupService);

        return backupService.GetAvailableBackups()
            .OrderBy(b => b.CreatedAt)
            .FirstOrDefault();
    }

    /// <summary>
    /// Checks if any backups exist.
    /// </summary>
    /// <param name="backupService">The backup service instance.</param>
    /// <exception cref="ArgumentNullException"><paramref name="backupService"/> is <see langword="null"/>.</exception>
    /// <returns>True if backups exist; otherwise, false.</returns>
    public static bool HasBackups(this BackupService backupService)
    {
        ArgumentNullException.ThrowIfNull(backupService);

        return backupService.GetAvailableBackups().Count > 0;
    }

    /// <summary>
    /// Gets the backup with the specified ID.
    /// </summary>
    /// <param name="backupService">The backup service instance.</param>
    /// <param name="id">The backup ID to find.</param>
    /// <exception cref="ArgumentNullException"><paramref name="backupService"/> is <see langword="null"/>.</exception>
    /// <returns>The backup with the specified ID, or <see langword="null"/> if not found.</returns>
    public static BackupInfo? GetBackupById(this BackupService backupService, Guid id)
    {
        ArgumentNullException.ThrowIfNull(backupService);

        return backupService.GetAvailableBackups()
            .FirstOrDefault(b => b.Id == id);
    }

    /// <summary>
    /// Gets backups sorted by file count (descending).
    /// </summary>
    /// <param name="backupService">The backup service instance.</param>
    /// <exception cref="ArgumentNullException"><paramref name="backupService"/> is <see langword="null"/>.</exception>
    /// <returns>Backups sorted by file count in descending order.</returns>
    public static List<BackupInfo> GetBackupsByFileCountDescending(this BackupService backupService)
    {
        ArgumentNullException.ThrowIfNull(backupService);

        return backupService.GetAvailableBackups()
            .OrderByDescending(b => b.FileCount)
            .ToList();
    }

    /// <summary>
    /// Gets backups sorted by age (oldest first).
    /// </summary>
    /// <param name="backupService">The backup service instance.</param>
    /// <exception cref="ArgumentNullException"><paramref name="backupService"/> is <see langword="null"/>.</exception>
    /// <returns>Backups sorted by age in ascending order (oldest first).</returns>
    public static List<BackupInfo> GetBackupsByAgeAscending(this BackupService backupService)
    {
        ArgumentNullException.ThrowIfNull(backupService);

        return backupService.GetAvailableBackups()
            .OrderBy(b => b.CreatedAt)
            .ToList();
    }
}