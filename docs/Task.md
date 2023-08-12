# Task

Represents a task entity in the Notion Task Sync system, containing metadata for synchronization between Notion and local file systems. The type tracks task state, ownership, priority, and temporal attributes while supporting soft deletion and validation.

## API

### `Id`
- **Type**: `Guid`
- **Purpose**: Unique identifier for the task, immutable after creation.
- **Usage**: Primary key for task identification and synchronization.

### `Title`
- **Type**: `required string`
- **Purpose**: Human-readable title of the task, enforced as non-null.
- **Constraints**: Must not exceed platform-specific length limits (enforced by `Validate`).

### `Description`
- **Type**: `string?`
- **Purpose**: Optional detailed description of the task.
- **Constraints**: May be null or empty.

### `NotionPageId`
- **Type**: `string?`
- **Purpose**: Identifier for the corresponding Notion page, if synchronized.
- **Constraints**: Must match Notion's page ID format when non-null.

### `LocalFilePath`
- **Type**: `string?`
- **Purpose**: Filesystem path for local task artifacts, if applicable.
- **Constraints**: Must be a valid filesystem path when non-null.

### `Status`
- **Type**: `TaskStatus`
- **Purpose**: Current state of the task (e.g., `Todo`, `InProgress`, `Done`).
- **Default**: `Todo` (implied by constructor initialization).

### `Priority`
- **Type**: `int`
- **Purpose**: Numeric priority level (higher values indicate higher priority).
- **Range**: Typically 0–10, though exact bounds are enforced by `Validate`.

### `CreatedAt`
- **Type**: `DateTime`
- **Purpose**: Timestamp of task creation, immutable after assignment.
- **Default**: Current UTC time on instantiation.

### `UpdatedAt`
- **Type**: `DateTime`
- **Purpose**: Timestamp of last modification, updated by `UpdateTimestamp`.
- **Default**: Equals `CreatedAt` on instantiation.

### `DueDate`
- **Type**: `DateTime?`
- **Purpose**: Optional deadline for task completion.
- **Constraints**: Must be in the future relative to `CreatedAt` when non-null.

### `CompletedAt`
- **Type**: `DateTime?`
- **Purpose**: Timestamp of completion, set by `Complete`.
- **Constraints**: Must be non-null only when `Status` is `Done`.

### `AssignedTo`
- **Type**: `string?`
- **Purpose**: Identifier for the user assigned to the task.
- **Constraints**: May be null or a valid user ID.

### `Tags`
- **Type**: `string?`
- **Purpose**: Comma-separated or structured list of task tags.
- **Constraints**: No enforced format; validated by `Validate`.

### `IsDeleted`
- **Type**: `bool`
- **Purpose**: Indicates whether the task is soft-deleted.
- **Default**: `false`.

### `DeletedAt`
- **Type**: `DateTime?`
- **Purpose**: Timestamp of soft deletion, set by `MarkAsDeleted`.
- **Constraints**: Must be non-null only when `IsDeleted` is `true`.

### `Validate`
- **Type**: `bool`
- **Purpose**: Flag indicating whether the task has passed validation checks.
- **Default**: `false` on instantiation; set to `true` after successful validation.
- **Side Effects**: Updates `UpdatedAt` if validation succeeds.

### `UpdateTimestamp`
- **Type**: `public void`
- **Purpose**: Updates `UpdatedAt` to the current UTC time.
- **Side Effects**: Modifies `UpdatedAt`; does not alter other fields.

### `Complete`
- **Type**: `public void`
- **Purpose**: Marks the task as completed, setting `Status` to `Done` and `CompletedAt` to the current UTC time.
- **Preconditions**: Task must not be soft-deleted (`IsDeleted == false`).
- **Side Effects**: Sets `Status`, `CompletedAt`, and updates `UpdatedAt`.

### `MarkAsDeleted`
- **Type**: `public void`
- **Purpose**: Soft-deletes the task, setting `IsDeleted` to `true` and `DeletedAt` to the current UTC time.
- **Preconditions**: Task must not already be soft-deleted (`IsDeleted == false`).
- **Side Effects**: Sets `IsDeleted`, `DeletedAt`, and updates `UpdatedAt`.

### `Clone`
- **Type**: `public Task Clone`
- **Purpose**: Creates a deep copy of the task with a new `Id`.
- **Return Value**: A new `Task` instance with identical field values except for `Id`, `CreatedAt` (set to current UTC time), and `UpdatedAt` (set to current UTC time).
- **Side Effects**: None on the original instance.

## Usage

### Example 1: Creating and Completing a Task
