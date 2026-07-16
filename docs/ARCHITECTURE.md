# Architecture

This document describes the actual architecture of Notion Task Sync as it exists in the code today. Everything below maps to real files in the repository - if a component is listed here, you can open it and read it.

## What the application is

A .NET console application (`Program.cs`) that performs a bidirectional synchronization between a Notion database and a local task store, then exits. There is no web server, no REST API, and no long-running host in the default entry point - one run equals one sync cycle. `Workers/SyncWorker.cs` exists for periodic execution but is not wired into `Program.cs` (see Extension points below).

## High-level flow

```
Program.cs
  └─ builds ServiceCollection by hand (Microsoft.Extensions.DependencyInjection)
  └─ DependencyInjection.ValidateConfiguration()  - fail fast on missing NotionApi:ApiKey
  └─ resolves SyncService and calls ExecuteSyncAsync(SyncConfig)

SyncService.ExecuteSyncAsync (Services/SyncService.cs)
  1. Validate SyncConfig
  2. Load state:
       local  -> ITaskRepository.GetAllAsync()
       notion -> NotionApiService.FetchPagesAsync() or, when config.LastSyncAt
                 is set, FetchPagesSinceAsync() (incremental, filtered by
                 last_edited_time on the Notion side)
  3. ChangeDetectionService.DetectLocalChanges / DetectNotionChanges
  4. ChangeDetectionService.DetectConflicts(localChanges, notionChanges)
  5. ConflictResolutionService.ResolveConflictsAsync(conflicts,
       config.ConflictStrategy, config.FieldConflictStrategies)
  6. ApplyChangesAsync - pushes/pulls according to SyncDirection
  7. Persist via ITaskRepository, update sync metadata, return SyncResult
```

Errors inside the cycle do not throw out of `ExecuteSyncAsync`; they are captured into `SyncResult.Status = Failed` plus `ErrorMessage`/`ErrorDetails`. Rationale: the caller always gets a structured result it can log or persist, and one failed cycle never crashes a scheduling host around it.

## Component breakdown

### Core sync path (registered in DI, actually executed)

| Component | File | Responsibility |
|---|---|---|
| `SyncService` | `Services/SyncService.cs` | Orchestrator. Owns the cycle above and the `SyncResult` type (nested class - it is the return shape of this service only, so it lives with it). |
| `ChangeDetectionService` | `Services/ChangeDetectionService.cs` | Comparison of local tasks vs Notion pages against `LastSyncAt`; produces change lists and conflicts. |
| `ConflictResolutionService` | `Services/ConflictResolutionService.cs` | Applies `ConflictResolutionStrategy` (`LastWrite`, `LocalWins`, `NotionWins`, `Manual`) with optional per-field overrides from `SyncConfig.FieldConflictStrategies`. `Manual` yields `ResolutionStatus.PendingReview` instead of blocking. |
| `ConflictDiffService` | `Services/ConflictDiffService.cs` | Field-level diffing used by conflict inspection (`ConflictCommand`). |
| `NotionApiService` | `Services/NotionApiService.cs` | Notion REST client: Bearer auth, pinned `Notion-Version: 2022-06-28`, cursor-based pagination, `System.Text.Json` parsing. Methods are `virtual` so tests can subclass instead of needing an HTTP fake. |
| `ITaskRepository` / `TaskRepository` | `Data/Repositories/` | Local task persistence behind an interface. |
| `IChangeLogRepository` / `ChangeLogRepository` | `Data/Repositories/` | History of sync operations. |
| `CalendarSyncService`, `BulkOperationService` | `Services/` | Secondary features surfaced through `CalendarCommand` and `BulkCommand`. |

All registrations live in `Infrastructure/Configuration/DependencyInjection.AddApplicationServices()`. Everything is a singleton - correct for a single-shot console process where there is no request scope to speak of.

### Domain layer (`Domain/`)

Plain models with no infrastructure dependencies: `Task`, `NotionPage`, `SyncConfig`, `ChangeLog`, `ConflictDiff`, `ConflictResolution`, plus the `SyncStatus` enum and the `SyncException` hierarchy. `SyncConfig` carries the knobs that drive the whole cycle: `Direction` (`Bidirectional`/`LocalToNotion`/`NotionToLocal`), `ConflictStrategy`, `FieldConflictStrategies`, `LastSyncAt`.

### CLI (`Cli/`, `Commands/`)

`CliArgumentParser` plus command classes (`SyncCommand`, `ConfigureCommand`, `StatusCommand`, `BulkCommand`, `CalendarCommand`, `ConflictCommand`, `HelpCommand`). `CalendarCommand`, `BulkCommand` and `ConflictCommand` are DI-registered; the default `Program.Main` currently runs a sync directly rather than dispatching through the parser.

### Supporting libraries (present, NOT on the default execution path)

These modules compile and are unit-tested, but nothing in `Program.cs`/`DependencyInjection.cs` registers or invokes them. They are building blocks for embedding scenarios (see `examples/`):

