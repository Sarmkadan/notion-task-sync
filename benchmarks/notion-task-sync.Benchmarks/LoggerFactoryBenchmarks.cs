using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Running;
using Microsoft.Extensions.Logging;
using NotionTaskSync.Infrastructure.Logging;
using System;
using System.IO;

[MemoryDiagnoser]
public class LoggerFactoryBenchmarks
{
    private LoggerFactory _loggerFactory;
    private string _logFilePath;

    [GlobalSetup]
    public void Setup()
    {
        _logFilePath = Path.GetTempFileName();
        _loggerFactory = new LoggerFactory(_logFilePath, LogLevel.Debug, true, true);
    }

    [Benchmark]
    public void CreateLogger()
    {
        _loggerFactory.CreateLogger<LoggerFactoryBenchmarks>();
    }

    [Benchmark]
    [Params(10, 100, 1000)]
    public void RotateLogFile(int iterations)
    {
        for (int i = 0; i < iterations; i++)
        {
            _loggerFactory.RotateLogFile();
        }
    }

    [Benchmark]
    [Params(10, 100, 1000)]
    public void CleanupOldLogs(int iterations)
    {
        for (int i = 0; i < iterations; i++)
        {
            _loggerFactory.CleanupOldLogs();
        }
    }

    [Benchmark]
    public void ValidateLogPath()
    {
        _loggerFactory.ValidateLogPath();
    }
}
