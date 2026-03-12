// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

# Deployment Guide

Comprehensive guide for deploying Notion Task Sync in production environments.

## Deployment Options

### Option 1: Docker (Recommended)

#### Prerequisites
- Docker 20.10+
- Docker Compose 2.0+ (optional)

#### Single Container

```bash
# Build image
docker build -t notion-task-sync:latest .

# Create config
mkdir -p config
cat > config/appsettings.json << 'EOF'
{
  "NotionApi": {
    "ApiKey": "${NOTION_API_KEY}",
    "DatabaseId": "${NOTION_DATABASE_ID}"
  },
  "AppSettings": {
    "LocalTasksDirectory": "/data/tasks",
    "LogFilePath": "/var/log/notion-sync/sync.log"
  },
  "SyncConfig": {
    "SyncInterval": 300,
    "ConflictResolutionStrategy": "latest-wins"
  }
}
EOF

# Run container
docker run -d \
  --name notion-sync \
  -e NOTION_API_KEY="your_token" \
  -e NOTION_DATABASE_ID="your_db_id" \
  -v $(pwd)/config:/app/config \
  -v $(pwd)/data:/data \
  -v $(pwd)/logs:/var/log/notion-sync \
  --restart unless-stopped \
  notion-task-sync:latest
```

#### Docker Compose

Create `docker-compose.yml`:

```yaml
version: '3.8'

services:
  notion-sync:
    build: .
    container_name: notion-task-sync
    environment:
      - NotionApi__ApiKey=${NOTION_API_KEY}
      - NotionApi__DatabaseId=${NOTION_DATABASE_ID}
      - AppSettings__LocalTasksDirectory=/data/tasks
      - Logging__LogLevel=Information
    volumes:
      - ./config:/app/config
      - ./data/tasks:/data/tasks
      - ./data/backups:/app/backups
      - ./logs:/var/log/notion-sync
    restart: unless-stopped
    healthcheck:
      test: ["CMD", "dotnet", "run", "--", "status"]
      interval: 300s
      timeout: 10s
      retries: 3

  # Optional: Log aggregation
  loki:
    image: grafana/loki:latest
    volumes:
      - ./loki-config.yml:/etc/loki/local-config.yaml
    command: -config.file=/etc/loki/local-config.yaml

volumes:
  logs:
    driver: local
```

Create `.env`:

```bash
NOTION_API_KEY=your_integration_token
NOTION_DATABASE_ID=your_database_id
```

Start:

```bash
docker-compose up -d
```

### Option 2: Kubernetes

#### Prerequisites
- Kubernetes 1.24+
- kubectl configured
- PersistentVolume provisioner

#### Deployment YAML

Create `k8s-deployment.yaml`:

