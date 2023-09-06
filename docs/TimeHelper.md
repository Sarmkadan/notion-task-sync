# TimeHelper

Utility class providing common date/time operations for task synchronization scenarios, including UTC conversion, formatting, comparison, and scheduling helpers.

## API

### `public static DateTime GetCurrentUtcTime`
Returns the current UTC date and time.
**Returns:** `DateTime` representing the current UTC time.
**Throws:** `System.InvalidOperationException` if system clock cannot be accessed.

---

### `public static string FormatDateTime(DateTime dateTime, string format = "yyyy-MM-dd HH:mm:ss")`
Formats a `DateTime` as a string using the specified format.
**Parameters:**
- `dateTime` – The date/time to format.
- `format` – Optional format string (default: `"yyyy-MM-dd HH:mm:ss"`).
**Returns:** Formatted date/time string.
**Throws:** `System.FormatException` if the format string is invalid.

---

### `public static DateTime? ParseDateTime(string input, string[] formats = null)`
Parses a date/time string into a `DateTime` using optional format providers.
**Parameters:**
- `input` – The string to parse.
- `formats` – Optional array of format strings (default: `null`).
**Returns:** Parsed `DateTime` or `null` if parsing fails.
**Throws:** None.

---
### `public static bool IsPast(DateTime dateTime)`
Checks whether the given date/time is in the past (strictly before now).
**Parameters:**
- `dateTime` – The date/time to check.
**Returns:** `true` if the date/time is in the past; otherwise, `false`.

---
### `public static bool IsFuture(DateTime dateTime)`
Checks whether the given date/time is in the future (strictly after now).
**Parameters:**
- `dateTime` – The date/time to check.
**Returns:** `true` if the date/time is in the future; otherwise, `false`.

---
### `public static int DaysBetween(DateTime start, DateTime end)`
Calculates the number of whole days between two dates.
**Parameters:**
- `start` – The start date.
- `end` – The end date.
**Returns:** Number of days from `start` to `end` (positive if `end` is later, negative otherwise).

---
### `public static double HoursSince(DateTime pastTime)`
Calculates the number of hours elapsed since a given past time.
**Parameters:**
- `pastTime` – The past date/time.
**Returns:** Total hours elapsed (fractional).
**Throws:** `System.ArgumentOutOfRangeException` if `pastTime` is in the future.

---
### `public static bool IsOverdue(DateTime dueDate)`
Checks whether a due date is overdue (strictly before now).
**Parameters:**
- `dueDate` – The due date/time.
**Returns:** `true` if the due date is overdue; otherwise, `false`.

---
### `public static string FormatTimeSpan(TimeSpan span)`
Formats a `TimeSpan` into a human-readable string (e.g., "2 days, 3 hours").
**Parameters:**
- `span` – The time span to format.
**Returns:** Human-readable string representation.

---
### `public static string GetRelativeTime(DateTime dateTime)`
Generates a relative time string (e.g., "2 hours ago", "in 3 days").
**Parameters:**
- `dateTime` – The date/time to describe.
**Returns:** Relative time string.

---
### `public static bool ShouldSync(DateTime lastSyncTime, TimeSpan interval)`
Determines whether a sync operation should occur based on the last sync time and interval.
**Parameters:**
- `lastSyncTime` – The last successful sync time.
- `interval` – The required interval between syncs.
**Returns:** `true` if the interval has elapsed; otherwise, `false`.

---
### `public static DateTime CalculateNextSyncTime(DateTime lastSyncTime, TimeSpan interval)`
Calculates the next recommended sync time based on the last sync and interval.
**Parameters:**
- `lastSyncTime` – The last successful sync time.
- `interval` – The required interval between syncs.
**Returns:** Next sync time.

---
### `public static DateTime GetTodayStart()`
Returns the start of the current UTC day (00:00:00).
**Returns:** `DateTime` representing the start of today in UTC.

---
### `public static DateTime GetTodayEnd()`
Returns the end of the current UTC day (23:59:59.999).
**Returns:** `DateTime` representing the end of today in UTC.

---
### `public static DateTime GetWeekStart(DateTime date)`
Returns the start of the week (Monday 00:00:00) for the given date.
**Parameters:**
- `date` – The reference date.
**Returns:** `DateTime` representing the start of the week.

---
### `public static DateTime GetMonthStart(DateTime date)`
Returns the start of the month (day 1, 00:00:00) for the given date.
**Parameters:**
- `date` – The reference date.
**Returns:** `DateTime` representing the start of the month.

---
### `public static bool AreWithinInterval(DateTime date, DateTime start, DateTime end)`
Checks whether a date falls within a specified interval (inclusive).
**Parameters:**
- `date` – The date to check.
- `start` – The start of the interval.
- `end` – The end of the interval.
**Returns:** `true` if the date is within the interval; otherwise, `false`.

## Usage
