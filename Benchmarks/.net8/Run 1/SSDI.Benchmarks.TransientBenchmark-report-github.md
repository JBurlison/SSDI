```

BenchmarkDotNet v0.14.0, Windows 11 (10.0.26200.7171)
Unknown processor
.NET SDK 10.0.100
  [Host]     : .NET 8.0.17 (8.0.1725.26602), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI
  Job-QWTSRU : .NET 8.0.17 (8.0.1725.26602), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI

IterationCount=3  WarmupCount=1  

```
| Method    | Mean        | Error     | StdDev   | Rank | Gen0   | Allocated |
|---------- |------------:|----------:|---------:|-----:|-------:|----------:|
| Grace     |    69.38 ns | 41.761 ns | 2.289 ns |    1 | 0.0048 |     240 B |
| SSDI      |    72.15 ns | 90.145 ns | 4.941 ns |    1 | 0.0048 |     240 B |
| DryIoc    |    75.63 ns |  5.852 ns | 0.321 ns |    1 | 0.0048 |     240 B |
| MS.DI     |    82.41 ns |  3.352 ns | 0.184 ns |    1 | 0.0048 |     240 B |
| SimpleInj |   106.30 ns |  7.608 ns | 0.417 ns |    2 | 0.0048 |     240 B |
| Autofac   | 1,463.39 ns | 97.865 ns | 5.364 ns |    3 | 0.1640 |    8320 B |
