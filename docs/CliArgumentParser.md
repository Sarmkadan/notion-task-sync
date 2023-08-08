# CliArgumentParser
The `CliArgumentParser` type is designed to handle command-line argument parsing for the notion-task-sync project. It provides a structured way to define and execute commands with various options and arguments, allowing for flexible and robust command-line interface (CLI) implementations.

## API
* `public void RegisterCommand`: Registers a command with the parser. This method is used to define the structure of a command, including its name, options, and arguments.
* `public void RegisterGlobalOption`: Registers a global option with the parser. This method is used to define options that are applicable to all commands.
* `public ParsedCommand? Parse`: Parses the command-line arguments and returns a `ParsedCommand` object if successful, or `null` if parsing fails.
* `public string GenerateHelpText`: Generates help text for the registered commands and options.
* `public abstract string Description`: Gets a brief description of the command.
* `public abstract Task<int> ExecuteAsync`: Executes the command asynchronously and returns an integer indicating the execution result.
* `public required string CommandName`: Gets the name of the command.
* `public required Dictionary<string, string> Options`: Gets a dictionary of options and their corresponding values.
* `public required List<string> Arguments`: Gets a list of command-line arguments.
* `public string? Error`: Gets an error message if parsing or execution fails.

## Usage
The following examples demonstrate how to use the `CliArgumentParser` type:
```csharp
// Example 1: Registering a command and parsing arguments
var parser = new MyCliArgumentParser();
parser.RegisterCommand("mycommand", new[] { "option1", "option2" }, new[] { "arg1", "arg2" });
var parsedCommand = parser.Parse(new[] { "mycommand", "--option1", "value1", "arg1" });
if (parsedCommand != null)
{
    Console.WriteLine($"Command: {parsedCommand.CommandName}, Options: {string.Join(", ", parsedCommand.Options)}");
}

// Example 2: Executing a command and handling errors
var parser2 = new MyCliArgumentParser();
parser2.RegisterCommand("mycommand", new[] { "option1", "option2" }, new[] { "arg1", "arg2" });
var result = await parser2.ExecuteAsync();
if (parser2.Error != null)
{
    Console.WriteLine($"Error: {parser2.Error}");
}
else
{
    Console.WriteLine($"Execution result: {result}");
}
```

## Notes
When using the `CliArgumentParser` type, consider the following edge cases and thread-safety remarks:
* The `RegisterCommand` and `RegisterGlobalOption` methods should be called before parsing or executing commands to ensure proper registration.
* The `Parse` method may return `null` if the command-line arguments are invalid or incomplete.
* The `ExecuteAsync` method is asynchronous and may throw exceptions if execution fails.
* The `Error` property is used to store error messages, which should be checked after parsing or execution to handle any errors that may have occurred.
* The `CliArgumentParser` type is not inherently thread-safe, and care should be taken to ensure that instances are not shared across multiple threads or accessed concurrently.
