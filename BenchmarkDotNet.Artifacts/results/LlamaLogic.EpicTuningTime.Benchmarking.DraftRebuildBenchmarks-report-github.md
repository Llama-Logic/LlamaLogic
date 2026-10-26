```

BenchmarkDotNet v0.15.2, Windows 11 (10.0.26200.9711)
Unknown processor
.NET SDK 10.0.402
  [Host]     : .NET 9.0.21 (9.0.2126.50404), X64 RyuJIT AVX2
  Job-SAYKNZ : .NET 9.0.21 (9.0.2126.50404), X64 RyuJIT AVX2

IterationCount=10  LaunchCount=1  RunStrategy=Monitoring  
WarmupCount=2  

```
| Method                   | Fixture              | Mean         | Error        | StdDev       | Gen0      | Gen1      | Allocated    |
|------------------------- |--------------------- |-------------:|-------------:|-------------:|----------:|----------:|-------------:|
| **BuildDraftAnnotations**    | **Group(...)thday [22]** |     **189.4 μs** |     **29.67 μs** |     **19.62 μs** |         **-** |         **-** |    **166.45 KB** |
| BuildDraftPropertyTree   | Group(...)thday [22] |     375.0 μs |     46.59 μs |     30.81 μs |         - |         - |    208.29 KB |
| BuildOriginalAnnotations | Group(...)thday [22] |     194.0 μs |     46.71 μs |     30.89 μs |         - |         - |    168.77 KB |
| GenerateCoreLibrary      | Group(...)thday [22] |     525.6 μs |     46.58 μs |     30.81 μs |         - |         - |     189.8 KB |
| GeneratePython           | Group(...)thday [22] |     911.0 μs |    153.44 μs |    101.49 μs |         - |         - |    708.42 KB |
| GenerateXmlInjector      | Group(...)thday [22] |     533.3 μs |     73.67 μs |     48.73 μs |         - |         - |    183.32 KB |
| Rebuild                  | Group(...)thday [22] |   2,457.1 μs |    529.77 μs |    350.41 μs |         - |         - |   1625.05 KB |
| **BuildDraftAnnotations**    | **SimSpawner**           |  **14,939.3 μs** |  **1,747.33 μs** |  **1,155.75 μs** | **1000.0000** |         **-** |  **27789.93 KB** |
| BuildDraftPropertyTree   | SimSpawner           |  31,444.3 μs |  3,939.41 μs |  2,605.67 μs | 1000.0000 |         - |  38852.37 KB |
| BuildOriginalAnnotations | SimSpawner           |  15,366.5 μs |  1,726.32 μs |  1,141.86 μs | 1000.0000 |         - |   27790.2 KB |
| GenerateCoreLibrary      | SimSpawner           |  10,915.4 μs |  6,479.86 μs |  4,286.02 μs |         - |         - |  13072.32 KB |
| GeneratePython           | SimSpawner           |  41,156.8 μs |  3,642.65 μs |  2,409.39 μs |         - |         - |  19152.54 KB |
| GenerateXmlInjector      | SimSpawner           |  15,717.5 μs |  9,688.47 μs |  6,408.33 μs |         - |         - |  13065.63 KB |
| Rebuild                  | SimSpawner           | 147,770.4 μs | 10,328.77 μs |  6,831.84 μs | 5000.0000 | 3000.0000 | 139723.02 KB |
| **BuildDraftAnnotations**    | **TeaSetPourTeaPottery** |   **4,161.1 μs** |    **800.74 μs** |    **529.64 μs** |         **-** |         **-** |    **6465.8 KB** |
| BuildDraftPropertyTree   | TeaSetPourTeaPottery |  10,422.0 μs |  2,029.66 μs |  1,342.49 μs |         - |         - |   8352.89 KB |
| BuildOriginalAnnotations | TeaSetPourTeaPottery |   3,842.8 μs |    885.68 μs |    585.82 μs |         - |         - |   6466.63 KB |
| GenerateCoreLibrary      | TeaSetPourTeaPottery |   5,254.1 μs |  1,176.18 μs |    777.97 μs |         - |         - |   2675.63 KB |
| GeneratePython           | TeaSetPourTeaPottery |  31,836.4 μs | 15,172.46 μs | 10,035.64 μs | 1000.0000 |         - |  37457.04 KB |
| GenerateXmlInjector      | TeaSetPourTeaPottery |   5,105.2 μs |    708.44 μs |    468.59 μs |         - |         - |   2669.31 KB |
| Rebuild                  | TeaSetPourTeaPottery |  52,448.4 μs | 20,967.15 μs | 13,868.47 μs | 2000.0000 | 1000.0000 |  64087.31 KB |
| **BuildDraftAnnotations**    | **UiTuning**             |   **3,932.2 μs** |  **1,098.59 μs** |    **726.65 μs** |         **-** |         **-** |    **4003.8 KB** |
| BuildDraftPropertyTree   | UiTuning             |   7,210.7 μs |  1,969.81 μs |  1,302.91 μs |         - |         - |   4125.51 KB |
| BuildOriginalAnnotations | UiTuning             |   3,631.3 μs |    737.90 μs |    488.08 μs |         - |         - |   4004.27 KB |
| GenerateCoreLibrary      | UiTuning             |   8,708.6 μs |  2,291.64 μs |  1,515.78 μs |         - |         - |    3877.7 KB |
| GeneratePython           | UiTuning             |   6,034.1 μs |  1,230.26 μs |    813.74 μs |         - |         - |   1783.86 KB |
| GenerateXmlInjector      | UiTuning             |   9,393.1 μs |  3,230.71 μs |  2,136.92 μs |         - |         - |   3871.22 KB |
| Rebuild                  | UiTuning             |  36,340.5 μs | 12,741.84 μs |  8,427.94 μs | 1000.0000 |         - |  21666.36 KB |
