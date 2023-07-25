# EventBus

The `EventBus` class provides an in-memory, decoupled message distribution mechanism within the `notion-task-sync` application. It enables components to publish events and subscribe to specific event types, facilitating communication without requiring direct dependencies between publishers and subscribers. This implementation supports both synchronous and asynchronous event handling.

## API

### Constructors
- `public EventBus()`: Initializes a new instance of the `EventBus` class.

### Methods
- `public void Subscribe<T>(Action<T> handler)`: Registers a synchronous handler for events of type `T`.
- `public void Subscribe<T>(Func<T, Task> handler)`: Registers an asynchronous handler for events of type `T`.
- `public async Task PublishAsync<T>(T eventData)`: Asynchronously invokes all handlers registered for events of type `T`.
- `public void Publish<T>(T eventData)`: Synchronously invokes all handlers registered for events of type `T`.
- `public int GetSubscriberCount<T>()`: Returns the number of registered handlers for the specified event type `T`.
- `public void UnsubscribeAll<T>()`: Removes all handlers associated with event type `T`.
- `public void Clear()`: Removes all handlers across all registered event types.
- `public Dictionary<string, int> GetSubscriberInfo()`: Returns a dictionary containing event type names as keys and their respective subscriber counts as values.

### Properties
- `public Guid EventId`: Gets the unique identifier for this `EventBus` instance.
- `public DateTime Timestamp`: Gets the timestamp indicating when this `EventBus` instance was created.
- `public string? Source`: Gets or sets a descriptive string identifying the origin or context of this `EventBus` instance.

## Usage

### Example 1: Synchronous Subscription and Publication
```csharp
var eventBus = new EventBus();

// Subscribe to a custom event
eventBus.Subscribe<string>(message => Console.WriteLine($"Received: {message}"));

// Publish an event
eventBus.Publish("Task completed successfully.");
```

### Example 2: Asynchronous Publication with Task-based Handlers
```csharp
var eventBus = new EventBus();

// Subscribe to an asynchronous event handler
eventBus.Subscribe<int>(async id => {
    await Task.Delay(100);
    Console.WriteLine($"Processing task: {id}");
});

// Asynchronously publish the event
await eventBus.PublishAsync(42);
```

## Notes

- **Thread Safety**: The `EventBus` is designed to be thread-safe for both subscription management and event publication. Multiple threads may concurrently subscribe, unsubscribe, or publish events without causing corruption to the underlying registration state.
- **Exception Handling**: The `EventBus` does not internally catch or suppress exceptions thrown by registered event handlers. If a handler throws an exception, it will propagate up to the caller of the `Publish` or `PublishAsync` method. Publishers should implement appropriate error handling strategies around event publication.
- **Execution Order**: When multiple handlers are registered for the same event type, they are invoked in the order in which they were subscribed. For asynchronous publication, handlers are triggered sequentially, awaiting each task completion before proceeding to the next, unless the handlers themselves are implemented to run concurrently.
