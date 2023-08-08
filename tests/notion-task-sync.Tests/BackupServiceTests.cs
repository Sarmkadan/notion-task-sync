#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace NotionTaskSync.Tests;

using NotionTaskSync.Services;
using NotionTaskSync.Domain.Models;
using FluentAssertions;
using Moq;
using Xunit;
using System.Threading.Tasks;
using Task = System.Threading.Tasks.Task;

/// <summary>
/// Contains unit tests for the <see cref="BackupService"/> class.
/// Tests various scenarios for backup creation, retrieval, and error handling.
/// </summary>
public class BackupServiceTests : IDisposable
{
    /// <summary>
    /// Temporary directory path used for storing backup files during tests.
    /// </summary>
    private readonly string _backupDirectory;

    /// <summary>
    /// Temporary directory path used for storing task files during tests.
    /// </summary>
    private readonly string _tasksDirectory;

    /// <summary>
    /// Mock implementation of <see cref="LocalFileService"/> for testing backup operations.
    /// </summary>
    private readonly Mock<LocalFileService> _mockFileService;

    /// <summary>
    /// Instance of <see cref="BackupService"/> being tested.
    /// </summary>
    private readonly BackupService _backupService;

    /// <summary>
    /// Initializes a new instance of the <see cref="BackupServiceTests"/> class.
    /// Sets up temporary directories and mock services for testing backup functionality.
    /// </summary>
    public BackupServiceTests()
    {
        _backupDirectory = Path.Combine(Path.GetTempPath(), $"backup_{Guid.NewGuid()}");
        _tasksDirectory = Path.Combine(Path.GetTempPath(), $"tasks_{Guid.NewGuid()}");
        Directory.CreateDirectory(_backupDirectory);
        Directory.CreateDirectory(_tasksDirectory);

        _mockFileService = new Mock<LocalFileService>(_tasksDirectory);
        _backupService = new BackupService(_backupDirectory, 5, _mockFileService.Object);
    }

    public void Dispose()
    {
        if (Directory.Exists(_backupDirectory))
            Directory.Delete(_backupDirectory, recursive: true);
        if (Directory.Exists(_tasksDirectory))
            Directory.Delete(_tasksDirectory, recursive: true);
    }

