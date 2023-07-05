// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

# Architecture Overview

This document describes the internal architecture, design patterns, and data flow of Notion Task Sync.

## System Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                         PRESENTATION LAYER                      │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐           │
│  │  CLI Module  │  │  REST API    │  │  Event Bus   │           │
│  └──────┬───────┘  └──────┬───────┘  └──────┬───────┘           │
└─────────┼────────────────┼────────────────┼──────────────────────┘
          │                │                │
┌─────────▼────────────────▼────────────────▼──────────────────────┐
│                      APPLICATION LAYER                           │
│  ┌──────────────────────────────────────────────────────┐        │
│  │  SyncService (Orchestrator)                          │        │
│  │  - Manages sync workflow                             │        │
│  │  - Coordinates sub-services                          │        │
│  └──────────┬──────────────────────────────┬────────────┘        │
│             │                              │                     │
│  ┌──────────▼─────────┐    ┌───────────────▼────────┐            │
│  │ ChangeDetection    │    │ ConflictResolution     │            │
│  │ Service            │    │ Service                │            │
│  │ - Diff detection   │    │ - Strategy application │            │
│  │ - Change tracking  │    │ - Manual resolution    │            │
│  └──────┬─────────────┘    └──────┬────────────────┘            │
│         │                         │                              │
└─────────┼─────────────────────────┼──────────────────────────────┘
          │                         │
┌─────────▼─────────────────────────▼──────────────────────────────┐
│                    BUSINESS LOGIC LAYER                          │
│  ┌──────────────┐  ┌────────────────────┐  ┌────────────────┐   │
│  │ Local File   │  │ Notion API Service │  │ Backup Service │   │
│  │ Service      │  │ - REST client      │  │ - Backup/      │   │
│  │ - Read/Write │  │ - Caching          │  │   Restore      │   │
│  └──────┬───────┘  └────────┬───────────┘  └────────┬───────┘   │
│         │                   │                       │            │
│  ┌──────▼─────────────────────────────────────────┐ │            │
│  │ Cache Provider                                │ │            │
│  │ - In-memory cache                            │ │            │
│  │ - Expires on TTL or manual invalidation     │ │            │
│  └──────┬──────────────────────────────────────┘ │            │
│         │                                        │            │
└─────────┼────────────────────────────────────────┼────────────┘
          │                                        │
┌─────────▼────────────────────────────────────────▼───────────────┐
│                      DATA ACCESS LAYER                           │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐            │
│  │ Local FS     │  │ HTTP Client  │  │ SQLite DB    │            │
│  │ Read/Write   │  │ REST Calls   │  │ Repository   │            │
│  └──────┬───────┘  └──────┬───────┘  └──────┬───────┘            │
│         │                │                 │                    │
└─────────▼─────────────────▼─────────────────▼────────────────────┘
          │                │                 │
     Filesystem        Notion API        SQLite DB
```

## Component Description

### Presentation Layer

#### CLI Module (`Cli/CliArgumentParser.cs`)
- Parses command-line arguments
- Routes to appropriate commands (sync, configure, status, help)
- Handles user input validation
- Formats console output

#### Commands (`Commands/`)
- `ConfigureCommand` - Setup new sync profiles
- `SyncCommand` - Execute synchronization
- `StatusCommand` - Display current status
- `HelpCommand` - Display help information

#### Event Bus (`Events/EventBus.cs`)
- Pub/sub event system
- Decouples components
- Handles async event handlers
- Built-in handlers for ConflictDetected, SyncCompleted

### Application Layer

#### SyncService (`Services/SyncService.cs`)
The central orchestrator managing the entire sync workflow:

```csharp
public async Task<SyncResult> ExecuteSyncAsync(SyncConfig config)
{
    // 1. Validation
    ValidateConfiguration(config);
    
    // 2. Create backup (if enabled)
    await BackupService.CreateBackupAsync(config);
    
    // 3. Load current state
    var localTasks = await LocalFileService.LoadTasksAsync();
    var notionPages = await NotionApiService.FetchPagesAsync();
    
    // 4. Detect changes
    var changes = await ChangeDetectionService.DetectChangesAsync(
        localTasks, notionPages);
    
    // 5. Identify conflicts
    var conflicts = IdentifyConflicts(changes);
    
    // 6. Resolve conflicts
    var resolutions = await ConflictResolutionService.ResolveAsync(
        conflicts, config.Strategy);
    
    // 7. Apply changes
    await ApplyChangesAsync(changes, resolutions);
    
    // 8. Persist sync state
    await ChangeLogRepository.RecordChangesAsync(changes);
    
    // 9. Publish events
    await EventBus.PublishAsync(new SyncCompletedEvent(...));
    
    return new SyncResult { ... };
}
```

#### ChangeDetectionService (`Services/ChangeDetectionService.cs`)
Compares local and Notion states to identify modifications:

```csharp
public async Task<List<Change>> DetectChangesAsync(
    List<Task> local, List<NotionPage> notion)
{
    var changes = new List<Change>();
    
    // Compare each task
    foreach (var localTask in local)
    {
        var notionPage = notion.FirstOrDefault(p => 
            p.Id == localTask.NotionPageId);
        
        if (notionPage == null)
            changes.Add(new Change(ChangeType.Deleted, localTask));
        else if (HasChanged(localTask, notionPage))
            changes.Add(new Change(ChangeType.Modified, localTask));
    }
    
    // Find new Notion pages not in local
    foreach (var page in notion)
    {
        if (!local.Any(t => t.NotionPageId == page.Id))
            changes.Add(new Change(ChangeType.Created, page));
    }
    
    return changes;
}

