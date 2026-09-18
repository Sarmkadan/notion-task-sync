# CLAUDE.md

.NET 10 console app + library for bidirectional sync between a Notion database and local task files, with change detection and conflict resolution. See `docs/ARCHITECTURE.md` for the full picture.

## Build

No `.sln` file - the root `NotionTaskSync.csproj` is the main project; `tests/`, `examples/`, `benchmarks/` are excluded from it via `<Compile Remove>` and have their own csproj files.

```bash
dotnet restore
dotnet build -c Release --no-restore     # or: make build
dotnet run -- sync                        # or: make run (sync --verbose via make run-dev)
dotnet run -- sync --dry-run              # no mutations; ./verify_dry_run.sh checks this
dotnet publish -c Release -o ./bin/publish
```

Docker: `make docker-build`, `docker-compose up -d` (needs `NOTION_API_KEY`, `NOTION_DATABASE_ID`).

## Test

xUnit + FluentAssertions + Moq in `tests/notion-task-sync.Tests/`.

```bash
dotnet test tests/notion-task-sync.Tests/notion-task-sync.Tests.csproj
dotnet test tests/notion-task-sync.Tests/ --filter "FullyQualifiedName~SyncServiceTests"
```

`make test` runs `dotnet test --no-build -c Release` from the root, so build first. CI (`.github/workflows/ci.yml`, `build.yml`) runs restore -> build Release -> test on .NET 10.0.x.

Benchmarks: `benchmarks/notion-task-sync.Benchmarks/` (BenchmarkDotNet).

## Lint / Format

```bash
dotnet format                                        # make format
dotnet build /p:EnforceCodeStyleInBuild=true         # make lint
```

Rules live in `.editorconfig`: 4-space indent, LF, Allman braces, `_camelCase` private fields, `var` only when type is apparent, usings outside namespace.

## Key directories

| Path | Role |
|---|---|
| `Program.cs` | Entry point (`NotionTaskSync.Program`). Builds DI by hand, validates config, runs one `SyncService.ExecuteSyncAsync` cycle, exits. |
| `Services/` | Core sync path: `SyncService` (orchestrator), `ChangeDetectionService`, `ConflictResolutionService`, `ConflictDiffService`, `NotionApiService` (REST client, `Notion-Version: 2022-06-28`), `LocalFileService`, `BackupService`, `BulkOperationService`, `CalendarSyncService`, `SyncCheckpointStore`. |
| `Domain/` | Plain models (`Task`, `NotionPage`, `SyncConfig`, `ChangeLog`, `ConflictDiff`, `ConflictResolution`), `Enums/SyncStatus`, `Exceptions/SyncException`. No infra deps. |
| `Data/` | `Repositories/` (`ITaskRepository`, `IChangeLogRepository` + impls), `Mappers/` (`TaskMapper`, `NotionMapper`), `Database/`. |
| `Infrastructure/Configuration/` | `DependencyInjection.AddApplicationServices()` - all registrations (singletons), `ValidateConfiguration()`, `AppSettings`, `NotionApiSettings`. |
| `Cli/`, `Commands/` | `CliArgumentParser` and command classes (`SyncCommand`, `ConfigureCommand`, `StatusCommand`, `BulkCommand`, `CalendarCommand`, `ConflictCommand`, `HelpCommand`). |
| `Pipeline/`, `Middleware/`, `Workers/`, `Events/`, `Observers/`, `Caching/`, `Collaboration/`, `Formatters/` | Supporting libraries; mostly not wired into the default `Program.Main` path (see ARCHITECTURE.md "Extension points"). |
| `appsettings.json` | Config; `NotionApi:ApiKey`, `NotionApi:DatabaseId`, `AppSettings:LocalTasksDirectory`. Env vars override. |
| `docs/` | Per-class docs (`docs/<ClassName>.md`) plus `ARCHITECTURE.md`, `docker-guide.md`, `migration-guide-v2.md`. |
| `examples/` | Standalone usage samples. |

## Conventions

- Every `.cs` file starts with `#nullable enable` and the author header block, then file-scoped `namespace NotionTaskSync.<Folder>;`, then usings.
- Namespaces mirror folders: `NotionTaskSync.Services`, `NotionTaskSync.Domain.Models`, `NotionTaskSync.Data.Repositories`, etc. Tests use `NotionTaskSync.Tests`.
- Domain `Task` collides with `System.Threading.Tasks.Task`; alias with `using Task = System.Threading.Tasks.Task;` and `using DomainTask = NotionTaskSync.Domain.Models.Task;`.
- Services are `sealed` classes with constructor-injected dependencies; `NotionApiService` methods are `virtual` so tests can subclass instead of faking HTTP.
- Helper logic is split into partial-style companion files: `<Class>Extensions.cs`, `<Class>Validation.cs`, `<Class>JsonExtensions.cs` next to the main class. Same pattern in tests (`<Class>Tests.cs`, `<Class>TestsValidation.cs`, `<Class>TestsExtensions.cs`).
- Sync errors do not throw out of `ExecuteSyncAsync`; they are returned as `SyncResult.Status = Failed` with `ErrorMessage`/`ErrorDetails`.
- JSON via `System.Text.Json` with a source-generated `Domain/Models/JsonSerializerContext.cs`.
- XML doc comments on public types and members (`GenerateDocumentationFile` is on).
- New public class -> add `docs/<ClassName>.md`. Commit messages follow Conventional Commits (`fix:`, `docs:`, `chore(scope):`).
