// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace NotionTaskSync.Domain.Models;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

/// <summary>Classifies the kind of change an operation applies to task content.</summary>
public enum OperationType
{
    /// <summary>Inserts content at a specific position within a text property.</summary>
    Insert,
    /// <summary>Removes a range of content from a text property.</summary>
    Delete,
    /// <summary>Replaces the entire value of a scalar or enum property.</summary>
    Update,
    /// <summary>No-op placeholder used as a structural component in composite deltas.</summary>
    Retain,
    /// <summary>Repositions a task within an ordered collection or database.</summary>
    Move
}

/// <summary>
/// A Lamport-style vector clock for tracking causal ordering of distributed operations.
/// Thread-safe for concurrent reads and writes from multiple collaboration participants.
/// </summary>
public sealed class VectorClock
{
    private readonly ConcurrentDictionary<string, long> _components =
        new(StringComparer.Ordinal);

    /// <summary>Gets a snapshot of all participant clock components.</summary>
    public IReadOnlyDictionary<string, long> Components => _components;

    /// <summary>Advances the logical time for <paramref name="participantId"/> by one tick.</summary>
    public void Tick(string participantId) =>
        _components.AddOrUpdate(participantId, 1L, (_, v) => v + 1);

    /// <summary>Returns the current logical time for <paramref name="participantId"/>, or 0 if unknown.</summary>
    public long Get(string participantId) =>
        _components.TryGetValue(participantId, out var v) ? v : 0L;

    /// <summary>
    /// Merges <paramref name="other"/> into this clock by taking the component-wise maximum.
    /// Call this when receiving a remote operation to advance local causal knowledge.
    /// </summary>
    public void Merge(VectorClock other)
    {
        foreach (var (id, remote) in other._components)
            _components.AddOrUpdate(id, remote, (_, local) => Math.Max(local, remote));
    }

    /// <summary>
    /// Returns <see langword="true"/> when this clock strictly happens-before <paramref name="other"/>:
    /// every component is ≤ the matching component of <paramref name="other"/>,
    /// with at least one strictly less.
    /// </summary>
    public bool HappensBefore(VectorClock other)
    {
        var allIds = _components.Keys
            .Concat(other._components.Keys)
            .Distinct(StringComparer.Ordinal);
        bool anyLess = false;
        foreach (var id in allIds)
        {
            long a = Get(id), b = other.Get(id);
            if (a > b) return false;
            if (a < b) anyLess = true;
        }
        return anyLess;
    }

    /// <summary>Produces a deep copy of this clock.</summary>
    public VectorClock Clone()
    {
        var copy = new VectorClock();
        foreach (var (id, v) in _components) copy._components[id] = v;
        return copy;
    }
}

/// <summary>Causal metadata attached to every distributed operation.</summary>
/// <param name="SessionId">The collaboration session this operation belongs to.</param>
/// <param name="ParticipantId">Stable identifier of the originating participant.</param>
/// <param name="SequenceNumber">Per-participant monotonic counter for gap detection and replay.</param>
/// <param name="Clock">Snapshot of the participant's vector clock at the moment of creation.</param>
public sealed record TransformContext(
    string SessionId,
    string ParticipantId,
    long SequenceNumber,
    VectorClock Clock);

/// <summary>
/// An immutable, invertible operation applied to a single task property.
/// Text-addressed operations use <see cref="Position"/> and <see cref="Length"/>;
/// scalar-property operations populate only <see cref="Payload"/>.
/// </summary>
/// <param name="Id">Unique identifier of this operation instance.</param>
/// <param name="TaskId">The task targeted by this operation.</param>
/// <param name="PropertyName">Name of the task property being modified.</param>
/// <param name="Type">Kind of change this operation represents.</param>
/// <param name="Position">
/// Character offset (text ops) or item index (list ops); -1 for whole-value ops.
/// </param>
/// <param name="Length">
/// Number of characters to remove (Delete/Retain) or 0 for non-range operations.
/// </param>
/// <param name="Payload">New value, inserted content, or serialised delta.</param>
/// <param name="PreviousValue">
/// Original value before this operation; required for inversion and rollback.
/// </param>
/// <param name="Context">Causal context at the moment this operation was authored.</param>
/// <param name="CreatedAt">UTC creation timestamp on the originating client.</param>
public sealed record Operation(
    Guid Id,
    Guid TaskId,
    string PropertyName,
    OperationType Type,
    int Position,
    int Length,
    string? Payload,
    string? PreviousValue,
    TransformContext Context,
    DateTime CreatedAt)
{
    /// <summary>
    /// Factory that auto-populates <see cref="Id"/> and <see cref="CreatedAt"/>.
    /// </summary>
    public static Operation Create(
        Guid taskId,
        string propertyName,
        OperationType type,
        int position,
        int length,
        string? payload,
        string? previousValue,
        TransformContext context) =>
        new(Guid.NewGuid(), taskId, propertyName, type,
            position, length, payload, previousValue, context, DateTime.UtcNow);

    /// <summary>
    /// Returns the logical inverse of this operation for undo or rollback.
    /// Insert ↔ Delete are swapped; Update swaps <see cref="Payload"/> and <see cref="PreviousValue"/>.
    /// </summary>
    public Operation Invert() => this with
    {
        Id = Guid.NewGuid(),
        Type = Type switch
        {
            OperationType.Insert => OperationType.Delete,
            OperationType.Delete => OperationType.Insert,
            _ => Type
        },
        Payload = PreviousValue,
        PreviousValue = Payload,
        CreatedAt = DateTime.UtcNow
    };

    /// <summary>
    /// Returns <see langword="true"/> when this operation addresses a character-level
    /// position within a text property.
    /// </summary>
    public bool IsTextualOp =>
        Type is OperationType.Insert or OperationType.Delete or OperationType.Retain;
}

/// <summary>Outcome of transforming two concurrent operations against each other.</summary>
/// <param name="Transformed">The rebased operation, safe to apply after the concurrent one.</param>
/// <param name="HasConflict">
/// <see langword="true"/> when the transform required an ambiguous or lossy resolution.
/// </param>
/// <param name="ConflictReason">Human-readable explanation of the conflict, if any.</param>
public sealed record TransformResult(
    Operation Transformed,
    bool HasConflict,
    string? ConflictReason = null);

/// <summary>
/// An ordered, atomic group of operations from the same participant submitted in one round-trip.
/// All operations share causal context and must be applied together or not at all.
/// </summary>
public sealed class OperationBatch
{
    /// <summary>Gets the unique batch identifier.</summary>
    public Guid BatchId { get; init; } = Guid.NewGuid();

    /// <summary>Gets the session in which this batch was assembled.</summary>
    public required string SessionId { get; init; }

    /// <summary>Gets the participant who assembled this batch.</summary>
    public required string ParticipantId { get; init; }

    /// <summary>Gets the operations in this batch, in the order they must be applied.</summary>
    public IReadOnlyList<Operation> Operations { get; init; } = [];

    /// <summary>Gets the UTC time this batch was assembled on the client.</summary>
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

    /// <summary>Returns <see langword="true"/> when this batch contains no operations.</summary>
    public bool IsEmpty => Operations.Count == 0;
}