private bool HasChanged(Task local, NotionPage notion)
    => local.ModifiedAt < notion.LastEditedTime
    || !FieldsEqual(local, notion);
```

#### ConflictResolutionService (`Services/ConflictResolutionService.cs`)
Applies conflict resolution strategies when changes conflict:

```csharp
public async Task<List<Resolution>> ResolveAsync(
    List<Conflict> conflicts, ResolutionStrategy strategy)
{
    var resolutions = new List<Resolution>();
    
    foreach (var conflict in conflicts)
    {
        var resolution = strategy switch
        {
            "latest-wins" => conflict.LocalChange.Timestamp > 
                conflict.NotionChange.Timestamp
                ? conflict.LocalChange 
                : conflict.NotionChange,
            
            "local-priority" => conflict.LocalChange,
            
            "notion-priority" => conflict.NotionChange,
            
            "merge" => await MergeChangesAsync(conflict),
            
            "manual" => await PromptUserAsync(conflict),
            
            _ => throw new InvalidOperationException()
        };
        
        resolutions.Add(new Resolution(conflict, resolution));
        
        if (conflict.IsConflict)
            await EventBus.PublishAsync(
                new ConflictDetectedEvent(conflict));
    }
    
    return resolutions;
}
```

### Business Logic Layer

#### LocalFileService (`Services/LocalFileService.cs`)
Manages local task file I/O:

```csharp
public async Task<List<Task>> LoadTasksAsync(string folderPath)
{
    var tasks = new List<Task>();
    var files = Directory.GetFiles(folderPath, "*.json");
    
    foreach (var file in files)
    {
        var json = await File.ReadAllTextAsync(file);
        var task = JsonConvert.DeserializeObject<Task>(json);
        tasks.Add(task);
    }
    
    return tasks;
}

public async Task SaveTaskAsync(Task task)
{
    task.ModifiedAt = DateTime.UtcNow;
    var json = JsonConvert.SerializeObject(task, Formatting.Indented);
    var filePath = Path.Combine(_basePath, $"{task.Id}.json");
    await File.WriteAllTextAsync(filePath, json);
}
```

#### NotionApiService (`Services/NotionApiService.cs`)
Interfaces with Notion API with caching and rate limiting:

```csharp
public async Task<List<NotionPage>> FetchPagesAsync(string databaseId)
{
    // Check cache first
    var cacheKey = CacheKey.Build("notion_pages", databaseId);
    if (_cache.TryGet(cacheKey, out var cached))
        return cached as List<NotionPage>;
    
    // Apply rate limiting
    await _rateLimiter.AcquireAsync();
    
    // Fetch from API
    var response = await _httpClient.PostAsync(
        $"/databases/{databaseId}/query",
        new { filter = new { } });
    
    var pages = ParseResponse(response);
    
    // Cache result
    _cache.Set(cacheKey, pages, TimeSpan.FromMinutes(5));
    
    return pages;
}
```

#### BackupService (`Services/BackupService.cs`)
Creates and manages backups before sync operations:

```csharp
public async Task<string> CreateBackupAsync(
    string syncConfigId, string backupName)
{
    var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
    var backupDir = Path.Combine(_backupPath, 
        $"{backupName}_{timestamp}");
    
    Directory.CreateDirectory(backupDir);
    
    // Backup local files
    await CopyDirectoryAsync(_localPath, 
        Path.Combine(backupDir, "local"));
    
    // Backup database
    await _database.ExportAsync(
        Path.Combine(backupDir, "database.db"));
    
    // Create manifest
    var manifest = new BackupManifest
    {
        CreatedAt = DateTime.UtcNow,
        SyncConfigId = syncConfigId,
        FilesBackedUp = Directory.GetFiles(_localPath).Length
    };
    
    await File.WriteAllTextAsync(
        Path.Combine(backupDir, "manifest.json"),
        JsonConvert.SerializeObject(manifest));
    
    return backupDir;
}
```

### Data Access Layer

#### TaskRepository (`Data/Repositories/TaskRepository.cs`)
Persists task metadata and sync state:

```csharp
public async Task<List<Task>> GetAllAsync()
{
    using var connection = _dbConnection.GetConnection();
    var tasks = await connection.QueryAsync<Task>(
        "SELECT * FROM Tasks ORDER BY CreatedAt DESC");
    return tasks.ToList();
}

