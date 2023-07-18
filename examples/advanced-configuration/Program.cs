#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using NotionTaskSync.Domain.Models;

class Program
{
    static void Main()
    {
        // Complex configuration example - multiple sync profiles
        var profiles = new List<SyncConfig>
        {
            new SyncConfig("WorkProfile", "database-id-1", "./work-tasks")
            {
                ConflictStrategy = ConflictResolutionStrategy.LastWrite,
                IsEnabled = true
            },
            new SyncConfig("PersonalProfile", "database-id-2", "./personal-tasks")
            {
                ConflictStrategy = ConflictResolutionStrategy.Manual,
                IsEnabled = false
            }
        };

        // Save configuration
        var json = JsonSerializer.Serialize(profiles, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText("complex-config.json", json);

        Console.WriteLine("Complex configuration saved!");
    }
}
