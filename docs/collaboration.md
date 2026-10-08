# Collaboration Model

This document describes the real-time collaboration contract in `notion-task-sync`: the session interface (`ICollaborationSession`), its configuration (`CollaborationSessionOptions`), and the operational transformation (OT) data model (`Operation`, `OperationBatch`, `TransformResult`, `VectorClock`) that the session routes and linearises.

Source files:

- `Collaboration/ICollaborationSession.cs` - session, registry, and wire records
- `Collaboration/CollaborationSessionOptions.cs` - options and conflict policy enum
- `Domain/Models/OperationalTransform.cs` - OT model

## ICollaborationSession

A session is scoped to a single sync scope (a Notion database ID or a local path) and provides:

| Member | Purpose |
| --- | --- |
| `SessionId` | Stable session identifier. |
| `ScopeId` | Notion database ID or local path the session covers. |
| `OpenedAt` | UTC time the session was opened. |
| `CurrentRevision` | Server revision. Incremented atomically with each accepted operation. |
| `ActiveParticipants` | Snapshot of current participants (`IReadOnlyList<Participant>`). |
| `JoinAsync(Participant, ct)` | Registers a participant and returns `SessionJoinResult` with the full operation log up to the current revision. Throws `CollaborationException` when the participant limit is reached. |
| `LeaveAsync(participantId, ct)` | Removes a participant. Remaining participants are notified via the event bus. |
| `SubmitBatchAsync(OperationBatch, ct)` | Sends a batch through server-side OT and linearisation. Returns `BatchAcknowledgement`. Throws `CollaborationException` when the batch exceeds size limits, the submitter is not a member, or an observer submits while observer edits are disabled. |
| `GetLogSince(fromRevision)` | Returns `ServerOperation` entries from `fromRevision` (inclusive). Used for catch-up after reconnect without a full snapshot. |

`ICollaborationSessionRegistry` manages sessions by ID and scope:

- `GetOrCreateAsync(scopeId, options, ct)` - opens a new session or returns the existing one for the scope.
- `TryGet(sessionId)` - returns the session or `null`.
- `ActiveSessions` - point-in-time list of open sessions.
- `CloseAsync(sessionId, ct)` - closes the session and evicts all remaining participants.

Implementations must be thread-safe.

### Wire records

| Record | Fields | Meaning |
| --- | --- | --- |
| `Participant(ParticipantId, DisplayName, Role, JoinedAt)` | `Role` is a `ParticipantRole` | A participant in a session. |
| `ServerOperation(Revision, Operation, AppliedAt)` | | An operation as accepted and stored by the server, with its revision. |
| `SessionJoinResult(SessionId, CurrentRevision, Participants, OperationLog)` | | Bootstrap state for a joining participant. |
| `BatchAcknowledgement(BatchId, StartRevision, EndRevision, TransformedOperations)` | | Confirmation of a batch. `TransformedOperations` may differ from the submitted operations if OT rebasing was needed. |

### ParticipantRole

| Role | Permissions |
| --- | --- |
| `Observer` | Reads the log and receives updates. Cannot submit operations unless `AllowObserverEdits` is enabled. |
| `Editor` | Submits operations. Cannot manage the session lifecycle. |
| `Owner` | Submits operations and can close the session or evict participants. |

## CollaborationSessionOptions

Bound from the `Collaboration` configuration section (`CollaborationSessionOptions.SectionName`). Can be overridden per session through the `configure` delegate of `CollaborationExtensions.AddRealtimeCollaboration`.

| Property | Default | Range / constraint | Effect |
| --- | --- | --- | --- |
| `MaxParticipantsPerSession` | `20` | 1-500 | Participant cap. `JoinAsync` throws when reached. |
| `OperationLogCapacity` | `1000` | 100-100 000 | Size of the in-memory replay buffer. Clients that need older history must take a full snapshot. |
| `MaxOperationsPerBatch` | `50` | 1-500 | Oversized batches are rejected before OT runs. |
| `IdleTimeout` | `30 min` | > 0 | Session with no batches and no joins is closed and participants are evicted. |
| `HeartbeatInterval` | `30 s` | > 0 | Expected heartbeat interval. Participants missing two consecutive windows are evicted. |
| `AllowAutomaticTextMerge` | `true` | | When `true`, overlapping Insert/Delete on the same text property are merged by OT. When `false`, positional overlap is escalated as a conflict. |
| `ScalarConflictPolicy` | `LastWriterWins` | | Strategy for concurrent scalar `Update` operations on the same property that cannot be merged. |
| `PersistOperationsToChangeLog` | `true` | | Writes each accepted operation to `IChangeLogRepository`. Disabling improves throughput but loses edits on crash. |
| `AllowObserverEdits` | `false` | | Lets `Observer` participants submit batches. |

