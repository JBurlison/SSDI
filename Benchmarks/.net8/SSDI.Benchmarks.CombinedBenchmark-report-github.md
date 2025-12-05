```

BenchmarkDotNet v0.14.0, Windows 11 (10.0.26200.7171)
Unknown processor
.NET SDK 10.0.100
  [Host]     : .NET 8.0.17 (8.0.1725.26602), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI
  Job-JOBITY : .NET 8.0.17 (8.0.1725.26602), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI

IterationCount=10  WarmupCount=2  

```
| Method    | Mean       | Error      | StdDev    | Min        | Max        | Median     | P90        | P95        | Rank | Gen0   | Allocated |
|---------- |-----------:|-----------:|----------:|-----------:|-----------:|-----------:|-----------:|-----------:|-----:|-------:|----------:|
| Grace     |   9.687 ns |  0.2035 ns | 0.1065 ns |   9.503 ns |   9.787 ns |   9.725 ns |   9.780 ns |   9.783 ns |    1 | 0.0011 |      56 B |
| MS.DI     |  11.309 ns |  0.4823 ns | 0.3190 ns |  10.955 ns |  11.712 ns |  11.230 ns |  11.694 ns |  11.703 ns |    1 | 0.0011 |      56 B |
| DryIoc    |  11.784 ns |  0.5108 ns | 0.3379 ns |  11.410 ns |  12.372 ns |  11.625 ns |  12.285 ns |  12.328 ns |    1 | 0.0011 |      56 B |
| SimpleInj |  13.574 ns |  0.4823 ns | 0.2522 ns |  13.235 ns |  13.870 ns |  13.574 ns |  13.857 ns |  13.864 ns |    1 | 0.0011 |      56 B |
| SSDI      |  16.191 ns |  0.7551 ns | 0.4994 ns |  15.376 ns |  16.818 ns |  16.286 ns |  16.790 ns |  16.804 ns |    1 | 0.0011 |      56 B |
| Autofac   | 356.682 ns | 11.6731 ns | 7.7210 ns | 346.874 ns | 369.155 ns | 355.367 ns | 366.481 ns | 367.818 ns |    2 | 0.0339 |    1720 B |
