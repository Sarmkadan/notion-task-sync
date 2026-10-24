# NotionSyncOptionsValidator

`NotionSyncOptionsValidator` implements `IValidateOptions<NotionSyncOptions>` and checks the `NotionSync` section at startup. It reports every failing property in one `OptionsValidationException`, and each message names the property as `NotionSync:<Property>` with the reason.

## Registration

Use `AddNotionTaskSync` from `DependencyInjectionExtensions`:

```csharp
services.AddNotionTaskSync(configuration);
```

This binds `NotionSyncOptions`, calls `ValidateOnStart()`, and registers the validator as a singleton.

## When validation runs

*   When `IOptions<NotionSyncOptions>.Value` is first resolved, the validator runs and throws `OptionsValidationException` if the section is invalid.
*   `ValidateOnStart()` runs the same check at host startup. It only takes effect when a generic host starts. `Program.cs` builds its container by hand and does not call `AddNotionTaskSync`, so nothing triggers the check there yet.

## Rules

| Property | Rule |
|---|---|
| `ApiToken` | Required; not null, empty, or whitespace. |
| `DatabaseId` | Required; 32 hex digits, or 8-4-4-4-12 dashed UUID. |
| `SyncInterval` | At least 1 minute. |
| `ConflictStrategy` | A known `ConflictResolutionStrategy` name, case-insensitive. |
| `LocalStorePath` | Required; must be an absolute path. |

## Example message

```
NotionSync:SyncInterval must be at least 1 minute; was 00:00:30.
```
