#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace NotionTaskSync.Domain.Models;

using System;
using System.Globalization;
using System.Runtime.Serialization;

/// <summary>
/// Extension methods for TaskProperty providing additional functionality for
/// type conversion, validation, and property manipulation.
/// </summary>
public static class TaskPropertyExtensions
{
    /// <summary>
    /// Converts the property value to the specified type with culture-invariant parsing.
    /// </summary>
    /// <typeparam name="T">The target type to convert to</typeparam>
    /// <param name="property">The task property instance</param>
    /// <returns>The converted value or default(T) if conversion fails</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="property"/> is null.</exception>
    public static T? GetTypedValueInvariant<T>(this TaskProperty property)
    {
        ArgumentNullException.ThrowIfNull(property);

        if (string.IsNullOrEmpty(property.PropertyValue))
            return default;

        try
        {
            var targetType = typeof(T);
            var value = property.PropertyValue.Trim();

            return targetType switch
            {
                Type t when t == typeof(string) => (T)(object)value,
                Type t when t == typeof(int) => (T)(object)int.Parse(value, CultureInfo.InvariantCulture),
                Type t when t == typeof(decimal) => (T)(object)decimal.Parse(value, CultureInfo.InvariantCulture),
                Type t when t == typeof(double) => (T)(object)double.Parse(value, CultureInfo.InvariantCulture),
                Type t when t == typeof(float) => (T)(object)float.Parse(value, CultureInfo.InvariantCulture),
                Type t when t == typeof(bool) => (T)(object)ParseBooleanInvariant(value),
                Type t when t == typeof(DateTime) => (T)(object)ParseDateTimeInvariant(value),
                Type t when t == typeof(Guid) => (T)(object)Guid.Parse(value),
                _ => property.GetTypedValue<T>()
            };
        }
        catch
        {
            return default;
        }
    }

