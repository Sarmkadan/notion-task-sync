# TaskMapper

`TaskMapper` provides bidirectional conversion between Notion page representations, internal `Task` domain objects, and data‑transfer objects (`TaskDto`). It also defines the shape of a task entity with properties that mirror the fields stored in Notion and the application’s data model.

## API

### MapFromNotionPage(NotionPage page)
**Purpose** – Creates a new `Task` instance populated with data from a Notion page.  
**Parameters**  
- `page`: The source `NotionPage` to read values from.  
**Return Value** – A `Task` object whose properties (`Id`, `Title`, `Description`, `Status`, `Priority`, `CreatedAt`, `UpdatedAt`, `DueDate`, `AssignedTo`, `NotionPageId`, `IsDeleted`) are set according to the corresponding fields in `page`.  
**Exceptions**  
- `ArgumentNullException` if `page` is `null`.  
- `InvalidOperationException` if a required field (e.g., `Id` or `Title`) is missing or cannot be parsed.

### UpdateTaskFromPage(Task task, NotionPage page)
**Purpose** – Mutates an existing `Task` instance with the latest values from a Notion page.  
**Parameters**  
- `task`: The target `Task` to update.  
- `page`: The source `NotionPage` containing new values.  
**Return Value** – None.  
**Exceptions**  
- `ArgumentNullException` if either `task` or `page` is `null`.  
- `InvalidOperationException` if the Notion page contains data that cannot be applied to the task (e.g., an invalid priority value).

### MapToNotionPage(Task task)
**Purpose** – Produces a `NotionPage` representation suitable for sending to the Notion API from a `Task` object.  
**Parameters**  
- `task`: The source task to serialize.  
**Return Value** – A new `NotionPage` instance with its fields filled from the task’s properties.  
**Exceptions**  
- `ArgumentNullException` if `task` is `null`.  
- `InvalidOperationException` if any property required by Notion (e.g., `Title`) is `null` or empty.

### MapToDto(Task task)
**Purpose** – Maps a domain `Task` to a lightweight `TaskDto` used for API responses or internal messaging.  
**Parameters**  
- `task`: The task to convert.  
**Return Value** – A `TaskDto` containing the same logical data as `task`.  
**Exceptions**  
- `ArgumentNullException` if `task` is `null`.

### Id
**Purpose** – Unique identifier for the task, typically a GUID generated when the task is first created.  
**Type** – `Guid` (read/write).

### Title
**Purpose** – Human‑readable name of the task.  
**Type** – `string?` (read/write; may be `null` if not set).

### Description
**Purpose** – Detailed description or notes associated with the task.  
**Type** – `string?` (read/write).

### Status
**Purpose** – Current workflow status (e.g., “To Do”, “In Progress”, “Done”).  
**Type** – `string?` (read/write).

### Priority
**Purpose** – Relative importance of the task; higher numbers indicate higher priority.  
**Type** – `int` (read/write).

### CreatedAt
**Purpose** – Timestamp indicating when the task was originally created.  
**Type** – `DateTime` (read/write).

### UpdatedAt
**Purpose** – Timestamp indicating when the task was last modified.  
**Type** – `DateTime` (read/write).

### DueDate
**Purpose** – Optional deadline for the task.  
**Type** – `DateTime?` (read/write; `null` if no due date is set).

### AssignedTo
**Purpose** – Identifier of the user or team responsible for the task.  
**Type** – `string?` (read/write).

### NotionPageId
**Purpose** – The Notion page ID that backs this task; used for synchronization.  
**Type** – `string?` (read/write).

### IsDeleted
**Purpose** – Flag indicating whether the task has been logically deleted and should be ignored during sync.  
**Type** – `bool` (read/write).

## Usage

```csharp
// Example 1: Convert a Notion page to a domain task and then to a DTO.
NotionPage notionPage = await notionClient.GetPageAsync(pageId);
Task task = TaskMapper.MapFromNotionPage(notionPage);
TaskDto dto = TaskMapper.MapToDto(task);
// dto can now be returned from an API endpoint.
```

```csharp
// Example 2: Update an existing task with changes from Notion and push back.
Task existingTask = repository.GetTaskByNotionId(notionPageId);
TaskMapper.UpdateTaskFromPage(existingTask, notionPage);
NotionPage updatedPage = TaskMapper.MapToNotionPage(existingTask);
await notionClient.UpdatePageAsync(updatedPage);
```

## Notes

- All static mapping methods are pure functions; they do not rely on mutable internal state and are therefore thread‑safe when called concurrently with distinct arguments.  
- Instance properties are not thread‑safe; simultaneous reads and writes from multiple threads should be synchronized externally (e.g., using locks or concurrent collections) if the same `TaskMapper` instance is shared.  
- Mapping methods validate only for `null` arguments and obvious data‑format problems; they do not guarantee that the resulting object conforms to business rules beyond those encoded in the Notion schema. Callers should perform additional validation if required.  
- The `IsDeleted` flag is intended for soft‑delete semantics; when `true` the task should be excluded from active queries but retained for audit or possible restoration.  
- `DueDate` being `null` indicates that no deadline is assigned; consumers should treat this as “no due date” rather than a default value.
