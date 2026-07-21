#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace NotionTaskSync.Tests;

using NotionTaskSync.Domain.Models;
using NotionTaskSync.Data.Repositories;
using NotionTaskSync.Services;
using FluentAssertions;
using Moq;
using Xunit;

/// <summary>
/// Tests for the ChangeDetectionService class.
/// </summary>
public class ChangeDetectionServiceTests
{
    private readonly Mock<IChangeLogRepository> _mockRepo;
    private readonly ChangeDetectionService _service;

    public ChangeDetectionServiceTests()
    {
        _mockRepo = new Mock<IChangeLogRepository>();
        _service = new ChangeDetectionService(_mockRepo.Object);
    }

    /// <summary>
    /// Tests the DetectLocalChanges method when a new task is created after the since timestamp.
    /// </summary>
    [Fact]
    public void DetectLocalChanges_NewTaskCreatedAfterSinceTimestamp_ReturnsCreatedChangeLog()
    {
        // Arrange
        var since = DateTime.UtcNow.AddHours(-1);
        var createdAt = DateTime.UtcNow.AddMinutes(-30);

        var tasks = new List<Task>
        {
            new Task
            {
                Id = Guid.NewGuid(),
                Title = "New Sync Task",
                CreatedAt = createdAt,
                UpdatedAt = createdAt
            }
        };

        // Act
        var changes = _service.DetectLocalChanges(tasks, since);

        // Assert
        changes.Should().HaveCount(1);
        changes[0].ChangeType.Should().Be("Created");
        changes[0].Source.Should().Be(ChangeSource.Local);
    }

    /// <summary>
    /// Tests the DetectLocalChanges method when a task is modified after the since timestamp.
    /// </summary>
    [Fact]
    public void DetectLocalChanges_ModifiedTaskAfterSinceTimestamp_ReturnsUpdatedChangeLog()
    {
        // Arrange
        var since = DateTime.UtcNow.AddHours(-1);
        var createdAt = DateTime.UtcNow.AddDays(-2);
        var updatedAt = DateTime.UtcNow.AddMinutes(-15);

        var tasks = new List<Task>
        {
            new Task
            {
                Id = Guid.NewGuid(),
                Title = "Updated Sync Task",
                CreatedAt = createdAt,
                UpdatedAt = updatedAt
            }
        };

        // Act
        var changes = _service.DetectLocalChanges(tasks, since);

        // Assert
        changes.Should().HaveCount(1);
        changes[0].ChangeType.Should().Be("Updated");
        changes[0].Source.Should().Be(ChangeSource.Local);
    }

    /// <summary>
    /// Tests the DetectLocalChanges method when identical items produce no changes.
    /// </summary>
    [Fact]
    public void DetectLocalChanges_IdenticalItemsSinceTimestamp_ReturnsNoChanges()
    {
        // Arrange
        var since = DateTime.UtcNow.AddHours(-1);
        var createdAt = DateTime.UtcNow.AddDays(-2);
        var updatedAt = DateTime.UtcNow.AddDays(-2); // Same as created, no changes since 'since'

        var tasks = new List<Task>
        {
            new Task
            {
                Id = Guid.NewGuid(),
                Title = "Unchanged Task",
                CreatedAt = createdAt,
                UpdatedAt = updatedAt
            }
        };

        // Act
        var changes = _service.DetectLocalChanges(tasks, since);

        // Assert
        changes.Should().BeEmpty();
    }

    /// <summary>
    /// Tests the DetectLocalChanges method when a task is deleted after the since timestamp.
    /// </summary>
    [Fact]
    public void DetectLocalChanges_DeletedTaskAfterSinceTimestamp_ReturnsDeletedChangeLog()
    {
        // Arrange
        var since = DateTime.UtcNow.AddHours(-1);
        var createdAt = DateTime.UtcNow.AddDays(-2);
        var deletedAt = DateTime.UtcNow.AddMinutes(-10);

        var tasks = new List<Task>
        {
            new Task
            {
                Id = Guid.NewGuid(),
                Title = "Deleted Task",
                CreatedAt = createdAt,
                UpdatedAt = createdAt,
                IsDeleted = true,
                DeletedAt = deletedAt
            }
        };

        // Act
        var changes = _service.DetectLocalChanges(tasks, since);

        // Assert
        changes.Should().HaveCount(1);
        changes[0].ChangeType.Should().Be("Deleted");
        changes[0].Source.Should().Be(ChangeSource.Local);
    }

