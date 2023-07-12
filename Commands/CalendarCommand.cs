#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace NotionTaskSync.Commands;

using NotionTaskSync.Cli;
using NotionTaskSync.Services;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

/// <summary>
/// Synchronizes task due dates with an iCal (.ics) calendar file.
/// Supports exporting tasks to a calendar, importing events as tasks, or
/// running a full bidirectional sync against a shared .ics file.
/// </summary>
public class CalendarCommand : CliCommand
{
    private readonly CalendarSyncService _calendarSyncService;
    private readonly ILogger<CalendarCommand> _logger;

    public override string Description => "Sync task due dates with an iCal (.ics) calendar file";

    public override Dictionary<string, string> Options => new()
    {
        { "action",  "Action to perform: export, import, or sync (default: sync)" },
        { "file",    "Path to the .ics calendar file (default: notion-tasks.ics)" },
        { "verbose", "Enable verbose output" }
    };

    public CalendarCommand(CalendarSyncService calendarSyncService, ILogger<CalendarCommand> logger)
    {
        _calendarSyncService = calendarSyncService ?? throw new ArgumentNullException(nameof(calendarSyncService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Executes the calendar sync action specified by the <c>--action</c> option.
    /// Defaults to bidirectional sync when no action is provided.
    /// </summary>
    public override async Task<int> ExecuteAsync(List<string> arguments, Dictionary<string, string> options)
    {
        try
        {
            var action = options.GetValueOrDefault("action", "sync").ToLowerInvariant();
            var filePath = options.GetValueOrDefault("file", "notion-tasks.ics");

            switch (action)
            {
                case "export":
                    return await RunExportAsync(filePath);
                case "import":
                    return await RunImportAsync(filePath);
                case "sync":
                    return await RunBidirectionalSyncAsync(filePath);
                default:
                    _logger.LogError("Unknown action '{Action}'. Use: export, import, or sync", action);
                    return 1;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Calendar command failed");
            return 1;
        }
    }

    private async Task<int> RunExportAsync(string filePath)
    {
        _logger.LogInformation("Exporting tasks to calendar file: {Path}", filePath);
        var result = await _calendarSyncService.ExportToCalendarAsync(filePath);

        Console.WriteLine($"\nCalendar Export Complete");
        Console.WriteLine($"  Events exported: {result.EventsExported}");
        Console.WriteLine($"  Output file:     {filePath}");

        foreach (var warning in result.Warnings)
            Console.WriteLine($"  Warning: {warning}");

        return 0;
    }

    private async Task<int> RunImportAsync(string filePath)
    {
        _logger.LogInformation("Importing calendar events from: {Path}", filePath);
        var result = await _calendarSyncService.ImportFromCalendarAsync(filePath);

        Console.WriteLine($"\nCalendar Import Complete");
        Console.WriteLine($"  Events imported: {result.EventsImported}");
        Console.WriteLine($"  Tasks created:   {result.TasksCreated}");
        Console.WriteLine($"  Tasks updated:   {result.TasksUpdated}");
        Console.WriteLine($"  Skipped:         {result.Skipped}");

        foreach (var warning in result.Warnings)
            Console.WriteLine($"  Warning: {warning}");

        return 0;
    }

    private async Task<int> RunBidirectionalSyncAsync(string filePath)
    {
        _logger.LogInformation("Running bidirectional calendar sync with: {Path}", filePath);
        var result = await _calendarSyncService.BidirectionalSyncAsync(filePath);

        Console.WriteLine($"\nBidirectional Calendar Sync Complete");
        Console.WriteLine($"  Events exported: {result.EventsExported}");
        Console.WriteLine($"  Events imported: {result.EventsImported}");
        Console.WriteLine($"  Tasks created:   {result.TasksCreated}");
        Console.WriteLine($"  Tasks updated:   {result.TasksUpdated}");
        Console.WriteLine($"  Skipped:         {result.Skipped}");

        foreach (var warning in result.Warnings)
            Console.WriteLine($"  Warning: {warning}");

        return 0;
    }
}
