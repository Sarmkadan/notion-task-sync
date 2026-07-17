## Architecture

For the big picture - what runs on the default path, how a sync cycle flows, why the design is the way it is, and where the extension seams are - see [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md). Short version: a single-shot console app where `SyncService` orchestrates change detection, conflict resolution and bidirectional apply between a Notion database and a local task store.

## SyncConfig

The `SyncConfig` class represents the configuration for a synchronization operation between local task storage and Notion databases. It defines all parameters needed to execute a sync cycle including authentication, database identification, sync direction, conflict resolution strategies, scheduling, and field mappings.

### Usage Example

```csharp
using Domain.Models;
using System;
using System.Collections.Generic;

class Program
{
    static void Main()
    {
        // Create a sync configuration for bidirectional synchronization
        var config = new SyncConfig
        {
            Id = Guid.NewGuid(),
            Name = "Daily Team Sync",
            NotionDatabaseId = "550e8400-e29b-41d4-a716-446655440000",
            LocalFolderPath = @"./tasks",
            NotionApiKey = "secret_test_api_key_1234567890abcdef",
            Direction = SyncDirection.Bidirectional,
            ConflictStrategy = ConflictResolutionStrategy.LastWrite,
            SyncIntervalSeconds = 86400, // 24 hours
            MaxRetries = 3,
            IsEnabled = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            FieldMappings = new Dictionary<string, string>
            {
                { "Title", "title" },
                { "Description", "rich_text" },
                { "Status", "status" },
                { "Priority", "number" }
            },
            IgnoredFields = new List<string> { "CreatedAt", "UpdatedAt" },
            FieldConflictStrategies = new Dictionary<string, ConflictResolutionStrategy>
            {
                { "Status", ConflictResolutionStrategy.LocalWins },
                { "Priority", ConflictResolutionStrategy.NotionWins }
            }
        };

        // Validate the configuration
        if (config.Validate())
        {
            Console.WriteLine($"Valid sync configuration: {config.Name}");
            Console.WriteLine($"Sync direction: {config.Direction}");
            Console.WriteLine($"Conflict strategy: {config.ConflictStrategy}");
            Console.WriteLine($"Sync interval: {config.SyncIntervalSeconds / 60} minutes");
        }

        // Map a local field to its Notion equivalent
        var notionPropertyName = config.MapLocalFieldToNotion("Status");
        Console.WriteLine($"Local 'Status' maps to Notion property: {notionPropertyName}");
    }
}
```

## Task

The `Task` class represents a task entity that can be synchronized between local storage and Notion databases. It encapsulates all properties needed for task management including identification, status tracking, priority levels, due dates, assignees, and metadata for synchronization workflows.

### Usage Example

```csharp
using Domain.Models;
using System;

class Program
{
    static void Main()
    {
        // Create a new task
        var task = new Domain.Models.Task
        {
            Id = Guid.NewGuid(),
            Title = "Implement Task documentation feature",
            Description = "Add Task section to README.md with realistic usage examples",
            NotionPageId = "550e8400-e29b-41d4-a716-446655440000",
            LocalFilePath = @"./tasks/task-implement-documentation.md",
            Status = TaskStatus.InProgress,
            Priority = 75,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            DueDate = DateTime.UtcNow.AddDays(7),
            AssignedTo = "developer@example.com",
            Tags = "documentation,readme,feature"
        };

        // Validate the task
        if (task.Validate())
        {
            Console.WriteLine($"Valid task created: {task.Title}");
            Console.WriteLine($"Status: {task.Status}");
            Console.WriteLine($"Priority: {task.Priority}");
            Console.WriteLine($"Due: {task.DueDate?.ToString("yyyy-MM-dd")}");
        }

        // Update task status
        task.UpdateTimestamp();
        task.Status = TaskStatus.Done;
        task.CompletedAt = DateTime.UtcNow;

        Console.WriteLine($"\nTask completed: {task.Title}");
        Console.WriteLine($"Completed at: {task.CompletedAt:u}");

        // Mark as deleted (soft delete)
        task.MarkAsDeleted();
        task.DeletedAt = DateTime.UtcNow;
        task.IsDeleted = true;

        Console.WriteLine($"\nTask marked as deleted: {task.Title}");
        Console.WriteLine($"Deleted at: {task.DeletedAt:u}");

        // Clone a task
        var clonedTask = task.Clone();
        Console.WriteLine($"\nCloned task ID: {clonedTask.Id}");
        Console.WriteLine($"Clone title: {clonedTask.Title}");
    }
}
```

## TaskRepository

The `TaskRepository` class provides an in-memory implementation of the `ITaskRepository` interface for managing task entities. It serves as a data access layer for task operations including creation, retrieval, updating, and deletion, with support for various query patterns needed for task synchronization workflows. The repository handles soft deletion (marking tasks as deleted rather than removing them) and provides change tracking capabilities.

### Public Members

- `AddAsync(Task task)` - Adds a new task to the repository
- `UpdateAsync(Task task)` - Updates an existing task in the repository
- `DeleteAsync(Guid taskId)` - Deletes a task from the repository by ID
- `GetByIdAsync(Guid taskId)` - Retrieves a task by its ID (excluding deleted tasks)
- `GetByNotionPageIdAsync(string notionPageId)` - Retrieves a task by its Notion page ID (excluding deleted tasks)
- `GetAllAsync()` - Retrieves all non-deleted tasks
- `GetByStatusAsync(TaskStatus status)` - Retrieves tasks filtered by status
- `GetModifiedSinceAsync(DateTime since)` - Retrieves tasks modified since a specific timestamp
- `GetAssignedToAsync(string assignee)` - Retrieves tasks assigned to a specific user
- `GetOverdueAsync(DateTime beforeDate)` - Retrieves overdue tasks that are not yet completed
- `SaveAsync()` - Persists changes (in-memory implementation)
- `CountAsync()` - Returns the total count of non-deleted tasks
- `CountByStatusAsync()` - Returns a dictionary of task counts grouped by status
- `GetAllIncludingDeletedAsync()` - Retrieves all tasks including deleted ones (for administrative purposes)
- `HasPendingChanges` - Property indicating if there are unsaved changes

### Usage Example

```csharp
using NotionTaskSync.Data.Repositories;
using NotionTaskSync.Domain.Models;
using System;
using System.Threading.Tasks;

class Program
{
static async Task Main()
{
// Initialize TaskRepository
var taskRepository = new TaskRepository();

// Example 1: Add a new task
var newTask = new Task
{
Id = Guid.NewGuid(),
Title = "Implement TaskRepository documentation feature",
Description = "Add TaskRepository section to README.md with realistic usage examples",
Status = TaskStatus.InProgress,
Priority = 75,
CreatedAt = DateTime.UtcNow,
UpdatedAt = DateTime.UtcNow,
DueDate = DateTime.UtcNow.AddDays(7),
AssignedTo = "developer@example.com",
Tags = "documentation,readme,feature"
};

await taskRepository.AddAsync(newTask);
Console.WriteLine($"Added task: {newTask.Title}");

// Example 2: Retrieve tasks by status
var inProgressTasks = await taskRepository.GetByStatusAsync(TaskStatus.InProgress);
Console.WriteLine($"In progress tasks: {inProgressTasks.Count}");

// Example 3: Retrieve tasks assigned to a specific user
var assignedTasks = await taskRepository.GetAssignedToAsync("developer@example.com");
Console.WriteLine($"Tasks assigned to developer@example.com: {assignedTasks.Count}");

// Example 4: Update a task
var taskToUpdate = inProgressTasks.FirstOrDefault();
if (taskToUpdate != null)
{
    taskToUpdate.Status = TaskStatus.Done;
    taskToUpdate.CompletedAt = DateTime.UtcNow;
    taskToUpdate.UpdateTimestamp();

    await taskRepository.UpdateAsync(taskToUpdate);
    Console.WriteLine($"Updated task: {taskToUpdate.Title} to {taskToUpdate.Status}");
}

// Example 5: Count tasks by status
var statusCounts = await taskRepository.CountByStatusAsync();
foreach (var kvp in statusCounts)
{
    Console.WriteLine($"{kvp.Key}: {kvp.Value} tasks");
}

// Example 6: Get overdue tasks
var overdueTasks = await taskRepository.GetOverdueAsync(DateTime.UtcNow);
Console.WriteLine($"Overdue tasks: {overdueTasks.Count}");

// Example 7: Get all tasks
var allTasks = await taskRepository.GetAllAsync();
Console.WriteLine($"Total active tasks: {allTasks.Count}");

// Example 8: Check for pending changes
if (taskRepository.HasPendingChanges)
{
    await taskRepository.SaveAsync();
    Console.WriteLine("Changes saved successfully");
}

// Example 9: Get tasks modified since a specific date
var recentChanges = await taskRepository.GetModifiedSinceAsync(DateTime.UtcNow.AddDays(-1));
Console.WriteLine($"Tasks modified in last 24 hours: {recentChanges.Count}");

// Example 10: Delete a task
if (allTasks.Count > 0)
{
    await taskRepository.DeleteAsync(allTasks[0].Id);
    Console.WriteLine("Task marked as deleted (soft delete)");
}

// Example 11: Get task by Notion page ID
var taskByNotionId = await taskRepository.GetByNotionPageIdAsync(newTask.NotionPageId);
Console.WriteLine($"Found task by Notion ID: {taskByNotionId?.Title}");
}
}
```

## FileSystemHelper

The `FileSystemHelper` class provides safe and reliable filesystem operations with comprehensive error handling and logging. It abstracts away filesystem complexity, making code more testable and robust. The helper handles directory creation, file reading/writing, file operations, and path normalization across different platforms.

### Public Members

- `EnsureDirectoryExists(string path)` - Ensures a directory exists, creating it if necessary
- `ReadFileAsync(string path)` - Safely reads a file's content with proper error handling
- `WriteFileAsync(string path, string content)` - Safely writes content to a file with automatic directory creation
- `AppendFileAsync(string path, string content)` - Safely appends content to a file, creating it if it doesn't exist
- `DeleteFile(string path)` - Safely deletes a file with logging
- `DeleteDirectory(string path)` - Safely deletes a directory and all its contents (recursive)
- `CopyFile(string sourcePath, string destinationPath, bool overwrite = false)` - Copies a file with overwrite handling
- `GetFileSize(string path)` - Gets the size of a file in bytes
- `IsDirectory(string path)` - Checks if a path is a directory
- `IsFile(string path)` - Checks if a path is a file
- `NormalizePath(string path)` - Normalizes a file path to use forward slashes and removes redundant segments
- `GetLastModifiedTime(string path)` - Gets the last modified time of a file

### Usage Example

```csharp
using NotionTaskSync.Utils;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Threading.Tasks;

class Program
{
    static async Task Main()
    {
        // Initialize FileSystemHelper with logger
        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        var logger = loggerFactory.CreateLogger<FileSystemHelper>();
        var fileSystem = new FileSystemHelper(logger);

        // Example 1: Ensure directory exists and create if needed
        var taskDirectory = @"./tasks/sync-2024";
        var directoryCreated = fileSystem.EnsureDirectoryExists(taskDirectory);
        Console.WriteLine($"Directory created/verified: {directoryCreated}");

        // Example 2: Read file content safely
        var readmePath = Path.Combine(taskDirectory, "README.md");
        var fileContent = await fileSystem.ReadFileAsync(readmePath);
        if (fileContent != null)
        {
            Console.WriteLine($"File exists with {fileContent.Length} characters");
        }
        else
        {
            Console.WriteLine("File doesn't exist or couldn't be read");
        }

        // Example 3: Write content to a new file
        var newFilePath = Path.Combine(taskDirectory, "task-config.json");
        var writeSuccess = await fileSystem.WriteFileAsync(
            newFilePath,
            @"{
                ""title"": ""Sync configuration"",
                ""databaseId"": ""550e8400-e29b-41d4-a716-446655440000""
            }"
        );
        Console.WriteLine($"File written successfully: {writeSuccess}");

        // Example 4: Append to log file
        var logPath = Path.Combine(taskDirectory, "sync.log");
        var appendSuccess = await fileSystem.AppendFileAsync(
            logPath,
            $"[{DateTime.UtcNow:O}] Sync operation started\n"
        );
        Console.WriteLine($"Log entry appended: {appendSuccess}");

        // Example 5: Get file information
        var fileSize = fileSystem.GetFileSize(newFilePath);
        var lastModified = fileSystem.GetLastModifiedTime(newFilePath);
        Console.WriteLine($"File size: {fileSize} bytes");
        Console.WriteLine($"Last modified: {lastModified:u}");

        // Example 6: Check if path is file or directory
        var isFile = fileSystem.IsFile(newFilePath);
        var isDirectory = fileSystem.IsDirectory(taskDirectory);
        Console.WriteLine($"Is file: {isFile}, Is directory: {isDirectory}");

        // Example 7: Normalize paths for cross-platform consistency
        var windowsPath = @"C:\Users\Developer\tasks\sync";
        var normalizedPath = FileSystemHelper.NormalizePath(windowsPath);
        Console.WriteLine($"Normalized path: {normalizedPath}");

        // Example 8: Copy configuration file
        var backupPath = Path.Combine(taskDirectory, "config-backup.json");
        var copySuccess = fileSystem.CopyFile(newFilePath, backupPath);
        Console.WriteLine($"File copied: {copySuccess}");

        // Example 9: Clean up - delete files
        var deleteFileSuccess = fileSystem.DeleteFile(backupPath);
        Console.WriteLine($"File deleted: {deleteFileSuccess}");

        // Example 10: Clean up - delete directory
        var deleteDirSuccess = fileSystem.DeleteDirectory(taskDirectory);
        Console.WriteLine($"Directory deleted: {deleteDirSuccess}");
    }
}
```

## XmlFormatter

The `XmlFormatter` class provides XML serialization and deserialization for task entities, enabling data interchange with systems that require XML format compatibility. It supports converting task objects to XML elements/documents and parsing XML back into task collections, making it ideal for integration scenarios, API responses, and data export/import workflows.

### Public Members

- `FormatTask(Task task)` - Formats a single task as an XML element
- `FormatTasks(List<Task> tasks)` - Formats a collection of tasks as an XML document
- `ParseTasks(string xml)` - Parses an XML string back into task objects
- `IsValidXml(string xml)` - Validates if a string is valid XML

### Usage Example

```csharp
using NotionTaskSync.Formatters;
using NotionTaskSync.Domain.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;

class Program
{ 
static void Main()
{ 
// Initialize XmlFormatter with logger
var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
var logger = loggerFactory.CreateLogger<XmlFormatter>();
var xmlFormatter = new XmlFormatter(logger);

// Example 1: Format a single task as XML
var task = new Task
{ 
Id = Guid.NewGuid(),
Title = "Implement XmlFormatter documentation",
Description = "Add XmlFormatter section to README.md with realistic usage examples",
Status = TaskStatus.InProgress,
Priority = 75,
CreatedAt = DateTime.UtcNow,
UpdatedAt = DateTime.UtcNow,
DueDate = DateTime.UtcNow.AddDays(7),
AssignedTo = "developer@example.com",
Tags = "documentation,xml,formatter",
NotionPageId = "550e8400-e29b-41d4-a716-446655440000",
IsDeleted = false 
};

var taskElement = xmlFormatter.FormatTask(task);
Console.WriteLine("Formatted task as XML:");
Console.WriteLine(taskElement);

// Example 2: Format multiple tasks as XML document
var tasks = new List<Task>
{ 
new Task
{ 
Id = Guid.NewGuid(),
Title = "Task 1",
Status = TaskStatus.Todo,
Priority = 50,
CreatedAt = DateTime.UtcNow,
UpdatedAt = DateTime.UtcNow 
},
new Task
{ 
Id = Guid.NewGuid(),
Title = "Task 2",
Status = TaskStatus.InProgress,
Priority = 75,
CreatedAt = DateTime.UtcNow,
UpdatedAt = DateTime.UtcNow,
DueDate = DateTime.UtcNow.AddDays(3) 
} 
};

var xmlDocument = xmlFormatter.FormatTasks(tasks);
Console.WriteLine("\nFormatted tasks as XML document:");
Console.WriteLine(xmlDocument);

// Example 3: Validate XML format
var isValid = xmlFormatter.IsValidXml(xmlDocument);
Console.WriteLine($"\nXML validation: {isValid}");

// Example 4: Parse XML back into tasks
var parsedTasks = xmlFormatter.ParseTasks(xmlDocument);
Console.WriteLine($"\nParsed {parsedTasks.Count} tasks from XML");
foreach (var parsedTask in parsedTasks)
{ 
Console.WriteLine($"- {parsedTask.Title} (Status: {parsedTask.Status})"); 
} 

// Example 5: Round-trip serialization
var originalTask = new Task
{ 
Id = Guid.NewGuid(),
Title = "Round-trip test",
Description = "Test XML serialization and deserialization",
Status = TaskStatus.Done,
Priority = 100,
CreatedAt = DateTime.UtcNow,
UpdatedAt = DateTime.UtcNow,
CompletedAt = DateTime.UtcNow,
IsDeleted = false 
};

var formatted = xmlFormatter.FormatTask(originalTask);
var xmlString = formatted.ToString();
var roundTripTasks = xmlFormatter.ParseTasks(xmlString);
var roundTripTask = roundTripTasks[0];

Console.WriteLine($"\nRound-trip test:");
Console.WriteLine($"Original ID: {originalTask.Id}");
Console.WriteLine($"Round-trip ID: {roundTripTask.Id}");
Console.WriteLine($"IDs match: {originalTask.Id == roundTripTask.Id}");
Console.WriteLine($"Title match: {originalTask.Title == roundTripTask.Title}"); 
} 
} 
```

## JsonFormatter

The `JsonFormatter` class provides JSON serialization and deserialization utilities for task entities, sync configurations, and other domain objects. It handles consistent JSON formatting with camelCase property naming, null value handling, and supports both serialization and deserialization operations. The formatter is critical for API responses, configuration export/import, and inter-system communication.

### Public Members

- `FormatTask(Task task)` - Serializes a single task to JSON string
- `FormatTasks(List<Task> tasks)` - Serializes a collection of tasks to JSON array string
- `FormatSyncConfig(SyncConfig config)` - Serializes a sync configuration to JSON
- `Format<T>(T obj)` - Serializes arbitrary objects to JSON with consistent formatting
- `DeserializeTask(string json)` - Deserializes a JSON string back into a task object
- `DeserializeTasks(string json)` - Deserializes a JSON array string into a collection of tasks
- `Deserialize<T>(string json)` - Deserializes arbitrary JSON into specified type
- `IsValidJson(string json)` - Validates if a string is valid JSON
- `Minify(string json)` - Minifies JSON by removing whitespace and formatting
- `PrettyPrint(string json)` - Pretty-prints JSON by expanding whitespace for readability

### Usage Example

```csharp
using NotionTaskSync.Formatters;
using NotionTaskSync.Domain.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;

class Program
{
    static void Main()
    {
        // Initialize JsonFormatter with logger
        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        var logger = loggerFactory.CreateLogger<JsonFormatter>();
        var jsonFormatter = new JsonFormatter(logger);

        // Example 1: Format a single task as JSON
        var task = new Task
        {
            Id = Guid.NewGuid(),
            Title = "Implement JsonFormatter documentation",
            Description = "Add JsonFormatter section to README.md with realistic usage examples",
            Status = TaskStatus.InProgress,
            Priority = 75,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            DueDate = DateTime.UtcNow.AddDays(7),
            AssignedTo = "developer@example.com",
            Tags = "documentation,json,formatter",
            NotionPageId = "550e8400-e29b-41d4-a716-446655440000",
            IsDeleted = false
        };

        var taskJson = jsonFormatter.FormatTask(task);
        Console.WriteLine("Formatted task as JSON:");
        Console.WriteLine(taskJson);

        // Example 2: Format multiple tasks as JSON array
        var tasks = new List<Task>
        {
            new Task
            {
                Id = Guid.NewGuid(),
                Title = "Task 1",
                Status = TaskStatus.Todo,
                Priority = 50,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new Task
            {
                Id = Guid.NewGuid(),
                Title = "Task 2",
                Status = TaskStatus.InProgress,
                Priority = 75,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                DueDate = DateTime.UtcNow.AddDays(3)
            }
        };

        var tasksJson = jsonFormatter.FormatTasks(tasks);
        Console.WriteLine("\nFormatted tasks as JSON array:");
        Console.WriteLine(tasksJson);

        // Example 3: Format a sync configuration
        var config = new SyncConfig
        {
            Name = "JsonFormatterSync",
            NotionDatabaseId = "550e8400-e29b-41d4-a716-446655440000",
            LocalFolderPath = @"./tasks",
            NotionApiKey = "secret_test_api_key_1234567890abcdef",
            Direction = SyncDirection.Bidirectional,
            ConflictStrategy = ConflictResolutionStrategy.LocalWins,
            SyncIntervalSeconds = 86400,
            IsEnabled = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var configJson = jsonFormatter.FormatSyncConfig(config);
        Console.WriteLine("\nFormatted sync configuration as JSON:");
        Console.WriteLine(configJson);

        // Example 4: Validate JSON
        var isValid = jsonFormatter.IsValidJson(taskJson);
        Console.WriteLine($"\nJSON validation: {isValid}");

        // Example 5: Minify JSON for data transfer
        var minified = jsonFormatter.Minify(tasksJson);
        Console.WriteLine($"\nMinified JSON size: {minified.Length} characters");

        // Example 6: Pretty-print JSON for debugging
        var pretty = jsonFormatter.PrettyPrint(minified);
        Console.WriteLine("\nPretty-printed JSON:");
        Console.WriteLine(pretty);

        // Example 7: Deserialize JSON back to objects
        var deserializedTask = jsonFormatter.DeserializeTask(taskJson);
        Console.WriteLine($"\nDeserialized task: {deserializedTask?.Title}");

        var deserializedTasks = jsonFormatter.DeserializeTasks(tasksJson);
        Console.WriteLine($"Deserialized {deserializedTasks?.Count} tasks");

        // Example 8: Generic serialization and deserialization
        var taskDto = new { Id = task.Id, Title = task.Title, Status = task.Status.ToString() };
        var dtoJson = jsonFormatter.Format(taskDto);
        Console.WriteLine($"\nSerialized DTO: {dtoJson}");

        var parsedDto = jsonFormatter.Deserialize<Dictionary<string, object>>(dtoJson);
        Console.WriteLine($"Deserialized DTO has {parsedDto?.Count} properties");
    }
}
```
## ChangeDetectionServiceJsonExtensions

The `ChangeDetectionServiceJsonExtensions` class provides System.Text.Json serialization extensions for the `ChangeDetectionService` class and related data models. It enables JSON serialization and deserialization of change detection data structures with camelCase naming policy, cycle reference handling, and null value suppression. This extension class is essential for persisting and transmitting change detection state between application sessions and across service boundaries.

### Usage Example