    /// <summary>
    /// Tests the DetectNotionChanges method when identical items produce no changes.
    /// </summary>
    [Fact]
    public void DetectNotionChanges_IdenticalItemsSinceTimestamp_ReturnsNoChanges()
    {
        // Arrange
        var since = DateTime.UtcNow.AddHours(-1);
        var createdTime = DateTime.UtcNow.AddDays(-2);
        var lastEditedTime = DateTime.UtcNow.AddDays(-2); // Same as created, no changes since 'since'

        var pages = new List<NotionPage>
        {
            new NotionPage("page-123", "db-456", "Unchanged Page")
            {
                CreatedTime = createdTime,
                LastEditedTime = lastEditedTime
            }
        };

        // Act
        var changes = _service.DetectNotionChanges(pages, since);

        // Assert
        changes.Should().BeEmpty();
    }

    /// <summary>
    /// Tests the DetectNotionChanges method when a Notion page is modified after the since timestamp.
    /// </summary>
    [Fact]
    public void DetectNotionChanges_ModifiedPageAfterSinceTimestamp_ReturnsUpdatedChangeLog()
    {
        // Arrange
        var since = DateTime.UtcNow.AddHours(-1);
        var createdTime = DateTime.UtcNow.AddDays(-2);
        var lastEditedTime = DateTime.UtcNow.AddMinutes(-15);

        var pages = new List<NotionPage>
        {
            new NotionPage("page-123", "db-456", "Updated Page")
            {
                CreatedTime = createdTime,
                LastEditedTime = lastEditedTime,
                LastEditedBy = "user@example.com"
            }
        };

        // Act
        var changes = _service.DetectNotionChanges(pages, since);

        // Assert
        changes.Should().HaveCount(1);
        changes[0].ChangeType.Should().Be("Updated");
        changes[0].Source.Should().Be(ChangeSource.Notion);
        changes[0].UserEmail.Should().Be("user@example.com");
    }

    /// <summary>
    /// Tests the DetectNotionChanges method when a Notion page is archived (deleted) after the since timestamp.
    /// </summary>
    [Fact]
    public void DetectNotionChanges_ArchivedPageAfterSinceTimestamp_ReturnsDeletedChangeLog()
    {
        // Arrange
        var since = DateTime.UtcNow.AddHours(-1);
        var createdTime = DateTime.UtcNow.AddDays(-2);
        var lastEditedTime = DateTime.UtcNow.AddMinutes(-10);

        var pages = new List<NotionPage>
        {
            new NotionPage("page-123", "db-456", "Archived Page")
            {
                CreatedTime = createdTime,
                LastEditedTime = lastEditedTime,
                Archived = true
            }
        };

        // Act
        var changes = _service.DetectNotionChanges(pages, since);

        // Assert
        changes.Should().HaveCount(1);
        changes[0].ChangeType.Should().Be("Deleted");
        changes[0].Source.Should().Be(ChangeSource.Notion);
        changes[0].Description.Should().Contain("Archived Page");
    }

    /// <summary>
    /// Tests the DetectConflicts method when local and Notion changes are identical (no conflict).
    /// </summary>
    [Fact]
    public void DetectConflicts_IdenticalLocalAndNotionChanges_ReturnsNoConflicts()
    {
        // Arrange
        var taskId = Guid.NewGuid();
        var timestamp = DateTime.UtcNow.AddMinutes(-10);

        var localChanges = new List<ChangeLog>
        {
            new ChangeLog
            {
                TaskId = taskId,
                ChangeType = "Updated",
                Source = ChangeSource.Local,
                Timestamp = timestamp,
                PropertyName = "Title",
                OldValue = "Old Title",
                NewValue = "New Title"
            }
        };

        var notionChanges = new List<ChangeLog>
        {
            new ChangeLog
            {
                TaskId = taskId,
                ChangeType = "Updated",
                Source = ChangeSource.Notion,
                Timestamp = timestamp,
                PropertyName = "Title",
                OldValue = "Old Title",
                NewValue = "New Title"
            }
        };

        // Act
        var conflicts = _service.DetectConflicts(localChanges, notionChanges);

        // Assert
        conflicts.Should().BeEmpty();
    }

