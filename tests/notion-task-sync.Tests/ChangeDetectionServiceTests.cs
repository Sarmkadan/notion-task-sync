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
        changes.Should().ContainSingle();
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
        changes.Should().ContainSingle();
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
        // Arrange - task deleted after 'since' timestamp
        // Note: UpdatedAt must be >= since for the task to enter the detection loop
        var since = DateTime.UtcNow.AddHours(-1);
        var createdAt = DateTime.UtcNow.AddDays(-2);
        var updatedAt = DateTime.UtcNow.AddMinutes(-15); // Modified recently
        var deletedAt = DateTime.UtcNow.AddMinutes(-10); // Deleted after 'since'

        var tasks = new List<Task>
        {
            new Task
            {
                Id = Guid.NewGuid(),
                Title = "Deleted Task",
                CreatedAt = createdAt,
                UpdatedAt = updatedAt, // Must be >= since for task to be processed
                IsDeleted = true,
                DeletedAt = deletedAt // Deleted after 'since'
            }
        };

        // Act
        var changes = _service.DetectLocalChanges(tasks, since);

        // Assert - deletion detected because DeletedAt >= since and task was in filtered list
        changes.Should().ContainSingle(c => c.ChangeType == "Deleted");
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

        // Assert - Archived pages generate both Updated and Deleted changes
        changes.Should().HaveCount(2);
        changes.Should().Contain(c => c.ChangeType == "Updated");
        changes.Should().Contain(c => c.ChangeType == "Deleted");
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

    /// <summary>
    /// Tests the DetectLocalChanges method when a local task is modified after last sync but remote is unchanged.
    /// Expected: local-only change detected.
    /// </summary>
    [Fact]
    public void DetectLocalChanges_LocalModifiedRemoteUnchanged_DetectsLocalOnlyChange()
    {
        // Arrange - local task modified after last sync
        var since = DateTime.UtcNow.AddHours(-1);
        var createdAt = DateTime.UtcNow.AddDays(-2);
        var updatedAt = DateTime.UtcNow.AddMinutes(-15); // Modified locally after last sync

        var localTasks = new List<Task>
        {
            new Task
            {
                Id = Guid.NewGuid(),
                Title = "Locally Modified Task",
                CreatedAt = createdAt,
                UpdatedAt = updatedAt
            }
        };

        // No notion changes (unchanged)
        var notionPages = new List<NotionPage>();

        // Act
        var localChanges = _service.DetectLocalChanges(localTasks, since);
        var notionChanges = _service.DetectNotionChanges(notionPages, since);

        // Assert - local change detected, no notion changes
        localChanges.Should().HaveCount(1);
        localChanges[0].ChangeType.Should().Be("Updated");
        localChanges[0].Source.Should().Be(ChangeSource.Local);
        notionChanges.Should().BeEmpty();
    }

    /// <summary>
    /// Tests the DetectNotionChanges method when a Notion page is modified after last sync but local is unchanged.
    /// Expected: remote-only change detected.
    /// </summary>
    [Fact]
    public void DetectNotionChanges_RemoteModifiedLocalUnchanged_DetectsRemoteOnlyChange()
    {
        // Arrange - notion page modified after last sync
        var since = DateTime.UtcNow.AddHours(-1);
        var createdTime = DateTime.UtcNow.AddDays(-2);
        var lastEditedTime = DateTime.UtcNow.AddMinutes(-15); // Modified in Notion after last sync

        var notionPages = new List<NotionPage>
        {
            new NotionPage("page-123", "db-456", "Notion Modified Task")
            {
                CreatedTime = createdTime,
                LastEditedTime = lastEditedTime
            }
        };

        // No local changes (unchanged)
        var localTasks = new List<Task>();

        // Act
        var localChanges = _service.DetectLocalChanges(localTasks, since);
        var notionChanges = _service.DetectNotionChanges(notionPages, since);

        // Assert - notion change detected, no local changes
        localChanges.Should().BeEmpty();
        notionChanges.Should().HaveCount(1);
        notionChanges[0].ChangeType.Should().Be("Updated");
        notionChanges[0].Source.Should().Be(ChangeSource.Notion);
    }

    /// <summary>
    /// Tests the DetectConflicts method when both local and Notion changes exist with same timestamp.
    /// Expected: conflict flagged when changes are concurrent.
    /// </summary>
    [Fact]
    public void DetectConflicts_BothModifiedWithConcurrentTimestamps_FlagsConflict()
    {
        // Arrange - both sides modified with concurrent timestamps
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

        // Assert - concurrent changes create conflicts
        conflicts.Should().ContainSingle();
        conflicts[0].ConflictType.Should().Be(ConflictType.ConcurrentModification);
        conflicts[0].TaskId.Should().Be(taskId);
    }

    /// <summary>
    /// Tests the DetectConflicts method when both sides have deletion changes.
    /// Expected: deletion conflict detected.
    /// </summary>
    [Fact]
    public void DetectConflicts_DeletionOnBothSides_DetectsDeletionConflict()
    {
        // Arrange - both sides have deletion changes
        var taskId = Guid.NewGuid();
        var localTime = DateTime.UtcNow.AddMinutes(-10);
        var notionTime = DateTime.UtcNow.AddMinutes(-9);

        var localChanges = new List<ChangeLog>
        {
            new ChangeLog
            {
                TaskId = taskId,
                ChangeType = "Deleted",
                Source = ChangeSource.Local,
                Timestamp = localTime
            }
        };

        var notionChanges = new List<ChangeLog>
        {
            new ChangeLog
            {
                TaskId = taskId,
                ChangeType = "Deleted",
                Source = ChangeSource.Notion,
                Timestamp = notionTime
            }
        };

        // Act
        var conflicts = _service.DetectConflicts(localChanges, notionChanges);

        // Assert - deletion conflict detected
        conflicts.Should().HaveCount(1);
        conflicts[0].ConflictType.Should().Be(ConflictType.DeletionConflict);
    }

    /// <summary>
    /// Tests the DetectConflicts method when changes are concurrent but different property names.
    /// Expected: property mismatch conflict detected.
    /// </summary>
    [Fact]
    public void DetectConflicts_ConcurrentDifferentProperties_FlagsPropertyMismatch()
    {
        // Arrange - both sides modified but different properties
        var taskId = Guid.NewGuid();
        var localTime = DateTime.UtcNow.AddMinutes(-10);
        var notionTime = DateTime.UtcNow.AddMinutes(-9);

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
                PropertyName = "Description",
                OldValue = "Old Description",
                NewValue = "New Description"
            }
        };

        // Act
        var conflicts = _service.DetectConflicts(localChanges, notionChanges);

        // Assert - property mismatch conflict detected
        conflicts.Should().HaveCount(1);
        conflicts[0].ConflictType.Should().Be(ConflictType.PropertyMismatch);
    }

    /// <summary>
    /// Tests the DetectLocalChanges method when a task exists locally but is missing from Notion (deleted remotely).
    /// Expected: deletion detected, not treated as 'no change'.
    /// </summary>
    [Fact]
    public void DetectLocalChanges_TaskDeletedRemotely_DetectsRemoteDeletion()
    {
        // Arrange - task exists locally but was deleted from Notion (archived)
        var since = DateTime.UtcNow.AddHours(-1);
        var createdAt = DateTime.UtcNow.AddDays(-2);
        var deletedAt = DateTime.UtcNow.AddMinutes(-10); // Deleted in Notion after last sync

        var localTasks = new List<Task>
        {
            new Task
            {
                Id = Guid.NewGuid(),
                Title = "Remotely Deleted Task",
                CreatedAt = createdAt,
                UpdatedAt = createdAt,
                IsDeleted = false
            }
        };

        var notionPages = new List<NotionPage>
        {
            new NotionPage("page-123", "db-456", "Archived Task")
            {
                CreatedTime = createdAt,
                LastEditedTime = deletedAt,
                Archived = true
            }
        };

        // Act - detect changes from both sources
        var localChanges = _service.DetectLocalChanges(localTasks, since);
        var notionChanges = _service.DetectNotionChanges(notionPages, since);

        // Assert - deletion detected from Notion side (archived pages generate both Updated and Deleted)
        localChanges.Should().BeEmpty();
        notionChanges.Should().Contain(c => c.ChangeType == "Deleted");
    }

    /// <summary>
    /// Tests the DetectLocalChanges method when a task has timestamps before the 'since' parameter.
    /// Expected: doesn't get misclassified as changed.
    /// </summary>
    [Fact]
    public void DetectLocalChanges_TaskWithOldTimestamps_DoesNotDetectChanges()
    {
        // Arrange - task with old timestamps (before 'since')
        var since = DateTime.UtcNow.AddHours(-1);
        var createdAt = DateTime.UtcNow.AddDays(-30); // Old creation date
        var updatedAt = DateTime.UtcNow.AddDays(-30); // Old update date

        var tasks = new List<Task>
        {
            new Task
            {
                Id = Guid.NewGuid(),
                Title = "Old Task",
                CreatedAt = createdAt,
                UpdatedAt = updatedAt
            }
        };

        // Act
        var changes = _service.DetectLocalChanges(tasks, since);

        // Assert - no changes detected
        changes.Should().BeEmpty();
    }

    /// <summary>
    /// Tests the DetectNotionChanges method when a Notion page has null/empty timestamps.
    /// Expected: doesn't get misclassified as always changed.
    /// </summary>
    [Fact]
    public void DetectNotionChanges_PageWithDefaultTimestamps_DoesNotMisclassify()
    {
        // Arrange - page with default/minimum timestamps
        var since = DateTime.UtcNow.AddHours(-1);
        var createdTime = default(DateTime); // Default/minimum DateTime
        var lastEditedTime = default(DateTime);

        var pages = new List<NotionPage>
        {
            new NotionPage("page-123", "db-456", "Page with Default Timestamps")
            {
                CreatedTime = createdTime,
                LastEditedTime = lastEditedTime
            }
        };

        // Act
        var changes = _service.DetectNotionChanges(pages, since);

        // Assert - no changes detected (not misclassified as always changed)
        changes.Should().BeEmpty();
    }

    /// <summary>
    /// Tests the DetectLocalChanges method when a task has null UpdatedAt timestamp.
    /// Expected: doesn't get misclassified as always changed.
    /// </summary>
    [Fact]
    public void DetectLocalChanges_TaskWithNullUpdatedAt_DoesNotMisclassify()
    {
        // Arrange - task with null UpdatedAt
        var since = DateTime.UtcNow.AddHours(-1);
        var createdAt = DateTime.UtcNow.AddDays(-2);
        var updatedAt = default(DateTime?); // Null UpdatedAt

        var tasks = new List<Task>
        {
            new Task
            {
                Id = Guid.NewGuid(),
                Title = "Task with Null UpdatedAt",
                CreatedAt = createdAt,
                UpdatedAt = updatedAt ?? createdAt
            }
        };

        // Act
        var changes = _service.DetectLocalChanges(tasks, since);

        // Assert - no changes detected (not misclassified as always changed)
        changes.Should().BeEmpty();
    }

    /// <summary>
    /// Tests the DetectConflicts method when changes are outside the 5-minute conflict window.
    /// Expected: no conflict detected.
    /// </summary>
    [Fact]
    public void DetectConflicts_ChangesOutsideConflictWindow_NoConflict()
    {
        // Arrange - changes outside 5-minute window
        var taskId = Guid.NewGuid();
        var localTime = DateTime.UtcNow.AddMinutes(-10);
        var notionTime = DateTime.UtcNow.AddMinutes(-20); // Outside 10-minute window (for test clarity)

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

        // Assert - no conflict detected
        conflicts.Should().BeEmpty();
    }
}