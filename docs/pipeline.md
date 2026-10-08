# SyncPipeline

`SyncPipeline` (`Pipeline/SyncPipeline.cs`, namespace `NotionTaskSync.Pipeline`) runs an ordered list of `ISyncStep` objects against a shared `PipelineContext`. It is a standalone composition seam: it is not called by `SyncService` or `Program.Main`, and no code in the repository currently registers steps with it (see "Status" below).

## Components

| Type | Role |
|---|---|
| `SyncPipeline` | Holds the step list, runs it, and returns a `PipelineResult`. |
| `ISyncStep` | One stage. Exposes `Name`, `IsCritical`, and `Task<bool> ExecuteAsync(PipelineContext)`. |
| `PipelineContext` | Shared state for the run: `Data` dictionary (`GetData<T>` / `SetData<T>`) and `Messages` list (`AddMessage`, timestamped). |
| `PipelineResult` | Outcome of a run: `Success`, `ErrorMessage`, and `StepResults`. |
| `StepResult` | Per-step record: `StepName`, `Success`, `ExecutedAt` (UTC), `ErrorMessage`. |

## Stages

A run of `ExecuteAsync(context)` goes through these stages:

1. **Start.** Logs the step count. A new `PipelineResult` is created.
2. **Iterate steps in insertion order.** For each step, index `i` (1-based in logs):
   1. Logs `Executing step i/N` at Debug level.
   2. Awaits `step.ExecuteAsync(context)`.
   3. Appends a `StepResult` with the returned `bool` as `Success`.
   4. **Failure handling** when the step returned `false`:
      - `IsCritical == true`: sets `Success = false` and `ErrorMessage = "Critical step '<name>' failed. Pipeline aborted."`, logs an error, and stops the loop.
      - `IsCritical == false`: logs a warning and continues with the next step.
   5. **Exception handling** when the step throws:
      - Logs the exception, appends a failed `StepResult` with `ErrorMessage = ex.Message`.
      - `IsCritical == true`: sets `Success = false`, `ErrorMessage = "Exception in critical step '<name>': <message>"`, and stops the loop.
      - `IsCritical == false`: continues with the next step.
3. **Finalize.** If `ErrorMessage` is still `null`, `Success` is set to `true`. Logs the final outcome and step count, then returns the result.

Consequences of the rules above:

- A non-critical failure does not change `Success`. The run still ends with `Success = true`, and the failure is visible only in `StepResults` and logs.
- Steps after an aborting critical step are never executed and have no `StepResult`.
- `ExecuteAsync` does not rethrow step exceptions; they are captured in the result.

## Shared state

Steps communicate only through `PipelineContext`. `Data` holds references, so a mutable object written by one step is visible to later steps without copying. `Messages` is append-only and is not part of `PipelineResult`; the caller must read it from the context it passed in.

## Usage

```csharp
var pipeline = new SyncPipeline(logger);
pipeline.AddStep(new FetchStep());     // IsCritical = true
pipeline.AddStep(new NotifyStep());    // IsCritical = false

var context = new PipelineContext();
var result = await pipeline.ExecuteAsync(context);

if (!result.Success)
{
    Console.Error.WriteLine(result.ErrorMessage);
}
```

`AddStep` throws `ArgumentNullException` for a `null` step. `GetSteps()` returns a read-only view. `Clear()` removes all steps.

## Status and caveats

- **Not wired in.** No type in the repository references `SyncPipeline`, `ISyncStep`, or `PipelineContext` outside `Pipeline/SyncPipeline.cs`. The production sync path is `SyncService`. See `docs/ARCHITECTURE.md` ("Extension points").
- **Not thread-safe.** `AddStep`, `Clear`, `ExecuteAsync`, and direct access to `Data` or `Messages` must not run concurrently.
- **Re-runnable, but state is not reset.** Each `ExecuteAsync` call builds a new `PipelineResult`, but the `PipelineContext` you pass in keeps its `Data` and `Messages` between calls. Use a fresh context per run unless carrying state is intended.
- **Steps are not isolated.** A step that mutates shared `Data` affects every later step in the same run.
- `docs/SyncPipeline.md` describes a different API (for example `ExecuteAsync(CancellationToken)`, a `StepName` property on the pipeline, and `SetData`/`Data` on the pipeline itself). It does not match `Pipeline/SyncPipeline.cs`; trust the source file.
