#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// ===================================================================

namespace NotionTaskSync.Services;

using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Globalization;
using NotionTaskSync.Domain.Models;

/// <summary>
/// Provides System.Text.Json serialization extensions for <see cref="ChangeDetectionService"/>
/// to enable JSON serialization/deserialization of the service's data structures.
/// </summary>
/// <remarks>
/// This class provides convenience methods for serializing and deserializing ChangeDetectionService
/// and related data models to/from JSON using camelCase naming policy and ignoring cycles.
/// </remarks>
public static class ChangeDetectionServiceJsonExtensions
{
    private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
        ReferenceHandler = ReferenceHandler.IgnoreCycles,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    /// <summary>
    /// Converts a <see cref="ChangeDetectionService"/> instance to its JSON representation.
    /// </summary>
    /// <param name="value">The service instance to serialize.</param>
    /// <param name="indented">Whether to format the JSON with indentation for readability.</param>
    /// <returns>A JSON string representation of the service instance.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null.</exception>
    public static string ToJson(this ChangeDetectionService value, bool indented = false)
        => JsonSerializer.Serialize(value, indented ? new JsonSerializerOptions(_jsonOptions) { WriteIndented = true } : _jsonOptions);

    /// <summary>
    /// Deserializes a JSON string to a <see cref="ChangeDetectionService"/> instance.
    /// </summary>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <returns>A <see cref="ChangeDetectionService"/> instance populated from JSON, or null if deserialization fails.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="json"/> is null or empty.</exception>
    public static ChangeDetectionService? FromJson(string json)
    {
        ArgumentException.ThrowIfNullOrEmpty(json);
        return JsonSerializer.Deserialize<ChangeDetectionService>(json, _jsonOptions);
    }

    /// <summary>
    /// Attempts to deserialize a JSON string to a <see cref="ChangeDetectionService"/> instance.
    /// </summary>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <param name="value">Receives the deserialized service instance if successful, otherwise null.</param>
    /// <returns>True if deserialization succeeds; otherwise, false.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="json"/> is null or empty.</exception>
    public static bool TryFromJson(string json, out ChangeDetectionService? value)
    {
        ArgumentException.ThrowIfNullOrEmpty(json);
        try
        {
            value = JsonSerializer.Deserialize<ChangeDetectionService>(json, _jsonOptions);
            return true;
        }
        catch (JsonException)
        {
            value = null;
            return false;
        }
    }

    /// <summary>
    /// Serializes a <see cref="List{T}"/> of <see cref="ChangeLog"/> to JSON.
    /// </summary>
    /// <param name="changes">The change log collection to serialize.</param>
    /// <param name="indented">Whether to format the JSON with indentation for readability.</param>
    /// <returns>A JSON string representation of the change logs.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="changes"/> is null.</exception>
    public static string ToJson(this List<ChangeLog> changes, bool indented = false)
        => JsonSerializer.Serialize(changes, indented ? new JsonSerializerOptions(_jsonOptions) { WriteIndented = true } : _jsonOptions);

    /// <summary>
    /// Deserializes a JSON string to a <see cref="List{T}"/> of <see cref="ChangeLog"/>.
    /// </summary>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <returns>A list of change logs populated from JSON, or null if deserialization fails.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="json"/> is null or empty.</exception>
    public static List<ChangeLog>? FromJsonToChangeLogList(string json)
    {
        ArgumentException.ThrowIfNullOrEmpty(json);
        return JsonSerializer.Deserialize<List<ChangeLog>>(json, _jsonOptions);
    }

    /// <summary>
    /// Attempts to deserialize a JSON string to a <see cref="List{T}"/> of <see cref="ChangeLog"/>.
    /// </summary>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <param name="value">Receives the deserialized change log list if successful, otherwise null.</param>
    /// <returns>True if deserialization succeeds; otherwise, false.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="json"/> is null or empty.</exception>
    public static bool TryFromJson(string json, out List<ChangeLog>? value)
    {
        ArgumentException.ThrowIfNullOrEmpty(json);
        try
        {
            value = JsonSerializer.Deserialize<List<ChangeLog>>(json, _jsonOptions);
            return true;
        }
        catch (JsonException)
        {
            value = null;
            return false;
        }
    }

    /// <summary>
    /// Serializes a <see cref="List{T}"/> of <see cref="ConflictResolution"/> to JSON.
    /// </summary>
    /// <param name="conflicts">The conflict resolution collection to serialize.</param>
    /// <param name="indented">Whether to format the JSON with indentation for readability.</param>
    /// <returns>A JSON string representation of the conflict resolutions.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="conflicts"/> is null.</exception>
    public static string ToJson(this List<ConflictResolution> conflicts, bool indented = false)
        => JsonSerializer.Serialize(conflicts, indented ? new JsonSerializerOptions(_jsonOptions) { WriteIndented = true } : _jsonOptions);

    /// <summary>
    /// Deserializes a JSON string to a <see cref="List{T}"/> of <see cref="ConflictResolution"/>.
    /// </summary>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <returns>A list of conflict resolutions populated from JSON, or null if deserialization fails.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="json"/> is null or empty.</exception>
    public static List<ConflictResolution>? FromJsonToConflictResolutionList(string json)
    {
        ArgumentException.ThrowIfNullOrEmpty(json);
        return JsonSerializer.Deserialize<List<ConflictResolution>>(json, _jsonOptions);
    }

    /// <summary>
    /// Attempts to deserialize a JSON string to a <see cref="List{T}"/> of <see cref="ConflictResolution"/>.
    /// </summary>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <param name="value">Receives the deserialized conflict resolution list if successful, otherwise null.</param>
    /// <returns>True if deserialization succeeds; otherwise, false.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="json"/> is null or empty.</exception>
    public static bool TryFromJson(string json, out List<ConflictResolution>? value)
    {
        ArgumentException.ThrowIfNullOrEmpty(json);
        try
        {
            value = JsonSerializer.Deserialize<List<ConflictResolution>>(json, _jsonOptions);
            return true;
        }
        catch (JsonException)
        {
            value = null;
            return false;
        }
    }
}