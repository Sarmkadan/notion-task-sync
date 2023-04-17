// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace NotionTaskSync.Cli;

using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Parses command-line arguments into structured command objects.
/// Handles validation, normalization, and help text generation.
/// Designed to work without external dependencies for maximum compatibility.
/// </summary>
public class CliArgumentParser
{
    private readonly Dictionary<string, CliCommand> _registeredCommands = new();
    private readonly Dictionary<string, string> _globalOptions = new();

    /// <summary>
    /// Registers a command that can be invoked from the CLI.
    /// </summary>
    public void RegisterCommand(string name, CliCommand command)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Command name cannot be null or empty", nameof(name));

        _registeredCommands[name.ToLowerInvariant()] = command;
    }

    /// <summary>
    /// Registers a global option that applies to all commands.
    /// </summary>
    public void RegisterGlobalOption(string name, string description)
    {
        _globalOptions[name] = description;
    }

    /// <summary>
    /// Parses command-line arguments and returns an executable command.
    /// Returns null if parsing fails or help is requested.
    /// </summary>
    public ParsedCommand? Parse(string[] args)
    {
        if (args == null || args.Length == 0)
        {
            return new ParsedCommand
            {
                CommandName = "help",
                Options = new Dictionary<string, string>(),
                Arguments = new List<string>()
            };
        }

        var commandName = args[0].ToLowerInvariant();

        // Handle help requests
        if (commandName == "--help" || commandName == "-h" || commandName == "help")
        {
            return new ParsedCommand
            {
                CommandName = "help",
                Options = new Dictionary<string, string>(),
                Arguments = new List<string>()
            };
        }

        // Check if command is registered
        if (!_registeredCommands.ContainsKey(commandName))
        {
            return new ParsedCommand
            {
                CommandName = "help",
                Options = new Dictionary<string, string>(),
                Arguments = new List<string>(),
                Error = $"Unknown command: '{commandName}'"
            };
        }

        var options = new Dictionary<string, string>();
        var arguments = new List<string>();

        // Parse remaining arguments
        for (int i = 1; i < args.Length; i++)
        {
            var arg = args[i];

            // Handle options (--key=value or --key value)
            if (arg.StartsWith("--"))
            {
                var optionName = arg.Substring(2);
                string? optionValue = null;

                if (optionName.Contains("="))
                {
                    var parts = optionName.Split('=', 2);
                    optionName = parts[0];
                    optionValue = parts[1];
                }
                else if (i + 1 < args.Length && !args[i + 1].StartsWith("--"))
                {
                    optionValue = args[i + 1];
                    i++;
                }

                options[optionName] = optionValue ?? "true";
            }
            // Handle short options (-k value)
            else if (arg.StartsWith("-") && arg.Length == 2)
            {
                var optionName = arg.Substring(1);
                string? optionValue = null;

                if (i + 1 < args.Length && !args[i + 1].StartsWith("-"))
                {
                    optionValue = args[i + 1];
                    i++;
                }

                options[optionName] = optionValue ?? "true";
            }
            else
            {
                // Positional argument
                arguments.Add(arg);
            }
        }

        return new ParsedCommand
        {
            CommandName = commandName,
            Options = options,
            Arguments = arguments
        };
    }

    /// <summary>
    /// Generates help text for all registered commands.
    /// </summary>
    public string GenerateHelpText()
    {
        var lines = new List<string>
        {
            "Notion Task Sync - CLI Tool",
            "Usage: notion-sync <command> [options] [arguments]",
            "",
            "Commands:"
        };

        foreach (var kvp in _registeredCommands)
        {
            var command = kvp.Value;
            lines.Add($"  {kvp.Key.PadRight(15)} {command.Description}");

            if (command.Options.Any())
            {
                foreach (var opt in command.Options)
                {
                    lines.Add($"    --{opt.Key.PadRight(12)} {opt.Value}");
                }
            }
        }

        lines.Add("");
        lines.Add("Global Options:");
        foreach (var kvp in _globalOptions)
        {
            lines.Add($"  --{kvp.Key.PadRight(12)} {kvp.Value}");
        }

        lines.Add("");
        lines.Add("Examples:");
        lines.Add("  notion-sync sync --database-id abc123 --verbose");
        lines.Add("  notion-sync status");
        lines.Add("  notion-sync configure --token YOUR_API_KEY");

        return string.Join(Environment.NewLine, lines);
    }
}

/// <summary>
/// Represents a CLI command that can be executed.
/// </summary>
public abstract class CliCommand
{
    /// <summary>
    /// User-friendly description of what the command does.
    /// </summary>
    public abstract string Description { get; }

    /// <summary>
    /// Dictionary of available options and their descriptions.
    /// </summary>
    public virtual Dictionary<string, string> Options => new();

    /// <summary>
    /// Executes the command with the provided arguments and options.
    /// Returns an exit code (0 = success, non-zero = failure).
    /// </summary>
    public abstract Task<int> ExecuteAsync(List<string> arguments, Dictionary<string, string> options);

    /// <summary>
    /// Validates that required options are provided.
    /// </summary>
    protected bool ValidateRequiredOptions(Dictionary<string, string> options, params string[] requiredOptions)
    {
        foreach (var option in requiredOptions)
        {
            if (!options.ContainsKey(option) || string.IsNullOrWhiteSpace(options[option]))
                return false;
        }
        return true;
    }
}

/// <summary>
/// Represents the result of parsing command-line arguments.
/// </summary>
public class ParsedCommand
{
    /// <summary>
    /// The name of the command to execute.
    /// </summary>
    public required string CommandName { get; set; }

    /// <summary>
    /// Parsed options and their values.
    /// </summary>
    public required Dictionary<string, string> Options { get; set; }

    /// <summary>
    /// Positional arguments passed to the command.
    /// </summary>
    public required List<string> Arguments { get; set; }

    /// <summary>
    /// Error message if parsing failed, otherwise null.
    /// </summary>
    public string? Error { get; set; }

    /// <summary>
    /// Indicates if parsing was successful and command is executable.
    /// </summary>
    public bool IsValid => string.IsNullOrEmpty(Error);
}
