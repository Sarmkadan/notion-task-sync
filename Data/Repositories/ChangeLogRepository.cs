// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace NotionTaskSync.Data.Repositories;

using NotionTaskSync.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

/// <summary>
/// In-memory implementation of IChangeLogRepository for managing audit trails.
/// Tracks all changes for conflict detection and sync history.
/// </summary>
public class ChangeLogRepository : IChangeLogRepository
{
    private readonly List<ChangeLog> _changeLogs;
    private bool _hasChanges;

    public ChangeLogRepository()
    {
        _changeLogs = new List<ChangeLog>();
        _hasChanges = false;
    }

    public async Task AddAsync(ChangeLog changeLog)
    {
        if (changeLog == null)
            throw new ArgumentNullException(nameof(changeLog));

        if (!changeLog.Validate())
            throw new InvalidOperationException("Change log validation failed");

        _changeLogs.Add(changeLog);
        _hasChanges = true;

        await Task.CompletedTask;
    }

    public async Task<List<ChangeLog>> GetByTaskIdAsync(Guid taskId, int limit = 100)
    {
        return await Task.FromResult(
            _changeLogs
                .Where(c => c.TaskId == taskId)
                .OrderByDescending(c => c.Timestamp)
                .Take(limit)
                .ToList());
    }

    public async Task<List<ChangeLog>> GetByDateRangeAsync(DateTime from, DateTime to)
    {
        return await Task.FromResult(
            _changeLogs
                .Where(c => c.Timestamp >= from && c.Timestamp <= to)
                .OrderByDescending(c => c.Timestamp)
                .ToList());
    }

    public async Task<List<ChangeLog>> GetBySourceAsync(ChangeSource source)
    {
        return await Task.FromResult(
            _changeLogs
                .Where(c => c.Source == source)
                .OrderByDescending(c => c.Timestamp)
                .ToList());
    }

    public async Task<List<ChangeLog>> GetByChangeTypeAsync(string changeType)
    {
        if (string.IsNullOrEmpty(changeType))
            return new List<ChangeLog>();

        return await Task.FromResult(
            _changeLogs
                .Where(c => c.ChangeType == changeType)
                .OrderByDescending(c => c.Timestamp)
                .ToList());
    }

    public async Task<List<ChangeLog>> GetConflictChangesAsync()
    {
        return await Task.FromResult(
            _changeLogs
                .Where(c => c.IsConflict)
                .OrderByDescending(c => c.Timestamp)
                .ToList());
    }

    public async Task<List<ChangeLog>> GetLatestAsync(int limit = 50)
    {
        return await Task.FromResult(
            _changeLogs
                .OrderByDescending(c => c.Timestamp)
                .Take(limit)
                .ToList());
    }

    public async Task<int> CountAsync()
    {
        return await Task.FromResult(_changeLogs.Count);
    }

    public async Task<int> CountConflictsAsync()
    {
        return await Task.FromResult(_changeLogs.Count(c => c.IsConflict));
    }

    public async Task SaveAsync()
    {
        // In-memory implementation: would persist to database here
        _hasChanges = false;

        await Task.CompletedTask;
    }

    /// <summary>
    /// Gets the full audit trail for a task including all historical changes.
    /// </summary>
    public async Task<List<ChangeLog>> GetFullAuditTrailAsync(Guid taskId)
    {
        return await GetByTaskIdAsync(taskId, int.MaxValue);
    }

    /// <summary>
    /// Gets change summary statistics.
    /// </summary>
    public async Task<ChangeLogStats> GetStatsAsync()
    {
        var stats = new ChangeLogStats
        {
            TotalChanges = _changeLogs.Count,
            LocalChanges = _changeLogs.Count(c => c.Source == ChangeSource.Local),
            NotionChanges = _changeLogs.Count(c => c.Source == ChangeSource.Notion),
            SystemChanges = _changeLogs.Count(c => c.Source == ChangeSource.System),
            ConflictCount = _changeLogs.Count(c => c.IsConflict),
            CreatedCount = _changeLogs.Count(c => c.ChangeType == "Created"),
            UpdatedCount = _changeLogs.Count(c => c.ChangeType == "Updated"),
            DeletedCount = _changeLogs.Count(c => c.ChangeType == "Deleted")
        };

        return await Task.FromResult(stats);
    }

    /// <summary>
    /// Determines if there are unsaved changes.
    /// </summary>
    public bool HasPendingChanges => _hasChanges;
}

/// <summary>
/// Statistics about change log entries.
/// </summary>
public class ChangeLogStats
{
    public int TotalChanges { get; set; }
    public int LocalChanges { get; set; }
    public int NotionChanges { get; set; }
    public int SystemChanges { get; set; }
    public int ConflictCount { get; set; }
    public int CreatedCount { get; set; }
    public int UpdatedCount { get; set; }
    public int DeletedCount { get; set; }

    public override string ToString()
    {
        return $"Changes: {TotalChanges}, Conflicts: {ConflictCount}, Local: {LocalChanges}, Notion: {NotionChanges}";
    }
}