public async Task SaveAsync(Task task)
{
    using var connection = _dbConnection.GetConnection();
    await connection.ExecuteAsync(
        @"INSERT OR REPLACE INTO Tasks 
          (Id, Title, Status, Priority, ModifiedAt) 
          VALUES (@Id, @Title, @Status, @Priority, @ModifiedAt)",
        task);
}
```

#### ChangeLogRepository (`Data/Repositories/ChangeLogRepository.cs`)
Tracks all sync operations and changes:

```csharp
public async Task<List<ChangeLog>> GetLatestChangesAsync(
    string syncConfigId, int limit)
{
    using var connection = _dbConnection.GetConnection();
    var changes = await connection.QueryAsync<ChangeLog>(
        @"SELECT * FROM ChangeLogs 
          WHERE SyncConfigId = @ConfigId
          ORDER BY CreatedAt DESC
          LIMIT @Limit",
        new { ConfigId = syncConfigId, Limit = limit });
    
    return changes.ToList();
}
```

## Data Models

### Task Domain Model

```csharp
public class Task
{
    public string Id { get; set; }
    public string NotionPageId { get; set; }
    public string Title { get; set; }
    public TaskStatus Status { get; set; }
    public TaskPriority Priority { get; set; }
    public DateTime? DueDate { get; set; }
    public List<string> Tags { get; set; }
    public string AssigneeId { get; set; }
    public string Description { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime ModifiedAt { get; set; }
    public DateTime? SyncedAt { get; set; }
    
    public bool Validate() => !string.IsNullOrWhiteSpace(Title);
}
```

### Change Tracking

```csharp
public class Change
{
    public string Id { get; set; }
    public string TaskId { get; set; }
    public ChangeType Type { get; set; }
    public string Source { get; set; } // "local" or "notion"
    public DateTime DetectedAt { get; set; }
    public Dictionary<string, object> NewValues { get; set; }
    public Dictionary<string, object> OldValues { get; set; }
}

public enum ChangeType { Created, Modified, Deleted }
```

### Conflict Model

```csharp
public class Conflict
{
    public string TaskId { get; set; }
    public Change LocalChange { get; set; }
    public Change NotionChange { get; set; }
    
    public bool IsConflict => LocalChange != null && NotionChange != null
        && LocalChange.Timestamp > SyncedAt
        && NotionChange.Timestamp > SyncedAt;
}
```

## Design Patterns Used

### Dependency Injection
Services are registered in DependencyInjection.cs and injected via constructor:

```csharp
services.AddScoped<SyncService>();
services.AddScoped<ChangeDetectionService>();
services.AddScoped<ConflictResolutionService>();
```

### Repository Pattern
Data access abstracted through repository interfaces:

```csharp
public interface ITaskRepository
{
    Task<List<Task>> GetAllAsync();
    Task SaveAsync(Task task);
    Task DeleteAsync(string taskId);
}
```

### Strategy Pattern
Conflict resolution strategies implemented as interchangeable algorithms:

```csharp
public interface IConflictResolutionStrategy
{
    Task<Change> ResolveAsync(Conflict conflict);
}

public class LatestWinsStrategy : IConflictResolutionStrategy
{
    public async Task<Change> ResolveAsync(Conflict conflict)
        => conflict.LocalChange.Timestamp > conflict.NotionChange.Timestamp
            ? conflict.LocalChange 
            : conflict.NotionChange;
}
```

### Observer Pattern
Event system for loose coupling:

```csharp
public interface IEventHandler<T>
{
    Task HandleAsync(T @event);
}

public class ConflictDetectedHandler : IEventHandler<ConflictDetectedEvent>
{
    public async Task HandleAsync(ConflictDetectedEvent @event)
    {
        // Custom conflict handling logic
    }
}
```

### Caching Strategy
Decorator pattern for API service caching:

```csharp
public class CachedNotionApiService : INotionApiService
{
    private readonly INotionApiService _inner;
    private readonly ICacheProvider _cache;
    
