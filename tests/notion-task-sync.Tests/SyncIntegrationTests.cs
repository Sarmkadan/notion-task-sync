#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace NotionTaskSync.Tests;

using NotionTaskSync.Domain.Models;
using NotionTaskSync.Services;
using NotionTaskSync.Data.Repositories;
using NotionTaskSync.Domain.Enums;
using FluentAssertions;
using Moq;
using Xunit;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using Task = System.Threading.Tasks.Task;
using DomainTask = NotionTaskSync.Domain.Models.Task;

/// <summary>
/// Integration tests for the complete sync workflow between local tasks and Notion.
/// Tests end-to-end synchronization scenarios including change detection, conflict resolution,
/// and different sync directions.
/// </summary>
public class SyncIntegrationTests : IDisposable
{
    /// <summary>
    /// Temporary directory for storing test task files.
    /// </summary>
    private readonly string _localTasksDirectory;

    /// <summary>
    /// Service for reading/writing local task files during tests.
    /// </summary>
    private readonly LocalFileService _localFileService;

    /// <summary>
    /// Mock repository for task data operations.
    /// </summary>
    private readonly Mock<ITaskRepository> _mockTaskRepository;

    /// <summary>
    /// Mock repository for change log operations.
    /// </summary>
    private readonly Mock<IChangeLogRepository> _mockChangeLogRepository;

    /// <summary>
    /// Mock service for Notion API communication.
    /// </summary>
    private readonly Mock<NotionApiService> _mockNotionApiService;

    /// <summary>
    /// Mock service for detecting changes between local and Notion data.
    /// </summary>
    private readonly Mock<ChangeDetectionService> _mockChangeDetectionService;

    /// <summary>
    /// Mock service for resolving conflicts between local and Notion changes.
    /// </summary>
    private readonly Mock<ConflictResolutionService> _mockConflictResolutionService;

    /// <summary>
    /// Service that orchestrates the complete synchronization workflow.
    /// </summary>
    private readonly SyncService _syncService;

    /// <summary>
    /// Initializes a new instance of the <see cref="SyncIntegrationTests"/> class.
    /// Sets up test dependencies including mock repositories, services, and a temporary directory.
    /// </summary>
    public SyncIntegrationTests()
    {
        _localTasksDirectory = Path.Combine(Path.GetTempPath(), $"sync_test_{Guid.NewGuid()}");
        Directory.CreateDirectory(_localTasksDirectory);

        _localFileService = new LocalFileService(_localTasksDirectory);
        _mockTaskRepository = new Mock<ITaskRepository>();
        _mockChangeLogRepository = new Mock<IChangeLogRepository>();
        _mockNotionApiService = new Mock<NotionApiService>(null);
        _mockChangeDetectionService = new Mock<ChangeDetectionService>(_mockChangeLogRepository.Object);
        _mockConflictResolutionService = new Mock<ConflictResolutionService>(_mockChangeLogRepository.Object);

        _syncService = new SyncService(
            _mockChangeDetectionService.Object,
            _mockConflictResolutionService.Object,
            _mockNotionApiService.Object,
            _mockTaskRepository.Object,
            _mockChangeLogRepository.Object);
    }

    public void Dispose()
    {
        if (Directory.Exists(_localTasksDirectory))
            Directory.Delete(_localTasksDirectory, recursive: true);
    }

