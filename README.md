## Architecture

For the big picture - what runs on the default path, how a sync cycle flows, why the design is the way it is, and where the extension seams are - see [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md). Short version: a single-shot console app where `SyncService` orchestrates change detection, conflict resolution and bidirectional apply between a Notion database and a local task store.

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

## ConflictResolutionTests

The `ConflictResolutionTests` class contains unit tests for conflict resolution functionality in task synchronization workflows. These tests verify how conflicts are detected, resolved, and tracked across local and Notion systems, including automatic resolution strategies, manual review workflows, and statistics calculations.

### Usage Example

```csharp
using NotionTaskSync.Domain.Models;
using NotionTaskSync.Data.Repositories;
using NotionTaskSync.Services;
using FluentAssertions;
using Moq;
using Xunit;

class Program
{
    static void Main()
    {
        // Create mock repository and service
        var mockRepo = new Mock<IChangeLogRepository>();
        var resolutionService = new ConflictResolutionService(mockRepo.Object);

        // Example 1: Resolve a conflict using LocalWins strategy
        var conflict1 = new ConflictResolution
        {
            TaskId = Guid.NewGuid(),
            LocalValue = "local task title",
            NotionValue = "notion task title",
            PropertyName = "Title",
            ConflictType = ConflictType.ConcurrentModification
        };

        conflict1.Resolve("local task title", ResolutionMethod.LocalWins, "local changes take precedence");
        Console.WriteLine($"Conflict resolved: {conflict1.Status}");
        Console.WriteLine($"Resolved value: {conflict1.ResolvedValue}");
        Console.WriteLine($"Resolution method: {conflict1.ResolutionMethod}");

        // Example 2: Mark a conflict for manual review
        var conflict2 = new ConflictResolution
        {
            TaskId = Guid.NewGuid(),
            LocalValue = "in-progress",
            NotionValue = "completed",
            PropertyName = "Status",
            ConflictType = ConflictType.ConcurrentModification
        };

        conflict2.MarkForManualReview("Values diverged significantly, manual inspection required");
        Console.WriteLine($"Conflict marked for review: {conflict2.Status}");
        Console.WriteLine($"Review reason: {conflict2.ResolutionNotes}");

        // Example 3: Merge conflicts with identical values (auto-resolves)
        var conflict3 = new ConflictResolution
        {
            TaskId = Guid.NewGuid(),
            LocalValue = "same value",
            NotionValue = "same value",
            PropertyName = "Description"
        };

        var mergedResult = resolutionService.MergeConflicts(conflict3);
        Console.WriteLine($"Merged conflict status: {mergedResult.Status}");
        Console.WriteLine($"Merged resolution method: {mergedResult.ResolutionMethod}");

        // Example 4: Get resolution statistics from mixed conflict statuses
        var conflicts = new List<ConflictResolution>
        {
            new() { TaskId = Guid.NewGuid(), Status = ResolutionStatus.Resolved },
            new() { TaskId = Guid.NewGuid(), Status = ResolutionStatus.Resolved },
            new() { TaskId = Guid.NewGuid(), Status = ResolutionStatus.Pending },
            new() { TaskId = Guid.NewGuid(), Status = ResolutionStatus.PendingReview },
            new() { TaskId = Guid.NewGuid(), Status = ResolutionStatus.Abandoned }
        };

        var stats = resolutionService.GetResolutionStats(conflicts);
        Console.WriteLine($"Total conflicts: {stats.TotalConflicts}");
        Console.WriteLine($"Resolved: {stats.ResolvedCount}");
        Console.WriteLine($"Pending review: {stats.PendingReviewCount}");
        Console.WriteLine($"Resolution rate: {stats.ResolutionRate:P}");

        // Example 5: Filter pending conflicts
        var pendingConflicts = resolutionService.GetPendingConflicts(conflicts);
        Console.WriteLine($"Pending conflicts count: {pendingConflicts.Count}");
        Console.WriteLine($"All pending: {pendingConflicts.All(c => c.IsPending())}");

        // Example 6: Merge conflicts with different values (requires manual review)
        var conflict6 = new ConflictResolution
        {
            TaskId = Guid.NewGuid(),
            LocalValue = "local status",
            NotionValue = "notion status",
            PropertyName = "Status"
        };

        var manualReviewResult = resolutionService.MergeConflicts(conflict6);
        Console.WriteLine($"Manual review required: {manualReviewResult.Status == ResolutionStatus.PendingReview}");
        Console.WriteLine($"Review reason: {manualReviewResult.ResolutionNotes}");
    }
}
```

