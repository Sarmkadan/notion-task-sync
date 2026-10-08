# Conflict Detection and Resolution

This document describes how a sync conflict flows through the conflict-related services and models: diff generation (`ConflictDiffService`), resolution (`ConflictResolutionService`), the strategy enums (`ConflictStrategy` and `ConflictResolutionStrategy`), and the diff model (`ConflictDiffResult`, `DiffLine`, `DiffLineKind`).

Detection itself (`ChangeDetectionService.DetectConflicts`) is outside this document's scope. It is called from `SyncService` and produces the `ConflictResolution` objects that the rest of the flow consumes.

## Components

| Type | Location | Role |
|------|----------|------|
| `ConflictResolution` | `Domain/Models/ConflictResolution.cs` | Conflict record: local value, Notion value, timestamps, status, resolution method |
| `ResolutionMethod` / `ResolutionStatus` | `Domain/Models/ConflictResolution.cs` | Outcome enums stored on the record |
| `ConflictResolutionStrategy` | `Domain/Models/SyncConfig.cs` | Sync-level strategy: `LastWrite`, `LocalWins`, `NotionWins`, `Manual` |
| `ConflictStrategy` | `Services/ConflictStrategy.cs` | Per-call strategy used by `ResolveWith`: `PreferLocal`, `PreferRemote`, `Newest` |
| `ConflictResolutionService` | `Services/ConflictResolutionService.cs` | Applies a strategy to conflicts and writes audit entries |
| `ConflictDiffService` | `Services/ConflictDiffService.cs` | Builds an LCS line diff between local and Notion values, and renders it as unified-diff text |
| `ConflictDiffResult`, `DiffLine`, `DiffLineKind` | `Domain/Models/ConflictDiff.cs` | Diff model. Note that the file does not define a type named `ConflictDiff`. |
| `ConflictDiffServiceValidation` | `Services/ConflictDiffServiceValidation.cs` | Optional validation helpers. Nothing in the runtime path calls them. |

## Flow

```
ChangeDetectionService.DetectConflicts
        |
        v
List<ConflictResolution>  --->  SyncService (line ~130)
        |                              |
        |                              v
        |                  ConflictResolutionService.ResolveConflictsAsync
        |                  (config.ConflictStrategy + config.FieldConflictStrategies)
        |                              |
        |                              v
        |                  ChangeLog entry "LocalChangeDiscarded" (when applicable)
        |
        v
ConflictCommand (display path)
        |
        v
ConflictDiffService.GenerateDiffAsync -> RenderAsTextAsync
```

The resolution path and the diff path are independent. `ConflictResolutionService` does not call `ConflictDiffService`. The diff is produced for operator review. In `Commands/ConflictCommand.cs` (around lines 241-242), `GenerateDiffAsync` is followed by `RenderAsTextAsync`. The diff does not change which value wins.

## Diff generation (`ConflictDiffService`)

### Entry points

- `GenerateDiffAsync(ConflictResolution, ct)`: unwraps the conflict and calls `GenerateDiffForPropertyAsync`. The property label falls back to `"value"` when `PropertyName` is null. Exceptions are logged and rethrown. Cancellation is logged at debug level and rethrown.
- `GenerateDiffForPropertyAsync(localValue, notionValue, propertyName, conflictId, ct)`: the core. It takes raw strings and does not need a `ConflictResolution`.
- `RenderAsTextAsync(ConflictDiffResult, ct)`: renders a unified-diff string. It emits `--- local/<prop>` and `+++ notion/<prop>` headers, a `(no differences)` marker when the diff is identical, hunk headers `@@ -L +N @@` where the included regions have gaps, and a final `Summary: +X added, -Y removed` line.
- `GenerateBatchDiffsAsync(IReadOnlyList<ConflictResolution>, ct)`: runs `GenerateDiffAsync` sequentially and returns a dictionary keyed by conflict ID. A duplicate conflict ID overwrites the earlier entry.

### Algorithm

1. `SplitLines` converts `null` or empty input into an empty array. It normalizes `\r\n` to `\n` and splits on `\n`.
2. `ComputeLcs` builds a full `(m+1) x (n+1)` LCS table. Memory is O(m*n) in the number of lines on each side.
3. `BacktrackDiff` walks the table from the end and pushes lines onto a stack, then pops them into the result. Each line is one of:
   - `Context`: present on both sides, with both line numbers set.
   - `Removed`: local only, with `LocalLineNumber` set.
   - `Added`: Notion only, with `NotionLineNumber` set.
   When the two choices tie, the backtrack prefers `Added`.
4. `ComputeIncludedIndices` (used only by `RenderAsTextAsync`) marks every changed line plus `ContextLines` (3) lines on either side. Unchanged lines outside those windows are skipped in the rendered output.

### Model semantics

- `ConflictDiffResult.AddedCount` and `RemovedCount` count lines by `Kind`.
- `IsIdentical` is true when both counts are zero. Empty and identical inputs both produce this result.
- `DiffLine.Sigil` returns `+`, `-`, `@`, or a space. `Header` is never produced by the current algorithm. It exists for renderers.
- `GeneratedAt` is set to `DateTime.UtcNow`.

