```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04 LTS (Resolute Raccoon)
AMD EPYC-Rome Processor 2.45GHz, 1 CPU, 16 logical and 16 physical cores
.NET SDK 10.0.300
  [Host]     : .NET 10.0.8 (10.0.8, 10.0.826.23019), X64 RyuJIT x86-64-v3
  DefaultJob : .NET 10.0.8 (10.0.8, 10.0.826.23019), X64 RyuJIT x86-64-v3


```
| Method                     | Mean       | Error    | StdDev   | Gen0   | Allocated |
|--------------------------- |-----------:|---------:|---------:|-------:|----------:|
| NormalizeRichText          |   193.3 ns |  3.94 ns | 10.11 ns | 0.0966 |     808 B |
| MapFromNotionPageBenchmark | 1,181.9 ns | 23.09 ns | 31.60 ns | 0.0973 |     816 B |
