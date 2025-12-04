```

BenchmarkDotNet v0.14.0, Windows 11 (10.0.26200.7171)
Unknown processor
.NET SDK 10.0.100
  [Host]     : .NET 8.0.17 (8.0.1725.26602), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI
  Job-JAKLOI : .NET 8.0.17 (8.0.1725.26602), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI

IterationCount=10  WarmupCount=2  

```
| Method    | Mean       | Error     | StdDev    | Rank | Gen0   | Allocated |
|---------- |-----------:|----------:|----------:|-----:|-------:|----------:|
| Grace     |   4.765 ns | 0.0187 ns | 0.0124 ns |    1 |      - |         - |
| DryIoc    |   5.562 ns | 0.0213 ns | 0.0127 ns |    1 |      - |         - |
| MS.DI     |   6.097 ns | 0.0730 ns | 0.0483 ns |    1 |      - |         - |
| SSDI      |   6.142 ns | 0.3607 ns | 0.2386 ns |    1 |      - |         - |
| SimpleInj |   7.964 ns | 0.0593 ns | 0.0353 ns |    2 |      - |         - |
| Autofac   | 100.161 ns | 0.9134 ns | 0.6042 ns |    3 | 0.0161 |     808 B |
