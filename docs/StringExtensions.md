# StringExtensions

Provides a collection of static utility methods for common string operations, validation, and transformations used throughout the `notion-task-sync` project. These extensions centralize null-safe string handling, formatting conventions, and content checks to reduce boilerplate across the codebase.

## API

### IsNullOrEmpty

```csharp
public static bool IsNullOrEmpty(this string value)
```

Determines whether a string is `null` or has a length of zero. Returns `true` if the string is `null` or `""`; otherwise `false`. This method never throws.

### HasContent

```csharp
public static bool HasContent(this string value)
```

Indicates whether a string contains non-whitespace characters. Returns `true` if the string is not `null`, not empty, and contains at least one character that is not classified as white space; otherwise `false`. This method never throws.

### Truncate

```csharp
public static string Truncate(this string value, int maxLength)
```

Returns a substring of the input string limited to the specified `maxLength`. If the string is `null` or its length is less than or equal to `maxLength`, the original string is returned unchanged. When truncation occurs, the result is the first `maxLength` characters. Throws `ArgumentOutOfRangeException` if `maxLength` is negative.

### SanitizeForFilename

```csharp
public static string SanitizeForFilename(this string value)
```

Replaces or removes characters that are invalid in file names on common operating systems. Invalid characters are typically replaced with a safe alternative or stripped. Returns the sanitized string. Passing `null` returns an empty string. This method never throws.

### IsValidEmail

```csharp
public static bool IsValidEmail(this string value)
```

Evaluates whether the string conforms to a basic email address format. Returns `true` if the string is not `null` and matches a pattern requiring a local part, an `@` symbol, and a domain part; otherwise `false`. This method never throws.

### IsValidGuid

```csharp
public static bool IsValidGuid(this string value)
```

Checks whether the string can be parsed as a `Guid`. Returns `true` if the string is not `null` and represents a valid GUID in any of the standard formats recognized by `Guid.TryParse`; otherwise `false`. This method never throws.

### ToPascalCase

```csharp
public static string ToPascalCase(this string value)
```

Converts a string to PascalCase, where the first letter of each word is capitalized and all other letters are lowercased. Word boundaries are inferred from spaces, hyphens, underscores, or existing casing transitions. Returns the transformed string. Passing `null` returns `null`. This method never throws.

### ToSnakeCase

```csharp
public static string ToSnakeCase(this string value)
```

Converts a string to snake_case, where words are separated by underscores and all letters are lowercased. Word boundaries follow the same rules as `ToPascalCase`. Returns the transformed string. Passing `null` returns `null`. This method never throws.

### AfterLast

```csharp
public static string AfterLast(this string value, char delimiter)
```

Extracts the substring that occurs after the final occurrence of the specified `delimiter`. If the delimiter is not found, the original string is returned. Passing `null` returns `null`. This method never throws.

### BeforeLast

```csharp
public static string BeforeLast(this string value, char delimiter)
```

Extracts the substring that occurs before the final occurrence of the specified `delimiter`. If the delimiter is not found, the original string is returned. Passing `null` returns `null`. This method never throws.

### ContainsIgnoreCase

```csharp
public static bool ContainsIgnoreCase(this string value, string substring)
```

Determines whether the string contains the specified `substring` using ordinal case-insensitive comparison. Returns `true` if `substring` is found within the string; otherwise `false`. Passing `null` for either parameter returns `false`. This method never throws.

### NormalizeLineEndings

```csharp
public static string NormalizeLineEndings(this string value)
```

Replaces all line-ending variations (`\r\n`, `\r`) with the Unix-style `\n` character. Returns the normalized string. Passing `null` returns `null`. This method never throws.

### ToSlug

```csharp
public static string ToSlug(this string value)
```

Generates a URL-friendly slug from the input string by lowercasing, replacing non-alphanumeric characters with hyphens, collapsing consecutive hyphens, and trimming leading/trailing hyphens. Returns the slugified string. Passing `null` returns an empty string. This method never throws.

## Usage

### Example 1: Processing a task title into a safe filename and slug

```csharp
string rawTitle = "  Review Q4 Report (Final Draft)!!!  ";

if (rawTitle.HasContent())
{
    string safeFileName = rawTitle
        .Trim()
        .SanitizeForFilename()
        .Truncate(120);

    string slug = rawTitle.ToSlug();

    Console.WriteLine($"Filename: {safeFileName}.md");
    Console.WriteLine($"Slug: {slug}");
}
// Output:
// Filename: Review Q4 Report (Final Draft).md
// Slug: review-q4-report-final-draft
```

### Example 2: Validating and normalizing user input before persistence

```csharp
string email = "User@Example.com\r\n";
string guidCandidate = "550e8400-e29b-41d4-a716-446655440000";

string normalizedEmail = email.NormalizeLineEndings().Trim();

if (normalizedEmail.IsValidEmail())
{
    Console.WriteLine($"Valid email: {normalizedEmail}");
}

if (guidCandidate.IsValidGuid())
{
    Guid parsed = Guid.Parse(guidCandidate);
    Console.WriteLine($"Parsed GUID: {parsed}");
}
// Output:
// Valid email: User@Example.com
// Parsed GUID: 550e8400-e29b-41d4-a716-446655440000
```

## Notes

- All methods are static extension methods and are thread-safe; they operate on immutable string instances without shared state.
- Methods that accept `null` input handle it gracefully as documented per method—either returning `null`, an empty string, or `false`—without throwing `NullReferenceException`.
- `IsValidEmail` performs a lightweight pattern check suitable for basic validation; it does not verify domain existence or perform SMTP-level validation.
- `IsValidGuid` delegates to `Guid.TryParse` and accepts dashes, braces, parentheses, and hexadecimal formats.
- `Truncate` throws only when `maxLength` is negative; a `maxLength` of zero returns an empty string.
- `SanitizeForFilename` and `ToSlug` are culture-invariant and target ASCII-compatible output; characters outside this range may be removed or replaced with hyphens.
- `NormalizeLineEndings` does not add or remove trailing newlines; it only standardizes existing line separators to `\n`.
- `AfterLast` and `BeforeLast` operate on a single `char` delimiter; for multi-character delimiters, use alternative string splitting approaches.
