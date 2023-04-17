// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

# Frequently Asked Questions (FAQ)

## General Questions

### What is Notion Task Sync?

Notion Task Sync is a .NET application that synchronizes tasks bidirectionally between Notion databases and local files. It detects changes automatically, resolves conflicts intelligently, and tracks all operations with detailed logging.

### Why would I use Notion Task Sync?

- **Keep tasks in version control** - Store tasks alongside code in Git
- **Work offline** - Sync when you reconnect to the internet
- **Programmatic access** - Integrate with other tools and scripts
- **Local development** - Use with IDEs and text editors
- **Team workflows** - Merge task changes like code
- **Automation** - Trigger syncs from CI/CD pipelines

### What formats are supported?

- **Local files**: JSON (native), also readable as CSV, XML, Markdown
- **Remote**: Notion API (official, REST-based)
- **Export**: JSON, CSV, XML, Markdown

### Is there a web UI?

Currently, no. Notion Task Sync is CLI-based and programmatic. You interact with it through:
- Command-line interface
- REST API (when running as a service)
- Programmatic C# API
- Your favorite text editor (for JSON files)

We recommend using Notion's web UI for viewing/editing tasks, and using Notion Task Sync to keep local copies synchronized.

### What .NET versions are supported?

- **.NET 10** (recommended, latest)
- .NET 9 (with minor adjustments)
- Earlier versions require code modifications

### Does it cost anything?

**No**, it's completely free and open-source under the MIT license. You only need a free Notion Integration.

---

## Installation & Setup

### Do I need a Notion account?

Yes, a free account is sufficient. You need:
- A workspace
- A database with task-like structure
- An integration for API access

### How do I get my Notion API key?

1. Go to https://www.notion.so/my-integrations
2. Click "Create new integration"
3. Name it "Notion Task Sync"
4. Accept the terms and create it
5. Copy the "Internal Integration Token"

**Important**: Keep your token secret! Treat it like a password.

### Can I use an existing database?

Yes! Your database can have any structure. During setup, map your columns to Notion Task Sync's fields:
- Title → Task name
- Status → Task state (Todo, In Progress, Done, Cancelled)
- Priority → Task importance
- Due Date → When it's due
- Tags → Categories
- Assignee → Who's working on it

Unmapped columns are preserved but not synced locally.

### What permissions does the integration need?

Notion Task Sync uses the minimal required permissions:
- Read database content
- Read page content  
- Create pages
- Update pages
- Delete pages
- Read comments (optional)

These are automatically configured when you share the database with the integration.

### Can I use the same integration for multiple databases?

Yes, an integration can access multiple databases. Just configure different sync profiles:

```bash
dotnet run -- configure --name "Project1" --database-id "db1-id"
dotnet run -- configure --name "Project2" --database-id "db2-id"
```

---

## Sync & Conflict Resolution

### How often does it sync?

Default is every 5 minutes. You can change this:

```json
{
  "SyncConfig": {
    "SyncInterval": 600  // 10 minutes in seconds
  }
}
```

Or run manually:
```bash
dotnet run -- sync
```

### What's a conflict?

A conflict occurs when the same task is modified in both locations since the last sync:

```
Local file (modified at 14:30):
  Status: In Progress

Notion database (modified at 14:31):
  Status: Done

Last synced: 14:25
```

Both changes happened after the last sync, so the system doesn't know which to trust.

### How are conflicts resolved?

You configure a resolution strategy:

| Strategy | Behavior | Best For |
|----------|----------|----------|
| `latest-wins` | Newest timestamp wins | Single user, automated workflows |
| `local-priority` | Local changes always win | Local-first development |
| `notion-priority` | Notion changes always win | Notion-first workflows |
| `manual` | Prompt user for each | High-stakes changes |
| `merge` | Intelligently merge fields | Non-overlapping changes |

**Example:**

```json
{
  "SyncConfig": {
    "ConflictResolutionStrategy": "latest-wins",
    "PreferLocalChangesWhen": ["description"],
    "PreferNotionChangesWhen": ["status"]
  }
}
```

### Can I manually resolve conflicts?

Yes, use `manual` strategy and `--force` flag:

```bash
dotnet run -- sync  # Will prompt for each conflict

# Or review before syncing
dotnet run -- sync --dry-run --verbose
```

### Can conflicts cause data loss?

Only if you choose a strategy that overwrites one side. We recommend:
1. Always backup before sync: `--no-backup false`
2. Use `latest-wins` (safest default)
3. Review high-stakes changes with `--dry-run`

### What if I have uncommitted conflicts?

The sync log tracks all conflicts:

```bash
dotnet run -- history --format json > conflicts.json
```

Restore from backup:

```bash
dotnet run -- backup restore --path ./backups/backup-name
```

---

## Performance & Scale

### How many tasks can it handle?

