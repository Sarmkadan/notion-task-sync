# CLI Commands

The `Commands/` folder contains one class per CLI verb. Each class derives from the abstract `CliCommand` base (declared in `Cli/CliArgumentParser.cs`) and implements:

* `Description` - one-line text shown in the generated help.
* `Options` - map of option name to description, shown under the command in help.
* `ExecuteAsync(List<string> arguments, Dictionary<string, string> options)` - returns an exit code (0 = success).

`CliArgumentParser` (in `Cli/`) turns raw `string[]` arguments into a `ParsedCommand` and produces the help text. It does not execute commands itself.

## Commands at a glance

| Command | Class | Purpose |
|---|---|---|
| `sync` | `SyncCommand` | Synchronize tasks between Notion and local files |
| `bulk` | `BulkCommand` | Run one bulk operation over many tasks |
| `calendar` | `CalendarCommand` | Export, import, or sync task due dates with an `.ics` file |
| `configure` | `ConfigureCommand` | Set application settings, interactively or via options |
| `conflict` | `ConflictCommand` | Review and resolve pending sync conflicts |
| `status` | `StatusCommand` | Show current sync status and statistics |
| `help` | `HelpCommand` | Print help and usage examples |

### SyncCommand

Runs a sync operation through `SyncService.ExecuteSyncAsync`.

| Option | Description |
|---|---|
| `--database-id` | Notion database ID to sync with (required) |
| `--direction` | `bidirectional`, `local-to-notion`, or `notion-to-local` (default: `bidirectional`) |
| `--strategy` | `last-write`, `manual`, or `local-priority` (default: `last-write`) |
| `--verbose` | Enable verbose logging |
| `--dry-run` | Preview changes without applying them |
| `--backup` | Create a backup before syncing (default: `true`) |

### BulkCommand

The first positional argument is the operation name. Operations are dispatched by a `switch` expression; an unknown name throws `ArgumentException`. With no positional argument, the command prints usage and returns 1.

Usage: `bulk <operation> [options]`

| Option | Description |
|---|---|
| `--ids` | Comma-separated task GUIDs |
| `--filter` | Filter expression, e.g. `status:done`, `tag:bug`, `assignee:alice` |
| `--status` | Target status for `update-status`: `todo`, `inprogress`, `done`, `blocked`, `archived` |
| `--tag` | Tag for `add-tag` or `remove-tag` (required by those operations) |
| `--assignee` | Assignee for `assign` |
| `--priority` | Integer 0-100 for `set-priority` |
| `--dry-run` | Print affected tasks without applying changes |

Validation errors (for example, missing `--status` for `update-status`, or a non-integer `--priority`) are raised as `ArgumentException`.

### CalendarCommand

Selects an action from `--action`:

* `export` - write task due dates to an `.ics` file.
* `import` - read events from an `.ics` file.
* `sync` - bidirectional sync with the file (default), via `RunBidirectionalSyncAsync`.

| Option | Description |
|---|---|
| `--action` | `export`, `import`, or `sync` (default: `sync`) |
| `--file` | Path to the `.ics` file (default: `notion-tasks.ics`) |
| `--verbose` | Enable verbose output |

### ConfigureCommand

Applies settings from options when given; otherwise runs an interactive prompt. Values are held in a private `ConfigurationSettings` object during setup. `ConfigureCommandExtensions` provides helpers for reading and validating the resulting configuration (API key, database ID, sync interval, conflict strategy, configuration file).

| Option | Description |
|---|---|
| `--api-key` | Notion API key |
| `--database-id` | Notion database ID |
| `--task-directory` | Local tasks directory (default: `./tasks`) |
| `--sync-interval` | Sync interval in seconds (default: 300) |
| `--conflict-strategy` | Default conflict strategy (default: `last-write`) |

### ConflictCommand

Opens the conflict resolution session (`ConflictResolutionService` and its UI).

| Option | Description |
|---|---|
| `--strategy` | Auto-resolve all conflicts: `local`, `notion`, or `last-write` |
| `--show-diff` | Show the diff before prompting (default: `true`) |
| `--json` | Output resolutions as JSON instead of prompts |
| `--limit` | Maximum number of conflicts to process in this session (default: all) |

### StatusCommand

Shows the current state of sync and statistics.

| Option | Description |
|---|---|
| `--database-id` | Filter by database ID (optional) |
| `--verbose` | Show detailed change history |
| `--json` | Output as JSON |

### HelpCommand

Prints the text from `GetHelpText()` and returns 0. `help`, `--help`, `-h`, and running with no arguments all route here.

## How CliArgumentParser routes input

`CliArgumentParser.Parse(string[] args)` works as follows:

1. **No arguments** - returns a `ParsedCommand` with `CommandName = "help"`.
2. **Command name** - `args[0]` is lowercased. `--help`, `-h`, and `help` return `CommandName = "help"`.
3. **Registration check** - the name is looked up in the dictionary filled by `RegisterCommand(name, command)`. If it is not registered, the result is `CommandName = "help"` with `Error = "Unknown command: '<name>'"`.
4. **Remaining tokens** are read from index 1:
   * `--key=value` sets `key` to `value`.
   * `--key value` takes the next token as the value, unless that token starts with `--`.
   * `--key` alone (or followed by another `--` token) stores `"true"`.
   * `-k value` works the same way for single-letter short options, but the value is skipped only if the next token starts with `-`.
   * Any other token is a positional argument.
5. Returns a `ParsedCommand` with `CommandName`, `Options`, `Arguments`, and optionally `Error`.

The parser does not call `ExecuteAsync`. The caller takes the `ParsedCommand`, looks up the registered `CliCommand` by `CommandName`, and calls `ExecuteAsync(parsed.Arguments, parsed.Options)`.

`GenerateHelpText()` builds the help screen from the registered commands. It prints each command's `Description` and `Options`, then the global options from `RegisterGlobalOption`, then fixed examples.

### Current wiring

In this repository, nothing outside `Cli/` calls `RegisterCommand`, `Parse`, or `GenerateHelpText`. `Program.cs` reads only the `--dry-run` flag itself and does not dispatch to these command classes. The command classes are therefore reachable only if a caller registers them and dispatches as described above.

## Adding a command

1. Create `Commands/<Name>Command.cs` deriving from `CliCommand`.
2. Override `Description`, `Options`, and `ExecuteAsync`.
3. Register it with `parser.RegisterCommand("<name>", command)`.
4. Return 0 on success and a non-zero code on failure.
