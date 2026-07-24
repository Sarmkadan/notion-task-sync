#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

namespace NotionTaskSync.Domain.Models;

using System.Text.Json;
using System.Text.Json.Serialization;

/// <summary>
/// Source-generated JsonSerializerContext for core DTOs used in hot serialization loops.
/// Improves performance by avoiding reflection-based serialization and providing
/// compile-time generated serialization metadata.
/// </summary>
[JsonSerializable(typeof(Task))]
[JsonSerializable(typeof(SyncConfig))]
[JsonSerializable(typeof(NotionPage))]
[JsonSerializable(typeof(ConflictResolution))]
[JsonSerializable(typeof(ChangeLog))]
[JsonSerializable(typeof(global::NotionTaskSync.Domain.Exceptions.SyncException))]
[JsonSerializable(typeof(global::NotionTaskSync.Domain.Exceptions.NotionApiException))]
[JsonSerializable(typeof(global::NotionTaskSync.Domain.Exceptions.LocalFileException))]
[JsonSerializable(typeof(global::NotionTaskSync.Domain.Exceptions.ValidationException))]
[JsonSerializable(typeof(global::NotionTaskSync.Domain.Exceptions.ConflictException))]
[JsonSerializable(typeof(global::NotionTaskSync.Domain.Exceptions.ConfigurationException))]
[JsonSerializable(typeof(TaskStatus), TypeInfoPropertyName = "TaskStatus")]
[JsonSerializable(typeof(SyncDirection), TypeInfoPropertyName = "SyncDirection")]
[JsonSerializable(typeof(ConflictResolutionStrategy), TypeInfoPropertyName = "ConflictResolutionStrategy")]
[JsonSerializable(typeof(ConflictType), TypeInfoPropertyName = "ConflictType")]
[JsonSerializable(typeof(ResolutionMethod), TypeInfoPropertyName = "ResolutionMethod")]
[JsonSerializable(typeof(ResolutionStatus), TypeInfoPropertyName = "ResolutionStatus")]
[JsonSerializable(typeof(ChangeSource), TypeInfoPropertyName = "ChangeSource")]
[JsonSerializable(typeof(Dictionary<string, object?>))]
[JsonSerializable(typeof(List<Task>))]
[JsonSerializable(typeof(List<ConflictResolution>))]
[JsonSerializable(typeof(List<ChangeLog>))]
[JsonSerializable(typeof(NotionRichTextObject))]
[JsonSerializable(typeof(NotionTextObject))]
[JsonSerializable(typeof(NotionRichTextAnnotations))]
[JsonSerializable(typeof(NotionSelectOption))]
[JsonSerializable(typeof(NotionStatusOption))]
[JsonSerializable(typeof(NotionDateValue))]
[JsonSerializable(typeof(NotionMultiSelectValue))]
[JsonSerializable(typeof(NotionNumberValue))]
[JsonSerializable(typeof(NotionCheckboxValue))]
[JsonSerializable(typeof(NotionUrlValue))]
[JsonSerializable(typeof(NotionEmailValue))]
[JsonSerializable(typeof(NotionPhoneNumberValue))]
public partial class AppJsonSerializerContext : JsonSerializerContext
{
    /// <summary>
    /// Serializes an object to JSON using the context's options.
    /// </summary>
    /// <typeparam name="T">Type of the object to serialize.</typeparam>
    /// <param name="value">The object to serialize.</param>
    /// <returns>JSON string representation of the object.</returns>
    public string Serialize<T>(T value)
    {
        return JsonSerializer.Serialize(value, base.Options);
    }

    /// <summary>
    /// Deserializes JSON to an object of type T.
    /// </summary>
    /// <typeparam name="T">Type to deserialize to.</typeparam>
    /// <param name="json">JSON string to deserialize.</param>
    /// <returns>Deserialized object.</returns>
    public T? Deserialize<T>(string json)
    {
        return JsonSerializer.Deserialize<T>(json, base.Options);
    }
}