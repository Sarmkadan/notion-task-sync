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
    private const string Separator = ":";
    private readonly string _prefix;

    public CacheKeyBuilder(string prefix = "notion-sync")
    {
        _prefix = prefix;
    }

    /// <summary>
    /// Builds a cache key for a task by ID.
    /// </summary>
    public string BuildTaskKey(string taskId)
    {
        return $"{_prefix}{Separator}task{Separator}{taskId}";
    }

    /// <summary>
    /// Builds a cache key for all tasks in a database.
    /// </summary>
    public string BuildDatabaseTasksKey(string databaseId)
    {
        return $"{_prefix}{Separator}db{Separator}{databaseId}{Separator}tasks";
    }

    /// <summary>
    /// Builds a cache key for a Notion page by ID.
    /// </summary>
    public string BuildNotionPageKey(string pageId)
    {
        return $"{_prefix}{Separator}notion{Separator}page{Separator}{pageId}";
    }

    /// <summary>
    /// Builds a cache key for sync configuration.
    /// </summary>
    public string BuildConfigKey(string configId)
    {
        return $"{_prefix}{Separator}config{Separator}{configId}";
    }

    /// <summary>
    /// Builds a cache key for sync statistics.
    /// </summary>
    public string BuildStatisticsKey()
    {
        return $"{_prefix}{Separator}statistics";
    }

    /// <summary>
    /// Builds a cache key for API responses.
    /// </summary>
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
    public string BuildChangeLogKey(string configId, int days = 30)
    {
        return $"{_prefix}{Separator}changelog{Separator}{configId}{Separator}{days}d";
    }

    /// <summary>
    /// Builds a cache key for rate limit status.
    /// </summary>
    public string BuildRateLimitKey(string service)
    {
        return $"{_prefix}{Separator}ratelimit{Separator}{service}";
    }

    /// <summary>
    /// Builds a pattern-based cache key for wildcard invalidation.
    /// Used with RemoveByPattern to clear related entries.
    /// </summary>
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
    private static readonly CacheKeyBuilder Builder = new();

    public static string AllTasks => $"{Builder.BuildDatabaseTasksKey("all")}";
    public static string SyncStatistics => Builder.BuildStatisticsKey();
    public static string RateLimitNotionApi => Builder.BuildRateLimitKey("notion");
    public static string LastSyncTime => "notion-sync:last-sync-time";
    public static string HealthStatus => "notion-sync:health-status";

    public static string ForTask(string taskId) => Builder.BuildTaskKey(taskId);
    public static string ForDatabase(string databaseId) => Builder.BuildDatabaseTasksKey(databaseId);
    public static string ForNotionPage(string pageId) => Builder.BuildNotionPageKey(pageId);
}