`Validate()` returns `false` when any range or time constraint above is violated. It checks the values; it does not throw.

### CollaborationConflictPolicy

| Value | Behaviour |
| --- | --- |
| `LastWriterWins` | The operation with the later `CreatedAt` wins. Depends on synchronised client clocks. |
| `FirstWriterWins` | The operation with the lower server revision is kept. The later one becomes a no-op `Retain`. |
| `ManualResolution` | Both operations are kept and the conflict goes to the session owner through `ConflictResolutionService`. |

## OperationalTransform model

### OperationType

| Value | Meaning |
| --- | --- |
| `Insert` | Inserts content at a character position in a text property. |
| `Delete` | Removes a range from a text property. |
| `Update` | Replaces the whole value of a scalar or enum property. |
| `Retain` | No-op placeholder used as a structural component in composite deltas. |
| `Move` | Repositions a task within an ordered collection or database. |

### Operation

`Operation` is an immutable record applied to one property of one task:

| Field | Meaning |
| --- | --- |
| `Id` | Unique operation ID. |
| `TaskId` | Targeted task. |
| `PropertyName` | Targeted property. |
| `Type` | `OperationType`. |
| `Position` | Character offset for text ops, item index for list ops, `-1` for whole-value ops. |
| `Length` | Characters to remove (`Delete`, `Retain`), otherwise `0`. |
| `Payload` | New value, inserted text, or serialised delta. |
| `PreviousValue` | Value before the operation. Needed for inversion and rollback. |
| `Context` | `TransformContext` (causal metadata). |
| `CreatedAt` | UTC creation time on the originating client. |

Members:

- `Operation.Create(...)` - factory that sets `Id` (new GUID) and `CreatedAt` (UTC now).
- `Invert()` - returns the inverse for undo. Swaps `Insert` and `Delete`, and swaps `Payload` with `PreviousValue`. Other types keep their `Type`. Assigns a new `Id` and `CreatedAt`.
- `IsTextualOp` - `true` for `Insert`, `Delete`, `Retain`.

### TransformContext

`TransformContext(SessionId, ParticipantId, SequenceNumber, Clock)` carries causal context. `SequenceNumber` is a per-participant counter used for gap detection. `Clock` is a `VectorClock` snapshot taken when the operation was created.

### VectorClock

A Lamport-style vector clock, thread-safe:

- `Tick(participantId)` - advances one participant's component.
- `Get(participantId)` - returns the component, or `0` if unknown.
- `Merge(other)` - component-wise maximum. Call on receipt of a remote operation.
- `HappensBefore(other)` - `true` when this clock is less than or equal to `other` in every component and strictly less in at least one.
- `Clone()` - deep copy.

### OperationBatch

`OperationBatch` is an ordered group of operations from one participant, sent in one round-trip. All operations share causal context and are applied together or not at all.

- `BatchId` - new GUID by default.
- `SessionId`, `ParticipantId` - required.
- `Operations` - applied in list order.
- `CreatedAt` - client assembly time (UTC).
- `IsEmpty` - `true` when there are no operations.

### TransformResult

`TransformResult(Transformed, HasConflict, ConflictReason)` is the outcome of transforming two concurrent operations. `Transformed` is the rebased operation, safe to apply after the concurrent one. `HasConflict` is `true` when the transform needed an ambiguous or lossy resolution, and `ConflictReason` explains why.

## Flow

1. A client calls `JoinAsync` and receives `SessionJoinResult`, including the operation log, to bootstrap local state.
2. Local edits become `Operation` values, grouped into an `OperationBatch` (at most `MaxOperationsPerBatch` entries).
3. `SubmitBatchAsync` runs OT on the server. Each accepted operation gets the next revision, and `CurrentRevision` advances.
4. The client receives `BatchAcknowledgement` and reconciles any rebased operations in `TransformedOperations`.
5. After a reconnect, the client calls `GetLogSince(lastRevision)` to catch up. If the gap is larger than `OperationLogCapacity`, it takes a full snapshot instead.
