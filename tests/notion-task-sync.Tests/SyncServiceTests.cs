#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace NotionTaskSync.Tests;

using NotionTaskSync.Domain.Models;
using NotionTaskSync.Services;
using NotionTaskSync.Data.Repositories;
using NotionTaskSync.Domain.Exceptions;
using NotionTaskSync.Domain.Enums;
using FluentAssertions;
using Moq;
using Xunit;
using Microsoft.Extensions.Logging;

public class SyncServiceTests
{
    private readonly Mock<ChangeDetectionService> _mockChangeDetectionService;
    private readonly Mock<ConflictResolutionService> _mockConflictResolutionService;
    private readonly Mock<NotionApiService> _mockNotionApiService;
    private readonly Mock<ITaskRepository> _mockTaskRepository;
    private readonly Mock<IChangeLogRepository> _mockChangeLogRepository;
    private readonly Mock<ILogger<SyncService>> _mockLogger;
    private readonly SyncService _syncService;

    public SyncServiceTests()
    {
        _mockChangeDetectionService = new Mock<ChangeDetectionService>(
            new Mock<IChangeLogRepository>().Object);
        _mockConflictResolutionService = new Mock<ConflictResolutionService>(
            new Mock<IChangeLogRepository>().Object);
        _mockNotionApiService = new Mock<NotionApiService>(null);
        _mockTaskRepository = new Mock<ITaskRepository>();
        _mockChangeLogRepository = new Mock<IChangeLogRepository>();
        _mockLogger = new Mock<ILogger<SyncService>>();

        _syncService = new SyncService(
            _mockChangeDetectionService.Object,
            _mockConflictResolutionService.Object,
            _mockNotionApiService.Object,
            _mockTaskRepository.Object,
            _mockChangeLogRepository.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task ExecuteSyncAsync_WithValidConfig_ReturnsCompletedStatus()
    {
        // Arrange
        var config = new SyncConfig("test-sync", "550e8400-e29b-41d4-a716-446655440000", "/tmp/tasks")
        {
            Direction = SyncDirection.Bidirectional
        };

        _mockTaskRepository.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Task>());
        _mockNotionApiService.Setup(a => a.FetchPagesAsync(It.IsAny<string>(), It.IsAny<int>()))
            .ReturnsAsync(new List<NotionPage>());
        _mockChangeDetectionService.Setup(s => s.DetectLocalChanges(It.IsAny<List<Task>>(), It.IsAny<DateTime>()))
            .Returns(new List<ChangeLog>());
        _mockChangeDetectionService.Setup(s => s.DetectNotionChanges(It.IsAny<List<NotionPage>>(), It.IsAny<DateTime>()))
            .Returns(new List<ChangeLog>());
        _mockChangeDetectionService.Setup(s => s.DetectConflicts(It.IsAny<List<ChangeLog>>(), It.IsAny<List<ChangeLog>>()))
            .Returns(new List<ConflictResolution>());

        // Act
        var result = await _syncService.ExecuteSyncAsync(config);

        // Assert
        result.Status.Should().Be(SyncStatus.Completed);
        result.ConfigId.Should().Be(config.Id);
        result.StartedAt.Should().BeLessThanOrEqualTo(DateTime.UtcNow);
    }

    [Fact]
    public async Task ExecuteSyncAsync_WithInvalidConfig_ThrowsConfigurationException()
    {
        // Arrange
        var invalidConfig = new SyncConfig("", "invalid-id", "/tmp");

        // Act & Assert
        await Assert.ThrowsAsync<ConfigurationException>(() => _syncService.ExecuteSyncAsync(invalidConfig));
    }

    [Fact]
    public async Task ExecuteSyncAsync_WithoutPreviousSyncTime_FetchesAllPages()
    {
        // Arrange
        var config = new SyncConfig("test-sync", "550e8400-e29b-41d4-a716-446655440000", "/tmp/tasks")
        {
            LastSyncAt = null
        };

        _mockTaskRepository.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Task>());
        _mockNotionApiService.Setup(a => a.FetchPagesAsync(It.IsAny<string>(), It.IsAny<int>()))
            .ReturnsAsync(new List<NotionPage>());
        _mockChangeDetectionService.Setup(s => s.DetectLocalChanges(It.IsAny<List<Task>>(), It.IsAny<DateTime>()))
            .Returns(new List<ChangeLog>());
        _mockChangeDetectionService.Setup(s => s.DetectNotionChanges(It.IsAny<List<NotionPage>>(), It.IsAny<DateTime>()))
            .Returns(new List<ChangeLog>());
        _mockChangeDetectionService.Setup(s => s.DetectConflicts(It.IsAny<List<ChangeLog>>(), It.IsAny<List<ChangeLog>>()))
            .Returns(new List<ConflictResolution>());

        // Act
        await _syncService.ExecuteSyncAsync(config);

        // Assert
        _mockNotionApiService.Verify(a => a.FetchPagesAsync(config.NotionDatabaseId, 100), Times.Once);
        _mockNotionApiService.Verify(a => a.FetchPagesSinceAsync(It.IsAny<string>(), It.IsAny<DateTime>()), Times.Never);
    }

