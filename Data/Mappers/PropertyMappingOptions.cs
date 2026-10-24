#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace NotionTaskSync.Data.Mappers;

using System.Collections.Generic;

/// <summary>
/// Property-mapping configuration checked by <see cref="PropertyMapper"/> at startup.
/// </summary>
public sealed class PropertyMappingOptions
{
    /// <summary>
    /// Gets or sets the mapping from local field names (for example <c>Title</c>) to Notion property names.
    /// </summary>
    public Dictionary<string, string> Properties { get; set; } = new();

    /// <summary>
    /// Gets or sets the mapping from local status names to Notion status option names.
    /// Stored as a list so that entries differing only by case remain visible to validation.
    /// </summary>
    public List<StatusNameMapping> StatusNames { get; set; } = new();

    /// <summary>
    /// A single local-to-Notion status name pair.
    /// </summary>
    public sealed class StatusNameMapping
    {
        /// <summary>
        /// Gets or sets the local status name.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the Notion status option name.
        /// </summary>
        public string NotionName { get; set; } = string.Empty;
    }
}
