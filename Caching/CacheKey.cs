#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace NotionTaskSync.Caching;

/// <summary>
/// Builder for creating consistent cache key strings.
/// Ensures cache keys follow a standardized format across the application.
/// Reduces bugs from inconsistent key generation.
/// </summary>
public class CacheKeyBuilder
{
    /// <summary>
    /// Separator placed between the segments of every generated key.
    /// </summary>
    private const string Separator = ":";

    /// <summary>
    /// Namespace prefix prepended to every generated key.
    /// </summary>
    private readonly string _prefix;

    /// <summary>
    /// Initializes a new instance of the <see cref="CacheKeyBuilder"/> class.
    /// </summary>
    /// <param name="prefix">Prefix prepended to every generated key. Defaults to <c>notion-sync</c>.</param>
    public CacheKeyBuilder(string prefix = "notion-sync")
    {
        _prefix = prefix;
    }

    /// <summary>
    /// Builds a cache key for a task by ID.
    /// </summary>
    /// <param name="taskId">The task identifier.</param>
    /// <returns>A key of the form <c>{prefix}:task:{taskId}</c>.</returns>
    public string BuildTaskKey(string taskId)
    {
        return $"{_prefix}{Separator}task{Separator}{taskId}";
    }

    /// <summary>
    /// Builds a cache key for all tasks in a database.
    /// </summary>
    /// <param name="databaseId">The Notion database identifier.</param>
    /// <returns>A key of the form <c>{prefix}:db:{databaseId}:tasks</c>.</returns>
    public string BuildDatabaseTasksKey(string databaseId)
    {
        return $"{_prefix}{Separator}db{Separator}{databaseId}{Separator}tasks";
    }

    /// <summary>
    /// Builds a cache key for a Notion page by ID.
    /// </summary>
    /// <param name="pageId">The Notion page identifier.</param>
    /// <returns>A key of the form <c>{prefix}:notion:page:{pageId}</c>.</returns>
    public string BuildNotionPageKey(string pageId)
    {
        return $"{_prefix}{Separator}notion{Separator}page{Separator}{pageId}";
    }

    /// <summary>
    /// Builds a cache key for sync configuration.
    /// </summary>
    /// <param name="configId">The sync configuration identifier.</param>
    /// <returns>A key of the form <c>{prefix}:config:{configId}</c>.</returns>
    public string BuildConfigKey(string configId)
    {
        return $"{_prefix}{Separator}config{Separator}{configId}";
    }

    /// <summary>
    /// Builds a cache key for sync statistics.
    /// </summary>
    /// <returns>A key of the form <c>{prefix}:statistics</c>.</returns>
    public string BuildStatisticsKey()
    {
        return $"{_prefix}{Separator}statistics";
    }

    /// <summary>
    /// Builds a cache key for API responses.
    /// </summary>
    /// <param name="endpoint">The API endpoint the response belongs to.</param>
    /// <param name="query">Optional query string appended to the key. Ignored when null or empty.</param>
    /// <returns>A key of the form <c>{prefix}:api:{endpoint}</c>, with <c>:{query}</c> appended when a query is supplied.</returns>
    public string BuildApiResponseKey(string endpoint, string? query = null)
    {
        var key = $"{_prefix}{Separator}api{Separator}{endpoint}";
        if (!string.IsNullOrEmpty(query))
            key += $"{Separator}{query}";
        return key;
    }

    /// <summary>
    /// Builds a cache key for change logs.
    /// </summary>
    /// <param name="configId">The sync configuration identifier the change log belongs to.</param>
    /// <param name="days">The number of days the change log covers. Defaults to 30.</param>
    /// <returns>A key of the form <c>{prefix}:changelog:{configId}:{days}d</c>.</returns>
    public string BuildChangeLogKey(string configId, int days = 30)
    {
        return $"{_prefix}{Separator}changelog{Separator}{configId}{Separator}{days}d";
    }

    /// <summary>
    /// Builds a cache key for rate limit status.
    /// </summary>
    /// <param name="service">The name of the service being rate limited (for example, <c>notion</c>).</param>
    /// <returns>A key of the form <c>{prefix}:ratelimit:{service}</c>.</returns>
    public string BuildRateLimitKey(string service)
    {
        return $"{_prefix}{Separator}ratelimit{Separator}{service}";
    }

    /// <summary>
    /// Builds a pattern-based cache key for wildcard invalidation.
    /// Used with RemoveByPattern to clear related entries.
    /// </summary>
    /// <param name="entityType">The entity type segment to match against.</param>
    /// <returns>A key of the form <c>{prefix}:{entityType}</c>.</returns>
    public string BuildPatternKey(string entityType)
    {
        return $"{_prefix}{Separator}{entityType}";
    }
}

/// <summary>
/// Predefined cache keys for common use cases.
/// </summary>
public static class CommonCacheKeys
{
    /// <summary>
    /// Shared builder used to generate the keys exposed by this class.
    /// </summary>
    private static readonly CacheKeyBuilder Builder = new();

    /// <summary>
    /// Gets the cache key for the collection of all tasks.
    /// </summary>
    public static string AllTasks => $"{Builder.BuildDatabaseTasksKey("all")}";

    /// <summary>
    /// Gets the cache key for sync statistics.
    /// </summary>
    public static string SyncStatistics => Builder.BuildStatisticsKey();

    /// <summary>
    /// Gets the cache key for the Notion API rate limit status.
    /// </summary>
    public static string RateLimitNotionApi => Builder.BuildRateLimitKey("notion");

    /// <summary>
    /// Gets the cache key for the time of the last successful sync.
    /// </summary>
    public static string LastSyncTime => "notion-sync:last-sync-time";

    /// <summary>
    /// Gets the cache key for the sync health status.
    /// </summary>
    public static string HealthStatus => "notion-sync:health-status";

    /// <summary>
    /// Gets the cache key for a single task.
    /// </summary>
    /// <param name="taskId">The task identifier.</param>
    /// <returns>The task cache key.</returns>
    public static string ForTask(string taskId) => Builder.BuildTaskKey(taskId);

    /// <summary>
    /// Gets the cache key for all tasks in a database.
    /// </summary>
    /// <param name="databaseId">The Notion database identifier.</param>
    /// <returns>The database tasks cache key.</returns>
    public static string ForDatabase(string databaseId) => Builder.BuildDatabaseTasksKey(databaseId);

    /// <summary>
    /// Gets the cache key for a Notion page.
    /// </summary>
    /// <param name="pageId">The Notion page identifier.</param>
    /// <returns>The Notion page cache key.</returns>
    public static string ForNotionPage(string pageId) => Builder.BuildNotionPageKey(pageId);
}
