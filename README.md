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

## CollaborationSessionOptions

The `CollaborationSessionOptions` class provides fine-grained configuration for real-time collaboration sessions. It controls participant limits, operation batching, conflict resolution, and session lifecycle settings.

### Usage Example

```csharp
using NotionTaskSync.Collaboration;
using Microsoft.Extensions.Options;

class Program
{
    static void Main()
    {
        // Configure options from appsettings.json via Options pattern
        var options = Options.Create(new CollaborationSessionOptions
        {
            MaxParticipantsPerSession = 10,
            OperationLogCapacity = 500,
            MaxOperationsPerBatch = 25,
            IdleTimeout = TimeSpan.FromMinutes(15),
            HeartbeatInterval = TimeSpan.FromSeconds(20),
            AllowAutomaticTextMerge = true,
            ScalarConflictPolicy = CollaborationConflictPolicy.LastWriterWins,
            PersistOperationsToChangeLog = true,
            AllowObserverEdits = false
        });

        // Validate configuration
        if (options.Value.Validate())
        {
            Console.WriteLine("Collaboration session options are valid");
            Console.WriteLine($"Max participants: {options.Value.MaxParticipantsPerSession}");
            Console.WriteLine($"Operation log capacity: {options.Value.OperationLogCapacity}");
            Console.WriteLine($"Idle timeout: {options.Value.IdleTimeout.TotalMinutes} minutes");
            Console.WriteLine($"Heartbeat interval: {options.Value.HeartbeatInterval.TotalSeconds} seconds");
            Console.WriteLine($"Conflict policy: {options.Value.ScalarConflictPolicy}");
        }
        else
        {
            Console.WriteLine("Invalid collaboration session options");
        }
    }
}
```

## ValidationHelperTests

The `ValidationHelperTests` class contains unit tests for the `ValidationHelper` utility class, which provides various validation methods for common data formats including Notion IDs, emails, file paths, API keys, priorities, URLs, and identifier names. These tests verify that validation methods correctly handle valid inputs, edge cases, and invalid inputs.

### Usage Example

```csharp
using NotionTaskSync.Utils;
using FluentAssertions;
using Xunit;

class Program
{
    static void Main()
    {
        // Test Notion ID validation
        var notionIdWithoutDashes = "550e8400e29b41d4a716446655440000";
        var notionIdWithDashes = "550e8400-e29b-41d4-a716-446655440000";
        
        Console.WriteLine($"Notion ID without dashes valid: {ValidationHelper.IsValidNotionId(notionIdWithoutDashes)}");
        Console.WriteLine($"Notion ID with dashes valid: {ValidationHelper.IsValidNotionId(notionIdWithDashes)}");
        Console.WriteLine($"Null Notion ID valid: {ValidationHelper.IsValidNotionId(null)}");
        Console.WriteLine($"Empty Notion ID valid: {ValidationHelper.IsValidNotionId(string.Empty)}");
        
        // Test email validation
        var validEmail = "user@example.com";
        var invalidEmail = "notanemail";
        
        Console.WriteLine($"Valid email: {ValidationHelper.IsValidEmail(validEmail)}");
        Console.WriteLine($"Invalid email: {ValidationHelper.IsValidEmail(invalidEmail)}");
        
        // Test file path validation
        var validFilePath = "/tmp/test.txt";
        var validDirectoryPath = "/tmp";
        
        Console.WriteLine($"Valid file path: {ValidationHelper.IsValidFilePath(validFilePath)}");
        Console.WriteLine($"Valid directory path: {ValidationHelper.IsValidDirectoryPath(validDirectoryPath)}");
        Console.WriteLine($"Null file path valid: {ValidationHelper.IsValidFilePath(null)}");
        
        // Test API key validation
        var validApiKey = new string('a', 32); // 32 characters
        var shortApiKey = new string('a', 10); // Only 10 characters
        
        Console.WriteLine($"Valid API key (32 chars): {ValidationHelper.IsValidApiKey(validApiKey)}");
        Console.WriteLine($"Short API key (10 chars): {ValidationHelper.IsValidApiKey(shortApiKey)}");
        
        // Test priority validation
        Console.WriteLine($"Valid priority (50): {ValidationHelper.IsValidPriority(50)}");
        Console.WriteLine($"Invalid priority (-1): {ValidationHelper.IsValidPriority(-1)}");
        
        // Test URL validation
        var validUrl = "https://example.com";
        var invalidUrl = "ftp://example.com";
        
        Console.WriteLine($"Valid URL: {ValidationHelper.IsValidUrl(validUrl)}");
        Console.WriteLine($"Invalid URL: {ValidationHelper.IsValidUrl(invalidUrl)}");
    }
}
```