## StringExtensionsTests

The `StringExtensionsTests` class contains unit tests for string extension methods that provide utilities for text manipulation and formatting. These methods include truncation, filename sanitization, case conversion, and slug generation, which are commonly used throughout the task synchronization workflow.

### Usage Example

```csharp
using NotionTaskSync.Utils;

class Program
{
    static void Main()
    {
        // Truncate a long string with default suffix
        var longText = "This is a very long text that needs to be truncated";
        var truncated = longText.Truncate(20);
        Console.WriteLine(truncated); // "This is a very long..."
        
        // Sanitize for filename (empty string returns "untitled")
        var emptyFileName = "".SanitizeForFilename();
        Console.WriteLine(emptyFileName); // "untitled"
        
        // Sanitize for filename (replace spaces with underscores)
        var fileName = "My Task File.txt".SanitizeForFilename();
        Console.WriteLine(fileName); // "My_Task_File.txt"
        
        // Convert PascalCase to snake_case
        var pascalCase = "NotionTaskSync".ToSnakeCase();
        Console.WriteLine(pascalCase); // "notion_task_sync"
        
        // Convert to URL-friendly slug
        var title = "Hello World!".ToSlug();
        Console.WriteLine(title); // "hello-world"
        
        // Use in a task title context
        var taskTitle = "Implement New Feature 🚀".ToSlug();
        Console.WriteLine(taskTitle); // "implement-new-feature"
    }
}
```

## BulkOperationServiceTests

The `BulkOperationServiceTests` class contains unit tests for the `BulkOperationService` class, which provides batch operations for managing multiple tasks simultaneously. These tests verify bulk updates including status changes, tag management, assignee assignment, priority setting, and soft deletion, with proper handling of edge cases and validation.

### Usage Example

```csharp
using NotionTaskSync.Services;
using NotionTaskSync.Domain.Models;
using System;
using System.Linq;
using System.Threading.Tasks;

class Program
{
static async Task Main()
{
// Create sample tasks
var task1 = new Domain.Models.Task
{
    Id = Guid.NewGuid(),
    Title = "Implement BulkOperationService feature",
    Status = TaskStatus.Todo,
    Priority = 50,
    Tags = "backend,feature"
};

var task2 = new Domain.Models.Task
{
    Id = Guid.NewGuid(),
    Title = "Write documentation",
    Status = TaskStatus.InProgress,
    Priority = 75,
    Tags = "docs"
};

var task3 = new Domain.Models.Task
{
    Id = Guid.NewGuid(),
    Title = "Fix critical bug",
    Status = TaskStatus.Todo,
    Priority = 25,
    Tags = "bug,urgent"
};

// Example 1: Update status for multiple tasks
var bulkService = new BulkOperationService(taskRepository, logger);
var statusResult = await bulkService.UpdateStatusAsync(
    new[] { task1.Id, task2.Id, task3.Id },
    TaskStatus.Done
);
Console.WriteLine($"Updated {statusResult.Affected} tasks, skipped {statusResult.Skipped} missing tasks");

// Example 2: Add tags to tasks (avoids duplicates)
var tagResult = await bulkService.AddTagAsync(
    new[] { task1.Id, task2.Id },
    "high-priority"
);
Console.WriteLine($"Added tag to {tagResult.Affected} tasks");

// Example 3: Remove a tag from tasks
var removeResult = await bulkService.RemoveTagAsync(
    new[] { task3.Id },
    "urgent"
);
Console.WriteLine($"Removed tag from {removeResult.Affected} tasks");

// Example 4: Assign tasks to a person
var assignResult = await bulkService.AssignAsync(
    new[] { task1.Id, task2.Id },
    "developer@example.com"
);
Console.WriteLine($"Assigned {assignResult.Affected} tasks");

// Example 5: Set priority for tasks
var priorityResult = await bulkService.SetPriorityAsync(
    new[] { task1.Id, task2.Id, task3.Id },
    90
);
Console.WriteLine($"Set priority for {priorityResult.Affected} tasks");

// Example 6: Soft delete tasks
var deleteResult = await bulkService.DeleteAsync(
    new[] { task3.Id }
);
Console.WriteLine($"Soft deleted {deleteResult.Affected} tasks");

// Example 7: Query tasks with filters
var matchingTasks = await bulkService.QueryAsync(
    t => t.Status == TaskStatus.Done && t.Priority >= 75
);
Console.WriteLine($"Found {matchingTasks.Count} matching tasks");

// Example 8: Handle validation errors (priority out of range)
try
{
    await bulkService.SetPriorityAsync(new[] { task1.Id }, 200); // Invalid priority
}
catch (ArgumentOutOfRangeException)
{
    Console.WriteLine("Correctly caught ArgumentOutOfRangeException for invalid priority");
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

## CryptoHelperTests

The `CryptoHelperTests` class contains unit tests for the `CryptoHelper` utility class, which provides cryptographic operations including SHA-256 and MD5 hashing, HMAC-SHA256 signature generation, and random token generation. These tests ensure the correct behavior and robustness of these security-critical functions, including edge cases like null inputs and invalid length constraints.

### Usage Example

```csharp
using NotionTaskSync.Utils;
using System;
using Xunit;
using FluentAssertions;