    public async Task<List<NotionPage>> FetchPagesAsync(string dbId)
    {
        var cached = _cache.Get(CacheKey.Build("pages", dbId));
        if (cached != null) return cached;
        
        var result = await _inner.FetchPagesAsync(dbId);
        _cache.Set(CacheKey.Build("pages", dbId), result, TTL);
        return result;
    }
}
```

## Concurrency & Threading

### Async/Await
All I/O operations use async/await for non-blocking execution:

```csharp
public async Task<SyncResult> ExecuteSyncAsync(SyncConfig config)
{
    // Parallel local and Notion loads
    var localTask = LocalFileService.LoadTasksAsync(config.LocalPath);
    var notionTask = NotionApiService.FetchPagesAsync(config.DatabaseId);
    
    await Task.WhenAll(localTask, notionTask);
    
    var local = localTask.Result;
    var notion = notionTask.Result;
}
```

### Rate Limiting
Semaphore-based rate limiter prevents API throttling:

```csharp
public class RateLimiter
{
    private readonly SemaphoreSlim _semaphore;
    private readonly int _allowedRequests;
    private readonly TimeSpan _window;
    
    public async Task AcquireAsync()
    {
        await _semaphore.WaitAsync();
        // Reset counter every window
    }
}
```

## Error Handling

### Custom Exceptions
Domain-specific exceptions for different failure scenarios:

```csharp
public class SyncException : Exception { }
public class NotionApiException : SyncException { }
public class ConfigurationException : SyncException { }
public class ConflictException : SyncException { }
```

### Retry Logic
Transient failures retried with exponential backoff:

```csharp
public async Task<T> ExecuteWithRetryAsync<T>(
    Func<Task<T>> operation, int maxRetries = 3)
{
    for (int attempt = 0; attempt < maxRetries; attempt++)
    {
        try
        {
            return await operation();
        }
        catch (Exception ex) when (IsTransient(ex) && attempt < maxRetries - 1)
        {
            await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, attempt)));
        }
    }
}
```

## Performance Considerations

### Caching Strategy
- **In-memory cache** for frequently accessed data (default TTL: 5 minutes)
- **Cache invalidation** on successful sync operations
- **Cache statistics** exposed for monitoring

### Batch Processing
- Process tasks in batches to reduce memory usage
- Configurable batch size in configuration
- Pagination for large datasets

### Connection Pooling
- HTTP client reused across requests
- SQLite connection pooling enabled
- File system operations buffered

## Deployment Architecture

```
┌──────────────────────────────────────────────────────┐
│              Kubernetes / Docker Swarm              │
├──────────────────────────────────────────────────────┤
│                                                       │
│  ┌────────────────────────────────────────────────┐  │
│  │  Notion Task Sync Pod                         │  │
│  │  ┌──────────────────────────────────────────┐ │  │
│  │  │ dotnet run -- sync                      │ │  │
│  │  │ (or web service on port 5000)           │ │  │
│  │  └──────────────────────────────────────────┘ │  │
│  └────────────────────────────────────────────────┘  │
│                       │                              │
│  ┌────────────────────▼─────────────────────────┐  │
│  │  Persistent Volume (Tasks & Backups)        │  │
│  └────────────────────────────────────────────────┘  │
│                                                       │
└──────────────────────────────────────────────────────┘
         │
         ├─► Notion API (Cloud)
         └─► SQLite Database (Local/Persistent)
```

---

**Built by [Vladyslav Zaiets](https://sarmkadan.com) - CTO & Software Architect**
