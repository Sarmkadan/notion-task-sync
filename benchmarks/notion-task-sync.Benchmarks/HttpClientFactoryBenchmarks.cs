using BenchmarkDotNet.Attributes;

namespace NotionTaskSync.Benchmarks
{
    [MemoryDiagnoser]
    public class HttpClientFactoryBenchmarks
    {
        [GlobalSetup]
        public void Setup() { }

        [Benchmark]
        public void Benchmark_HttpClientFactory_Get() { }

        [Benchmark]
        public void Benchmark_HttpClientFactory_Post() { }

        [Benchmark]
        public void Benchmark_HttpClientFactory_Put() { }

        [Benchmark]
        public void Benchmark_HttpClientFactory_Delete() { }
    }
}
