#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

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
using System.Threading.Tasks;
using Task = System.Threading.Tasks.Task;
using DomainTask = NotionTaskSync.Domain.Models.Task;

/// <summary>
/// Contains unit tests for the <see cref="SyncService"/> class.
/// Tests various scenarios including valid configurations, error handling, conflict resolution,
/// and synchronization direction behaviors.
/// </summary>
public class SyncServiceTests
{
	/// <summary>
	/// Mock for the change detection service used to identify differences between local and Notion data.
	/// </summary>
	private readonly Mock<ChangeDetectionService> _mockChangeDetectionService;

	/// <summary>
	/// Mock for the conflict resolution service used to handle synchronization conflicts.
	/// </summary>
	private readonly Mock<ConflictResolutionService> _mockConflictResolutionService;

	/// <summary>
	/// Mock for the Notion API service used to interact with Notion database.
	/// </summary>
	private readonly Mock<NotionApiService> _mockNotionApiService;

	/// <summary>
	/// Mock for the task repository used to access local task storage.
	/// </summary>
	private readonly Mock<ITaskRepository> _mockTaskRepository;

	/// <summary>
	/// Mock for the change log repository used to track synchronization history.
	/// </summary>
	private readonly Mock<IChangeLogRepository> _mockChangeLogRepository;

	/// <summary>
	/// Mock for the logger used to record synchronization events.
	/// </summary>
	private readonly Mock<ILogger<SyncService>> _mockLogger;

	/// <summary>
	/// Instance of the sync service under test with all dependencies mocked.
	/// </summary>
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

