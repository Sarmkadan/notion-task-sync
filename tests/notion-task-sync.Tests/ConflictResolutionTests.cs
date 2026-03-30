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

public class ConflictResolutionTests
{
    [Fact]
    public void Resolve_WhenCalled_SetsResolvedStatusAndMethod()
    {
        // Arrange
        var conflict = new ConflictResolution
        {
            TaskId = Guid.NewGuid(),
            LocalValue = "local version",
            NotionValue = "notion version"
        };

        // Act
        conflict.Resolve("local version", ResolutionMethod.LocalWins, "local takes precedence");

        // Assert
        conflict.Status.Should().Be(ResolutionStatus.Resolved);
        conflict.ResolvedValue.Should().Be("local version");
        conflict.ResolutionMethod.Should().Be(ResolutionMethod.LocalWins);
        conflict.ResolutionNotes.Should().Be("local takes precedence");
        conflict.ResolvedAt.Should().NotBeNull();
    }

    [Fact]
    public void MarkForManualReview_WhenCalled_SetsPendingReviewStatusWithReason()
    {
        // Arrange
        var conflict = new ConflictResolution
        {
            TaskId = Guid.NewGuid(),
            ConflictType = ConflictType.ConcurrentModification
        };

        // Act
        conflict.MarkForManualReview("Values diverged, manual inspection needed");

        // Assert
        conflict.Status.Should().Be(ResolutionStatus.PendingReview);
        conflict.IsPending().Should().BeTrue();
        conflict.ResolutionNotes.Should().Contain("manual inspection needed");
    }

    [Fact]
    public void GetResolutionStats_WithMixedConflictStatuses_ReturnsCorrectResolutionRate()
    {
        // Arrange
        var mockRepo = new Mock<IChangeLogRepository>();
        var service = new ConflictResolutionService(mockRepo.Object);

        var conflicts = new List<ConflictResolution>
        {
            new() { TaskId = Guid.NewGuid(), Status = ResolutionStatus.Resolved },
            new() { TaskId = Guid.NewGuid(), Status = ResolutionStatus.Resolved },
            new() { TaskId = Guid.NewGuid(), Status = ResolutionStatus.Pending },
            new() { TaskId = Guid.NewGuid(), Status = ResolutionStatus.PendingReview }
        };

        // Act
        var stats = service.GetResolutionStats(conflicts);

        // Assert
        stats.TotalConflicts.Should().Be(4);
        stats.ResolvedCount.Should().Be(2);
        stats.PendingReviewCount.Should().Be(1);
        stats.ResolutionRate.Should().Be(0.5);
    }

    [Fact]
    public void GetPendingConflicts_WithMixedConflictStatuses_ExcludesResolvedConflicts()
    {
        // Arrange
        var mockRepo = new Mock<IChangeLogRepository>();
        var service = new ConflictResolutionService(mockRepo.Object);

        var conflicts = new List<ConflictResolution>
        {
            new() { TaskId = Guid.NewGuid(), Status = ResolutionStatus.Pending },
            new() { TaskId = Guid.NewGuid(), Status = ResolutionStatus.PendingReview },
            new() { TaskId = Guid.NewGuid(), Status = ResolutionStatus.Resolved },
            new() { TaskId = Guid.NewGuid(), Status = ResolutionStatus.Abandoned }
        };

        // Act
        var pending = service.GetPendingConflicts(conflicts);

        // Assert
        pending.Should().HaveCount(2);
        pending.Should().OnlyContain(c => c.IsPending());
    }
}
