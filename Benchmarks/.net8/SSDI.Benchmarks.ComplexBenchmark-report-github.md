```

BenchmarkDotNet v0.14.0, Windows 11 (10.0.26200.7171)
Unknown processor
.NET SDK 10.0.100
  [Host]     : .NET 8.0.17 (8.0.1725.26602), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI
  Job-MJPMRZ : .NET 8.0.17 (8.0.1725.26602), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI

IterationCount=10  WarmupCount=2  

```
| Method     | Mean        | Error    | StdDev   | Rank | Gen0   | Allocated |
|----------- |------------:|---------:|---------:|-----:|-------:|----------:|
| Grace      |    17.71 ns | 1.549 ns | 0.810 ns |    1 | 0.0027 |     136 B |
| MS.DI      |    17.99 ns | 0.320 ns | 0.212 ns |    1 | 0.0027 |     136 B |
| DryIoc     |    18.32 ns | 0.160 ns | 0.106 ns |    1 | 0.0027 |     136 B |
| SSDI-Eager |    22.47 ns | 0.134 ns | 0.080 ns |    1 | 0.0027 |     136 B |
| SimpleInj  |    23.68 ns | 1.303 ns | 0.862 ns |    1 | 0.0027 |     136 B |
| SSDI       |    46.97 ns | 0.212 ns | 0.140 ns |    2 | 0.0027 |     136 B |
| Autofac    | 1,155.34 ns | 7.413 ns | 4.903 ns |    3 | 0.0935 |    4744 B |
