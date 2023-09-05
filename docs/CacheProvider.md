# CacheProvider

A lightweight in-memory cache provider that supports typed value retrieval, expiration, and statistics. Designed for scenarios requiring fast access to frequently used data with optional time-based invalidation.

## API

### `public CacheProvider`

Initializes a new instance of the `CacheProvider` with default settings (no automatic cleanup, unlimited size).

### `public T? Get<T>(string key)`

Retrieves a value from the cache by its key.

- **Parameters**:
  - `key` (string): The unique identifier for the cached item.
- **Returns**: The cached value of type `T`, or `null` if the key does not exist or the item has expired.
- **Throws**: `ArgumentNullException` if `key` is `null`.

### `public void Set<T>(string key, T value, TimeSpan? expiresIn = null)`

Stores a value in the cache with an optional expiration time.

- **Parameters**:
  - `key` (string): The unique identifier for the cached item.
  - `value` (T): The value to cache.
  - `expiresIn` (TimeSpan?, optional): The duration after which the item expires. If `null`, the item does not expire.
- **Throws**: `ArgumentNullException` if `key` is `null`.

### `public T GetOrSet<T>(string key, Func<T> valueFactory, TimeSpan? expiresIn = null)`

Retrieves a value from the cache, or computes and stores it if the key is missing.

- **Parameters**:
  - `key` (string): The unique identifier for the cached item.
  - `valueFactory` (Func<T>): A function that computes the value if the key is not found.
  - `expiresIn` (TimeSpan?, optional): The duration after which the item expires.
- **Returns**: The cached or newly computed value.
- **Throws**:
  - `ArgumentNullException` if `key` is `null` or `valueFactory` is `null`.
  - Any exception thrown by `valueFactory`.

### `public async System.Threading.Tasks.Task<T> GetOrSetAsync<T>(string key, Func<System.Threading.Tasks.Task<T>> valueFactory, TimeSpan? expiresIn = null)`

Asynchronously retrieves a value from the cache, or computes and stores it if the key is missing.

- **Parameters**:
  - `key` (string): The unique identifier for the cached item.
  - `valueFactory` (Func<Task<T>>): An asynchronous function that computes the value if the key is not found.
  - `expiresIn` (TimeSpan?, optional): The duration after which the item expires.
- **Returns**: A task representing the cached or newly computed value.
- **Throws**:
  - `ArgumentNullException` if `key` is `null` or `valueFactory` is `null`.
  - Any exception thrown by `valueFactory`.

### `public bool Remove(string key)`

Removes a single item from the cache by its key.

- **Parameters**:
  - `key` (string): The unique identifier for the cached item.
- **Returns**: `true` if the item was found and removed; otherwise, `false`.
- **Throws**: `ArgumentNullException` if `key` is `null`.

### `public int RemoveByPattern(string pattern)`

Removes all items whose keys match the given regex pattern.

- **Parameters**:
  - `pattern` (string): A regex pattern to match keys for removal.
- **Returns**: The number of items removed.
- **Throws**: `ArgumentNullException` if `pattern` is `null`.

### `public void Clear()`

Removes all items from the cache.

### `public int RemoveExpired()`

Removes all expired items from the cache.

- **Returns**: The number of items removed.

### `public CacheStatistics GetStatistics()`

Retrieves runtime statistics about the cache.

- **Returns**: A `CacheStatistics` object containing counts of total, valid, and expired entries, along with approximate memory usage.

### `public object? Value`

Gets the underlying cached value (used internally by cache entries).

### `public DateTime ExpiresAt`

Gets the expiration timestamp of the cache entry.

### `public DateTime CreatedAt`

Gets the creation timestamp of the cache entry.

### `public int TotalEntries`

Gets the total number of entries in the cache, including expired ones.

### `public int ValidEntries`

Gets the number of non-expired entries in the cache.

### `public int ExpiredEntries`

Gets the number of expired entries currently in the cache.

### `public long ApproximateSizeBytes`

Gets an approximate size of the cache in bytes (may include overhead).

## Usage

### Example 1: Basic Usage
