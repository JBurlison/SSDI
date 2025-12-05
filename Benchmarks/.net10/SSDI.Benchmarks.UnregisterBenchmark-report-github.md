```

BenchmarkDotNet v0.14.0, Windows 11 (10.0.26200.7171)
Unknown processor
.NET SDK 10.0.100
  [Host]     : .NET 10.0.0 (10.0.25.52411), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI
  Job-GNDUPB : .NET 10.0.0 (10.0.25.52411), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI

IterationCount=10  WarmupCount=2  

```
| Method             | Mean      | Error    | StdDev   | Rank | Gen0   | Gen1   | Allocated |
|------------------- |----------:|---------:|---------:|-----:|-------:|-------:|----------:|
| UnregisterAll      |        NA |       NA |       NA |    ? |     NA |     NA |        NA |
| Hot-Swap           |        NA |       NA |       NA |    ? |     NA |     NA |        NA |
| &#39;Simple Transient&#39; |  27.93 μs | 1.093 μs | 0.723 μs |    1 | 0.3662 | 0.3052 |  18.08 KB |
| Singleton          |  35.63 μs | 0.657 μs | 0.344 μs |    2 | 0.3662 | 0.3052 |  18.33 KB |
| &#39;With Dependents&#39;  | 409.22 μs | 6.670 μs | 4.412 μs |    3 | 0.9766 | 0.4883 |  57.67 KB |

Benchmarks with issues:
  UnregisterBenchmark.UnregisterAll: Job-GNDUPB(IterationCount=10, WarmupCount=2)
  UnregisterBenchmark.Hot-Swap: Job-GNDUPB(IterationCount=10, WarmupCount=2)
