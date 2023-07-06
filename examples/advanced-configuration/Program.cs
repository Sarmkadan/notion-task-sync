#nullable enable
using System;
using System.IO;
using NotionTaskSync.Configuration;

class Program
{
    static void Main()
    {
        // Complex configuration example
        var config = new SyncConfiguration
        {
            Profiles = new List<SyncProfile>
            {
                new SyncProfile
                {
                    Name = "WorkProfile",
                    NotionDatabaseId = "database-id-1",
                    LocalPath = "./work-tasks",
                    ConflictResolution = ConflictResolutionStrategy.LatestWins,
                    AutoBackup = true
                },
                new SyncProfile
                {
                    Name = "PersonalProfile",
                    NotionDatabaseId = "database-id-2",
                    LocalPath = "./personal-tasks",
                    ConflictResolution = ConflictResolutionStrategy.Manual,
                    AutoBackup = false
                }
            }
        };

        // Save configuration
        var json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText("complex-config.json", json);

        Console.WriteLine("Complex configuration saved!");
    }
}