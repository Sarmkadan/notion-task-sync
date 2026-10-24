# PropertyMappingOptions
`PropertyMappingOptions` holds the property-mapping configuration that `PropertyMapper` checks. It is built from `NotionApiSettings.PropertyMappings` and `NotionApiSettings.StatusNameMappings` at startup.

## API
* `Properties`: A dictionary from local field names (for example `Title`) to Notion property names (for example `Name`).
* `StatusNames`: A list of `StatusNameMapping` entries from a local status name to a Notion status option name. It is a list rather than a dictionary so that names differing only by case stay visible to validation.
* `StatusNameMapping.Name`: The local status name.
* `StatusNameMapping.NotionName`: The Notion status option name.

## Checks
`PropertyMapper.ValidateMappingOptions(options)` runs without network access and reports:
* A missing `Title` or `Status` mapping. These are required.
* An empty field name or empty Notion property name.
* One Notion property mapped by more than one field.
* Status names that differ only by case, or that are repeated.
* Empty status names or empty Notion status names.

`DependencyInjection.ValidateConfiguration` runs these checks at startup and throws `ConfigurationException` listing every problem.

`PropertyMapper.ValidateAgainstDatabaseAsync(client, databaseId, options)` fetches the database schema through `NotionClient.GetDatabaseAsync` and reports:
* A mapped Notion property that does not exist in the database. Comparison is exact and case-sensitive.
* A `Title` mapping whose Notion type is not `title`, or a `Status` mapping whose type is not `status`.

Other fields are checked for existence only. Transport and authentication errors are not converted to problems; they propagate as `NotionApiException` or `HttpRequestException`. This method is not called at startup.

## Usage
```csharp
var mapper = new PropertyMapper();
var options = new PropertyMappingOptions
{
    Properties = new Dictionary<string, string> { ["Title"] = "Name", ["Status"] = "Status" },
};

IReadOnlyList<string> offline = mapper.ValidateMappingOptions(options);
IReadOnlyList<string> schema = await mapper.ValidateAgainstDatabaseAsync(client, databaseId, options);
```

## Notes
* These mappings are validated but not applied. The sync pass does not read `PropertyMappings` (see the README), so a misconfigured mapping can stop startup without changing sync behavior.
