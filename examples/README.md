# Code Examples

This directory contains practical examples demonstrating Notion Task Sync usage and integration patterns.

## Quick Start - Essential Examples

These three examples cover the most common use cases and are recommended starting points:

### 1. BasicUsage.cs - Minimal Setup and First Sync
**Difficulty**: Beginner | **Time**: 5 minutes

The simplest way to use Notion Task Sync - load configuration and execute a sync.

**Key Concepts:**
- Setting up dependency injection
- Creating a sync configuration
- Executing sync operation
- Handling basic results

**When to use:** First-time users, simple integration, getting started quickly

**Run it:**
```bash
cd examples
# The example is self-contained - just compile and run
```

**What you'll learn:**
```csharp
// Minimal setup pattern
var services = new ServiceCollection();
services.AddLogging(builder => builder.AddConsole());
services.AddApplicationServices(configuration);

var syncService = serviceProvider.GetRequiredService<SyncService>();

var config = new SyncConfig(
    name: "MyFirstSync",
    notionDatabaseId: "your_database_id_here",
    localFolderPath: "./tasks"
);

var result = await syncService.ExecuteSyncAsync(config);
```

---

### 2. AdvancedUsage.cs - Configuration and Custom Options
**Difficulty**: Intermediate | **Time**: 15 minutes

Advanced configuration with custom options, field mappings, error handling, and monitoring.

**Key Concepts:**
- Custom configuration with field mappings
- Per-field conflict resolution strategies
- Error handling and retry logic
- Monitoring and logging
- Integration with external services

**When to use:** Production deployments, fine-grained control over sync behavior

**Run it:**
```bash
cd examples
# The example demonstrates advanced patterns
```

**What you'll learn:**
```csharp
// Advanced configuration
var config = new SyncConfig("TeamProjectSync", "your_db_id_here", "./team-tasks")
{
    Direction = SyncDirection.NotionToLocal,
    ConflictStrategy = ConflictResolutionStrategy.LocalWins,
    SyncIntervalSeconds = 300,
    IsEnabled = true
};

// Field mappings
config.FieldMappings = new Dictionary<string, string>
{
    {"title", "Title"},
    {"status", "Status"},
    {"priority", "Priority"}
};

// Per-field strategies
config.FieldConflictStrategies = new Dictionary<string, ConflictResolutionStrategy>
{
    {"description", ConflictResolutionStrategy.LocalWins},
    {"status", ConflictResolutionStrategy.NotionWins}
};

// Error handling
try
{
    var result = await syncService.ExecuteSyncAsync(config);
}
catch (NotionApiException ex) when (ex.StatusCode == 429)
{
    // Handle rate limiting
}
```

---

### 3. IntegrationExample.cs - ASP.NET Core DI Integration
**Difficulty**: Intermediate | **Time**: 15 minutes

Integrate Notion Task Sync into an ASP.NET Core application with dependency injection.

**Key Concepts:**
- Setting up the DI container in ASP.NET Core style
- Registering services with IHostBuilder
- Using the sync service in controllers
- Background service integration
- Configuration management

**When to use:** Web applications, REST APIs, background workers

**Run it:**
```bash
cd examples
# Demonstrates ASP.NET Core integration patterns
```

**What you'll learn:**
```csharp
// ASP.NET Core style setup
var hostBuilder = new HostBuilder();

hostBuilder.ConfigureServices((context, services) =>
{
    services.AddLogging(builder => builder.AddConsole());
    services.AddApplicationServices(context.Configuration);
    services.AddHostedService<SyncBackgroundService>();
    services.AddSingleton<SyncService>();
});

// Controller integration
public class SyncController : ControllerBase
{
    private readonly SyncService _syncService;
    
    public SyncController(SyncService syncService) => _syncService = syncService;
    
    [HttpPost("sync")]
    public async Task<IActionResult> Sync([FromBody] SyncRequest request)
    {
        var config = new SyncConfig(request.Name, request.DatabaseId, request.LocalPath);
        var result = await _syncService.ExecuteSyncAsync(config);
        return Ok(result);
    }
}

// Background service
public class SyncBackgroundService : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var result = await _syncService.ExecuteSyncAsync(_config.Value);
            await Task.Delay(TimeSpan.FromSeconds(_config.Value.SyncIntervalSeconds), stoppingToken);
        }
    }
}
```

---

## Additional Examples

These examples cover specific scenarios and advanced patterns:

### ConflictResolutionExample.cs
**Difficulty**: Intermediate | **Time**: 10 minutes

Demonstrates all conflict resolution strategies and field-level preferences.

**Key Concepts:**
- Different conflict strategies (latest-wins, local-priority, notion-priority, manual, merge)
- Field-level overrides
- Testing different approaches

**When to use:** Setting up conflict handling for your use case

---

### EventHandlingExample.cs
**Difficulty**: Intermediate | **Time**: 10 minutes

Subscribe to sync events and implement custom handlers for notifications and monitoring.

**Key Concepts:**
- Event bus architecture
- Subscribing to events
- Custom event handling
- Integration with external services

**When to use:** Notifications, monitoring, custom workflows

