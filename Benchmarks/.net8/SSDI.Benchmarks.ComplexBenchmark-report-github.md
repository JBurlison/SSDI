```

BenchmarkDotNet v0.14.0, Windows 11 (10.0.26200.7171)
Unknown processor
.NET SDK 10.0.100
  [Host]     : .NET 8.0.17 (8.0.1725.26602), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI
  Job-JOBITY : .NET 8.0.17 (8.0.1725.26602), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI

IterationCount=10  WarmupCount=2  

```
| Method     | Mean        | Error    | StdDev   | Min         | Max         | Median      | P90         | P95         | Rank | Gen0   | Allocated |
|----------- |------------:|---------:|---------:|------------:|------------:|------------:|------------:|------------:|-----:|-------:|----------:|
| MS.DI      |    18.09 ns | 0.248 ns | 0.164 ns |    17.81 ns |    18.30 ns |    18.14 ns |    18.25 ns |    18.27 ns |    1 | 0.0027 |     136 B |
| DryIoc     |    18.30 ns | 0.321 ns | 0.212 ns |    17.96 ns |    18.54 ns |    18.36 ns |    18.52 ns |    18.53 ns |    1 | 0.0027 |     136 B |
| Grace      |    18.66 ns | 0.901 ns | 0.596 ns |    17.83 ns |    19.58 ns |    18.59 ns |    19.56 ns |    19.57 ns |    1 | 0.0027 |     136 B |
| SimpleInj  |    21.50 ns | 0.796 ns | 0.473 ns |    20.81 ns |    22.11 ns |    21.57 ns |    22.06 ns |    22.09 ns |    1 | 0.0027 |     136 B |
| SSDI-Eager |    22.38 ns | 0.194 ns | 0.128 ns |    22.19 ns |    22.60 ns |    22.41 ns |    22.48 ns |    22.54 ns |    1 | 0.0027 |     136 B |
| SSDI       |    47.04 ns | 0.327 ns | 0.216 ns |    46.72 ns |    47.46 ns |    47.02 ns |    47.25 ns |    47.36 ns |    2 | 0.0027 |     136 B |
| Autofac    | 1,182.31 ns | 5.251 ns | 3.473 ns | 1,177.62 ns | 1,187.55 ns | 1,181.64 ns | 1,186.57 ns | 1,187.06 ns |    3 | 0.0935 |    4744 B |