    /// <summary>
    /// Tests that <see cref="BackupService.CreateBackupAsync()"/> creates a backup directory with valid input.
    /// Verifies that the returned backup object is not null and contains expected properties.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [Fact]
    public async Task CreateBackupAsync_WithValidInput_CreatesBackupDirectory()
    {
        // Arrange
        _mockFileService
            .Setup(x => x.BackupTasksAsync(It.IsAny<string>()))
            .ReturnsAsync(Path.Combine(_backupDirectory, $"backup_{DateTime.UtcNow:yyyyMMdd_HHmmss}_auto"));

        // Act
        var backup = await _backupService.CreateBackupAsync();

        // Assert
        backup.Should().NotBeNull();
        backup.Id.Should().NotBeEmpty();
        backup.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    /// <summary>
    /// Tests that <see cref="BackupService.CreateBackupAsync(string)"/> includes the provided label in the backup information.
    /// Verifies that the backup object contains the correct label value.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [Fact]
    public async Task CreateBackupAsync_WithLabel_IncludesLabelInBackupInfo()
    {
        // Arrange
        var label = "before-migration";
        _mockFileService
            .Setup(x => x.BackupTasksAsync(It.IsAny<string>()))
            .ReturnsAsync(Path.Combine(_backupDirectory, $"backup_{DateTime.UtcNow:yyyyMMdd_HHmmss}_{label}"));

        // Act
        var backup = await _backupService.CreateBackupAsync(label);

        // Assert
        backup.Label.Should().Be(label);
    }

    /// <summary>
    /// Tests that <see cref="BackupService.CreateBackupAsync()"/> uses "auto" as the default label when no label is provided.
    /// Verifies that the backup object contains the default "auto" label.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [Fact]
    public async Task CreateBackupAsync_WithoutLabel_UsesDefaultAutoLabel()
    {
        // Arrange
        _mockFileService
            .Setup(x => x.BackupTasksAsync(It.IsAny<string>()))
            .ReturnsAsync(Path.Combine(_backupDirectory, $"backup_{DateTime.UtcNow:yyyyMMdd_HHmmss}_auto"));

        // Act
        var backup = await _backupService.CreateBackupAsync();

        // Assert
        backup.Label.Should().Be("auto");
    }

    /// <summary>
    /// Tests that <see cref="BackupService.GetAvailableBackups()"/> returns all available backups when multiple backups exist.
    /// Verifies that the returned list contains all created backups and is ordered by creation date in descending order.
    /// </summary>
    [Fact]
    public void GetAvailableBackups_WithMultipleBackups_ReturnsAllBackups()
    {
        // Arrange
        var backup1Dir = Path.Combine(_backupDirectory, "backup_20240101_120000_test1");
        var backup2Dir = Path.Combine(_backupDirectory, "backup_20240102_120000_test2");
        var backup3Dir = Path.Combine(_backupDirectory, "backup_20240103_120000_test3");

        Directory.CreateDirectory(backup1Dir);
        Directory.CreateDirectory(backup2Dir);
        Directory.CreateDirectory(backup3Dir);

        File.WriteAllText(Path.Combine(backup1Dir, "task1.md"), "content");
        File.WriteAllText(Path.Combine(backup2Dir, "task2.md"), "content");
        File.WriteAllText(Path.Combine(backup3Dir, "task3.md"), "content");

        // Act
        var backups = _backupService.GetAvailableBackups();

        // Assert
        backups.Should().HaveCount(3);
        backups.Should().BeInDescendingOrder(b => b.CreatedAt);
    }

    /// <summary>
    /// Tests that <see cref="BackupService.GetAvailableBackups()"/> returns an empty list when the backup directory is empty.
    /// Verifies that no backups are returned when no backup directories exist.
    /// </summary>
    [Fact]
    public void GetAvailableBackups_WithEmptyBackupDirectory_ReturnsEmptyList()
    {
        // Act
        var backups = _backupService.GetAvailableBackups();

        // Assert
        backups.Should().BeEmpty();
    }

    /// <summary>
    /// Tests that <see cref="BackupService.GetAvailableBackups()"/> includes the correct file count for each backup.
    /// Verifies that each backup object in the returned list contains the accurate count of files in its directory.
    /// </summary>
    [Fact]
    public void GetAvailableBackups_IncludesFileCountForEachBackup()
    {
        // Arrange
        var backupDir = Path.Combine(_backupDirectory, "backup_20240101_120000_test");
        Directory.CreateDirectory(backupDir);

        File.WriteAllText(Path.Combine(backupDir, "task1.md"), "content1");
        File.WriteAllText(Path.Combine(backupDir, "task2.md"), "content2");
        File.WriteAllText(Path.Combine(backupDir, "task3.md"), "content3");

        // Act
        var backups = _backupService.GetAvailableBackups();

        // Assert
        backups.Should().HaveCount(1);
        backups[0].FileCount.Should().Be(3);
    }

    /// <summary>
    /// Tests that <see cref="BackupService.GetAvailableBackups()"/> returns an empty list when the backup directory does not exist.
    /// Verifies that the service handles non-existent directories gracefully without throwing exceptions.
    /// </summary>
    [Fact]
    public void GetAvailableBackups_WithNonExistentBackupDirectory_ReturnsEmptyList()
    {
        // Arrange
        var nonExistentPath = Path.Combine(Path.GetTempPath(), $"nonexistent_{Guid.NewGuid()}");
        var service = new BackupService(nonExistentPath, 5, _mockFileService.Object);

        // Act
        var backups = service.GetAvailableBackups();

        // Assert
        backups.Should().BeEmpty();
    }

    /// <summary>
    /// Tests that <see cref="BackupService.CreateBackupAsync()"/> invokes the backup tasks method on the file service.
    /// Verifies that the <see cref="LocalFileService.BackupTasksAsync(string)"/> method is called exactly once during backup creation.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [Fact]
    public async Task CreateBackupAsync_InvokesBackupTasksOnFileService()
    {
        // Arrange
        _mockFileService
            .Setup(x => x.BackupTasksAsync(It.IsAny<string>()))
            .ReturnsAsync(Path.Combine(_backupDirectory, "backup_test"));

        // Act
        await _backupService.CreateBackupAsync("test-label");

        // Assert
        _mockFileService.Verify(
            x => x.BackupTasksAsync(It.IsAny<string>()),
            Times.Once);
    }

    /// <summary>
    /// Tests that <see cref="BackupService.CreateBackupAsync()"/> throws a <see cref="SyncException"/> when the file service throws an exception.
    /// Verifies that IO exceptions from the file service are properly wrapped and rethrown as <see cref="SyncException"/>.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [Fact]
    public async Task CreateBackupAsync_WithFileServiceThrowingException_ThrowsSyncException()
    {
        // Arrange
        _mockFileService
            .Setup(x => x.BackupTasksAsync(It.IsAny<string>()))
            .ThrowsAsync(new IOException("Disk full"));

        // Act & Assert
        await Assert.ThrowsAsync<NotionTaskSync.Domain.Exceptions.SyncException>(
            () => _backupService.CreateBackupAsync());
    }

    /// <summary>
    /// Tests that <see cref="BackupService.CreateBackupAsync()"/> creates a backup with the correct timestamp.
    /// Verifies that the backup's CreatedAt property falls within the expected time range around the method call.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [Fact]
    public async Task CreateBackupAsync_CreatesBackupWithCorrectTimestamp()
    {
        // Arrange
        var beforeCall = DateTime.UtcNow;
        _mockFileService
            .Setup(x => x.BackupTasksAsync(It.IsAny<string>()))
            .ReturnsAsync(Path.Combine(_backupDirectory, $"backup_{beforeCall:yyyyMMdd_HHmmss}_auto"));

        // Act
        var backup = await _backupService.CreateBackupAsync();
        var afterCall = DateTime.UtcNow;

        // Assert
        backup.CreatedAt.Should().BeOnOrAfter(beforeCall);
        backup.CreatedAt.Should().BeOnOrBefore(afterCall.AddSeconds(1));
    }

    /// <summary>
    /// Tests that <see cref="BackupService.GetAvailableBackups()"/> orders backups by creation date in descending order.
    /// Verifies that the most recently created backups appear first in the returned list.
    /// </summary>
    [Fact]
    public void GetAvailableBackups_OrdersByCreationDateDescending()
    {
        // Arrange - create backups with known timestamps
        var oldBackup = Path.Combine(_backupDirectory, "backup_20240101_120000_old");
        var newBackup = Path.Combine(_backupDirectory, "backup_20240103_120000_new");
        var middleBackup = Path.Combine(_backupDirectory, "backup_20240102_120000_middle");

        Directory.CreateDirectory(oldBackup);
        Directory.CreateDirectory(newBackup);
        Directory.CreateDirectory(middleBackup);

        File.WriteAllText(Path.Combine(oldBackup, "file.md"), "old");
        File.WriteAllText(Path.Combine(newBackup, "file.md"), "new");
        File.WriteAllText(Path.Combine(middleBackup, "file.md"), "middle");

        // Set creation times to ensure proper ordering
        var oldTime = DateTime.UtcNow.AddHours(-2);
        var middleTime = DateTime.UtcNow.AddHours(-1);
        var newTime = DateTime.UtcNow;

        Directory.SetCreationTimeUtc(oldBackup, oldTime);
        Directory.SetCreationTimeUtc(middleBackup, middleTime);
        Directory.SetCreationTimeUtc(newBackup, newTime);

        // Act
        var backups = _backupService.GetAvailableBackups();

        // Assert
        backups.Should().HaveCount(3);
        backups[0].CreatedAt.Should().BeAfter(backups[1].CreatedAt);
        backups[1].CreatedAt.Should().BeAfter(backups[2].CreatedAt);
    }

    /// <summary>
    /// Tests that <see cref="BackupService.CreateBackupAsync()"/> populates backup information with correct data.
    /// Verifies that the returned backup object contains accurate Id, Path, CreatedAt, Label, and FileCount properties.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [Fact]
    public async Task CreateBackupAsync_PopulatesBackupInfoWithCorrectData()
    {
        // Arrange
        var expectedBackupPath = Path.Combine(_backupDirectory, "backup_test_path");
        _mockFileService
            .Setup(x => x.BackupTasksAsync(It.IsAny<string>()))
            .ReturnsAsync(expectedBackupPath);

        // Create some test files in the backup directory
        Directory.CreateDirectory(expectedBackupPath);
        File.WriteAllText(Path.Combine(expectedBackupPath, "task1.md"), "content1");
        File.WriteAllText(Path.Combine(expectedBackupPath, "task2.md"), "content2");

        // Act
        var backup = await _backupService.CreateBackupAsync("migration");

        // Assert
        backup.Id.Should().NotBeEmpty();
        backup.Path.Should().Be(expectedBackupPath);
        backup.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        backup.Label.Should().Be("migration");
        backup.FileCount.Should().Be(2);
    }
}