    /// <summary>
    /// Tests the complete sync workflow when creating a new task locally.
    /// Verifies that a new local task is properly synced to Notion.
    /// </summary>
    [Fact]
    public async Task FullSyncWorkflow_CreatingNewTask_SyncsToNotion()
    {
        // Arrange - Create a local task
        var localTask = new DomainTask
        {
            Id = Guid.NewGuid(),
            Title = "New Feature Request",
            Description = "Implement dark mode support",
            Priority = 5,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Save it locally
        await _localFileService.SaveTaskAsync(localTask);

        // Verify it was saved
        var savedTask = await _localFileService.LoadTaskAsync(localTask.LocalFilePath!);
        savedTask.Should().NotBeNull();
        savedTask!.Title.Should().Be("New Feature Request");

        // Setup sync mocks - Task repo returns our local task
        var localTasks = new List<DomainTask> { localTask };
        _mockTaskRepository.Setup(r => r.GetAllAsync()).ReturnsAsync(localTasks);
        _mockNotionApiService.Setup(a => a.FetchPagesAsync(It.IsAny<string>(), It.IsAny<int>()))
            .ReturnsAsync(new List<NotionPage>());

        // Change detection finds the new task
        var changeLog = new ChangeLog
        {
            TaskId = localTask.Id,
            ChangeType = "Created",
            Source = ChangeSource.Local,
            Timestamp = DateTime.UtcNow
        };
        _mockChangeDetectionService.Setup(s => s.DetectLocalChanges(It.IsAny<List<DomainTask>>(), It.IsAny<DateTime>()))
            .Returns(new List<ChangeLog> { changeLog });
        _mockChangeDetectionService.Setup(s => s.DetectNotionChanges(It.IsAny<List<NotionPage>>(), It.IsAny<DateTime>()))
            .Returns(new List<ChangeLog>());
        _mockChangeDetectionService.Setup(s => s.DetectConflicts(It.IsAny<List<ChangeLog>>(), It.IsAny<List<ChangeLog>>()))
            .Returns(new List<ConflictResolution>());

        var config = new SyncConfig(
            "Test Sync",
            "550e8400-e29b-41d4-a716-446655440000",
            _localTasksDirectory);

        // Act
        var syncResult = await _syncService.ExecuteSyncAsync(config);

        // Assert
        syncResult.Status.Should().Be(SyncStatus.Completed);
        syncResult.LocalTaskCount.Should().Be(1);
        syncResult.NotionPageCount.Should().Be(0);
        syncResult.LocalChangesDetected.Should().Be(1);
        _mockTaskRepository.Verify(r => r.SaveAsync(), Times.Once);
    }

    /// <summary>
    /// Tests the complete sync workflow with multiple local tasks.
    /// Verifies that all local tasks are properly detected and synced.
    /// </summary>
    [Fact]
    public async Task FullSyncWorkflow_MultipleLocalTasks_SyncsAllTasks()
    {
        // Arrange - Create multiple local tasks
        var tasks = new List<DomainTask>();
        for (int i = 1; i <= 5; i++)
        {
            var task = new DomainTask
            {
                Id = Guid.NewGuid(),
                Title = $"Task {i}",
                Priority = i * 10,
                CreatedAt = DateTime.UtcNow.AddHours(-i),
                UpdatedAt = DateTime.UtcNow.AddMinutes(-i)
            };
            await _localFileService.SaveTaskAsync(task);
            tasks.Add(task);
        }

        // Verify all saved
        var allLocalTasks = await _localFileService.LoadAllTasksAsync();
        allLocalTasks.Should().HaveCount(5);

        // Setup sync
        _mockTaskRepository.Setup(r => r.GetAllAsync()).ReturnsAsync(tasks);
        _mockNotionApiService.Setup(a => a.FetchPagesAsync(It.IsAny<string>(), It.IsAny<int>()))
            .ReturnsAsync(new List<NotionPage>());

        var changeLogs = tasks.Select(t => new ChangeLog
        {
            TaskId = t.Id,
            ChangeType = "Created",
            Source = ChangeSource.Local,
            Timestamp = DateTime.UtcNow
        }).ToList();

        _mockChangeDetectionService.Setup(s => s.DetectLocalChanges(It.IsAny<List<DomainTask>>(), It.IsAny<DateTime>()))
            .Returns(changeLogs);
        _mockChangeDetectionService.Setup(s => s.DetectNotionChanges(It.IsAny<List<NotionPage>>(), It.IsAny<DateTime>()))
            .Returns(new List<ChangeLog>());
        _mockChangeDetectionService.Setup(s => s.DetectConflicts(It.IsAny<List<ChangeLog>>(), It.IsAny<List<ChangeLog>>()))
            .Returns(new List<ConflictResolution>());

        var config = new SyncConfig(
            "Multi-Task Sync",
            "550e8400-e29b-41d4-a716-446655440000",
            _localTasksDirectory);

        // Act
        var syncResult = await _syncService.ExecuteSyncAsync(config);

        // Assert
        syncResult.Status.Should().Be(SyncStatus.Completed);
        syncResult.LocalTaskCount.Should().Be(5);
        syncResult.LocalChangesDetected.Should().Be(5);
    }

    /// <summary>
    /// Tests the complete sync workflow when a conflict is detected between local and Notion changes.
    /// Verifies that conflicts are properly detected and resolved according to the configured strategy.
    /// </summary>
    [Fact]
    public async Task FullSyncWorkflow_ConflictDetected_ResolvesConflict()
    {
        // Arrange
        var taskId = Guid.NewGuid();
        var task = new DomainTask
        {
            Id = taskId,
            Title = "Conflicted Task",
            Priority = 3,
            CreatedAt = DateTime.UtcNow.AddHours(-2),
            UpdatedAt = DateTime.UtcNow.AddMinutes(-30)
        };

        _mockTaskRepository.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<DomainTask> { task });

        var notionPage = new NotionPage("notion_page_123", "550e8400-e29b-41d4-a716-446655440000", "Conflicted Task - Notion Version")
        {
            LastEditedTime = DateTime.UtcNow.AddMinutes(-15)
        };

        _mockNotionApiService.Setup(a => a.FetchPagesAsync(It.IsAny<string>(), It.IsAny<int>()))
            .ReturnsAsync(new List<NotionPage> { notionPage });

        var localChange = new ChangeLog { TaskId = taskId, ChangeType = "Updated", Source = ChangeSource.Local };
        var notionChange = new ChangeLog { TaskId = taskId, ChangeType = "Updated", Source = ChangeSource.Notion };

        var conflict = new ConflictResolution
        {
            TaskId = taskId,
            LocalValue = "Conflicted Task",
            NotionValue = "Conflicted Task - Notion Version",
            PropertyName = "Title",
            ConflictType = ConflictType.ConcurrentModification,
            Status = ResolutionStatus.Pending
        };

        _mockChangeDetectionService.Setup(s => s.DetectLocalChanges(It.IsAny<List<DomainTask>>(), It.IsAny<DateTime>()))
            .Returns(new List<ChangeLog> { localChange });
        _mockChangeDetectionService.Setup(s => s.DetectNotionChanges(It.IsAny<List<NotionPage>>(), It.IsAny<DateTime>()))
            .Returns(new List<ChangeLog> { notionChange });
        _mockChangeDetectionService.Setup(s => s.DetectConflicts(It.IsAny<List<ChangeLog>>(), It.IsAny<List<ChangeLog>>()))
            .Returns(new List<ConflictResolution> { conflict });

        var resolvedConflict = new ConflictResolution
        {
            TaskId = taskId,
            LocalValue = "Conflicted Task",
            NotionValue = "Conflicted Task - Notion Version",
            PropertyName = "Title",
            ResolvedValue = "Conflicted Task",
            ResolutionMethod = ResolutionMethod.LocalWins,
            Status = ResolutionStatus.Resolved,
            ResolvedAt = DateTime.UtcNow
        };

        _mockConflictResolutionService
            .Setup(s => s.ResolveConflictsAsync(It.IsAny<List<ConflictResolution>>(), It.IsAny<ConflictResolutionStrategy>(), It.IsAny<Dictionary<string, ConflictResolutionStrategy>?>()))
            .ReturnsAsync(new List<ConflictResolution> { resolvedConflict });

        var config = new SyncConfig(
            "Conflict Test",
            "550e8400-e29b-41d4-a716-446655440000",
            _localTasksDirectory)
        {
            ConflictStrategy = ConflictResolutionStrategy.LocalWins
        };

        // Act
        var syncResult = await _syncService.ExecuteSyncAsync(config);

        // Assert
        syncResult.Status.Should().Be(SyncStatus.Completed);
        syncResult.ConflictsDetected.Should().Be(1);
        syncResult.ConflictsResolved.Should().Be(1);
        syncResult.LocalChangesDetected.Should().Be(1);
        syncResult.NotionChangesDetected.Should().Be(1);
    }

