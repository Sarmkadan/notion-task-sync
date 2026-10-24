using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.Logging.Abstractions;
using NotionTaskSync.Events;
using System;
using System.Threading.Tasks;

[MemoryDiagnoser]
public class EventBusBenchmarks
{
    private EventBus _eventBus = null!;
    private BenchmarkEvent _event = null!;

    [Params(10, 100, 1000)]
    public int Count;

    [GlobalSetup]
    public void Setup()
    {
        _eventBus = new EventBus(NullLogger<EventBus>.Instance);
        _event = new BenchmarkEvent();
    }

    [Benchmark]
    public void Benchmark_SubscribeAsync()
    {
        for (int i = 0; i < Count; i++)
        {
            _eventBus.Subscribe<BenchmarkEvent>(e => Task.CompletedTask);
        }
    }

    [Benchmark]
    public void Benchmark_SubscribeSync()
    {
        for (int i = 0; i < Count; i++)
        {
            _eventBus.Subscribe<BenchmarkEvent>(e => { });
        }
    }

    [Benchmark]
    public void Benchmark_PublishAsync()
    {
        for (int i = 0; i < Count; i++)
        {
            _eventBus.Subscribe<BenchmarkEvent>(e => Task.CompletedTask);
        }

        _eventBus.PublishAsync(_event).GetAwaiter().GetResult();
    }

    [Benchmark]
    public void Benchmark_PublishSync()
    {
        for (int i = 0; i < Count; i++)
        {
            _eventBus.Subscribe<BenchmarkEvent>(e => { });
        }

        _eventBus.Publish(_event);
    }

    private sealed class BenchmarkEvent : ApplicationEvent
    {
    }
}
