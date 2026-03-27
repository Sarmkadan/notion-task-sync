// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace NotionTaskSync.Domain.Models;

using System;
using System.ComponentModel.DataAnnotations;

/// <summary>
/// Represents a custom property or extended attribute for a task.
/// Supports flexible schema for tasks with additional metadata.
/// </summary>
public class TaskProperty
{
    public Guid Id { get; set; }

    [Required]
    public required Guid TaskId { get; set; }

    [Required]
    [StringLength(100)]
    public required string PropertyName { get; set; }

    [StringLength(2000)]
    public string? PropertyValue { get; set; }

    public PropertyDataType DataType { get; set; }

    public bool IsRequired { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public bool SyncToNotion { get; set; } = true;

    public bool SyncToLocal { get; set; } = true;

    /// <summary>
    /// Initializes a new TaskProperty with name, value, and data type.
    /// </summary>
    public TaskProperty(string propertyName, string? propertyValue, PropertyDataType dataType)
    {
        PropertyName = propertyName;
        PropertyValue = propertyValue;
        DataType = dataType;
    }

    /// <summary>
    /// Validates the task property ensuring name and data type consistency.
    /// </summary>
    public bool Validate()
    {
        if (TaskId == Guid.Empty)
            return false;

        if (string.IsNullOrWhiteSpace(PropertyName) || PropertyName.Length > 100)
            return false;

        if (DataType == PropertyDataType.Unknown)
            return false;

        return ValidateDataType();
    }

    /// <summary>
    /// Validates that the property value conforms to its declared data type.
    /// </summary>
    private bool ValidateDataType()
    {
        if (string.IsNullOrEmpty(PropertyValue))
            return !IsRequired;

        return DataType switch
        {
            PropertyDataType.String => true,
            PropertyDataType.Integer => int.TryParse(PropertyValue, out _),
            PropertyDataType.Decimal => decimal.TryParse(PropertyValue, out _),
            PropertyDataType.Boolean => bool.TryParse(PropertyValue, out _),
            PropertyDataType.DateTime => DateTime.TryParse(PropertyValue, out _),
            PropertyDataType.Json => IsValidJson(PropertyValue),
            _ => false
        };
    }

    /// <summary>
    /// Simple JSON validation by attempting to parse the value.
    /// </summary>
    private static bool IsValidJson(string value)
    {
        try
        {
            System.Text.Json.JsonDocument.Parse(value);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Gets the property value as a typed instance, with null return if parsing fails.
    /// </summary>
    public T? GetTypedValue<T>()
    {
        if (string.IsNullOrEmpty(PropertyValue))
            return default;

        try
        {
            return (T?)Convert.ChangeType(PropertyValue, typeof(T));
        }
        catch
        {
            return default;
        }
    }

    /// <summary>
    /// Updates the property value and refreshes the modification timestamp.
    /// </summary>
    public bool UpdateValue(string newValue)
    {
        PropertyValue = newValue;
        UpdatedAt = DateTime.UtcNow;
        return Validate();
    }

    /// <summary>
    /// Returns a string representation of the property for logging.
    /// </summary>
    public override string ToString()
    {
        return $"{PropertyName}={PropertyValue} ({DataType})";
    }
}

public enum PropertyDataType
{
    Unknown = 0,
    String = 1,
    Integer = 2,
    Decimal = 3,
    Boolean = 4,
    DateTime = 5,
    Json = 6
}