```csharp
using NotionTaskSync.Services;
using NotionTaskSync.Domain.Models;
using System;
using System.Collections.Generic;

class Program
{
    static void Main()
    {
        // Example 1: Serialize ChangeDetectionService to JSON
        var changeDetectionService = new ChangeDetectionService
        {
            Id = Guid.NewGuid(),
            Name = "Task Sync Service",
            LastSyncTime = DateTime.UtcNow,
            DetectedChanges = 42,
            ResolvedConflicts = 3,
            IsRunning = true
        };

        // Serialize with compact JSON
        var jsonCompact = changeDetectionService.ToJson();
        Console.WriteLine("Compact JSON representation:");
        Console.WriteLine(jsonCompact);

        // Serialize with pretty-printed JSON
        var jsonPretty = changeDetectionService.ToJson(indented: true);
        Console.WriteLine("\nPretty-printed JSON representation:");
        Console.WriteLine(jsonPretty);

        // Example 2: Deserialize ChangeDetectionService from JSON
        var jsonInput = @"{
            \"id\": \"550e8400-e29b-41d4-a716-446655440000\",
            \"name\": \"Database Sync Service\",
            \"lastSyncTime\": \"2024-07-19T14:30:00Z\",
            \"detectedChanges\": 25,
            \"resolvedConflicts\": 2,
            \"isRunning\": false
        }";

        var deserializedService = ChangeDetectionServiceJsonExtensions.FromJson(jsonInput);
        if (deserializedService != null)
        {
            Console.WriteLine($"\nDeserialized service: {deserializedService.Name}");
            Console.WriteLine($"Last sync: {deserializedService.LastSyncTime}");
            Console.WriteLine($"Changes detected: {deserializedService.DetectedChanges}");
        }

        // Example 3: Safe deserialization with error handling
        var invalidJson = "{ invalid json";
        var success = ChangeDetectionServiceJsonExtensions.TryFromJson(invalidJson, out var safeResult);
        Console.WriteLine($"\nSafe deserialization of invalid JSON: {(success ? "Success" : "Failed (as expected)")}");

        // Example 4: Serialize and deserialize ChangeLog list
        var changeLogs = new List<ChangeLog>
        {
            new ChangeLog
            {
                TaskId = Guid.NewGuid(),
                ChangeType = ChangeType.Created,
                Timestamp = DateTime.UtcNow,
                PropertyName = "Status",
                OldValue = "Todo",
                NewValue = "InProgress"
            },
            new ChangeLog
            {
                TaskId = Guid.NewGuid(),
                ChangeType = ChangeType.Updated,
                Timestamp = DateTime.UtcNow,
                PropertyName = "Priority",
                OldValue = "50",
                NewValue = "75"
            }
        };

        var logsJson = changeLogs.ToJson(indented: true);
        Console.WriteLine("\nChangeLog list as JSON:");
        Console.WriteLine(logsJson);

        // Example 5: Deserialize ChangeLog list
        var logsJsonInput = @"[
            {
                \"taskId\": \"550e8400-e29b-41d4-a716-446655440001\",
                \"changeType\": 1,
                \"timestamp\": \"2024-07-19T14:30:00Z\",
                \"propertyName\": \"Status\",
                \"oldValue\": \"Todo\",
                \"newValue\": \"InProgress\"
            },
            {
                \"taskId\": \"550e8400-e29b-41d4-a716-446655440002\",
                \"changeType\": 2,
                \"timestamp\": \"2024-07-19T14:30:05Z\",
                \"propertyName\": \"Priority\",
                \"oldValue\": \"50\",
                \"newValue\": \"75\"
            }
        ]";

        var deserializedLogs = ChangeDetectionServiceJsonExtensions.FromJsonToChangeLogList(logsJsonInput);
        Console.WriteLine($"\nDeserialized {deserializedLogs?.Count ?? 0} change logs");

        // Example 6: Serialize and deserialize ConflictResolution list
        var conflictResolutions = new List<ConflictResolution>
        {
            new ConflictResolution
            {
                TaskId = Guid.NewGuid(),
                ResolutionStrategy = ConflictResolutionStrategy.LocalWins,
                ResolvedAt = DateTime.UtcNow,
                ConflictingProperty = "Status"
            },
            new ConflictResolution
            {
                TaskId = Guid.NewGuid(),
                ResolutionStrategy = ConflictResolutionStrategy.NotionWins,
                ResolvedAt = DateTime.UtcNow,
                ConflictingProperty = "Priority"
            }
        };

        var conflictsJson = conflictResolutions.ToJson(indented: true);
        Console.WriteLine("\nConflictResolution list as JSON:");
        Console.WriteLine(conflictsJson);
    }
}
```
## RetryHelper

The `RetryHelper` class provides retry logic with exponential backoff for handling transient failures. It's essential for API calls that may temporarily fail due to rate limits, network issues, or temporary service unavailability. The helper implements industry-standard retry patterns to improve reliability without requiring caller code duplication.



### Public Members

- `ExecuteWithRetryAsync<T>(Func<Task<T>> operation, int maxRetries = 3, int initialDelayMs = 1000)` - Executes an operation with automatic retry on failure using exponential backoff
- `ExecuteWithRetryAsync<T>(Func<Task<T>> operation, Func<Exception, bool> shouldRetry, int maxRetries = 3, int initialDelayMs = 1000)` - Executes an operation with automatic retry, allowing caller to determine if retry should occur
- `ExecuteWithRetry<T>(Func<T> operation, int maxRetries = 3, int initialDelayMs = 1000)` - Executes a synchronous operation with retry logic
- `ExecuteWithCircuitBreakerAsync<T>(Func<Task<T>> operation, int maxFailures = 5, int resetTimeoutMs = 30000)` - Implements a circuit breaker pattern that stops retrying after too many failures

### Usage Example

```csharp
using NotionTaskSync.Utils;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

class Program
{
static async Task Main()
{
// Initialize RetryHelper with logger
var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
var logger = loggerFactory.CreateLogger<RetryHelper>();
var retryHelper = new RetryHelper(logger);

// Example 1: Basic async retry with exponential backoff
var result1 = await retryHelper.ExecuteWithRetryAsync(async () =>
{
// Simulate an API call that might fail
await Task.Delay(100);
return "API response data";
});

Console.WriteLine($"Operation succeeded: {result1}");

// Example 2: Async retry with custom retry condition
var result2 = await retryHelper.ExecuteWithRetryAsync(
async () =>
{
// Simulate an API call
await Task.Delay(50);
return 42;
},
// Only retry on specific exceptions (e.g., network errors)
ex => ex is HttpRequestException || ex is TaskCanceledException,
maxRetries: 5,
initialDelayMs: 500
);

Console.WriteLine($"Operation with custom retry succeeded: {result2}");

// Example 3: Synchronous retry (blocks calling thread)
var result3 = retryHelper.ExecuteWithRetry(() =>
{
// Simulate a local operation that might fail
return DateTime.UtcNow.Second % 2 == 0 ? "Success" : throw new InvalidOperationException("Temporary failure");
});

Console.WriteLine($"Synchronous operation result: {result3}");

// Example 4: Circuit breaker pattern - stops after too many failures
var (result4, success4) = await retryHelper.ExecuteWithCircuitBreakerAsync(async () =>
{
// Simulate a failing operation
await Task.Delay(10);
throw new InvalidOperationException("Service unavailable");
});

Console.WriteLine($"Circuit breaker result - Success: {success4}, Result: {result4}");

// Example 5: Real-world usage with API calls
var apiResult = await retryHelper.ExecuteWithRetryAsync(async () =>
{
// Simulate calling a Notion API endpoint
if (DateTime.UtcNow.Second % 4 == 0)
{
throw new HttpRequestException("Rate limit exceeded");
}

return new { Status = "Success", Data = "Notion page data" };
},
maxRetries: 3,
initialDelayMs: 1000
);

Console.WriteLine($"API call result: {apiResult.Status}");
}
}
```

## ValidationHelper

The `ValidationHelper` static class provides validation utilities for common data types and patterns used throughout the application. It includes methods for validating Notion IDs, email addresses, file paths, API keys, priorities, URLs, and more. These utilities help ensure data integrity and prevent invalid inputs from propagating through the sync pipeline.

### Public Members

- `IsValidNotionId(string? id)` - Validates that a string is a valid Notion page or database ID format
- `IsValidEmail(string? email)` - Validates that a string is a valid email address
- `IsValidFilePath(string? path)` - Validates that a file path is within acceptable bounds
- `IsValidDirectoryPath(string? path)` - Validates that a string represents a valid directory path
- `IsValidApiKey(string? apiKey)` - Validates that an API key has the correct format
- `IsValidPriority(int priority)` - Validates that a task priority is within acceptable range (0-100)
- `IsInRange(int value, int min, int max)` - Validates that an integer is within a specified range
- `IsLengthValid(string? value, int minLength, int maxLength)` - Validates that a string length is within bounds
- `SanitizeString(string? input)` - Sanitizes a string by removing potentially harmful characters
- `IsValidIdentifierName(string? name)` - Validates that a name follows identifier naming conventions
- `IsValidUrl(string? url)` - Validates that a URL is properly formatted

### Usage Example

```csharp
using NotionTaskSync.Utils;
using System;

class Program
{
  static void Main()
  {
    // Example 1: Validate Notion IDs
    var validNotionId = "550e8400-e29b-41d4-a716-446655440000";
    var invalidNotionId = "invalid-id";
    Console.WriteLine($"Valid Notion ID: {ValidationHelper.IsValidNotionId(validNotionId)}"); // True
    Console.WriteLine($"Invalid Notion ID: {ValidationHelper.IsValidNotionId(invalidNotionId)}"); // False

    // Example 2: Validate email addresses
    var validEmail = "user@example.com";
    var invalidEmail = "not-an-email";
    Console.WriteLine($"Valid email: {ValidationHelper.IsValidEmail(validEmail)}"); // True
    Console.WriteLine($"Invalid email: {ValidationHelper.IsValidEmail(invalidEmail)}"); // False

    // Example 3: Validate file and directory paths
    var validFilePath = @"./tasks/task-config.json";
    var invalidPath = "";
    Console.WriteLine($"Valid file path: {ValidationHelper.IsValidFilePath(validFilePath)}"); // True
    Console.WriteLine($"Invalid file path: {ValidationHelper.IsValidFilePath(invalidPath)}"); // False
    
    var validDirPath = @"./tasks";
    Console.WriteLine($"Valid directory path: {ValidationHelper.IsValidDirectoryPath(validDirPath)}"); // True

    // Example 4: Validate API keys
    var validApiKey = "sk_1234567890abcdefghijklmnop";
    var shortApiKey = "short-key";
    Console.WriteLine($"Valid API key: {ValidationHelper.IsValidApiKey(validApiKey)}"); // True
    Console.WriteLine($"Short API key: {ValidationHelper.IsValidApiKey(shortApiKey)}"); // False

    // Example 5: Validate task priorities
    var validPriority = 75;
    var invalidPriority = 150;
    Console.WriteLine($"Valid priority (75): {ValidationHelper.IsValidPriority(validPriority)}"); // True
    Console.WriteLine($"Invalid priority (150): {ValidationHelper.IsValidPriority(invalidPriority)}"); // False

    // Example 6: Validate range constraints
    var valueInRange = 50;
    var valueOutOfRange = 150;
    Console.WriteLine($"Value 50 in range 0-100: {ValidationHelper.IsInRange(valueInRange, 0, 100)}"); // True
    Console.WriteLine($"Value 150 in range 0-100: {ValidationHelper.IsInRange(valueOutOfRange, 0, 100)}"); // False

    // Example 7: Validate string lengths
    var text = "This is a task description";
    Console.WriteLine($"Text length valid (min 10, max 50): {ValidationHelper.IsLengthValid(text, 10, 50)}"); // True
    Console.WriteLine($"Text length valid (min 100, max 200): {ValidationHelper.IsLengthValid(text, 100, 200)}"); // False

    // Example 8: Sanitize strings
    var unsafeString = "Task description with\r\ncontrol characters\x00and\x1Finvalid\x7Fdata";
    var sanitized = ValidationHelper.SanitizeString(unsafeString);
    Console.WriteLine($"Sanitized string: '{sanitized}'");
    Console.WriteLine($"Sanitized length: {sanitized.Length}");

    // Example 9: Validate identifier names
    var validIdentifier = "taskName_123";
    var invalidIdentifier = "123invalid";
    Console.WriteLine($"Valid identifier: {ValidationHelper.IsValidIdentifierName(validIdentifier)}"); // True
    Console.WriteLine($"Invalid identifier: {ValidationHelper.IsValidIdentifierName(invalidIdentifier)}"); // False

    // Example 10: Validate URLs
    var validUrl = "https://api.example.com/tasks";
    var invalidUrl = "not-a-url";
    Console.WriteLine($"Valid URL: {ValidationHelper.IsValidUrl(validUrl)}"); // True
    Console.WriteLine($"Invalid URL: {ValidationHelper.IsValidUrl(invalidUrl)}"); // False

    // Example 11: Real-world usage in configuration validation
    var config = new SyncConfig
    {
      NotionDatabaseId = "550e8400-e29b-41d4-a716-446655440000",
      NotionApiKey = "sk_1234567890abcdefghijklmnop",
      LocalFolderPath = @"./tasks"
    };

    var isConfigValid = ValidationHelper.IsValidNotionId(config.NotionDatabaseId) &&
                       ValidationHelper.IsValidApiKey(config.NotionApiKey) &&
                       ValidationHelper.IsValidDirectoryPath(config.LocalFolderPath);
    
    Console.WriteLine($"\nConfiguration validation: {isConfigValid}");
    if (isConfigValid)
    {
      Console.WriteLine("All configuration values are valid!");
    }
  }
}
```

## TimeHelper

The `TimeHelper` static class provides utility methods for working with dates, times, and time intervals in the application. It handles UTC time operations, date formatting, parsing, time comparisons, and synchronization timing calculations. The helper is designed for scenarios involving task scheduling, sync operations, and temporal calculations.

### Public Members

- `GetCurrentUtcTime()` - Gets the current UTC time
- `FormatDateTime(DateTime dateTime)` - Formats a DateTime as a string using the application's standard format
- `ParseDateTime(string? dateString)` - Parses a string into a DateTime using the application's standard format
- `IsPast(DateTime dateTime)` - Determines if a given date is in the past
- `IsFuture(DateTime dateTime)` - Determines if a given date is in the future
- `DaysBetween(DateTime from, DateTime to)` - Calculates the number of days between two dates
- `HoursSince(DateTime dateTime)` - Calculates the number of hours since a given time
- `IsOverdue(DateTime dueDate)` - Determines if a date is overdue based on current time
- `FormatTimeSpan(TimeSpan timeSpan)` - Formats a TimeSpan into a human-readable string
- `GetRelativeTime(DateTime dateTime)` - Gets a human-readable string for how long ago a date was
- `ShouldSync(DateTime? lastSyncTime, int intervalSeconds)` - Determines if a sync should run based on the last sync time and interval
- `CalculateNextSyncTime(DateTime? lastSyncTime, int intervalSeconds)` - Calculates when the next sync should occur
- `GetTodayStart()` - Gets the start of the current day in UTC
- `GetTodayEnd()` - Gets the end of the current day in UTC
- `GetWeekStart()` - Gets the start of the current week in UTC
- `GetMonthStart()` - Gets the start of the current month in UTC
- `AreWithinInterval(DateTime time1, DateTime time2, TimeSpan interval)` - Determines if two times are within a specified interval of each other

### Usage Example

```csharp
using NotionTaskSync.Utils;
using System;

class Program
{
static void Main()
{
// Example 1: Get current UTC time
var currentTime = TimeHelper.GetCurrentUtcTime();
Console.WriteLine($"Current UTC time: {currentTime:O}");

// Example 2: Format and parse dates
var formattedDate = TimeHelper.FormatDateTime(currentTime);
Console.WriteLine($"Formatted date: {formattedDate}");

var parsedDate = TimeHelper.ParseDateTime(formattedDate);
Console.WriteLine($"Parsed date: {parsedDate?.ToString("O")}");

// Example 3: Check if dates are past or future
var yesterday = DateTime.UtcNow.AddDays(-1);
var tomorrow = DateTime.UtcNow.AddDays(1);

Console.WriteLine($"Yesterday is past: {TimeHelper.IsPast(yesterday)}"); // True
Console.WriteLine($"Tomorrow is future: {TimeHelper.IsFuture(tomorrow)}"); // True

// Example 4: Calculate time differences
var startDate = DateTime.UtcNow.AddDays(-5);
var endDate = DateTime.UtcNow.AddDays(10);
var daysBetween = TimeHelper.DaysBetween(startDate, endDate);
Console.WriteLine($"Days between dates: {daysBetween}");

var hoursSince = TimeHelper.HoursSince(startDate);
Console.WriteLine($"Hours since start date: {hoursSince:F1}");

// Example 5: Check for overdue items
var dueDate = DateTime.UtcNow.AddDays(-2);
Console.WriteLine($"Is overdue: {TimeHelper.IsOverdue(dueDate)}"); // True

// Example 6: Format time spans
var timeSpan = TimeSpan.FromHours(2.5);
Console.WriteLine($"Formatted time span: {TimeHelper.FormatTimeSpan(timeSpan)}"); // "2h"

var longSpan = TimeSpan.FromDays(3.2);
Console.WriteLine($"Formatted long span: {TimeHelper.FormatTimeSpan(longSpan)}"); // "3d"

// Example 7: Get relative time descriptions
var oneHourAgo = DateTime.UtcNow.AddHours(-1);
Console.WriteLine($"Relative time: {TimeHelper.GetRelativeTime(oneHourAgo)}"); // "1h ago"

var threeDaysAgo = DateTime.UtcNow.AddDays(-3);
Console.WriteLine($"Relative time: {TimeHelper.GetRelativeTime(threeDaysAgo)}"); // "3d ago"

// Example 8: Sync timing calculations
DateTime? lastSync = DateTime.UtcNow.AddMinutes(-30);
int syncInterval = 3600; // 1 hour

Console.WriteLine($"Should sync now: {TimeHelper.ShouldSync(lastSync, syncInterval)}"); // False

var nextSync = TimeHelper.CalculateNextSyncTime(lastSync, syncInterval);
Console.WriteLine($"Next sync at: {nextSync:O}");

// Example 9: Get day/week/month boundaries
var todayStart = TimeHelper.GetTodayStart();
var todayEnd = TimeHelper.GetTodayEnd();
Console.WriteLine($"Today: {todayStart:yyyy-MM-dd} to {todayEnd:yyyy-MM-dd HH:mm:ss}");

var weekStart = TimeHelper.GetWeekStart();
Console.WriteLine($"Week starts: {weekStart:yyyy-MM-dd} (Monday)");

var monthStart = TimeHelper.GetMonthStart();
Console.WriteLine($"Month starts: {monthStart:yyyy-MM-dd}");

// Example 10: Check time intervals
var time1 = DateTime.UtcNow.AddMinutes(-5);
var time2 = DateTime.UtcNow.AddMinutes(-4);
var withinInterval = TimeHelper.AreWithinInterval(time1, time2, TimeSpan.FromMinutes(10));
Console.WriteLine($"Times are within 10 minute interval: {withinInterval}"); // True
}
}
```

## ConfigRepository

The `ConfigRepository` class provides persistence and retrieval of sync configurations as JSON files. It enables users to define multiple sync profiles for different Notion databases, supporting configuration export/import for sharing and backup purposes. The repository handles file system operations with proper error handling and logging.

### Public Members

- `public async Task<bool> SaveConfigAsync(SyncConfig config)` - Saves a configuration to a JSON file
- `public async Task<SyncConfig?> GetConfigAsync(string configName)` - Loads a configuration by name
- `public async Task<List<SyncConfig>> GetAllConfigsAsync()` - Gets all saved configurations
- `public bool DeleteConfig(string configName)` - Deletes a configuration by name
- `public async Task<bool> ExportConfigAsync(SyncConfig config, string exportPath)` - Exports a configuration to a specific file path
- `public async Task<SyncConfig?> ImportConfigAsync(string importPath)` - Imports a configuration from a file
- `public bool ConfigExists(string configName)` - Checks if a configuration exists

### Usage Example

```csharp
using NotionTaskSync.Repositories;
using NotionTaskSync.Domain.Models;
using System;
using System.Threading.Tasks;

class Program
{
static async Task Main()
{
// Initialize ConfigRepository with configuration directory
var configRepository = new ConfigRepository(
    configDirectory: @"./configs",
    fileSystemHelper: new FileSystemHelper(),
    jsonFormatter: new JsonFormatter(),
    logger: new Logger<ConfigRepository>(new LoggerFactory())
);

// Example 1: Save a new configuration
var config = new SyncConfig
{
    Name = "TeamDailySync",
    NotionDatabaseId = "550e8400-e29b-41d4-a716-446655440000",
    LocalFolderPath = @"./tasks",
    NotionApiKey = "secret_test_api_key_1234567890abcdef",
    Direction = SyncDirection.Bidirectional,
    ConflictStrategy = ConflictResolutionStrategy.LocalWins,
    SyncIntervalSeconds = 86400, // 24 hours
    IsEnabled = true
};

var saveSuccess = await configRepository.SaveConfigAsync(config);
Console.WriteLine(saveSuccess ? "Configuration saved successfully" : "Failed to save configuration");

// Example 2: Load an existing configuration
var loadedConfig = await configRepository.GetConfigAsync("TeamDailySync");
if (loadedConfig != null)
{
    Console.WriteLine($"Loaded configuration: {loadedConfig.Name}");
    Console.WriteLine($"Notion Database: {loadedConfig.NotionDatabaseId}");
    Console.WriteLine($"Sync Direction: {loadedConfig.Direction}");
    Console.WriteLine($"Conflict Strategy: {loadedConfig.ConflictStrategy}");
}

// Example 3: Get all configurations
var allConfigs = await configRepository.GetAllConfigsAsync();
Console.WriteLine($"Total configurations: {allConfigs.Count}");

// Example 4: Check if configuration exists
var exists = configRepository.ConfigExists("TeamDailySync");
Console.WriteLine($"Configuration exists: {exists}");

// Example 5: Export configuration to a specific location
var exportSuccess = await configRepository.ExportConfigAsync(config, @"./backups/TeamDailySync-backup.json");
Console.WriteLine(exportSuccess ? "Configuration exported" : "Failed to export configuration");

// Example 6: Import configuration from file
var importSuccess = await configRepository.ImportConfigAsync(@"./backups/TeamDailySync-backup.json");
if (importSuccess != null)
{
    Console.WriteLine($"Imported configuration: {importSuccess.Name}");
}

// Example 7: Delete a configuration
var deleteSuccess = configRepository.DeleteConfig("TeamDailySync");
Console.WriteLine(deleteSuccess ? "Configuration deleted" : "Failed to delete configuration");
}
}
```

## SyncServiceTestsValidation

The `SyncServiceTestsValidation` static class provides validation utilities specifically for synchronization configuration objects used in `SyncServiceTests`. It validates null/empty strings, out-of-range numbers, default dates, and other constraints based on the semantic meaning of each member. This validation ensures that test configurations meet the necessary requirements before being used in test scenarios.

### Public Members

- `Validate(SyncConfig config)` - Validates a `SyncConfig` instance and returns a list of validation problems
- `IsValid(SyncConfig config)` - Determines if a `SyncConfig` instance is valid
- `EnsureValid(SyncConfig config)` - Validates a `SyncConfig` instance and throws if invalid
- `Validate(SyncServiceTests tests)` - Validates a `SyncServiceTests` instance and returns a list of validation problems
- `IsValid(SyncServiceTests tests)` - Determines if a `SyncServiceTests` instance is valid
- `EnsureValid(SyncServiceTests tests)` - Validates a `SyncServiceTests` instance and throws if invalid

### Usage Example

```csharp
using NotionTaskSync.Tests;
using NotionTaskSync.Domain.Models;
using System;

class Program
{
    static void Main()
    {
        // Example 1: Validate a SyncConfig instance
        var validConfig = new SyncConfig
        {
            Name = "TeamDailySync",
            NotionDatabaseId = "550e8400-e29b-41d4-a716-446655440000",
            LocalFolderPath = @"./tasks",
            SyncIntervalSeconds = 300,
            MaxRetries = 3,
            IsEnabled = true
        };

        var validationErrors = SyncServiceTestsValidation.Validate(validConfig);
        
        if (validationErrors.Count == 0)
        {
            Console.WriteLine("Configuration is valid!");
        }
        else
        {
            Console.WriteLine("Validation errors found:");
            foreach (var error in validationErrors)
            {
                Console.WriteLine($"- {error}");
            }
        }

        // Example 2: Check if configuration is valid using IsValid
        var isValid = SyncServiceTestsValidation.IsValid(validConfig);
        Console.WriteLine($"Is configuration valid: {isValid}");

        // Example 3: Ensure configuration is valid (throws if invalid)
        try
        {
            SyncServiceTestsValidation.EnsureValid(validConfig);
            Console.WriteLine("Configuration passed validation!");
        }
        catch (ArgumentException ex)
        {
            Console.WriteLine($"Validation failed: {ex.Message}");
        }

        // Example 4: Validate an invalid configuration to see error messages
        var invalidConfig = new SyncConfig
        {
            Name = "", // Invalid: empty name
            NotionDatabaseId = "invalid-id", // Invalid: wrong format
            LocalFolderPath = @"./nonexistent", // Invalid: directory doesn't exist
            SyncIntervalSeconds = 0, // Invalid: out of range
            MaxRetries = 150 // Invalid: out of range
        };

        var invalidErrors = SyncServiceTestsValidation.Validate(invalidConfig);
        Console.WriteLine($"\nInvalid configuration has {invalidErrors.Count} validation errors:");
        foreach (var error in invalidErrors)
        {
            Console.WriteLine($"- {error}");
        }
    }
}
```


