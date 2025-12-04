```

BenchmarkDotNet v0.14.0, Windows 11 (10.0.26200.7171)
Unknown processor
.NET SDK 10.0.100
  [Host]     : .NET 10.0.0 (10.0.25.52411), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI
  Job-QHIBGV : .NET 10.0.0 (10.0.25.52411), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI

IterationCount=10  WarmupCount=2  

```
| Method    | Mean        | Error     | StdDev   | Rank | Gen0   | Allocated |
|---------- |------------:|----------:|---------:|-----:|-------:|----------:|
| Grace     |    42.34 ns |  0.445 ns | 0.265 ns |    1 | 0.0048 |     240 B |
| SSDI      |    59.30 ns |  0.761 ns | 0.453 ns |    2 | 0.0048 |     240 B |
| DryIoc    |    69.39 ns |  1.033 ns | 0.614 ns |    2 | 0.0048 |     240 B |
| MS.DI     |    70.71 ns |  0.637 ns | 0.421 ns |    2 | 0.0048 |     240 B |
| SimpleInj |    85.19 ns |  0.675 ns | 0.447 ns |    3 | 0.0048 |     240 B |
| Autofac   | 1,346.16 ns | 12.091 ns | 7.997 ns |    4 | 0.1640 |    8320 B |
