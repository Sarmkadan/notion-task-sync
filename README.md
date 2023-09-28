## AdvancedUsageExtensions

The `AdvancedUsageExtensions` class provides advanced utilities for validating, optimizing, and analyzing task synchronization workflows. It includes methods to validate configuration, optimize sync settings, execute syncs with retry logic, and analyze performance metrics.

### Usage Example

```csharp
using Domain.Models;
using SyncService;

class Program
{
    static async Task Main()
    {
        // Validate and optimize sync configuration
        var config = new SyncConfig();
        var validationReport = AdvancedUsageExtensions.ValidateConfiguration(config);

        if (validationReport.IsValid)
        {
            var optimizedConfig = AdvancedUsageExtensions.CreateOptimizedConfiguration(config);

            // Execute sync with retry logic
            var result = await AdvancedUsageExtensions.ExecuteWithRetryAsync(optimizedConfig);

            // Analyze results
            var analysis = AdvancedUsageExtensions.AnalyzeResults(result);

            // Output key metrics
            Console.WriteLine($"Total Tasks: {analysis.TotalTasks}");
            Console.WriteLine($"Synced Tasks: {analysis.SyncedTasks}");
            Console.WriteLine($"Conflicts: {analysis.Conflicts}");
            Console.WriteLine($"Success Rate: {analysis.SuccessRate:P}");
            Console.WriteLine($"Efficiency: {analysis.EfficiencyRating}");
        }
        else
        {
            Console.WriteLine("Configuration issues found:");
            foreach (var issue in validationReport.Issues)
            {
                Console.WriteLine($"- {issue}");
            }
        }
    }
}
```

## RateLimitingMiddlewareExtensions

The `RateLimitingMiddlewareExtensions` class provides utilities for handling rate limiting in task synchronization workflows. It includes methods to execute actions with retry logic, check if the rate limit has been exceeded, and get the time until the rate limit resets. 

Here's an example of how to use `RateLimitingMiddlewareExtensions` to execute an action with retry logic:
```csharp
var result = await RateLimitingMiddlewareExtensions.ExecuteWithRetryAsync<string>(() => 
{
    // Code to execute with retry logic
    return "Success";
});
Console.WriteLine(result);
```

## SyncServiceExtensions

The `SyncServiceExtensions` class provides extension methods for analyzing and processing synchronization results. It includes utilities to check success status, extract metrics, filter results, and generate summaries from sync operations.

### Usage Example

```csharp
using Services;

class Program
{
    static void Main()
    {
        // Assume we have a collection of sync results
        var results = GetSyncResults(); // IEnumerable<SyncService.SyncResult>

        // Analyze individual result
        var latestResult = results.GetMostRecent();
        if (latestResult.IsSuccessful)
        {
            Console.WriteLine($"Success! Changes: {latestResult.GetTotalChangesDetected()}");
            Console.WriteLine($"Duration: {latestResult.GetDuration()?.TotalMinutes:F1} minutes");
        }
        else
        {
            Console.WriteLine($"Failed: {latestResult.GetErrorMessage()}");
        }

        // Analyze collection of results
        var successful = results.WhereSuccessful().OrderByCompletion();
        var failed = results.WhereFailed();
        
        Console.WriteLine($"\nSummary:");
        Console.WriteLine($"- Total: {results.Count()}");
        Console.WriteLine($"- Success: {successful.Count()}");
        Console.WriteLine($"- Failures: {failed.Count()}");
        Console.WriteLine($"- Completion %: {results.GetCompletionPercentage():F1}%");
        
        foreach (var result in successful)
        {
            Console.WriteLine($"\n{result.GetSummary()}");
        }
    }
}
```

## BackupServiceExtensions

The `BackupServiceExtensions` class provides utilities for managing backup operations, including creating daily backups, querying backup metadata, and retrieving backups by various criteria like labels, age, and file count.

### Usage Example

```csharp
using Services;

class Program
{
    static async Task Main()
    {
        var backupService = new BackupService();
        
        // Create daily backup
        var dailyBackup = await BackupServiceExtensions.CreateDailyBackupAsync(backupService);
        Console.WriteLine($"Created backup with ID: {dailyBackup.Id}");
        
        // Check for existing backups
        if (BackupServiceExtensions.HasBackupWithLabel("critical"))
        {
            var latestBackup = BackupServiceExtensions.GetLatestBackup();
            Console.WriteLine($"Latest backup has {latestBackup.GetTotalFileCount()} files");
            Console.WriteLine($"Total backup age: {BackupServiceExtensions.GetTotalAge().TotalDays:F1} days");
        }

        // Find backups by label pattern
        var patternBackups = BackupServiceExtensions.GetBackupsByLabelPattern("daily-2024");
        Console.WriteLine($"Found {patternBackups.Count} backups matching pattern");
        
        // Get backups sorted by age
        var sortedBackups = BackupServiceExtensions.GetBackupsByAgeAscending();
        Console.WriteLine($"\nOldest backup: {sortedBackups.First().CreationTime}");
        
        // Get backups in date range
        var rangeBackups = BackupServiceExtensions.GetBackupsInRange(
            DateTime.Now.AddDays(-7), 
            DateTime.Now
        );
        Console.WriteLine($"Found {rangeBackups.Count} backups in last 7 days");
    }
}
```
