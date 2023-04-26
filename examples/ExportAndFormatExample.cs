#nullable enable
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
    public static async global::System.Threading.Tasks.Task Main(string[] args)
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
            var tasks = await localFileService.LoadAllTasksAsync().ConfigureAwait(false);
            logger.LogInformation("Loaded {Count} tasks", tasks.Count);

            // Export in multiple formats
            await ExportAsJsonAsync(logger, serviceProvider, tasks).ConfigureAwait(false);
            await ExportAsCsvAsync(logger, serviceProvider, tasks).ConfigureAwait(false);
            await ExportAsXmlAsync(logger, serviceProvider, tasks).ConfigureAwait(false);
            await ExportAsMarkdownAsync(logger, serviceProvider, tasks).ConfigureAwait(false);

            logger.LogInformation("All exports completed");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Export example failed");
            Environment.Exit(1);
        }
    }

    private static async global::System.Threading.Tasks.Task ExportAsJsonAsync(
        ILogger logger,
        IServiceProvider serviceProvider,
        List<Domain.Models.Task> tasks)
    {
        logger.LogInformation("==============================================");
        logger.LogInformation("Exporting as JSON...");
        logger.LogInformation("==============================================");

        var formatter = serviceProvider.GetRequiredService<JsonFormatter>();
        var output = formatter.FormatTasks(tasks);
        var filePath = $"./exports/tasks_{DateTime.Now:yyyyMMdd_HHmmss}.json";

        Directory.CreateDirectory("./exports");
        await File.WriteAllTextAsync(filePath, output).ConfigureAwait(false);

        logger.LogInformation("JSON export completed");
        logger.LogInformation("  File: {Path}", filePath);
        logger.LogInformation("  Size: {Size} bytes", new FileInfo(filePath).Length);
        logger.LogInformation("");
    }

    private static async global::System.Threading.Tasks.Task ExportAsCsvAsync(
        ILogger logger,
        IServiceProvider serviceProvider,
        List<Domain.Models.Task> tasks)
    {
        logger.LogInformation("==============================================");
        logger.LogInformation("Exporting as CSV...");
        logger.LogInformation("==============================================");

        var formatter = serviceProvider.GetRequiredService<CsvFormatter>();
        var output = formatter.FormatTasks(tasks);
        var filePath = $"./exports/tasks_{DateTime.Now:yyyyMMdd_HHmmss}.csv";

        Directory.CreateDirectory("./exports");
        await File.WriteAllTextAsync(filePath, output).ConfigureAwait(false);

        logger.LogInformation("CSV export completed");
        logger.LogInformation("  File: {Path}", filePath);
        logger.LogInformation("  Lines: {Count}", output.Split(Environment.NewLine).Length);
        logger.LogInformation("");
    }

    private static async global::System.Threading.Tasks.Task ExportAsXmlAsync(
        ILogger logger,
        IServiceProvider serviceProvider,
        List<Domain.Models.Task> tasks)
    {
        logger.LogInformation("==============================================");
        logger.LogInformation("Exporting as XML...");
        logger.LogInformation("==============================================");

        var formatter = serviceProvider.GetRequiredService<XmlFormatter>();
        var output = formatter.FormatTasks(tasks);
        var filePath = $"./exports/tasks_{DateTime.Now:yyyyMMdd_HHmmss}.xml";

        Directory.CreateDirectory("./exports");
        await File.WriteAllTextAsync(filePath, output).ConfigureAwait(false);

        logger.LogInformation("XML export completed");
        logger.LogInformation("  File: {Path}", filePath);
        logger.LogInformation("  Size: {Size} bytes", new FileInfo(filePath).Length);
        logger.LogInformation("");
    }

    private static async global::System.Threading.Tasks.Task ExportAsMarkdownAsync(
        ILogger logger,
        IServiceProvider serviceProvider,
        List<Domain.Models.Task> tasks)
    {
        logger.LogInformation("==============================================");
        logger.LogInformation("Exporting as Markdown...");
        logger.LogInformation("==============================================");

        var formatter = serviceProvider.GetRequiredService<MarkdownFormatter>();
        var output = formatter.FormatTasks(tasks);
        var filePath = $"./exports/tasks_{DateTime.Now:yyyyMMdd_HHmmss}.md";

        Directory.CreateDirectory("./exports");
        await File.WriteAllTextAsync(filePath, output).ConfigureAwait(false);

        logger.LogInformation("Markdown export completed");
        logger.LogInformation("  File: {Path}", filePath);
        logger.LogInformation("  Lines: {Count}", output.Split(Environment.NewLine).Length);

        logger.LogInformation("");
        logger.LogInformation("Preview (first 500 characters):");
        logger.LogInformation(output.Substring(0, Math.Min(500, output.Length)));
        logger.LogInformation("");
    }
}
