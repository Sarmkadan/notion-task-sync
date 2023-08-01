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
    /// <summary>
    /// Tests the DetectLocalChanges method when a new task is created after the since timestamp.
    /// </summary>
    [Fact]
    public void DetectLocalChanges_NewTaskCreatedAfterSinceTimestamp_ReturnsCreatedChangeLog()
    {
        // Arrange
        var mockRepo = new Mock<IChangeLogRepository>();
        var service = new ChangeDetectionService(mockRepo.Object);

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
        var changes = service.DetectLocalChanges(tasks, since);

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
        var mockRepo = new Mock<IChangeLogRepository>();
        var service = new ChangeDetectionService(mockRepo.Object);

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
        var changes = service.DetectLocalChanges(tasks, since);

        // Assert
        changes.Should().HaveCount(1);
        changes[0].ChangeType.Should().Be("Updated");
        changes[0].Source.Should().Be(ChangeSource.Local);
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

        var mockRepo = new Mock<IChangeLogRepository>();
        mockRepo
            .Setup(r => r.GetByTaskIdAsync(taskId, 1))
            .ReturnsAsync(new List<ChangeLog> { expectedChange });

        var service = new ChangeDetectionService(mockRepo.Object);

        // Act
        var result = service.GetLastChange(taskId);

        // Assert
        result.Should().NotBeNull();
        result!.ChangeType.Should().Be("Updated");
        result.Source.Should().Be(ChangeSource.Notion);
        mockRepo.Verify(r => r.GetByTaskIdAsync(taskId, 1), Times.Once);
    }
}
