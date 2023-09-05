# CollectionExtensions
The `CollectionExtensions` class provides a set of extension methods for working with collections in C#. It offers various methods for checking, manipulating, and transforming collections, making it a useful utility class for tasks such as data processing, filtering, and grouping.

## API
* `public static bool IsNullOrEmpty<T>(this IEnumerable<T> source)`: Checks if the source collection is null or empty. Returns `true` if the collection is null or empty, `false` otherwise. Throws no exceptions.
* `public static bool HasItems<T>(this IEnumerable<T> source)`: Checks if the source collection has any items. Returns `true` if the collection has at least one item, `false` otherwise. Throws no exceptions.
* `public static T? SafeGetAt<T>(this IList<T> list, int index)`: Safely retrieves an item at the specified index from the list. Returns the item at the specified index if it exists, `null` otherwise. Throws no exceptions.
* `public static IEnumerable<IEnumerable<T>> Batch<T>(this IEnumerable<T> source, int batchSize)`: Batches the source collection into chunks of the specified size. Returns an enumerable of batches. Throws `ArgumentOutOfRangeException` if `batchSize` is less than or equal to 0.
* `public static (List<T> matching, List<T> notMatching) Partition<T>(this IEnumerable<T> source, Func<T, bool> predicate)`: Partitions the source collection into two lists based on the specified predicate. Returns a tuple containing the matching and non-matching items. Throws no exceptions.
* `public static IEnumerable<IGrouping<TKey, T>> GroupByFrequency<T, TKey>(this IEnumerable<T> source, Func<T, TKey> keySelector)`: Groups the source collection by the frequency of the specified key. Returns an enumerable of groupings. Throws no exceptions.
* `public static IEnumerable<T> Flatten<T>(this IEnumerable<IEnumerable<T>> source)`: Flattens a collection of collections into a single collection. Returns an enumerable of flattened items. Throws no exceptions.
* `public static Dictionary<TKey, TValue> SafeToDictionary<TItem, TKey, TValue>(this IEnumerable<TItem> source, Func<TItem, TKey> keySelector, Func<TItem, TValue> valueSelector)`: Safely converts a collection to a dictionary. Returns a dictionary containing the key-value pairs. Throws `ArgumentException` if the source collection contains duplicate keys.
* `public static List<List<T>> SplitWhere<T>(this IEnumerable<T> source, Func<T, bool> predicate)`: Splits the source collection into sublists based on the specified predicate. Returns a list of sublists. Throws no exceptions.
* `public static IEnumerable<T> WhereNotNull<T>(this IEnumerable<T> source)`: Filters out null items from the source collection. Returns an enumerable of non-null items. Throws no exceptions.
* `public static IEnumerable<T> DistinctBy<T, TKey>(this IEnumerable<T> source, Func<T, TKey> keySelector)`: Returns a distinct collection of items based on the specified key selector. Returns an enumerable of distinct items. Throws no exceptions.
* `public static IEnumerable<T> IntersectBy<T, TKey>(this IEnumerable<T> source, IEnumerable<T> other, Func<T, TKey> keySelector)`: Returns the intersection of two collections based on the specified key selector. Returns an enumerable of intersecting items. Throws no exceptions.
* `public static void AddIf<T>(this ICollection<T> collection, T item, Func<T, bool> predicate)`: Adds an item to the collection if the specified predicate is true. Throws no exceptions.
* `public static void Shuffle<T>(this IList<T> list)`: Shuffles the items in the list. Throws no exceptions.

## Usage
The following examples demonstrate how to use the `CollectionExtensions` class:
```csharp
// Example 1: Filtering and grouping a collection
var numbers = new[] { 1, 2, 2, 3, 3, 3, 4, 4, 4, 4 };
var groupedNumbers = numbers.GroupByFrequency(x => x);
foreach (var group in groupedNumbers)
{
    Console.WriteLine($"Number: {group.Key}, Frequency: {group.Count()}");
}

// Example 2: Batching and processing a large collection
var largeCollection = Enumerable.Range(1, 100);
var batches = largeCollection.Batch(10);
foreach (var batch in batches)
{
    Console.WriteLine($"Processing batch of {batch.Count()} items");
    // Process the batch
}
```

## Notes
* The `CollectionExtensions` class is designed to work with collections of any type, but some methods may have specific requirements or behaviors for certain types (e.g., `IList<T>` vs. `IEnumerable<T>`).
* When using methods that modify collections (e.g., `AddIf`, `Shuffle`), be aware that these modifications are made in-place and may affect other parts of the program that rely on the original collection.
* The `CollectionExtensions` class is thread-safe, but individual methods may have specific thread-safety characteristics depending on the underlying collections and operations being performed. Always consider the thread-safety implications when using these methods in multi-threaded environments.
