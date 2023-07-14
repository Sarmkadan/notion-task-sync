#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NotionTaskSync.Domain.Models;
using NotionTaskSync.Infrastructure.Configuration;
using NotionTaskSync.Services;
using NotionTaskSync.Data.Repositories;

/// <summary>
/// IntegrationExample.cs - ASP.NET Core Dependency Injection Integration
///
/// This example demonstrates how to integrate Notion Task Sync into an ASP.NET Core application.
/// It shows:
/// - Setting up the DI container in ASP.NET Core style
/// - Registering services with IHostBuilder
/// - Using the sync service in controllers
/// - Background service integration
/// - Configuration management
///
/// Use this when you want to integrate sync functionality into a web application.
/// </summary>
public class IntegrationExample
{
    /// <summary>
    /// Example 1: Minimal ASP.NET Core integration
    /// Shows basic setup in a console app simulating ASP.NET Core DI.
    /// </summary>
    public static async Task RunMinimalAspNetCoreIntegration()
    {
        Console.WriteLine("=== ASP.NET Core Style Integration (Minimal) ===\n");

        // Simulate ASP.NET Core's WebApplicationBuilder pattern
        var hostBuilder = new HostBuilder();

        // Configure services (similar to builder.Services in ASP.NET Core)
        hostBuilder.ConfigureServices((context, services) =>
        {
            // Add logging (similar to builder.Logging in ASP.NET Core)
            services.AddLogging(builder =>
            {
                builder.ClearProviders();
                builder.AddConsole();
                builder.SetMinimumLevel(LogLevel.Information);
            });

            // Add application services
            services.AddApplicationServices(context.Configuration);

            // Register sync service as hosted service (background task)
            services.AddHostedService<SyncBackgroundService>();

            // You can also register it as a singleton for direct access
            services.AddSingleton<SyncService>();
        });

        // Configure appsettings.json (similar to builder.Configuration in ASP.NET Core)
        hostBuilder.ConfigureAppConfiguration((hostingContext, config) =>
        {
            config.SetBasePath(Directory.GetCurrentDirectory());
            config.AddJsonFile("appsettings.json", optional: false);
            config.AddEnvironmentVariables("NOTION_"); // Prefix for env vars
        });

        // Build the host
        var host = hostBuilder.Build();

        Console.WriteLine("ASP.NET Core style services configured:");
        Console.WriteLine("  - Logging: Console");
        Console.WriteLine("  - Application Services: Added");
        Console.WriteLine("  - Hosted Service: SyncBackgroundService registered");
        Console.WriteLine("  - Configuration: From appsettings.json and env vars");
        Console.WriteLine();

        // Get services for demonstration
        var syncService = host.Services.GetRequiredService<SyncService>();
        var config = new SyncConfig(
            "AspNetCoreIntegration",
            host.Services.GetRequiredService<IOptions<NotionApiSettings>>().Value.DatabaseId,
            "./aspnet-tasks"
        );

        Console.WriteLine("Executing sync from ASP.NET Core integration...");
        var result = await syncService.ExecuteSyncAsync(config);
        Console.WriteLine($"✅ Sync completed: {result.LocalTaskCount} tasks\n");
    }

    /// <summary>
    /// Example 2: Full ASP.NET Core Web API integration
    /// Shows how to use sync in a controller.
    /// </summary>
    public static void ShowControllerIntegrationPattern()
    {
        Console.WriteLine("=== ASP.NET Core Controller Integration Pattern ===\n");

        Console.WriteLine("Typical controller pattern:");
        Console.WriteLine("```csharp");
        Console.WriteLine("public class SyncController : ControllerBase");
        Console.WriteLine("{");
        Console.WriteLine("    private readonly SyncService _syncService;");
        Console.WriteLine("    private readonly ILogger<SyncController> _logger;");
        Console.WriteLine("");
        Console.WriteLine("    public SyncController(SyncService syncService, ILogger<SyncController> logger)");
        Console.WriteLine("    {");
        Console.WriteLine("        _syncService = syncService;");
        Console.WriteLine("        _logger = logger;");
        Console.WriteLine("    }");
        Console.WriteLine("");
        Console.WriteLine("    [HttpPost(\\"sync\")");
        Console.WriteLine("    public async Task<IActionResult> Sync([FromBody] SyncRequest request)");
        Console.WriteLine("    {");
        Console.WriteLine("        var config = new SyncConfig(");
        Console.WriteLine("            request.Name,");
        Console.WriteLine("            request.DatabaseId,");
        Console.WriteLine("            request.LocalPath");
        Console.WriteLine("        );");
        Console.WriteLine("");
        Console.WriteLine("        var result = await _syncService.ExecuteSyncAsync(config);");
        Console.WriteLine("");
        Console.WriteLine("        return Ok(result);");
        Console.WriteLine("    }");
        Console.WriteLine("}");
        Console.WriteLine("```\n");
    }

