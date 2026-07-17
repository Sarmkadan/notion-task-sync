# CliArgumentParserValidation

Provides static validation utilities for command-line argument parsing within the `notion-task-sync` application. This class centralizes checks for required options, option validity, and range/date constraints, returning structured results or throwing on invalid input.

## API

### Validate

```csharp
public static IReadOnlyList<string> Validate(string[] args, CliOptionSet optionSet)
public static IReadOnlyList<string> Validate(string[] args, CliOptionSet optionSet, CliValidationMode mode)
```

Validates the given arguments against the specified option set and returns a list of error messages. The overload without `mode` uses a default validation mode. Returns an empty list when validation passes.

### IsValid

```csharp
public static bool IsValid(string[] args, CliOptionSet optionSet)
public static bool IsValid(string[] args, CliOptionSet optionSet, CliValidationMode mode)
```

Returns `true` if the arguments satisfy all constraints defined by the option set; otherwise `false`. The overload without `mode` uses a default validation mode. Does not throw.

### EnsureValid

```csharp
public static void EnsureValid(string[] args, CliOptionSet optionSet)
public static void EnsureValid(string[] args, CliOptionSet optionSet, CliValidationMode mode)
```

Performs the same validation as `Validate` but throws an `ArgumentException` (or a derived exception) if any errors are found. The overload without `mode` uses a default validation mode.

### HasRequiredOptions

```csharp
public static bool HasRequiredOptions(string[] args, CliOptionSet optionSet)
```

Returns `true` only if **all** required options defined in the option set are present in the arguments. Returns `false` if any single required option is missing.

### HasAnyRequiredOption

```csharp
public static bool HasAnyRequiredOption(string[] args, CliOptionSet optionSet)
```

Returns `true` if **at least one** required option from the option set is present in the arguments. Useful for scenarios where any of a set of alternatives satisfies the requirement.

### IsValidOption\<T\>

```csharp
public static bool IsValidOption<T>(string value)
```

Checks whether the string `value` can be successfully parsed or converted to type `T`. Returns `true` if the conversion is possible; otherwise `false`. The type parameter `T` is constrained to types the parser supports (e.g., `int`, `string`, `bool`).

### IsValidRangeOption

```csharp
public static bool IsValidRangeOption(string value, int min, int max)
```

Returns `true` if `value` represents an integer within the inclusive range `[min, max]`. Returns `false` if the value cannot be parsed as an integer or falls outside the bounds.

### IsValidDateOption

```csharp
public static bool IsValidDateOption(string value, string format)
```

Returns `true` if `value` can be parsed as a date according to the specified `format` string. Returns `false` if parsing fails or the format is unrecognized.

### IsValidDateTimeOption

```csharp
public static bool IsValidDateTimeOption(string value, string format)
```

Returns `true` if `value` can be parsed as a date and time according to the specified `format` string. Returns `false` if parsing fails or the format is unrecognized.

## Usage

### Example 1: Full validation with error collection

```csharp
var optionSet = new CliOptionSet
{
    RequiredOptions = { "source", "destination" },
    RangedOptions = { ("priority", 1, 5) }
};

string[] args = { "--source", "db1", "--priority", "8" };

IReadOnlyList<string> errors = CliArgumentParserValidation.Validate(args, optionSet);

if (errors.Count > 0)
{
    foreach (var error in errors)
        Console.WriteLine($"Validation error: {error}");
    return;
}

// Proceed with parsed arguments
```

### Example 2: Guard clause with EnsureValid

```csharp
var optionSet = new CliOptionSet
{
    RequiredOptions = { "token" },
    DateOptions = { ("since", "yyyy-MM-dd") }
};

string[] args = { "--token", "abc123", "--since", "2024-01-15" };

try
{
    CliArgumentParserValidation.EnsureValid(args, optionSet, CliValidationMode.Strict);
}
catch (ArgumentException ex)
{
    Console.WriteLine($"Argument error: {ex.Message}");
    Environment.Exit(1);
}

// Arguments are guaranteed valid at this point
```

## Notes

- All methods are static and stateless; they are safe to call concurrently from multiple threads without external synchronization.
- `Validate` and `EnsureValid` overloads without an explicit `CliValidationMode` parameter use a default mode (likely `Standard` or equivalent). Behavior differences between modes are defined by the `CliValidationMode` enumeration.
- `HasRequiredOptions` demands **all** required options, while `HasAnyRequiredOption` is satisfied by **any** single match. Choose based on whether the requirement is conjunctive or disjunctive.
- `IsValidOption<T>` relies on the parser's type support. Unsupported type parameters will likely return `false` for all inputs rather than throwing.
- `IsValidRangeOption` treats non-integer strings as out-of-range and returns `false`; it does not throw a format exception.
- `IsValidDateOption` and `IsValidDateTimeOption` are format-sensitive. An empty or null format string will cause parsing failure and return `false`.
- `EnsureValid` throws on the first encountered error; subsequent errors are not reported in the exception. Use `Validate` when you need the complete error list.