## ConflictResolutionUiTests

The `ConflictResolutionUiTests` class contains unit tests for the conflict resolution UI infrastructure, including `ConflictDiffService` and `ConflictResolutionService`. These tests verify diff generation, rendering, and various conflict resolution strategies including local wins, notion wins, manual review, and per-field overrides.

### Usage Example

```csharp
using NotionTaskSync.Tests;
using NotionTaskSync.Domain.Models;
using FluentAssertions;
using Xunit;

class Program
{
    static async Task Main()
    {
        // Test diff generation for identical values
        var diffService = new ConflictDiffService();
        var identicalDiff = await diffService.GenerateDiffForPropertyAsync("hello", "hello", "Title");
        Console.WriteLine($"Identical: {identicalDiff.IsIdentical}, Added: {identicalDiff.AddedCount}, Removed: {identicalDiff.RemovedCount}");
        
        // Test diff generation for different values
        var differentDiff = await diffService.GenerateDiffForPropertyAsync(
            "line one\nline two", 
            "line one\nLINE TWO", 
            "Description");
        Console.WriteLine($"Different: Added={differentDiff.AddedCount}, Removed={differentDiff.RemovedCount}");
        
        // Test text rendering
        var rendered = await diffService.RenderAsTextAsync(differentDiff);
        Console.WriteLine(rendered);
        
        // Test batch diff generation
        var conflicts = new List<ConflictResolution>
        {
            new() { TaskId = Guid.NewGuid(), LocalValue = "local1", NotionValue = "notion1", PropertyName = "Title" },
            new() { TaskId = Guid.NewGuid(), LocalValue = "local2", NotionValue = "notion2", PropertyName = "Status" }
        };
        var batchResults = await diffService.GenerateBatchDiffsAsync(conflicts);
        Console.WriteLine($"Generated {batchResults.Count} diffs");
        
        // Test conflict resolution with different strategies
        var resolutionService = new ConflictResolutionService();
        var localWinsResolutions = await resolutionService.ResolveConflictsAsync(
            conflicts,
            ConflictResolutionStrategy.LocalWins);
        Console.WriteLine($"Local wins resolved: {localWinsResolutions.Count(r => r.Status == ResolutionStatus.Resolved)}");
        
        var manualResolutions = await resolutionService.ResolveConflictsAsync(
            conflicts,
            ConflictResolutionStrategy.Manual);
        Console.WriteLine($"Manual review required: {manualResolutions.Count(r => r.Status == ResolutionStatus.PendingReview)}");
        
        // Test per-field override strategy
        var fieldStrategies = new Dictionary<string, ConflictResolutionStrategy>
        {
            { "Title", ConflictResolutionStrategy.LocalWins },
            { "Status", ConflictResolutionStrategy.NotionWins }
        };
        var overrideResolutions = await resolutionService.ResolveConflictsAsync(
            conflicts,
            ConflictResolutionStrategy.LastWrite,
            fieldStrategies);
        Console.WriteLine($"Field override applied: {overrideResolutions.Count}");
    }
}
```

## LocalFileServiceTests

The `LocalFileServiceTests` class contains unit tests for the `LocalFileService` class, which provides file system operations for persisting tasks to the local file system. These tests verify saving tasks to markdown files, loading tasks from files, handling edge cases like invalid inputs, and managing task collections.

### Usage Example

