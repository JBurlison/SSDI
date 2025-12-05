```

BenchmarkDotNet v0.14.0, Windows 11 (10.0.26200.7171)
Unknown processor
.NET SDK 10.0.100
  [Host]     : .NET 10.0.0 (10.0.25.52411), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI
  Job-GNDUPB : .NET 10.0.0 (10.0.25.52411), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI

IterationCount=10  WarmupCount=2  

```
| Method    | Mean      | Error     | StdDev    | Rank | Gen0   | Allocated |
|---------- |----------:|----------:|----------:|-----:|-------:|----------:|
| Grace     |  2.887 ns | 0.0443 ns | 0.0232 ns |    1 |      - |         - |
| SSDI      |  3.450 ns | 0.0241 ns | 0.0159 ns |    1 |      - |         - |
| SimpleInj |  4.792 ns | 0.1423 ns | 0.0744 ns |    2 |      - |         - |
| DryIoc    |  4.985 ns | 0.1454 ns | 0.0865 ns |    2 |      - |         - |
| MS.DI     |  5.817 ns | 0.0207 ns | 0.0137 ns |    2 |      - |         - |
| Autofac   | 74.118 ns | 1.9468 ns | 1.2877 ns |    3 | 0.0161 |     808 B |