	/// <summary>
	/// Tests that ExecuteSyncAsync returns completed status when provided with a valid configuration.
	/// Verifies that the sync service successfully completes and returns the correct configuration ID and timestamps.
	/// </summary>
	[Fact]
	public async Task ExecuteSyncAsync_WithValidConfig_ReturnsCompletedStatus()
	{
		// Arrange
		var config = new SyncConfig("test-sync", "550e8400-e29b-41d4-a716-446655440000", "/tmp/tasks")
		{
			Direction = SyncDirection.Bidirectional
		};

		_mockTaskRepository.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<DomainTask>());
		_mockNotionApiService.Setup(a => a.FetchPagesAsync(It.IsAny<string>(), It.IsAny<int>()))
			.ReturnsAsync(new List<NotionPage>());
		_mockChangeDetectionService.Setup(s => s.DetectLocalChanges(It.IsAny<List<DomainTask>>(), It.IsAny<DateTime>()))
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
		result.StartedAt.Should().BeOnOrBefore(DateTime.UtcNow);
	}

	/// <summary>
	/// Tests that ExecuteSyncAsync throws ConfigurationException when provided with an invalid configuration.
	/// Verifies that the sync service properly validates configuration parameters.
	/// </summary>
	[Fact]
	public async Task ExecuteSyncAsync_WithInvalidConfig_ThrowsConfigurationException()
	{
		// Arrange
		var invalidConfig = new SyncConfig("", "invalid-id", "/tmp");

		// Act & Assert
		await Assert.ThrowsAsync<ConfigurationException>(() => _syncService.ExecuteSyncAsync(invalidConfig));
	}

	/// <summary>
	/// Tests that ExecuteSyncAsync fetches all pages when there is no previous sync time.
	/// Verifies that the sync service performs a full sync when LastSyncAt is null.
	/// </summary>
	[Fact]
	public async Task ExecuteSyncAsync_WithoutPreviousSyncTime_FetchesAllPages()
	{
		// Arrange
		var config = new SyncConfig("test-sync", "550e8400-e29b-41d4-a716-446655440000", "/tmp/tasks")
		{
			LastSyncAt = null
		};

		_mockTaskRepository.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<DomainTask>());
		_mockNotionApiService.Setup(a => a.FetchPagesAsync(It.IsAny<string>(), It.IsAny<int>()))
			.ReturnsAsync(new List<NotionPage>());
		_mockChangeDetectionService.Setup(s => s.DetectLocalChanges(It.IsAny<List<DomainTask>>(), It.IsAny<DateTime>()))
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

	/// <summary>
	/// Tests that ExecuteSyncAsync fetches incremental pages when there is a previous sync time.
	/// Verifies that the sync service performs an incremental sync when LastSyncAt has a value.
	/// </summary>
	[Fact]
	public async Task ExecuteSyncAsync_WithPreviousSyncTime_FetchesIncrementalPages()
	{
		// Arrange
		var lastSyncTime = DateTime.UtcNow.AddHours(-1);
		var config = new SyncConfig("test-sync", "550e8400-e29b-41d4-a716-446655440000", "/tmp/tasks")
		{
			LastSyncAt = lastSyncTime
		};

		_mockTaskRepository.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<DomainTask>());
		_mockNotionApiService.Setup(a => a.FetchPagesSinceAsync(It.IsAny<string>(), It.IsAny<DateTime>()))
			.ReturnsAsync(new List<NotionPage>());
		_mockChangeDetectionService.Setup(s => s.DetectLocalChanges(It.IsAny<List<DomainTask>>(), It.IsAny<DateTime>()))
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

	/// <summary>
	/// Tests that ExecuteSyncAsync properly resolves conflicts when they are detected.
	/// Verifies that the sync service detects conflicts and uses the conflict resolution service.
	/// <param name="config">The synchronization configuration.</param>
	/// <returns>The sync result with conflict information.</returns>
	/// </summary>
	[Fact]
	public async Task ExecuteSyncAsync_WhenConflictsDetected_ResolvesConflicts()
	{
		// Arrange
		var config = new SyncConfig("test-sync", "550e8400-e29b-41d4-a716-446655440000", "/tmp/tasks");
		var conflicts = new List<ConflictResolution>
		{
			new() { TaskId = Guid.NewGuid(), Status = ResolutionStatus.Pending }
		};

		_mockTaskRepository.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<DomainTask>());
		_mockNotionApiService.Setup(a => a.FetchPagesAsync(It.IsAny<string>(), It.IsAny<int>()))
			.ReturnsAsync(new List<NotionPage>());
		_mockChangeDetectionService.Setup(s => s.DetectLocalChanges(It.IsAny<List<DomainTask>>(), It.IsAny<DateTime>()))
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

	/// <summary>
	/// Tests that ExecuteSyncAsync returns zero counts when no changes are detected.
	/// Verifies that the sync service correctly reports when there are no changes to sync.
	/// </summary>
	[Fact]
	public async Task ExecuteSyncAsync_WhenNoChanges_ReturnsEmptyCounts()
	{
		// Arrange
		var config = new SyncConfig("test-sync", "550e8400-e29b-41d4-a716-446655440000", "/tmp/tasks");

		_mockTaskRepository.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<DomainTask>());
		_mockNotionApiService.Setup(a => a.FetchPagesAsync(It.IsAny<string>(), It.IsAny<int>()))
			.ReturnsAsync(new List<NotionPage>());
		_mockChangeDetectionService.Setup(s => s.DetectLocalChanges(It.IsAny<List<DomainTask>>(), It.IsAny<DateTime>()))
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

	/// <summary>
	/// Tests that ExecuteSyncAsync catches exceptions and returns failed status.
	/// Verifies that the sync service properly handles exceptions and returns appropriate error information.
	/// <param name="config">The synchronization configuration.</param>
	/// <param name="exception">The exception to throw during sync.</param>
	/// </summary>
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

	/// <summary>
	/// Tests that ExecuteSyncAsync updates the sync timestamp after completion.
	/// Verifies that the sync service updates the LastSyncAt property of the configuration.
	/// <param name="config">The synchronization configuration.</param>
	/// </summary>
	[Fact]
	public async Task ExecuteSyncAsync_UpdatesSyncTimestamp()
	{
		// Arrange
		var config = new SyncConfig("test-sync", "550e8400-e29b-41d4-a716-446655440000", "/tmp/tasks");
		var originalLastSync = config.LastSyncAt;

		_mockTaskRepository.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<DomainTask>());
		_mockNotionApiService.Setup(a => a.FetchPagesAsync(It.IsAny<string>(), It.IsAny<int>()))
			.ReturnsAsync(new List<NotionPage>());
		_mockChangeDetectionService.Setup(s => s.DetectLocalChanges(It.IsAny<List<DomainTask>>(), It.IsAny<DateTime>()))
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

	/// <summary>
	/// Tests that ExecuteSyncAsync records start and completion times.
	/// Verifies that the sync service properly tracks synchronization timing information.
	/// <param name="beforeSync">The time before sync execution.</param>
	/// <param name="config">The synchronization configuration.</param>
	/// </summary>
	[Fact]
	public async Task ExecuteSyncAsync_RecordsStartAndCompletionTimes()
	{
		// Arrange
		var beforeSync = DateTime.UtcNow;
		var config = new SyncConfig("test-sync", "550e8400-e29b-41d4-a716-446655440000", "/tmp/tasks");

		_mockTaskRepository.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<DomainTask>());
		_mockNotionApiService.Setup(a => a.FetchPagesAsync(It.IsAny<string>(), It.IsAny<int>()))
			.ReturnsAsync(new List<NotionPage>());
		_mockChangeDetectionService.Setup(s => s.DetectLocalChanges(It.IsAny<List<DomainTask>>(), It.IsAny<DateTime>()))
			.Returns(new List<ChangeLog>());
		_mockChangeDetectionService.Setup(s => s.DetectNotionChanges(It.IsAny<List<NotionPage>>(), It.IsAny<DateTime>()))
			.Returns(new List<ChangeLog>());
		_mockChangeDetectionService.Setup(s => s.DetectConflicts(It.IsAny<List<ChangeLog>>(), It.IsAny<List<ChangeLog>>()))
			.Returns(new List<ConflictResolution>());

		// Act
		var result = await _syncService.ExecuteSyncAsync(config);
		var afterSync = DateTime.UtcNow;

		// Assert
		result.StartedAt.Should().BeOnOrAfter(beforeSync);
		result.CompletedAt.Should().BeOnOrAfter(result.StartedAt);
		result.CompletedAt.Should().BeOnOrBefore(afterSync);
	}

	/// <summary>
	/// Tests that ExecuteSyncAsync applies changes when using bidirectional sync direction.
	/// Verifies that the sync service saves changes to the repository when sync direction is bidirectional.
	/// <param name="taskId">The ID of the task to sync.</param>
	/// <param name="pageId">The ID of the Notion page.</param>
	/// <param name="task">The domain task to sync.</param>
	/// <param name="config">The synchronization configuration with bidirectional direction.</param>
	/// </summary>
	[Fact]
	public async Task ExecuteSyncAsync_WithBidirectionalSync_AppliesChanges()
	{
		// Arrange
		var taskId = Guid.NewGuid();
		var pageId = "page123";
		var task = new DomainTask
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

		_mockTaskRepository.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<DomainTask> { task });
		_mockNotionApiService.Setup(a => a.FetchPagesAsync(It.IsAny<string>(), It.IsAny<int>()))
			.ReturnsAsync(new List<NotionPage>());
		_mockChangeDetectionService.Setup(s => s.DetectLocalChanges(It.IsAny<List<DomainTask>>(), It.IsAny<DateTime>()))
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

	/// <summary>
	/// Tests that SyncResult calculates duration correctly.
	/// Verifies that the duration property returns appropriate time span between start and completion.
	/// <param name="result">The sync result with start and completion times.</param>
	/// </summary>
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

	/// <summary>
	/// Tests that SyncResult generates a summary string with correct counts.
	/// Verifies that the summary property returns a properly formatted string with operation counts.
	/// <param name="result">The sync result with various operation counts.</param>
	/// </summary>
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