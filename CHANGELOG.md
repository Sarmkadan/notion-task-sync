// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

# Changelog

All notable changes to Notion Task Sync are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [2.0.1] - 2026-12-29

### Security
- Added input validation and length limits for string parameters
- Added request timeout configuration
- Added security policy and vulnerability reporting

## [Unreleased]

### Planned Features
- REST API server mode for webhook integration
- GUI/TUI for interactive configuration
- Multi-database sync orchestration
- Advanced filtering and search capabilities
- Sync statistics dashboard
- Plugin system for custom formatters and handlers

---

## [2.0.0] - 2026-12-22

### Added
- **Stable release** - Production-ready bidirectional sync
- **Docker support** - Official Dockerfile and docker-compose.yml
- **Health check worker** - Background worker for continuous service monitoring
- **Rate limiting middleware** - Transparent enforcement of Notion API quota
- **.NET 10 upgrade** - Targeting net10.0 for all project components
- **Complete documentation** - Getting started, API reference, and deployment guides

### Changed
- Stabilized public API surface for library consumers
- Improved HTTP performance with connection pooling optimizations
- Version bump from .NET 9 to .NET 10

### Fixed
- Race condition in concurrent bidirectional sync operations
- Memory leak in HTTP client connection pooling under sustained load
- Incorrect timestamp comparisons when local files lack timezone info
- File handle not released in LocalFileService on read error

---

## [0.9.0] - 2025-09-08

### Added
- **RetryHelper** - Configurable exponential-backoff retry for transient failures
- **WebhookHandler** - Trigger a sync in response to an inbound HTTP event
- **ExternalApiWrapper** - Thin abstraction for third-party service calls
- Improved CLI argument parsing with positional and named parameter support

### Changed
- Refactored SyncService to use an explicit pipeline pattern
- Enhanced error messages now include actionable remediation steps
- Startup configuration validation surfaces problems before first API call

### Fixed
- Tasks deleted locally not always propagating a delete to Notion
- Configuration serialization edge case with deeply nested complex types
- SyncObserver not firing completion event after a partial failure

---

## [0.8.0] - 2025-08-01

### Added
- **Event bus** - Lightweight publish/subscribe system for sync lifecycle events
- **ConflictDetectedHandler** - Event handler that reacts to detected conflicts
- **SyncCompletedHandler** - Post-sync hooks for notifications and reporting
- **SyncObserver** - Decoupled monitoring without touching core sync logic

### Changed
- EventBus now supports typed, generic event subscriptions
- Middleware pipeline is fully composable via `IMiddleware` chain

### Fixed
- Event handlers not disposing correctly under sustained high-frequency events

---

## [0.7.0] - 2025-07-03

### Added
- **Caching layer** - TTL-based in-memory cache for Notion API responses
- **CacheProvider** - Pluggable backend supporting in-memory and persistent stores
- **Logging middleware** - Structured request/response logging with duration tracking
- **Error handling middleware** - Centralized exception management and formatting

### Changed
- HttpClientFactory now reuses connections across requests for lower overhead
- Warm cache reduces redundant API calls by up to 80% in steady-state usage

### Fixed
- Cache invalidation not triggering correctly after a task deletion

---

## [0.6.0] - 2025-06-09

### Added
- **Multiple export formats** - JSON, CSV, XML, and Markdown formatters
- **CsvFormatter** - Configurable delimiter and optional header row
- **XmlFormatter** - Standards-compliant XML serialization for task lists
- **MarkdownFormatter** - Human-readable task tables for documentation workflows

### Changed
- Formatter interface unified across all output types for consistent usage
- Export filenames now include an ISO timestamp by default

---

## [0.5.0] - 2025-05-14

### Added
- **BackupService** - Create, list, and restore point-in-time backup snapshots
- **Automatic pre-sync backups** - Opt-in safety net before each sync operation
- **ChangeLogRepository** - Persistent history of all sync operations and outcomes
- **DatabaseContext** - SQLite persistence for sync state across restarts

### Changed
- Repository pattern introduced to cleanly decouple storage from business logic

### Fixed
- Sync state was not persisting correctly across application restarts

---

## [0.4.0] - 2025-04-02

### Added
- **ConflictDiffService** - Compute side-by-side diffs before resolution
- **ConflictDiff model** - Structured representation of a conflicting change pair
- **Field-level strategy overrides** - Per-field resolution preferences in config
- **configure CLI command** - Interactive wizard for setting up a sync profile

