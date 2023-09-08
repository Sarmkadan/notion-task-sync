# CsvFormatter

Utility class for converting between collections of `Task` objects and CSV-formatted strings. Supports both serialization of task collections to CSV and parsing of CSV strings back into task collections.

## API

### `public CsvFormatter()`

Initializes a new instance of the `CsvFormatter` class. No external dependencies are required for instantiation.

### `public string FormatTasks(List<Task> tasks)`

Serializes a list of `Task` objects into a CSV-formatted string.

- **Parameters**
  - `tasks`: A list of `Task` objects to serialize. Must not be `null`.
- **Return value**
  - A string containing the CSV representation of the tasks, with each task on a new line and fields separated by commas. Fields containing commas or newlines are not escaped.
- **Exceptions**
  - Throws `ArgumentNullException` if `tasks` is `null`.

### `public string FormatTask(Task task)`

Serializes a single `Task` object into a CSV-formatted string.

- **Parameters**
  - `task`: The `Task` object to serialize. Must not be `null`.
- **Return value**
  - A string containing the CSV representation of the task, with fields separated by commas. Fields containing commas or newlines are not escaped.
- **Exceptions**
  - Throws `ArgumentNullException` if `task` is `null`.

### `public List<Task> ParseTasks(string csv)`

Parses a CSV-formatted string into a list of `Task` objects.

- **Parameters**
  - `csv`: The CSV-formatted string to parse. Must not be `null` or empty.
- **Return value**
  - A list of `Task` objects reconstructed from the CSV data. The order of tasks matches the order in the CSV input.
- **Exceptions**
  - Throws `ArgumentNullException` if `csv` is `null`.
  - Throws `ArgumentException` if `csv` is empty or contains malformed data that cannot be parsed into tasks.

## Usage
