# NotionSyncOptions

`NotionSyncOptions` holds the startup settings for the Notion sync. It is bound from the `NotionSync` configuration section and checked by [`NotionSyncOptionsValidator`](NotionSyncOptionsValidator.md) when registered with `AddNotionTaskSync`.

## Configuration

```json
{
  "NotionSync": {
    "ApiToken": "<integration token>",
    "DatabaseId": "5f1c0a2e-7b3d-4c8e-9a61-2d4b8e0f7a10",
    "SyncInterval": "00:05:00",
    "ConflictStrategy": "LastWrite",
    "LocalStorePath": "/var/lib/notion-task-sync/tasks"
  }
}
```

## Properties

### SectionName
*   **Type**: `const string`
*   **Value**: `"NotionSync"`

### ApiToken
*   **Type**: `string?`
*   **Required**: yes. Must not be blank.

### DatabaseId
*   **Type**: `string?`
*   **Required**: yes. Must be a Notion UUID: 32 hex digits, or the 8-4-4-4-12 dashed form.

### SyncInterval
*   **Type**: `TimeSpan`
*   **Default**: `00:05:00`
*   **Constraint**: at least 1 minute.

### ConflictStrategy
*   **Type**: `string`
*   **Default**: `"LastWrite"`
*   **Constraint**: one of `LastWrite`, `LocalWins`, `NotionWins`, `Manual` (case-insensitive). Numeric values are rejected.

### LocalStorePath
*   **Type**: `string?`
*   **Required**: yes. Must be an absolute path (`Path.IsPathFullyQualified`).
