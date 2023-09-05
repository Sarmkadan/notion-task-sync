# CacheKeyBuilder

Utility class for constructing deterministic cache keys used throughout the notion‑task‑sync application. Centralizing key generation ensures consistent naming across different caching layers and reduces the risk of collisions.

## API

### `public string BuildTaskKey(string taskId)`
- **Purpose**: Creates a cache key for a single Notion task.
- **Parameters**: 
  - `taskId` – The unique identifier of the task (e.g., Notion page ID). Must not be null, empty, or whitespace.
- **Return value**: A string representing the cache key, formatted as `task:{taskId}`.
- **Exceptions**: 
  - `ArgumentNullException` if `taskId` is null.
  - `ArgumentException` if `taskId` consists only of whitespace.

### `public string BuildDatabaseTasksKey(string databaseId)`
- **Purpose**: Creates a cache key for the collection of tasks belonging to a specific database.
- **Parameters**: 
  - `databaseId` – The unique identifier of the Notion database. Must not be null, empty, or whitespace.
- **Return value**: A string representing the cache key, formatted as `database:{databaseId}:tasks`.
- **Exceptions**: 
  - `ArgumentNullException` if `databaseId` is null.
  - `ArgumentException` if `databaseId` consists only of whitespace.

### `public string BuildNotionPageKey(string pageId)`
- **Purpose**: Creates a cache key for a generic Notion page (used when the page type is not a task or database).
- **Parameters**: 
  - `pageId` – The unique identifier of the Notion page. Must not be null, empty, or whitespace.
- **Return value**: A string representing the cache key, formatted as `page:{pageId}`.
- **Exceptions**: 
  - `ArgumentNullException` if `pageId` is null.
  - `ArgumentException` if `pageId` consists only of whitespace.

### `public string BuildConfigKey(string configName)`
- **Purpose**: Creates a cache key for application configuration values.
- **Parameters**: 
  - `configName` – The name of the configuration setting. Must not be null, empty, or whitespace.
- **Return value**: A string representing the cache key, formatted as `config:{configName}`.
- **Exceptions**: 
  - `ArgumentNullException` if `configName` is null.
  - `ArgumentException` if `configName` consists only of whitespace.

### `public string BuildStatisticsKey(string statisticId)`
- **Purpose**: Creates a cache key for a specific statistic entry (e.g., sync counts, latency metrics).
- **Parameters**: 
  - `statisticId` – Identifier for the statistic being cached. Must not be null, empty, or whitespace.
- **Return value**: A string representing the cache key, formatted as `stat:{statisticId}`.
- **Exceptions**: 
  - `ArgumentNullException` if `statisticId` is null.
  - `ArgumentException` if `statisticId` consists only of whitespace.

### `public string BuildApiResponseKey(string endpoint, string queryHash)`
- **Purpose**: Creates a cache key for an HTTP API response, allowing differentiation by endpoint and query parameters.
- **Parameters**: 
  - `endpoint` – The API endpoint path (e.g., `/v1/databases/{id}/query`). Must not be null, empty, or whitespace.
  - `queryHash` – A hash representing the query string or request body. Must not be null, empty, or whitespace.
- **Return value**: A string representing the cache key, formatted as `api:{endpoint}:{queryHash}`.
- **Exceptions**: 
  - `ArgumentNullException` if either parameter is null.
  - `ArgumentException` if either parameter consists only of whitespace.

### `public string BuildChangeLogKey(string entityType, string entityId)`
- **Purpose**: Creates a cache key for a change‑log entry associated with a specific entity.
- **Parameters**: 
  - `entityType` – The type of entity (e.g., `task`, `database`, `page`). Must not be null, empty, or whitespace.
  - `entityId` – The unique identifier of the entity. Must not be null, empty, or whitespace.
- **Return value**: A string representing the cache key, formatted as `changelog:{entityType}:{entityId}`.
- **Exceptions**: 
  - `ArgumentNullException` if either parameter is null.
  - `ArgumentException` if either parameter consists only of whitespace.

