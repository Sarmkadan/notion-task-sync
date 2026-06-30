#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NotionTaskSync.Domain.Models;
using NotionTaskSync.Infrastructure.Configuration;
using NotionTaskSync.Services;
using NotionTaskSync.Data.Repositories;
using NotionTaskSync.Domain.Enums;

/// <summary>
/// AdvancedUsage.cs - Configuration, custom options, and error handling
///
/// This example demonstrates advanced usage patterns including:
/// - Custom configuration with field mappings
/// - Per-field conflict resolution strategies
/// - Error handling and retry logic
/// - Monitoring and logging
/// - Integration with external services
///
/// Use this when you need fine-grained control over sync behavior.
/// </summary>
public class AdvancedUsage
{
    private readonly ILogger<AdvancedUsage> _logger;

    public AdvancedUsage(ILogger<AdvancedUsage> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Creates a fully configured sync setup with custom options.
    /// </summary>
    public static async Task RunAdvancedConfigurationExample()
    {
        Console.WriteLine("=== Notion Task Sync - Advanced Configuration Example ===\n");

        // Step 1: Create advanced configuration
        var config = CreateAdvancedConfiguration();

        // Step 2: Set up services with custom logging
        var (serviceProvider, syncService) = SetupServicesWithCustomLogging();

        // Step 3: Execute sync with error handling
        var result = await ExecuteSyncWithErrorHandling(syncService, config);

        // Step 4: Process advanced results
        DisplayAdvancedResults(result);
    }

    /// <summary>
    /// Creates a sync configuration with advanced options.
    /// </summary>
    private static SyncConfig CreateAdvancedConfiguration()
    {
        Console.WriteLine("Creating advanced sync configuration...\n");

        var config = new SyncConfig(
            name: "TeamProjectSync",
            notionDatabaseId: "your_team_database_id_here",
            localFolderPath: "./team-tasks"
        )
        {
            // Sync direction - only sync from Notion to local for this example
            Direction = SyncDirection.NotionToLocal,

            // Conflict resolution strategy
            ConflictStrategy = ConflictResolutionStrategy.LocalWins,

            // Sync interval in seconds (5 minutes)
            SyncIntervalSeconds = 300,

            // Enable automatic backups
            IsEnabled = true
        };

        // Field mappings - map local field names to Notion property names
        // This allows you to use different field names in your local files vs Notion
        config.FieldMappings = new Dictionary<string, string>
        {
            { "title", "Title" },
            { "status", "Status" },
            { "priority", "Priority" },
            { "dueDate", "Due Date" },
            { "assignee", "Assignee" },
            { "description", "Description" },
            { "tags", "Tags" }
        };

        // Ignored fields - exclude these from sync
        config.IgnoredFields = new List<string> { "internalId", "createdBy" };

        // Per-field conflict resolution overrides
        // For example, always prefer local changes for description and notes
        // Always prefer Notion changes for status and priority
        config.FieldConflictStrategies = new Dictionary<string, ConflictResolutionStrategy>
        {
            { "description", ConflictResolutionStrategy.LocalWins },
            { "notes", ConflictResolutionStrategy.LocalWins },
            { "status", ConflictResolutionStrategy.NotionWins },
            { "priority", ConflictResolutionStrategy.NotionWins }
        };

        Console.WriteLine("Configuration created:");
        Console.WriteLine($"  Name: {config.Name}");
        Console.WriteLine($"  Direction: {config.Direction}");
        Console.WriteLine($"  Conflict Strategy: {config.ConflictStrategy}");
        Console.WriteLine($"  Field Mappings: {config.FieldMappings?.Count ?? 0} mappings");
        Console.WriteLine($"  Ignored Fields: {config.IgnoredFields?.Count ?? 0} fields");
        Console.WriteLine($"  Per-Field Strategies: {config.FieldConflictStrategies?.Count ?? 0} overrides");
        Console.WriteLine();

        return config;
    }

    /// <summary>
    /// Sets up services with custom logging configuration.
    /// </summary>
    private static (IServiceProvider, SyncService) SetupServicesWithCustomLogging()
    {
        Console.WriteLine("Setting up services with custom logging...\n");

        var services = new ServiceCollection();

        // Configure logging with custom settings
        services.AddLogging(builder =>
        {
            builder.ClearProviders(); // Clear default providers
            builder.AddConsole();
            builder.AddDebug();
            builder.SetMinimumLevel(LogLevel.Debug); // Capture all log levels
        });

        // Add application services
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string>
            {
                {"NotionApi:ApiKey", "your_api_key"},
                {"NotionApi:DatabaseId", "your_db_id"},
                {"NotionApi:RateLimitPerSecond", "3"},
                {"AppSettings:LocalTasksDirectory", "./team-tasks"},
                {"AppSettings:BackupDirectory", "./backups"},
                {"SyncConfig:ConflictResolutionStrategy", "local-wins"}
            })
            .Build();

        services.AddApplicationServices(configuration);

        // You can also configure specific services programmatically
        // services.Configure<NotionApiSettings>(configuration.GetSection("NotionApi"));

        var serviceProvider = services.BuildServiceProvider();

        var syncService = serviceProvider.GetRequiredService<SyncService>();

        Console.WriteLine("Services configured with:");
        Console.WriteLine("  - Console logging");
        Console.WriteLine("  - Debug logging");
        Console.WriteLine("  - Debug log level");
        Console.WriteLine();

        return (serviceProvider, syncService);
    }

