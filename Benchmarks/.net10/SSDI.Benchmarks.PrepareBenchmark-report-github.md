```

BenchmarkDotNet v0.14.0, Windows 11 (10.0.26200.7171)
Unknown processor
.NET SDK 10.0.100
  [Host]     : .NET 10.0.0 (10.0.25.52411), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI
  Job-GNDUPB : .NET 10.0.0 (10.0.25.52411), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI

IterationCount=10  WarmupCount=2  

```
| Method     | Mean            | Error           | StdDev          | Rank | Gen0   | Gen1   | Allocated |
|----------- |----------------:|----------------:|----------------:|-----:|-------:|-------:|----------:|
| DryIoc     |        823.0 ns |         6.24 ns |         4.13 ns |    1 | 0.0820 |      - |   4.02 KB |
| MS.DI      |      1,421.2 ns |        81.59 ns |        53.96 ns |    2 | 0.2518 | 0.0629 |   12.4 KB |
| Grace      |      4,225.7 ns |        26.90 ns |        17.79 ns |    3 | 0.4272 | 0.0153 |   21.3 KB |
| SimpleInj  |     14,288.5 ns |       647.70 ns |       385.44 ns |    4 | 1.0986 | 0.4883 |  55.63 KB |
| Autofac    |     14,480.1 ns |        83.11 ns |        54.97 ns |    4 | 1.4954 |      - |  73.81 KB |
| SSDI       |     26,908.1 ns |     2,393.83 ns |     1,583.37 ns |    5 | 0.6104 | 0.5798 |  30.65 KB |
| SSDI-Eager | 10,403,466.7 ns | 4,272,914.61 ns | 2,826,268.46 ns |    6 | 7.8125 | 3.9063 | 484.88 KB |
