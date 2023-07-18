#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace NotionTaskSync.Tests;

using NotionTaskSync.Domain.Models;
using NotionTaskSync.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;
using Task = System.Threading.Tasks.Task;

/// <summary>
/// Unit tests for the conflict resolution UI infrastructure —
/// <see cref="ConflictDiffService"/> and the interactive resolution helpers
/// exercised via <see cref="ConflictResolutionService"/>.
/// </summary>
public class ConflictResolutionUiTests
{
    // -------------------------------------------------------------------------
    // ConflictDiffService — diff generation
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GenerateDiff_WhenValuesAreIdentical_ReportsZeroAddedAndRemoved()
    {
        var service = new ConflictDiffService(NullLogger<ConflictDiffService>.Instance);

        var diff = await service.GenerateDiffForPropertyAsync("hello world", "hello world", "Title");

        diff.IsIdentical.Should().BeTrue();
        diff.AddedCount.Should().Be(0);
        diff.RemovedCount.Should().Be(0);
    }

    [Fact]
    public async Task GenerateDiff_WhenValuesAreDifferent_ReportsCorrectChangeCounts()
    {
        var service = new ConflictDiffService(NullLogger<ConflictDiffService>.Instance);

        var diff = await service.GenerateDiffForPropertyAsync(
            "line one\nline two",
            "line one\nline THREE",
            "Description");

        diff.IsIdentical.Should().BeFalse();
        diff.RemovedCount.Should().BeGreaterThan(0); // "line two" removed
        diff.AddedCount.Should().BeGreaterThan(0);   // "line THREE" added
    }

    [Fact]
    public async Task GenerateDiff_WhenLocalValueIsNull_TreatsItAsEmpty()
    {
        var service = new ConflictDiffService(NullLogger<ConflictDiffService>.Instance);

        var diff = await service.GenerateDiffForPropertyAsync(null, "notion content", "Body");

        diff.AddedCount.Should().BeGreaterThan(0);
        diff.RemovedCount.Should().Be(0);
    }

    [Fact]
    public async Task RenderAsText_ContainsDiffHeaderLines()
    {
        var service = new ConflictDiffService(NullLogger<ConflictDiffService>.Instance);

        var diff = await service.GenerateDiffForPropertyAsync("old value", "new value", "Status");
        var rendered = await service.RenderAsTextAsync(diff);

        rendered.Should().Contain("--- local/Status");
        rendered.Should().Contain("+++ notion/Status");
    }

    [Fact]
    public async Task GenerateBatchDiffs_ReturnsEntryForEachConflict()
    {
        var service = new ConflictDiffService(NullLogger<ConflictDiffService>.Instance);

        var conflicts = new List<ConflictResolution>
        {
            new() { TaskId = Guid.NewGuid(), LocalValue = "a", NotionValue = "b", PropertyName = "Title" },
            new() { TaskId = Guid.NewGuid(), LocalValue = "x", NotionValue = "y", PropertyName = "Status" }
        };

        var results = await service.GenerateBatchDiffsAsync(conflicts);

        results.Should().HaveCount(2);
        results.Keys.Should().Contain(conflicts[0].Id);
        results.Keys.Should().Contain(conflicts[1].Id);
    }

    // -------------------------------------------------------------------------
    // ConflictResolutionService — manual resolution flow (simulates UI action)
    // -------------------------------------------------------------------------

    [Fact]
    public async Task ResolveConflicts_WithLocalWinsStrategy_ResolvesAllWithLocalValue()
    {
        var repoMock = new Mock<Data.Repositories.IChangeLogRepository>();
        repoMock.Setup(r => r.AddAsync(It.IsAny<ChangeLog>())).Returns(Task.CompletedTask);
        var service = new ConflictResolutionService(repoMock.Object);

        var conflicts = new List<ConflictResolution>
        {
            new()
            {
                TaskId = Guid.NewGuid(),
                LocalValue = "local title",
                NotionValue = "notion title",
                PropertyName = "Title"
            },
            new()
            {
                TaskId = Guid.NewGuid(),
                LocalValue = "local desc",
                NotionValue = "notion desc",
                PropertyName = "Description"
            }
        };

        var resolutions = await service.ResolveConflictsAsync(conflicts, ConflictResolutionStrategy.LocalWins);

        resolutions.Should().AllSatisfy(r =>
        {
            r.Status.Should().Be(ResolutionStatus.Resolved);
            r.ResolutionMethod.Should().Be(ResolutionMethod.LocalWins);
            r.ResolvedValue.Should().Be(r.LocalValue);
        });
    }

    [Fact]
    public async Task ResolveConflicts_WithManualStrategy_MarksConflictsForReview()
    {
        var repoMock = new Mock<Data.Repositories.IChangeLogRepository>();
        var service = new ConflictResolutionService(repoMock.Object);

        var conflicts = new List<ConflictResolution>
        {
            new() { TaskId = Guid.NewGuid(), LocalValue = "local", NotionValue = "notion" }
        };

        var resolutions = await service.ResolveConflictsAsync(conflicts, ConflictResolutionStrategy.Manual);

        resolutions[0].Status.Should().Be(ResolutionStatus.PendingReview);
        resolutions[0].IsPending().Should().BeTrue();
    }

    [Fact]
    public async Task ResolveConflicts_PerFieldOverride_AppliesToSpecifiedField()
    {
        var repoMock = new Mock<Data.Repositories.IChangeLogRepository>();
        repoMock.Setup(r => r.AddAsync(It.IsAny<ChangeLog>())).Returns(Task.CompletedTask);
        var service = new ConflictResolutionService(repoMock.Object);

        var titleConflict = new ConflictResolution
        {
            TaskId = Guid.NewGuid(),
            PropertyName = "Title",
            LocalValue = "local title",
            NotionValue = "notion title"
        };
        var statusConflict = new ConflictResolution
        {
            TaskId = Guid.NewGuid(),
            PropertyName = "Status",
            LocalValue = "local status",
            NotionValue = "notion status"
        };

        var fieldStrategies = new Dictionary<string, ConflictResolutionStrategy>
        {
            { "Title", ConflictResolutionStrategy.LocalWins },
            { "Status", ConflictResolutionStrategy.NotionWins }
        };

        var resolutions = await service.ResolveConflictsAsync(
            new List<ConflictResolution> { titleConflict, statusConflict },
            ConflictResolutionStrategy.LastWrite,
            fieldStrategies);

        resolutions[0].ResolutionMethod.Should().Be(ResolutionMethod.LocalWins);
        resolutions[0].ResolvedValue.Should().Be("local title");

        resolutions[1].ResolutionMethod.Should().Be(ResolutionMethod.NotionWins);
        resolutions[1].ResolvedValue.Should().Be("notion status");
    }
}
