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

    /// <summary>
    /// Tests that ResolveByLastWrite handles equal timestamps by preferring Notion value.
    /// This addresses the classic bidirectional sync tie-breaking problem.
    /// </summary>
    [Fact]
    public void ResolveByLastWrite_WhenTimestampsAreEqual_PrefersNotionValue()
    {
        // Arrange
        var mockRepo = new Mock<IChangeLogRepository>();
        var service = new ConflictResolutionService(mockRepo.Object);

        var conflict = new ConflictResolution
        {
            TaskId = Guid.NewGuid(),
            LocalValue = "local edited value",
            NotionValue = "notion edited value",
            LocalModifiedAt = new DateTime(2024, 1, 1, 12, 0, 0),
            NotionModifiedAt = new DateTime(2024, 1, 1, 12, 0, 0), // Same timestamp
            ConflictType = ConflictType.ConcurrentModification
        };

        // Act - Use the ResolveWith method with Newest strategy which calls ResolveWithNewest internally
        var result = service.ResolveWith(conflict, ConflictStrategy.Newest);

        // Assert
        result.Status.Should().Be(ResolutionStatus.Resolved);
        result.ResolutionMethod.Should().Be(ResolutionMethod.LastWrite);
        result.ResolvedValue.Should().Be("notion edited value");
        result.ResolutionNotes.Should().Contain("clock skew");
        result.ResolutionNotes.Should().Contain("Preferred Notion value");
    }

    /// <summary>
    /// Tests that ResolveByLastWrite handles clock skew scenarios correctly.
    /// </summary>
    [Fact]
    public void ResolveByLastWrite_WhenClockSkewExists_UsesTimestampComparison()
    {
        // Arrange
        var mockRepo = new Mock<IChangeLogRepository>();
        var service = new ConflictResolutionService(mockRepo.Object);

        var localTime = new DateTime(2024, 1, 1, 12, 0, 0);
        var notionTime = localTime.AddSeconds(5); // 5 seconds difference

        var conflict = new ConflictResolution
        {
            TaskId = Guid.NewGuid(),
            LocalValue = "local value",
            NotionValue = "notion value",
            LocalModifiedAt = localTime,
            NotionModifiedAt = notionTime,
            ConflictType = ConflictType.ConcurrentModification
        };

        // Act - Use the ResolveWith method with Newest strategy
        var result = service.ResolveWith(conflict, ConflictStrategy.Newest);

        // Assert
        result.Status.Should().Be(ResolutionStatus.Resolved);
        result.ResolvedValue.Should().Be("notion value"); // Notion is newer
        result.ResolutionNotes.Should().Contain("Notion value is newer");
    }

    /// <summary>
    /// Tests conflict resolution when local value is newer than Notion value.
    /// </summary>
    [Fact]
    public void ResolveByLastWrite_WhenLocalIsNewer_PrefersLocalValue()
    {
        // Arrange
        var mockRepo = new Mock<IChangeLogRepository>();
        var service = new ConflictResolutionService(mockRepo.Object);

        var localTime = new DateTime(2024, 1, 1, 12, 5, 0);
        var notionTime = new DateTime(2024, 1, 1, 12, 0, 0);

        var conflict = new ConflictResolution
        {
            TaskId = Guid.NewGuid(),
            LocalValue = "local edited value",
            NotionValue = "notion original value",
            LocalModifiedAt = localTime,
            NotionModifiedAt = notionTime,
            ConflictType = ConflictType.ConcurrentModification
        };

        // Act - Use the ResolveWith method with Newest strategy
        var result = service.ResolveWith(conflict, ConflictStrategy.Newest);

        // Assert
        result.Status.Should().Be(ResolutionStatus.Resolved);
        result.ResolvedValue.Should().Be("local edited value");
        result.ResolutionNotes.Should().Contain("Local value is newer");
        result.ResolutionNotes.Should().Contain("Discarded Notion value");
    }

    /// <summary>
    /// Tests conflict resolution for delete vs edit scenarios.
    /// </summary>
    [Fact]
    public void ResolveByLastWrite_DeleteVsEdit_HandlesGracefully()
    {
        // Arrange
        var mockRepo = new Mock<IChangeLogRepository>();
        var service = new ConflictResolutionService(mockRepo.Object);

        var conflict = new ConflictResolution
        {
            TaskId = Guid.NewGuid(),
            LocalValue = "", // Empty means deleted locally
            NotionValue = "notion edited value",
            LocalModifiedAt = new DateTime(2024, 1, 1, 12, 0, 0),
            NotionModifiedAt = new DateTime(2024, 1, 1, 12, 0, 0), // Same timestamp
            ConflictType = ConflictType.DeletionConflict
        };

        // Act - Use the ResolveWith method with Newest strategy
        var result = service.ResolveWith(conflict, ConflictStrategy.Newest);

        // Assert - should prefer Notion value when local is empty (deleted)
        result.Status.Should().Be(ResolutionStatus.Resolved);
        result.ResolvedValue.Should().Be("notion edited value");
        result.ResolutionNotes.Should().Contain("clock skew");
    }

    /// <summary>
    /// Tests that ResolveWith method properly delegates to strategy-specific resolvers.
    /// </summary>
    [Fact]
    public void ResolveWith_DelegatesToStrategySpecificResolvers()
    {
        // Arrange
        var mockRepo = new Mock<IChangeLogRepository>();
        var service = new ConflictResolutionService(mockRepo.Object);

        var conflict = new ConflictResolution
        {
            TaskId = Guid.NewGuid(),
            LocalValue = "local value",
            NotionValue = "notion value"
        };

        // Act - Test PreferLocal
        var preferLocalResult = service.ResolveWith(conflict, ConflictStrategy.PreferLocal);
        preferLocalResult.ResolutionMethod.Should().Be(ResolutionMethod.LocalWins);
        preferLocalResult.ResolvedValue.Should().Be("local value");

        // Act - Test PreferRemote
        var preferRemoteResult = service.ResolveWith(conflict, ConflictStrategy.PreferRemote);
        preferRemoteResult.ResolutionMethod.Should().Be(ResolutionMethod.NotionWins);
        preferRemoteResult.ResolvedValue.Should().Be("notion value");

        // Act - Test Newest (this will resolve based on timestamp comparison)
        var newestResult = service.ResolveWith(conflict, ConflictStrategy.Newest);
        newestResult.Status.Should().Be(ResolutionStatus.Resolved);
    }

    /// <summary>
    /// Tests that ResolveConflictsAsync properly handles multiple conflicts with mixed scenarios.
    /// </summary>
    [Fact]
    public void ResolveConflictsAsync_HandlesMultipleConflictsWithDifferentScenarios()
    {
        // Arrange
        var mockRepo = new Mock<IChangeLogRepository>();
        var service = new ConflictResolutionService(mockRepo.Object);

        var conflicts = new List<ConflictResolution>
        {
            new()
            {
                TaskId = Guid.NewGuid(),
                LocalValue = "local1",
                NotionValue = "notion1",
                LocalModifiedAt = new DateTime(2024, 1, 1, 12, 0, 0),
                NotionModifiedAt = new DateTime(2024, 1, 1, 12, 0, 1), // Notion is newer
                ConflictType = ConflictType.ConcurrentModification
            },
            new()
            {
                TaskId = Guid.NewGuid(),
                LocalValue = "local2",
                NotionValue = "notion2",
                LocalModifiedAt = new DateTime(2024, 1, 1, 12, 0, 0),
                NotionModifiedAt = new DateTime(2024, 1, 1, 12, 0, 0), // Equal timestamps
                ConflictType = ConflictType.ConcurrentModification
            },
            new()
            {
                TaskId = Guid.NewGuid(),
                LocalValue = "local3",
                NotionValue = "notion3",
                LocalModifiedAt = new DateTime(2024, 1, 1, 12, 0, 2),
                NotionModifiedAt = new DateTime(2024, 1, 1, 12, 0, 0), // Local is newer
                ConflictType = ConflictType.ConcurrentModification
            }
        };

        // Act
        var results = service.ResolveConflictsAsync(conflicts, ConflictResolutionStrategy.LastWrite).Result;

        // Assert
        results.Should().HaveCount(3);
        results[0].ResolvedValue.Should().Be("notion1"); // Notion newer
        results[1].ResolvedValue.Should().Be("notion2"); // Tie-break to Notion
        results[2].ResolvedValue.Should().Be("local3"); // Local newer

        results.Should().OnlyContain(r => r.Status == ResolutionStatus.Resolved);
    }

    /// <summary>
    /// Tests clock skew tolerance with timestamps within tolerance window.
    /// When timestamps are within the tolerance window, they should be considered equal.
    /// </summary>
    [Fact]
    public void ResolveWithNewest_WhenTimestampsWithinTolerance_UsesTieBreakRule()
    {
        // Arrange
        var mockRepo = new Mock<IChangeLogRepository>();
        var service = new ConflictResolutionService(mockRepo.Object);

        var baseTime = new DateTime(2024, 1, 1, 12, 0, 0);

        // Test with timestamps within default 1-minute tolerance (60000ms)
        var localTime = baseTime;
        var notionTime = baseTime.AddMilliseconds(30000); // 30 seconds difference

        var conflict = new ConflictResolution
        {
            TaskId = Guid.NewGuid(),
            LocalValue = "local value",
            NotionValue = "notion value",
            LocalModifiedAt = localTime,
            NotionModifiedAt = notionTime,
            ConflictType = ConflictType.ConcurrentModification
        };

        // Act - Use the ResolveWith method with Newest strategy
        var result = service.ResolveWith(conflict, ConflictStrategy.Newest);

        // Assert - Notion is newer by 30 seconds (within tolerance), so it should be selected
        result.Status.Should().Be(ResolutionStatus.Resolved);
        result.ResolutionMethod.Should().Be(ResolutionMethod.LastWrite);
        result.ResolvedValue.Should().Be("notion value"); // Notion is newer
        result.ResolutionNotes.Should().Contain("Notion value is newer");
    }

    /// <summary>
    /// Tests clock skew tolerance with timestamps outside tolerance window.
    /// When timestamps exceed tolerance, the newer timestamp should win.
    /// </summary>
    [Fact]
    public void ResolveWithNewest_WhenTimestampsOutsideTolerance_UsesTimestampComparison()
    {
        // Arrange
        var mockRepo = new Mock<IChangeLogRepository>();
        var service = new ConflictResolutionService(mockRepo.Object);

        var baseTime = new DateTime(2024, 1, 1, 12, 0, 0);

        // Test with timestamps exceeding default 1-minute tolerance (60000ms)
        var localTime = baseTime;
        var notionTime = baseTime.AddMilliseconds(90000); // 90 seconds difference (exceeds 60s tolerance)

        var conflict = new ConflictResolution
        {
            TaskId = Guid.NewGuid(),
            LocalValue = "local value",
            NotionValue = "notion value",
            LocalModifiedAt = localTime,
            NotionModifiedAt = notionTime,
            ConflictType = ConflictType.ConcurrentModification
        };

        // Act - Use the ResolveWith method with Newest strategy
        var result = service.ResolveWith(conflict, ConflictStrategy.Newest);

        // Assert - should prefer Notion since it's newer and outside tolerance
        result.Status.Should().Be(ResolutionStatus.Resolved);
        result.ResolvedValue.Should().Be("notion value");
        result.ResolutionNotes.Should().Contain("Notion value is newer");
    }

    /// <summary>
    /// Tests clock skew tolerance with zero timestamp difference.
    /// </summary>
    [Fact]
    public void ResolveWithNewest_WhenTimestampsAreIdentical_UsesTieBreakRule()
    {
        // Arrange
        var mockRepo = new Mock<IChangeLogRepository>();
        var service = new ConflictResolutionService(mockRepo.Object);

        var conflict = new ConflictResolution
        {
            TaskId = Guid.NewGuid(),
            LocalValue = "local value",
            NotionValue = "notion value",
            LocalModifiedAt = new DateTime(2024, 1, 1, 12, 0, 0),
            NotionModifiedAt = new DateTime(2024, 1, 1, 12, 0, 0), // Exactly identical
            ConflictType = ConflictType.ConcurrentModification
        };

        // Act - Use the ResolveWith method with Newest strategy
        var result = service.ResolveWith(conflict, ConflictStrategy.Newest);

        // Assert
        result.Status.Should().Be(ResolutionStatus.Resolved);
        result.ResolutionMethod.Should().Be(ResolutionMethod.LastWrite);
        result.ResolvedValue.Should().Be("notion value"); // Tie-break to Notion
        result.ResolutionNotes.Should().Contain("clock skew");
        result.ResolutionNotes.Should().Contain("0ms");
    }

    /// <summary>
    /// Tests conflict resolution with ResolveConflictsAsync using LastWrite strategy.
    /// </summary>
    [Fact]
    public void ResolveConflictsAsync_WithClockSkewTolerance_HandlesMixedScenarios()
    {
        // Arrange
        var mockRepo = new Mock<IChangeLogRepository>();
        var service = new ConflictResolutionService(mockRepo.Object);

        var baseTime = new DateTime(2024, 1, 1, 12, 0, 0);

        var conflicts = new List<ConflictResolution>
        {
            new()
            {
                TaskId = Guid.NewGuid(),
                LocalValue = "local1",
                NotionValue = "notion1",
                LocalModifiedAt = baseTime,
                NotionModifiedAt = baseTime.AddMilliseconds(30000), // 30s difference (within tolerance)
                ConflictType = ConflictType.ConcurrentModification
            },
            new()
            {
                TaskId = Guid.NewGuid(),
                LocalValue = "local2",
                NotionValue = "notion2",
                LocalModifiedAt = baseTime,
                NotionModifiedAt = baseTime.AddMilliseconds(120000), // 120s difference (outside tolerance)
                ConflictType = ConflictType.ConcurrentModification
            },
            new()
            {
                TaskId = Guid.NewGuid(),
                LocalValue = "local3",
                NotionValue = "notion3",
                LocalModifiedAt = baseTime.AddMilliseconds(1000), // 1s difference (within tolerance)
                NotionModifiedAt = baseTime,
                ConflictType = ConflictType.ConcurrentModification
            }
        };

        // Act
        var results = service.ResolveConflictsAsync(conflicts, ConflictResolutionStrategy.LastWrite).Result;

        // Assert
        results.Should().HaveCount(3);
        results[0].ResolvedValue.Should().Be("notion1"); // Tie-break to Notion (30s within tolerance)
        results[1].ResolvedValue.Should().Be("notion2"); // Notion is newer (120s outside tolerance)
        results[2].ResolvedValue.Should().Be("local3"); // Local is newer (1s within tolerance)

        results.Should().OnlyContain(r => r.Status == ResolutionStatus.Resolved);
    }

    /// <summary>
    /// Tests that ResolveConflictsAsync throws ArgumentNullException when local task value is null.
    /// Verifies proper null handling for individual conflict properties.
    /// </summary>
    [Fact]
    public void ResolveConflictsAsync_WithNullLocalTaskValue_ThrowsArgumentNullException()
    {
        // Arrange
        var mockRepo = new Mock<IChangeLogRepository>();
        var service = new ConflictResolutionService(mockRepo.Object);

        var conflict = new ConflictResolution
        {
            TaskId = Guid.NewGuid(),
            LocalValue = null, // Null local value
            NotionValue = "notion value",
            ConflictType = ConflictType.ConcurrentModification
        };

        // Act & Assert
        var act = () => service.ResolveConflictsAsync(new List<ConflictResolution> { conflict }, ConflictResolutionStrategy.LastWrite);

        act.Should().ThrowAsync<ArgumentNullException>()
            .WithMessage("*LocalValue*");
    }

    /// <summary>
    /// Tests that ResolveConflictsAsync throws ArgumentNullException when remote (Notion) task value is null.
    /// Verifies proper null handling for individual conflict properties.
    /// </summary>
    [Fact]
    public void ResolveConflictsAsync_WithNullRemoteTaskValue_ThrowsArgumentNullException()
    {
        // Arrange
        var mockRepo = new Mock<IChangeLogRepository>();
        var service = new ConflictResolutionService(mockRepo.Object);

        var conflict = new ConflictResolution
        {
            TaskId = Guid.NewGuid(),
            LocalValue = "local value",
            NotionValue = null, // Null Notion value
            ConflictType = ConflictType.ConcurrentModification
        };

        // Act & Assert
        var act = () => service.ResolveConflictsAsync(new List<ConflictResolution> { conflict }, ConflictResolutionStrategy.LastWrite);

        act.Should().ThrowAsync<ArgumentNullException>()
            .WithMessage("*NotionValue*");
    }

    /// <summary>
    /// Tests conflict resolution when both sides have identical content and identical timestamps.
    /// Expected behavior: This should be a no-op resolution where the values are already identical.
    /// The resolution should still mark the conflict as resolved but preserve the identical values.
    /// </summary>
    [Fact]
    public void ResolveConflictsAsync_WithIdenticalContentIdenticalTimestamps_NoOpResolution()
    {
        // Arrange
        var mockRepo = new Mock<IChangeLogRepository>();
        var service = new ConflictResolutionService(mockRepo.Object);

        var conflict = new ConflictResolution
        {
            TaskId = Guid.NewGuid(),
            LocalValue = "identical content",
            NotionValue = "identical content", // Same content
            LocalModifiedAt = new DateTime(2024, 1, 1, 12, 0, 0),
            NotionModifiedAt = new DateTime(2024, 1, 1, 12, 0, 0), // Same timestamp
            ConflictType = ConflictType.ConcurrentModification
        };

        // Act - Use LastWrite strategy
        var results = service.ResolveConflictsAsync(new List<ConflictResolution> { conflict }, ConflictResolutionStrategy.LastWrite).Result;

        // Assert - Should resolve successfully with identical values
        results.Should().HaveCount(1);
        results[0].Status.Should().Be(ResolutionStatus.Resolved);
        results[0].ResolutionMethod.Should().Be(ResolutionMethod.LastWrite);
        results[0].ResolvedValue.Should().Be("identical content");
        results[0].ResolutionNotes.Should().Contain("tie-break");
        results[0].ResolutionNotes.Should().Contain("Preferred Notion value");
    }

    /// <summary>
    /// Tests that ResolveConflictsAsync throws InvalidOperationException for truly unrecognized ConflictResolutionStrategy values.
    /// Instead of silently falling back to a default strategy, it should throw a specific exception
    /// to alert developers about configuration errors.
    /// </summary>
    [Fact]
    public void ResolveConflictsAsync_WithUnrecognizedStrategy_ThrowsInvalidOperationException()
    {
        // Arrange
        var mockRepo = new Mock<IChangeLogRepository>();
        var service = new ConflictResolutionService(mockRepo.Object);

        var conflict = new ConflictResolution
        {
            TaskId = Guid.NewGuid(),
            LocalValue = "local value",
            NotionValue = "notion value",
            LocalModifiedAt = new DateTime(2024, 1, 1, 12, 0, 0),
            NotionModifiedAt = new DateTime(2024, 1, 1, 12, 0, 0),
            ConflictType = ConflictType.ConcurrentModification
        };

        // Act & Assert - Use an unrecognized enum value that's outside the valid range
        var act = () => service.ResolveConflictsAsync(
            new List<ConflictResolution> { conflict },
            (ConflictResolutionStrategy)int.MaxValue);

        act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*unrecognized*strategy*");
    }

    /// <summary>
    /// Tests all supported conflict resolution strategies with genuine two-sided conflicts.
    /// Verifies that each strategy correctly preserves the expected side's data.
    /// </summary>
    [Fact]
    public void ResolveConflictsAsync_AllSupportedStrategies_AppliesCorrectResolution()
    {
        // Arrange
        var mockRepo = new Mock<IChangeLogRepository>();
        var service = new ConflictResolutionService(mockRepo.Object);

        // Create conflicts where timestamps are equal to ensure strategy determines outcome
        var baseTime = new DateTime(2024, 1, 1, 12, 0, 0);

        var conflicts = new List<ConflictResolution>
        {
            new()
            {
                TaskId = Guid.NewGuid(),
                LocalValue = "local edited title",
                NotionValue = "notion original title",
                LocalModifiedAt = baseTime,
                NotionModifiedAt = baseTime,
                PropertyName = "Title",
                ConflictType = ConflictType.ConcurrentModification
            },
            new()
            {
                TaskId = Guid.NewGuid(),
                LocalValue = "local updated description",
                NotionValue = "notion original description",
                LocalModifiedAt = baseTime,
                NotionModifiedAt = baseTime,
                PropertyName = "Description",
                ConflictType = ConflictType.ConcurrentModification
            },
            new()
            {
                TaskId = Guid.NewGuid(),
                LocalValue = "local changed status",
                NotionValue = "notion original status",
                LocalModifiedAt = baseTime,
                NotionModifiedAt = baseTime,
                PropertyName = "Status",
                ConflictType = ConflictType.ConcurrentModification
            },
            new()
            {
                TaskId = Guid.NewGuid(),
                LocalValue = "local final value",
                NotionValue = "notion original value",
                LocalModifiedAt = baseTime,
                NotionModifiedAt = baseTime,
                PropertyName = "Priority",
                ConflictType = ConflictType.ConcurrentModification
            }
        };

        // Act & Assert for each strategy
        var lastWriteResults = service.ResolveConflictsAsync(
            conflicts, ConflictResolutionStrategy.LastWrite).Result;
        lastWriteResults.Should().OnlyContain(r => r.Status == ResolutionStatus.Resolved);
        lastWriteResults.Should().OnlyContain(r => r.ResolutionMethod == ResolutionMethod.LastWrite);
        // LastWrite with equal timestamps should prefer Notion
        lastWriteResults.Should().OnlyContain(r => r.ResolvedValue == r.NotionValue);

        var localWinsResults = service.ResolveConflictsAsync(
            conflicts, ConflictResolutionStrategy.LocalWins).Result;
        localWinsResults.Should().OnlyContain(r => r.Status == ResolutionStatus.Resolved);
        localWinsResults.Should().OnlyContain(r => r.ResolutionMethod == ResolutionMethod.LocalWins);
        // LocalWins should always preserve local value
        localWinsResults.Should().OnlyContain(r => r.ResolvedValue == r.LocalValue);

        var notionWinsResults = service.ResolveConflictsAsync(
            conflicts, ConflictResolutionStrategy.NotionWins).Result;
        notionWinsResults.Should().OnlyContain(r => r.Status == ResolutionStatus.Resolved);
        notionWinsResults.Should().OnlyContain(r => r.ResolutionMethod == ResolutionMethod.NotionWins);
        // NotionWins should always preserve Notion value
        notionWinsResults.Should().OnlyContain(r => r.ResolvedValue == r.NotionValue);

        var manualResults = service.ResolveConflictsAsync(
            conflicts, ConflictResolutionStrategy.Manual).Result;
        manualResults.Should().OnlyContain(r => r.Status == ResolutionStatus.PendingReview);
        manualResults.Should().OnlyContain(r => r.ResolutionMethod == ResolutionMethod.Manual);
    }
}