class Program
{
    static void Main()
    {
        // 1. Hash with SHA256
        var hash = CryptoHelper.HashSha256("test-data");
        Console.WriteLine($"SHA256 Hash: {hash}");
        
        // 2. Handle null or empty input
        var emptyHash = CryptoHelper.HashSha256("");
        Console.WriteLine($"Empty input hash: '{emptyHash}'");
        
        // 3. Hash with MD5
        var md5Hash = CryptoHelper.HashMd5("test-data");
        Console.WriteLine($"MD5 Hash: {md5Hash}");
        
        // 4. Generate random token
        var token = CryptoHelper.GenerateRandomToken(32);
        Console.WriteLine($"Token (length 32): {token}");
        
        // 5. Generate HMAC-SHA256 signature
        var signature = CryptoHelper.ComputeHmacSha256("data", "key");
        Console.WriteLine($"HMAC signature: {signature}");
        
        // 6. Verify HMAC or SHA256 hash
        bool isValid = CryptoHelper.VerifyHashSha256("test-data", hash);
        Console.WriteLine($"Is hash valid: {isValid}");
    }
}
```
```

## SyncIntegrationTests

The `SyncIntegrationTests` class contains integration tests for the complete synchronization workflow between local task storage and Notion databases. These tests verify end-to-end scenarios including change detection, conflict resolution, different sync directions, backup creation, and incremental sync operations.

### Usage Example

