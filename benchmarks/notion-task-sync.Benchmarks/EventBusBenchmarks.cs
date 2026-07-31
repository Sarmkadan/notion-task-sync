using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;
using Microsoft.Extensions.Logging;
using NotionTaskSync.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

[MemoryDiagnoser]
public class EventBusBenchmarks
{
    private EventBus _eventBus;
    private List<Func<ApplicationEvent, Task>> _asyncHandlers;
    private List<Action<ApplicationEvent>> _syncHandlers;
    private ApplicationEvent _event;

    [GlobalSetup]
    public void Setup()
    {
        _eventBus = new EventBus(new LoggerFactory().CreateLogger<EventBus>());
        _asyncHandlers = new List<Func<ApplicationEvent, Task>>();
        _syncHandlers = new List<Action<ApplicationEvent>>();
        _event = new ApplicationEvent();

        for (int i = 0; i < 100; i++)
        {
            _asyncHandlers.Add(e => Task.CompletedTask);
            _syncHandlers.Add(e => { });
        }
    }

    [Benchmark]
    [Params(10, 100, 1000)]
    public void Benchmark_SubscribeAsync(int count)
    {
        for (int i = 0; i < count; i++)
        {
            _eventBus.Subscribe<ApplicationEvent>(e => Task.CompletedTask);
        }
    }

    [Benchmark]
    [Params(10, 100, 1000)]
    public void Benchmark_SubscribeSync(int count)
    {
        for (int i = 0; i < count; i++)
        {
            _eventBus.Subscribe<ApplicationEvent>(e => { });
        }
    }

    [Benchmark]
    [Params(10, 100, 1000)]
    public void Benchmark_PublishAsync(int count)
    {
        for (int i = 0; i < count; i++)
        {
            _eventBus.Subscribe<ApplicationEvent>(e => Task.CompletedTask);
        }

        _eventBus.PublishAsync(_event);
    }

    [Benchmark]
    [Params(10, 100, 1000)]
    public void Benchmark_PublishSync(int count)
    {
        for (int i = 0; i < count; i++)
        {
            _eventBus.Subscribe<ApplicationEvent>(e => { });
        }

        _eventBus.Publish(_event);
    }
}
