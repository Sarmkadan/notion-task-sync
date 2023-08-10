# ChangeLog

The `ChangeLog` class serves as an immutable audit record representing a specific state transition or modification event within the `notion-task-sync` project. It captures the context of changes applied to tasks, including the nature of the modification, the values before and after the update, and the origin of the change. This entity is critical for conflict detection, synchronization history tracking, and providing detailed summaries of task evolution over time.

## API

### Properties

*   **`public Guid Id`**
    *   **Purpose:** Uniquely identifies this specific log entry within the system.
    *   **Return Value:** A globally unique identifier.
    *   **Remarks:** Automatically generated upon instantiation; never null.

*   **`public required Guid TaskId`**
    *   **Purpose:** Links the log entry to the specific task that was modified.
    *   **Return Value:** The unique identifier of the associated task.
    *   **Remarks:** Must be provided during object initialization.

*   **`public required string ChangeType`**
    *   **Purpose:** Categorizes the kind of operation performed (e.g., "Create", "Update", "Delete", "PropertyChange").
    *   **Return Value:** A string descriptor of the action.
    *   **Remarks:** Must be provided during object initialization.

*   **`public string? PropertyName`**
    *   **Purpose:** Specifies the name of the specific task property that was altered, if applicable.
    *   **Return Value:** The property name, or `null` if the change does not target a specific property.

*   **`public string? OldValue`**
    *   **Purpose:** Stores the state of the data prior to the modification.
    *   **Return Value:** The previous value as a string representation, or `null` if unavailable or not applicable.

*   **`public string? NewValue`**
    *   **Purpose:** Stores the state of the data after the modification.
    *   **Return Value:** The current value as a string representation, or `null` if the value was cleared.

*   **`public ChangeSource Source`**
    *   **Purpose:** Indicates the origin of the change (e.g., Local User, Notion API, Background Sync).
    *   **Return Value:** An enumeration value of type `ChangeSource`.

*   **`public DateTime Timestamp`**
    *   **Purpose:** Records the exact date and time the change occurred.
    *   **Return Value:** A `DateTime` object, typically in UTC.

*   **`public string? UserEmail`**
    *   **Purpose:** Identifies the user responsible for the change, if known.
    *   **Return Value:** The email address of the user, or `null` for system-generated changes.

*   **`public string? Description`**
    *   **Purpose:** Provides a human-readable explanation or context for the change.
    *   **Return Value:** A descriptive string, or `null` if no description was provided.

*   **`public bool IsConflict`**
    *   **Purpose:** Flags whether this specific change resulted in or is part of a synchronization conflict.
    *   **Return Value:** `true` if a conflict exists; otherwise `false`.

*   **`public string? ConflictResolutionStrategy`**
    *   **Purpose:** Documents the method used to resolve the conflict if `IsConflict` is true.
    *   **Return Value:** A string describing the strategy (e.g., "KeepLocal", "KeepRemote", "Merge"), or `null` if no conflict exists or resolution is pending.

*   **`public bool Validate`**
    *   **Purpose:** Indicates whether the change data has passed integrity checks.
    *   **Return Value:** `true` if the entry is valid; otherwise `false`.

### Methods

*   **`public string GetSummary()`**
    *   **Purpose:** Generates a concise, human-readable summary of the change event.
    *   **Return Value:** A formatted string combining `ChangeType`, `PropertyName`, and value transitions.
    *   **Exceptions:** Does not throw under normal conditions; returns an empty string if critical data is missing.

*   **`public void MarkAsConflict(string resolutionStrategy)`**
    *   **Purpose:** Updates the instance to reflect a conflict state and records the chosen resolution.
    *   **Parameters:**
        *   `resolutionStrategy`: The strategy employed to resolve the conflict.
    *   **Return Value:** `void`.
    *   **Exceptions:** May throw `ArgumentNullException` if `resolutionStrategy` is null or empty. Updates `IsConflict` to `true`.

*   **`public bool IsWithinTimeWindow(DateTime start, DateTime end)`**
    *   **Purpose:** Determines if the `Timestamp` of this log entry falls within a specified range.
    *   **Parameters:**
        *   `start`: The beginning of the time window (inclusive).
        *   `end`: The end of the time window (inclusive).
    *   **Return Value:** `true` if `Timestamp` is greater than or equal to `start` and less than or equal to `end`; otherwise `false`.
    *   **Exceptions:** Does not throw; handles standard `DateTime` comparisons.

## Usage

### Example 1: Creating and Summarizing a Change Entry
This example demonstrates instantiating a `ChangeLog` entry for a property update and generating a summary for logging purposes.

```csharp
var changeEntry = new ChangeLog
{
    Id = Guid.NewGuid(),
    TaskId = Guid.Parse("a1b2c3d4-e5f6-7890-g1h2-i3j4k5l6m7n8"),
    ChangeType = "PropertyUpdate",
    PropertyName = "Status",
    OldValue = "In Progress",
    NewValue = "Completed",
    Source = ChangeSource.LocalUser,
    Timestamp = DateTime.UtcNow,
    UserEmail = "developer@example.com",
    Description = "Marked task as completed via desktop client",
    Validate = true
};

// Output a summary for audit logs
Console.WriteLine($"Audit: {changeEntry.GetSummary()}");
// Output: Audit: PropertyUpdate on 'Status' changed from 'In Progress' to 'Completed'
```

### Example 2: Conflict Detection and Resolution
This example illustrates checking a time window for recent changes and marking an entry as a conflict with a specific resolution strategy.

```csharp
DateTime windowStart = DateTime.UtcNow.AddMinutes(-5);
DateTime windowEnd = DateTime.UtcNow;

if (changeEntry.IsWithinTimeWindow(windowStart, windowEnd))
{
    // Simulate conflict detection logic
    if (detectConflict(changeEntry)) 
    {
        changeEntry.MarkAsConflict("KeepRemote");
        
        if (changeEntry.IsConflict)
        {
            Console.WriteLine($"Conflict resolved for task {changeEntry.TaskId}: {changeEntry.ConflictResolutionStrategy}");
        }
    }
}
```

## Notes

*   **Immutability vs. State Mutation:** While most properties are set at initialization (indicated by `required`), the `IsConflict` and `ConflictResolutionStrategy` fields are mutable via the `MarkAsConflict` method. Care should be taken when sharing instances across threads; if an instance is being read while `MarkAsConflict` is called, race conditions may occur regarding the conflict state. External synchronization is recommended for concurrent access.
*   **Nullable Values:** Properties such as `PropertyName`, `OldValue`, `NewValue`, `UserEmail`, and `Description` are nullable. Consumers must handle `null` values gracefully, particularly when generating summaries or displaying history, as not all change types (e.g., Task Creation) will have an `OldValue`.
*   **Validation State:** The `Validate` property is a boolean flag and does not automatically enforce data integrity. It is the responsibility of the creating service to ensure that `Validate` is only set to `true` after all required business rules (e.g., valid `TaskId` reference, non-empty `ChangeType`) have been satisfied.
*   **Time Zone Handling:** The `Timestamp` property should consistently be stored in UTC to ensure `IsWithinTimeWindow` functions correctly across different server locales. Passing local time instances to `IsWithinTimeWindow` without conversion may result in incorrect filtering.
