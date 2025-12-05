```

BenchmarkDotNet v0.14.0, Windows 11 (10.0.26200.7171)
Unknown processor
.NET SDK 10.0.100
  [Host]     : .NET 8.0.17 (8.0.1725.26602), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI
  Job-JOBITY : .NET 8.0.17 (8.0.1725.26602), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI

IterationCount=10  WarmupCount=2  

```
| Method    | Mean       | Error     | StdDev    | Min       | Max        | Median     | P90        | P95        | Rank | Gen0   | Allocated |
|---------- |-----------:|----------:|----------:|----------:|-----------:|-----------:|-----------:|-----------:|-----:|-------:|----------:|
| Grace     |   5.207 ns | 1.0000 ns | 0.6614 ns |  4.646 ns |   6.592 ns |   4.859 ns |   6.010 ns |   6.301 ns |    1 |      - |         - |
| DryIoc    |   5.984 ns | 0.1676 ns | 0.0998 ns |  5.824 ns |   6.119 ns |   5.986 ns |   6.108 ns |   6.113 ns |    1 |      - |         - |
| MS.DI     |   5.997 ns | 0.0388 ns | 0.0257 ns |  5.962 ns |   6.041 ns |   6.003 ns |   6.023 ns |   6.032 ns |    1 |      - |         - |
| SSDI      |   6.089 ns | 0.1578 ns | 0.1044 ns |  5.912 ns |   6.192 ns |   6.123 ns |   6.177 ns |   6.185 ns |    1 |      - |         - |
| SimpleInj |   8.530 ns | 0.4543 ns | 0.2376 ns |  8.259 ns |   9.013 ns |   8.518 ns |   8.742 ns |   8.877 ns |    2 |      - |         - |
| Autofac   | 102.384 ns | 8.0307 ns | 5.3118 ns | 93.610 ns | 110.575 ns | 100.198 ns | 108.501 ns | 109.538 ns |    3 | 0.0161 |     808 B |
