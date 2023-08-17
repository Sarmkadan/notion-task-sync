# IntegrationExample

`IntegrationExample` provides a collection of static helper methods and instance properties that illustrate common patterns for synchronizing data between a Notion workspace and a local file system using the `notion-task-sync` library. The type is intended for demonstration and testing purposes; its members showcase how to configure and run synchronization in ASP.NET Core applications, background services, event‑driven scenarios, and multiple profile setups.

## API

### `public static async Task RunMinimalAspNetCoreIntegration()`
**Purpose:** Boots a minimal ASP.NET Core host, registers the synchronization services with default settings, and runs a single sync cycle. Useful for quick integration tests or sample applications.  
**Parameters:** None.  
**Return Value:** A `Task` that completes when the host has shut down after the sync operation.  
**Exceptions:**  
- `InvalidOperationException` if required configuration (e.g., Notion token) is missing.  
- Any exception thrown by the underlying sync pipeline is propagated wrapped in a `Task`.

### `public static void ShowControllerIntegrationPattern()`
**Purpose:** Writes to the console a typical controller‑based integration pattern, showing how to inject a sync service into an ASP.NET Core MVC controller and trigger a sync on demand.  
**Parameters:** None.  
**Return Value:** None.  
**Exceptions:** None (the method only performs console output).

### `public static async Task RunBackgroundServiceExample()`
**Purpose:** Demonstrates hosting the sync logic inside a .NET `BackgroundService`. The method builds a host, adds the hosted service, starts it, waits for a configurable interval, then stops the host.  
**Parameters:** None.  
**Return Value:** A `Task` that completes when the background service has been stopped.  
**Exceptions:**  
- `InvalidOperationException` if the background service fails to start.  
- Sync‑related exceptions are propagated as part of the returned task.

### `public static void ShowEventDrivenIntegration()`
**Purpose:** Prints an example of wiring the sync service to .NET events (e.g., file system watcher) so that synchronization is triggered by external changes.  
**Parameters:** None.  
**Return Value:** None.  
**Exceptions:** None.

### `public static void ShowMultipleSyncProfiles()`
**Purpose:** Illustrates how to define and run several named synchronization profiles, each with its own Notion database, local folder, and direction.  
**Parameters:** None.  
**Return Value:** None.  
**Exceptions:** None.

### `public SyncBackgroundService SyncBackgroundService { get; }`
**Purpose:** Provides access to the configured `SyncBackgroundService` instance that performs the actual synchronization work. This property is populated after the `IntegrationExample` object is initialized with valid settings.  
**Parameters:** None.  
**Return Value:** An instance of `SyncBackgroundService` ready to be started or inspected.  
**Exceptions:**  
- `InvalidOperationException` if the property is accessed before `Name`, `DatabaseId`, `LocalPath`, and `Direction` have been set.

### `public static void ShowControllerExample()`
**Purpose:** Outputs a concise code snippet showing how to register the sync service in `Startup.cs` and call it from a controller action.  
**Parameters:** None.  
**Return Value:** None.  
**Exceptions:** None.

### `public static async Task Main(string[] args)`
**Purpose:** Entry point for a console application that runs a full synchronization cycle using the values of the instance properties (`Name`, `DatabaseId`, `LocalPath`, `Direction`).  
**Parameters:** `args` – command‑line arguments (currently unused).  
**Return Value:** A `Task` representing the asynchronous operation.  
**Exceptions:**  
- `InvalidOperationException` if any required property is null or empty.  
- Any exception thrown during the sync process is propagated.

### `public string Name { get; set; }`
**Purpose:** A user‑friendly identifier for the synchronization configuration (e.g., “Tasks‑Sync”).  
**Parameters:** None.  
**Return Value:** The current name string.  
**Exceptions:** None.

### `public string DatabaseId { get; set; }`
**Purpose:** The Notion database ID that serves as the source or target of the sync operation.  
**Parameters:** None.  
**Return Value:** The current database ID string.  
**Exceptions:** None.

### `public string LocalPath { get; set; }`
**Purpose:** The local file system path where files are read from or written to during synchronization.  
**Parameters:** None.  
**Return Value:** The current local path string.  
**Exceptions:** None.

### `public SyncDirection? Direction { get; set; }`
**Purpose:** Indicates the direction of synchronization. `null` defaults to bidirectional sync; otherwise specifies `Upload`, `Download`, or `Bidirectional`.  
**Parameters:** None.  
**Return Value:** The current `SyncDirection` value or `null`.  
**Exceptions:** None.

## Usage

### Example 1: Running a minimal ASP.NET Core integration
```csharp
using NotionTaskSync;

// Configure the integration example
var example = new IntegrationExample
{
    Name = "DemoSync",
    DatabaseId = "1234567890abcdef",
    LocalPath = @"C:\Data\NotionSync",
    Direction = SyncDirection.Bidirectional
};

// Start the minimal ASP.NET Core host and perform a sync
await IntegrationExample.RunMinimalAspNetCoreIntegration();
```

### Example 2: Using the background service pattern in a console app
```csharp
using System.Threading.Tasks;
using NotionTaskSync;

class Program
{
    static async Task Main(string[] args)
    {
        // Set up the example with desired settings
        var example = new IntegrationExample
        {
            Name = "BackgroundSync",
            DatabaseId = "fedcba0987654321",
            LocalPath = @"/var/notion-sync",
            Direction = SyncDirection.Upload
        };

        // Run the background service demo; it will sync periodically
        await IntegrationExample.RunBackgroundServiceExample();
    }
}
```

## Notes

- **Required configuration:** The instance properties `Name`, `DatabaseId`, `LocalPath`, and `Direction` must be set before invoking any instance‑dependent members (e.g., accessing `SyncBackgroundService` or calling `Main`). Failure to do so results in an `InvalidOperationException`.
- **Thread safety:**  
  - All static methods are safe to call concurrently from multiple threads as they do not mutate shared state; each call creates its own host or service scope.  
  - Instance properties are **not** thread‑safe. If the same `IntegrationExample` instance is accessed from multiple threads simultaneously, external synchronization is required.  
  - The `SyncBackgroundService` returned by the property is intended for use after the object is fully configured; calling its start/stop methods from multiple threads without coordination may lead to undefined behavior.
- **Direction semantics:** When `Direction` is `null`, the underlying sync pipeline treats the operation as bidirectional. Explicitly setting `Upload` or `Download` restricts data flow accordingly; attempting to sync in an unsupported direction (e.g., uploading when the local path is read‑only) will cause the sync service to throw an `IOException`.
- **Exception propagation:** Async methods return `Task` objects that surface any exceptions thrown during execution. Callers should `await` these tasks and handle exceptions appropriately (e.g., logging, graceful shutdown).  
- **Console output:** The `Show*` methods are purely illustrative; they write to standard output and do not perform any synchronization. They are useful for documentation or quick demos but should not be relied upon in production code.
