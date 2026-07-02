using BenchmarkDotNet.Attributes;
using NotionTaskSync.Data.Mappers;
using NotionTaskSync.Domain.Models;
using System.Collections.Generic;

namespace NotionTaskSync.Benchmarks;

[MemoryDiagnoser]
public class MapperBenchmarks
{
    private NotionPage? _samplePage;
    private Dictionary<string, object?>? _sampleRichText;

    [GlobalSetup]
    public void Setup()
    {
        _samplePage = new NotionPage("page-id", "db-id", "Sample Title")
        {
            Properties = new Dictionary<string, object?>
            {
                { "Description", "Sample description" },
                { "Status", "InProgress" },
                { "Priority", 1 },
                { "DueDate", "2026-07-02T12:00:00Z" }
            }
        };

        _sampleRichText = new Dictionary<string, object?>
        {
            { "plain_text", "Sample text" }
        };
    }

    [Benchmark]
    public string NormalizeRichText()
    {
        var richTextList = new List<object>
        {
            new Dictionary<string, object?> { { "plain_text", "Sample text" } },
            new Dictionary<string, object?> { { "plain_text", " with more text" } }
        };
        return NotionMapper.NormalizeRichTextForComparison(richTextList);
    }

    [Benchmark]
    public NotionTaskSync.Domain.Models.Task MapFromNotionPageBenchmark()
    {
        // We need a valid NotionPage for MapFromNotionPage to work
        // GUIDs are 36 characters
        var page = new NotionPage("12345678-1234-1234-1234-123456789012", "12345678-1234-1234-1234-123456789012", "Sample Title")
        {
            Properties = new Dictionary<string, object?>
            {
                { "Description", "Sample description" },
                { "Status", "InProgress" },
                { "Priority", "1" },
                { "DueDate", "2026-07-02T12:00:00Z" }
            }
        };
        return TaskMapper.MapFromNotionPage(page);
    }
}