### `public string BuildRateLimitKey(string limiterName)`
- **Purpose**: Creates a cache key for tracking rate‑limit state (e.g., number of requests in a window).
- **Parameters**: 
  - `limiterName` – A name identifying the specific limiter (e.g., `notionApi`, `internalSync`). Must not be null, empty, or whitespace.
- **Return value**: A string representing the cache key, formatted as `ratelimit:{limiterName}`.
- **Exceptions**: 
  - `ArgumentNullException` if `limiterName` is null.
  - `ArgumentException` if `limiterName` consists only of whitespace.

### `public string BuildPatternKey(string pattern)`
- **Purpose**: Creates a cache key from an arbitrary user‑supplied pattern string, allowing flexible key naming.
- **Parameters**: 
  - `pattern` – The pattern to incorporate into the key. Must not be null, empty, or whitespace.
- **Return value**: A string representing the cache key, formatted as `pattern:{pattern}`.
- **Exceptions**: 
  - `ArgumentNullException` if `pattern` is null.
  - `ArgumentException` if `pattern` consists only of whitespace.

### `public static string ForTask`
- **Purpose**: Provides a constant prefix used when building task‑related cache keys.
- **Return value**: The string `"task"`.
- **Exceptions**: None.

### `public static string ForDatabase`
- **Purpose**: Provides a constant prefix used when building database‑related cache keys.
- **Return value**: The string `"database"`.
- **Exceptions**: None.

### `public static string ForNotionPage`
- **Purpose**: Provides a constant prefix used when building generic Notion page cache keys.
- **Return value**: The string `"page"`.
- **Exceptions**: None.

## Usage

```csharp
using NotionTaskSync.Caching;

// Example 1: Building a key for a specific task and storing/retrieving from an ICache.
string taskId = "a1b2c3d4-e5f6-7890-g1h2-i3j4k5l6m7n8";
string taskKey = cacheKeyBuilder.BuildTaskKey(taskId);

// Assume `cache` implements a simple Get/Set interface.
var task = cache.Get<TaskDto>(taskKey);
if (task == null)
{
    task = notionClient.GetTask(taskId);
    cache.Set(taskKey, task, TimeSpan.FromMinutes(10));
}
```

```csharp
using NotionTaskSync.Caching;

// Example 2: Using the static prefixes to compose a custom key for a rate limiter.
string limiter = CacheKeyBuilder.ForTask + ":notionApi";
string rateLimitKey = cacheKeyBuilder.BuildRateLimitKey(limiter);

// Increment a counter stored in the cache to enforce a per‑minute request limit.
long current = cache.Increment(rateLimitKey, 1, TimeSpan.FromMinutes(1));
if (current > 100)
{
    // Throttle further requests.
    throw new RateLimitExceededException("Notion API rate limit exceeded.");
}
```

## Notes

- All instance methods throw `ArgumentNullException` when a required argument is `null` and `ArgumentException` when the argument consists only of whitespace. Callers should validate or sanitize input before invoking these methods to avoid unexpected exceptions.
- The methods are **pure**; they do not modify any internal state and rely solely on their input parameters. Consequently, `CacheKeyBuilder` instances are inherently thread‑safe and can be shared freely across threads without synchronization.
- The static fields (`ForTask`, `ForDatabase`, `ForNotionPage`) are read‑only constants and pose no thread‑safety concerns.
- Cache keys are deterministic: identical inputs always produce the same output. This property is essential for reliable cache look‑ups; altering the formatting logic would break existing cached entries.
- While the current implementation uses a simple `{prefix}:{value}` (or `{prefix}:{value1}:{value2}`) scheme, consumers should not depend on the exact delimiter (`:`) beyond what is documented, as internal changes to the separator would not be considered a breaking change as long as the documented format is preserved. However, the present contract guarantees the colon separator as shown.
