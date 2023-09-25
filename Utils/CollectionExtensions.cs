#nullable enable

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
/// <remarks>
/// All public methods include proper null checking and argument validation.
/// Methods follow .NET design guidelines for extension methods and are optimized for common scenarios.
/// </remarks>
public static class CollectionExtensions
{
    /// <summary>
    /// Determines if a collection is null or contains no elements.
    /// Cleaner syntax than checking both conditions separately.
    /// </summary>
    /// <typeparam name="T">The type of elements in the collection.</typeparam>
    /// <param name="collection">The collection to check.</param>
    /// <returns><see langword="true"/> if the collection is null or empty; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="collection"/> is <see langword="null"/>.</exception>
    public static bool IsNullOrEmpty<T>(this IEnumerable<T>? collection)
    {
        ArgumentNullException.ThrowIfNull(collection);
        return !collection.Any();
    }

    /// <summary>
    /// Determines if a collection has elements with meaningful content.
    /// Opposite of IsNullOrEmpty for improved readability.
    /// </summary>
    /// <typeparam name="T">The type of elements in the collection.</typeparam>
    /// <param name="collection">The collection to check.</param>
    /// <returns><see langword="true"/> if the collection is not null and contains elements; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="collection"/> is <see langword="null"/>.</exception>
    public static bool HasItems<T>(this IEnumerable<T>? collection)
    {
        ArgumentNullException.ThrowIfNull(collection);
        return collection.Any();
    }

    /// <summary>
    /// Safely gets an item at a specific index, returning a default value if index is out of range.
    /// Prevents IndexOutOfRangeException in scenarios where existence cannot be guaranteed.
    /// </summary>
    /// <typeparam name="T">The type of elements in the list.</typeparam>
    /// <param name="list">The list to access.</param>
    /// <param name="index">The zero-based index of the element to get.</param>
    /// <param name="defaultValue">The default value to return if index is out of range.</param>
    /// <returns>The element at the specified index, or <paramref name="defaultValue"/> if index is out of range.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="list"/> is <see langword="null"/>.</exception>
    public static T? SafeGetAt<T>(this IList<T> list, int index, T? defaultValue = default)
    {
        ArgumentNullException.ThrowIfNull(list);

        if (index < 0 || index >= list.Count)
            return defaultValue;

        return list[index];
    }