    [Fact]
    public async Task ExecuteSyncAsync_WithPreviousSyncTime_FetchesIncrementalPages()
    {
        // Arrange
        var lastSyncTime = DateTime.UtcNow.AddHours(-1);
        var config = new SyncConfig("test-sync", "550e8400-e29b-41d4-a716-446655440000", "/tmp/tasks")
        {
            LastSyncAt = lastSyncTime
        };

        _mockTaskRepository.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Task>());
        _mockNotionApiService.Setup(a => a.FetchPagesSinceAsync(It.IsAny<string>(), It.IsAny<DateTime>()))
            .ReturnsAsync(new List<NotionPage>());
        _mockChangeDetectionService.Setup(s => s.DetectLocalChanges(It.IsAny<List<Task>>(), It.IsAny<DateTime>()))
            .Returns(new List<ChangeLog>());
        _mockChangeDetectionService.Setup(s => s.DetectNotionChanges(It.IsAny<List<NotionPage>>(), It.IsAny<DateTime>()))
            .Returns(new List<ChangeLog>());
        _mockChangeDetectionService.Setup(s => s.DetectConflicts(It.IsAny<List<ChangeLog>>(), It.IsAny<List<ChangeLog>>()))
            .Returns(new List<ConflictResolution>());

        // Act
        await _syncService.ExecuteSyncAsync(config);

