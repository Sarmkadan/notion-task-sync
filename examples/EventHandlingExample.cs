// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NotionTaskSync.Events;
using NotionTaskSync.Infrastructure.Configuration;
using NotionTaskSync.Services;

namespace NotionTaskSync.Examples;

/// <summary>
/// Example demonstrating event subscription and handling.
/// Shows how to react to sync events like conflicts and completion.
/// </summary>
public class EventHandlingExample
{
    private static ILogger _logger;

    public static async Task Main(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false)
            .AddEnvironmentVariables()
            .Build();

        var services = new ServiceCollection();
        services.AddSingleton(configuration);
        services.AddLogging(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Information);
        });
        services.AddHttpClient();
        services.AddApplicationServices(configuration);

        var serviceProvider = services.BuildServiceProvider();
        _logger = serviceProvider.GetRequiredService<ILogger<EventHandlingExample>>();
        var eventBus = serviceProvider.GetRequiredService<EventBus>();
        var syncService = serviceProvider.GetRequiredService<SyncService>();

        try
        {
            // Subscribe to events before sync
            SubscribeToEvents(eventBus);

            _logger.LogInformation("Starting sync with event monitoring...");

            var config = new Domain.Models.SyncConfig(
                name: "EventHandlingSync",
                notionDatabaseId: configuration["NotionApi:DatabaseId"] ?? "test-db",
                localFolderPath: "./tasks"
            );

            var result = await syncService.ExecuteSyncAsync(config);

            _logger.LogInformation("═══════════════════════════════════════════════");
            _logger.LogInformation("Final Results:");
            _logger.LogInformation("  Status: {Status}", result.Status);
            _logger.LogInformation("  Local Tasks: {Count}", result.LocalTaskCount);
            _logger.LogInformation("  Notion Pages: {Count}", result.NotionPageCount);
            _logger.LogInformation("  Conflicts: {Count}", result.ConflictsDetected);
            _logger.LogInformation("═══════════════════════════════════════════════");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Event handling example failed");
            Environment.Exit(1);
        }
    }

    private static void SubscribeToEvents(EventBus eventBus)
    {
        // Subscribe to conflict detection events
        eventBus.Subscribe<ConflictDetectedEvent>(async e =>
        {
            _logger.LogWarning("⚠️  CONFLICT DETECTED");
            _logger.LogWarning("Task ID: {TaskId}", e.TaskId);
            _logger.LogWarning("Local change at: {LocalTime}", e.LocalModifiedAt);
            _logger.LogWarning("Notion change at: {NotionTime}", e.RemoteModifiedAt);

            // You could trigger alerts, send notifications, etc.
            await NotifyTeamAsync(e.TaskId.ToString(), "Conflict detected - manual review needed");
        });

        // Subscribe to sync completion events
        eventBus.Subscribe<SyncCompletedEvent>(async e =>
        {
            _logger.LogInformation("✓ SYNC COMPLETED");
            _logger.LogInformation("  Status: {Status}", e.Success ? "Success" : "Failed");
            _logger.LogInformation("  Duration: {Duration}ms", e.Duration.TotalMilliseconds);
            _logger.LogInformation("  Conflicts Resolved: {Count}", e.ConflictsResolved);

            if (!string.IsNullOrEmpty(e.ErrorMessage))
            {
                _logger.LogError("  Error: {Error}", e.ErrorMessage);
            }

            // Log to external service, update dashboard, etc.
            await LogSyncResultsAsync(e);
        });

        _logger.LogInformation("Event subscriptions configured");
    }

    private static async Task NotifyTeamAsync(string taskId, string message)
    {
        // Example: Send Slack notification
        _logger.LogInformation("  → Would send notification: {TaskId} - {Message}", taskId, message);
        await Task.CompletedTask;

        // Real implementation would:
        // var slackClient = new SlackClient(token);
        // await slackClient.SendMessageAsync(channel, message);
    }

    private static async Task LogSyncResultsAsync(SyncCompletedEvent @event)
    {
        // Example: Log to external monitoring service
        _logger.LogInformation("  → Would log to monitoring service");
        await Task.CompletedTask;

        // Real implementation would:
        // var client = new DatadogClient(apiKey);
        // await client.LogMetricAsync("notion_sync.completed", 1);
        // await client.LogMetricAsync("notion_sync.duration_ms", @event.Duration.TotalMilliseconds);
    }
}

