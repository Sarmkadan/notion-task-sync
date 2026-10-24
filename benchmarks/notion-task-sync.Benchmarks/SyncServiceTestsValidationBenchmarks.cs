using BenchmarkDotNet.Attributes;

[MemoryDiagnoser]
public class SyncServiceTestsValidationBenchmarks
{
    [Params(10)]
    public int InputSize;

    [Benchmark]
    public void BenchmarkMethod1()
    {
        // Setup and test data
        var testData = new object[] { /* test data */ };
        // Benchmark code
    }

    [Benchmark]
    public void BenchmarkMethod2()
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
