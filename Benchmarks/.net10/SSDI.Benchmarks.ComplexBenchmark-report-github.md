```

BenchmarkDotNet v0.14.0, Windows 11 (10.0.26200.7171)
Unknown processor
.NET SDK 10.0.100
  [Host]     : .NET 10.0.0 (10.0.25.52411), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI
  Job-GNDUPB : .NET 10.0.0 (10.0.25.52411), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI

IterationCount=10  WarmupCount=2  

```
| Method     | Mean      | Error     | StdDev    | Rank | Gen0   | Gen1   | Allocated |
|----------- |----------:|----------:|----------:|-----:|-------:|-------:|----------:|
| MS.DI      |  15.76 ns |  0.574 ns |  0.380 ns |    1 | 0.0027 |      - |     136 B |
| Grace      |  16.09 ns |  0.613 ns |  0.320 ns |    1 | 0.0027 |      - |     136 B |
| DryIoc     |  17.09 ns |  0.359 ns |  0.188 ns |    1 | 0.0027 |      - |     136 B |
| SimpleInj  |  17.42 ns |  0.642 ns |  0.424 ns |    1 | 0.0027 |      - |     136 B |
| SSDI-Eager |  19.42 ns |  0.564 ns |  0.336 ns |    1 | 0.0027 |      - |     136 B |
| SSDI       |  41.28 ns |  1.155 ns |  0.764 ns |    2 | 0.0027 |      - |     136 B |
| Autofac    | 846.74 ns | 21.807 ns | 14.424 ns |    3 | 0.0868 | 0.0010 |    4384 B |
