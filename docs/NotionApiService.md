# NotionApiService

`NotionApiService` provides a client interface for interacting with the Notion API to manage pages within a task synchronization context. It encapsulates the creation, retrieval, updating, and archival of Notion pages, as well as connectivity verification, exposing asynchronous operations that return strongly typed `NotionPage` objects.

## API

### NotionApiService
Constructor for the service. Initializes a new instance configured to communicate with the Notion API.

### FetchPagesAsync
```csharp
public async Task<List<NotionPage>> FetchPagesAsync()
```
Retrieves all accessible pages from the configured Notion workspace or database.  
**Returns:** A list of `NotionPage` objects representing the current state of each page.  
**Throws:** May throw `HttpRequestException` or a Notion-specific exception if the API request fails due to network issues, authentication problems, or rate limiting.

### FetchPagesSinceAsync
```csharp
public async Task<List<NotionPage>> FetchPagesSinceAsync(DateTime since)
```
Retrieves pages that have been created or updated after the specified timestamp. Useful for incremental synchronization.  
**Parameters:**  
- `since` — A `DateTime` value representing the exclusive lower bound for page modification timestamps.  
**Returns:** A filtered list of `NotionPage` objects modified after the given time.  
**Throws:** Throws `ArgumentException` if `since` is in the future or represents an invalid timestamp range. May throw the same API-level exceptions as `FetchPagesAsync`.

### FetchPageAsync
```csharp
public async Task<NotionPage> FetchPageAsync(string pageId)
```
Retrieves a single page by its Notion identifier.  
**Parameters:**  
- `pageId` — The UUID string of the target page.  
**Returns:** The `NotionPage` corresponding to the given identifier.  
**Throws:** Throws `ArgumentException` if `pageId` is null or empty. Throws a `NotFoundException` (or equivalent API-wrapped exception) if the page does not exist or is inaccessible.

### CreatePageAsync
```csharp
public async Task<NotionPage> CreatePageAsync(NotionPage page)
```
Creates a new page in Notion based on the supplied `NotionPage` object’s properties.  
**Parameters:**  
- `page` — A `NotionPage` instance containing the desired title, properties, and parent database or page information.  
**Returns:** The newly created `NotionPage` as confirmed by the API, including the assigned identifier.  
**Throws:** Throws `ArgumentNullException` if `page` is null. Throws `ValidationException` (or similar) if required properties are missing or malformed. API communication failures produce the standard HTTP-related exceptions.

### UpdatePageAsync
```csharp
public async Task<NotionPage> UpdatePageAsync(string pageId, NotionPage updatedPage)
```
Updates an existing page’s properties with the values provided in `updatedPage`.  
**Parameters:**  
- `pageId` — The UUID string of the page to update.  
- `updatedPage` — A `NotionPage` object containing the new property values to apply.  
**Returns:** The updated `NotionPage` as returned by the API.  
**Throws:** Throws `ArgumentException` for null or empty `pageId`. Throws `ArgumentNullException` if `updatedPage` is null. Throws a `NotFoundException` if the target page does not exist. API-level exceptions apply.

### ArchivePageAsync
```csharp
public async System.Threading.Tasks.Task ArchivePageAsync(string pageId)
```
Archives (soft-deletes) a page by its identifier. The page is removed from active views but remains recoverable via the Notion UI or API.  
**Parameters:**  
- `pageId` — The UUID string of the page to archive.  
**Returns:** A `Task` representing the asynchronous operation. No value is returned upon success.  
**Throws:** Throws `ArgumentException` for null or empty `pageId`. Throws a `NotFoundException` if the page does not exist. API communication failures produce the standard HTTP-related exceptions.

### TestConnectionAsync
```csharp
public async Task<bool> TestConnectionAsync()
```
Verifies that the service can successfully authenticate and communicate with the Notion API.  
**Returns:** `true` if a minimal API request completes without errors; otherwise `false`.  
**Throws:** Does not throw under normal circumstances. Network or authentication failures are caught internally and result in a `false` return value.

## Usage

### Example 1: Incremental Sync
```csharp
var service = new NotionApiService();
DateTime lastSync = DateTime.UtcNow.AddHours(-1);

try
{
    List<NotionPage> recentPages = await service.FetchPagesSinceAsync(lastSync);
    foreach (var page in recentPages)
    {
        Console.WriteLine($"Processing updated page: {page.Title}");
    }
}
catch (HttpRequestException ex)
{
    Console.WriteLine($"Sync failed due to network error: {ex.Message}");
}
```

### Example 2: Create and Archive a Task Page
```csharp
var service = new NotionApiService();

var newTask = new NotionPage
{
    Title = "Review pull request",
    Properties = { /* task-specific properties */ }
};

NotionPage created = await service.CreatePageAsync(newTask);
Console.WriteLine($"Created page with ID: {created.Id}");

// Later, when the task is complete:
await service.ArchivePageAsync(created.Id);
Console.WriteLine("Task page archived.");
```

## Notes

- **Edge Cases:** `FetchPagesSinceAsync` with a timestamp equal to the exact modification time of a page may exclude that page depending on API precision; use a slightly earlier timestamp to ensure inclusion. Passing an empty or whitespace `pageId` to `FetchPageAsync`, `UpdatePageAsync`, or `ArchivePageAsync` will consistently throw `ArgumentException` before any network call is made. `TestConnectionAsync` returns `false` rather than throwing, making it safe to call in health-check loops without try/catch blocks.
- **Thread Safety:** Instance methods are not guaranteed to be thread-safe. Concurrent calls to `CreatePageAsync`, `UpdatePageAsync`, or `ArchivePageAsync` on the same service instance should be externally synchronized if they target overlapping resources. The underlying HTTP client may be reused, but the service itself does not implement internal locking.
- **Rate Limiting:** The Notion API enforces rate limits. Rapid successive calls to any method may result in `HttpRequestException` with a 429 status code. Callers should implement exponential backoff or throttle requests accordingly.
- **Archival Behavior:** `ArchivePageAsync` performs a soft delete. The page is not permanently destroyed and can be restored through the Notion UI or a separate API call not exposed by this service.
