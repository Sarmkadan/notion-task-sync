#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NotionTaskSync.Infrastructure.Configuration;
using NotionTaskSync.Services;

namespace NotionTaskSync.Examples;

/// <summary>
/// Example demonstrating backup creation and recovery procedures.
/// Shows how to safeguard against data loss during sync operations.
/// </summary>
public class BackupAndRecoveryExample
{
    public static async global::System.Threading.Tasks.Task Main(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false)
            .AddEnvironmentVariables()
            .Build();

        var services = new ServiceCollection();
        services.AddSingleton(configuration);
        services.AddLogging(builder => builder.AddConsole());
        services.AddHttpClient();
        services.AddApplicationServices(configuration);

        var serviceProvider = services.BuildServiceProvider();
        var logger = serviceProvider.GetRequiredService<ILogger<BackupAndRecoveryExample>>();
        var backupService = serviceProvider.GetRequiredService<BackupService>();
        var syncService = serviceProvider.GetRequiredService<SyncService>();

        try
        {
            // Step 1: List existing backups
            await ListBackupsAsync(logger, backupService).ConfigureAwait(false);

            // Step 2: Create a backup before risky operation
            var backupPath = await CreateBackupAsync(logger, backupService).ConfigureAwait(false);

            // Step 3: Perform sync operation
            await PerformSyncAsync(logger, syncService, configuration).ConfigureAwait(false);

            // Step 4: Verify sync succeeded
            var successVerified = await VerifyBackupAsync(logger, backupPath).ConfigureAwait(false);

            if (!successVerified)
            {
                logger.LogWarning("Sync verification failed, would restore from backup");
                // await RecoverFromBackupAsync(logger, backupService, backupPath);
            }
            else
            {
                logger.LogInformation("Sync successful, backup available for safety");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Backup and recovery example failed");
            Environment.Exit(1);
        }
    }

    private static async global::System.Threading.Tasks.Task ListBackupsAsync(
        ILogger logger,
        BackupService backupService)
    {
        logger.LogInformation("==============================================");
        logger.LogInformation("Available Backups:");
        logger.LogInformation("==============================================");

        var backupDir = new DirectoryInfo("./backups");
        if (!backupDir.Exists)
        {
            logger.LogInformation("No backups directory found");
            return;
        }

        var backups = backupDir.GetDirectories()
            .OrderByDescending(d => d.CreationTime)
            .Take(10);

        if (!backups.Any())
        {
            logger.LogInformation("No backups found");
            return;
        }

        foreach (var backup in backups)
        {
            var size = GetDirectorySize(backup);
            var age = DateTime.Now - backup.CreationTime;
            logger.LogInformation("  {Name}", backup.Name);
            logger.LogInformation("     Size: {Size:N0} bytes", size);
            logger.LogInformation("     Age: {Age} days", age.Days);
        }

        logger.LogInformation("");
    }

    private static async global::System.Threading.Tasks.Task<string> CreateBackupAsync(
        ILogger logger,
        BackupService backupService)
    {
        logger.LogInformation("==============================================");
        logger.LogInformation("Creating Backup...");
        logger.LogInformation("==============================================");

        var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
        var backupName = $"pre-sync-operation-{timestamp}";

        try
        {
            var backupInfo = await backupService.CreateBackupAsync(backupName).ConfigureAwait(false);

            logger.LogInformation("Backup created successfully");
            logger.LogInformation("  Path: {Path}", backupInfo.Path);
            logger.LogInformation("");

            return backupInfo.Path ?? string.Empty;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to create backup");
            throw;
        }
    }

    private static async global::System.Threading.Tasks.Task PerformSyncAsync(
        ILogger logger,
        SyncService syncService,
        IConfiguration configuration)
    {
        logger.LogInformation("==============================================");
        logger.LogInformation("Performing Sync Operation...");
        logger.LogInformation("==============================================");

        var config = new Domain.Models.SyncConfig(
            name: "BackupExampleSync",
            notionDatabaseId: configuration["NotionApi:DatabaseId"] ?? "test-db-id-backup-0",
            localFolderPath: "./tasks"
        );

        var result = await syncService.ExecuteSyncAsync(config).ConfigureAwait(false);

        logger.LogInformation("Sync completed");
        logger.LogInformation("  Status: {Status}", result.Status);
        logger.LogInformation("  Local Tasks: {Count}", result.LocalTaskCount);
        logger.LogInformation("  Notion Pages: {Count}", result.NotionPageCount);
        logger.LogInformation("  Conflicts: {Count}", result.ConflictsDetected);
        logger.LogInformation("  Duration: {Duration}", result.Duration);
        logger.LogInformation("");
    }

    private static async global::System.Threading.Tasks.Task<bool> VerifyBackupAsync(
        ILogger logger,
        string backupPath)
    {
        logger.LogInformation("==============================================");
        logger.LogInformation("Verifying Backup Integrity...");
        logger.LogInformation("==============================================");

        if (string.IsNullOrEmpty(backupPath) || !Directory.Exists(backupPath))
        {
            logger.LogWarning("Backup path not found: {Path}", backupPath);
            return false;
        }

        var backupDir = new DirectoryInfo(backupPath);

        var hasLocalFiles = backupDir.EnumerateFiles("*", SearchOption.AllDirectories).Any();
        var hasManifest = backupDir.EnumerateFiles("manifest.json").Any();

        logger.LogInformation("Backup Contents:");
        logger.LogInformation("  Files: {Found}", hasLocalFiles ? "Found" : "Missing");
        logger.LogInformation("  Manifest: {Found}", hasManifest ? "Found" : "Missing");

        var isValid = hasLocalFiles;
        logger.LogInformation("");
        logger.LogInformation(isValid ? "Backup verified successfully" : "Backup verification failed");
        logger.LogInformation("");

        return isValid;
    }

    private static async global::System.Threading.Tasks.Task RecoverFromBackupAsync(
        ILogger logger,
        BackupService backupService,
        string backupPath)
    {
        logger.LogInformation("==============================================");
        logger.LogInformation("Recovering from Backup...");
        logger.LogInformation("==============================================");

        try
        {
            await backupService.RestoreFromBackupAsync(backupPath).ConfigureAwait(false);
            logger.LogInformation("Recovery completed successfully");
            logger.LogInformation("  Restored from: {Path}", backupPath);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Recovery failed");
            throw;
        }
    }

    private static long GetDirectorySize(DirectoryInfo directory)
    {
        try
        {
            return directory.EnumerateFiles("*", SearchOption.AllDirectories)
                .Sum(f => f.Length);
        }
        catch
        {
            return 0;
        }
    }
}
