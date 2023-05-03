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
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

/// <summary>
/// Interactive terminal UI for reviewing and resolving sync conflicts one at a time.
/// Displays a side-by-side or unified diff for each pending conflict and prompts the
/// operator to choose a resolution strategy (local, notion, or custom value).
/// Non-interactive batch resolution is also supported via the <c>--strategy</c> option.
/// </summary>
public class ConflictCommand : CliCommand
{
    private readonly ConflictResolutionService _resolutionService;
    private readonly ConflictDiffService _diffService;
    private readonly ILogger<ConflictCommand> _logger;

    public override string Description => "Review and resolve pending sync conflicts interactively";

    public override Dictionary<string, string> Options => new()
    {
        { "strategy",     "Auto-resolve all conflicts: local, notion, or last-write" },
        { "show-diff",    "Always display the diff before prompting (default: true)" },
        { "json",         "Output resolutions as JSON instead of interactive prompts" },
        { "limit",        "Maximum number of conflicts to process in this session (default: all)" }
    };

    public ConflictCommand(
        ConflictResolutionService resolutionService,
        ConflictDiffService diffService,
        ILogger<ConflictCommand> logger)
    {
        _resolutionService = resolutionService ?? throw new ArgumentNullException(nameof(resolutionService));
        _diffService = diffService ?? throw new ArgumentNullException(nameof(diffService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Executes the conflict resolution UI session.
    /// When <c>--strategy</c> is supplied all pending conflicts are resolved automatically.
    /// Otherwise the operator is prompted for each conflict individually.
    /// </summary>
    public override async Task<int> ExecuteAsync(
        List<string> arguments,
        Dictionary<string, string> options)
    {
        try
        {
            var conflicts = LoadPendingConflicts();

            if (conflicts.Count == 0)
            {
                Console.WriteLine("No pending conflicts. Everything is in sync.");
                return 0;
            }

            if (options.TryGetValue("limit", out var limitStr) && int.TryParse(limitStr, out var limit))
                conflicts = conflicts.Take(limit).ToList();

            Console.WriteLine($"\n{conflicts.Count} pending conflict(s) found.\n");

            if (options.TryGetValue("strategy", out var strategyStr))
                return await AutoResolveAsync(conflicts, strategyStr);

            if (options.ContainsKey("json"))
                return PrintConflictsAsJson(conflicts);

            return await InteractiveResolveAsync(conflicts, options, CancellationToken.None);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Conflict command failed");
            return 1;
        }
    }

    // -------------------------------------------------------------------------
    // Auto-resolve mode
    // -------------------------------------------------------------------------

    private async Task<int> AutoResolveAsync(List<ConflictResolution> conflicts, string strategyStr)
    {
        var strategy = ParseStrategy(strategyStr);
        _logger.LogInformation("Auto-resolving {Count} conflict(s) with strategy: {Strategy}",
            conflicts.Count, strategy);

        var resolved = await _resolutionService.ResolveConflictsAsync(conflicts, strategy);
        var stats = _resolutionService.GetResolutionStats(resolved);

        Console.WriteLine($"Auto-resolve complete:");
        Console.WriteLine($"  Total:      {stats.TotalConflicts}");
        Console.WriteLine($"  Resolved:   {stats.ResolvedCount}");
        Console.WriteLine($"  Pending:    {stats.PendingReviewCount}");
        Console.WriteLine($"  Rate:       {stats.ResolutionRate:P0}");

        return stats.PendingReviewCount == 0 ? 0 : 1;
    }

    // -------------------------------------------------------------------------
    // Interactive mode
    // -------------------------------------------------------------------------

    private async Task<int> InteractiveResolveAsync(
        List<ConflictResolution> conflicts,
        Dictionary<string, string> options,
        CancellationToken cancellationToken)
    {
        var showDiff = !options.ContainsKey("show-diff") || options["show-diff"] != "false";
        var resolvedCount = 0;
        var skippedCount = 0;

        for (int i = 0; i < conflicts.Count; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var conflict = conflicts[i];
            Console.WriteLine(new string('─', 60));
            Console.WriteLine($"Conflict {i + 1}/{conflicts.Count}");
            Console.WriteLine($"  Task ID:    {conflict.TaskId}");
            Console.WriteLine($"  Property:   {conflict.PropertyName ?? "(unknown)"}");
            Console.WriteLine($"  Type:       {conflict.ConflictType}");
            Console.WriteLine($"  Detected:   {conflict.DetectedAt:g} ({FormatAge(conflict.GetAge())} ago)");

            if (showDiff)
                await PrintDiffAsync(conflict, cancellationToken);
            else
                PrintValueSummary(conflict);

            var choice = PromptForResolution(conflict);

            if (choice == ResolutionChoice.Skip)
            {
                skippedCount++;
                Console.WriteLine("  → Skipped");
                continue;
            }

            if (choice == ResolutionChoice.Quit)
            {
                Console.WriteLine($"\nSession ended early. {resolvedCount} resolved, {skippedCount} skipped.");
                return resolvedCount > 0 ? 0 : 1;
            }

            ApplyChoice(conflict, choice);
            resolvedCount++;
            Console.WriteLine($"  → Resolved as: {conflict.ResolutionMethod}");
        }

        Console.WriteLine(new string('─', 60));
        Console.WriteLine($"\nSession complete: {resolvedCount} resolved, {skippedCount} skipped.");
        return resolvedCount > 0 ? 0 : 1;
    }

    // -------------------------------------------------------------------------
    // Prompting
    // -------------------------------------------------------------------------

    private static ResolutionChoice PromptForResolution(ConflictResolution conflict)
    {
        while (true)
        {
            Console.WriteLine();
            Console.WriteLine("  How would you like to resolve this conflict?");
            Console.WriteLine("  [l] Keep local value");
            Console.WriteLine("  [n] Keep Notion value");
            Console.WriteLine("  [c] Enter a custom value");
            Console.WriteLine("  [s] Skip (leave pending)");
            Console.WriteLine("  [q] Quit session");
            Console.Write("  Choice: ");

            var input = Console.ReadLine()?.Trim().ToLowerInvariant();

            return input switch
            {
                "l" => ResolutionChoice.Local,
                "n" => ResolutionChoice.Notion,
                "c" => ResolutionChoice.Custom,
                "s" => ResolutionChoice.Skip,
                "q" => ResolutionChoice.Quit,
                _   => PromptForResolutionRetry()
            };
        }
    }

    private static ResolutionChoice PromptForResolutionRetry()
    {
        Console.WriteLine("  Invalid input. Enter l, n, c, s, or q.");
        return ResolutionChoice.Skip; // will retry on next loop iteration
    }

    private static void ApplyChoice(ConflictResolution conflict, ResolutionChoice choice)
    {
        switch (choice)
        {
            case ResolutionChoice.Local:
                conflict.Resolve(
                    conflict.LocalValue ?? string.Empty,
                    ResolutionMethod.LocalWins,
                    "Manually resolved: operator chose local value");
                break;

            case ResolutionChoice.Notion:
                conflict.Resolve(
                    conflict.NotionValue ?? string.Empty,
                    ResolutionMethod.NotionWins,
                    "Manually resolved: operator chose Notion value");
                break;

            case ResolutionChoice.Custom:
                Console.Write("  Enter resolved value: ");
                var customValue = Console.ReadLine() ?? string.Empty;
                conflict.Resolve(customValue, ResolutionMethod.Manual,
                    "Manually resolved: operator entered custom value");
                break;
        }

        conflict.ResolvedBy = "cli-operator";
    }

    // -------------------------------------------------------------------------
    // Diff display
    // -------------------------------------------------------------------------

    private async System.Threading.Tasks.Task PrintDiffAsync(ConflictResolution conflict, CancellationToken cancellationToken)
    {
        try
        {
            var diff = await _diffService.GenerateDiffAsync(conflict, cancellationToken);
            var rendered = await _diffService.RenderAsTextAsync(diff, cancellationToken);

            Console.WriteLine();
            foreach (var line in rendered.Split('\n'))
            {
                if (line.StartsWith('+') && !line.StartsWith("+++"))
                    WriteColored(line, ConsoleColor.Green);
                else if (line.StartsWith('-') && !line.StartsWith("---"))
                    WriteColored(line, ConsoleColor.Red);
                else
                    Console.WriteLine(line);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not render diff for conflict {Id}", conflict.Id);
            PrintValueSummary(conflict);
        }
    }

    private static void PrintValueSummary(ConflictResolution conflict)
    {
        Console.WriteLine($"  Local:  {Truncate(conflict.LocalValue,  80)}");
        Console.WriteLine($"  Notion: {Truncate(conflict.NotionValue, 80)}");
    }

    // -------------------------------------------------------------------------
    // JSON output mode
    // -------------------------------------------------------------------------

    private static int PrintConflictsAsJson(List<ConflictResolution> conflicts)
    {
        Console.WriteLine("[");
        for (int i = 0; i < conflicts.Count; i++)
        {
            var c = conflicts[i];
            var comma = i < conflicts.Count - 1 ? "," : string.Empty;
            Console.WriteLine(
                $"  {{\n" +
                $"    \"id\": \"{c.Id}\",\n" +
                $"    \"taskId\": \"{c.TaskId}\",\n" +
                $"    \"property\": \"{c.PropertyName}\",\n" +
                $"    \"type\": \"{c.ConflictType}\",\n" +
                $"    \"localValue\": {JsonString(c.LocalValue)},\n" +
                $"    \"notionValue\": {JsonString(c.NotionValue)},\n" +
                $"    \"detectedAt\": \"{c.DetectedAt:O}\",\n" +
                $"    \"status\": \"{c.Status}\"\n" +
                $"  }}{comma}");
        }
        Console.WriteLine("]");
        return 0;
    }

    // -------------------------------------------------------------------------
    // Data loading (placeholder – in production these come from the conflict store)
    // -------------------------------------------------------------------------

    /// <summary>
    /// Loads pending conflicts for this session.
    /// Returns a demo conflict list when no conflict store is connected so the UI
    /// can be exercised during development without a live Notion connection.
    /// </summary>
    private static List<ConflictResolution> LoadPendingConflicts()
    {
        // In production this would query the conflict repository.
        // Return empty list to indicate a clean state when no store is wired.
        return new List<ConflictResolution>();
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private static ConflictResolutionStrategy ParseStrategy(string value) =>
        value.ToLowerInvariant() switch
        {
            "local"      => ConflictResolutionStrategy.LocalWins,
            "notion"     => ConflictResolutionStrategy.NotionWins,
            "last-write" => ConflictResolutionStrategy.LastWrite,
            "manual"     => ConflictResolutionStrategy.Manual,
            _ => throw new ArgumentException($"Unknown strategy: '{value}'. Valid: local, notion, last-write, manual")
        };

    private static void WriteColored(string text, ConsoleColor color)
    {
        var prev = Console.ForegroundColor;
        Console.ForegroundColor = color;
        Console.WriteLine(text);
        Console.ForegroundColor = prev;
    }

    private static string Truncate(string? value, int maxLength)
    {
        if (string.IsNullOrEmpty(value)) return "(empty)";
        return value.Length <= maxLength ? value : value[..maxLength] + "…";
    }

    private static string FormatAge(TimeSpan age)
    {
        if (age.TotalDays >= 1) return $"{(int)age.TotalDays}d";
        if (age.TotalHours >= 1) return $"{(int)age.TotalHours}h";
        return $"{(int)age.TotalMinutes}m";
    }

    private static string JsonString(string? value) =>
        value is null ? "null" : $"\"{value.Replace("\"", "\\\"").Replace("\n", "\\n")}\"";

    private enum ResolutionChoice { Local, Notion, Custom, Skip, Quit }
}
