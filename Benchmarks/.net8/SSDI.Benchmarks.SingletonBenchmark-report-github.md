```

BenchmarkDotNet v0.14.0, Windows 11 (10.0.26200.7171)
Unknown processor
.NET SDK 10.0.100
  [Host]     : .NET 8.0.17 (8.0.1725.26602), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI
  Job-MJPMRZ : .NET 8.0.17 (8.0.1725.26602), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI

IterationCount=10  WarmupCount=2  

```
| Method    | Mean       | Error     | StdDev    | Rank | Gen0   | Allocated |
|---------- |-----------:|----------:|----------:|-----:|-------:|----------:|
| Grace     |   4.720 ns | 0.0354 ns | 0.0234 ns |    1 |      - |         - |
| DryIoc    |   5.570 ns | 0.0565 ns | 0.0374 ns |    2 |      - |         - |
| SSDI      |   5.927 ns | 0.0524 ns | 0.0312 ns |    2 |      - |         - |
| MS.DI     |   6.584 ns | 0.0381 ns | 0.0227 ns |    2 |      - |         - |
| SimpleInj |   7.913 ns | 0.0640 ns | 0.0381 ns |    2 |      - |         - |
| Autofac   | 100.530 ns | 1.1107 ns | 0.7347 ns |    3 | 0.0161 |     808 B |
