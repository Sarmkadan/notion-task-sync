# Event System

The event system is an in-memory publish-subscribe layer inside `notion-task-sync`. Sync components publish typed event objects to an `EventBus`, and subscribers react to them without a direct reference to the publisher.

The system has three parts:

- **SyncEvents** - event payload classes (`Events/SyncEvents.cs`).
- **EventBus** - the dispatcher that stores subscribers and delivers events (`Events/EventBus.cs`).
- **EventHandlers** - handler classes that react to specific events (`Events/EventHandlers/`).

## SyncEvents

All event classes derive from the abstract `ApplicationEvent` base class (defined in `Events/EventBus.cs`):

| Member | Type | Description |
|---|---|---|
| `EventId` | `Guid` | Unique id, generated on construction. |
| `Timestamp` | `DateTime` | UTC creation time, set on construction. |
| `Source` | `string?` | Optional name of the publisher, e.g. `nameof(SyncWorker)`. |

Event classes defined in `Events/SyncEvents.cs`:

| Event | Key properties | Published by (in production code) |
|---|---|---|
| `SyncStartedEvent` | `SyncConfigId`, `DatabaseId`, `StartTime` | `Workers/SyncWorker.cs` |
| `SyncCompletedEvent` | `SyncConfigId`, `TasksProcessed`, `ChangesDetected`, `ConflictsResolved`, `Duration`, `Success`, `ErrorMessage` | `Workers/SyncWorker.cs` |
| `ConflictDetectedEvent` | `TaskId`, `TaskTitle`, `LocalModifiedAt`, `RemoteModifiedAt`, `ConflictType`, `LocalValues`, `RemoteValues` | None (see "Current state") |
| `ChangeDetectedEvent` | `TaskId`, `ChangeType`, `Source`, `ChangedAt`, `ChangedProperties` | `Integration/WebhookHandler.cs` |
| `TaskSynchronizedEvent` | `TaskId`, `TaskTitle`, `SyncDirection`, `Successful`, `ErrorMessage` | None |
| `BackupCreatedEvent` | `BackupPath`, `TaskCount`, `FileSizeBytes`, `CreatedAt` | None |
| `RateLimitWarningEvent` | `ApiService`, `RequestsRemaining`, `RequestLimit`, `ResetTime` | `Middleware/RateLimitingMiddleware.cs` |
| `ConfigurationChangedEvent` | `ConfigId`, `FieldName`, `OldValue`, `NewValue` | None |
| `ValidationFailedEvent` | `TaskId`, `ValidationType`, `ErrorMessages`, `InvalidData` | None |

Events are plain mutable DTOs. Publishers set properties through object initializers.

Helper extensions for events live in `Events/SyncEventExtensions.cs`, `Events/SyncStartedEventExtensions.cs` and `Events/SyncCompletedEventExtensions.cs`.

## EventBus

`EventBus` (`Events/EventBus.cs`) is a single in-process dispatcher. Subscribers are stored per event type (`Type` as key) in a `Dictionary<Type, List<Delegate>>`, guarded by `lock`.

### Constructor

```csharp
public EventBus(ILogger<EventBus> logger)
```

Throws `ArgumentNullException` if `logger` is null. Registered as a dependency in the DI container.

### Subscribing

```csharp
void Subscribe<T>(Func<T, Task> handler) where T : class   // async handler
void Subscribe<T>(Action<T> handler) where T : class       // sync handler
```

Multiple handlers can be registered for the same event type. Handlers are invoked in registration order.

### Publishing

```csharp
Task PublishAsync<T>(T @event) where T : class   // awaits all async handlers
void Publish<T>(T @event) where T : class        // fire-and-forget style, synchronous
```

Behavior of `PublishAsync<T>`:

1. Takes a snapshot copy of the handler list under the lock, so handlers can subscribe or unsubscribe during dispatch.
2. Invokes each handler. `Func<T, Task>` handlers are started and their tasks collected. `Action<T>` handlers run inline.
3. Any exception thrown by a handler is caught and logged (`LogError`). It does not stop other handlers and is not rethrown.
4. Awaits `Task.WhenAll` over the collected async tasks. Note that an exception inside an async handler is caught only if it is thrown synchronously; faults that happen inside the returned `Task` surface through `Task.WhenAll`.

Behavior of `Publish<T>` (synchronous):

- Runs only `Action<T>` handlers.
- `Func<T, Task>` handlers are skipped and a warning is logged, because they cannot be awaited.

If no handler is registered for the type, both methods return without error (a debug log entry is written).

### Management and diagnostics

