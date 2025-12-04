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
| Grace     |   8.954 ns | 0.1262 ns | 0.0751 ns |    1 | 0.0011 |      56 B |
| DryIoc    |  10.899 ns | 0.1868 ns | 0.1236 ns |    1 | 0.0011 |      56 B |
| MS.DI     |  11.186 ns | 0.0793 ns | 0.0472 ns |    1 | 0.0011 |      56 B |
| SimpleInj |  13.265 ns | 0.1196 ns | 0.0712 ns |    1 | 0.0011 |      56 B |
| SSDI      |  21.578 ns | 0.1441 ns | 0.0953 ns |    2 | 0.0011 |      56 B |
| Autofac   | 354.176 ns | 4.1368 ns | 2.7362 ns |    3 | 0.0339 |    1720 B |