        // Assert
        _mockNotionApiService.Verify(a => a.FetchPagesSinceAsync(config.NotionDatabaseId, lastSyncTime), Times.Once);
        _mockNotionApiService.Verify(a => a.FetchPagesAsync(It.IsAny<string>(), It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task ExecuteSyncAsync_WhenConflictsDetected_ResolvesConflicts()
    {
        // Arrange
        var config = new SyncConfig("test-sync", "550e8400-e29b-41d4-a716-446655440000", "/tmp/tasks");
        var conflicts = new List<ConflictResolution>
        {
            new() { TaskId = Guid.NewGuid(), Status = ResolutionStatus.Pending }
        };

        _mockTaskRepository.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Task>());
        _mockNotionApiService.Setup(a => a.FetchPagesAsync(It.IsAny<string>(), It.IsAny<int>()))
            .ReturnsAsync(new List<NotionPage>());
        _mockChangeDetectionService.Setup(s => s.DetectLocalChanges(It.IsAny<List<Task>>(), It.IsAny<DateTime>()))
            .Returns(new List<ChangeLog>());
        _mockChangeDetectionService.Setup(s => s.DetectNotionChanges(It.IsAny<List<NotionPage>>(), It.IsAny<DateTime>()))
            .Returns(new List<ChangeLog>());
        _mockChangeDetectionService.Setup(s => s.DetectConflicts(It.IsAny<List<ChangeLog>>(), It.IsAny<List<ChangeLog>>()))
            .Returns(conflicts);
        _mockConflictResolutionService
            .Setup(s => s.ResolveConflictsAsync(It.IsAny<List<ConflictResolution>>(), It.IsAny<ConflictResolutionStrategy>(), It.IsAny<Dictionary<string, ConflictResolutionStrategy>?>()))
            .ReturnsAsync(new List<ConflictResolution>
            {
                new() { TaskId = Guid.NewGuid(), Status = ResolutionStatus.Resolved }
            });

        // Act
        var result = await _syncService.ExecuteSyncAsync(config);

        // Assert
        result.ConflictsDetected.Should().Be(1);
        result.ConflictsResolved.Should().Be(1);
        _mockConflictResolutionService.Verify(
            s => s.ResolveConflictsAsync(It.IsAny<List<ConflictResolution>>(), It.IsAny<ConflictResolutionStrategy>(), It.IsAny<Dictionary<string, ConflictResolutionStrategy>?>()),
            Times.Once);
    }

    [Fact]
    public async Task ExecuteSyncAsync_WhenNoChanges_ReturnsEmptyCounts()
    {
        // Arrange
        var config = new SyncConfig("test-sync", "550e8400-e29b-41d4-a716-446655440000", "/tmp/tasks");

        _mockTaskRepository.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Task>());
        _mockNotionApiService.Setup(a => a.FetchPagesAsync(It.IsAny<string>(), It.IsAny<int>()))
            .ReturnsAsync(new List<NotionPage>());
        _mockChangeDetectionService.Setup(s => s.DetectLocalChanges(It.IsAny<List<Task>>(), It.IsAny<DateTime>()))
            .Returns(new List<ChangeLog>());
        _mockChangeDetectionService.Setup(s => s.DetectNotionChanges(It.IsAny<List<NotionPage>>(), It.IsAny<DateTime>()))
            .Returns(new List<ChangeLog>());
        _mockChangeDetectionService.Setup(s => s.DetectConflicts(It.IsAny<List<ChangeLog>>(), It.IsAny<List<ChangeLog>>()))
            .Returns(new List<ConflictResolution>());

        // Act
        var result = await _syncService.ExecuteSyncAsync(config);

        // Assert
        result.LocalChangesDetected.Should().Be(0);
        result.NotionChangesDetected.Should().Be(0);
        result.ConflictsDetected.Should().Be(0);
        result.Created.Should().Be(0);
        result.Updated.Should().Be(0);
        result.Deleted.Should().Be(0);
    }

    [Fact]
    public async Task ExecuteSyncAsync_WhenExceptionOccurs_CatchesAndReturnsFailedStatus()
    {
        // Arrange
        var config = new SyncConfig("test-sync", "550e8400-e29b-41d4-a716-446655440000", "/tmp/tasks");
        var exception = new InvalidOperationException("API connection failed");

        _mockTaskRepository.Setup(r => r.GetAllAsync())
            .ThrowsAsync(exception);

        // Act
        var result = await _syncService.ExecuteSyncAsync(config);

        // Assert
        result.Status.Should().Be(SyncStatus.Failed);
        result.ErrorMessage.Should().Contain("API connection failed");
        result.CompletedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task ExecuteSyncAsync_UpdatesSyncTimestamp()
    {
        // Arrange
        var config = new SyncConfig("test-sync", "550e8400-e29b-41d4-a716-446655440000", "/tmp/tasks");
        var originalLastSync = config.LastSyncAt;

        _mockTaskRepository.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Task>());
        _mockNotionApiService.Setup(a => a.FetchPagesAsync(It.IsAny<string>(), It.IsAny<int>()))
            .ReturnsAsync(new List<NotionPage>());
        _mockChangeDetectionService.Setup(s => s.DetectLocalChanges(It.IsAny<List<Task>>(), It.IsAny<DateTime>()))
            .Returns(new List<ChangeLog>());
        _mockChangeDetectionService.Setup(s => s.DetectNotionChanges(It.IsAny<List<NotionPage>>(), It.IsAny<DateTime>()))
            .Returns(new List<ChangeLog>());
        _mockChangeDetectionService.Setup(s => s.DetectConflicts(It.IsAny<List<ChangeLog>>(), It.IsAny<List<ChangeLog>>()))
            .Returns(new List<ConflictResolution>());

        // Act
        await _syncService.ExecuteSyncAsync(config);

        // Assert
        config.LastSyncAt.Should().NotBe(originalLastSync);
        config.LastSyncAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task ExecuteSyncAsync_RecordsStartAndCompletionTimes()
    {
        // Arrange
        var beforeSync = DateTime.UtcNow;
        var config = new SyncConfig("test-sync", "550e8400-e29b-41d4-a716-446655440000", "/tmp/tasks");

        _mockTaskRepository.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Task>());
        _mockNotionApiService.Setup(a => a.FetchPagesAsync(It.IsAny<string>(), It.IsAny<int>()))
            .ReturnsAsync(new List<NotionPage>());
        _mockChangeDetectionService.Setup(s => s.DetectLocalChanges(It.IsAny<List<Task>>(), It.IsAny<DateTime>()))
            .Returns(new List<ChangeLog>());
        _mockChangeDetectionService.Setup(s => s.DetectNotionChanges(It.IsAny<List<NotionPage>>(), It.IsAny<DateTime>()))
            .Returns(new List<ChangeLog>());
        _mockChangeDetectionService.Setup(s => s.DetectConflicts(It.IsAny<List<ChangeLog>>(), It.IsAny<List<ChangeLog>>()))
            .Returns(new List<ConflictResolution>());

        // Act
        var result = await _syncService.ExecuteSyncAsync(config);
        var afterSync = DateTime.UtcNow;

        // Assert
        result.StartedAt.Should().BeGreaterThanOrEqualTo(beforeSync);
        result.CompletedAt.Should().BeGreaterThanOrEqualTo(result.StartedAt);
        result.CompletedAt.Should().BeLessThanOrEqualTo(afterSync);
    }

