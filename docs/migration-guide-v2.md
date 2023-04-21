## Migration Guide to v2.0

### Breaking Changes
* Removed deprecated API endpoints
* Changed default database schema

### New Features
* Added support for real-time collaboration
* Improved conflict resolution strategies

### Step-by-Step Migration
1. Update your Notion database schema to the new format
2. Configure the new conflict resolution strategies
3. Test your application with the new version

### Code Examples
* Old API endpoint: `https://api.notion.com/v1/pages`
* New API endpoint: `https://api.notion.com/v2/pages`
* Example of new conflict resolution strategy: `latest-wins`