[MemoryDiagnoser]
public class SyncServiceTestsValidationBenchmarks
{
    [Benchmark]
    public void BenchmarkMethod1()
    {
        // Setup and test data
        var testData = new object[] { /* test data */ };
        // Benchmark code
    }

    [Benchmark]
    public void BenchmarkMethod2([Params(10)] int inputSize)
    {
        // Setup and test data
        var testData = new object[] { /* test data */ };
        // Benchmark code
    }

    [Benchmark]
    public void BenchmarkMethod3()
    {
        // Setup and test data
        var testData = new object[] { /* test data */ };
        // Benchmark code
    }
}