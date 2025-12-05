```

BenchmarkDotNet v0.14.0, Windows 11 (10.0.26200.7171)
Unknown processor
.NET SDK 10.0.100
  [Host]     : .NET 10.0.0 (10.0.25.52411), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI
  Job-JMJFHY : .NET 10.0.0 (10.0.25.52411), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI

IterationCount=10  WarmupCount=2  

```
| Method    | Mean        | Error     | StdDev   | Min         | Max         | Median      | P90         | P95         | Rank | Gen0   | Allocated |
|---------- |------------:|----------:|---------:|------------:|------------:|------------:|------------:|------------:|-----:|-------:|----------:|
| Grace     |    43.34 ns |  1.117 ns | 0.739 ns |    42.43 ns |    44.36 ns |    43.24 ns |    44.17 ns |    44.26 ns |    1 | 0.0048 |     240 B |
| SSDI      |    61.85 ns |  4.300 ns | 2.559 ns |    59.23 ns |    66.93 ns |    61.03 ns |    65.10 ns |    66.01 ns |    2 | 0.0048 |     240 B |
| DryIoc    |    67.88 ns |  1.157 ns | 0.765 ns |    66.63 ns |    68.94 ns |    67.85 ns |    68.64 ns |    68.79 ns |    2 | 0.0048 |     240 B |
| MS.DI     |    71.99 ns |  1.184 ns | 0.619 ns |    71.08 ns |    72.65 ns |    72.10 ns |    72.58 ns |    72.61 ns |    2 | 0.0048 |     240 B |
| SimpleInj |    89.28 ns |  3.899 ns | 2.579 ns |    86.45 ns |    94.21 ns |    87.89 ns |    92.31 ns |    93.26 ns |    2 | 0.0048 |     240 B |
| Autofac   | 1,335.02 ns | 17.374 ns | 9.087 ns | 1,325.74 ns | 1,355.03 ns | 1,334.80 ns | 1,342.30 ns | 1,348.67 ns |    3 | 0.1640 |    8320 B |
