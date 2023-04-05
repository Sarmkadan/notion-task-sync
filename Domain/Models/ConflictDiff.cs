// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace NotionTaskSync.Domain.Models;

using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// The result of comparing two conflicting property values, structured as an ordered
/// sequence of annotated lines ready for terminal or UI rendering.
/// </summary>
public class ConflictDiffResult
{
    /// <summary>Gets or sets the identifier of the source <see cref="ConflictResolution"/>.</summary>
    public Guid ConflictId { get; set; }

    /// <summary>Gets or sets the name of the property whose values are being compared.</summary>
    public string PropertyName { get; set; } = string.Empty;

    /// <summary>Gets or sets the raw local (file-side) value that was compared.</summary>
    public string? LocalValue { get; set; }

    /// <summary>Gets or sets the raw Notion-side value that was compared.</summary>
    public string? NotionValue { get; set; }

    /// <summary>Gets or sets the ordered list of diff lines produced by the comparison.</summary>
    public List<DiffLine> Lines { get; set; } = new();

    /// <summary>Gets or sets the UTC timestamp when this diff was generated.</summary>
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Gets the number of lines present only in the Notion version.</summary>
    public int AddedCount => Lines.Count(l => l.Kind == DiffLineKind.Added);

    /// <summary>Gets the number of lines present only in the local version.</summary>
    public int RemovedCount => Lines.Count(l => l.Kind == DiffLineKind.Removed);

    /// <summary>
    /// Returns <see langword="true"/> when both sides are textually identical
    /// and the diff contains no additions or removals.
    /// </summary>
    public bool IsIdentical => AddedCount == 0 && RemovedCount == 0;
}

/// <summary>
/// A single annotated line within a diff view, carrying its text content,
/// change classification, and optional line-number references for both sides.
/// </summary>
public class DiffLine
{
    /// <summary>Gets or sets the text content of this line (without the leading sigil).</summary>
    public string Text { get; set; } = string.Empty;

    /// <summary>Gets or sets the kind of change this line represents.</summary>
    public DiffLineKind Kind { get; set; }

    /// <summary>Gets or sets the 1-based line number in the local (left) side, when applicable.</summary>
    public int? LocalLineNumber { get; set; }

    /// <summary>Gets or sets the 1-based line number in the Notion (right) side, when applicable.</summary>
    public int? NotionLineNumber { get; set; }

    /// <summary>
    /// Returns the unified-diff sigil character used when rendering this line as plain text:
    /// <c>+</c> for added, <c>-</c> for removed, <c>@</c> for hunk headers, and a space for context.
    /// </summary>
    public char Sigil => Kind switch
    {
        DiffLineKind.Added   => '+',
        DiffLineKind.Removed => '-',
        DiffLineKind.Header  => '@',
        _                    => ' '
    };
}

/// <summary>
/// Classifies the role of a single line within a conflict diff view.
/// </summary>
public enum DiffLineKind
{
    /// <summary>The line is unchanged and appears in both versions.</summary>
    Context = 0,

    /// <summary>The line exists only in the local version (removed from Notion's perspective).</summary>
    Removed = 1,

    /// <summary>The line exists only in the Notion version (added from Notion's perspective).</summary>
    Added = 2,

    /// <summary>A synthetic hunk-header line (e.g. <c>@@ -1,3 +1,4 @@</c>) inserted by the renderer.</summary>
    Header = 3
}
