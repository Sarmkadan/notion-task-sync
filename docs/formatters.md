# Formatters

This document describes the formatter contract used to render `Task` objects and the four implementations that exist in the `Formatters` folder: `CsvFormatter`, `JsonFormatter`, `MarkdownFormatter` and `XmlFormatter`.

## Contract

There is no single interface that all four formatters implement. The code currently has two interface declarations, and only one of them is in use.

### `IFormatter` (in use)

File: `Formatters/IFormatter.cs`, namespace `NotionTaskSync.Formatters`.

```csharp
public interface IFormatter
{
    string FormatTasks(List<Task> tasks);
    string FormatTask(Task task);
}
```

- `FormatTasks` renders a list of tasks. The argument must not be null.
- `FormatTask` renders a single task. The argument must not be null.
- Both throw `ArgumentNullException` for a null argument.
- Implemented by `CsvFormatter` and `JsonFormatter`.

### `ITaskFormatter` (declared, not implemented)

File: `Formatters/ITaskFormatter.cs`.

```csharp
public interface ITaskFormatter
{
    string FormatTask(Task task);
}
```

- Declares only `FormatTask`. It has no namespace declaration and no `using` directives.
- No class in the repository implements it, and no code references it.
- Treat it as a leftover. Do not write new code against it without first deciding whether to remove it or wire it up.

### Implicit contract

`MarkdownFormatter` and `XmlFormatter` expose the same method names as `IFormatter` but do not implement it:

| Formatter | Implements | `FormatTask` return type | `FormatTasks` signature |
|---|---|---|---|
| `CsvFormatter` | `IFormatter` | `string` | `string FormatTasks(List<Task>)` |
| `JsonFormatter` | `IFormatter` | `string` | `string FormatTasks(List<Task>)` |
| `MarkdownFormatter` | none | `string` | `string FormatTasks(List<Task>, string title = "Tasks")` |
| `XmlFormatter` | none | `XElement` | `string FormatTasks(List<Task>)` |

Notes on the differences:

- `XmlFormatter.FormatTask` returns an `XElement`, not a string. Its `FormatTasks` builds the document from those elements.
- `MarkdownFormatter.FormatTasks` takes an optional document title.
- `JsonFormatter` also exposes `FormatSyncConfig`, `Format<T>`, the `Deserialize*` methods, `IsValidJson`, `Minify` and `PrettyPrint`. These are outside the interface.
- `CsvFormatter` also exposes `ParseTasks`. `XmlFormatter` exposes `ParseTasks` and `IsValidXml`. `MarkdownFormatter` has no parser.

### Shared behavior

- Every `FormatTask` and `FormatTasks` method logs the error with `ILogger<T>` and rethrows it when formatting fails.
- Formatting does not write files. The caller decides where the output goes.

## Input model

The formatters operate on `NotionTaskSync.Domain.Models.Task`. The fields that appear in output are:

| Field | Type | Notes |
|---|---|---|
| `Id` | `Guid` | Local identifier |
| `Title` | `string` | Required |
| `Description` | `string?` | Optional |
| `NotionPageId` | `string?` | Optional |
| `Status` | `TaskStatus` | `Todo`, `InProgress`, `Done`, `Blocked`, `Archived` |
| `Priority` | `int` | 0 to 100 |
| `CreatedAt`, `UpdatedAt` | `DateTime` | UTC |
| `DueDate`, `CompletedAt` | `DateTime?` | Optional |
| `AssignedTo`, `Tags` | `string?` | Optional |
| `IsDeleted` | `bool` | Soft-delete flag |

The examples below use this task:

- Id `3f2b8c1e-5a4d-4e6f-9b7a-1c2d3e4f5a6b`
- Title `Prepare menu, "summer" edition`
- Description `Update prices, add vegan options`
- NotionPageId `a1b2c3d4e5f6`
- Status `InProgress`, Priority `70`
- CreatedAt `2026-10-01T09:00:00Z`, UpdatedAt `2026-10-05T14:30:00Z`
- DueDate `2026-10-15T00:00:00Z`
- AssignedTo `Marco`, Tags `menu,summer`
- IsDeleted `false`

