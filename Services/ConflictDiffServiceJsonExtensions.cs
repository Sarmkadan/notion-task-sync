#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace NotionTaskSync.Services;

using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using NotionTaskSync.Domain.Models;

/// <summary>
/// Provides JSON serialization and deserialization extensions for <see cref="ConflictDiffService"/>
/// and related diff types using System.Text.Json.
/// </summary>
public static class ConflictDiffServiceJsonExtensions
{
    private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        ReferenceHandler = ReferenceHandler.IgnoreCycles
    };

    /// <summary>
    /// Serializes the <see cref="ConflictDiffService"/> instance to a JSON string.
    /// </summary>
    /// <param name="value">The service instance to serialize.</param>
    /// <param name="indented">Whether to format the JSON with indentation for readability.</param>
    /// <returns>A JSON string representation of the service.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is <see langword="null"/>.</exception>
    public static string ToJson(this ConflictDiffService value, bool indented = false)
    {
        ArgumentNullException.ThrowIfNull(value);

        var options = indented
            ? new JsonSerializerOptions(_jsonOptions) { WriteIndented = true }
            : _jsonOptions;

        return JsonSerializer.Serialize(value, options);
    }

    /// <summary>
    /// Deserializes a JSON string into a <see cref="ConflictDiffService"/> instance.
    /// </summary>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <returns>A deserialized <see cref="ConflictDiffService"/> instance, or <see langword="null"/> if the JSON is invalid.</returns>
    /// <exception cref="JsonException">Thrown when the JSON is malformed or cannot be deserialized.</exception>
    public static ConflictDiffService? FromJson(string json)
    {
        ArgumentException.ThrowIfNullOrEmpty(json);

        return JsonSerializer.Deserialize<ConflictDiffService>(json, _jsonOptions);
    }

    /// <summary>
    /// Attempts to deserialize a JSON string into a <see cref="ConflictDiffService"/> instance.
    /// </summary>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <param name="value">Receives the deserialized instance if successful, otherwise <see langword="null"/>.</param>
    /// <returns><see langword="true"/> if deserialization succeeds; otherwise, <see langword="false"/>.</returns>
    public static bool TryFromJson(string json, out ConflictDiffService? value)
    {
        ArgumentException.ThrowIfNullOrEmpty(json);

        try
        {
            value = JsonSerializer.Deserialize<ConflictDiffService>(json, _jsonOptions);
            return true;
        }
        catch (JsonException)
        {
            value = null;
            return false;
        }
    }

    // -------------------------------------------------------------------------
    // Internal JSON converters for domain types
    // -------------------------------------------------------------------------

    private sealed class DiffLineKindConverter : JsonConverter<DiffLineKind>
    {
        public override DiffLineKind Read(
            ref Utf8JsonReader reader,
            Type typeToConvert,
            JsonSerializerOptions options)
        {
            return reader.TokenType switch
            {
                JsonTokenType.String => Enum.Parse<DiffLineKind>(reader.GetString()!),
                JsonTokenType.Number => (DiffLineKind)reader.GetInt32(),
                _ => throw new JsonException($"Unexpected token {reader.TokenType} when parsing DiffLineKind")
            };
        }

        public override void Write(
            Utf8JsonWriter writer,
            DiffLineKind value,
            JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToString());
        }
    }

    private sealed class DiffLineConverter : JsonConverter<DiffLine>
    {
        public override DiffLine Read(
            ref Utf8JsonReader reader,
            Type typeToConvert,
            JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.StartObject)
                throw new JsonException("Expected StartObject token for DiffLine");

            var line = new DiffLine();
            while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
            {
                if (reader.TokenType != JsonTokenType.PropertyName)
                    continue;

                var propertyName = reader.GetString();
                reader.Read();

                switch (propertyName)
                {
                    case "text":
                        line.Text = reader.GetString() ?? string.Empty;
                        break;
                    case "kind":
                        line.Kind = options.Converters.OfType<DiffLineKindConverter>()
                            .FirstOrDefault()
                            ?.Read(ref reader, typeof(DiffLineKind), options) ??
                            Enum.Parse<DiffLineKind>(reader.GetString()!);
                        break;
                    case "localLineNumber":
                        line.LocalLineNumber = reader.TokenType == JsonTokenType.Null
                            ? null
                            : reader.GetInt32();
                        break;
                    case "notionLineNumber":
                        line.NotionLineNumber = reader.TokenType == JsonTokenType.Null
                            ? null
                            : reader.GetInt32();
                        break;
                }
            }

            return line;
        }

        public override void Write(
            Utf8JsonWriter writer,
            DiffLine value,
            JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            writer.WriteString("text", value.Text);
            writer.WritePropertyName("kind");
            JsonSerializer.Serialize(writer, value.Kind, options);

            if (value.LocalLineNumber.HasValue)
                writer.WriteNumber("localLineNumber", value.LocalLineNumber.Value);

            if (value.NotionLineNumber.HasValue)
                writer.WriteNumber("notionLineNumber", value.NotionLineNumber.Value);

            writer.WriteEndObject();
        }
    }

    private sealed class ConflictDiffResultConverter : JsonConverter<ConflictDiffResult>
    {
        public override ConflictDiffResult Read(
            ref Utf8JsonReader reader,
            Type typeToConvert,
            JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.StartObject)
                throw new JsonException("Expected StartObject token for ConflictDiffResult");

            var result = new ConflictDiffResult();
            var lines = new List<DiffLine>();

            while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
            {
                if (reader.TokenType != JsonTokenType.PropertyName)
                    continue;

                var propertyName = reader.GetString();
                reader.Read();

                switch (propertyName)
                {
                    case "conflictId":
                        result.ConflictId = reader.GetGuid();
                        break;
                    case "propertyName":
                        result.PropertyName = reader.GetString() ?? string.Empty;
                        break;
                    case "localValue":
                        result.LocalValue = reader.GetString();
                        break;
                    case "notionValue":
                        result.NotionValue = reader.GetString();
                        break;
                    case "lines":
                        lines = JsonSerializer.Deserialize<List<DiffLine>>(ref reader, options) ?? new();
                        break;
                    case "generatedAt":
                        result.GeneratedAt = reader.GetDateTime().ToUniversalTime();
                        break;
                }
            }

            result.Lines = lines;
            return result;
        }

        public override void Write(
            Utf8JsonWriter writer,
            ConflictDiffResult value,
            JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            writer.WriteString("propertyName", value.PropertyName);

            if (value.LocalValue != null)
                writer.WriteString("localValue", value.LocalValue);

            if (value.NotionValue != null)
                writer.WriteString("notionValue", value.NotionValue);

            writer.WritePropertyName("lines");
            JsonSerializer.Serialize(writer, value.Lines, options);
            writer.WriteString("generatedAt", value.GeneratedAt);
            writer.WriteString("conflictId", value.ConflictId);
            writer.WriteEndObject();
        }
    }
}

internal static class JsonReaderExtensions
{
    public static Guid GetGuid(this ref Utf8JsonReader reader)
    {
        if (reader.TokenType == JsonTokenType.String && Guid.TryParse(reader.GetString(), out var guid))
            return guid;
        if (reader.TokenType == JsonTokenType.StartArray)
        {
            Span<byte> bytes = stackalloc byte[16];
            for (int i = 0; i < 16; i++)
            {
                reader.Read();
                bytes[i] = (byte)reader.GetInt32();
            }
            return new Guid(bytes);
        }
        throw new JsonException("Invalid GUID format");
    }

    public static DateTime GetDateTime(this ref Utf8JsonReader reader)
    {
        return reader.GetDateTime();
    }
}