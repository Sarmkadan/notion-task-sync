// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace NotionTaskSync.Data.Repositories;

using NotionTaskSync.Domain.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

/// <summary>
/// Repository interface for ChangeLog CRUD operations and audit queries.
/// Manages the audit trail of all sync-related changes.
/// </summary>
public interface IChangeLogRepository
{
    /// <summary>
    /// Adds a new change log entry.
    /// </summary>
    Task AddAsync(ChangeLog changeLog);

    /// <summary>
    /// Retrieves change logs for a specific task.
    /// </summary>
    Task<List<ChangeLog>> GetByTaskIdAsync(Guid taskId, int limit = 100);

    /// <summary>
    /// Retrieves change logs within a date range.
    /// </summary>
    Task<List<ChangeLog>> GetByDateRangeAsync(DateTime from, DateTime to);

    /// <summary>
    /// Retrieves change logs by source (Local or Notion).
    /// </summary>
    Task<List<ChangeLog>> GetBySourceAsync(ChangeSource source);

    /// <summary>
    /// Retrieves change logs by change type (Created, Updated, Deleted, etc).
    /// </summary>
    Task<List<ChangeLog>> GetByChangeTypeAsync(string changeType);

    /// <summary>
    /// Retrieves all conflicts from the change log.
    /// </summary>
    Task<List<ChangeLog>> GetConflictChangesAsync();

    /// <summary>
    /// Retrieves the most recent change logs.
    /// </summary>
    Task<List<ChangeLog>> GetLatestAsync(int limit = 50);

    /// <summary>
    /// Counts total change log entries.
    /// </summary>
    Task<int> CountAsync();

    /// <summary>
    /// Counts conflicts in the change log.
    /// </summary>
    Task<int> CountConflictsAsync();

    /// <summary>
    /// Saves all pending changes.
    /// </summary>
    Task SaveAsync();
}
