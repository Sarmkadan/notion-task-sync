#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace NotionTaskSync.Services;

using NotionTaskSync.Domain.Exceptions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

/// <summary>
/// Manages task file backups and restoration.
/// Provides automated and manual backup functionality with retention policies.
/// </summary>
public class BackupService
{
    private readonly string _backupDirectory;
    private readonly int _maxBackupFiles;
    private readonly LocalFileService _fileService;

    public BackupService(string backupDirectory, int maxBackupFiles, LocalFileService fileService)
    {
        _backupDirectory = backupDirectory;
        _maxBackupFiles = maxBackupFiles;
        _fileService = fileService;
    }

    /// <summary>
    /// Creates a backup of all task files.
    /// </summary>
    public async Task<BackupInfo> CreateBackupAsync(string? label = null)
    {
        try
        {
            EnsureBackupDirectoryExists();

            var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
            var backupLabel = label ?? "auto";
            var backupPath = Path.Combine(_backupDirectory, $"backup_{timestamp}_{backupLabel}");

            Directory.CreateDirectory(backupPath);

            var backupedPath = await _fileService.BackupTasksAsync(backupPath).ConfigureAwait(false);

            var info = new BackupInfo
            {
                Id = Guid.NewGuid(),
                Path = backupedPath,
                CreatedAt = DateTime.UtcNow,
                Label = label,
                FileCount = Directory.GetFiles(backupedPath).Length
            };

            await CleanupOldBackupsAsync().ConfigureAwait(false);

            return info;
        }
        catch (Exception ex)
        {
            throw new SyncException($"Failed to create backup: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Lists all available backups ordered by creation date (newest first).
    /// </summary>
    public List<BackupInfo> GetAvailableBackups()
    {
        var backups = new List<BackupInfo>();

        try
        {
            if (!Directory.Exists(_backupDirectory))
                return backups;

            var directories = Directory.GetDirectories(_backupDirectory);

            foreach (var dir in directories)
            {
                var dirInfo = new DirectoryInfo(dir);
                var createdAt = dirInfo.CreationTimeUtc;
                var fileCount = Directory.GetFiles(dir).Length;

                backups.Add(new BackupInfo
                {
                    Path = dir,
                    CreatedAt = createdAt,
                    FileCount = fileCount,
                    Label = Path.GetFileName(dir)
                });
            }

            return backups.OrderByDescending(b => b.CreatedAt).ToList();
        }
        catch
        {
            return backups;
        }
    }

    /// <summary>
    /// Restores tasks from a specific backup.
    /// </summary>
    public async Task RestoreFromBackupAsync(string backupPath)
    {
        try
        {
            if (!Directory.Exists(backupPath))
                throw new InvalidOperationException($"Backup path not found: {backupPath}");

            var taskFiles = Directory.GetFiles(backupPath, "*.md");
            var targetDir = _fileService.GetBasePath();

            EnsureTargetDirectoryExists(targetDir);

            foreach (var file in taskFiles)
            {
                var fileName = Path.GetFileName(file);
                var targetFile = Path.Combine(targetDir, fileName);

                File.Copy(file, targetFile, overwrite: true);
            }

            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            throw new SyncException($"Failed to restore from backup: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Deletes a specific backup by path.
    /// </summary>
    public async Task DeleteBackupAsync(string backupPath)
    {
        try
        {
            if (Directory.Exists(backupPath))
            {
                Directory.Delete(backupPath, recursive: true);
            }

            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            throw new SyncException($"Failed to delete backup: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Gets backup statistics.
    /// </summary>
    public BackupStats GetBackupStats()
    {
        var stats = new BackupStats();

        try
        {
            if (!Directory.Exists(_backupDirectory))
                return stats;

            var directories = Directory.GetDirectories(_backupDirectory);
            stats.TotalBackups = directories.Length;

            foreach (var dir in directories)
            {
                var dirInfo = new DirectoryInfo(dir);
                stats.TotalSizeBytes += dirInfo.EnumerateFiles("*", SearchOption.AllDirectories)
                    .Sum(f => f.Length);
            }

            var latestBackup = GetAvailableBackups().FirstOrDefault();

            if (latestBackup is not null)
                stats.LastBackupTime = latestBackup.CreatedAt;
        }
        catch
        {
            // Silently ignore stats errors
        }

        return stats;
    }

    /// <summary>
    /// Cleans up old backups based on retention policy.
    /// </summary>
    private async Task CleanupOldBackupsAsync()
    {
        try
        {
            var backups = GetAvailableBackups();

            if (backups.Count > _maxBackupFiles)
            {
                var backupsToDelete = backups.Skip(_maxBackupFiles).ToList();

                foreach (var backup in backupsToDelete)
                {
                    await DeleteBackupAsync(backup.Path).ConfigureAwait(false);
                }
            }
        }
        catch
        {
            // Silently ignore cleanup errors
        }
    }

    /// <summary>
    /// Ensures the backup directory exists.
    /// </summary>
    private void EnsureBackupDirectoryExists()
    {
        if (!Directory.Exists(_backupDirectory))
        {
            Directory.CreateDirectory(_backupDirectory);
        }
    }

    /// <summary>
    /// Ensures the target directory exists for restore operations.
    /// </summary>
    private void EnsureTargetDirectoryExists(string targetDir)
    {
        if (!Directory.Exists(targetDir))
        {
            Directory.CreateDirectory(targetDir);
        }
    }
}

/// <summary>
/// Information about a backup.
/// </summary>
public class BackupInfo
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string? Path { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? Label { get; set; }
    public int FileCount { get; set; }

    public TimeSpan GetAge()
    {
        return DateTime.UtcNow - CreatedAt;
    }

    public override string ToString()
    {
        return $"Backup({Label}, {FileCount} files, {CreatedAt:O})";
    }
}

/// <summary>
/// Statistics about backups.
/// </summary>
public class BackupStats
{
    public int TotalBackups { get; set; }
    public long TotalSizeBytes { get; set; }
    public DateTime? LastBackupTime { get; set; }

    public double GetTotalSizeMB()
    {
        return TotalSizeBytes / (1024.0 * 1024.0);
    }

    public override string ToString()
    {
        return $"BackupStats(Total: {TotalBackups}, Size: {GetTotalSizeMB():F2}MB, LastBackup: {LastBackupTime:O})";
    }
}
