```

BenchmarkDotNet v0.14.0, Windows 11 (10.0.26200.7171)
Unknown processor
.NET SDK 10.0.100
  [Host]     : .NET 10.0.0 (10.0.25.52411), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI
  Job-GNDUPB : .NET 10.0.0 (10.0.25.52411), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI

IterationCount=10  WarmupCount=2  

```
| Method    | Mean       | Error     | StdDev    | Rank | Gen0   | Allocated |
|---------- |-----------:|----------:|----------:|-----:|-------:|----------:|
| Grace     |   7.493 ns | 0.1863 ns | 0.1232 ns |    1 | 0.0011 |      56 B |
| MS.DI     |   9.269 ns | 0.2862 ns | 0.1893 ns |    2 | 0.0011 |      56 B |
| DryIoc    |   9.711 ns | 0.3649 ns | 0.2413 ns |    2 | 0.0011 |      56 B |
| SimpleInj |   9.853 ns | 0.3732 ns | 0.2468 ns |    2 | 0.0011 |      56 B |
| SSDI      |  12.660 ns | 0.2189 ns | 0.1448 ns |    3 | 0.0011 |      56 B |
| Autofac   | 271.402 ns | 5.9755 ns | 3.9524 ns |    4 | 0.0339 |    1720 B |
