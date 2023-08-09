# CalendarEvent

The `CalendarEvent` class serves as the core data transfer object within the `notion-task-sync` project, representing a singular calendar entry synchronized between external calendar providers and Notion tasks. It encapsulates both the semantic details of the event (such as title, timing, and location) and operational metadata required for synchronization logic, including source identification, linkage to specific Notion tasks, and statistical counters tracking the outcome of sync operations. This type supports both incoming imports and outgoing exports, maintaining state through validation flags and warning collections to ensure data integrity during complex synchronization cycles.

## API

### `Id`
```csharp
public Guid Id
```
A unique identifier for the calendar event instance within the local synchronization context. This value is typically generated upon instantiation and remains immutable for the lifecycle of the object to ensure consistent tracking across sync batches.

### `Title`
```csharp
public required string Title
```
The mandatory display name of the calendar event. As a `required` member, this property must be initialized during object construction; failure to provide a value will result in a compile-time error or a runtime `InvalidOperationException` depending on the C# version and initialization context.

### `Description`
```csharp
public string? Description
```
An optional detailed body text associated with the event. This property may be `null` if no description is provided by the source calendar or if the user has not entered one.

### `StartDate`
```csharp
public DateTime StartDate
```
The precise date and time marking the beginning of the event. For all-day events, this typically represents the start of the day in the relevant timezone, though the `DateTime` kind (UTC vs. Local) should be verified based on the `Source` configuration.

### `EndDate`
```csharp
public DateTime? EndDate
```
The optional date and time marking the conclusion of the event. This property is `null` for events that have no defined end time or for specific all-day configurations where duration is inferred solely from the start date.

### `IsAllDay`
```csharp
public bool IsAllDay
```
A boolean flag indicating whether the event spans an entire day rather than a specific time range. When `true`, time components in `StartDate` and `EndDate` are often ignored by rendering engines, and the `GetDuration` logic may adjust accordingly.

### `Location`
```csharp
public string? Location
```
An optional string specifying the physical or virtual location of the event. This may contain addresses, conference room names, or meeting links.

### `ExternalUid`
```csharp
public string? ExternalUid
```
The unique identifier assigned to this event by the external calendar provider (e.g., Google Calendar ID or Outlook Exchange ID). This is used to correlate local instances with remote resources during update and delete operations. It is `null` for locally created events that have not yet been pushed to an external provider.

### `LinkedTaskId`
```csharp
public Guid? LinkedTaskId
```
The identifier of the corresponding Notion task linked to this calendar event. If the event exists independently without a bidirectional sync to a specific Notion page, this value is `null`.

### `CreatedAt`
```csharp
public DateTime CreatedAt
```
The timestamp indicating when this `CalendarEvent` record was initially created in the local database or synchronization store.

### `UpdatedAt`
```csharp
public DateTime UpdatedAt
```
The timestamp of the last modification made to this record. This is used during conflict resolution to determine which version of the event (local vs. remote) is more recent.

### `Source`
```csharp
public CalendarEventSource Source
```
An enumeration value defining the origin of the event (e.g., `Google`, `Outlook`, `Notion`, `Local`). This dictates which synchronization adapter processes the event and determines the rules for field mapping.

### `Validate`
```csharp
public bool Validate
```
A control flag used during the synchronization pipeline. When set to `true`, the event undergoes strict schema and logic validation before being committed or exported. If `false`, validation steps may be skipped for performance or during specific bulk import scenarios.

### `GetDuration`
```csharp
public TimeSpan? GetDuration
```
A computed property or method accessor (signature implies a getter) that returns the calculated length of the event. It returns `null` if the `EndDate` is missing or if the calculation cannot be performed (e.g., invalid date ranges). For all-day events, this typically returns a multiple of 24 hours.

### `EventsExported`
```csharp
public int EventsExported
```
A counter tracking the number of successful export operations performed for this specific event configuration or batch context. In single-event instances, this usually reflects the success count of the last push operation.

### `EventsImported`
```csharp
public int EventsImported
```
A counter tracking the number of successful import operations where this event definition was utilized or created from an external source.

### `TasksCreated`
```csharp
public int TasksCreated
```
The total count of Notion tasks successfully generated from this calendar event during synchronization runs.

### `TasksUpdated`
```csharp
public int TasksUpdated
```
The total count of existing Notion tasks that were modified to reflect changes in this calendar event.

