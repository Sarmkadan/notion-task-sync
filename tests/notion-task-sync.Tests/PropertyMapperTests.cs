#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace NotionTaskSync.Tests;

using System;
using System.Collections.Generic;
using FluentAssertions;
using NotionTaskSync.Data.Mappers;
using Xunit;

/// <summary>
/// Unit tests for the offline checks in <see cref="PropertyMapper.ValidateMappingOptions"/>.
/// </summary>
public class PropertyMapperTests
{
    private readonly PropertyMapper _mapper = new();

    private static PropertyMappingOptions ValidOptions() => new()
    {
        Properties = new Dictionary<string, string>
        {
            ["Title"] = "Name",
            ["Status"] = "Status",
            ["Priority"] = "Priority",
        },
    };

    private static PropertyMappingOptions.StatusNameMapping Status(string name, string notionName) =>
        new() { Name = name, NotionName = notionName };

    [Fact]
    public void ValidateMappingOptions_ValidOptions_ReturnsNoProblems()
    {
        var problems = _mapper.ValidateMappingOptions(ValidOptions());

        problems.Should().BeEmpty();
    }

    [Fact]
    public void ValidateMappingOptions_EmptyOptions_ReportsBothRequiredMappings()
    {
        var problems = _mapper.ValidateMappingOptions(new PropertyMappingOptions());

        problems.Should().Contain("Required property mapping 'Title' is missing.");
        problems.Should().Contain("Required property mapping 'Status' is missing.");
        problems.Should().HaveCount(2);
    }

    [Fact]
    public void ValidateMappingOptions_MissingStatus_ReportsStatusOnly()
    {
        var options = ValidOptions();
        options.Properties.Remove("Status");

        var problems = _mapper.ValidateMappingOptions(options);

        problems.Should().ContainSingle()
            .Which.Should().Be("Required property mapping 'Status' is missing.");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void ValidateMappingOptions_BlankNotionName_IsReported(string notionName)
    {
        var options = ValidOptions();
        options.Properties["Priority"] = notionName;

        var problems = _mapper.ValidateMappingOptions(options);

        problems.Should().ContainSingle()
            .Which.Should().Be("Property mapping 'Priority' has an empty Notion property name.");
    }

    [Fact]
    public void ValidateMappingOptions_BlankFieldName_IsReported()
    {
        var options = ValidOptions();
        options.Properties[" "] = "Notes";

        var problems = _mapper.ValidateMappingOptions(options);

        problems.Should().Contain("A property mapping has an empty field name.");
    }

    [Fact]
    public void ValidateMappingOptions_TwoFieldsOnSameNotionProperty_ReportsBothFields()
    {
        var options = ValidOptions();
        options.Properties["DueDate"] = "Priority";

        var problems = _mapper.ValidateMappingOptions(options);

        problems.Should().ContainSingle()
            .Which.Should().Be("Notion property 'Priority' is mapped by more than one field: DueDate, Priority.");
    }

    [Fact]
    public void ValidateMappingOptions_DuplicateStatusNamesDifferingOnlyByCase_IsReported()
    {
        var options = ValidOptions();
        options.StatusNames.Add(Status("InProgress", "In progress"));
        options.StatusNames.Add(Status("inprogress", "In Progress"));

        var problems = _mapper.ValidateMappingOptions(options);

        problems.Should().ContainSingle()
            .Which.Should().Be("Status name keys 'InProgress', 'inprogress' differ only by case; keep one spelling.");
    }

    [Fact]
    public void ValidateMappingOptions_ExactlyDuplicateStatusNames_AreReportedAsDuplicates()
    {
        var options = ValidOptions();
        options.StatusNames.Add(Status("Done", "Done"));
        options.StatusNames.Add(Status("Done", "Completed"));

        var problems = _mapper.ValidateMappingOptions(options);

        problems.Should().ContainSingle()
            .Which.Should().Be("Status name 'Done' is mapped more than once.");
    }

    [Fact]
    public void ValidateMappingOptions_DistinctStatusNames_AreAccepted()
    {
        var options = ValidOptions();
        options.StatusNames.Add(Status("Todo", "Not started"));
        options.StatusNames.Add(Status("Done", "Done"));

        var problems = _mapper.ValidateMappingOptions(options);

        problems.Should().BeEmpty();
    }

    [Fact]
    public void ValidateMappingOptions_EmptyStatusFields_AreReported()
    {
        var options = ValidOptions();
        options.StatusNames.Add(Status("", "Done"));
        options.StatusNames.Add(Status("Blocked", " "));

        var problems = _mapper.ValidateMappingOptions(options);

        problems.Should().Contain("A status name mapping has an empty status name.");
        problems.Should().Contain("Status mapping 'Blocked' has an empty Notion status name.");
        problems.Should().HaveCount(2);
    }

    [Fact]
    public void ValidateMappingOptions_MultipleProblems_AreAllReported()
    {
        var options = new PropertyMappingOptions
        {
            Properties = new Dictionary<string, string>
            {
                ["Title"] = "Same",
                ["Priority"] = "Same",
                ["DueDate"] = "",
            },
            StatusNames = new List<PropertyMappingOptions.StatusNameMapping>
            {
                Status("Done", "Done"),
                Status("done", "Completed"),
            },
        };

        var problems = _mapper.ValidateMappingOptions(options);

        problems.Should().HaveCount(4);
        problems.Should().Contain("Required property mapping 'Status' is missing.");
        problems.Should().Contain("Property mapping 'DueDate' has an empty Notion property name.");
        problems.Should().Contain("Notion property 'Same' is mapped by more than one field: Priority, Title.");
        problems.Should().Contain("Status name keys 'Done', 'done' differ only by case; keep one spelling.");
    }

    [Fact]
    public void ValidateMappingOptions_NullOptions_Throws()
    {
        var act = () => _mapper.ValidateMappingOptions(null!);

        act.Should().Throw<ArgumentNullException>();
    }
}
