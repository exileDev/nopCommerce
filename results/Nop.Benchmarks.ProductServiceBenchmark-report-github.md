``` ini

BenchmarkDotNet=v0.12.1, OS=Windows 10.0.18362.592 (1903/May2019Update/19H1)
Intel Core i5-3450 CPU 3.10GHz (Ivy Bridge), 1 CPU, 4 logical and 4 physical cores
.NET Core SDK=3.1.404
  [Host]     : .NET Core 3.1.10 (CoreCLR 4.700.20.51601, CoreFX 4.700.20.51901), X64 RyuJIT
  Job-AHXCEP : .NET Core 3.1.10 (CoreCLR 4.700.20.51601, CoreFX 4.700.20.51901), X64 RyuJIT

Jit=RyuJit  Platform=X64  Runtime=.NET Core 3.1  

```
|             Method |        Mean |    Gen 0 |   Gen 1 | Gen 2 | Allocated |
|------------------- |------------:|---------:|--------:|------:|----------:|
|     SearchProducts | 5,284.46 μs | 203.1250 | 39.0625 |     - | 663.87 KB |
|        GetProducts |    77.66 μs |   2.6855 |       - |     - |   8.43 KB |
| GetCurrentCustomer |   806.83 μs |  44.9219 | 10.7422 |     - | 139.17 KB |
| GetCustomerRoleIds |   875.02 μs |  47.8516 | 11.7188 |     - | 148.03 KB |
|        ToPagedList | 2,414.89 μs |  58.5938 |       - |     - | 183.73 KB |
