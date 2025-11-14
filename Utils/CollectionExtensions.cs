// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace NotionTaskSync.Utils;

using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Extension methods for collections (lists, enumerables, dictionaries).
/// Provides common operations used in data processing and transformation pipelines.
/// Reduces repetition in collection manipulation code throughout the application.
/// </summary>
public static class CollectionExtensions
{
    /// <summary>
    /// Determines if a collection is null or contains no elements.
    /// Cleaner syntax than checking both conditions separately.
    /// </summary>
    public static bool IsNullOrEmpty<T>(this IEnumerable<T>? collection)
    {
        return collection == null || !collection.Any();
    }

    /// <summary>
    /// Determines if a collection has elements with meaningful content.
    /// Opposite of IsNullOrEmpty for improved readability.
    /// </summary>
    public static bool HasItems<T>(this IEnumerable<T>? collection)
    {
        return collection != null && collection.Any();
    }

    /// <summary>
    /// Safely gets an item at a specific index, returning a default value if index is out of range.
    /// Prevents IndexOutOfRangeException in scenarios where existence cannot be guaranteed.
    /// </summary>
    public static T? SafeGetAt<T>(this IList<T> list, int index, T? defaultValue = default)
    {
        if (list == null || index < 0 || index >= list.Count)
            return defaultValue;

        return list[index];
    }

    /// <summary>
    /// Batches a collection into chunks of specified size.
    /// Useful for processing large datasets in manageable portions (e.g., API rate limiting).
    /// </summary>
    public static IEnumerable<IEnumerable<T>> Batch<T>(this IEnumerable<T> items, int batchSize)
    {
        if (batchSize <= 0)
            throw new ArgumentException("Batch size must be positive", nameof(batchSize));

        var batch = new List<T>(batchSize);

        foreach (var item in items)
        {
            batch.Add(item);
            if (batch.Count == batchSize)
            {
                yield return batch;
                batch = new List<T>(batchSize);
            }
        }

        if (batch.Count > 0)
            yield return batch;
    }

    /// <summary>
    /// Partitions a collection into two groups based on a predicate.
    /// Useful for separating matching and non-matching items in a single pass.
    /// </summary>
    public static (List<T> matching, List<T> notMatching) Partition<T>(
        this IEnumerable<T> items,
        Func<T, bool> predicate)
    {
        var matching = new List<T>();
        var notMatching = new List<T>();

        foreach (var item in items)
        {
            if (predicate(item))
                matching.Add(item);
            else
                notMatching.Add(item);
        }

        return (matching, notMatching);
    }

    /// <summary>
    /// Groups items and returns the groups with the highest occurrence count first.
    /// Useful for identifying most common values in change detection.
    /// </summary>
    public static IEnumerable<IGrouping<TKey, T>> GroupByFrequency<T, TKey>(
        this IEnumerable<T> items,
        Func<T, TKey> keySelector) where TKey : notnull
    {
        return items
            .GroupBy(keySelector)
            .OrderByDescending(g => g.Count());
    }

    /// <summary>
    /// Flattens a nested collection (collection of collections) into a single collection.
    /// Alias for SelectMany with improved naming semantics.
    /// </summary>
    public static IEnumerable<T> Flatten<T>(this IEnumerable<IEnumerable<T>> nested)
    {
        return nested.SelectMany(x => x);
    }

    /// <summary>
    /// Creates a dictionary from a collection, handling duplicate keys gracefully.
    /// By default, last occurrence wins; can be customized with valueSelector.
    /// </summary>
    public static Dictionary<TKey, TValue> SafeToDictionary<TItem, TKey, TValue>(
        this IEnumerable<TItem> items,
        Func<TItem, TKey> keySelector,
        Func<TItem, TValue> valueSelector) where TKey : notnull
    {
        var dict = new Dictionary<TKey, TValue>();

        foreach (var item in items)
        {
            var key = keySelector(item);
            var value = valueSelector(item);
            dict[key] = value; // Last occurrence wins
        }

        return dict;
    }

    /// <summary>
    /// Splits a collection at indices where a predicate returns true.
    /// Useful for separating sequential data into logical groups.
    /// </summary>
    public static List<List<T>> SplitWhere<T>(
        this IEnumerable<T> items,
        Func<T, bool> splitCondition)
    {
        var groups = new List<List<T>>();
        var currentGroup = new List<T>();

        foreach (var item in items)
        {
            if (splitCondition(item) && currentGroup.Count > 0)
            {
                groups.Add(currentGroup);
                currentGroup = new List<T>();
            }

            currentGroup.Add(item);
        }

        if (currentGroup.Count > 0)
            groups.Add(currentGroup);

        return groups;
    }

    /// <summary>
    /// Removes all null values from a collection.
    /// Fluent alternative to Where(x => x != null).
    /// </summary>
    public static IEnumerable<T> WhereNotNull<T>(this IEnumerable<T?> items) where T : class
    {
        return items.Where(x => x != null)!;
    }

    /// <summary>
    /// Returns distinct items based on a key selector function.
    /// More flexible than Distinct when uniqueness is based on specific properties.
    /// </summary>
    public static IEnumerable<T> DistinctBy<T, TKey>(
        this IEnumerable<T> items,
        Func<T, TKey> keySelector) where TKey : notnull
    {
        var seen = new HashSet<TKey>();

        foreach (var item in items)
        {
            var key = keySelector(item);
            if (seen.Add(key))
                yield return item;
        }
    }

    /// <summary>
    /// Intersects two collections based on a key selector function.
    /// Returns items from the first collection that have matching keys in the second.
    /// </summary>
    public static IEnumerable<T> IntersectBy<T, TKey>(
        this IEnumerable<T> items,
        IEnumerable<T> other,
        Func<T, TKey> keySelector) where TKey : notnull
    {
        var otherKeys = other.Select(keySelector).ToHashSet();
        return items.Where(x => otherKeys.Contains(keySelector(x)));
    }

    /// <summary>
    /// Adds an item to a collection if it passes a condition.
    /// Shorthand for if-then-add patterns.
    /// </summary>
    public static void AddIf<T>(this List<T> list, T item, Func<T, bool> condition)
    {
        if (condition(item))
            list.Add(item);
    }

    /// <summary>
    /// Shuffles a collection in-place using Fisher-Yates algorithm.
    /// Ensures random distribution without replacement.
    /// </summary>
    public static void Shuffle<T>(this IList<T> list, Random? random = null)
    {
        random ??= new Random();

        for (int i = list.Count - 1; i > 0; i--)
        {
            int randomIndex = random.Next(i + 1);
            (list[i], list[randomIndex]) = (list[randomIndex], list[i]);
        }
    }
}
