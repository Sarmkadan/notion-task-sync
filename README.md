![Build](https://github.com/sarmkadan/notion-task-sync/actions/workflows/build.yml/badge.svg)
![License](https://img.shields.io/github/license/sarmkadan/notion-task-sync)
![.NET](https://img.shields.io/badge/.NET-10.0-512BD4)

# Notion Task Sync

A powerful .NET application for bidirectional synchronization between Notion databases and local task files with intelligent conflict resolution and change detection.

## Table of Contents

- [Overview](#overview)
- [Features](#features)
- [Architecture](#architecture)
- [Installation](#installation)
- [Quick Start](#quick-start)
- [Usage Examples](#usage-examples)
- [CLI Reference](#cli-reference)
- [Configuration](#configuration)
- [Troubleshooting](#troubleshooting)
- [Testing](#testing)
- [Performance](#performance)
- [Related Projects](#related-projects)
- [Contributing](#contributing)
- [License](#license)

## Overview

**Notion Task Sync** bridges the gap between Notion databases and local task management workflows. It enables developers, project managers, and teams to:

- Keep Notion databases synchronized with local task files
- Detect changes automatically in both directions
- Resolve conflicts intelligently with configurable strategies
- Track sync history and maintain audit logs
- Run scheduled syncs via CLI or integrated workers
- Export tasks in multiple formats (JSON, CSV, XML, Markdown)

### Why Notion Task Sync?

Traditional task management either locks you into a single platform or requires manual synchronization. Notion Task Sync enables:

- **Local-first development** - Keep your tasks in version control alongside your code
- **Notion integration** - Leverage Notion's powerful database features and UI
- **Conflict-free workflow** - Intelligent resolution prevents data loss during concurrent edits
- **Format flexibility** - Export to your favorite tools (Jira, GitHub Projects, etc.)
- **Team collaboration** - Shared sync configuration with customizable conflict strategies

## Features

### Core Functionality
- ✅ **Bidirectional Sync** - Sync from Notion to local files and vice versa
- ✅ **Change Detection** - Intelligent diff detection with timestamp-based tracking
- ✅ **Conflict Resolution** - Multiple strategies (latest-wins, manual, merge, local-priority, notion-priority)
- ✅ **Backup & Recovery** - Automatic backups before sync operations
- ✅ **Event System** - Hooks for custom workflows and monitoring
- ✅ **Caching** - Reduce API calls with intelligent caching
- ✅ **Rate Limiting** - Built-in rate limiter for API calls
- ✅ **Two-way Calendar Sync** - Export task due dates to iCal (.ics) and import calendar events back as tasks
- ✅ **Bulk Operations CLI** - Batch update status, tags, assignees, and priority across many tasks at once
- ✅ **Conflict Resolution UI** - Interactive terminal UI for reviewing diffs and resolving conflicts manually

### Format Support
- JSON, CSV, XML, Markdown exports
- Configurable task property mapping
- Custom field serialization

### Configuration & Deployment
- JSON-based configuration files
- Environment variable overrides
- Docker support
- Systemd integration examples
- GitHub Actions CI/CD ready

### Monitoring & Logging
- Comprehensive structured logging
- Event-driven notifications
- Sync statistics and reporting
- Health check endpoints

## Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                     Notion Task Sync                         │
├─────────────────────────────────────────────────────────────┤
│                                                               │
│  ┌──────────────┐              ┌──────────────┐             │
│  │   CLI/REST   │              │  Event Bus   │             │
│  │   Interface  │              │  (Handlers)  │             │
│  └──────┬───────┘              └──────┬───────┘             │
│         │                              │                     │
│  ┌──────▼──────────────────────────────▼─────┐              │
│  │         SyncService (Orchestrator)        │              │
│  └──────┬──────────────────────────────┬─────┘              │
│         │                              │                     │
│  ┌──────▼────────────┐    ┌───────────▼────────┐            │
│  │ Change Detection  │    │ Conflict Resolution │            │
│  │    Service        │    │    Service          │            │
│  └──────┬────────────┘    └───────────┬────────┘            │
│         │                              │                     │
│  ┌──────▼────────────┐    ┌───────────▼────────┐            │
│  │ Local File Service│    │  Notion API Service│            │
│  └──────┬────────────┘    └───────────┬────────┘            │
│         │                              │                     │
│  ┌──────▼────────────┐    ┌───────────▼────────┐            │
│  │ FileSystem I/O    │    │  HTTP Client       │            │
│  └───────────────────┘    │  + Cache           │            │
│                           └────────────────────┘            │
│                                                               │
│  ┌──────────────────────────────────────┐                  │
│  │     Data Layer (SQLite Repository)   │                  │
│  │  - Tasks, ChangeLogs, Configurations │                  │
│  └──────────────────────────────────────┘                  │
│                                                               │
└─────────────────────────────────────────────────────────────┘
```

### Component Overview

- **CLI/REST Interface** - Command-line and HTTP endpoints for triggering syncs
- **SyncService** - Central orchestrator managing the sync workflow
- **Change Detection Service** - Compares states and identifies modifications
- **Conflict Resolution Service** - Intelligently resolves conflicting changes
- **Local File Service** - Handles reading/writing task files
- **Notion API Service** - Interfaces with Notion API (with caching)
- **Repository Layer** - Persists sync state and configuration
- **Event Bus** - Decoupled event handling for extensibility

## Installation

### Prerequisites

- .NET 10 SDK or later (for source builds)
- Docker (for containerized deployment)
- Notion API token (obtain from [Notion Integration Settings](https://www.notion.so/my-integrations))
- Git (for version control)

### Method 1: From Source

```bash
# Clone the repository
git clone https://github.com/Sarmkadan/notion-task-sync.git
cd notion-task-sync

# Restore dependencies
dotnet restore

# Build the project
dotnet build -c Release

# Run the application
dotnet run -- sync
```

## Docker Usage

Notion Task Sync is container-ready. 

### Prerequisites

- Docker installed on your host machine.

### Quick Start with Docker Compose

1. **Configure Environment Variables:**
   Copy the example environment file and fill in your credentials:
   ```bash
   cp .env.example .env
   # Edit .env and add your NOTION_API_KEY and NOTION_DATABASE_ID
   ```

2. **Start the container:**
   ```bash
   docker-compose up -d
   ```

3. **Check logs:**
   ```bash
   docker-compose logs -f notion-sync
   ```

### Building and Running Manually

```bash
# Build the image
docker build -t notion-task-sync .

# Run the container
docker run -d \
  -e NotionApi__ApiKey=your_token \
  -e NotionApi__DatabaseId=your_db_id \
  -v $(pwd)/data:/data \
  notion-task-sync
```

### Method 4: As a Global Tool

```bash
# Install as a global tool
dotnet tool install --global --add-source ./nupkg NotionTaskSync

# Use from anywhere
notion-sync configure
notion-sync sync
```

### Method 5: Manual Binary

```bash
# Download precompiled binary from releases
wget https://github.com/Sarmkadan/notion-task-sync/releases/download/v2.0.2/notion-task-sync-linux-x64.tar.gz

# Extract
tar -xzf notion-task-sync-linux-x64.tar.gz

# Run
./notion-task-sync sync
```

## Quick Start

### 1. Create Notion Integration

1. Go to [Notion Integrations](https://www.notion.so/my-integrations)
2. Click "Create new integration"
3. Name it "Notion Task Sync"
4. Copy the "Internal Integration Token"

### 2. Create a Notion Database

1. Create a new database in Notion with these columns:
   - **Title** (text) - Task name
   - **Status** (select: Todo, In Progress, Done)
   - **Priority** (select: Low, Medium, High)
   - **DueDate** (date)
   - **Tags** (multi-select)
   - **Assignee** (person)

2. Share the database with your integration (click Share → Select your integration)

3. Copy the Database ID from the URL: `https://notion.so/workspace/{database_id}?v=...`

### 3. Configure the Application

Create `appsettings.json`:

```json
{
  "NotionApi": {
    "ApiKey": "your_integration_token_here",
    "DatabaseId": "your_database_id_here",
    "RateLimitPerSecond": 3
  },
  "AppSettings": {
    "LocalTasksDirectory": "./tasks",
    "BackupDirectory": "./backups",
    "LogFilePath": "./logs/sync.log"
  },
  "SyncConfig": {
    "ConflictResolutionStrategy": "latest-wins",
    "AutoBackup": true,
    "EnableChangeDetection": true,
    "SyncInterval": 300
  }
}
```

### 4. Run First Sync

```bash
# Run interactive configuration
dotnet run -- configure

# Execute sync
dotnet run -- sync

# Check status
dotnet run -- status
```

## Usage Examples

See the **[examples/](examples/README.md)** directory for complete, runnable examples:

- **[BasicUsage.cs](examples/BasicUsage.cs)** - Minimal setup and first sync call
- **[AdvancedUsage.cs](examples/AdvancedUsage.cs)** - Configuration, custom options, and error handling
- **[IntegrationExample.cs](examples/IntegrationExample.cs)** - ASP.NET Core DI integration



### Example 1: Basic Sync with Default Configuration

```csharp
var config = new SyncConfig(
    name: "MyProjectSync",
    notionDatabaseId: "abc123def456...",
    localFolderPath: "./project-tasks"
);

var syncService = serviceProvider.GetRequiredService<SyncService>();
var result = await syncService.ExecuteSyncAsync(config);

Console.WriteLine($"Synced {result.LocalTaskCount} local tasks");
Console.WriteLine($"Synced {result.NotionPageCount} Notion pages");
```

### Example 2: Custom Conflict Resolution

```csharp
var config = new SyncConfig(
    name: "TeamSync",
    notionDatabaseId: "team-db-id",
    localFolderPath: "./team-tasks"
)
{
    ConflictResolutionStrategy = "manual",
    AutoResolveNonConflicting = true,
    PreferLocalChangesWhen = new[] { "description", "notes" },
    PreferNotionChangesWhen = new[] { "status", "priority" }
};

await syncService.ExecuteSyncAsync(config);
```

### Example 3: Export to Multiple Formats

```csharp
var changeLogRepository = serviceProvider.GetRequiredService<IChangeLogRepository>();
var changes = await changeLogRepository.GetLatestChangesAsync(
    syncConfigId: "sync-1",
    limit: 100
);

// Export to JSON
var jsonFormatter = new JsonFormatter();
var jsonOutput = jsonFormatter.Format(changes);
File.WriteAllText("changes.json", jsonOutput);

// Export to CSV
var csvFormatter = new CsvFormatter();
var csvOutput = csvFormatter.Format(changes);
File.WriteAllText("changes.csv", csvOutput);

// Export to Markdown
var mdFormatter = new MarkdownFormatter();
var mdOutput = mdFormatter.Format(changes);
File.WriteAllText("changes.md", mdOutput);
```

### Example 4: Scheduled Sync with Health Checks

```bash
# Run sync every 5 minutes
while true; do
    dotnet run -- sync
    sleep 300
done

# Or use the built-in worker
dotnet run -- worker --type sync --interval 300
```

### Example 5: Monitor Sync Events

```csharp
var eventBus = serviceProvider.GetRequiredService<IEventBus>();

eventBus.Subscribe<ConflictDetectedEvent>(async e =>
{
    Console.WriteLine($"Conflict detected: {e.TaskId}");
    Console.WriteLine($"Local version: {e.LocalChange.Timestamp}");
    Console.WriteLine($"Notion version: {e.NotionChange.Timestamp}");
});

eventBus.Subscribe<SyncCompletedEvent>(async e =>
{
    Console.WriteLine($"Sync completed: {e.Status}");
    Console.WriteLine($"Duration: {e.Duration}ms");
});
```

### Example 6: Programmatic Task Management

```csharp
var localFileService = serviceProvider.GetRequiredService<LocalFileService>();

// Load tasks from local folder
var tasks = await localFileService.LoadTasksAsync("./tasks");

// Modify a task
tasks[0].Status = "In Progress";
tasks[0].Priority = "High";
tasks[0].ModifiedAt = DateTime.UtcNow;

// Save changes
await localFileService.SaveTaskAsync(tasks[0]);

// Export a task
var taskJson = JsonConvert.SerializeObject(tasks[0], Formatting.Indented);
```

### Example 7: Backup Before Risky Operations

```csharp
var backupService = serviceProvider.GetRequiredService<BackupService>();

// Create backup
var backupPath = await backupService.CreateBackupAsync(
    syncConfigId: "sync-1",
    backupName: "pre-migration",
    includeDatabase: true,
    includeLocalFiles: true
);

Console.WriteLine($"Backup created at: {backupPath}");

// Restore from backup if needed
await backupService.RestoreBackupAsync(backupPath);
```

### Example 8: Webhook Integration

```csharp
// In your ASP.NET Core app or webhook listener
var webhookHandler = serviceProvider.GetRequiredService<WebhookHandler>();

app.MapPost("/webhook/notion-update", async (HttpContext context) =>
{
    var body = await context.Request.Body.ReadAsStringAsync();
    var notionEvent = JsonConvert.DeserializeObject<NotionEvent>(body);
    
    // Trigger sync on webhook
    await webhookHandler.HandleNotionWebhookAsync(notionEvent);
    
    return Results.Ok();
});
```

### Example 9: Retry Failed Syncs

```csharp
var retryHelper = new RetryHelper(maxRetries: 3, delayMs: 1000);

var result = await retryHelper.ExecuteWithRetryAsync(async () =>
{
    return await syncService.ExecuteSyncAsync(config);
}, onRetry: (attempt, ex) =>
{
    Console.WriteLine($"Retry attempt {attempt}: {ex.Message}");
});
```

### Example 10: CLI Commands

```bash
# Show help
dotnet run -- help

# Configure sync
dotnet run -- configure --name "MySync" \
                         --database-id "abc123..." \
                         --local-path "./tasks"

# Execute sync
dotnet run -- sync --config "MySync"

# Check status
dotnet run -- status --config "MySync"

# View sync history
dotnet run -- history --limit 50 --format json

# Create backup
dotnet run -- backup create --config "MySync" --name "backup-1"

# Restore from backup
dotnet run -- backup restore --path "./backups/backup-1"
```

## CLI Reference

### Global Options

```
--config-file    Path to configuration file (default: appsettings.json)
--log-level      Logging level (Debug, Information, Warning, Error)
--verbose        Enable verbose output
--dry-run        Simulate sync without making changes
```

### Commands

#### configure

Configure a new sync profile.

```bash
dotnet run -- configure \
  --name "ProjectSync" \
  --database-id "your_database_id" \
  --local-path "./tasks" \
  --conflict-strategy "latest-wins"
```

Options:
- `--name` (required) - Sync profile name
- `--database-id` (required) - Notion database ID
- `--local-path` (required) - Local directory path
- `--conflict-strategy` - Resolution strategy (default: latest-wins)
- `--auto-backup` - Enable automatic backups (default: true)

#### sync

Execute synchronization.

```bash
dotnet run -- sync \
  --config "ProjectSync" \
  --direction "both" \
  --force
```

Options:
- `--config` - Sync profile name
- `--direction` - Sync direction: "local-to-notion", "notion-to-local", "both" (default: both)
- `--force` - Skip conflict prompts and use configured strategy
- `--no-backup` - Skip backup before sync

#### status

Show current sync status.

```bash
dotnet run -- status --config "ProjectSync" --verbose
```

#### calendar

Synchronize task due dates with an iCal (.ics) calendar file.

```bash
# Bidirectional sync (export tasks → import events)
dotnet run -- calendar --action sync --file my-calendar.ics

# Export only: write tasks with due dates to an .ics file
dotnet run -- calendar --action export --file notion-tasks.ics

# Import only: create/update tasks from an existing .ics file
dotnet run -- calendar --action import --file external.ics
```

Options:
- `--action` - `export`, `import`, or `sync` (default: `sync`)
- `--file` - Path to the .ics file (default: `notion-tasks.ics`)
- `--verbose` - Show detailed output

#### bulk

Perform bulk operations on multiple tasks at once.

```bash
# Mark all in-progress tasks as done
dotnet run -- bulk update-status --status done --filter status:inprogress

# Tag a specific set of tasks
dotnet run -- bulk add-tag --tag urgent --ids id1,id2,id3

# Reassign tasks matching a tag
dotnet run -- bulk assign --assignee alice@example.com --filter tag:backend

# Preview a bulk delete without applying it
dotnet run -- bulk delete --filter tag:obsolete --dry-run
```

Operations: `update-status`, `add-tag`, `remove-tag`, `assign`, `set-priority`, `delete`

Options:
- `--ids` - Comma-separated list of task GUIDs
- `--filter` - Filter expression: `status:<value>`, `tag:<value>`, `assignee:<value>`
- `--status` - Target status for `update-status` (todo, inprogress, done, blocked, archived)
- `--tag` - Tag value for `add-tag` / `remove-tag`
- `--assignee` - Assignee email or username for `assign`
- `--priority` - Priority 0–100 for `set-priority`
- `--dry-run` - Preview affected tasks without applying changes

#### conflict

Review and resolve pending sync conflicts interactively.

```bash
# Interactive session — resolve one conflict at a time
dotnet run -- conflict

# Auto-resolve all pending conflicts with a strategy
dotnet run -- conflict --strategy local
dotnet run -- conflict --strategy notion
dotnet run -- conflict --strategy last-write

# Print pending conflicts as JSON
dotnet run -- conflict --json

# Limit interactive session to first 5 conflicts
dotnet run -- conflict --limit 5
```

Options:
- `--strategy` - Auto-resolve mode: `local`, `notion`, or `last-write`
- `--show-diff` - Show unified diff before each prompt (default: true)
- `--json` - Output conflict list as JSON instead of interactive mode
- `--limit` - Maximum conflicts to process in this session

#### history

View sync history.

```bash
dotnet run -- history \
  --limit 50 \
  --format "json" \
  --since "2024-01-01"
```

## Configuration

### appsettings.json Structure

```json
{
  "NotionApi": {
    "ApiKey": "your_token",
    "DatabaseId": "your_db_id",
    "BaseUrl": "https://api.notion.com/v1",
    "RateLimitPerSecond": 3,
    "RequestTimeoutSeconds": 30,
    "MaxRetries": 3
  },
  "AppSettings": {
    "LocalTasksDirectory": "./tasks",
    "BackupDirectory": "./backups",
    "LogFilePath": "./logs/sync.log",
    "LogLevel": "Information",
    "MaxLogFileSizeMb": 100,
    "ArchiveOldLogs": true
  },
  "SyncConfig": {
    "ConflictResolutionStrategy": "latest-wins",
    "AutoBackup": true,
    "EnableChangeDetection": true,
    "SyncInterval": 300,
    "MaxTasksPerSync": 1000,
    "TaskPropertyMapping": {
      "title": "Title",
      "status": "Status",
      "priority": "Priority",
      "dueDate": "DueDate",
      "tags": "Tags"
    }
  },
  "Caching": {
    "Enabled": true,
    "DurationSeconds": 300,
    "MaxEntries": 1000
  },
  "RateLimiting": {
    "Enabled": true,
    "RequestsPerSecond": 3,
    "BurstSize": 5
  }
}
```

### Environment Variables

All configuration can be overridden via environment variables:

```bash
export NotionApi__ApiKey="your_token"
export NotionApi__DatabaseId="your_db_id"
export AppSettings__LocalTasksDirectory="/opt/tasks"
export SyncConfig__ConflictResolutionStrategy="manual"
```

### Conflict Resolution Strategies

1. **latest-wins** - Last modification wins (default)
   - Best for: Single-user workflows
   - Risk: May lose data if not careful

2. **manual** - Prompt user for each conflict
   - Best for: High-stakes changes
   - Risk: Blocks automated syncs

3. **merge** - Attempt to intelligently merge changes
   - Best for: Non-overlapping field changes
   - Risk: May not work for complex conflicts

4. **local-priority** - Prefer local file changes
   - Best for: Local-first development
   - Risk: Overwrites Notion changes

5. **notion-priority** - Prefer Notion database changes
   - Best for: Notion-first workflows
   - Risk: Overwrites local file changes

## Troubleshooting

### Issue: "API Key Invalid" Error

**Symptom**: `Error: Invalid API token`

**Solution**:
1. Verify your Notion Integration token in [Integrations settings](https://www.notion.so/my-integrations)
2. Ensure the token hasn't expired (regenerate if needed)
3. Check `appsettings.json` has the correct `NotionApi:ApiKey`
4. Verify environment variables aren't conflicting: `unset NotionApi__ApiKey`

### Issue: "Database Not Found" Error

**Symptom**: `Error: Database not found or not accessible`

**Solution**:
1. Confirm the Database ID is correct (from the Notion URL)
2. Share the Notion database with your integration:
   - Open database
   - Click Share → Find your integration → Invite
3. Try with a public database first to test connectivity

### Issue: Rate Limiting / Throttling

**Symptom**: `Error: Rate limit exceeded`

**Solution**:
1. Increase `SyncConfig:SyncInterval` in config
2. Decrease `RateLimiting:RequestsPerSecond`
3. Implement batching for large syncs:
   ```bash
   dotnet run -- sync --batch-size 50
   ```

### Issue: Local Files Not Being Updated

**Symptom**: Notion changes don't appear in local files

**Solution**:
1. Check directory permissions: `ls -la ./tasks`
2. Verify path in config: `AppSettings:LocalTasksDirectory`
3. Check sync direction: `--direction "notion-to-local"`
4. Review logs: `tail -f ./logs/sync.log`

### Issue: Conflicts Not Resolving

**Symptom**: Sync hangs waiting for conflict resolution

**Solution**:
1. Use `--force` flag to apply configured strategy
2. Change strategy to `latest-wins` for testing
3. Review conflict logs in detail mode

### Issue: Out of Memory / Performance

**Symptom**: High memory usage with large databases

**Solution**:
1. Reduce `MaxTasksPerSync` in configuration
2. Enable caching: `Caching:Enabled = true`
3. Use batch processing with smaller intervals
4. Increase available RAM or use pagination API

## Testing

```bash
# Run all tests
dotnet test

# Run with code coverage
dotnet test --collect:"XPlat Code Coverage"

# Run a specific test class
dotnet test --filter "ClassName=ChangeDetectionServiceTests"
```

The test suite covers:

- **ChangeDetectionServiceTests** - validates conflict detection across concurrent edits
- **ConflictResolutionTests** - verifies all five resolution strategies
- **StringExtensionsTests** - utility method correctness

## Benchmarks

This project includes a performance benchmarking suite based on [BenchmarkDotNet](https://benchmarkdotnet.org/).

### Running Benchmarks

To run the benchmarks, navigate to the benchmarks project directory and run the following command:

```bash
cd benchmarks/notion-task-sync.Benchmarks
dotnet run -c Release
```

### Benchmark Results

The following table summarizes the performance of critical operations:

| Method                     | Mean       | Error    | StdDev   | Gen0   | Allocated |
|--------------------------- |-----------:|---------:|---------:|-------:|----------:|
| NormalizeRichText          |   193.3 ns |  3.94 ns | 10.11 ns | 0.0966 |     808 B |
| MapFromNotionPageBenchmark | 1,181.9 ns | 23.09 ns | 31.60 ns | 0.0973 |     816 B |

## Related Projects

### Ecosystem

Part of a collection of .NET libraries and tools. See more at [github.com/sarmkadan](https://github.com/sarmkadan).

### Integration Examples

**Syncing tasks and tagging completed items in a single pipeline:**

```csharp
var syncService = serviceProvider.GetRequiredService<SyncService>();
var config = serviceProvider.GetRequiredService<IOptions<SyncConfig>>().Value;

var result = await syncService.ExecuteSyncAsync(config);
foreach (var task in result.SyncedTasks.Where(t => t.Status == "Done"))
{
    task.Tags = task.Tags.Append("archived").ToArray();
    await syncService.PushTaskToNotionAsync(task, config);
}
```

**Triggering a sync in response to an external event:**

```csharp
var eventBus = serviceProvider.GetRequiredService<IEventBus>();

eventBus.Subscribe<DeploymentCompletedEvent>(async _ =>
{
    var result = await syncService.ExecuteSyncAsync(config);
    logger.LogInformation("Post-deploy sync: {Count} tasks updated", result.SyncedCount);
});
```

## Contributing

Contributions are welcome! Please follow these guidelines:

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Follow the code style (see `.editorconfig`)
4. Add tests for new functionality
5. Commit with clear messages (`git commit -m 'Add amazing feature'`)
6. Push to your branch (`git push origin feature/amazing-feature`)
7. Open a Pull Request

### Development Setup

```bash
# Clone and setup
git clone https://github.com/Sarmkadan/notion-task-sync.git
cd notion-task-sync
dotnet restore

# Create local settings
cp appsettings.json appsettings.local.json
# Edit with your test credentials

# Run tests
dotnet test

# Run in debug mode
dotnet run -- sync --verbose
```

### Code Guidelines

- Use meaningful variable names
- Add XML documentation comments to public methods
- Follow C# naming conventions (PascalCase for classes, camelCase for locals)
- Keep methods focused and under 30 lines when possible
- Avoid nested if statements (use early returns)

## License

MIT License - Copyright 2026 Vladyslav Zaiets

This project is provided as-is for educational and commercial use. See LICENSE file for details.

## Changelog

See [CHANGELOG.md](CHANGELOG.md) for version history and migration guides.

## Support

- 📖 [Documentation](./docs/README.md)
- 🐛 [Issue Tracker](https://github.com/Sarmkadan/notion-task-sync/issues)
- 💬 [Discussions](https://github.com/Sarmkadan/notion-task-sync/discussions)
- 📧 Contact: [GitHub Issues](https://github.com/Sarmkadan/notion-task-sync/issues)

---

**Built by [Vladyslav Zaiets](https://sarmkadan.com) - CTO & Software Architect**

[Portfolio](https://sarmkadan.com) | [GitHub](https://github.com/Sarmkadan) | [Telegram](https://t.me/sarmkadan)
