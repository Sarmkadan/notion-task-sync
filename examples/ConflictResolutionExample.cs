#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NotionTaskSync.Domain.Models;
using NotionTaskSync.Infrastructure.Configuration;
using NotionTaskSync.Services;

namespace NotionTaskSync.Examples;

/// <summary>
/// Example demonstrating custom conflict resolution strategies.
/// Shows how to handle conflicts when changes occur in both locations simultaneously.
/// </summary>
public class ConflictResolutionExample
{
    public static async global::System.Threading.Tasks.Task Main(string[] args)
    {
        var configuration = BuildConfiguration();
        var services = SetupDependencyInjection(configuration);
        var logger = services.GetRequiredService<ILogger<ConflictResolutionExample>>();
        var syncService = services.GetRequiredService<SyncService>();

        try
        {
            // Example 1: Latest-wins strategy (automatic, no prompts)
            await RunSyncWithStrategy(logger, syncService, ConflictResolutionStrategy.LastWrite,
                "Uses timestamp to determine winner automatically");

            // Example 2: Local priority strategy
            await RunSyncWithStrategy(logger, syncService, ConflictResolutionStrategy.LocalWins,
                "Prefers local file changes over Notion changes");

            // Example 3: Notion priority strategy
            await RunSyncWithStrategy(logger, syncService, ConflictResolutionStrategy.NotionWins,
                "Prefers Notion database changes over local files");

            // Example 4: Manual resolution strategy
            await RunSyncWithStrategy(logger, syncService, ConflictResolutionStrategy.Manual,
                "Marks conflicts for manual review");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Example failed");
            Environment.Exit(1);
        }
    }

    private static async global::System.Threading.Tasks.Task RunSyncWithStrategy(
        ILogger logger,
        SyncService syncService,
        ConflictResolutionStrategy strategy,
        string description)
    {
        logger.LogInformation("==============================================");
        logger.LogInformation("Testing Strategy: {Strategy}", strategy);
        logger.LogInformation("Description: {Description}", description);
        logger.LogInformation("==============================================");

        var config = new SyncConfig(
            name: $"ConflictTest-{strategy}",
            notionDatabaseId: "test-db-id-conflict-0",
            localFolderPath: "./tasks"
        );
        config.ConflictStrategy = strategy;

        var result = await syncService.ExecuteSyncAsync(config);

        logger.LogInformation("Results:");
        logger.LogInformation("  Status: {Status}", result.Status);
        logger.LogInformation("  Conflicts Detected: {Detected}", result.ConflictsDetected);
        logger.LogInformation("  Conflicts Resolved: {Resolved}", result.ConflictsResolved);
        logger.LogInformation("  Duration: {Duration}", result.Duration);
        logger.LogInformation("");
    }

    private static async global::System.Threading.Tasks.Task RunCustomFieldStrategy(
        ILogger logger,
        SyncService syncService)
    {
        logger.LogInformation("==============================================");
        logger.LogInformation("Testing Manual Field-Based Strategy");
        logger.LogInformation("==============================================");

        var config = new SyncConfig(
            name: "CustomFieldStrategy",
            notionDatabaseId: "test-db-id-custom-0",
            localFolderPath: "./tasks"
        );
        config.ConflictStrategy = ConflictResolutionStrategy.Manual;

        var result = await syncService.ExecuteSyncAsync(config);

        logger.LogInformation("Results:");
        logger.LogInformation("  Status: {Status}", result.Status);
        logger.LogInformation("  Conflicts Resolved: {Count}", result.ConflictsResolved);
        logger.LogInformation("");
    }

    private static IServiceProvider SetupDependencyInjection(IConfiguration configuration)
    {
        var services = new ServiceCollection();
        services.AddSingleton(configuration);
        services.AddLogging(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Information);
        });
        services.AddHttpClient();
        services.AddApplicationServices(configuration);

        return services.BuildServiceProvider();
    }

    private static IConfiguration BuildConfiguration()
    {
        return new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false)
            .AddEnvironmentVariables()
            .Build();
    }
}