    /// <summary>
    /// Example 3: Background service for scheduled sync
    /// Shows how to run sync in the background.
    /// </summary>
    public static async Task RunBackgroundServiceExample()
    {
        Console.WriteLine("=== Background Service for Scheduled Sync ===\n");

        var hostBuilder = new HostBuilder();

        hostBuilder.ConfigureServices((context, services) =>
        {
            services.AddLogging(builder => builder.AddConsole());
            services.AddApplicationServices(context.Configuration);

            // Register our custom background service
            services.AddHostedService<SyncBackgroundService>();
            
            // Configure sync interval via options
            services.Configure<SyncConfig>(options =>
            {
                options.Name = "BackgroundSync";
                options.SyncIntervalSeconds = 300; // 5 minutes
                options.IsEnabled = true;
            });
        });

        hostBuilder.ConfigureAppConfiguration((hostingContext, config) =>
        {
            config.SetBasePath(Directory.GetCurrentDirectory());
            config.AddJsonFile("appsettings.json", optional: false);
        });

        var host = hostBuilder.Build();

        Console.WriteLine("Background service configured:");
        Console.WriteLine("  - SyncBackgroundService registered as IHostedService");
        Console.WriteLine("  - Will run every 5 minutes (SyncIntervalSeconds)");
        Console.WriteLine("  - Automatically starts when host starts");
        Console.WriteLine("  - Gracefully stops when host stops");
        Console.WriteLine();

        Console.WriteLine("Starting host (Ctrl+C to stop)...");
        
        // Start the host (in real app, this would run continuously)
        await host.StartAsync();
        
        Console.WriteLine("✅ Background sync service started!");
        Console.WriteLine("(In a real application, this would run continuously)");
        Console.WriteLine();
    }

    /// <summary>
    /// Example 4: Event-driven integration with ASP.NET Core
    /// Shows subscribing to sync events in a web application.
    /// </summary>
    public static void ShowEventDrivenIntegration()
    {
        Console.WriteLine("=== Event-Driven Integration with ASP.NET Core ===\n");

        Console.WriteLine("Typical event subscriber pattern in ASP.NET Core:");
        Console.WriteLine("```csharp");
        Console.WriteLine("public class SyncEventHandler : BackgroundService");
        Console.WriteLine("{");
        Console.WriteLine("    private readonly IEventBus _eventBus;");
        Console.WriteLine("    private readonly ILogger<SyncEventHandler> _logger;");
        Console.WriteLine("");
        Console.WriteLine("    public SyncEventHandler(IEventBus eventBus, ILogger<SyncEventHandler> logger)");
        Console.WriteLine("    {");
        Console.WriteLine("        _eventBus = eventBus;");
        Console.WriteLine("        _logger = logger;");
        Console.WriteLine("    }");
        Console.WriteLine("");
        Console.WriteLine("    protected override async Task ExecuteAsync(CancellationToken stoppingToken)");
        Console.WriteLine("    {");
        Console.WriteLine("        _eventBus.Subscribe<SyncCompletedEvent>(HandleSyncCompleted);");
        Console.WriteLine("        _eventBus.Subscribe<ConflictDetectedEvent>(HandleConflict);");
        Console.WriteLine("");
        Console.WriteLine("        while (!stoppingToken.IsCancellationRequested)");
        Console.WriteLine("        {");
        Console.WriteLine("            // Keep service alive");
        Console.WriteLine("            await Task.Delay(1000, stoppingToken);");
        Console.WriteLine("        }");
        Console.WriteLine("    }");
        Console.WriteLine("");
        Console.WriteLine("    private void HandleSyncCompleted(SyncCompletedEvent e)");
        Console.WriteLine("    {");
        Console.WriteLine("        _logger.LogInformation(\\"Sync completed: {Status}\\", e.Status);");
        Console.WriteLine("        // Send notification, update dashboard, etc.");
        Console.WriteLine("    }");
        Console.WriteLine("");
        Console.WriteLine("    private void HandleConflict(ConflictDetectedEvent e)");
        Console.WriteLine("    {");
        Console.WriteLine("        _logger.LogWarning(\\"Conflict detected: {TaskId}\\", e.TaskId);");
        Console.WriteLine("        // Send alert, log to monitoring, etc.");
        Console.WriteLine("    }");
        Console.WriteLine("}");
        Console.WriteLine("```\n");
    }

