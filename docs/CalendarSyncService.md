# CalendarSyncService

`CalendarSyncService` orchestrates synchronization between the Notion task database and an external calendar provider. It handles the full lifecycle of exporting local tasks to calendar events, importing external calendar events as Notion tasks, and performing bidirectional merges that reconcile both sources according to configurable conflict-resolution rules.

## API

### `public CalendarSyncService`

Constructor. Initializes a new instance of the service with the required dependencies for calendar access and Notion API communication. The specific constructor parameters are determined by the underlying implementation and injected at composition time.

### `public async Task<CalendarSyncResult> ExportToCalendarAsync`

Exports Notion tasks that match the configured criteria to the external calendar as events.

- **Parameters:** Accepts an `ExportRequest` (or equivalent configuration object) specifying the source Notion database, date range, task filters, and target calendar identifier.
- **Returns:** A `CalendarSyncResult` containing the count of created events, any failures per task, and a summary status.
- **Throws:** `ArgumentNullException` when the request is null; `CalendarAuthenticationException` when the calendar provider credentials are invalid or expired; `NotionApiException` when the Notion API returns an error during task retrieval.

### `public async Task<CalendarSyncResult> ImportFromCalendarAsync`

Imports events from the external calendar into the Notion task database.

- **Parameters:** Accepts an `ImportRequest` (or equivalent configuration object) specifying the source calendar, date range, event filters, and target Notion database.
- **Returns:** A `CalendarSyncResult` containing the count of created Notion tasks, any failures per event, and a summary status.
- **Throws:** `ArgumentNullException` when the request is null; `CalendarAuthenticationException` when the calendar provider credentials are invalid or expired; `NotionApiException` when the Notion API returns an error during task creation.

### `public async Task<CalendarSyncResult> BidirectionalSyncAsync`

Performs a two-way synchronization between Notion tasks and calendar events. Creates, updates, or deletes items on both sides to bring them into agreement based on the configured conflict-resolution strategy (e.g., last-write-wins, Notion-as-source-of-truth, or calendar-as-source-of-truth).

- **Parameters:** Accepts a `BidirectionalSyncRequest` (or equivalent configuration object) specifying both Notion and calendar identifiers, date range, conflict-resolution mode, and field-mapping rules.
- **Returns:** A `CalendarSyncResult` containing aggregate counts of created, updated, and deleted items on each side, per-item failure details, and a summary status.
- **Throws:** `ArgumentNullException` when the request is null; `CalendarAuthenticationException` when the calendar provider credentials are invalid or expired; `NotionApiException` when the Notion API returns an error; `SyncConflictException` when an unresolvable conflict is encountered and the configured strategy does not allow automatic resolution.

## Usage

### Example 1: Exporting a week of tasks to a calendar

```csharp
var syncService = new CalendarSyncService(calendarAdapter, notionClient);

var exportRequest = new ExportRequest
{
    SourceDatabaseId = "notion-db-123",
    TargetCalendarId = "primary",
    DateRange = (DateTime.Today, DateTime.Today.AddDays(7)),
    TaskFilter = t => t.Status == "Scheduled" && t.DueDate != null
};

CalendarSyncResult result = await syncService.ExportToCalendarAsync(exportRequest);

Console.WriteLine($"Exported {result.CreatedCount} tasks.");
if (result.Failures.Any())
{
    foreach (var failure in result.Failures)
        Console.WriteLine($"Failed: {failure.ItemId} — {failure.Reason}");
}
```

### Example 2: Bidirectional sync with last-write-wins

```csharp
var syncService = new CalendarSyncService(calendarAdapter, notionClient);

var syncRequest = new BidirectionalSyncRequest
{
    NotionDatabaseId = "notion-db-123",
    CalendarId = "primary",
    DateRange = (DateTime.Today.AddDays(-7), DateTime.Today.AddDays(14)),
    ConflictResolution = ConflictResolutionMode.LastWriteWins,
    FieldMapping = new Dictionary<string, string>
    {
        ["Title"] = "summary",
        ["DueDate"] = "start.dateTime",
        ["Description"] = "description"
    }
};

CalendarSyncResult result = await syncService.BidirectionalSyncAsync(syncRequest);

Console.WriteLine($"Created: {result.CreatedCount}, Updated: {result.UpdatedCount}, Deleted: {result.DeletedCount}");
```

## Notes

- All three methods accept a date range; items outside that window are ignored on both sides. Narrow ranges reduce the risk of large-scale unintended modifications.
- `BidirectionalSyncAsync` may throw `SyncConflictException` when two items have diverged and the selected conflict-resolution mode prohibits automatic merging. Callers should catch this exception and either log for manual intervention or retry with a different resolution mode.
- The service does not maintain internal mutable state across calls. Each method invocation is independent, making instances safe for concurrent use from multiple threads without additional synchronization.
- Calendar provider authentication tokens are typically short-lived. `CalendarAuthenticationException` can surface at any point during a long-running sync operation; callers should implement retry logic with token refresh where appropriate.
- `CalendarSyncResult.Failures` may contain partial failures even when the overall operation succeeds. Always inspect the failures collection after a non-throwing return to detect per-item errors such as rate-limiting or malformed field data.
