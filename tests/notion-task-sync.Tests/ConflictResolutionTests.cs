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
/// Tests for conflict resolution functionality.
/// </summary>
public class ConflictResolutionTests
{
    /// <summary>
    /// Tests that the Resolve method sets the resolved status and method correctly.
    /// </summary>
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

    /// <summary>
    /// Tests that the MarkForManualReview method sets the pending review status with a reason.
    /// </summary>
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

    /// <summary>
    /// Tests that the GetResolutionStats method returns the correct resolution rate.
    /// </summary>
    /// <param name="conflicts">A list of conflicts with mixed statuses.</param>
    /// <returns>A tuple containing the total number of conflicts, the number of resolved conflicts, and the resolution rate.</returns>
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

    /// <summary>
    /// Tests that the GetPendingConflicts method excludes resolved conflicts.
    /// </summary>
    /// <param name="conflicts">A list of conflicts with mixed statuses.</param>
    /// <returns>A list of pending conflicts.</returns>
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

    /// <summary>
    /// Tests that the MergeConflicts method marks a conflict for manual review when both sides have modified the same property with different values.
    /// </summary>
    [Fact]
    public void MergeConflicts_WhenBothSidesModifiedSamePropertyWithDifferentValues_MarksForManualReview()
    {
        // Arrange
        var mockRepo = new Mock<IChangeLogRepository>();
        var service = new ConflictResolutionService(mockRepo.Object);

        var conflict = new ConflictResolution
        {
            TaskId = Guid.NewGuid(),
            LocalValue = "local status value",
            NotionValue = "notion status value",
            PropertyName = "Status",
            ConflictType = ConflictType.ConcurrentModification
        };

        // Act
        var result = service.MergeConflicts(conflict);

        // Assert
        result.Status.Should().Be(ResolutionStatus.PendingReview);
        result.ResolutionNotes.Should().Contain("both sides modified Status");
        result.IsPending().Should().BeTrue();
    }

    /// <summary>
    /// Tests that the MergeConflicts method resolves a conflict with the merged method when both sides have identical values.
    /// </summary>
    [Fact]
    public void MergeConflicts_WhenBothSidesHaveIdenticalValues_ResolvesWithMergedMethod()
    {
        // Arrange
        var mockRepo = new Mock<IChangeLogRepository>();
        var service = new ConflictResolutionService(mockRepo.Object);

        var conflict = new ConflictResolution
        {
            TaskId = Guid.NewGuid(),
            LocalValue = "same value",
            NotionValue = "same value",
            PropertyName = "Title"
        };

        // Act
        var result = service.MergeConflicts(conflict);

        // Assert
        result.Status.Should().Be(ResolutionStatus.Resolved);
        result.ResolutionMethod.Should().Be(ResolutionMethod.Merged);
        result.ResolvedValue.Should().Be("same value");
    }
}
