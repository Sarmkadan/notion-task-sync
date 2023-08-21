#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace NotionTaskSync.Integration;

using System;
using System.Text.Json;

/// <summary>
/// Provides System.Text.Json serialization extensions for <see cref="ExternalApiWrapper"/>.
/// Includes methods for converting <see cref="ExternalApiWrapper"/> instances to JSON and
/// parsing JSON back into <see cref="ExternalApiWrapper"/> objects.
/// </summary>
public static class ExternalApiWrapperJsonExtensions
{
    private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
    };

    /// <summary>
    /// Serializes the <see cref="ExternalApiWrapper"/> instance to a JSON string.
    /// </summary>
    /// <param name="value">The <see cref="ExternalApiWrapper"/> instance to serialize.</param>
    /// <param name="indented">Whether to format the JSON with indentation for readability.</param>
    /// <returns>A JSON string representation of the <see cref="ExternalApiWrapper"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null.</exception>
    public static string ToJson(this ExternalApiWrapper value, bool indented = false)
    {
        ArgumentNullException.ThrowIfNull(value);

        var options = indented
            ? new JsonSerializerOptions(_jsonOptions) { WriteIndented = true }
            : _jsonOptions;

        return JsonSerializer.Serialize(value, options);
    }

    /// <summary>
    /// Parses a JSON string and deserializes it into an <see cref="ExternalApiWrapper"/> instance.
    /// </summary>
    /// <param name="json">The JSON string to parse.</param>
    /// <returns>An <see cref="ExternalApiWrapper"/> instance deserialized from the JSON, or null if the JSON is empty.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="json"/> is null.</exception>
    /// <exception cref="JsonException">Thrown when the JSON is invalid or cannot be deserialized.</exception>
    public static ExternalApiWrapper? FromJson(string json)
    {
        ArgumentNullException.ThrowIfNull(json);

        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        return JsonSerializer.Deserialize<ExternalApiWrapper>(json, _jsonOptions);
    }

    /// <summary>
    /// Attempts to parse a JSON string and deserialize it into an <see cref="ExternalApiWrapper"/> instance.
    /// </summary>
    /// <param name="json">The JSON string to parse.</param>
    /// <param name="value">Receives the deserialized <see cref="ExternalApiWrapper"/> instance if successful, otherwise null.</param>
    /// <returns>True if the JSON was successfully parsed and deserialized; otherwise, false.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="json"/> is null.</exception>
    public static bool TryFromJson(string json, out ExternalApiWrapper? value)
    {
        ArgumentNullException.ThrowIfNull(json);

        value = null;

        if (string.IsNullOrWhiteSpace(json))
        {
            return false;
        }

        try
        {
            value = JsonSerializer.Deserialize<ExternalApiWrapper>(json, _jsonOptions);
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }
}