## ConflictDiffServiceJsonExtensions

The `ConflictDiffServiceJsonExtensions` static class provides JSON serialization and deserialization extensions for the `ConflictDiffService` and related diff types using System.Text.Json. It enables converting conflict resolution results and diff line information to/from JSON format for storage, transmission, or logging purposes.

### Public Members

- `ToJson(this ConflictDiffService value, bool indented = false)` - Serializes a `ConflictDiffService` instance to a JSON string with optional indentation
- `FromJson(string json)` - Deserializes a JSON string into a `ConflictDiffService` instance (returns null on failure)
- `TryFromJson(string json, out ConflictDiffService? value)` - Attempts to deserialize JSON with error handling, returns true on success

### Usage Example

```csharp
using NotionTaskSync.Services;
using NotionTaskSync.Domain.Models;
using System;
using System.Text.Json;

class Program
{
    static void Main()
    {
        // Create a conflict diff result
        var conflictResult = new ConflictDiffResult
        {
            ConflictId = Guid.NewGuid(),
            PropertyName = "Status",
            LocalValue = "InProgress",
            NotionValue = "Todo",
            GeneratedAt = DateTime.UtcNow
        };

        conflictResult.Lines.Add(new DiffLine
        {
            Text = "Local: InProgress",
            Kind = DiffLineKind.Local,
            LocalLineNumber = 5,
            NotionLineNumber = null
        });

        conflictResult.Lines.Add(new DiffLine
        {
            Text = "Remote: Todo",
            Kind = DiffLineKind.Remote,
            LocalLineNumber = null,
            NotionLineNumber = 8
        });

        // Serialize to JSON
        var json = conflictResult.ToJson(indented: true);
        Console.WriteLine("Serialized conflict diff:");
        Console.WriteLine(json);

        // Deserialize back to object
        var deserialized = ConflictDiffServiceJsonExtensions.FromJson(json);
        Console.WriteLine($"
Deserialized conflict ID: {deserialized?.ConflictId}");
        Console.WriteLine($"Property: {deserialized?.PropertyName}");
        Console.WriteLine($"Lines count: {deserialized?.Lines.Count}");

        // Serialize ConflictDiffService
        var diffService = new ConflictDiffService();
        var serviceJson = diffService.ToJson();
        Console.WriteLine($"
ConflictDiffService JSON length: {serviceJson.Length} characters");

        // TryFromJson with error handling
        var invalidJson = "{ invalid json }";
        var success = ConflictDiffServiceJsonExtensions.TryFromJson(invalidJson, out var parsedService);
        Console.WriteLine($"
TryFromJson with invalid JSON succeeded: {success}");
    }
}
```
## CollectionExtensions

The `CollectionExtensions` static class provides extension methods for collections (lists, enumerables, dictionaries) that simplify common collection operations used throughout the application. It includes utilities for checking collection state, safe access, batching, partitioning, filtering, and transformation operations that reduce repetitive code patterns in data processing pipelines.

### Public Members

- `IsNullOrEmpty<T>(this IEnumerable<T>? collection)` - Determines if a collection is null or contains no elements
- `HasItems<T>(this IEnumerable<T>? collection)` - Determines if a collection has elements with meaningful content
- `SafeGetAt<T>(this IList<T> list, int index, T? defaultValue = default)` - Safely gets an item at a specific index, returning a default value if index is out of range
- `Batch<T>(this IEnumerable<T> items, int batchSize)` - Batches a collection into chunks of specified size
- `Partition<T>(this IEnumerable<T> items, Func<T, bool> predicate)` - Partitions a collection into two groups based on a predicate
- `GroupByFrequency<T, TKey>(this IEnumerable<T> items, Func<T, TKey> keySelector)` - Groups items and returns the groups with the highest occurrence count first
- `Flatten<T>(this IEnumerable<IEnumerable<T>> nested)` - Flattens a nested collection (collection of collections) into a single collection
- `SafeToDictionary<TItem, TKey, TValue>(this IEnumerable<TItem> items, Func<TItem, TKey> keySelector, Func<TItem, TValue> valueSelector)` - Creates a dictionary from a collection, handling duplicate keys gracefully
- `SplitWhere<T>(this IEnumerable<T> items, Func<T, bool> splitCondition)` - Splits a collection at indices where a predicate returns true
- `WhereNotNull<T>(this IEnumerable<T?> items)` - Removes all null values from a collection
- `DistinctBy<T, TKey>(this IEnumerable<T> items, Func<T, TKey> keySelector)` - Returns distinct items based on a key selector function
- `IntersectBy<T, TKey>(this IEnumerable<T> items, IEnumerable<T> other, Func<T, TKey> keySelector)` - Intersects two collections based on a key selector function
- `AddIf<T>(this List<T> list, T item, Func<T, bool> condition)` - Adds an item to a collection if it passes a condition
- `Shuffle<T>(this IList<T> list, Random? random = null)` - Shuffles a collection in-place using Fisher-Yates algorithm

### Usage Example

```csharp
using NotionTaskSync.Utils;
using System;
using System.Collections.Generic;
using System.Linq;

class Program
{
  static void Main()
  {
    // Example 1: Check if collection is null or empty
    List<string>? nullList = null;
    var isNullOrEmpty = nullList.IsNullOrEmpty();
    Console.WriteLine($"Null list is null or empty: {isNullOrEmpty}"); // True

    var emptyList = new List<string>();
    Console.WriteLine($"Empty list is null or empty: {emptyList.IsNullOrEmpty()}"); // True

    var populatedList = new List<string> { "item1", "item2" };
    Console.WriteLine($"Populated list is null or empty: {populatedList.IsNullOrEmpty()}"); // False

    // Example 2: Check if collection has items
    Console.WriteLine($"Null list has items: {nullList.HasItems()}"); // False
    Console.WriteLine($"Empty list has items: {emptyList.HasItems()}"); // False
    Console.WriteLine($"Populated list has items: {populatedList.HasItems()}"); // True

    // Example 3: Safe index access
    var numbers = new List<int> { 10, 20, 30, 40, 50 };
    var first = numbers.SafeGetAt(0); // 10
    var last = numbers.SafeGetAt(4); // 50
    var outOfRange = numbers.SafeGetAt(10); // null (default)
    var outOfRangeWithDefault = numbers.SafeGetAt(10, -1); // -1
    
    Console.WriteLine($"First element: {first}");
    Console.WriteLine($"Last element: {last}");
    Console.WriteLine($"Out of range (default): {outOfRange}");
    Console.WriteLine($"Out of range (custom default): {outOfRangeWithDefault}");

    // Example 4: Batch processing
    var largeCollection = Enumerable.Range(1, 100);
    var batches = largeCollection.Batch(10);
    
    Console.WriteLine($"Total batches: {batches.Count()}");
    foreach (var batch in batches)
    {
      Console.WriteLine($"Batch size: {batch.Count()}");
    }

    // Example 5: Partition collection
    var mixedNumbers = new List<int> { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
    var (evenNumbers, oddNumbers) = mixedNumbers.Partition(x => x % 2 == 0);
    
    Console.WriteLine($"Even numbers: {string.Join(", ", evenNumbers)}");
    Console.WriteLine($"Odd numbers: {string.Join(", ", oddNumbers)}");

    // Example 6: Group by frequency
    var fruits = new List<string> { "apple", "banana", "apple", "orange", "banana", "apple", "kiwi" };
    var frequencyGroups = fruits.GroupByFrequency(x => x);
    
    Console.WriteLine("Fruit frequencies:");
    foreach (var group in frequencyGroups)
    {
      Console.WriteLine($"{group.Key}: {group.Count()} occurrences");
    }

    // Example 7: Flatten nested collections
    var nestedLists = new List<List<int>>
    {
      new List<int> { 1, 2, 3 },
      new List<int> { 4, 5 },
      new List<int> { 6, 7, 8, 9 }
    };
    
    var flattened = nestedLists.Flatten();
    Console.WriteLine($"Flattened count: {flattened.Count()}");
    Console.WriteLine($"Flattened values: {string.Join(", ", flattened)}");

    // Example 8: Safe dictionary conversion
    var people = new List<(string Name, int Age)>
    {
      ("Alice", 30),
      ("Bob", 25),
      ("Charlie", 35),
      ("Alice", 31) // Duplicate key - last wins
    };
    
    var ageDictionary = people.SafeToDictionary(
      person => person.Name,
      person => person.Age
    );
    
    Console.WriteLine($"Age dictionary: {string.Join(", ", ageDictionary.Select(kvp => $"{kvp.Key}={kvp.Value}"))}");

    // Example 9: Split collection where condition is met
    var logEntries = new List<string>
    {
      "[INFO] Application started",
      "[DEBUG] Loading configuration",
      "[INFO] User logged in",
      "---",
      "[ERROR] Database connection failed",
      "[INFO] Retrying connection..."
    };
    
    var logGroups = logEntries.SplitWhere(entry => entry == "---");
    Console.WriteLine($"Log groups: {logGroups.Count}");
    foreach (var group in logGroups)
    {
      Console.WriteLine($"Group has {group.Count} entries");
    }

    // Example 10: Remove null values
    var nullableList = new List<string?> { "hello", null, "world", null, "test" };
    var nonNullItems = nullableList.WhereNotNull();
    
    Console.WriteLine($"Original count: {nullableList.Count}");
    Console.WriteLine($"After filtering nulls: {nonNullItems.Count()}");
    Console.WriteLine($"Values: {string.Join(", ", nonNullItems)}");

    // Example 11: Distinct by property
    var products = new List<(string Name, string Category, decimal Price)>
    {
      ("Laptop", "Electronics", 999.99m),
      ("Phone", "Electronics", 699.99m),
      ("Laptop", "Computers", 1099.99m), // Same name, different category
      ("Monitor", "Electronics", 249.99m)
    };
    
    var uniqueProducts = products.DistinctBy(p => p.Name);
    Console.WriteLine($"Unique products by name: {uniqueProducts.Count()}");

    // Example 12: Intersect collections by key
    var ids1 = new List<int> { 1, 2, 3, 4, 5 };
    var ids2 = new List<int> { 3, 4, 5, 6, 7 };
    var commonIds = ids1.IntersectBy(ids2, x => x);
    
    Console.WriteLine($"Common IDs: {string.Join(", ", commonIds)}");

    // Example 13: Conditional add
    var numbersList = new List<int> { 1, 2, 3 };
    numbersList.AddIf(4, x => x % 2 == 0); // 4 is even, will be added
    numbersList.AddIf(5, x => x % 2 == 0); // 5 is odd, will NOT be added
    
    Console.WriteLine($"Numbers after conditional adds: {string.Join(", ", numbersList)}");

    // Example 14: Shuffle list
    var deck = new List<string> { "Ace", "King", "Queen", "Jack", "10", "9" };
    var random = new Random(42); // Use fixed seed for reproducible example
    deck.Shuffle(random);
    
    Console.WriteLine($"Shuffled deck: {string.Join(", ", deck)}");
  }
}
```

## StringExtensions

The `StringExtensions` static class provides extension methods for string manipulation and validation. It centralizes common string operations used throughout the sync pipeline, improving code readability and reducing duplication. Methods include truncation, sanitization for filenames, email/GUID validation, case conversion, substring extraction, case-insensitive comparison, line ending normalization, and slug generation.

### Public Members

- `Truncate(this string str, int maxLength, string suffix = "...")` - Truncates a string to a maximum length with optional ellipsis suffix
- `SanitizeForFilename(this string str)` - Sanitizes a string by removing or replacing invalid filesystem characters
- `IsValidEmail(this string str)` - Validates if a string is a valid email address format
- `IsValidGuid(this string str)` - Validates if a string matches a UUID/GUID format
- `ToPascalCase(this string str)` - Converts a string to PascalCase (first letter uppercase, word boundaries at capitals)
- `ToSnakeCase(this string str)` - Converts a string to snake_case (lowercase with underscores)
- `AfterLast(this string str, string delimiter)` - Extracts the portion of a string after the last occurrence of a delimiter
- `BeforeLast(this string str, string delimiter)` - Extracts the portion of a string before the last occurrence of a delimiter
- `ContainsIgnoreCase(this string str, string value)` - Determines if a string contains another string case-insensitively
- `NormalizeLineEndings(this string str)` - Normalizes line endings to a consistent format (Unix style \n)
- `ToSlug(this string str)` - Generates a slug from a string suitable for URLs or identifiers

### Usage Example

```csharp
using NotionTaskSync.Utils;
using System;

class Program
{
  static void Main()
  {
    // Example 1: Truncate strings for display
    var longTitle = "This is a very long task title that needs to be truncated for display purposes";
    var truncated = longTitle.Truncate(30);
    Console.WriteLine($"Original: {longTitle}");
    Console.WriteLine($"Truncated: {truncated}"); // "This is a very long task tit..."

    // Example 2: Sanitize strings for filenames
    var invalidFilename = "Task #1: Fix / Important 🚨";
    var safeFilename = invalidFilename.SanitizeForFilename();
    Console.WriteLine($"Safe filename: {safeFilename}"); // "Task_1_Fix_Important__.txt"

    // Example 3: Validate email addresses
    var validEmail = "user@example.com";
    var invalidEmail = "not-an-email";
    Console.WriteLine($"Valid email: {validEmail.IsValidEmail()}"); // True
    Console.WriteLine($"Invalid email: {invalidEmail.IsValidEmail()}"); // False

    // Example 4: Validate GUID/UUID strings
    var validGuid = "550e8400-e29b-41d4-a716-446655440000";
    var invalidGuid = "not-a-guid";
    Console.WriteLine($"Valid GUID: {validGuid.IsValidGuid()}"); // True
    Console.WriteLine($"Invalid GUID: {invalidGuid.IsValidGuid()}"); // False

    // Example 5: Convert to PascalCase
    var snakeCaseInput = "user_authentication_token";
    var pascalCase = snakeCaseInput.ToPascalCase();
    Console.WriteLine($"PascalCase: {pascalCase}"); // "UserAuthenticationToken"

    // Example 6: Convert to snake_case
    var pascalCaseInput = "UserAuthenticationToken";
    var snakeCase = pascalCaseInput.ToSnakeCase();
    Console.WriteLine($"snake_case: {snakeCase}"); // "user_authentication_token"

    // Example 7: Extract substring after last delimiter
    var filePath = "/home/user/documents/report.pdf";
    var fileName = filePath.AfterLast("/");
    Console.WriteLine($"Filename: {fileName}"); // "report.pdf"

    // Example 8: Extract substring before last delimiter
    var url = "https://api.example.com/v1/users";
    var baseUrl = url.BeforeLast("/");
    Console.WriteLine($"Base URL: {baseUrl}"); // "https://api.example.com/v1"

    // Example 9: Case-insensitive string search
    var text = "The Quick Brown Fox";
    Console.WriteLine($"Contains 'quick': {text.ContainsIgnoreCase("quick")}"); // True
    Console.WriteLine($"Contains 'FAST': {text.ContainsIgnoreCase("FAST")}"); // False

    // Example 10: Normalize line endings
    var mixedLineEndings = "Line1\r\nLine2\rLine3\nLine4";
    var normalized = mixedLineEndings.NormalizeLineEndings();
    Console.WriteLine($"Normalized line count: {normalized.Split('\n').Length}"); // 4

    // Example 11: Generate URL slugs
    var title = "Implement StringExtensions Documentation";
    var slug = title.ToSlug();
    Console.WriteLine($"Slug: {slug}"); // "implement-stringextensions-documentation"
  }
}
```

## ChangeLogRepository

The `TaskRepository` class provides an in-memory implementation of the `ITaskRepository` interface for managing task entities.
using NotionTaskSync.Domain.Models;
using System;
using System.Threading.Tasks;

class Program
{
    static async Task Main()
    {
        // Initialize TaskRepository
        var taskRepository = new TaskRepository();

        // Example 1: Add a new task
        var newTask = new Task
        {
            Id = Guid.NewGuid(),
            Title = "Implement TaskRepository documentation feature",
            Description = "Add TaskRepository section to README.md with realistic usage examples",
            Status = TaskStatus.InProgress,
            Priority = 75,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            DueDate = DateTime.UtcNow.AddDays(7),
            AssignedTo = "developer@example.com",
            Tags = "documentation,readme,feature"
        };

        await taskRepository.AddAsync(newTask);
        Console.WriteLine($"Added task: {newTask.Title}");

        // Example 2: Retrieve tasks by status
        var inProgressTasks = await taskRepository.GetByStatusAsync(TaskStatus.InProgress);
        Console.WriteLine($"In progress tasks: {inProgressTasks.Count}");

        // Example 3: Retrieve tasks assigned to a specific user
        var assignedTasks = await taskRepository.GetAssignedToAsync("developer@example.com");
        Console.WriteLine($"Tasks assigned to developer@example.com: {assignedTasks.Count}");

        // Example 4: Update a task
        var taskToUpdate = inProgressTasks.FirstOrDefault();
        if (taskToUpdate != null)
        {
            taskToUpdate.Status = TaskStatus.Done;
            taskToUpdate.CompletedAt = DateTime.UtcNow;
            taskToUpdate.UpdateTimestamp();
            
            await taskRepository.UpdateAsync(taskToUpdate);
            Console.WriteLine($"Updated task: {taskToUpdate.Title} to {taskToUpdate.Status}");
        }

        // Example 5: Count tasks by status
        var statusCounts = await taskRepository.CountByStatusAsync();
        foreach (var kvp in statusCounts)
        {
            Console.WriteLine($"{kvp.Key}: {kvp.Value} tasks");
        }

        // Example 6: Get overdue tasks
        var overdueTasks = await taskRepository.GetOverdueAsync(DateTime.UtcNow);
        Console.WriteLine($"Overdue tasks: {overdueTasks.Count}");

        // Example 7: Get all tasks
        var allTasks = await taskRepository.GetAllAsync();
        Console.WriteLine($"Total active tasks: {allTasks.Count}");

        // Example 8: Check for pending changes
        if (taskRepository.HasPendingChanges)
        {
            await taskRepository.SaveAsync();
            Console.WriteLine("Changes saved successfully");
        }

        // Example 9: Get tasks modified since a specific date
        var recentChanges = await taskRepository.GetModifiedSinceAsync(DateTime.UtcNow.AddDays(-1));
        Console.WriteLine($"Tasks modified in last 24 hours: {recentChanges.Count}");

        // Example 10: Delete a task
        if (allTasks.Count > 0)
        {
            await taskRepository.DeleteAsync(allTasks[0].Id);
            Console.WriteLine("Task marked as deleted (soft delete)");
        }

        // Example 11: Get task by Notion page ID
        var taskByNotionId = await taskRepository.GetByNotionPageIdAsync(newTask.NotionPageId);
        Console.WriteLine($"Found task by Notion ID: {taskByNotionId?.Title}");
    }
}
```

## CliArgumentParserValidation

The `CliArgumentParserValidation` static class provides validation utilities for command-line argument parsing results. It validates parsed command structures, option values, argument lists, and error states to ensure CLI commands are properly formed before execution. The validation methods help catch parsing failures, missing required options, invalid option values, and inconsistent error states.

### Public Members

- `Validate(this CliArgumentParser value)` - Validates a parsed command and returns a list of human-readable problems
- `Validate(this ParsedCommand value)` - Validates a parsed command structure and returns validation problems
- `IsValid(this CliArgumentParser value)` - Determines if a parsed command is valid
- `IsValid(this ParsedCommand value)` - Determines if a parsed command structure is valid
- `EnsureValid(this CliArgumentParser value)` - Validates a parsed command and throws if invalid
- `EnsureValid(this ParsedCommand value)` - Validates a parsed command structure and throws if invalid
- `HasRequiredOptions(this IReadOnlyDictionary<string, string> options, params string[] requiredOptionNames)` - Validates that required options are present and non-empty
- `HasAnyRequiredOption(this IReadOnlyDictionary<string, string> options, params string[] requiredOptionNames)` - Validates that at least one of the required options is present and non-empty
- `IsValidOption<T>(this IReadOnlyDictionary<string, string> options, string optionName, Func<string, T> parser, string? validationMessage = null)` - Validates that option values can be parsed as specific types
- `IsValidRangeOption(this IReadOnlyDictionary<string, string> options, string optionName, int minValue, int maxValue, string? validationMessage = null)` - Validates that option values fall within a specific range
- `IsValidDateOption(this IReadOnlyDictionary<string, string> options, string optionName, string? validationMessage = null)` - Validates that option values are valid dates
- `IsValidDateTimeOption(this IReadOnlyDictionary<string, string> options, string optionName, string? validationMessage = null)` - Validates that option values are valid date-time values

### Usage Example

```csharp
using NotionTaskSync.Cli;
using System;
using System.Collections.Generic;

class Program
{
    static void Main()
    {
        // Example 1: Validate a parsed command
        var parser = new CliArgumentParser();
        var parsedCommand = parser.Parse(new[] { "sync", "--database-id", "test-db-123", "--local-path", @"./tasks" });
        
        var validationProblems = parsedCommand.Validate();
        if (validationProblems.Count > 0)
        {
            Console.WriteLine("Validation errors found:");
            foreach (var problem in validationProblems)
            {
                Console.WriteLine($"- {problem}");
            }
        }
        else
        {
            Console.WriteLine("Command is valid!");
        }
        
        // Example 2: Check if command is valid using IsValid
        var isValid = parsedCommand.IsValid();
        Console.WriteLine($"Is command valid: {isValid}");
        
        // Example 3: Ensure command is valid (throws if invalid)
        try
        {
            parsedCommand.EnsureValid();
            Console.WriteLine("Command passed validation!");
        }
        catch (ArgumentException ex)
        {
            Console.WriteLine($"Validation failed: {ex.Message}");
        }
        
        // Example 4: Validate required options
        var options = new Dictionary<string, string>
        {
            {"database-id", "test-db-123"},
            {"local-path", @"./tasks"}
        };
        
        var hasRequired = options.HasRequiredOptions("database-id", "local-path");
        Console.WriteLine($"Has required options: {hasRequired}");
        
        // Example 5: Validate option value types
        var isValidInt = options.IsValidOption("max-retries", int.Parse);
        Console.WriteLine($"Is 'max-retries' a valid integer: {isValidInt}");
        
        // Example 6: Validate option value ranges
        var isInRange = options.IsValidRangeOption("max-retries", 1, 10);
        Console.WriteLine($"Is 'max-retries' in range 1-10: {isInRange}");
        
        // Example 7: Validate date options
        var dateOptions = new Dictionary<string, string>
        {
            {"due-date", "2024-12-31"}
        };
        
        var isValidDate = dateOptions.IsValidDateOption("due-date");
        Console.WriteLine($"Is 'due-date' a valid date: {isValidDate}");
        
        // Example 8: Validate date-time options
        var dateTimeOptions = new Dictionary<string, string>
        {
            {"start-time", "2024-12-31T14:30:00"}
        };
        
        var isValidDateTime = dateTimeOptions.IsValidDateTimeOption("start-time");
        Console.WriteLine($"Is 'start-time' a valid date-time: {isValidDateTime}");
    }
}
```

## CacheKeyBuilder

The `CacheKeyBuilder` class provides a fluent interface for generating consistent and standardized cache keys throughout the application. It ensures cache keys follow a predictable format with a configurable prefix, reducing bugs from inconsistent key generation and making cache invalidation more reliable. The builder supports various entity types and provides both instance methods for dynamic keys and static factory methods for common use cases.

### Public Members

- `CacheKeyBuilder(string prefix = "notion-sync")` - Constructor that initializes the builder with a custom prefix (defaults to "notion-sync")
- `BuildTaskKey(string taskId)` - Builds a cache key for a task by ID
- `BuildDatabaseTasksKey(string databaseId)` - Builds a cache key for all tasks in a database
- `BuildNotionPageKey(string pageId)` - Builds a cache key for a Notion page by ID
- `BuildConfigKey(string configId)` - Builds a cache key for sync configuration
- `BuildStatisticsKey()` - Builds a cache key for sync statistics
- `BuildApiResponseKey(string endpoint, string? query = null)` - Builds a cache key for API responses
- `BuildChangeLogKey(string configId, int days = 30)` - Builds a cache key for change logs
- `BuildRateLimitKey(string service)` - Builds a cache key for rate limit status
- `BuildPatternKey(string entityType)` - Builds a pattern-based cache key for wildcard invalidation
- `ForTask(string taskId)` - Static factory method for task cache keys
- `ForDatabase(string databaseId)` - Static factory method for database task cache keys
- `ForNotionPage(string pageId)` - Static factory method for Notion page cache keys

### Usage Example

```csharp
using NotionTaskSync.Caching;
using System;

