# SyncPipeline
`SyncPipeline` orchestrates a sequence of `ISyncStep` objects, allowing steps to be added, executed, and inspected while providing a shared data store and message collection for communication between steps.

## API
### SyncPipeline()
Creates an empty pipeline with default values for `Success`, `ErrorMessage`, `StepResults`, `Data`, and `Messages`. The pipeline is ready to receive steps via `AddStep`.

### void AddStep(ISyncStep step)
Adds `step` to the end of the pipeline’s step list.  
- **Parameters**  
  - `step`: The step to add; must not be `null`.  
- **Throws**  
  - `ArgumentNullException` if `step` is `null`.

### Task<PipelineResult> ExecuteAsync(CancellationToken cancellationToken = default)
Executes all added steps in order, awaiting each step’s asynchronous work.  
- **Parameters**  
  - `cancellationToken`: Optional token to cancel execution.  
- **Return Value**  
  - A `PipelineResult` summarizing outcome, including overall success, any error message, and per‑step results.  
- **Throws**  
  - `InvalidOperationException` if the pipeline contains no steps.  
  - `OperationCanceledException` if cancellation is requested.  
  - Propagates any exception thrown by a step (wrapped in the resulting `PipelineResult`).

### IReadOnlyList<ISyncStep> GetSteps()
Returns a read‑only view of the steps currently stored in the pipeline.  
- **Return Value**  
  - An `IReadOnlyList<ISyncStep>`; modifications to the list are not permitted.

### void Clear()
Removes all steps and resets runtime state (`Success`, `ErrorMessage`, `StepResults`, `Data`, `Messages`) to their initial values.  
- **Throws**  
  - None.

### Dictionary<string, object?> Data
A shared dictionary for storing arbitrary objects accessible by steps during execution. Keys are strings; values may be `null`.  
- **Remarks**  
  - The dictionary is instantiated when the pipeline is created and is never replaced; only its contents change.

### List<string> Messages
A collection of informational or diagnostic messages added via `AddMessage`.  
- **Remarks**  
  - The list is instantiated when the pipeline is created and is never replaced; only its contents change.

### T? GetData<T>(string key)
Retrieves a value of type `T` from `Data` associated with `key`.  
- **Parameters**  
  - `key`: The key to look up; must not be `null`.  
- **Return Value**  
  - The value cast to `T`, or `default(T)` if the key is absent or the stored value cannot be cast to `T`.  
- **Throws**  
  - `ArgumentNullException` if `key` is `null`.  
  - `InvalidCastException` if the stored value exists but is not compatible with `T` (unless `T` is `object?`).

### void SetData<T>(string key, T value)
Stores `value` in `Data` under `key`, overwriting any existing entry.  
- **Parameters**  
  - `key`: The key under which to store the value; must not be `null`.  
  - `value`: The value to store; may be `null`.  
- **Throws**  
  - `ArgumentNullException` if `key` is `null`.

### void AddMessage(string message)
Appends `message` to the `Messages` list.  
- **Parameters**  
  - `message`: The text to add; must not be `null` or empty.  
- **Throws**  
  - `ArgumentNullException` if `message` is `null`.  
  - `ArgumentException` if `message` is empty.

### bool Success
Gets whether the pipeline executed without error. Set internally by `ExecuteAsync`; `true` when no step failed and no error was recorded.

### string? ErrorMessage
Gets an error message if the pipeline execution failed; otherwise `null`. Populated by `ExecuteAsync` when a step throws or returns a failure.

### List<StepResult> StepResults
Gets the result of each step after execution, in the same order as the steps were added. Each `StepResult` contains the step’s outcome, any error, and execution timestamp.

### string StepName
Gets or sets the name of the pipeline. The property is marked `required`, meaning a non‑`null` value must be supplied after object creation (e.g., via object initializer).

### DateTime ExecutedAt
Gets the UTC timestamp of the most recent call to `ExecuteAsync`. If the pipeline has never been executed, the value is `DateTime.MinValue`.

## Usage
### Example 1: Basic pipeline execution
```csharp
var pipeline = new SyncPipeline { StepName = "Task Sync" };
pipeline.AddStep(new NotionFetchStep());
pipeline.AddStep(new TodoCreateStep());

var result = await pipeline.ExecuteAsync();

if (pipeline.Success)
{
    Console.WriteLine("Pipeline completed successfully.");
}
else
{
    Console.Error.WriteLine($"Pipeline failed: {pipeline.ErrorMessage}");
}
```

### Example 2: Sharing data and collecting messages
```csharp
var pipeline = new SyncPipeline { StepName = "Report Generation" };
pipeline.SetData("StartTime", DateTime.UtcNow);

pipeline.AddStep(new LogStep("Starting report"));
pipeline.AddStep(new ComputeMetricsStep());
pipeline.AddStep(new LogStep("Report computed"));

await pipeline.ExecuteAsync();

// Retrieve shared data
if (pipeline.GetData<DateTime>("StartTime") is var start && start != default)
{
    var elapsed = DateTime.UtcNow - start;
    pipeline.AddMessage($"Report generation took {elapsed.TotalSeconds:F2}s");
}

// Inspect messages
foreach (var msg in pipeline.Messages)
{
    Console.WriteLine(msg);
}
```

## Notes
- The class is **not thread‑safe**. Concurrent calls to `AddStep`, `Clear`, `ExecuteAsync`, or direct manipulation of `Data`/`Messages` from multiple threads may lead to undefined behavior. External synchronization is required if shared access is needed.
- `ExecuteAsync` may be called multiple times; each call resets `Success`, `ErrorMessage`, and `StepResults` before running the steps again.
- The `Data` dictionary holds references; storing mutable objects allows steps to share state, but modifications are visible to all subsequent steps without copying.
- `StepName` must be set before the pipeline is used; attempting to read it before assignment yields `null` and violates the `required` contract.
- If a step throws an exception, `ExecuteAsync` captures it, sets `Success` to `false`, stores the exception’s message in `ErrorMessage`, and records a failed `StepResult`. Execution stops at the first failing step unless individual steps handle exceptions internally.
