```

BenchmarkDotNet v0.14.0, Windows 11 (10.0.26200.7171)
Unknown processor
.NET SDK 10.0.100
  [Host]     : .NET 8.0.17 (8.0.1725.26602), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI
  Job-QWTSRU : .NET 8.0.17 (8.0.1725.26602), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI

IterationCount=3  WarmupCount=1  

```
| Method    | Mean       | Error      | StdDev    | Rank | Gen0   | Allocated |
|---------- |-----------:|-----------:|----------:|-----:|-------:|----------:|
| Grace     |   9.138 ns |  2.1918 ns | 0.1201 ns |    1 | 0.0011 |      56 B |
| DryIoc    |  10.761 ns |  0.4574 ns | 0.0251 ns |    1 | 0.0011 |      56 B |
| MS.DI     |  10.849 ns |  1.3298 ns | 0.0729 ns |    1 | 0.0011 |      56 B |
| SimpleInj |  13.228 ns |  0.9068 ns | 0.0497 ns |    1 | 0.0011 |      56 B |
| SSDI      |  21.752 ns |  3.2052 ns | 0.1757 ns |    2 | 0.0011 |      56 B |
| Autofac   | 343.616 ns | 43.9529 ns | 2.4092 ns |    3 | 0.0339 |    1720 B |
