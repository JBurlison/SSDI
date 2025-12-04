```

BenchmarkDotNet v0.14.0, Windows 11 (10.0.26200.7171)
Unknown processor
.NET SDK 10.0.100
  [Host]     : .NET 8.0.17 (8.0.1725.26602), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI
  Job-QWTSRU : .NET 8.0.17 (8.0.1725.26602), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI

IterationCount=3  WarmupCount=1  

```
| Method    | Mean        | Error      | StdDev   | Rank | Gen0   | Allocated |
|---------- |------------:|-----------:|---------:|-----:|-------:|----------:|
| Grace     |    17.95 ns |   5.620 ns | 0.308 ns |    1 | 0.0027 |     136 B |
| MS.DI     |    18.22 ns |   2.455 ns | 0.135 ns |    1 | 0.0027 |     136 B |
| DryIoc    |    18.22 ns |   2.818 ns | 0.154 ns |    1 | 0.0027 |     136 B |
| SimpleInj |    20.98 ns |   1.320 ns | 0.072 ns |    1 | 0.0027 |     136 B |
| SSDI      |    69.55 ns |  16.913 ns | 0.927 ns |    2 | 0.0026 |     136 B |
| Autofac   | 1,226.73 ns | 110.248 ns | 6.043 ns |    3 | 0.0935 |    4744 B |