Tested with:
- **Small**: <100 tasks → No issues
- **Medium**: 100-1000 tasks → Optimal performance
- **Large**: 1000-10000 tasks → Batch processing recommended
- **Very large**: 10000+ tasks → Multi-instance deployment needed

For large databases:

```json
{
  "SyncConfig": {
    "MaxTasksPerSync": 500,
    "SyncInterval": 1800
  },
  "Caching": {
    "MaxEntries": 5000
  }
}
```

### Why is sync slow?

**Possible causes and solutions:**

```
Issue: Rate limiting by Notion API
Solution: Increase SyncInterval, reduce RateLimitPerSecond

Issue: Large file operations
Solution: Use batch processing, optimize LocalFolderPath

Issue: Many conflicts to resolve
Solution: Use automated strategy instead of manual

Issue: Network latency
Solution: Run closer to Notion (on cloud), check connectivity

Issue: Disk I/O bottleneck
Solution: Use faster storage (SSD), reduce concurrent operations
```

### Does caching help?

Yes, significantly. Enable it:

```json
{
  "Caching": {
    "Enabled": true,
    "DurationSeconds": 300,
    "MaxEntries": 1000,
    "InvalidateOnSync": true
  }
}
```

This reduces API calls from ~20 to ~5 per sync.

---

## Troubleshooting

### "Invalid API Token" Error

```
[ERROR] Notion API request failed: Invalid API token
```

**Check:**
1. Token is correct: `appsettings.json` or `NOTION_API_KEY` env var
2. Token hasn't expired → Regenerate in Notion Integrations
3. Integration is shared with your database
4. No extra spaces or quotes around token

**Fix:**
```bash
# Regenerate token
# 1. Go to https://www.notion.so/my-integrations
# 2. Find your integration
# 3. Click "Regenerate" 
# 4. Update appsettings.json
```

### "Database Not Found"

```
[ERROR] Database not found or not accessible
```

**Checklist:**
1. Is the Database ID correct? (Copy from URL)
2. Is the integration shared with the database?
   ```
   Database → Share → Find integration → Click to add
   ```
3. Does the integration have necessary permissions?
4. Is the database actually a database? (Not a page)

**Test with a new database:**
```bash
# Create simple test database in Notion
# Share with integration
# Try sync
dotnet run -- sync --verbose
```

### "Rate Limit Exceeded"

```
[ERROR] HTTP 429: Too Many Requests
```

**Solution:**
```json
{
  "NotionApi": {
    "RateLimitPerSecond": 1
  },
  "SyncConfig": {
    "SyncInterval": 600
  }
}
```

Notion allows ~3 requests/second, but shared rate limits apply. Reduce to be safe.

### Local Files Not Updating

**Checklist:**
1. Do you have write permissions? `ls -la ./tasks`
2. Is the path correct in config?
3. Are you syncing in the right direction?
   ```bash
   dotnet run -- sync --direction notion-to-local
   ```
4. Are tasks actually changing in Notion?
5. Check logs: `tail -f ./logs/sync.log`

### Out of Memory Error

**Symptoms:**
- Process crashes or hangs
- High memory usage: `docker stats`

**Solutions:**
```json
{
  "SyncConfig": {
    "MaxTasksPerSync": 100  // Process fewer at a time
  },
  "Caching": {
    "MaxEntries": 500  // Smaller cache
  }
}
```

Or increase available memory:
```bash
docker run -m 1g notion-sync:latest
```

### Sync Takes Too Long

**Increase concurrency:**
```json
{
  "SyncConfig": {
    "MaxTasksPerSync": 100,
    "ParallelismDegree": 4
  },
  "NotionApi": {
    "RateLimitPerSecond": 5
  }
}
```

**Or batch larger syncs:**
```bash
# Instead of syncing 10000 tasks at once
dotnet run -- sync --batch-size 500
# Run multiple times
```

### Disk Space Issues

```
[ERROR] Insufficient disk space
```

**Check and clean:**
```bash
# View size
du -sh ./tasks ./backups ./logs

# Archive old logs
gzip ./logs/sync-*.log

# Delete old backups (keep recent ones)
ls -la ./backups
rm -rf ./backups/backup-old-name

# Clean up local tasks (verify before deleting!)
rm -f ./tasks/archived-*.json
```

---

## Backup & Recovery

### How do backups work?

Before each sync, if enabled, a backup is created:

```
./backups/
├── before-migration_20240115_143215/
│   ├── local/                  # Local tasks snapshot
│   ├── database.db             # SQLite state
│   └── manifest.json           # Backup metadata
├── auto-backup_20240115_093000/
└── ...
```

### How do I restore a backup?

```bash
# List backups
ls -la ./backups

# Restore specific backup
dotnet run -- backup restore --path ./backups/backup-name

# This restores BOTH local files and sync state
```

### Can I restore partially?

Currently, backups are all-or-nothing. To restore partially:

1. Extract backup: `tar -xzf backup.tar.gz`
2. Manually copy files you want
3. Run sync to propagate

### How long are backups kept?

Default: 30 days. Change in config:

```json
{
  "BackupConfig": {
    "RetentionDays": 60
  }
}
```

### Can I backup to cloud storage?

Not built-in, but you can:

```bash
# S3 backup
tar czf backup.tar.gz ./backups/latest
aws s3 cp backup.tar.gz s3://my-bucket/backups/

# Google Cloud
gsutil cp backup.tar.gz gs://my-bucket/backups/

# Azure
az storage blob upload --file backup.tar.gz \
  --container-name backups \
  --name backup.tar.gz
```

---

## Features & Customization

### Can I customize field mapping?

Yes, in `appsettings.json`:

```json
{
  "SyncConfig": {
    "TaskPropertyMapping": {
      "title": "Title",
      "status": "Status",
      "priority": "Priority",
      "dueDate": "Due Date",
      "tags": "Tags",
      "assignee": "Assigned To",
      "customField1": "Custom Field"
    }
  }
}
```

### Can I filter which tasks sync?

Not built-in, but you can:

1. Create separate databases per project
2. Use multiple sync profiles
3. Manually filter files in local folder

### Can I export/import tasks?

**Export:**
```bash
dotnet run -- history --format json > tasks.json
dotnet run -- history --format csv > tasks.csv
```

**Import:**
Manually place JSON files in local folder, then sync:
```bash
dotnet run -- sync --direction local-to-notion
```

### Can I use webhooks?

Not in the current version. Alternatives:

1. **Scheduled sync** (every 5 minutes) - Default behavior
2. **Manual sync** - `dotnet run -- sync`
3. **Systemd timer** - Run on schedule
4. **CI/CD** - Trigger from GitHub Actions, etc.

### Can I run multiple syncs simultaneously?

Not recommended. Configuration includes locking to prevent race conditions. Use:

```json
{
  "SyncConfig": {
    "SyncInterval": 300,
    "MaxConcurrentSyncs": 1
  }
}
```

---

## Integration & Automation

### Can I use this in CI/CD?

Yes! Examples:

**GitHub Actions:**
```yaml
- name: Sync Tasks
  run: |
    dotnet run -- sync \
      --config "CI-Sync" \
      --force \
      --verbose
```

**GitLab CI:**
```yaml
sync_tasks:
  script:
    - dotnet run -- sync --config "CI-Sync" --force
```

### Can I integrate with other tools?

You can read/write local files as JSON. Examples:

**Read in Python:**
```python
import json
with open('tasks/task-123.json') as f:
    task = json.load(f)
```

**Write from your app:**
```csharp
var task = new Task { Title = "New", Status = "Todo" };
File.WriteAllText("tasks/task-id.json", JsonConvert.SerializeObject(task));
```

Then sync:
```bash
dotnet run -- sync --direction local-to-notion
```

### Can I subscribe to sync events?

Yes, programmatically:

```csharp
var eventBus = serviceProvider.GetRequiredService<IEventBus>();

eventBus.Subscribe<SyncCompletedEvent>(async e =>
{
    Console.WriteLine($"Sync completed: {e.Status}");
    // Your custom logic here
});
```

---

## Support & Contributing

### Where can I get help?

- 📖 **Documentation**: [README.md](../README.md)
- 🐛 **Issues**: [GitHub Issues](https://github.com/Sarmkadan/notion-task-sync/issues)
- 💬 **Discussions**: [GitHub Discussions](https://github.com/Sarmkadan/notion-task-sync/discussions)

### How do I report a bug?

1. Check existing issues first
2. Create detailed report with:
   - Steps to reproduce
   - Expected behavior
   - Actual behavior
   - Error logs (redact secrets)
   - Environment (OS, .NET version, etc.)

### Can I contribute?

Absolutely! See [CONTRIBUTING.md](../README.md#contributing) for guidelines.

### Is there a roadmap?

Check [GitHub Issues](https://github.com/Sarmkadan/notion-task-sync/issues) for planned features.

### How often is it updated?

Currently maintained actively. Check [CHANGELOG.md](../CHANGELOG.md) for version history.

---

## Legal & Licensing

### Is it free to use?

Yes, MIT licensed. Free for personal and commercial use.

### Do you collect data?

No. All sync happens locally. We never see your tasks or API keys.

### What about Notion's API limits?

Notion free plan allows 5 million API requests/month (sufficient for most users). Check [Notion API Limits](https://developers.notion.com/docs/getting-started/rate-limits).

### Can I use this commercially?

Yes, MIT license permits commercial use. No restrictions.

### Do you provide support?

Community support via GitHub. No SLA, but active maintainer.

---

**Built by [Vladyslav Zaiets](https://sarmkadan.com) - CTO & Software Architect**
