# MarkdownFormatter

A utility class for converting task data into Markdown-formatted strings, suitable for exporting or displaying task lists in a structured, human-readable format.

## API

### `public MarkdownFormatter`

Initializes a new instance of the `MarkdownFormatter` class. This constructor has no parameters and does not throw exceptions.

### `public string FormatTask(Task task)`

Formats a single task into a Markdown string.

- **Parameters**:
  - `task`: The task to format. Must not be `null`.
- **Return value**: A Markdown-formatted string representing the task.
- **Exceptions**:
  - Throws `ArgumentNullException` if `task` is `null`.

### `public string FormatTasks(IEnumerable<Task> tasks)`

Formats a collection of tasks into a single Markdown string, with each task separated by a blank line.

- **Parameters**:
  - `tasks`: The collection of tasks to format. Must not be `null`, and no element must be `null`.
- **Return value**: A Markdown-formatted string representing all tasks.
- **Exceptions**:
  - Throws `ArgumentNullException` if `tasks` is `null`.
  - Throws `ArgumentException` if any element in `tasks` is `null`.

### `public string FormatTasksAsTable(IEnumerable<Task> tasks)`

Formats a collection of tasks into a Markdown table, with each task represented as a row and properties as columns.

- **Parameters**:
  - `tasks`: The collection of tasks to format. Must not be `null`, and no element must be `null`.
- **Return value**: A Markdown-formatted table string with headers and aligned columns.
- **Exceptions**:
  - Throws `ArgumentNullException` if `tasks` is `null`.
  - Throws `ArgumentException` if any element in `tasks` is `null`.

## Usage
