// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace NotionTaskSync.Commands;

using NotionTaskSync.Cli;
using NotionTaskSync.Infrastructure.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

/// <summary>
/// Configures application settings interactively or via command-line options.
/// Updates appsettings.json with API keys, database IDs, and sync preferences.
/// Validates settings before persisting to ensure application remains functional.
/// </summary>
public class ConfigureCommand : CliCommand
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<ConfigureCommand> _logger;

    public override string Description => "Configure application settings interactively";

    public override Dictionary<string, string> Options => new()
    {
        { "api-key", "Notion API key" },
        { "database-id", "Notion database ID" },
        { "task-directory", "Local tasks directory path" },
        { "sync-interval", "Default sync interval in seconds" },
        { "conflict-strategy", "Default conflict resolution strategy" }
    };

    public ConfigureCommand(IConfiguration configuration, ILogger<ConfigureCommand> logger)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Executes interactive configuration or applies provided options.
    /// If options are provided, uses them; otherwise prompts user interactively.
    /// </summary>
    public override async Task<int> ExecuteAsync(List<string> arguments, Dictionary<string, string> options)
    {
        try
        {
            _logger.LogInformation("Starting configuration setup");

            var settings = new ConfigurationSettings();

            // If options provided, use them; otherwise prompt interactively
            if (options.Count > 0)
            {
                settings = ApplyProvidedOptions(options);
            }
            else
            {
                settings = await PromptUserInteractively();
            }

            // Validate settings before saving
            if (!ValidateSettings(settings))
            {
                _logger.LogError("Configuration validation failed");
                return 1;
            }

            // Save settings to appsettings.json
            var success = await SaveSettingsAsync(settings);
            if (success)
            {
                _logger.LogInformation("Configuration saved successfully");
                Console.WriteLine("\n✓ Configuration updated successfully!");
                return 0;
            }

            _logger.LogError("Failed to save configuration");
            return 1;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Configuration command execution failed");
            return 1;
        }
    }

    /// <summary>
    /// Applies configuration options provided on the command line.
    /// </summary>
    private ConfigurationSettings ApplyProvidedOptions(Dictionary<string, string> options)
    {
        var settings = new ConfigurationSettings();

        if (options.ContainsKey("api-key"))
            settings.ApiKey = options["api-key"];

        if (options.ContainsKey("database-id"))
            settings.DatabaseId = options["database-id"];

        if (options.ContainsKey("task-directory"))
            settings.TaskDirectory = options["task-directory"];

        if (options.ContainsKey("sync-interval") && int.TryParse(options["sync-interval"], out var interval))
            settings.SyncIntervalSeconds = interval;

        if (options.ContainsKey("conflict-strategy"))
            settings.ConflictStrategy = options["conflict-strategy"];

        return settings;
    }

    /// <summary>
    /// Prompts user interactively for configuration values.
    /// Uses default values where possible to streamline setup.
    /// </summary>
    private async Task<ConfigurationSettings> PromptUserInteractively()
    {
        var settings = new ConfigurationSettings();

        Console.WriteLine("\n" + new string('=', 50));
        Console.WriteLine("Notion Task Sync - Configuration");
        Console.WriteLine(new string('=', 50) + "\n");

        // Prompt for API key
        Console.Write("Enter your Notion API key: ");
        settings.ApiKey = Console.ReadLine() ?? string.Empty;

        // Prompt for database ID
        Console.Write("Enter your Notion database ID: ");
        settings.DatabaseId = Console.ReadLine() ?? string.Empty;

        // Prompt for local directory
        Console.Write("Enter local tasks directory (default: ./tasks): ");
        var taskDir = Console.ReadLine() ?? string.Empty;
        settings.TaskDirectory = string.IsNullOrWhiteSpace(taskDir) ? "./tasks" : taskDir;

        // Prompt for sync interval
        Console.Write("Enter sync interval in seconds (default: 300): ");
        var interval = Console.ReadLine() ?? string.Empty;
        settings.SyncIntervalSeconds = int.TryParse(interval, out var sec) ? sec : 300;

        // Prompt for conflict strategy
        Console.Write("Select conflict strategy (last-write/manual/local-priority) [default: last-write]: ");
        var strategy = Console.ReadLine() ?? string.Empty;
        settings.ConflictStrategy = string.IsNullOrWhiteSpace(strategy) ? "last-write" : strategy;

        return settings;
    }

    /// <summary>
    /// Validates that all required settings are present and valid.
    /// </summary>
    private bool ValidateSettings(ConfigurationSettings settings)
    {
        if (string.IsNullOrWhiteSpace(settings.ApiKey))
        {
            _logger.LogError("API key is required");
            return false;
        }

        if (string.IsNullOrWhiteSpace(settings.DatabaseId))
        {
            _logger.LogError("Database ID is required");
            return false;
        }

        if (settings.SyncIntervalSeconds <= 0)
        {
            _logger.LogError("Sync interval must be positive");
            return false;
        }

        return true;
    }

    /// <summary>
    /// Saves configuration settings to appsettings.json file.
    /// Preserves existing settings while updating provided values.
    /// </summary>
    private async Task<bool> SaveSettingsAsync(ConfigurationSettings settings)
    {
        try
        {
            var appSettingsPath = "appsettings.json";

            // Read existing settings if they exist
            dynamic appSettings = new System.Dynamic.ExpandoObject();
            if (File.Exists(appSettingsPath))
            {
                var content = await File.ReadAllTextAsync(appSettingsPath);
                appSettings = JsonConvert.DeserializeObject<dynamic>(content) ?? appSettings;
            }

            // Update with new values
            appSettings["NotionApi"]["ApiKey"] = settings.ApiKey;
            appSettings["NotionApi"]["DatabaseIds"] = new[] { settings.DatabaseId };
            appSettings["AppSettings"]["LocalTasksDirectory"] = settings.TaskDirectory;
            appSettings["AppSettings"]["DefaultSyncIntervalSeconds"] = settings.SyncIntervalSeconds;
            appSettings["AppSettings"]["DefaultConflictStrategy"] = settings.ConflictStrategy;

            // Write updated settings
            var updatedJson = JsonConvert.SerializeObject(appSettings, Formatting.Indented);
            await File.WriteAllTextAsync(appSettingsPath, updatedJson);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving settings to file");
            return false;
        }
    }

    /// <summary>
    /// Internal class to hold configuration settings during setup.
    /// </summary>
    private class ConfigurationSettings
    {
        public string? ApiKey { get; set; }
        public string? DatabaseId { get; set; }
        public string TaskDirectory { get; set; } = "./tasks";
        public int SyncIntervalSeconds { get; set; } = 300;
        public string ConflictStrategy { get; set; } = "last-write";
    }
}
