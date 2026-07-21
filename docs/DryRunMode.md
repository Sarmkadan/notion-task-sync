# Dry-Run Mode Implementation

## Overview

The `--dry-run` mode allows users to compute and preview planned creates/updates/deletes without executing any mutation operations against the Notion API or local file system.

## Usage

### Command Line

```bash
# Run in dry-run mode
dotnet run -- --dry-run

# Short form
dotnet run -- -d

# Combined with other arguments
dotnet run -- --dry-run --some-other-flag
```

### Configuration File

You can also set the dry-run mode in `appsettings.json`:

```json
{
  "AppSettings": {
    "DryRun": true,
    ...
  }
}
```

The command-line flag takes precedence over the configuration file setting.

## Implementation Details

### 1. Program.cs Changes

- Added `ParseDryRunFlag()` method to parse `--dry-run` or `-d` command-line arguments
- Added logging messages to indicate when the application is running in dry-run mode
- Passes the dry-run flag to the `SyncConfig` instance before executing the sync

### 2. SyncConfig.cs Changes (Domain/Models/SyncConfig.cs)

- Added `IsDryRun` property (boolean, default: false)
- Property includes XML documentation explaining its purpose

### 3. SyncService.cs Changes (Services/SyncService.cs)

Modified the `ApplyChangesAsync()` method to skip mutation operations when `IsDryRun` is true:

#### Local to Notion Sync (Lines 147-182)
- **Update operations**: Skips `UpdatePageAsync()` calls and logs "DRY-RUN: Would update page..."
- **Archive operations**: Skips `UpdatePageAsync()` calls and logs "DRY-RUN: Would archive page..."
- **Create operations**: Skips `CreatePageAsync()` calls and logs "DRY-RUN: Would create new page..."

#### Notion to Local Sync (Lines 192-220)
- **Update operations**: Skips `_taskRepository.UpdateAsync()` calls and logs "DRY-RUN: Would update local task..."
- **Create operations**: Skips `_taskRepository.AddAsync()` calls and logs "DRY-RUN: Would create local task..."

### 4. AppSettings.cs Changes (Infrastructure/Configuration/AppSettings.cs)

- Already had `DryRun` property defined (line 38)
- Property includes XML documentation explaining its purpose

## Behavior

### When Dry-Run is Enabled:

1. **No API calls**: All Notion API mutation calls (`CreatePageAsync`, `UpdatePageAsync`, `ArchivePageAsync`) are skipped
2. **No file system changes**: All local file repository operations (`AddAsync`, `UpdateAsync`) are skipped
3. **Comprehensive logging**: Every planned mutation is logged with "DRY-RUN: Would..." prefix
4. **Statistics computed**: All change detection, conflict resolution, and statistics are computed as normal
5. **No actual changes**: Notion database and local task files remain unchanged

### When Dry-Run is Disabled (Normal Mode):

- All operations execute normally
- No "DRY-RUN" log messages are displayed
- Changes are applied to both Notion and local file system

## Use Cases

1. **Preview changes before executing**: Verify what will change before running a production sync
2. **Debug sync behavior**: Understand why certain changes are being made
3. **CI/CD integration**: Run sync in dry-run mode as part of automated testing
4. **Safety check**: Validate configuration before enabling automated syncs
5. **Training/onboarding**: Demonstrate sync behavior without affecting production data

## Example Output

```
Notion Task Sync Application Starting (DRY-RUN MODE - No mutations will be executed)
...
Starting sync from Notion to local tasks... (DRY-RUN - Only computing changes)
...
DRY-RUN: Would update page abc123 with task 456
DRY-RUN: Would create new page in database xyz789 for task 101
DRY-RUN: Would update local task def456 from page ghi789
...
DRY-RUN COMPLETED - No changes were applied to Notion or local files
```

## Testing

The implementation has been verified to:
- Compile successfully with `dotnet build`
- Parse command-line arguments correctly
- Skip all mutation operations when dry-run is enabled
- Log all planned operations with clear "DRY-RUN" prefixes
- Maintain backward compatibility (default behavior unchanged)

## Future Enhancements

Potential improvements for future iterations:
- Add `--dry-run` flag to configuration profiles in `appsettings.json`
- Include dry-run status in `SyncResult` output
- Add dry-run summary with counts of planned operations
- Support dry-run mode for specific sync directions only
