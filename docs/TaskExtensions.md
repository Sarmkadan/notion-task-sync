# TaskExtensions

Utility class providing extension and helper methods for working with `Task` objects in the Notion task synchronization system. These methods simplify common checks and transformations when evaluating task due dates, priorities, tags, and lifecycle states.

## API

### `public static bool IsOverdue(DateTime dueDate)`

Determines whether a task's due date is in the past relative to the current system time.

- **Parameters**
  - `dueDate`: The due date of the task to check.
- **Return value**
  - `true` if `dueDate` is earlier than `DateTime.UtcNow`; otherwise, `false`.
- **Exceptions**
  - Throws `ArgumentOutOfRangeException` if `dueDate` is `DateTime.MinValue` or `DateTime.MaxValue`.

---

### `public static bool IsDueToday(DateTime dueDate)`

Checks if a task's due date falls on the current calendar day in UTC.

- **Parameters**
  - `dueDate`: The due date of the task to check.
- **Return value**
  - `true` if `dueDate` is on the same UTC calendar day as `DateTime.UtcNow`; otherwise, `false`.
- **Exceptions**
  - Throws `ArgumentOutOfRangeException` if `dueDate` is `DateTime.MinValue` or `DateTime.MaxValue`.

---

### `public static bool IsHighPriority(string priority)`

Determines whether a task's priority string indicates high urgency.

- **Parameters**
  - `priority`: The priority label of the task (case-insensitive).
- **Return value**
  - `true` if `priority` equals `"high"` (case-insensitive); otherwise, `false`.
- **Exceptions**
  - Throws `ArgumentNullException` if `priority` is `null`.

---

### `public static bool IsBlocked(string status)`

Checks if a task's status indicates it is blocked or impeded.

- **Parameters**
  - `status`: The status label of the task (case-insensitive).
- **Return value**
  - `true` if `status` equals `"blocked"` or `"impeded"` (case-insensitive); otherwise, `false`.
- **Exceptions**
  - Throws `ArgumentNullException` if `status` is `null`.

---
### `public static int GetAgeInDays(DateTime createdDate)`

Calculates the number of full days elapsed since the task was created.

- **Parameters**
  - `createdDate`: The creation timestamp of the task.
- **Return value**
  - The number of full days between `createdDate` and `DateTime.UtcNow`, truncated toward zero.
- **Exceptions**
  - Throws `ArgumentOutOfRangeException` if `createdDate` is `DateTime.MinValue` or `DateTime.MaxValue`.

---
### `public static bool IsRecent(DateTime createdDate, int thresholdDays = 7)`

Determines whether a task was created within a specified number of recent days.

- **Parameters**
  - `createdDate`: The creation timestamp of the task.
  - `thresholdDays`: The maximum age in days to consider "recent" (default: 7).
- **Return value**
  - `true` if the age of `createdDate` is less than or equal to `thresholdDays`; otherwise, `false`.
- **Exceptions**
  - Throws `ArgumentOutOfRangeException` if `createdDate` is `DateTime.MinValue` or `DateTime.MaxValue`.
  - Throws `ArgumentOutOfRangeException` if `thresholdDays` is negative.

---
### `public static string GetPriorityLevel(string priority)`

Maps a raw priority string to a standardized level.

- **Parameters**
  - `priority`: The raw priority label of the task (case-insensitive).
- **Return value**
  - One of `"low"`, `"medium"`, `"high"`, or `"none"` based on the input:
    - `"low"` or `"lowest"` → `"low"`
    - `"medium"` → `"medium"`
    - `"high"` or `"urgent"` → `"high"`
    - Any other value → `"none"`
- **Exceptions**
  - Throws `ArgumentNullException` if `priority` is `null`.

---
### `public static bool HasTags(string tags)`

Checks whether a task has any non-empty tags.

- **Parameters**
  - `tags`: A comma-separated string of tags associated with the task.
- **Return value**
  - `true` if `tags` is not `null`, not empty, and contains at least one non-whitespace character; otherwise, `false`.
- **Exceptions**
  - None.

---
### `public static IEnumerable<string> GetTagList(string tags)`

Splits a comma-separated tag string into an enumerable list of trimmed, non-empty tags.

- **Parameters**
  - `tags`: A comma-separated string of tags.
- **Return value**
  - An `IEnumerable<string>` containing each non-empty, whitespace-trimmed tag from `tags`, in original order.
- **Exceptions**
  - None.

---
### `public static bool HasTag(string tags, string tag)`

Checks whether a specific tag exists in a comma-separated tag string.

- **Parameters**
  - `tags`: A comma-separated string of tags.
  - `tag`: The tag to search for (case-insensitive).
- **Return value**
  - `true` if `tag` (case-insensitive) appears as a distinct, trimmed substring in `tags`; otherwise, `false`.
- **Exceptions**
  - Throws `ArgumentNullException` if `tags` or `tag` is `null`.

## Usage