class Program
{
    static void Main()
    {
        // Example 1: Create a CacheKeyBuilder with default prefix
        var builder = new CacheKeyBuilder();
        
        // Build cache keys for different entities
        var taskKey = builder.BuildTaskKey("550e8400-e29b-41d4-a716-446655440000");
        var databaseKey = builder.BuildDatabaseTasksKey("880e8400-e29b-41d4-a716-446655440000");
        var pageKey = builder.BuildNotionPageKey("990e8400-e29b-41d4-a716-446655440000");
        var configKey = builder.BuildConfigKey("sync-config-123");
        var statsKey = builder.BuildStatisticsKey();
        var apiKey = builder.BuildApiResponseKey("tasks", "status=completed");
        var changeLogKey = builder.BuildChangeLogKey("config-123", days: 7);
        var rateLimitKey = builder.BuildRateLimitKey("notion-api");
        var patternKey = builder.BuildPatternKey("task:*");
        
        Console.WriteLine("Generated cache keys:");
        Console.WriteLine($"Task: {taskKey}");
        Console.WriteLine($"Database: {databaseKey}");
        Console.WriteLine($"Page: {pageKey}");
        Console.WriteLine($"Config: {configKey}");
        Console.WriteLine($"Statistics: {statsKey}");
        Console.WriteLine($"API Response: {apiKey}");
        Console.WriteLine($"Change Log: {changeLogKey}");
        Console.WriteLine($"Rate Limit: {rateLimitKey}");
        Console.WriteLine($"Pattern: {patternKey}");
        
        // Example 2: Use custom prefix
        var customBuilder = new CacheKeyBuilder("my-custom-app");
        var customTaskKey = customBuilder.BuildTaskKey("task-456");
        Console.WriteLine($"\nCustom prefix task key: {customTaskKey}");
        
        // Example 3: Use static factory methods
        var staticTaskKey = CommonCacheKeys.ForTask("550e8400-e29b-41d4-a716-446655440000");
        var staticDatabaseKey = CommonCacheKeys.ForDatabase("880e8400-e29b-41d4-a716-446655440000");
        var staticPageKey = CommonCacheKeys.ForNotionPage("990e8400-e29b-41d4-a716-446655440000");
        
        Console.WriteLine($"\nStatic factory keys:");
        Console.WriteLine($"Task: {staticTaskKey}");
        Console.WriteLine($"Database: {staticDatabaseKey}");
        Console.WriteLine($"Page: {staticPageKey}");
        
        // Example 4: Use for cache invalidation patterns
        var invalidationPattern = builder.BuildPatternKey("notion:*");
        Console.WriteLine($"\nInvalidation pattern: {invalidationPattern}");
    }
}
```

## ChangeLogRepository

The `ChangeLogRepository` class provides an in-memory implementation for tracking and managing change history during task synchronization workflows. It serves as an audit trail for all modifications, enabling conflict detection, change analysis, and sync history tracking between local storage and Notion databases.

### Public Members

- `AddAsync(ChangeLog changeLog)` - Adds a new change log entry
- `GetByTaskIdAsync(Guid taskId, int limit = 100)` - Retrieves change logs for a specific task
- `GetByDateRangeAsync(DateTime from, DateTime to)` - Retrieves change logs within a date range
- `GetBySourceAsync(ChangeSource source)` - Retrieves change logs from a specific source
- `GetByChangeTypeAsync(string changeType)` - Retrieves change logs of a specific type
- `GetConflictChangesAsync()` - Retrieves all conflict change logs
- `GetLatestAsync(int limit = 50)` - Retrieves the most recent change logs
- `CountAsync()` - Returns the total number of change logs
- `CountConflictsAsync()` - Returns the number of conflict change logs
- `SaveAsync()` - Persists changes (in-memory implementation)
- `GetFullAuditTrailAsync(Guid taskId)` - Retrieves complete change history for a task
- `GetStatsAsync()` - Returns summary statistics about change logs
- `HasPendingChanges` - Property indicating if there are unsaved changes

### Usage Example

```csharp
using NotionTaskSync.Data.Repositories;
using NotionTaskSync.Domain.Models;
using System;
using System.Threading.Tasks;

class Program
{
    static async Task Main()
    {
        // Initialize ChangeLogRepository
        var changeLogRepository = new ChangeLogRepository();

        // Example 1: Add change log entries
        var localChange = new ChangeLog
        {
            TaskId = Guid.NewGuid(),
            Timestamp = DateTime.UtcNow,
            Source = ChangeSource.Local,
            ChangeType = "Created",
            PropertyName = "Title",
            OldValue = null,
            NewValue = "Implement ChangeLogRepository feature",
            IsConflict = false
        };

        await changeLogRepository.AddAsync(localChange);

        var notionChange = new ChangeLog
        {
            TaskId = Guid.NewGuid(),
            Timestamp = DateTime.UtcNow.AddMinutes(5),
            Source = ChangeSource.Notion,
            ChangeType = "Updated",
            PropertyName = "Status",
            OldValue = "Todo",
            NewValue = "InProgress",
            IsConflict = false
        };

        await changeLogRepository.AddAsync(notionChange);

        // Example 2: Query change logs by task
        var taskChanges = await changeLogRepository.GetByTaskIdAsync(localChange.TaskId);
        Console.WriteLine($"Found {taskChanges.Count} changes for task");

        foreach (var change in taskChanges)
        {
            Console.WriteLine($"- {change.ChangeType} at {change.Timestamp:u}: {change.PropertyName}");
        }

        // Example 3: Get conflict changes
        var conflictChanges = await changeLogRepository.GetConflictChangesAsync();
        Console.WriteLine($"Total conflicts detected: {conflictChanges.Count}");

        // Example 4: Get statistics
        var stats = await changeLogRepository.GetStatsAsync();
        Console.WriteLine($"\nChange statistics:");
        Console.WriteLine($"- Total changes: {stats.TotalChanges}");
        Console.WriteLine($"- Local changes: {stats.LocalChanges}");
        Console.WriteLine($"- Notion changes: {stats.NotionChanges}");
        Console.WriteLine($"- Conflicts: {stats.ConflictCount}");
        Console.WriteLine($"- Created: {stats.CreatedCount}");
        Console.WriteLine($"- Updated: {stats.UpdatedCount}");

        // Example 5: Get full audit trail for a task
        var auditTrail = await changeLogRepository.GetFullAuditTrailAsync(localChange.TaskId);
        Console.WriteLine($"\nFull audit trail has {auditTrail.Count} entries");

        // Example 6: Get changes by source
        var localChanges = await changeLogRepository.GetBySourceAsync(ChangeSource.Local);
        Console.WriteLine($"Local changes: {localChanges.Count}");

        var notionChanges = await changeLogRepository.GetBySourceAsync(ChangeSource.Notion);
        Console.WriteLine($"Notion changes: {notionChanges.Count}");

        // Example 7: Get changes by date range
        var recentChanges = await changeLogRepository.GetByDateRangeAsync(
            DateTime.UtcNow.AddHours(-1),
            DateTime.UtcNow
        );
        Console.WriteLine($"Recent changes in last hour: {recentChanges.Count}");

        // Example 8: Get changes by type
        var createdChanges = await changeLogRepository.GetByChangeTypeAsync("Created");
        Console.WriteLine($"Created changes: {createdChanges.Count}");

        var updatedChanges = await changeLogRepository.GetByChangeTypeAsync("Updated");
        Console.WriteLine($"Updated changes: {updatedChanges.Count}");

        // Example 9: Get latest changes
        var latestChanges = await changeLogRepository.GetLatestAsync(10);
        Console.WriteLine($"Latest 10 changes: {latestChanges.Count}");

        // Example 10: Check for pending changes
        var hasPending = changeLogRepository.HasPendingChanges;
        Console.WriteLine($"\nHas pending changes: {hasPending}");
    }
}
```

## AdvancedUsage

The `AdvancedUsage` class demonstrates advanced configuration patterns, custom options, and comprehensive error handling for task synchronization workflows. It includes examples for:

- Creating fully configured sync setups with custom field mappings and per-field conflict resolution
- Setting up services with custom logging configuration
- Implementing comprehensive error handling and retry logic
- Using the IOptions pattern for strongly-typed configuration
- Conditional sync based on change detection

### Usage Examples

#### Basic Advanced Usage

```csharp
using NotionTaskSync.Services;
using NotionTaskSync.Domain.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

class Program
{
    static async Task Main()
    {
        // Create advanced configuration with custom settings
        var config = new SyncConfig(
            name: "TeamProjectSync",
            notionDatabaseId: "your_team_database_id_here",
            localFolderPath: "./team-tasks"
        )
        {
            Direction = SyncDirection.NotionToLocal,
            ConflictStrategy = ConflictResolutionStrategy.LocalWins,
            SyncIntervalSeconds = 300, // 5 minutes
            IsEnabled = true
        };

        // Configure field mappings (local field name → Notion property name)
        config.FieldMappings = new Dictionary<string, string>
        {
            {"title", "Title"},
            {"status", "Status"},
            {"priority", "Priority"},
            {"dueDate", "Due Date"}
        };

        // Configure per-field conflict resolution overrides
        config.FieldConflictStrategies = new Dictionary<string, ConflictResolutionStrategy>
        {
            {"description", ConflictResolutionStrategy.LocalWins},
            {"status", ConflictResolutionStrategy.NotionWins}
        };

        // Set up services with custom logging
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddConsole());
        services.AddApplicationServices(configuration); // Your configuration
        
        var serviceProvider = services.BuildServiceProvider();
        var syncService = serviceProvider.GetRequiredService<SyncService>();

        // Execute sync with error handling
        try
        {
            var result = await syncService.ExecuteSyncAsync(config);
            Console.WriteLine($"Sync completed: {result.SyncedCount} tasks synced");
        }
        catch (NotionApiException ex) when (ex.StatusCode == 429)
        {
            Console.WriteLine("Rate limited! Consider increasing SyncIntervalSeconds");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Sync failed: {ex.Message}");
        }
    }
}
```

#### Using IOptions Pattern

```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NotionTaskSync.Infrastructure.Configuration;
using NotionTaskSync.Services;

class Program
{
    static async Task Main()
    {
        var services = new ServiceCollection();

        // Configure strongly-typed settings
        services.Configure<NotionApiSettings>(options =>
        {
            options.ApiKey = "your_api_key_here";
            options.DatabaseId = "your_db_id_here";
            options.RateLimitPerSecond = 3;
        });

        services.AddLogging(builder => builder.AddConsole());
        services.AddApplicationServices(new ConfigurationBuilder().Build());

        var serviceProvider = services.BuildServiceProvider();
        
        // Resolve IOptions directly
        var notionApiOptions = serviceProvider.GetRequiredService<IOptions<NotionApiSettings>>();
        var syncService = serviceProvider.GetRequiredService<SyncService>();

        var config = new SyncConfig(
            "OptionsPatternSync",
            notionApiOptions.Value.DatabaseId,
            "./tasks"
        );

        var result = await syncService.ExecuteSyncAsync(config);
    }
}
```

#### Conditional Sync Based on Changes

```csharp
using NotionTaskSync.Services;
using NotionTaskSync.Domain.Models;

class Program
{
    static async Task Main()
    {
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddConsole());
        services.AddApplicationServices(new ConfigurationBuilder().Build());
        
        var serviceProvider = services.BuildServiceProvider();
        var syncService = serviceProvider.GetRequiredService<SyncService>();
        var changeDetection = serviceProvider.GetRequiredService<ChangeDetectionService>();

        var config = new SyncConfig("ConditionalSync", "test-db-id", "./tasks");

        // Detect changes before syncing
        var localTasks = await changeDetection.LoadLocalTasksAsync(config.LocalFolderPath);
        var notionPages = await changeDetection.LoadNotionPagesAsync(config.NotionDatabaseId);
        
        var localChanges = changeDetection.DetectLocalChanges(localTasks, DateTime.UtcNow.AddDays(-1));
        var notionChanges = changeDetection.DetectNotionChanges(notionPages, DateTime.UtcNow.AddDays(-1));

        // Only sync if changes detected
        if (localChanges.Count > 0 || notionChanges.Count > 0)
        {
            var result = await syncService.ExecuteSyncAsync(config);
            Console.WriteLine($"Synced {result.SyncedCount} tasks");
        }
        else
        {
            Console.WriteLine("No changes detected - skipping sync");
        }
    }
}
```

## TaskMapper

The `TaskMapper` static class provides bidirectional mapping between `Task` domain entities and their Notion representations, as well as DTO conversion for API responses. It handles serialization and deserialization of task data, enabling seamless integration between local storage and Notion databases.

### Public Members

- `MapFromNotionPage(NotionPage page)` - Creates a new `Task` entity from a Notion page
- `UpdateTaskFromPage(Task task, NotionPage page)` - Updates an existing `Task` entity from a Notion page
- `MapToNotionPage(Task task, string databaseId)` - Converts a `Task` entity to a Notion page representation
- `MapToDto(Task task)` - Creates a DTO representation of a task for API responses

### Usage Example

```csharp
using NotionTaskSync.Data.Mappers;
using NotionTaskSync.Domain.Models;
using System;

class Program
{
    static void Main()
    {
        // Example 1: Map from Notion page to Task entity
        var notionPage = new NotionPage(
            pageId: "550e8400-e29b-41d4-a716-446655440000",
            databaseId: "880e8400-e29b-41d4-a716-446655440000",
            title: "Implement TaskMapper documentation"
        );
        
        notionPage.Properties = new()
        {
            ["Description"] = "Add TaskMapper section to README.md",
            ["Status"] = "InProgress",
            ["Priority"] = "75",
            ["DueDate"] = DateTime.UtcNow.AddDays(7).ToString("O"),
            ["AssignedTo"] = "developer@example.com",
            ["Tags"] = "documentation,mappers"
        };
        
        var taskFromNotion = TaskMapper.MapFromNotionPage(notionPage);
        Console.WriteLine($"Mapped from Notion: {taskFromNotion.Title}");
        Console.WriteLine($"Status: {taskFromNotion.Status}");
        Console.WriteLine($"Priority: {taskFromNotion.Priority}");
        
        // Example 2: Update existing task from Notion page
        var existingTask = new Task
        {
            Id = Guid.NewGuid(),
            Title = "Original task title",
            Description = "Original description",
            Status = TaskStatus.Todo,
            Priority = 50,
            CreatedAt = DateTime.UtcNow.AddDays(-1),
            UpdatedAt = DateTime.UtcNow.AddDays(-1),
            DueDate = DateTime.UtcNow.AddDays(14),
            AssignedTo = "old@example.com",
            NotionPageId = notionPage.PageId,
            IsDeleted = false
        };
        
        TaskMapper.UpdateTaskFromPage(existingTask, notionPage);
        Console.WriteLine($"\nUpdated task: {existingTask.Title}");
        Console.WriteLine($"New status: {existingTask.Status}");
        Console.WriteLine($"New priority: {existingTask.Priority}");
        
        // Example 3: Map Task entity to Notion page
        var notionPageFromTask = TaskMapper.MapToNotionPage(existingTask, "880e8400-e29b-41d4-a716-446655440000");
        Console.WriteLine($"\nMapped to Notion page: {notionPageFromTask.Title}");
        Console.WriteLine($"Database ID: {notionPageFromTask.DatabaseId}");
        Console.WriteLine($"Properties count: {notionPageFromTask.Properties?.Count}");
        
        // Example 4: Map Task entity to DTO
        var taskDto = TaskMapper.MapToDto(existingTask);
        Console.WriteLine($"\nMapped to DTO:");
        Console.WriteLine($"ID: {taskDto.Id}");
        Console.WriteLine($"Title: {taskDto.Title}");
        Console.WriteLine($"Status: {taskDto.Status}");
        Console.WriteLine($"Priority: {taskDto.Priority}");
        Console.WriteLine($"Created: {taskDto.CreatedAt:u}");
        Console.WriteLine($"Updated: {taskDto.UpdatedAt:u}");
        Console.WriteLine($"Due: {taskDto.DueDate?.ToString("yyyy-MM-dd")}");
        Console.WriteLine($"Assigned: {taskDto.AssignedTo}");
        Console.WriteLine($"Notion Page ID: {taskDto.NotionPageId}");
        Console.WriteLine($"Is Deleted: {taskDto.IsDeleted}");
    }
}
```

## SyncService

The `SyncService` class orchestrates bidirectional synchronization between Notion databases and local task storage. It coordinates change detection, conflict resolution, and data propagation across both systems according to the configured sync direction and conflict resolution strategy.

### Public Members

- `SyncService(...)` - Constructor that initializes the service with all required dependencies
- `public async Task<SyncResult> ExecuteSyncAsync(SyncConfig config)` - Executes a full bidirectional sync for a given configuration
- `public async Task<List<SyncResult>> GetSyncHistoryAsync(Guid configId, int limit = 50)` - Retrieves sync history for a configuration
- `public sealed class SyncResult` - Contains the results and statistics from a sync operation with properties:
  - `ConfigId` (Guid)
  - `StartedAt` (DateTime)
  - `CompletedAt` (DateTime?)
  - `Status` (SyncStatus)
  - `LocalTaskCount` (int)
  - `NotionPageCount` (int)
  - `LocalChangesDetected` (int)
  - `NotionChangesDetected` (int)
  - `ConflictsDetected` (int)
  - `ConflictsResolved` (int)
  - `ConflictsPendingReview` (int)
  - `Created` (int)
  - `Updated` (int)
  - `Deleted` (int)
  - `Unchanged` (int)
  - `ErrorMessage` (string?)
  - `ErrorDetails` (string?)

### Usage Example

```csharp
using NotionTaskSync.Services;
using NotionTaskSync.Domain.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

class Program
{
    static async Task Main()
    {
        // Setup dependency injection
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddConsole());
        services.AddApplicationServices(new ConfigurationBuilder().Build());

        var serviceProvider = services.BuildServiceProvider();
        var syncService = serviceProvider.GetRequiredService<SyncService>();

        // Create sync configuration
        var config = new SyncConfig(
            "DailyTeamSync",
            "550e8400-e29b-41d4-a716-446655440000", // Notion database ID
            @"./tasks" // Local folder path
        );

        // Configure sync direction and strategy
        config.Direction = SyncDirection.Bidirectional;
        config.ConflictStrategy = ConflictResolutionStrategy.LocalWins;
        config.SyncIntervalSeconds = 300; // 5 minutes
        config.IsEnabled = true;

        // Execute sync with error handling
        try
        {
            var result = await syncService.ExecuteSyncAsync(config);

            Console.WriteLine($"Sync completed: {result.Status}");
            Console.WriteLine($"Duration: {result.Duration?.TotalSeconds:F1} seconds");
            Console.WriteLine($"Tasks processed: {result.LocalTaskCount} local, {result.NotionPageCount} Notion");
            Console.WriteLine($"Changes: {result.LocalChangesDetected} local, {result.NotionChangesDetected} Notion");
            Console.WriteLine($"Operations: {result.Created} created, {result.Updated} updated, {result.Deleted} deleted");
            Console.WriteLine($"Conflicts: {result.ConflictsDetected} detected, {result.ConflictsResolved} resolved");

            if (result.Status == SyncStatus.Failed)
            {
                Console.WriteLine($"Error: {result.ErrorMessage}");
            }
        }
        catch (ConfigurationException ex)
        {
            Console.WriteLine($"Configuration error: {ex.Message}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Sync failed: {ex.Message}");
        }
    }
}
```

## DateTimeExtensions

The `DateTimeExtensions` static class provides extension methods for DateTime operations commonly used in synchronization scenarios. It centralizes recurring patterns for timestamp comparisons, timezone handling, and formatting, improving code readability and reducing duplication across the codebase.

### Public Members

- `IsWithinDays(this DateTime dateTime, int days)` - Determines if a timestamp falls within a specified number of days from now
- `IsNewerThan(this DateTime dateTime, DateTime comparison)` - Determines if a timestamp is more recent than another timestamp
- `RoundToMinute(this DateTime dateTime)` - Rounds a DateTime to the nearest minute boundary
- `RoundToSecond(this DateTime dateTime)` - Rounds a DateTime to the nearest second boundary
- `ToIso8601String(this DateTime dateTime)` - Converts a DateTime to ISO 8601 format string
- `ToUserFriendlyString(this DateTime dateTime)` - Converts a DateTime to a human-readable format suitable for logging and display
- `ToTimeAgoString(this DateTime dateTime)` - Calculates the time elapsed since the given timestamp in a human-readable format
- `IsSameDay(this DateTime dateTime, DateTime other)` - Determines if two DateTime objects represent the same day in UTC
- `ToUnixTimestamp(this DateTime dateTime)` - Converts a DateTime to a Unix timestamp (seconds since epoch)
- `FromUnixTimestamp(this long unixTimestamp)` - Converts a Unix timestamp to a DateTime object
- `IsStale(this DateTime dateTime, int maxAgeHours)` - Determines if a timestamp is stale based on a maximum age in hours
- `GetStartOfDay(this DateTime dateTime)` - Gets the start of the day (00:00:00) for a given DateTime
- `GetEndOfDay(this DateTime dateTime)` - Gets the end of the day (23:59:59.999...) for a given DateTime

### Usage Example

```csharp
using NotionTaskSync.Utils;
using System;

class Program
{
    static void Main()
    {
        // Example 1: Check if timestamp is within recent days
        var recentDate = DateTime.UtcNow.AddHours(-6);
        var isWithinLastDay = recentDate.IsWithinDays(1);
        Console.WriteLine($"Is within last 24 hours: {isWithinLastDay}"); // True
        
        var oldDate = DateTime.UtcNow.AddDays(-30);
        var isWithinLastMonth = oldDate.IsWithinDays(30);
        Console.WriteLine($"Is within last 30 days: {isWithinLastMonth}"); // True
        
        // Example 2: Compare timestamps for recency
        var taskUpdated = DateTime.UtcNow.AddMinutes(-15);
        var lastSync = DateTime.UtcNow.AddHours(-2);
        var hasRecentChanges = taskUpdated.IsNewerThan(lastSync);
        Console.WriteLine($"Has recent changes: {hasRecentChanges}"); // True
        
        // Example 3: Round timestamps for consistent comparison
        var preciseTime = DateTime.UtcNow.AddMilliseconds(456);
        var roundedMinute = preciseTime.RoundToMinute();
        var roundedSecond = preciseTime.RoundToSecond();
        Console.WriteLine($"Original: {preciseTime:O}");
        Console.WriteLine($"Rounded to minute: {roundedMinute:O}");
        Console.WriteLine($"Rounded to second: {roundedSecond:O}");
        
        // Example 4: Format timestamps for different use cases
        var now = DateTime.UtcNow;
        Console.WriteLine($"ISO 8601: {now.ToIso8601String()}");
        Console.WriteLine($"User friendly: {now.ToUserFriendlyString()}");
        Console.WriteLine($"Time ago: {now.ToTimeAgoString()}");
        
        // Example 5: Date-based comparisons
        var morning = DateTime.UtcNow.Date.AddHours(9);
        var evening = DateTime.UtcNow.Date.AddHours(21);
        var isSameDay = morning.IsSameDay(evening);
        Console.WriteLine($"Same day: {isSameDay}"); // True
        
        // Example 6: Unix timestamp conversions
        var timestamp = DateTime.UtcNow.ToUnixTimestamp();
        Console.WriteLine($"Unix timestamp: {timestamp}");
        var convertedBack = timestamp.FromUnixTimestamp();
        Console.WriteLine($"Converted back: {convertedBack:O}");
        
        // Example 7: Check for stale data
        var recentTask = DateTime.UtcNow.AddMinutes(-30);
        var isStale = recentTask.IsStale(maxAgeHours: 1);
        Console.WriteLine($"Is stale (1 hour threshold): {isStale}"); // False
        
        var oldTask = DateTime.UtcNow.AddHours(-25);
        var isOldStale = oldTask.IsStale(maxAgeHours: 24);
        Console.WriteLine($"Is stale (24 hour threshold): {isOldStale}"); // True
        
        // Example 8: Get day boundaries for range queries
        var today = DateTime.UtcNow;
        var startOfDay = today.GetStartOfDay();
        var endOfDay = today.GetEndOfDay();
        Console.WriteLine($"Start of day: {startOfDay:O}");
        Console.WriteLine($"End of day: {endOfDay:O}");
    }
}
```

## CryptoHelper

The `CryptoHelper` static class provides cryptographic utilities for hashing, encryption, and data integrity verification. It's designed for secure handling of sensitive data like API keys and authentication tokens, with constant-time comparison to prevent timing attacks.

### Public Members

- `HashSha256(string input)` - Computes a SHA256 hash of a string for fingerprinting and verification
- `HashMd5(string input)` - Computes an MD5 hash of a string for checksums (less secure than SHA256)
- `GenerateRandomToken(int length = 32)` - Generates a cryptographically secure random token for authentication
- `VerifyHashSha256(string plaintext, string hash)` - Verifies if a plaintext matches a SHA256 hash
- `ComputeHmacSha256(string data, string key)` - Computes HMAC-SHA256 signature of data with a key for data integrity
- `VerifyHmacSha256(string data, string signature, string key)` - Verifies an HMAC-SHA256 signature to detect tampering
- `GenerateFingerprint(string content)` - Generates a cryptographic fingerprint of content for change detection

### Usage Example

```csharp
using NotionTaskSync.Utils;
using System;

