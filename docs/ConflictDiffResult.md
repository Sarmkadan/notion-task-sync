# ConflictDiffResult

Represents the result of a conflict resolution between a local task and its Notion counterpart, including detailed line-by-line differences and metadata for conflict tracking.

## API

### `ConflictId`
- **Purpose**: Uniquely identifies the conflict instance.
- **Type**: `Guid`
- **Return value**: A globally unique identifier for the conflict.

### `PropertyName`
- **Purpose**: The name of the property where the conflict occurred.
- **Type**: `string`
- **Return value**: The name of the conflicting property.

### `LocalValue`
- **Purpose**: The value of the property as stored locally.
- **Type**: `string?`
- **Return value**: The local value, or `null` if not applicable.

### `NotionValue`
- **Purpose**: The value of the property as stored in Notion.
- **Type**: `string?`
- **Return value**: The Notion value, or `null` if not applicable.

### `Lines`
- **Purpose**: A collection of line-by-line differences for the property.
- **Type**: `List<DiffLine>`
- **Return value**: A list of `DiffLine` objects detailing the differences.

### `GeneratedAt`
- **Purpose**: The timestamp when the conflict diff was generated.
- **Type**: `DateTime`
- **Return value**: The UTC timestamp of diff generation.

### `Text`
- **Purpose**: A human-readable summary of the conflict.
- **Type**: `string`
- **Return value**: A concise description of the conflict.

### `Kind`
- **Purpose**: The type of conflict detected.
- **Type**: `DiffLineKind`
- **Return value**: The `DiffLineKind` enum value indicating the nature of the conflict.

### `LocalLineNumber`
- **Purpose**: The line number in the local representation where the conflict starts.
- **Type**: `int?`
- **Return value**: The 1-based line number, or `null` if not applicable.

### `NotionLineNumber`
- **Purpose**: The line number in the Notion representation where the conflict starts.
- **Type**: `int?`
- **Return value**: The 1-based line number, or `null` if not applicable.

## Usage

### Example 1: Basic Conflict Resolution
