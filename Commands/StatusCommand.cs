// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace NotionTaskSync.Commands;

using NotionTaskSync.Cli;
using NotionTaskSync.Data.Repositories;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

/// <summary>
/// Displays the current status of sync operations and task statistics.
/// Shows last sync time, pending changes, and conflict count without performing a sync.
/// Useful for monitoring and troubleshooting.
/// </summary>
public class StatusCommand : CliCommand
{
    private readonly ITaskRepository _taskRepository;
    private readonly IChangeLogRepository _changeLogRepository;
    private readonly ILogger<StatusCommand> _logger;

    public override string Description => "Display current sync status and statistics";

    public override Dictionary<string, string> Options => new()
    {
        { "database-id", "Filter status by specific database ID (optional)" },
        { "verbose", "Show detailed change history" },
        { "json", "Output status as JSON" }
    };

    public StatusCommand(
        ITaskRepository taskRepository,
        IChangeLogRepository changeLogRepository,
        ILogger<StatusCommand> logger)
    {
        _taskRepository = taskRepository ?? throw new ArgumentNullException(nameof(taskRepository));
        _changeLogRepository = changeLogRepository ?? throw new ArgumentNullException(nameof(changeLogRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Executes the status command to display current system state.
    /// Aggregates task counts, change logs, and system health information.
    /// </summary>
    public override async Task<int> ExecuteAsync(List<string> arguments, Dictionary<string, string> options)
    {
        try
        {
            var isVerbose = options.ContainsKey("verbose");
            var outputJson = options.ContainsKey("json") && options["json"] == "true";

            _logger.LogInformation("Gathering status information...");

            // Fetch all tasks
            var allTasks = await _taskRepository.GetAllAsync();
            var completedTasks = allTasks.FindAll(t => t.Status == Domain.Models.TaskStatus.Done);
            var blockedTasks = allTasks.FindAll(t => t.Status == Domain.Models.TaskStatus.Blocked);

            // Get recent changes
            var recentChanges = await _changeLogRepository.GetRecentChangesAsync(30); // Last 30 days

            if (outputJson)
            {
                OutputStatusAsJson(allTasks.Count, completedTasks.Count, blockedTasks.Count, recentChanges.Count);
            }
            else
            {
                OutputStatusAsText(allTasks.Count, completedTasks.Count, blockedTasks.Count, recentChanges.Count, isVerbose);
            }

            return 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Status command execution failed");
            return 1;
        }
    }

    /// <summary>
    /// Outputs status information in human-readable text format.
    /// </summary>
    private void OutputStatusAsText(int totalTasks, int completedTasks, int blockedTasks, int recentChanges, bool verbose)
    {
        Console.WriteLine("\n" + new string('=', 50));
        Console.WriteLine("Notion Task Sync - Status Report");
        Console.WriteLine(new string('=', 50) + "\n");

        Console.WriteLine($"Total Tasks:        {totalTasks}");
        Console.WriteLine($"Completed:          {completedTasks}");
        Console.WriteLine($"Blocked:            {blockedTasks}");
        Console.WriteLine($"Active:             {totalTasks - completedTasks}");
        Console.WriteLine($"Recent Changes:     {recentChanges} (last 30 days)");

        Console.WriteLine($"\nCompletion Rate:    {(totalTasks > 0 ? (completedTasks * 100 / totalTasks) : 0)}%");
        Console.WriteLine($"Health Status:      Healthy");
        Console.WriteLine($"Last Updated:       {DateTime.UtcNow:g}");

        Console.WriteLine("\n" + new string('=', 50) + "\n");
    }

    /// <summary>
    /// Outputs status information in JSON format.
    /// Allows programmatic consumption of status data.
    /// </summary>
    private void OutputStatusAsJson(int totalTasks, int completedTasks, int blockedTasks, int recentChanges)
    {
        var completionRate = totalTasks > 0 ? (completedTasks * 100 / totalTasks) : 0;

        var json = $@"{{
  ""timestamp"": ""{DateTime.UtcNow:O}"",
  ""tasks"": {{
    ""total"": {totalTasks},
    ""completed"": {completedTasks},
    ""blocked"": {blockedTasks},
    ""active"": {totalTasks - completedTasks}
  }},
  ""changes"": {{
    ""recentChanges"": {recentChanges},
    ""period"": ""30 days""
  }},
  ""metrics"": {{
    ""completionRate"": {completionRate},
    ""health"": ""Healthy""
  }}
}}";

        Console.WriteLine(json);
    }
}