```csharp
using NotionTaskSync.Tests;
using NotionTaskSync.Domain.Models;
using NotionTaskSync.Domain.Enums;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Threading.Tasks;

class Program
{
static async Task Main()
{
// Setup test dependencies
var localTasksDirectory = Path.Combine(Path.GetTempPath(), $"sync_test_{Guid.NewGuid()}");
Directory.CreateDirectory(localTasksDirectory);

var localFileService = new LocalFileService(localTasksDirectory);
var mockTaskRepository = new Mock<ITaskRepository>();
var mockChangeLogRepository = new Mock<IChangeLogRepository>();
var mockNotionApiService = new Mock<NotionApiService>(null);
var mockChangeDetectionService = new Mock<ChangeDetectionService>(mockChangeLogRepository.Object);
var mockConflictResolutionService = new Mock<ConflictResolutionService>(mockChangeLogRepository.Object);
var mockLogger = new Mock<ILogger<SyncService>>();

// Create SyncService with mocked dependencies
var syncService = new SyncService(
    mockChangeDetectionService.Object,
    mockConflictResolutionService.Object,
    mockNotionApiService.Object,
    mockTaskRepository.Object,
    mockChangeLogRepository.Object);

// Example 1: Test creating a new task and syncing to Notion
var newTask = new Domain.Models.Task
{
    Id = Guid.NewGuid(),
    Title = "Implement SyncIntegrationTests feature",
    Description = "Add documentation for SyncIntegrationTests",
    Priority = 5,
    CreatedAt = DateTime.UtcNow,
    UpdatedAt = DateTime.UtcNow
};

await localFileService.SaveTaskAsync(newTask);

// Setup mocks to simulate successful sync
mockTaskRepository.Setup(r => r.GetAllAsync())
    .ReturnsAsync(new List<Domain.Models.Task> { newTask });
mockNotionApiService.Setup(a => a.FetchPagesAsync(It.IsAny<string>(), It.IsAny<int>()))
    .ReturnsAsync(new List<NotionPage>());
mockChangeDetectionService.Setup(s => s.DetectLocalChanges(It.IsAny<List<Domain.Models.Task>>(), It.IsAny<DateTime>()))
    .Returns(new List<ChangeLog>());
mockChangeDetectionService.Setup(s => s.DetectNotionChanges(It.IsAny<List<NotionPage>>(), It.IsAny<DateTime>()))
    .Returns(new List<ChangeLog>());
mockChangeDetectionService.Setup(s => s.DetectConflicts(It.IsAny<List<ChangeLog>>(), It.IsAny<List<ChangeLog>>()))
    .Returns(new List<ConflictResolution>());

var config = new SyncConfig(
    "Test Sync",
    "550e8400-e29b-41d4-a716-446655440000",
    localTasksDirectory);

var syncResult = await syncService.ExecuteSyncAsync(config);

Console.WriteLine($"Sync completed: {syncResult.Status}");
Console.WriteLine($"Local tasks: {syncResult.LocalTaskCount}");
Console.WriteLine($"Notion pages: {syncResult.NotionPageCount}");

// Example 2: Test conflict resolution
var conflictedTask = new Domain.Models.Task
{
    Id = Guid.NewGuid(),
    Title = "Conflicted Task",
    Priority = 3,
    CreatedAt = DateTime.UtcNow.AddHours(-1),
    UpdatedAt = DateTime.UtcNow
};

mockTaskRepository.Setup(r => r.GetAllAsync())
    .ReturnsAsync(new List<Domain.Models.Task> { conflictedTask });

var notionPage = new NotionPage("page_123", "550e8400-e29b-41d4-a716-446655440000", "Conflicted Task - Notion Version")
{
    LastEditedTime = DateTime.UtcNow.AddMinutes(-15)
};

mockNotionApiService.Setup(a => a.FetchPagesAsync(It.IsAny<string>(), It.IsAny<int>()))
    .ReturnsAsync(new List<NotionPage> { notionPage });

var localChange = new ChangeLog { TaskId = conflictedTask.Id, ChangeType = "Updated", Source = ChangeSource.Local };
var notionChange = new ChangeLog { TaskId = conflictedTask.Id, ChangeType = "Updated", Source = ChangeSource.Notion };

var conflict = new ConflictResolution
{
    TaskId = conflictedTask.Id,
    LocalValue = "Conflicted Task",
    NotionValue = "Conflicted Task - Notion Version",
    PropertyName = "Title",
    ConflictType = ConflictType.ConcurrentModification,
    Status = ResolutionStatus.Pending
};

mockChangeDetectionService.Setup(s => s.DetectLocalChanges(It.IsAny<List<Domain.Models.Task>>(), It.IsAny<DateTime>()))
    .Returns(new List<ChangeLog> { localChange });
mockChangeDetectionService.Setup(s => s.DetectNotionChanges(It.IsAny<List<NotionPage>>(), It.IsAny<DateTime>()))
    .Returns(new List<ChangeLog> { notionChange });
mockChangeDetectionService.Setup(s => s.DetectConflicts(It.IsAny<List<ChangeLog>>(), It.IsAny<List<ChangeLog>>()))
    .Returns(new List<ConflictResolution> { conflict });

var resolvedConflict = new ConflictResolution
{
    TaskId = conflictedTask.Id,
    LocalValue = "Conflicted Task",
    NotionValue = "Conflicted Task - Notion Version",
    PropertyName = "Title",
    ResolvedValue = "Conflicted Task",
    ResolutionMethod = ResolutionMethod.LocalWins,
    Status = ResolutionStatus.Resolved,
    ResolvedAt = DateTime.UtcNow
};

mockConflictResolutionService.Setup(s => s.ResolveConflictsAsync(
    It.IsAny<List<ConflictResolution>>(),
    It.IsAny<ConflictResolutionStrategy>(),
    It.IsAny<Dictionary<string, ConflictResolutionStrategy>?>()))
    .ReturnsAsync(new List<ConflictResolution> { resolvedConflict });

var conflictConfig = new SyncConfig("Conflict Test", "550e8400-e29b-41d4-a716-446655440000", localTasksDirectory)
{
    ConflictStrategy = ConflictResolutionStrategy.LocalWins
};

var conflictResult = await syncService.ExecuteSyncAsync(conflictConfig);

Console.WriteLine($"Conflicts detected: {conflictResult.ConflictsDetected}");
Console.WriteLine($"Conflicts resolved: {conflictResult.ConflictsResolved}");

// Example 3: Test different sync directions
var localToNotionConfig = new SyncConfig("Local to Notion", "550e8400-e29b-41d4-a716-446655440000", localTasksDirectory)
{
    Direction = SyncDirection.LocalToNotion
};

var localResult = await syncService.ExecuteSyncAsync(localToNotionConfig);
Console.WriteLine($"Local to Notion sync: {localResult.Status}");

var notionToLocalConfig = new SyncConfig("Notion to Local", "550e8400-e29b-41d4-a716-446655440000", localTasksDirectory)
{
    Direction = SyncDirection.NotionToLocal
};

var notionResult = await syncService.ExecuteSyncAsync(notionToLocalConfig);
Console.WriteLine($"Notion to Local sync: {notionResult.Status}");

// Cleanup
if (Directory.Exists(localTasksDirectory))
    Directory.Delete(localTasksDirectory, recursive: true);
}
}
```

