```

BenchmarkDotNet v0.14.0, Windows 11 (10.0.26200.7171)
Unknown processor
.NET SDK 10.0.100
  [Host]     : .NET 10.0.0 (10.0.25.52411), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI
  Job-JMJFHY : .NET 10.0.0 (10.0.25.52411), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI

IterationCount=10  WarmupCount=2  

```
| Method     | Mean      | Error    | StdDev   | Min       | Max       | Median    | P90       | P95       | Rank | Gen0   | Gen1   | Allocated |
|----------- |----------:|---------:|---------:|----------:|----------:|----------:|----------:|----------:|-----:|-------:|-------:|----------:|
| MS.DI      |  16.27 ns | 0.551 ns | 0.328 ns |  15.91 ns |  16.90 ns |  16.19 ns |  16.63 ns |  16.77 ns |    1 | 0.0027 |      - |     136 B |
| Grace      |  16.65 ns | 0.785 ns | 0.520 ns |  16.10 ns |  17.74 ns |  16.57 ns |  17.10 ns |  17.42 ns |    1 | 0.0027 |      - |     136 B |
| DryIoc     |  17.94 ns | 0.725 ns | 0.480 ns |  17.36 ns |  18.79 ns |  17.81 ns |  18.62 ns |  18.70 ns |    1 | 0.0027 |      - |     136 B |
| SimpleInj  |  18.21 ns | 0.569 ns | 0.377 ns |  17.64 ns |  18.69 ns |  18.21 ns |  18.60 ns |  18.64 ns |    1 | 0.0027 |      - |     136 B |
| SSDI-Eager |  20.27 ns | 0.602 ns | 0.398 ns |  19.65 ns |  20.97 ns |  20.31 ns |  20.68 ns |  20.82 ns |    2 | 0.0027 |      - |     136 B |
| SSDI       |  41.28 ns | 1.174 ns | 0.699 ns |  40.46 ns |  42.72 ns |  41.21 ns |  42.10 ns |  42.41 ns |    3 | 0.0027 |      - |     136 B |
| Autofac    | 960.70 ns | 9.199 ns | 5.474 ns | 950.53 ns | 969.15 ns | 958.76 ns | 965.82 ns | 967.48 ns |    4 | 0.0868 | 0.0010 |    4384 B |
