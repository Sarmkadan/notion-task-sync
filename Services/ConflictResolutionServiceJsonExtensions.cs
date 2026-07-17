#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

namespace NotionTaskSync.Services;

using System;
using System.Text.Json;
using System.Text.Json.Serialization;

/// <summary>
/// Provides System.Text.Json serialization extensions for <see cref="ConflictResolutionService"/>.
/// Enables round-trip serialization of conflict resolution strategies and results.
/// This class cannot be inherited.
/// </summary>
public static class ConflictResolutionServiceJsonExtensions
{
    private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    /// <summary>
    /// Serializes the <see cref="ConflictResolutionService"/> instance to a JSON string.
    /// </summary>
    /// <param name="value">The service instance to serialize.</param>
    /// <param name="indented">Whether to format the JSON with indentation for readability.</param>
    /// <returns>A JSON string representation of the service using camelCase property naming.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is <see langword="null"/>.</exception>
    public static string ToJson(this ConflictResolutionService value, bool indented = false)
    {
        ArgumentNullException.ThrowIfNull(value);

        var options = indented
            ? new JsonSerializerOptions(_jsonOptions) { WriteIndented = true }
            : _jsonOptions;

        return JsonSerializer.Serialize(value, options);
    }

    /// <summary>
    /// Deserializes a JSON string to a <see cref="ConflictResolutionService"/> instance.
    /// </summary>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <returns>The deserialized <see cref="ConflictResolutionService"/> instance, or <see langword="null"/> if deserialization fails.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="json"/> is <see langword="null"/> or empty.</exception>
    public static ConflictResolutionService? FromJson(string json)
    {
        ArgumentException.ThrowIfNullOrEmpty(json);

        try
        {
            return JsonSerializer.Deserialize<ConflictResolutionService>(json, _jsonOptions);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    /// <summary>
    /// Attempts to deserialize a JSON string to a <see cref="ConflictResolutionService"/> instance.
    /// </summary>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <param name="value">Receives the deserialized instance if successful; otherwise, <see langword="null"/>.</param>
    /// <returns><see langword="true"/> if deserialization succeeded; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="json"/> is <see langword="null"/> or empty.</exception>
    public static bool TryFromJson(string json, out ConflictResolutionService? value)
    {
        ArgumentException.ThrowIfNullOrEmpty(json);

        try
        {
            value = JsonSerializer.Deserialize<ConflictResolutionService>(json, _jsonOptions);
            return true;
        }
        catch (JsonException)
        {
            value = null;
            return false;
        }
    }
}