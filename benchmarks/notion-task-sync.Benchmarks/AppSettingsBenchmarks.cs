using BenchmarkDotNet.Attributes;

namespace NotionTaskSync.Benchmarks
{
    [MemoryDiagnoser]
    public class AppSettingsBenchmarks
    {
        [GlobalSetup]
        public void Setup() { }

        [Benchmark]
        public void Benchmark_AppSettings_GetString() { }

        [Benchmark]
        public void Benchmark_AppSettings_GetInt() { }

        [Benchmark]
        public void Benchmark_AppSettings_GetBool() { }
    }
}
