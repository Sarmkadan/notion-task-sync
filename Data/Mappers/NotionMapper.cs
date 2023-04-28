#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace NotionTaskSync.Data.Mappers;

using NotionTaskSync.Domain.Models;
using System;
using System.Collections.Generic;

/// <summary>
/// Maps between Notion API responses and NotionPage domain entities.
/// Handles transformation of Notion's complex property structures.
/// </summary>
public static class NotionMapper
{
    /// <summary>
    /// Parses a Notion API response and creates a NotionPage entity.
    /// </summary>
    public static NotionPage ParseFromNotionResponse(Dictionary<string, object?>? response)
    {
        if (response is null)
            throw new ArgumentNullException(nameof(response));

        var pageId = ExtractString(response, "id");
        var title = ExtractTitle(response);
        var databaseId = ExtractDatabaseId(response);

        if (string.IsNullOrEmpty(pageId) || string.IsNullOrEmpty(title))
            throw new InvalidOperationException("Invalid Notion response: missing required fields");

        var page = new NotionPage(pageId, databaseId, title);

        // Extract timestamps
        if (DateTime.TryParse(ExtractString(response, "created_time"), out var createdTime))
            page.CreatedTime = createdTime;

        if (DateTime.TryParse(ExtractString(response, "last_edited_time"), out var editedTime))
            page.LastEditedTime = editedTime;

        // Extract other fields
        page.Archived = ExtractBoolean(response, "archived");
        page.CreatedBy = ExtractString(response, "created_by");
        page.LastEditedBy = ExtractString(response, "last_edited_by");
        page.Url = ExtractString(response, "url");

        // Extract properties
        page.Properties = ExtractProperties(response);

        return page;
    }

    /// <summary>
    /// Converts a NotionPage to a format suitable for sending to Notion API.
    /// </summary>
    public static Dictionary<string, object> MapToNotionUpdate(NotionPage page)
    {
        var update = new Dictionary<string, object>
        {
            ["properties"] = new Dictionary<string, object>()
        };

        var properties = (Dictionary<string, object>)update["properties"];

        if (!string.IsNullOrEmpty(page.Title))
        {
            properties["Title"] = new
            {
                title = new[] { new { text = new { content = page.Title } } }
            };
        }

        if (page.Properties is not null)
        {
            foreach (var prop in page.Properties)
            {
                properties[prop.Key] = prop.Value ?? "";
            }
        }

        return update;
    }

    /// <summary>
    /// Converts a NotionPage to a creation payload for Notion API.
    /// </summary>
    public static Dictionary<string, object> MapToNotionCreate(NotionPage page, string databaseId)
    {
        var create = new Dictionary<string, object>
        {
            ["parent"] = new { database_id = databaseId },
            ["properties"] = new Dictionary<string, object>()
        };

        var properties = (Dictionary<string, object>)create["properties"];

        if (!string.IsNullOrEmpty(page.Title))
        {
            properties["Title"] = new
            {
                title = new[] { new { text = new { content = page.Title } } }
            };
        }

        if (page.Properties is not null)
        {
            foreach (var prop in page.Properties)
            {
                if (prop.Key != "Title")
                    properties[prop.Key] = prop.Value ?? "";
            }
        }

        return create;
    }

    /// <summary>
    /// Extracts the title from a Notion page response.
    /// </summary>
    private static string ExtractTitle(Dictionary<string, object?>? response)
    {
        if (response is null || !response.ContainsKey("properties"))
            return "Untitled";

        if (response["properties"] is Dictionary<string, object?> props &&
            props.ContainsKey("Title"))
        {
            if (props["Title"] is Dictionary<string, object?> titleProp &&
                titleProp.ContainsKey("title"))
            {
                if (titleProp["title"] is List<object> titleArray && titleArray.Count > 0)
                {
                    if (titleArray[0] is Dictionary<string, object?> titleObj &&
                        titleObj.ContainsKey("text"))
                    {
                        if (titleObj["text"] is Dictionary<string, object?> textObj &&
                            textObj.ContainsKey("content"))
                        {
                            return textObj["content"]?.ToString() ?? "Untitled";
                        }
                    }
                }
            }
        }

        return "Untitled";
    }

