#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace NotionTaskSync.Services;

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Microsoft.Extensions.Logging;
using NotionTaskSync.Domain.Models;

/// <summary>
/// Generates structured diff previews for conflicting task property values using an
/// LCS-based algorithm, and renders results as unified-diff text for terminal or log output.
/// Designed to be called before a resolution strategy is applied so that operators can
/// review exactly what changed on each side before committing to a winner.
/// </summary>
public class ConflictDiffService
{
    private const int ContextLines = 3;

    private readonly ILogger<ConflictDiffService> _logger;

    /// <summary>
    /// Initialises a new instance of <see cref="ConflictDiffService"/>.
    /// </summary>
    public ConflictDiffService(ILogger<ConflictDiffService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Generates a <see cref="ConflictDiffResult"/> by comparing the local and Notion values
    /// stored in <paramref name="conflict"/>.
    /// </summary>
    /// <param name="conflict">The conflict whose two sides are to be compared.</param>
    /// <param name="cancellationToken">Token used to abort the operation.</param>
    /// <returns>A diff result containing annotated lines and summary statistics.</returns>
    public async Task<ConflictDiffResult> GenerateDiffAsync(
        ConflictResolution conflict,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(conflict);

        _logger.LogDebug(
            "Generating diff for conflict {ConflictId} on property '{Property}'",
            conflict.Id, conflict.PropertyName);

        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            var result = await GenerateDiffForPropertyAsync(
                conflict.LocalValue,
                conflict.NotionValue,
                conflict.PropertyName ?? "value",
                conflict.Id,
                cancellationToken);

            _logger.LogDebug(
                "Diff generated for conflict {ConflictId}: +{Added} -{Removed}",
                conflict.Id, result.AddedCount, result.RemovedCount);

            return result;
        }
        catch (OperationCanceledException)
        {
            _logger.LogDebug("Diff generation cancelled for conflict {ConflictId}", conflict.Id);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate diff for conflict {ConflictId}", conflict.Id);
            throw;
        }
    }

