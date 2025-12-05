```

BenchmarkDotNet v0.14.0, Windows 11 (10.0.26200.7171)
Unknown processor
.NET SDK 10.0.100
  [Host]     : .NET 10.0.0 (10.0.25.52411), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI
  Job-JMJFHY : .NET 10.0.0 (10.0.25.52411), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI

IterationCount=10  WarmupCount=2  

```
| Method             | Mean      | Error     | StdDev    | Min       | Max       | Median    | P90       | P95       | Rank | Gen0   | Gen1   | Allocated |
|------------------- |----------:|----------:|----------:|----------:|----------:|----------:|----------:|----------:|-----:|-------:|-------:|----------:|
| UnregisterAll      |        NA |        NA |        NA |        NA |        NA |        NA |        NA |        NA |    ? |     NA |     NA |        NA |
| Hot-Swap           |        NA |        NA |        NA |        NA |        NA |        NA |        NA |        NA |    ? |     NA |     NA |        NA |
| &#39;Simple Transient&#39; |  28.03 μs |  1.069 μs |  0.707 μs |  27.16 μs |  29.00 μs |  28.04 μs |  28.88 μs |  28.94 μs |    1 | 0.3662 | 0.3052 |  18.09 KB |
| Singleton          |  35.98 μs |  1.068 μs |  0.706 μs |  34.70 μs |  36.92 μs |  35.96 μs |  36.91 μs |  36.91 μs |    2 | 0.3662 | 0.3052 |  18.33 KB |
| &#39;With Dependents&#39;  | 407.83 μs | 67.799 μs | 44.845 μs | 368.60 μs | 490.75 μs | 385.26 μs | 471.62 μs | 481.19 μs |    3 | 0.9766 |      - |  57.61 KB |

Benchmarks with issues:
  UnregisterBenchmark.UnregisterAll: Job-JMJFHY(IterationCount=10, WarmupCount=2)
  UnregisterBenchmark.Hot-Swap: Job-JMJFHY(IterationCount=10, WarmupCount=2)
