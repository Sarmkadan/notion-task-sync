# BulkOperationServiceTests

Unit tests for the `BulkOperationService` class, verifying bulk operations on Notion tasks including status updates, tag management, assignee assignment, priority setting, and soft deletion. The test suite ensures correct behavior for both existing and missing tasks, duplicate handling, and edge cases like empty assignee values or out-of-range priorities.

## API

### `UpdateStatus_SetsStatusOnAllMatchedTasks`
Verifies that the service correctly updates the status of all tasks matching the provided filter criteria. The test asserts that the status is applied uniformly across the matched tasks.

### `UpdateStatus_SkipsMissingTasks`
Ensures that tasks not found in the system are skipped without throwing exceptions during bulk status updates. The test validates that the operation completes successfully even when some tasks are absent.

### `AddTag_AppendTagToTasksWithoutDuplicates`
Confirms that the specified tag is appended to all matching tasks, and that duplicate tags are not added to tasks already containing the tag. The test checks that each task receives the tag exactly once.

### `AddTag_SkipsTaskAlreadyHavingTag`
Validates that tasks already containing the specified tag are skipped during bulk tag addition. The test ensures no redundant operations are performed on tasks that already have the tag.

### `RemoveTag_RemovesTagFromTask`
Tests that the specified tag is removed from all matching tasks. The test verifies that the tag is deleted only from tasks where it exists, and that the operation does not affect tasks lacking the tag.

### `Assign_SetsAssigneeProperly`
Checks that the assignee is correctly assigned to all tasks matching the filter criteria. The test asserts that the assignee field is updated as expected for each matched task.

### `Assign_ClearsAssigneeWhenEmptyStringProvided`
Ensures that providing an empty string as the assignee clears the assignee field for all matched tasks. The test validates that the operation behaves correctly when clearing assignees.

### `SetPriority_UpdatesPriorityOnAllTasks`
Verifies that the priority is updated on all tasks matching the filter criteria. The test asserts that the priority value is applied uniformly across the matched tasks.

### `SetPriority_ThrowsForOutOfRangePriority`
Confirms that the service throws an exception when an invalid priority value (outside the allowed range) is provided. The test ensures proper validation of priority input.

### `Delete_SoftDeletesAllMatchedTasks`
Tests that all tasks matching the filter criteria are soft-deleted. The test verifies that the tasks are marked as deleted without being permanently removed from the system.

### `Query_ReturnsOnlyMatchingNonDeletedTasks`
Ensures that the query operation returns only tasks matching the filter criteria and excludes any soft-deleted tasks. The test validates that the query respects both the filter and the soft-deletion status.

## Usage
