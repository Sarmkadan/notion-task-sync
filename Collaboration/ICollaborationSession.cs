// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace NotionTaskSync.Collaboration;

using NotionTaskSync.Domain.Models;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Defines the contract for a real-time collaboration session tied to a single sync scope.
/// A session tracks participants, routes operations through the OT engine, and maintains
/// a server-linearised operation log that all clients can replay to reach a consistent state.
/// </summary>
public interface ICollaborationSession
{
    /// <summary>Gets the stable identifier of this session.</summary>
    string SessionId { get; }

    /// <summary>Gets the Notion database ID or local path this session is scoped to.</summary>
    string ScopeId { get; }

    /// <summary>Gets the UTC time this session was opened.</summary>
    DateTime OpenedAt { get; }

    /// <summary>Gets the current server revision; incremented atomically with every accepted operation.</summary>
    long CurrentRevision { get; }

    /// <summary>Gets a snapshot of currently active participants.</summary>
    IReadOnlyList<Participant> ActiveParticipants { get; }

    /// <summary>
    /// Registers a new participant and returns their initial bootstrap state,
    /// including the full server operation log up to the current revision.
    /// </summary>
    /// <exception cref="CollaborationException">Thrown when the session's participant limit is reached.</exception>
    Task<SessionJoinResult> JoinAsync(Participant participant, CancellationToken ct = default);

    /// <summary>
    /// Gracefully removes a participant from the session.
    /// Remaining participants are notified via the event bus.
    /// </summary>
    Task LeaveAsync(string participantId, CancellationToken ct = default);

    /// <summary>
    /// Submits an <see cref="OperationBatch"/> for server-side OT transformation and linearisation.
    /// Returns the acknowledged revisions and the transformed operations as stored on the server.
    /// </summary>
    /// <exception cref="CollaborationException">
    /// Thrown when the batch exceeds size limits, the submitter is not a session member,
    /// or an observer submits while observer edits are disabled.
    /// </exception>
    Task<BatchAcknowledgement> SubmitBatchAsync(OperationBatch batch, CancellationToken ct = default);

    /// <summary>
    /// Returns the server operation log starting at <paramref name="fromRevision"/> (inclusive).
    /// Clients use this to catch up after a reconnect without a full state snapshot.
    /// </summary>
    IReadOnlyList<ServerOperation> GetLogSince(long fromRevision);
}

/// <summary>
/// Represents a human or automated agent participating in a collaboration session.
/// </summary>
/// <param name="ParticipantId">Stable unique identifier (e.g. user GUID or service name).</param>
/// <param name="DisplayName">Human-readable label shown in presence indicators.</param>
/// <param name="Role">The participant's role governing edit permissions.</param>
/// <param name="JoinedAt">UTC time the participant entered the session.</param>
public sealed record Participant(
    string ParticipantId,
    string DisplayName,
    ParticipantRole Role,
    DateTime JoinedAt);

/// <summary>Defines the permission level granted to a session participant.</summary>
public enum ParticipantRole
{
    /// <summary>May read the operation log and receive live updates but cannot submit new operations.</summary>
    Observer,

    /// <summary>May submit operations but cannot manage the session lifecycle.</summary>
    Editor,

    /// <summary>May submit operations and control the session lifecycle (close, evict participants).</summary>
    Owner
}

/// <summary>
/// Wraps a client <see cref="Operation"/> together with the server-assigned revision and
/// application timestamp after the operation has been accepted and linearised in the server log.
/// </summary>
/// <param name="Revision">Monotonically increasing server revision at which this operation was applied.</param>
/// <param name="Operation">The (possibly OT-transformed) operation as stored server-side.</param>
/// <param name="AppliedAt">UTC timestamp of server-side application.</param>
public sealed record ServerOperation(long Revision, Operation Operation, DateTime AppliedAt);

/// <summary>
/// Returned by <see cref="ICollaborationSession.JoinAsync"/> and contains everything a
/// joining participant needs to bootstrap its local state without a separate round-trip.
/// </summary>
/// <param name="SessionId">The session the participant has joined.</param>
/// <param name="CurrentRevision">The server revision at join time.</param>
/// <param name="Participants">All currently active participants including the newcomer.</param>
/// <param name="OperationLog">The full server log, enabling the client to reconstruct document state.</param>
public sealed record SessionJoinResult(
    string SessionId,
    long CurrentRevision,
    IReadOnlyList<Participant> Participants,
    IReadOnlyList<ServerOperation> OperationLog);

/// <summary>
/// Acknowledgement returned to the submitter after their <see cref="OperationBatch"/> has been
/// server-ordered and all OT transforms applied.
/// </summary>
/// <param name="BatchId">The batch that was acknowledged.</param>
/// <param name="StartRevision">Server revision of the first operation in this batch.</param>
/// <param name="EndRevision">Server revision of the last operation in this batch.</param>
/// <param name="TransformedOperations">
/// The operations as stored on the server, which may differ from the submitted operations if OT
/// rebasing was required to resolve concurrency with earlier operations.
/// </param>
public sealed record BatchAcknowledgement(
    Guid BatchId,
    long StartRevision,
    long EndRevision,
    IReadOnlyList<ServerOperation> TransformedOperations);

/// <summary>
/// Provides a registry of active sessions keyed by session identifier.
/// Implementations must be thread-safe as sessions are opened and closed concurrently.
/// </summary>
public interface ICollaborationSessionRegistry
{
    /// <summary>Creates a new session for the given scope, or returns the existing one if already open.</summary>
    Task<ICollaborationSession> GetOrCreateAsync(
        string scopeId,
        CollaborationSessionOptions options,
        CancellationToken ct = default);

    /// <summary>Returns the session with the given identifier, or <see langword="null"/> if not found.</summary>
    ICollaborationSession? TryGet(string sessionId);

    /// <summary>Gets a point-in-time list of all currently active sessions.</summary>
    IReadOnlyList<ICollaborationSession> ActiveSessions { get; }

    /// <summary>Closes the named session and evicts all remaining participants.</summary>
    Task CloseAsync(string sessionId, CancellationToken ct = default);
}
