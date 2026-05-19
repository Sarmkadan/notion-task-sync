#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace NotionTaskSync.Commands;

using NotionTaskSync.Cli;
using NotionTaskSync.Domain.Models;
using NotionTaskSync.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

/// <summary>
/// Performs bulk operations on multiple tasks in a single command invocation.
/// Supports status updates, tagging, assignment, priority changes, and deletion.
/// Tasks can be selected by explicit ID list or by a filter expression.
/// </summary>
/// <remarks>
/// <para><b>Usage examples:</b></para>
/// <list type="bullet">
///   <item><description><c>bulk update-status --status done --ids id1,id2,id3</c></description></item>
///   <item><description><c>bulk add-tag --tag bug --filter "status:inprogress"</c></description></item>
///   <item><description><c>bulk assign --assignee alice@example.com --filter "tag:backend"</c></description></item>
///   <item><description><c>bulk set-priority --priority 80 --ids id1,id2</c></description></item>
///   <item><description><c>bulk delete --filter "status:done"</c></description></item>
/// </list>
/// </remarks>
public class BulkCommand : CliCommand
{
    private readonly BulkOperationService _bulkService;
    private readonly ILogger<BulkCommand> _logger;

    public override string Description => "Perform bulk operations on multiple tasks at once";

    public override Dictionary<string, string> Options => new()
    {
        { "ids",      "Comma-separated list of task GUIDs to operate on" },
        { "filter",   "Filter expression to select tasks (e.g. status:done, tag:bug, assignee:alice)" },
        { "status",   "Target status for update-status: todo, inprogress, done, blocked, archived" },
        { "tag",      "Tag value for add-tag or remove-tag operations" },
        { "assignee", "Assignee email or name for the assign operation" },
        { "priority", "Priority value 0-100 for set-priority operation" },
        { "dry-run",  "Preview affected tasks without applying changes" }
    };

