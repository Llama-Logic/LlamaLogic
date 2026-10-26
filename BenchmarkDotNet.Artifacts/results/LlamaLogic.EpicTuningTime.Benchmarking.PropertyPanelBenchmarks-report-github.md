```

BenchmarkDotNet v0.15.2, Windows 11 (10.0.26200.9711)
Unknown processor
.NET SDK 10.0.402
  [Host]     : .NET 9.0.21 (9.0.2126.50404), X64 RyuJIT AVX2
  Job-SAYKNZ : .NET 9.0.21 (9.0.2126.50404), X64 RyuJIT AVX2

IterationCount=10  LaunchCount=1  RunStrategy=Monitoring  
WarmupCount=2  

```
| Method                     | Fixture              | Mean        | Error        | StdDev       | Gen0      | Gen1      | Allocated    |
|--------------------------- |--------------------- |------------:|-------------:|-------------:|----------:|----------:|-------------:|
| **Analyze**                    | **Group(...)thday [22]** |  **1,037.9 μs** |     **93.01 μs** |     **61.52 μs** |         **-** |         **-** |     **458.3 KB** |
| BuildAnnotations           | Group(...)thday [22] |    834.1 μs |    137.51 μs |     90.96 μs |         - |         - |    768.03 KB |
| BuildPropertyTree          | Group(...)thday [22] |    285.4 μs |     53.40 μs |     35.32 μs |         - |         - |    189.04 KB |
| BuildPropertyTreeWithUnset | Group(...)thday [22] |    992.9 μs |    326.04 μs |    215.65 μs |         - |         - |     423.1 KB |
| **Analyze**                    | **SimSpawner**           | **78,880.2 μs** | **41,604.36 μs** | **27,518.71 μs** | **3000.0000** | **2000.0000** |  **60510.44 KB** |
| BuildAnnotations           | SimSpawner           | 74,707.9 μs | 25,640.21 μs | 16,959.41 μs | 8000.0000 | 4000.0000 | 166318.11 KB |
| BuildPropertyTree          | SimSpawner           | 26,141.0 μs |  5,386.68 μs |  3,562.95 μs | 1000.0000 |         - |  38846.19 KB |
| BuildPropertyTreeWithUnset | SimSpawner           | 28,749.0 μs |  7,044.90 μs |  4,659.77 μs | 1000.0000 |         - |  40259.68 KB |
| **Analyze**                    | **TeaSetPourTeaPottery** | **21,110.9 μs** | **16,503.52 μs** | **10,916.06 μs** |         **-** |         **-** |  **16715.32 KB** |
| BuildAnnotations           | TeaSetPourTeaPottery | 19,043.5 μs |  2,959.55 μs |  1,957.56 μs | 1000.0000 |         - |  32261.91 KB |
| BuildPropertyTree          | TeaSetPourTeaPottery |  7,618.1 μs |  2,040.05 μs |  1,349.37 μs |         - |         - |    8326.6 KB |
| BuildPropertyTreeWithUnset | TeaSetPourTeaPottery | 14,935.9 μs |  2,497.01 μs |  1,651.61 μs |         - |         - |  14592.46 KB |
| **Analyze**                    | **UiTuning**             | **17,782.8 μs** |  **3,278.91 μs** |  **2,168.79 μs** |         **-** |         **-** |  **12603.08 KB** |
| BuildAnnotations           | UiTuning             | 11,693.8 μs |  3,780.98 μs |  2,500.89 μs |         - |         - |  16759.25 KB |
| BuildPropertyTree          | UiTuning             |  5,634.0 μs |  1,211.65 μs |    801.43 μs |         - |         - |   4112.44 KB |
| BuildPropertyTreeWithUnset | UiTuning             | 10,368.2 μs |  1,629.47 μs |  1,077.80 μs |         - |         - |   6655.06 KB |
