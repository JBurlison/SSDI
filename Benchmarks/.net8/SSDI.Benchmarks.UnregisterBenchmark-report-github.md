```

BenchmarkDotNet v0.14.0, Windows 11 (10.0.26200.7171)
Unknown processor
.NET SDK 10.0.100
  [Host]     : .NET 8.0.17 (8.0.1725.26602), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI
  Job-JOBITY : .NET 8.0.17 (8.0.1725.26602), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI

IterationCount=10  WarmupCount=2  

```
| Method             | Mean      | Error    | StdDev   | Min       | Max       | Median    | P90       | P95       | Rank | Gen0   | Gen1   | Allocated |
|------------------- |----------:|---------:|---------:|----------:|----------:|----------:|----------:|----------:|-----:|-------:|-------:|----------:|
| UnregisterAll      |        NA |       NA |       NA |        NA |        NA |        NA |        NA |        NA |    ? |     NA |     NA |        NA |
| Hot-Swap           |        NA |       NA |       NA |        NA |        NA |        NA |        NA |        NA |    ? |     NA |     NA |        NA |
| &#39;Simple Transient&#39; |  24.11 μs | 0.550 μs | 0.288 μs |  23.65 μs |  24.38 μs |  24.23 μs |  24.33 μs |  24.36 μs |    1 | 0.3662 | 0.3357 |  18.09 KB |
| Singleton          |  32.19 μs | 0.852 μs | 0.446 μs |  31.43 μs |  32.74 μs |  32.35 μs |  32.61 μs |  32.67 μs |    2 | 0.3662 | 0.3052 |  18.33 KB |
| &#39;With Dependents&#39;  | 412.91 μs | 6.934 μs | 3.627 μs | 408.05 μs | 419.83 μs | 412.86 μs | 416.15 μs | 417.99 μs |    3 | 0.9766 | 0.4883 |  57.77 KB |

Benchmarks with issues:
  UnregisterBenchmark.UnregisterAll: Job-JOBITY(IterationCount=10, WarmupCount=2)
  UnregisterBenchmark.Hot-Swap: Job-JOBITY(IterationCount=10, WarmupCount=2)
