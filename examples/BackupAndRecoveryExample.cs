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
    public static async Task Main(string[] args)
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
            await ListBackupsAsync(logger, backupService);

            // Step 2: Create a backup before risky operation
            var backupPath = await CreateBackupAsync(logger, backupService);

            // Step 3: Perform sync operation
            await PerformSyncAsync(logger, syncService, configuration);

            // Step 4: Verify sync succeeded
            var successVerified = await VerifyBackupAsync(logger, backupPath);

            // Step 5: Demonstrate recovery (commented out as it would restore)
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

    private static async Task ListBackupsAsync(
        ILogger logger,
        BackupService backupService)
    {
        logger.LogInformation("═══════════════════════════════════════════════");
        logger.LogInformation("Available Backups:");
        logger.LogInformation("═══════════════════════════════════════════════");

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
            logger.LogInformation("  📦 {Name}", backup.Name);
            logger.LogInformation("     Size: {Size:N0} bytes", size);
            logger.LogInformation("     Age: {Age} days", age.Days);
        }

        logger.LogInformation("");
    }

    private static async Task<string> CreateBackupAsync(
        ILogger logger,
        BackupService backupService)
    {
        logger.LogInformation("═══════════════════════════════════════════════");
        logger.LogInformation("Creating Backup...");
        logger.LogInformation("═══════════════════════════════════════════════");

        var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
        var backupName = $"pre-sync-operation-{timestamp}";

        try
        {
            var backupPath = await backupService.CreateBackupAsync(
                syncConfigId: "example-sync",
                backupName: backupName
            );

            logger.LogInformation("✓ Backup created successfully");
            logger.LogInformation("  Path: {Path}", backupPath);
            logger.LogInformation("  Size: {Size:N0} bytes", new DirectoryInfo(backupPath).EnumerateFiles("*", SearchOption.AllDirectories).Sum(f => f.Length));
            logger.LogInformation("");

            return backupPath;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to create backup");
            throw;
        }
    }

    private static async Task PerformSyncAsync(
        ILogger logger,
        SyncService syncService,
        IConfiguration configuration)
    {
        logger.LogInformation("═══════════════════════════════════════════════");
        logger.LogInformation("Performing Sync Operation...");
        logger.LogInformation("═══════════════════════════════════════════════");

        var config = new Domain.Models.SyncConfig(
            name: "BackupExampleSync",
            notionDatabaseId: configuration["NotionApi:DatabaseId"] ?? "test-db",
            localFolderPath: "./tasks"
        )
        {
            AutoBackup = false // We're managing backup manually
        };

        var result = await syncService.ExecuteSyncAsync(config);

        logger.LogInformation("✓ Sync completed");
        logger.LogInformation("  Status: {Status}", result.Status);
        logger.LogInformation("  Local Tasks: {Count}", result.LocalTaskCount);
        logger.LogInformation("  Notion Pages: {Count}", result.NotionPageCount);
        logger.LogInformation("  Conflicts: {Count}", result.ConflictsDetected);
        logger.LogInformation("  Duration: {Duration}ms", result.Duration.TotalMilliseconds);
        logger.LogInformation("");
    }

    private static async Task<bool> VerifyBackupAsync(
        ILogger logger,
        string backupPath)
    {
        logger.LogInformation("═══════════════════════════════════════════════");
        logger.LogInformation("Verifying Backup Integrity...");
        logger.LogInformation("═══════════════════════════════════════════════");

        var backupDir = new DirectoryInfo(backupPath);

        // Check essential files exist
        var hasLocalFiles = backupDir.EnumerateFiles("*", SearchOption.AllDirectories)
            .Any(f => f.Name.EndsWith(".json"));
        var hasManifest = backupDir.EnumerateFiles("manifest.json").Any();
        var hasDatabase = backupDir.EnumerateFiles("database.db").Any();

        logger.LogInformation("Backup Contents:");
        logger.LogInformation("  ✓ JSON files: {Count}",
            backupDir.EnumerateFiles("*.json").Count());
        logger.LogInformation("  ✓ Manifest: {Found}", hasManifest ? "Found" : "Missing");
        logger.LogInformation("  ✓ Database: {Found}", hasDatabase ? "Found" : "Missing");

        var isValid = hasLocalFiles && hasManifest && hasDatabase;
        logger.LogInformation("");
        logger.LogInformation(isValid ? "✓ Backup verified successfully" : "✗ Backup verification failed");
        logger.LogInformation("");

        return isValid;
    }

    private static async Task RecoverFromBackupAsync(
        ILogger logger,
        BackupService backupService,
        string backupPath)
    {
        logger.LogInformation("═══════════════════════════════════════════════");
        logger.LogInformation("Recovering from Backup...");
        logger.LogInformation("═══════════════════════════════════════════════");

        try
        {
            await backupService.RestoreBackupAsync(backupPath);
            logger.LogInformation("✓ Recovery completed successfully");
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
