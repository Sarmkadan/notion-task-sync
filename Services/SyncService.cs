#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace NotionTaskSync.Services;

using NotionTaskSync.Domain.Models;
using NotionTaskSync.Domain.Exceptions;
using NotionTaskSync.Domain.Enums;
using NotionTaskSync.Data.Repositories;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Orchestrates bidirectional sync between Notion and local file system.
/// Coordinates change detection, conflict resolution, and data propagation.
/// </summary>
public sealed class SyncService
{
    private readonly ChangeDetectionService _changeDetectionService;
    private readonly ConflictResolutionService _conflictResolutionService;
    private readonly NotionApiService _notionApiService;
    private readonly ITaskRepository _taskRepository;
    private readonly IChangeLogRepository _changeLogRepository;
    private readonly ILogger<SyncService>? _logger;
    private readonly ISyncCheckpointStore? _checkpointStore;

    /// <summary>
    /// Initializes a new instance of the <see cref="SyncService"/> class.
    /// </summary>
    /// <param name="changeDetectionService">Service used to detect local and Notion changes.</param>
    /// <param name="conflictResolutionService">Service used to resolve concurrent-edit conflicts.</param>
    /// <param name="notionApiService">Client used to read from and write to Notion.</param>
    /// <param name="taskRepository">Repository for locally persisted tasks.</param>
    /// <param name="changeLogRepository">Repository for the change audit trail.</param>
    /// <param name="logger">Optional logger for sync diagnostics.</param>
    /// <param name="checkpointStore">
    /// Optional checkpoint store used to record items as they are applied and to skip
    /// items already applied by a prior cycle that crashed partway through. When omitted,
    /// no crash-resume protection is applied.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Any of <paramref name="changeDetectionService"/>, <paramref name="conflictResolutionService"/>,
    /// <paramref name="notionApiService"/>, <paramref name="taskRepository"/> or
    /// <paramref name="changeLogRepository"/> is <see langword="null"/>.
    /// </exception>
    public SyncService(
        ChangeDetectionService changeDetectionService,
        ConflictResolutionService conflictResolutionService,
        NotionApiService notionApiService,
        ITaskRepository taskRepository,
        IChangeLogRepository changeLogRepository,
        ILogger<SyncService>? logger = null,
        ISyncCheckpointStore? checkpointStore = null)
    {
        ArgumentNullException.ThrowIfNull(changeDetectionService);
        ArgumentNullException.ThrowIfNull(conflictResolutionService);
        ArgumentNullException.ThrowIfNull(notionApiService);
        ArgumentNullException.ThrowIfNull(taskRepository);
        ArgumentNullException.ThrowIfNull(changeLogRepository);

        _changeDetectionService = changeDetectionService;
        _conflictResolutionService = conflictResolutionService;
        _notionApiService = notionApiService;
        _taskRepository = taskRepository;
        _changeLogRepository = changeLogRepository;
        _logger = logger;
        _checkpointStore = checkpointStore;
    }

