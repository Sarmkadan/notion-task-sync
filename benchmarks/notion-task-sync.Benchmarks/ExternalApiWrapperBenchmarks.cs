[MemoryDiagnoser]
public class ExternalApiWrapperBenchmarks
{
    [Benchmark]
    public void Benchmark_ExternalApiWrapper_GetData()
    {
        // setup test data
        var requestData = new RequestData();
        // benchmark
        var stopwatch = Stopwatch.StartNew();
        ExternalApiWrapper.GetData(requestData);
        stopwatch.Stop();
        Console.WriteLine($"GetData: {stopwatch.ElapsedMilliseconds}ms");
    }

    [Benchmark]
    public void Benchmark_ExternalApiWrapper_SendData()
    {
        // setup test data
        var requestData = new RequestData();
        var responseData = new ResponseData();
        // benchmark
        var stopwatch = Stopwatch.StartNew();
        ExternalApiWrapper.SendData(requestData, responseData);
        stopwatch.Stop();
        Console.WriteLine($"SendData: {stopwatch.ElapsedMilliseconds}ms");
    }

    [Benchmark]
    public void Benchmark_ExternalApiWrapper_ProcessData()
    {
        // setup test data
        var requestData = new RequestData();
        var responseData = new ResponseData();
        // benchmark
        var stopwatch = Stopwatch.StartNew();
        ExternalApiWrapper.ProcessData(requestData, responseData);
        stopwatch.Stop();
        Console.WriteLine($"ProcessData: {stopwatch.ElapsedMilliseconds}ms");
    }
}