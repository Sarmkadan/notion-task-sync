#nullable enable
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
/// Extension methods for TaskRepository providing additional convenience operations.
/// </summary>
public static class TaskRepositoryExtensions
{
    /// <summary>
    /// Gets all tasks that are due within a specified time window.
    /// </summary>
    /// <param name="repository">The task repository.</param>
    /// <param name="fromDate">The start date of the window.</param>
    /// <param name="toDate">The end date of the window.</param>
    /// <returns>A list of tasks due within the specified window.</returns>
    public static async Task<List<Task>> GetDueWithinAsync(this TaskRepository repository, DateTime fromDate, DateTime toDate)
    {
        if (repository is null)
            throw new ArgumentNullException(nameof(repository));

        return await System.Threading.Tasks.Task.FromResult(
            (await repository.GetAllAsync())
                .Where(t =>
                    t.DueDate.HasValue &&
                    t.DueDate.Value >= fromDate &&
                    t.DueDate.Value <= toDate &&
                    t.Status != TaskStatus.Done)
                .ToList());
    }

    /// <summary>
    /// Gets all tasks assigned to a specific user that are overdue.
    /// </summary>
    /// <param name="repository">The task repository.</param>
    /// <param name="assignee">The user to filter by.</param>
    /// <param name="currentDate">The current date for overdue calculation.</param>
    /// <returns>A list of overdue tasks for the specified assignee.</returns>
    public static async Task<List<Task>> GetAssignedOverdueAsync(this TaskRepository repository, string assignee, DateTime currentDate)
    {
        if (repository is null)
            throw new ArgumentNullException(nameof(repository));

        if (string.IsNullOrEmpty(assignee))
            return new List<Task>();

        return await System.Threading.Tasks.Task.FromResult(
            (await repository.GetAssignedToAsync(assignee))
                .Where(t =>
                    t.DueDate.HasValue &&
                    t.DueDate.Value < currentDate &&
                    t.Status != TaskStatus.Done)
                .ToList());
    }

    /// <summary>
    /// Gets all tasks with a specific priority level.
    /// </summary>
    /// <param name="repository">The task repository.</param>
    /// <param name="priority">The priority level to filter by (0-100).</param>
    /// <returns>A list of tasks with the specified priority.</returns>
    public static async Task<List<Task>> GetByPriorityAsync(this TaskRepository repository, int priority)
    {
        if (repository is null)
            throw new ArgumentNullException(nameof(repository));

        if (priority < 0 || priority > 100)
            throw new ArgumentOutOfRangeException(nameof(priority), "Priority must be between 0 and 100");

        return await System.Threading.Tasks.Task.FromResult(
            (await repository.GetAllAsync())
                .Where(t => t.Priority == priority)
                .ToList());
    }

    /// <summary>
    /// Gets all tasks that match multiple criteria using a predicate builder pattern.
    /// </summary>
    /// <param name="repository">The task repository.</param>
    /// <param name="predicate">The predicate to filter tasks.</param>
    /// <returns>A list of tasks matching the predicate criteria.</returns>
    public static async Task<List<Task>> GetWhereAsync(this TaskRepository repository, Func<Task, bool> predicate)
    {
        if (repository is null)
            throw new ArgumentNullException(nameof(repository));

        if (predicate is null)
            throw new ArgumentNullException(nameof(predicate));

        return await System.Threading.Tasks.Task.FromResult(
            (await repository.GetAllAsync())
                .Where(predicate)
                .ToList());
    }
}