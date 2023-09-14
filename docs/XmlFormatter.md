# XmlFormatter

The `XmlFormatter` class provides utilities to serialize and deserialize collections of `Task` objects to and from XML format. It is designed to handle structured task data for synchronization purposes, ensuring consistent formatting and validation of XML task representations.

## API

### `public XmlFormatter()`

Initializes a new instance of the `XmlFormatter` class with default settings.

### `public XElement FormatTask(Task task)`

Serializes a single `Task` object into an `XElement`.

- **Parameters**
  - `task`: The `Task` to serialize.
- **Return Value**
  - An `XElement` representing the serialized task.
- **Exceptions**
  - Throws `ArgumentNullException` if `task` is `null`.

### `public string FormatTasks(List<Task> tasks)`

Serializes a list of `Task` objects into a single XML string.

- **Parameters**
  - `tasks`: The list of `Task` objects to serialize.
- **Return Value**
  - A string containing the XML representation of the tasks.
- **Exceptions**
  - Throws `ArgumentNullException` if `tasks` is `null`.

### `public List<Task> ParseTasks(string xml)`

Deserializes an XML string into a list of `Task` objects.

- **Parameters**
  - `xml`: The XML string to deserialize.
- **Return Value**
  - A `List<Task>` containing the deserialized tasks.
- **Exceptions**
  - Throws `ArgumentNullException` if `xml` is `null`.
  - Throws `XmlException` if the XML is malformed or invalid.

### `public bool IsValidXml(string xml)`

Validates whether the provided XML string is well-formed and adheres to the expected schema.

- **Parameters**
  - `xml`: The XML string to validate.
- **Return Value**
  - `true` if the XML is valid; otherwise, `false`.
- **Exceptions**
  - Throws `ArgumentNullException` if `xml` is `null`.

## Usage

### Example 1: Serializing a List of Tasks to XML
