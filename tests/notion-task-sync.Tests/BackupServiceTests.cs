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

public class BackupServiceTests : IDisposable
{
    private readonly string _backupDirectory;
    private readonly string _tasksDirectory;
    private readonly Mock<LocalFileService> _mockFileService;
    private readonly BackupService _backupService;

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

    [Fact]
    public void GetAvailableBackups_WithEmptyBackupDirectory_ReturnsEmptyList()
    {
        // Act
        var backups = _backupService.GetAvailableBackups();

        // Assert
        backups.Should().BeEmpty();
    }

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
