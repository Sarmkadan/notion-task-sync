#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

namespace NotionTaskSync.Caching;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

/// <summary>
/// Extension methods for CacheProvider providing additional convenience and batch operations.
/// </summary>
public static class CacheProviderExtensions
{
    /// <summary>
    /// Gets multiple values from cache in a single operation.
    /// Returns dictionary with found values (empty for missing keys).
    /// </summary>
    public static Dictionary<string, T?> GetMultiple<T>(this CacheProvider cache, IEnumerable<string> keys)
    {
        if (cache is null)
            throw new ArgumentNullException(nameof(cache));

        if (keys is null)
            throw new ArgumentNullException(nameof(keys));

        var result = new Dictionary<string, T?>();
        var keyList = keys.ToList();

        if (keyList.Count == 0)
            return result;

        lock (cache.GetType().GetField("_cache", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(cache))
        {
            foreach (var key in keyList)
            {
                var value = cache.Get<T>(key);
                result[key] = value;
            }
        }

        return result;
    }

    /// <summary>
    /// Sets multiple values in cache in a single batch operation.
    /// More efficient than individual Set calls for bulk operations.
    /// </summary>
    public static void SetMultiple<T>(this CacheProvider cache, Dictionary<string, T> values, TimeSpan? expiration = null)
    {
        if (cache is null)
            throw new ArgumentNullException(nameof(cache));

        if (values is null)
            throw new ArgumentNullException(nameof(values));

        if (values.Count == 0)
            return;

        var expiresAt = DateTime.UtcNow.Add(expiration ?? TimeSpan.FromHours(1));
        var cacheField = cache.GetType().GetField("_cache", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var cacheDict = (Dictionary<string, object>)cacheField?.GetValue(cache)!;

        lock (cacheDict)
        {
            foreach (var kvp in values)
            {
                cacheDict[kvp.Key] = new
                {
                    Value = kvp.Value,
                    ExpiresAt = expiresAt,
                    CreatedAt = DateTime.UtcNow
                };
            }
        }
    }

    /// <summary>
    /// Gets a value from cache, or computes and caches it if not found.
    /// Supports async factory function with cancellation token.
    /// </summary>
    public static async Task<T> GetOrSetAsync<T>(this CacheProvider cache, string key, Func<Task<T>> factory, TimeSpan? expiration = null, System.Threading.CancellationToken cancellationToken = default)
    {
        if (cache is null)
            throw new ArgumentNullException(nameof(cache));

        if (key is null)
            throw new ArgumentNullException(nameof(key));

        if (factory is null)
            throw new ArgumentNullException(nameof(factory));

        cancellationToken.ThrowIfCancellationRequested();

        var cached = cache.Get<T>(key);
        if (cached is not null)
            return cached;

        var value = await factory();
        cache.Set(key, value, expiration);

        return value;
    }

    /// <summary>
    /// Attempts to remove multiple keys from cache in a single operation.
    /// Returns count of successfully removed keys.
    /// </summary>
    public static int RemoveMultiple(this CacheProvider cache, IEnumerable<string> keys)
    {
        if (cache is null)
            throw new ArgumentNullException(nameof(cache));

        if (keys is null)
            throw new ArgumentNullException(nameof(keys));

        var keyList = keys.ToList();
        if (keyList.Count == 0)
            return 0;

        var cacheField = cache.GetType().GetField("_cache", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var cacheDict = (Dictionary<string, object>)cacheField?.GetValue(cache)!;
        var removedCount = 0;

        lock (cacheDict)
        {
            foreach (var key in keyList)
            {
                if (cacheDict.Remove(key))
                    removedCount++;
            }
        }

        return removedCount;
    }
}
