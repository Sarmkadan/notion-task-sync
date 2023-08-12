# NotionPage

The `NotionPage` class serves as the primary data transfer object and domain model for representing a single page within a Notion workspace in the `notion-task-sync` project. It encapsulates essential metadata such as identifiers, timestamps, and archival status, while providing a flexible dictionary-based structure for dynamic page properties. Beyond data storage, the type includes state management capabilities to track synchronization freshness (`IsStale`, `LastSyncTime`) and offers typed accessors for property manipulation, ensuring type safety when interacting with the heterogeneous data structure of the Notion API.

## API

### Properties

*   **`public required string PageId`**
    The unique identifier for the Notion page. This property is mandatory and must be set during object initialization.

*   **`public required string DatabaseId`**
    The unique identifier of the parent database containing this page. This property is mandatory and must be set during object initialization.

*   **`public required string Title`**
    The human-readable title of the page. This property is mandatory and must be set during object initialization.

*   **`public Dictionary<string, object?>? Properties`**
    A dictionary containing the dynamic properties of the page, where the key is the property name and the value is the raw data object. This may be `null` if no properties are present.

*   **`public DateTime CreatedTime`**
    The UTC timestamp indicating when the page was originally created in Notion.

*   **`public DateTime LastEditedTime`**
    The UTC timestamp indicating when the page was last modified in Notion.

*   **`public string? CreatedBy`**
    The identifier or name of the user who created the page. This value may be `null` if the information is unavailable.

*   **`public string? LastEditedBy`**
    The identifier or name of the user who last edited the page. This value may be `null` if the information is unavailable.

*   **`public bool Archived`**
    A flag indicating whether the page has been moved to the trash (archived) in Notion.

*   **`public bool IsStale`**
    A local state flag indicating whether the data in this instance is out of sync with the remote source.

*   **`public DateTime? LastSyncTime`**
    The local timestamp recording when this instance was last successfully synchronized with the Notion API. This is `null` if no sync has occurred.

*   **`public string? Url`**
    The full HTTPS URL linking directly to the page in the Notion workspace.

### Methods

*   **`public T? GetProperty<T>(string propertyName)`**
    Retrieves a specific property value cast to the specified generic type `T`.
    *   **Parameters**: `propertyName` (The key of the property in the `Properties` dictionary).
    *   **Returns**: The value cast to `T`, or `default(T)` (usually `null` for reference types) if the key does not exist or the cast fails.
    *   **Throws**: Does not throw if the key is missing; returns default. May throw `InvalidCastException` if the underlying object cannot be cast to `T`.

*   **`public void SetProperty(string propertyName, object? value)`**
    Sets or updates a specific property in the `Properties` dictionary.
    *   **Parameters**: `propertyName` (The key), `value` (The data to store).
    *   **Returns**: `void`.
    *   **Throws**: Throws `ArgumentNullException` if `propertyName` is null or empty.

*   **`public void MarkAsStale()`**
    Sets the `IsStale` flag to `true`, indicating that the local data requires re-validation or re-fetching.
    *   **Returns**: `void`.

*   **`public void UpdateSyncTime()`**
    Updates the `LastSyncTime` to the current UTC time and sets `IsStale` to `false`.
    *   **Returns**: `void`.

*   **`public void Archive()`**
    Sets the `Archived` flag to `true`. This is a local state change and does not automatically invoke an API call to Notion.
    *   **Returns**: `void`.

*   **`public bool Validate()`**
    Performs integrity checks on the required fields and internal state.
    *   **Returns**: `true` if the instance is valid (e.g., `PageId`, `DatabaseId`, and `Title` are populated); otherwise `false`.

*   **`public override string ToString()`**
    Returns a string representation of the page, typically formatted as `"Title (PageId)"`.
    *   **Returns**: A formatted string.

## Usage

### Example 1: Instantiation and Property Access
This example demonstrates creating a new `NotionPage` instance using object initializers for required fields and retrieving a typed property.

```csharp
var page = new NotionPage
{
    PageId = "a1b2c3d4-e5f6-7890-g1h2-i3j4k5l6m7n8",
    DatabaseId = "db-9876543210",
    Title = "Q4 Marketing Plan",
    CreatedTime = DateTime.UtcNow,
    LastEditedTime = DateTime.UtcNow,
    Archived = false
};

// Set a custom property
page.SetProperty("Status", "In Progress");
page.SetProperty("Budget", 50000.00);

// Retrieve a typed property
string? status = page.GetProperty<string>("Status");
double? budget = page.GetProperty<double>("Budget");

if (page.Validate())
{
    Console.WriteLine($"Validated: {page}");
}
```

### Example 2: Synchronization State Management
This example illustrates managing the synchronization lifecycle, marking a page as stale after a local modification and updating the sync time after a successful remote update.

```csharp
public void ProcessPageUpdate(NotionPage page)
{
    // Simulate a local change that invalidates remote consistency
    page.SetProperty("LastModifiedLocal", DateTime.Now);
    page.MarkAsStale();

    if (page.IsStale)
    {
        Console.WriteLine($"Page '{page.Title}' is stale. Initiating sync...");
        
        // ... Perform API call to update Notion ...
        
        // Update local state upon success
        page.UpdateSyncTime();
        page.Archive(); // Example: conditionally archiving
        
        Console.WriteLine($"Sync complete at {page.LastSyncTime}. Archived: {page.Archived}");
    }
}
```

## Notes

*   **Initialization Requirements**: As `PageId`, `DatabaseId`, and `Title` are marked as `required`, instances cannot be created without explicitly setting these values. Failure to do so will result in a compile-time error or a runtime `NullReferenceException` depending on the initialization context.
*   **Type Safety in Properties**: The `Properties` dictionary stores values as `object?`. When using `GetProperty<T>`, ensure the expected type matches the actual stored type to avoid `InvalidCastException`. The method returns `default(T)` silently if a key is missing, so null-checking the result is recommended for reference types.
*   **State vs. API Actions**: Methods such as `Archive()`, `MarkAsStale()`, and `SetProperty()` modify the local state of the object only. They do not trigger HTTP requests to the Notion API. The consumer of this class is responsible for persisting these changes to the remote service.
*   **Thread Safety**: The `NotionPage` class is not thread-safe. The `Properties` dictionary and mutable boolean flags (`IsStale`, `Archived`) can be modified concurrently. If an instance is shared across multiple threads, external locking mechanisms must be used during read/write operations.
*   **Nullability**: `Properties`, `CreatedBy`, `LastEditedBy`, `Url`, and `LastSyncTime` are nullable. Consumers should handle `null` cases appropriately, particularly when accessing `Properties` before calling `SetProperty` or `GetProperty`.
