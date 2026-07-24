#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace NotionTaskSync.Tests;

using NotionTaskSync.Domain.Models;
using NotionTaskSync.Services;
using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.IO;
using Xunit;

/// <summary>
/// Tests for <see cref="SyncCheckpointStore"/> and its use by <see cref="ChangeDetectionService"/>
/// to avoid double-applying items after a crashed sync cycle.
/// </summary>
public sealed class SyncCheckpointStoreTests : IDisposable
{
    private readonly string _directory;
    private readonly SyncCheckpointStore _store;

    public SyncCheckpointStoreTests()
    {
        _directory = Path.Combine(Path.GetTempPath(), "sync-checkpoint-tests-" + Guid.NewGuid().ToString("N"));
        _store = new SyncCheckpointStore(_directory);
    }

    public void Dispose()
    {
        if (Directory.Exists(_directory))
            Directory.Delete(_directory, recursive: true);
    }

    /// <summary>
    /// Simulates a process crash partway through applying a batch of items: only some items
    /// call <see cref="ISyncCheckpointStore.MarkItemApplied"/> before the "crash", and
    /// <see cref="ISyncCheckpointStore.CompleteCycle"/> is never reached. A fresh store
    /// instance opened afterwards - standing in for the next process run - must still see
    /// exactly the items that were marked before the crash.
    /// </summary>
    [Fact]
    public void MarkItemApplied_ProcessCrashesMidApply_SurvivingMarkersAreVisibleOnNextRun()
    {
        // Arrange
        var configId = Guid.NewGuid();
        var itemsToApply = new[] { "item-1", "item-2", "item-3", "item-4" };

        // Act: apply the first two items and record them, then simulate a crash by
        // simply never calling CompleteCycle and discarding the in-memory store instance.
        _store.MarkItemApplied(configId, itemsToApply[0]);
        _store.MarkItemApplied(configId, itemsToApply[1]);
        // Crash occurs here, before items 3 and 4 are applied or the cycle is completed.

        // A new process starting up would construct a brand new store over the same directory.
        var storeAfterCrash = new SyncCheckpointStore(_directory);
        var checkpoint = storeAfterCrash.LoadCheckpoint(configId);

        // Assert
        checkpoint.LastSuccessfulSyncAt.Should().BeNull("the cycle never completed");
        checkpoint.AppliedItemKeys.Should().BeEquivalentTo(new[] { itemsToApply[0], itemsToApply[1] });
        checkpoint.AppliedItemKeys.Should().NotContain(itemsToApply[2]);
        checkpoint.AppliedItemKeys.Should().NotContain(itemsToApply[3]);
    }

    /// <summary>
    /// After a crash mid-cycle, <see cref="ChangeDetectionService.FilterAlreadyApplied"/> must
    /// drop the changes that were already applied before the crash, so a retried cycle only
    /// re-attempts the items that never made it through.
    /// </summary>
    [Fact]
    public void FilterAlreadyApplied_AfterSimulatedCrash_SkipsItemsAppliedBeforeCrash()
    {
        // Arrange: two local changes detected for the current cycle.
        var configId = Guid.NewGuid();
        var taskIdApplied = Guid.NewGuid();
        var taskIdPending = Guid.NewGuid();
        var timestamp = DateTime.UtcNow;

        var appliedChange = new ChangeLog
        {
            Id = Guid.NewGuid(),
            TaskId = taskIdApplied,
            ChangeType = "Updated",
            Source = ChangeSource.Local,
            Timestamp = timestamp
        };
        var pendingChange = new ChangeLog
        {
            Id = Guid.NewGuid(),
            TaskId = taskIdPending,
            ChangeType = "Updated",
            Source = ChangeSource.Local,
            Timestamp = timestamp
        };

        // Simulate the previous cycle: it pushed `appliedChange` and recorded it, then
        // crashed before pushing `pendingChange` or calling CompleteCycle.
        _store.MarkItemApplied(configId, ChangeDetectionService.BuildItemKey(appliedChange));

        var changeLogRepository = new Mock<NotionTaskSync.Data.Repositories.IChangeLogRepository>();
        var service = new ChangeDetectionService(changeLogRepository.Object, _store);

        // Act: the retried cycle re-detects both changes (since nothing advanced LastSyncAt).
        var detected = new List<ChangeLog> { appliedChange, pendingChange };
        var remaining = service.FilterAlreadyApplied(detected, configId);

        // Assert: only the change that never got applied before the crash remains.
        remaining.Should().ContainSingle();
        remaining[0].TaskId.Should().Be(taskIdPending);
    }

    /// <summary>
    /// A successful, non-crashed cycle clears the per-item markers and advances the
    /// last-successful-sync timestamp so they are not needed on the next incremental run.
    /// </summary>
    [Fact]
    public void CompleteCycle_ClearsAppliedMarkersAndAdvancesTimestamp()
    {
        // Arrange
        var configId = Guid.NewGuid();
        _store.MarkItemApplied(configId, "item-1");
        var completedAt = DateTime.UtcNow;

        // Act
        _store.CompleteCycle(configId, completedAt);
        var checkpoint = _store.LoadCheckpoint(configId);

        // Assert
        checkpoint.LastSuccessfulSyncAt.Should().Be(completedAt);
        checkpoint.AppliedItemKeys.Should().BeEmpty();
    }
}