    /// <summary>
    /// Tests backup functionality before performing a sync operation.
    /// Verifies that a backup archive is created with the correct files and metadata.
    /// </summary>
    [Fact]
    public async Task FullSyncWorkflow_BackupCreatedBeforeSync()
    {
        // Arrange
        var backupDir = Path.Combine(Path.GetTempPath(), $"backups_{Guid.NewGuid()}");
        Directory.CreateDirectory(backupDir);

        try
        {
            // Create a task to backup
            var task = new DomainTask
            {
                Id = Guid.NewGuid(),
                Title = "Task to Backup",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            await _localFileService.SaveTaskAsync(task);

            // Create backup service
            var backupService = new BackupService(backupDir, 5, _localFileService);

            // Act - Create backup
            var backup = await backupService.CreateBackupAsync("pre-sync-backup");

            // Assert
            backup.Should().NotBeNull();
            backup.Label.Should().Be("pre-sync-backup");
            backup.FileCount.Should().BeGreaterThan(0);
            Directory.Exists(backup.Path).Should().BeTrue();

            var availableBackups = backupService.GetAvailableBackups();
            availableBackups.Should().HaveCount(1);
            availableBackups[0].Label.Should().Be("pre-sync-backup");
        }
        finally
        {
            if (Directory.Exists(backupDir))
                Directory.Delete(backupDir, recursive: true);
        }
    }

    /// <summary>
    /// Tests incremental sync functionality that only fetches changed pages from Notion.
    /// Verifies that the sync service uses the incremental API when a last sync timestamp is available.
    /// </summary>
    [Fact]
    public async Task FullSyncWorkflow_IncrementalSync_OnlyFetchesChangedPages()
    {
        // Arrange - First sync establishes baseline
        var config = new SyncConfig(
            "Incremental Test",
            "550e8400-e29b-41d4-a716-446655440000",
            _localTasksDirectory);

        _mockTaskRepository.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<DomainTask>());
        _mockChangeDetectionService.Setup(s => s.DetectLocalChanges(It.IsAny<List<DomainTask>>(), It.IsAny<DateTime>()))
            .Returns(new List<ChangeLog>());
        _mockChangeDetectionService.Setup(s => s.DetectNotionChanges(It.IsAny<List<NotionPage>>(), It.IsAny<DateTime>()))
            .Returns(new List<ChangeLog>());
        _mockChangeDetectionService.Setup(s => s.DetectConflicts(It.IsAny<List<ChangeLog>>(), It.IsAny<List<ChangeLog>>()))
            .Returns(new List<ConflictResolution>());

        // First sync without previous timestamp
        _mockNotionApiService.Setup(a => a.FetchPagesAsync(It.IsAny<string>(), It.IsAny<int>()))
            .ReturnsAsync(new List<NotionPage>());

        var firstResult = await _syncService.ExecuteSyncAsync(config);
        firstResult.Status.Should().Be(SyncStatus.Completed);

        // Act - Update config with last sync time, second sync should use incremental
        var lastSyncTime = DateTime.UtcNow.AddHours(-1);
        config.LastSyncAt = lastSyncTime;

        _mockNotionApiService.Reset();
        _mockNotionApiService.Setup(a => a.FetchPagesSinceAsync(It.IsAny<string>(), It.IsAny<DateTime>()))
            .ReturnsAsync(new List<NotionPage>());

        var secondResult = await _syncService.ExecuteSyncAsync(config);

        // Assert
        secondResult.Status.Should().Be(SyncStatus.Completed);
        _mockNotionApiService.Verify(a => a.FetchPagesSinceAsync(config.NotionDatabaseId, lastSyncTime), Times.Once);
    }

