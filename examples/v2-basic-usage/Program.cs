#nullable enable
using System;
using System.Threading.Tasks;
using NotionTaskSync.Data.Repositories;
using NotionTaskSync.Domain.Models;
using NotionTaskSync.Services;
using Task = System.Threading.Tasks.Task;

class Program
{
    static async Task Main(string[] args)
    {
        // This is a minimal example using v2 features
        Console.WriteLine("Hello, Notion Task Sync v2!");

        // Wire up the minimal set of dependencies needed for a sync run
        ITaskRepository taskRepository = new TaskRepository();
        IChangeLogRepository changeLogRepository = new ChangeLogRepository();
        var notionApiService = new NotionApiService(apiKey: null);
        var changeDetectionService = new ChangeDetectionService(changeLogRepository);
        var conflictResolutionService = new ConflictResolutionService(changeLogRepository);

        var syncService = new SyncService(
            changeDetectionService,
            conflictResolutionService,
            notionApiService,
            taskRepository,
            changeLogRepository);

        var config = new SyncConfig("v2-basic-usage", "database-id", "./tasks");

        // Execute a basic sync operation
        await syncService.ExecuteSyncAsync(config);

        Console.WriteLine("Sync completed!");
    }
}
