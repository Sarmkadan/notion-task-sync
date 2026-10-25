/// <summary>
/// Defines a contract for formatters that convert a single task into a serialized string representation.
/// </summary>
public interface ITaskFormatter
    {
        /// <summary>
        /// Formats a single task into the target format.
        /// </summary>
        /// <param name="task">Task to format.</param>
        /// <returns>Formatted string representation of the task.</returns>
        string FormatTask(Task task);
    }


