# V2 Basic Usage Example

This example demonstrates the basic usage of Notion Task Sync v2.0 with real-time collaboration features.

## Prerequisites
- .NET 10 SDK
- Notion API token
- Notion database ID

## Setup

1. Navigate to the example directory:
```bash
cd examples/v2-basic-usage
```

2. Build the project:
```bash
dotnet build
```

3. Run the example:
```bash
dotnet run
```

## Features Demonstrated
- Basic sync operation
- Configuration loading
- Error handling

## Configuration

Create an `appsettings.json` file with your Notion credentials:

```json
{
  "NotionApi": {
    "ApiKey": "your_integration_token",
    "DatabaseId": "your_database_id"
  },
  "AppSettings": {
    "LocalTasksDirectory": "./tasks"
  }
}
```

## Next Steps
Try adding more complex sync scenarios by extending this basic example.
