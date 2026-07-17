#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace NotionTaskSync.Cli;

using System;
using System.Collections.Generic;
using System.Globalization;

/// <summary>
/// Provides validation helpers for <see cref="CliArgumentParser"/> instances.
/// Validates parsed command structures, options, arguments, and error states.
/// </summary>
public static class CliArgumentParserValidation
{
    /// <summary>
    /// Validates a parsed command and returns a list of human-readable problems.
    /// </summary>
    /// <param name="value">The parsed command to validate.</param>
    /// <returns>An enumerable of validation problems; empty if valid.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null.</exception>
    public static IReadOnlyList<string> Validate(this CliArgumentParser value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var problems = new List<string>();

        // Validate the parsed command structure itself
        if (value.Parse([]) is { } parsedCommand)
        {
            problems.AddRange(parsedCommand.Validate());
        }
        else
        {
            problems.Add("Parse operation returned null, indicating a critical parsing failure.");
        }

        return problems.AsReadOnly();
    }

    /// <summary>
    /// Validates a parsed command and returns a list of human-readable problems.
    /// </summary>
    /// <param name="value">The parsed command to validate.</param>
    /// <returns>An enumerable of validation problems; empty if valid.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null.</exception>
    public static IReadOnlyList<string> Validate(this ParsedCommand value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var problems = new List<string>();

        // Validate CommandName
        if (string.IsNullOrWhiteSpace(value.CommandName))
        {
            problems.Add("CommandName cannot be null or whitespace.");
        }
        else if (value.CommandName == "help" && !string.IsNullOrEmpty(value.Error))
        {
            problems.Add($"Help command should not have an error: {value.Error}");
        }

        // Validate Options
        if (value.Options is null)
        {
            problems.Add("Options dictionary cannot be null.");
        }
        else
        {
            foreach (var kvp in value.Options)
            {
                if (string.IsNullOrWhiteSpace(kvp.Key))
                {
                    problems.Add("Option key cannot be null or whitespace.");
                    break;
                }

                if (kvp.Value is null)
                {
                    problems.Add($"Option '{kvp.Key}' has null value.");
                }
            }
        }

        // Validate Arguments
        if (value.Arguments is null)
        {
            problems.Add("Arguments list cannot be null.");
        }
        else
        {
            for (int i = 0; i < value.Arguments.Count; i++)
            {
                if (string.IsNullOrWhiteSpace(value.Arguments[i]))
                {
                    problems.Add($"Argument at index {i} cannot be null or whitespace.");
                }
            }
        }

        // Validate Error state consistency
        if (!string.IsNullOrEmpty(value.Error) && value.IsValid)
        {
            problems.Add("Error property is set but IsValid returns true. These states should be mutually exclusive.");
        }

        if (string.IsNullOrEmpty(value.Error) && !value.IsValid)
        {
            problems.Add("IsValid returns false but Error property is null or empty. Error should be set when invalid.");
        }

        return problems.AsReadOnly();
    }

    /// <summary>
    /// Determines whether the parsed command is valid.
    /// </summary>
    /// <param name="value">The parsed command to check.</param>
    /// <returns>True if the command is valid; otherwise, false.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null.</exception>
    public static bool IsValid(this CliArgumentParser value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var parsed = value.Parse([]);
        return parsed?.IsValid ?? false;
    }

    /// <summary>
    /// Determines whether the parsed command is valid.
    /// </summary>
    /// <param name="value">The parsed command to check.</param>
    /// <returns>True if the command is valid; otherwise, false.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null.</exception>
    public static bool IsValid(this ParsedCommand value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return string.IsNullOrEmpty(value.Error);
    }

    /// <summary>
    /// Ensures that the parsed command is valid, throwing an exception if not.
    /// </summary>
    /// <param name="value">The parsed command to validate.</param>
    /// <exception cref="ArgumentException">Thrown if the command is invalid, containing all validation problems.</exception>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null.</exception>
    public static void EnsureValid(this CliArgumentParser value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var problems = value.Validate();
        if (problems.Count > 0)
        {
            throw new ArgumentException(
                $"Parsed command is invalid. Problems: {string.Join("; ", problems)}",
                nameof(value));
        }
    }

    /// <summary>
    /// Ensures that the parsed command is valid, throwing an exception if not.
    /// </summary>
    /// <param name="value">The parsed command to validate.</param>
    /// <exception cref="ArgumentException">Thrown if the command is invalid, containing all validation problems.</exception>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null.</exception>
    public static void EnsureValid(this ParsedCommand value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var problems = value.Validate();
        if (problems.Count > 0)
        {
            throw new ArgumentException(
                $"Parsed command is invalid. Problems: {string.Join("; ", problems)}",
                nameof(value));
        }
    }

    /// <summary>
    /// Validates that required options are present and non-empty.
    /// </summary>
    /// <param name="options">The options dictionary to validate.</param>
    /// <param name="requiredOptionNames">Names of required options.</param>
    /// <returns>True if all required options are present and non-empty; otherwise, false.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="options"/> or <paramref name="requiredOptionNames"/> is null.</exception>
    public static bool HasRequiredOptions(
        this IReadOnlyDictionary<string, string> options,
        params string[] requiredOptionNames)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(requiredOptionNames);