## BackupServiceTests

The `BackupServiceTests` class contains unit tests for the `BackupService` class, which provides backup functionality for task synchronization workflows. These tests verify backup creation with labels, retrieval of available backups, proper error handling, and validation of backup metadata including timestamps, file counts, and ordering.

### Usage Example

```csharp
using NotionTaskSync.Services;
using NotionTaskSync.Domain.Models;
using NotionTaskSync.Domain.Exceptions;
using FluentAssertions;
using Moq;
using Xunit;

class Program
{
  static async Task Main()
  {
    // Setup temporary directories
    var backupDirectory = Path.Combine(Path.GetTempPath(), $"backup_test_{Guid.NewGuid()}");
    var tasksDirectory = Path.Combine(Path.GetTempPath(), $"tasks_{Guid.NewGuid()}");
    Directory.CreateDirectory(backupDirectory);
    Directory.CreateDirectory(tasksDirectory);

    try
    {
      // Create mock file service and backup service
      var mockFileService = new Mock<LocalFileService>(tasksDirectory);
      var backupService = new BackupService(backupDirectory, 5, mockFileService.Object);

      // Example 1: Create a backup with a custom label
      var labeledBackup = await backupService.CreateBackupAsync("pre-migration");
      Console.WriteLine($"Created backup with label: {labeledBackup.Label}");
      Console.WriteLine($"Backup ID: {labeledBackup.Id}");
      Console.WriteLine($"Backup created at: {labeledBackup.CreatedAt}");
      Console.WriteLine($"Backup path: {labeledBackup.Path}");
      Console.WriteLine($"Files in backup: {labeledBackup.FileCount}");

      // Example 2: Create a backup with default "auto" label
      var autoBackup = await backupService.CreateBackupAsync();
      Console.WriteLine($"Created backup with default label: {autoBackup.Label}");

      // Example 3: Get all available backups
      var allBackups = backupService.GetAvailableBackups();
      Console.WriteLine($"Total backups available: {allBackups.Count}");
      Console.WriteLine($"Backups ordered by creation date (newest first): {allBackups.Count > 0}");

      // Example 4: Handle file service exceptions
      mockFileService
        .Setup(x => x.BackupTasksAsync(It.IsAny<string>()))
        .ThrowsAsync(new IOException("Disk full"));

      try
      {
        await backupService.CreateBackupAsync("test");
        Console.WriteLine("ERROR: Should have thrown SyncException");
      }
      catch (SyncException ex)
      {
        Console.WriteLine($"Correctly caught SyncException: {ex.Message}");
      }

      // Example 5: Verify backup ordering
      var backup1Dir = Path.Combine(backupDirectory, "backup_20240101_120000_old");
      var backup2Dir = Path.Combine(backupDirectory, "backup_20240102_120000_new");
      Directory.CreateDirectory(backup1Dir);
      Directory.CreateDirectory(backup2Dir);

      var oldTime = DateTime.UtcNow.AddHours(-1);
      var newTime = DateTime.UtcNow;
      Directory.SetCreationTimeUtc(backup1Dir, oldTime);
      Directory.SetCreationTimeUtc(backup2Dir, newTime);

      var orderedBackups = backupService.GetAvailableBackups();
      Console.WriteLine($"Backups ordered correctly: {orderedBackups[0].CreatedAt > orderedBackups[1].CreatedAt}");
    }
    finally
    {
      // Cleanup
      if (Directory.Exists(backupDirectory))
        Directory.Delete(backupDirectory, recursive: true);
      if (Directory.Exists(tasksDirectory))
        Directory.Delete(tasksDirectory, recursive: true);
    }
  }
}
```