class Program
{
    static void Main()
    {
        // Example 1: Generate secure random tokens for authentication
        var apiToken = CryptoHelper.GenerateRandomToken();
        var sessionToken = CryptoHelper.GenerateRandomToken(64);
        Console.WriteLine($"API Token: {apiToken}");
        Console.WriteLine($"Session Token: {sessionToken}");

        // Example 2: Hash passwords for secure storage
        var password = "user-secret-password-123";
        var passwordHash = CryptoHelper.HashSha256(password);
        Console.WriteLine($"Password hash: {passwordHash}");

        // Example 3: Verify password without storing plain text
        var isPasswordValid = CryptoHelper.VerifyHashSha256(password, passwordHash);
        Console.WriteLine($"Password valid: {isPasswordValid}");

        // Example 4: Generate checksums for data integrity
        var fileContent = "Important configuration data";
        var checksum = CryptoHelper.HashMd5(fileContent);
        Console.WriteLine($"MD5 checksum: {checksum}");

        // Example 5: Create HMAC signatures for API requests
        var secretKey = "my-secret-api-key";
        var requestData = "{\"userId\": 123, \"action\": \"update\"}";
        var signature = CryptoHelper.ComputeHmacSha256(requestData, secretKey);
        Console.WriteLine($"Request signature: {signature}");

        // Example 6: Verify API request integrity
        var isSignatureValid = CryptoHelper.VerifyHmacSha256(requestData, signature, secretKey);
        Console.WriteLine($"Signature valid: {isSignatureValid}");

        // Example 7: Generate fingerprints for change detection
        var document = "This is the document content that needs fingerprinting";
        var fingerprint = CryptoHelper.GenerateFingerprint(document);
        Console.WriteLine($"Document fingerprint: {fingerprint}");

        // Example 8: Use tokens in a realistic scenario
        var userRegistrationToken = CryptoHelper.GenerateRandomToken(48);
        Console.WriteLine($"\nUser registration workflow:");
        Console.WriteLine($"1. Generated secure token: {userRegistrationToken.Substring(0, 16)}...");
        Console.WriteLine($"2. Stored hash only: {CryptoHelper.HashSha256(userRegistrationToken).Substring(0, 16)}...");
        Console.WriteLine($"3. Can verify without storing plain token");
    }
}
```

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

## LoggerFactoryExtensions

The `LoggerFactoryExtensions` static class provides extension methods for `ILoggerFactory` that enable file-based logging operations with automatic log rotation and cleanup. These extensions help manage log file directories, rotate logs when they reach maximum size, and clean up old log files based on retention policies, ensuring that logging infrastructure remains reliable and doesn't consume excessive disk space.

### Public Members

- `EnsureLogDirectoryExists(this ILoggerFactory factory, string logFilePath)` - Ensures the directory for log files exists, creating it if necessary
- `RotateAndCleanupLogs(this ILoggerFactory factory, string logFilePath, long maxSizeBytes = 10485760, int retentionDays = 30)` - Rotates the log file if it exceeds the specified maximum size and cleans up old logs
- `RotateLogFile(this ILoggerFactory factory, string logFilePath, long maxSizeBytes = 10485760)` - Rotates the log file if it exceeds the specified maximum size, creating a timestamped archive
- `CleanupOldLogs(this ILoggerFactory factory, string logFilePath, int retentionDays = 30)` - Cleans up old log files based on retention policy
- `ValidateLogPath(this ILoggerFactory factory, string logFilePath)` - Validates that the log directory is accessible and writable

### Usage Example

```csharp
using Microsoft.Extensions.Logging;
using NotionTaskSync.Infrastructure.Logging;
using System;
using System.IO;

class Program
{
    static void Main()
    {
        // Setup logger factory with file logging
        var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder
                .AddConsole()
                .AddFile("logs/app-logs.txt"); // Requires Microsoft.Extensions.Logging.File extension
        });

        // Example 1: Ensure log directory exists
        var logFilePath = @"./logs/application.log";
        loggerFactory.EnsureLogDirectoryExists(logFilePath);
        Console.WriteLine("Log directory ensured");

        // Example 2: Validate log path before writing
        var isPathValid = loggerFactory.ValidateLogPath(logFilePath);
        Console.WriteLine($"Log path valid: {isPathValid}");

        // Example 3: Rotate and cleanup logs (typically called during application startup)
        loggerFactory.RotateAndCleanupLogs(logFilePath, maxSizeBytes: 5 * 1024 * 1024, retentionDays: 14);
        Console.WriteLine("Log rotation and cleanup completed");

        // Example 4: Manual log rotation when file gets too large
        var logFileInfo = new FileInfo(logFilePath);
        if (logFileInfo.Exists && logFileInfo.Length > 10 * 1024 * 1024) // 10MB
        {
            loggerFactory.RotateLogFile(logFilePath, maxSizeBytes: 10 * 1024 * 1024);
            Console.WriteLine("Log file rotated due to size limit");
        }

        // Example 5: Manual cleanup of old logs
        loggerFactory.CleanupOldLogs(logFilePath, retentionDays: 7);
        Console.WriteLine("Old logs cleaned up");

        // Get logger and use it
        var logger = loggerFactory.CreateLogger<Program>();
        logger.LogInformation("LoggerFactoryExtensions initialized successfully");
    }
}
```

## LoggingMiddleware

The `LoggingMiddleware` class provides structured logging for all synchronization operations, enabling observability and debugging. It wraps operations with timing, status logging, and error tracking, creating consistent log entries that can be analyzed for performance monitoring and error diagnosis. The middleware supports both synchronous and asynchronous operations with detailed logging at different verbosity levels.

### Usage Example

```csharp
using NotionTaskSync.Middleware;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