    /// <summary>
    /// Executes a full bidirectional sync for a given configuration.
    /// Detects changes, resolves conflicts, and propagates updates.
    /// </summary>
    /// <param name="config">The synchronization configuration to apply.</param>
    /// <returns>A <see cref="SyncResult"/> containing the outcome of the sync operation.</returns>
    public async Task<SyncResult> ExecuteSyncAsync(SyncConfig config)
    {
        if (!config.Validate())
            throw new ConfigurationException("Invalid sync configuration provided");

        var result = new SyncResult { ConfigId = config.Id, StartedAt = DateTime.UtcNow };

        try
        {
            // Fetch current state from both sources.
            // When a previous sync timestamp is available use incremental mode:
            // FetchPagesSinceAsync applies a last_edited_time filter so only recently
            // changed pages are retrieved, dramatically reducing API calls for large databases.
            var localTasks = await _taskRepository.GetAllAsync();
            var notionPages = config.LastSyncAt.HasValue
                ? await _notionApiService.FetchPagesSinceAsync(
                    config.NotionDatabaseId, config.LastSyncAt.Value)
                : await _notionApiService.FetchPagesAsync(config.NotionDatabaseId);

            result.LocalTaskCount = localTasks.Count;
            result.NotionPageCount = notionPages.Count;

            // Detect changes since last sync
            var localChanges = _changeDetectionService.DetectLocalChanges(
                localTasks,
                config.LastSyncAt ?? DateTime.MinValue);

            var notionChanges = _changeDetectionService.DetectNotionChanges(
                notionPages,
                config.LastSyncAt ?? DateTime.MinValue);

            // Drop items already applied by a previous cycle that crashed before
            // completing, so they are not pushed/pulled a second time.
            if (_checkpointStore is not null && config.Id != Guid.Empty)
            {
                localChanges = _changeDetectionService.FilterAlreadyApplied(localChanges, config.Id);
                notionChanges = _changeDetectionService.FilterAlreadyApplied(notionChanges, config.Id);
            }

            result.LocalChangesDetected = localChanges.Count;
            result.NotionChangesDetected = notionChanges.Count;

            // Identify conflicts
            var conflicts = _changeDetectionService.DetectConflicts(localChanges, notionChanges);
            result.ConflictsDetected = conflicts.Count;

            if (conflicts.Count > 0)
            {
                // Resolve conflicts using configured strategy and optional per-field overrides
                var resolutions = await _conflictResolutionService.ResolveConflictsAsync(
                    conflicts,
                    config.ConflictStrategy,
                    config.FieldConflictStrategies);

                result.ConflictsResolved = resolutions.Count(r => r.Status == ResolutionStatus.Resolved);
                result.ConflictsPendingReview = resolutions.Count(r => r.Status == ResolutionStatus.PendingReview);
            }

            // Apply changes based on sync direction
            var (created, updated, deleted) = await ApplyChangesAsync(localTasks, notionPages, config);
            result.Created = created;
            result.Updated = updated;
            result.Deleted = deleted;
            result.Unchanged = (result.LocalTaskCount + result.NotionPageCount)
                - (result.LocalChangesDetected + result.NotionChangesDetected);
            if (result.Unchanged < 0) result.Unchanged = 0;

            // Update sync metadata
            config.UpdateSyncStatus();
            await _taskRepository.SaveAsync();

            // The cycle reached the end without crashing: advance the checkpoint's
            // last-successful timestamp and drop the per-item markers accumulated
            // above, since they are now covered by config.LastSyncAt.
            if (_checkpointStore is not null && config.Id != Guid.Empty)
                _checkpointStore.CompleteCycle(config.Id, config.LastSyncAt ?? DateTime.UtcNow);

            result.Status = SyncStatus.Completed;
            result.CompletedAt = DateTime.UtcNow;

            _logger?.LogInformation(result.Summary);
        }
        catch (Exception ex)
        {
            result.Status = SyncStatus.Failed;
            result.CompletedAt = DateTime.UtcNow;
            result.ErrorMessage = ex.Message;
            result.ErrorDetails = ex.StackTrace;
        }

        return result;
    }

    /// <summary>
    /// Applies changes from one source to the other based on sync direction.
    /// Returns counts of (created, updated, deleted) operations performed.
    /// </summary>
    private async Task<(int Created, int Updated, int Deleted)> ApplyChangesAsync(List<Task> localTasks, List<NotionPage> notionPages, SyncConfig config)
    {
        int created = 0, updated = 0, deleted = 0;
        var since = config.LastSyncAt ?? DateTime.MinValue;

        if (config.Direction == SyncDirection.Bidirectional || config.Direction == SyncDirection.LocalToNotion)
        {
            // Push local changes to Notion
            foreach (var task in localTasks.Where(t => t.UpdatedAt > (config.LastSyncAt ?? DateTime.MinValue)))
            {
                var page = notionPages.FirstOrDefault(p => p.PageId == task.NotionPageId);

                if (page is not null)
                {
                    if (task.IsDeleted)
                    {
                        if (!config.IsDryRun)
                        {
                            await _notionApiService.UpdatePageAsync(page.PageId, task);
                            MarkLocalTaskApplied(config.Id, task, since);
                        }
                        _logger?.LogInformation("DRY-RUN: Would archive page {PageId} (task {TaskId})", page.PageId, task.Id);
                        deleted++;
                    }
                    else
                    {
                        if (!config.IsDryRun)
                        {
                            await _notionApiService.UpdatePageAsync(page.PageId, task);
                            MarkLocalTaskApplied(config.Id, task, since);
                        }
                        _logger?.LogInformation("DRY-RUN: Would update page {PageId} with task {TaskId}", page.PageId, task.Id);
                        updated++;
                    }
                }
                else if (task.NotionPageId is null)
                {
                    if (!config.IsDryRun)
                    {
                        var newPage = await _notionApiService.CreatePageAsync(config.NotionDatabaseId, task);

                        if (newPage is not null)
                        {
                            task.NotionPageId = newPage.PageId;
                            await _taskRepository.UpdateAsync(task);
                            MarkLocalTaskApplied(config.Id, task, since);
                        }
                    }
                    _logger?.LogInformation("DRY-RUN: Would create new page in database {DatabaseId} for task {TaskId}", config.NotionDatabaseId, task.Id);
                    created++;
                }
            }
        }

        if (config.Direction == SyncDirection.Bidirectional || config.Direction == SyncDirection.NotionToLocal)
        {
            // Pull Notion changes to local
            foreach (var page in notionPages.Where(p => p.LastEditedTime > (config.LastSyncAt ?? DateTime.MinValue)))
            {
                var task = localTasks.FirstOrDefault(t => t.NotionPageId == page.PageId);

                if (task is not null)
                {
                    UpdateTaskFromPage(task, page);
                    if (!config.IsDryRun)
                    {
                        await _taskRepository.UpdateAsync(task);
                        MarkNotionPageApplied(config.Id, task.Id, page, since);
                    }
                    _logger?.LogInformation("DRY-RUN: Would update local task {TaskId} from page {PageId}", task.Id, page.PageId);
                    if (page.Archived)
                        deleted++;
                    else
                        updated++;
                }
                else
                {
                    var newTask = CreateTaskFromPage(page);
                    if (!config.IsDryRun)
                    {
                        await _taskRepository.AddAsync(newTask);
                        MarkNotionPageApplied(config.Id, newTask.Id, page, since);
                    }
                    _logger?.LogInformation("DRY-RUN: Would create local task {TaskId} from page {PageId}", newTask.Id, page.PageId);
                    created++;
                }
            }
        }

        return (created, updated, deleted);
    }

