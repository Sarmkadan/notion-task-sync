# ConflictResolutionTests

The `ConflictResolutionTests` class contains unit tests for validating the conflict resolution logic in the `notion-task-sync` project. These tests verify how conflicts between task updates from different sources are detected, merged, or flagged for manual review based on predefined rules. The tests cover scenarios such as automatic resolution when values are identical, manual review triggers for conflicting modifications, and statistical reporting of resolution outcomes.

## API

### `Resolve_WhenCalled_SetsResolvedStatusAndMethod`
**Purpose**: Verifies that calling the `Resolve` method correctly updates the conflict status to "resolved" and records the resolution method used.
**Parameters**: None.
**Return Value**: None.
**Throws**: Does not throw exceptions under normal test execution. Test failures will raise assertion exceptions.

---

### `MarkForManualReview_WhenCalled_SetsPendingReviewStatusWithReason`
**Purpose**: Ensures that conflicts marked for manual review are assigned the correct status (`PendingReview`) along with a descriptive reason for the review requirement.
**Parameters**: None.
**Return Value**: None.
**Throws**: Does not throw exceptions under normal test execution. Test failures will raise assertion exceptions.

---

### `GetResolutionStats_WithMixedConflictStatuses_ReturnsCorrectResolutionRate`
**Purpose**: Tests that the `GetResolutionStats` method accurately calculates the resolution rate when provided with a mix of resolved and unresolved conflicts.
**Parameters**: None.
**Return Value**: None.
**Throws**: Does not throw exceptions under normal test execution. Test failures will raise assertion exceptions.

---

### `GetPendingConflicts_WithMixedConflictStatuses_ExcludesResolvedConflicts`
**Purpose**: Validates that the `GetPendingConflicts` method correctly filters out resolved conflicts, returning only those pending review or unresolved.
**Parameters**: None.
**Return Value**: None.
**Throws**: Does not throw exceptions under normal test execution. Test failures will raise assertion exceptions.

---

### `MergeConflicts_WhenBothSidesModifiedSamePropertyWithDifferentValues_MarksForManualReview`
**Purpose**: Confirms that when two sources modify the same property with differing values, the conflict is marked for manual review rather than being automatically resolved.
**Parameters**: None.
**Return Value**: None.
**Throws**: Does not throw exceptions under normal test execution. Test failures will raise assertion exceptions.

---

### `MergeConflicts_WhenBothSidesHaveIdenticalValues_ResolvesWithMergedMethod`
**Purpose**: Ensures that when two sources modify the same property with identical values, the conflict is automatically resolved using the merged method.
**Parameters**: None.
**Return Value**: None.
**Throws**: Does not throw exceptions under normal test execution. Test failures will raise assertion exceptions.

## Usage

### Example 1: Testing Automatic Resolution of Identical Values
