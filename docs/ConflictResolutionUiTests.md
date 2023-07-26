# ConflictResolutionUiTests

The `ConflictResolutionUiTests` class is dedicated to validating the logic and UI representation of conflict resolution mechanisms within the `notion-task-sync` project. It ensures that discrepancies between local and remote task data are accurately detected, diffed, and processed according to various resolution strategies, guaranteeing reliable synchronization behavior.

## API

*   **`GenerateDiff_WhenValuesAreIdentical_ReportsZeroAddedAndRemoved`**
    Verifies that the diffing algorithm correctly reports no additions or removals when comparing identical local and remote data fields.
*   **`GenerateDiff_WhenValuesAreDifferent_ReportsCorrectChangeCounts`**
    Validates that the change detection logic accurately calculates and reports the number of differences when local and remote values diverge.
*   **`GenerateDiff_WhenLocalValueIsNull_TreatsItAsEmpty`**
    Confirms that the system correctly handles null local values by treating them as empty inputs during the generation of a diff.
*   **`RenderAsText_ContainsDiffHeaderLines`**
    Ensures that the text-based UI representation of a generated conflict diff includes the required header lines for proper identification.
*   **`GenerateBatchDiffs_ReturnsEntryForEachConflict`**
    Verifies that when processing a batch of conflicts, the engine generates a distinct and correct diff entry for each individual conflict detected.
*   **`ResolveConflicts_WithLocalWinsStrategy_ResolvesAllWithLocalValue`**
    Tests the "Local Wins" resolution strategy to ensure that all conflicting fields are automatically resolved using the local state.
*   **`ResolveConflicts_WithManualStrategy_MarksConflictsForReview`**
    Validates that the "Manual" resolution strategy correctly flags conflicts for user review rather than applying automatic resolution.
*   **`ResolveConflicts_PerFieldOverride_AppliesToSpecifiedField`**
    Confirms that per-field resolution overrides are correctly applied and take precedence over general conflict resolution strategies.

## Usage

### Example 1: Verifying Manual Conflict Resolution
This example demonstrates invoking the manual strategy test to ensure conflicts are correctly flagged.

```csharp
public async Task RunManualStrategyValidation()
{
    var testSuite = new ConflictResolutionUiTests();
    
    // Execute the test to ensure conflicts are marked for manual review
    await testSuite.ResolveConflicts_WithManualStrategy_MarksConflictsForReview();
}
```

### Example 2: Verifying Diffing Accuracy
This example demonstrates executing multiple tests to validate the diff generation logic under different data scenarios.

```csharp
public async Task RunDiffingValidationSuite()
{
    var testSuite = new ConflictResolutionUiTests();
    
    // Verify behavior with identical values
    await testSuite.GenerateDiff_WhenValuesAreIdentical_ReportsZeroAddedAndRemoved();
    
    // Verify handling of null local values
    await testSuite.GenerateDiff_WhenLocalValueIsNull_TreatsItAsEmpty();
}
```

## Notes

*   **Execution Context**: All methods are defined as `async Task` and require an asynchronous execution environment. They are designed to be run by standard C# test runners (e.g., NUnit, xUnit).
*   **Test Isolation**: While the methods themselves do not maintain shared state within the class, they depend on the underlying synchronization infrastructure. They should be executed in an isolated manner to prevent cross-test interference, assuming the supporting infrastructure provides the necessary mock or test-specific state.
*   **Dependencies**: These tests implicitly require a configured environment with mocked Notion API services to simulate conflict scenarios without performing actual remote network requests.
