// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace NotionTaskSync.Domain.Models;

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

/// <summary>
/// Represents a Notion database page with its properties and metadata.
/// Serves as the bridge between Notion API responses and local task management.
/// </summary>
public class NotionPage
{
    [Required]
    [StringLength(36)]
    public required string PageId { get; set; }

    [Required]
    [StringLength(36)]
    public required string DatabaseId { get; set; }

    [Required]
    [StringLength(500)]
    public required string Title { get; set; }

    public Dictionary<string, object?>? Properties { get; set; }

    public DateTime CreatedTime { get; set; } = DateTime.UtcNow;

    public DateTime LastEditedTime { get; set; } = DateTime.UtcNow;

    public string? CreatedBy { get; set; }

    public string? LastEditedBy { get; set; }

    public bool Archived { get; set; }

    public bool IsStale { get; set; }

    public DateTime? LastSyncTime { get; set; }

    public string? Url { get; set; }

    /// <summary>
    /// Initializes a new NotionPage with required identifiers.
    /// </summary>
    public NotionPage(string pageId, string databaseId, string title)
    {
        PageId = pageId;
        DatabaseId = databaseId;
        Title = title;
        Properties = new Dictionary<string, object?>();
    }

    /// <summary>
    /// Validates the Notion page ensuring all required identifiers are present.
    /// </summary>
    public bool Validate()
    {
        if (string.IsNullOrWhiteSpace(PageId) || PageId.Length != 36)
            return false;

        if (string.IsNullOrWhiteSpace(DatabaseId) || DatabaseId.Length != 36)
            return false;

        if (string.IsNullOrWhiteSpace(Title) || Title.Length > 500)
            return false;

        return true;
    }

    /// <summary>
    /// Retrieves a property value from the Properties dictionary with type safety.
    /// </summary>
    public T? GetProperty<T>(string propertyName)
    {
        if (Properties == null || !Properties.ContainsKey(propertyName))
            return default;

        var value = Properties[propertyName];
        return value is T typedValue ? typedValue : default;
    }

    /// <summary>
    /// Sets a property value in the Properties dictionary, updating the edit timestamp.
    /// </summary>
    public void SetProperty(string propertyName, object? value)
    {
        Properties ??= new Dictionary<string, object?>();
        Properties[propertyName] = value;
        LastEditedTime = DateTime.UtcNow;
    }

    /// <summary>
    /// Marks the page as stale indicating it needs to be refreshed from Notion.
    /// </summary>
    public void MarkAsStale()
    {
        IsStale = true;
    }

    /// <summary>
    /// Updates the last sync time and clears the stale flag.
    /// </summary>
    public void UpdateSyncTime()
    {
        LastSyncTime = DateTime.UtcNow;
        IsStale = false;
    }

    /// <summary>
    /// Archivees the page on Notion side without deletion.
    /// </summary>
    public void Archive()
    {
        Archived = true;
        LastEditedTime = DateTime.UtcNow;
    }

    /// <summary>
    /// Returns a summary string representation of the Notion page.
    /// </summary>
    public override string ToString()
    {
        return $"NotionPage(Id={PageId}, Title={Title}, Archived={Archived}, Stale={IsStale})";
    }
}