    /// <summary>
    /// Example 5: Configuration management with multiple sync profiles
    /// Shows managing multiple sync configurations in a web app.
    /// </summary>
    public static void ShowMultipleSyncProfiles()
    {
        Console.WriteLine("=== Multiple Sync Profiles in ASP.NET Core ===\n");

        Console.WriteLine("Managing multiple sync configurations:");
        Console.WriteLine("```csharp");
        Console.WriteLine("// In Program.cs or Startup.cs");
        Console.WriteLine("builder.Services.Configure<SyncConfig>(\\"ProjectA\\", options =>");
        Console.WriteLine("{");
        Console.WriteLine("    options.Name = \\"ProjectA\";");
        Console.WriteLine("    options.NotionDatabaseId = Configuration[\\"Sync:ProjectA:DatabaseId\\"];");
        Console.WriteLine("    options.LocalFolderPath = Configuration[\\"Sync:ProjectA:LocalPath\\"];");
        Console.WriteLine("    options.ConflictStrategy = ConflictResolutionStrategy.LocalWins;");
        Console.WriteLine("});");
        Console.WriteLine("");
        Console.WriteLine("builder.Services.Configure<SyncConfig>(\\"ProjectB\\", options =>");
        Console.WriteLine("{");
        Console.WriteLine("    options.Name = \\"ProjectB\";");
        Console.WriteLine("    options.NotionDatabaseId = Configuration[\\"Sync:ProjectB:DatabaseId\\"];");
        Console.WriteLine("    options.LocalFolderPath = Configuration[\\"Sync:ProjectB:LocalPath\\"];");
        Console.WriteLine("    options.ConflictStrategy = ConflictResolutionStrategy.NotionWins;");
        Console.WriteLine("});");
        Console.WriteLine("");
        Console.WriteLine("// Then inject IOptionsSnapshot<SyncConfig> to access specific configs");
        Console.WriteLine("public class SyncManager");
        Console.WriteLine("{");
        Console.WriteLine("    private readonly IOptionsSnapshot<SyncConfig> _configs;");
        Console.WriteLine("");
        Console.WriteLine("    public SyncManager(IOptionsSnapshot<SyncConfig> configs)");
        Console.WriteLine("    {");
        Console.WriteLine("        _configs = configs;");
        Console.WriteLine("    }");
        Console.WriteLine("");
        Console.WriteLine("    public void SyncProjectA()");
        Console.WriteLine("    {");
        Console.WriteLine("        var config = _configs.Get(\\"ProjectA\\");");
        Console.WriteLine("        // Execute sync for ProjectA");
        Console.WriteLine("    }");
        Console.WriteLine("}");
        Console.WriteLine("```\n");
    }
}

/// <summary>
/// Custom background service for scheduled sync operations.
/// Implements IHostedService for ASP.NET Core integration.
/// </summary>
public class SyncBackgroundService : BackgroundService
{
    private readonly SyncService _syncService;
    private readonly ILogger<SyncBackgroundService> _logger;
    private readonly IOptions<SyncConfig> _config;

    public SyncBackgroundService(
        SyncService syncService,
        ILogger<SyncBackgroundService> logger,
        IOptions<SyncConfig> config)
    {
        _syncService = syncService;
        _logger = logger;
        _config = config;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("SyncBackgroundService starting...");
        
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                _logger.LogInformation("Running scheduled sync...");
                
                var result = await _syncService.ExecuteSyncAsync(_config.Value);
                
                _logger.LogInformation(
                    "Sync completed: {LocalTasks} local tasks, {NotionPages} Notion pages, {Status}",
                    result.LocalTaskCount,
                    result.NotionPageCount,
                    result.Status
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during scheduled sync");
            }

            // Wait for next sync interval
            var delay = TimeSpan.FromSeconds(_config.Value.SyncIntervalSeconds);
            await Task.Delay(delay, stoppingToken);
        }

        _logger.LogInformation("SyncBackgroundService stopped.");
    }
}