    /// <summary>
    /// Compares <paramref name="localValue"/> against <paramref name="notionValue"/> line by line
    /// using the longest-common-subsequence algorithm and returns a structured diff.
    /// </summary>
    /// <param name="localValue">The local (file-side) text.</param>
    /// <param name="notionValue">The Notion-side text.</param>
    /// <param name="propertyName">Label used in the diff header (e.g. <c>Title</c>).</param>
    /// <param name="conflictId">Optional identifier to embed in the result for traceability.</param>
    /// <param name="cancellationToken">Token used to abort the operation.</param>
    public async Task<ConflictDiffResult> GenerateDiffForPropertyAsync(
        string? localValue,
        string? notionValue,
        string propertyName,
        Guid conflictId = default,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var localLines  = SplitLines(localValue);
        var notionLines = SplitLines(notionValue);
        var dp          = ComputeLcs(localLines, notionLines);
        var diffLines   = BacktrackDiff(dp, localLines, notionLines);

        await System.Threading.Tasks.Task.CompletedTask;

        return new ConflictDiffResult
        {
            ConflictId   = conflictId,
            PropertyName = propertyName,
            LocalValue   = localValue,
            NotionValue  = notionValue,
            Lines        = diffLines,
            GeneratedAt  = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Renders a <see cref="ConflictDiffResult"/> as a unified-diff string with hunk headers
    /// and <see cref="ContextLines"/> lines of surrounding context per changed region.
    /// Suitable for terminal output, log files, or plain-text review workflows.
    /// </summary>
    /// <param name="diff">The diff result to render.</param>
    /// <param name="cancellationToken">Token used to abort the operation.</param>
    /// <returns>A formatted unified-diff string.</returns>
    public async Task<string> RenderAsTextAsync(
        ConflictDiffResult diff,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(diff);
        cancellationToken.ThrowIfCancellationRequested();

        await System.Threading.Tasks.Task.CompletedTask;

        var sb = new StringBuilder();
        sb.AppendLine($"--- local/{diff.PropertyName}");
        sb.AppendLine($"+++ notion/{diff.PropertyName}");

        if (diff.IsIdentical)
        {
            sb.AppendLine("(no differences)");
            return sb.ToString();
        }

        var included  = ComputeIncludedIndices(diff.Lines);
        int? lastIdx  = null;

        for (int i = 0; i < diff.Lines.Count; i++)
        {
            if (!included.Contains(i))
                continue;

            if (lastIdx.HasValue && i > lastIdx.Value + 1)
            {
                var hl = diff.Lines[i];
                sb.AppendLine($"@@ -{hl.LocalLineNumber ?? 0} +{hl.NotionLineNumber ?? 0} @@");
            }

            var dl = diff.Lines[i];
            sb.AppendLine($"{dl.Sigil} {dl.Text}");
            lastIdx = i;
        }

        sb.AppendLine();
        sb.AppendLine($"Summary: +{diff.AddedCount} added, -{diff.RemovedCount} removed");

        return sb.ToString();
    }

    /// <summary>
    /// Generates diffs for every conflict in <paramref name="conflicts"/> and returns
    /// the results in a dictionary keyed by conflict ID.
    /// </summary>
    /// <param name="conflicts">The collection of conflicts to process.</param>
    /// <param name="cancellationToken">Token used to abort the batch.</param>
    public async Task<Dictionary<Guid, ConflictDiffResult>> GenerateBatchDiffsAsync(
        IReadOnlyList<ConflictResolution> conflicts,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(conflicts);

        var results = new Dictionary<Guid, ConflictDiffResult>(conflicts.Count);

        foreach (var conflict in conflicts)
        {
            cancellationToken.ThrowIfCancellationRequested();
            results[conflict.Id] = await GenerateDiffAsync(conflict, cancellationToken).ConfigureAwait(false);
        }

        _logger.LogInformation(
            "Batch diff complete: {Count} conflict(s) processed", results.Count);

        return results;
    }

    // -------------------------------------------------------------------------
    // Private helpers
    // -------------------------------------------------------------------------

    private static string[] SplitLines(string? value) =>
        string.IsNullOrEmpty(value)
            ? new string[0]
            : value.Replace("\r\n", "\n").Split('\n');

    private static int[,] ComputeLcs(string[] local, string[] notion)
    {
        int m  = local.Length;
        int n  = notion.Length;
        var dp = new int[m + 1, n + 1];

        for (int i = 1; i <= m; i++)
            for (int j = 1; j <= n; j++)
                dp[i, j] = local[i - 1] == notion[j - 1]
                    ? dp[i - 1, j - 1] + 1
                    : Math.Max(dp[i - 1, j], dp[i, j - 1]);

        return dp;
    }

    private static List<DiffLine> BacktrackDiff(int[,] dp, string[] local, string[] notion)
    {
        var stack = new Stack<DiffLine>();
        int i = local.Length;
        int j = notion.Length;

        while (i > 0 || j > 0)
        {
            if (i > 0 && j > 0 && local[i - 1] == notion[j - 1])
            {
                stack.Push(new DiffLine { Text = local[i - 1], Kind = DiffLineKind.Context, LocalLineNumber = i, NotionLineNumber = j });
                i--; j--;
            }
            else if (j > 0 && (i == 0 || dp[i, j - 1] >= dp[i - 1, j]))
            {
                stack.Push(new DiffLine { Text = notion[j - 1], Kind = DiffLineKind.Added, NotionLineNumber = j });
                j--;
            }
            else
            {
                stack.Push(new DiffLine { Text = local[i - 1], Kind = DiffLineKind.Removed, LocalLineNumber = i });
                i--;
            }
        }

        var lines = new List<DiffLine>(stack.Count);
        while (stack.Count > 0)
            lines.Add(stack.Pop());

        return lines;
    }

    private static HashSet<int> ComputeIncludedIndices(List<DiffLine> lines)
    {
        var result = new HashSet<int>();

        for (int i = 0; i < lines.Count; i++)
        {
            if (lines[i].Kind == DiffLineKind.Context)
                continue;

            int lo = Math.Max(0, i - ContextLines);
            int hi = Math.Min(lines.Count - 1, i + ContextLines);

            for (int c = lo; c <= hi; c++)
                result.Add(c);
        }

        return result;
    }
}
