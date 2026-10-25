#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace NotionTaskSync.Data.Repositories;

using NotionTaskSync.Domain.Models;
using System;
using System.Collections.Generic;

/// <summary>
/// Repository interface for Task CRUD operations and queries.
/// Abstracts the underlying storage mechanism for tasks.
/// </summary>
public interface ITaskRepository
{
    /// <summary>
    /// Adds a new task to the repository.
    /// </summary>
    /// <param name="task">The task to add. Must pass <c>Task.Validate()</c>.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="task"/> is <c>null</c>.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the task fails validation.</exception>
    System.Threading.Tasks.Task AddAsync(Task task);

    /// <summary>
    /// Updates an existing task, matched by its <c>Id</c>.
    /// </summary>
    /// <param name="task">The updated task. Must pass <c>Task.Validate()</c>.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="task"/> is <c>null</c>.</exception>
    /// <exception cref="InvalidOperationException">Thrown when no task with the same ID exists or the task fails validation.</exception>
    System.Threading.Tasks.Task UpdateAsync(Task task);

    /// <summary>
    /// Deletes a task by its ID.
    /// </summary>
    /// <param name="taskId">The ID of the task to delete.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <exception cref="InvalidOperationException">Thrown when no task with the given ID exists.</exception>
    System.Threading.Tasks.Task DeleteAsync(Guid taskId);

    /// <summary>
    /// Retrieves a task by its ID. Soft-deleted tasks are not returned.
    /// </summary>
    /// <param name="taskId">The ID of the task to retrieve.</param>
    /// <returns>The matching task, or <c>null</c> if none exists.</returns>
    System.Threading.Tasks.Task<Task?> GetByIdAsync(Guid taskId);

    /// <summary>
    /// Retrieves a task by its Notion page ID. Soft-deleted tasks are not returned.
    /// </summary>
    /// <param name="notionPageId">The Notion page ID to look up.</param>
    /// <returns>The matching task, or <c>null</c> if none exists or <paramref name="notionPageId"/> is null or empty.</returns>
    System.Threading.Tasks.Task<Task?> GetByNotionPageIdAsync(string notionPageId);

    /// <summary>
    /// Retrieves all tasks. Soft-deleted tasks are not returned.
    /// </summary>
    /// <returns>A list of all active tasks; empty if there are none.</returns>
    System.Threading.Tasks.Task<List<Task>> GetAllAsync();

    /// <summary>
    /// Retrieves tasks filtered by status. Soft-deleted tasks are not returned.
    /// </summary>
    /// <param name="status">The status to filter by.</param>
    /// <returns>A list of matching tasks; empty if there are none.</returns>
    System.Threading.Tasks.Task<List<Task>> GetByStatusAsync(TaskStatus status);

    /// <summary>
    /// Retrieves tasks that were modified since a given date. Soft-deleted tasks are not returned.
    /// </summary>
    /// <param name="since">The inclusive lower bound on the task's last update time.</param>
    /// <returns>A list of matching tasks; empty if there are none.</returns>
    System.Threading.Tasks.Task<List<Task>> GetModifiedSinceAsync(DateTime since);

    /// <summary>
    /// Retrieves tasks assigned to a specific person. Soft-deleted tasks are not returned.
    /// </summary>
    /// <param name="assignee">The assignee name to match exactly.</param>
    /// <returns>A list of matching tasks; empty if there are none or <paramref name="assignee"/> is null or empty.</returns>
    System.Threading.Tasks.Task<List<Task>> GetAssignedToAsync(string assignee);

    /// <summary>
    /// Retrieves tasks with a due date before the specified date. Tasks with status <c>Done</c> and soft-deleted tasks are not returned.
    /// </summary>
    /// <param name="beforeDate">The exclusive upper bound on the task's due date.</param>
    /// <returns>A list of matching tasks; empty if there are none.</returns>
    System.Threading.Tasks.Task<List<Task>> GetOverdueAsync(DateTime beforeDate);

    /// <summary>
    /// Saves all pending changes to the repository.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    System.Threading.Tasks.Task SaveAsync();

    /// <summary>
    /// Counts total tasks in the repository. Soft-deleted tasks are not counted.
    /// </summary>
    /// <returns>The number of active tasks in the repository.</returns>
    System.Threading.Tasks.Task<int> CountAsync();

    /// <summary>
    /// Counts tasks by status. Soft-deleted tasks are not counted.
    /// </summary>
    /// <returns>A dictionary mapping each <see cref="TaskStatus"/> present to its number of active tasks.</returns>
    System.Threading.Tasks.Task<Dictionary<TaskStatus, int>> CountByStatusAsync();
}
