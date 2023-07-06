// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace NotionTaskSync.Collaboration;

using System;
using System.ComponentModel.DataAnnotations;

/// <summary>
/// Fine-grained configuration for real-time collaboration sessions.
/// Bind from <c>appsettings.json → Collaboration</c> via the Options pattern and
/// override per-session through the <c>configure</c> delegate in
/// <see cref="CollaborationExtensions.AddRealtimeCollaboration"/>.
/// </summary>
public sealed class CollaborationSessionOptions
{
    /// <summary>Configuration section name used when binding from <c>appsettings.json</c>.</summary>
    public const string SectionName = "Collaboration";

    /// <summary>
    /// Gets or sets the maximum number of concurrent participants allowed per session.
    /// When the limit is reached, <see cref="ICollaborationSession.JoinAsync"/> throws.
    /// Defaults to <c>20</c>.
    /// </summary>
    [Range(1, 500)]
    public int MaxParticipantsPerSession { get; set; } = 20;

    /// <summary>
    /// Gets or sets how many server-side operations are retained in the in-memory replay
    /// buffer. Clients that reconnect and need more history than this must perform a full
    /// state snapshot instead of a log replay. Defaults to <c>1 000</c>.
    /// </summary>
    [Range(100, 100_000)]
    public int OperationLogCapacity { get; set; } = 1_000;

    /// <summary>
    /// Gets or sets the maximum number of <see cref="Domain.Models.Operation"/> entries
    /// allowed in a single submitted <see cref="Domain.Models.OperationBatch"/>.
    /// Oversized batches are rejected before OT processing. Defaults to <c>50</c>.
    /// </summary>
    [Range(1, 500)]
    public int MaxOperationsPerBatch { get; set; } = 50;

    /// <summary>
    /// Gets or sets how long a session may be completely idle (no submitted batches, no joins)
    /// before it is automatically closed and all participants are evicted.
    /// Defaults to <c>30 minutes</c>.
    /// </summary>
    public TimeSpan IdleTimeout { get; set; } = TimeSpan.FromMinutes(30);

    /// <summary>
    /// Gets or sets the expected interval between participant heartbeats.
    /// The session manager evicts participants that miss two consecutive heartbeat windows.
    /// Defaults to <c>30 seconds</c>.
    /// </summary>
    public TimeSpan HeartbeatInterval { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Gets or sets whether concurrent text-range operations (Insert/Delete) on the same
    /// property are merged automatically by the OT engine.
    /// When <see langword="false"/>, any positional overlap is escalated as a conflict.
    /// Defaults to <see langword="true"/>.
    /// </summary>
    public bool AllowAutomaticTextMerge { get; set; } = true;

    /// <summary>
    /// Gets or sets the strategy applied when two concurrent scalar Update operations
    /// target the same property and cannot be merged automatically.
    /// Defaults to <see cref="CollaborationConflictPolicy.LastWriterWins"/>.
    /// </summary>
    public CollaborationConflictPolicy ScalarConflictPolicy { get; set; } =
        CollaborationConflictPolicy.LastWriterWins;

    /// <summary>
    /// Gets or sets whether every accepted server operation is immediately written to the
    /// <see cref="Data.Repositories.IChangeLogRepository"/> for durability.
    /// Disabling this improves throughput at the cost of losing collaborative edits on crash.
    /// Defaults to <see langword="true"/>.
    /// </summary>
    public bool PersistOperationsToChangeLog { get; set; } = true;

    /// <summary>
    /// Gets or sets whether <see cref="ParticipantRole.Observer"/> participants are
    /// permitted to submit operation batches.
    /// Defaults to <see langword="false"/>.
    /// </summary>
    public bool AllowObserverEdits { get; set; } = false;

    /// <summary>
    /// Performs a logical validity check on all option values.
    /// Returns <see langword="false"/> when any range or time constraint is violated.
    /// </summary>
    public bool Validate() =>
        MaxParticipantsPerSession is >= 1 and <= 500 &&
        OperationLogCapacity is >= 100 and <= 100_000 &&
        MaxOperationsPerBatch is >= 1 and <= 500 &&
        IdleTimeout > TimeSpan.Zero &&
        HeartbeatInterval > TimeSpan.Zero;
}

/// <summary>
/// Determines how the OT engine resolves two concurrent scalar Update operations
/// that both target the same task property.
/// </summary>
public enum CollaborationConflictPolicy
{
    /// <summary>
    /// The operation with the later <see cref="Domain.Models.Operation.CreatedAt"/> timestamp wins.
    /// Requires participants to have reasonably synchronised clocks.
    /// </summary>
    LastWriterWins,

    /// <summary>
    /// The operation that arrived at the server first (lower server revision) is preserved;
    /// the later-arriving operation is demoted to a no-op Retain.
    /// </summary>
    FirstWriterWins,

    /// <summary>
    /// Both operations are preserved and the conflict is surfaced to the session owner
    /// for manual resolution through the existing
    /// <see cref="Services.ConflictResolutionService"/> pipeline.
    /// </summary>
    ManualResolution
}
