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
using System.Threading.Tasks;
using Task = System.Threading.Tasks.Task;
using DomainTask = NotionTaskSync.Domain.Models.Task;

/// <summary>
/// Unit tests for the <see cref="LocalFileService"/> class that verify file system operations for task persistence.
/// Tests cover saving, loading, and managing task files in the local file system.
/// </summary>
public class LocalFileServiceTests : IDisposable
{
    /// <summary>
    /// The temporary directory used for testing file operations.
    /// </summary>
    private readonly string _testDirectory;

    /// <summary>
    /// The <see cref="LocalFileService"/> instance under test.
    /// </summary>
    private readonly LocalFileService _fileService;

    /// <summary>
    /// Initializes a new instance of the <see cref="LocalFileServiceTests"/> class.
    /// Sets up a temporary directory for testing and creates a <see cref="LocalFileService"/> instance.
    /// </summary>
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

    /// <summary>
    /// Tests that the <see cref="LocalFileService"/> constructor throws an <see cref="ArgumentNullException"/> when provided with a null base path.
    /// </summary>
    [Fact]
    public void Constructor_WithNullBasePath_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new LocalFileService(null!));
    }

    /// <summary>
    /// Tests that the <see cref="LocalFileService"/> constructor throws an <see cref="ArgumentNullException"/> when provided with an empty base path.
    /// </summary>
    [Fact]
    public void Constructor_WithEmptyBasePath_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new LocalFileService(string.Empty));
    }

    /// <summary>
    /// Tests that <see cref="LocalFileService.SaveTaskAsync"/> creates a markdown file with the task's title as the filename and contains the task's title and description.
    /// </summary>
    [Fact]
    public async Task SaveTaskAsync_WithValidTask_CreatesFileWithTaskContent()
    {
        // Arrange
        var task = new DomainTask
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

    /// <summary>
    /// Tests that <see cref="LocalFileService.SaveTaskAsync"/> creates separate files for multiple tasks with different titles.
    /// </summary>
    [Fact]
    public async Task SaveTaskAsync_WithMultipleTasks_CreatesSeparateFiles()
    {
        // Arrange
        var task1 = new DomainTask
        {
            Id = Guid.NewGuid(),
            Title = "Task One",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var task2 = new DomainTask
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

    /// <summary>
    /// Tests that <see cref="LocalFileService.SaveTaskAsync"/> throws a <see cref="ValidationException"/> when the task has invalid properties (empty ID or empty title).
    /// </summary>
    [Fact]
    public async Task SaveTaskAsync_WithInvalidTask_ThrowsValidationException()
    {
        // Arrange
        var invalidTask = new DomainTask
        {
            Id = Guid.Empty, // Invalid: empty ID
            Title = string.Empty, // Invalid: empty title
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(() => _fileService.SaveTaskAsync(invalidTask));
    }

    /// <summary>
    /// Tests that <see cref="LocalFileService.SaveTaskAsync"/> sanitizes the task title to create a valid filename by removing special characters like slashes, backslashes, and colons.
    /// </summary>
    [Fact]
    public async Task SaveTaskAsync_WithSpecialCharactersInTitle_SanitizesFileName()
    {
        // Arrange
        var task = new DomainTask
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

    /// <summary>
    /// Tests that <see cref="LocalFileService.LoadTaskAsync"/> successfully loads a task from a file when given a valid file path.
    /// </summary>
    [Fact]
    public async Task LoadTaskAsync_WithValidFilePath_ReturnsTask()
    {
        // Arrange
        var originalTask = new DomainTask
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

    /// <summary>
    /// Tests that <see cref="LocalFileService.LoadTaskAsync"/> returns null when attempting to load a task from a non-existent file path.
    /// </summary>
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

    /// <summary>
    /// Tests that <see cref="LocalFileService.LoadTaskAsync"/> returns null when provided with a null file path.
    /// </summary>
    [Fact]
    public async Task LoadTaskAsync_WithNullFilePath_ReturnsNull()
    {
        // Act
        var result = await _fileService.LoadTaskAsync(null!);

        // Assert
        result.Should().BeNull();
    }

    /// <summary>
    /// Tests that <see cref="LocalFileService.LoadTaskAsync"/> returns null when provided with an empty file path.
    /// </summary>
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
            new DomainTask
            {
                Id = Guid.NewGuid(),
                Title = "Task 1",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new DomainTask
            {
                Id = Guid.NewGuid(),
                Title = "Task 2",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new DomainTask
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

    /// <summary>
    /// Tests that <see cref="LocalFileService.LoadAllTasksAsync"/> returns an empty list when the directory contains no task files.
    /// </summary>
    [Fact]
    public async Task LoadAllTasksAsync_WithEmptyDirectory_ReturnsEmptyList()
    {
        // Act
        var result = await _fileService.LoadAllTasksAsync();

        // Assert
        result.Should().BeEmpty();
    }

    /// <summary>
    /// Tests that <see cref="LocalFileService.LoadAllTasksAsync"/> returns an empty list when attempting to load tasks from a non-existent directory.
    /// </summary>
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

    /// <summary>
    /// Tests that <see cref="LocalFileService.SaveTaskAsync"/> overwrites an existing file when saving a task with the same title as a previously saved task.
    /// </summary>
    [Fact]
    public async Task SaveTaskAsync_WithSameTitle_OverwritesExistingFile()
    {
        // Arrange
        var task1 = new DomainTask
        {
            Id = Guid.NewGuid(),
            Title = "Same Title",
            Description = "Version 1",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var task2 = new DomainTask
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

    /// <summary>
    /// Tests that <see cref="LocalFileService.SaveTaskAsync"/> updates the task's LocalFilePath property with the full path to the created markdown file.
    /// </summary>
    [Fact]
    public async Task SaveTaskAsync_UpdatesLocalFilePathProperty()
    {
        // Arrange
        var task = new DomainTask
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

    /// <summary>
    /// Tests that <see cref="LocalFileService.LoadTaskAsync"/> throws a <see cref="LocalFileException"/> when attempting to load a file with invalid markdown format that cannot be parsed.
    /// </summary>
    [Fact]
    public async Task LoadTaskAsync_WithInvalidMarkdownFormat_ThrowsLocalFileException()
    {
        // Arrange
        var filePath = Path.Combine(_testDirectory, "invalid.md");
        await File.WriteAllTextAsync(filePath, "invalid content that can't be parsed");

        // Act & Assert
        await Assert.ThrowsAsync<LocalFileException>(() => _fileService.LoadTaskAsync(filePath));
    }

    /// <summary>
    /// Tests that <see cref="LocalFileService.SaveTaskAsync"/> creates the base directory if it does not exist before saving a task.
    /// </summary>
    [Fact]
    public async Task SaveTaskAsync_WhenDirectoryDoesNotExist_CreatesDirectory()
    {
        // Arrange
        var nonExistentPath = Path.Combine(Path.GetTempPath(), $"new_dir_{Guid.NewGuid()}");
        var service = new LocalFileService(nonExistentPath);

        var task = new DomainTask
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