- `Pipeline/SyncPipeline.cs` - ordered `ISyncStep` execution with a shared `PipelineContext`; the intended seam for composing custom sync stages without touching `SyncService`.
- `Events/EventBus.cs` - in-process pub/sub (`Subscribe<T>`, `PublishAsync<T>`) with sample handlers in `Events/EventHandlers/`.
- `Caching/` - `CacheProvider` (in-memory, TTL) and `CacheKey` builder. Note: `NotionApiService` does not use it; caching is opt-in by the embedder.
- `Middleware/` - error handling, logging and rate limiting wrappers.
- `Workers/` - `SyncWorker` (loop + cancellation for periodic sync) and `HealthCheckWorker`.
- `Formatters/` - JSON/CSV/Markdown/XML export of tasks.
- `Utils/` - `RetryHelper` (retry with backoff), `CryptoHelper`, validation and string/date helpers.

Documenting this honestly matters: earlier revisions of this file described a layered system where the cache and rate limiter sat inside the API call path. They do not - wire them yourself if you need them.

## Key design decisions and trade-offs

1. **Single-shot process over hosted service.** Sync is idempotent per cycle and cheap to restart, so cron/systemd-timer/container-scheduler semantics are simpler and more robust than an always-on daemon. Trade-off: no in-process scheduling or webhook push; `SyncWorker` exists if that changes.

2. **Incremental fetch as a first-class mode.** `FetchPagesSinceAsync` pushes the `last_edited_time` filter to Notion instead of fetching everything and diffing locally. For large databases this is the difference between a handful of requests and hundreds. Trade-off: correctness depends on `LastSyncAt` being trustworthy; a lost timestamp degrades to a full sync, which is the safe direction.

3. **Concrete service classes, interfaces only at the persistence boundary.** `ITaskRepository`/`IChangeLogRepository` are interfaces because storage is the most likely thing to be swapped (SQLite vs files vs test doubles). The services are concrete with `virtual` members for test overrides - fewer indirection layers to read through, at the cost of slightly clumsier mocking. Deliberate: interface-per-class was rejected as ceremony.

4. **Conflict resolution is strategy-per-field capable.** A single global strategy is wrong in practice (you usually want `NotionWins` for status but `LocalWins` for notes). `SyncConfig.FieldConflictStrategies` overrides the global `ConflictStrategy` per field name. Trade-off: field names are strings, so a typo means the override silently never matches.

5. **Errors become data, not exceptions.** `SyncResult` carries status, counters (created/updated/deleted/unchanged/conflicted) and error details. Only configuration errors throw (`ConfigurationException`) - those are operator mistakes that should fail loudly before any I/O happens.

6. **Notion API version is pinned.** `Notion-Version: 2022-06-28` is a constant in `NotionApiService`. Upgrading is a conscious code change with a re-test, never an ambient breakage.

## Data flow (bidirectional apply)

`SyncService.ApplyChangesAsync` respects `SyncConfig.Direction`:

- **LocalToNotion / Bidirectional:** local tasks with `UpdatedAt > LastSyncAt` are pushed - existing pages via `UpdatePageAsync`, tasks without a `NotionPageId` via `CreatePageAsync` (the returned page id is written back to the task).
- **NotionToLocal / Bidirectional:** Notion pages with `LastEditedTime > LastSyncAt` are pulled - matched by `NotionPageId`, updated or created locally; `Archived` pages mark the local task deleted.

The linkage key between the two worlds is `Task.NotionPageId`.

## Extension points

- **New sync stage:** implement `ISyncStep`, compose via `SyncPipeline` (see `examples/`).
- **React to sync events:** `EventBus.Subscribe<T>` with the event types in `Events/SyncEvents.cs`.
- **Different storage:** implement `ITaskRepository` / `IChangeLogRepository`, swap the DI registration.
- **New export format:** add a formatter alongside `Formatters/JsonFormatter.cs` et al.
- **Periodic execution:** host `SyncWorker` in a generic host or wire it into `Program.cs`.

## Known limitations

- `Program.Main` runs one hardcoded "Default Sync" config; multi-profile support exists in the model (`SyncConfig`) but not in the entry point.
- `SyncService.GetSyncHistoryAsync` is a stub returning an empty list - history persistence is not implemented end-to-end.
- No rate limiting or caching on the live Notion call path (both exist as opt-in components, see above). Notion's ~3 req/s limit is currently respected only by virtue of sequential awaits.
- `Manual` conflict strategy parks conflicts as `PendingReview`; there is no interactive resolution loop in the console flow yet (`ConflictCommand` covers inspection).
- Deletion propagation local-to-Notion currently calls `UpdatePageAsync` for deleted tasks rather than archiving pages.

## Related docs

- [GETTING_STARTED.md](GETTING_STARTED.md) - setup and first sync
- [API_REFERENCE.md](API_REFERENCE.md) - public surface
- [DEPLOYMENT.md](DEPLOYMENT.md) - Docker / scheduling
