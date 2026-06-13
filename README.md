DjvuNet Library
===============

## CI status of master branch

| OS / Architecture | x64 | arm64 |
| :--- | :---: | :---: |
| **Windows** | [![CI Build](https://github.com/DjvuNet/DjvuNet/actions/workflows/build.yml/badge.svg?branch=master)](https://github.com/DjvuNet/DjvuNet/actions/workflows/build.yml) | [![CI Build](https://github.com/DjvuNet/DjvuNet/actions/workflows/build.yml/badge.svg?branch=master)](https://github.com/DjvuNet/DjvuNet/actions/workflows/build.yml) |
| **Linux** | [![CI Build](https://github.com/DjvuNet/DjvuNet/actions/workflows/build.yml/badge.svg?branch=master)](https://github.com/DjvuNet/DjvuNet/actions/workflows/build.yml) | [![CI Build](https://github.com/DjvuNet/DjvuNet/actions/workflows/build.yml/badge.svg?branch=master)](https://github.com/DjvuNet/DjvuNet/actions/workflows/build.yml) |
| **macOS** | [![CI Build](https://github.com/DjvuNet/DjvuNet/actions/workflows/build.yml/badge.svg?branch=master)](https://github.com/DjvuNet/DjvuNet/actions/workflows/build.yml) | [![CI Build](https://github.com/DjvuNet/DjvuNet/actions/workflows/build.yml/badge.svg?branch=master)](https://github.com/DjvuNet/DjvuNet/actions/workflows/build.yml) |


## Introduction

DjvuNet is an open source library designed to process and create documents encoded with DjVu format. Library is written in C# for .NET platform with
no external dependencies. Library supports Djvu format specification version 3 up to the minor version 26 (v3.26).
The so called "Secure DjVu" format is not supported as this specification was never published. Project was started several years ago
by [Telavian](https://github.com/Telavian) and after remaining inactive for some time currently is continued at new
[GitHub DjvuNet](https://github.com/DjvuNet) repo location. *Code is not production ready and still early in development. You should expect bugs, incomplete features, and API breakage as we work to improve it.* There are known bugs but anyway it should work on large number of djvu files (obviously it's still only a subset of all DjVu files which can be found out in the wild). Therefore, use it at your own risk and do not blame us for any of your problems.

## Current Status

*DjvuNet library is not ready for production use and is still in early development.* There are several known bugs which need to be fixed and missing features which need to be implemented first
before library could be treated as production ready or fully functional. Furthermore, there are some bugs in image decoder that leave some of images distorted.

Library supports full .NET Framework 4.7.2 or newer on Windows and .NET Core 10.0.0 or newer on Windows, Linux, macOS.

Project undergoes several architectural and implementations changes, which are done in "dev" branch.

- DjVu file format parser was optimized and refactored what so far resulted in more than 10x speedup.

- Image data decoding and **encoding** with Interpolated Dubuc-Deslauriers-Lemire (DDL) (4, 4) Discrete Wavelet Transform is close
to be finished but still has couple bugs which need to be fixed.

- There was very limited optimization work done in this area with some 30 - 40% improvements in performance and identification of several next optimization targets.

- ZP arithmetic coder and BZZ encoding/decoding is fully implemented and reached binary compatibility with DjvuLibre. It still awaits final optimizations.

- JB decoding is implemented but not optimized, encoding is not implemented.

- Image segmentation for Mixed Raster Content done in DjvuLibre with ColorPalette histogram calculation will be entirely rewritten as there was significant progress in image segmentation algorithms in the last two decades.

- Support for some DjvuLibre masked image formats is not implemented yet.

- Test framework is systematically developed and is composed of unit and functional tests. It covers project in top down way and provides
around 85% code coverage using 2 586 test cases with implementation target being more than 90% code coverage.

- Performance tests are implemented using BenchmarkDotNet in the `DjvuNet.Benchmarks` project.

- AVX2 SIMD optimizations for the YCbCr-to-RGB color space conversion (Pigeon transform) achieved a ~3.3x speedup over the legacy scalar implementation for 3-byte continuous conversions. The inverse `Rgb2YCbCr` transform achieved an even greater ~5.7x speedup over the scalar baseline.

- The color transform pipelines diverge from the DjVuLibre C++ implementation regarding image stride handling. The native C++ library assumes the input stride is a multiple of the 3-byte pixel size, which would lead to corruption of images with stride not being exact multiple of pixel size. DjvuNet's SIMD implementation manages byte strides using an overlapping last vector tail-shift to stay within bounds of processed input data while retaining 100% binary compatibility with the C++ outputs.

```ini
BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.8457/25H2/2025Update/HudsonValley2)
AMD Ryzen 5 3600 3.60GHz, 1 CPU, 12 logical and 6 physical cores
.NET SDK 10.0.300
  [Host] : .NET 10.0.8 (10.0.8, 10.0.826.23019), X64 RyuJIT x86-64-v3
```

**Rgb2YCbCr Benchmark Results (Contiguous):**
| Method    | Mean     | Error    | StdDev   | Ratio | Allocated | Alloc Ratio |
|---------- |---------:|---------:|---------:|------:|----------:|------------:|
| Native    | 75.08 ms | 0.068 ms | 0.057 ms |  1.00 |     432 B |        1.00 |
| Scalar    | 67.82 ms | 0.693 ms | 0.648 ms |  0.90 |     498 B |        1.15 |
| Unified   | 11.72 ms | 0.023 ms | 0.021 ms |  0.16 |      58 B |        0.13 |

**Rgb2YCbCr Benchmark Results (Padded [47]):**
| Method    | Mean     | Error    | StdDev   | Ratio | Allocated | Alloc Ratio |
|---------- |---------:|---------:|---------:|------:|----------:|------------:|
| Native    | 74.82 ms | 0.425 ms | 0.397 ms |  1.00 |     425 B |        1.00 |
| Scalar    | 67.41 ms | 0.803 ms | 0.751 ms |  0.90 |     126 B |        0.30 |
| Unified   | 11.67 ms | 0.017 ms | 0.015 ms |  0.16 |      41 B |        0.10 |

**YCbCr2Rgb Benchmark Results (Contiguous):**
| Method     | Mean     | Error    | StdDev   | Ratio | Allocated | Alloc Ratio |
|----------- |---------:|---------:|---------:|------:|----------:|------------:|
| Native     | 64.18 ms | 0.309 ms | 0.258 ms |  1.00 |     230 B |        1.00 |
| Scalar     | 36.01 ms | 0.200 ms | 0.187 ms |  0.56 |         - |        0.00 |
| Unified    | 11.23 ms | 0.029 ms | 0.026 ms |  0.18 |     202 B |        0.88 |

**YCbCr2Rgb Benchmark Results (Padded [47]):**
| Method     | Mean     | Error    | StdDev   | Ratio | Allocated | Alloc Ratio |
|----------- |---------:|---------:|---------:|------:|----------:|------------:|
| Native     | 63.46 ms | 0.139 ms | 0.116 ms |  1.00 |     202 B |        1.00 |
| Scalar     | 35.87 ms | 0.170 ms | 0.151 ms |  0.57 |     163 B |        0.81 |
| Unified    | 10.96 ms | 0.040 ms | 0.033 ms |  0.17 |     202 B |        1.00 |

*(Note: "Unified" denotes implementations combining AVX2 intrinsics with byte-stride tail handling).*

### SIMD and Parallelization Scaling Analysis (Rgb2YCbCr)

We have vectorized and parallelized the `Rgb2YCbCr` color space conversion pipeline, and vectorized the `YCbCr2Rgb` pipeline. The implementation routes execution across `Vector256` (AVX2) and `Vector128` (SSSE3/AdvSimd) intrinsics based on CPU topology and memory bandwidth constraints.

#### Baseline Performance Ratios (Rgb2YCbCr)
Benchmarking the `Rgb2YCbCr` transform across image sizes from 1,024 pixels to 20 Megapixels shows the throughput of the hardware-accelerated pipelines. In single-threaded execution, the AVX2 pipeline outperforms the C# Scalar and native DjVuLibre implementations:
- **C# Scalar vs Native C++:** The C# scalar loop outperforms the native C++ implementation by **~12–15%**.
- **Vector256 (AVX2) vs Native C++:** The AVX2 single-threaded pipeline is **~6.9x** faster than Native C++.
- **Vector256 (AVX2) vs C# Scalar:** The AVX2 single-threaded pipeline is **~6.1x** faster than C# Scalar.

#### Unified Single-Thread Baseline Matrix (Rgb2YCbCr Time per Image)

The following matrix tracks single-threaded execution time across the four `Rgb2YCbCr` implementations to establish performance ratios before parallelization overhead. All times are normalized to microseconds (µs) to represent the absolute "Time per Image".

| Image Size | Native C++ |  C# Scalar |  Vector128 | Vector256 (AVX2) |
|-----------:|-----------:|-----------:|-----------:|-----------------:|
|      1,024 |     4.3 µs |     3.3 µs |     1.8 µs |           0.5 µs |
|      4,096 |    15.8 µs |    13.3 µs |     7.1 µs |           2.1 µs |
|      9,216 |    35.1 µs |    29.5 µs |    16.0 µs |           4.8 µs |
|     16,384 |    61.6 µs |    51.6 µs |    28.2 µs |           8.3 µs |
|     36,864 |   136.8 µs |   116.5 µs |    64.0 µs |          18.8 µs |
|     65,536 |   242.3 µs |   205.5 µs |   113.3 µs |          33.5 µs |
|    262,144 |   958.5 µs |   831.8 µs |   453.0 µs |         135.4 µs |
|  1,048,576 |  3833.8 µs |  3344.0 µs |  1820.3 µs |         564.1 µs |
|  2,096,704 |  7655.0 µs |  6707.2 µs |  3623.3 µs |        1141.8 µs |
|  4,194,304 | 15328.0 µs | 13459.0 µs |  7268.3 µs |        2325.1 µs |
| 20,081,328 | 73125.0 µs | 65004.0 µs | 34860.0 µs |       10588.0 µs |

#### Parallel SIMD Scaling Matrix (Rgb2YCbCr Time per Image)

Building on the single-threaded baseline, the router dynamically scales the `MaxDegreeOfParallelism` for the `Rgb2YCbCr` transform. It calculates break-even thresholds to avoid parallelization overhead on small images and caps maximum threads on large images to prevent memory bus saturation (e.g., AVX2 saturates a dual-channel memory bus at 4 to 6 threads).

**MEMORY TOPOLOGY DISCLAIMER:**
The saturation caps (6 for AVX2, 12 for Vector128) are calibrated against standard dual-channel (2-channel) memory configurations typical of consumer hardware. We did not benchmark quad-channel or octa-channel memory configurations, as DjvuNet is not expected to be primarily utilized on enterprise server-grade or workstation hardware (e.g., EPYC, Threadripper, Xeon). This missing optimization for high-bandwidth server topologies can be addressed in future iterations if required.

| Image Size |   V128 (1T) |  V128 (2T) |  V128 (4T) |  V128 (6T) | V128 (12T) |   V256 (1T) |  V256 (2T) |  V256 (4T) |  V256 (6T) | V256 (12T) |
|-----------:|------------:|-----------:|-----------:|-----------:|-----------:|------------:|-----------:|-----------:|-----------:|-----------:|
|      1,024 |   *1.81 µs* |    2.59 µs |    4.12 µs |    4.80 µs |    6.16 µs |   *0.55 µs* |    1.82 µs |    2.85 µs |    3.49 µs |    4.60 µs |
|      4,096 |   *7.15 µs* |    5.83 µs |    7.16 µs |    7.90 µs |    9.29 µs |   *2.13 µs* |    3.55 µs |    4.94 µs |    5.57 µs |    6.87 µs |
|      9,216 |   16.00 µs  | *10.60 µs* | *10.60 µs* |   11.00 µs |   13.10 µs |   *4.88 µs* |    5.06 µs |    6.85 µs |    7.68 µs |    9.14 µs |
|     16,384 |   28.50 µs  |   19.00 µs | *13.50 µs* |   13.80 µs |   16.60 µs |    8.59 µs  |  *7.05 µs* |    8.67 µs |    9.45 µs |   11.90 µs |
|     36,864 |   63.50 µs  |   38.10 µs |   24.80 µs | *23.40 µs* |   25.20 µs |   18.90 µs  |   14.20 µs | *12.00 µs* |   13.10 µs |   16.20 µs |
|     65,536 |  113.20 µs  |   61.70 µs |   37.00 µs | *33.00 µs* |   35.60 µs |   34.30 µs  |   23.10 µs | *16.80 µs* |   18.80 µs |   22.60 µs |
|    262,144 |  455.00 µs  |  237.60 µs |  127.70 µs | *92.60 µs* |   98.30 µs |  139.70 µs  |   76.50 µs | *53.00 µs* |   54.30 µs |   59.90 µs |
|  1,048,576 | 1813.00 µs  |  935.10 µs |  491.60 µs |  345.80 µs |*323.40 µs* |  567.40 µs  |  300.40 µs |*203.80 µs* |  209.40 µs |  223.50 µs |
|  2,096,704 | 3645.00 µs  | 1860.00 µs |  964.40 µs |  673.10 µs |*601.00 µs* | 1118.00 µs  |  592.80 µs |  429.00 µs |*426.70 µs* |  450.40 µs |
|  4,194,304 | 7262.00 µs  | 3752.00 µs | 1919.00 µs | 1316.00 µs |*1183.00 µs*| 2249.00 µs  | 1319.00 µs |  985.20 µs |*968.40 µs* | 1027.00 µs |
| 20,081,328 |34860.00 µs  |17694.00 µs | 9124.00 µs | 6387.00 µs |*6151.00 µs*|10588.00 µs  | 6128.00 µs |*5888.00 µs*| 6081.00 µs | 6158.00 µs |

*(Note: Data reflects normalized "Time/Op (Real)" processing bounds per image payload. Asterisks (*) denote the dynamically selected optimal routing path).*

### SIMD and Parallelization Scaling Analysis (YCbCr2Rgb)

The inverse `YCbCr2Rgb` transform pipeline exhibits significantly different hardware constraints compared to the forward transform. Because the processing of 3-byte YCbCr pixels to BGR is less computationally demanding, the multithreaded SIMD pipeline faster saturates the CPU memory bus (or CPU cache memory), and multi-threading yields diminishing returns much earlier in the benchmark matrix.

The `GetThreadCountForYCbCr2Rgb` dispatcher shares identical routing logic for both AVX2 (`Vector256`) and SSSE3/AdvSimd (`Vector128`) paths because the bottleneck is identical (memory write bandwidth, strictly capped at 4 threads).

#### Unified Single-Thread Baseline Matrix (YCbCr2Rgb Time per Image)

The following matrix tracks single-threaded execution time across the four `YCbCr2Rgb` implementations. All times are normalized to microseconds (µs) to represent the absolute "Time per Image".

| Image Size | Native C++ |  C# Scalar |  Vector128 | Vector256 (AVX2) |
|-----------:|-----------:|-----------:|-----------:|-----------------:|
|      1,024 |     3.4 µs |     1.9 µs |     0.5 µs |           0.5 µs |
|      4,096 |    13.2 µs |     7.8 µs |     2.0 µs |           2.2 µs |
|      9,216 |    29.2 µs |    17.3 µs |     4.6 µs |           4.9 µs |
|     16,384 |    51.6 µs |    30.8 µs |     8.1 µs |           8.7 µs |
|     36,864 |   115.5 µs |    68.9 µs |    22.6 µs |          19.6 µs |
|     65,536 |   204.8 µs |   119.7 µs |    40.0 µs |          34.8 µs |
|    262,144 |   819.8 µs |   476.6 µs |   132.1 µs |         139.7 µs |
|  1,048,576 |  3278.0 µs |  1899.1 µs |   528.4 µs |         558.9 µs |
|  2,096,704 |  6564.9 µs |  3783.7 µs |  1035.5 µs |        1114.5 µs |
|  4,194,304 | 13043.2 µs |  7576.3 µs |  2097.3 µs |        2224.2 µs |
| 20,081,328 | 62727.2 µs | 36178.6 µs |  9865.7 µs |       10564.8 µs |

#### Parallel SIMD Scaling Matrix (YCbCr2Rgb Time per Image)

Building on the single-threaded baseline, the router dynamically scales the `MaxDegreeOfParallelism` based on the following exact thresholds for both Vector sizes:
- `pixelCount < 9,000`: 1 Thread (Avoid TPL overhead)
- `pixelCount >= 9,000`: 2 Threads
- `pixelCount >= 36,000`: 4 Threads (Memory bus saturation cap)

**MEMORY TOPOLOGY DISCLAIMER:**
The saturation caps (4 for both AVX2 and Vector128) are calibrated against standard dual-channel (2-channel) memory configurations typical of consumer hardware. We did not benchmark quad-channel or octa-channel memory configurations, as DjvuNet is not expected to be primarily utilized on enterprise server-grade or workstation hardware (e.g., EPYC, Threadripper, Xeon). This missing optimization for high-bandwidth server topologies can be addressed in future iterations if required.

| Image Size |   V128 (1T) |  V128 (2T) |  V128 (4T) |  V128 (6T) | V128 (12T) |   V256 (1T) |  V256 (2T) |  V256 (4T) |  V256 (6T) | V256 (12T) |
|-----------:|------------:|-----------:|-----------:|-----------:|-----------:|------------:|-----------:|-----------:|-----------:|-----------:|
|      1,024 |   *0.52 µs* |    1.74 µs |    2.74 µs |    3.20 µs |    4.26 µs |   *0.55 µs* |    1.78 µs |    2.71 µs |    3.10 µs |    4.05 µs |
|      4,096 |   *2.02 µs* |    3.09 µs |    4.29 µs |    5.09 µs |    6.09 µs |   *2.20 µs* |    3.43 µs |    4.30 µs |    5.10 µs |    6.08 µs |
|      9,216 |    4.63 µs  |  *4.45 µs* |    5.79 µs |    6.77 µs |    8.31 µs |    4.91 µs  |  *4.79 µs* |    5.73 µs |    6.68 µs |    8.15 µs |
|     16,384 |    8.08 µs  |  *6.59 µs* |    6.97 µs |    8.04 µs |   10.40 µs |    8.72 µs  |  *6.77 µs* |    7.01 µs |    8.14 µs |   10.29 µs |
|     36,864 |   22.64 µs  |   13.12 µs | *10.77 µs* |   11.54 µs |   13.89 µs |   19.60 µs  |   12.84 µs | *10.76 µs* |   11.52 µs |   13.58 µs |
|     65,536 |   40.02 µs  |   21.24 µs | *15.98 µs* |   16.36 µs |   19.74 µs |   34.78 µs  |   22.66 µs | *16.11 µs* |   16.53 µs |   19.48 µs |
|    262,144 |  132.08 µs  |   74.21 µs | *53.06 µs* |   53.90 µs |   57.14 µs |  139.65 µs  |   77.32 µs | *53.45 µs* |   53.49 µs |   56.53 µs |
|  1,048,576 |  528.43 µs  |  276.37 µs |*204.07 µs* |  206.81 µs |  214.44 µs |  558.86 µs  |  294.91 µs |*203.82 µs* |  205.09 µs |  215.21 µs |
|  2,096,704 | 1035.50 µs  |  536.39 µs |*401.37 µs* |  406.31 µs |  422.04 µs | 1114.50 µs  |  587.87 µs |*401.12 µs* |  410.04 µs |  420.35 µs |
|  4,194,304 | 2097.30 µs  | 1069.40 µs |*792.48 µs* |  797.62 µs |  823.14 µs | 2224.20 µs  | 1165.80 µs |*804.03 µs* |  798.43 µs |  816.77 µs |
| 20,081,328 | 9865.70 µs  | 5034.10 µs |*3805.40 µs*| 3853.00 µs | 3925.70 µs | 10564.80 µs | 5494.20 µs |*3801.60 µs*| 3845.40 µs | 3931.90 µs |

*(Note: Data reflects normalized "Time/Op (Real)" processing bounds per image payload. Asterisks (*) denote the dynamically selected optimal routing path).*


## DjVu Format Support Validation

Full library format handling validation is realized by using [DjVuLibre](http://djvu.sourceforge.net/) reference library implementation of DjVu format and supporting tools. Our github mirror of DjVuLibre is available here: [DjVuLibre for DjvuNet](https://github.com/DjvuNet/DjVuLibre).
.NET Bindings for majority of C API are available in DjvuNet.DjvuLibre project. It builds for
x86 and x64 targets only. Perhaps AnyCPU target will be available via NuGet packaging or alternatively via embedding of native binaries
in managed assembly - the issue is still open.

DjVuLibre was modified by creating libdjvulibre build integration with DjvuNet projects and modifying library by expanding some C APIs through
addition of memory management functions exports, implementation of Json formatted output from some dump functions and tools (djvudump),
and addition of functions bypassing s-expressions formatting used in text retrieval.

Modified library used for testing DjvuNet implementation of DjVu format is available here: [DjVuLibre for DjvuNet](https://github.com/DjvuNet/DjVuLibre).

Due to more restrictive licensing conditions of DjVuLibre .NET bindings project DjvuNet.DjvuLibre is double licensed under MIT and GPL v2 licenses.

## Building

### Windows for .NET Core 10.0 LTS

#### Prerequisites

- Visual Studio 2026 v18 with at least following workloads: .NET desktop development, desktop development with C++, .NET Core cross-platform development

- or alternatively to Visual Studio 2026 v18 one can use .NET Core SDK command line tools with any code editor and Visual Studio build tools (tested with Visual Studio Build Tools 2026 v18.4)

- Git

- Internet access for restoring dependencies

#### Building

* Building and testing is easily done with help of build.sh / build.cmd scripts which are located in root directory of DjvuNet repository. It accepts multiple configuration parameters and can be used to build and test all targets with one command line. It automatically clones and downloads all required repos and dependencies.*

Building from command line on Windows (tested on Windows 11 with Visual Studio 2026 v18.4 installed).

Open Visual Studio 2026 developer command prompt and clone repository
`````
git clone https://github.com/DjvuNet/DjvuNet.git
`````
Change directory to your repo
`````
cd djvunet
`````
Here one can run build.cmd script from command line (command accepts multiple configuration parameters)
`````
build -p x64 -c Release -t Rebuild -Test -f net10.0 (execute build -h to see all available options)
`````
Available dependencies fetching modes:
`````
By default, immutable tar.gz snapshots from latest tagged release are downloaded.
Use -cdps or -CloneDeps [full] to bypass tar.gz tagged release downloads and execute git clone for artifacts, djvulibre, and libgit2sharp.
This allows building with repository states unsynchronized to given tags between tagged releases based on latest commits.
`````
Available configurations:
`````
Debug, Release  (example: -c Debug),  default value Debug
`````
Available platforms:
DjvuNet.DjvuLibre and libdjvulibre are built only for x64 platforms - x86 is no longer tested and is not supported by CI builds, however, it should be possible to build locally.
`````
x64, x86, arm64, arm (example -p x64, default dotnet value AnyCPU is not supported for CI builds)
`````
Available targets:
`````
Clean, Build, Rebuild, Restore   (example -t Clean), default value Rebuild
`````

To build with Visual Studio open DjvuNet.sln file located in root directory of DjvuNet cloned
repository and build DjvuNet.csproj or entire solution.

#### Testing

Test data are stored in separate repository [artifacts](https://github.com/DjvuNet/artifacts).
Clone repository with git command (run it from DjvuNet repo root directory):
`````
git clone --depth 1 https://github.com/DjvuNet/artifacts.git
`````

Tests can be run by building and running tests from DjvuNet.Tests.dll and DjvuNet.Wavelet.Tests.dll
assemblies under Visual Studio from Test Explorer or using xUnit test runner from command line.

All tests should pass except for skipped.

Performance tests can be run with help of DjvuNetTest project.

### Windows for net10.0 target

#### Prerequisites

Visual Studio 2026 v18 with the following workloads: .NET desktop development, desktop development with C++, .NET Core cross-platform development
VS 2026 versions can be installed side by side and preview version can be safely used side by side with RTM versions.

- .NET Core 10.0.203 SDK

- Git

- Internet access for restoring dependencies

#### Building

Building from command line on Windows (tested on Windows 11 with Visual Studio 2026 v18.4 installed).

Open Visual Studio 2026 developer command prompt and clone repository
`````
git clone https://github.com/DjvuNet/DjvuNet.git
`````
Change directory to your repo
`````
cd djvunet
`````

Clone DjVuLibre from DjvuNet GutHub (this library was modified to integrate it into DjvuNet project)
`````
git clone https://github.com/DjvuNet/DjVuLibre.git
`````
DjVuLibre repo is now located in DjVuLibre directory of your DjvuNet repo.

From command prompt in DjvuNet root directory run:
`````
 build -c {Configuration} -p {Platform} -f {Framework}
`````

#### Testing

Test setup for all targets is not streamlined and is a bit involved. To avoid any problems use build script as follows:
`````
build -c {Configuration} -p {Platform} -f {Framework} -Test
`````
It is possible to skip building and testing of libdjvulibre and DjvuNet.DjvuLibre libraries by passing -sn or -SkipNative
command line switches to build script:
`````
build -c {Configuration} -p {Platform} -f {Framework} -Test -sn
`````

### Linux for net10.0 target

#### Prerequisites

Tested on Ubuntu 24.04 and 26.04, however, it should work on other distributions as well.

Install required tools and dependencies:

`````
sudo apt-get update
sudo apt-get install git zip unzip tar curl cmake pkg-config ninja-build autoconf automake libtool libgdiplus
`````

#### Building

Clone repository:
`````
git clone https://github.com/DjvuNet/DjvuNet.git
`````

Change directory to cloned DjvuNet repo:
`````
cd DjvuNet
`````

Use build script * build.sh * which will automatically install required .NET Core SDK version in repo for local use or install .NET Core 10.0.203 SDK manually. Provided .build.sh script will default to installed .NET Core SDK if version is compatible with required version for building defined global.json file and SDK tools are in PATH, otherwise, it will install required version locally in repo in Tools/dotnetcli directoryand use it for building. Build script accepts multiple configuration parameters and can be used to build and test all targets with one command line - run *./build.sh -h* to see all available options and defaults.

Building with script:
`````
./build.sh -t Rebuild -c Release -p x64 -f net10.0
`````

#### Testing

The easiest way to run tests is to use build.sh script with -Test command line switch. Script will automatically download required test artifacts if not present, build the repository, and run tests:
`````
./build.sh -t Build -c Release -p x64 -f net10.0 -Test
`````

If you want to build tests only and run them manually use -BuildTests switch:
`````
./build.sh -t Build -c Release -p x64 -f net10.0 -BuildTests
`````

Required for tests data and files can be downloaded manually - use latest available release.
`````
curl -L -o artifacts.tar.gz -s https://github.com/DjvuNet/artifacts/archive/refs/tags/v0.9.26132.0.tar.gz
tar -xzf artifacts.tar.gz
mv artifacts-0.9.26132.0 artifacts
rm artifacts.tar.gz
`````

Build and run DjvuNet tests (commands are starting from repo root):
`````
cd DjvuNet.Tests
dotnet build -c Release
dotnet publish -c Release
dotnet publish/path/DjvuNet.Tests.dll

# Return to repo root
cd ..

cd DjvuNet.Wavelet.Tests
dotnet build -c Release
dotnet publish -c Release
dotnet publish/path/DjvuNet.Wavelet.Tests.dll
`````


### macOS for net10.0 target

The macOS build and test process has full parity with Linux and is fully supported by the unified `build.sh` script.

#### Prerequisites

Install required tools and dependencies using Homebrew:

`````
brew install autoconf automake libtool mono-libgdiplus
`````

Download and install the .NET 10.0 SDK from [.NET Downloads](https://dotnet.microsoft.com/download/dotnet/10.0).

#### Building and Testing

Follow the Linux instructions for Building and Testing using the `./build.sh` script.

### Fast Developer Loop (Targeted Build, Test and Benchmark Execution)

When iterating quickly on a specific feature, running the full `build.{cmd|sh} -Test` suite is too slow. Before entering the fast developer loop, you **must build the whole repository once using the build script** (e.g., `build.cmd -c Release -BuildTests`) targeting the configuration you want to use to bootstrap the environment and restore all managed and native dependencies.

Because DjvuNet relies on native P/Invoke bindings, to run a single test, use `dotnet publish` to resolve dependencies and collect all binaries in publish directories, and then execute the standalone xUnit v3 test executable directly.

**Step 1: Publish the Test Project**
`````
dotnet publish -c Release DjvuNet.Wavelet.Tests\DjvuNet.Wavelet.Tests.csproj
`````

**Step 2: Execute the Test Directly**
Run the executable directly from the publish output directory using the xUnit `-method` filter. The method name must be a fully qualified name, or you can use wildcard characters like `*methodname*`.

Example for Windows x64 Release:
`````
build\bin\Windows.x64.Release\binaries\net10.0\win-x64\publish\DjvuNet.Wavelet.Tests.exe -method *methodname*
`````

To check compilation status of the single project simply run (if you run this command before you have run the whole repo build it may fail due to missing dependencies):

Example for Windows Debug:
`````
dotnet build DjvuNet.Wavelet.Tests\DjvuNet.Wavelet.Tests.csproj
`````

Example for Windows Release:
`````
dotnet build -c Release DjvuNet\DjvuNet.csproj
`````

### Fast Developer Loop (Interactive Benchmark Execution)

Similarly, the `DjvuNet.Benchmarks` project compiles to a standalone executable leveraging BenchmarkDotNet. Because it also references native dependencies, you must publish the project first.

**Step 1: Publish the Benchmark Project**
`````
dotnet publish -c Release DjvuNet.Benchmarks\DjvuNet.Benchmarks.csproj
`````

**Step 2: Execute the Benchmark Menu**
Run the executable directly from the publish output directory *without any arguments*. This will trigger an interactive menu listing all available benchmark classes.

Example for Windows x64 Release:
`````
build\bin\Windows.x64.Release\binaries\net10.0\win-x64\publish\DjvuNet.Benchmarks.exe
`````

**Example Output:**
`````
Available Benchmarks:
  #0  ImageCacheBenchmark
  #1  PigeonTransformBenchmark
  #2  Rgb2YCbCrBenchmark
  #3  Rgb2YCbCrHybridVsUnifiedBenchmark
...
You should select the target benchmark(s). Please, print a number of a benchmark (e.g. `0`) or a contained benchmark caption (e.g. `ImageCacheBenchmark`).
If you want to select few, please separate them with space ` ` (e.g. `1 2 3`).
You can also provide the class name in console arguments by using --filter. (e.g. `--filter *ImageCacheBenchmark*`).
Enter the asterisk `*` to select all.
`````

*(Note: You can bypass the interactive menu by passing the `--filter` argument directly, e.g., `DjvuNet.Benchmarks.exe --filter *PigeonTransformBenchmark*`)*

**Step 3: Review Results**
The benchmark executable features a custom post-processing pipeline. Upon completion, it automatically formats and archives the BenchmarkDotNet reports (HTML, MD, CSV, JSON, and ASM disassembly dumps) into the `TestResults\Benchmarks\reports\` directory at the repository root, injecting version and timestamp headers. Log files are saved in `TestResults\Benchmarks\`.

**Note on Unit Test Results:**
When executing the fast loop directly in the console, test results print to `stdout`. However, when running the full suite via the build script (e.g., `build.cmd -Test`), all xUnit XML test results are centralized and saved to the `TestResults\<Framework>\` directory (e.g., `TestResults\net10.0\DjvuNet.Wavelet.Tests.xml`).

## Usage

`````c#
using DjvuNet;

using(DjvuDocument doc = new DjvuDocument())
{
    doc.Load("Document.djvu");
    if (doc.Pages.Length > 0)
    {
        var firstPage = doc.Pages[0];
        var lastPage = doc.Pages[doc.Pages.Length - 1];

        using(System.Drawing.Bitmap pageImage = firstPage.BuildPageImage())
            firstPage.Save("DocumentTestImage1.png", ImageFormat.Png);

        string firstPageText = firstPage.Text;
        string lastPageText = lastPage.Text;
    }
}
`````

`````c#
using DjvuNet;

using(DjvuDocument doc = new DjvuDocument("Mcguffey's_Primer.djvu"))
{
    var page = doc.Pages[0];
    using(System.Drawing.Bitmap pageImage = page.BuildPageImage())
    {
        pageImage.Save("TestImage1.png", ImageFormat.Png);
        string pageText = page.Text;
    }
}
`````

## Reporting Issues

In case of build, test or DjvuNet library usage problems open new issue in [GitHub DjvuNet repo](https://github.com/DjvuNet/DjvuNet/issues) providing
detailed information on error (logs, command line output, stack trace, minidump) and used system.

We will try to adress all problems quickly unless they depend on missing features or known bugs which will be implemented or fixed according to our roadmap.

## License

DjvuNet is licensed under [MIT license](https://opensource.org/licenses/mit-license.php).

DjvuNet.DjvuLibre is double licensed under [MIT license](https://opensource.org/licenses/mit-license.php) and [GPL v2](https://opensource.org/licenses/GPL-2.0) or later.

DjVuLibre used for format support validation is licensed under [GPL v2](https://opensource.org/licenses/GPL-2.0) or later.
