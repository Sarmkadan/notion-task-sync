# AdvancedUsageExtensions

The `AdvancedUsageExtensions` static class provides advanced operational capabilities for the `notion-task-sync` library, including configuration validation, optimization, results analysis, and robust execution patterns. It facilitates complex workflows requiring pre-flight checks, performance tuning, and resilient task synchronization.

## API

### Methods in `AdvancedUsageExtensions`

#### `ValidateConfiguration(SyncConfig config)`
Validates the provided `SyncConfig` to ensure all required fields are set and logic constraints are met.
- **Returns:** A `SyncConfigValidationReport` containing the validation status and any identified issues.

#### `CreateOptimizedConfiguration(SyncConfig config)`
Generates an optimized version of the provided `SyncConfig` to enhance synchronization performance.
- **Returns:** An optimized `SyncConfig` instance.

#### `AnalyzeResults(IEnumerable<SyncService.SyncResult> results)`
Processes a collection of synchronization results to produce a comprehensive performance report.
- **Returns:** A `SyncAnalysisReport` summarizing task performance and efficiency.

#### `ExecuteWithRetryAsync(Func<Task<SyncService.SyncResult>> syncTask)`
Executes the provided synchronization task asynchronously with built-in retry logic for resilience against transient failures.
- **Returns:** A `Task<SyncService.SyncResult>` representing the result of the operation.
- **Throws:** `SyncFailedException` if the operation fails after all retries.

### `SyncConfigValidationReport` Properties

- `bool IsValid`: Indicates if the configuration is valid.
- `List<string> Issues`: A list of critical issues found during validation.
- `List<string> Warnings`: A list of potential issues or warnings.
- `List<string> Recommendations`: A list of suggested improvements for the configuration.

### `SyncAnalysisReport` Properties

- `int TotalTasks`: The total number of tasks processed.
- `int SyncedTasks`: The number of tasks successfully synced.
- `int Conflicts`: The number of conflicts encountered.
- `double DurationMs`: The total time taken in milliseconds.
- `double TasksPerSecond`: The processing throughput in tasks per second.
- `double AvgProcessingTimeMs`: The average time taken per task in milliseconds.
- `double SuccessRate`: The ratio of successful tasks.
- `double ConflictRate`: The ratio of conflicts encountered.
- `string EfficiencyRating`: A qualitative assessment of the sync efficiency.

### `SyncFailedException`

Represents an error that occurs when a synchronization operation fails definitively after retry attempts.

## Usage

### 1. Validating and Executing a Sync
```csharp
var config = new SyncConfig { /* ... */ };
var report = AdvancedUsageExtensions.ValidateConfiguration(config);

if (report.IsValid)
{
    var optimizedConfig = AdvancedUsageExtensions.CreateOptimizedConfiguration(config);
    try
    {
        var result = await AdvancedUsageExtensions.ExecuteWithRetryAsync(async () => await SyncService.SyncAsync(optimizedConfig));
        Console.WriteLine("Sync completed successfully.");
    }
    catch (SyncFailedException ex)
    {
        Console.WriteLine($"Sync failed: {ex.Message}");
    }
}
else
{
    foreach (var issue in report.Issues) Console.WriteLine($"Issue: {issue}");
}
```

### 2. Analyzing Sync Results
```csharp
var results = new List<SyncService.SyncResult> { /* ... */ };
var analysis = AdvancedUsageExtensions.AnalyzeResults(results);

Console.WriteLine($"Total Tasks: {analysis.TotalTasks}");
Console.WriteLine($"Efficiency Rating: {analysis.EfficiencyRating}");
```

## Notes

- **Thread Safety:** The methods within `AdvancedUsageExtensions` are designed to be thread-safe, assuming the provided configurations and result sets are managed appropriately according to standard C# concurrency practices.
- **Edge Cases:**
    - `ExecuteWithRetryAsync` will throw a `SyncFailedException` if the underlying operation fails after exhausting the retry policy.
    - If `ValidateConfiguration` encounters an empty or fundamentally broken `SyncConfig`, it may report critical `Issues` that prevent synchronization until corrected.
    - `AnalyzeResults` should be called with a non-null collection of `SyncResult` items to avoid unexpected behavior.