    /// <summary>
    /// Batches a collection into chunks of specified size.
    /// Useful for processing large datasets in manageable portions (e.g., API rate limiting).
    /// </summary>
    /// <typeparam name="T">The type of elements in the collection.</typeparam>
    /// <param name="items">The collection to batch.</param>
    /// <param name="batchSize">The maximum size of each batch (must be positive).</param>
    /// <returns>An enumerable of batches, each containing at most <paramref name="batchSize"/> items.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="items"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="batchSize"/> is not positive.</exception>
    public static IEnumerable<IEnumerable<T>> Batch<T>(this IEnumerable<T> items, int batchSize)
    {
        ArgumentNullException.ThrowIfNull(items);

        if (batchSize <= 0)
            throw new ArgumentOutOfRangeException(nameof(batchSize), "Batch size must be positive");

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
    /// <typeparam name="T">The type of elements in the collection.</typeparam>
    /// <param name="items">The collection to partition.</param>
    /// <param name="predicate">The function to test each element.</param>
    /// <returns>A tuple containing two lists: the first with elements where <paramref name="predicate"/> returned <see langword="true"/>, the second with elements where it returned <see langword="false"/>.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="items"/> is <see langword="null"/>.
    /// <paramref name="predicate"/> is <see langword="null"/>.
    /// </exception>
    public static (List<T> matching, List<T> notMatching) Partition<T>(
        this IEnumerable<T> items,
        Func<T, bool> predicate)
    {
        ArgumentNullException.ThrowIfNull(items);
        ArgumentNullException.ThrowIfNull(predicate);

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
    /// <typeparam name="T">The type of source elements.</typeparam>
    /// <typeparam name="TKey">The type of keys returned by <paramref name="keySelector"/>.</typeparam>
    /// <param name="items">The collection to group.</param>
    /// <param name="keySelector">A function to extract a key from each element.</param>
    /// <returns>An enumerable of groupings ordered by frequency in descending order.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="items"/> is <see langword="null"/>.
    /// <paramref name="keySelector"/> is <see langword="null"/>.
    /// </exception>
    public static IEnumerable<IGrouping<TKey, T>> GroupByFrequency<T, TKey>(
        this IEnumerable<T> items,
        Func<T, TKey> keySelector) where TKey : notnull
    {
        ArgumentNullException.ThrowIfNull(items);
        ArgumentNullException.ThrowIfNull(keySelector);

        return items
            .GroupBy(keySelector)
            .OrderByDescending(g => g.Count());
    }

    /// <summary>
    /// Flattens a nested collection (collection of collections) into a single collection.
    /// Alias for SelectMany with improved naming semantics.
    /// </summary>
    /// <typeparam name="T">The type of elements in the collections.</typeparam>
    /// <param name="nested">The nested collection to flatten.</param>
    /// <returns>A single enumerable containing all elements from all nested collections.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="nested"/> is <see langword="null"/>.</exception>
    public static IEnumerable<T> Flatten<T>(this IEnumerable<IEnumerable<T>> nested)
    {
        ArgumentNullException.ThrowIfNull(nested);
        return nested.SelectMany(x => x);
    }

    /// <summary>
    /// Creates a dictionary from a collection, handling duplicate keys gracefully.
    /// By default, last occurrence wins; can be customized with valueSelector.
    /// </summary>
    /// <typeparam name="TItem">The type of elements in the source collection.</typeparam>
    /// <typeparam name="TKey">The type of keys in the dictionary.</typeparam>
    /// <typeparam name="TValue">The type of values in the dictionary.</typeparam>
    /// <param name="items">The collection to convert to a dictionary.</param>
    /// <param name="keySelector">A function to extract a key from each element.</param>
    /// <param name="valueSelector">A function to extract a value from each element.</param>
    /// <returns>A dictionary containing the key-value pairs.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="items"/> is <see langword="null"/>.
    /// <paramref name="keySelector"/> is <see langword="null"/>.
    /// <paramref name="valueSelector"/> is <see langword="null"/>.
    /// </exception>
    public static Dictionary<TKey, TValue> SafeToDictionary<TItem, TKey, TValue>(
        this IEnumerable<TItem> items,
        Func<TItem, TKey> keySelector,
        Func<TItem, TValue> valueSelector) where TKey : notnull
    {
        ArgumentNullException.ThrowIfNull(items);
        ArgumentNullException.ThrowIfNull(keySelector);
        ArgumentNullException.ThrowIfNull(valueSelector);

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
    /// <typeparam name="T">The type of elements in the collection.</typeparam>
    /// <param name="items">The collection to split.</param>
    /// <param name="splitCondition">A function that returns <see langword="true"/> when a split should occur.</param>
    /// <returns>A list of lists, where each inner list represents a group separated by split points.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="items"/> is <see langword="null"/>.
    /// <paramref name="splitCondition"/> is <see langword="null"/>.
    /// </exception>
    public static List<List<T>> SplitWhere<T>(
        this IEnumerable<T> items,
        Func<T, bool> splitCondition)
    {
        ArgumentNullException.ThrowIfNull(items);
        ArgumentNullException.ThrowIfNull(splitCondition);

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
    /// <typeparam name="T">The type of elements in the collection (must be a reference type).</typeparam>
    /// <param name="items">The collection to filter.</param>
    /// <returns>An enumerable containing only non-null elements.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="items"/> is <see langword="null"/>.</exception>
    public static IEnumerable<T> WhereNotNull<T>(this IEnumerable<T?> items) where T : class
    {
        ArgumentNullException.ThrowIfNull(items);
        return items.Where(x => x is not null)!;
    }

    /// <summary>
    /// Returns distinct items based on a key selector function.
    /// More flexible than Distinct when uniqueness is based on specific properties.
    /// </summary>
    /// <typeparam name="T">The type of elements in the collection.</typeparam>
    /// <typeparam name="TKey">The type of keys used for determining uniqueness.</typeparam>
    /// <param name="items">The collection to process.</param>
    /// <param name="keySelector">A function to extract a key from each element.</param>
    /// <returns>An enumerable containing only distinct elements based on the key selector.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="items"/> is <see langword="null"/>.
    /// <paramref name="keySelector"/> is <see langword="null"/>.
    /// </exception>
    public static IEnumerable<T> DistinctBy<T, TKey>(
        this IEnumerable<T> items,
        Func<T, TKey> keySelector)
    {
        ArgumentNullException.ThrowIfNull(items);
        ArgumentNullException.ThrowIfNull(keySelector);

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
    /// <typeparam name="T">The type of elements in the collections.</typeparam>
    /// <typeparam name="TKey">The type of keys used for comparison.</typeparam>
    /// <param name="items">The first collection (items to filter).</param>
    /// <param name="other">The second collection (keys to match against).</param>
    /// <param name="keySelector">A function to extract a key from each element.</param>
    /// <returns>An enumerable containing items from <paramref name="items"/> whose keys exist in <paramref name="other"/>.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="items"/> is <see langword="null"/>.
    /// <paramref name="other"/> is <see langword="null"/>.
    /// <paramref name="keySelector"/> is <see langword="null"/>.
    /// </exception>
    public static IEnumerable<T> IntersectBy<T, TKey>(
        this IEnumerable<T> items,
        IEnumerable<T> other,
        Func<T, TKey> keySelector) where TKey : notnull
    {
        ArgumentNullException.ThrowIfNull(items);
        ArgumentNullException.ThrowIfNull(other);
        ArgumentNullException.ThrowIfNull(keySelector);

        var otherKeys = other.Select(keySelector).ToHashSet();
        return items.Where(x => otherKeys.Contains(keySelector(x)));
    }

    /// <summary>
    /// Adds an item to a collection if it passes a condition.
    /// Shorthand for if-then-add patterns.
    /// </summary>
    /// <typeparam name="T">The type of elements in the list.</typeparam>
    /// <param name="list">The list to add to.</param>
    /// <param name="item">The item to potentially add.</param>
    /// <param name="condition">The function to test the item.</param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="list"/> is <see langword="null"/>.
    /// <paramref name="condition"/> is <see langword="null"/>.
    /// </exception>
    public static void AddIf<T>(this List<T> list, T item, Func<T, bool> condition)
    {
        ArgumentNullException.ThrowIfNull(list);
        ArgumentNullException.ThrowIfNull(condition);

        if (condition(item))
            list.Add(item);
    }

    /// <summary>
    /// Shuffles a collection in-place using Fisher-Yates algorithm.
    /// Ensures random distribution without replacement.
    /// </summary>
    /// <typeparam name="T">The type of elements in the list.</typeparam>
    /// <param name="list">The list to shuffle.</param>
    /// <param name="random">Optional random number generator. If null, a new one will be created.</param>
    /// <exception cref="ArgumentNullException"><paramref name="list"/> is <see langword="null"/>.</exception>
    public static void Shuffle<T>(this IList<T> list, Random? random = null)
    {
        ArgumentNullException.ThrowIfNull(list);

        random ??= new Random();

        for (int i = list.Count - 1; i > 0; i--)
        {
            int randomIndex = random.Next(i + 1);
            (list[i], list[randomIndex]) = (list[randomIndex], list[i]);
        }
    }
}