## CsvFormatter

File: `Formatters/CsvFormatter.cs`. Implements `IFormatter`.

Header, always the first line:

```
Id,Title,Description,Status,Priority,CreatedAt,UpdatedAt,DueDate,CompletedAt,AssignedTo,Tags,IsDeleted
```

Each row has the same 12 columns in the same order. Timestamps use the round-trip `o` format. Null values become empty fields. A field that contains a comma, a double quote or a newline is wrapped in double quotes, and inner double quotes are doubled. Lines end with `\n` from `StringBuilder.AppendLine` on Linux, and with `\r\n` on Windows.

`FormatTask` returns the header and one row joined by `\n`. `FormatTasks` returns the header and one row per task, each line ending with a newline.

Example output of `FormatTasks` for the sample task:

```csv
Id,Title,Description,Status,Priority,CreatedAt,UpdatedAt,DueDate,CompletedAt,AssignedTo,Tags,IsDeleted
3f2b8c1e-5a4d-4e6f-9b7a-1c2d3e4f5a6b,"Prepare menu, ""summer"" edition","Update prices, add vegan options",InProgress,70,2026-10-01T09:00:00.0000000Z,2026-10-05T14:30:00.0000000Z,2026-10-15T00:00:00.0000000Z,,Marco,"menu,summer",False
```

Note: `Title`, `Description` and `Tags` are quoted because they contain commas, and `Title` also contains double quotes, which are doubled. The `CompletedAt` field is empty because the sample task is not completed. The timestamp digits shown are illustrative; the exact precision depends on the `o` format of the runtime value.

`CsvFormatter` also has `ParseTasks(string csv)`. It never throws and returns an empty list on errors.

## JsonFormatter

File: `Formatters/JsonFormatter.cs`. Implements `IFormatter`.

Serialization goes through `AppJsonSerializerContext`, a source-generated `JsonSerializerContext`. The property names and formatting come from that context's options, so check `Domain/Models/JsonSerializerContext.cs` for the exact shape. The output below is illustrative and was not generated by running the code.

`FormatTask` returns a single JSON object. `FormatTasks` returns a JSON array of such objects.

Example output of `FormatTask` for the sample task:

```json
{
  "id": "3f2b8c1e-5a4d-4e6f-9b7a-1c2d3e4f5a6b",
  "title": "Prepare menu, \"summer\" edition",
  "description": "Update prices, add vegan options",
  "notionPageId": "a1b2c3d4e5f6",
  "status": "InProgress",
  "priority": 70,
  "createdAt": "2026-10-01T09:00:00Z",
  "updatedAt": "2026-10-05T14:30:00Z",
  "dueDate": "2026-10-15T00:00:00Z",
  "completedAt": null,
  "assignedTo": "Marco",
  "tags": "menu,summer",
  "isDeleted": false
}
```

Other members outside the interface:

- `Minify` and `PrettyPrint` reformat an existing JSON string through `JsonDocument`.
- `IsValidJson` reports whether a string parses.
- `Deserialize<T>`, `DeserializeTask` and `DeserializeTasks` read JSON back into models. They return `null` on failure.

## MarkdownFormatter

File: `Formatters/MarkdownFormatter.cs`. Does not implement `IFormatter`.

`FormatTask` returns a level-2 heading with a status icon, a property table, an optional description, and blockquote lines with the IDs. The status icon is chosen by `GetStatusEmoji`:

| Status | Icon |
|---|---|
| `Done` | ✅ |
| `InProgress` | 🔄 |
| `Blocked` | ⛔ |
| `Archived` | 📦 |
| `Todo` and others | ⭕ |

Text fields are passed through `EscapeMarkdown`. Dates use the general short format `g` of the current culture.