```csharp
using NotionTaskSync.Domain.Models;
using NotionTaskSync.Services;
using System;
using System.IO;
using System.Threading.Tasks;

class Program
{
    static async Task Main()
    {
        // Create a temporary directory for testing
        var testDirectory = Path.Combine(Path.GetTempPath(), $"local_file_service_test_{Guid.NewGuid()}");
        Directory.CreateDirectory(testDirectory);
        
        try
        {
            // Create LocalFileService instance
            var fileService = new LocalFileService(testDirectory);
            
            // Test 1: Save a valid task
            var task1 = new Domain.Models.Task
            {
                Id = Guid.NewGuid(),
                Title = "Implement LocalFileService feature",
                Description = "Create documentation for LocalFileServiceTests",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            
            await fileService.SaveTaskAsync(task1);
            Console.WriteLine($"Task saved to: {task1.LocalFilePath}");
            
            // Test 2: Save multiple tasks
            var task2 = new Domain.Models.Task
            {
                Id = Guid.NewGuid(),
                Title = "Test file operations",
                Description = "Verify file system operations work correctly",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            
            await fileService.SaveTaskAsync(task2);
            Console.WriteLine($"Second task saved to: {task2.LocalFilePath}");
            
            // Test 3: Load all tasks
            var allTasks = await fileService.LoadAllTasksAsync();
            Console.WriteLine($"Loaded {allTasks.Count} tasks from directory");
            
            foreach (var task in allTasks)
            {
                Console.WriteLine($"- {task.Title}: {task.LocalFilePath}");
            }
            
            // Test 4: Load a specific task
            if (allTasks.Count > 0)
            {
                var loadedTask = await fileService.LoadTaskAsync(allTasks[0].LocalFilePath!);
                Console.WriteLine($"Loaded task: {loadedTask?.Title}");
            }
            
            // Test 5: Handle invalid task (should throw ValidationException)
            try
            {
                var invalidTask = new Domain.Models.Task
                {
                    Id = Guid.Empty,
                    Title = string.Empty,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                
                await fileService.SaveTaskAsync(invalidTask);
                Console.WriteLine("ERROR: Should have thrown ValidationException");
            }
            catch (ValidationException ex)
            {
                Console.WriteLine($"Correctly caught ValidationException: {ex.Message}");
            }
            
            // Test 6: Handle special characters in title
            var specialTask = new Domain.Models.Task
            {
                Id = Guid.NewGuid(),
                Title = "Task / With \\ Special : Characters",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            
            await fileService.SaveTaskAsync(specialTask);
            var files = Directory.GetFiles(testDirectory);
            Console.WriteLine($"Special characters sanitized: {Path.GetFileName(files[0])}");
            
            // Test 7: Overwrite existing file
            var overwriteTask = new Domain.Models.Task
            {
                Id = Guid.NewGuid(),
                Title = "Implement LocalFileService feature",
                Description = "Updated description",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            
            await fileService.SaveTaskAsync(overwriteTask);
            var content = await File.ReadAllTextAsync(overwriteTask.LocalFilePath!);
            Console.WriteLine($"File overwritten, contains updated description: {content.Contains("Updated description")}");
        }
        finally
        {
            // Clean up
            if (Directory.Exists(testDirectory))
            {
                Directory.Delete(testDirectory, recursive: true);
            }
        }
    }
}
```

## RetryHelperTests

The `RetryHelperTests` class contains unit tests for the `RetryHelper` utility, which provides robust retry and circuit breaker patterns for handling transient failures in distributed systems. These tests verify retry behavior with exponential backoff, circuit breaker state transitions, predicate-based retry conditions, and proper error handling.

### Usage Example

```csharp
using NotionTaskSync.Utils;
using System;
using System.Threading.Tasks;

class Program
{
    static async Task Main()
    {
        // Example 1: Simple retry with exponential backoff
        var result1 = await RetryHelper.ExecuteWithRetryAsync<string>(
            async () => await FetchDataFromUnstableServiceAsync(),
            maxRetries: 5,
            initialDelayMs: 100);
        
        Console.WriteLine($"Success after retries: {result1}");

        // Example 2: Retry with custom retry predicate
        var result2 = await RetryHelper.ExecuteWithRetryAsync<TaskResult>(
            async () => await RiskyOperationAsync(),
            maxRetries: 3,
            shouldRetry: ex => ex is TimeoutException || ex is HttpRequestException,
            initialDelayMs: 200);
        
        Console.WriteLine($"Operation completed: {result2.IsSuccess}");

        // Example 3: Circuit breaker pattern
        var circuitBreakerResult = await RetryHelper.ExecuteWithCircuitBreakerAsync(
            async () => await ExternalApiCallAsync(),
            failureThreshold: 3,
            recoveryTimeoutMs: 5000);
        
        Console.WriteLine($"Circuit breaker success: {circuitBreakerResult.Success}, message: {circuitBreakerResult.Message}");

        // Example 4: Synchronous retry
        var syncResult = RetryHelper.ExecuteWithRetry(
            () => ComputeValue(),
            maxRetries: 2);
        
        Console.WriteLine($"Sync result: {syncResult}");
    }
    
    static async Task<string> FetchDataFromUnstableServiceAsync()
    {
        // Simulate an unstable service that might fail temporarily
        if (DateTime.UtcNow.Second % 3 == 0)
        {
            throw new InvalidOperationException("Service temporarily unavailable");
        }
        return "Data fetched successfully";
    }
    
    static async Task<TaskResult> RiskyOperationAsync()
    {
        // Simulate an operation that might timeout
        if (DateTime.UtcNow.Second % 5 == 0)
        {
            throw new TimeoutException("Operation timed out");
        }
        return new TaskResult { IsSuccess = true };
    }
    
    static async Task<string> ExternalApiCallAsync()
    {
        // Simulate external API that might be down
        if (DateTime.UtcNow.Second % 7 == 0)
        {
            throw new HttpRequestException("API unavailable");
        }
        return "API response";
    }
    
    static int ComputeValue()
    {
        // Simple synchronous computation
        return 42;
    }
}

// Simple result class for demonstration
public class TaskResult
{
    public bool IsSuccess { get; set; }
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

## SyncStartedEvent

The `SyncStartedEvent` class represents an event that is published when a synchronization operation is initiated. This event provides essential context about the sync process including configuration details, target database, and start timestamp.

### Usage Example

```csharp
using NotionTaskSync.Events;
using System;

