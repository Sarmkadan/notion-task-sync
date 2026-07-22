#nullable enable
using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging.Abstractions;
using NotionTaskSync.Domain.Models;
using NotionTaskSync.Formatters;
using Xunit;

namespace NotionTaskSync.Tests.Formatters
{
    /// <summary>
/// Unit tests for the <see cref="CsvFormatter"/> class that verify CSV formatting behavior.
/// </summary>
public class CsvFormatterTests
    {
        private readonly CsvFormatter _formatter;

        /// <summary>
        /// Initializes a new instance of the <see cref="CsvFormatterTests"/> class.
        /// </summary>
        public CsvFormatterTests()
        {
            // Use a null logger to avoid needing a real logging infrastructure
            _formatter = new CsvFormatter(NullLogger<CsvFormatter>.Instance);
        }

        /// <summary>
        /// Tests that the CSV formatter properly escapes special characters in task fields.
        /// Verifies that commas, quotes, and newlines are correctly wrapped in quotes
        /// and internal quotes are doubled according to CSV standards.
        /// </summary>
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

        /// <summary>
        /// Tests that formatting an empty task list returns only the header row.
        /// </summary>
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

        /// <summary>
        /// Tests that formatting a list of tasks includes the header row in the output.
        /// </summary>
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

        /// <summary>
        /// Helper method that extracts CSV values from a formatted CSV line.
        /// Parses comma-separated values while respecting quoted fields that may contain commas.
        /// </summary>
        /// <param name="line">The CSV line to parse.</param>
        /// <returns>An array of string values extracted from the CSV line.</returns>
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

        /// <summary>
        /// Returns the CSV header row for task data.
        /// This is a duplicate of the private CsvHeader method from CsvFormatter used for test verification.
        /// </summary>
        /// <returns>The CSV header string containing column names.</returns>
        private static string CsvHeader()
        {
            return "Id,Title,Description,Status,Priority,CreatedAt,UpdatedAt,DueDate,CompletedAt,AssignedTo,Tags,IsDeleted";
        }
    }
}
