#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace NotionTaskSync.Services;

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

/// <summary>
/// Point-in-time snapshot of sync progress for a single configuration, combining the
/// timestamp of the last fully-completed cycle with the set of items already applied
/// during a cycle that may not have finished (e.g. because the process crashed).
/// </summary>
public sealed class SyncCheckpoint
{
    /// <summary>
    /// Gets the timestamp of the last sync cycle that completed successfully end-to-end,
    /// or <see langword="null"/> if no cycle has ever completed for this configuration.
    /// </summary>
    public DateTime? LastSuccessfulSyncAt { get; init; }

    /// <summary>
    /// Gets the set of item keys that have already been applied, possibly during a cycle
    /// that crashed before reaching <see cref="LastSuccessfulSyncAt"/>. Consulted so a
    /// re-run does not double-apply them.
    /// </summary>
    public HashSet<string> AppliedItemKeys { get; init; } = new(StringComparer.Ordinal);
}

/// <summary>
/// Persists sync progress so a crashed cycle can be resumed without re-applying items
/// that were already pushed or pulled.
/// </summary>
public interface ISyncCheckpointStore
{
    /// <summary>
    /// Loads the current checkpoint for a sync configuration.
    /// </summary>
    /// <param name="configId">Identifier of the sync configuration.</param>
    /// <returns>The persisted checkpoint, or an empty checkpoint if none exists yet.</returns>
    SyncCheckpoint LoadCheckpoint(Guid configId);

    /// <summary>
    /// Records that a single item has been applied, flushing to durable storage immediately
    /// so the marker survives a crash that occurs before the cycle finishes.
    /// </summary>
    /// <param name="configId">Identifier of the sync configuration.</param>
    /// <param name="itemKey">Stable key identifying the applied item.</param>
    void MarkItemApplied(Guid configId, string itemKey);

    /// <summary>
    /// Records that a sync cycle finished successfully end-to-end, advancing
    /// <see cref="SyncCheckpoint.LastSuccessfulSyncAt"/> and clearing the per-item markers
    /// accumulated during the cycle, since they are now subsumed by the new timestamp.
    /// </summary>
    /// <param name="configId">Identifier of the sync configuration.</param>
    /// <param name="completedAt">Timestamp the cycle completed at.</param>
    void CompleteCycle(Guid configId, DateTime completedAt);
}

/// <summary>
/// File-backed implementation of <see cref="ISyncCheckpointStore"/>. Per-item markers are
/// appended to a plain-text file, one key per line, with the file flushed and closed on
/// every call so a process crash mid-cycle only ever loses the marker currently being
/// written, never markers already recorded. The last-successful-cycle timestamp is stored
/// separately and only updated once the whole cycle has finished.
/// </summary>
public sealed class SyncCheckpointStore : ISyncCheckpointStore
{
    private readonly string _directory;

    /// <summary>
    /// Initializes a new instance of <see cref="SyncCheckpointStore"/>.
    /// </summary>
    /// <param name="directoryPath">
    /// Directory where checkpoint files are stored. Created if it does not exist.
    /// Defaults to a ".sync-checkpoints" folder under the application base directory.
    /// </param>
    public SyncCheckpointStore(string? directoryPath = null)
    {
        _directory = string.IsNullOrWhiteSpace(directoryPath)
            ? Path.Combine(AppContext.BaseDirectory, ".sync-checkpoints")
            : directoryPath;

        Directory.CreateDirectory(_directory);
    }

    /// <inheritdoc />
    /// <exception cref="ArgumentException"><paramref name="configId"/> is <see cref="Guid.Empty"/>.</exception>
    public SyncCheckpoint LoadCheckpoint(Guid configId)
    {
        if (configId == Guid.Empty)
            throw new ArgumentException("Configuration id must not be empty.", nameof(configId));

        DateTime? lastSuccessfulSyncAt = null;
        var statePath = GetStateFilePath(configId);
        if (File.Exists(statePath))
        {
            var json = File.ReadAllText(statePath);
            var state = JsonSerializer.Deserialize<CheckpointState>(json);
            lastSuccessfulSyncAt = state?.LastSuccessfulSyncAt;
        }

        var appliedItemKeys = new HashSet<string>(StringComparer.Ordinal);
        var appliedPath = GetAppliedItemsFilePath(configId);
        if (File.Exists(appliedPath))
        {
            foreach (var line in File.ReadAllLines(appliedPath))
            {
                if (!string.IsNullOrWhiteSpace(line))
                    appliedItemKeys.Add(line);
            }
        }

        return new SyncCheckpoint
        {
            LastSuccessfulSyncAt = lastSuccessfulSyncAt,
            AppliedItemKeys = appliedItemKeys
        };
    }

    /// <inheritdoc />
    /// <exception cref="ArgumentException">
    /// <paramref name="configId"/> is <see cref="Guid.Empty"/>, or <paramref name="itemKey"/> is null or empty.
    /// </exception>
    public void MarkItemApplied(Guid configId, string itemKey)
    {
        if (configId == Guid.Empty)
            throw new ArgumentException("Configuration id must not be empty.", nameof(configId));
        ArgumentException.ThrowIfNullOrEmpty(itemKey);

        // Open, append, flush and close on every call so the marker is durable the
        // instant this method returns - if the process dies on the very next line,
        // this item is still recorded as applied on the next run.
        using var writer = new StreamWriter(GetAppliedItemsFilePath(configId), append: true);
        writer.WriteLine(itemKey);
    }

    /// <inheritdoc />
    /// <exception cref="ArgumentException"><paramref name="configId"/> is <see cref="Guid.Empty"/>.</exception>
    public void CompleteCycle(Guid configId, DateTime completedAt)
    {
        if (configId == Guid.Empty)
            throw new ArgumentException("Configuration id must not be empty.", nameof(configId));

        var state = new CheckpointState { LastSuccessfulSyncAt = completedAt };
        File.WriteAllText(GetStateFilePath(configId), JsonSerializer.Serialize(state));

        var appliedPath = GetAppliedItemsFilePath(configId);
        if (File.Exists(appliedPath))
            File.Delete(appliedPath);
    }

    private string GetStateFilePath(Guid configId) =>
        Path.Combine(_directory, $"{configId:N}.checkpoint.json");

    private string GetAppliedItemsFilePath(Guid configId) =>
        Path.Combine(_directory, $"{configId:N}.applied.jsonl");

    private sealed class CheckpointState
    {
        public DateTime? LastSuccessfulSyncAt { get; set; }
    }
}
