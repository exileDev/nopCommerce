``` ini

BenchmarkDotNet=v0.12.1, OS=Windows 10.0.18362.592 (1903/May2019Update/19H1)
Intel Core i5-3450 CPU 3.10GHz (Ivy Bridge), 1 CPU, 4 logical and 4 physical cores
.NET Core SDK=3.1.404
  [Host]     : .NET Core 3.1.10 (CoreCLR 4.700.20.51601, CoreFX 4.700.20.51901), X64 RyuJIT
  Job-GFUQZY : .NET Core 3.1.10 (CoreCLR 4.700.20.51601, CoreFX 4.700.20.51901), X64 RyuJIT

Jit=RyuJit  Platform=X64  Runtime=.NET Core 3.1  

```
|             Method |        Mean |    Gen 0 |   Gen 1 | Gen 2 | Allocated |
|------------------- |------------:|---------:|--------:|------:|----------:|
|     SearchProducts | 7,832.19 μs |        - |       - |     - | 814.59 KB |
|        GetProducts |    65.30 μs |   2.0752 |       - |     - |   6.41 KB |
| GetCurrentCustomer |   885.24 μs |  54.6875 | 10.7422 |     - | 170.37 KB |
| GetCustomerRoleIds |    53.36 μs |   3.4180 |       - |     - |   10.6 KB |
|        ToPagedList | 4,405.09 μs | 109.3750 |       - |     - | 350.49 KB |
