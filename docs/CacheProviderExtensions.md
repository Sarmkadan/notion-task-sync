# CacheProviderExtensions

The `CacheProviderExtensions` class provides a suite of static extension methods for the `ICacheProvider` interface, simplifying common bulk and atomic cache operations within the `notion-task-sync` application. These extensions facilitate efficient retrieval and management of multiple cache entries and provide robust support for the cache-aside pattern, minimizing boilerplate code when interacting with underlying caching mechanisms.

## API

### GetMultiple\<T>
Retrieves multiple values from the cache based on the provided collection of keys.
- **Parameters:** `ICacheProvider` provider, `IEnumerable<string>` keys
- **Return Value:** A `Dictionary<string, T?>` mapping each requested key to its corresponding value (or null if the key is not present).
- **Exceptions:** Throws `ArgumentNullException` if `provider` or `keys` is null.

### SetMultiple\<T>
Adds or updates multiple items in the cache in batch.
- **Parameters:** `ICacheProvider` provider, `IDictionary<string, T>` items
- **Return Value:** `void`
- **Exceptions:** Throws `ArgumentNullException` if `provider` or `items` is null.

### GetOrSetAsync\<T>
Implements the cache-aside pattern by attempting to retrieve a value from the cache; if the value is missing, it executes the provided factory function to compute the value, stores it in the cache, and then returns it.
- **Parameters:** `ICacheProvider` provider, `string` key, `Func<Task<T>>` factory
- **Return Value:** A `Task<T>` representing the cached or newly computed value.
- **Exceptions:** Throws `ArgumentNullException` if `provider`, `key`, or `factory` is null.

### RemoveMultiple
Removes multiple items from the cache using their corresponding keys.
- **Parameters:** `ICacheProvider` provider, `IEnumerable<string>` keys
- **Return Value:** An `int` representing the total number of items successfully removed from the cache.
- **Exceptions:** Throws `ArgumentNullException` if `provider` or `keys` is null.

## Usage

### Bulk Cache Operations
```csharp
// Assuming 'cacheProvider' implements ICacheProvider
var keysToFetch = new List<string> { "task_1", "task_2", "task_3" };

// Retrieve multiple items
var results = cacheProvider.GetMultiple<TaskItem>(keysToFetch);

// Set multiple items
var newItems = new Dictionary<string, TaskItem> {
    { "task_4", newTask4 },
    { "task_5", newTask5 }
};
cacheProvider.SetMultiple(newItems);
```

### Cache-Aside Pattern
```csharp
// Retrieve a task, computing it if not cached
TaskItem task = await cacheProvider.GetOrSetAsync("task_123", async () => {
    return await _repository.GetTaskByIdAsync("task_123");
});
```

## Notes

- **Thread Safety:** The thread safety of these extension methods depends entirely on the implementation of the `ICacheProvider` instance. Callers should ensure the underlying cache implementation handles concurrent access as required.
- **Cache Misses:** For `GetMultiple`, keys not found in the cache return `null` within the resulting dictionary; consumers should implement appropriate null-checking logic.
- **Atomicity:** `SetMultiple` and `RemoveMultiple` do not guarantee atomic operations across all `ICacheProvider` implementations. Refer to the documentation of the specific cache provider being used to confirm transactional or atomic guarantees.
- **Performance:** `GetOrSetAsync` is designed to minimize redundant I/O by only executing the factory function on a cache miss.
