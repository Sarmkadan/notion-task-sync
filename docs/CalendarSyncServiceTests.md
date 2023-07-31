# CalendarSyncServiceTests

Unit tests for the `CalendarSyncService` class, which provides bidirectional synchronization between Notion tasks and calendar events. These tests verify the core export and import operations, ensuring correct behavior when writing `.ics` files and when creating or updating tasks based on calendar events.

## API

### `ExportToCalendar_WritesIcsFileWithCorrectEventCount`

Verifies that exporting tasks to a calendar file generates an `.ics` file containing the expected number of events.

- **Purpose**: Ensures that all active, non-deleted tasks are correctly serialized into a calendar file.
- **Parameters**: None.
- **Return value**: `Task` (completes when the file is written).
- **Throws**: Propagates any exceptions from underlying file system or serialization logic.

### `ImportFromCalendar_CreatesNewTasksForUnknownEvents`

Ensures that importing events from a calendar creates new Notion tasks for events that do not correspond to existing tasks.

- **Purpose**: Validates that new calendar events are translated into new tasks when no matching task exists.
- **Parameters**: None.
- **Return value**: `Task` (completes when the import is finished).
- **Throws**: Propagates exceptions from calendar parsing or Notion API calls.

### `ImportFromCalendar_UpdatesExistingTaskDueDate_WhenUidMatches`

Confirms that importing a calendar event updates the due date of an existing Notion task when the event’s UID matches the task’s identifier.

- **Purpose**: Ensures bidirectional sync updates task metadata when calendar events change.
- **Parameters**: None.
- **Return value**: `Task` (completes when the update is applied).
- **Throws**: Propagates exceptions from calendar parsing or Notion API calls.

### `ExportToCalendar_ExcludesDeletedTasks`

Confirms that tasks marked as deleted in Notion are excluded from the exported `.ics` file.

- **Purpose**: Ensures that only active tasks are synchronized to the calendar.
- **Parameters**: None.
- **Return value**: `Task` (completes when the file is written).
- **Throws**: Propagates any exceptions from file system or serialization logic.

## Usage