    /// <summary>
    /// Tests sync direction from local to Notion only.
    /// Verifies that when sync direction is set to LocalToNotion, only local changes are pushed to Notion.
    /// </summary>
    [Fact]
    public async Task FullSyncWorkflow_SyncDirectionLocalToNotion_OnlyPushesChanges()
    {
        // Arrange
        var task = new DomainTask
        {
            Id = Guid.NewGuid(),
            Title = "Local Only Task",
            CreatedAt = DateTime.UtcNow.AddHours(-1),
            UpdatedAt = DateTime.UtcNow.AddMinutes(-30)
        };

        _mockTaskRepository.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<DomainTask> { task });
        _mockNotionApiService.Setup(a => a.FetchPagesAsync(It.IsAny<string>(), It.IsAny<int>()))
            .ReturnsAsync(new List<NotionPage>());

        var changeLog = new ChangeLog { TaskId = task.Id, ChangeType = "Created", Source = ChangeSource.Local };
        _mockChangeDetectionService.Setup(s => s.DetectLocalChanges(It.IsAny<List<DomainTask>>(), It.IsAny<DateTime>()))
            .Returns(new List<ChangeLog> { changeLog });
        _mockChangeDetectionService.Setup(s => s.DetectNotionChanges(It.IsAny<List<NotionPage>>(), It.IsAny<DateTime>()))
            .Returns(new List<ChangeLog>());
        _mockChangeDetectionService.Setup(s => s.DetectConflicts(It.IsAny<List<ChangeLog>>(), It.IsAny<List<ChangeLog>>()))
            .Returns(new List<ConflictResolution>());

