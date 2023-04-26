#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace NotionTaskSync.Caching;

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;

/// <summary>
/// In-memory cache provider for storing frequently accessed data.
/// Reduces API calls to Notion by caching results for configurable durations.
/// Implements automatic expiration and thread-safe operations.
/// </summary>
public class CacheProvider
{
    private readonly Dictionary<string, CacheEntry> _cache = new();
    private readonly ILogger<CacheProvider> _logger;

    public CacheProvider(ILogger<CacheProvider> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Gets a value from cache if it exists and hasn't expired.
    /// Returns null if key not found or entry has expired.
    /// </summary>
    public T? Get<T>(string key)
    {
        lock (_cache)
        {
            if (!_cache.TryGetValue(key, out var entry))
                return default;

            // Check if entry has expired
            if (entry.ExpiresAt < DateTime.UtcNow)
            {
                _cache.Remove(key);
                _logger.LogDebug("Cache entry expired: {Key}", key);
                return default;
            }

            _logger.LogDebug("Cache hit: {Key}", key);
            return (T?)entry.Value;
        }
    }

    /// <summary>
    /// Sets a value in cache with optional expiration time.
    /// Overwrites existing value if key already exists.
    /// </summary>
    public void Set<T>(string key, T value, TimeSpan? expiration = null)
    {
        lock (_cache)
        {
            var expiresAt = DateTime.UtcNow.Add(expiration ?? TimeSpan.FromHours(1));

            _cache[key] = new CacheEntry
            {
                Value = value,
                ExpiresAt = expiresAt,
                CreatedAt = DateTime.UtcNow
            };

            _logger.LogDebug("Cache set: {Key}, expires in {Minutes} minutes", key, (int)expiration?.TotalMinutes);
        }
    }

    /// <summary>
    /// Gets a value from cache, or computes and caches it if not found.
    /// Useful for lazy-loading with automatic cache management.
    /// </summary>
    public T GetOrSet<T>(string key, Func<T> factory, TimeSpan? expiration = null)
    {
        var cached = Get<T>(key);
        if (cached is not null)
            return cached;

        _logger.LogDebug("Cache miss: {Key}, computing value", key);
        var value = factory();
        Set(key, value, expiration);

        return value;
    }

    /// <summary>
    /// Async version of GetOrSet for operations that are awaitable.
    /// </summary>
    public async System.Threading.Tasks.Task<T> GetOrSetAsync<T>(
        string key,
        Func<System.Threading.Tasks.Task<T>> factory,
        TimeSpan? expiration = null)
    {
        var cached = Get<T>(key);
        if (cached is not null)
            return cached;

        _logger.LogDebug("Cache miss: {Key}, computing value asynchronously", key);
        var value = await factory().ConfigureAwait(false);
        Set(key, value, expiration);

        return value;
    }

    /// <summary>
    /// Removes a specific key from cache.
    /// </summary>
    public bool Remove(string key)
    {
        lock (_cache)
        {
            var removed = _cache.Remove(key);
            if (removed)
                _logger.LogDebug("Cache invalidated: {Key}", key);
            return removed;
        }
    }

    /// <summary>
    /// Removes all cache entries that match a pattern.
    /// Useful for invalidating cache for an entity type.
    /// </summary>
    public int RemoveByPattern(string pattern)
    {
        lock (_cache)
        {
            var keysToRemove = _cache.Keys
                .Where(k => k.Contains(pattern, StringComparison.OrdinalIgnoreCase))
                .ToList();

            foreach (var key in keysToRemove)
            {
                _cache.Remove(key);
            }

            _logger.LogInformation("Cache invalidated {Count} entries matching pattern: {Pattern}",
                keysToRemove.Count, pattern);

            return keysToRemove.Count;
        }
    }

    /// <summary>
    /// Clears all cache entries.
    /// </summary>
    public void Clear()
    {
        lock (_cache)
        {
            var count = _cache.Count;
            _cache.Clear();
            _logger.LogInformation("Cache cleared ({Count} entries removed)", count);
        }
    }

    /// <summary>
    /// Removes all expired entries from cache (cleanup).
    /// Called periodically to free memory.
    /// </summary>
    public int RemoveExpired()
    {
        lock (_cache)
        {
            var expiredKeys = _cache
                .Where(x => x.Value.ExpiresAt < DateTime.UtcNow)
                .Select(x => x.Key)
                .ToList();

            foreach (var key in expiredKeys)
            {
                _cache.Remove(key);
            }

            _logger.LogDebug("Removed {Count} expired cache entries", expiredKeys.Count);
            return expiredKeys.Count;
        }
    }

    /// <summary>
    /// Gets cache statistics (size, entry count, etc).
    /// </summary>
    public CacheStatistics GetStatistics()
    {
        lock (_cache)
        {
            var validEntries = _cache.Count(x => x.Value.ExpiresAt >= DateTime.UtcNow);
            var expiredEntries = _cache.Count - validEntries;

            return new CacheStatistics
            {
                TotalEntries = _cache.Count,
                ValidEntries = validEntries,
                ExpiredEntries = expiredEntries,
                ApproximateSizeBytes = EstimateCacheSize()
            };
        }
    }

    /// <summary>
    /// Estimates the approximate memory usage of the cache.
    /// </summary>
    private long EstimateCacheSize()
    {
        lock (_cache)
        {
            // Rough estimation: key length + value estimate
            return _cache.Sum(x =>
            {
                var size = System.Text.Encoding.UTF8.GetByteCount(x.Key);
                size += x.Value.Value?.ToString()?.Length ?? 0;
                return size;
            });
        }
    }

    /// <summary>
    /// Internal class representing a cached entry with expiration metadata.
    /// </summary>
    private class CacheEntry
    {
        public object? Value { get; set; }
        public DateTime ExpiresAt { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}

/// <summary>
/// Statistics about cache health and usage.
/// </summary>
public class CacheStatistics
{
    public int TotalEntries { get; set; }
    public int ValidEntries { get; set; }
    public int ExpiredEntries { get; set; }
    public long ApproximateSizeBytes { get; set; }

    /// <summary>
    /// Gets the percentage of cache entries that are still valid.
    /// </summary>
    public double ValidEntriesPercentage =>
        TotalEntries > 0 ? (ValidEntries * 100.0 / TotalEntries) : 0;
}
