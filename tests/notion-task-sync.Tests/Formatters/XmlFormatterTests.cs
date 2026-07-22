#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
//
// Unit tests for XmlFormatter class.
// Tests XML serialization/deserialization, validation, and formatting methods.
// =============================================================================

namespace NotionTaskSync.Tests.Formatters;

using NotionTaskSync.Formatters;
using NotionTaskSync.Domain.Models;
using FluentAssertions;
using Xunit;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Xml.Linq;

/// <summary>
/// Contains unit tests for the <see cref="XmlFormatter"/> class.
/// Tests XML serialization, deserialization, validation, and formatting operations.
/// </summary>
public class XmlFormatterTests
{
    private readonly Mock<ILogger<XmlFormatter>> _mockLogger;
    private readonly XmlFormatter _formatter;

    public XmlFormatterTests()
    {
        _mockLogger = new Mock<ILogger<XmlFormatter>>();
        _formatter = new XmlFormatter(_mockLogger.Object);
    }

    [Fact]
    public void FormatTask_SerializesTaskToValidXmlElement()
    {
        var task = new Task
        {
            Id = Guid.NewGuid(),
            Title = "Test Task",
            Description = "This is a test description",
            Status = TaskStatus.Todo,
            Priority = 50,
            CreatedAt = new DateTime(2024, 1, 1, 10, 0, 0, DateTimeKind.Utc),
            UpdatedAt = new DateTime(2024, 1, 2, 15, 30, 0, DateTimeKind.Utc),
            DueDate = new DateTime(2024, 1, 10, 0, 0, 0, DateTimeKind.Utc),
            AssignedTo = "test-user",
            Tags = "test,important",
            NotionPageId = "page-123",
            IsDeleted = false
        };

        var xmlElement = _formatter.FormatTask(task);

        xmlElement.Should().NotBeNull();
        xmlElement.Name.LocalName.Should().Be("Task");
        xmlElement.Attribute("id")?.Value.Should().Be(task.Id.ToString());
        xmlElement.Element("Title")?.Value.Should().Be("Test Task");
        xmlElement.Element("Description")?.Value.Should().Be("This is a test description");
        xmlElement.Element("Status")?.Value.Should().Be("Todo");
        xmlElement.Element("Priority")?.Value.Should().Be("50");
        xmlElement.Element("CreatedAt")?.Value.Should().Be(new DateTime(2024, 1, 1, 10, 0, 0, DateTimeKind.Utc).ToString("O"));
        xmlElement.Element("UpdatedAt")?.Value.Should().Be(new DateTime(2024, 1, 2, 15, 30, 0, DateTimeKind.Utc).ToString("O"));
        xmlElement.Element("DueDate")?.Value.Should().Be(new DateTime(2024, 1, 10, 0, 0, 0, DateTimeKind.Utc).ToString("O"));
        xmlElement.Element("AssignedTo")?.Value.Should().Be("test-user");
        xmlElement.Element("Tags")?.Value.Should().Be("test,important");
        xmlElement.Element("NotionPageId")?.Value.Should().Be("page-123");
        xmlElement.Element("IsDeleted")?.Value.Should().Be("false");
    }

