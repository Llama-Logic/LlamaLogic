```

BenchmarkDotNet v0.15.2, Windows 11 (10.0.26200.9711)
Unknown processor
.NET SDK 10.0.402
  [Host]     : .NET 9.0.21 (9.0.2126.50404), X64 RyuJIT AVX2
  Job-SNZIMP : .NET 9.0.21 (9.0.2126.50404), X64 RyuJIT AVX2

InvocationCount=1  IterationCount=10  LaunchCount=1  
RunStrategy=Monitoring  UnrollFactor=1  WarmupCount=2  

```
| Method                          | Fixture              | Mean       | Error      | StdDev     | Median     | Gen0      | Gen1      | Allocated    |
|-------------------------------- |--------------------- |-----------:|-----------:|-----------:|-----------:|----------:|----------:|-------------:|
| **AddItemToLargestList**            | **Group(...)thday [22]** |   **1.655 ms** |  **0.2278 ms** |  **0.1507 ms** |   **1.614 ms** |         **-** |         **-** |    **989.82 KB** |
| DuplicateFirstItemOfLargestList | Group(...)thday [22] |   1.672 ms |  0.1903 ms |  0.1259 ms |   1.642 ms |         - |         - |   1047.85 KB |
| RemoveFirstItemOfLargestList    | Group(...)thday [22] |   1.662 ms |  0.2131 ms |  0.1409 ms |   1.648 ms |         - |         - |   1016.68 KB |
| RevertFirstItemOfLargestList    | Group(...)thday [22] |   1.912 ms |  0.4750 ms |  0.3142 ms |   1.735 ms |         - |         - |   1041.46 KB |
| SetDeepestValue                 | Group(...)thday [22] |   1.597 ms |  0.2313 ms |  0.1530 ms |   1.545 ms |         - |         - |   1025.24 KB |
| **AddItemToLargestList**            | **SimSpawner**           | **178.672 ms** | **74.7095 ms** | **49.4157 ms** | **162.967 ms** | **6000.0000** | **4000.0000** | **131922.43 KB** |
| DuplicateFirstItemOfLargestList | SimSpawner           | 174.305 ms | 53.5504 ms | 35.4203 ms | 164.809 ms | 6000.0000 | 4000.0000 | 134176.39 KB |
| RemoveFirstItemOfLargestList    | SimSpawner           | 179.011 ms | 57.3917 ms | 37.9611 ms | 165.891 ms | 6000.0000 | 4000.0000 | 134171.69 KB |
| RevertFirstItemOfLargestList    | SimSpawner           | 164.408 ms | 20.2215 ms | 13.3753 ms | 165.238 ms | 6000.0000 | 4000.0000 | 137875.08 KB |
| SetDeepestValue                 | SimSpawner           | 174.940 ms | 53.6769 ms | 35.5039 ms | 166.233 ms | 6000.0000 | 4000.0000 | 138849.91 KB |
| **AddItemToLargestList**            | **TeaSetPourTeaPottery** |  **44.709 ms** | **26.1550 ms** | **17.2999 ms** |  **35.747 ms** | **1000.0000** |         **-** |   **36877.1 KB** |
| DuplicateFirstItemOfLargestList | TeaSetPourTeaPottery |  47.180 ms | 32.1320 ms | 21.2534 ms |  35.119 ms | 1000.0000 |         - |   37188.4 KB |
| RemoveFirstItemOfLargestList    | TeaSetPourTeaPottery |  45.307 ms | 27.6727 ms | 18.3038 ms |  37.101 ms | 1000.0000 |         - |   37174.7 KB |
| RevertFirstItemOfLargestList    | TeaSetPourTeaPottery |  53.060 ms | 27.6570 ms | 18.2934 ms |  47.240 ms | 2000.0000 |         - |  40286.58 KB |
| SetDeepestValue                 | TeaSetPourTeaPottery |  41.620 ms | 23.3091 ms | 15.4176 ms |  31.770 ms | 1000.0000 |         - |  36562.91 KB |
| **AddItemToLargestList**            | **UiTuning**             |  **36.517 ms** | **15.9240 ms** | **10.5327 ms** |  **37.108 ms** | **1000.0000** |         **-** |  **23696.07 KB** |
| DuplicateFirstItemOfLargestList | UiTuning             |  32.689 ms | 19.8787 ms | 13.1485 ms |  28.982 ms | 1000.0000 |         - |   23750.7 KB |
| RemoveFirstItemOfLargestList    | UiTuning             |  37.843 ms | 21.3162 ms | 14.0994 ms |  40.703 ms | 1000.0000 |         - |   23745.1 KB |
| RevertFirstItemOfLargestList    | UiTuning             |  36.374 ms | 21.0378 ms | 13.9152 ms |  35.993 ms | 1000.0000 |         - |   23809.3 KB |
| SetDeepestValue                 | UiTuning             |  39.084 ms | 19.0262 ms | 12.5847 ms |  41.785 ms | 1000.0000 |         - |  24831.33 KB |
