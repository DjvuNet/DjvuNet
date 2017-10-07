DjvuNet
=======

### Windows
## ![Image](https://ci.appveyor.com/api/projects/status/github/djvunet/djvunet?svg=true)

### Linux
## [![Build Status](https://travis-ci.org/DjvuNet/DjvuNet.svg?branch=dev)](https://travis-ci.org/DjvuNet/DjvuNet)

### macOS
## [![Build Status](https://travis-ci.org/DjvuNet/DjvuNet.svg?branch=dev)](https://travis-ci.org/DjvuNet/DjvuNet)

DjvuNet is an open source library designed to process and create documents encoded with DjVu format. Library is written in C# for .NET platform with 
no external dependencies except for .NET Core target which temporarily up to .NET Core v2.1.0 requires CoreCompat.System.Drawing package. 
Library supports Djvu format specification version 3 up to the minor version 26 (v3.26). 
The so called "Secure DjVu" format is not supported as this specification was never published. Project was started several years ago
by [Telavian](https://github.com/Telavian) and after remaining inactive for some time currently is continued at new 
[GitHub DjvuNet](https://github.com/DjvuNet) repo location. *Code is not production ready.* There are known bugs but it should work on large number of djvu 
files but obviously they are still only a subset of all DjVu files which can be found out in the wild. 
Therefore, use it at your own risk and do not blame us for any of your problems.

## Current Status

*DjvuNet library is not ready for production use*. There are several bugs which need to be fixed and missing features which need to be implemented first 
before library could be treated as production ready or fully functional. Furthermore, there are some bugs in image decoder that leave some of images skewed making them useless.

Library supports full .NET Framework 4.6.1 or newer on Windows and .NET Core 2.0.0 or newer on Windows, Linux, macOS.

Project undergoes several architectural and implementations changes, which are done in "dev" branch. 

- DjVu file format parser was optimized and refactored what so far resulted in more than 10x speedup.

- Image data decoding and **encoding** with Interpolated Dubuc-Deslauriers-Lemire (DDL) (4, 4) Discrete Wavelet Transform is close 
to be finished but still has couple bugs which need to be fixed.

- There was very limited optimization work done in this area with some 30 - 40% improvements in performance and identification of several next optimization targets.

- ZP arithmetic coder and BZZ encoding/decoding is fully implemented and reached binary compatibility with DjvuLibre. It still awaits final optimizations.

- JB decoding is implemented but not optimized, encoding is not implemented.

- Image segmentation for Mixed Raster Content done in DjvuLibre with ColorPalette histogram calculation will be entirely rewritten as there was significant progress in image segmentation algorithms in last decade. 

- Support for some DjvuLibre masked image formats is not implemented yet.

- Test framework is systematically developed and is composed of unit and functional tests. It covers project in top down way and provides 
around 85% code coverage using 2 586 test cases with implementation target being more than 90% code coverage. 

- Performance tests are based on DjvuNetTest project with some additional micro benchmarks planned for implementation soon. 


## DjVu Format Support Validation

Full library format handling validation is realized by using [DjVuLibre](http://djvu.sourceforge.net/) reference library implementation of DjVu format and supporting tools. 
.NET Bindings for majority of C API are available in DjvuNet.DjvuLibre project. It builds for
x86 and x64 targets only. Perhaps AnyCPU target will be available via NuGet packaging or alternatively via embedding of native binaries
in managed assembly - the issue is still open.

DjVuLibre was modified by creating libdjvulibre build integration with DjvuNet projects and modifying library by expanding some C APIs through 
addition of memory management functions exports, implementation of Json formatted output from some dump functions and tools (djvudump),
and addition of functions bypassing s-expressions formatting used in text retrieval. 

Modified library used for testing DjvuNet implementation of DjVu format is available here: [DjVuLibre for DjvuNet](https://github.com/DjvuNet/DjVuLibre).

Due to more restrictive licensing conditions of DjVuLibre .NET bindings project DjvuNet.DjvuLibre is double licensed under MIT and GPL v2 licenses.

DjvuNet is developed as part of larger effort to create scientific information analysis and understanding framework. 

Steps in data analysis comprise data retrieval, reading of data and data conversion into format which later can be processed further. This project covers ssmall part of the pipline dealing with input of data encoded in DjVu format. 

## License

DjvuNet is licensed under [MIT license](https://opensource.org/licenses/mit-license.php).

DjvuNet.DjvuLibre is double licensed under [MIT license](https://opensource.org/licenses/mit-license.php) and [GPL v2](https://opensource.org/licenses/GPL-2.0) or later.

DjVuLibre used for format support validation is licensed under [GPL v2](https://opensource.org/licenses/GPL-2.0) or later.

## Building

### Windows for .NET Framework 

#### Prerequisites

- Visual Studio 2017 RTM with at least following workloads: .NET desktop development, desktop development with C++, .NET Core cross-platform development
 
- Git

- Internet access for restoring dependencies

#### Building

Building from command line on Windows (tested on Windows 10 with Visual Studio 2017 installed).

Open Visual Studio 2017 developer command prompt and clone repository
`````
git clone https://github.com/DjvuNet/DjvuNet.git
`````
Change directory to your repo
`````
cd djvunet
`````
Here one can run build from command line (command accepts Configuration, Platform and Target parameters and option Test in any order)
`````
build x86 Release Rebuild Test (Supported case sensitive values: Build, Rebuild, Clean, Debug, Release, x86, x64, Test)
`````
or do it step by step as described below:

Clone DjVuLibre from DjvuNet GutHub (this library was modified to integrate it into DjvuNet project)
`````
git clone https://github.com/DjvuNet/DjVuLibre.git
`````
DjVuLibre repo is now located in DjVuLibre directory of your DjvuNet repo.

Build using MSBuild from command line
`````
msbuild /t:Rebuild /p:Configuration=Release /p:Platform:x86 djvunet\djvunet.csproj
`````
Available configurations: 
`````
Debug, Release  (example - /p:Configuration=Debug, default value Debug)
`````
Available platforms:
DjvuNet.DjvuLibre and libdjvulibre are built only for x86 and x64 platforms
`````
x86, x64 (example /p:Platform=x64, default value AnyCPU is temporarily not supported for CI builds)
`````
Available targets:
`````
Clean, Build, Rebuild   (example /t:Clean, default value Build)
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

### Windows for netstandard2.0 or netcore2.0 target

#### Prerequisites

Visual Studio 2017 at least v15.3.3 with following workloads: .NET desktop development, desktop development with C++, .NET Core cross-platform development
VS 2017 versions can be installed side by side and preview version can be safely used side by side with RTM versions. 

- .NET Core 2.0 SDK

- Git

- Internet access for restoring dependencies

#### Building

Building from command line on Windows (tested on Windows 10 with Visual Studio 2017 installed).

Open Visual Studio 2017 developer command prompt and clone repository
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

Build DjvuNet.NETStandard2.0.csproj to get netstandard2.0 binaries or DjvuNet.Core.csproj to get netcoreapp2.0 binaries.
`````
cd DjvuNet.Core
dotnet restore
dotnet build 
`````


#### Testing

Test setup for .Net Core 2.0 target is not streamlined and is a bit involved.

Test data are stored in separate repository [artifacts](https://github.com/DjvuNet/artifacts). 
Clone artifacts repository with git command (run it from DjvuNet repo root directory):
`````
git clone --depth 1 https://github.com/DjvuNet/artifacts.git

cd DjvuNet.Tests.Core
dotnet restore
dotnet xunit 

cd ..\..

cd DjvuNet.Wavelet.Tests.Core
dotnet restore
dotnet xunit
`````
All tests should pass except for skipped.

### Linux for netstandard2.0 / netcore2.0 target

#### Prerequisites

Tested on Ubuntu 14.04 and 16.04.

Install required tools and dependencies:

`````
sudo apt-get update
sudo apt-get install git zip unzip curl libgdiplus
`````

Install dotnet 2.0 SDK (check for latest instructions [here](https://github.com/dotnet/docs/blob/master/docs/core/linux-prerequisites.md#install-net-core-for-ubuntu-1404-ubuntu-1604-ubuntu-1610--linux-mint-17-linux-mint-18-64-bit)):

1. Remove any previous preview versions of .NET Core from your system.
2. Register the Microsoft Product key as trusted.
`````
curl https://packages.microsoft.com/keys/microsoft.asc | gpg --dearmor > microsoft.gpg
sudo mv microsoft.gpg /etc/apt/trusted.gpg.d/microsoft.gpg
`````
3. Set up the desired version host package feed.

Ubuntu 17.04
`````
sudo sh -c 'echo "deb [arch=amd64] https://packages.microsoft.com/repos/microsoft-ubuntu-zesty-prod zesty main" > /etc/apt/sources.list.d/dotnetdev.list'
sudo apt-get update
`````

Ubuntu 16.04 / Linux Mint 18
`````
sudo sh -c 'echo "deb [arch=amd64] https://packages.microsoft.com/repos/microsoft-ubuntu-xenial-prod xenial main" > /etc/apt/sources.list.d/dotnetdev.list'
sudo apt-get update
`````

Ubuntu 14.04 / Linux Mint 17
`````
sudo sh -c 'echo "deb [arch=amd64] https://packages.microsoft.com/repos/microsoft-ubuntu-trusty-prod trusty main" > /etc/apt/sources.list.d/dotnetdev.list'
sudo apt-get update
`````
4. Install .NET Core.
`````
sudo apt-get install dotnet-sdk-2.0.0
`````
5. Run the dotnet --version command to prove the installation succeeded.
`````
dotnet --version
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

Restore dependencies
`````
dotnet restore DjvuNet.Core.sln
`````

Build either netstandard2.0 or netcoreapp2.0 target
`````
# For netstandard2.0 build
cd DjvuNet.NETStandard2.0
dotnet build -c Release

# For netcoreapp2.0 build
cd DjvuNet.Core
dotnet build -c Release
`````

#### Testing

Download required test artifacts into DjvuNet repository root and extract them to artifacts directory:
`````
curl -L -o artifacts.zip -s https://github.com/DjvuNet/artifacts/releases/download/v0.7.0.11/artifacts.zip
unzip -q artifacts.zip -d artifacts
`````

Build and run DjvuNet tests (commands are starting from repo root):
`````
cd DjvuNet.Tests.Core
dotnet build -c Release
dotnet xunit -configuration Release -parallel none -nobuild -notrait Category=SkipNetCoreApp -framework netcoreapp2.0

# Return to repo root
cd ..
    
cd DjvuNet.Wavelet.Tests.Core
dotnet build -c Release
dotnet xunit -configuration Release -parallel none -nobuild -notrait Category=SkipNetCoreApp -framework netcoreapp2.0
`````


### macOS for netstandard2.0 / netcore2.0 target

Supported on macOS 10.12 "Sierra" and later versions

#### Prerequisites

Download and install the .NET Core SDK from [.NET Downloads](https://www.microsoft.com/net/download/core).

#### Building and Testing

Follow Linux instructions for Building and Testing

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
## Known Issues

- Tests for .NET Core cannot be run from Visual Studio

- Tests for .NET Standard targeting libraries have to be compiled as netcoreapp2.0 binaries as xunit does not support netstandard2.0 binaries

- Tests for .NET Core need to be sut up manualy by copying libdjvulibre.dll and pdb to test directory with tested binaries

## Reporting Issues

In case of build, test or DjvuNet library usage problems open new issue in [GitHub DjvuNet repo](https://github.com/DjvuNet/DjvuNet/issues) providing
detailed information on error (logs, command line output, stack trace, minidump) and used system.

We will try to adress all problems quickly unless they depend on missing features or known bugs which will be implemented or fixed according to our roadmap.
