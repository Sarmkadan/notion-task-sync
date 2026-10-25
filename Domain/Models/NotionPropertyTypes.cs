#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

namespace NotionTaskSync.Domain.Models;

using System.Text.Json.Serialization;

/// <summary>
/// Represents a Notion rich text object with annotations and plain text.
/// Used in title, rich_text, and other text-based properties.
/// </summary>
/// <remarks>
/// Maps to a single element of the Notion API <c>rich_text</c> array. Property names are
/// snake_case to match the Notion JSON exactly, so they are serialized without renaming.
/// </remarks>
/// <param name="type">Always "text" for rich text objects.</param>
/// <param name="text">The actual text content.</param>
/// <param name="annotations">Text formatting annotations.</param>
/// <param name="plain_text">Plain text representation without annotations.</param>
/// <param name="href">Optional URL if the text is a link.</param>
public record NotionRichTextObject(
    string type,
    NotionTextObject text,
    NotionRichTextAnnotations annotations,
    string plain_text,
    string? href = null);

/// <summary>
/// The text content of a rich text object.
/// </summary>
/// <remarks>
/// Corresponds to the <c>text</c> field of a <see cref="NotionRichTextObject"/>.
/// </remarks>
/// <param name="content">The actual text string.</param>
public record NotionTextObject(string content);

/// <summary>
/// Text formatting annotations for rich text.
/// </summary>
/// <remarks>
/// Corresponds to the <c>annotations</c> field of a <see cref="NotionRichTextObject"/>.
/// All flags default to off and the color defaults to "default", matching Notion's defaults.
/// </remarks>
/// <param name="bold">Whether text is bold.</param>
/// <param name="italic">Whether text is italic.</param>
/// <param name="strikethrough">Whether text has strikethrough.</param>
/// <param name="underline">Whether text is underlined.</param>
/// <param name="code">Whether text is code-formatted.</param>
/// <param name="color">Text color (e.g., "default", "gray_background").</param>
public record NotionRichTextAnnotations(
    bool bold = false,
    bool italic = false,
    bool strikethrough = false,
    bool underline = false,
    bool code = false,
    string color = "default");

/// <summary>
/// Represents a Notion select property value.
/// </summary>
/// <remarks>
/// Used for a single option of a <c>select</c> property. Both <see cref="id"/> and
/// <see cref="name"/> are nullable because either may be absent in a partial payload.
/// </remarks>
/// <param name="id">The ID of the select option.</param>
/// <param name="name">The display name of the select option.</param>
/// <param name="color">The color of the select option.</param>
public record NotionSelectOption(
    string? id = null,
    string? name = null,
    string color = "default");

/// <summary>
/// Represents a Notion status property value.
/// Similar to select but with additional status-specific semantics.
/// </summary>
/// <remarks>
/// Used for a single option of a <c>status</c> property. Shares the same shape as
/// <see cref="NotionSelectOption"/>, but Notion groups status options into categories
/// (for example "To-do", "In progress", "Complete") that this type does not model.
/// </remarks>
/// <param name="id">The ID of the status option.</param>
/// <param name="name">The display name of the status.</param>
/// <param name="color">The color of the status.</param>
public record NotionStatusOption(
    string? id = null,
    string? name = null,
    string color = "default");

/// <summary>
/// Represents a Notion date property value.
/// </summary>
/// <remarks>
/// Used for a <c>date</c> property. A value with only <see cref="start"/> is a single date
/// or datetime; adding <see cref="end"/> makes it a range.
/// </remarks>
/// <param name="start">The start date in ISO 8601 format.</param>
/// <param name="end">Optional end date in ISO 8601 format for date ranges.</param>
/// <param name="time_zone">Optional timezone.</param>
public record NotionDateValue(
    string start,
    string? end = null,
    string? time_zone = null);

/// <summary>
/// Represents a Notion multi-select property value.
/// </summary>
/// <remarks>
/// Used for a <c>multi_select</c> property. The array holds every selected option;
/// an empty array means nothing is selected.
/// </remarks>
/// <param name="options">Array of selected options.</param>
public record NotionMultiSelectValue(NotionSelectOption[] options);

/// <summary>
/// Represents a Notion number property value.
/// </summary>
/// <remarks>
/// Used for a <c>number</c> property. A null value means the property is empty in Notion,
/// which is distinct from zero.
/// </remarks>
/// <param name="number">The numeric value.</param>
public record NotionNumberValue(decimal? number);

/// <summary>
/// Represents a Notion checkbox property value.
/// </summary>
/// <remarks>
/// Used for a <c>checkbox</c> property. The property name is escaped with <c>@</c>
/// because <c>checked</c> is a C# keyword; the JSON name is still "checked".
/// </remarks>
/// <param name="checked">Whether the checkbox is checked.</param>
public record NotionCheckboxValue(bool @checked);

/// <summary>
/// Represents a Notion URL property value.
/// </summary>
/// <remarks>
/// Used for a <c>url</c> property. The value is stored as written and is not validated here.
/// </remarks>
/// <param name="url">The URL string.</param>
public record NotionUrlValue(string url);

/// <summary>
/// Represents a Notion email property value.
/// </summary>
/// <remarks>
/// Used for an <c>email</c> property. The value is stored as written and is not validated here.
/// </remarks>
/// <param name="email">The email address string.</param>
public record NotionEmailValue(string email);

/// <summary>
/// Represents a Notion phone number property value.
/// </summary>
/// <remarks>
/// Used for a <c>phone_number</c> property. The value is stored as written and is not
/// normalized to any particular format.
/// </remarks>
/// <param name="phone_number">The phone number string.</param>
public record NotionPhoneNumberValue(string phone_number);
