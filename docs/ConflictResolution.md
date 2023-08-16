# ConflictResolution

Represents a synchronization conflict between a local task and its Notion counterpart, capturing details necessary to resolve discrepancies and track resolution status.

## API

### `Id`
- **Type**: `Guid`
- **Purpose**: Unique identifier for the conflict record.
- **Mutability**: Read-only; set at creation.

### `TaskId`
- **Type**: `Guid`
- **Purpose**: Identifier of the task involved in the conflict.
- **Mutability**: Required at creation; read-only thereafter.

### `ConflictType`
- **Type**: `ConflictType`
- **Purpose**: Categorizes the type of conflict (e.g., field mismatch, deletion, structural change).
- **Mutability**: Set at creation; read-only thereafter.

### `PropertyName`
- **Type**: `string?`
- **Purpose**: Name of the property in conflict (e.g., "Description", "DueDate").
- **Mutability**: Optional; may be `null` if conflict is not property-specific.

### `LocalValue`
- **Type**: `string?`
- **Purpose**: Local value of the conflicting property at the time of detection.
- **Mutability**: Optional; may be `null` if not applicable.

### `NotionValue`
- **Type**: `string?`
- **Purpose**: Notion value of the conflicting property at the time of detection.
- **Mutability**: Optional; may be `null` if not applicable.

### `ResolvedValue`
- **Type**: `string?`
- **Purpose**: Value selected as the resolution outcome.
- **Mutability**: Updated by `Resolve` or `MarkForManualReview`.

### `ResolutionMethod`
- **Type**: `ResolutionMethod`
- **Purpose**: Strategy used to resolve the conflict (e.g., "LocalWins", "NotionWins", "Merge").
- **Mutability**: Updated by `Resolve` or `MarkForManualReview`.

### `DetectedAt`
- **Type**: `DateTime`
- **Purpose**: Timestamp when the conflict was detected.
- **Mutability**: Set at creation; read-only thereafter.

### `ResolvedAt`
- **Type**: `DateTime?`
- **Purpose**: Timestamp when the conflict was resolved.
- **Mutability**: Updated by `Resolve` or `MarkForManualReview`; `null` if unresolved.

### `Status`
- **Type**: `ResolutionStatus`
- **Purpose**: Current state of the conflict (e.g., "Pending", "Resolved", "ManualReview").
- **Mutability**: Updated by `Resolve` or `MarkForManualReview`.

### `LocalModifiedAt`
- **Type**: `DateTime?`
- **Purpose**: Timestamp of the local modification that contributed to the conflict.
- **Mutability**: Optional; set at creation if available.

### `NotionModifiedAt`
- **Type**: `DateTime?`
- **Purpose**: Timestamp of the Notion modification that contributed to the conflict.
- **Mutability**: Optional; set at creation if available.

### `ResolutionNotes`
- **Type**: `string?`
- **Purpose**: Free-form notes explaining the resolution decision.
- **Mutability**: Updated by `Resolve` or `MarkForManualReview`.

### `ResolvedBy`
- **Type**: `string?`
- **Purpose**: Identifier or name of the entity (user/system) that resolved the conflict.
- **Mutability**: Updated by `Resolve` or `MarkForManualReview`.

### `Validate`
- **Type**: `bool`
- **Purpose**: Indicates whether the conflict record is valid and can be processed.
- **Mutability**: Read-only; derived from internal consistency checks.

### `Resolve(ResolutionMethod method, string? notes = null)`
- **Purpose**: Applies a resolution strategy to the conflict and updates state.
- **Parameters**:
  - `method`: Chosen resolution strategy.
  - `notes`: Optional resolution notes.
- **Mutations**:
  - Sets `ResolvedValue` based on `method`.
  - Updates `ResolutionMethod`, `ResolvedAt`, `Status`, `ResolutionNotes`, and `ResolvedBy`.
- **Throws**: `InvalidOperationException` if `Validate` is `false`.

### `MarkForManualReview(string? notes = null)`
- **Purpose**: Escalates the conflict for human intervention.
- **Parameters**:
  - `notes`: Optional context for reviewers.
- **Mutations**:
  - Sets `ResolutionMethod` to `ManualReview`.
  - Updates `Status` to `ManualReview`.
  - Sets `ResolvedAt` to current UTC time.
  - Updates `ResolutionNotes` and `ResolvedBy`.
- **Throws**: `InvalidOperationException` if `Validate` is `false`.

### `GetConflictSummary()`
- **Returns**: `string`
- **Purpose**: Generates a human-readable summary of the conflict.
- **Content**: Includes `ConflictType`, `PropertyName`, `LocalValue`, `NotionValue`, and timestamps.
- **Throws**: Never.

### `GetAge()`
- **Returns**: `TimeSpan`
- **Purpose**: Computes the age of the conflict from `DetectedAt` to now.
- **Throws**: Never.

## Usage

### Example 1: Automatic Conflict Resolution
