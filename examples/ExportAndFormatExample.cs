// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NotionTaskSync.Formatters;
using NotionTaskSync.Infrastructure.Configuration;
using NotionTaskSync.Services;

namespace NotionTaskSync.Examples;

/// <summary>
/// Example demonstrating task export to multiple formats.
/// Shows how to export tasks as JSON, CSV, XML, and Markdown.
/// </summary>
public class ExportAndFormatExample
{
    public static async Task Main(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false)
            .AddEnvironmentVariables()
            .Build();

        var services = new ServiceCollection();
        services.AddSingleton(configuration);
        services.AddLogging(builder => builder.AddConsole());
        services.AddHttpClient();
        services.AddApplicationServices(configuration);

        var serviceProvider = services.BuildServiceProvider();
        var logger = serviceProvider.GetRequiredService<ILogger<ExportAndFormatExample>>();
        var localFileService = serviceProvider.GetRequiredService<LocalFileService>();

        try
        {
            // Load tasks from local directory
            var tasks = await localFileService.LoadTasksAsync("./tasks");
            logger.LogInformation("Loaded {Count} tasks", tasks.Count);

            // Export in multiple formats
            await ExportAsJsonAsync(logger, tasks);
            await ExportAsCsvAsync(logger, tasks);
            await ExportAsXmlAsync(logger, tasks);
            await ExportAsMarkdownAsync(logger, tasks);

            logger.LogInformation("✓ All exports completed");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Export example failed");
            Environment.Exit(1);
        }
    }

    private static async Task ExportAsJsonAsync(
        ILogger logger,
        List<Domain.Models.Task> tasks)
    {
        logger.LogInformation("═══════════════════════════════════════════════");
        logger.LogInformation("Exporting as JSON...");
        logger.LogInformation("═══════════════════════════════════════════════");

        var formatter = new JsonFormatter();
        var output = formatter.Format(tasks);
        var filePath = $"./exports/tasks_{DateTime.Now:yyyyMMdd_HHmmss}.json";

        Directory.CreateDirectory("./exports");
        await File.WriteAllTextAsync(filePath, output);

        logger.LogInformation("✓ JSON export completed");
        logger.LogInformation("  File: {Path}", filePath);
        logger.LogInformation("  Size: {Size} bytes", new FileInfo(filePath).Length);
        logger.LogInformation("");
    }

    private static async Task ExportAsCsvAsync(
        ILogger logger,
        List<Domain.Models.Task> tasks)
    {
        logger.LogInformation("═══════════════════════════════════════════════");
        logger.LogInformation("Exporting as CSV...");
        logger.LogInformation("═══════════════════════════════════════════════");

        var formatter = new CsvFormatter();
        var output = formatter.Format(tasks);
        var filePath = $"./exports/tasks_{DateTime.Now:yyyyMMdd_HHmmss}.csv";

        Directory.CreateDirectory("./exports");
        await File.WriteAllTextAsync(filePath, output);

        logger.LogInformation("✓ CSV export completed");
        logger.LogInformation("  File: {Path}", filePath);
        logger.LogInformation("  Lines: {Count}", output.Split(Environment.NewLine).Length);
        logger.LogInformation("");
    }

    private static async Task ExportAsXmlAsync(
        ILogger logger,
        List<Domain.Models.Task> tasks)
    {
        logger.LogInformation("═══════════════════════════════════════════════");
        logger.LogInformation("Exporting as XML...");
        logger.LogInformation("═══════════════════════════════════════════════");

        var formatter = new XmlFormatter();
        var output = formatter.Format(tasks);
        var filePath = $"./exports/tasks_{DateTime.Now:yyyyMMdd_HHmmss}.xml";

        Directory.CreateDirectory("./exports");
        await File.WriteAllTextAsync(filePath, output);

        logger.LogInformation("✓ XML export completed");
        logger.LogInformation("  File: {Path}", filePath);
        logger.LogInformation("  Size: {Size} bytes", new FileInfo(filePath).Length);
        logger.LogInformation("");
    }

    private static async Task ExportAsMarkdownAsync(
        ILogger logger,
        List<Domain.Models.Task> tasks)
    {
        logger.LogInformation("═══════════════════════════════════════════════");
        logger.LogInformation("Exporting as Markdown...");
        logger.LogInformation("═══════════════════════════════════════════════");

        var formatter = new MarkdownFormatter();
        var output = formatter.Format(tasks);
        var filePath = $"./exports/tasks_{DateTime.Now:yyyyMMdd_HHmmss}.md";

        Directory.CreateDirectory("./exports");
        await File.WriteAllTextAsync(filePath, output);

        logger.LogInformation("✓ Markdown export completed");
        logger.LogInformation("  File: {Path}", filePath);
        logger.LogInformation("  Lines: {Count}", output.Split(Environment.NewLine).Length);

        // Display preview
        logger.LogInformation("");
        logger.LogInformation("Preview (first 500 characters):");
        logger.LogInformation(output.Substring(0, Math.Min(500, output.Length)));
        logger.LogInformation("");
    }
}