class Program
{
    static async Task Main()
    {
        // Setup logger
        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        var logger = loggerFactory.CreateLogger<LoggingMiddleware>();

        // Create middleware instance
        var loggingMiddleware = new LoggingMiddleware(logger);

        // Example 1: Async operation with logging
        var asyncResult = await loggingMiddleware.ExecuteWithLoggingAsync("SyncTaskOperation", async () =>
        {
            // Simulate async work
            await Task.Delay(100);
            return new { TasksSynced = 42, Status = "Success" };
        });

        Console.WriteLine($"Async operation result: {asyncResult.TasksSynced} tasks synced");

        // Example 2: Sync operation with logging
        var syncResult = loggingMiddleware.ExecuteWithLogging("LocalFileOperation", () =>
        {
            // Simulate sync work
            return "File sync completed successfully";
        });

        Console.WriteLine($"Sync operation result: {syncResult}");

        // Example 3: Structured sync operation logging
        loggingMiddleware.LogSyncOperation(
            operationName: "FullDatabaseSync",
            status: "Completed",
            itemCount: 150,
            changeCount: 25,
            duration: TimeSpan.FromSeconds(45)
        );

        // Example 4: Log warning for slow operation
        loggingMiddleware.LogWarning(
            "DatabaseSync",
            "High conflict count detected: {ConflictCount} conflicts",
            15
        );

        // Example 5: Log debug information (only visible with Verbose logging)
        loggingMiddleware.LogDebug(
            "ConflictResolution",
            "Conflict resolution strategy applied: {Strategy}",
            "LocalWins"
        );

        // Example 6: Error handling with logging
        try
        {
            await loggingMiddleware.ExecuteWithLoggingAsync("RiskyOperation", async () =>
            {
                throw new InvalidOperationException("Simulated failure");
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Caught exception (already logged by middleware): {ex.Message}");
        }
    }
}
```

## ErrorHandlingMiddleware

The `ErrorHandlingMiddleware` class provides robust exception handling and error management for task synchronization workflows. It wraps operations to catch exceptions, log them appropriately, and return structured error information instead of throwing. This middleware supports both synchronous and asynchronous operations, maps exceptions to appropriate HTTP status codes, and determines if errors are retryable.

### Usage Example

```csharp
using NotionTaskSync.Middleware;
using NotionTaskSync.Domain.Exceptions;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

class Program
{
    static async Task Main()
    {
        // Setup logger
        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        var logger = loggerFactory.CreateLogger<ErrorHandlingMiddleware>();

        // Create middleware instance
        var errorHandler = new ErrorHandlingMiddleware(logger);

        // Example 1: Handle async operation with error checking
        var syncResult = await errorHandler.TryExecuteAsync("SyncTaskOperation", async () =>
        {
            // Simulate a sync operation that might fail
            if (DateTime.UtcNow.Second % 3 == 0)
            {
                throw new SyncException("Failed to sync with Notion API: rate limit exceeded");
            }

            return new { TasksSynced = 42, Status = "Success" };
        });

        if (syncResult.success)
        {
            Console.WriteLine($"Operation succeeded: {syncResult.result}");
        }
        else
        {
            Console.WriteLine($"Operation failed: {syncResult.error}");

            // Get appropriate status code for the error
            var statusCode = errorHandler.GetStatusCode(new SyncException(syncResult.error!));
            Console.WriteLine($"HTTP Status Code: {statusCode}");

            // Check if error is retryable
            var isRetryable = errorHandler.IsRetryable(new SyncException(syncResult.error!));
            Console.WriteLine($"Is retryable: {isRetryable}");
        }
    }
}
```

## RateLimitingMiddleware

The `RateLimitingMiddleware` class enforces rate limiting to prevent exceeding API quotas during task synchronization. It tracks API calls within a 60-second sliding window and automatically delays requests when approaching configured rate limits. The middleware supports both synchronous and asynchronous operations, and can process API-provided rate limit headers for intelligent backoff.

### Usage Example

```csharp
using NotionTaskSync.Middleware;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

class Program
{
    static async Task Main()
    {
        // Setup logger
        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        var logger = loggerFactory.CreateLogger<RateLimitingMiddleware>();

        // Create rate limiting middleware with 30 requests per minute limit
        var rateLimiter = new RateLimitingMiddleware(
            requestsPerMinute: 30,
            eventBus: new EventBus(),
            logger: logger
        );

        // Example 1: Async operation with automatic rate limiting
        var asyncResult = await rateLimiter.ExecuteWithRateLimitAsync(async () =>
        {
            // Simulate API call
            await Task.Delay(50);
            return new { Status = "API call successful", Data = "response data" };
        }, "NotionAPI");

        Console.WriteLine($"Async operation result: {asyncResult.Status}");
        Console.WriteLine($"Rate limit status: {rateLimiter.GetStatus().RequestsUsed} used, {rateLimiter.GetStatus().RequestsRemaining} remaining");

        // Example 2: Sync operation with rate limiting
        var syncResult = rateLimiter.ExecuteWithRateLimit(() =>
        {
            // Simulate local operation
            return "Local operation completed";
        }, "LocalService");

        Console.WriteLine($"Sync operation result: {syncResult}");

        // Example 3: Process API rate limit headers
        rateLimiter.ProcessRateLimitHeader(
            remainingRequests: 5,
            resetEpoch: (int)DateTimeOffset.UtcNow.AddMinutes(1).ToUnixTimeSeconds(),
            apiService: "NotionAPI"
        );

        // Example 4: Check current rate limit status
        var status = rateLimiter.GetStatus();
        Console.WriteLine($"Rate limit: {status.RequestsUsed}/{status.LimitPerMinute} used");
        Console.WriteLine($"Reset at: {status.WindowResetAt:T}");
        Console.WriteLine($"Critical: {status.IsCritical}, High: {status.IsHigh}");

        // Example 5: Reset rate limiter counters
        rateLimiter.Reset();
        Console.WriteLine($"Rate limiter reset. New window started.");
    }
}
```

### Usage Example

```csharp
using NotionTaskSync.Middleware;
using NotionTaskSync.Domain.Exceptions;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

class Program
{
    static async Task Main()
    {
        // Setup logger
        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        var logger = loggerFactory.CreateLogger<ErrorHandlingMiddleware>();
        
        // Create middleware instance
        var errorHandler = new ErrorHandlingMiddleware(logger);

        // Example 1: Handle async operation with error checking
        var syncResult = await errorHandler.TryExecuteAsync("SyncTaskOperation", async () =>
        {
            // Simulate a sync operation that might fail
            if (DateTime.UtcNow.Second % 3 == 0)
            {
                throw new SyncException("Failed to sync with Notion API: rate limit exceeded");
            }
            
            return new { TasksSynced = 42, Status = "Success" };
        });

        if (syncResult.success)
        {
            Console.WriteLine($"Operation succeeded: {syncResult.result}");
        }
        else
        {
            Console.WriteLine($"Operation failed: {syncResult.error}");
            
            // Get appropriate status code for the error
            var statusCode = errorHandler.GetStatusCode(new SyncException(syncResult.error!));
            Console.WriteLine($"HTTP Status Code: {statusCode}");
            
            // Check if error is retryable
            var isRetryable = errorHandler.IsRetryable(new SyncException(syncResult.error!));
            Console.WriteLine($"Is retryable: {isRetryable}");
        }

        // Example 2: Handle sync operation with retry logic
        var retryResult = await errorHandler.TryExecuteAsync("SyncWithRetry", async () =>
        {
            // Your sync operation here
            await Task.Delay(100);
            return "Sync completed successfully";
        });

        Console.WriteLine(retryResult.success ? "Success!" : "Failed: " + retryResult.error);

        // Example 3: Format user-friendly error messages
        try
        {
            throw new ConfigurationException("Missing Notion API key in configuration");
        }
        catch (Exception ex)
        {
            var friendlyMessage = errorHandler.FormatErrorMessage(ex);
            Console.WriteLine($"User-friendly error: {friendlyMessage}");
            
            var statusCode = errorHandler.GetStatusCode(ex);
            Console.WriteLine($"Status code for this error: {statusCode}");
        }

        // Example 4: Synchronous operation with error handling
        var syncOperation = errorHandler.TryExecute("ValidateConfiguration", () =>
        {
            // Validate configuration synchronously
            if (string.IsNullOrEmpty("notion-api-key"))
            {
                throw new ConfigurationException("API key cannot be empty");
            }
            return true;
        });

        Console.WriteLine(syncOperation.success 
            ? "Configuration is valid"
            : "Configuration error: " + syncOperation.error);

        // Example 5: Execute operation and throw on error
        try
        {
            await errorHandler.ExecuteAsync("CriticalSyncOperation", async () =>
            {
                // Critical operation that should throw on failure
                await Task.Delay(50);
                if (DateTime.UtcNow.Second % 5 == 0)
                {
                    throw new InvalidOperationException("Cannot proceed with invalid state");
                }
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Caught exception: {errorHandler.FormatErrorMessage(ex)}");
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

## ConflictDiffService

The `ConflictDiffService` class generates structured diff previews for conflicting task property values using an LCS-based algorithm. It compares local and Notion values line by line and renders results as unified-diff text for terminal or log output. This service is designed to be called before a resolution strategy is applied so operators can review exactly what changed on each side before committing to a winner.

### Public Members

- `ConflictDiffService(ILogger<ConflictDiffService> logger)` - Constructor that initializes the service with a logger
- `GenerateDiffAsync(ConflictResolution conflict, CancellationToken cancellationToken)` - Generates a diff for a conflict resolution
- `GenerateDiffForPropertyAsync(string? localValue, string? notionValue, string propertyName, Guid conflictId, CancellationToken cancellationToken)` - Compares two text values and returns a structured diff
- `RenderAsTextAsync(ConflictDiffResult diff, CancellationToken cancellationToken)` - Renders a diff result as unified-diff text
- `GenerateBatchDiffsAsync(IReadOnlyList<ConflictResolution> conflicts, CancellationToken cancellationToken)` - Generates diffs for multiple conflicts in batch

### Usage Example

```csharp
using NotionTaskSync.Services;
using NotionTaskSync.Domain.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

class Program
{
    static async Task Main()
    {
        // Setup logger
        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        var logger = loggerFactory.CreateLogger<ConflictDiffService>();

        // Create ConflictDiffService instance
        var diffService = new ConflictDiffService(logger);

        // Example 1: Generate diff for a single conflict
        var conflict = new ConflictResolution
        {
            Id = Guid.NewGuid(),
            TaskId = Guid.NewGuid(),
            PropertyName = "Title",
            LocalValue = "Implement new feature",
            NotionValue = "Implement new feature 🚀"
        };

        var diffResult = await diffService.GenerateDiffAsync(conflict);
        Console.WriteLine($"Diff generated for property: {diffResult.PropertyName}");
        Console.WriteLine($"Added lines: {diffResult.AddedCount}, Removed lines: {diffResult.RemovedCount}");

        // Example 2: Generate diff for property values directly
        var propertyDiff = await diffService.GenerateDiffForPropertyAsync(
            "This is the local description\nwith multiple lines",
            "This is the Notion description\nwith different content",
            "Description",
            Guid.NewGuid()
        );

        Console.WriteLine("\nGenerated diff:");
        var renderedDiff = await diffService.RenderAsTextAsync(propertyDiff);
        Console.WriteLine(renderedDiff);

        // Example 3: Generate diffs for multiple conflicts in batch
        var conflicts = new List<ConflictResolution>
        {
            new ConflictResolution
            {
                Id = Guid.NewGuid(),
                TaskId = Guid.NewGuid(),
                PropertyName = "Status",
                LocalValue = "In Progress",
                NotionValue = "Done"
            },
            new ConflictResolution
            {
                Id = Guid.NewGuid(),
                TaskId = Guid.NewGuid(),
                PropertyName = "Priority",
                LocalValue = "High",
                NotionValue = "Medium"
            }
        };

        var batchResults = await diffService.GenerateBatchDiffsAsync(conflicts);
        Console.WriteLine($"\nBatch processed {batchResults.Count} conflicts");

        // Example 4: Render diff as text for display
        if (batchResults.Count > 0)
        {
            var firstDiff = batchResults.First().Value;
            var textOutput = await diffService.RenderAsTextAsync(firstDiff);
            Console.WriteLine(textOutput);
        }
    }
}
```

## ChangeDetectionService

The `ChangeDetectionService` class detects changes to tasks in both local and Notion sources, enabling efficient synchronization workflows. It identifies new, updated, and deleted tasks, detects conflicts when concurrent modifications occur, and provides change history tracking for auditability. The service supports bidirectional synchronization scenarios by comparing timestamps and determining which changes need to be propagated.

### Usage Example

```csharp
using NotionTaskSync.Services;
using NotionTaskSync.Domain.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

class Program
{
    static async Task Main()
    {
        // Setup dependency injection
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddConsole());
        services.AddSingleton<IChangeLogRepository, InMemoryChangeLogRepository>();
        services.AddSingleton<ChangeDetectionService>();
        
        var serviceProvider = services.BuildServiceProvider();
        var changeDetection = serviceProvider.GetRequiredService<ChangeDetectionService>();

        // Example 1: Detect local changes since a specific timestamp
        var localTasks = new List<Task>();
        var localChanges = changeDetection.DetectLocalChanges(localTasks, DateTime.UtcNow.AddDays(-1));
        Console.WriteLine($"Detected {localChanges.Count} local changes");
        
        foreach (var change in localChanges)
        {
            Console.WriteLine($"- {change.ChangeType} at {change.Timestamp:u}");
        }

        // Example 2: Detect Notion changes since a specific timestamp
        var notionPages = new List<NotionPage>();
        var notionChanges = changeDetection.DetectNotionChanges(notionPages, DateTime.UtcNow.AddDays(-1));
        Console.WriteLine($"\nDetected {notionChanges.Count} Notion changes");
        
        foreach (var change in notionChanges)
        {
            Console.WriteLine($"- {change.ChangeType} at {change.Timestamp:u}");
        }

        // Example 3: Detect conflicts between local and Notion changes
        var conflicts = changeDetection.DetectConflicts(localChanges, notionChanges);
        Console.WriteLine($"\nDetected {conflicts.Count} conflicts");
        
        foreach (var conflict in conflicts)
        {
            Console.WriteLine($"- Conflict on task {conflict.TaskId}: {conflict.PropertyName}");
            Console.WriteLine($"  Local value: {conflict.LocalValue}");
            Console.WriteLine($"  Notion value: {conflict.NotionValue}");
        }

        // Example 4: Check if a specific task has changed since a timestamp
        var sampleTask = new Task { Id = Guid.NewGuid(), UpdatedAt = DateTime.UtcNow.AddHours(-2) };
        var hasChanged = changeDetection.HasChangedSince(sampleTask, DateTime.UtcNow.AddDays(-1));
        Console.WriteLine($"\nTask has changed since yesterday: {hasChanged}");

        // Example 5: Get change history for a task
        var changeHistory = changeDetection.GetTaskChangeHistory(sampleTask.Id);
        Console.WriteLine($"Task change history entries: {changeHistory.Count}");

        // Example 6: Get the most recent change for a task
        var lastChange = changeDetection.GetLastChange(sampleTask.Id);
        Console.WriteLine($"Last change: {lastChange?.ChangeType ?? "None"} at {lastChange?.Timestamp:u}");

        // Example 7: Compare property values for equality
        var areEqual = ChangeDetectionService.ArePropertyValuesEqual("Hello World", "Hello World");
        Console.WriteLine($"\nProperty values equal: {areEqual}");
        
        var areDifferent = ChangeDetectionService.ArePropertyValuesEqual("Hello", "World");
        Console.WriteLine($"Property values different: {areDifferent}");
    }
}
```

## BackupService

The `BackupService` class manages task file backups and restoration, providing automated and manual backup functionality with retention policies. It creates timestamped backup directories containing copies of all task markdown files, tracks backup metadata, and automatically cleans up old backups based on a retention limit.

### Usage Example

```csharp
using NotionTaskSync.Services;
using NotionTaskSync.Domain.Models;
using System;
using System.Threading.Tasks;

class Program
{
static async Task Main()
{
// Initialize BackupService with backup directory and retention policy
var backupService = new BackupService(
backupDirectory: @"./backups",
maxBackupFiles: 10,
fileService: new LocalFileService(@"./tasks")
);

// Create a manual backup with a descriptive label
var backupInfo = await backupService.CreateBackupAsync("pre-major-sync");
Console.WriteLine($"Backup created: {backupInfo}");
Console.WriteLine($"Backup ID: {backupInfo.Id}");
Console.WriteLine($"Backup path: {backupInfo.Path}");
Console.WriteLine($"Files backed up: {backupInfo.FileCount}");
Console.WriteLine($"Backup age: {backupInfo.GetAge().TotalHours:F1} hours");

// Get backup statistics
var stats = backupService.GetBackupStats();
Console.WriteLine($"\nBackup Statistics:");
Console.WriteLine($"Total backups: {stats.TotalBackups}");
Console.WriteLine($"Total size: {stats.GetTotalSizeMB():F2} MB");
Console.WriteLine($"Last backup: {stats.LastBackupTime:O}");

// List all available backups
var allBackups = backupService.GetAvailableBackups();
Console.WriteLine($"\nAvailable backups ({allBackups.Count}):");
foreach (var backup in allBackups)
{
Console.WriteLine($"- {backup.ToString()}");
}

// Restore from a specific backup
if (allBackups.Count > 0)
{
var latestBackup = allBackups[0];
await backupService.RestoreFromBackupAsync(latestBackup.Path);
Console.WriteLine($"\nRestored from backup: {latestBackup.Label}");
}

// Delete an old backup
if (allBackups.Count > 5)
{
var backupToDelete = allBackups.Last();
await backupService.DeleteBackupAsync(backupToDelete.Path);
Console.WriteLine($"Deleted old backup: {backupToDelete.Label}");
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

## HealthCheckWorker

The `HealthCheckWorker` class provides background monitoring of application health and resource usage. It performs periodic health checks to detect memory leaks, connection issues, and performance degradation by tracking memory usage, thread count, uptime, and connectivity status. The worker runs in the background and logs warnings when thresholds are exceeded.

### Public Members

- `HealthCheckWorker(ILogger<HealthCheckWorker> logger, int checkIntervalSeconds = 300)` - Constructor that initializes the worker with a logger and optional check interval
- `Start()` - Starts the health check worker in the background
- `StopAsync()` - Stops the health check worker gracefully
- `GetStatus()` - Gets the current health status
- `IsHealthy` - Property indicating if the application is currently healthy
- `MemoryUsageMb` - Property for current memory usage in megabytes
- `ThreadCount` - Property for current thread count
- `UptimeSeconds` - Property for current uptime in seconds
- `CheckedAt` - Property for timestamp when health was last checked
- `Dispose()` - Disposes the worker and cleans up resources
- `ToString()` - Returns a formatted health status string

### Usage Example

```csharp
using NotionTaskSync.Workers;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

class Program
{
    static async Task Main()
    {
        // Setup logger
        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        var logger = loggerFactory.CreateLogger<HealthCheckWorker>();

        // Create health check worker with 60-second interval
        var healthChecker = new HealthCheckWorker(logger, checkIntervalSeconds: 60);

        // Start the worker
        healthChecker.Start();
        Console.WriteLine("Health check worker started");

        // Wait for a few health checks to complete
        await Task.Delay(TimeSpan.FromSeconds(180));

        // Get current health status
        var status = healthChecker.GetStatus();
        Console.WriteLine($"\nHealth Status:");
        Console.WriteLine($"- Healthy: {status.IsHealthy}");
        Console.WriteLine($"- Memory Usage: {status.MemoryUsageMb} MB");
        Console.WriteLine($"- Thread Count: {status.ThreadCount}");
        Console.WriteLine($"- Uptime: {status.UptimeSeconds} seconds");
        Console.WriteLine($"- Last Checked: {status.CheckedAt:g}");
        Console.WriteLine($"\nStatus output:\n{status}");

        // Stop the worker
        await healthChecker.StopAsync();
        Console.WriteLine("\nHealth check worker stopped");
    }
}
```

## HealthCheckWorkerValidation

The `HealthCheckWorkerValidation` static class provides validation helpers for health check data, ensuring that `HealthCheckWorker` and `HealthStatus` instances contain valid, non-default values within expected ranges. It helps catch configuration errors, memory leaks, and invalid states early by validating memory usage, thread counts, uptime, and timestamp values.

### Public Members

- `Validate(this HealthCheckWorker value)` - Validates a `HealthCheckWorker` instance and returns a list of validation problems
- `Validate(this HealthStatus value)` - Validates a `HealthStatus` instance and returns a list of validation problems  
- `IsValid(this HealthCheckWorker value)` - Determines whether a `HealthCheckWorker` instance is valid
- `IsValid(this HealthStatus value)` - Determines whether a `HealthStatus` instance is valid
- `EnsureValid(this HealthCheckWorker value)` - Ensures that a `HealthCheckWorker` instance is valid, throwing if invalid
- `EnsureValid(this HealthStatus value)` - Ensures that a `HealthStatus` instance is valid, throwing if invalid

### Usage Example

```csharp
using NotionTaskSync.Workers;
using System;

class Program
{
    static void Main()
    {
        // Create a valid health check worker
        var healthWorker = new HealthCheckWorker(
            logger: null, // In real usage, provide a proper ILogger
            checkIntervalSeconds: 60);

        // Validate the worker
        var validationProblems = healthWorker.Validate();
        
        if (validationProblems.Count > 0)
        {
            Console.WriteLine("Validation failed:");
            foreach (var problem in validationProblems)
            {
                Console.WriteLine($"- {problem}");
            }
        }
        else
        {
            Console.WriteLine("HealthCheckWorker is valid!");
        }

        // Validate using IsValid helper
        bool isValid = healthWorker.IsValid();
        Console.WriteLine($"IsValid: {isValid}");

        // Validate using EnsureValid (throws if invalid)
        try
        {
            healthWorker.EnsureValid();
            Console.WriteLine("EnsureValid passed - worker is valid");
        }
        catch (ArgumentException ex)
        {
            Console.WriteLine($"Validation failed: {ex.Message}");
        }

        // Validate HealthStatus directly
        var status = new HealthStatus
        {
            IsHealthy = true,
            MemoryUsageMb = 128,
            ThreadCount = 10,
            UptimeSeconds = 3600,
            CheckedAt = DateTime.UtcNow
        };
        
        var statusProblems = status.Validate();
        Console.WriteLine($"HealthStatus has {statusProblems.Count} validation problems");
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

## IntegrationExample

The `IntegrationExample` class demonstrates how to integrate Notion Task Sync into ASP.NET Core applications using dependency injection patterns. It provides examples for setting up sync services in web applications, creating background services for scheduled syncs, implementing REST API controllers, and managing multiple sync profiles with different configurations.

### Usage Examples

#### Basic ASP.NET Core Integration

```csharp
using NotionTaskSync.Services;
using NotionTaskSync.Infrastructure.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

// Setup host builder (similar to ASP.NET Core WebApplicationBuilder)
var hostBuilder = new HostBuilder();

// Configure services
// Similar to builder.Services in ASP.NET Core

hostBuilder.ConfigureServices((context, services) =>
{
    // Add logging
    services.AddLogging(builder => builder.AddConsole());
    
    // Add application services
    services.AddApplicationServices(context.Configuration);
    
    // Register sync as hosted service (background task)
    services.AddHostedService<SyncBackgroundService>();
    
    // Or register as singleton for direct access
    services.AddSingleton<SyncService>();
});

// Configure appsettings.json
// Similar to builder.Configuration in ASP.NET Core

hostBuilder.ConfigureAppConfiguration((hostingContext, config) =>
{
    config.SetBasePath(Directory.GetCurrentDirectory());
    config.AddJsonFile("appsettings.json", optional: false);
    config.AddEnvironmentVariables("NOTION_");
});

// Build and start the host
var host = hostBuilder.Build();

// Access services
var syncService = host.Services.GetRequiredService<SyncService>();

// Create and execute sync configuration
var config = new SyncConfig(
    "MySyncProfile",
    host.Services.GetRequiredService<IOptions<NotionApiSettings>>().Value.DatabaseId,
    "./my-tasks"
);

var result = await syncService.ExecuteSyncAsync(config);
```

#### REST API Controller Integration

```csharp
[ApiController]
[Route("api/[controller]")]
public class SyncController : ControllerBase
{
    private readonly SyncService _syncService;
    private readonly ILogger<SyncController> _logger;

    public SyncController(SyncService syncService, ILogger<SyncController> logger)
    {
        _syncService = syncService;
        _logger = logger;
    }

    [HttpPost("sync")]
    public async Task<IActionResult> Sync([FromBody] SyncRequest request)
    {
        _logger.LogInformation("Starting sync: {Name}", request.Name);

        var config = new SyncConfig(
            request.Name,
            request.DatabaseId,
            request.LocalPath
        );

        if (request.Direction.HasValue)
            config.Direction = request.Direction.Value;

        var result = await _syncService.ExecuteSyncAsync(config);

        _logger.LogInformation("Sync completed: {Status}", result.Status);

        return Ok(new { Status = result.Status, TasksSynced = result.SyncedCount });
    }

    [HttpGet("status")]
    public IActionResult GetStatus()
    {
        return Ok(new {
            IsHealthy = true,
            LastSync = DateTime.UtcNow,
            Message = "Sync service running"
        });
    }
}

// Request DTO
public class SyncRequest
{
    public string Name { get; set; } = string.Empty;
    public string DatabaseId { get; set; } = string.Empty;
    public string LocalPath { get; set; } = "./tasks";
    public SyncDirection? Direction { get; set; }
}
```

#### Background Service for Scheduled Sync

```csharp
// Custom background service implementing IHostedService
public class SyncBackgroundService : BackgroundService
{
    private readonly SyncService _syncService;
    private readonly ILogger<SyncBackgroundService> _logger;
    private readonly IOptions<SyncConfig> _config;

    public SyncBackgroundService(
        SyncService syncService,
        ILogger<SyncBackgroundService> logger,
        IOptions<SyncConfig> config)
    {
        _syncService = syncService;
        _logger = logger;
        _config = config;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("SyncBackgroundService starting...");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                _logger.LogInformation("Running scheduled sync...");

                var result = await _syncService.ExecuteSyncAsync(_config.Value);

                _logger.LogInformation(
                    "Sync completed: {LocalTasks} local tasks, {NotionPages} Notion pages, {Status}",
                    result.LocalTaskCount,
                    result.NotionPageCount,
                    result.Status
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during scheduled sync");
            }

            // Wait for next sync interval
            var delay = TimeSpan.FromSeconds(_config.Value.SyncIntervalSeconds);
            await Task.Delay(delay, stoppingToken);
        }

        _logger.LogInformation("SyncBackgroundService stopped.");
    }
}

// Register in DI container
services.AddHostedService<SyncBackgroundService>();
services.Configure<SyncConfig>(options => {
    options.Name = "BackgroundSync";
    options.SyncIntervalSeconds = 300; // 5 minutes
    options.IsEnabled = true;
});
```

#### Multiple Sync Profiles

```csharp
// In Program.cs or Startup.cs
builder.Services.Configure<SyncConfig>("ProjectA", options => {
    options.Name = "ProjectA";
    options.NotionDatabaseId = Configuration["Sync:ProjectA:DatabaseId"];
    options.LocalFolderPath = Configuration["Sync:ProjectA:LocalPath"];
    options.ConflictStrategy = ConflictResolutionStrategy.LocalWins;
});

builder.Services.Configure<SyncConfig>("ProjectB", options => {
    options.Name = "ProjectB";
    options.NotionDatabaseId = Configuration["Sync:ProjectB:DatabaseId"];
    options.LocalFolderPath = Configuration["Sync:ProjectB:LocalPath"];
    options.ConflictStrategy = ConflictResolutionStrategy.NotionWins;
});

// Access specific configuration
public class SyncManager
{
    private readonly IOptionsSnapshot<SyncConfig> _configs;

    public SyncManager(IOptionsSnapshot<SyncConfig> configs)
    {
        _configs = configs;
    }

    public void SyncProjectA()
    {
        var config = _configs.Get("ProjectA");
        // Execute sync for ProjectA
    }
}
```

## DatabaseContext

The `DatabaseContext` class provides centralized data access and transaction management for database operations. It serves as the main entry point for executing queries, commands, and managing database connections and transactions across different database engines (SQLite, SQL Server, PostgreSQL).

### Public Members

- `DatabaseContext(string connectionString, string databaseEngine = "SQLite", int commandTimeout = 30)` - Constructor that initializes the context with connection string, database engine type, and command timeout
- `IDbConnection Connection` - Gets or creates the database connection
- `bool HasActiveTransaction` - Gets a value indicating whether a transaction is currently active
- `public async Task OpenAsync()` - Opens the database connection asynchronously
- `public async Task CloseAsync()` - Closes the database connection
- `public IDbTransaction BeginTransaction()` - Begins a new transaction if one is not already active
- `public async Task CommitAsync()` - Commits the current transaction if one is active
- `public async Task RollbackAsync()` - Rolls back the current transaction if one is active
- `public async Task<bool> TestConnectionAsync()` - Tests the database connection
- `public async Task ExecuteInTransactionAsync(Func<Task> action)` - Executes an action within a transaction scope, auto-committing on success
- `public async Task<T> ExecuteInTransactionAsync<T>(Func<Task<T>> action)` - Executes an action within a transaction scope and returns a result
- `public async Task<int> ExecuteAsync(string command, object? parameters = null)` - Executes a command and returns the number of affected rows
- `public async Task<T?> QueryFirstAsync<T>(string query, object? parameters = null)` - Executes a query and returns the first result
- `public async Task<object?> QueryScalarAsync(string query, object? parameters = null)` - Executes a scalar query and returns a single value
- `public string GetDatabaseEngine()` - Gets the current database engine being used
- `public string GetMaskedConnectionString()` - Gets the connection string with sensitive data masked
- `public void Dispose()` - Disposes the database context and closes the connection

### Usage Example

```csharp
using NotionTaskSync.Data.Database;
using System;
using System.Threading.Tasks;

class Program
{
    static async Task Main()
    {
        // Initialize DatabaseContext with connection string and database engine
        var dbContext = new DatabaseContext(
            connectionString: "Data Source=./tasks.db",
            databaseEngine: "SQLite",
            commandTimeout: 30
        );

        try
        {
            // Open the database connection
            await dbContext.OpenAsync();
            Console.WriteLine("Database connection opened successfully");
            Console.WriteLine($"Using database engine: {dbContext.GetDatabaseEngine()}");
            Console.WriteLine($"Connection string: {dbContext.GetMaskedConnectionString()}");

            // Test the connection
            var isConnected = await dbContext.TestConnectionAsync();
            Console.WriteLine($"Connection test: {(isConnected ? "Success" : "Failed")}");

            // Execute a simple query
            var result = await dbContext.QueryFirstAsync<int>(
                "SELECT COUNT(*) FROM Tasks WHERE Status = @status",
                new { status = "InProgress" }
            );
            Console.WriteLine($"Active tasks count: {result}");

            // Execute a command within a transaction
            await dbContext.ExecuteInTransactionAsync(async () =>
            {
                // Transactional operations
                var updateCount = await dbContext.ExecuteAsync(
                    "UPDATE Tasks SET Status = @newStatus WHERE Status = @oldStatus",
                    new { newStatus = "Done", oldStatus = "InProgress" }
                );
                Console.WriteLine($"Updated {updateCount} tasks to 'Done' status");
            });

            // Execute a query with parameters
            var tasks = await dbContext.QueryFirstAsync<string>(
                "SELECT Title FROM Tasks WHERE Priority > @priority",
                new { priority = 50 }
            );
            Console.WriteLine($"High priority tasks: {tasks}");

            // Execute scalar query
            var count = await dbContext.QueryScalarAsync(
                "SELECT COUNT(*) FROM Tasks"
            );
            Console.WriteLine($"Total tasks: {count}");
        }
        finally
        {
            // Close the connection and dispose the context
            await dbContext.CloseAsync();
            dbContext.Dispose();
            Console.WriteLine("Database connection closed and context disposed");
        }
    }
}
```

## LocalFileService

The `LocalFileService` class provides file system operations for persisting tasks to the local file system. It handles reading and writing task files in Markdown format with metadata headers, supports backup operations, and provides utility methods for file management and statistics.

### Public Members

- `LocalFileService(string basePath)` - Constructor that initializes the service with a base directory path
- `SaveTaskAsync(Task task)` - Saves a task to a local Markdown file
- `LoadTaskAsync(string filePath)` - Loads a task from a local file by its path
- `LoadAllTasksAsync()` - Loads all tasks from files in the base directory
- `DeleteTaskAsync(string filePath)` - Deletes a task file from the local file system
- `BackupTasksAsync(string backupDir)` - Backs up all task files to a backup directory
- `GetLastModifiedTime()` - Gets the last modified time of any task file
- `CountTaskFiles()` - Counts the number of task files in the directory
- `GetBasePath()` - Gets the base directory path

### Usage Example

```csharp
using NotionTaskSync.Services;
using NotionTaskSync.Domain.Models;
using System;
using System.Threading.Tasks;

class Program
{
    static async Task Main()
    {
        // Initialize LocalFileService with a base directory path
        var fileService = new LocalFileService(@"./tasks");
        
        // Create a new task
        var task = new Task
        {
            Id = Guid.NewGuid(),
            Title = "Implement LocalFileService feature",
            Description = "Add documentation for LocalFileService class",
            Status = TaskStatus.InProgress,
            Priority = 75,
            DueDate = DateTime.UtcNow.AddDays(7),
            AssignedTo = "developer@example.com",
            Tags = "documentation,readme",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        
        // Save the task to a Markdown file
        await fileService.SaveTaskAsync(task);
        Console.WriteLine($"Task saved to: {task.LocalFilePath}");
        
        // Load all tasks from the directory
        var allTasks = await fileService.LoadAllTasksAsync();
        Console.WriteLine($"Loaded {allTasks.Count} tasks from directory");
        
        // Get statistics about task files
        var fileCount = fileService.CountTaskFiles();
        var lastModified = fileService.GetLastModifiedTime();
        Console.WriteLine($"Total task files: {fileCount}");
        Console.WriteLine($"Last modified: {lastModified:O}");
        
        // Create a backup of all task files
        var backupPath = await fileService.BackupTasksAsync(@"./backups");
        Console.WriteLine($"Backup created at: {backupPath}");
        
        // Load a specific task by file path
        if (allTasks.Count > 0)
        {
            var loadedTask = await fileService.LoadTaskAsync(allTasks[0].LocalFilePath!);
            Console.WriteLine($"Loaded task: {loadedTask?.Title}");
        }
        
        // Delete a task file
        if (allTasks.Count > 0)
        {
            await fileService.DeleteTaskAsync(allTasks[0].LocalFilePath!);
            Console.WriteLine("Task file deleted");
        }
        
        // Get the base path
        var basePath = fileService.GetBasePath();
        Console.WriteLine($"Base path: {basePath}");
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

## BulkOperationService

The `BulkOperationService` class provides batch operations for managing multiple tasks simultaneously. It enables efficient bulk updates including status changes, tag management, assignee assignment, priority setting, and soft deletion across multiple tasks in a single operation. The service tracks operation statistics including requested items, successfully affected items, and skipped items that couldn't be processed.

### Public Members

- `BulkOperationService(ITaskRepository taskRepository, ILogger<BulkOperationService> logger)` - Constructor that initializes the service with task repository and logger
- `public async Task<BulkResult> UpdateStatusAsync(Guid[] taskIds, TaskStatus newStatus)` - Updates the status of multiple tasks
- `public async Task<BulkResult> AddTagAsync(Guid[] taskIds, string tag)` - Adds a tag to multiple tasks (avoids duplicates)
- `public async Task<BulkResult> RemoveTagAsync(Guid[] taskIds, string tag)` - Removes a tag from multiple tasks
- `public async Task<BulkResult> AssignAsync(Guid[] taskIds, string assigneeEmail)` - Assigns multiple tasks to a person
- `public async Task<BulkResult> SetPriorityAsync(Guid[] taskIds, int priority)` - Sets priority for multiple tasks
- `public async Task<BulkResult> DeleteAsync(Guid[] taskIds)` - Soft deletes multiple tasks
- `public async Task<List<Domain.Models.Task>> QueryAsync(Func<Domain.Models.Task, bool> predicate)` - Queries tasks with a filter predicate
- `public string Operation` - Gets the current operation name
- `public int Requested` - Gets the number of tasks requested for the operation
- `public int Affected` - Gets the number of tasks successfully affected
- `public int Skipped` - Gets the number of tasks skipped (missing or invalid)

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
        // Setup dependency injection
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddConsole());
        services.AddSingleton<ITaskRepository, InMemoryTaskRepository>();
        services.AddSingleton<BulkOperationService>();
        
        var serviceProvider = services.BuildServiceProvider();
        var bulkService = serviceProvider.GetRequiredService<BulkOperationService>();
        var taskRepository = serviceProvider.GetRequiredService<ITaskRepository>();

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

        await taskRepository.SaveAsync(task1);
        await taskRepository.SaveAsync(task2);
        await taskRepository.SaveAsync(task3);

        // Example 1: Update status for multiple tasks
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

        // Example 8: Check operation statistics
        Console.WriteLine($"\nOperation: {bulkService.Operation}");
        Console.WriteLine($"Requested: {bulkService.Requested}");
        Console.WriteLine($"Affected: {bulkService.Affected}");
        Console.WriteLine($"Skipped: {bulkService.Skipped}");
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

## SyncStatistics

The `SyncStatistics` class aggregates statistics about sync operations for monitoring and reporting. It tracks success rates, performance metrics, and error patterns, making it ideal for generating health reports and identifying performance bottlenecks in task synchronization workflows.

### Usage Example

```csharp
using NotionTaskSync.Models;
using NotionTaskSync.Services;
using System;

class Program
{
    static void Main()
    {
        // Create statistics tracker
        var stats = new SyncStatistics();
        
        // Record a successful sync operation
        var successfulSnapshot = new SyncStatistics.SyncOperationSnapshot
        {
            Timestamp = DateTime.UtcNow,
            DurationMs = 1500,
            Successful = true,
            TasksProcessed = 42,
            ChangesDetected = 15,
            ConflictsDetected = 3,
            ConflictsResolved = 3,
            ErrorMessage = null
        };
        
        stats.RecordOperation(successfulSnapshot);
        
        // Record a failed sync operation
        var failedSnapshot = new SyncStatistics.SyncOperationSnapshot
        {
            Timestamp = DateTime.UtcNow.AddMinutes(5),
            DurationMs = 800,
            Successful = false,
            TasksProcessed = 25,
            ChangesDetected = 8,
            ConflictsDetected = 1,
            ConflictsResolved = 0,
            ErrorMessage = "Network timeout"
        };
        
        stats.RecordOperation(failedSnapshot);
        
        // Display statistics
        Console.WriteLine(stats.ToString());
        Console.WriteLine($"\nSuccess Rate: {stats.SuccessRate:F1}%");
        Console.WriteLine($"Conflict Resolution Rate: {stats.ConflictResolutionRate:F1}%");
        Console.WriteLine($"Average Duration: {stats.AverageSyncDuration:mm\\:ss}");
        
        // Reset statistics
        stats.Reset();
        Console.WriteLine($"\nStatistics reset at: {stats.LastResetAt:g}");
        
        // Create from actual sync result
        var syncService = new SyncService(...); // Setup with dependencies
        var config = new SyncConfig(...);
        var result = await syncService.ExecuteSyncAsync(config);
        
        var fromResult = SyncStatistics.SyncOperationSnapshot.FromSyncResult(result);
        stats.RecordOperation(fromResult);
    }
}
```

## NotionApiService

The `NotionApiService` class provides integration with the Notion API for reading and writing task data. It handles authentication, pagination, error handling, and data transformation between Notion's API format and the application's domain models. The service supports both full database queries and incremental sync operations based on modification timestamps.

### Usage Example

```csharp
using NotionTaskSync.Services;
using NotionTaskSync.Domain.Models;
using System;
using System.Threading.Tasks;

class Program
{
    static async Task Main()
    {
        // Initialize Notion API service with your API key
        var notionApiService = new NotionApiService("your-notion-api-key-here");
        
        // Test API connection
        bool isConnected = await notionApiService.TestConnectionAsync();
        Console.WriteLine($"API connection test: {(isConnected ? "Success" : "Failed")}");
        
        // Fetch all pages from a database
        var allPages = await notionApiService.FetchPagesAsync("550e8400-e29b-41d4-a716-446655440000");
        Console.WriteLine($"Fetched {allPages.Count} pages from database");
        
        // Fetch only pages modified since a specific timestamp (incremental sync)
        var recentPages = await notionApiService.FetchPagesSinceAsync(
            "550e8400-e29b-41d4-a716-446655440000",
            DateTime.UtcNow.AddDays(-1)
        );
        Console.WriteLine($"Fetched {recentPages.Count} pages modified in the last 24 hours");
        
        // Fetch a specific page by ID
        if (allPages.Count > 0)
        {
            var page = await notionApiService.FetchPageAsync(allPages[0].Id);
            Console.WriteLine($"Fetched page: {page.Title}");
        }
        
        // Create a new page in Notion
        var newTask = new Task
        {
            Id = Guid.NewGuid(),
            Title = "Implement NotionApiService documentation",
            Description = "Add documentation section for NotionApiService in README.md",
            Status = TaskStatus.Todo,
            Priority = 75,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        
        var createdPage = await notionApiService.CreatePageAsync(
            "550e8400-e29b-41d4-a716-446655440000",
            newTask
        );
        Console.WriteLine($"Created page: {createdPage.Title} with ID: {createdPage.Id}");
        
        // Update an existing page
        newTask.Status = TaskStatus.InProgress;
        newTask.UpdatedAt = DateTime.UtcNow;
        var updatedPage = await notionApiService.UpdatePageAsync(createdPage.Id, newTask);
        Console.WriteLine($"Updated page status to: {updatedPage.Status}");
        
        // Archive/delete a page
        await notionApiService.ArchivePageAsync(createdPage.Id);
        Console.WriteLine("Page archived successfully");
    }
}
```

## ConfigureCommand

The `ConfigureCommand` class provides interactive and command-line configuration for the Notion Task Sync application. It allows users to set up API keys, database IDs, local task directories, sync intervals, and conflict resolution strategies. The command validates settings before saving them to `appsettings.json`, ensuring the application remains functional after configuration.

### Usage Example

```csharp
using NotionTaskSync.Commands;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

class Program
{
    static async Task Main(string[] args)
    {
        // Setup configuration
        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json")
            .Build();

        // Create logger
        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        var logger = loggerFactory.CreateLogger<ConfigureCommand>();

        // Create ConfigureCommand instance
        var configureCommand = new ConfigureCommand(configuration, logger);

        // Example 1: Interactive configuration
        var interactiveOptions = new Dictionary<string, string>();
        var exitCode = await configureCommand.ExecuteAsync(new List<string>(), interactiveOptions);
        Console.WriteLine($"Interactive configuration exit code: {exitCode}");

        // Example 2: Command-line configuration
        var commandLineOptions = new Dictionary<string, string>
        {
            {"api-key", "your-notion-api-key-here"},
            {"database-id", "550e8400-e29b-41d4-a716-446655440000"},
            {"task-directory", "./my-tasks"},
            {"sync-interval", "600"},
            {"conflict-strategy", "last-write"}
        };
        
        var commandLineExitCode = await configureCommand.ExecuteAsync(new List<string>(), commandLineOptions);
        Console.WriteLine($"Command-line configuration exit code: {commandLineExitCode}");
    }
}
```

## ConflictResolutionService

The `ConflictResolutionService` class handles conflicts detected during synchronization between local task storage and Notion databases. It provides multiple resolution strategies including last-write-wins, local-wins, notion-wins, and manual review workflows. The service tracks resolution methods, timestamps, and automatically logs discarded local changes for auditability.

### Usage Example

```csharp
using NotionTaskSync.Services;
using NotionTaskSync.Domain.Models;
using NotionTaskSync.Data.Repositories;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

class Program
{
    static async Task Main()
    {
        // Setup dependency injection
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddConsole());
        services.AddSingleton<IChangeLogRepository, InMemoryChangeLogRepository>();
        services.AddSingleton<ConflictResolutionService>();
        
        var serviceProvider = services.BuildServiceProvider();
        var conflictResolutionService = serviceProvider.GetRequiredService<ConflictResolutionService>();

        // Example 1: Create conflicts for demonstration
        var conflicts = new List<ConflictResolution>
        {
            new ConflictResolution
            {
                TaskId = Guid.NewGuid(),
                PropertyName = "Title",
                LocalValue = "Implement ConflictResolution feature",
                NotionValue = "Document ConflictResolution class",
                LocalModifiedAt = DateTime.UtcNow.AddMinutes(-30),
                NotionModifiedAt = DateTime.UtcNow.AddMinutes(-15),
                ConflictType = ConflictType.ConcurrentModification
            },
            new ConflictResolution
            {
                TaskId = Guid.NewGuid(),
                PropertyName = "Status",
                LocalValue = "In Progress",
                NotionValue = "Done",
                ConflictType = ConflictType.ConcurrentModification
            },
            new ConflictResolution
            {
                TaskId = Guid.NewGuid(),
                PropertyName = "Priority",
                LocalValue = "50",
                NotionValue = "75",
                ConflictType = ConflictType.ConcurrentModification
            }
        };

        // Example 2: Resolve conflicts using different strategies
        Console.WriteLine("=== Resolving conflicts using LastWrite strategy ===");
        var lastWriteResolutions = await conflictResolutionService.ResolveConflictsAsync(
            conflicts,
            ConflictResolutionStrategy.LastWrite
        );
        
        foreach (var resolution in lastWriteResolutions)
        {
            Console.WriteLine($"Conflict {resolution.TaskId}: {resolution.PropertyName}");
            Console.WriteLine($"  Status: {resolution.Status}");
            Console.WriteLine($"  Method: {resolution.ResolutionMethod}");
            Console.WriteLine($"  Resolved value: {resolution.ResolvedValue}");
            Console.WriteLine($"  Notes: {resolution.ResolutionNotes}");
        }

        // Example 3: Resolve conflicts using LocalWins strategy
        Console.WriteLine("\n=== Resolving conflicts using LocalWins strategy ===");
        var localWinsResolutions = await conflictResolutionService.ResolveConflictsAsync(
            conflicts,
            ConflictResolutionStrategy.LocalWins
        );
        
        Console.WriteLine($"Resolved {localWinsResolutions.Count(r => r.Status == ResolutionStatus.Resolved)} conflicts");
        Console.WriteLine($"Pending review: {localWinsResolutions.Count(r => r.Status == ResolutionStatus.PendingReview)}");

        // Example 4: Resolve conflicts using Manual strategy
        Console.WriteLine("\n=== Resolving conflicts using Manual strategy ===");
        var manualResolutions = await conflictResolutionService.ResolveConflictsAsync(
            conflicts,
            ConflictResolutionStrategy.Manual
        );
        
        Console.WriteLine($"Conflicts requiring manual review: {manualResolutions.Count(r => r.Status == ResolutionStatus.PendingReview)}");

        // Example 5: Use per-field conflict resolution strategies
        Console.WriteLine("\n=== Resolving conflicts using field-specific strategies ===");
        var fieldStrategies = new Dictionary<string, ConflictResolutionStrategy>
        {
            {"Title", ConflictResolutionStrategy.LocalWins},
            {"Status", ConflictResolutionStrategy.NotionWins},
            {"Priority", ConflictResolutionStrategy.LastWrite}
        };
        
        var fieldStrategyResolutions = await conflictResolutionService.ResolveConflictsAsync(
            conflicts,
            ConflictResolutionStrategy.LastWrite, // Default strategy
            fieldStrategies
        );
        
        Console.WriteLine("Applied field-specific strategies:");
        foreach (var resolution in fieldStrategyResolutions)
        {
            Console.WriteLine($"  {resolution.PropertyName}: {resolution.ResolutionMethod}");
        }

        // Example 6: Get pending conflicts
        Console.WriteLine("\n=== Getting pending conflicts ===");
        var allResolutions = await conflictResolutionService.ResolveConflictsAsync(
            conflicts,
            ConflictResolutionStrategy.Manual
        );
        
        var pendingConflicts = conflictResolutionService.GetPendingConflicts(allResolutions);
        Console.WriteLine($"Total conflicts: {allResolutions.Count}");
        Console.WriteLine($"Pending review: {pendingConflicts.Count}");

        // Example 7: Get resolution statistics
        Console.WriteLine("\n=== Getting resolution statistics ===");
        var stats = conflictResolutionService.GetResolutionStats(allResolutions);
        Console.WriteLine($"Total conflicts: {stats.TotalConflicts}");
        Console.WriteLine($"Resolved: {stats.ResolvedCount}");
        Console.WriteLine($"Pending review: {stats.PendingReviewCount}");
        Console.WriteLine($"Abandoned: {stats.AbandonedCount}");
        Console.WriteLine($"Resolution rate: {stats.ResolutionRate:P}");

        // Example 8: Manually resolve a conflict
        Console.WriteLine("\n=== Manually resolving a conflict ===");
        var manualResolution = await conflictResolutionService.ManuallyResolveAsync(
            Guid.NewGuid(),
            "Manually resolved value",
            ResolutionMethod.Manual,
            "Manually reviewed and resolved"
        );
        
        Console.WriteLine($"Manual resolution status: {manualResolution.Status}");
        Console.WriteLine($"Resolved value: {manualResolution.ResolvedValue}");

        // Example 9: Merge conflicts
        Console.WriteLine("\n=== Merging conflicts ===");
        var mergeConflict = new ConflictResolution
        {
            TaskId = Guid.NewGuid(),
            PropertyName = "Tags",
            LocalValue = "backend,feature",
            NotionValue = "backend,api"
        };
        
        var mergedResult = conflictResolutionService.MergeConflicts(mergeConflict);
        Console.WriteLine($"Merge result status: {mergedResult.Status}");
        Console.WriteLine($"Merged value: {mergedResult.ResolvedValue}");
    }
}
```

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

## ConflictDiffResult

The `ConflictDiffResult` class represents the result of comparing two conflicting property values between local and Notion systems. It provides a structured diff view with annotated lines ready for terminal or UI rendering, showing exactly what differs between the two versions.

### Usage Example

```csharp
using NotionTaskSync.Domain.Models;
using System;

class Program
{
    static void Main()
    {
        // Create a conflict diff result for a property that differs between local and Notion
        var diffResult = new ConflictDiffResult
        {
            ConflictId = Guid.NewGuid(),
            PropertyName = "Description",
            LocalValue = "This is the local description\nwith multiple lines\nand some changes",
            NotionValue = "This is the Notion description\nwith multiple lines\nand SOME CHANGES",
            GeneratedAt = DateTime.UtcNow
        };

        // The Lines collection would normally be populated by a diff algorithm
        // For demonstration, we'll add some sample diff lines
        diffResult.Lines.Add(new DiffLine
        {
            Text = "This is the local description",
            Kind = DiffLineKind.Context,
            LocalLineNumber = 1,
            NotionLineNumber = 1
        });

        diffResult.Lines.Add(new DiffLine
        {
            Text = "with multiple lines",
            Kind = DiffLineKind.Context,
            LocalLineNumber = 2,
            NotionLineNumber = 2
        });

        diffResult.Lines.Add(new DiffLine
        {
            Text = "and some changes",
            Kind = DiffLineKind.Removed,
            LocalLineNumber = 3,
            NotionLineNumber = null
        });

        diffResult.Lines.Add(new DiffLine
        {
            Text = "and SOME CHANGES",
            Kind = DiffLineKind.Added,
            LocalLineNumber = null,
            NotionLineNumber = 3
        });

        // Access diff statistics
        Console.WriteLine($"Property: {diffResult.PropertyName}");
        Console.WriteLine($"Local value: {diffResult.LocalValue}");
        Console.WriteLine($"Notion value: {diffResult.NotionValue}");
        Console.WriteLine($"Lines added: {diffResult.AddedCount}");
        Console.WriteLine($"Lines removed: {diffResult.RemovedCount}");
        Console.WriteLine($"Is identical: {diffResult.IsIdentical}");
        Console.WriteLine($"Generated at: {diffResult.GeneratedAt:u}");

        // Render the diff (simplified example)
        Console.WriteLine("\nDiff view:");
        foreach (var line in diffResult.Lines)
        {
            Console.WriteLine($"{line.Sigil} {line.Text}");
        }
    }
}
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

## ConflictDiffServiceValidation

The `ConflictDiffServiceValidation` class provides comprehensive validation helpers for the `ConflictDiffService` and related conflict resolution types. It ensures that service instances, conflict resolutions, diff results, and method parameters are valid before operations are performed, preventing runtime errors and providing clear validation feedback.

### Key Features
- Validates `ConflictDiffService` instances for proper initialization
- Validates `ConflictResolution` objects with comprehensive property checks
- Validates `ConflictDiffResult` objects for rendering
- Validates batch conflict collections for bulk operations
- Provides both boolean checks (`IsValid`) and exception-throwing validation (`EnsureValid`)

### Usage Example

```csharp
using NotionTaskSync.Services;
using NotionTaskSync.Domain.Models;
using System;
using Microsoft.Extensions.Logging;

class Program
{
    static void Main()
    {
        // 1. Validate a ConflictDiffService instance
        var loggerFactory = LoggerFactory.Create(builder => { });
        var conflictDiffService = new ConflictDiffService(loggerFactory.CreateLogger<ConflictDiffService>());
        
        var serviceErrors = conflictDiffService.Validate();
        if (serviceErrors.Count > 0)
        {
            Console.WriteLine("Service validation failed:");
            foreach (var error in serviceErrors)
            {
                Console.WriteLine($"  - {error}");
            }
        }
        else
        {
            Console.WriteLine("Service instance is valid");
        }
        
        // 2. Validate a ConflictResolution object
        var conflict = new ConflictResolution
        {
            Id = Guid.NewGuid(),
            PropertyName = "Description",
            LocalValue = "Local description",
            NotionValue = "Notion description",
            DetectedAt = DateTime.UtcNow
        };
        
        var conflictErrors = conflict.Validate();
        if (conflictErrors.Count > 0)
        {
            Console.WriteLine("Conflict validation failed:");
            foreach (var error in conflictErrors)
            {
                Console.WriteLine($"  - {error}");
            }
        }
        
        // 3. Validate parameters for GenerateDiffForPropertyAsync
        var paramErrors = ConflictDiffServiceValidation.Validate(
            localValue: "local data",
            notionValue: "notion data",
            propertyName: "Description",
            conflictId: Guid.NewGuid()
        );
        
        if (paramErrors.Count > 0)
        {
            Console.WriteLine("Parameter validation failed:");
            foreach (var error in paramErrors)
            {
                Console.WriteLine($"  - {error}");
            }
        }
        
        // 4. Use EnsureValid to throw on invalid input
        try
        {
            ConflictDiffServiceValidation.EnsureValid(conflict);
            Console.WriteLine("Conflict is valid!");
        }
        catch (ArgumentException ex)
        {
            Console.WriteLine($"Validation error: {ex.Message}");
        }
        
        // 5. Check validity without exceptions
        bool isValid = conflict.IsValid();
        Console.WriteLine($"Is conflict valid: {isValid}");
    }
}
```

## ConflictResolution

The `ConflictResolution` class represents a conflict detected during synchronization between local task storage and Notion databases. It tracks both local and Notion versions of conflicting properties, enabling automatic resolution strategies (like last-write-wins) or manual review workflows. Each conflict maintains timestamps for both local and Notion modifications, allowing precise conflict resolution based on temporal ordering or explicit strategy selection.

### Usage Example

```csharp
using NotionTaskSync.Domain.Models;
using System;

class Program
{
    static void Main()
    {
        // Create a conflict for a task title that was modified in both local and Notion
        var conflict = new ConflictResolution
        {
            TaskId = Guid.Parse("550e8400-e29b-41d4-a716-446655440000"),
            ConflictType = ConflictType.ConcurrentModification,
            PropertyName = "Title",
            LocalValue = "Implement ConflictResolution feature",
            NotionValue = "Document ConflictResolution class",
            LocalModifiedAt = DateTime.UtcNow.AddMinutes(-30),
            NotionModifiedAt = DateTime.UtcNow.AddMinutes(-15),
            Status = ResolutionStatus.Pending
        };

        // Resolve using last-write-wins strategy
        if (conflict.LocalModifiedAt > conflict.NotionModifiedAt)
        {
            conflict.Resolve(conflict.LocalValue!, ResolutionMethod.LastWrite, 
                          "Local modification is more recent");
        }
        else
        {
            conflict.Resolve(conflict.NotionValue!, ResolutionMethod.LastWrite, 
                          "Notion modification is more recent");
        }

        Console.WriteLine($"Conflict resolved: {conflict.Status}");
        Console.WriteLine($"Resolved value: {conflict.ResolvedValue}");
        Console.WriteLine($"Resolution method: {conflict.ResolutionMethod}");
        Console.WriteLine($"Conflict age: {conflict.GetAge().TotalMinutes:F1} minutes");

        // Mark a complex conflict for manual review
        var complexConflict = new ConflictResolution
        {
            TaskId = Guid.Parse("123e4567-e89b-12d3-a456-426614174000"),
            ConflictType = ConflictType.PropertyMismatch,
            PropertyName = "Priority",
            LocalValue = "High",
            NotionValue = "Critical",
            LocalModifiedAt = DateTime.UtcNow.AddHours(-2),
            NotionModifiedAt = DateTime.UtcNow.AddMinutes(-5),
            Status = ResolutionStatus.Pending
        };

        complexConflict.MarkForManualReview(
            "Priority values diverged significantly - requires business decision");

        Console.WriteLine($"Conflict marked for review: {complexConflict.Status}");
        Console.WriteLine($"Review reason: {complexConflict.ResolutionNotes}");

        // Validate the conflict resolution
        if (conflict.Validate())
        {
            Console.WriteLine("Conflict resolution is valid and ready for application");
        }
    }
}
```

## ChangeLog

The `ChangeLog` class tracks changes made during synchronization operations between local task storage and Notion databases. It records detailed information about each modification including what changed, when, by whom, and how conflicts were resolved. This comprehensive audit trail enables debugging, rollback, and analysis of synchronization workflows.

### Usage Example

```csharp
using Domain.Models;
using System;

class Program
{
    static void Main()
    {
        // Create a change log entry for a task title update
        var changeLog = new ChangeLog
        {
            Id = Guid.NewGuid(),
            TaskId = Guid.Parse("550e8400-e29b-41d4-a716-446655440000"),
            ChangeType = "Updated",
            PropertyName = "Title",
            OldValue = "Old task title",
            NewValue = "New improved task title",
            Source = ChangeSource.Local,
            Timestamp = DateTime.UtcNow,
            UserEmail = "user@example.com",
            Description = "Updated task title to better reflect actual work needed",
            IsConflict = false,
            ConflictResolutionStrategy = "None",
            Validate = true
        };

        // Generate a summary of the change
        var summary = changeLog.GetSummary();
        Console.WriteLine(summary);
        // Output: "[2026-07-16 14:30:00] user@example.com: Updated 'Title' from 'Old task title' to 'New improved task title'"

        // Mark as conflict if needed
        changeLog.MarkAsConflict("Manual review required - conflicting changes detected");
        Console.WriteLine($"Conflict status: {changeLog.IsConflict}");
        Console.WriteLine($"Resolution strategy: {changeLog.ConflictResolutionStrategy}");

        // Check if change is within a specific time window
        var timeWindow = TimeSpan.FromHours(1);
        var isWithinWindow = changeLog.IsWithinTimeWindow(timeWindow);
        Console.WriteLine($"Within 1 hour window: {isWithinWindow}");
    }
}
```

## NotionPage

The `NotionPage` class represents a Notion page entity that can be synchronized with local task storage. It encapsulates all metadata and properties of a Notion page including identification, timestamps, content properties, and synchronization state. This class is central to the bidirectional synchronization workflow between local tasks and Notion databases.

### Usage Example

```csharp
using Domain.Models;
using System;
using System.Collections.Generic;

class Program
{
    static void Main()
    {
        // Create a Notion page representing a task
        var notionPage = new NotionPage(
            pageId: "550e8400-e29b-41d4-a716-446655440000",
            databaseId: "123e4567-e89b-12d3-a456-426614174000",
            title: "Implement NotionPage documentation"
        )
        {
            // Set additional properties
            Properties = new Dictionary<string, object?>
            {
                { "Description", "Add NotionPage section to README.md" },
                { "Status", "In Progress" },
                { "Priority", 75 },
                { "Due Date", DateTime.UtcNow.AddDays(3) }
            };
            
            // Access core properties
            Console.WriteLine($"Page ID: {notionPage.PageId}");
            Console.WriteLine($"Database ID: {notionPage.DatabaseId}");
            Console.WriteLine($"Title: {notionPage.Title}");
            Console.WriteLine($"Created: {notionPage.CreatedTime:u}");
            Console.WriteLine($"Last edited: {notionPage.LastEditedTime:u}");
            Console.WriteLine($"URL: {notionPage.Url}");
            
            // Access property values
            var status = notionPage.GetProperty<string>("Status");
            var priority = notionPage.GetProperty<int>("Priority");
            Console.WriteLine($"Status: {status}, Priority: {priority}");
            
            // Update properties
            notionPage.SetProperty("Status", "Done");
            notionPage.SetProperty("Completed", DateTime.UtcNow);
            
            // Mark as stale for next sync
            notionPage.MarkAsStale();
            
            // Update sync timestamp
            notionPage.UpdateSyncTime();
            
            // Archive the page
            notionPage.Archive();
            
            // Validate the page
            bool isValid = notionPage.Validate();
            Console.WriteLine($"Page validation: {isValid}");
            
            // String representation
            Console.WriteLine($"Page info: {notionPage}");
        }
    }
}
```

## CalendarEvent

The `CalendarEvent` class represents calendar events that can be synchronized with tasks' due dates and schedules. It supports two-way synchronization between local tasks and iCal (.ics) calendar files, enabling tasks with due dates to be visible in external calendar applications. Events can originate from local tasks or be imported from external calendar sources.

### Usage Example

```csharp
using NotionTaskSync.Domain.Models;
using System;

class Program
{
    static void Main()
    {
        // Create a calendar event from a local task
        var taskEvent = new CalendarEvent
        {
            Title = "Implement Calendar Sync Feature",
            Description = "Add CalendarEvent documentation to README.md",
            StartDate = new DateTime(2026, 7, 18, 9, 0, 0), // July 18, 2026 at 9:00 AM
            EndDate = new DateTime(2026, 7, 18, 17, 0, 0),   // July 18, 2026 at 5:00 PM
            IsAllDay = false,
            Location = "Office Conference Room A",
            Source = CalendarEventSource.Task,
            LinkedTaskId = Guid.Parse("550e8400-e29b-41d4-a716-446655440000")
        };

        // Validate the event
        bool isValid = taskEvent.Validate();
        Console.WriteLine($"Event validation: {isValid}");

        // Calculate duration
        var duration = taskEvent.GetDuration();
        Console.WriteLine($"Event duration: {duration?.TotalHours} hours");

        // Create an all-day event imported from external calendar
        var importedEvent = new CalendarEvent
        {
            Title = "Team Meeting - Q3 Planning",
            Description = "Quarterly planning session for all teams",
            StartDate = new DateTime(2026, 7, 20),
            EndDate = new DateTime(2026, 7, 20),
            IsAllDay = true,
            Location = "Virtual",
            ExternalUid = "ical-uid-12345@example.com",
            Source = CalendarEventSource.Import
        };

        // Access event properties
        Console.WriteLine($"Event: {importedEvent.Title}");
        Console.WriteLine($"Start: {importedEvent.StartDate:yyyy-MM-dd}");
        Console.WriteLine($"End: {importedEvent.EndDate?.ToString("yyyy-MM-dd") ?? "N/A"}");
        Console.WriteLine($"All day: {importedEvent.IsAllDay}");
        Console.WriteLine($"Location: {importedEvent.Location}");
        Console.WriteLine($"External UID: {importedEvent.ExternalUid}");
        Console.WriteLine($"Created: {importedEvent.CreatedAt:u}");
        Console.WriteLine($"Updated: {importedEvent.UpdatedAt:u}");
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

## CliArgumentParser

`CliArgumentParser` provides command-line argument parsing and help text generation for console applications. It supports registering commands, global options, and positional arguments, then parsing command-line input into a structured format. The parser automatically generates comprehensive help documentation and handles error cases gracefully.

### Usage Example

```csharp
using NotionTaskSync.Cli;
using System;
using System.Collections.Generic;

class Program
{
    static async Task Main(string[] args)
    {
        // Create parser with command name and description
        var parser = new CliArgumentParser(
            commandName: "sync",
            description: "Synchronize tasks between local storage and Notion"
        );

        // Register a command with options and arguments
        parser.RegisterCommand(
            commandName: "run",
            description: "Execute a synchronization cycle",
            options: new Dictionary<string, string>
            {
                {"--database-id", "Notion database ID to sync with"},
                {"--direction", "Sync direction (local-to-notion, notion-to-local, bidirectional)"},
                {"--dry-run", "Preview changes without applying them"}
            },
            arguments: new List<string> { "config-name" }
        );

        // Register global options available for all commands
        parser.RegisterGlobalOption(
            "--verbose",
            "Enable verbose logging output"
        );

        // Parse command line arguments
        var parsedCommand = parser.Parse(args);

        if (parsedCommand == null || parsedCommand.Error != null)
        {
            Console.WriteLine(parser.GenerateHelpText());
            if (parsedCommand?.Error != null)
            {
                Console.WriteLine($"\nError: {parsedCommand.Error}");
            }
            return;
        }

        // Access parsed values
        Console.WriteLine($"Executing command: {parsedCommand.CommandName}");
        Console.WriteLine($"Config name: {parsedCommand.Arguments[0]}");
        
        if (parsedCommand.Options.TryGetValue("--database-id", out var databaseId))
        {
            Console.WriteLine($"Database ID: {databaseId}");
        }
        
        if (parsedCommand.Options.TryGetValue("--verbose", out var _))
        {
            Console.WriteLine("Verbose mode enabled");
        }

        // Execute the command
        var exitCode = await parsedCommand.ExecuteAsync();
        Environment.Exit(exitCode);
    }
}
```

## SyncPipeline

`SyncPipeline` provides a pipeline pattern implementation for organizing and executing sequential synchronization workflows. It enables composing multiple operations into a pipeline that shares context, handles errors, and tracks execution results through a context object. The pipeline uses dependency injection for logging and maintains shared state across steps.

### Usage Example

```csharp
using NotionTaskSync.Pipeline;
using NotionTaskSync.Domain.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

class Program
{
    static async Task Main()
    {
        // Setup logging (required for pipeline)
        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        var logger = loggerFactory.CreateLogger<SyncPipeline>();
        
        // Create a new pipeline
        var pipeline = new SyncPipeline(logger);
        
        // Create context for shared data and messages
        var context = new PipelineContext();
        
        // Define a sync step for loading tasks
        var loadStep = new FuncSyncStep("Load Local Tasks", async ctx =>
        {
            // Simulate loading tasks from local storage
            var tasks = new List<Domain.Models.Task>
            {
                new Domain.Models.Task { Id = Guid.NewGuid(), Title = "Implement SyncPipeline feature" },
                new Domain.Models.Task { Id = Guid.NewGuid(), Title = "Add documentation" }
            };
            ctx.SetData("localTasks", tasks);
            ctx.AddMessage("Loaded 2 local tasks");
            return true; // Success
        });
        
        pipeline.AddStep(loadStep);
        
        // Define a sync step for validation
        var validateStep = new FuncSyncStep("Validate Configuration", async ctx =>
        {
            // Simulate configuration validation
            var config = new SyncConfig
            {
                Name = "Test Sync",
                NotionDatabaseId = "550e8400-e29b-41d4-a716-446655440000"
            };
            
            if (string.IsNullOrEmpty(config.NotionDatabaseId))
            {
                ctx.AddMessage("Configuration validation failed: Database ID is required");
                return false; // Failure
            }
            
            ctx.SetData("syncConfig", config);
            ctx.AddMessage("Configuration validated successfully");
            return true; // Success
        });
        
        pipeline.AddStep(validateStep);
        
        // Define a sync step for syncing to Notion
        var syncStep = new FuncSyncStep("Sync to Notion", async ctx =>
        {
            // Retrieve shared data
            var config = ctx.GetData<SyncConfig>("syncConfig");
            var localTasks = ctx.GetData<List<Domain.Models.Task>>("localTasks");
            
            ctx.AddMessage($"Syncing {localTasks?.Count ?? 0} tasks to Notion database {config?.NotionDatabaseId}");
            
            // Simulate successful sync
            ctx.AddMessage("Successfully synced tasks to Notion");
            return true; // Success
        });
        
        pipeline.AddStep(syncStep);
        
        // Execute the pipeline
        var result = await pipeline.ExecuteAsync(context);
        
        Console.WriteLine($"\nPipeline execution completed:");
        Console.WriteLine($"- Success: {result.Success}");
        Console.WriteLine($"- Error: {result.ErrorMessage ?? "None"}");
        Console.WriteLine($"- Steps executed: {result.StepResults.Count}");
        
        // Display step results
        Console.WriteLine("\nStep execution details:");
        foreach (var stepResult in result.StepResults)
        {
            Console.WriteLine($"- {stepResult.StepName}: {stepResult.Success} at {stepResult.ExecutedAt:HH:mm:ss}");
        }
        
        // Display collected messages
        Console.WriteLine("\nMessages:");
        foreach (var message in context.Messages)
        {
            Console.WriteLine($"- {message}");
        }
        
        // Access shared data
        var syncedTasks = context.GetData<List<Domain.Models.Task>>("localTasks");
        Console.WriteLine($"\nShared data contains {syncedTasks?.Count ?? 0} tasks");
    }
}

// Helper class for creating sync steps from functions
public class FuncSyncStep : ISyncStep
{
    private readonly Func<PipelineContext, Task<bool>> _func;
    
    public FuncSyncStep(string name, Func<PipelineContext, Task<bool>> func, bool isCritical = false)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        _func = func ?? throw new ArgumentNullException(nameof(func));
        IsCritical = isCritical;
    }
    
    public string Name { get; }
    public bool IsCritical { get; }
    
    public async Task<bool> ExecuteAsync(PipelineContext context)
    {
        return await _func(context);
    }
}
```

## TaskProperty

The `TaskProperty` class represents a custom property or extended attribute for tasks, enabling flexible schema support for additional metadata beyond the standard task fields. It supports various data types (string, integer, decimal, boolean, datetime, JSON) and provides validation, type conversion, and synchronization control between local storage and Notion.

### Usage Example

```csharp
using NotionTaskSync.Domain.Models;
using System;

class Program
{
    static void Main()
    {
        // Create a task property for tracking estimated hours
        var estimatedHoursProperty = new TaskProperty(
            propertyName: "EstimatedHours",
            propertyValue: "8",
            dataType: PropertyDataType.Integer
        )
        {
            Id = Guid.NewGuid(),
            TaskId = Guid.Parse("550e8400-e29b-41d4-a716-446655440000"),
            IsRequired = true,
            SyncToNotion = true,
            SyncToLocal = true
        };

        // Validate the property
        if (estimatedHoursProperty.Validate())
        {
            Console.WriteLine($"Valid property: {estimatedHoursProperty}");
            Console.WriteLine($"Data type: {estimatedHoursProperty.DataType}");
            Console.WriteLine($"Typed value: {estimatedHoursProperty.GetTypedValue<int>()}");
        }

        // Update the property value
        var updateSuccess = estimatedHoursProperty.UpdateValue("12");
        Console.WriteLine($"Update successful: {updateSuccess}, New value: {estimatedHoursProperty.PropertyValue}");

        // Create a string property for custom tags
        var tagsProperty = new TaskProperty(
            propertyName: "CustomTags",
            propertyValue: "backend,api,performance",
            dataType: PropertyDataType.String
        )
        {
            Id = Guid.NewGuid(),
            TaskId = Guid.Parse("123e4567-e89b-12d3-a456-426614174000"),
            IsRequired = false
        };

        // Get typed value (returns string)
        var tags = tagsProperty.GetTypedValue<string>();
        Console.WriteLine($"Task tags: {tags}");

        // Create a boolean property for tracking completion status
        var isHighPriorityProperty = new TaskProperty(
            propertyName: "IsHighPriority",
            propertyValue: "true",
            dataType: PropertyDataType.Boolean
        )
        {
            Id = Guid.NewGuid(),
            TaskId = Guid.Parse("abcdef12-3456-7890-abcd-ef1234567890"),
            IsRequired = false
        };

        // Get typed boolean value
        var isHighPriority = isHighPriorityProperty.GetTypedValue<bool>();
        Console.WriteLine($"Is high priority: {isHighPriority}");

        // Create a datetime property for deadline tracking
        var deadlineProperty = new TaskProperty(
            propertyName: "Deadline",
            propertyValue: "2026-07-31T23:59:59Z",
            dataType: PropertyDataType.DateTime
        )
        {
            Id = Guid.NewGuid(),
            TaskId = Guid.Parse("550e8400-e29b-41d4-a716-446655440000")
        };

        // Get typed datetime value
        var deadline = deadlineProperty.GetTypedValue<DateTime>();
        Console.WriteLine($"Deadline: {deadline:yyyy-MM-dd}");
    }
}
```

## SyncException

`SyncException` is the base exception type for all synchronization-related errors in the Notion Task Sync application. It provides contextual information about failed sync operations including configuration IDs, timestamps, and detailed error context. This exception hierarchy enables precise error handling and logging throughout the sync pipeline.

### Usage Example

```csharp
using NotionTaskSync.Domain.Exceptions;
using NotionTaskSync.Domain.Models;
using System;

class Program
{
    static void Main()
    {
        try
        {
            // Simulate a sync operation that might fail
            ExecuteSyncOperation("production-sync-config", "550e8400-e29b-41d4-a716-446655440000");
        }
        catch (SyncException ex) when (ex is NotionApiException)
        {
            Console.WriteLine($"Notion API error: {ex.Message}");
            if (ex is NotionApiException notionEx && notionEx.HttpStatusCode.HasValue)
            {
                Console.WriteLine($"HTTP Status: {notionEx.HttpStatusCode}");
            }
        }
        catch (SyncException ex)
        {
            Console.WriteLine($"Sync failed: {ex.Message}");
            Console.WriteLine($"Configuration: {ex.SyncConfigId ?? "unknown"}");
            Console.WriteLine($"Occurred at: {ex.OccurredAt:u}");
            
            if (!string.IsNullOrEmpty(ex.Details))
            {
                Console.WriteLine($"Details: {ex.Details}");
            }
            
            // Create a new exception with additional context
            var detailedException = SyncException.CreateWithContext(
                "Failed to sync task due to network issues",
                ex.SyncConfigId,
                $"Task ID: {Guid.NewGuid()}, Database: {ex.SyncConfigId}"
            );
            
            // Handle specific exception types
            if (ex is ValidationException validationEx)
            {
                Console.WriteLine($"Validation failed for field: {validationEx.FieldName}");
                Console.WriteLine($"Invalid value: {validationEx.InvalidValue}");
            }
            else if (ex is LocalFileException fileEx)
            {
                Console.WriteLine($"File operation failed: {fileEx.FilePath}");
            }
            else if (ex is ConflictException conflictEx)
            {
                Console.WriteLine($"Conflict detected for task: {conflictEx.TaskId}");
                Console.WriteLine($"Conflict details: {conflictEx.ConflictDetails}");
            }
        }
    }
    
    static void ExecuteSyncOperation(string configId, string databaseId)
    {
        // Simulate various sync exceptions
        throw new NotionApiException("Rate limit exceeded for Notion API")
        {
            SyncConfigId = configId,
            HttpStatusCode = 429,
            ApiErrorCode = "rate_limited"
        };
        
        // throw new LocalFileException("Failed to write task file")
        // {
        //     SyncConfigId = configId,
        //     FilePath = @"/tasks/task-123.md"
        // };
        
        // throw new ValidationException("Invalid task priority")
        // {
        //     SyncConfigId = configId,
        //     FieldName = "Priority",
        //     InvalidValue = 150
        // };
        
        // throw new ConflictException("Task modified in both local and Notion")
        // {
        //     SyncConfigId = configId,
        //     TaskId = Guid.NewGuid(),
        //     ConflictDetails = "Title and Description fields conflict"
        // };
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

## VectorClock

The `VectorClock` class implements a Lamport-style vector clock for tracking causal ordering of distributed operations in collaborative task synchronization scenarios. It maintains a thread-safe collection of logical timestamps for each participant, enabling precise detection of concurrent modifications, operation ordering, and conflict resolution.

Vector clocks are particularly useful in distributed systems where multiple participants can modify shared data concurrently. Each participant maintains their own logical clock that advances with each local operation, and vector clocks can be merged to establish a partial ordering of events across the entire system.

### Usage Example

```csharp
using Domain.Models;
using System;
using System.Collections.Generic;

class Program
{
    static void Main()
    {
        // Create vector clocks for two collaboration participants
        var aliceClock = new VectorClock();
        var bobClock = new VectorClock();

        // Alice performs some operations and advances her clock
        aliceClock.Tick("alice@example.com"); // Operation 1
        aliceClock.Tick("alice@example.com"); // Operation 2
        Console.WriteLine($"Alice's clock: {FormatClock(aliceClock)}"); // { "alice@example.com": 2 }

        // Bob performs some operations and advances his clock
        bobClock.Tick("bob@example.com"); // Operation A
        bobClock.Tick("bob@example.com"); // Operation B
        bobClock.Tick("bob@example.com"); // Operation C
        Console.WriteLine($"Bob's clock: {FormatClock(bobClock)}"); // { "bob@example.com": 3 }

        // Alice receives Bob's operations and merges his clock
        aliceClock.Merge(bobClock);
        Console.WriteLine($"Alice after merge: {FormatClock(aliceClock)}");
        // { "alice@example.com": 2, "bob@example.com": 3 }

        // Bob receives Alice's operations and merges her clock
        bobClock.Merge(aliceClock);
        Console.WriteLine($"Bob after merge: {FormatClock(bobClock)}");
        // { "alice@example.com": 2, "bob@example.com": 3 }

        // Check causal relationships between clocks
        var earlierClock = new VectorClock();
        earlierClock.Tick("alice@example.com");
        var laterClock = new VectorClock();
        laterClock.Tick("alice@example.com");
        laterClock.Tick("alice@example.com");

        Console.WriteLine($"Earlier happens before later: {earlierClock.HappensBefore(laterClock)}"); // True
        Console.WriteLine($"Later happens before earlier: {laterClock.HappensBefore(earlierClock)}"); // False

        // Clone a vector clock for safe sharing
        var clonedClock = aliceClock.Clone();
        Console.WriteLine($"Cloned clock equals original: {clonedClock.Components.Count == aliceClock.Components.Count}");

        // Get specific participant's timestamp
        var aliceTimestamp = aliceClock.Get("alice@example.com");
        var unknownParticipantTimestamp = aliceClock.Get("unknown@example.com"); // Returns 0
        Console.WriteLine($"Alice's timestamp: {aliceTimestamp}, Unknown participant: {unknownParticipantTimestamp}");
    }

    static string FormatClock(VectorClock clock)
    {
        var components = new List<string>();
        foreach (var kvp in clock.Components)
        {
            components.Add($"\"{kvp.Key}\": {kvp.Value}");
        }
        return "{ " + string.Join(", ", components) + " }";
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

## CacheProvider

The `CacheProvider` class provides an in-memory caching solution for reducing API calls to Notion by storing frequently accessed data with configurable expiration times. It implements automatic expiration, thread-safe operations, and comprehensive cache statistics for monitoring cache health.

### Public Members

- `public T? Get<T>(string key)` - Retrieves a cached value if it exists and hasn't expired
- `public void Set<T>(string key, T value, TimeSpan? expiration = null)` - Stores a value in cache with optional expiration
- `public T GetOrSet<T>(string key, Func<T> factory, TimeSpan? expiration = null)` - Gets cached value or computes and caches it if not found
- `public async System.Threading.Tasks.Task<T> GetOrSetAsync<T>(string key, Func<System.Threading.Tasks.Task<T>> factory, TimeSpan? expiration = null)` - Async version of GetOrSet
- `public bool Remove(string key)` - Removes a specific key from cache
- `public int RemoveByPattern(string pattern)` - Removes all cache entries matching a pattern
- `public void Clear()` - Clears all cache entries
- `public int RemoveExpired()` - Removes all expired entries from cache
- `public CacheStatistics GetStatistics()` - Gets cache statistics

### Usage Example

```csharp
using NotionTaskSync.Caching;
using Microsoft.Extensions.Logging;
using System;

class Program
{
    static void Main()
    {
        // Initialize CacheProvider with logger
        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        var logger = loggerFactory.CreateLogger<CacheProvider>();
        var cacheProvider = new CacheProvider(logger);

        // Example 1: Basic caching with Set and Get
        cacheProvider.Set("user:123", "John Doe", TimeSpan.FromMinutes(30));
        var cachedUser = cacheProvider.Get<string>("user:123");
        Console.WriteLine(cachedUser); // Output: John Doe

        // Example 2: GetOrSet for lazy-loading with automatic caching
        var task = cacheProvider.GetOrSet("expensive:task", () => {
            Console.WriteLine("Computing expensive task...");
            return ComputeExpensiveResult();
        });
        
        // Subsequent calls will use cached value
        var cachedTask = cacheProvider.GetOrSet("expensive:task", () => {
            Console.WriteLine("This won't be called - value is cached");
            return "ignored";
        });

        // Example 3: Async caching for I/O operations
        var asyncTask = await cacheProvider.GetOrSetAsync("api:data", async () => {
            Console.WriteLine("Fetching data from API...");
            await Task.Delay(100); // Simulate API call
            return "API Response Data";
        });

        // Example 4: Cache invalidation
        cacheProvider.Set("notion:pages", new List<string> { "page1", "page2" });
        cacheProvider.Remove("notion:pages");
        var removedValue = cacheProvider.Get<List<string>>("notion:pages");
        Console.WriteLine(removedValue == null ? "Value removed successfully" : "Still cached");

        // Example 5: Pattern-based invalidation
        cacheProvider.Set("notion:pages:123", "data1");
        cacheProvider.Set("notion:pages:456", "data2");
        cacheProvider.Set("notion:users:789", "user1");
        
        var removedCount = cacheProvider.RemoveByPattern("notion:pages");
        Console.WriteLine($"Removed {removedCount} entries matching 'notion:pages'");

        // Example 6: Get cache statistics
        var stats = cacheProvider.GetStatistics();
        Console.WriteLine($"Cache stats - Total: {stats.TotalEntries}, Valid: {stats.ValidEntries}, " +
                        $"Expired: {stats.ExpiredEntries}, Size: {stats.ApproximateSizeBytes} bytes");
        Console.WriteLine($"Valid entries: {stats.ValidEntriesPercentage:F1}%");

        // Example 7: Clear entire cache
        cacheProvider.Clear();
        var emptyStats = cacheProvider.GetStatistics();
        Console.WriteLine($"Cache cleared. Total entries: {emptyStats.TotalEntries}");
    }
    
    static string ComputeExpensiveResult()
    {
        return "Expensive Result";
    }
}
```
## ValidationHelperJsonExtensions

The `ValidationHelperJsonExtensions` class provides System.Text.Json serialization extensions for validation-related data structures. It enables JSON serialization and deserialization of `ValidationResult` instances, making it easy to persist or transmit validation outcomes.

### Public Members

- `ToJson(this ValidationResult result, bool indented = false)` - Serializes a validation result to a JSON string
- `FromJson(string json)` - Deserializes a JSON string to a ValidationResult instance
- `TryFromJson(string json, out ValidationResult? result)` - Attempts to deserialize a JSON string to a ValidationResult instance

### Usage Example

```csharp
using NotionTaskSync.Utils;
using System.Text.Json;

// Example 1: Creating and serializing a successful validation result
var successResult = ValidationResult.Success(
    value: "valid-email@example.com", 
    validationType: "EmailFormat"
);

string json = successResult.ToJson(indented: true);
Console.WriteLine("Serialized validation result:");
Console.WriteLine(json);

// Example 2: Deserializing from JSON
ValidationResult? deserializedResult = ValidationHelperJsonExtensions.FromJson(json);
if (deserializedResult != null)
{
    Console.WriteLine($"Deserialized - IsValid: {deserializedResult.IsValid}");
    Console.WriteLine($"Value: {deserializedResult.Value}");
    Console.WriteLine($"ValidationType: {deserializedResult.ValidationType}");
}

// Example 3: Using TryFromJson for safe deserialization
ValidationResult? safeResult = null;
if (ValidationHelperJsonExtensions.TryFromJson(json, out safeResult))
{
    Console.WriteLine($"TryFromJson successful - IsValid: {safeResult.IsValid}");
    Console.WriteLine($"Value: {safeResult.Value}");
    Console.WriteLine($"ValidationType: {safeResult.ValidationType}");
}
else
{
    Console.WriteLine("Failed to deserialize JSON");
}

// Example 4: Working with failed validation results
var failureResult = ValidationResult.Failure(
    errorMessage: "Email format is invalid",
    value: "invalid-email",
    validationType: "EmailFormat"
);

string failureJson = failureResult.ToJson();
Console.WriteLine("\nFailed validation result JSON:");
Console.WriteLine(failureJson);

// Example 5: Round-trip serialization
ValidationResult? roundTripResult = ValidationHelperJsonExtensions.FromJson(failureJson);
if (roundTripResult != null)
{
    Console.WriteLine($"\nRound-trip result - IsValid: {roundTripResult.IsValid}");
    Console.WriteLine($"ErrorMessage: {roundTripResult.ErrorMessage}");
    Console.WriteLine($"Value: {roundTripResult.Value}");
}
```
## CollectionExtensionsJsonExtensions

The `CollectionExtensionsJsonExtensions` class provides System.Text.Json serialization extensions for collection extension utilities. It contains a serializable marker type that enables JSON serialization of collection extension concepts.

### Public Members

- `CollectionExtensionsMarker` - Serializable marker type representing collection extension utilities
- `bool Equals(CollectionExtensionsMarker? other)` - Determines whether the specified object is equal to the current object
- `override bool Equals(object? obj)` - Determines whether the specified object is equal to the current object
- `override int GetHashCode()` - Returns the hash code for this instance
- `override string ToString()` - Returns a string representation of the object
- `static string ToJson(this CollectionExtensionsMarker value, bool indented = false)` - Serializes collection extension utilities to a JSON string
- `static CollectionExtensionsMarker? FromJson(string json)` - Deserializes a JSON string to a CollectionExtensionsMarker instance
- `static bool TryFromJson(string json, out CollectionExtensionsMarker? value)` - Attempts to deserialize a JSON string to a CollectionExtensionsMarker instance

### Usage Example

```csharp
using NotionTaskSync.Utils;

// Example 1: Creating and serializing a collection extensions marker
var marker = new CollectionExtensionsMarker();

// Serialize to JSON
string json = marker.ToJson(indented: true);
Console.WriteLine("Serialized collection extensions marker:");
Console.WriteLine(json);

// Example 2: Deserializing from JSON
CollectionExtensionsMarker? deserializedMarker = CollectionExtensionsJsonExtensions.FromJson(json);
if (deserializedMarker != null)
{
    Console.WriteLine($"Deserialized marker type: {deserializedMarker.GetType().Name}");
    Console.WriteLine($"ToString(): {deserializedMarker.ToString()}");
}

// Example 3: Using TryFromJson for safe deserialization
CollectionExtensionsMarker? safeMarker = null;
if (CollectionExtensionsJsonExtensions.TryFromJson(json, out safeMarker))
{
    Console.WriteLine($"TryFromJson successful - Type: {safeMarker?.GetType().Name}");
}
else
{
    Console.WriteLine("Failed to deserialize JSON");
}

// Example 4: Equality checks
var marker1 = new CollectionExtensionsMarker();
var marker2 = new CollectionExtensionsMarker();
Console.WriteLine($"Reference equality: {ReferenceEquals(marker1, marker2)}"); // False (different instances)
Console.WriteLine($"Value equality: {marker1.Equals(marker2)}"); // False (reference equality)

// Example 5: Round-trip serialization
string roundTripJson = marker1.ToJson();
CollectionExtensionsMarker? roundTripMarker = CollectionExtensionsJsonExtensions.FromJson(roundTripJson);
if (roundTripMarker != null)
{
    Console.WriteLine($"\nRound-trip marker type: {roundTripMarker.GetType().Name}");
    Console.WriteLine($"ToString(): {roundTripMarker.ToString()}");
}
```