**Events demonstrated:**
- `ConflictDetectedEvent` - When conflicts are found
- `SyncCompletedEvent` - When sync finishes

---

### BackupAndRecoveryExample.cs
**Difficulty**: Intermediate | **Time**: 15 minutes

Create backups before risky operations and recover if needed.

**Key Concepts:**
- Creating backups
- Listing available backups
- Verifying backup integrity
- Recovering from backups

**When to use:** Before major changes, migration procedures

---

### ExportAndFormatExample.cs
**Difficulty**: Beginner | **Time**: 10 minutes

Export tasks in multiple formats for integration with other tools.

**Key Concepts:**
- Loading tasks from local storage
- Formatting tasks
- Exporting to different formats
- Integrating with external tools

**Supported formats:** JSON, CSV, XML, Markdown

---

### ProgrammaticTaskManagementExample.cs
**Difficulty**: Advanced | **Time**: 15 minutes

Create, modify, and manage tasks entirely through code.

**Key Concepts:**
- Creating new tasks
- Loading and modifying tasks
- Organizing tasks
- Generating statistics

**When to use:** Bulk operations, automation, scripting

---

## Running Examples

### Option 1: Compile and Run
Each example is a complete C# file with a `Main()` method:

```bash
cd examples
# Compile and run (example files are standalone)
dotnet run -- BasicUsage.cs
```

### Option 2: Copy to Your Project
Create a test console project and copy example code:

```bash
dotnet new console -n MyExample
cd MyExample

# Copy example code into Program.cs
# Add reference to Notion Task Sync

dotnet add reference ../NotionTaskSync.csproj
dotnet run
```

### Option 3: Use as Templates
Copy example code into your application and adapt for your needs.

---

## Configuration for Examples

All examples use the standard `appsettings.json`:

```json
{
  "NotionApi": {
    "ApiKey": "your_token_here",
    "DatabaseId": "your_database_id_here"
  },
  "AppSettings": {
    "LocalTasksDirectory": "./tasks"
  }
}
```

### Setting Environment Variables

```bash
export NOTION_API_KEY="your_token"
export NOTION_DATABASE_ID="your_database_id"

# Then run example
dotnet run
```

---

## Common Patterns

### Pattern 1: Simple Sync Loop

```csharp
while (true)
{
    var result = await syncService.ExecuteSyncAsync(config);
    Console.WriteLine($"Synced {result.LocalTaskCount} tasks");
    await Task.Delay(TimeSpan.FromMinutes(5));
}
```

### Pattern 2: Error Handling with Retry

```csharp
var retryHelper = new RetryHelper(maxRetries: 3, delayMs: 1000);
var result = await retryHelper.ExecuteWithRetryAsync(
    () => syncService.ExecuteSyncAsync(config),
    onRetry: (attempt, ex) => logger.LogWarning($"Retry {attempt}: {ex.Message}")
);
```

### Pattern 3: Conditional Sync

```csharp
var changes = await changeDetectionService.DetectChangesAsync(local, notion);
if (changes.Any(c => c.Type == ChangeType.Modified))
{
    await syncService.ExecuteSyncAsync(config);
}
```

### Pattern 4: Task Transformation

```csharp
var tasks = await localFileService.LoadTasksAsync("./tasks");
var updated = tasks
    .Where(t => t.Status == "Done")
    .Select(t => { t.ModifiedAt = DateTime.UtcNow; return t; })
    .ToList();

foreach (var task in updated)
    await localFileService.SaveTaskAsync(task);
```

---

## Debugging Examples

### Enable Verbose Logging

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "Microsoft": "Warning"
    }
  }
}
```

### Dry-Run Mode

Test before making changes:
```csharp
var result = await syncService.ExecuteSyncAsync(config);
if (result.ConflictsDetected > 0) { /* handle */ }
```

### Inspect Local Files
```bash
ls -la ./tasks
cat ./tasks/task-*.json | jq .
```

---

## Best Practices

1. **Always backup before risky operations**
```csharp
await backupService.CreateBackupAsync(config.Id, "pre-operation");
```

2. **Use DRY-RUN for testing**
```csharp
var result = await syncService.ExecuteSyncAsync(config);
if (result.ConflictsDetected > 0) { /* handle */ }
```

3. **Subscribe to events for monitoring**
```csharp
eventBus.Subscribe<SyncCompletedEvent>(LogResults);
```

4. **Handle exceptions gracefully**
```csharp
try { /* operation */ }
catch (ConflictException) { /* special handling */ }
catch (SyncException) { /* retry */ }
```

5. **Log important operations**
```csharp
logger.LogInformation("Created {Count} tasks", tasks.Count);
```

---

## More Resources

- 📚 [Documentation](../docs/README.md)
- 🚀 [API Reference](../docs/API_REFERENCE.md)
- 🏗️ [Architecture](../docs/ARCHITECTURE.md)
- 🐛 [FAQ](../docs/FAQ.md)
- 📖 [Main README](../README.md)

---

**Built by [Vladyslav Zaiets](https://sarmkadan.com) - CTO & Software Architect**