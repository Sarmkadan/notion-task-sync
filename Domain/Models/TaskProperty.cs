#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace NotionTaskSync.Domain.Models;

using System;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

/// <summary>
/// Represents a custom property or extended attribute for a task.
/// Supports flexible schema for tasks with additional metadata.
/// </summary>
public class TaskProperty
{
    /// <summary>
    /// Gets or sets the unique identifier of this property.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the task this property belongs to.
    /// </summary>
    [Required]
    public required Guid TaskId { get; set; }

    /// <summary>
    /// Gets or sets the name of the property. Limited to 100 characters.
    /// </summary>
    [Required]
    [StringLength(100)]
    public required string PropertyName { get; set; }

    /// <summary>
    /// Gets or sets the raw string value of the property. Limited to 2000 characters; may be <c>null</c>.
    /// </summary>
    [StringLength(2000)]
    public string? PropertyValue { get; set; }

    /// <summary>
    /// Gets or sets the data type that <see cref="PropertyValue"/> is expected to conform to.
    /// </summary>
    public PropertyDataType DataType { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether a non-empty <see cref="PropertyValue"/> is required.
    /// </summary>
    public bool IsRequired { get; set; }

    /// <summary>
    /// Gets or sets the UTC timestamp when the property was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the UTC timestamp when the property was last updated.
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets a value indicating whether this property is pushed to Notion during sync.
    /// </summary>
    public bool SyncToNotion { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether this property is written to local task files during sync.
    /// </summary>
    public bool SyncToLocal { get; set; } = true;

    /// <summary>
    /// Initializes a new TaskProperty with name, value, and data type.
    /// </summary>
    [SetsRequiredMembers]
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
