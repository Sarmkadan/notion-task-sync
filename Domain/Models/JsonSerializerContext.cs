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
/// <remarks>
/// The <see cref="JsonSerializableAttribute"/> declarations below list every type that is
/// serialized or deserialized through this context. The System.Text.Json source generator
/// emits the metadata for those types at build time, so no reflection is needed at run time.
/// This also makes the serialization path compatible with trimming and AOT publishing.
/// Types that are not listed here are not covered by the generated metadata, so add a new
/// attribute when a new DTO needs to go through this context.
/// Enums are registered with an explicit <c>TypeInfoPropertyName</c> so that each generated
/// <c>JsonTypeInfo</c> property has a stable, unambiguous name.
/// The instance <see cref="Serialize{T}"/> and <see cref="Deserialize{T}"/> helpers pass
/// <see cref="JsonSerializerContext.Options"/> to <see cref="JsonSerializer"/> so callers use
/// the generated options.
/// Used by <c>Formatters/JsonFormatter.cs</c>; covered by <c>JsonSerializerContextTests</c>.
/// </remarks>
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