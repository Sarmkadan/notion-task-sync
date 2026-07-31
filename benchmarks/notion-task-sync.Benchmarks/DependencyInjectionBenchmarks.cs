using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;
using NotionTaskSync.Infrastructure.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;

[MemoryDiagnoser]
public class DependencyInjectionBenchmarks
{
    private readonly AppSettings _appSettings;
    private readonly NotionApiSettings _notionApiSettings;

    [GlobalSetup]
    public void Setup()
    {
        _appSettings = new AppSettings();
        _notionApiSettings = new NotionApiSettings();
    }

    [Benchmark]
    [Params(10, 100, 1000)]
    public void Benchmark_AddApplicationServices(int count)
    {
        var services = new ServiceCollection();
        for (int i = 0; i < count; i++)
        {
            services.AddApplicationServices(_appSettings, _notionApiSettings);
        }
    }

    [Benchmark]
    public void Benchmark_ValidateConfiguration()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new[]
            {
                new KeyValuePair<string, string>("NotionApi:ApiKey", "test-api-key"),
                new KeyValuePair<string, string>("AppSettings:LocalTasksDirectory", "/path/to/tasks")
            })
            .Build();

        DependencyInjection.ValidateConfiguration(configuration);
    }

    [Benchmark]
    public void Benchmark_AddHttpClients()
    {
        var services = new ServiceCollection();
        services.AddHttpClients();
    }
}
