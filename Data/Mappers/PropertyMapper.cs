#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace NotionTaskSync.Data.Mappers;

using NotionTaskSync.Domain.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

/// <summary>
/// Maps between Notion property structures and local <see cref="TaskProperty"/> entities.
/// Handles type coercion, null propagation, and bidirectional property transformation.
/// </summary>
public sealed class PropertyMapper
{
    private static readonly Dictionary<string, PropertyDataType> NotionTypeMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["title"] = PropertyDataType.String,
        ["rich_text"] = PropertyDataType.String,
        ["number"] = PropertyDataType.Decimal,
        ["select"] = PropertyDataType.String,
        ["multi_select"] = PropertyDataType.String,
        ["date"] = PropertyDataType.DateTime,
        ["checkbox"] = PropertyDataType.Boolean,
        ["url"] = PropertyDataType.String,
        ["email"] = PropertyDataType.String,
        ["phone_number"] = PropertyDataType.String,
        ["status"] = PropertyDataType.String,
    };

    /// <summary>
    /// Converts a Notion property dictionary entry into a <see cref="TaskProperty"/>.
    /// </summary>
    /// <param name="taskId">The owning task identifier.</param>
    /// <param name="propertyName">Notion property name.</param>
    /// <param name="notionValue">Raw value object from the Notion API response.</param>
    /// <returns>A populated <see cref="TaskProperty"/>, or <see langword="null"/> when the value cannot be mapped.</returns>
    public TaskProperty? MapFromNotion(Guid taskId, string propertyName, object? notionValue)
    {
        if (string.IsNullOrWhiteSpace(propertyName))
            return null;

        var (value, dataType) = ExtractValueAndType(notionValue);

        return new TaskProperty(propertyName, value, dataType)
        {
            Id = Guid.NewGuid(),
            TaskId = taskId,
            PropertyName = propertyName,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };
    }

    /// <summary>
    /// Converts a collection of Notion properties into local <see cref="TaskProperty"/> instances.
    /// </summary>
    /// <param name="taskId">The owning task identifier.</param>
    /// <param name="notionProperties">Property dictionary from a Notion page.</param>
    /// <returns>List of mapped task properties; entries that cannot be mapped are skipped.</returns>
    public List<TaskProperty> MapAllFromNotion(Guid taskId, Dictionary<string, object?>? notionProperties)
    {
        if (notionProperties is null || notionProperties.Count == 0)
            return new List<TaskProperty>();

        var result = new List<TaskProperty>(notionProperties.Count);

        foreach (var kvp in notionProperties)
        {
            var mapped = MapFromNotion(taskId, kvp.Key, kvp.Value);
            if (mapped is not null)
                result.Add(mapped);
        }

        return result;
    }

    /// <summary>
    /// Converts a local <see cref="TaskProperty"/> into a Notion-compatible property update payload.
    /// </summary>
    /// <param name="property">The local task property.</param>
    /// <returns>A key-value pair suitable for inclusion in a Notion API update request.</returns>
    public KeyValuePair<string, object> MapToNotion(TaskProperty property)
    {
        ArgumentNullException.ThrowIfNull(property);

        object payload = property.DataType switch
        {
            PropertyDataType.Boolean => new Dictionary<string, object> { ["checkbox"] = bool.TryParse(property.PropertyValue, out var b) && b },
            PropertyDataType.Integer or PropertyDataType.Decimal => new Dictionary<string, object> { ["number"] = double.TryParse(property.PropertyValue, NumberStyles.Any, CultureInfo.InvariantCulture, out var n) ? n : 0 },
            PropertyDataType.DateTime => new Dictionary<string, object> { ["date"] = new Dictionary<string, string?> { ["start"] = property.PropertyValue } },
            _ => new Dictionary<string, object> { ["rich_text"] = new[] { new Dictionary<string, object> { ["text"] = new Dictionary<string, string?> { ["content"] = property.PropertyValue } } } },
        };

        return new KeyValuePair<string, object>(property.PropertyName, payload);
    }

    /// <summary>
    /// Determines whether two property values are semantically equal, accounting for type coercion.
    /// </summary>
    /// <param name="local">Local property value.</param>
    /// <param name="notion">Notion property value.</param>
    /// <param name="dataType">The data type used for comparison.</param>
    /// <returns><see langword="true"/> when values are considered equivalent.</returns>
    public bool AreValuesEqual(string? local, string? notion, PropertyDataType dataType)
    {
        if (local is null && notion is null) return true;
        if (local is null || notion is null) return false;

        return dataType switch
        {
            PropertyDataType.Integer or PropertyDataType.Decimal =>
                double.TryParse(local, NumberStyles.Any, CultureInfo.InvariantCulture, out var ln) &&
                double.TryParse(notion, NumberStyles.Any, CultureInfo.InvariantCulture, out var nn) &&
                Math.Abs(ln - nn) < 1e-9,
            PropertyDataType.Boolean =>
                bool.TryParse(local, out var lb) && bool.TryParse(notion, out var nb) && lb == nb,
            PropertyDataType.DateTime =>
                DateTime.TryParse(local, CultureInfo.InvariantCulture, DateTimeStyles.None, out var ld) &&
                DateTime.TryParse(notion, CultureInfo.InvariantCulture, DateTimeStyles.None, out var nd) &&
                ld == nd,
            _ => string.Equals(local.Trim(), notion.Trim(), StringComparison.Ordinal),
        };
    }

    private static (string? Value, PropertyDataType DataType) ExtractValueAndType(object? notionValue)
    {
        if (notionValue is null)
            return (null, PropertyDataType.String);

        if (notionValue is Dictionary<string, object?> dict)
        {
            var notionType = dict.TryGetValue("type", out var t) ? t?.ToString() : null;
            var dataType = notionType is not null && NotionTypeMap.TryGetValue(notionType, out var dt) ? dt : PropertyDataType.String;
            var value = ExtractScalarValue(dict, notionType);
            return (value, dataType);
        }

        return (notionValue.ToString(), PropertyDataType.String);
    }

    private static string? ExtractScalarValue(Dictionary<string, object?> dict, string? notionType)
    {
        if (notionType is null) return dict.ToString();

        if (dict.TryGetValue(notionType, out var inner))
        {
            if (inner is null) return null;
            if (inner is bool b) return b.ToString();
            if (inner is Dictionary<string, object?> nested)
            {
                if (nested.TryGetValue("name", out var name)) return name?.ToString();
                if (nested.TryGetValue("start", out var start)) return start?.ToString();
                if (nested.TryGetValue("content", out var content)) return content?.ToString();
            }
            return inner.ToString();
        }

        return null;
    }
}
