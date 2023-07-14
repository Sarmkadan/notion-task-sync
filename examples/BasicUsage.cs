#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NotionTaskSync.Domain.Models;
using NotionTaskSync.Infrastructure.Configuration;
using NotionTaskSync.Services;

/// <summary>
/// BasicUsage.cs - Minimal setup and first sync call
///
/// This example demonstrates the simplest possible way to use Notion Task Sync.
/// It shows how to:
/// - Set up dependency injection
/// - Create a sync configuration
/// - Execute a sync operation
/// - Handle basic results
///
/// Use this when you want to get started quickly with minimal boilerplate.
/// </summary>
public class BasicUsage
{
    /// <summary>
    /// Minimal example showing the essential steps to perform a sync.
    /// </summary>
    public static async Task RunMinimalExample()
    {
        Console.WriteLine("=== Notion Task Sync - Basic Usage Example ===\n");

        // Step 1: Set up dependency injection container
        // This creates all the services needed for sync operations
        var services = new ServiceCollection();

        // Add logging (console output)
        services.AddLogging(builder => builder.AddConsole());

        // Add application services with default configuration
        // In a real app, you would load from appsettings.json
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string>
            {
                {"NotionApi:ApiKey", "your_notion_api_key_here"},
                {"NotionApi:DatabaseId", "your_database_id_here"},
                {"AppSettings:LocalTasksDirectory", "./tasks"}
            })
            .Build();

        services.AddApplicationServices(configuration);

        // Build the service provider
        var serviceProvider = services.BuildServiceProvider();

        // Step 2: Get the SyncService from DI container
        var syncService = serviceProvider.GetRequiredService<SyncService>();

        // Step 3: Create a sync configuration
        // This tells the sync service which database to sync with and where to store local files
        var config = new SyncConfig(
            name: "MyFirstSync",
            notionDatabaseId: configuration["NotionApi:DatabaseId"] ?? "default-db-id",
            localFolderPath: configuration["AppSettings:LocalTasksDirectory"] ?? "./tasks"
        )
        {
            // Optional: Set sync direction (default is Bidirectional)
            Direction = SyncDirection.Bidirectional,

            // Optional: Set conflict resolution strategy (default is LastWrite)
            ConflictStrategy = ConflictResolutionStrategy.LastWrite
        };

        Console.WriteLine($"Sync Configuration:");
        Console.WriteLine($"  Name: {config.Name}");
        Console.WriteLine($"  Notion Database: {config.NotionDatabaseId}");
        Console.WriteLine($"  Local Folder: {config.LocalFolderPath}");
        Console.WriteLine($"  Direction: {config.Direction}");
        Console.WriteLine($"  Conflict Strategy: {config.ConflictStrategy}");
        Console.WriteLine();

        // Step 4: Execute the sync operation
        Console.WriteLine("Starting sync...");
        var syncResult = await syncService.ExecuteSyncAsync(config);

        // Step 5: Process results
        Console.WriteLine($"\nSync completed successfully!");
        Console.WriteLine($"  Local tasks synced: {syncResult.LocalTaskCount}");
        Console.WriteLine($"  Notion pages synced: {syncResult.NotionPageCount}");
        Console.WriteLine($"  Changes detected (local): {syncResult.LocalChangesDetected}");
        Console.WriteLine($"  Changes detected (Notion): {syncResult.NotionChangesDetected}");
        Console.WriteLine($"  Conflicts detected: {syncResult.ConflictsDetected}");
        Console.WriteLine($"  Conflicts resolved: {syncResult.ConflictsResolved}");
        Console.WriteLine($"  Duration: {syncResult.Duration}ms");

        if (!string.IsNullOrEmpty(syncResult.ErrorMessage))
        {
            Console.WriteLine($"\n⚠️  Warning: {syncResult.ErrorMessage}");
        }

        // Check if sync was successful
        if (syncResult.Status == SyncStatus.Success)
        {
            Console.WriteLine("\n✅ Sync completed successfully!");
        }
        else
        {
            Console.WriteLine($"\n❌ Sync failed with status: {syncResult.Status}");
        }
    }

    /// <summary>
    /// Even simpler example - using default configuration from appsettings.json
    /// This is what most users will do in production.
    /// </summary>
    public static async Task RunWithDefaultConfig()
    {
        Console.WriteLine("=== Basic Usage with Default Configuration ===\n");

        // Step 1: Load configuration from appsettings.json
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false)
            .AddEnvironmentVariables()
            .Build();

        // Step 2: Set up services (same as above)
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddConsole());
        services.AddApplicationServices(configuration);
        var serviceProvider = services.BuildServiceProvider();

        // Step 3: Get sync service
        var syncService = serviceProvider.GetRequiredService<SyncService>();

        // Step 4: Create config using values from appsettings.json
        var config = new SyncConfig(
            name: "DefaultSync",
            notionDatabaseId: configuration["NotionApi:DatabaseId"] ?? "default-db-id",
            localFolderPath: configuration["AppSettings:LocalTasksDirectory"] ?? "./tasks"
        );

        // Step 5: Execute sync
        Console.WriteLine("Starting sync with default configuration...");
        var result = await syncService.ExecuteSyncAsync(config);

        Console.WriteLine($"\n✅ Synced {result.LocalTaskCount} local tasks and {result.NotionPageCount} Notion pages");
    }
}

/// <summary>
/// Usage demonstration
/// </summary>
public class BasicUsageDemo
{
    public static async Task Main()
    {
        try
        {
            // Run the minimal example
            await BasicUsage.RunMinimalExample();

            Console.WriteLine("\n" + new string('=', 60));

            // Run the default config example
            await BasicUsage.RunWithDefaultConfig();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\n❌ Error: {ex.Message}");
            Console.WriteLine($"Stack Trace: {ex.StackTrace}");
            return;
        }
    }
}