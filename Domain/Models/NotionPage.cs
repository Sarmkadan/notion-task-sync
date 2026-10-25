#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace NotionTaskSync.Domain.Models;

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

/// <summary>
/// Represents a Notion database page with its properties and metadata.
/// Serves as the bridge between Notion API responses and local task management.
/// </summary>
public class NotionPage
{
    /// <summary>
    /// Gets or sets the Notion page identifier (36 characters, UUID format).
    /// </summary>
    [Required]
    [StringLength(36)]
    public required string PageId { get; set; }

    /// <summary>
    /// Gets or sets the identifier (36 characters, UUID format) of the database that contains this page.
    /// </summary>
    [Required]
    [StringLength(36)]
    public required string DatabaseId { get; set; }

    /// <summary>
    /// Gets or sets the page title. Must not exceed 500 characters.
    /// </summary>
    [Required]
    [StringLength(500)]
    public required string Title { get; set; }

    /// <summary>
    /// Gets or sets the page property values keyed by property name.
    /// Values are stored as <see cref="object"/> and may be <c>null</c>.
    /// </summary>
    public Dictionary<string, object?>? Properties { get; set; }

    /// <summary>
    /// Gets or sets the UTC time when the page was created in Notion.
    /// </summary>
    public DateTime CreatedTime { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the UTC time when the page was last edited.
    /// </summary>
    public DateTime LastEditedTime { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the identifier of the user who created the page, if known.
    /// </summary>
    public string? CreatedBy { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the user who last edited the page, if known.
    /// </summary>
    public string? LastEditedBy { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the page is archived in Notion.
    /// </summary>
    public bool Archived { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the page needs to be refreshed from Notion.
    /// </summary>
    public bool IsStale { get; set; }

    /// <summary>
    /// Gets or sets the UTC time of the last successful synchronization, or <c>null</c> if the page has never been synced.
    /// </summary>
    public DateTime? LastSyncTime { get; set; }

    /// <summary>
    /// Gets or sets the public Notion URL of the page, if available.
    /// </summary>
    public string? Url { get; set; }

    /// <summary>
    /// Initializes a new NotionPage with required identifiers.
    /// </summary>
    /// <param name="pageId">The Notion page identifier.</param>
    /// <param name="databaseId">The identifier of the database that contains the page.</param>
    /// <param name="title">The page title.</param>
    [SetsRequiredMembers]
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
    /// <returns>
    /// <c>true</c> if <see cref="PageId"/> and <see cref="DatabaseId"/> are 36 characters long,
    /// and <see cref="Title"/> is non-blank and at most 500 characters; otherwise <c>false</c>.
    /// </returns>
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
    /// <typeparam name="T">The expected type of the property value.</typeparam>
    /// <param name="propertyName">The name of the property to retrieve.</param>
    /// <returns>
    /// The property value cast to <c>T</c>, or <c>default</c> if <see cref="Properties"/> is <c>null</c>,
    /// the property does not exist, or the value is not of type <c>T</c>.
    /// </returns>
    public T? GetProperty<T>(string propertyName)
    {
        if (Properties is null || !Properties.ContainsKey(propertyName))
            return default;

        var value = Properties[propertyName];
        return value is T typedValue ? typedValue : default;
    }

    /// <summary>
    /// Sets a property value in the Properties dictionary, updating the edit timestamp.
    /// </summary>
    /// <param name="propertyName">The name of the property to set.</param>
    /// <param name="value">The new property value; may be <c>null</c>.</param>
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
    /// Updates the last sync time to the current UTC time and clears the stale flag.
    /// </summary>
    public void UpdateSyncTime()
    {
        LastSyncTime = DateTime.UtcNow;
        IsStale = false;
    }

    /// <summary>
    /// Archives the page on the Notion side without deleting it.
    /// </summary>
    public void Archive()
    {
        Archived = true;
        LastEditedTime = DateTime.UtcNow;
    }

    /// <summary>
    /// Returns a summary string representation of the Notion page.
    /// </summary>
    /// <returns>A string containing the page identifier, title, archived flag, and stale flag.</returns>
    public override string ToString()
    {
        return $"NotionPage(Id={PageId}, Title={Title}, Archived={Archived}, Stale={IsStale})";
    }
}
