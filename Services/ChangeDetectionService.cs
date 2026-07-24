#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace NotionTaskSync.Services;

using NotionTaskSync.Domain.Models;
using NotionTaskSync.Data.Repositories;
using NotionTaskSync.Data.Mappers;
using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Detects changes to tasks in local and Notion sources since last sync.
/// Identifies conflicts when concurrent modifications occur.
/// </summary>
public class ChangeDetectionService
{
    private readonly IChangeLogRepository _changeLogRepository;
    private readonly ISyncCheckpointStore? _checkpointStore;

    /// <summary>
    /// Initializes a new instance of the <see cref="ChangeDetectionService"/> class without
    /// crash-resume protection; <see cref="FilterAlreadyApplied"/> becomes a no-op.
    /// </summary>
    /// <param name="changeLogRepository">Repository used to read persisted change history.</param>
    /// <exception cref="ArgumentNullException"><paramref name="changeLogRepository"/> is <see langword="null"/>.</exception>
    public ChangeDetectionService(IChangeLogRepository changeLogRepository)
        : this(changeLogRepository, checkpointStore: null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ChangeDetectionService"/> class.
    /// </summary>
    /// <param name="changeLogRepository">Repository used to read persisted change history.</param>
    /// <param name="checkpointStore">
    /// Checkpoint store consulted by <see cref="FilterAlreadyApplied"/> to skip items that
    /// were already applied in a prior, possibly crashed, sync cycle. May be <see langword="null"/>
    /// to disable filtering.
    /// </param>
    /// <exception cref="ArgumentNullException"><paramref name="changeLogRepository"/> is <see langword="null"/>.</exception>
    public ChangeDetectionService(IChangeLogRepository changeLogRepository, ISyncCheckpointStore? checkpointStore)
    {
        ArgumentNullException.ThrowIfNull(changeLogRepository);

        _changeLogRepository = changeLogRepository;
        _checkpointStore = checkpointStore;
    }

    /// <summary>
    /// Builds the stable, deterministic key used to identify a change as "already applied"
    /// in the checkpoint store.
    /// </summary>
    /// <param name="change">The change to build a key for.</param>
    /// <returns>A stable string key combining source, task id, change type and timestamp.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="change"/> is <see langword="null"/>.</exception>
    public static string BuildItemKey(ChangeLog change)
    {
        ArgumentNullException.ThrowIfNull(change);
        return $"{change.Source}:{change.TaskId}:{change.ChangeType}:{change.Timestamp:O}";
    }

    /// <summary>
    /// Removes changes that were already applied during a previous, possibly crashed, sync
    /// cycle for the given configuration, as recorded in the checkpoint store. If no
    /// checkpoint store was configured, all changes are returned unfiltered.
    /// </summary>
    /// <param name="changes">The candidate changes detected for the current cycle.</param>
    /// <param name="configId">Identifier of the sync configuration the changes belong to.</param>
    /// <returns>The subset of <paramref name="changes"/> not already marked as applied.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="changes"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="configId"/> is <see cref="Guid.Empty"/>.</exception>
    public virtual List<ChangeLog> FilterAlreadyApplied(List<ChangeLog> changes, Guid configId)
    {
        ArgumentNullException.ThrowIfNull(changes);
        if (configId == Guid.Empty)
            throw new ArgumentException("Configuration id must not be empty.", nameof(configId));

        if (_checkpointStore is null)
            return changes;

        var checkpoint = _checkpointStore.LoadCheckpoint(configId);
        if (checkpoint.AppliedItemKeys.Count == 0)
            return changes;

        return changes.Where(c => !checkpoint.AppliedItemKeys.Contains(BuildItemKey(c))).ToList();
    }

    /// <summary>
    /// Builds the checkpoint item key(s) that <see cref="DetectLocalChanges"/> would produce
    /// for a single local task relative to <paramref name="since"/>, without running full
    /// detection over the whole task list. Intended for callers that apply a task
    /// individually and want to mark it applied for checkpointing purposes right away.
    /// </summary>
    /// <param name="task">The local task that was just applied.</param>
    /// <param name="since">The timestamp changes are being detected relative to.</param>
    /// <returns>Zero, one or two item keys covering the task's creation/update and deletion.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="task"/> is <see langword="null"/>.</exception>
    public static IReadOnlyList<string> BuildLocalItemKeys(Task task, DateTime since)
    {
        ArgumentNullException.ThrowIfNull(task);

        var keys = new List<string>();

        if (task.CreatedAt >= since && task.UpdatedAt == task.CreatedAt)
            keys.Add(BuildItemKey(new ChangeLog { TaskId = task.Id, ChangeType = "Created", Source = ChangeSource.Local, Timestamp = task.CreatedAt }));
        else if (task.UpdatedAt >= since)
            keys.Add(BuildItemKey(new ChangeLog { TaskId = task.Id, ChangeType = "Updated", Source = ChangeSource.Local, Timestamp = task.UpdatedAt }));

        if (task.IsDeleted && task.DeletedAt >= since)
            keys.Add(BuildItemKey(new ChangeLog { TaskId = task.Id, ChangeType = "Deleted", Source = ChangeSource.Local, Timestamp = task.DeletedAt ?? DateTime.UtcNow }));

        return keys;
    }

    /// <summary>
    /// Builds the checkpoint item key(s) that <see cref="DetectNotionChanges"/> would produce
    /// for a single Notion page relative to <paramref name="since"/>, without running full
    /// detection over the whole page list. Intended for callers that apply a page individually
    /// and want to mark it applied for checkpointing purposes right away.
    /// </summary>
    /// <param name="taskId">The local task id the page was mapped to.</param>
    /// <param name="page">The Notion page that was just applied.</param>
    /// <param name="since">The timestamp changes are being detected relative to.</param>
    /// <returns>Zero, one or two item keys covering the page's creation/update and archival.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="page"/> is <see langword="null"/>.</exception>
    public static IReadOnlyList<string> BuildNotionItemKeys(Guid taskId, NotionPage page, DateTime since)
    {
        ArgumentNullException.ThrowIfNull(page);

        var keys = new List<string>();

        if (page.CreatedTime >= since && page.LastEditedTime == page.CreatedTime)
            keys.Add(BuildItemKey(new ChangeLog { TaskId = taskId, ChangeType = "Created", Source = ChangeSource.Notion, Timestamp = page.CreatedTime }));
        else if (page.LastEditedTime >= since)
            keys.Add(BuildItemKey(new ChangeLog { TaskId = taskId, ChangeType = "Updated", Source = ChangeSource.Notion, Timestamp = page.LastEditedTime }));

        if (page.Archived)
            keys.Add(BuildItemKey(new ChangeLog { TaskId = taskId, ChangeType = "Deleted", Source = ChangeSource.Notion, Timestamp = page.LastEditedTime }));

        return keys;
    }

    /// <summary>
    /// Detects all changes made to local tasks since a given timestamp.
    /// </summary>
    public virtual List<ChangeLog> DetectLocalChanges(List<Task> tasks, DateTime since)
    {
        var changes = new List<ChangeLog>();

        foreach (var task in tasks.Where(t => t.UpdatedAt >= since || t.CreatedAt >= since))
        {
            if (task.CreatedAt >= since && task.UpdatedAt == task.CreatedAt)
            {
                // New task created
                changes.Add(new ChangeLog
                {
                    Id = Guid.NewGuid(),
                    TaskId = task.Id,
                    ChangeType = "Created",
                    Source = ChangeSource.Local,
                    Timestamp = task.CreatedAt
                });
            }
            else if (task.UpdatedAt >= since)
            {
                // Task modified
                changes.Add(new ChangeLog
                {
                    Id = Guid.NewGuid(),
                    TaskId = task.Id,
                    ChangeType = "Updated",
                    Source = ChangeSource.Local,
                    Timestamp = task.UpdatedAt,
                    PropertyName = "General"
                });
            }

            if (task.IsDeleted && task.DeletedAt >= since)
            {
                changes.Add(new ChangeLog
                {
                    Id = Guid.NewGuid(),
                    TaskId = task.Id,
                    ChangeType = "Deleted",
                    Source = ChangeSource.Local,
                    Timestamp = task.DeletedAt ?? DateTime.UtcNow
                });
            }
        }

        return changes;
    }

    /// <summary>
    /// Detects all changes made to Notion pages since a given timestamp.
    /// </summary>
    public virtual List<ChangeLog> DetectNotionChanges(List<NotionPage> pages, DateTime since)
    {
        var changes = new List<ChangeLog>();

        foreach (var page in pages.Where(p => p.LastEditedTime >= since || p.CreatedTime >= since))
        {
            if (page.CreatedTime >= since && page.LastEditedTime == page.CreatedTime)
            {
                // New page created
                changes.Add(new ChangeLog
                {
                    Id = Guid.NewGuid(),
                    TaskId = Guid.Empty, // Would be mapped later
                    ChangeType = "Created",
                    Source = ChangeSource.Notion,
                    Timestamp = page.CreatedTime,
                    Description = $"Notion page {page.PageId} created"
                });
            }
            else if (page.LastEditedTime >= since)
            {
                // Page modified
                changes.Add(new ChangeLog
                {
                    Id = Guid.NewGuid(),
                    TaskId = Guid.Empty,
                    ChangeType = "Updated",
                    Source = ChangeSource.Notion,
                    Timestamp = page.LastEditedTime,
                    PropertyName = "General",
                    UserEmail = page.LastEditedBy
                });
            }

            if (page.Archived)
            {
                changes.Add(new ChangeLog
                {
                    Id = Guid.NewGuid(),
                    TaskId = Guid.Empty,
                    ChangeType = "Deleted",
                    Source = ChangeSource.Notion,
                    Timestamp = page.LastEditedTime,
                    Description = $"Notion page {page.PageId} archived"
                });
            }
        }

        return changes;
    }

    /// <summary>
    /// Detects conflicts between local and Notion changes for the same task.
    /// </summary>
    public virtual List<ConflictResolution> DetectConflicts(List<ChangeLog> localChanges, List<ChangeLog> notionChanges)
    {
        var conflicts = new List<ConflictResolution>();
        var conflictWindow = TimeSpan.FromMinutes(5); // Consider changes within 5 minutes as potential conflicts

        // Group changes by TaskId
        var localByTask = localChanges.ToLookup(c => c.TaskId);
        var notionByTask = notionChanges.ToLookup(c => c.TaskId);

        // Find tasks with changes in both sources
        var conflictingTaskIds = localByTask.Select(g => g.Key)
            .Intersect(notionByTask.Select(g => g.Key));

        foreach (var taskId in conflictingTaskIds)
        {
            var localTaskChanges = localByTask[taskId].ToList();
            var notionTaskChanges = notionByTask[taskId].ToList();

            // Check if changes are concurrent (within conflict window)
            foreach (var localChange in localTaskChanges)
            {
                foreach (var notionChange in notionTaskChanges)
                {
                    var timeDiff = Math.Abs((notionChange.Timestamp - localChange.Timestamp).TotalSeconds);

                    if (timeDiff <= conflictWindow.TotalSeconds && localChange.ChangeType == notionChange.ChangeType)
                    {
                        var conflict = new ConflictResolution
                        {
                            Id = Guid.NewGuid(),
                            TaskId = taskId,
                            ConflictType = DetermineConflictType(localChange, notionChange),
                            PropertyName = localChange.PropertyName,
                            LocalValue = localChange.NewValue,
                            NotionValue = notionChange.NewValue,
                            Status = ResolutionStatus.Pending,
                            DetectedAt = DateTime.UtcNow
                        };

                        conflicts.Add(conflict);
                    }
                }
            }
        }

        return conflicts;
    }

    /// <summary>
    /// Determines the type of conflict based on the nature of changes.
    /// </summary>
    private ConflictType DetermineConflictType(ChangeLog localChange, ChangeLog notionChange)
    {
        if (localChange.ChangeType == "Deleted" || notionChange.ChangeType == "Deleted")
            return ConflictType.DeletionConflict;

        if (localChange.PropertyName != notionChange.PropertyName)
            return ConflictType.PropertyMismatch;

        return ConflictType.ConcurrentModification;
    }

    /// <summary>
    /// Gets the change history for a specific task.
    /// </summary>
    public List<ChangeLog> GetTaskChangeHistory(Guid taskId, int limit = 100)
    {
        return _changeLogRepository.GetByTaskIdAsync(taskId, limit).Result;
    }

    /// <summary>
    /// Determines if a task has been modified since a given timestamp.
    /// </summary>
    public bool HasChangedSince(Task task, DateTime since)
    {
        return task.UpdatedAt > since || task.DeletedAt > since;
    }

    /// <summary>
    /// Retrieves the most recent change for a specific task.
    /// </summary>
    public ChangeLog? GetLastChange(Guid taskId)
    {
        var changes = _changeLogRepository.GetByTaskIdAsync(taskId, 1).Result;
        return changes.FirstOrDefault();
    }

    /// <summary>
    /// Compares two Notion property values for semantic equality.
    /// Rich-text fields are normalised to plain text before comparison so that
    /// differing annotation orderings or split text runs do not produce false positives.
    /// </summary>
    public static bool ArePropertyValuesEqual(object? localValue, object? notionValue)
    {
        if (localValue is null && notionValue is null)
            return true;

        if (localValue is null || notionValue is null)
            return false;

        var normalizedLocal = NotionMapper.NormalizeRichTextForComparison(localValue);
        var normalizedNotion = NotionMapper.NormalizeRichTextForComparison(notionValue);

        return string.Equals(normalizedLocal, normalizedNotion, StringComparison.Ordinal);
    }
}