    /// <summary>
    /// Executes sync with comprehensive error handling and retry logic.
    /// </summary>
    private static async Task<SyncService.SyncResult> ExecuteSyncWithErrorHandling(
        SyncService syncService,
        SyncConfig config)
    {
        Console.WriteLine("Executing sync with error handling...\n");

        SyncService.SyncResult result;

        try
        {
            // Execute the sync operation
            result = await syncService.ExecuteSyncAsync(config);

            // Log success
            Console.WriteLine("✅ Sync completed successfully!");
        }
        catch (ConfigurationException ex)
        {
            Console.WriteLine($"❌ Configuration error: {ex.Message}");
            Console.WriteLine("Please check your configuration and try again.");
            throw;
        }
        catch (NotionApiException ex) when (ex.StatusCode == 429)
        {
            Console.WriteLine($"⚠️  Rate limited! Status: {ex.StatusCode}");
            Console.WriteLine("Consider:");
            Console.WriteLine("  - Increasing SyncIntervalSeconds");
            Console.WriteLine("  - Reducing RateLimitPerSecond");
            Console.WriteLine("  - Enabling caching");
            throw;
        }
        catch (NotionApiException ex)
        {
            Console.WriteLine($"❌ Notion API error: {ex.Message}");
            Console.WriteLine($"Status Code: {ex.StatusCode}");
            Console.WriteLine("Please check your API key and database configuration.");
            throw;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Unexpected error: {ex.Message}");
            Console.WriteLine($"Type: {ex.GetType().Name}");
            throw;
        }

        return result;
    }

    /// <summary>
    /// Displays advanced results with additional metrics and analysis.
    /// </summary>
    private static void DisplayAdvancedResults(SyncService.SyncResult result)
    {
        Console.WriteLine("\n=== Sync Results ===");
        Console.WriteLine($"Status: {result.Status}");
        Console.WriteLine($"Duration: {result.Duration}ms");
        Console.WriteLine($"Timestamp: {result.CompletedAt:yyyy-MM-dd HH:mm:ss}");
        Console.WriteLine();

        Console.WriteLine("=== Task Counts ===");
        Console.WriteLine($"Local tasks: {result.LocalTaskCount}");
        Console.WriteLine($"Notion pages: {result.NotionPageCount}");
        Console.WriteLine($"Synced tasks: {result.SyncedCount}");
        Console.WriteLine();

        Console.WriteLine("=== Changes Detected ===");
        Console.WriteLine($"Local changes: {result.LocalChangesDetected}");
        Console.WriteLine($"Notion changes: {result.NotionChangesDetected}");
        Console.WriteLine();

        Console.WriteLine("=== Conflicts ===");
        Console.WriteLine($"Conflicts detected: {result.ConflictsDetected}");
        Console.WriteLine($"Conflicts resolved: {result.ConflictsResolved}");
        Console.WriteLine($"Conflicts pending: {result.ConflictsPendingReview}");
        Console.WriteLine();

        if (result.ChangedTasks?.Count > 0)
        {
            Console.WriteLine("=== Changed Tasks ===");
            foreach (var task in result.ChangedTasks.Take(5))
            {
                Console.WriteLine($"  - {task.Title} ({task.Status})");
            }
            if (result.ChangedTasks.Count > 5)
            {
                Console.WriteLine($"  ... and {result.ChangedTasks.Count - 5} more");
            }
            Console.WriteLine();
        }

        if (!string.IsNullOrEmpty(result.ErrorMessage))
        {
            Console.WriteLine("=== Errors ===");
            Console.WriteLine(result.ErrorMessage);
            Console.WriteLine();
        }

        // Calculate sync efficiency
        if (result.LocalTaskCount > 0 && result.Duration > 0)
        {
            var tasksPerSecond = result.LocalTaskCount / (result.Duration / 1000.0);
            Console.WriteLine($"📊 Sync efficiency: {tasksPerSecond:F2} tasks/second");
        }
    }

