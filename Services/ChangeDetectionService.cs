#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace NotionTaskSync.Services;

using NotionTaskSync.Domain.Models;
using NotionTaskSync.Data.Repositories;
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

    public ChangeDetectionService(IChangeLogRepository changeLogRepository)
    {
        _changeLogRepository = changeLogRepository;
    }

    /// <summary>
    /// Detects all changes made to local tasks since a given timestamp.
    /// </summary>
    public List<ChangeLog> DetectLocalChanges(List<Task> tasks, DateTime since)
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
    public List<ChangeLog> DetectNotionChanges(List<NotionPage> pages, DateTime since)
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
    public List<ConflictResolution> DetectConflicts(List<ChangeLog> localChanges, List<ChangeLog> notionChanges)
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
}
