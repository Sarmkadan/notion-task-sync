# SyncServiceTests

Unit test suite for the `SyncService` class, validating synchronization behavior under various configuration, timing, conflict, and error scenarios.

## API

### `public SyncServiceTests`
Constructor for the test fixture. Initializes the test environment, including mocks for Notion client, configuration provider, and conflict resolver.

### `public async Task ExecuteSyncAsync_WithValidConfig_ReturnsCompletedStatus`
Tests that a valid configuration results in a successful sync with a `Completed` status.

- **Parameters**: None
- **Return value**: `Task` completing when the sync finishes
- **Throws**: Only test framework exceptions (e.g., `XunitException`)

### `public async Task ExecuteSyncAsync_WithInvalidConfig_ThrowsConfigurationException`
Verifies that an invalid configuration causes the sync to fail with a `ConfigurationException`.

- **Parameters**: None
- **Return value**: `Task` completing when the sync is attempted
- **Throws**: `ConfigurationException` with details of the invalid configuration

### `public async Task ExecuteSyncAsync_WithoutPreviousSyncTime_FetchesAllPages`
Ensures that when no prior sync timestamp exists, the service fetches all pages from Notion.

- **Parameters**: None
- **Return value**: `Task` completing after the fetch and sync operations
- **Throws**: Only test framework exceptions

### `public async Task ExecuteSyncAsync_WithPreviousSyncTime_FetchesIncrementalPages`
Validates that an existing sync timestamp triggers an incremental fetch of changed pages only.

- **Parameters**: None
- **Return value**: `Task` completing after the incremental sync
- **Throws**: Only test framework exceptions

### `public async Task ExecuteSyncAsync_WhenConflictsDetected_ResolvesConflicts`
Confirms that detected conflicts during sync are resolved using the configured resolver.

- **Parameters**: None
- **Return value**: `Task` completing after conflict resolution
- **Throws**: Only test framework exceptions

### `public async Task ExecuteSyncAsync_WhenNoChanges_ReturnsEmptyCounts`
Checks that a sync with no changes returns zero updated, created, and deleted counts.

- **Parameters**: None
- **Return value**: `Task` completing after the sync attempt
- **Throws**: Only test framework exceptions

### `public async Task ExecuteSyncAsync_WhenExceptionOccurs_CatchesAndReturnsFailedStatus`
Ensures that exceptions during sync are caught and result in a `Failed` status.

- **Parameters**: None
- **Return value**: `Task` completing after the exception is caught
- **Throws**: Only test framework exceptions

### `public async Task ExecuteSyncAsync_UpdatesSyncTimestamp`
Verifies that the sync timestamp is updated in persistent storage after a successful sync.

- **Parameters**: None
- **Return value**: `Task` completing after the timestamp update
- **Throws**: Only test framework exceptions

### `public async Task ExecuteSyncAsync_RecordsStartAndCompletionTimes`
Validates that the service records both start and completion timestamps for the sync operation.

- **Parameters**: None
- **Return value**: `Task` completing after the timestamps are recorded
- **Throws**: Only test framework exceptions

### `public async Task ExecuteSyncAsync_WithBidirectionalSync_AppliesChanges`
Tests that bidirectional sync mode correctly applies local changes to Notion and remote changes to the local store.

- **Parameters**: None
- **Return value**: `Task` completing after bidirectional changes are applied
- **Throws**: Only test framework exceptions

### `public void SyncResult_CalculatesDuration`
Unit test verifying that the `SyncResult.Duration` property accurately reflects the elapsed time between sync start and completion.

- **Parameters**: None
- **Return value**: `void`
- **Throws**: Only test framework exceptions

### `public void SyncResult_GeneratesSummary`
Unit test ensuring that the `SyncResult.Summary` property produces a human-readable summary of the sync outcome.

- **Parameters**: None
- **Return value**: `void`
- **Throws**: Only test framework exceptions

## Usage
