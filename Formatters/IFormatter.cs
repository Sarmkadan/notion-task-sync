#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

namespace NotionTaskSync.Formatters;

using System;
using System.Collections.Generic;
using NotionTaskSync.Domain.Models;

/// <summary>
/// Defines a contract for formatters that convert between object models and serialized formats.
/// Implementations must provide consistent exception handling and null safety guarantees.
/// </summary>
public interface IFormatter
{
    /// <summary>
    /// Formats a collection of tasks into the target format.
    /// </summary>
    /// <param name="tasks">Collection of tasks to format. Must not be null.</param>
    /// <returns>Formatted string representation of the tasks.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="tasks"/> is null.</exception>
    string FormatTasks(List<Task> tasks);

    /// <summary>
    /// Formats a single task into the target format.
    /// </summary>
    /// <param name="task">Task to format. Must not be null.</param>
    /// <returns>Formatted string representation of the task.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="task"/> is null.</exception>
    string FormatTask(Task task);
}
