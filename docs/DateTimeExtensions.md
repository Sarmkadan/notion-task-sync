# DateTimeExtensions

A utility class providing common date and time manipulation and formatting extensions for `System.DateTime` in .NET applications. Designed to simplify working with timestamps, day boundaries, rounding, and human-readable formatting in the `notion-task-sync` project.

## API

### `public static bool IsWithinDays(DateTime date, int days)`

Determines whether the given `date` falls within the specified number of `days` from the current UTC time.

- **Parameters**:
  - `date` (`DateTime`): The date to check.
  - `days` (`int`): The number of days to compare against.
- **Returns**: `true` if the absolute difference between `date` and `DateTime.UtcNow` is less than or equal to `days`; otherwise, `false`.
- **Throws**: `ArgumentOutOfRangeException` if `days` is negative.

---

### `public static bool IsNewerThan(DateTime date, DateTime other)`

Checks if the first date is newer (i.e., later) than the second date.

- **Parameters**:
  - `date` (`DateTime`): The date to compare.
  - `other` (`DateTime`): The reference date.
- **Returns**: `true` if `date` is greater than `other`; otherwise, `false`.
- **Throws**: No exceptions.

---

### `public static DateTime RoundToMinute(DateTime date)`

Rounds the given `date` down to the nearest whole minute.

- **Parameters**:
  - `date` (`DateTime`): The date to round.
- **Returns**: A new `DateTime` with seconds and milliseconds set to zero.
- **Throws**: No exceptions.

---

### `public static DateTime RoundToSecond(DateTime date)`

Rounds the given `date` down to the nearest whole second.

- **Parameters**:
  - `date` (`DateTime`): The date to round.
- **Returns**: A new `DateTime` with milliseconds set to zero.
- **Throws**: No exceptions.

---
### `public static string ToIso8601String(DateTime date)`

Formats the given `date` as an ISO 8601-compliant UTC string (e.g., `2025-04-05T14:30:00Z`).

- **Parameters**:
  - `date` (`DateTime`): The date to format.
- **Returns**: An ISO 8601 formatted string in UTC.
- **Throws**: No exceptions.

---
### `public static string ToUserFriendlyString(DateTime date)`

Converts the given `date` into a user-friendly, localized string representation (e.g., "Apr 5, 2025, 2:30 PM").

- **Parameters**:
  - `date` (`DateTime`): The date to format.
- **Returns**: A human-readable string.
- **Throws**: No exceptions.

---
### `public static string ToTimeAgoString(DateTime date)`

Generates a relative time string indicating how long ago the `date` was (e.g., "2 hours ago", "3 days ago").

- **Parameters**:
  - `date` (`DateTime`): The past date to describe.
- **Returns**: A string describing the time elapsed since `date`.
- **Throws**: No exceptions.

---
### `public static bool IsSameDay(DateTime date1, DateTime date2)`

Determines whether two dates fall on the same calendar day, regardless of time.

- **Parameters**:
  - `date1` (`DateTime`): The first date.
  - `date2` (`DateTime`): The second date.
- **Returns**: `true` if both dates have the same year, month, and day; otherwise, `false`.
- **Throws**: No exceptions.

---
### `public static long ToUnixTimestamp(DateTime date)`

Converts the given `date` to a Unix timestamp (seconds since 1970-01-01T00:00:00Z).

- **Parameters**:
  - `date` (`DateTime`): The date to convert.
- **Returns**: The Unix timestamp as a `long`.
- **Throws**: No exceptions.

---
### `public static DateTime FromUnixTimestamp(long timestamp)`

Converts a Unix timestamp (seconds since 1970-01-01T00:00:00Z) back to a `DateTime`.

- **Parameters**:
  - `timestamp` (`long`): The Unix timestamp to convert.
- **Returns**: A `DateTime` representing the timestamp in UTC.
- **Throws**: No exceptions.

---
### `public static bool IsStale(DateTime date, int maxAgeInDays)`

Checks if the given `date` is older than `maxAgeInDays` days from the current UTC time.

- **Parameters**:
  - `date` (`DateTime`): The date to check.
  - `maxAgeInDays` (`int`): The maximum allowed age in days.
- **Returns**: `true` if `date` is older than `maxAgeInDays`; otherwise, `false`.
- **Throws**: `ArgumentOutOfRangeException` if `maxAgeInDays` is negative.

---
### `public static DateTime GetStartOfDay(DateTime date)`

Returns a `DateTime` representing the start of the day (00:00:00) for the given `date`.

- **Parameters**:
  - `date` (`DateTime`): The input date.
- **Returns**: A new `DateTime` with time set to 00:00:00.
- **Throws**: No exceptions.

---
### `public static DateTime GetEndOfDay(DateTime date)`

Returns a `DateTime` representing the end of the day (23:59:59.999) for the given `date`.

- **Parameters**:
  - `date` (`DateTime`): The input date.
- **Returns**: A new `DateTime` with time set to 23:59:59.999.
- **Throws**: No exceptions.

## Usage
