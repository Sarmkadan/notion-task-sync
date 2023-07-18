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
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;
using Task = System.Threading.Tasks.Task;
using DomainTask = NotionTaskSync.Domain.Models.Task;
using TaskStatus = NotionTaskSync.Domain.Models.TaskStatus;

/// <summary>
/// Unit tests for <see cref="BulkOperationService"/>.
/// </summary>
public class BulkOperationServiceTests
{
    private static (Mock<ITaskRepository> mock, BulkOperationService service) Build(
        params Domain.Models.Task[] tasks)
    {
        var mock = new Mock<ITaskRepository>();

        foreach (var t in tasks)
        {
            var captured = t;
            mock.Setup(r => r.GetByIdAsync(captured.Id))
                .ReturnsAsync(captured);
        }

        mock.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Domain.Models.Task>(tasks));
        mock.Setup(r => r.UpdateAsync(It.IsAny<Domain.Models.Task>())).Returns(Task.CompletedTask);
        mock.Setup(r => r.AddAsync(It.IsAny<Domain.Models.Task>())).Returns(Task.CompletedTask);
        mock.Setup(r => r.SaveAsync()).Returns(Task.CompletedTask);

        var logger = Microsoft.Extensions.Logging.Abstractions.NullLogger<BulkOperationService>.Instance;
        var service = new BulkOperationService(mock.Object, logger);
        return (mock, service);
    }

    // -------------------------------------------------------------------------
    // UpdateStatus
    // -------------------------------------------------------------------------

    [Fact]
    public async Task UpdateStatus_SetsStatusOnAllMatchedTasks()
    {
        var t1 = new Domain.Models.Task { Id = Guid.NewGuid(), Title = "A", Status = TaskStatus.Todo };
        var t2 = new Domain.Models.Task { Id = Guid.NewGuid(), Title = "B", Status = TaskStatus.Todo };
        var (_, service) = Build(t1, t2);

        var result = await service.UpdateStatusAsync(new[] { t1.Id, t2.Id }, TaskStatus.Done);

        result.Affected.Should().Be(2);
        result.Skipped.Should().Be(0);
        t1.Status.Should().Be(TaskStatus.Done);
        t2.Status.Should().Be(TaskStatus.Done);
        t1.CompletedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task UpdateStatus_SkipsMissingTasks()
    {
        var t1 = new Domain.Models.Task { Id = Guid.NewGuid(), Title = "A" };
        var (_, service) = Build(t1);

        var missingId = Guid.NewGuid();
        var result = await service.UpdateStatusAsync(new[] { t1.Id, missingId }, TaskStatus.Blocked);

        result.Affected.Should().Be(1);
        result.Skipped.Should().Be(1);
    }

    // -------------------------------------------------------------------------
    // AddTag / RemoveTag
    // -------------------------------------------------------------------------

    [Fact]
    public async Task AddTag_AppendTagToTasksWithoutDuplicates()
    {
        var task = new Domain.Models.Task { Id = Guid.NewGuid(), Title = "T", Tags = "existing" };
        var (_, service) = Build(task);

        var result = await service.AddTagAsync(new[] { task.Id }, "urgent");

        result.Affected.Should().Be(1);
        task.Tags.Should().Contain("urgent");
        task.Tags.Should().Contain("existing");
    }

    [Fact]
    public async Task AddTag_SkipsTaskAlreadyHavingTag()
    {
        var task = new Domain.Models.Task { Id = Guid.NewGuid(), Title = "T", Tags = "urgent" };
        var (_, service) = Build(task);

        var result = await service.AddTagAsync(new[] { task.Id }, "urgent");

        result.Skipped.Should().Be(1);
        result.Affected.Should().Be(0);
    }

    [Fact]
    public async Task RemoveTag_RemovesTagFromTask()
    {
        var task = new Domain.Models.Task { Id = Guid.NewGuid(), Title = "T", Tags = "bug,urgent" };
        var (_, service) = Build(task);

        var result = await service.RemoveTagAsync(new[] { task.Id }, "bug");

        result.Affected.Should().Be(1);
        task.Tags.Should().NotContain("bug");
        task.Tags.Should().Contain("urgent");
    }

    // -------------------------------------------------------------------------
    // Assign
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Assign_SetsAssigneeProperly()
    {
        var task = new Domain.Models.Task { Id = Guid.NewGuid(), Title = "T" };
        var (_, service) = Build(task);

        var result = await service.AssignAsync(new[] { task.Id }, "alice@example.com");

        result.Affected.Should().Be(1);
        task.AssignedTo.Should().Be("alice@example.com");
    }

    [Fact]
    public async Task Assign_ClearsAssigneeWhenEmptyStringProvided()
    {
        var task = new Domain.Models.Task { Id = Guid.NewGuid(), Title = "T", AssignedTo = "bob" };
        var (_, service) = Build(task);

        await service.AssignAsync(new[] { task.Id }, "");

        task.AssignedTo.Should().BeNull();
    }

    // -------------------------------------------------------------------------
    // SetPriority
    // -------------------------------------------------------------------------

    [Fact]
    public async Task SetPriority_UpdatesPriorityOnAllTasks()
    {
        var t1 = new Domain.Models.Task { Id = Guid.NewGuid(), Title = "T1", Priority = 10 };
        var t2 = new Domain.Models.Task { Id = Guid.NewGuid(), Title = "T2", Priority = 20 };
        var (_, service) = Build(t1, t2);

        var result = await service.SetPriorityAsync(new[] { t1.Id, t2.Id }, 90);

        result.Affected.Should().Be(2);
        t1.Priority.Should().Be(90);
        t2.Priority.Should().Be(90);
    }

    [Fact]
    public void SetPriority_ThrowsForOutOfRangePriority()
    {
        var task = new Domain.Models.Task { Id = Guid.NewGuid(), Title = "T" };
        var (_, service) = Build(task);

        Func<Task> act = () => service.SetPriorityAsync(new[] { task.Id }, 200);
        act.Should().ThrowAsync<ArgumentOutOfRangeException>();
    }

    // -------------------------------------------------------------------------
    // Delete
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Delete_SoftDeletesAllMatchedTasks()
    {
        var t1 = new Domain.Models.Task { Id = Guid.NewGuid(), Title = "T1" };
        var t2 = new Domain.Models.Task { Id = Guid.NewGuid(), Title = "T2" };
        var (_, service) = Build(t1, t2);

        var result = await service.DeleteAsync(new[] { t1.Id, t2.Id });

        result.Affected.Should().Be(2);
        t1.IsDeleted.Should().BeTrue();
        t2.IsDeleted.Should().BeTrue();
        t1.DeletedAt.Should().NotBeNull();
    }

    // -------------------------------------------------------------------------
    // Query
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Query_ReturnsOnlyMatchingNonDeletedTasks()
    {
        var tasks = new[]
        {
            new Domain.Models.Task { Id = Guid.NewGuid(), Title = "A", Status = TaskStatus.Done },
            new Domain.Models.Task { Id = Guid.NewGuid(), Title = "B", Status = TaskStatus.Todo },
            new Domain.Models.Task { Id = Guid.NewGuid(), Title = "C", Status = TaskStatus.Done, IsDeleted = true }
        };
        var (_, service) = Build(tasks);

        var result = await service.QueryAsync(t => t.Status == TaskStatus.Done);

        result.Should().HaveCount(1);
        result[0].Title.Should().Be("A");
    }
}
