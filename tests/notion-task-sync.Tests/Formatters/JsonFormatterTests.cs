#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
//
// Unit tests for JsonFormatter class.
// Tests JSON serialization/deserialization, validation, and formatting methods.
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
using System.Text.Json;

/// <summary>
/// Contains unit tests for the <see cref="JsonFormatter"/> class.
/// Tests JSON serialization, deserialization, validation, and formatting operations.
/// </summary>
public class JsonFormatterTests
{
    private readonly Mock<ILogger<JsonFormatter>> _mockLogger;
    private readonly JsonFormatter _formatter;

    public JsonFormatterTests()
    {
        _mockLogger = new Mock<ILogger<JsonFormatter>>();
        _formatter = new JsonFormatter(_mockLogger.Object);
    }

    [Fact]
    public void FormatTask_SerializesTaskToValidJson()
    {
        var task = new Task
        {
            Id = Guid.NewGuid(),
            Title = "Test Task",
            Description = "Test description",
            Status = TaskStatus.Todo,
            Priority = 50,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var json = _formatter.FormatTask(task);

        json.Should().NotBeNullOrWhiteSpace();
        json.Should().Contain(task.Id.ToString());
        json.Should().Contain("\"Title\"");

        Action act = () => JsonDocument.Parse(json);
        act.Should().NotThrow();
    }

    [Fact]
    public void FormatTasks_SerializesTaskListToValidJsonArray()
    {
        var tasks = new List<Task>
        {
            new Task { Id = Guid.NewGuid(), Title = "Task 1", Status = TaskStatus.Todo, Priority = 10, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new Task { Id = Guid.NewGuid(), Title = "Task 2", Status = TaskStatus.InProgress, Priority = 20, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow }
        };

        var json = _formatter.FormatTasks(tasks);

        json.Should().NotBeNullOrWhiteSpace();
        json.Should().StartWith("[");
        json.Should().EndWith("]");

        Action act = () => JsonDocument.Parse(json);
        act.Should().NotThrow();

        var parsedTasks = _formatter.DeserializeTasks(json);
        parsedTasks.Should().NotBeNull();
        parsedTasks.Should().HaveCount(2);
    }

    [Fact]
    public void FormatTasks_HandlesEmptyTaskList()
    {
        var tasks = new List<Task>();
        var json = _formatter.FormatTasks(tasks);

        json.Should().Be("[]");

        Action act = () => JsonDocument.Parse(json);
        act.Should().NotThrow();

        var parsedTasks = _formatter.DeserializeTasks(json);
        parsedTasks.Should().NotBeNull();
        parsedTasks.Should().BeEmpty();
    }

    [Fact]
    public void DeserializeTask_DeserializesValidJsonToTask()
    {
        var originalTask = new Task
        {
            Id = Guid.NewGuid(),
            Title = "Original Task",
            Description = "Original description",
            Status = TaskStatus.Blocked,
            Priority = 75,
            CreatedAt = new DateTime(2024, 1, 1, 10, 0, 0, DateTimeKind.Utc),
            UpdatedAt = new DateTime(2024, 1, 2, 15, 30, 0, DateTimeKind.Utc)
        };

        var json = _formatter.FormatTask(originalTask);
        var deserializedTask = _formatter.DeserializeTask(json);

        deserializedTask.Should().NotBeNull();
        deserializedTask.Id.Should().Be(originalTask.Id);
        deserializedTask.Title.Should().Be(originalTask.Title);
    }

    [Fact]
    public void DeserializeTasks_DeserializesValidJsonArrayToTaskList()
    {
        var originalTasks = new List<Task>
        {
            new Task { Id = Guid.NewGuid(), Title = "Task A", Status = TaskStatus.Todo, Priority = 10, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new Task { Id = Guid.NewGuid(), Title = "Task B", Status = TaskStatus.InProgress, Priority = 20, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow }
        };

        var json = _formatter.FormatTasks(originalTasks);
        var deserializedTasks = _formatter.DeserializeTasks(json);

        deserializedTasks.Should().NotBeNull();
        deserializedTasks.Should().HaveCount(2);
        deserializedTasks[0].Title.Should().Be("Task A");
        deserializedTasks[1].Title.Should().Be("Task B");
    }

    [Fact]
    public void IsValidJson_IdentifiesValidJson()
    {
        var validJson = "{\"name\":\"test\",\"value\":42}";
        _formatter.IsValidJson(validJson).Should().BeTrue();
    }

    [Fact]
    public void IsValidJson_IdentifiesInvalidJson()
    {
        var invalidJson = "{name:test,value:42}";
        _formatter.IsValidJson(invalidJson).Should().BeFalse();
    }

    [Fact]
    public void IsValidJson_HandlesNullAndEmptyStrings()
    {
        _formatter.IsValidJson(null).Should().BeFalse();
        _formatter.IsValidJson("").Should().BeFalse();
        _formatter.IsValidJson(" ").Should().BeFalse();
    }

    [Fact]
    public void Minify_RemovesWhitespaceFromJson()
    {
        var prettyJson = "{\"name\":\"test\", \"value\": 42}";
        var minified = _formatter.Minify(prettyJson);

        minified.Should().NotContain(" ");
        minified.Should().NotContain("\t");
        minified.Should().NotContain("\n");
        Action act = () => JsonDocument.Parse(minified);
        act.Should().NotThrow();
    }

    [Fact]
    public void PrettyPrint_AddsWhitespaceToJson()
    {
        var minifiedJson = "{\"name\":\"test\",\"value\":42}";
        var pretty = _formatter.PrettyPrint(minifiedJson);

        pretty.Should().Contain("\n");
        pretty.Should().Contain(" ");
        Action act = () => JsonDocument.Parse(pretty);
        act.Should().NotThrow();
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

            var json = _formatter.FormatTask(task);
            Action act = () => JsonDocument.Parse(json);
            act.Should().NotThrow();

            var deserialized = _formatter.DeserializeTask(json);
            deserialized.Should().NotBeNull();
            deserialized.Status.Should().Be(status);
        }
    }

    [Fact]
    public void FormatTask_EscapesSpecialCharactersInJson()
    {
        var task = new Task
        {
            Id = Guid.NewGuid(),
            Title = "Task with \"quotes\" and 'apostrophes' and special chars: <>&\n\t",
            Description = "Description with \"quotes\" and \nnewlines and \ttabs",
            Status = TaskStatus.Todo,
            Priority = 50,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var json = _formatter.FormatTask(task);
        json.Should().NotBeNullOrWhiteSpace();

        Action act = () => JsonDocument.Parse(json);
        act.Should().NotThrow();

        var deserialized = _formatter.DeserializeTask(json);
        deserialized.Should().NotBeNull();
        deserialized.Title.Should().Contain("quotes");
        deserialized.Description.Should().Contain("quotes");
    }

    [Fact]
    public void FormatTasks_EmptyListProducesValidEmptyArray()
    {
        var tasks = new List<Task>();
        var json = _formatter.FormatTasks(tasks);

        json.Should().Be("[]");
        json.Should().NotBeNullOrWhiteSpace();

        Action act = () => JsonDocument.Parse(json);
        act.Should().NotThrow();

        var parsedTasks = _formatter.DeserializeTasks(json);
        parsedTasks.Should().NotBeNull();
        parsedTasks.Should().BeEmpty();
    }

    [Fact]
    public void FormatTask_HandlesNullValuesInTask()
    {
        var task = new Task
        {
            Id = Guid.NewGuid(),
            Title = "Task with null fields",
            Description = null,
            Status = TaskStatus.Todo,
            Priority = 50,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            NotionPageId = null,
            AssignedTo = null,
            Tags = null
        };

        var json = _formatter.FormatTask(task);
        json.Should().NotBeNullOrWhiteSpace();

        Action act = () => JsonDocument.Parse(json);
        act.Should().NotThrow();

        var deserialized = _formatter.DeserializeTask(json);
        deserialized.Should().NotBeNull();
        deserialized.Description.Should().BeNull();
        deserialized.NotionPageId.Should().BeNull();
        deserialized.AssignedTo.Should().BeNull();
        deserialized.Tags.Should().BeNull();
    }

    [Fact]
    public void FormatTask_HandlesUnicodeCharacters()
    {
        var task = new Task
        {
            Id = Guid.NewGuid(),
            Title = "Task with unicode: 你好 🎉 Привет",
            Description = "Description with émojis 🚀 and special chars: ©®™",
            Status = TaskStatus.Todo,
            Priority = 50,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var json = _formatter.FormatTask(task);
        json.Should().NotBeNullOrWhiteSpace();

        Action act = () => JsonDocument.Parse(json);
        act.Should().NotThrow();

        var deserialized = _formatter.DeserializeTask(json);
        deserialized.Should().NotBeNull();
        deserialized.Title.Should().Contain("你好");
        deserialized.Title.Should().Contain("🎉");
    }
}