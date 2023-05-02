#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace NotionTaskSync.Tests;

using NotionTaskSync.Domain.Models;
using NotionTaskSync.Services;
using NotionTaskSync.Domain.Exceptions;
using FluentAssertions;
using Xunit;

public class LocalFileServiceTests : IDisposable
{
    private readonly string _testDirectory;
    private readonly LocalFileService _fileService;

    public LocalFileServiceTests()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), $"test_sync_{Guid.NewGuid()}");
        Directory.CreateDirectory(_testDirectory);
        _fileService = new LocalFileService(_testDirectory);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDirectory))
            Directory.Delete(_testDirectory, recursive: true);
    }

    [Fact]
    public void Constructor_WithNullBasePath_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new LocalFileService(null!));
    }

    [Fact]
    public void Constructor_WithEmptyBasePath_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new LocalFileService(string.Empty));
    }

    [Fact]
    public async Task SaveTaskAsync_WithValidTask_CreatesFileWithTaskContent()
    {
        // Arrange
        var task = new Task
        {
            Id = Guid.NewGuid(),
            Title = "Test Task",
            Description = "Test Description",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Act
        await _fileService.SaveTaskAsync(task);

        // Assert
        var expectedFileName = Path.Combine(_testDirectory, $"{task.Title}.md");
        File.Exists(expectedFileName).Should().BeTrue();

        var content = await File.ReadAllTextAsync(expectedFileName);
        content.Should().Contain("Test Task");
        content.Should().Contain("Test Description");
    }

    [Fact]
    public async Task SaveTaskAsync_WithMultipleTasks_CreatesSeparateFiles()
    {
        // Arrange
        var task1 = new Task
        {
            Id = Guid.NewGuid(),
            Title = "Task One",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var task2 = new Task
        {
            Id = Guid.NewGuid(),
            Title = "Task Two",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Act
        await _fileService.SaveTaskAsync(task1);
        await _fileService.SaveTaskAsync(task2);

        // Assert
        var files = Directory.GetFiles(_testDirectory);
        files.Should().HaveCount(2);
    }

    [Fact]
    public async Task SaveTaskAsync_WithInvalidTask_ThrowsValidationException()
    {
        // Arrange
        var invalidTask = new Task
        {
            Id = Guid.Empty, // Invalid: empty ID
            Title = string.Empty, // Invalid: empty title
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(() => _fileService.SaveTaskAsync(invalidTask));
    }

    [Fact]
    public async Task SaveTaskAsync_WithSpecialCharactersInTitle_SanitizesFileName()
    {
        // Arrange
        var task = new Task
        {
            Id = Guid.NewGuid(),
            Title = "Task / With \\ Special : Characters",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Act
        await _fileService.SaveTaskAsync(task);

        // Assert
        var files = Directory.GetFiles(_testDirectory);
        files.Should().HaveCount(1);
        var fileName = Path.GetFileName(files[0]);
        fileName.Should().NotContain("/");
        fileName.Should().NotContain("\\");
        fileName.Should().NotContain(":");
    }

    [Fact]
    public async Task LoadTaskAsync_WithValidFilePath_ReturnsTask()
    {
        // Arrange
        var originalTask = new Task
        {
            Id = Guid.NewGuid(),
            Title = "Load Test Task",
            Description = "Test loading",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _fileService.SaveTaskAsync(originalTask);
        var filePath = Path.Combine(_testDirectory, $"{originalTask.Title}.md");

        // Act
        var loadedTask = await _fileService.LoadTaskAsync(filePath);

        // Assert
        loadedTask.Should().NotBeNull();
        loadedTask!.Title.Should().Be("Load Test Task");
        loadedTask.Description.Should().Contain("Test loading");
    }

    [Fact]
    public async Task LoadTaskAsync_WithNonExistentFile_ReturnsNull()
    {
        // Arrange
        var nonExistentPath = Path.Combine(_testDirectory, "nonexistent.md");

        // Act
        var result = await _fileService.LoadTaskAsync(nonExistentPath);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task LoadTaskAsync_WithNullFilePath_ReturnsNull()
    {
        // Act
        var result = await _fileService.LoadTaskAsync(null!);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task LoadTaskAsync_WithEmptyFilePath_ReturnsNull()
    {
        // Act
        var result = await _fileService.LoadTaskAsync(string.Empty);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task LoadAllTasksAsync_WithMultipleTasks_ReturnsAllTasks()
    {
        // Arrange
        var tasks = new[]
        {
            new Task
            {
                Id = Guid.NewGuid(),
                Title = "Task 1",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new Task
            {
                Id = Guid.NewGuid(),
                Title = "Task 2",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new Task
            {
                Id = Guid.NewGuid(),
                Title = "Task 3",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }
        };

        foreach (var task in tasks)
            await _fileService.SaveTaskAsync(task);

        // Act
        var loadedTasks = await _fileService.LoadAllTasksAsync();

        // Assert
        loadedTasks.Should().HaveCount(3);
        loadedTasks.Select(t => t.Title).Should().Contain(new[] { "Task 1", "Task 2", "Task 3" });
    }

    [Fact]
    public async Task LoadAllTasksAsync_WithEmptyDirectory_ReturnsEmptyList()
    {
        // Act
        var result = await _fileService.LoadAllTasksAsync();

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task LoadAllTasksAsync_WithNonExistentDirectory_ReturnsEmptyList()
    {
        // Arrange
        var nonExistentPath = Path.Combine(Path.GetTempPath(), $"nonexistent_{Guid.NewGuid()}");
        var service = new LocalFileService(nonExistentPath);

        // Act
        var result = await service.LoadAllTasksAsync();

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task SaveTaskAsync_WithSameTitle_OverwritesExistingFile()
    {
        // Arrange
        var task1 = new Task
        {
            Id = Guid.NewGuid(),
            Title = "Same Title",
            Description = "Version 1",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var task2 = new Task
        {
            Id = Guid.NewGuid(),
            Title = "Same Title",
            Description = "Version 2",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Act
        await _fileService.SaveTaskAsync(task1);
        await _fileService.SaveTaskAsync(task2);

        var files = Directory.GetFiles(_testDirectory);

        // Assert
        files.Should().HaveCount(1);
        var content = await File.ReadAllTextAsync(files[0]);
        content.Should().Contain("Version 2");
        content.Should().NotContain("Version 1");
    }

    [Fact]
    public async Task SaveTaskAsync_UpdatesLocalFilePathProperty()
    {
        // Arrange
        var task = new Task
        {
            Id = Guid.NewGuid(),
            Title = "Path Update Task",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        task.LocalFilePath.Should().BeNull();

        // Act
        await _fileService.SaveTaskAsync(task);

        // Assert
        task.LocalFilePath.Should().NotBeNull();
        task.LocalFilePath.Should().EndWith(".md");
        File.Exists(task.LocalFilePath!).Should().BeTrue();
    }

    [Fact]
    public async Task LoadTaskAsync_WithInvalidMarkdownFormat_ThrowsLocalFileException()
    {
        // Arrange
        var filePath = Path.Combine(_testDirectory, "invalid.md");
        await File.WriteAllTextAsync(filePath, "invalid content that can't be parsed");

        // Act & Assert
        await Assert.ThrowsAsync<LocalFileException>(() => _fileService.LoadTaskAsync(filePath));
    }

    [Fact]
    public async Task SaveTaskAsync_WhenDirectoryDoesNotExist_CreatesDirectory()
    {
        // Arrange
        var nonExistentPath = Path.Combine(Path.GetTempPath(), $"new_dir_{Guid.NewGuid()}");
        var service = new LocalFileService(nonExistentPath);

        var task = new Task
        {
            Id = Guid.NewGuid(),
            Title = "Test",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        try
        {
            // Act
            await service.SaveTaskAsync(task);

            // Assert
            Directory.Exists(nonExistentPath).Should().BeTrue();
            var files = Directory.GetFiles(nonExistentPath);
            files.Should().HaveCount(1);
        }
        finally
        {
            if (Directory.Exists(nonExistentPath))
                Directory.Delete(nonExistentPath, recursive: true);
        }
    }
}
