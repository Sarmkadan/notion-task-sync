using BenchmarkDotNet.Running;
using NotionTaskSync.Benchmarks;

namespace NotionTaskSync.Benchmarks;

class Program
{
    static void Main(string[] args)
    {
        BenchmarkRunner.Run<MapperBenchmarks>();
    }
}
