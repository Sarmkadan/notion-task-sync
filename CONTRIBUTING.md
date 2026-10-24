# Contributing to Notion Task Sync

Thanks for your interest in contributing! This guide covers setup, testing, and the pull request process.

## Prerequisites

- **.NET 10 SDK** (`net10.0`; CI uses `10.0.x`) — [download](https://dotnet.microsoft.com/download)
- **Git**
- Optional: `make` (wrappers around the commands below), Docker (for `docker-compose.yml`)

Verify your SDK with `dotnet --version`; it should report `10.0.x`.

## Getting the Code

Fork the repository on GitHub, then clone your fork:

```bash
git clone https://github.com/<your-username>/notion-task-sync.git
cd notion-task-sync
```

## Project Layout

| Path | Contents |
|---|---|
| `NotionTaskSync.csproj` | Main app and library (the only project at the root) |
| `Services/`, `Domain/`, `Data/`, `Infrastructure/`, `Cli/`, `Commands/` | Core sync code and CLI |
| `tests/notion-task-sync.Tests/` | Unit tests (xUnit, FluentAssertions, Moq) |
| `tests/notion-task-sync.IntegrationTests/` | Integration tests |
| `benchmarks/notion-task-sync.Benchmarks/` | BenchmarkDotNet benchmarks |
| `examples/` | Standalone usage samples |
| `docs/` | Per-class docs and `ARCHITECTURE.md` |

The root project excludes `tests/`, `examples/`, and `benchmarks/` from compilation, so the test projects must be built and run explicitly.

## Restore, Build, and Run

```bash
dotnet restore
dotnet build -c Release --no-restore     # or: make build
dotnet run -- sync --dry-run             # no mutations
dotnet run -- --help
```

`make build` runs `clean` first; the plain `dotnet` commands do not.

## Running Tests

Run the unit tests by pointing at the test project. **`dotnet test` at the repository root does not run the suite**, because the root contains no test project.

```bash
dotnet build -c Release --no-restore
dotnet test tests/notion-task-sync.Tests/notion-task-sync.Tests.csproj -c Release

# Run a subset
dotnet test tests/notion-task-sync.Tests/notion-task-sync.Tests.csproj --filter "FullyQualifiedName~SyncServiceTests"

# Integration tests
dotnet test tests/notion-task-sync.IntegrationTests/notion-task-sync.IntegrationTests.csproj -c Release
```

`make test` runs `dotnet test --no-build` from the root, so build first. Add tests for every behavior change; new public classes need a matching `docs/<ClassName>.md`.

Formatting and style checks:

```bash
dotnet format                                   # or: make format
dotnet build /p:EnforceCodeStyleInBuild=true    # or: make lint
```

## Code Style

- Rules live in `.editorconfig`: 4-space indent, LF line endings, Allman braces, `_camelCase` private fields, `var` only when the type is apparent, usings outside the namespace.
- Every `.cs` file starts with `#nullable enable`, then the author header block, then a file-scoped `namespace NotionTaskSync.<Folder>;`.
- Put XML doc comments on public types and members.
- **Keep author headers intact.** If you edit a file that has one, leave it as it is.

## Branch Naming

Create a branch from an up-to-date `main`, named `<type>/<short-kebab-description>` with the same type you will use in the commit, for example:

- `feat/export-csv-formatter`
- `fix/null-due-date-sync`
- `docs/contributing-guide`

```bash
git checkout main && git pull
git checkout -b fix/null-due-date-sync
```

## Commit Messages

Use [Conventional Commits](https://www.conventionalcommits.org/):

```
<type>(<optional scope>): <short imperative summary>
```

Common types: `feat`, `fix`, `docs`, `refactor`, `test`, `chore`. Examples from this repo:

```
fix(sync): remove duplicate null guard in ExecuteSyncAsync
refactor(integration): add ArgumentNullException guards to WebhookHandler public methods
docs: add contributing guide
```

Keep the subject under about 72 characters. Use the body to explain *why* when it isn't obvious.

## Submitting a Pull Request

1. Keep PRs small and focused on one change. Split unrelated work into separate PRs.
2. Make sure the Release build and the unit tests pass locally first (see above).
3. Push your branch and open a PR against `main`.
4. Describe the change in the PR body: the problem, what you changed, and how you tested it. The PR template at `.github/PULL_REQUEST_TEMPLATE.md` is filled in automatically; complete its checklist.
5. CI must pass before a PR can be merged. The `CI` and `Build & Test` workflows run on every PR to `main` and build in Release on .NET `10.0.x`.
6. Address review comments with new commits, or squash them if the PR is small and the history is noisy.

## Reporting Issues

Use GitHub Issues for bugs and feature requests. Include:

- A clear, descriptive title.
- Steps to reproduce.
- Expected behavior and what actually happened.
- Relevant logs or stack traces (redact any Notion API keys or database IDs).

Report security vulnerabilities privately as described in [SECURITY.md](SECURITY.md), not in public issues.

## License

By contributing, you agree that your contributions are licensed under the project's [MIT License](LICENSE).
