```

BenchmarkDotNet v0.14.0, Windows 11 (10.0.26200.7171)
Unknown processor
.NET SDK 10.0.100
  [Host]     : .NET 8.0.17 (8.0.1725.26602), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI
  Job-JAKLOI : .NET 8.0.17 (8.0.1725.26602), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI

IterationCount=10  WarmupCount=2  

```
| Method    | Mean        | Error    | StdDev   | Rank | Gen0   | Allocated |
|---------- |------------:|---------:|---------:|-----:|-------:|----------:|
| Grace     |    17.81 ns | 0.253 ns | 0.167 ns |    1 | 0.0027 |     136 B |
| MS.DI     |    18.30 ns | 0.425 ns | 0.281 ns |    1 | 0.0027 |     136 B |
| DryIoc    |    18.55 ns | 0.635 ns | 0.420 ns |    1 | 0.0027 |     136 B |
| SimpleInj |    23.23 ns | 2.617 ns | 1.731 ns |    2 | 0.0027 |     136 B |
| SSDI      |    70.57 ns | 0.724 ns | 0.431 ns |    3 | 0.0026 |     136 B |
| Autofac   | 1,188.41 ns | 7.629 ns | 5.046 ns |    4 | 0.0935 |    4744 B |