    /// <summary>
    /// Tests the DetectConflicts method when local and Notion changes detect a modification conflict.
    /// </summary>
    [Fact]
    public void DetectConflicts_ConcurrentModifications_ReturnsConflict()
    {
        // Arrange
        var taskId = Guid.NewGuid();
        var localTime = DateTime.UtcNow.AddMinutes(-10);
        var notionTime = DateTime.UtcNow.AddMinutes(-9); // Within 5 minute window

        var localChanges = new List<ChangeLog>
        {
            new ChangeLog
            {
                TaskId = taskId,
                ChangeType = "Updated",
                Source = ChangeSource.Local,
                Timestamp = localTime,
                PropertyName = "Title",
                OldValue = "Old Title",
                NewValue = "Locally Modified Title"
            }
        };

        var notionChanges = new List<ChangeLog>
        {
            new ChangeLog
            {
                TaskId = taskId,
                ChangeType = "Updated",
                Source = ChangeSource.Notion,
                Timestamp = notionTime,
                PropertyName = "Title",
                OldValue = "Old Title",
                NewValue = "Notion Modified Title"
            }
        };

        // Act
        var conflicts = _service.DetectConflicts(localChanges, notionChanges);

        // Assert
        conflicts.Should().HaveCount(1);
        conflicts[0].ConflictType.Should().Be(ConflictType.ConcurrentModification);
        conflicts[0].TaskId.Should().Be(taskId);
        conflicts[0].PropertyName.Should().Be("Title");
        conflicts[0].LocalValue.Should().Be("Locally Modified Title");
        conflicts[0].NotionValue.Should().Be("Notion Modified Title");
    }

    /// <summary>
    /// Tests the DetectConflicts method when changes are outside the conflict window (no conflict).
    /// </summary>
    [Fact]
    public void DetectConflicts_ChangesOutsideTimeWindow_ReturnsNoConflicts()
    {
        // Arrange
        var taskId = Guid.NewGuid();
        var localTime = DateTime.UtcNow.AddMinutes(-10);
        var notionTime = DateTime.UtcNow.AddMinutes(-20); // Outside 5 minute window

        var localChanges = new List<ChangeLog>
        {
            new ChangeLog
            {
                TaskId = taskId,
                ChangeType = "Updated",
                Source = ChangeSource.Local,
                Timestamp = localTime,
                PropertyName = "Title",
                OldValue = "Old Title",
                NewValue = "New Title"
            }
        };

        var notionChanges = new List<ChangeLog>
        {
            new ChangeLog
            {
                TaskId = taskId,
                ChangeType = "Updated",
                Source = ChangeSource.Notion,
                Timestamp = notionTime,
                PropertyName = "Title",
                OldValue = "Old Title",
                NewValue = "New Title"
            }
        };

        // Act
        var conflicts = _service.DetectConflicts(localChanges, notionChanges);

        // Assert
        conflicts.Should().BeEmpty();
    }

    /// <summary>
    /// Tests the ArePropertyValuesEqual method with identical values.
    /// </summary>
    [Fact]
    public void ArePropertyValuesEqual_IdenticalValues_ReturnsTrue()
    {
        // Act & Assert
        ChangeDetectionService.ArePropertyValuesEqual("test", "test").Should().BeTrue();
        ChangeDetectionService.ArePropertyValuesEqual(42, 42).Should().BeTrue();
        ChangeDetectionService.ArePropertyValuesEqual(null, null).Should().BeTrue();
    }

    /// <summary>
    /// Tests the ArePropertyValuesEqual method with different values.
    /// </summary>
    [Fact]
    public void ArePropertyValuesEqual_DifferentValues_ReturnsFalse()
    {
        // Act & Assert
        ChangeDetectionService.ArePropertyValuesEqual("test1", "test2").Should().BeFalse();
        ChangeDetectionService.ArePropertyValuesEqual(42, 43).Should().BeFalse();
        ChangeDetectionService.ArePropertyValuesEqual("value", null).Should().BeFalse();
        ChangeDetectionService.ArePropertyValuesEqual(null, "value").Should().BeFalse();
    }

