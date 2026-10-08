# Caching architecture

This document describes the caching layer in the `NotionTaskSync.Caching` namespace (source folder `Caching/`). It covers the in-memory provider, the key builder, the extension classes, and a usage example.

Everything described here is a plain in-process, in-memory cache. Nothing is persisted to disk, and nothing is shared between processes. Cache contents are lost on restart.

## Files and types

| File | Types | Role |
|------|-------|------|
| `Caching/CacheProvider.cs` | `CacheProvider`, `CacheStatistics` | Core in-memory cache with expiration and statistics |
| `Caching/CacheKey.cs` | `CacheKeyBuilder`, `CommonCacheKeys` | Standardized key construction and predefined keys |
| `Caching/CacheProviderExtensions.cs` | `CacheProviderExtensions` | Batch and async helpers for `CacheProvider` |
| `Caching/CacheProviderJsonExtensions.cs` | `CacheProviderJsonExtensions` | JSON serialization helpers for `CacheProvider` |

Note: the key builder class is named `CacheKeyBuilder`, even though the file is `CacheKey.cs`.

## CacheProvider

`CacheProvider` is a class with one constructor:

```csharp
public CacheProvider(ILogger<CacheProvider> logger)
```

`logger` is required. The constructor throws `ArgumentNullException` when it is `null`.

### Storage model

- Entries live in a private `Dictionary<string, CacheEntry>`.
- Each `CacheEntry` holds the value as `object?`, plus `ExpiresAt` and `CreatedAt` (both UTC).
- Every public method takes `lock (_cache)`, so individual calls are thread-safe.
- The value type is not stored. `Get<T>` casts the stored object to `T?` at read time. Reading a key with a different `T` than it was written with fails with an `InvalidCastException`.

### Expiration

- `Set` uses `expiration ?? TimeSpan.FromHours(1)`. A missing expiration means **one hour**, not "never expires".
- `Get` checks `ExpiresAt < DateTime.UtcNow`. An expired entry is removed on read and `default` is returned.
- `RemoveExpired()` removes all expired entries in one pass. Nothing calls it automatically. Something must call it if memory cleanup matters.

### Members

| Member | Behavior |
|--------|----------|
| `T? Get<T>(string key)` | Returns the value, or `default` if missing or expired. |
| `void Set<T>(string key, T value, TimeSpan? expiration = null)` | Stores or overwrites the entry. |
| `T GetOrSet<T>(string key, Func<T> factory, TimeSpan? expiration = null)` | Returns the cached value, or runs `factory`, stores the result, and returns it. |
| `Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiration = null)` | Async version of `GetOrSet`. |
| `bool Remove(string key)` | Removes one key. Returns `true` if it existed. |
| `int RemoveByPattern(string pattern)` | Removes every key that **contains** `pattern`, case-insensitive. This is a substring match, not a regex or glob. Returns the count removed. |
| `void Clear()` | Removes all entries. |
| `int RemoveExpired()` | Removes expired entries. Returns the count removed. |
| `CacheStatistics GetStatistics()` | Returns counts of total, valid, and expired entries, plus an approximate size. |

### Behaviors to know before relying on them

- **Null values are treated as misses.** `GetOrSet` and `GetOrSetAsync` check `cached is not null`. A factory that returns `null` runs again on every call, and the `null` is still stored.
- **Two different cache-hit checks.** `Get` returns `default` for a stored `null`, so a stored `null` is indistinguishable from a miss.
- **`ApproximateSizeBytes` is a rough estimate.** It adds the UTF-8 byte count of each key to `Value?.ToString()?.Length`. It is not a byte count of the value.
- **Debug log for an unset expiration is wrong.** The `Set` debug log computes minutes from `expiration?.TotalMinutes`, so it logs `0` when no expiration was passed, even though the real expiration is one hour.

## CacheStatistics

A plain DTO returned by `GetStatistics()`:

- `TotalEntries`, `ValidEntries`, `ExpiredEntries` (int)
- `ApproximateSizeBytes` (long)
- `ValidEntriesPercentage` (double, computed; `0` when there are no entries)

Expired entries are counted in `GetStatistics()` but are only removed by `Get` (for that key) or `RemoveExpired()`.

## CacheProviderExtensions

Extension methods on `CacheProvider` in `CacheProviderExtensions`. They are thin wrappers over the core methods and do not add storage of their own.

| Method | Behavior |
|--------|----------|
| `Dictionary<string, T?> GetMultiple<T>(IEnumerable<string> keys)` | Looks up each key. Missing keys map to `null`. Returns an empty dictionary for an empty input. |
| `void SetMultiple<T>(Dictionary<string, T> values, TimeSpan? expiration = null)` | Stores each pair with the same expiration. |
| `Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiration = null, CancellationToken cancellationToken = default)` | Async get-or-set with a cancellation token checked before the lookup. |
| `int RemoveMultiple(IEnumerable<string> keys)` | Removes each key. Returns the number actually removed. |

### Known caveats