```yaml
apiVersion: v1
kind: ConfigMap
metadata:
  name: notion-sync-config
data:
  appsettings.json: |
    {
      "NotionApi": {
        "ApiKey": "from-secret",
        "DatabaseId": "from-secret",
        "RateLimitPerSecond": 3
      },
      "AppSettings": {
        "LocalTasksDirectory": "/data/tasks",
        "LogFilePath": "/var/log/notion-sync/sync.log"
      },
      "SyncConfig": {
        "SyncInterval": 300,
        "ConflictResolutionStrategy": "latest-wins"
      }
    }

---
apiVersion: v1
kind: Secret
metadata:
  name: notion-sync-secret
type: Opaque
stringData:
  api-key: YOUR_NOTION_API_KEY
  database-id: YOUR_DATABASE_ID

---
apiVersion: v1
kind: PersistentVolumeClaim
metadata:
  name: notion-sync-pvc
spec:
  accessModes:
    - ReadWriteOnce
  resources:
    requests:
      storage: 10Gi

---
apiVersion: apps/v1
kind: Deployment
metadata:
  name: notion-task-sync
spec:
  replicas: 1
  selector:
    matchLabels:
      app: notion-sync
  template:
    metadata:
      labels:
        app: notion-sync
    spec:
      containers:
      - name: notion-sync
        image: notion-task-sync:latest
        imagePullPolicy: Always
        
        env:
        - name: NotionApi__ApiKey
          valueFrom:
            secretKeyRef:
              name: notion-sync-secret
              key: api-key
        - name: NotionApi__DatabaseId
          valueFrom:
            secretKeyRef:
              name: notion-sync-secret
              key: database-id
        - name: AppSettings__LocalTasksDirectory
          value: /data/tasks
        - name: Logging__LogLevel
          value: Information
        
        volumeMounts:
        - name: config
          mountPath: /app/config
        - name: data
          mountPath: /data
        - name: logs
          mountPath: /var/log/notion-sync
        
        resources:
          requests:
            memory: "256Mi"
            cpu: "250m"
          limits:
            memory: "512Mi"
            cpu: "500m"
        
        livenessProbe:
          exec:
            command:
            - dotnet
            - run
            - --
            - status
          initialDelaySeconds: 30
          periodSeconds: 300
        
        readinessProbe:
          exec:
            command:
            - dotnet
            - run
            - --
            - status
          initialDelaySeconds: 10
          periodSeconds: 60
      
      volumes:
      - name: config
        configMap:
          name: notion-sync-config
      - name: data
        persistentVolumeClaim:
          claimName: notion-sync-pvc
      - name: logs
        emptyDir: {}

---
apiVersion: batch/v1
kind: CronJob
metadata:
  name: notion-sync-job
spec:
  schedule: "*/5 * * * *"  # Every 5 minutes
  jobTemplate:
    spec:
      template:
        spec:
          containers:
          - name: notion-sync
            image: notion-task-sync:latest
            command:
            - /bin/sh
            - -c
            - dotnet run -- sync
            env:
            - name: NotionApi__ApiKey
              valueFrom:
                secretKeyRef:
                  name: notion-sync-secret
                  key: api-key
            - name: NotionApi__DatabaseId
              valueFrom:
                secretKeyRef:
                  name: notion-sync-secret
                  key: database-id
          restartPolicy: OnFailure
```

Deploy:

```bash
kubectl apply -f k8s-deployment.yaml

# Verify deployment
kubectl get deployments
kubectl get pods
kubectl logs -f deployment/notion-task-sync
```

### Option 3: Systemd Service (Linux)

#### Create Service File

Create `/etc/systemd/system/notion-sync.service`:

```ini
[Unit]
Description=Notion Task Sync Service
After=network.target
Wants=notion-sync.timer

[Service]
Type=simple
User=notion
WorkingDirectory=/opt/notion-sync
ExecStart=/usr/bin/dotnet run -- worker --type sync
Restart=on-failure
RestartSec=10
StandardOutput=journal
StandardError=journal
SyslogIdentifier=notion-sync

# Environment variables
EnvironmentFile=/etc/notion-sync/.env

# Resource limits
MemoryLimit=512M
CPUQuota=50%

[Install]
WantedBy=multi-user.target
```

#### Create Timer

Create `/etc/systemd/system/notion-sync.timer`:

```ini
[Unit]
Description=Notion Task Sync Timer
Requires=notion-sync.service

[Timer]
# Run every 5 minutes
OnBootSec=1min
OnUnitActiveSec=5min
Persistent=true

[Install]
WantedBy=timers.target
```

#### Enable and Start

```bash
# Create user
sudo useradd -r -s /bin/false notion

# Setup directory
sudo mkdir -p /opt/notion-sync
sudo chown notion:notion /opt/notion-sync

# Copy application
sudo cp -r . /opt/notion-sync

# Create config directory
sudo mkdir -p /etc/notion-sync
sudo tee /etc/notion-sync/.env << EOF
NOTION_API_KEY=your_token
NOTION_DATABASE_ID=your_db_id
EOF
sudo chmod 600 /etc/notion-sync/.env
sudo chown notion:notion /etc/notion-sync/.env

# Enable service
sudo systemctl daemon-reload
sudo systemctl enable notion-sync.timer
sudo systemctl start notion-sync.timer

# Check status
sudo systemctl status notion-sync.timer
sudo systemctl list-timers
```

