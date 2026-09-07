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

- Image data decoding and encoding with Interpolated Dubuc-Deslauriers-Lemire (DDL) (4, 4) Discrete Wavelet Transform is close
to be finished but still has couple bugs which need to be fixed.

- There was very limited optimization work done in this area with some 30 - 40% improvements in performance and identification of several next optimization targets.

- ZP arithmetic coder and BZZ encoding/decoding is fully implemented and reached binary compatibility with DjvuLibre. It still awaits final optimizations.

- JB2 decoding and encoding is fully implemented and reached binary parity as well.

- Image segmentation for Mixed Raster Content done in DjvuLibre with ColorPalette histogram calculation will be entirely rewritten as there was significant progress in image segmentation algorithms in the last two decades.

- Support for some DjvuLibre masked image formats is not implemented yet.

- Test framework is systematically developed and is composed of unit and functional tests. It covers project in top down way and provides
around 85% code coverage using 2 586 test cases with implementation target being more than 90% code coverage.

- Performance tests are implemented using BenchmarkDotNet in the `DjvuNet.Benchmarks` project.

- Zero-Allocation Memory Architecture & SIMD Integration: The rendering pipelines for `Bitmap` and `PixelMap` utilize unmanaged memory pinning, dynamic thread saturation routing, and `Span<T>` pooling. Hardware-accelerated SIMD intrinsics (scaling dynamically from `Vector128`, through `AVX2`, up to `AVX-512`) execute YCbCr-to-RGB color space conversions (Pigeon transform), Blit sub-sampling, and RLE compression/decompression operations.

- Processing Throughput Metrics: The benchmark suites in the `DjvuNet.Benchmarks` project record up to 51 GB/s in RLE compression pipelines and > 100 GB/s in Blit with subsampling pipelines. These results outperform the native C++ `djvulibre` implementations. Detailed benchmark reports can be generated locally. *(Note: Peak throughput numbers were recorded locally on a machine equipped with an AMD Ryzen 9 9950X, 16-Core CPU, and 64GB DDR5 RAM).*


## DjVu Format Support Validation

Full library format handling validation is realized by using [DjVuLibre](http://djvu.sourceforge.net/) reference library implementation of DjVu format and supporting tools. Our github mirror of DjVuLibre is available here: [DjVuLibre for DjvuNet](https://github.com/DjvuNet/DjVuLibre).
.NET Bindings for majority of C API are available in DjvuNet.DjvuLibre project. It natively targets `x64` and `arm64` hardware architectures across all supported operating systems.

DjVuLibre was modified by creating libdjvulibre build integration with DjvuNet projects and modifying library by expanding some C APIs through
addition of memory management functions exports, implementation of Json formatted output from some dump functions and tools (djvudump),
and addition of functions bypassing s-expressions formatting used in text retrieval.

Modified library used for testing DjvuNet implementation of DjVu format is available here: [DjVuLibre for DjvuNet](https://github.com/DjvuNet/DjVuLibre).

Due to more restrictive licensing conditions of DjVuLibre .NET bindings project DjvuNet.DjvuLibre is double licensed under MIT and GPL v2 licenses.

### Cross-Platform Building and Testing (Windows, Linux, macOS)

DjvuNet uses unified command-line scripts (`build.cmd` on Windows, `build.sh` on Unix) to automatically fetch dependencies, compile native C++ layers, and build the C# .NET solution.

#### 1. Prerequisites
- **Windows:** Visual Studio 2026 (or newer) Developer Command Prompt, git.
- **Linux (Ubuntu):** `sudo apt-get install git zip unzip tar curl cmake pkg-config ninja-build autoconf automake libtool libgdiplus`
- **macOS:** `brew install autoconf automake libtool mono-libgdiplus`

#### 2. Clone the Repository
`````bash
git clone https://github.com/DjvuNet/DjvuNet.git
cd DjvuNet
`````

#### 3. Build & Test
The easiest way to bootstrap the environment, compile all code, and run tests is to use the automated build script.

**Windows:**
`````cmd
build -t Rebuild -c Release -p x64 -Test
`````
**Linux / macOS:**
`````bash
./build.sh -t Rebuild -c Release -p x64 -Test
`````

*(Note: You can swap `-p x64` to `-p arm64` when compiling on Apple Silicon or ARM64 Linux/Windows environments).*


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