    /// <summary>
    /// Example: Using configuration from IOptions pattern
    /// This is useful when you want to inject configuration directly.
    /// </summary>
    public static async Task RunWithOptionsPattern()
    {
        Console.WriteLine("=== Using IOptions Pattern ===\n");

        var services = new ServiceCollection();

        // Configure services with strongly-typed configuration
        services.Configure<NotionApiSettings>(options =>
        {
            options.ApiKey = "your_api_key_from_options";
            options.DatabaseId = "your_db_id_from_options";
            options.RateLimitPerSecond = 3;
        });

        services.Configure<AppSettings>(options =>
        {
            options.LocalTasksDirectory = "./options-tasks";
            options.BackupDirectory = "./options-backups";
        });

        services.AddLogging(builder => builder.AddConsole());
        services.AddApplicationServices(new ConfigurationBuilder().Build());

        var serviceProvider = services.BuildServiceProvider();

        // Resolve IOptions directly
        var notionApiOptions = serviceProvider.GetRequiredService<IOptions<NotionApiSettings>>();
        var appSettings = serviceProvider.GetRequiredService<IOptions<AppSettings>>();

        Console.WriteLine("Configuration from IOptions:");
        Console.WriteLine($"  API Key: {notionApiOptions.Value.ApiKey?.Substring(0, 5)}...");
        Console.WriteLine($"  Database: {notionApiOptions.Value.DatabaseId}");
        Console.WriteLine($"  Local Dir: {appSettings.Value.LocalTasksDirectory}");
        Console.WriteLine();

        var syncService = serviceProvider.GetRequiredService<SyncService>();
        var config = new SyncConfig(
            "OptionsPatternSync",
            notionApiOptions.Value.DatabaseId,
            appSettings.Value.LocalTasksDirectory
        );

        var result = await syncService.ExecuteSyncAsync(config);
        Console.WriteLine($"✅ Sync completed: {result.LocalTaskCount} tasks\n");
    }

    /// <summary>
    /// Example: Conditional sync based on changes
    /// Only sync if changes are detected.
    /// </summary>
    public static async Task RunConditionalSyncExample()
    {
        Console.WriteLine("=== Conditional Sync Example ===\n");

        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddConsole());
        services.AddApplicationServices(new ConfigurationBuilder().Build());
        var serviceProvider = services.BuildServiceProvider();

        var syncService = serviceProvider.GetRequiredService<SyncService>();
        var changeDetection = serviceProvider.GetRequiredService<ChangeDetectionService>();

        var config = new SyncConfig(
            "ConditionalSync",
            "test-db-id",
            "./conditional-tasks"
        );

        // Load current state
        var localTasks = await changeDetection.LoadLocalTasksAsync(config.LocalFolderPath);
        var notionPages = await changeDetection.LoadNotionPagesAsync(config.NotionDatabaseId);

        // Detect changes
        var localChanges = changeDetection.DetectLocalChanges(localTasks, DateTime.UtcNow.AddDays(-1));
        var notionChanges = changeDetection.DetectNotionChanges(notionPages, DateTime.UtcNow.AddDays(-1));

        Console.WriteLine($"Changes detected: {localChanges.Count} local, {notionChanges.Count} Notion");

        // Only sync if changes detected
        if (localChanges.Count > 0 || notionChanges.Count > 0)
        {
            Console.WriteLine("Changes detected - executing sync...");
            var result = await syncService.ExecuteSyncAsync(config);
            Console.WriteLine($"✅ Sync completed: {result.SyncedCount} tasks\n");
        }
        else
        {
            Console.WriteLine("No changes detected - skipping sync\n");
        }
    }
}

/// <summary>
/// Usage demonstration
/// </summary>
public class AdvancedUsageDemo
{
    public static async Task Main()
    {
        try
        {
            // Run advanced configuration example
            await AdvancedUsage.RunAdvancedConfigurationExample();

            Console.WriteLine("\n" + new string('=', 60));

            // Run IOptions pattern example
            await AdvancedUsage.RunWithOptionsPattern();

            Console.WriteLine("\n" + new string('=', 60));

            // Run conditional sync example
            await AdvancedUsage.RunConditionalSyncExample();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\n❌ Demo failed: {ex.Message}");
            return;
        }
    }
}