## HttpClientFactory

`HttpClientFactory` centralizes HTTP client creation and configuration for the application, providing specialized clients for different use cases including authenticated requests to external APIs like Notion. It handles client lifecycle management, header configuration, and rate limiting awareness.

### Usage Example

```csharp
using NotionTaskSync.Integration;
using System;
using System.Net.Http;
using System.Threading.Tasks;

class Program
{
    static async Task Main()
    {
        // Create HttpClientFactory with configuration
        var factory = new HttpClientFactory(
            baseAddress: new Uri("https://api.notion.com/v1"),
            defaultRequestHeaders: new()
            {
                {"User-Agent", "NotionTaskSync/1.0"},
                {"Accept", "application/json"}
            }
        );

        // Example 1: Get a pre-configured Notion HTTP client
        using var notionClient = factory.GetNotionHttpClient("secret_test_api_key_1234567890abcdef");
        
        Console.WriteLine($"Notion client base address: {notionClient.BaseAddress}");
        Console.WriteLine($"Notion client default headers: {notionClient.DefaultRequestHeaders.UserAgent}");

        // Example 2: Create a generic HTTP client for external services
        using var genericClient = factory.CreateGenericHttpClient();
        
        Console.WriteLine($"Generic client base address: {genericClient.BaseAddress}");
        Console.WriteLine($"Generic client has notion auth header: {genericClient.DefaultRequestHeaders.Contains("Authorization")}");

        // Example 3: Create an authenticated HTTP client with custom headers
        using var authenticatedClient = factory.CreateAuthenticatedHttpClient(
            apiKey: "custom-api-key-12345",
            additionalHeaders: new() { {"X-Custom-Header", "custom-value"} }
        );

        Console.WriteLine($"Authenticated client has auth header: {authenticatedClient.DefaultRequestHeaders.Authorization != null}");
        Console.WriteLine($"Custom header present: {authenticatedClient.DefaultRequestHeaders.Contains("X-Custom-Header")}");

        // Example 4: Create a rate-limit aware HTTP client
        using var rateLimitClient = factory.CreateRateLimitAwareHttpClient(
            maxRequestsPerSecond: 10,
            burstCapacity: 20
        );

        Console.WriteLine("Rate limit aware client created successfully");

        // Example 5: Configure custom headers for an existing client
        factory.ConfigureHeaders(notionClient, 
            new() { {"X-Notion-Version", "2022-06-28"} }
        );

        Console.WriteLine("Headers configured successfully");

        // Clean up
        factory.Dispose();
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

## WebhookHandler

The `WebhookHandler` class processes incoming webhook events from external services like Notion, GitHub, or other integrations. It validates, routes, and publishes domain events based on webhook payloads, enabling real-time reactive synchronization workflows.

### Usage Example

```csharp
using NotionTaskSync.Integration;
using NotionTaskSync.Events;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