    public BulkCommand(BulkOperationService bulkService, ILogger<BulkCommand> logger)
    {
        _bulkService = bulkService ?? throw new ArgumentNullException(nameof(bulkService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Executes the bulk operation specified as the first positional argument.
    /// Requires either <c>--ids</c> or <c>--filter</c> to identify the target tasks.
    /// </summary>
    public override async Task<int> ExecuteAsync(List<string> arguments, Dictionary<string, string> options)
    {
        if (arguments.Count == 0)
        {
            PrintUsage();
            return 1;
        }

        var operation = arguments[0].ToLowerInvariant();
        var isDryRun = options.ContainsKey("dry-run") && options["dry-run"] == "true";

        try
        {
            var taskIds = await ResolveTaskIdsAsync(options);

            if (taskIds.Count == 0)
            {
                _logger.LogWarning("No tasks matched the provided selection criteria");
                Console.WriteLine("No tasks matched. Use --ids or --filter to select tasks.");
                return 0;
            }

            if (isDryRun)
            {
                Console.WriteLine($"[dry-run] {operation} would affect {taskIds.Count} task(s):");
                foreach (var id in taskIds)
                    Console.WriteLine($"  - {id}");
                return 0;
            }

            BulkResult result = operation switch
            {
                "update-status" => await RunUpdateStatusAsync(taskIds, options),
                "add-tag"       => await RunAddTagAsync(taskIds, options),
                "remove-tag"    => await RunRemoveTagAsync(taskIds, options),
                "assign"        => await RunAssignAsync(taskIds, options),
                "set-priority"  => await RunSetPriorityAsync(taskIds, options),
                "delete"        => await _bulkService.DeleteAsync(taskIds),
                _ => throw new ArgumentException($"Unknown operation: '{operation}'")
            };

            PrintResult(result);
            return result.Affected > 0 ? 0 : 1;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Bulk command '{Operation}' failed", operation);
            Console.WriteLine($"Error: {ex.Message}");
            return 1;
        }
    }

    // -------------------------------------------------------------------------
    // Operation runners
    // -------------------------------------------------------------------------

    private async Task<BulkResult> RunUpdateStatusAsync(List<Guid> ids, Dictionary<string, string> options)
    {
        if (!options.TryGetValue("status", out var statusStr))
            throw new ArgumentException("--status is required for update-status");

        var status = ParseStatus(statusStr);
        return await _bulkService.UpdateStatusAsync(ids, status);
    }

    private async Task<BulkResult> RunAddTagAsync(List<Guid> ids, Dictionary<string, string> options)
    {
        if (!options.TryGetValue("tag", out var tag))
            throw new ArgumentException("--tag is required for add-tag");
        return await _bulkService.AddTagAsync(ids, tag);
    }

    private async Task<BulkResult> RunRemoveTagAsync(List<Guid> ids, Dictionary<string, string> options)
    {
        if (!options.TryGetValue("tag", out var tag))
            throw new ArgumentException("--tag is required for remove-tag");
        return await _bulkService.RemoveTagAsync(ids, tag);
    }

    private async Task<BulkResult> RunAssignAsync(List<Guid> ids, Dictionary<string, string> options)
    {
        var assignee = options.GetValueOrDefault("assignee", string.Empty);
        return await _bulkService.AssignAsync(ids, assignee);
    }

    private async Task<BulkResult> RunSetPriorityAsync(List<Guid> ids, Dictionary<string, string> options)
    {
        if (!options.TryGetValue("priority", out var priorityStr) || !int.TryParse(priorityStr, out var priority))
            throw new ArgumentException("--priority must be an integer between 0 and 100");
        return await _bulkService.SetPriorityAsync(ids, priority);
    }

    // -------------------------------------------------------------------------
    // Task ID resolution
    // -------------------------------------------------------------------------

    /// <summary>
    /// Resolves the target task IDs from either explicit <c>--ids</c> or <c>--filter</c>.
    /// </summary>
    private async Task<List<Guid>> ResolveTaskIdsAsync(Dictionary<string, string> options)
    {
        if (options.TryGetValue("ids", out var idsRaw) && !string.IsNullOrWhiteSpace(idsRaw))
        {
            return idsRaw
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(s => Guid.TryParse(s, out var g) ? g : Guid.Empty)
                .Where(g => g != Guid.Empty)
                .ToList();
        }

        if (options.TryGetValue("filter", out var filter) && !string.IsNullOrWhiteSpace(filter))
        {
            var predicate = BuildFilterPredicate(filter);
            var tasks = await _bulkService.QueryAsync(predicate);
            return tasks.Select(t => t.Id).ToList();
        }

        return new List<Guid>();
    }

    /// <summary>
    /// Builds a predicate from a simple <c>key:value</c> filter expression.
    /// Supported keys: <c>status</c>, <c>tag</c>, <c>assignee</c>.
    /// </summary>
    private static Func<Domain.Models.Task, bool> BuildFilterPredicate(string filter)
    {
        var parts = filter.Split(':', 2);
        if (parts.Length != 2)
            return _ => true;

        var key = parts[0].Trim().ToLowerInvariant();
        var value = parts[1].Trim().ToLowerInvariant();

        return key switch
        {
            "status" => t => t.Status.ToString().ToLowerInvariant() == value
                          || t.Status.ToString().Replace(" ", "").ToLowerInvariant() == value,
            "tag" => t => !string.IsNullOrEmpty(t.Tags)
                       && t.Tags.Split(',').Any(tag => tag.Trim().ToLowerInvariant() == value),
            "assignee" => t => string.Equals(t.AssignedTo, value, StringComparison.OrdinalIgnoreCase),
            _ => _ => true
        };
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private static Domain.Models.TaskStatus ParseStatus(string value) =>
        value.ToLowerInvariant() switch
        {
            "todo"        => Domain.Models.TaskStatus.Todo,
            "inprogress"  => Domain.Models.TaskStatus.InProgress,
            "in-progress" => Domain.Models.TaskStatus.InProgress,
            "done"        => Domain.Models.TaskStatus.Done,
            "blocked"     => Domain.Models.TaskStatus.Blocked,
            "archived"    => Domain.Models.TaskStatus.Archived,
            _ => throw new ArgumentException($"Unknown status: '{value}'. Valid: todo, inprogress, done, blocked, archived")
        };

    private static void PrintResult(BulkResult result)
    {
        Console.WriteLine($"\nBulk {result.Operation} Complete");
        Console.WriteLine($"  Requested: {result.Requested}");
        Console.WriteLine($"  Affected:  {result.Affected}");
        Console.WriteLine($"  Skipped:   {result.Skipped}");
    }

    private static void PrintUsage()
    {
        Console.WriteLine("Usage: bulk <operation> [options]");
        Console.WriteLine("Operations: update-status, add-tag, remove-tag, assign, set-priority, delete");
        Console.WriteLine("Examples:");
        Console.WriteLine("  bulk update-status --status done --filter status:inprogress");
        Console.WriteLine("  bulk add-tag --tag urgent --ids id1,id2");
        Console.WriteLine("  bulk delete --filter tag:obsolete --dry-run");
    }
}