    [Fact]
    public async Task ExecuteSyncAsync_WithBidirectionalSync_AppliesChanges()
    {
        // Arrange
        var taskId = Guid.NewGuid();
        var pageId = "page123";
        var task = new Task
        {
            Id = taskId,
            Title = "Updated Task",
            CreatedAt = DateTime.UtcNow.AddHours(-1),
            UpdatedAt = DateTime.UtcNow
        };

        var config = new SyncConfig("test-sync", "550e8400-e29b-41d4-a716-446655440000", "/tmp/tasks")
        {
            Direction = SyncDirection.Bidirectional
        };

        _mockTaskRepository.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Task> { task });
        _mockNotionApiService.Setup(a => a.FetchPagesAsync(It.IsAny<string>(), It.IsAny<int>()))
            .ReturnsAsync(new List<NotionPage>());
        _mockChangeDetectionService.Setup(s => s.DetectLocalChanges(It.IsAny<List<Task>>(), It.IsAny<DateTime>()))
            .Returns(new List<ChangeLog>());
        _mockChangeDetectionService.Setup(s => s.DetectNotionChanges(It.IsAny<List<NotionPage>>(), It.IsAny<DateTime>()))
            .Returns(new List<ChangeLog>());
        _mockChangeDetectionService.Setup(s => s.DetectConflicts(It.IsAny<List<ChangeLog>>(), It.IsAny<List<ChangeLog>>()))
            .Returns(new List<ConflictResolution>());

        // Act
        var result = await _syncService.ExecuteSyncAsync(config);

        // Assert
        result.Status.Should().Be(SyncStatus.Completed);
        _mockTaskRepository.Verify(r => r.SaveAsync(), Times.Once);
    }

    [Fact]
    public void SyncResult_CalculatesDuration()
    {
        // Arrange
        var result = new SyncService.SyncResult
        {
            StartedAt = DateTime.UtcNow.AddSeconds(-5),
            CompletedAt = DateTime.UtcNow
        };

        // Act & Assert
        result.Duration.Should().NotBeNull();
        result.Duration?.TotalSeconds.Should().BeGreaterThanOrEqualTo(4);
        result.Duration?.TotalSeconds.Should().BeLessThan(10);
    }

    [Fact]
    public void SyncResult_GeneratesSummary()
    {
        // Arrange
        var result = new SyncService.SyncResult
        {
            Status = SyncStatus.Completed,
            StartedAt = DateTime.UtcNow.AddSeconds(-2),
            CompletedAt = DateTime.UtcNow,
            Created = 3,
            Updated = 2,
            Deleted = 1,
            Unchanged = 94,
            ConflictsDetected = 1
        };

        // Act
        var summary = result.Summary;

        // Assert
        summary.Should().Contain("3 created");
        summary.Should().Contain("2 updated");
        summary.Should().Contain("1 deleted");
        summary.Should().Contain("94 unchanged");
        summary.Should().Contain("1 conflicted");
    }
}
