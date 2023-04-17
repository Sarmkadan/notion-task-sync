// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace NotionTaskSync.Commands;

using NotionTaskSync.Cli;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

/// <summary>
/// Displays help information about the application and available commands.
/// Provides detailed usage instructions and examples to guide users.
/// Implements the standard --help / -h / help command pattern.
/// </summary>
public class HelpCommand : CliCommand
{
    private readonly ILogger<HelpCommand> _logger;

    public override string Description => "Display help information and usage examples";

    public HelpCommand(ILogger<HelpCommand> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Executes the help command and displays comprehensive usage information.
    /// </summary>
    public override async Task<int> ExecuteAsync(List<string> arguments, Dictionary<string, string> options)
    {
        Console.WriteLine(GetHelpText());
        _logger.LogInformation("Help displayed");
        return await Task.FromResult(0);
    }

    /// <summary>
    /// Generates comprehensive help text with all commands and examples.
    /// </summary>
    private string GetHelpText()
    {
        return @"
╔════════════════════════════════════════════════════════════════════════════╗
║                        NOTION TASK SYNC - Help                             ║
║                                                                             ║
║  Bidirectional sync between Notion databases and local task files           ║
║                                                                             ║
╚════════════════════════════════════════════════════════════════════════════╝

USAGE:
  notion-sync <command> [options] [arguments]

COMMANDS:
  sync                    Synchronize tasks between Notion and local files
  status                  Display current sync status and statistics
  configure               Configure API keys and sync settings
  help                    Display this help information

EXAMPLES:

  1. Initial setup:
     $ notion-sync configure
     Follow the interactive prompts to enter your Notion API key and database ID

  2. Check current status:
     $ notion-sync status

  3. Run a full sync:
     $ notion-sync sync --database-id your-db-id

  4. Sync with specific direction:
     $ notion-sync sync --database-id your-db-id --direction notion-to-local

SYNC OPTIONS:
  --database-id ID         Notion database ID (required)
  --direction DIR          Sync direction:
                           - bidirectional (default)
                           - local-to-notion
                           - notion-to-local
  --strategy STRATEGY      Conflict resolution strategy:
                           - last-write (default) - use most recent
                           - manual - prompt for decisions
                           - local-priority - prefer local changes
  --verbose                Show detailed operation logs
  --dry-run                Preview changes without applying
  --backup                 Create backup before sync (default: true)

STATUS OPTIONS:
  --database-id ID         Filter by specific database (optional)
  --verbose                Show detailed change history
  --json                   Output as JSON

CONFIGURATION OPTIONS:
  --api-key KEY            Notion API key (get from https://notion.so/my-integrations)
  --database-id ID         Notion database ID
  --task-directory PATH    Local directory for task files
  --sync-interval SECONDS  Sync interval in seconds (default: 300)
  --conflict-strategy STR  Default conflict resolution strategy

GLOBAL OPTIONS:
  --help, -h               Show this help message
  --version                Show application version
  --debug                  Enable debug logging

ENVIRONMENT VARIABLES:
  NOTION_API_KEY          Your Notion API key (alternative to --api-key)
  NOTION_DATABASE_ID      Your Notion database ID (alternative to --database-id)
  NOTION_SYNC_DIR         Local task directory (alternative to --task-directory)

CONFIGURATION FILE:
  Configuration is stored in appsettings.json in the current directory
  You can manually edit this file to adjust settings

CONFLICT RESOLUTION STRATEGIES:

  last-write: Uses the most recently modified version
    Advantages: Automatic, no user interaction needed
    Disadvantages: May lose intentional older edits

  manual: Prompts user for each conflict
    Advantages: Full control over each decision
    Disadvantages: Time-consuming for many conflicts

  local-priority: Always uses local version
    Advantages: Predictable, local edits always win
    Disadvantages: May lose remote updates

TROUBLESHOOTING:

  Problem: ""Invalid API key""
  Solution: Check your Notion API key at https://notion.so/my-integrations

  Problem: ""Database not found""
  Solution: Verify the database ID is correct and the integration has access

  Problem: Too many conflicts
  Solution: Review conflicts and resolve manually, or check for concurrent edits

SUPPORT:
  Documentation: https://github.com/vladyslav-zaiets/notion-task-sync
  Issues: https://github.com/vladyslav-zaiets/notion-task-sync/issues

";
    }
}