    /// <summary>
    /// Records a locally-originated push as applied in the checkpoint store, if one is
    /// configured, so a crash later in the cycle does not cause it to be re-pushed on the
    /// next run.
    /// </summary>
    private void MarkLocalTaskApplied(Guid configId, Task task, DateTime since)
    {
        if (_checkpointStore is null || configId == Guid.Empty)
            return;

        foreach (var key in ChangeDetectionService.BuildLocalItemKeys(task, since))
            _checkpointStore.MarkItemApplied(configId, key);
    }

    /// <summary>
    /// Records a Notion-originated pull as applied in the checkpoint store, if one is
    /// configured, so a crash later in the cycle does not cause it to be re-pulled on the
    /// next run.
    /// </summary>
    private void MarkNotionPageApplied(Guid configId, Guid taskId, NotionPage page, DateTime since)
    {
        if (_checkpointStore is null || configId == Guid.Empty)
            return;

        foreach (var key in ChangeDetectionService.BuildNotionItemKeys(taskId, page, since))
            _checkpointStore.MarkItemApplied(configId, key);
    }

    /// <summary>
    /// Updates a local task with data from a Notion page.
    /// </summary>
    private void UpdateTaskFromPage(Task task, NotionPage page)
    {
        task.Title = page.Title;
        task.UpdatedAt = page.LastEditedTime;
        task.IsDeleted = page.Archived;
    }

    /// <summary>
    /// Creates a new local task from a Notion page.
    /// </summary>
    private Task CreateTaskFromPage(NotionPage page)
    {
        return new Task
        {
            Id = Guid.NewGuid(),
            Title = page.Title,
            NotionPageId = page.PageId,
            CreatedAt = page.CreatedTime,
            UpdatedAt = page.LastEditedTime,
            IsDeleted = page.Archived
        };
    }

    /// <summary>
    /// Retrieves sync history for a configuration.
    /// </summary>
    public async Task<List<SyncResult>> GetSyncHistoryAsync(Guid configId, int limit = 50)
    {
        // Implementation would fetch from persistent storage
        return new List<SyncResult>();
    }

    /// <summary>
    /// Contains the results and statistics from a sync operation.
    /// </summary>
    public sealed class SyncResult
    {
        public Guid ConfigId { get; set; }
        public DateTime StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public SyncStatus Status { get; set; }
        public int LocalTaskCount { get; set; }
        public int NotionPageCount { get; set; }
        public int LocalChangesDetected { get; set; }
        public int NotionChangesDetected { get; set; }
        public int ConflictsDetected { get; set; }
        public int ConflictsResolved { get; set; }
        public int ConflictsPendingReview { get; set; }
        public int Created { get; set; }
        public int Updated { get; set; }
        public int Deleted { get; set; }
        public int Unchanged { get; set; }
        public string? ErrorMessage { get; set; }
        public string? ErrorDetails { get; set; }

        public TimeSpan? Duration => CompletedAt.HasValue ? CompletedAt.Value - StartedAt : null;

        /// <summary>
        /// Returns a human-readable summary of the sync cycle results.
        /// Example: "Sync completed: 3 created, 2 updated, 0 deleted, 45 unchanged, 1 conflicted (1.2s)"
        /// </summary>
        public string Summary
        {
            get
            {
                var durationSec = Duration.HasValue ? Duration.Value.TotalSeconds.ToString("F1") : "?";
                return $"Sync completed: {Created} created, {Updated} updated, {Deleted} deleted, " +
                       $"{Unchanged} unchanged, {ConflictsDetected} conflicted ({durationSec}s)";
            }
        }
    }
}