### Option 4: Cloud Platforms

#### AWS Lambda

Create `lambda-handler.cs`:

```csharp
using Amazon.Lambda.Core;

[assembly: LambdaSerializer(
    typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace NotionTaskSync.Lambda;

public class SyncHandler
{
    private readonly SyncService _syncService;
    
    public SyncHandler()
    {
        var config = BuildConfiguration();
        var services = new ServiceCollection();
        services.AddApplicationServices(config);
        var provider = services.BuildServiceProvider();
        _syncService = provider.GetRequiredService<SyncService>();
    }
    
    public async Task<string> HandleAsync(ILambdaContext context)
    {
        try
        {
            var syncConfig = LoadConfigFromDynamoDB();
            var result = await _syncService.ExecuteSyncAsync(syncConfig);
            return $"Sync completed: {result.Status}";
        }
        catch (Exception ex)
        {
            context.Logger.LogError($"Sync failed: {ex.Message}");
            throw;
        }
    }
}
```

Deploy:

```bash
dotnet lambda package --configuration Release
aws lambda create-function \
  --function-name notion-sync \
  --runtime dotnet10 \
  --role arn:aws:iam::YOUR_ACCOUNT:role/lambda-role \
  --handler NotionTaskSync.Lambda::NotionTaskSync.Lambda.SyncHandler::HandleAsync \
  --zip-file fileb://package.zip

# Create CloudWatch Events trigger
aws events put-rule \
  --name notion-sync-schedule \
  --schedule-expression "rate(5 minutes)"
```

#### Azure Container Instances

```bash
# Create resource group
az group create -n notion-sync -l eastus

# Create container registry
az acr create -g notion-sync -n notionsyncreg --sku Basic

# Build and push image
az acr build -r notionsyncreg \
  --image notion-sync:latest \
  .

# Deploy container
az container create \
  -g notion-sync \
  -n notion-sync-container \
  --image notionsyncreg.azurecr.io/notion-sync:latest \
  --environment-variables \
    NOTION_API_KEY=$NOTION_TOKEN \
    NOTION_DATABASE_ID=$DATABASE_ID \
  --restart-policy Always \
  --cpu 0.5 \
  --memory 0.5 \
  --registry-login-server notionsyncreg.azurecr.io \
  --registry-username <username> \
  --registry-password <password>
```

## Production Configuration

### Security Best Practices

#### Environment Variables

Never commit secrets to version control:

```bash
# Use .env files (added to .gitignore)
NOTION_API_KEY=secret_key_here
NOTION_DATABASE_ID=database_id_here
```

#### Secret Management

Use platform-specific secret managers:

**Docker:**
```bash
docker secret create notion_api_key /path/to/secret
```

**Kubernetes:**
```bash
kubectl create secret generic notion-sync-secret \
  --from-literal=api-key=YOUR_KEY \
  --from-literal=database-id=YOUR_ID
```

**AWS:**
```bash
aws secretsmanager create-secret \
  --name notion-sync/api-key \
  --secret-string $NOTION_API_KEY
```

### Performance Tuning

#### Connection Pooling

```json
{
  "AppSettings": {
    "DatabaseConnectionPoolSize": 10,
    "HttpClientMaxConnections": 5,
    "LocalFileBufferSize": 8192
  }
}
```

#### Caching Strategy

```json
{
  "Caching": {
    "Enabled": true,
    "DurationSeconds": 600,
    "MaxEntries": 5000,
    "InvalidateOnSync": true
  }
}
```

#### Rate Limiting

```json
{
  "RateLimiting": {
    "Enabled": true,
    "RequestsPerSecond": 2,
    "BurstSize": 3,
    "RetryAfterMs": 1000
  }
}
```

### Monitoring & Logging

