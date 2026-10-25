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
/// Repository interface for ChangeLog CRUD operations and audit queries.
/// Manages the audit trail of all sync-related changes.
/// </summary>
/// <remarks>
/// Query methods that return lists order results newest first by <see cref="ChangeLog.Timestamp"/>.
/// </remarks>
public interface IChangeLogRepository
{
    /// <summary>
    /// Adds a new change log entry.
    /// </summary>
    /// <param name="changeLog">The change log entry to add. Must not be null and must pass <see cref="ChangeLog.Validate"/>.</param>
    /// <returns>A task that completes when the entry has been added.</returns>
    global::System.Threading.Tasks.Task AddAsync(ChangeLog changeLog);

    /// <summary>
    /// Retrieves change logs for a specific task.
    /// </summary>
    /// <param name="taskId">The identifier of the task whose change logs are requested.</param>
    /// <param name="limit">The maximum number of entries to return. Defaults to 100.</param>
    /// <returns>A task that resolves to up to <paramref name="limit"/> change logs for the task, newest first.</returns>
    Task<List<ChangeLog>> GetByTaskIdAsync(Guid taskId, int limit = 100);

    /// <summary>
    /// Retrieves change logs within a date range.
    /// </summary>
    /// <param name="from">The inclusive lower bound on <see cref="ChangeLog.Timestamp"/>.</param>
    /// <param name="to">The inclusive upper bound on <see cref="ChangeLog.Timestamp"/>.</param>
    /// <returns>A task that resolves to all change logs whose timestamp falls within the range, newest first.</returns>
    Task<List<ChangeLog>> GetByDateRangeAsync(DateTime from, DateTime to);

    /// <summary>
    /// Retrieves change logs by source (Local or Notion).
    /// </summary>
    /// <param name="source">The <see cref="ChangeSource"/> to filter by.</param>
    /// <returns>A task that resolves to all change logs from the given source, newest first.</returns>
    Task<List<ChangeLog>> GetBySourceAsync(ChangeSource source);

    /// <summary>
    /// Retrieves change logs by change type (Created, Updated, Deleted, etc).
    /// </summary>
    /// <param name="changeType">The change type to match exactly (for example "Created", "Updated", "Deleted").</param>
    /// <returns>
    /// A task that resolves to all change logs of the given type, newest first.
    /// Resolves to an empty list when <paramref name="changeType"/> is null or empty.
    /// </returns>
    Task<List<ChangeLog>> GetByChangeTypeAsync(string changeType);

    /// <summary>
    /// Retrieves all conflicts from the change log.
    /// </summary>
    /// <returns>A task that resolves to all change logs flagged as conflicts, newest first.</returns>
    Task<List<ChangeLog>> GetConflictChangesAsync();

    /// <summary>
    /// Retrieves the most recent change logs.
    /// </summary>
    /// <param name="limit">The maximum number of entries to return. Defaults to 50.</param>
    /// <returns>A task that resolves to up to <paramref name="limit"/> change logs across all tasks, newest first.</returns>
    Task<List<ChangeLog>> GetLatestAsync(int limit = 50);

    /// <summary>
    /// Counts total change log entries.
    /// </summary>
    /// <returns>A task that resolves to the total number of change log entries.</returns>
    Task<int> CountAsync();

    /// <summary>
    /// Counts conflicts in the change log.
    /// </summary>
    /// <returns>A task that resolves to the number of change log entries flagged as conflicts.</returns>
    Task<int> CountConflictsAsync();

    /// <summary>
    /// Saves all pending changes.
    /// </summary>
    /// <returns>A task that completes when pending changes have been persisted.</returns>
    /// <remarks>
    /// Implementations that keep entries only in memory may treat this as a no-op.
    /// </remarks>
    global::System.Threading.Tasks.Task SaveAsync();
}
