```

BenchmarkDotNet v0.14.0, Windows 11 (10.0.26200.7171)
Unknown processor
.NET SDK 10.0.100
  [Host]     : .NET 10.0.0 (10.0.25.52411), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI
  Job-QHIBGV : .NET 10.0.0 (10.0.25.52411), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI

IterationCount=10  WarmupCount=2  

```
| Method    | Mean      | Error    | StdDev   | Rank | Gen0   | Gen1   | Allocated |
|---------- |----------:|---------:|---------:|-----:|-------:|-------:|----------:|
| Grace     |  15.96 ns | 0.687 ns | 0.409 ns |    1 | 0.0027 |      - |     136 B |
| MS.DI     |  16.01 ns | 0.142 ns | 0.074 ns |    1 | 0.0027 |      - |     136 B |
| SimpleInj |  17.50 ns | 0.276 ns | 0.182 ns |    1 | 0.0027 |      - |     136 B |
| DryIoc    |  17.63 ns | 0.369 ns | 0.244 ns |    1 | 0.0027 |      - |     136 B |
| SSDI      |  51.65 ns | 0.177 ns | 0.117 ns |    2 | 0.0027 |      - |     136 B |
| Autofac   | 912.56 ns | 8.446 ns | 5.587 ns |    3 | 0.0868 | 0.0010 |    4384 B |