        foreach (var optionName in requiredOptionNames)
        {
            if (string.IsNullOrWhiteSpace(optionName))
            {
                throw new ArgumentException("Required option name cannot be null or whitespace.", nameof(requiredOptionNames));
            }

            if (!options.TryGetValue(optionName, out var value) || string.IsNullOrWhiteSpace(value))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Validates that at least one of the required options is present and non-empty.
    /// </summary>
    /// <param name="options">The options dictionary to validate.</param>
    /// <param name="requiredOptionNames">Names of required options (at least one must be present).</param>
    /// <returns>True if at least one required option is present and non-empty; otherwise, false.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="options"/> or <paramref name="requiredOptionNames"/> is null.</exception>
    public static bool HasAnyRequiredOption(
        this IReadOnlyDictionary<string, string> options,
        params string[] requiredOptionNames)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(requiredOptionNames);

        if (requiredOptionNames.Length == 0)
        {
            return true;
        }

        foreach (var optionName in requiredOptionNames)
        {
            if (string.IsNullOrWhiteSpace(optionName))
            {
                throw new ArgumentException("Required option name cannot be null or whitespace.", nameof(requiredOptionNames));
            }

            if (options.TryGetValue(optionName, out var value) && !string.IsNullOrWhiteSpace(value))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Validates that option values can be parsed as specific types.
    /// </summary>
    /// <param name="options">The options dictionary to validate.</param>
    /// <param name="optionName">Name of the option to validate.</param>
    /// <param name="parser">Function to parse the option value.</param>
    /// <param name="validationMessage">Custom validation message if parsing fails.</param>
    /// <typeparam name="T">The target type to parse to.</typeparam>
    /// <returns>True if the option exists and can be parsed; otherwise, false.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="options"/> or <paramref name="parser"/> is null.</exception>
    public static bool IsValidOption<T>(
        this IReadOnlyDictionary<string, string> options,
        string optionName,
        Func<string, T> parser,
        string? validationMessage = null)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentException.ThrowIfNullOrEmpty(optionName);
        ArgumentNullException.ThrowIfNull(parser);

        if (!options.TryGetValue(optionName, out var value) || string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        try
        {
            parser(value);
            return true;
        }
        catch (Exception ex) when (ex is FormatException or OverflowException)
        {
            return false;
        }
    }

    /// <summary>
    /// Validates that option values fall within a specific range.
    /// </summary>
    /// <param name="options">The options dictionary to validate.</param>
    /// <param name="optionName">Name of the option to validate.</param>
    /// <param name="minValue">Minimum allowed value (inclusive).</param>
    /// <param name="maxValue">Maximum allowed value (inclusive).</param>
    /// <param name="validationMessage">Custom validation message if validation fails.</param>
    /// <returns>True if the option exists, can be parsed, and is within range; otherwise, false.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="options"/> is null.</exception>
    public static bool IsValidRangeOption(
        this IReadOnlyDictionary<string, string> options,
        string optionName,
        int minValue,
        int maxValue,
        string? validationMessage = null)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentException.ThrowIfNullOrEmpty(optionName);

        if (!options.TryGetValue(optionName, out var value) || string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var intValue))
        {
            return false;
        }

        return intValue >= minValue && intValue <= maxValue;
    }

    /// <summary>
    /// Validates that option values are valid dates (not default/min date).
    /// </summary>
    /// <param name="options">The options dictionary to validate.</param>
    /// <param name="optionName">Name of the option to validate.</param>
    /// <param name="validationMessage">Custom validation message if validation fails.</param>
    /// <returns>True if the option exists and contains a valid date; otherwise, false.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="options"/> is null.</exception>
    public static bool IsValidDateOption(
        this IReadOnlyDictionary<string, string> options,
        string optionName,
        string? validationMessage = null)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentException.ThrowIfNullOrEmpty(optionName);

        if (!options.TryGetValue(optionName, out var value) || string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        // Try parsing as ISO 8601 date format (yyyy-MM-dd)
        return DateOnly.TryParseExact(
            value,
            ["yyyy-MM-dd", "yyyyMMdd"],
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out _);
    }

    /// <summary>
    /// Validates that option values are valid date-time values (not default/min date-time).
    /// </summary>
    /// <param name="options">The options dictionary to validate.</param>
    /// <param name="optionName">Name of the option to validate.</param>
    /// <param name="validationMessage">Custom validation message if validation fails.</param>
    /// <returns>True if the option exists and contains a valid date-time; otherwise, false.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="options"/> is null.</exception>
    public static bool IsValidDateTimeOption(
        this IReadOnlyDictionary<string, string> options,
        string optionName,
        string? validationMessage = null)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentException.ThrowIfNullOrEmpty(optionName);

        if (!options.TryGetValue(optionName, out var value) || string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        // Try parsing as ISO 8601 date-time format
        return DateTime.TryParseExact(
            value,
            ["yyyy-MM-ddTHH:mm:ss", "yyyy-MM-dd HH:mm:ss", "o"],
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out _);
    }
}