### Changed
- Improved dependency injection wiring; all services registered via extension methods
- Restructured project into feature-focused directories

---

## [0.3.0] - 2025-03-10

### Added
- **Full conflict resolution strategy set** - manual, merge, local-priority, notion-priority
- **ConflictResolutionService** - Pluggable strategy implementations behind a single interface
- **SyncConfig model** - Serializable configuration with per-profile strategy selection
- **ValidationHelper** - Input validation utilities shared across services

### Changed
- Latest-wins strategy promoted from hard-coded default to explicit configurable option

### Security
- API key validated against Notion on startup; application refuses to start if invalid

---

## [0.2.0] - 2025-02-17

### Added
- **Change detection** - Timestamp-based diff between local task files and Notion state
- **Latest-wins conflict resolution** - Default strategy for resolving concurrent edits
- **NotionMapper and TaskMapper** - Bidirectional property mapping between formats
- **status CLI command** - Show pending changes and time of last successful sync
- **Structured logging** - Console and rotating file sinks

### Fixed
- Tasks with identical modification timestamps not being compared correctly
- Null reference in NotionApiService during first-run when database is empty

---

## [0.1.0] - 2025-01-20

### Added
- Initial prototype
- **Notion API client** - REST client with bearer token authentication
- **LocalFileService** - Read and write JSON task files from a configured directory
- **Basic sync workflow** - Unidirectional Notion to local synchronization
- **CLI interface** - `sync` and `help` commands
- **Configuration system** - appsettings.json with environment variable overrides
- **Console logging** - Basic output for sync progress and errors

---

## Version History Summary

| Version | Release Date | Status | .NET Version | Key Features |
|---------|--------------|--------|--------------|--------------|
| 1.0.0 | 2025-10-22 | Stable | .NET 10 | Docker, health checks, stable API |
| 0.9.0 | 2025-09-08 | Stable | .NET 9 | Retry, webhooks, pipeline refactor |
| 0.8.0 | 2025-08-01 | Stable | .NET 9 | Events, observer pattern |
| 0.7.0 | 2025-07-03 | Stable | .NET 9 | Caching, middleware |
| 0.6.0 | 2025-06-09 | Stable | .NET 9 | Export formats |
| 0.5.0 | 2025-05-14 | Stable | .NET 9 | Backup, persistence |
| 0.4.0 | 2025-04-02 | Stable | .NET 9 | Conflict diff, config wizard |
| 0.3.0 | 2025-03-10 | Stable | .NET 9 | Full conflict strategies |
| 0.2.0 | 2025-02-17 | Beta | .NET 9 | Change detection, latest-wins |
| 0.1.0 | 2025-01-20 | Alpha | .NET 9 | Initial prototype |

---

## Migration Guides

### Upgrading from 0.9.0 to 1.0.0

No breaking changes. Upgrade recommended for all users due to stability improvements and the .NET 10 upgrade.

```bash
# Backup before upgrading
dotnet run -- backup create --name "pre-upgrade-1.0.0"

# Update
git pull origin main
dotnet build -c Release

# Verify with a dry run
dotnet run -- sync --dry-run
```

### Upgrading from 0.4.0 to 0.5.0

**Changes:**
- Task model now includes sync-state fields (auto-migrated on first run)
- `appsettings.json` gains a new `Backup` section

**Migration Steps:**
1. Back up existing task files before upgrading
2. Add the `Backup` section to `appsettings.json`
3. Re-run sync to establish the new persistent sync state

---

## Installation from Specific Versions

```bash
# Clone a specific tag
git clone --branch v1.0.0 https://github.com/Sarmkadan/notion-task-sync.git

# Or check out from an existing clone
git checkout v1.0.0
```

---

## Contributors

- **Vladyslav Zaiets** - Author, CTO & Software Architect
- [Community contributors](https://github.com/Sarmkadan/notion-task-sync/graphs/contributors)

---

## Support & Troubleshooting

- [Documentation](./README.md)
- [Report issues](https://github.com/Sarmkadan/notion-task-sync/issues)
- [Discussions](https://github.com/Sarmkadan/notion-task-sync/discussions)
- [Contact](https://sarmkadan.com)

---

**Built by [Vladyslav Zaiets](https://sarmkadan.com) - CTO & Software Architect**
