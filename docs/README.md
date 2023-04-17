// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

# Documentation

Welcome to Notion Task Sync documentation. This directory contains comprehensive guides and references.

## Quick Navigation

### Getting Started
- **[GETTING_STARTED.md](./GETTING_STARTED.md)** - Installation and initial setup (15 min guide)
- **[FAQ.md](./FAQ.md)** - Frequently asked questions and troubleshooting

### Developer Resources
- **[ARCHITECTURE.md](./ARCHITECTURE.md)** - System design and component overview
- **[API_REFERENCE.md](./API_REFERENCE.md)** - Complete API documentation
- **[DEPLOYMENT.md](./DEPLOYMENT.md)** - Deployment guides for all platforms

## Documentation Structure

```
docs/
├── README.md                 # This file
├── GETTING_STARTED.md        # Installation and setup guide
├── ARCHITECTURE.md           # System architecture and design patterns
├── API_REFERENCE.md          # API documentation (CLI, REST, programmatic)
├── DEPLOYMENT.md             # Deployment on Docker, K8s, cloud platforms
└── FAQ.md                    # FAQs and troubleshooting
```

## For Different Audiences

### I want to use Notion Task Sync
👉 Start with [GETTING_STARTED.md](./GETTING_STARTED.md)

### I'm integrating it into my app
👉 Check [API_REFERENCE.md](./API_REFERENCE.md)

### I'm deploying to production
👉 Read [DEPLOYMENT.md](./DEPLOYMENT.md)

### I'm contributing code
👉 Review [ARCHITECTURE.md](./ARCHITECTURE.md) then the [README](../README.md#contributing)

### I have questions
👉 Search [FAQ.md](./FAQ.md) first

## Key Concepts

### Sync Cycle
1. **Load State** - Read local files and fetch Notion pages
2. **Detect Changes** - Compare against last sync state
3. **Identify Conflicts** - Find simultaneous modifications
4. **Resolve Conflicts** - Apply configured strategy
5. **Apply Changes** - Update both directions
6. **Log Results** - Record what happened

### Conflict Scenarios
- **Non-conflict change** - Same field modified only in one location → Apply automatically
- **Conflict** - Same field modified in both locations after last sync → Use conflict resolution strategy
- **Deletion conflict** - Task deleted in one location, modified in another → Requires resolution

### Configuration
- **CLI-based** - `dotnet run -- configure` command
- **File-based** - Edit `appsettings.json` directly
- **Environment vars** - Override via `NOTION_API_KEY`, `NOTION_DATABASE_ID`, etc.

## Common Workflows

### Workflow 1: Local-First Development
```
Your code changes
     ↓
Edit tasks locally (JSON files)
     ↓
Commit to Git
     ↓
Sync local → Notion every 5 minutes
     ↓
Team sees changes in Notion
```

### Workflow 2: Notion as Source of Truth
```
Team updates Notion database
     ↓
Sync Notion → Local every 5 minutes
     ↓
Developer pulls latest local files
     ↓
Integrate tasks into development
```

### Workflow 3: CI/CD Automation
```
GitHub Action triggers on push
     ↓
Run `dotnet run -- sync`
     ↓
Notion database auto-updated
     ↓
Slack notification sent
```

## Troubleshooting Quick Links

| Problem | Solution |
|---------|----------|
| API Key invalid | [FAQ → Installation Setup](./FAQ.md#do-i-need-a-notion-account) |
| Database not found | [FAQ → Setup](./FAQ.md#how-do-i-get-my-notion-api-key) |
| Sync is slow | [DEPLOYMENT → Performance Tuning](./DEPLOYMENT.md#performance-tuning) |
| Out of memory | [FAQ → Performance & Scale](./FAQ.md#out-of-memory-error) |
| Conflicts keep happening | [FAQ → Sync & Conflict Resolution](./FAQ.md#how-are-conflicts-resolved) |

## API Quick Reference

### CLI Commands
```bash
# Configure a sync
dotnet run -- configure --name "MySync" --database-id "abc..."

# Execute sync
dotnet run -- sync

# Check status
dotnet run -- status

# View history
dotnet run -- history --limit 100
```

### Programmatic API
```csharp
// Get sync service
var syncService = serviceProvider.GetRequiredService<SyncService>();

// Execute sync
var result = await syncService.ExecuteSyncAsync(config);

// Subscribe to events
eventBus.Subscribe<SyncCompletedEvent>(e => { ... });
```

### REST Endpoints
```
POST /api/sync
GET /api/status
POST /api/backup
GET /api/history
```

## Configuration Reference

Essential settings in `appsettings.json`:

```json
{
  "NotionApi": {
    "ApiKey": "your_token",
    "DatabaseId": "your_db_id",
    "RateLimitPerSecond": 3
  },
  "AppSettings": {
    "LocalTasksDirectory": "./tasks",
    "BackupDirectory": "./backups",
    "LogFilePath": "./logs/sync.log"
  },
  "SyncConfig": {
    "SyncInterval": 300,
    "ConflictResolutionStrategy": "latest-wins",
    "AutoBackup": true
  }
}
```

See [DEPLOYMENT.md → Production Configuration](./DEPLOYMENT.md#production-configuration) for complete reference.

## Performance Benchmarks

| Database Size | Sync Duration | Memory Usage | Disk I/O |
|---|---|---|---|
| <100 tasks | ~1s | <100MB | Minimal |
| 100-1000 tasks | 2-5s | 100-200MB | Low |
| 1000-10000 tasks | 5-15s | 200-400MB | Medium |
| >10000 tasks | >30s | >500MB | High |

For large databases, use batching and increase sync intervals.

## Version Support

- **Latest (1.2.0)** - .NET 10, full feature support
- **Stable (1.1.0)** - .NET 8, all core features
- **LTS (1.0.0)** - .NET 8, security updates only

See [CHANGELOG.md](../CHANGELOG.md) for version details.

## Resources

- 🏠 **[Main README](../README.md)** - Project overview
- 📚 **[Examples](../examples/)** - Code samples
- 🐛 **[Issue Tracker](https://github.com/Sarmkadan/notion-task-sync/issues)**
- 💬 **[Discussions](https://github.com/Sarmkadan/notion-task-sync/discussions)**

## Contributing to Docs

Documentation improvements are welcome! See [README → Contributing](../README.md#contributing) for guidelines.

---

**Built by [Vladyslav Zaiets](https://sarmkadan.com) - CTO & Software Architect**
