#nullable enable
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
/// <para>
/// Supported argument shape: <c>&lt;command&gt; [options] [positional arguments]</c>.
/// The first argument is the command name (matched case-insensitively against registered commands).
/// The remaining arguments are parsed as follows:
/// </para>
/// <list type="bullet">
/// <item><description><c>--key=value</c> - long option with an inline value (split on the first <c>=</c>).</description></item>
/// <item><description><c>--key value</c> - long option whose value is the next argument, unless that argument starts with <c>--</c>.</description></item>
/// <item><description><c>--flag</c> - long option with no value; stored as <c>"true"</c>.</description></item>
/// <item><description><c>-k value</c> - single-letter short option (exactly two characters including the dash); the value is the next argument unless it starts with <c>-</c>. Without a value it is stored as <c>"true"</c>.</description></item>
/// <item><description>Any other argument - positional, collected in order into <see cref="ParsedCommand.Arguments"/>.</description></item>
/// </list>
/// <para>
/// Help is requested with <c>--help</c>, <c>-h</c>, <c>help</c>, or by passing no arguments at all.
/// An unregistered command name yields a help result with <see cref="ParsedCommand.Error"/> set.
/// </para>
/// </summary>
public class CliArgumentParser
{
    private readonly Dictionary<string, CliCommand> _registeredCommands = new();
    private readonly Dictionary<string, string> _globalOptions = new();

    /// <summary>
    /// Registers a command that can be invoked from the CLI.
    /// The name is stored in lowercase invariant form, so lookups are case-insensitive.
    /// Registering the same name again replaces the previous command.
    /// </summary>
    /// <param name="name">The command name typed on the command line (for example <c>sync</c>). Must not be null, empty, or whitespace.</param>
    /// <param name="command">The command instance to execute when <paramref name="name"/> is parsed.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="name"/> is null, empty, or whitespace.</exception>
    public void RegisterCommand(string name, CliCommand command)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Command name cannot be null or empty", nameof(name));

        _registeredCommands[name.ToLowerInvariant()] = command;
    }

    /// <summary>
    /// Registers a global option that applies to all commands.
    /// Global options are listed in the help text only; <see cref="Parse"/> does not validate them.
    /// Registering the same name again replaces the previous description.
    /// </summary>
    /// <param name="name">The option name without the leading dashes (for example <c>verbose</c> for <c>--verbose</c>).</param>
    /// <param name="description">Human-readable description shown in the help text.</param>
    public void RegisterGlobalOption(string name, string description)
    {
        _globalOptions[name] = description;
    }

    /// <summary>
    /// Parses command-line arguments and returns an executable command.
    /// Argument forms are described on <see cref="CliArgumentParser"/>.
    /// Never returns null: a help request (no arguments, <c>--help</c>, <c>-h</c>, <c>help</c>)
    /// returns a result with <see cref="ParsedCommand.CommandName"/> set to <c>help</c>, and an unknown
    /// command returns a <c>help</c> result with <see cref="ParsedCommand.Error"/> set.
    /// Check <see cref="ParsedCommand.IsValid"/> before executing the result.
    /// </summary>
    /// <param name="args">The raw command-line arguments, typically <c>args</c> from <c>Main</c>. The first element is the command name.</param>
    /// <returns>The parsed command, with options, positional arguments, and any error set.</returns>
    public ParsedCommand? Parse(string[] args)
    {
        if (args is null || args.Length == 0)
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
    /// Generates help text for all registered commands, including each command's options,
    /// the registered global options, and a few usage examples.
    /// </summary>
    /// <returns>The help text, with lines joined by the platform newline.</returns>
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
