# Utils

Overview of the helper classes in `Utils/`. Each section gives a one-paragraph summary; see the per-class docs (`docs/<ClassName>.md`) for method-level details.

## CryptoHelper

`CryptoHelper` is a static class with hashing, HMAC, and token utilities. `HashSha256` and `HashMd5` return Base64-encoded digests of UTF-8 input (empty input yields an empty string), `ComputeHmacSha256` and `VerifyHmacSha256` sign and check data with a key, and `VerifyHashSha256` compares a plaintext against a stored hash. `VerifyHmacSha256` uses a constant-time comparison, while `VerifyHashSha256` uses plain `==`. `GenerateRandomToken` returns a Base64 string built from cryptographically secure random bytes and rejects lengths below 8. `GenerateFingerprint` returns the first 16 characters of a SHA-256 hash, which is useful for detecting content changes without storing the content itself. MD5 is provided for compatibility only and should not be used for security purposes.

## DateTimeExtensions

`DateTimeExtensions` is a set of extension methods on `DateTime` and `long`. They cover recency and staleness checks against the current UTC time (`IsWithinDays`, `IsStale`, `IsInPast`, `IsNewerThan`, `IsSameDay`), rounding to minute or second boundaries (`RoundToMinute`, `RoundToSecond`) to avoid false positives in change detection, and day boundaries (`GetStartOfDay`, `GetEndOfDay`). It also converts values to and from strings and Unix time: `ToIso8601String` uses the `"O"` format, `ToUserFriendlyString` uses `"g"`, and `ToTimeAgoString` produces phrases such as "5 minutes ago". `ToUnixTimestamp` and `FromUnixTimestamp` convert between `DateTime` and Unix seconds. The methods that take a day or hour count throw `ArgumentOutOfRangeException` when the value is negative.

## FileSystemHelper

`FileSystemHelper` is an instance class that takes an `ILogger<FileSystemHelper>` and wraps common file and directory operations so that failures are logged rather than thrown. It provides `EnsureDirectoryExists`, `ReadFileAsync`, `WriteFileAsync`, `AppendFileAsync`, `DeleteFile`, `DeleteDirectory`, and `CopyFile`, and the write and copy methods create missing parent directories first. Read-only queries such as `GetFileSize`, `GetLastModifiedTime`, `IsFile`, and `IsDirectory` return sentinel values (`-1`, `DateTime.MinValue`, `false`) instead of throwing. The static `NormalizePath` resolves a path with `Path.GetFullPath` and converts backslashes to forward slashes for cross-platform comparison. Because `DeleteDirectory` removes contents recursively, callers should check the target before using it.

## RetryHelper

`RetryHelper` is an instance class with an `ILogger<RetryHelper>` that retries failing operations. The async `ExecuteWithRetryAsync` overloads and the synchronous `ExecuteWithRetry` use exponential backoff with jitter, starting at `initialDelayMs` and doubling on each attempt; the overload with a `shouldRetry` predicate retries only the exceptions the predicate accepts. All overloads reject `maxRetries` below 1 and rethrow the last exception once attempts are exhausted. `ExecuteWithCircuitBreakerAsync` returns a `(result, success)` tuple instead of throwing. Its failure counter and last-failure timestamp are local variables, so they reset on every call and the breaker never opens across calls; treat it as a single-call guard rather than a persistent circuit breaker.

## StringExtensions

`StringExtensions` is a static class of extension methods for common string work in the sync pipeline. `Truncate` shortens text with a suffix (default `"..."`), and `SanitizeForFilename` turns a task title into a safe file name by removing invalid characters, replacing spaces with underscores, and collapsing repeated underscores. `ToSlug` produces a lowercase, hyphenated identifier, while `ToPascalCase` and `ToSnakeCase` convert between naming styles. Validation helpers `IsValidEmail` and `IsValidGuid` check formats for assignee addresses and Notion IDs. The remaining methods cover substring splitting (`AfterLast`, `BeforeLast`), case-insensitive comparison (`ContainsIgnoreCase`, `EqualsIgnoreCase`, which accepts nulls), line-ending normalization (`NormalizeLineEndings`), and `CapitalizeFirst`.

## TimeHelper

`TimeHelper` is a static class of time utilities built around `DateTime.UtcNow`. Its methods format and parse timestamps with the application's `AppConstants.LastSyncTimeFormat` (`FormatDateTime`, `ParseDateTime`, the latter returning `null` on invalid input), and it provides relative time helpers such as `GetRelativeTime`, `FormatTimeSpan`, and `HoursSince`. Scheduling helpers `ShouldSync` and `CalculateNextSyncTime` decide when the next sync is due from the last sync time and an interval in seconds. Period helpers `GetTodayStart`, `GetTodayEnd`, `GetWeekStart` (Monday-based), and `GetMonthStart` return boundaries for the current UTC day, week, and month. `DaysBetween`, `AreWithinInterval`, `IsPast`, `IsFuture`, and `IsOverdue` cover the remaining comparisons.

## ValidationHelper

`ValidationHelper` is a static class of validators that return `bool` and never throw for bad input. `IsValidNotionId` accepts 32 hex characters or the 36-character dashed UUID form, and `IsValidEmail` requires a dot in the domain part on top of the standard `MailAddress` check. `IsValidFilePath` and `IsValidDirectoryPath` only confirm that `Path.GetFullPath` succeeds, so they do not check that the path exists. `IsValidUrl` accepts only HTTP and HTTPS. Other checks cover API key length (at least 20 characters), priority range (0 to 100), generic integer ranges (`IsInRange`), string length bounds (`IsLengthValid`), and identifier names (`IsValidIdentifierName`). `SanitizeString` removes control characters and trims the result.

## CollectionExtensions

`CollectionExtensions` is a static class of LINQ-style extension methods for `IEnumerable<T>` and `IList<T>`. Null and emptiness checks are provided by `IsNullOrEmpty` (which treats null as empty) and `HasItems` (which throws on null). `Batch` and `BatchChunks` split a sequence into fixed-size chunks, which suits API rate limits, and `SplitWhere` starts a new group at each matching element. `Partition` splits a sequence into matching and non-matching lists in one pass, `GroupByFrequency` orders groups by size, and `Flatten` merges nested sequences. `DistinctBy` and `IntersectBy` compare elements by a key selector, and `SafeToDictionary` keeps the last value for duplicate keys. `SafeGetAt` returns a default value for out-of-range indexes, `WhereNotNull` removes nulls, `AddIf` adds conditionally, and `Shuffle` performs an in-place Fisher-Yates shuffle. `ForEach` is an eager iteration helper, so it runs immediately rather than as part of a deferred query.
