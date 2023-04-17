// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

# API Reference

Comprehensive reference for all Notion Task Sync APIs, including CLI commands, REST endpoints, and programmatic interfaces.

## Table of Contents
- [CLI Commands](#cli-commands)
- [Programmatic API](#programmatic-api)
- [REST Endpoints](#rest-endpoints)
- [Models](#models)
- [Events](#events)

## CLI Commands

### Global Options

All commands support these options:

```
--config-file <path>     Path to configuration file (default: appsettings.json)
--verbose               Enable verbose output
--log-level <level>     Set logging level (Debug, Information, Warning, Error)
--dry-run              Simulate operation without making changes
```

### configure

Create or update a sync configuration profile.

```bash
dotnet run -- configure [OPTIONS]
```

**Options:**

| Option | Type | Required | Description |
|--------|------|----------|-------------|
| `--name` | string | Yes | Unique name for this sync profile |
| `--database-id` | string | Yes | Notion database ID |
| `--local-path` | string | Yes | Local directory for tasks |
| `--conflict-strategy` | string | No | Resolution strategy (default: latest-wins) |
| `--auto-backup` | bool | No | Enable automatic backups (default: true) |
| `--sync-interval` | int | No | Sync interval in seconds (default: 300) |
| `--rate-limit` | int | No | API requests per second (default: 3) |

**Example:**

```bash
dotnet run -- configure \
  --name "MyProjectSync" \
  --database-id "abc123..." \
  --local-path "./tasks" \
  --conflict-strategy "manual" \
  --auto-backup true
```

### sync

Execute synchronization between Notion and local files.

```bash
dotnet run -- sync [OPTIONS]
```

**Options:**

| Option | Type | Description |
|--------|------|-------------|
| `--config` | string | Sync profile name (default: first profile) |
| `--direction` | string | Sync direction: both, local-to-notion, notion-to-local (default: both) |
| `--force` | bool | Skip conflicts and use configured strategy |
| `--no-backup` | bool | Skip automatic backup |
| `--batch-size` | int | Tasks per batch (default: 100) |

**Example:**

```bash
# Full bidirectional sync
dotnet run -- sync --verbose

# Notion to local only
dotnet run -- sync --direction notion-to-local

# Force resolution without prompts
dotnet run -- sync --force

# Large database with batching
dotnet run -- sync --batch-size 50
```

**Output:**

```
[INFO] Notion Task Sync Starting
[INFO] Loading configuration: Default Sync
[INFO] Notion Database ID: abc123...
[INFO] Local path: ./tasks

[INFO] Phase 1: Load Current State
[INFO] - Loaded 42 local tasks
[INFO] - Loaded 45 Notion pages
[INFO] - Current sync state: 40 synced items

[INFO] Phase 2: Change Detection
[INFO] - Local changes: 3 modified, 2 new
[INFO] - Notion changes: 5 modified, 0 new
[INFO] - No previous conflicts

[INFO] Phase 3: Conflict Resolution
[INFO] - Conflicts detected: 0
[INFO] - Ready to sync

[INFO] Phase 4: Apply Changes
[INFO] - Syncing 3 modified local → Notion
[INFO] - Syncing 5 modified Notion → local
[INFO] - Creating 2 new in Notion
[INFO] - Creating 0 new locally

[INFO] Sync Complete
[INFO] Duration: 3.2s
[INFO] Status: SUCCESS
```

### status

Display current sync status and statistics.

```bash
dotnet run -- status [OPTIONS]
```

**Options:**

| Option | Type | Description |
|--------|------|-------------|
| `--config` | string | Sync profile name |
| `--verbose` | bool | Show detailed information |

**Example:**

```bash
dotnet run -- status --verbose
```

**Output:**

```
SYNC STATUS
═══════════════════════════════════════════════

Profile: MyProjectSync
Status: HEALTHY
Last Sync: 2024-01-15 14:32:15 UTC (5 minutes ago)
Next Sync: 2024-01-15 14:37:15 UTC (in 3 minutes)

LOCAL STATE
  Tasks: 42
  Modified: 3 (within 24h)
  Last Updated: 2024-01-15 14:25:00 UTC

NOTION STATE
  Pages: 45
  Modified: 5 (within 24h)
  Last Pulled: 2024-01-15 14:32:15 UTC

SYNC STATISTICS
  Total Syncs: 156
  Successful: 154
  Failed: 2
  Conflicts Resolved: 8
  
RECENT CHANGES
  ├─ 2024-01-15 14:32 - Task #3 synced (local→notion)
  ├─ 2024-01-15 14:32 - Task #7 synced (notion→local)
  ├─ 2024-01-15 14:31 - Task #12 created (local)
  └─ 2024-01-15 14:30 - Task #9 modified (notion)
```

### history

View sync operation history.

```bash
dotnet run -- history [OPTIONS]
```

**Options:**

| Option | Type | Description |
|--------|------|-------------|
| `--limit` | int | Number of records to show (default: 50) |
| `--format` | string | Output format: text, json, csv (default: text) |
| `--since` | datetime | Show changes since date (ISO 8601) |
| `--until` | datetime | Show changes until date (ISO 8601) |

**Example:**

```bash
# Last 20 syncs
dotnet run -- history --limit 20

# Export to JSON
dotnet run -- history --limit 100 --format json > history.json

# Changes in last 7 days
dotnet run -- history --since "2024-01-08"
```

### help

Display help information.

```bash
dotnet run -- help [COMMAND]
```

**Examples:**

```bash
dotnet run -- help
dotnet run -- help sync
dotnet run -- help configure
```

## Programmatic API

### SyncService

Main service for executing synchronization.

```csharp
public interface ISyncService
{
    Task<SyncResult> ExecuteSyncAsync(SyncConfig config);
}

public class SyncResult
{
    public string Status { get; set; }
    public int LocalTaskCount { get; set; }
    public int NotionPageCount { get; set; }
    public int LocalChangesDetected { get; set; }
    public int NotionChangesDetected { get; set; }
    public int ConflictsDetected { get; set; }
    public int ConflictsResolved { get; set; }
    public TimeSpan Duration { get; set; }
    public string ErrorMessage { get; set; }
}
```

**Usage:**

```csharp
var syncService = serviceProvider.GetRequiredService<SyncService>();

var result = await syncService.ExecuteSyncAsync(
    new SyncConfig(
        name: "MySync",
        notionDatabaseId: "abc123...",
        localFolderPath: "./tasks"
    )
);

if (result.Status == "SUCCESS")
    Console.WriteLine($"Synced {result.LocalTaskCount} tasks");
```

### LocalFileService

Manage local task files.

```csharp
public interface ILocalFileService
{
    Task<List<Task>> LoadTasksAsync(string folderPath);
    Task SaveTaskAsync(Task task);
    Task DeleteTaskAsync(string taskId);
    Task<Task> LoadTaskAsync(string taskId);
}
```

**Usage:**

```csharp
var localService = serviceProvider.GetRequiredService<LocalFileService>();

// Load all tasks
var tasks = await localService.LoadTasksAsync("./tasks");

// Save a task
var task = new Task { Title = "New Task", Status = "Todo" };
await localService.SaveTaskAsync(task);

// Delete a task
await localService.DeleteTaskAsync("task-id");
```

### NotionApiService

Interact with Notion API.

```csharp
public interface INotionApiService
{
    Task<List<NotionPage>> FetchPagesAsync(string databaseId);
    Task<NotionPage> CreatePageAsync(string databaseId, NotionPage page);
    Task<NotionPage> UpdatePageAsync(string databaseId, NotionPage page);
    Task DeletePageAsync(string databaseId, string pageId);
}
```

**Usage:**

```csharp
var notionService = serviceProvider.GetRequiredService<NotionApiService>();

// Fetch pages
var pages = await notionService.FetchPagesAsync("database-id");

// Create page
var newPage = new NotionPage
{
    Title = "New Task",
    Properties = new Dictionary<string, object>
    {
        { "Status", "Todo" },
        { "Priority", "High" }
    }
};
await notionService.CreatePageAsync("database-id", newPage);

// Update page
page.Properties["Status"] = "In Progress";
await notionService.UpdatePageAsync("database-id", page);

// Delete page
await notionService.DeletePageAsync("database-id", "page-id");
```

### ChangeDetectionService

Detect changes between states.

```csharp
public interface IChangeDetectionService
{
    Task<List<Change>> DetectChangesAsync(
        List<Task> localTasks, 
        List<NotionPage> notionPages);
    
    bool HasChanged(Task local, NotionPage notion);
}
```

**Usage:**

```csharp
var changeService = serviceProvider
    .GetRequiredService<ChangeDetectionService>();

var changes = await changeService.DetectChangesAsync(
    localTasks, 
    notionPages
);

foreach (var change in changes)
{
    Console.WriteLine($"{change.Type}: {change.TaskId}");
    Console.WriteLine($"  Old: {change.OldValues}");
    Console.WriteLine($"  New: {change.NewValues}");
}
```

### ConflictResolutionService

Resolve conflicting changes.

```csharp
public interface IConflictResolutionService
{
    Task<List<Resolution>> ResolveAsync(
        List<Conflict> conflicts,
        string strategy);
}

public class Resolution
{
    public Conflict Conflict { get; set; }
    public Change PreferredChange { get; set; }
    public string Strategy { get; set; }
}
```

**Usage:**

```csharp
var conflictService = serviceProvider
    .GetRequiredService<ConflictResolutionService>();

var resolutions = await conflictService.ResolveAsync(
    conflicts,
    strategy: "latest-wins"
);

foreach (var resolution in resolutions)
{
    Console.WriteLine($"Conflict on {resolution.Conflict.TaskId}");
    Console.WriteLine($"  Resolved using: {resolution.Strategy}");
    Console.WriteLine($"  Preferred: {resolution.PreferredChange.Source}");
}
```

### BackupService

Create and manage backups.

```csharp
public interface IBackupService
{
    Task<string> CreateBackupAsync(
        string syncConfigId, 
        string backupName);
    
    Task RestoreBackupAsync(string backupPath);
    
    Task<List<BackupInfo>> ListBackupsAsync(string syncConfigId);
}
```

**Usage:**

```csharp
var backupService = serviceProvider
    .GetRequiredService<BackupService>();

// Create backup
var backupPath = await backupService.CreateBackupAsync(
    syncConfigId: "sync-1",
    backupName: "pre-risky-operation"
);

Console.WriteLine($"Backup created at: {backupPath}");

// List backups
var backups = await backupService.ListBackupsAsync("sync-1");
foreach (var backup in backups)
    Console.WriteLine($"{backup.Name} - {backup.CreatedAt}");

// Restore
await backupService.RestoreBackupAsync(backupPath);
```

### EventBus

Publish and subscribe to events.

```csharp
public interface IEventBus
{
    void Subscribe<T>(Func<T, Task> handler) where T : IEvent;
    Task PublishAsync<T>(T @event) where T : IEvent;
}
```

**Usage:**

```csharp
var eventBus = serviceProvider.GetRequiredService<IEventBus>();

// Subscribe to events
eventBus.Subscribe<ConflictDetectedEvent>(async e =>
{
    Console.WriteLine($"Conflict on task {e.TaskId}");
    Console.WriteLine($"  Local: {e.LocalChange.Timestamp}");
    Console.WriteLine($"  Notion: {e.NotionChange.Timestamp}");
});

eventBus.Subscribe<SyncCompletedEvent>(async e =>
{
    Console.WriteLine($"Sync completed: {e.Status}");
    Console.WriteLine($"  Duration: {e.Duration.TotalSeconds}s");
});
```

## REST Endpoints

*(When running as a web service)*

### POST /api/sync

Execute synchronization.

**Request:**

```json
{
  "configName": "MySync",
  "direction": "both",
  "force": false
}
```

**Response:**

```json
{
  "status": "SUCCESS",
  "localTaskCount": 42,
  "notionPageCount": 45,
  "conflictsDetected": 0,
  "conflictsResolved": 0,
  "duration": "PT3.2S"
}
```

### GET /api/status

Get current sync status.

**Response:**

```json
{
  "syncProfile": "MySync",
  "status": "HEALTHY",
  "lastSync": "2024-01-15T14:32:15Z",
  "nextSync": "2024-01-15T14:37:15Z",
  "localTaskCount": 42,
  "notionPageCount": 45,
  "pendingChanges": 3
}
```

### POST /api/backup

Create a backup.

**Request:**

```json
{
  "syncConfigId": "sync-1",
  "backupName": "before-migration"
}
```

**Response:**

```json
{
  "backupPath": "/backups/before-migration_20240115_143215",
  "createdAt": "2024-01-15T14:32:15Z"
}
```

### GET /api/history

Get sync history.

**Query Parameters:**

| Parameter | Type | Description |
|-----------|------|-------------|
| `limit` | int | Number of records (default: 50) |
| `since` | datetime | Show changes since date |
| `format` | string | Output format (json, csv) |

**Response:**

```json
[
  {
    "timestamp": "2024-01-15T14:32:15Z",
    "type": "SYNC_COMPLETED",
    "status": "SUCCESS",
    "tasksSynced": 8,
    "conflictsDetected": 0
  },
  {
    "timestamp": "2024-01-15T14:27:10Z",
    "type": "SYNC_COMPLETED",
    "status": "SUCCESS",
    "tasksSynced": 3,
    "conflictsDetected": 1
  }
]
```

## Models

### Task

```csharp
public class Task
{
    public string Id { get; set; }
    public string NotionPageId { get; set; }
    public string Title { get; set; }
    public TaskStatus Status { get; set; }        // Todo, InProgress, Done, Cancelled
    public TaskPriority Priority { get; set; }    // Low, Medium, High
    public DateTime? DueDate { get; set; }
    public List<string> Tags { get; set; }
    public string AssigneeId { get; set; }
    public string Description { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime ModifiedAt { get; set; }
    public DateTime? SyncedAt { get; set; }
}
```

### SyncConfig

```csharp
public class SyncConfig
{
    public string Name { get; set; }
    public string NotionDatabaseId { get; set; }
    public string LocalFolderPath { get; set; }
    public string ConflictResolutionStrategy { get; set; }
    public int SyncIntervalSeconds { get; set; }
    public bool AutoBackup { get; set; }
    public int RateLimitPerSecond { get; set; }
    public Dictionary<string, string> FieldMapping { get; set; }
}
```

### Change

```csharp
public class Change
{
    public string Id { get; set; }
    public string TaskId { get; set; }
    public ChangeType Type { get; set; }             // Created, Modified, Deleted
    public string Source { get; set; }               // "local" or "notion"
    public DateTime DetectedAt { get; set; }
    public Dictionary<string, object> NewValues { get; set; }
    public Dictionary<string, object> OldValues { get; set; }
}
```

### Conflict

```csharp
public class Conflict
{
    public string TaskId { get; set; }
    public Change LocalChange { get; set; }
    public Change NotionChange { get; set; }
    public bool IsConflict { get; set; }
}
```

## Events

### ConflictDetectedEvent

Fired when a conflict is detected during sync.

```csharp
public class ConflictDetectedEvent : IEvent
{
    public string TaskId { get; set; }
    public Change LocalChange { get; set; }
    public Change NotionChange { get; set; }
    public DateTime DetectedAt { get; set; }
}
```

### SyncCompletedEvent

Fired when sync operation completes.

```csharp
public class SyncCompletedEvent : IEvent
{
    public string Status { get; set; }              // SUCCESS, FAILURE, PARTIAL
    public int TasksSynced { get; set; }
    public int ConflictsDetected { get; set; }
    public int ConflictsResolved { get; set; }
    public TimeSpan Duration { get; set; }
    public string ErrorMessage { get; set; }
}
```

### ChangeDetectedEvent

Fired when changes are detected.

```csharp
public class ChangeDetectedEvent : IEvent
{
    public string TaskId { get; set; }
    public ChangeType Type { get; set; }
    public string Source { get; set; }
    public Dictionary<string, object> Changes { get; set; }
}
```

---

**Built by [Vladyslav Zaiets](https://sarmkadan.com) - CTO & Software Architect**
