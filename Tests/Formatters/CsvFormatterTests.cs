#nullable enable
using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging.Abstractions;
using NotionTaskSync.Domain.Models;
using NotionTaskSync.Formatters;
using Xunit;

namespace NotionTaskSync.Tests.Formatters
{
    public class CsvFormatterTests
    {
        private readonly CsvFormatter _formatter;

        public CsvFormatterTests()
        {
            // Use a null logger to avoid needing a real logging infrastructure
            _formatter = new CsvFormatter(NullLogger<CsvFormatter>.Instance);
        }

        [Fact]
        public void FormatTask_EscapesSpecialCharacters()
        {
            // Arrange: task fields contain commas, quotes and newlines
            var task = new Task
            {
                Id = Guid.NewGuid(),
                Title = "Title, with, commas",
                Description = "Description with \"quotes\" and a newline\nsecond line",
                Status = TaskStatus.Open,
                Priority = 1,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                DueDate = null,
                CompletedAt = null,
                AssignedTo = "User, Name",
                Tags = "tag1\ntag2",
                IsDeleted = false
            };

            // Act
            var csv = _formatter.FormatTask(task);

            // Assert: each field that contains a special character should be wrapped in quotes
            // and internal quotes should be doubled.
            var lines = csv.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
            Assert.Equal(2, lines.Length); // header + row

            var header = lines[0];
            var row = lines[1];

            // Verify header is correct
            Assert.Equal("Id,Title,Description,Status,Priority,CreatedAt,UpdatedAt,DueDate,CompletedAt,AssignedTo,Tags,IsDeleted", header);

            // Split row using the same unescaping logic as the formatter to make verification easier
            var values = GetCsvValues(row);
            Assert.Equal(12, values.Length);

            // Title contains commas -> should be quoted
            Assert.Equal($"\"{task.Title}\"", values[1]);

            // Description contains quotes and newline -> should be quoted and internal quotes doubled
            var expectedDescription = $"\"{task.Description.Replace("\"", "\"\"")}\"";
            Assert.Equal(expectedDescription, values[2]);

            // AssignedTo contains commas -> quoted
            Assert.Equal($"\"{task.AssignedTo}\"", values[9]);

            // Tags contains newline -> quoted
            var expectedTags = $"\"{task.Tags.Replace("\n", "\r\n")}\""; // CsvEscape does not replace newline chars, just wraps in quotes
            // Since the original string contains '\n', after escaping it will be wrapped in quotes unchanged.
            Assert.Equal($"\"{task.Tags}\"", values[10]);
        }

        [Fact]
        public void FormatTasks_EmptyList_ReturnsHeaderOnly()
        {
            // Arrange
            var emptyList = new List<Task>();

            // Act
            var csv = _formatter.FormatTasks(emptyList);

            // Assert: result should be only the header line followed by a newline
            var expected = $"{CsvHeader()}{Environment.NewLine}";
            Assert.Equal(expected, csv);
        }

        [Fact]
        public void FormatTasks_IncludesHeaderRow()
        {
            // Arrange
            var task = new Task
            {
                Id = Guid.NewGuid(),
                Title = "Simple",
                Description = "Desc",
                Status = TaskStatus.Open,
                Priority = 0,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                DueDate = null,
                CompletedAt = null,
                AssignedTo = null,
                Tags = null,
                IsDeleted = false
            };
            var list = new List<Task> { task };

            // Act
            var csv = _formatter.FormatTasks(list);

            // Assert
            var lines = csv.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
            Assert.True(lines.Length >= 2, "CSV should contain at least header and one data row");
            Assert.Equal(CsvHeader(), lines[0]);
        }

        // Helper to retrieve the raw CSV values (including any surrounding quotes) using the same logic as CsvFormatter
        private static string[] GetCsvValues(string line)
        {
            var values = new List<string>();
            var current = new System.Text.StringBuilder();
            var inQuotes = false;

            for (int i = 0; i < line.Length; i++)
            {
                var c = line[i];
                if (c == '"')
                {
                    if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                    {
                        current.Append('"');
                        i++;
                    }
                    else
                    {
                        inQuotes = !inQuotes;
                    }
                }
                else if (c == ',' && !inQuotes)
                {
                    values.Add(current.ToString());
                    current.Clear();
                }
                else
                {
                    current.Append(c);
                }
            }

            values.Add(current.ToString());
            return values.ToArray();
        }

        // Duplicate of the private CsvHeader method from CsvFormatter for test verification
        private static string CsvHeader()
        {
            return "Id,Title,Description,Status,Priority,CreatedAt,UpdatedAt,DueDate,CompletedAt,AssignedTo,Tags,IsDeleted";
        }
    }
}
