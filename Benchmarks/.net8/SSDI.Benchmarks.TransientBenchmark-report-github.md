```

BenchmarkDotNet v0.14.0, Windows 11 (10.0.26200.7171)
Unknown processor
.NET SDK 10.0.100
  [Host]     : .NET 8.0.17 (8.0.1725.26602), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI
  Job-MJPMRZ : .NET 8.0.17 (8.0.1725.26602), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI

IterationCount=10  WarmupCount=2  

```
| Method    | Mean        | Error     | StdDev   | Rank | Gen0   | Allocated |
|---------- |------------:|----------:|---------:|-----:|-------:|----------:|
| SSDI      |    63.17 ns |  0.794 ns | 0.525 ns |    1 | 0.0048 |     240 B |
| Grace     |    65.18 ns |  0.669 ns | 0.443 ns |    1 | 0.0048 |     240 B |
| DryIoc    |    74.92 ns |  0.516 ns | 0.341 ns |    2 | 0.0048 |     240 B |
| MS.DI     |    81.57 ns |  0.813 ns | 0.537 ns |    3 | 0.0048 |     240 B |
| SimpleInj |   106.42 ns |  0.551 ns | 0.364 ns |    4 | 0.0048 |     240 B |
| Autofac   | 1,415.67 ns | 14.071 ns | 9.307 ns |    5 | 0.1640 |    8320 B |
