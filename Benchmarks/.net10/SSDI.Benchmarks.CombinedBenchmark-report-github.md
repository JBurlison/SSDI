```

BenchmarkDotNet v0.14.0, Windows 11 (10.0.26200.7171)
Unknown processor
.NET SDK 10.0.100
  [Host]     : .NET 10.0.0 (10.0.25.52411), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI
  Job-JMJFHY : .NET 10.0.0 (10.0.25.52411), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI

IterationCount=10  WarmupCount=2  

```
| Method    | Mean       | Error     | StdDev    | Min        | Max        | Median     | P90        | P95        | Rank | Gen0   | Allocated |
|---------- |-----------:|----------:|----------:|-----------:|-----------:|-----------:|-----------:|-----------:|-----:|-------:|----------:|
| Grace     |   7.779 ns | 0.1783 ns | 0.0932 ns |   7.600 ns |   7.903 ns |   7.792 ns |   7.874 ns |   7.889 ns |    1 | 0.0011 |      56 B |
| MS.DI     |   9.140 ns | 0.0688 ns | 0.0455 ns |   9.069 ns |   9.198 ns |   9.145 ns |   9.195 ns |   9.196 ns |    1 | 0.0011 |      56 B |
| DryIoc    |  10.028 ns | 0.3583 ns | 0.2370 ns |   9.775 ns |  10.414 ns |   9.943 ns |  10.380 ns |  10.397 ns |    2 | 0.0011 |      56 B |
| SimpleInj |  10.056 ns | 0.2986 ns | 0.1975 ns |   9.797 ns |  10.439 ns |  10.000 ns |  10.267 ns |  10.353 ns |    2 | 0.0011 |      56 B |
| SSDI      |  12.450 ns | 0.0573 ns | 0.0379 ns |  12.391 ns |  12.513 ns |  12.447 ns |  12.499 ns |  12.506 ns |    3 | 0.0011 |      56 B |
| Autofac   | 294.594 ns | 3.9014 ns | 2.0405 ns | 290.597 ns | 296.611 ns | 294.920 ns | 296.598 ns | 296.605 ns |    4 | 0.0339 |    1720 B |