    /// <summary>
    /// Tests the ArePropertyValuesEqual method with rich text normalization.
    /// </summary>
    [Fact]
    public void ArePropertyValuesEqual_RichTextWithDifferentAnnotations_ReturnsTrue()
    {
        // Arrange
        var richTextWithAnnotations = new List<object>
        {
            new Dictionary<string, object>
            {
                ["text"] = new Dictionary<string, object>
                {
                    ["content"] = "Hello World"
                },
                ["annotations"] = new Dictionary<string, object>
                {
                    ["bold"] = true,
                    ["italic"] = false
                },
                ["plain_text"] = "Hello World"
            }
        };

        var richTextWithDifferentAnnotations = new List<object>
        {
            new Dictionary<string, object>
            {
                ["text"] = new Dictionary<string, object>
                {
                    ["content"] = "Hello World"
                },
                ["annotations"] = new Dictionary<string, object>
                {
                    ["bold"] = false,
                    ["italic"] = true
                },
                ["plain_text"] = "Hello World"
            }
        };

        // Act & Assert
        ChangeDetectionService.ArePropertyValuesEqual(richTextWithAnnotations, richTextWithDifferentAnnotations)
            .Should().BeTrue();
    }

    /// <summary>
    /// Tests the GetLastChange method when the repository has changes.
    /// </summary>
    [Fact]
    public void GetLastChange_WhenRepositoryHasChanges_ReturnsFirstEntry()
    {
        // Arrange
        var taskId = Guid.NewGuid();
        var expectedChange = new ChangeLog
        {
            TaskId = taskId,
            ChangeType = "Updated",
            Source = ChangeSource.Notion,
            Timestamp = DateTime.UtcNow.AddMinutes(-5)
        };

        _mockRepo
            .Setup(r => r.GetByTaskIdAsync(taskId, 1))
            .ReturnsAsync(new List<ChangeLog> { expectedChange });

        // Act
        var result = _service.GetLastChange(taskId);

        // Assert
        result.Should().NotBeNull();
        result!.ChangeType.Should().Be("Updated");
        result.Source.Should().Be(ChangeSource.Notion);
        _mockRepo.Verify(r => r.GetByTaskIdAsync(taskId, 1), Times.Once);
    }

    /// <summary>
    /// Tests the HasChangedSince method when a task has been modified since the timestamp.
    /// </summary>
    [Fact]
    public void HasChangedSince_TaskModifiedSinceTimestamp_ReturnsTrue()
    {
        // Arrange
        var since = DateTime.UtcNow.AddHours(-1);
        var task = new Task
        {
            Id = Guid.NewGuid(),
            Title = "Test Task",
            CreatedAt = DateTime.UtcNow.AddDays(-2),
            UpdatedAt = DateTime.UtcNow.AddMinutes(-30) // Modified since 'since'
        };

        // Act
        var result = _service.HasChangedSince(task, since);

        // Assert
        result.Should().BeTrue();
    }

    /// <summary>
    /// Tests the HasChangedSince method when a task has not been modified since the timestamp.
    /// </summary>
    [Fact]
    public void HasChangedSince_TaskNotModifiedSinceTimestamp_ReturnsFalse()
    {
        // Arrange
        var since = DateTime.UtcNow.AddHours(-1);
        var task = new Task
        {
            Id = Guid.NewGuid(),
            Title = "Test Task",
            CreatedAt = DateTime.UtcNow.AddDays(-2),
            UpdatedAt = DateTime.UtcNow.AddDays(-2) // Not modified since 'since'
        };

        // Act
        var result = _service.HasChangedSince(task, since);

        // Assert
        result.Should().BeFalse();
    }

    /// <summary>
    /// Tests the HasChangedSince method when a task is deleted since the timestamp.
    /// </summary>
    [Fact]
    public void HasChangedSince_TaskDeletedSinceTimestamp_ReturnsTrue()
    {
        // Arrange
        var since = DateTime.UtcNow.AddHours(-1);
        var task = new Task
        {
            Id = Guid.NewGuid(),
            Title = "Test Task",
            CreatedAt = DateTime.UtcNow.AddDays(-2),
            UpdatedAt = DateTime.UtcNow.AddDays(-2),
            IsDeleted = true,
            DeletedAt = DateTime.UtcNow.AddMinutes(-30) // Deleted since 'since'
        };

        // Act
        var result = _service.HasChangedSince(task, since);

        // Assert
        result.Should().BeTrue();
    }
}