Example output of `FormatTask` for the sample task (dates shown in the `g` format for `en-US`):

```markdown
## 🔄 Prepare menu, "summer" edition

| Property | Value |
|----------|-------|
| Status | `InProgress` |
| Priority | 70 |
| Created | 10/1/2026 9:00 AM |
| Updated | 10/5/2026 2:30 PM |
| Due Date | 10/15/2026 12:00 AM |
| Assigned To | Marco |
| Tags | menu,summer |

### Description
Update prices, add vegan options

> **Notion ID:** `a1b2c3d4e5f6`
> **Local ID:** `3f2b8c1e-5a4d-4e6f-9b7a-1c2d3e4f5a6b`
```

`FormatTasks(List<Task>, string title = "Tasks")` builds a document with a title and the `FormatTask` section for each task. `FormatTasksAsTable` produces a compact summary table with the columns Status, Title, Priority, Due Date and Assigned. Titles in that table are truncated to 30 characters.

Example output of `FormatTasksAsTable` for the sample task:

```markdown
| Status | Title | Priority | Due Date | Assigned |
|--------|-------|----------|----------|----------|
| 🔄 | Prepare menu, "summer" edition | 70 | 10/15/2026 | Marco |
```

The example is illustrative. The exact title text depends on `EscapeMarkdown` and `Truncate`, which were not reviewed in full for this document.

## XmlFormatter

File: `Formatters/XmlFormatter.cs`. Does not implement `IFormatter`.

`FormatTask` returns an `XElement` named `Task`. The `id` attribute holds the local ID. Child elements are `Title`, `Description`, `Status`, `Priority`, `CreatedAt`, `UpdatedAt`, and `IsDeleted`. `DueDate`, `CompletedAt`, `AssignedTo`, `Tags` and `NotionPageId` are added only when they have a value. Dates use the round-trip `O` format.

`FormatTasks` returns a string with an XML declaration and a `Tasks` root element. The root has `count` and `generated` attributes. The generated timestamp is the UTC time at formatting.

Example output of `FormatTasks` with one task:

```xml
<?xml version="1.0" encoding="utf-8" standalone="yes"?>
<Tasks count="1" generated="2026-10-08T10:00:00.0000000Z">
  <Task id="3f2b8c1e-5a4d-4e6f-9b7a-1c2d3e4f5a6b">
    <Title>Prepare menu, "summer" edition</Title>
    <Description>Update prices, add vegan options</Description>
    <Status>InProgress</Status>
    <Priority>70</Priority>
    <CreatedAt>2026-10-01T09:00:00.0000000Z</CreatedAt>
    <UpdatedAt>2026-10-05T14:30:00.0000000Z</UpdatedAt>
    <DueDate>2026-10-15T00:00:00.0000000Z</DueDate>
    <AssignedTo>Marco</AssignedTo>
    <Tags>menu,summer</Tags>
    <NotionPageId>a1b2c3d4e5f6</NotionPageId>
    <IsDeleted>false</IsDeleted>
  </Task>
</Tasks>
```

Note: `CompletedAt` is absent in this example because the sample task has no completion date. Timestamp digits are illustrative.

`XmlFormatter` also has `ParseTasks(string xml)` and `IsValidXml(string xml)`.

## Choosing a formatter

| Need | Formatter |
|---|---|
| Spreadsheet import, flat data | `CsvFormatter` |
| Machine-readable exchange, API payloads | `JsonFormatter` |
| Human-readable notes or review | `MarkdownFormatter` |
| Structured interchange with XML consumers | `XmlFormatter` |

## Known gaps

- `ITaskFormatter` is unused and has no namespace. It should either be removed or made the common contract for all four formatters. This document does not change it.
- `MarkdownFormatter` and `XmlFormatter` do not implement `IFormatter`, so they cannot be used interchangeably through that interface.
- The `FormatTask` return types differ between formatters (`string` versus `XElement`).
