```

BenchmarkDotNet v0.14.0, Windows 11 (10.0.26200.7171)
Unknown processor
.NET SDK 10.0.100
  [Host]     : .NET 8.0.17 (8.0.1725.26602), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI
  Job-MJPMRZ : .NET 8.0.17 (8.0.1725.26602), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI

IterationCount=10  WarmupCount=2  

```
| Method             | Mean      | Error     | StdDev   | Rank | Gen0   | Gen1   | Allocated |
|------------------- |----------:|----------:|---------:|-----:|-------:|-------:|----------:|
| UnregisterAll      |        NA |        NA |       NA |    ? |     NA |     NA |        NA |
| Hot-Swap           |        NA |        NA |       NA |    ? |     NA |     NA |        NA |
| &#39;Simple Transient&#39; |  24.43 μs |  0.909 μs | 0.541 μs |    1 | 0.3662 | 0.3357 |  18.02 KB |
| Singleton          |  32.35 μs |  1.757 μs | 0.919 μs |    2 | 0.3662 | 0.3052 |  18.26 KB |
| &#39;With Dependents&#39;  | 388.30 μs | 18.636 μs | 9.747 μs |    3 | 0.9766 |      - |  57.79 KB |

Benchmarks with issues:
  UnregisterBenchmark.UnregisterAll: Job-MJPMRZ(IterationCount=10, WarmupCount=2)
  UnregisterBenchmark.Hot-Swap: Job-MJPMRZ(IterationCount=10, WarmupCount=2)