- **`GetOrSetAsync` extension is shadowed by the instance method.** `CacheProvider` already has `GetOrSetAsync<T>(string, Func<Task<T>>, TimeSpan?)`. A call such as `cache.GetOrSetAsync(key, factory)` binds to the instance method, so the extension's `cancellationToken` parameter is only reachable by calling the extension explicitly, e.g. `CacheProviderExtensions.GetOrSetAsync(cache, key, factory, null, token)`.
- **Extension methods lock the cache object itself.** `GetMultiple`, `SetMultiple`, and `RemoveMultiple` call `lock (cache)` (the `CacheProvider` instance), then call the core methods, which take their own lock on `_cache`. Two separate locks are taken in sequence, so this does not deadlock, but a batch is not atomic against other readers.
- **`SetMultiple` computes an unused value.** It calculates an `expiresAt` local that is never read. Each entry gets the expiration passed to it, or one hour.

## CacheProviderJsonExtensions

`CacheProviderJsonExtensions` adds JSON conversion for `CacheProvider`:

- `string ToJson(this CacheProvider value, bool indented = false)`
- `CacheProvider? FromJson(string json)`
- `bool TryFromJson(string json, out CacheProvider? value)`

The options use `JsonSerializerDefaults.Web`, camelCase names, and `ReferenceHandler.IgnoreCycles`.

Caveat: `CacheProvider` has no public properties and no parameterless constructor. Serializing it produces an empty object (`{}`), and the constructor needs an `ILogger`, so `FromJson` cannot construct one. Treat these helpers as non-functional for cache contents. Only use them if the class is changed to serialize its entries. Do not build data flows on them. `TryFromJson` returns `false` on a `JsonException` instead of throwing.

## CacheKeyBuilder

`CacheKeyBuilder` builds consistent key strings. The default prefix is `notion-sync`. Parts are joined with `:`.

| Method | Example output (prefix `notion-sync`) |
|--------|----------------------------------------|
| `BuildTaskKey(taskId)` | `notion-sync:task:{taskId}` |
| `BuildDatabaseTasksKey(databaseId)` | `notion-sync:db:{databaseId}:tasks` |
| `BuildNotionPageKey(pageId)` | `notion-sync:notion:page:{pageId}` |
| `BuildConfigKey(configId)` | `notion-sync:config:{configId}` |
| `BuildStatisticsKey()` | `notion-sync:statistics` |
| `BuildApiResponseKey(endpoint, query?)` | `notion-sync:api:{endpoint}` plus `:{query}` if given |
| `BuildChangeLogKey(configId, days = 30)` | `notion-sync:changelog:{configId}:{days}d` |
| `BuildRateLimitKey(service)` | `notion-sync:ratelimit:{service}` |
| `BuildPatternKey(entityType)` | `notion-sync:{entityType}` (use as a prefix for `RemoveByPattern`) |

Values are not escaped. An ID that contains `:` changes the key structure.

## CommonCacheKeys

Static, ready-made keys built on a default `CacheKeyBuilder`:

- `AllTasks` - `notion-sync:db:all:tasks`
- `SyncStatistics` - `notion-sync:statistics`
- `RateLimitNotionApi` - `notion-sync:ratelimit:notion`
- `LastSyncTime` - `notion-sync:last-sync-time` (hard-coded literal, not built by the builder)
- `HealthStatus` - `notion-sync:health-status` (hard-coded literal)
- `ForTask(taskId)`, `ForDatabase(databaseId)`, `ForNotionPage(pageId)` - shortcuts to the builder methods

`CommonCacheKeys` uses the default prefix only. A `CacheKeyBuilder` with a different prefix produces keys that `CommonCacheKeys` will not match.

## Usage example

The example below uses only the types in `Caching/`. It needs a logger, which can come from `ILoggerFactory` (for example, from `Microsoft.Extensions.Logging`).

```csharp
using Microsoft.Extensions.Logging;
using NotionTaskSync.Caching;

using var loggerFactory = LoggerFactory.Create(b => b.AddConsole());
var cache = new CacheProvider(loggerFactory.CreateLogger<CacheProvider>());
var keys = new CacheKeyBuilder();

// Lazy load a database's task list for 5 minutes.
string dbId = "9f1c2d3e";
string tasksKey = keys.BuildDatabaseTasksKey(dbId);

List<string> tasks = cache.GetOrSet(
    tasksKey,
    () => new List<string> { "Write docs", "Review PR" },   // stands in for a Notion API call
    TimeSpan.FromMinutes(5));

// Batch read and write.
cache.SetMultiple(new Dictionary<string, string>
{
    [keys.BuildTaskKey("t-1")] = "Task one",
    [keys.BuildTaskKey("t-2")] = "Task two",
}, TimeSpan.FromMinutes(10));

var found = cache.GetMultiple<string>(new[]
{
    keys.BuildTaskKey("t-1"),
    keys.BuildTaskKey("t-999"),   // missing, maps to null
});

// Invalidate everything related to one database.
int removed = cache.RemoveByPattern(keys.BuildDatabaseTasksKey(dbId));

// Inspect cache health.
var stats = cache.GetStatistics();
Console.WriteLine($"Valid: {stats.ValidEntries}/{stats.TotalEntries} ({stats.ValidEntriesPercentage:F0}%)");
```

## Where caching is used

As of this writing, nothing outside `Caching/` references `CacheProvider`, `CacheKeyBuilder`, or `CommonCacheKeys`. The types are not registered in the dependency injection setup. Adding them to services is a separate change.
