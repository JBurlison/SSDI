```

BenchmarkDotNet v0.14.0, Windows 11 (10.0.26200.7171)
Unknown processor
.NET SDK 10.0.100
  [Host]     : .NET 10.0.0 (10.0.25.52411), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI
  Job-QHIBGV : .NET 10.0.0 (10.0.25.52411), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI

IterationCount=10  WarmupCount=2  

```
| Method    | Mean      | Error     | StdDev    | Rank | Gen0   | Allocated |
|---------- |----------:|----------:|----------:|-----:|-------:|----------:|
| Grace     |  2.905 ns | 0.0273 ns | 0.0143 ns |    1 |      - |         - |
| SSDI      |  3.505 ns | 0.0163 ns | 0.0097 ns |    1 |      - |         - |
| MS.DI     |  4.333 ns | 0.0346 ns | 0.0229 ns |    1 |      - |         - |
| DryIoc    |  4.783 ns | 0.0574 ns | 0.0380 ns |    2 |      - |         - |
| SimpleInj |  5.164 ns | 0.0099 ns | 0.0052 ns |    2 |      - |         - |
| Autofac   | 87.637 ns | 1.0472 ns | 0.6926 ns |    3 | 0.0161 |     808 B |
