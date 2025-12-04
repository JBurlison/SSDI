```

BenchmarkDotNet v0.14.0, Windows 11 (10.0.26200.7171)
Unknown processor
.NET SDK 10.0.100
  [Host]     : .NET 8.0.17 (8.0.1725.26602), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI
  Job-JAKLOI : .NET 8.0.17 (8.0.1725.26602), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI

IterationCount=10  WarmupCount=2  

```
| Method    | Mean        | Error     | StdDev   | Rank | Gen0   | Allocated |
|---------- |------------:|----------:|---------:|-----:|-------:|----------:|
| SSDI      |    65.05 ns |  0.554 ns | 0.330 ns |    1 | 0.0048 |     240 B |
| Grace     |    66.75 ns |  0.362 ns | 0.239 ns |    1 | 0.0048 |     240 B |
| DryIoc    |    75.99 ns |  0.941 ns | 0.623 ns |    2 | 0.0048 |     240 B |
| MS.DI     |    81.43 ns |  0.738 ns | 0.488 ns |    3 | 0.0048 |     240 B |
| SimpleInj |   106.58 ns |  0.701 ns | 0.464 ns |    4 | 0.0048 |     240 B |
| Autofac   | 1,445.70 ns | 10.443 ns | 6.215 ns |    5 | 0.1640 |    8320 B |
