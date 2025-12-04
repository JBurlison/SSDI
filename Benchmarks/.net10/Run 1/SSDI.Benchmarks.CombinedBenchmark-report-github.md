```

BenchmarkDotNet v0.14.0, Windows 11 (10.0.26200.7171)
Unknown processor
.NET SDK 10.0.100
  [Host]     : .NET 10.0.0 (10.0.25.52411), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI
  Job-QHIBGV : .NET 10.0.0 (10.0.25.52411), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI

IterationCount=10  WarmupCount=2  

```
| Method    | Mean       | Error     | StdDev    | Rank | Gen0   | Allocated |
|---------- |-----------:|----------:|----------:|-----:|-------:|----------:|
| Grace     |   7.611 ns | 0.1651 ns | 0.0983 ns |    1 | 0.0011 |      56 B |
| MS.DI     |   9.211 ns | 0.1109 ns | 0.0733 ns |    1 | 0.0011 |      56 B |
| DryIoc    |   9.588 ns | 0.1047 ns | 0.0692 ns |    1 | 0.0011 |      56 B |
| SimpleInj |   9.769 ns | 0.0697 ns | 0.0461 ns |    1 | 0.0011 |      56 B |
| SSDI      |  17.455 ns | 0.1712 ns | 0.1019 ns |    2 | 0.0011 |      56 B |
| Autofac   | 293.200 ns | 3.7012 ns | 2.4481 ns |    3 | 0.0339 |    1720 B |