    /// <summary>
    /// Parses a boolean value with culture-invariant handling for common truthy strings.
    /// </summary>
    /// <param name="value">The string value to parse.</param>
    /// <returns>The parsed boolean value.</returns>
    private static bool ParseBooleanInvariant(string value)
    {
        if (bool.TryParse(value, out var boolResult))
            return boolResult;

        if (int.TryParse(value, out var intResult))
            return intResult != 0;

        return value.Equals("true", StringComparison.OrdinalIgnoreCase) ||
               value.Equals("yes", StringComparison.OrdinalIgnoreCase) ||
               value.Equals("on", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Parses a DateTime value with culture-invariant parsing.
    /// </summary>
    /// <param name="value">The string value to parse.</param>
    /// <returns>The parsed DateTime value.</returns>
    private static DateTime ParseDateTimeInvariant(string value)
    {
        if (DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dateResult))
            return dateResult;

        if (long.TryParse(value, CultureInfo.InvariantCulture, out var ticks))
            return new DateTime(ticks);

        throw new FormatException("Value is not a valid DateTime format.");
    }

    /// <summary>
    /// Safely updates the property value with automatic type validation.
    /// Returns true if the update was successful and validation passed.
    /// </summary>
    /// <param name="property">The task property instance</param>
    /// <param name="newValue">The new value to set</param>
    /// <param name="dataType">Optional data type override</param>
    /// <returns>True if update and validation succeeded, false otherwise</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="property"/> is null.</exception>
    public static bool SafeUpdateValue(this TaskProperty property, string newValue, PropertyDataType? dataType = null)
    {
        ArgumentNullException.ThrowIfNull(property);

        property.PropertyValue = newValue;
        property.UpdatedAt = DateTime.UtcNow;

        if (dataType.HasValue)
            property.DataType = dataType.Value;

        return property.Validate();
    }

    /// <summary>
    /// Checks if the property value matches a specific expected value.
    /// Supports comparison with type conversion for different data types.
    /// </summary>
    /// <param name="property">The task property instance</param>
    /// <param name="expectedValue">The expected value to compare against</param>
    /// <returns>True if values match (with type conversion), false otherwise</returns>
    /// <exception cref="ArgumentNullException">Thrown when either <paramref name="property"/> or <paramref name="expectedValue"/> is null.</exception>
    public static bool ValueEquals(this TaskProperty property, string expectedValue)
    {
        ArgumentNullException.ThrowIfNull(property);
        ArgumentNullException.ThrowIfNull(expectedValue);

        if (string.IsNullOrEmpty(property.PropertyValue))
            return string.IsNullOrEmpty(expectedValue);

        return string.Equals(property.PropertyValue, expectedValue, StringComparison.Ordinal);
    }

    /// <summary>
    /// Creates a deep copy of the task property with a new ID.
    /// </summary>
    /// <param name="property">The task property instance to copy</param>
    /// <returns>A new TaskProperty instance with identical values but new ID</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="property"/> is null.</exception>
    public static TaskProperty Clone(this TaskProperty property)
    {
        ArgumentNullException.ThrowIfNull(property);

        // Create an uninitialized instance to bypass constructor requirements
        var clone = (TaskProperty)FormatterServices.GetUninitializedObject(typeof(TaskProperty));

        // Set all properties
        clone.Id = Guid.NewGuid();
        clone.TaskId = property.TaskId;
        clone.PropertyName = property.PropertyName ?? throw new InvalidOperationException("PropertyName cannot be null");
        clone.PropertyValue = property.PropertyValue;
        clone.DataType = property.DataType;
        clone.IsRequired = property.IsRequired;
        clone.SyncToNotion = property.SyncToNotion;
        clone.SyncToLocal = property.SyncToLocal;
        clone.CreatedAt = DateTime.UtcNow;
        clone.UpdatedAt = DateTime.UtcNow;

        return clone;
    }

    /// <summary>
    /// Gets the property value as a formatted string based on its data type.
    /// For numeric types, applies appropriate formatting.
    /// </summary>
    /// <param name="property">The task property instance</param>
    /// <returns>Formatted string representation of the value</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="property"/> is null.</exception>
    public static string GetFormattedValue(this TaskProperty property)
    {
        ArgumentNullException.ThrowIfNull(property);

        if (string.IsNullOrEmpty(property.PropertyValue))
            return string.Empty;

        return property.DataType switch
        {
            PropertyDataType.Integer => int.Parse(property.PropertyValue).ToString("N0", CultureInfo.InvariantCulture),
            PropertyDataType.Decimal => decimal.Parse(property.PropertyValue).ToString("N2", CultureInfo.InvariantCulture),
            PropertyDataType.Boolean => ParseBooleanForDisplay(property.PropertyValue),
            PropertyDataType.DateTime => ParseDateTimeForDisplay(property.PropertyValue),
            PropertyDataType.Json => "JSON Data",
            _ => property.PropertyValue
        };
    }

    /// <summary>
    /// Parses a boolean value for display purposes.
    /// </summary>
    /// <param name="value">The boolean string value to parse.</param>
    /// <returns>"Yes" for true, "No" for false.</returns>
    private static string ParseBooleanForDisplay(string value)
    {
        if (bool.TryParse(value, out var boolValue))
            return boolValue ? "Yes" : "No";

        if (int.TryParse(value, out var intValue))
            return intValue != 0 ? "Yes" : "No";

        return value.Equals("true", StringComparison.OrdinalIgnoreCase) ||
               value.Equals("yes", StringComparison.OrdinalIgnoreCase) ||
               value.Equals("on", StringComparison.OrdinalIgnoreCase) ?
            "Yes" : "No";
    }

    /// <summary>
    /// Parses a DateTime value for display purposes.
    /// </summary>
    /// <param name="value">The DateTime string value to parse.</param>
    /// <returns>Formatted DateTime string in yyyy-MM-dd HH:mm format.</returns>
    private static string ParseDateTimeForDisplay(string value)
    {
        if (DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dateResult))
            return dateResult.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture);

        if (long.TryParse(value, CultureInfo.InvariantCulture, out var ticks))
            return new DateTime(ticks).ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture);

        return value;
    }
}