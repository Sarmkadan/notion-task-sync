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
/// Basic example demonstrating a simple sync operation with default configuration.
/// This is the minimal setup needed to get Notion Task Sync working.
/// </summary>
public class BasicSyncExample
{
    public static async Task Main(string[] args)
    {
        // Build configuration from appsettings.json
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false)
            .AddEnvironmentVariables()
            .Build();

        // Setup dependency injection
        var services = new ServiceCollection();
        services.AddSingleton(configuration);
        services.AddLogging(builder => builder.AddConsole());
        services.AddHttpClient();
        services.AddApplicationServices(configuration);

        var serviceProvider = services.BuildServiceProvider();
        var logger = serviceProvider.GetRequiredService<ILogger<BasicSyncExample>>();
        var syncService = serviceProvider.GetRequiredService<SyncService>();

        try
        {
            logger.LogInformation("Starting basic sync example...");

            // Create minimal sync configuration
            var syncConfig = new Domain.Models.SyncConfig(
                name: "BasicSync",
                notionDatabaseId: configuration["NotionApi:DatabaseId"] ?? throw new InvalidOperationException("DatabaseId not configured"),
                localFolderPath: "./tasks"
            );

            // Execute sync
            logger.LogInformation("Executing sync operation...");
            var result = await syncService.ExecuteSyncAsync(syncConfig);

            // Display results
            logger.LogInformation("Sync completed successfully!");
            logger.LogInformation("- Local tasks: {Count}", result.LocalTaskCount);
            logger.LogInformation("- Notion pages: {Count}", result.NotionPageCount);
            logger.LogInformation("- Status: {Status}", result.Status);
            logger.LogInformation("- Duration: {Duration}", result.Duration);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Sync failed");
            Environment.Exit(1);
        }
    }
}
