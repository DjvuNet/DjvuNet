DjvuNet
=======

DjvuNet is a fully managed Djvu reader in C# and supports Djvu format specification version 3 up to the minor version 26 (v3.26). 
The so called "Secure DjVu" format is not supported as it's specification was never published. Project was started several years ago
by [Telavian](https://github.com/Telavian) and after remaining inactive for some time currently is continued at new 
[GitHub DjvuNet](https://github.com/DjvuNet) location. Current status can be described as a fast code review with 
refactoring / rearchitecting and creation of test framework harness what should enable fixing of several bugs found 
in original source. *Code is not production ready.* There are known bugs but it should work on large number of djvu 
files but they are still only a subset of all DjVu files which can be found out in the wild. 
Therefore, use it at your own risk and do not blame us for any of your problems.

Project undergoes several architectural changes, which are done in "parser_archit" branch. Refactoring which should 
be rather called rearchitecturing comprises IFF file parser, DjVu specific file format parsing, image data processing, 
and some hot project code paths. Test framework which is systematically developed covers project in top down
way. First test are developed to verify top level functions of the library (i.e. document parsing, decompression, 
image decoding, image generation) and only later as code review and refactoring goes lower unit tests are added 
for lower level classes and functions with some notable exceptions. This should significantly improve code quality
in short and long term, as original project essentially did not have any test harness.

Project is developed as part of larger effort to create scientific data analysis framework which first has to read 
data which will be analyzed. 

License
=======

DjvuNet is licensed under MIT license.

Building
========

**Windows**

Building from command line on Windows (tested on Windows 10 with Visual Studio 2015 installed).

Open devloper command prompt and clone repository
`````
git clone https://github.com/DjvuNet/DjvuNet.git
`````
Change directory 
`````
cd djvunet
`````
Build using MSBuild from command line
`````
msbuild /t:Rebuild /p:Configuration=Release djvunet\djvunet\djvunet.csproj
`````
Available configurations: 
`````
Debug, Release  (/p:Configuration=Debug)
`````
Available targets:
`````
Clean, Build, Rebuild   (/t:Clean)
`````  

To build with Visual Studio open DjvuNet.sln file located in DjvuNet\DjvuNet directory and build DjvuNet.csproj.

**Usage**
`````c#
using DjvuNet;

using(DjvuDocument doc = new DjvuDocument(@"Mcguffey's_Primer.djvu"))
{
    var page = doc.Pages[0];
    using(System.Drawing.Bitmap pageImage = page.BuildPageImage())
    {
        pageImage.Save("TestImage1.png", ImageFormat.Png);
        string pageText = page.Text;
    }
}
`````
