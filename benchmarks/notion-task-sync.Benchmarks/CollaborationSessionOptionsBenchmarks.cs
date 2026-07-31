using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;
using NotionTaskSync.Collaboration;
using System;
using System.Collections.Generic;
using System.Linq;

[MemoryDiagnoser]
public class CollaborationSessionOptionsBenchmarks
{
    private CollaborationSessionOptions _options;
    private List<CollaborationSessionOptions> _optionsList;

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
    [Params(10, 100, 1000)]
    public void Benchmark_Validate(int count)
    {
        for (int i = 0; i < count; i++)
        {
            _options.Validate();
        }
    }

    [Benchmark]
    [Params(10, 100, 1000)]
    public void Benchmark_SetMaxParticipantsPerSession(int count)
    {
        for (int i = 0; i < count; i++)
        {
            _options.MaxParticipantsPerSession = i;
        }
    }

    [Benchmark]
    [Params(10, 100, 1000)]
    public void Benchmark_SetOperationLogCapacity(int count)
    {
        for (int i = 0; i < count; i++)
        {
            _options.OperationLogCapacity = i;
        }
    }

    [Benchmark]
    [Params(10, 100, 1000)]
    public void Benchmark_SetMaxOperationsPerBatch(int count)
    {
        for (int i = 0; i < count; i++)
        {
            _options.MaxOperationsPerBatch = i;
        }
    }
}
