using BenchmarkDotNet.Attributes;
using NotionTaskSync.Collaboration;
using System.Collections.Generic;

[MemoryDiagnoser]
public class CollaborationSessionOptionsBenchmarks
{
    private CollaborationSessionOptions _options = null!;
    private List<CollaborationSessionOptions> _optionsList = null!;

    [Params(10, 100, 1000)]
    public int Count;

    [GlobalSetup]
    public void Setup()
    {
        _options = new CollaborationSessionOptions();
        _optionsList = new List<CollaborationSessionOptions>();
        for (int i = 0; i < 100; i++)
        {
            _optionsList.Add(new CollaborationSessionOptions());
        }
    }

    [Benchmark]
    public void Benchmark_Validate()
    {
        for (int i = 0; i < Count; i++)
        {
            _options.Validate();
        }
    }

    [Benchmark]
    public void Benchmark_SetMaxParticipantsPerSession()
    {
        for (int i = 0; i < Count; i++)
        {
            _options.MaxParticipantsPerSession = i;
        }
    }

    [Benchmark]
    public void Benchmark_SetOperationLogCapacity()
    {
        for (int i = 0; i < Count; i++)
        {
            _options.OperationLogCapacity = i;
        }
    }

    [Benchmark]
    public void Benchmark_SetMaxOperationsPerBatch()
    {
        for (int i = 0; i < Count; i++)
        {
            _options.MaxOperationsPerBatch = i;
        }
    }
}
