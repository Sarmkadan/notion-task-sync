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
/// <param name="content">The actual text string.</param>
public record NotionTextObject(string content);

/// <summary>
/// Text formatting annotations for rich text.
/// </summary>
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
/// <param name="options">Array of selected options.</param>
public record NotionMultiSelectValue(NotionSelectOption[] options);

/// <summary>
/// Represents a Notion number property value.
/// </summary>
/// <param name="number">The numeric value.</param>
public record NotionNumberValue(decimal? number);

/// <summary>
/// Represents a Notion checkbox property value.
/// </summary>
/// <param name="checked">Whether the checkbox is checked.</param>
public record NotionCheckboxValue(bool @checked);

/// <summary>
/// Represents a Notion URL property value.
/// </summary>
/// <param name="url">The URL string.</param>
public record NotionUrlValue(string url);

/// <summary>
/// Represents a Notion email property value.
/// </summary>
/// <param name="email">The email address string.</param>
public record NotionEmailValue(string email);

/// <summary>
/// Represents a Notion phone number property value.
/// </summary>
/// <param name="phone_number">The phone number string.</param>
public record NotionPhoneNumberValue(string phone_number);
