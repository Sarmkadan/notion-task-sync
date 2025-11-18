// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NotionTaskSync.Domain.Models;
using NotionTaskSync.Infrastructure.Configuration;
using NotionTaskSync.Services;
using DomainTask = NotionTaskSync.Domain.Models.Task;
using DomainTaskStatus = NotionTaskSync.Domain.Models.TaskStatus;

namespace NotionTaskSync.Examples;

/// <summary>
/// Example demonstrating programmatic task creation, modification, and management.
/// Shows how to work with tasks through code rather than CLI.
/// </summary>
public class ProgrammaticTaskManagementExample
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
        var logger = serviceProvider.GetRequiredService<ILogger<ProgrammaticTaskManagementExample>>();
        var localFileService = serviceProvider.GetRequiredService<LocalFileService>();

        try
        {
            // Ensure tasks directory exists
            Directory.CreateDirectory("./tasks");

            // Create new tasks
            await CreateNewTasksAsync(logger, localFileService);

            // Load and modify existing tasks
            await LoadAndModifyTasksAsync(logger, localFileService);

            // Organize and categorize tasks
            await OrganizeTasksAsync(logger, localFileService);

            // Generate task statistics
            await GenerateTaskStatisticsAsync(logger, localFileService);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Programmatic task management example failed");
            Environment.Exit(1);
        }
    }

    private static async global::System.Threading.Tasks.Task CreateNewTasksAsync(
        ILogger logger,
        LocalFileService localFileService)
    {
        logger.LogInformation("==============================================");
        logger.LogInformation("Creating New Tasks...");
        logger.LogInformation("==============================================");

        var newTasks = new List<DomainTask>
        {
            new()
            {
                Id = Guid.NewGuid(),
                Title = "Implement feature authentication",
                Status = DomainTaskStatus.Todo,
                Priority = 3,
                DueDate = DateTime.UtcNow.AddDays(7),
                Tags = "backend,security",
                Description = "Add OAuth2 authentication to API",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new()
            {
                Id = Guid.NewGuid(),
                Title = "Write unit tests for ChangeDetectionService",
                Status = DomainTaskStatus.InProgress,
                Priority = 2,
                DueDate = DateTime.UtcNow.AddDays(3),
                Tags = "testing,core",
                Description = "Ensure 90%+ code coverage",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new()
            {
                Id = Guid.NewGuid(),
                Title = "Update documentation",
                Status = DomainTaskStatus.Todo,
                Priority = 1,
                DueDate = DateTime.UtcNow.AddDays(14),
                Tags = "docs",
                Description = "Update API documentation with new endpoints",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }
        };

        // Save each task
        foreach (var task in newTasks)
        {
            await localFileService.SaveTaskAsync(task);
            logger.LogInformation("Created: {Title}", task.Title);
        }

        logger.LogInformation("Created {Count} tasks", newTasks.Count);
        logger.LogInformation("");
    }

    private static async global::System.Threading.Tasks.Task LoadAndModifyTasksAsync(
        ILogger logger,
        LocalFileService localFileService)
    {
        logger.LogInformation("==============================================");
        logger.LogInformation("Loading and Modifying Tasks...");
        logger.LogInformation("==============================================");

        var tasks = await localFileService.LoadAllTasksAsync();
        logger.LogInformation("Loaded {Count} tasks", tasks.Count);

        // Modify first task
        if (tasks.Count > 0)
        {
            var task = tasks[0];
            logger.LogInformation("Modifying: {Title}", task.Title);

            task.Status = DomainTaskStatus.InProgress;
            task.Priority = 3;
            task.UpdatedAt = DateTime.UtcNow;

            await localFileService.SaveTaskAsync(task);
            logger.LogInformation("Task updated and saved");
        }

        logger.LogInformation("");
    }

    private static async global::System.Threading.Tasks.Task OrganizeTasksAsync(
        ILogger logger,
        LocalFileService localFileService)
    {
        logger.LogInformation("==============================================");
        logger.LogInformation("Organizing Tasks by Status...");
        logger.LogInformation("==============================================");

        var tasks = await localFileService.LoadAllTasksAsync();

        // Group by status
        var grouped = tasks.GroupBy(t => t.Status)
            .OrderBy(g => g.Key);

        foreach (var group in grouped)
        {
            logger.LogInformation("{Status}: {Count} tasks", group.Key, group.Count());
            foreach (var task in group)
            {
                logger.LogInformation("  [{Priority}] {Title}", task.Priority, task.Title);
            }
        }

        logger.LogInformation("");
    }

    private static async global::System.Threading.Tasks.Task GenerateTaskStatisticsAsync(
        ILogger logger,
        LocalFileService localFileService)
    {
        logger.LogInformation("==============================================");
        logger.LogInformation("Task Statistics...");
        logger.LogInformation("==============================================");

        var tasks = await localFileService.LoadAllTasksAsync();

        // Calculate statistics
        var totalTasks = tasks.Count;
        var completedCount = tasks.Count(t => t.Status == DomainTaskStatus.Done);
        var inProgressCount = tasks.Count(t => t.Status == DomainTaskStatus.InProgress);
        var highPriorityCount = tasks.Count(t => t.Priority >= 3);
        var overdueTasks = tasks.Where(t => t.DueDate.HasValue && t.DueDate < DateTime.UtcNow).ToList();

        logger.LogInformation("Total Tasks: {Count}", totalTasks);

        if (totalTasks > 0)
        {
            logger.LogInformation("Completed: {Count} ({Percent:P})", completedCount, (double)completedCount / totalTasks);
            logger.LogInformation("In Progress: {Count} ({Percent:P})", inProgressCount, (double)inProgressCount / totalTasks);
        }

        logger.LogInformation("High Priority: {Count}", highPriorityCount);
        logger.LogInformation("Overdue: {Count}", overdueTasks.Count);

        if (overdueTasks.Count > 0)
        {
            logger.LogInformation("");
            logger.LogInformation("Overdue Tasks:");
            foreach (var task in overdueTasks)
            {
                var days = (DateTime.UtcNow - task.DueDate!.Value).Days;
                logger.LogInformation("  {Title} ({Days}d overdue)", task.Title, days);
            }
        }

        logger.LogInformation("");
    }
}
