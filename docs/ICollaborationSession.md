# ICollaborationSession

`ICollaborationSession` defines the contract for a real-time collaboration session within the `notion-task-sync` project. It serves as the primary interface for managing participants, routing operations through the operational transformation (OT) engine, and maintaining a server-linearised operation log to ensure consistent state across all connected clients.

## API

### Participant
Represents a user or entity actively participating in the collaboration session.

### ServerOperation
Represents an atomic change operation that has been processed, transformed, and linearised by the server for distribution to all session participants.

### SessionJoinResult
Contains the outcome of a join request, including the participant's initial bootstrap state and the current server operation log required to synchronize the client.

### BatchAcknowledgement
Represents the server's confirmation of a batch of operations. It provides the final, transformed operations and their corresponding revisions as persisted on the server, allowing clients to reconcile their local state.

## Usage

### Joining a Session
```csharp
// Example of joining a collaboration session
var participant = new Participant("user-123", "Alice");
var session = await collaborationManager.GetOrCreateAsync("scope-abc", options);

SessionJoinResult joinResult = await session.JoinAsync(participant);
Console.WriteLine($"Joined session at revision: {joinResult.Revision}");
```

### Submitting Operations
```csharp
// Example of submitting a batch of changes to the session
var batch = new OperationBatch(operations);
BatchAcknowledgement ack = await session.SubmitBatchAsync(batch);

foreach (var op in ack.TransformedOperations)
{
    // Apply server-confirmed transformations to local state
    ApplyOperation(op);
}
```

## Notes

- **Thread-Safety:** Implementations of `ICollaborationSession` are generally expected to handle concurrent operations internally (e.g., via locking or actor-based models) to ensure consistent linearisation of the operation log.
- **Exceptions:** Methods may throw `CollaborationException` if the session is in an invalid state, if a participant limit is reached, or if batch submission violates session rules (e.g., observer restrictions).
- **Asynchrony:** All operations involving network communication or server-side transformation are asynchronous and return `Task` (or `Task<T>`).
- **Cancellation:** Most methods accept an optional `CancellationToken` to allow callers to abort pending operations.