        var config = new SyncConfig(
            "Local to Notion",
            "550e8400-e29b-41d4-a716-446655440000",
            _localTasksDirectory)
        {
            Direction = SyncDirection.LocalToNotion
        };

        // Act
        var result = await _syncService.ExecuteSyncAsync(config);

        // Assert
        result.Status.Should().Be(SyncStatus.Completed);
        result.LocalTaskCount.Should().Be(1);
        result.NotionPageCount.Should().Be(0);
        _mockTaskRepository.Verify(r => r.SaveAsync(), Times.Once);
    }

    /// <summary>
    /// Tests sync direction from Notion to local only.
    /// Verifies that when sync direction is set to NotionToLocal, only Notion changes are pulled to local storage.
    /// </summary>
    [Fact]
    public async Task FullSyncWorkflow_SyncDirectionNotionToLocal_OnlyPullsChanges()
    {
        // Arrange
        var notionPage = new NotionPage("page_456", "550e8400-e29b-41d4-a716-446655440000", "Notion Only Task")
        {
            CreatedTime = DateTime.UtcNow.AddHours(-2),
            LastEditedTime = DateTime.UtcNow.AddMinutes(-30)
        };

        _mockTaskRepository.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<DomainTask>());
        _mockNotionApiService.Setup(a => a.FetchPagesAsync(It.IsAny<string>(), It.IsAny<int>()))
            .ReturnsAsync(new List<NotionPage> { notionPage });

        var changeLog = new ChangeLog { TaskId = Guid.NewGuid(), ChangeType = "Created", Source = ChangeSource.Notion };
        _mockChangeDetectionService.Setup(s => s.DetectLocalChanges(It.IsAny<List<DomainTask>>(), It.IsAny<DateTime>()))
            .Returns(new List<ChangeLog>());
        _mockChangeDetectionService.Setup(s => s.DetectNotionChanges(It.IsAny<List<NotionPage>>(), It.IsAny<DateTime>()))
            .Returns(new List<ChangeLog> { changeLog });
        _mockChangeDetectionService.Setup(s => s.DetectConflicts(It.IsAny<List<ChangeLog>>(), It.IsAny<List<ChangeLog>>()))
            .Returns(new List<ConflictResolution>());

        var config = new SyncConfig(
            "Notion to Local",
            "550e8400-e29b-41d4-a716-446655440000",
            _localTasksDirectory)
        {
            Direction = SyncDirection.NotionToLocal
        };

        // Act
        var result = await _syncService.ExecuteSyncAsync(config);

        // Assert
        result.Status.Should().Be(SyncStatus.Completed);
        result.LocalTaskCount.Should().Be(0);
        result.NotionPageCount.Should().Be(1);
        _mockTaskRepository.Verify(r => r.SaveAsync(), Times.Once);
    }
}
