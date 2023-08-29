# NotionMapper

The `NotionMapper` class serves as a static utility within the `notion-task-sync` project, responsible for translating data between the application's internal domain models and the JSON structures required by the Notion API. It handles the serialization of task data for creation and updates, parses incoming API responses into usable page objects, and provides normalization logic to ensure accurate state comparison during synchronization processes.

## API

### `ParseFromNotionResponse`
Converts a raw JSON response string received from the Notion API into a strongly typed `NotionPage` object.
*   **Parameters**: Accepts a single `string` containing the JSON payload.
*   **Return Value**: Returns an instance of `NotionPage` populated with the parsed data.
*   **Exceptions**: Throws a deserialization exception if the input string is null, empty, or contains malformed JSON that does not match the expected `NotionPage` schema.

### `MapToNotionUpdate`
Generates a dictionary representing the payload required to update an existing page in Notion.
*   **Parameters**: Accepts the internal task object (or relevant data structure) containing the modified fields.
*   **Return Value**: Returns a `Dictionary<string, object>` where keys correspond to Notion property IDs or names and values contain the formatted update data.
*   **Exceptions**: May throw an argument exception if the source object lacks required identifiers or contains invalid property types unsupported by the Notion API.

### `MapToNotionCreate`
Constructs a dictionary representing the payload required to create a new page in Notion.
*   **Parameters**: Accepts the internal task object intended for creation.
*   **Return Value**: Returns a `Dictionary<string, object>` structured according to the Notion "Create Page" endpoint requirements, including parent references and initial properties.
*   **Exceptions**: Throws an exception if mandatory fields for page creation (such as parent database ID or page title) are missing from the source object.

### `NormalizeRichTextForComparison`
Processes rich text content to a standardized string format suitable for equality checks during sync operations.
*   **Parameters**: Accepts a rich text object or collection representing formatted text from Notion.
*   **Return Value**: Returns a `string` containing the plain text content with consistent whitespace and formatting applied.
*   **Exceptions**: Returns an empty string if the input is null; does not typically throw exceptions unless the input type is fundamentally incompatible.

## Usage

### Example 1: Creating a New Task in Notion
The following example demonstrates how to map a local task object to a payload suitable for the Notion API create endpoint.

```csharp
using NotionTaskSync.Mapping;
using NotionTaskSync.Models;

public async Task CreateRemoteTaskAsync(LocalTask task)
{
    // Map the local task to the Notion API create payload
    var createPayload = NotionMapper.MapToNotionCreate(task);

    // Assume _notionClient is an injected service handling the HTTP request
    var responseJson = await _notionClient.PostAsync("pages", createPayload);
    
    // Parse the response to get the newly created page details
    var createdPage = NotionMapper.ParseFromNotionResponse(responseJson);
    
    Console.WriteLine($"Created page with ID: {createdPage.Id}");
}
```

### Example 2: Synchronizing Updates and Detecting Changes
This example illustrates updating an existing page and using the normalization helper to determine if a text field has actually changed before triggering an update.

```csharp
using NotionTaskSync.Mapping;
using NotionTaskSync.Models;

public async Task SyncTaskChangesAsync(LocalTask localTask, NotionPage remotePage)
{
    // Normalize rich text from both sources to compare actual content
    string localText = NotionMapper.NormalizeRichTextForComparison(localTask.Description);
    string remoteText = NotionMapper.NormalizeRichTextForComparison(remotePage.Properties["Description"]);

    if (localText != remoteText)
    {
        // Only map and send update if content differs
        var updatePayload = NotionMapper.MapToNotionUpdate(localTask);
        
        await _notionClient.PatchAsync($"pages/{remotePage.Id}", updatePayload);
    }
}
```

## Notes

*   **Thread Safety**: As `NotionMapper` exposes only static methods and maintains no internal mutable state, it is inherently thread-safe and can be called concurrently from multiple threads without external locking mechanisms.
*   **Null Handling**: While `NormalizeRichTextForComparison` gracefully handles null inputs by returning an empty string, `ParseFromNotionResponse` will fail on null or empty JSON strings. Callers should validate network responses before passing them to the parser.
*   **Schema Dependency**: The `MapToNotionCreate` and `MapToNotionUpdate` methods rely on the internal task object matching the expected schema for the current version of the Notion API. If the Notion API introduces breaking changes to property structures, these mapping methods may require updates to prevent runtime serialization errors.
*   **Comparison Logic**: The normalization logic in `NormalizeRichTextForComparison` strips formatting annotations. Consequently, two rich text blocks with identical visible text but different styling (e.g., bold vs. italic) will be considered equal by this method.
