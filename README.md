DjvuNet
=======

## ![Image](https://ci.appveyor.com/api/projects/status/github/djvunet/djvunet?svg=true)

DjvuNet is a fully managed library written in C# for reading and analyzing Djvu documents. Library supports Djvu format specification version 3 up to the minor version 26 (v3.26). 
The so called "Secure DjVu" format is not supported as it's specification was never published. Project was started several years ago
by [Telavian](https://github.com/Telavian) and after remaining inactive for some time currently is continued at new 
[GitHub DjvuNet](https://github.com/DjvuNet) location. Current status can be described as a fast code review with 
refactoring / rearchitecting and creation of test framework harness what should enable fixing of several bugs found 
in original source. *Code is not production ready.* There are known bugs but it should work on large number of djvu 
files but they are still only a subset of all DjVu files which can be found out in the wild. 
Therefore, use it at your own risk and do not blame us for any of your problems.

## Current Developments

Project undergoes several architectural changes, which are done in "parser_archit" branch. Refactoring which should 
be rather called rearchitecturing comprises IFF file parser, DjVu specific file format parsing, image data processing, 
and some hot project code paths. Test framework which is systematically developed covers project in top down
way. First test have been developed to verify top level functions of the library (i.e. document parsing, decompression, 
image decoding, image generation) and only later as code review and refactoring goes lower unit tests are added 
for lower level classes and functions. Some notable exceptions are the hottest code paths. This should significantly improve code quality
in short and long term, as original project essentially did not have any test harness.

## DjVu Format Support Validation

Full library format handling validation is planned by using reference library implementation of DjVu format and supporting tools namely
[DjVuLibre](http://djvu.sourceforge.net/). .NET Bindings for majority of C API are available in DjvuNet.DjvuLibre project. It builds for
x86 and x64 targets only. Perhaps AnyCPU target will be available via NuGet packaging or alternatively via embedding of native binaries
in managed assembly.

DjVuLibre was modified by creating libdjvulibre build integration with DjvuNet and modifying library by expanding some C APIs through 
addition of memory management functions exports, implementation of Json formatted output from some dump functions and tools (djvudump),
and addition of functions bypassing s-expressions formatting used in text retrieval. Detailed description will be available in changelog
of DjVuLibre soon. Modified library is available here: [DjVuLibre for DjvuNet](https://github.com/DjvuNet/DjVuLibre).

Due to more restrictive licensing conditions of DjVuLibre .NET bindings project DjvuNet.DjvuLibre is double licensed under MIT and GPL v2 or later licenses.

DjvuNet is developed as part of larger effort to create scientific information analysis and understanding framework. First step in data analysis
is of course to get the data and the second one is to read data in format which can be understood by computers. 

## License

DjvuNet is licensed under [MIT license](https://opensource.org/licenses/mit-license.php).

DjvuNet.DjvuLibre is double licensed under [MIT license](https://opensource.org/licenses/mit-license.php) and [GPL v2](https://opensource.org/licenses/GPL-2.0) or later.

DjVuLibre used for format support validation is licensed under [GPL v2](https://opensource.org/licenses/GPL-2.0) or later.

## Building

#### Windows

Building from command line on Windows (tested on Windows 10 with Visual Studio 2015 installed).

Open Visual Studio devloper command prompt and clone repository
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
AnyCPU, x86, x64 (example /p:Platform=x64, default value AnyCPU)
`````
Available targets:
`````
Clean, Build, Rebuild   (example /t:Clean, default value Build)
`````  

To build with Visual Studio open DjvuNet.sln file located in root directory of DjvuNet cloned 
repository and build DjvuNet.csproj or entire solution. 

Tests can be run by building and running tests from DjvuNet.Tests.dll assembly under Visual Studio 
or xUnit test runner from command line or from Visual Studio using Test Explorer.

Performance tests can be run with help of DjvuNetTest project.

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