    [Fact]
    public void FormatTask_HandlesNullDescription()
    {
        var task = new Task
        {
            Id = Guid.NewGuid(),
            Title = "Task with null description",
            Description = null,
            Status = TaskStatus.InProgress,
            Priority = 30,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var xmlElement = _formatter.FormatTask(task);

        xmlElement.Should().NotBeNull();
        xmlElement.Element("Description")?.Value.Should().Be("");
    }

    [Fact]
    public void FormatTask_HandlesEmptyDescription()
    {
        var task = new Task
        {
            Id = Guid.NewGuid(),
            Title = "Task with empty description",
            Description = "",
            Status = TaskStatus.InProgress,
            Priority = 30,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var xmlElement = _formatter.FormatTask(task);

        xmlElement.Should().NotBeNull();
        xmlElement.Element("Description")?.Value.Should().Be("");
    }

    [Fact]
    public void FormatTasks_SerializesTaskListToValidXmlDocument()
    {
        var tasks = new List<Task>
        {
            new Task
            {
                Id = Guid.NewGuid(),
                Title = "Task 1",
                Description = "First task",
                Status = TaskStatus.Todo,
                Priority = 10,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new Task
            {
                Id = Guid.NewGuid(),
                Title = "Task 2",
                Description = "Second task",
                Status = TaskStatus.InProgress,
                Priority = 20,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }
        };

        var xml = _formatter.FormatTasks(tasks);

        xml.Should().NotBeNullOrWhiteSpace();
        xml.Should().Contain("<Tasks");
        xml.Should().Contain("<Task");
        xml.Should().Contain("</Tasks>");

        Action act = () => XDocument.Parse(xml);
        act.Should().NotThrow();
    }

    [Fact]
    public void FormatTasks_HandlesEmptyTaskList()
    {
        var tasks = new List<Task>();
        var xml = _formatter.FormatTasks(tasks);

        xml.Should().NotBeNullOrWhiteSpace();
        xml.Should().Contain("<Tasks");
        xml.Should().Contain("count=\"0\"");

        Action act = () => XDocument.Parse(xml);
        act.Should().NotThrow();

        var parsedTasks = _formatter.ParseTasks(xml);
        parsedTasks.Should().NotBeNull();
        parsedTasks.Should().BeEmpty();
    }

    [Fact]
    public void ParseTasks_DeserializesValidXmlToTaskList()
    {
        var originalTasks = new List<Task>
        {
            new Task
            {
                Id = Guid.NewGuid(),
                Title = "Original Task 1",
                Description = "Original description 1",
                Status = TaskStatus.Todo,
                Priority = 10,
                CreatedAt = new DateTime(2024, 1, 1, 10, 0, 0, DateTimeKind.Utc),
                UpdatedAt = new DateTime(2024, 1, 2, 15, 30, 0, DateTimeKind.Utc),
                AssignedTo = "user1",
                Tags = "tag1",
                NotionPageId = "page1",
                IsDeleted = false
            },
            new Task
            {
                Id = Guid.NewGuid(),
                Title = "Original Task 2",
                Description = "Original description 2",
                Status = TaskStatus.Done,
                Priority = 20,
                CreatedAt = new DateTime(2024, 1, 3, 11, 0, 0, DateTimeKind.Utc),
                UpdatedAt = new DateTime(2024, 1, 4, 16, 30, 0, DateTimeKind.Utc),
                DueDate = new DateTime(2024, 1, 15, 0, 0, 0, DateTimeKind.Utc),
                AssignedTo = "user2",
                Tags = "tag2",
                NotionPageId = "page2",
                IsDeleted = true,
                DeletedAt = new DateTime(2024, 1, 5, 0, 0, 0, DateTimeKind.Utc)
            }
        };

        var xml = _formatter.FormatTasks(originalTasks);
        var parsedTasks = _formatter.ParseTasks(xml);

        parsedTasks.Should().NotBeNull();
        parsedTasks.Should().HaveCount(2);
        parsedTasks[0].Title.Should().Be("Original Task 1");
        parsedTasks[0].Description.Should().Be("Original description 1");
        parsedTasks[0].Status.Should().Be(TaskStatus.Todo);
        parsedTasks[0].Priority.Should().Be(10);
        parsedTasks[0].AssignedTo.Should().Be("user1");
        parsedTasks[0].Tags.Should().Be("tag1");
        parsedTasks[0].NotionPageId.Should().Be("page1");
        parsedTasks[0].IsDeleted.Should().BeFalse();

        parsedTasks[1].Title.Should().Be("Original Task 2");
        parsedTasks[1].Description.Should().Be("Original description 2");
        parsedTasks[1].Status.Should().Be(TaskStatus.Done);
        parsedTasks[1].Priority.Should().Be(20);
        parsedTasks[1].DueDate.Should().Be(new DateTime(2024, 1, 15, 0, 0, 0, DateTimeKind.Utc));
        parsedTasks[1].AssignedTo.Should().Be("user2");
        parsedTasks[1].Tags.Should().Be("tag2");
        parsedTasks[1].NotionPageId.Should().Be("page2");
        parsedTasks[1].IsDeleted.Should().BeTrue();
    }

    [Fact]
    public void ParseTasks_HandlesNullOptionalFields()
    {
        var xml = $"<?xml version=\"1.0\" encoding=\"utf-8\"?><Tasks count=\"1\"><Task id=\"{Guid.NewGuid()}\"><Title>Minimal Task</Title><Description></Description><Status>Todo</Status><Priority>0</Priority><CreatedAt>{DateTime.UtcNow.ToString("O")}</CreatedAt><UpdatedAt>{DateTime.UtcNow.ToString("O")}</UpdatedAt><IsDeleted>false</IsDeleted></Task></Tasks>";

        var parsedTasks = _formatter.ParseTasks(xml);

        parsedTasks.Should().NotBeNull();
        parsedTasks.Should().HaveCount(1);
        var task = parsedTasks[0];
        task.AssignedTo.Should().BeNull();
        task.Tags.Should().BeNull();
        task.NotionPageId.Should().BeNull();
        task.DueDate.Should().BeNull();
        task.CompletedAt.Should().BeNull();
    }

    [Fact]
    public void ParseTasks_HandlesOptionalDateFields()
    {
        var taskId = Guid.NewGuid();
        var dueDate = new DateTime(2024, 6, 15, 0, 0, 0, DateTimeKind.Utc);
        var completedAt = new DateTime(2024, 6, 20, 0, 0, 0, DateTimeKind.Utc);
        var createdAt = new DateTime(2024, 6, 1, 0, 0, 0, DateTimeKind.Utc);
        var updatedAt = new DateTime(2024, 6, 21, 0, 0, 0, DateTimeKind.Utc);

        var xml = $"<?xml version=\"1.0\" encoding=\"utf-8\"?><Tasks count=\"1\"><Task id=\"{taskId}\"><Title>Task with dates</Title><Description>Description</Description><Status>Done</Status><Priority>50</Priority><CreatedAt>{createdAt.ToString("O")}</CreatedAt><UpdatedAt>{updatedAt.ToString("O")}</UpdatedAt><DueDate>{dueDate.ToString("O")}</DueDate><CompletedAt>{completedAt.ToString("O")}</CompletedAt><IsDeleted>false</IsDeleted></Task></Tasks>";

        var parsedTasks = _formatter.ParseTasks(xml);

        parsedTasks.Should().NotBeNull();
        parsedTasks.Should().HaveCount(1);
        parsedTasks[0].DueDate.Should().Be(dueDate);
        parsedTasks[0].CompletedAt.Should().Be(completedAt);
    }

    [Fact]
    public void IsValidXml_IdentifiesValidXml()
    {
        var validXml = "<?xml version=\"1.0\"?><Root><Element>value</Element></Root>";
        _formatter.IsValidXml(validXml).Should().BeTrue();
    }

    [Fact]
    public void IsValidXml_IdentifiesInvalidXml()
    {
        var invalidXml = "<Root><Element>value</WrongTag>";
        _formatter.IsValidXml(invalidXml).Should().BeFalse();
    }

    [Fact]
    public void IsValidXml_HandlesNullAndEmptyStrings()
    {
        _formatter.IsValidXml(null).Should().BeFalse();
        _formatter.IsValidXml("").Should().BeFalse();
        _formatter.IsValidXml(" ").Should().BeFalse();
    }

    [Fact]
    public void FormatTask_HandlesAllStatusValues()
    {
        foreach (TaskStatus status in Enum.GetValues(typeof(TaskStatus)))
        {
            var task = new Task
            {
                Id = Guid.NewGuid(),
                Title = $"Task with status {status}",
                Status = status,
                Priority = 50,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var xmlElement = _formatter.FormatTask(task);
            xmlElement.Should().NotBeNull();
            xmlElement.Element("Status")?.Value.Should().Be(status.ToString());

            var xmlString = xmlElement.ToString();
            var parsedTasks = _formatter.ParseTasks($"<?xml version=\"1.0\" encoding=\"utf-8\"?><Tasks count=\"1\">{xmlString}</Tasks>");
            parsedTasks.Should().NotBeNull();
            parsedTasks.Should().HaveCount(1);
            parsedTasks[0].Status.Should().Be(status);
        }
    }

    [Fact]
    public void FormatTasks_IncludesCorrectCountAttribute()
    {
        var singleTask = new List<Task>
        {
            new Task { Id = Guid.NewGuid(), Title = "Single Task", Status = TaskStatus.Todo, Priority = 10, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow }
        };

        var multipleTasks = new List<Task>
        {
            new Task { Id = Guid.NewGuid(), Title = "Task 1", Status = TaskStatus.Todo, Priority = 10, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new Task { Id = Guid.NewGuid(), Title = "Task 2", Status = TaskStatus.Todo, Priority = 20, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new Task { Id = Guid.NewGuid(), Title = "Task 3", Status = TaskStatus.Todo, Priority = 30, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow }
        };

        var singleXml = _formatter.FormatTasks(singleTask);
        var multipleXml = _formatter.FormatTasks(multipleTasks);

        singleXml.Should().Contain("count=\"1\"");
        multipleXml.Should().Contain("count=\"3\"");
    }

    [Fact]
    public void ParseTasks_HandlesMissingOptionalElements()
    {
        var taskId = Guid.NewGuid();
        var xml = $"<?xml version=\"1.0\" encoding=\"utf-8\"?><Tasks count=\"1\"><Task id=\"{taskId}\"><Title>Task without optional fields</Title><Status>Todo</Status><Priority>0</Priority><CreatedAt>{DateTime.UtcNow.ToString("O")}</CreatedAt><UpdatedAt>{DateTime.UtcNow.ToString("O")}</UpdatedAt><IsDeleted>false</IsDeleted></Task></Tasks>";

        var parsedTasks = _formatter.ParseTasks(xml);

        parsedTasks.Should().NotBeNull();
        parsedTasks.Should().HaveCount(1);
        var task = parsedTasks[0];
        task.AssignedTo.Should().BeNull();
        task.Tags.Should().BeNull();
        task.NotionPageId.Should().BeNull();
        task.DueDate.Should().BeNull();
        task.CompletedAt.Should().BeNull();
    }

    [Fact]
    public void FormatTask_EscapesSpecialCharactersInXml()
    {
        var task = new Task
        {
            Id = Guid.NewGuid(),
            Title = "Task with <special> & \"quotes\" characters",
            Description = "Description with <xml> & \"quotes\" and special chars",
            Status = TaskStatus.Todo,
            Priority = 50,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var xmlElement = _formatter.FormatTask(task);
        xmlElement.Should().NotBeNull();

        var xmlString = xmlElement.ToString();
        xmlString.Should().NotBeNullOrWhiteSpace();

        Action act = () => XDocument.Parse(xmlString);
        act.Should().NotThrow();

        var parsedTasks = _formatter.ParseTasks($"<?xml version=\"1.0\" encoding=\"utf-8\"?><Tasks count=\"1\">{xmlString}</Tasks>");
        parsedTasks.Should().NotBeNull();
        parsedTasks.Should().HaveCount(1);
        parsedTasks[0].Title.Should().Contain("special");
    }

    [Fact]
    public void FormatTasks_EmptyListProducesValidEmptyDocument()
    {
        var tasks = new List<Task>();
        var xml = _formatter.FormatTasks(tasks);

        xml.Should().NotBeNullOrWhiteSpace();
        xml.Should().Contain("<Tasks");
        xml.Should().Contain("count=\"0\"");

        Action act = () => XDocument.Parse(xml);
        act.Should().NotThrow();

        var parsedTasks = _formatter.ParseTasks(xml);
        parsedTasks.Should().NotBeNull();
        parsedTasks.Should().BeEmpty();
    }

    [Fact]
    public void FormatTask_HandlesUnicodeCharactersInXml()
    {
        var task = new Task
        {
            Id = Guid.NewGuid(),
            Title = "Task with unicode: 你好 🎉 Привет",
            Description = "Description with émojis 🚀",
            Status = TaskStatus.Todo,
            Priority = 50,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var xmlElement = _formatter.FormatTask(task);
        xmlElement.Should().NotBeNull();

        var xmlString = xmlElement.ToString();
        xmlString.Should().NotBeNullOrWhiteSpace();

        Action act = () => XDocument.Parse(xmlString);
        act.Should().NotThrow();

        var parsedTasks = _formatter.ParseTasks($"<?xml version=\"1.0\" encoding=\"utf-8\"?><Tasks count=\"1\">{xmlString}</Tasks>");
        parsedTasks.Should().NotBeNull();
        parsedTasks.Should().HaveCount(1);
        parsedTasks[0].Title.Should().Contain("你好");
    }

    [Fact]
    public void ParseTasks_HandlesMalformedXmlGracefully()
    {
        var malformedXml = "<Tasks><Task><Title>Valid</Title></Task><Task><Title>Invalid";

        var parsedTasks = _formatter.ParseTasks(malformedXml);
        parsedTasks.Should().NotBeNull();
        parsedTasks.Should().BeEmpty();
    }
}