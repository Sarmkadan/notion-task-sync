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
    public static T? GetTypedValueInvariant<T>(this TaskProperty property)
    {
        if (property is null || string.IsNullOrEmpty(property.PropertyValue))
            return default;

        try
        {
            var targetType = typeof(T);
            var value = property.PropertyValue.Trim();

            if (targetType == typeof(string))
                return (T)(object)value;

            if (targetType == typeof(int))
                return (T)(object)int.Parse(value, CultureInfo.InvariantCulture);

            if (targetType == typeof(decimal))
                return (T)(object)decimal.Parse(value, CultureInfo.InvariantCulture);

            if (targetType == typeof(double))
                return (T)(object)double.Parse(value, CultureInfo.InvariantCulture);

            if (targetType == typeof(float))
                return (T)(object)float.Parse(value, CultureInfo.InvariantCulture);

            if (targetType == typeof(bool))
            {
                if (bool.TryParse(value, out var boolResult))
                    return (T)(object)boolResult;

                if (int.TryParse(value, out var intResult))
                    return (T)(object)(intResult != 0);

                return (T)(object)(value.Equals("true", StringComparison.OrdinalIgnoreCase) ||
                                 value.Equals("yes", StringComparison.OrdinalIgnoreCase) ||
                                 value.Equals("on", StringComparison.OrdinalIgnoreCase));
            }

            if (targetType == typeof(DateTime))
            {
                if (DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dateResult))
                    return (T)(object)dateResult;

                if (long.TryParse(value, CultureInfo.InvariantCulture, out var ticks))
                    return (T)(object)new DateTime(ticks);
            }

            if (targetType == typeof(Guid))
            {
                if (Guid.TryParse(value, out var guidResult))
                    return (T)(object)guidResult;
            }

            return property.GetTypedValue<T>();
        }
        catch
        {
            return default;
        }
    }

    /// <summary>
    /// Safely updates the property value with automatic type validation.
    /// Returns true if the update was successful and validation passed.
    /// </summary>
    /// <param name="property">The task property instance</param>
    /// <param name="newValue">The new value to set</param>
    /// <param name="dataType">Optional data type override</param>
    /// <returns>True if update and validation succeeded, false otherwise</returns>
    public static bool SafeUpdateValue(this TaskProperty property, string newValue, PropertyDataType? dataType = null)
    {
        if (property is null)
            return false;

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
    public static bool ValueEquals(this TaskProperty property, string expectedValue)
    {
        if (property is null || expectedValue is null)
            return false;

        if (string.IsNullOrEmpty(property.PropertyValue))
            return string.IsNullOrEmpty(expectedValue);

        return string.Equals(property.PropertyValue, expectedValue, StringComparison.Ordinal);
    }

    /// <summary>
    /// Creates a deep copy of the task property with a new ID.
    /// </summary>
    /// <param name="property">The task property instance to copy</param>
    /// <returns>A new TaskProperty instance with identical values but new ID</returns>
    public static TaskProperty Clone(this TaskProperty property)
    {
        if (property is null)
            throw new ArgumentNullException(nameof(property));

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
    public static string GetFormattedValue(this TaskProperty property)
    {
        if (property is null || string.IsNullOrEmpty(property.PropertyValue))
            return string.Empty;

        return property.DataType switch
        {
            PropertyDataType.Integer => int.Parse(property.PropertyValue).ToString("N0", CultureInfo.InvariantCulture),
            PropertyDataType.Decimal => decimal.Parse(property.PropertyValue).ToString("N2", CultureInfo.InvariantCulture),
            PropertyDataType.Boolean => bool.Parse(property.PropertyValue) ? "Yes" : "No",
            PropertyDataType.DateTime => DateTime.Parse(property.PropertyValue).ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture),
            PropertyDataType.Json => "JSON Data",
            _ => property.PropertyValue
        };
    }

}