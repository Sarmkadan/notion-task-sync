#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace NotionTaskSync.Tests;

using System;
using System.Collections.Generic;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using NotionTaskSync.Infrastructure.Configuration;
using Xunit;

/// <summary>
/// Tests that <see cref="DependencyInjection.ValidateConfiguration"/> enforces the property-mapping checks at startup.
/// </summary>
public class DependencyInjectionValidationTests
{
    private static IConfiguration Build(params KeyValuePair<string, string?>[] extra)
    {
        var entries = new List<KeyValuePair<string, string?>>
        {
            new("NotionApi:ApiKey", "test-api-key"),
            new("NotionApi:PropertyMappings:Title", "Name"),
            new("NotionApi:PropertyMappings:Status", "Status"),
            new("AppSettings:LocalTasksDirectory", "/path/to/tasks"),
        };
        entries.AddRange(extra);

        return new ConfigurationBuilder()
            .AddInMemoryCollection(entries)
            .Build();
    }

    [Fact]
    public void ValidateConfiguration_ValidMappings_DoesNotThrow()
    {
        var act = () => DependencyInjection.ValidateConfiguration(Build());

        act.Should().NotThrow();
    }

    [Fact]
    public void ValidateConfiguration_MissingRequiredMapping_ThrowsWithPropertyName()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["NotionApi:ApiKey"] = "test-api-key",
                ["NotionApi:PropertyMappings:Title"] = "Name",
                ["AppSettings:LocalTasksDirectory"] = "/path/to/tasks",
            })
            .Build();

        var act = () => DependencyInjection.ValidateConfiguration(configuration);

        act.Should().Throw<ConfigurationException>()
            .WithMessage("*Required property mapping 'Status' is missing.*");
    }

    [Fact]
    public void ValidateConfiguration_StatusNamesDifferingOnlyByCase_ThrowsWithProblem()
    {
        var configuration = Build(
            new("NotionApi:StatusNameMappings:0:Name", "Done"),
            new("NotionApi:StatusNameMappings:0:NotionName", "Done"),
            new("NotionApi:StatusNameMappings:1:Name", "done"),
            new("NotionApi:StatusNameMappings:1:NotionName", "Completed"));

        var act = () => DependencyInjection.ValidateConfiguration(configuration);

        act.Should().Throw<ConfigurationException>()
            .WithMessage("*Status name keys 'Done', 'done' differ only by case*");
    }
}
