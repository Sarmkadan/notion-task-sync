#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

namespace NotionTaskSync.Tests;

using NotionTaskSync.Domain.Models;
using FluentAssertions;
using Xunit;

/// <summary>
/// Tests that the source-generated JsonSerializerContext includes all necessary
/// types for Notion polymorphic property values. This ensures that when reflection-based
/// serialization is disabled (AOT/trimming scenarios), all types can still be
/// properly serialized and deserialized.
/// </summary>
public class JsonSerializerContextTests
{
    private readonly AppJsonSerializerContext _jsonContext = new();

    [Fact]
    public void AppJsonSerializerContext_IncludesAllNotionPropertyTypes()
    {
        // This test verifies that all Notion polymorphic property types are registered
        // in the JsonSerializerContext. If any type is missing, it will throw a
        // JsonException during serialization/deserialization.

        // Test rich text types
        var richText = new NotionRichTextObject(
            type: "text",
            text: new NotionTextObject("Test"),
            annotations: new NotionRichTextAnnotations(),
            plain_text: "Test"
        );

        var richTextJson = _jsonContext.Serialize(richText);
        var deserializedRichText = _jsonContext.Deserialize<NotionRichTextObject>(richTextJson);
        deserializedRichText.Should().NotBeNull();

        // Test select option
        var selectOption = new NotionSelectOption(id: "1", name: "Option 1", color: "red");
        var selectJson = _jsonContext.Serialize(selectOption);
        var deserializedSelect = _jsonContext.Deserialize<NotionSelectOption>(selectJson);
        deserializedSelect.Should().NotBeNull();

        // Test status option
        var statusOption = new NotionStatusOption(id: "2", name: "In Progress", color: "yellow");
        var statusJson = _jsonContext.Serialize(statusOption);
        var deserializedStatus = _jsonContext.Deserialize<NotionStatusOption>(statusJson);
        deserializedStatus.Should().NotBeNull();

        // Test date value
        var dateValue = new NotionDateValue(start: "2024-01-01T00:00:00Z");
        var dateJson = _jsonContext.Serialize(dateValue);
        var deserializedDate = _jsonContext.Deserialize<NotionDateValue>(dateJson);
        deserializedDate.Should().NotBeNull();

        // Test multi-select
        var multiSelect = new NotionMultiSelectValue(
            options: [new NotionSelectOption(id: "1", name: "Tag1", color: "blue")]
        );
        var multiSelectJson = _jsonContext.Serialize(multiSelect);
        var deserializedMultiSelect = _jsonContext.Deserialize<NotionMultiSelectValue>(multiSelectJson);
        deserializedMultiSelect.Should().NotBeNull();

        // Test number value
        var numberValue = new NotionNumberValue(42);
        var numberJson = _jsonContext.Serialize(numberValue);
        var deserializedNumber = _jsonContext.Deserialize<NotionNumberValue>(numberJson);
        deserializedNumber.Should().NotBeNull();

        // Test checkbox
        var checkboxValue = new NotionCheckboxValue(@checked: true);
        var checkboxJson = _jsonContext.Serialize(checkboxValue);
        var deserializedCheckbox = _jsonContext.Deserialize<NotionCheckboxValue>(checkboxJson);
        deserializedCheckbox.Should().NotBeNull();

        // Test URL
        var urlValue = new NotionUrlValue("https://example.com");
        var urlJson = _jsonContext.Serialize(urlValue);
        var deserializedUrl = _jsonContext.Deserialize<NotionUrlValue>(urlJson);
        deserializedUrl.Should().NotBeNull();

        // Test email
        var emailValue = new NotionEmailValue("test@example.com");
        var emailJson = _jsonContext.Serialize(emailValue);
        var deserializedEmail = _jsonContext.Deserialize<NotionEmailValue>(emailJson);
        deserializedEmail.Should().NotBeNull();

        // Test phone number
        var phoneValue = new NotionPhoneNumberValue("+1 (555) 123-4567");
        var phoneJson = _jsonContext.Serialize(phoneValue);
        var deserializedPhone = _jsonContext.Deserialize<NotionPhoneNumberValue>(phoneJson);
        deserializedPhone.Should().NotBeNull();
    }

    [Fact]
    public void AppJsonSerializerContext_SupportsDictionaryWithNotionTypes()
    {
        // Test that dictionaries containing Notion types can be serialized
        var dict = new Dictionary<string, object?>
        {
            ["richText"] = new NotionRichTextObject(
                type: "text",
                text: new NotionTextObject("Title"),
                annotations: new NotionRichTextAnnotations(),
                plain_text: "Title"
            ),
            ["status"] = new NotionStatusOption(id: "1", name: "Done", color: "green"),
            ["priority"] = new NotionNumberValue(100)
        };

        var json = _jsonContext.Serialize(dict);
        json.Should().NotBeNullOrWhiteSpace();

        var deserialized = _jsonContext.Deserialize<Dictionary<string, object?>>(json);
        deserialized.Should().NotBeNull();
        deserialized.Should().HaveCount(3);
    }

    [Fact]
    public void AppJsonSerializerContext_AllTypesRegistered_ForAotScenarios()
    {
        // This test ensures that all types that might be encountered in Notion API
        // responses are registered in the JsonSerializerContext. This is critical
        // for AOT compilation and trimming scenarios where reflection-based serialization
        // is not available.

        // Common Notion property types that are polymorphic and need explicit registration
        var typesToTest = new object[]
        {
            // Text-based properties
            new NotionRichTextObject("text", new NotionTextObject("test"), new NotionRichTextAnnotations(), "test"),
            new NotionTextObject("test content"),

            // Selection properties
            new NotionSelectOption("id1", "Option 1", "red"),
            new NotionStatusOption("id2", "Status 1", "yellow"),
            new NotionMultiSelectValue([new NotionSelectOption("id3", "Tag 1", "blue")]),

            // Date/time properties
            new NotionDateValue("2024-01-01T00:00:00Z"),

            // Number properties
            new NotionNumberValue(42),
            new NotionNumberValue(null),

            // Boolean properties
            new NotionCheckboxValue(true),
            new NotionCheckboxValue(false),

            // String properties
            new NotionUrlValue("https://example.com"),
            new NotionEmailValue("test@example.com"),
            new NotionPhoneNumberValue("+1 (555) 123-4567"),

            // Annotations
            new NotionRichTextAnnotations(true, false, true, false, true, "red_background")
        };

        foreach (var typeInstance in typesToTest)
        {
            var type = typeInstance.GetType();
            var json = _jsonContext.Serialize(typeInstance);
            json.Should().NotBeNullOrWhiteSpace($"Failed to serialize {type.Name}");

            // Attempt to deserialize back to the original type
            var deserializeMethod = typeof(AppJsonSerializerContext).GetMethod(
                nameof(AppJsonSerializerContext.Deserialize),
                [typeof(string)]
            )!.MakeGenericMethod(type);

            var deserialized = deserializeMethod.Invoke(_jsonContext, [json]);
            deserialized.Should().NotBeNull($"Failed to deserialize {type.Name}");
        }
    }
}