### Current behavior notes

- The method is declared `async`, but it does no real asynchronous work. It awaits `Task.CompletedTask` and runs synchronously.
- `GenerateDiffAsync` writes to the logger at debug level on entry and exit, and at error level on failure.
- `ConflictDiffServiceValidation.Validate(ConflictResolution)` would reject null `LocalValue` and `NotionValue`. `GenerateDiffAsync` does not call it, so null values are accepted and treated as empty.

## Resolution (`ConflictResolutionService`)

There are two resolution paths. They use different enums, and they behave differently.

### Path A: `ResolveConflictsAsync` (used by sync)

Input: `List<ConflictResolution>`, a `ConflictResolutionStrategy`, and an optional `Dictionary<string, ConflictResolutionStrategy>` of per-field overrides.

For each conflict:

1. The effective strategy is the per-field override, looked up by `PropertyName`, if one exists. Otherwise it is the global strategy.
2. The strategy is dispatched:

   | Strategy | Method | Result |
   |----------|--------|--------|
   | `LastWrite` | `ResolveByLastWrite` | Newer timestamp wins. Notion wins on a tie or within 1 ms. |
   | `LocalWins` | `ResolveLocalWins` | Local value, method `LocalWins` |
   | `NotionWins` | `ResolveNotionWins` | Notion value, method `NotionWins` |
   | `Manual` | `ResolveManual` | `MarkForManualReview`, method `Manual`, status `PendingReview` |
   | (any other value) | `ResolveByLastWrite` | Fallback |

3. If the local value was non-empty, the resolution is `Resolved`, the method is not `LocalWins`, and the resolved value differs from the local value, then `LogDiscardedLocalChangeAsync` writes a `ChangeLog` entry. The entry has `ChangeType = "LocalChangeDiscarded"`, `Source = System`, `IsConflict = true`, and the discarded and applied values in its description.
4. The resolution is appended to the output list.

Timestamp rules for `LastWrite`:

- A missing timestamp is treated as `DateTime.MinValue`.
- Notion wins if `notion > local + 1 ms`. Local wins if `local > notion + 1 ms`.
- Otherwise the tie-break applies: Notion wins, and the notes say so. This matches the comment "prefer Notion for consistency in distributed sync scenarios."

### Path B: `ResolveWith(ConflictResolution, ConflictStrategy)` (not used by sync)

This path takes the `ConflictStrategy` enum from `Services/ConflictStrategy.cs`.

- `PreferLocal` sets the local value with method `LocalWins`.
- `PreferRemote` sets the Notion value with method `NotionWins`.
- `Newest` (and any unknown value) uses timestamp comparison with the same 1 ms threshold and Notion tie-break as Path A. The method is `LastWrite`.

Differences from Path A:

- It does not write a discarded-change `ChangeLog` entry. A local edit can be dropped silently through this path.
- It does not support per-field overrides.
- It does not support `Manual`. The `ConflictStrategy` enum has no such value.
- `ResolveByLastWrite` and `ResolveWithNewest` contain near-identical logic. Only the notes text differs.

### Other members

- `ManuallyResolveAsync` does not load the conflict from storage. It builds a stub `ConflictResolution` with the given ID and `TaskId = Guid.Empty`, then resolves it. The in-code comment says the real implementation should fetch from storage.
- `MergeConflicts` is public and is not part of either strategy path. It selects a non-empty value when only one side has one, accepts identical values, and marks anything else for manual review.
- `GetPendingConflicts` returns conflicts where `IsPending()` is true.
- `GetResolutionStats` returns counts by `ResolutionStatus` and a resolution rate. It is `resolved / total`, or 0 when the list is empty.

## Strategy enums

| Enum | Values | Used by |
|------|--------|---------|
| `ConflictResolutionStrategy` (`SyncConfig.cs`) | `LastWrite`, `LocalWins`, `NotionWins`, `Manual` | `SyncService` via `config.ConflictStrategy` and `config.FieldConflictStrategies`; Path A |
| `ConflictStrategy` (`ConflictStrategy.cs`) | `PreferLocal`, `PreferRemote`, `Newest` | `ResolveWith`; Path B |

These enums overlap in meaning but are not mapped to each other. Configuration values use `ConflictResolutionStrategy`. Code that calls `ResolveWith` must use `ConflictStrategy`.

## Known gaps and inconsistencies

1. Two resolution paths with different behavior. Path B does not write the discarded-change audit entry that Path A writes.
2. Two different strategy enums with no mapping between them.
3. `ResolveByLastWrite` and `ResolveWithNewest` duplicate logic.
4. `ManuallyResolveAsync` does not read from storage.
5. The diff path is display-only. Nothing in the resolution path uses `ConflictDiffResult`.
6. `ConflictDiffServiceValidation` is opt-in and is not called on the runtime path.

## Related files

- `Services/ConflictDiffService.cs`
- `Services/ConflictResolutionService.cs`
- `Services/ConflictStrategy.cs`
- `Domain/Models/ConflictDiff.cs`
- `Domain/Models/ConflictResolution.cs`
- `Domain/Models/SyncConfig.cs`
- `Services/SyncService.cs`
- `Commands/ConflictCommand.cs`
