#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace NotionTaskSync.Services;

using NotionTaskSync.Domain.Models;
using NotionTaskSync.Data.Repositories;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Executes bulk operations on multiple tasks in a single call.
/// All mutations are applied atomically within a single <see cref="ITaskRepository.SaveAsync"/> call
/// to reduce round-trips and keep the repository state consistent.
/// </summary>
public class BulkOperationService
{
    private readonly ITaskRepository _taskRepository;
    private readonly ILogger<BulkOperationService> _logger;

    public BulkOperationService(
        ITaskRepository taskRepository,
        ILogger<BulkOperationService> logger)
    {
        _taskRepository = taskRepository ?? throw new ArgumentNullException(nameof(taskRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Updates the status of all tasks matching the specified IDs.
    /// Tasks that are not found are silently skipped and counted in <see cref="BulkResult.Skipped"/>.
    /// </summary>
    /// <param name="taskIds">Collection of task GUIDs to update.</param>
    /// <param name="newStatus">The target <see cref="TaskStatus"/> to apply.</param>
    /// <returns>A <see cref="BulkResult"/> describing how many tasks were affected.</returns>
    public async Task<BulkResult> UpdateStatusAsync(IEnumerable<Guid> taskIds, TaskStatus newStatus)
    {
        var ids = taskIds.ToList();
        _logger.LogInformation("Bulk status update: {Count} tasks → {Status}", ids.Count, newStatus);

        var result = new BulkResult { Operation = "UpdateStatus", Requested = ids.Count };

        foreach (var id in ids)
        {
            var task = await _taskRepository.GetByIdAsync(id);
            if (task is null) { result.Skipped++; continue; }

            var previous = task.Status;
            task.Status = newStatus;

            if (newStatus == TaskStatus.Done && !task.CompletedAt.HasValue)
                task.CompletedAt = DateTime.UtcNow;

            task.UpdateTimestamp();
            await _taskRepository.UpdateAsync(task);

            result.Affected++;
            _logger.LogDebug("Task '{Title}': {Prev} → {New}", task.Title, previous, newStatus);
        }

        await _taskRepository.SaveAsync();
        _logger.LogInformation("Bulk status update complete: {Affected}/{Total}", result.Affected, ids.Count);
        return result;
    }

    /// <summary>
    /// Appends a tag to all tasks matching the specified IDs.
    /// Duplicate tags are ignored; existing tags on each task are preserved.
    /// </summary>
    /// <param name="taskIds">Collection of task GUIDs to tag.</param>
    /// <param name="tag">The tag string to add (whitespace trimmed).</param>
    /// <returns>A <see cref="BulkResult"/> describing how many tasks were affected.</returns>
    public async Task<BulkResult> AddTagAsync(IEnumerable<Guid> taskIds, string tag)
    {
        if (string.IsNullOrWhiteSpace(tag))
            throw new ArgumentException("Tag cannot be empty", nameof(tag));

        var normalizedTag = tag.Trim().ToLowerInvariant();
        var ids = taskIds.ToList();
        _logger.LogInformation("Bulk add tag '{Tag}': {Count} tasks", normalizedTag, ids.Count);

        var result = new BulkResult { Operation = "AddTag", Requested = ids.Count };

        foreach (var id in ids)
        {
            var task = await _taskRepository.GetByIdAsync(id);
            if (task is null) { result.Skipped++; continue; }

            var existingTags = ParseTags(task.Tags);
            if (existingTags.Contains(normalizedTag)) { result.Skipped++; continue; }

            existingTags.Add(normalizedTag);
            task.Tags = string.Join(",", existingTags);
            task.UpdateTimestamp();
            await _taskRepository.UpdateAsync(task);
            result.Affected++;
        }

        await _taskRepository.SaveAsync();
        return result;
    }

    /// <summary>
    /// Removes a tag from all tasks matching the specified IDs.
    /// Tasks that do not carry the tag are silently skipped.
    /// </summary>
    /// <param name="taskIds">Collection of task GUIDs to update.</param>
    /// <param name="tag">The tag to remove.</param>
    public async Task<BulkResult> RemoveTagAsync(IEnumerable<Guid> taskIds, string tag)
    {
        if (string.IsNullOrWhiteSpace(tag))
            throw new ArgumentException("Tag cannot be empty", nameof(tag));

        var normalizedTag = tag.Trim().ToLowerInvariant();
        var ids = taskIds.ToList();
        var result = new BulkResult { Operation = "RemoveTag", Requested = ids.Count };

        foreach (var id in ids)
        {
            var task = await _taskRepository.GetByIdAsync(id);
            if (task is null) { result.Skipped++; continue; }

            var existingTags = ParseTags(task.Tags);
            if (!existingTags.Remove(normalizedTag)) { result.Skipped++; continue; }

            task.Tags = string.Join(",", existingTags);
            task.UpdateTimestamp();
            await _taskRepository.UpdateAsync(task);
            result.Affected++;
        }

        await _taskRepository.SaveAsync();
        return result;
    }

    /// <summary>
    /// Assigns all specified tasks to a particular person.
    /// Pass an empty string to clear the assignment.
    /// </summary>
    /// <param name="taskIds">Collection of task GUIDs to reassign.</param>
    /// <param name="assignee">Target assignee identifier (e.g. email or username).</param>
    public async Task<BulkResult> AssignAsync(IEnumerable<Guid> taskIds, string assignee)
    {
        var ids = taskIds.ToList();
        _logger.LogInformation("Bulk assign to '{Assignee}': {Count} tasks", assignee, ids.Count);

        var result = new BulkResult { Operation = "Assign", Requested = ids.Count };

        foreach (var id in ids)
        {
            var task = await _taskRepository.GetByIdAsync(id);
            if (task is null) { result.Skipped++; continue; }

            task.AssignedTo = string.IsNullOrWhiteSpace(assignee) ? null : assignee.Trim();
            task.UpdateTimestamp();
            await _taskRepository.UpdateAsync(task);
            result.Affected++;
        }

        await _taskRepository.SaveAsync();
        return result;
    }

    /// <summary>
    /// Sets the priority of all specified tasks.
    /// </summary>
    /// <param name="taskIds">Collection of task GUIDs to update.</param>
    /// <param name="priority">Priority value between 0 (lowest) and 100 (highest).</param>
    public async Task<BulkResult> SetPriorityAsync(IEnumerable<Guid> taskIds, int priority)
    {
        if (priority < 0 || priority > 100)
            throw new ArgumentOutOfRangeException(nameof(priority), "Priority must be between 0 and 100");

        var ids = taskIds.ToList();
        var result = new BulkResult { Operation = "SetPriority", Requested = ids.Count };

        foreach (var id in ids)
        {
            var task = await _taskRepository.GetByIdAsync(id);
            if (task is null) { result.Skipped++; continue; }

            task.Priority = priority;
            task.UpdateTimestamp();
            await _taskRepository.UpdateAsync(task);
            result.Affected++;
        }

        await _taskRepository.SaveAsync();
        return result;
    }

    /// <summary>
    /// Soft-deletes all specified tasks by setting their <c>IsDeleted</c> flag.
    /// </summary>
    /// <param name="taskIds">Collection of task GUIDs to delete.</param>
    public async Task<BulkResult> DeleteAsync(IEnumerable<Guid> taskIds)
    {
        var ids = taskIds.ToList();
        _logger.LogInformation("Bulk soft-delete: {Count} tasks", ids.Count);

        var result = new BulkResult { Operation = "Delete", Requested = ids.Count };

        foreach (var id in ids)
        {
            var task = await _taskRepository.GetByIdAsync(id);
            if (task is null) { result.Skipped++; continue; }

            task.MarkAsDeleted();
            await _taskRepository.UpdateAsync(task);
            result.Affected++;
        }

        await _taskRepository.SaveAsync();
        return result;
    }

    /// <summary>
    /// Returns all tasks that match the given filter predicate.
    /// Useful for building the ID list before a bulk operation.
    /// </summary>
    /// <param name="filter">Predicate applied to each non-deleted task.</param>
    public async Task<List<Domain.Models.Task>> QueryAsync(Func<Domain.Models.Task, bool> filter)
    {
        var all = await _taskRepository.GetAllAsync();
        return all.Where(t => !t.IsDeleted && filter(t)).ToList();
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private static HashSet<string> ParseTags(string? rawTags)
    {
        if (string.IsNullOrWhiteSpace(rawTags))
            return new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        return new HashSet<string>(
            rawTags.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries),
            StringComparer.OrdinalIgnoreCase);
    }
}

/// <summary>
/// Describes the outcome of a bulk operation.
/// </summary>
public class BulkResult
{
    /// <summary>Name of the operation that was performed.</summary>
    public string Operation { get; set; } = string.Empty;

    /// <summary>Total number of task IDs submitted.</summary>
    public int Requested { get; set; }

    /// <summary>Number of tasks successfully updated.</summary>
    public int Affected { get; set; }

    /// <summary>Number of tasks not found or already in the target state.</summary>
    public int Skipped { get; set; }

    /// <summary>Indicates whether every submitted task was processed successfully.</summary>
    public bool IsFullSuccess => Skipped == 0 && Affected == Requested;
}