    /// <summary>
    /// Extracts the parent database ID from a Notion page response.
    /// </summary>
    private static string ExtractDatabaseId(Dictionary<string, object?>? response)
    {
        if (response is null || !response.ContainsKey("parent"))
            return string.Empty;

        if (response["parent"] is Dictionary<string, object?> parent &&
            parent.ContainsKey("database_id"))
        {
            return parent["database_id"]?.ToString() ?? string.Empty;
        }

        return string.Empty;
    }

    /// <summary>
    /// Extracts properties from a Notion page response.
    /// </summary>
    private static Dictionary<string, object?> ExtractProperties(Dictionary<string, object?>? response)
    {
        var props = new Dictionary<string, object?>();

        if (response is null || !response.ContainsKey("properties"))
            return props;

        if (response["properties"] is Dictionary<string, object?> properties)
        {
            foreach (var prop in properties)
            {
                props[prop.Key] = ExtractPropertyValue(prop.Value);
            }
        }

        return props;
    }

    /// <summary>
    /// Extracts a single property value from a Notion property object.
    /// </summary>
    private static object? ExtractPropertyValue(object? propertyObj)
    {
        if (propertyObj is null)
            return null;

        if (propertyObj is Dictionary<string, object?> propDict)
        {
            // Handle different property types
            if (propDict.ContainsKey("text"))
                return ExtractRichText(propDict["text"]);

            if (propDict.ContainsKey("title"))
                return ExtractRichText(propDict["title"]);

            if (propDict.ContainsKey("number"))
                return propDict["number"];

            if (propDict.ContainsKey("checkbox"))
                return propDict["checkbox"];

            if (propDict.ContainsKey("date"))
                return propDict["date"];

            if (propDict.ContainsKey("status"))
                return propDict["status"];
        }

        return null;
    }

    /// <summary>
    /// Extracts text content from Notion rich text arrays.
    /// </summary>
    private static string? ExtractRichText(object? richTextObj)
    {
        return NormalizeRichTextForComparison(richTextObj);
    }

    /// <summary>
    /// Normalizes a Notion rich-text value to plain text for semantic comparison.
    /// Strips annotation metadata (bold, italic, colour, etc.) and merges adjacent
    /// text runs so that fields which are semantically identical compare as equal even
    /// when Notion returns different annotation orderings or split text runs.
    /// Use this method whenever two property values are compared for change detection.
    /// </summary>
    public static string NormalizeRichTextForComparison(object? richTextValue)
    {
        if (richTextValue is null)
            return string.Empty;

        if (richTextValue is string strValue)
            return strValue.Trim();

        if (richTextValue is List<object> textArray)
        {
            var sb = new System.Text.StringBuilder();
            foreach (var item in textArray)
            {
                if (item is Dictionary<string, object?> textItem)
                {
                    // Notion always populates plain_text; prefer it over reconstructing from text.content
                    if (textItem.TryGetValue("plain_text", out var plainText))
                    {
                        sb.Append(plainText?.ToString());
                        continue;
                    }

                    // Fallback: extract content from the nested text object
                    if (textItem.TryGetValue("text", out var textObj) &&
                        textObj is Dictionary<string, object?> textContent &&
                        textContent.TryGetValue("content", out var content))
                    {
                        sb.Append(content?.ToString());
                    }
                }
            }

            return sb.ToString().Trim();
        }

        return richTextValue.ToString()?.Trim() ?? string.Empty;
    }

    /// <summary>
    /// Extracts a string value from the response dictionary.
    /// </summary>
    private static string? ExtractString(Dictionary<string, object?>? response, string key)
    {
        return response?.ContainsKey(key) == true ? response[key]?.ToString() : null;
    }

    /// <summary>
    /// Extracts a boolean value from the response dictionary.
    /// </summary>
    private static bool ExtractBoolean(Dictionary<string, object?>? response, string key)
    {
        if (response?.ContainsKey(key) == true && response[key] is bool value)
            return value;

        return false;
    }
}
