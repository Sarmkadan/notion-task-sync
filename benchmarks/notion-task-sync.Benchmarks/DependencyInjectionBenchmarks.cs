using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NotionTaskSync.Infrastructure.Configuration;
using System.Collections.Generic;

[MemoryDiagnoser]
public class DependencyInjectionBenchmarks
{
    private IConfiguration _configuration = null!;

    [Params(10, 100, 1000)]
    public int Count;

    [GlobalSetup]
    public void Setup()
    {
        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new[]
            {
                new KeyValuePair<string, string?>("NotionApi:ApiKey", "test-api-key"),
                new KeyValuePair<string, string?>("NotionApi:PropertyMappings:Title", "Name"),
                new KeyValuePair<string, string?>("NotionApi:PropertyMappings:Status", "Status"),
                new KeyValuePair<string, string?>("AppSettings:LocalTasksDirectory", "/path/to/tasks")
            })
            .Build();
    }

    [Benchmark]
    public void Benchmark_AddApplicationServices()
    {
        var services = new ServiceCollection();
        for (int i = 0; i < Count; i++)
        {
            services.AddApplicationServices(_configuration);
        }
    }

    [Benchmark]
    public void Benchmark_ValidateConfiguration()
    {
        DependencyInjection.ValidateConfiguration(_configuration);
    }

    [Benchmark]
    public void Benchmark_AddHttpClients()
    {
        var services = new ServiceCollection();
        services.AddHttpClients();
    }
}