class Program
{
    static async Task Main()
    {
        // Setup logging
        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        var logger = loggerFactory.CreateLogger<WebhookHandler>();
        
        // Create event bus for publishing domain events
        var eventBus = new EventBus(logger);
        
        // Initialize webhook handler
        var webhookHandler = new WebhookHandler(eventBus, logger);
        
        // Register custom webhook handler
        webhookHandler.RegisterHandler("custom_event", async (data) =>
        {
            Console.WriteLine($"Custom event received with data: {string.Join(", ", data.Select(kvp => $"{kvp.Key}={kvp.Value}"))}");
            
            // Publish domain event
            await eventBus.PublishAsync(new SyncStartedEvent
            {
                SyncConfigId = "webhook-triggered-sync",
                DatabaseId = "550e8400-e29b-41d4-a716-446655440000",
                StartTime = DateTime.UtcNow
            });
        });
        
        // Get registered webhook types
        var registeredTypes = webhookHandler.GetRegisteredWebhookTypes();
        Console.WriteLine($"Registered webhook types: {string.Join(", ", registeredTypes)}");
        
        // Handle a webhook
        var webhookData = new Dictionary<string, object>
        {
            { "page_id", "550e8400-e29b-41d4-a716-446655440000" },
            { "database_id", "123e4567-e89b-12d3-a456-426614174000" },
            { "title", "Updated Task Title" }
        };
        
        bool handled = await webhookHandler.HandleWebhookAsync("page_updated", webhookData);
        Console.WriteLine($"Webhook handled successfully: {handled}");
        
        // Validate webhook signature (example with test data)
        string payload = "{\"page_id\":\"550e8400-e29b-41d4-a716-446655440000\"}";
        string secret = "test-secret-key";
        string signature = Utils.CryptoHelper.ComputeHmacSha256(payload, secret);
        
        bool isValid = webhookHandler.ValidateWebhookSignature(payload, signature, secret);
        Console.WriteLine($"Webhook signature valid: {isValid}");
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

## NotionApiServiceTests

The `NotionApiServiceTests` class contains unit tests for the `NotionApiService` class, which provides a wrapper around the Notion API for fetching, creating, and updating pages in Notion databases. These tests verify API interaction patterns, error handling, authentication, pagination, and request/response formatting.

### Usage Example

```csharp
using NotionTaskSync.Services;
using NotionTaskSync.Domain.Models;
using NotionTaskSync.Domain.Exceptions;
using FluentAssertions;
using Xunit;

class Program
{
    static async Task Main()
    {
        // Example 1: Create NotionApiService with valid API key
        var apiService = new NotionApiService("secret_test_api_key_1234567890abcdef");
        Console.WriteLine("NotionApiService created with valid API key");
        
        // Example 2: Fetch pages from a Notion database
        var pages = await apiService.FetchPagesAsync("550e8400-e29b-41d4-a716-446655440000", pageSize: 50);
        Console.WriteLine($"Fetched {pages.Count} pages from database");
        
        // Example 3: Fetch pages with pagination
        var allPages = new List<NotionPage>();
        var pageSize = 100;
        var hasMore = true;
        string? startCursor = null;
        
        while (hasMore)
        {
            var batch = await apiService.FetchPagesAsync(
                "550e8400-e29b-41d4-a716-446655440000",
                pageSize: pageSize,
                startCursor: startCursor
            );
            
            allPages.AddRange(batch.Pages);
            hasMore = batch.HasMore;
            startCursor = batch.NextCursor;
        }
        
        Console.WriteLine($"Total pages fetched with pagination: {allPages.Count}");
        
        // Example 4: Fetch pages since a specific timestamp
        var cutoffTime = DateTime.UtcNow.AddDays(-7);
        var recentPages = await apiService.FetchPagesSinceAsync(
            "550e8400-e29b-41d4-a716-446655440000",
            cutoffTime
        );
        Console.WriteLine($"Pages modified in last 7 days: {recentPages.Count}");
        
        // Example 5: Create a new page in Notion
        var newTask = new NotionTask
        {
            Title = "Implement Notion API integration",
            Description = "Add NotionApiServiceTests documentation to README.md",
            Status = "In Progress",
            Priority = 50,
            DueDate = DateTime.UtcNow.AddDays(7),
            CreatedTime = DateTime.UtcNow
        };
        
        var createdPage = await apiService.CreatePageAsync(
            "550e8400-e29b-41d4-a716-446655440000",
            newTask
        );
        Console.WriteLine($"Created page with ID: {createdPage.Id}");
        
        // Example 6: Update an existing page
        newTask.Status = "Done";
        newTask.CompletedTime = DateTime.UtcNow;
        
        await apiService.UpdatePageAsync(
            createdPage.Id,
            newTask
        );
        Console.WriteLine("Page updated successfully");
        
        // Example 7: Handle validation exceptions
        try
        {
            await apiService.FetchPagesAsync(""); // Empty database ID
            Console.WriteLine("ERROR: Should have thrown ValidationException");
        }
        catch (ValidationException ex)
        {
            Console.WriteLine($"Correctly caught ValidationException: {ex.Message}");
        }
        
        // Example 8: Handle API exceptions
        try
        {
            var failingService = new NotionApiService("invalid_api_key");
            await failingService.FetchPagesAsync("550e8400-e29b-41d4-a716-446655440000");
            Console.WriteLine("ERROR: Should have thrown NotionApiException");
        }
        catch (NotionApiException ex)
        {
            Console.WriteLine($"Correctly caught NotionApiException: {ex.Message}");
        }
    }
}
```

## SyncServiceTests

The `SyncServiceTests` class contains unit tests for the `SyncService` class, which handles synchronization between local task storage and Notion databases. These tests verify synchronization execution, configuration validation, incremental/full sync modes, conflict detection and resolution, error handling, and timing tracking.

### Usage Example

```csharp
using NotionTaskSync.Domain.Models;
using NotionTaskSync.Services;
using NotionTaskSync.Data.Repositories;
using FluentAssertions;
using Moq;
using Xunit;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

class Program
{
    static async Task Main()
    {
        // Setup mocks for SyncService dependencies
        var mockChangeDetectionService = new Mock<ChangeDetectionService>(
            new Mock<IChangeLogRepository>().Object);
        var mockConflictResolutionService = new Mock<ConflictResolutionService>(
            new Mock<IChangeLogRepository>().Object);
        var mockNotionApiService = new Mock<NotionApiService>(null);
        var mockTaskRepository = new Mock<ITaskRepository>();
        var mockChangeLogRepository = new Mock<IChangeLogRepository>();
        var mockLogger = new Mock<ILogger<SyncService>>();

        // Create SyncService instance with mocked dependencies
        var syncService = new SyncService(
            mockChangeDetectionService.Object,
            mockConflictResolutionService.Object,
            mockNotionApiService.Object,
            mockTaskRepository.Object,
            mockChangeLogRepository.Object,
            mockLogger.Object);

        // Example 1: Test successful sync with valid configuration
        var validConfig = new SyncConfig("test-sync", "550e8400-e29b-41d4-a716-446655440000", "/tmp/tasks")
        {
            Direction = SyncDirection.Bidirectional
        };

        mockTaskRepository.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<DomainTask>());
        mockNotionApiService.Setup(a => a.FetchPagesAsync(It.IsAny<string>(), It.IsAny<int>()))
            .ReturnsAsync(new List<NotionPage>());
        mockChangeDetectionService.Setup(s => s.DetectLocalChanges(It.IsAny<List<DomainTask>>(), It.IsAny<DateTime>()))
            .Returns(new List<ChangeLog>());
        mockChangeDetectionService.Setup(s => s.DetectNotionChanges(It.IsAny<List<NotionPage>>(), It.IsAny<DateTime>()))
            .Returns(new List<ChangeLog>());
        mockChangeDetectionService.Setup(s => s.DetectConflicts(It.IsAny<List<ChangeLog>>(), It.IsAny<List<ChangeLog>>()))
            .Returns(new List<ConflictResolution>());

        var result = await syncService.ExecuteSyncAsync(validConfig);
        Console.WriteLine($"Sync status: {result.Status}"); // Should be Completed

        // Example 2: Test sync with invalid configuration (should throw exception)
        var invalidConfig = new SyncConfig("", "invalid-id", "/tmp");
        
        try
        {
            await syncService.ExecuteSyncAsync(invalidConfig);
            Console.WriteLine("ERROR: Should have thrown ConfigurationException");
        }
        catch (ConfigurationException ex)
        {
            Console.WriteLine($"Correctly caught ConfigurationException: {ex.Message}");
        }

        // Example 3: Test incremental sync with previous sync time
        var lastSyncTime = DateTime.UtcNow.AddHours(-1);
        var incrementalConfig = new SyncConfig("test-sync", "550e8400-e29b-41d4-a716-446655440000", "/tmp/tasks")
        {
            LastSyncAt = lastSyncTime
        };

        mockNotionApiService.Setup(a => a.FetchPagesSinceAsync(It.IsAny<string>(), It.IsAny<DateTime>()))
            .ReturnsAsync(new List<NotionPage>());

        await syncService.ExecuteSyncAsync(incrementalConfig);
        mockNotionApiService.Verify(a => a.FetchPagesSinceAsync(
            incrementalConfig.NotionDatabaseId, lastSyncTime), Times.Once);
    }
}
```
