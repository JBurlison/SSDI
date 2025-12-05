```

BenchmarkDotNet v0.14.0, Windows 11 (10.0.26200.7171)
Unknown processor
.NET SDK 10.0.100
  [Host]     : .NET 10.0.0 (10.0.25.52411), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI
  Job-GNDUPB : .NET 10.0.0 (10.0.25.52411), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI

IterationCount=10  WarmupCount=2  

```
| Method    | Mean        | Error     | StdDev    | Rank | Gen0   | Allocated |
|---------- |------------:|----------:|----------:|-----:|-------:|----------:|
| Grace     |    48.81 ns |  1.537 ns |  0.915 ns |    1 | 0.0048 |     240 B |
| SSDI      |    58.42 ns |  1.927 ns |  1.274 ns |    1 | 0.0048 |     240 B |
| DryIoc    |    67.45 ns |  2.703 ns |  1.788 ns |    2 | 0.0048 |     240 B |
| MS.DI     |    69.00 ns |  3.150 ns |  2.084 ns |    2 | 0.0048 |     240 B |
| SimpleInj |    86.25 ns |  2.129 ns |  1.113 ns |    3 | 0.0048 |     240 B |
| Autofac   | 1,179.15 ns | 25.494 ns | 16.862 ns |    4 | 0.1640 |    8320 B |
