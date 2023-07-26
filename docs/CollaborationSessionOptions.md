# CollaborationSessionOptions

`CollaborationSessionOptions` is a configuration class that defines the behavioral and performance parameters for a synchronized collaboration session within the `notion-task-sync` framework. It provides granular control over session constraints, network heartbeat frequencies, operational logging, and conflict resolution strategies, allowing developers to optimize for either responsiveness or data consistency based on specific application requirements.

## API

*   **`int MaxParticipantsPerSession`**
    Gets or sets the maximum number of concurrent participants permitted in a single session. Exceeding this limit will cause the session establishment to fail.
*   **`int OperationLogCapacity`**
    Gets or sets the maximum number of operations maintained in the local log buffer. When the capacity is reached, the oldest operations may be truncated or flushed to secondary storage, depending on configuration.
*   **`int MaxOperationsPerBatch`**
    Gets or sets the maximum number of operations processed in a single batch update to the underlying store. High values may improve throughput but can increase latency per transaction.
*   **`TimeSpan IdleTimeout`**
    Gets or sets the duration of inactivity after which a session is automatically terminated.
*   **`TimeSpan HeartbeatInterval`**
    Gets or sets the frequency at which the client sends a heartbeat signal to the server to maintain active session presence.
*   **`bool AllowAutomaticTextMerge`**
    Gets or sets a value indicating whether text-based conflicts are automatically resolved using an internal merging algorithm.
*   **`CollaborationConflictPolicy ScalarConflictPolicy`**
    Gets or sets the strategy used to resolve conflicts for non-textual data, such as property changes, date updates, or enum selections.
*   **`bool PersistOperationsToChangeLog`**
    Gets or sets a value indicating whether operational history is persisted to a durable change log for auditing or recovery purposes.
*   **`bool AllowObserverEdits`**
    Gets or sets a value indicating whether participants flagged as observers are permitted to perform write operations within the session.
*   **`bool Validate`**
    Gets or sets a value indicating whether the configuration options should be validated against internal constraints during session initialization. If `true`, invalid configurations will throw an `InvalidOperationException` upon session startup.

## Usage

### Basic Configuration

```csharp
var options = new CollaborationSessionOptions
{
    MaxParticipantsPerSession = 10,
    IdleTimeout = TimeSpan.FromMinutes(15),
    HeartbeatInterval = TimeSpan.FromSeconds(30),
    ScalarConflictPolicy = CollaborationConflictPolicy.LastWriterWins,
    Validate = true
};
```

### High-Throughput Environment Configuration

```csharp
var options = new CollaborationSessionOptions
{
    MaxParticipantsPerSession = 50,
    OperationLogCapacity = 5000,
    MaxOperationsPerBatch = 100,
    AllowAutomaticTextMerge = true,
    PersistOperationsToChangeLog = true,
    ScalarConflictPolicy = CollaborationConflictPolicy.ServerWins,
    Validate = true
};
```

## Notes

*   **Thread-Safety**: This class is not thread-safe. Configuration objects should be fully initialized on a single thread before being passed to a session manager. Mutations after a session has started may result in undefined behavior.
*   **Validation Constraints**: When `Validate` is set to `true`, configuring properties with invalid values (e.g., negative `TimeSpan` values, zero `MaxParticipantsPerSession`) will result in an `ArgumentOutOfRangeException` or `InvalidOperationException` when the session service attempts to utilize the options.
*   **Conflict Resolution**: Enabling `AllowAutomaticTextMerge` does not override the `ScalarConflictPolicy`. Scalar conflicts will strictly adhere to the defined policy, while text content will be processed according to the merge algorithm if `AllowAutomaticTextMerge` is enabled.
*   **Resource Management**: Setting a very high `OperationLogCapacity` or `MaxOperationsPerBatch` can increase memory consumption significantly on client devices. Monitor memory usage when adjusting these parameters for large-scale sessions.
