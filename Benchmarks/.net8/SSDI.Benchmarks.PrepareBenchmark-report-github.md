```

BenchmarkDotNet v0.14.0, Windows 11 (10.0.26200.7171)
Unknown processor
.NET SDK 10.0.100
  [Host]     : .NET 8.0.17 (8.0.1725.26602), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI
  Job-MJPMRZ : .NET 8.0.17 (8.0.1725.26602), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI

IterationCount=10  WarmupCount=2  

```
| Method     | Mean           | Error           | StdDev          | Rank | Gen0   | Gen1   | Allocated |
|----------- |---------------:|----------------:|----------------:|-----:|-------:|-------:|----------:|
| DryIoc     |       977.1 ns |         3.71 ns |         2.21 ns |    1 | 0.0801 |      - |   3.97 KB |
| MS.DI      |     1,536.1 ns |       221.76 ns |       146.68 ns |    2 | 0.2518 | 0.0629 |  12.37 KB |
| Grace      |     4,789.9 ns |        23.98 ns |        15.86 ns |    3 | 0.4349 | 0.0153 |  21.65 KB |
| SimpleInj  |    14,470.9 ns |       254.04 ns |       168.03 ns |    4 | 1.1292 | 0.5493 |  55.63 KB |
| Autofac    |    17,015.1 ns |       200.06 ns |       132.33 ns |    5 | 1.4038 |      - |  69.61 KB |
| SSDI       |    29,311.4 ns |     1,854.02 ns |     1,226.32 ns |    6 | 0.6104 | 0.5798 |  30.35 KB |
| SSDI-Eager | 9,752,113.9 ns | 3,938,652.11 ns | 2,605,174.51 ns |    7 | 7.8125 | 3.9063 | 490.61 KB |
