#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace NotionTaskSync.Formatters;

using System;
using System.Collections.Generic;
using System.Xml.Linq;
using NotionTaskSync.Domain.Models;
using Microsoft.Extensions.Logging;

/// <summary>
/// Formats tasks as XML for data interchange and system integration.
/// Provides XML serialization for scenarios requiring XML format compatibility.
/// </summary>
public class XmlFormatter
{
    private readonly ILogger<XmlFormatter> _logger;

    public XmlFormatter(ILogger<XmlFormatter> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Formats a single task as an XML element.
    /// </summary>
    public XElement FormatTask(Task task)
    {
        try
        {
            var element = new XElement("Task",
                new XAttribute("id", task.Id),
                new XElement("Title", task.Title),
                new XElement("Description", task.Description ?? ""),
                new XElement("Status", task.Status.ToString()),
                new XElement("Priority", task.Priority),
                new XElement("CreatedAt", task.CreatedAt.ToString("O")),
                new XElement("UpdatedAt", task.UpdatedAt.ToString("O"))
            );

            if (task.DueDate.HasValue)
                element.Add(new XElement("DueDate", task.DueDate.Value.ToString("O")));

            if (task.CompletedAt.HasValue)
                element.Add(new XElement("CompletedAt", task.CompletedAt.Value.ToString("O")));

            if (!string.IsNullOrEmpty(task.AssignedTo))
                element.Add(new XElement("AssignedTo", task.AssignedTo));

            if (!string.IsNullOrEmpty(task.Tags))
                element.Add(new XElement("Tags", task.Tags));

            if (!string.IsNullOrEmpty(task.NotionPageId))
                element.Add(new XElement("NotionPageId", task.NotionPageId));

            element.Add(new XElement("IsDeleted", task.IsDeleted));

            return element;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to format task {TaskId} as XML", task.Id);
            throw;
        }
    }

    /// <summary>
    /// Formats a collection of tasks as an XML document.
    /// </summary>
    public string FormatTasks(List<Task> tasks)
    {
        try
        {
            var document = new XDocument(
                new XDeclaration("1.0", "utf-8", "yes"),
                new XElement("Tasks",
                    new XAttribute("count", tasks.Count),
                    new XAttribute("generated", DateTime.UtcNow.ToString("O")),
                    tasks.Select(t => FormatTask(t))
                )
            );

            return document.ToString();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to format {TaskCount} tasks as XML", tasks.Count);
            throw;
        }
    }

    /// <summary>
    /// Parses an XML string back into task objects.
    /// </summary>
    public List<Task> ParseTasks(string xml)
    {
        var tasks = new List<Task>();

        try
        {
            var document = XDocument.Parse(xml);
            var tasksElement = document.Root;

            if (tasksElement?.Name != "Tasks")
            {
                _logger.LogWarning("Invalid XML format for tasks");
                return tasks;
            }

            foreach (var taskElement in tasksElement.Elements("Task"))
            {
                try
                {
                    var task = ParseTaskElement(taskElement);
                    if (task is not null)
                        tasks.Add(task);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to parse task element");
                }
            }

            return tasks;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse XML into tasks");
            return tasks;
        }
    }

    /// <summary>
    /// Parses a single XML element into a task object.
    /// </summary>
    private Task? ParseTaskElement(XElement element)
    {
        if (element is null)
            return null;

        var idAttr = element.Attribute("id");
        if (idAttr is null || !Guid.TryParse(idAttr.Value, out var id))
            return null;

        var task = new Task
        {
            Id = id,
            Title = element.Element("Title")?.Value ?? "Untitled",
            Description = element.Element("Description")?.Value,
            Status = Enum.Parse<TaskStatus>(element.Element("Status")?.Value ?? "Todo"),
            Priority = int.Parse(element.Element("Priority")?.Value ?? "0"),
            CreatedAt = DateTime.Parse(element.Element("CreatedAt")?.Value ?? DateTime.UtcNow.ToString("O")),
            UpdatedAt = DateTime.Parse(element.Element("UpdatedAt")?.Value ?? DateTime.UtcNow.ToString("O")),
            AssignedTo = element.Element("AssignedTo")?.Value,
            Tags = element.Element("Tags")?.Value,
            NotionPageId = element.Element("NotionPageId")?.Value,
            IsDeleted = bool.Parse(element.Element("IsDeleted")?.Value ?? "false")
        };

        // Parse optional date fields
        var dueDateStr = element.Element("DueDate")?.Value;
        if (!string.IsNullOrEmpty(dueDateStr) && DateTime.TryParse(dueDateStr, out var dueDate))
            task.DueDate = dueDate;

        var completedAtStr = element.Element("CompletedAt")?.Value;
        if (!string.IsNullOrEmpty(completedAtStr) && DateTime.TryParse(completedAtStr, out var completedAt))
            task.CompletedAt = completedAt;

        return task;
    }

    /// <summary>
    /// Validates if a string is valid XML.
    /// </summary>
    public bool IsValidXml(string xml)
    {
        if (string.IsNullOrWhiteSpace(xml))
            return false;

        try
        {
            XDocument.Parse(xml);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
