using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.Logging;
using NotionTaskSync.Infrastructure.Logging;
using System.IO;
using LoggerFactory = NotionTaskSync.Infrastructure.Logging.LoggerFactory;

[MemoryDiagnoser]
public class LoggerFactoryBenchmarks
{
    private LoggerFactory _loggerFactory = null!;
    private string _logFilePath = null!;

    [Params(10, 100, 1000)]
    public int Count;

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
    public void RotateLogFile()
    {
        for (int i = 0; i < Count; i++)
        {
            _loggerFactory.RotateLogFile();
        }
    }

    [Benchmark]
    public void CleanupOldLogs()
    {
        for (int i = 0; i < Count; i++)
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
