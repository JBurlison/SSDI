```

BenchmarkDotNet v0.14.0, Windows 11 (10.0.26200.7171)
Unknown processor
.NET SDK 10.0.100
  [Host]     : .NET 10.0.0 (10.0.25.52411), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI
  Job-QHIBGV : .NET 10.0.0 (10.0.25.52411), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI

IterationCount=10  WarmupCount=2  

```
| Method    | Mean        | Error     | StdDev    | Rank | Gen0   | Gen1   | Allocated |
|---------- |------------:|----------:|----------:|-----:|-------:|-------:|----------:|
| DryIoc    |    833.1 ns |   5.32 ns |   3.17 ns |    1 | 0.0820 |      - |   4.02 KB |
| MS.DI     |  1,450.6 ns |  70.17 ns |  46.41 ns |    2 | 0.2518 | 0.0629 |   12.4 KB |
| Grace     |  4,239.3 ns |  44.40 ns |  29.37 ns |    3 | 0.4272 | 0.0153 |   21.3 KB |
| SimpleInj | 14,351.3 ns | 487.37 ns | 290.03 ns |    4 | 1.0986 | 0.4883 |  55.63 KB |
| Autofac   | 14,578.5 ns | 236.89 ns | 123.90 ns |    4 | 1.4648 |      - |  73.82 KB |
| SSDI      | 20,956.9 ns | 332.35 ns | 197.78 ns |    5 | 0.4578 | 0.4272 |  23.34 KB |