### `Skipped`
```csharp
public int Skipped
```
A counter indicating how many times processing for this event was intentionally bypassed due to filters, unchanged state, or specific exclusion rules.

### `Warnings`
```csharp
public List<string> Warnings
```
A collection of non-fatal error messages or alerts generated during the processing of this event. This list allows the synchronization engine to continue operation while logging issues such as missing fields, deprecated properties, or partial sync failures.

## Usage

### Example 1: Creating and Validating a New Event
This example demonstrates instantiating a `CalendarEvent` with required fields, linking it to a Notion task, and triggering validation.

```csharp
var taskId = Guid.NewGuid();
var calendarEvent = new CalendarEvent
{
    Id = Guid.NewGuid(),
    Title = "Q3 Planning Review",
    Description = "Review quarterly goals and assign action items.",
    StartDate = DateTime.UtcNow.AddDays(1).Date.AddHours(10),
    EndDate = DateTime.UtcNow.AddDays(1).Date.AddHours(11),
    IsAllDay = false,
    Location = "Conference Room B",
    Source = CalendarEventSource.Google,
    LinkedTaskId = taskId,
    Validate = true,
    CreatedAt = DateTime.UtcNow,
    UpdatedAt = DateTime.UtcNow
};

// Simulate a processing step that might add warnings
if (string.IsNullOrEmpty(calendarEvent.ExternalUid))
{
    calendarEvent.Warnings.Add("Event has not yet been pushed to external provider.");
}

// Check duration
var duration = calendarEvent.GetDuration;
if (duration.HasValue && duration.Value.TotalMinutes > 60)
{
    calendarEvent.Warnings.Add("Event duration exceeds standard meeting slot.");
}
```

### Example 2: Processing Sync Statistics
This example illustrates how to inspect synchronization metrics and handle warnings after a sync operation.

```csharp
public void ProcessSyncResult(CalendarEvent evt)
{
    if (!evt.Validate)
    {
        Console.WriteLine($"Skipping validation for event: {evt.Title}");
        return;
    }

    Console.WriteLine($"Sync Stats for '{evt.Title}':");
    Console.WriteLine($"- Tasks Created: {evt.TasksCreated}");
    Console.WriteLine($"- Tasks Updated: {evt.TasksUpdated}");
    Console.WriteLine($"- Skipped Operations: {evt.Skipped}");

    if (evt.Warnings.Any())
    {
        Console.WriteLine("Warnings detected during sync:");
        foreach (var warning in evt.Warnings)
        {
            Console.WriteLine($"  [!] {warning}");
        }
    }

    if (evt.EventsExported > 0 && evt.ExternalUid == null)
    {
        // Logical inconsistency check based on counters
        throw new InvalidOperationException("Event marked as exported but lacks ExternalUid.");
    }
}
```

## Notes

*   **Nullability and Required Fields**: The `Title` property is marked as `required`, ensuring that no instance can exist without a title. However, `EndDate`, `Description`, `Location`, `ExternalUid`, and `LinkedTaskId` are nullable. Consumers must explicitly check for `null` before accessing these members to avoid `NullReferenceException`.
*   **Duration Calculation**: The `GetDuration` member returns a `TimeSpan?`. Callers must handle the `null` case, which occurs if `EndDate` is not set or if `StartDate` is later than `EndDate`. Logic relying on event length should not assume a valid `TimeSpan` is always present.
*   **Thread Safety**: The `Warnings` property is a mutable `List<string>`. This class is not thread-safe by default. If `CalendarEvent` instances are shared across multiple threads (e.g., parallel sync workers), external locking mechanisms must be used when adding to the `Warnings` list or modifying counters like `TasksCreated` and `EventsExported`.
*   **Date Kinds**: The `DateTime` properties (`StartDate`, `EndDate`, `CreatedAt`, `UpdatedAt`) do not enforce a specific `Kind` (UTC vs. Local) in their signature. Implementations must ensure consistency, typically preferring UTC for storage and conversion to local time only at the presentation layer, especially when `IsAllDay` is involved to prevent timezone drift.
*   **Statistical Integrity**: The integer counters (`EventsExported`, `TasksUpdated`, etc.) are primitive types. In high-concurrency environments where these counts are incremented simultaneously, race conditions may lead to inaccurate totals. These properties are intended for post-operation reporting rather than real-time atomic state management.
