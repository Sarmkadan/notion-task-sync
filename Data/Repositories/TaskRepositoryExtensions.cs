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
/// Extension methods for <see cref="TaskRepository"/> providing additional convenience operations for querying tasks.
/// </summary>
public static class TaskRepositoryExtensions
{
    /// <summary>
    /// Gets all tasks that are due within a specified time window.
    /// </summary>
    /// <param name="repository">The task repository.</param>
    /// <param name="fromDate">The start date of the window (inclusive).</param>
    /// <param name="toDate">The end date of the window (inclusive).</param>
    /// <returns>A list of tasks due within the specified window.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="repository"/> is <see langword="null"/>.</exception>
    public static async Task<List<Task>> GetDueWithinAsync(this TaskRepository repository, DateTime fromDate, DateTime toDate)
    {
        ArgumentNullException.ThrowIfNull(repository);

        var allTasks = await repository.GetAllAsync();
        return allTasks
            .Where(t => t.DueDate.HasValue
                && t.DueDate.Value >= fromDate
                && t.DueDate.Value <= toDate
                && t.Status != TaskStatus.Done)
            .ToList();
    }

    /// <summary>
    /// Gets all tasks assigned to a specific user that are overdue.
    /// </summary>
    /// <param name="repository">The task repository.</param>
    /// <param name="assignee">The user to filter by.</param>
    /// <param name="currentDate">The current date for overdue calculation.</param>
    /// <returns>A list of overdue tasks for the specified assignee.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="repository"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="assignee"/> is <see langword="null"/>, empty, or consists only of whitespace.</exception>
    public static async Task<List<Task>> GetAssignedOverdueAsync(this TaskRepository repository, string assignee, DateTime currentDate)
    {
        ArgumentNullException.ThrowIfNull(repository);
        ArgumentException.ThrowIfNullOrWhiteSpace(assignee);

        var assignedTasks = await repository.GetAssignedToAsync(assignee);
        return assignedTasks
            .Where(t => t.DueDate.HasValue
                && t.DueDate.Value < currentDate
                && t.Status != TaskStatus.Done)
            .ToList();
    }

    /// <summary>
    /// Gets all tasks with a specific priority level.
    /// </summary>
    /// <param name="repository">The task repository.</param>
    /// <param name="priority">The priority level to filter by (0-100).</param>
    /// <returns>A list of tasks with the specified priority.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="repository"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="priority"/> is not between 0 and 100.</exception>
    public static async Task<List<Task>> GetByPriorityAsync(this TaskRepository repository, int priority)
    {
        ArgumentNullException.ThrowIfNull(repository);

        if (priority is < 0 or > 100)
        {
            throw new ArgumentOutOfRangeException(nameof(priority), priority, "Priority must be between 0 and 100");
        }

        var allTasks = await repository.GetAllAsync();
        return allTasks
            .Where(t => t.Priority == priority)
            .ToList();
    }

    /// <summary>
    /// Gets all tasks that match the specified predicate.
    /// </summary>
    /// <param name="repository">The task repository.</param>
    /// <param name="predicate">The predicate to filter tasks.</param>
    /// <returns>A list of tasks matching the predicate criteria.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="repository"/> or <paramref name="predicate"/> is <see langword="null"/>.</exception>
    public static async Task<List<Task>> GetWhereAsync(this TaskRepository repository, Func<Task, bool> predicate)
    {
        ArgumentNullException.ThrowIfNull(repository);
        ArgumentNullException.ThrowIfNull(predicate);

        var allTasks = await repository.GetAllAsync();
        return allTasks
            .Where(predicate)
            .ToList();
    }
}