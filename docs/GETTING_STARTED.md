// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

# Getting Started with Notion Task Sync

This guide will walk you through installing and configuring Notion Task Sync in 15 minutes.

## Prerequisites

Before you begin, ensure you have:
- .NET 10 SDK ([download](https://dotnet.microsoft.com/download))
- A Notion account ([create one](https://www.notion.so))
- Basic familiarity with command line

## Step 1: Create a Notion Integration

1. Navigate to [Notion Integrations](https://www.notion.so/my-integrations)
2. Click **"Create new integration"**
3. Fill in the form:
   - **Name**: "Notion Task Sync"
   - **Logo**: (optional)
   - **Associated workspace**: Select your workspace
   - **Capabilities**: Keep defaults
4. Click **"Submit"**
5. Copy the **"Internal Integration Token"** (you'll need this)

## Step 2: Prepare Your Notion Database

### Option A: Create a New Database

1. Go to your Notion workspace
2. Create a new page and convert it to a database
3. Use the "Task template" or custom create these columns:

| Column | Type | Values |
|--------|------|--------|
| Title | Text | (auto-created) |
| Status | Select | Todo, In Progress, Done, Cancelled |
| Priority | Select | Low, Medium, High |
| Due Date | Date | (any date) |
| Tags | Multi-select | (your categories) |
| Assignee | Person | (your team members) |

4. Add a few test tasks to verify connectivity

### Option B: Use an Existing Database

Your existing database can be used as-is. Map your columns during configuration.

### Share with Integration

1. Open your task database
2. Click **Share** (top right)
3. Search for your integration name "Notion Task Sync"
4. Click to add it
5. Copy the **Database ID** from the URL:
   ```
   https://notion.so/WORKSPACE_ID/DATABASE_ID?v=VIEW_ID
   Copy this: DATABASE_ID
   ```

## Step 3: Install Notion Task Sync

### Option A: From Source (Recommended for Development)

```bash
# Clone repository
git clone https://github.com/Sarmkadan/notion-task-sync.git
cd notion-task-sync

# Create local settings file
cp appsettings.json appsettings.local.json

# Edit with your credentials
nano appsettings.local.json
# Set:
#   NotionApi.ApiKey = your_integration_token
#   NotionApi.DatabaseId = your_database_id

# Restore and build
dotnet restore
dotnet build -c Release

# Verify installation
dotnet run -- help
```

### Option B: Using Docker

```bash
# Pull image
docker pull notion-task-sync:latest

# Or build locally
docker build -t notion-task-sync:latest .

# Create config file
mkdir -p ./config
cat > ./config/appsettings.json << EOF
{
  "NotionApi": {
    "ApiKey": "your_token_here",
    "DatabaseId": "your_db_id_here"
  },
  "AppSettings": {
    "LocalTasksDirectory": "/app/tasks"
  }
}
EOF

# Run sync
docker run -v $(pwd)/config:/app/config \
           -v $(pwd)/tasks:/app/tasks \
           notion-task-sync:latest sync
```

### Option C: Using Docker Compose

```bash
# Clone and prepare
git clone https://github.com/Sarmkadan/notion-task-sync.git
cd notion-task-sync

# Create .env file
cat > .env << EOF
NOTION_API_KEY=your_token_here
NOTION_DATABASE_ID=your_db_id_here
LOCAL_TASKS_DIRECTORY=./tasks
EOF

# Start services
docker-compose up -d

# Check logs
docker-compose logs -f notion-sync
```

## Step 4: Configure the Application

Create your sync configuration:

```bash
dotnet run -- configure \
  --name "MyTaskSync" \
  --database-id "your_database_id" \
  --local-path "./tasks" \
  --conflict-strategy "latest-wins"
```

Or edit `appsettings.json` manually:

```json
{
  "NotionApi": {
    "ApiKey": "your_integration_token",
    "DatabaseId": "your_database_id",
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

## Step 5: Run Your First Sync

Test the connection:

```bash
# Dry run (no changes)
dotnet run -- sync --dry-run --verbose

# If successful, run actual sync
dotnet run -- sync --verbose

# Check status
dotnet run -- status
```

You should see output like:

```
[INFO] Starting sync from Notion to local tasks...
[INFO] Loading Notion database: your_database_id
[INFO] Found 5 pages in Notion
[INFO] Loading local tasks from ./tasks
[INFO] Found 3 local tasks
[INFO] Change detection: 2 new tasks from Notion
[INFO] Syncing 2 new tasks...
[INFO] Sync completed successfully
[INFO] - Synced pages: 5
[INFO] - Local tasks: 3
[INFO] - Conflicts: 0
[INFO] - Duration: 2.5s
```

## Step 6: Set Up Automated Syncs

### Option A: Scheduled Sync with Cron

```bash
# Edit crontab
crontab -e

# Add this line to run sync every 5 minutes
*/5 * * * * cd /path/to/notion-task-sync && dotnet run -- sync

# Run every hour
0 * * * * cd /path/to/notion-task-sync && dotnet run -- sync

# Run at 9 AM daily
0 9 * * * cd /path/to/notion-task-sync && dotnet run -- sync
```

### Option B: Systemd Service

Create `/etc/systemd/system/notion-sync.service`:

```ini
[Unit]
Description=Notion Task Sync Service
After=network.target

[Service]
Type=oneshot
User=ubuntu
WorkingDirectory=/home/ubuntu/notion-task-sync
ExecStart=/usr/bin/dotnet run -- sync

[Install]
WantedBy=multi-user.target
```

Create `/etc/systemd/system/notion-sync.timer`:

```ini
[Unit]
Description=Notion Task Sync Timer
Requires=notion-sync.service

[Timer]
# Run every 5 minutes
OnBootSec=1min
OnUnitActiveSec=5min

[Install]
WantedBy=timers.target
```

Enable and start:

```bash
sudo systemctl daemon-reload
sudo systemctl enable notion-sync.timer
sudo systemctl start notion-sync.timer

# Check status
sudo systemctl status notion-sync.timer
```

### Option C: Docker with Scheduler

In `docker-compose.yml`, use a scheduler like Ofelia:

```yaml
version: '3.8'
services:
  notion-sync:
    image: notion-task-sync:latest
    environment:
      - NotionApi__ApiKey=${NOTION_API_KEY}
      - NotionApi__DatabaseId=${NOTION_DATABASE_ID}
    volumes:
      - ./tasks:/app/tasks
    labels:
      ofelia.enabled: "true"
      ofelia.job-exec.sync.schedule: "@every 5m"
      ofelia.job-exec.sync.command: "dotnet run -- sync"

  scheduler:
    image: mcuadros/ofelia:latest
    volumes:
      - /var/run/docker.sock:/var/run/docker.sock
    command: daemon --docker
```

## Step 7: Verify Everything Works

### Test Sync Workflow

1. **Create a task in Notion**
   - Add a new page to your task database
   - Fill in fields: Title, Status, Priority, Due Date

2. **Run sync to local**
   ```bash
   dotnet run -- sync --direction notion-to-local
   ```

3. **Check local files**
   ```bash
   ls -la ./tasks
   cat ./tasks/*.json
   ```

4. **Modify local task**
   - Edit a local JSON file
   - Update a field

5. **Run sync from local**
   ```bash
   dotnet run -- sync --direction local-to-notion
   ```

6. **Verify in Notion**
   - Check that local changes appeared in Notion

### Monitoring and Debugging

Check logs:

```bash
# View recent logs
tail -f ./logs/sync.log

# Export logs for analysis
dotnet run -- history --limit 100 --format json > sync_history.json
```

## Common Configuration Scenarios

### Scenario 1: Local-First Development

You want tasks in version control alongside code:

```json
{
  "SyncConfig": {
    "ConflictResolutionStrategy": "local-priority",
    "PreferLocalChangesWhen": ["description", "notes", "status"],
    "SyncInterval": 600
  }
}
```

### Scenario 2: Notion as Source of Truth

Your team uses Notion as the primary interface:

```json
{
  "SyncConfig": {
    "ConflictResolutionStrategy": "notion-priority",
    "PreferNotionChangesWhen": ["status", "priority", "assignee"],
    "SyncInterval": 300
  }
}
```

### Scenario 3: Manual Conflict Resolution

For high-stakes environments requiring approval:

```json
{
  "SyncConfig": {
    "ConflictResolutionStrategy": "manual",
    "AutoResolveNonConflicting": true,
    "SyncInterval": 0
  }
}
```

### Scenario 4: Large Scale (1000+ tasks)

For performance with big databases:

```json
{
  "SyncConfig": {
    "MaxTasksPerSync": 500,
    "SyncInterval": 1800
  },
  "Caching": {
    "Enabled": true,
    "DurationSeconds": 600,
    "MaxEntries": 5000
  }
}
```

## Troubleshooting Common Issues

### "Invalid API Token" Error

```
[ERROR] Notion API request failed: Invalid API token
```

**Solution**:
1. Verify token hasn't expired
2. Regenerate in Notion Integrations settings
3. Update `appsettings.json` with new token
4. Test: `dotnet run -- status --verbose`

### "Database Not Found"

```
[ERROR] Database not found or not accessible
```

**Solution**:
1. Copy exact Database ID from URL
2. Ensure integration is shared with database
3. Test with smaller test database first
4. Check permissions in Notion

### "Rate Limit Exceeded"

```
[ERROR] Rate limit exceeded (429)
```

**Solution**:
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

### "Local Files Not Updating"

**Solution**:
1. Check directory permissions: `chmod 755 ./tasks`
2. Verify path in config: `AppSettings:LocalTasksDirectory`
3. Run with `--verbose` flag to see detailed logs
4. Check disk space: `df -h`

## Next Steps

1. **Read the full [README.md](../README.md)** for detailed features
2. **Explore [examples/](../examples/)** for code samples
3. **Review [docs/architecture.md](./architecture.md)** to understand internals
4. **Check [CHANGELOG.md](../CHANGELOG.md)** for version notes

## Getting Help

- 📖 Check [FAQ.md](./faq.md) for answers to common questions
- 🐛 Report issues on [GitHub Issues](https://github.com/Sarmkadan/notion-task-sync/issues)
- 💬 Discuss in [GitHub Discussions](https://github.com/Sarmkadan/notion-task-sync/discussions)

---

**Built by [Vladyslav Zaiets](https://sarmkadan.com) - CTO & Software Architect**
