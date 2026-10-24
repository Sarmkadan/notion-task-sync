#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace NotionTaskSync.IntegrationTests;

using System;
using System.Linq;
using System.Net.Http;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NotionTaskSync.Data.Repositories;
using NotionTaskSync.Domain.Enums;
using NotionTaskSync.Domain.Models;
using NotionTaskSync.IntegrationTests.Support;
using NotionTaskSync.Services;
using Xunit;
using Task = System.Threading.Tasks.Task;

/// <summary>
/// End-to-end tests for the Notion-to-local sync pipeline.
/// Notion is replaced by <see cref="RecordedNotionHttpHandler"/>, which replays recorded
/// database-query responses, so no test touches the network. The local store is the real
/// in-memory <see cref="TaskRepository"/> that <see cref="SyncService"/> writes to.
/// </summary>
public sealed class SyncPipelineTests
{
    private const string DatabaseId = "5f1c0a2e-7b3d-4c8e-9a61-2d4b8e0f7a10";
    private const string QuarterlyReportPageId = "3a7e1c2d-5b4f-4e8a-9c01-6f2d7b8e9a11";
    private const string SyncEnginePageId = "b84d2f10-6c3e-4a97-8d15-0e7f9c2a4b63";
    private const string OnboardingDocsPageId = "c19e5a73-2f8b-4d60-a4e2-7b3c1d9f0e85";

    private const string InitialFixture = "notion-query-initial.json";
    private const string TitleEditedFixture = "notion-query-title-edited.json";
    private const string ArchivedFixture = "notion-query-archived.json";

    // Incremental window for the second and third syncs. UpdateSyncStatus stamps the wall clock,
    // so the test pins LastSyncAt to keep the outcome independent of when the suite runs.
    private static readonly DateTime SyncWindowStart = new(2026, 10, 20, 12, 0, 0, DateTimeKind.Utc);

    private readonly RecordedNotionHttpHandler _notion = new();
    private readonly TaskRepository _localStore = new();
    private readonly SyncService _syncService;
    private readonly SyncConfig _config;

    /// <summary>
    /// Wires the real sync services together, with only the Notion transport replaced.
    /// </summary>
    public SyncPipelineTests()
    {
        var notionApi = new NotionApiService("integration-test-token", new HttpClient(_notion));

        var changeLogRepository = new Mock<IChangeLogRepository>();
        var changeDetection = new ChangeDetectionService(changeLogRepository.Object, checkpointStore: null);
        var conflictResolution = new ConflictResolutionService(changeLogRepository.Object);

        _syncService = new SyncService(
            changeDetection,
            conflictResolution,
            notionApi,
            _localStore,
            changeLogRepository.Object,
            NullLogger<SyncService>.Instance);

        // Pull-only keeps the test on the Notion-to-local path, so no PATCH or POST mocks are needed.
        _config = new SyncConfig("Integration", DatabaseId, Path.GetTempPath())
        {
            Direction = SyncDirection.NotionToLocal,
        };
    }

    /// <summary>
    /// Scenario 1: a first sync of three Notion pages creates three local tasks.
    /// </summary>
    [Fact]
    public async Task InitialSync_CreatesOneLocalTaskPerNotionPage()
    {
        _notion.EnqueueFixture(InitialFixture);

        var result = await _syncService.ExecuteSyncAsync(_config);

        result.Status.Should().Be(SyncStatus.Completed, because: result.ErrorMessage);
        result.Created.Should().Be(3);

        var tasks = await _localStore.GetAllAsync();
        tasks.Should().HaveCount(3);
        tasks.Should().OnlyContain(t => !t.IsDeleted);
        tasks.Select(t => t.NotionPageId).Should().BeEquivalentTo(
            QuarterlyReportPageId, SyncEnginePageId, OnboardingDocsPageId);
        tasks.Select(t => t.Title).Should().BeEquivalentTo(
            "Write quarterly report", "Refactor sync engine", "Update onboarding docs");

        var localByPage = tasks.ToDictionary(t => t.NotionPageId!);
        localByPage[SyncEnginePageId].UpdatedAt.Should().Be(new DateTime(2026, 10, 2, 14, 3, 19, DateTimeKind.Utc));

        _notion.Requests.Should().ContainSingle()
            .Which.Uri.AbsolutePath.Should().Be($"/v1/databases/{DatabaseId}/query");
    }

