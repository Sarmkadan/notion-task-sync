## AdvancedUsageExtensions

The `AdvancedUsageExtensions` class provides advanced utilities for validating, optimizing, and analyzing task synchronization workflows. It includes methods to validate configuration, optimize sync settings, execute syncs with retry logic, and analyze performance metrics.

### Usage Example

```csharp
using Domain.Models;
using SyncService;

class Program
{
    static async Task Main()
    {
        // Validate and optimize sync configuration
        var config = new SyncConfig();
        var validationReport = AdvancedUsageExtensions.ValidateConfiguration(config);

        if (validationReport.IsValid)
        {
            var optimizedConfig = AdvancedUsageExtensions.CreateOptimizedConfiguration(config);

            // Execute sync with retry logic
            var result = await AdvancedUsageExtensions.ExecuteWithRetryAsync(optimizedConfig);

            // Analyze results
            var analysis = AdvancedUsageExtensions.AnalyzeResults(result);

            // Output key metrics
            Console.WriteLine($"Total Tasks: {analysis.TotalTasks}");
            Console.WriteLine($"Synced Tasks: {analysis.SyncedTasks}");
            Console.WriteLine($"Conflicts: {analysis.Conflicts}");
            Console.WriteLine($"Success Rate: {analysis.SuccessRate:P}");
            Console.WriteLine($"Efficiency: {analysis.EfficiencyRating}");
        }
        else
        {
            Console.WriteLine("Configuration issues found:");
            foreach (var issue in validationReport.Issues)
            {
                Console.WriteLine($"- {issue}");
            }
        }
    }
}
```

## RateLimitingMiddlewareExtensions

The `RateLimitingMiddlewareExtensions` class provides utilities for handling rate limiting in task synchronization workflows. It includes methods to execute actions with retry logic, check if the rate limit has been exceeded, and get the time until the rate limit resets. 

Here's an example of how to use `RateLimitingMiddlewareExtensions` to execute an action with retry logic:
```csharp
var result = await RateLimitingMiddlewareExtensions.ExecuteWithRetryAsync<string>(() => 
{
    // Code to execute with retry logic
    return "Success";
});
Console.WriteLine(result);
```

## SyncServiceExtensions

The `SyncServiceExtensions` class provides extension methods for analyzing and processing synchronization results. It includes utilities to check success status, extract metrics, filter results, and generate summaries from sync operations.

### Usage Example

```csharp
using Services;

class Program
{
    static void Main()
    {
        // Assume we have a collection of sync results
        var results = GetSyncResults(); // IEnumerable<SyncService.SyncResult>

        // Analyze individual result
        var latestResult = results.GetMostRecent();
        if (latestResult.IsSuccessful)
        {
            Console.WriteLine($"Success! Changes: {latestResult.GetTotalChangesDetected()}");
            Console.WriteLine($"Duration: {latestResult.GetDuration()?.TotalMinutes:F1} minutes");
        }
        else
        {
            Console.WriteLine($"Failed: {latestResult.GetErrorMessage()}");
        }

        // Analyze collection of results
        var successful = results.WhereSuccessful().OrderByCompletion();
        var failed = results.WhereFailed();
        
        Console.WriteLine($"\nSummary:");
        Console.WriteLine($"- Total: {results.Count()}");
        Console.WriteLine($"- Success: {successful.Count()}");
        Console.WriteLine($"- Failures: {failed.Count()}");
        Console.WriteLine($"- Completion %: {results.GetCompletionPercentage():F1}%");
        
        foreach (var result in successful)
        {
            Console.WriteLine($"\n{result.GetSummary()}");
        }
    }
}
```

## BackupServiceExtensions

The `BackupServiceExtensions` class provides utilities for managing backup operations, including creating daily backups, querying backup metadata, and retrieving backups by various criteria like labels, age, and file count.

### Usage Example

```csharp
using Services;

class Program
{
    static async Task Main()
    {
        var backupService = new BackupService();
        
        // Create daily backup
        var dailyBackup = await BackupServiceExtensions.CreateDailyBackupAsync(backupService);
        Console.WriteLine($"Created backup with ID: {dailyBackup.Id}");
        
        // Check for existing backups
        if (BackupServiceExtensions.HasBackupWithLabel("critical"))
        {
            var latestBackup = BackupServiceExtensions.GetLatestBackup();
            Console.WriteLine($"Latest backup has {latestBackup.GetTotalFileCount()} files");
            Console.WriteLine($"Total backup age: {BackupServiceExtensions.GetTotalAge().TotalDays:F1} days");
        }

        // Find backups by label pattern
        var patternBackups = BackupServiceExtensions.GetBackupsByLabelPattern("daily-2024");
        Console.WriteLine($"Found {patternBackups.Count} backups matching pattern");
        
        // Get backups sorted by age
        var sortedBackups = BackupServiceExtensions.GetBackupsByAgeAscending();
        Console.WriteLine($"\nOldest backup: {sortedBackups.First().CreationTime}");
        
        // Get backups in date range
        var rangeBackups = BackupServiceExtensions.GetBackupsInRange(
            DateTime.Now.AddDays(-7), 
            DateTime.Now
        );
        Console.WriteLine($"Found {rangeBackups.Count} backups in last 7 days");
    }
}
```

## LoggerFactory

`LoggerFactory` creates and configures `ILogger` instances for the application, supporting both console and optional file logging. It also provides helpers to validate the log path, rotate oversized log files, and clean up old logs based on a retention policy.

