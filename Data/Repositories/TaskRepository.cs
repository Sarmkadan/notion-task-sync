// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace NotionTaskSync.Data.Repositories;

using NotionTaskSync.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// In-memory implementation of ITaskRepository for managing task entities.
/// In production, this would be replaced with a database-backed implementation.
/// </summary>
public class TaskRepository : ITaskRepository
{
    private readonly List<Task> _tasks;
    private bool _hasChanges;

    public TaskRepository()
    {
        _tasks = new List<Task>();
        _hasChanges = false;
    }

    public async global::System.Threading.Tasks.Task AddAsync(Task task)
    {
        if (task == null)
            throw new ArgumentNullException(nameof(task));

        if (!task.Validate())
            throw new InvalidOperationException("Task validation failed");

        _tasks.Add(task);
        _hasChanges = true;

        await System.Threading.Tasks.Task.CompletedTask;
    }

    public async global::System.Threading.Tasks.Task UpdateAsync(Task task)
    {
        if (task == null)
            throw new ArgumentNullException(nameof(task));

        var existing = _tasks.FirstOrDefault(t => t.Id == task.Id);

        if (existing == null)
            throw new InvalidOperationException($"Task with ID {task.Id} not found");

        if (!task.Validate())
            throw new InvalidOperationException("Task validation failed");

        // Update the existing task
        var index = _tasks.IndexOf(existing);
        _tasks[index] = task;
        _hasChanges = true;

        await System.Threading.Tasks.Task.CompletedTask;
    }

    public async global::System.Threading.Tasks.Task DeleteAsync(Guid taskId)
    {
        var task = _tasks.FirstOrDefault(t => t.Id == taskId);

        if (task == null)
            throw new InvalidOperationException($"Task with ID {taskId} not found");

        _tasks.Remove(task);
        _hasChanges = true;

        await System.Threading.Tasks.Task.CompletedTask;
    }

    public async Task<Task?> GetByIdAsync(Guid taskId)
    {
        return await System.Threading.Tasks.Task.FromResult(
            _tasks.FirstOrDefault(t => t.Id == taskId && !t.IsDeleted));
    }

    public async Task<Task?> GetByNotionPageIdAsync(string notionPageId)
    {
        if (string.IsNullOrEmpty(notionPageId))
            return null;

        return await System.Threading.Tasks.Task.FromResult(
            _tasks.FirstOrDefault(t => t.NotionPageId == notionPageId && !t.IsDeleted));
    }

    public async Task<List<Task>> GetAllAsync()
    {
        return await System.Threading.Tasks.Task.FromResult(
            _tasks.Where(t => !t.IsDeleted).ToList());
    }

    public async Task<List<Task>> GetByStatusAsync(TaskStatus status)
    {
        return await System.Threading.Tasks.Task.FromResult(
            _tasks.Where(t => t.Status == status && !t.IsDeleted).ToList());
    }

    public async Task<List<Task>> GetModifiedSinceAsync(DateTime since)
    {
        return await System.Threading.Tasks.Task.FromResult(
            _tasks.Where(t => t.UpdatedAt >= since && !t.IsDeleted).ToList());
    }

    public async Task<List<Task>> GetAssignedToAsync(string assignee)
    {
        if (string.IsNullOrEmpty(assignee))
            return new List<Task>();

        return await System.Threading.Tasks.Task.FromResult(
            _tasks.Where(t => t.AssignedTo == assignee && !t.IsDeleted).ToList());
    }

    public async Task<List<Task>> GetOverdueAsync(DateTime beforeDate)
    {
        return await System.Threading.Tasks.Task.FromResult(
            _tasks.Where(t =>
                t.DueDate.HasValue &&
                t.DueDate.Value < beforeDate &&
                t.Status != TaskStatus.Done &&
                !t.IsDeleted)
            .ToList());
    }

    public async global::System.Threading.Tasks.Task SaveAsync()
    {
        // In-memory implementation: persist to file or database here
        // For now, just reset the change flag
        _hasChanges = false;

        await System.Threading.Tasks.Task.CompletedTask;
    }

    public async Task<int> CountAsync()
    {
        return await System.Threading.Tasks.Task.FromResult(
            _tasks.Count(t => !t.IsDeleted));
    }

    public async Task<Dictionary<TaskStatus, int>> CountByStatusAsync()
    {
        var counts = _tasks
            .Where(t => !t.IsDeleted)
            .GroupBy(t => t.Status)
            .ToDictionary(g => g.Key, g => g.Count());

        return await System.Threading.Tasks.Task.FromResult(counts);
    }

    /// <summary>
    /// Gets all tasks including deleted ones (for administrative purposes).
    /// </summary>
    public async Task<List<Task>> GetAllIncludingDeletedAsync()
    {
        return await System.Threading.Tasks.Task.FromResult(_tasks.ToList());
    }

    /// <summary>
    /// Determines if there are any unsaved changes.
    /// </summary>
    public bool HasPendingChanges => _hasChanges;
}
