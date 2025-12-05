```

BenchmarkDotNet v0.14.0, Windows 11 (10.0.26200.7171)
Unknown processor
.NET SDK 10.0.100
  [Host]     : .NET 8.0.17 (8.0.1725.26602), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI
  Job-JOBITY : .NET 8.0.17 (8.0.1725.26602), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI

IterationCount=10  WarmupCount=2  

```
| Method    | Mean        | Error      | StdDev     | Min         | Max         | Median      | P90         | P95         | Rank | Gen0   | Allocated |
|---------- |------------:|-----------:|-----------:|------------:|------------:|------------:|------------:|------------:|-----:|-------:|----------:|
| SSDI      |    64.66 ns |   2.371 ns |   1.568 ns |    62.46 ns |    67.74 ns |    64.54 ns |    66.03 ns |    66.88 ns |    1 | 0.0048 |     240 B |
| Grace     |    80.36 ns |  13.739 ns |   9.087 ns |    71.84 ns |    95.39 ns |    76.78 ns |    93.98 ns |    94.69 ns |    2 | 0.0048 |     240 B |
| DryIoc    |    82.46 ns |   9.524 ns |   5.667 ns |    77.11 ns |    93.74 ns |    83.25 ns |    88.24 ns |    90.99 ns |    2 | 0.0048 |     240 B |
| MS.DI     |    82.81 ns |   1.850 ns |   1.101 ns |    81.20 ns |    84.83 ns |    82.77 ns |    83.83 ns |    84.33 ns |    2 | 0.0048 |     240 B |
| SimpleInj |   141.01 ns |  41.763 ns |  27.624 ns |   115.03 ns |   189.21 ns |   125.69 ns |   174.37 ns |   181.79 ns |    3 | 0.0048 |     240 B |
| Autofac   | 1,528.89 ns | 238.582 ns | 141.976 ns | 1,440.58 ns | 1,850.23 ns | 1,459.38 ns | 1,717.46 ns | 1,783.85 ns |    4 | 0.1640 |    8320 B |
