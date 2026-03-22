using System;
using System.Threading.Tasks;

class Program
{
    static async Task Main(string[] args)
    {
        // This is a minimal example using v2 features
        Console.WriteLine("Hello, Notion Task Sync v2!");

        // Initialize the sync service
        var syncService = new NotionTaskSync.Services.SyncService();

        // Execute a basic sync operation
        await syncService.ExecuteSyncAsync();

        Console.WriteLine("Sync completed!");
    }
}