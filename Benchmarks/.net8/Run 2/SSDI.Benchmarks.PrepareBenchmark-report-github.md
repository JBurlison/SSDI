```

BenchmarkDotNet v0.14.0, Windows 11 (10.0.26200.7171)
Unknown processor
.NET SDK 10.0.100
  [Host]     : .NET 8.0.17 (8.0.1725.26602), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI
  Job-JAKLOI : .NET 8.0.17 (8.0.1725.26602), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI

IterationCount=10  WarmupCount=2  

```
| Method    | Mean      | Error     | StdDev    | Rank | Gen0   | Gen1   | Allocated |
|---------- |----------:|----------:|----------:|-----:|-------:|-------:|----------:|
| DryIoc    |  1.033 μs | 0.1295 μs | 0.0857 μs |    1 | 0.0801 |      - |   3.97 KB |
| MS.DI     |  1.326 μs | 0.0597 μs | 0.0395 μs |    2 | 0.2518 | 0.0629 |  12.37 KB |
| Grace     |  4.871 μs | 0.0264 μs | 0.0157 μs |    3 | 0.4349 | 0.0153 |  21.65 KB |
| SimpleInj | 14.962 μs | 0.4412 μs | 0.2626 μs |    4 | 1.1292 | 0.5493 |  55.63 KB |
| Autofac   | 16.731 μs | 0.3913 μs | 0.2329 μs |    4 | 1.4038 |      - |  69.62 KB |
| SSDI      | 18.275 μs | 1.0304 μs | 0.5389 μs |    4 | 0.4578 | 0.4272 |  23.06 KB |
