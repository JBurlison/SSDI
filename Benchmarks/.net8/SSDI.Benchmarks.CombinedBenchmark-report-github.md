```

BenchmarkDotNet v0.14.0, Windows 11 (10.0.26200.7171)
Unknown processor
.NET SDK 10.0.100
  [Host]     : .NET 8.0.17 (8.0.1725.26602), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI
  Job-MJPMRZ : .NET 8.0.17 (8.0.1725.26602), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI

IterationCount=10  WarmupCount=2  

```
| Method    | Mean       | Error     | StdDev    | Rank | Gen0   | Allocated |
|---------- |-----------:|----------:|----------:|-----:|-------:|----------:|
| Grace     |   9.282 ns | 0.0905 ns | 0.0598 ns |    1 | 0.0011 |      56 B |
| MS.DI     |  10.756 ns | 0.0655 ns | 0.0433 ns |    2 | 0.0011 |      56 B |
| DryIoc    |  10.809 ns | 0.1059 ns | 0.0701 ns |    2 | 0.0011 |      56 B |
| SimpleInj |  13.990 ns | 0.0702 ns | 0.0464 ns |    3 | 0.0011 |      56 B |
| SSDI      |  15.116 ns | 0.2654 ns | 0.1756 ns |    4 | 0.0011 |      56 B |
| Autofac   | 345.041 ns | 4.2104 ns | 2.7849 ns |    5 | 0.0339 |    1720 B |