#### Structured Logging

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "NotionTaskSync": "Debug",
      "Microsoft": "Warning"
    },
    "Console": {
      "IncludeScopes": true,
      "TimestampFormat": "yyyy-MM-dd HH:mm:ss"
    }
  }
}
```

#### Log Shipping

Configure ELK Stack or equivalent:

```yaml
# filebeat.yml
filebeat.inputs:
- type: log
  paths:
    - /var/log/notion-sync/*.log
  fields:
    service: notion-task-sync

output.elasticsearch:
  hosts: ["elasticsearch:9200"]
```

#### Health Checks

```bash
# HTTP health endpoint
curl http://localhost:5000/health

# CLI health check
dotnet run -- status --exit-code

# Docker health check
docker inspect --format='{{.State.Health.Status}}' notion-sync
```

### Backup Strategy

#### Automated Backups

```json
{
  "BackupConfig": {
    "Enabled": true,
    "Schedule": "0 2 * * *",
    "RetentionDays": 30,
    "BackupPath": "/backups"
  }
}
```

#### S3 Remote Backup

```csharp
public class S3BackupStrategy : IBackupStrategy
{
    private readonly IAmazonS3 _s3Client;
    
    public async Task UploadBackupAsync(string backupPath)
    {
        var fileInfo = new FileInfo(backupPath);
        var request = new PutObjectRequest
        {
            BucketName = "notion-sync-backups",
            Key = $"backups/{fileInfo.Name}",
            FilePath = backupPath,
            ServerSideEncryptionMethod = ServerSideEncryptionMethod.AES256
        };
        
        await _s3Client.PutObjectAsync(request);
    }
}
```

## Scaling Considerations

### Horizontal Scaling (Multiple Instances)

Use a distributed lock mechanism to prevent simultaneous syncs:

```csharp
public class DistributedLockProvider
{
    public async Task<IDisposable> AcquireLockAsync(string key)
    {
        // Using Redis
        var lockValue = Guid.NewGuid().ToString();
        var acquired = await _redis.SetAsync(
            $"lock:{key}",
            lockValue,
            TimeSpan.FromMinutes(5)
        );
        
        if (!acquired)
            throw new LockAcquisitionException($"Could not acquire lock: {key}");
        
        return new RedisLockHandle(_redis, key, lockValue);
    }
}
```

### Database Sharding

For large task volumes, shard by user or project:

```csharp
public class ShardedTaskRepository
{
    public int GetShard(string taskId) => GetHashCode(taskId) % _shardCount;
    
    public async Task SaveAsync(Task task)
    {
        var shard = GetShard(task.Id);
        var db = _shardedConnections[shard];
        await db.ExecuteAsync("INSERT INTO Tasks ...", task);
    }
}
```

## Troubleshooting Deployment

### Common Issues

#### "API Key Invalid" in Production

```bash
# Verify environment variable
docker exec notion-sync env | grep NOTION

# Regenerate token in Notion Integrations
# Update secret
kubectl patch secret notion-sync-secret \
  -p '{"data":{"api-key":"'$(echo -n $NEW_KEY | base64)'"}}'
```

#### High Memory Usage

```bash
# Check memory limits
docker stats notion-sync

# Reduce cache size
{
  "Caching": {
    "MaxEntries": 1000
  }
}

# Enable garbage collection tuning
export DOTNET_TieredCompilation=1
export DOTNET_TieredCompilationQuickJit=1
```

#### Sync Taking Too Long

```bash
# Enable batch processing
dotnet run -- sync --batch-size 50

# Reduce rate limiting for faster API calls
{
  "RateLimiting": {
    "RequestsPerSecond": 5
  }
}
```

## Rollback Procedure

```bash
# Docker: Switch to previous image
docker service update \
  --image notion-task-sync:v1.1.0 \
  notion-sync

# Kubernetes: Rollback deployment
kubectl rollout undo deployment/notion-task-sync

# Systemd: Revert configuration
sudo systemctl stop notion-sync
sudo cp /etc/notion-sync/config.backup /etc/notion-sync/config
sudo systemctl start notion-sync

# Restore from backup
dotnet run -- backup restore \
  --path ./backups/pre-upgrade_20240115_143215
```

---

**Built by [Vladyslav Zaiets](https://sarmkadan.com) - CTO & Software Architect**
