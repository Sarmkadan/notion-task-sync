// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace NotionTaskSync.Data.Repositories;

using NotionTaskSync.Domain.Models;
using System;
using System.Collections.Generic;
using System.Threading.Task;

/// <summary>
/// Repository interface for Task CRUD operations and queries.
/// Abstracts the underlying storage mechanism for tasks.
/// </summary>
public interface ITaskRepository
{
    /// <summary>
    /// Adds a new task to the repository.
    /// </summary>
    System.Threading.Tasks.Task AddAsync(Task task);

    /// <summary>
    /// Updates an existing task.
    /// </summary>
    System.Threading.Tasks.Task UpdateAsync(Task task);

    /// <summary>
    /// Deletes a task by its ID.
    /// </summary>
    System.Threading.Tasks.Task DeleteAsync(Guid taskId);

    /// <summary>
    /// Retrieves a task by its ID.
    /// </summary>
    System.Threading.Tasks.Task<Task?> GetByIdAsync(Guid taskId);

    /// <summary>
    /// Retrieves a task by its Notion page ID.
    /// </summary>
    System.Threading.Tasks.Task<Task?> GetByNotionPageIdAsync(string notionPageId);

    /// <summary>
    /// Retrieves all tasks.
    /// </summary>
    System.Threading.Tasks.Task<List<Task>> GetAllAsync();

    /// <summary>
    /// Retrieves tasks filtered by status.
    /// </summary>
    System.Threading.Tasks.Task<List<Task>> GetByStatusAsync(TaskStatus status);

    /// <summary>
    /// Retrieves tasks that were modified since a given date.
    /// </summary>
    System.Threading.Tasks.Task<List<Task>> GetModifiedSinceAsync(DateTime since);

    /// <summary>
    /// Retrieves tasks assigned to a specific person.
    /// </summary>
    System.Threading.Tasks.Task<List<Task>> GetAssignedToAsync(string assignee);

    /// <summary>
    /// Retrieves tasks with a due date before the specified date.
    /// </summary>
    System.Threading.Tasks.Task<List<Task>> GetOverdueAsync(DateTime beforeDate);

    /// <summary>
    /// Saves all pending changes to the repository.
    /// </summary>
    System.Threading.Tasks.Task SaveAsync();

    /// <summary>
    /// Counts total tasks in the repository.
    /// </summary>
    System.Threading.Tasks.Task<int> CountAsync();

    /// <summary>
    /// Counts tasks by status.
    /// </summary>
    System.Threading.Tasks.Task<Dictionary<TaskStatus, int>> CountByStatusAsync();
}
