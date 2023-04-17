// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

# Code Examples

This directory contains practical examples demonstrating Notion Task Sync usage and integration patterns.

## Available Examples

### 1. BasicSyncExample.cs
**Difficulty**: Beginner  
**Time**: 5 minutes

The simplest way to use Notion Task Sync - load configuration and execute a sync.

**Key Concepts:**
- Building configuration
- Setting up dependency injection
- Executing sync operation
- Handling results

**When to use:** First-time users, simple integration

```bash
cd examples
dotnet run --project BasicSyncExample.cs
```

### 2. ConflictResolutionExample.cs
**Difficulty**: Intermediate  
**Time**: 10 minutes

Demonstrates all conflict resolution strategies and field-level preferences.

**Key Concepts:**
- Different conflict strategies (latest-wins, local-priority, notion-priority)
- Field-level overrides
- Testing different approaches
- Understanding conflict handling

**When to use:** Setting up conflict handling for your use case

```bash
# Run with verbose output to see conflict details
dotnet run -- sync --verbose
```

**Strategies explained:**
- `latest-wins` - Auto-resolve based on modification time
- `local-priority` - Always prefer local file changes
- `notion-priority` - Always prefer Notion database changes
- `manual` - Prompt user for each conflict
- `merge` - Attempt intelligent merge

### 3. EventHandlingExample.cs
**Difficulty**: Intermediate  
**Time**: 10 minutes

Subscribe to sync events and implement custom handlers.

**Key Concepts:**
- Event bus architecture
- Subscribing to events
- Custom event handling
- Integration with external services

**When to use:** Notifications, monitoring, custom workflows

**Events demonstrated:**
- `ConflictDetectedEvent` - When conflicts are found
- `SyncCompletedEvent` - When sync finishes

**Real-world use cases:**
- Send Slack notifications on conflicts
- Log metrics to monitoring service
- Trigger downstream workflows
- Update dashboards

### 4. BackupAndRecoveryExample.cs
**Difficulty**: Intermediate  
**Time**: 15 minutes

Create backups before risky operations and recover if needed.

**Key Concepts:**
- Creating backups
- Listing available backups
- Verifying backup integrity
- Recovering from backups

**When to use:** Before major changes, migration procedures

**Backup workflow:**
```
Create backup → Perform operation → Verify success → Keep backup → Recover if needed
```

**Data protected:**
- Local task files
- Sync state database
- Configuration
- Change history

### 5. ExportAndFormatExample.cs
**Difficulty**: Beginner  
**Time**: 10 minutes

Export tasks in multiple formats for integration with other tools.

**Key Concepts:**
- Loading tasks from local storage
- Formatting tasks
- Exporting to different formats
- Integrating with external tools

**When to use:** Integration with Jira, Asana, GitHub Projects, etc.

**Supported formats:**
- JSON - Programmatic processing
- CSV - Spreadsheet imports
- XML - Legacy system integration
- Markdown - Documentation and sharing

**Example:** Export to CSV for Excel analysis
```csharp
var formatter = new CsvFormatter();
var csv = formatter.Format(tasks);
await File.WriteAllTextAsync("tasks.csv", csv);
```

### 6. ProgrammaticTaskManagementExample.cs
**Difficulty**: Advanced  
**Time**: 15 minutes

Create, modify, and manage tasks entirely through code.

**Key Concepts:**
- Creating new tasks
- Loading and modifying tasks
- Organizing tasks
- Generating statistics

**When to use:** Bulk operations, automation, scripting

**Example operations:**
- Create 100 tasks from CSV
- Update priority based on due date
- Archive completed tasks
- Generate team reports

**Advanced patterns:**
- Batch create tasks
- Filter by criteria
- Group and aggregate
- Export to different formats

## Running Examples

### Option 1: Run Single Example
```bash
cd examples
dotnet run BasicSyncExample.cs
```

### Option 2: Create Example Project
Create a test console project:

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
// Check result.Status before applying changes
```

### Inspect Local Files
```bash
ls -la ./tasks
cat ./tasks/task-*.json | jq .
```

### Check Sync History
```bash
dotnet run -- history --format json | jq .
```

## Examples by Use Case

### "I want to automate task creation"
👉 **ProgrammaticTaskManagementExample.cs**
- Create tasks from code
- Bulk operations
- Scheduled creation

### "I want to export to other tools"
👉 **ExportAndFormatExample.cs**
- Multiple format support
- Integration templates
- Data transformation

### "I need to handle conflicts"
👉 **ConflictResolutionExample.cs**
- Conflict strategies
- Custom resolution logic
- Event-based handling

### "I want to add monitoring/alerts"
👉 **EventHandlingExample.cs**
- Subscribe to events
- Custom handlers
- Integration with monitoring

### "I need safe operations with rollback"
👉 **BackupAndRecoveryExample.cs**
- Pre-operation backups
- Integrity verification
- Recovery procedures

### "I'm just getting started"
👉 **BasicSyncExample.cs**
- Simplest approach
- Minimal configuration
- Common use case

## Best Practices from Examples

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

## Extending Examples

Each example is designed to be extended:

1. **Copy the example** to your project
2. **Modify for your needs** - Add your business logic
3. **Test thoroughly** - Use dry-run and backups
4. **Deploy to production** - Follow deployment guide

## Troubleshooting Examples

| Issue | Solution |
|-------|----------|
| "API Key invalid" | Update appsettings.json or env vars |
| "Database not found" | Verify Database ID and integration sharing |
| "Out of memory" | Reduce batch size, enable caching |
| "Rate limited" | Increase SyncInterval, reduce RateLimitPerSecond |
| "File permission denied" | Check ./tasks directory permissions |

## More Resources

- 📚 [Documentation](../docs/README.md)
- 🚀 [API Reference](../docs/API_REFERENCE.md)
- 🏗️ [Architecture](../docs/ARCHITECTURE.md)
- 🐛 [FAQ](../docs/FAQ.md)
- 📖 [Main README](../README.md)

---

**Built by [Vladyslav Zaiets](https://sarmkadan.com) - CTO & Software Architect**
