// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NotionTaskSync.Infrastructure.Configuration;
using NotionTaskSync.Services;

namespace NotionTaskSync;

/// <summary>
/// Main entry point for the Notion Task Sync application.
/// Initializes configuration, sets up dependencies, and orchestrates sync operations.
/// </summary>
public class Program
{
    /// <summary>
    /// Application entry point. Sets up DI container and starts sync service.
    /// </summary>
    public static async Task Main(string[] args)
    {
        try
        {
            // Load configuration
            var configuration = BuildConfiguration();

            // Validate configuration before proceeding
            DependencyInjection.ValidateConfiguration(configuration);

            // Build service provider with all dependencies
            var services = new ServiceCollection();
            services.AddSingleton(configuration);
            services.AddLogging(builder =>
            {
                builder.AddConsole();
                builder.SetMinimumLevel(LogLevel.Information);
            });

            services.AddHttpClient();
            services.AddApplicationServices(configuration);

            var serviceProvider = services.BuildServiceProvider();

            // Get logger for the application
            var logger = serviceProvider.GetRequiredService<ILogger<Program>>();

            logger.LogInformation("Notion Task Sync Application Starting");
            logger.LogInformation("Version: {Version}", "1.0.0");
            logger.LogInformation("Environment: {Environment}", "Development");

            // Get the sync service and execute sync
            var syncService = serviceProvider.GetRequiredService<SyncService>();

            // Create or load sync configuration
            var syncConfig = new Domain.Models.SyncConfig(
                name: "Default Sync",
                notionDatabaseId: configuration["NotionApi:DatabaseId"] ?? "default-db-id",
                localFolderPath: configuration["AppSettings:LocalTasksDirectory"] ?? "./tasks"
            );

            if (!syncConfig.Validate())
            {
                logger.LogError("Invalid sync configuration. Please check appsettings.json");
                return;
            }

            // Execute the sync
            logger.LogInformation("Starting sync from Notion to local tasks...");
            var result = await syncService.ExecuteSyncAsync(syncConfig);

            // Log results
            LogSyncResults(logger, result);

            logger.LogInformation("Notion Task Sync Application Completed Successfully");
        }
        catch (ConfigurationException ex)
        {
            Console.WriteLine($"Configuration Error: {ex.Message}");
            Environment.Exit(1);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Fatal Error: {ex.Message}");
            Console.WriteLine($"Stack Trace: {ex.StackTrace}");
            Environment.Exit(1);
        }
    }

    /// <summary>
    /// Builds the configuration from appsettings.json and environment variables.
    /// </summary>
    private static IConfiguration BuildConfiguration()
    {
        var configBuilder = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddJsonFile("appsettings.local.json", optional: true, reloadOnChange: true)
            .AddEnvironmentVariables();

        return configBuilder.Build();
    }

    /// <summary>
    /// Logs the results of a sync operation.
    /// </summary>
    private static void LogSyncResults(ILogger<Program> logger, SyncService.SyncResult result)
    {
        logger.LogInformation("Sync Results:");
        logger.LogInformation("- Status: {Status}", result.Status);
        logger.LogInformation("- Local Tasks: {LocalTaskCount}", result.LocalTaskCount);
        logger.LogInformation("- Notion Pages: {NotionPageCount}", result.NotionPageCount);
        logger.LogInformation("- Local Changes: {LocalChanges}", result.LocalChangesDetected);
        logger.LogInformation("- Notion Changes: {NotionChanges}", result.NotionChangesDetected);
        logger.LogInformation("- Conflicts Detected: {ConflictCount}", result.ConflictsDetected);
        logger.LogInformation("- Conflicts Resolved: {ResolvedCount}", result.ConflictsResolved);
        logger.LogInformation("- Duration: {Duration}", result.Duration);

        if (!string.IsNullOrEmpty(result.ErrorMessage))
        {
            logger.LogError("- Error: {ErrorMessage}", result.ErrorMessage);
        }
    }
}
