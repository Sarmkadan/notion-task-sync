# SyncIntegrationTests
The `SyncIntegrationTests` class is designed to test the integration of the notion-task-sync project, specifically focusing on the synchronization workflow between local tasks and Notion pages. This class provides a set of test methods to ensure the correct functionality of the full sync workflow, including scenarios such as creating new tasks, handling multiple local tasks, resolving conflicts, and performing incremental syncs.

## API
* `public SyncIntegrationTests`: The constructor for the `SyncIntegrationTests` class, used to initialize the test environment.
* `public void Dispose`: Releases any resources held by the test class, ensuring a clean state after each test.
* `public async Task FullSyncWorkflow_CreatingNewTask_SyncsToNotion`: Tests the full sync workflow when creating a new task, verifying that it is successfully synced to Notion.
* `public async Task FullSyncWorkflow_MultipleLocalTasks_SyncsAllTasks`: Tests the full sync workflow with multiple local tasks, ensuring that all tasks are synced correctly.
* `public async Task FullSyncWorkflow_ConflictDetected_ResolvesConflict`: Tests the full sync workflow when a conflict is detected, verifying that the conflict is resolved correctly.
* `public async Task FullSyncWorkflow_BackupCreatedBeforeSync`: Tests the full sync workflow when a backup is created before the sync, ensuring that the backup does not interfere with the sync process.
* `public async Task FullSyncWorkflow_IncrementalSync_OnlyFetchesChangedPages`: Tests the incremental sync feature, verifying that only changed pages are fetched during the sync.
* `public async Task FullSyncWorkflow_SyncDirectionLocalToNotion_OnlyPushesChanges`: Tests the sync direction from local to Notion, ensuring that only changes are pushed to Notion.
* `public async Task FullSyncWorkflow_SyncDirectionNotionToLocal_OnlyPullsChanges`: Tests the sync direction from Notion to local, ensuring that only changes are pulled from Notion.

## Usage
The following examples demonstrate how to use the `SyncIntegrationTests` class:
```csharp
// Example 1: Testing the full sync workflow with a new task
var tests = new SyncIntegrationTests();
await tests.FullSyncWorkflow_CreatingNewTask_SyncsToNotion();
tests.Dispose();

// Example 2: Testing the incremental sync feature
var tests = new SyncIntegrationTests();
await tests.FullSyncWorkflow_IncrementalSync_OnlyFetchesChangedPages();
tests.Dispose();
```

## Notes
When using the `SyncIntegrationTests` class, consider the following edge cases and thread-safety remarks:
* The `Dispose` method should be called after each test to ensure resources are released and the test environment is cleaned up.
* The test methods are designed to be run asynchronously, allowing for concurrent execution and improving test performance.
* The `FullSyncWorkflow` test methods may throw exceptions if the sync workflow fails or if there are issues with the Notion API or local task storage.
* The `SyncIntegrationTests` class is not designed to be thread-safe, and concurrent access to the test methods may result in unpredictable behavior. It is recommended to run each test method sequentially to ensure accurate results.
