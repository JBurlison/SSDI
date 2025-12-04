```

BenchmarkDotNet v0.14.0, Windows 11 (10.0.26200.7171)
Unknown processor
.NET SDK 10.0.100
  [Host]     : .NET 8.0.17 (8.0.1725.26602), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI
  Job-QWTSRU : .NET 8.0.17 (8.0.1725.26602), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI

IterationCount=3  WarmupCount=1  

```
| Method    | Mean        | Error        | StdDev    | Rank | Gen0   | Gen1   | Allocated |
|---------- |------------:|-------------:|----------:|-----:|-------:|-------:|----------:|
| DryIoc    |    980.5 ns |     29.52 ns |   1.62 ns |    1 | 0.0801 |      - |   3.97 KB |
| MS.DI     |  1,471.8 ns |    225.32 ns |  12.35 ns |    2 | 0.2518 | 0.0629 |  12.37 KB |
| Grace     |  4,841.2 ns |    946.61 ns |  51.89 ns |    3 | 0.4349 | 0.0153 |  21.65 KB |
| SimpleInj | 14,088.3 ns |  3,319.04 ns | 181.93 ns |    4 | 1.1292 | 0.5493 |  55.63 KB |
| Autofac   | 18,599.0 ns | 15,077.67 ns | 826.46 ns |    5 | 1.4038 |      - |  69.61 KB |
| SSDI      | 20,824.9 ns |  5,095.65 ns | 279.31 ns |    5 | 0.4578 | 0.4272 |  23.06 KB |