    /// <summary>
    /// Scenario 2: after one page is edited in Notion, the second sync updates only that task.
    /// </summary>
    [Fact]
    public async Task SecondSync_WithOneEditedPage_UpdatesOnlyThatTask()
    {
        _notion.EnqueueFixture(InitialFixture);
        _notion.EnqueueFixture(TitleEditedFixture);

        var firstResult = await _syncService.ExecuteSyncAsync(_config);
        firstResult.Status.Should().Be(SyncStatus.Completed, because: firstResult.ErrorMessage);

        var before = (await _localStore.GetAllAsync()).ToDictionary(
            t => t.NotionPageId!,
            t => (t.Title, t.UpdatedAt, t.IsDeleted));

        _config.LastSyncAt = SyncWindowStart;
        var secondResult = await _syncService.ExecuteSyncAsync(_config);

        secondResult.Status.Should().Be(SyncStatus.Completed, because: secondResult.ErrorMessage);
        secondResult.Updated.Should().Be(1);
        secondResult.Created.Should().Be(0);
        secondResult.Deleted.Should().Be(0);

        // The second sync must ask Notion only for pages edited since the window start.
        _notion.Requests.Should().HaveCount(2);
        _notion.Requests[1].Body.Should().Contain("last_edited_time").And.Contain("2026-10-20T12:00:00");

        var after = (await _localStore.GetAllAsync()).ToDictionary(t => t.NotionPageId!);
        after.Should().HaveCount(3);

        var edited = after[SyncEnginePageId];
        edited.Title.Should().Be("Refactor sync engine (rev 2)");
        edited.UpdatedAt.Should().Be(new DateTime(2026, 10, 22, 9, 30, 0, DateTimeKind.Utc));

        foreach (var untouchedPageId in new[] { QuarterlyReportPageId, OnboardingDocsPageId })
        {
            var current = after[untouchedPageId];
            (current.Title, current.UpdatedAt, current.IsDeleted)
                .Should().Be(before[untouchedPageId], because: $"page {untouchedPageId} did not change in Notion");
        }
    }

    /// <summary>
    /// Scenario 3: a page archived in Notion is removed from the local task set.
    /// </summary>
    [Fact]
    public async Task ArchivedNotionPage_IsRemovedFromLocalTasks()
    {
        _notion.EnqueueFixture(InitialFixture);
        _notion.EnqueueFixture(ArchivedFixture);

        var firstResult = await _syncService.ExecuteSyncAsync(_config);
        firstResult.Status.Should().Be(SyncStatus.Completed, because: firstResult.ErrorMessage);
        (await _localStore.GetAllAsync()).Should().HaveCount(3);

        _config.LastSyncAt = SyncWindowStart;
        var secondResult = await _syncService.ExecuteSyncAsync(_config);

        secondResult.Status.Should().Be(SyncStatus.Completed, because: secondResult.ErrorMessage);
        secondResult.Deleted.Should().Be(1);

        // SyncService soft-deletes: the task drops out of GetAllAsync but stays in the store flagged as deleted.
        var active = await _localStore.GetAllAsync();
        active.Should().HaveCount(2);
        active.Select(t => t.NotionPageId).Should().NotContain(OnboardingDocsPageId);

        var everything = await _localStore.GetAllIncludingDeletedAsync();
        everything.Single(t => t.NotionPageId == OnboardingDocsPageId).IsDeleted.Should().BeTrue();
    }
}