| Method | Purpose |
|---|---|
| `int GetSubscriberCount<T>()` | Number of handlers registered for `T`. |
| `void UnsubscribeAll<T>()` | Removes all handlers for `T`. |
| `void Clear()` | Removes all handlers for all types. |
| `Dictionary<string, int> GetSubscriberInfo()` | Event type name to handler count. |

`Events/EventBusValidation.cs` contains validation helpers for `EventBus` instances.

### Usage

```csharp
// Subscribe
eventBus.Subscribe<SyncCompletedEvent>(e =>
{
    Console.WriteLine($"Sync finished: success={e.Success}, tasks={e.TasksProcessed}");
});

// Publish (async, as SyncWorker does)
await eventBus.PublishAsync(new SyncCompletedEvent
{
    SyncConfigId = config.Id.ToString(),
    TasksProcessed = result.LocalTaskCount,
    Success = true,
    Duration = elapsed,
    Source = nameof(SyncWorker)
});
```

## EventHandlers

Handler classes live in `Events/EventHandlers/`. Each handler is a plain class with constructor-injected dependencies and a `HandleAsync(TEvent)` method. Handlers are not subscribed automatically; they must be wired to the bus by the caller.

### ConflictDetectedHandler

File: `Events/EventHandlers/ConflictDetectedHandler.cs`

Dependencies: `ILogger<ConflictDetectedHandler>`, `JsonFormatter` (`Formatters/`).

`HandleAsync(ConflictDetectedEvent)`:

1. Logs a warning with task title, task id, conflict type and both modification timestamps.
2. Logs the `LocalValues` and `RemoteValues` dictionaries at debug level, serialized with `JsonFormatter`. Only logged when the dictionary is non-empty.
3. Calls `NotifyConflictAsync`, which is currently an empty placeholder (`TODO`: email, Slack, webhook).
4. Catches and logs any exception. Does not rethrow.

### SyncCompletedHandler

File: `Events/EventHandlers/SyncCompletedHandler.cs`

Dependencies: `ILogger<SyncCompletedHandler>`, `SyncStatistics` (`Models/SyncStatistics.cs`).

`HandleAsync(SyncCompletedEvent)`:

1. Builds a `SyncOperationSnapshot` from the event and calls `SyncStatistics.RecordOperation`.
2. Logs success at information level, or failure at error level.
3. `CheckForWarnings` logs a warning when duration exceeds 60 s, when more than 10 conflicts were resolved, or when more than 100 changes were detected.
4. Calls `PerformPostSyncActionsAsync`, which is currently an empty placeholder (`TODO`: persist statistics, notifications, cleanup, archiving).
5. Catches and logs any exception. Does not rethrow.

### Wiring a handler

Example of registering the handlers manually:

```csharp
var conflictHandler = new ConflictDetectedHandler(conflictLogger, jsonFormatter);
var completedHandler = new SyncCompletedHandler(completedLogger, statistics);

eventBus.Subscribe<ConflictDetectedEvent>(conflictHandler.HandleAsync);
eventBus.Subscribe<SyncCompletedEvent>(completedHandler.HandleAsync);
```

## Current state

These points describe the code as it is today. They are facts about the existing implementation, not planned behavior.

- **Handlers are not registered anywhere.** A search of the non-test code finds no `Subscribe` call for `ConflictDetectedHandler` or `SyncCompletedHandler`, and no DI registration for them. Publishing a `SyncCompletedEvent` does not run `SyncCompletedHandler`.
- **`SyncObserver` has its own subscriptions.** `Observers/SyncObserver.cs` subscribes private methods to `SyncStartedEvent`, `SyncCompletedEvent`, `ConflictDetectedEvent`, `ChangeDetectedEvent`, `TaskSynchronizedEvent`, `RateLimitWarningEvent` and `ValidationFailedEvent`. Its `HandleSyncCompleted` also records a `SyncOperationSnapshot`. If `SyncCompletedHandler` is wired as well, each sync is recorded twice in `SyncStatistics`.
- **`ConflictDetectedEvent` has no publisher** in the non-test code, so conflict handling is not triggered by the sync pipeline.
- **Events are in-process only.** No persistence, no retry, no cross-process delivery.
- **Handler errors are swallowed** after logging. A failing handler does not affect the publisher.

## Related docs

- `docs/EventBus.md` - per-class reference for `EventBus`.
- `docs/SyncStartedEvent.md` - per-class reference for the started event.
- `docs/SyncObserver.md` - the observer that subscribes to the event types above.
- `docs/ARCHITECTURE.md` - where the event system sits in the overall design.