```csharp
using NotionTaskSync.Infrastructure.Logging;
using Microsoft.Extensions.Logging;

var loggerFactory = new LoggerFactory(
    logFilePath: "logs/app.log",
    minLogLevel: LogLevel.Information,
    enableConsole: true,
    enableFile: true);

// Create a logger for a specific class
ILogger logger = loggerFactory.CreateLogger<Program>();

// Verify the configured log file path is accessible
if (!loggerFactory.ValidateLogPath())
{
    logger.LogWarning("LoggerFactory", "Log file path is invalid or not writable.");
}

// Rotate the log file if it exceeds the default size and clean up old logs
loggerFactory.RotateLogFile();
loggerFactory.CleanupOldLogs();

// Retrieve the configured log file path (null if file logging is disabled)
string? path = loggerFactory.GetLogFilePath();
```

## NotionApiSettings

The `NotionApiSettings` class provides configuration settings for the Notion API. It includes properties for authentication, endpoints, rate limiting, and caching.

### Usage Example

```csharp
using NotionTaskSync.Infrastructure.Configuration;

class Program
{
    static void Main()
    {
        var settings = new NotionApiSettings
        {
            ApiKey = "your-api-key",
            BaseUrl = "https://api.notion.com/v1",
            ApiVersion = "2022-06-28",
            RequestTimeoutSeconds = 30,
            MaxRetries = 3,
            RetryDelayMs = 1000,
            RateLimitPerMinute = 30,
            RespectRateLimits = true,
            DefaultPageSize = 100,
            MaxPageSize = 100,
            EnableCaching = true,
            CacheDurationMinutes = 5,
            DatabaseIds = new List<string> { "database-id-1", "database-id-2" },
            PropertyMappings = new Dictionary<string, string> { { "property-name", "mapped-property-name" } }
        };

        // Validate the settings
        if (settings.Validate())
        {
            Console.WriteLine($"Valid settings: {settings}");
        }
        else
        {
            Console.WriteLine("Invalid settings");
        }

        // Get the masked API key
        var maskedApiKey = settings.GetMaskedApiKey();
        Console.WriteLine($"Masked API key: {maskedApiKey}");
    }
}
```

## DependencyInjection

The `DependencyInjection` class provides centralized configuration for the application's dependency injection container. It registers all services, repositories, configuration objects, and HTTP clients required by the application, following the Microsoft.Extensions.DependencyInjection pattern. The class includes methods to add application services, validate configuration, and register HTTP clients.

### Usage Example

```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using NotionTaskSync.Infrastructure.Configuration;
using NotionTaskSync.Services;
using NotionTaskSync.Data.Repositories;

class Program
{
    static void Main()
    {
        // Setup configuration
        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json")
            .Build();

        // Configure services
        var services = new ServiceCollection();

        // Validate configuration before registering services
        try
        {
            DependencyInjection.ValidateConfiguration(configuration);
            Console.WriteLine("Configuration validated successfully");
        }
        catch (ConfigurationException ex)
        {
            Console.WriteLine($"Configuration error: {ex.Message}");
            return;
        }

        // Register application services
        services.AddApplicationServices(configuration);

        // Register HTTP clients
        services.AddHttpClients();

        // Build service provider
        var serviceProvider = services.BuildServiceProvider();

        // Resolve services from DI container
        var syncService = serviceProvider.GetRequiredService<SyncService>();
        var notionApiService = serviceProvider.GetRequiredService<NotionApiService>();
        var taskRepository = serviceProvider.GetRequiredService<ITaskRepository>();
        var changeLogRepository = serviceProvider.GetRequiredService<IChangeLogRepository>();

        Console.WriteLine("Services registered successfully:");
        Console.WriteLine($"- SyncService: {syncService.GetType().Name}");
        Console.WriteLine($"- NotionApiService: {notionApiService.GetType().Name}");
        Console.WriteLine($"- TaskRepository: {taskRepository.GetType().Name}");
        Console.WriteLine($"- ChangeLogRepository: {changeLogRepository.GetType().Name}");
    }
}
```

## AppSettings

The `AppSettings` class provides application-wide configuration settings loaded from appsettings.json. It includes paths for local task storage, logging configuration, synchronization defaults, and backup settings.

### Usage Example

```csharp
using NotionTaskSync.Infrastructure.Configuration;

class Program
{
    static void Main()
    {
        var settings = new AppSettings
        {
            LocalTasksDirectory = "./my-tasks",
            LogLevel = "Debug",
            EnableConsoleLogging = true,
            LogFilePath = "./logs/app.log",
            DefaultSyncIntervalSeconds = 60,
            DefaultConflictStrategy = "Merge",
            MaxConcurrentSyncs = 2,
            EnableChangeTracking = true,
            MaxRetries = 5,
            ApiTimeoutSeconds = 60,
            BackupDirectory = "./backups",
            EnableAutoBackup = true,
            BackupFrequencyHours = 12,
            MaxBackupFiles = 20,
            Version = "2.0.0",
            Environment = "Production",
            SyncProfiles = new Dictionary<string, object>
            {
                { "daily-sync", new { Interval = 300, MaxConcurrent = 1 } },
                { "fast-sync", new { Interval = 60, MaxConcurrent = 4 } }
            }
        };

        // Validate the settings
        if (settings.Validate())
        {
            Console.WriteLine($"Valid configuration: {settings}");
        }
        else
        {
            Console.WriteLine("Invalid configuration detected");
        }

        // Configure logging based on settings
        Console.WriteLine($"Logging level: {settings.LogLevel}");
        Console.WriteLine($"Console logging enabled: {settings.EnableConsoleLogging}");
        Console.WriteLine($"File logging path: {settings.LogFilePath}");
        Console.WriteLine($"Sync interval: {settings.DefaultSyncIntervalSeconds} seconds");
        Console.WriteLine($"Environment: {settings.Environment}");
    }
}
```
