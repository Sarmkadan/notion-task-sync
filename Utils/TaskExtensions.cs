#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace NotionTaskSync.Utils;

using System;

/// <summary>
/// Extension methods for asynchronous task operations.
/// Provides small, reusable helpers for working with <see cref="System.Threading.Tasks.Task"/>.
/// </summary>
public static class TaskExtensions
{
    /// <summary>
    /// Awaits the task, throwing a <see cref="TimeoutException"/> if it does not complete within the specified timeout.
    /// The underlying task is not cancelled when the timeout elapses.
    /// </summary>
    /// <typeparam name="T">The result type of the task.</typeparam>
    /// <param name="task">The task to await.</param>
    /// <param name="timeout">The maximum time to wait for the task to complete.</param>
    /// <returns>The result of the completed task.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="task"/> is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="timeout"/> is negative or greater than the maximum supported delay.</exception>
    /// <exception cref="TimeoutException">Thrown if the task does not complete within <paramref name="timeout"/>.</exception>
    public static async System.Threading.Tasks.Task<T> WithTimeout<T>(this System.Threading.Tasks.Task<T> task, TimeSpan timeout)
    {
        ArgumentNullException.ThrowIfNull(task);

        var delay = System.Threading.Tasks.Task.Delay(timeout);
        var completed = await System.Threading.Tasks.Task.WhenAny(task, delay).ConfigureAwait(false);

        if (completed != task)
            throw new TimeoutException($"Task did not complete within {timeout}.");

        return await task.ConfigureAwait(false);
    }
}