class Program
{
    static void Main()
    {
        // Create a sync started event
        var syncStartedEvent = new SyncStartedEvent
        {
            SyncConfigId = "daily-sync-config",
            DatabaseId = "123e4567-e89b-12d3-a456-426614174000",
            StartTime = DateTime.UtcNow
        };

        Console.WriteLine($"Sync started for config: {syncStartedEvent.SyncConfigId}");
        Console.WriteLine($"Target database: {syncStartedEvent.DatabaseId}");
        Console.WriteLine($"Start time: {syncStartedEvent.StartTime:u}");
    }
}
```

## EventBus

The `EventBus` class implements a publish-subscribe pattern for loose coupling between application components. It allows different parts of the application to communicate through events without direct dependencies, enabling better separation of concerns and easier testing.


### Usage Example

```csharp
using NotionTaskSync.Events;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

class Program
{
    static async Task Main()
    {
        // Create logger and event bus
        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        var logger = loggerFactory.CreateLogger<EventBus>();
        var eventBus = new EventBus(logger);

        // Define custom event types
        public class TaskSyncedEvent : ApplicationEvent
        {
            public string TaskId { get; set; }
            public string NotionId { get; set; }
            public bool IsNew { get; set; }
        }

        public class TaskCreatedEvent : ApplicationEvent
        {
            public string TaskName { get; set; }
            public string Description { get; set; }
        }

        // Subscribe to events
        eventBus.Subscribe<TaskCreatedEvent>(async @event => {
            Console.WriteLine($"Handler 1: Task created - {@event.TaskName}");
            await Task.Delay(100); // Simulate async work
        });

        eventBus.Subscribe<TaskCreatedEvent>(@event => {
            Console.WriteLine($"Handler 2: Sync task created - {@event.TaskName}");
        });

        eventBus.Subscribe<TaskSyncedEvent>(async @event => {
            Console.WriteLine($"Sync handler: Task {@event.TaskId} synced to Notion {@event.NotionId}");
            await Task.Delay(50);
        });

        // Check subscriber count
        Console.WriteLine($"TaskCreatedEvent subscribers: {eventBus.GetSubscriberCount<TaskCreatedEvent>()}");
        Console.WriteLine($"TaskSyncedEvent subscribers: {eventBus.GetSubscriberCount<TaskSyncedEvent>()}");

        // Publish events
        var taskCreatedEvent = new TaskCreatedEvent
        {
            TaskName = "Implement EventBus documentation",
            Description = "Add EventBus section to README.md",
            Source = "Program.Main"
        };

        await eventBus.PublishAsync(taskCreatedEvent);

        var taskSyncedEvent = new TaskSyncedEvent
        {
            TaskId = "task-123",
            NotionId = "page-456",
            IsNew = true,
            Source = "SyncService"
        };

        await eventBus.PublishAsync(taskSyncedEvent);

        // Get diagnostic information
        var subscriberInfo = eventBus.GetSubscriberInfo();
        Console.WriteLine("\nSubscriber info:");
        foreach (var kvp in subscriberInfo)
        {
            Console.WriteLine($"- {kvp.Key}: {kvp.Value} subscribers");
        }

        // Unsubscribe and clear
        eventBus.UnsubscribeAll<TaskCreatedEvent>();
        Console.WriteLine($"\nAfter unsubscribing, TaskCreatedEvent subscribers: {eventBus.GetSubscriberCount<TaskCreatedEvent>()}");

        eventBus.Clear();
        Console.WriteLine("Event bus cleared");
    }
}

// Base ApplicationEvent class
public abstract class ApplicationEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime Timestamp { get; } = DateTime.UtcNow;
    public string? Source { get; set; }
}
```
