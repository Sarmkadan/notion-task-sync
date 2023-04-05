// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace NotionTaskSync.Services;

using NotionTaskSync.Domain.Models;
using NotionTaskSync.Domain.Exceptions;
using NotionTaskSync.Domain.Enums;
using NotionTaskSync.Data.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Orchestrates bidirectional sync between Notion and local file system.
/// Coordinates change detection, conflict resolution, and data propagation.
/// </summary>
public class SyncService
{
    private readonly ChangeDetectionService _changeDetectionService;
    private readonly ConflictResolutionService _conflictResolutionService;
    private readonly NotionApiService _notionApiService;
    private readonly ITaskRepository _taskRepository;
    private readonly IChangeLogRepository _changeLogRepository;

    public SyncService(
        ChangeDetectionService changeDetectionService,
        ConflictResolutionService conflictResolutionService,
        NotionApiService notionApiService,
        ITaskRepository taskRepository,
        IChangeLogRepository changeLogRepository)
    {
        _changeDetectionService = changeDetectionService;
        _conflictResolutionService = conflictResolutionService;
        _notionApiService = notionApiService;
        _taskRepository = taskRepository;
        _changeLogRepository = changeLogRepository;
    }

    /// <summary>
    /// Executes a full bidirectional sync for a given configuration.
    /// Detects changes, resolves conflicts, and propagates updates.
    /// </summary>
    public async Task<SyncResult> ExecuteSyncAsync(SyncConfig config)
    {
        if (!config.Validate())
            throw new ConfigurationException("Invalid sync configuration provided");

        var result = new SyncResult { ConfigId = config.Id, StartedAt = DateTime.UtcNow };

        try
        {
            // Fetch current state from both sources
            var localTasks = await _taskRepository.GetAllAsync();
            var notionPages = await _notionApiService.FetchPagesAsync(config.NotionDatabaseId);

            result.LocalTaskCount = localTasks.Count;
            result.NotionPageCount = notionPages.Count;

            // Detect changes since last sync
            var localChanges = _changeDetectionService.DetectLocalChanges(
                localTasks,
                config.LastSyncAt ?? DateTime.MinValue);

            var notionChanges = _changeDetectionService.DetectNotionChanges(
                notionPages,
                config.LastSyncAt ?? DateTime.MinValue);

            result.LocalChangesDetected = localChanges.Count;
            result.NotionChangesDetected = notionChanges.Count;

            // Identify conflicts
            var conflicts = _changeDetectionService.DetectConflicts(localChanges, notionChanges);
            result.ConflictsDetected = conflicts.Count;

            if (conflicts.Count > 0)
            {
                // Resolve conflicts using configured strategy
                var resolutions = await _conflictResolutionService.ResolveConflictsAsync(
                    conflicts,
                    config.ConflictStrategy);

                result.ConflictsResolved = resolutions.Count(r => r.Status == ResolutionStatus.Resolved);
                result.ConflictsPendingReview = resolutions.Count(r => r.Status == ResolutionStatus.PendingReview);
            }

            // Apply changes based on sync direction
            await ApplyChangesAsync(localTasks, notionPages, config);

            // Update sync metadata
            config.UpdateSyncStatus();
            await _taskRepository.SaveAsync();

            result.Status = SyncStatus.Completed;
            result.CompletedAt = DateTime.UtcNow;
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
    /// </summary>
    private async global::System.Threading.Tasks.Task ApplyChangesAsync(List<Task> localTasks, List<NotionPage> notionPages, SyncConfig config)
    {
        if (config.Direction == SyncDirection.Bidirectional || config.Direction == SyncDirection.LocalToNotion)
        {
            // Push local changes to Notion
            foreach (var task in localTasks.Where(t => t.UpdatedAt > (config.LastSyncAt ?? DateTime.MinValue)))
            {
                var page = notionPages.FirstOrDefault(p => p.PageId == task.NotionPageId);

                if (page != null)
                {
                    await _notionApiService.UpdatePageAsync(page.PageId, task);
                }
                else if (task.NotionPageId == null)
                {
                    var newPage = await _notionApiService.CreatePageAsync(config.NotionDatabaseId, task);
                    task.NotionPageId = newPage.PageId;
                    await _taskRepository.UpdateAsync(task);
                }
            }
        }

        if (config.Direction == SyncDirection.Bidirectional || config.Direction == SyncDirection.NotionToLocal)
        {
            // Pull Notion changes to local
            foreach (var page in notionPages.Where(p => p.LastEditedTime > (config.LastSyncAt ?? DateTime.MinValue)))
            {
                var task = localTasks.FirstOrDefault(t => t.NotionPageId == page.PageId);

                if (task != null)
                {
                    UpdateTaskFromPage(task, page);
                    await _taskRepository.UpdateAsync(task);
                }
                else
                {
                    var newTask = CreateTaskFromPage(page);
                    await _taskRepository.AddAsync(newTask);
                }
            }
        }
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
    public class SyncResult
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
        public string? ErrorMessage { get; set; }
        public string? ErrorDetails { get; set; }

        public TimeSpan? Duration => CompletedAt.HasValue ? CompletedAt.Value - StartedAt : null;
    }
}
