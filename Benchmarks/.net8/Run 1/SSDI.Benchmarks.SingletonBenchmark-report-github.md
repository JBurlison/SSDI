```

BenchmarkDotNet v0.14.0, Windows 11 (10.0.26200.7171)
Unknown processor
.NET SDK 10.0.100
  [Host]     : .NET 8.0.17 (8.0.1725.26602), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI
  Job-QWTSRU : .NET 8.0.17 (8.0.1725.26602), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI

IterationCount=3  WarmupCount=1  

```
| Method    | Mean      | Error      | StdDev    | Rank | Gen0   | Allocated |
|---------- |----------:|-----------:|----------:|-----:|-------:|----------:|
| Grace     |  4.808 ns |  0.4194 ns | 0.0230 ns |    1 |      - |         - |
| DryIoc    |  5.574 ns |  0.6318 ns | 0.0346 ns |    1 |      - |         - |
| SSDI      |  5.708 ns |  2.7040 ns | 0.1482 ns |    1 |      - |         - |
| MS.DI     |  6.062 ns |  0.3629 ns | 0.0199 ns |    1 |      - |         - |
| SimpleInj |  8.501 ns |  8.4812 ns | 0.4649 ns |    2 |      - |         - |
| Autofac   | 99.009 ns | 18.5062 ns | 1.0144 ns |    3 | 0.0161 |     808 B |
