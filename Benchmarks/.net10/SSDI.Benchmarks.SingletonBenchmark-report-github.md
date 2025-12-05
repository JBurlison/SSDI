```

BenchmarkDotNet v0.14.0, Windows 11 (10.0.26200.7171)
Unknown processor
.NET SDK 10.0.100
  [Host]     : .NET 10.0.0 (10.0.25.52411), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI
  Job-JMJFHY : .NET 10.0.0 (10.0.25.52411), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI

IterationCount=10  WarmupCount=2  

```
| Method    | Mean      | Error     | StdDev    | Min       | Max       | Median    | P90       | P95       | Rank | Gen0   | Allocated |
|---------- |----------:|----------:|----------:|----------:|----------:|----------:|----------:|----------:|-----:|-------:|----------:|
| Grace     |  2.995 ns | 0.1396 ns | 0.0924 ns |  2.911 ns |  3.188 ns |  2.961 ns |  3.089 ns |  3.139 ns |    1 |      - |         - |
| SSDI      |  3.528 ns | 0.0770 ns | 0.0458 ns |  3.462 ns |  3.611 ns |  3.515 ns |  3.570 ns |  3.590 ns |    1 |      - |         - |
| MS.DI     |  4.485 ns | 0.1307 ns | 0.0864 ns |  4.373 ns |  4.630 ns |  4.471 ns |  4.586 ns |  4.608 ns |    2 |      - |         - |
| SimpleInj |  4.785 ns | 0.1987 ns | 0.1182 ns |  4.668 ns |  4.960 ns |  4.708 ns |  4.938 ns |  4.949 ns |    2 |      - |         - |
| DryIoc    |  4.928 ns | 0.1458 ns | 0.0964 ns |  4.817 ns |  5.068 ns |  4.914 ns |  5.064 ns |  5.066 ns |    2 |      - |         - |
| Autofac   | 86.898 ns | 2.8068 ns | 1.8565 ns | 84.306 ns | 89.690 ns | 86.596 ns | 88.908 ns | 89.299 ns |    3 | 0.0161 |     808 B |