/// <summary>
/// Example controller showing REST API integration
/// </summary>
public class IntegrationExampleController
{
    // This would be in a real ASP.NET Core project
    public static void ShowControllerExample()
    {
        Console.WriteLine("=== REST API Controller Example ===\n");

        Console.WriteLine("Full controller implementation:");
        Console.WriteLine("```csharp");
        Console.WriteLine("[ApiController]");
        Console.WriteLine("[Route(\\"api/[controller]\")")
        Console.WriteLine("public class SyncController : ControllerBase");
        Console.WriteLine("{");
        Console.WriteLine("    private readonly SyncService _syncService;");
        Console.WriteLine("    private readonly ILogger<SyncController> _logger;");
        Console.WriteLine("");
        Console.WriteLine("    public SyncController(SyncService syncService, ILogger<SyncController> logger)");
        Console.WriteLine("    {");
        Console.WriteLine("        _syncService = syncService;");
        Console.WriteLine("        _logger = logger;");
        Console.WriteLine("    }");
        Console.WriteLine("");
        Console.WriteLine("    [HttpPost(\\"sync\")")
        Console.WriteLine("    public async Task<IActionResult> Sync([FromBody] SyncRequest request)");
        Console.WriteLine("    {");
        Console.WriteLine("        _logger.LogInformation(\\"Starting sync: {Name}\\", request.Name);");
        Console.WriteLine("");
        Console.WriteLine("        var config = new SyncConfig(");
        Console.WriteLine("            request.Name,");
        Console.WriteLine("            request.DatabaseId,");
        Console.WriteLine("            request.LocalPath");
        Console.WriteLine("        );");
        Console.WriteLine("");
        Console.WriteLine("        // Apply optional overrides from request");
        Console.WriteLine("        if (request.Direction.HasValue)");
        Console.WriteLine("            config.Direction = request.Direction.Value;");
        Console.WriteLine("");
        Console.WriteLine("        var result = await _syncService.ExecuteSyncAsync(config);");
        Console.WriteLine("");
        Console.WriteLine("        _logger.LogInformation(\\"Sync completed: {Status}\\", result.Status);");
        Console.WriteLine("");
        Console.WriteLine("        return Ok(new { Status = result.Status, TasksSynced = result.SyncedCount });");
        Console.WriteLine("    }");
        Console.WriteLine("");
        Console.WriteLine("    [HttpGet(\\"status\")")
        Console.WriteLine("    public IActionResult GetStatus()");
        Console.WriteLine("    {");
        Console.WriteLine("        var status = new {");
        Console.WriteLine("            IsHealthy = true,");
        Console.WriteLine("            LastSync = DateTime.UtcNow,");
        Console.WriteLine("            Message = \\"Sync service running\\"");");
        Console.WriteLine("        return Ok(status);");
        Console.WriteLine("    }");
        Console.WriteLine("}");
        Console.WriteLine("");
        Console.WriteLine("// Request DTO");
        Console.WriteLine("public class SyncRequest");
        Console.WriteLine("{");
        Console.WriteLine("    public string Name { get; set; } = string.Empty;");
        Console.WriteLine("    public string DatabaseId { get; set; } = string.Empty;");
        Console.WriteLine("    public string LocalPath { get; set; } = \"./tasks\";");
        Console.WriteLine("    public SyncDirection? Direction { get; set; }");
        Console.WriteLine("}");
        Console.WriteLine("```\n");
    }
}

/// <summary>
/// Usage demonstration
/// </summary>
public class IntegrationExampleDemo
{
    public static async Task Main()
    {
        try
        {
            // Example 1: Minimal ASP.NET Core integration
            await IntegrationExample.RunMinimalAspNetCoreIntegration();

            Console.WriteLine("\n" + new string('=', 60));

            // Example 2: Show controller pattern
            IntegrationExample.ShowControllerIntegrationPattern();

            Console.WriteLine("\n" + new string('=', 60));

            // Example 3: Show event-driven pattern
            IntegrationExample.ShowEventDrivenIntegration();

            Console.WriteLine("\n" + new string('=', 60));

            // Example 4: Show multiple sync profiles
            IntegrationExample.ShowMultipleSyncProfiles();

            Console.WriteLine("\n" + new string('=', 60));
            
            // Example 5: Show controller example
            IntegrationExampleController.ShowControllerExample();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\n❌ Demo failed: {ex.Message}");
            return;
        }
    }
}

/// <summary>
/// Simple request DTO for sync endpoint
/// </summary>
public class SyncRequest
{
    public string Name { get; set; } = string.Empty;
    public string DatabaseId { get; set; } = string.Empty;
    public string LocalPath { get; set; } = "./tasks";
    public SyncDirection? Direction { get; set; }
}
