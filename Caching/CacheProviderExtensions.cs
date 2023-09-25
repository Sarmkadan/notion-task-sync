#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// ===================================================================

namespace NotionTaskSync.Caching;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
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
    /// <typeparam name="T">The type of values to retrieve from cache.</typeparam>
    /// <param name="cache">The cache provider instance.</param>
    /// <param name="keys">The collection of keys to retrieve.</param>
    /// <returns>Dictionary mapping keys to their cached values (null if not found).</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="cache"/> or <paramref name="keys"/> is null.</exception>
    public static Dictionary<string, T?> GetMultiple<T>(this CacheProvider cache, IEnumerable<string> keys)
    {
        ArgumentNullException.ThrowIfNull(cache);
        ArgumentNullException.ThrowIfNull(keys);

        var result = new Dictionary<string, T?>();
        var keyList = keys.ToList();

        if (keyList.Count == 0)
            return result;

        lock (cache)
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
    /// <typeparam name="T">The type of values to store in cache.</typeparam>
    /// <param name="cache">The cache provider instance.</param>
    /// <param name="values">Dictionary of key-value pairs to store.</param>
    /// <param name="expiration">Optional expiration time span. Defaults to 1 hour.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="cache"/> or <paramref name="values"/> is null.</exception>
    public static void SetMultiple<T>(this CacheProvider cache, Dictionary<string, T> values, TimeSpan? expiration = null)
    {
        ArgumentNullException.ThrowIfNull(cache);
        ArgumentNullException.ThrowIfNull(values);

        if (values.Count == 0)
            return;

        var expiresAt = DateTime.UtcNow.Add(expiration ?? TimeSpan.FromHours(1));

        lock (cache)
        {
            foreach (var kvp in values)
            {
                cache.Set(kvp.Key, kvp.Value, expiration ?? TimeSpan.FromHours(1));
            }
        }
    }

    /// <summary>
    /// Gets a value from cache, or computes and caches it if not found.
    /// Supports async factory function with cancellation token.
    /// </summary>
    /// <typeparam name="T">The type of value to retrieve or compute.</typeparam>
    /// <param name="cache">The cache provider instance.</param>
    /// <param name="key">The cache key to look up.</param>
    /// <param name="factory">Async function to compute the value if not found in cache.</param>
    /// <param name="expiration">Optional expiration time span. Defaults to 1 hour.</param>
    /// <param name="cancellationToken">Cancellation token for the async operation.</param>
    /// <returns>The cached or newly computed value.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="cache"/>, <paramref name="key"/>, or <paramref name="factory"/> is null.</exception>
    public static async Task<T> GetOrSetAsync<T>(this CacheProvider cache, string key, Func<Task<T>> factory, TimeSpan? expiration = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(cache);
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(factory);

        cancellationToken.ThrowIfCancellationRequested();

        var cached = cache.Get<T>(key);
        if (cached is not null)
            return cached;

        var value = await factory().ConfigureAwait(false);
        cache.Set(key, value, expiration);

        return value;
    }

    /// <summary>
    /// Attempts to remove multiple keys from cache in a single operation.
    /// Returns count of successfully removed keys.
    /// </summary>
    /// <param name="cache">The cache provider instance.</param>
    /// <param name="keys">The collection of keys to remove.</param>
    /// <returns>Count of successfully removed keys.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="cache"/> or <paramref name="keys"/> is null.</exception>
    public static int RemoveMultiple(this CacheProvider cache, IEnumerable<string> keys)
    {
        ArgumentNullException.ThrowIfNull(cache);
        ArgumentNullException.ThrowIfNull(keys);

        var keyList = keys.ToList();
        if (keyList.Count == 0)
            return 0;

        var removedCount = 0;

        lock (cache)
        {
            foreach (var key in keyList)
            {
                if (cache.Remove(key))
                    removedCount++;
            }
        }

        return removedCount;
    }
}