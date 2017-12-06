@if not defined _echo @echo off
setlocal EnableDelayedExpansion EnableExtensions

set "__MsgPrefix=BUILD: "
set "__RepoRootDir=%~dp0"

if defined VisualStudioVersion (
    if not defined __VSVersion echo %__MsgPrefix%Detected Visual Studio %VisualStudioVersion% developer command ^prompt environment
    goto :Run
)

echo %__MsgPrefix%Searching ^for Visual Studio 2017 or 2015 installation
set _VSWHERE="%ProgramFiles(x86)%\Microsoft Visual Studio\Installer\vswhere.exe"
if exist %_VSWHERE% (
    for /f "usebackq tokens=*" %%i in (`%_VSWHERE% -latest -prerelease -property installationPath`) do set _VSCOMNTOOLS=%%i\Common7\Tools
)
if not exist "%_VSCOMNTOOLS%" set _VSCOMNTOOLS=%VS140COMNTOOLS%
if not exist "%_VSCOMNTOOLS%" (
    echo %__MsgPrefix%Error: Visual Studio 2015 or 2017 required.
    echo        Please see https://github.com/dotnet/corefx/blob/master/Documentation/project-docs/developer-guide.md for build instructions.
    exit /b 1
)

set VSCMD_START_DIR="%~dp0"
call "%_VSCOMNTOOLS%\VsDevCmd.bat"

:Run

if defined VS150COMNTOOLS (
  set "__VSToolsRoot=%VS150COMNTOOLS%"
  set "__VCToolsRoot=%VS150COMNTOOLS%\..\..\VC\Auxiliary\Build"
  set __VSVersion=vs2017
)

:parse
if /i "%1"=="" goto endparse

if /i "%1"=="-help" goto usage
if /i "%1"=="-h" goto usage
if /i "%1"=="-?" goto usage

if /i "%~1"=="-Configuration"       (set "_MSB_Configuration=%2" & shift & shift & goto :parse)
if /i "%~1"=="-c"                   (set "_MSB_Configuration=%2" & shift & shift & goto :parse)
if /i "%~1"=="-Platform"            (set "_MSB_Platform=%2" & shift & shift & goto :parse)
if /i "%~1"=="-p"                   (set "_MSB_Platform=%2" & shift & shift & goto :parse)
if /i "%~1"=="-Target"              (set "_MSB_Target=%2" & shift & shift & goto :parse)
if /i "%~1"=="-t"                   (set "_MSB_Target=%2" & shift & shift & goto :parse)
if /i "%~1"=="-Test"                (set "_Test=1" & shift & goto :parse)
if /i "%~1"=="-Framework"           (set "_Framework=%2" & shift & shift & goto :parse)
if /i "%~1"=="-f"                   (set "_Framework=%2" & shift & shift & goto :parse)
if /i "%~1"=="-SkipNative"          (set "_SkipNative=1" & shift & goto :parse)
if /i "%~1"=="-sn"                  (set "_SkipNative=1" & shift & goto :parse)

goto :usage
:endparse

if not defined _MSB_Target set _MSB_Target=Build
if not defined _MSB_Configuration set _MSB_Configuration=Debug
if not defined _MSB_Platform set _MSB_Platform=x86
if not defined _Framework set _Framework=net471

set __RootBuildDir=%__RepoRootDir%build\

echo %__MsgPrefix%Starting Build of DjvuNet at %DATE% %TIME%
echo %__MsgPrefix%Build Target:   %_MSB_Target%
echo %__MsgPrefix%Configuration:  %_MSB_Configuration%
echo %__MsgPrefix%Platform:       %_MSB_Platform%
echo %__MsgPrefix%Framework:      %_Framework%

if defined _Test (
    echo %__MsgPrefix%Run Tests:      True
) else (
    echo %__MsgPrefix%Run Tests:      False
)

if not exist .\DjvuNet.sln (
     echo %__MsgPrefix%Error: Missing DjvuNet files in repository directory %cd%
     goto exit_error
)

:: if defined _SkipNative goto :no_djvulibre

if not exist .\DjVuLibre\win32\djvulibre\libdjvulibre\libdjvulibre.vcxproj (
    echo %__MsgPrefix%Cloning DjVuLibre
    call git clone https://github.com/DjvuNet/DjVuLibre.git
    if not %ERRORLEVEL%==0 (
        echo %__MsgPrefix%Error: git clone https://github.com/DjvuNet/DjVuLibre.git returned error
        goto exit_error
    )
) else (
    echo %__MsgPrefix%DjvuLibre already cloned
)

::no_djvulibre

if /i "%_Framework%" == "netcoreapp" (
    set __RestoreCmd=dotnet
    set __RestoreCmdArgs=-v n
    set __BuildCommand=dotnet msbuild
    set __Framework=netcoreapp2.0
    set __BuildLibDjvuLibre=1
)
if /i "%_Framework%" == "netstandard" (
    set __RestoreCmd=dotnet
    set __RestoreCmdArgs=-v n
    set __BuildCommand=dotnet msbuild
    set __Framework=netstandard2.0
    set __BuildLibDjvuLibre=1
)
if /i "%_Framework%" == "net471" (
    set __RestoreCmd=%cd%\Tools\nuget.exe
    set __RestoreCmdArgs=-verbosity detailed
    set __BuildCommand=msbuild
    set __Framework=net471
)

set __DjvuTargetSolution=DjvuNet.sln
set __OutputDir=%__RootBuildDir%%_MSB_Platform%\%_MSB_Configuration%\%__Framework%\

echo %__MsgPrefix%Restoring nuget packages
echo %__MsgPrefix%Calling: %__RestoreCmd% restore %__DjvuTargetSolution% %__RestoreCmdArgs%
call %__RestoreCmd% restore %__DjvuTargetSolution% %__RestoreCmdArgs%

if not [%ERRORLEVEL%]==[0] (
    echo %__MsgPrefix%Error: nuget restore DjvuNet.sln returned error
    goto exit_error
) else (
    echo %__MsgPrefix%Success: nuget restore finished
)

if defined _SkipNative goto :no_djvulibre_build

set __LogsDir=%__RootBuildDir%logs
set __BuildLogRootName=libdjvulibre
set __BuildLog="%__LogsDir%\%__BuildLogRootName%_%_MSB_Platform%_%_MSB_Configuration%.log"
set __BuildWrn="%__LogsDir%\%__BuildLogRootName%_%_MSB_Platform%_%_MSB_Configuration%.wrn"
set __BuildErr="%__LogsDir%\%__BuildLogRootName%_%_MSB_Platform%_%_MSB_Configuration%.err"
set "__MsbuildLog=/flp:Verbosity=diag;LogFile=%__BuildLog%"
set "__MsbuildWrn=/flp1:WarningsOnly;LogFile=%__BuildWrn%
set "__MsbuildErr=/flp2:ErrorsOnly;LogFile=%__BuildErr%

if defined __BuildLibDjvuLibre (
    REM Scope environment changes start {
    setlocal

    echo %__MsgPrefix%Building native libdjvulibre.vcxproj

    set __VCBuildArch=x86_x64

    if /i "%_MSB_Platform%" == "x86" set __VCBuildArch=x86
    if /i "%_MSB_Platform%" == "arm" set __VCBuildArch=x86_arm
    if /i "%_MSB_Platform%" == "arm64" set __VCBuildArch=x86_arm64

    echo %__MsgPrefix%Using environment: "%__VCToolsRoot%\vcvarsall.bat" !__VCBuildArch!
    call                                 "%__VCToolsRoot%\vcvarsall.bat" !__VCBuildArch!

    echo %__MsgPrefix%Calling: msbuild /p:Configuration=%_MSB_Configuration% /p:Platform=%_MSB_Platform% /p:TargetFramework=%__Framework% /t:%_MSB_Target% /v:m /m:1 /nologo %__MsbuildLog% %__MsbuildWrn% %__MsbuildErr% "%__RepoRootDir%DjVuLibre\win32\djvulibre\libdjvulibre\libdjvulibre.vcxproj"
    call msbuild /p:Configuration=%_MSB_Configuration% /p:Platform=%_MSB_Platform% /p:TargetFramework=%__Framework% /t:%_MSB_Target% /v:m /m:1 /nologo %__MsbuildLog% %__MsbuildWrn% %__MsbuildErr% "%__RepoRootDir%DjVuLibre\win32\djvulibre\libdjvulibre\libdjvulibre.vcxproj"

    if not [%ERRORLEVEL%]==[0] (
        echo %__MsgPrefix%Error: native libdjvulibre library build failed. Refer to the build log files for details:
        echo     !__BuildLog!
        echo     !__BuildWrn!
        echo     !__BuildErr!
        exit /b 1
    )

REM } Scope environment changes end
    endlocal
)

:no_djvulibre_build

set __BuildLogRootName=%__DjvuTargetSolution%
set __BuildLog="%__LogsDir%\%__BuildLogRootName%_%_MSB_Platform%_%_MSB_Configuration%.log"
set __BuildWrn="%__LogsDir%\%__BuildLogRootName%_%_MSB_Platform%_%_MSB_Configuration%.wrn"
set __BuildErr="%__LogsDir%\%__BuildLogRootName%_%_MSB_Platform%_%_MSB_Configuration%.err"
set __MsbuildLog=/flp:Verbosity=diag;LogFile=%__BuildLog%
set __MsbuildWrn=/flp1:WarningsOnly;LogFile=%__BuildWrn%
set __MsbuildErr=/flp2:ErrorsOnly;LogFile=%__BuildErr%

set __DjvuNetGitTasksProj=build/tools/DjvuNet.Git.Tasks/DjvuNet.Git.Tasks.csproj
set __DjvuNetProj=DjvuNet/DjvuNet.csproj
set __DjvuNetDjvuLibreProj=DjvuNet.DjvuLibre/DjvuNet.DjvuLibre.csproj

echo.
echo %__MsgPrefix%Building %__DjvuNetGitTasksProj%
echo %__MsgPrefix%calling %__BuildCommand% /p:Configuration=%_MSB_Configuration% /p:Platform=%_MSB_Platform% /p:TargetFramework=%__Framework% /t:%_MSB_Target% /v:m /m:1 /nologo %__MsbuildLog% %__MsbuildWrn% %__MsbuildErr% "%__RepoRootDir%%__DjvuNetGitTasksProj%"
call %__BuildCommand% /p:Configuration=Release /p:Platform=AnyCPU /p:TargetFramework=%__Framework% /t:%_MSB_Target% /v:m /m:1 /nologo %__MsbuildLog% %__MsbuildWrn% %__MsbuildErr% "%__RepoRootDir%%__DjvuNetGitTasksProj%"

if not [%ERRORLEVEL%]==[0] (
    echo %__MsgPrefix%Error: %__DjvuNetGitTasksProj% build failed. Refer to the build log files for details:
    echo     !__BuildLog!
    echo     !__BuildWrn!
    echo     !__BuildErr!
    exit /b 1
)

echo.
echo %__MsgPrefix%Building %__DjvuNetProj%
echo %__MsgPrefix%calling %__BuildCommand% /p:Configuration=%_MSB_Configuration% /p:Platform=%_MSB_Platform% /p:TargetFramework=%__Framework% /t:%_MSB_Target% /v:m /m:1 /nologo %__MsbuildLog% %__MsbuildWrn% %__MsbuildErr% "%__RepoRootDir%%__DjvuNetProj%"
call %__BuildCommand% /p:Configuration=%_MSB_Configuration% /p:Platform=%_MSB_Platform% /p:TargetFramework=%__Framework% /t:%_MSB_Target% /v:m /m:1 /nologo %__MsbuildLog% %__MsbuildWrn% %__MsbuildErr% "%__RepoRootDir%%__DjvuNetProj%"

if not [%ERRORLEVEL%]==[0] (
    echo %__MsgPrefix%Error: %__DjvuNetProj% build failed. Refer to the build log files for details:
    echo     !__BuildLog!
    echo     !__BuildWrn!
    echo     !__BuildErr!
    exit /b 1
)

if defined _SkipNative goto :no_djvunet_djvulibre_build

echo.
echo %__MsgPrefix%Building %__DjvuNetDjvuLibreProj%
echo %__MsgPrefix%calling %__BuildCommand% /p:Configuration=%_MSB_Configuration% /p:Platform=%_MSB_Platform% /p:TargetFramework=%__Framework% /t:%_MSB_Target% /v:m /m:1 /nologo %__MsbuildLog% %__MsbuildWrn% %__MsbuildErr% "%__RepoRootDir%%__DjvuNetDjvuLibreProj%"
call %__BuildCommand% /p:Configuration=%_MSB_Configuration% /p:Platform=%_MSB_Platform% /p:TargetFramework=%__Framework% /t:%_MSB_Target% /v:m /m:1 /nologo %__MsbuildLog% %__MsbuildWrn% %__MsbuildErr% "%__RepoRootDir%%__DjvuNetDjvuLibreProj%"

if not [%ERRORLEVEL%]==[0] (
    echo %__MsgPrefix%Error: %__DjvuNetProj% build failed. Refer to the build log files for details:
    echo     !__BuildLog!
    echo     !__BuildWrn!
    echo     !__BuildErr!
    exit /b 1
)

:no_djvunet_djvulibre_build

if not defined _Test (
    goto exit_success
) else if not exist .\artifacts\test001C.djvu (
    echo.
    echo %__MsgPrefix%Cloning test data from https://github.com/DjvuNet/artifacts.git
    call git clone --depth 1 https://github.com/DjvuNet/artifacts.git
    if not [%ERRORLEVEL%]==[0] (
        echo.
        echo %__MsgPrefix%Error: git clone returned error
        goto exit_error
    )
)

if /i "%_Framework%" == "netstandard" (
    set __TestFramework=netcoreapp2.0
) else (
    set __TestFramework=%__Framework%
)

set __TestOutputDir=%__RootBuildDir%%_MSB_Platform%\%_MSB_Configuration%\%__TestFramework%\

set __DjvuNetTestsProj=DjvuNet.Tests/DjvuNet.Tests.csproj

echo.
echo %__MsgPrefix%Building %__DjvuNetTestsProj%
echo %__MsgPrefix%calling %__BuildCommand% /p:Configuration=%_MSB_Configuration% /p:Platform=%_MSB_Platform% /p:TargetFramework=%__TestFramework% /t:%_MSB_Target% /v:m /m:1 /nologo %__MsbuildLog% %__MsbuildWrn% %__MsbuildErr% "%__RepoRootDir%%__DjvuNetTestsProj%"
call %__BuildCommand% /p:Configuration=%_MSB_Configuration% /p:Platform=%_MSB_Platform% /p:TargetFramework=%__TestFramework% /t:%_MSB_Target% /v:m /m:1 /nologo %__MsbuildLog% %__MsbuildWrn% %__MsbuildErr% "%__RepoRootDir%%__DjvuNetTestsProj%"

if not [%ERRORLEVEL%]==[0] (
    echo %__MsgPrefix%Error: %__DjvuNetTestsProj% build failed. Refer to the build log files for details:
    echo     !__BuildLog!
    echo     !__BuildWrn!
    echo     !__BuildErr!
    exit /b 1
)

set __DjvuNetWaveletTestsProj=DjvuNet.Wavelet.Tests/DjvuNet.Wavelet.Tests.csproj

echo.
echo %__MsgPrefix%Building %__DjvuNetWaveletTestsProj%
echo %__MsgPrefix%calling %__BuildCommand% /p:Configuration=%_MSB_Configuration% /p:Platform=%_MSB_Platform% /p:TargetFramework=%__TestFramework% /t:%_MSB_Target% /v:m /m:1 /nologo %__MsbuildLog% %__MsbuildWrn% %__MsbuildErr% "%__RepoRootDir%%__DjvuNetWaveletTestsProj%"
call %__BuildCommand% /p:Configuration=%_MSB_Configuration% /p:Platform=%_MSB_Platform% /p:TargetFramework=%__TestFramework% /t:%_MSB_Target% /v:m /m:1 /nologo %__MsbuildLog% %__MsbuildWrn% %__MsbuildErr% "%__RepoRootDir%%__DjvuNetWaveletTestsProj%"

if not [%ERRORLEVEL%]==[0] (
    echo %__MsgPrefix%Error: %__DjvuNetWaveletTestsProj% build failed. Refer to the build log files for details:
    echo     !__BuildLog!
    echo     !__BuildWrn!
    echo     !__BuildErr!
    exit /b 1
)

if defined _SkipNative goto :no_djvunet_djvulibre_tests

set __LogsDir=%__RootBuildDir%logs
set __BuildLogRootName=libdjvulibre
set __BuildLog="%__LogsDir%\%__BuildLogRootName%_%_MSB_Platform%_%_MSB_Configuration%_Tests.log"
set __BuildWrn="%__LogsDir%\%__BuildLogRootName%_%_MSB_Platform%_%_MSB_Configuration%_Tests.wrn"
set __BuildErr="%__LogsDir%\%__BuildLogRootName%_%_MSB_Platform%_%_MSB_Configuration%_Tests.err"
set "__MsbuildLog=/flp:Verbosity=diag;LogFile=%__BuildLog%"
set "__MsbuildWrn=/flp1:WarningsOnly;LogFile=%__BuildWrn%
set "__MsbuildErr=/flp2:ErrorsOnly;LogFile=%__BuildErr%

if defined __BuildLibDjvuLibre (
    REM Scope environment changes start {
    setlocal

    echo %__MsgPrefix%Building native libdjvulibre.vcxproj for DjvuNet.DjvuLibre.Tests

    set __VCBuildArch=x86_x64

    if /i "%_MSB_Platform%" == "x86" set __VCBuildArch=x86
    if /i "%_MSB_Platform%" == "arm" set __VCBuildArch=x86_arm
    if /i "%_MSB_Platform%" == "arm64" set __VCBuildArch=x86_arm64

    echo %__MsgPrefix%Using environment: "%__VCToolsRoot%\vcvarsall.bat" !__VCBuildArch!
    call                                 "%__VCToolsRoot%\vcvarsall.bat" !__VCBuildArch!

    echo %__MsgPrefix%Calling: msbuild /p:Configuration=%_MSB_Configuration% /p:Platform=%_MSB_Platform% /p:TargetFramework=%__TestFramework% /t:%_MSB_Target% /v:m /m:1 /nologo %__MsbuildLog% %__MsbuildWrn% %__MsbuildErr% "%__RepoRootDir%DjVuLibre\win32\djvulibre\libdjvulibre\libdjvulibre.vcxproj"
    call msbuild /p:Configuration=%_MSB_Configuration% /p:Platform=%_MSB_Platform% /p:TargetFramework=%__TestFramework% /t:%_MSB_Target% /v:m /m:1 /nologo %__MsbuildLog% %__MsbuildWrn% %__MsbuildErr% "%__RepoRootDir%DjVuLibre\win32\djvulibre\libdjvulibre\libdjvulibre.vcxproj"

    if not [%ERRORLEVEL%]==[0] (
        echo %__MsgPrefix%Error: native libdjvulibre library build failed. Refer to the build log files for details:
        echo     !__BuildLog!
        echo     !__BuildWrn!
        echo     !__BuildErr!
        exit /b 1
    )

REM } Scope environment changes end
    endlocal
)

set __BuildLogRootName=%__DjvuTargetSolution%
set __BuildLog="%__LogsDir%\%__BuildLogRootName%_%_MSB_Platform%_%_MSB_Configuration%.log"
set __BuildWrn="%__LogsDir%\%__BuildLogRootName%_%_MSB_Platform%_%_MSB_Configuration%.wrn"
set __BuildErr="%__LogsDir%\%__BuildLogRootName%_%_MSB_Platform%_%_MSB_Configuration%.err"
set __MsbuildLog=/flp:Verbosity=diag;LogFile=%__BuildLog%
set __MsbuildWrn=/flp1:WarningsOnly;LogFile=%__BuildWrn%
set __MsbuildErr=/flp2:ErrorsOnly;LogFile=%__BuildErr%

set __DjvuNetDjvuLibreTestsProj=DjvuNet.DjvuLibre.Tests/DjvuNet.DjvuLibre.Tests.csproj

echo.
echo %__MsgPrefix%Building %__DjvuNetDjvuLibreTestsProj%
echo %__MsgPrefix%calling %__BuildCommand% /p:Configuration=%_MSB_Configuration% /p:Platform=%_MSB_Platform% /p:TargetFramework=%__TestFramework% /t:%_MSB_Target% /v:m /m:1 /nologo %__MsbuildLog% %__MsbuildWrn% %__MsbuildErr% "%__RepoRootDir%%__DjvuNetDjvuLibreTestsProj%"
call %__BuildCommand% /p:Configuration=%_MSB_Configuration% /p:Platform=%_MSB_Platform% /p:TargetFramework=%__TestFramework% /t:%_MSB_Target% /v:m /m:1 /nologo %__MsbuildLog% %__MsbuildWrn% %__MsbuildErr% "%__RepoRootDir%%__DjvuNetDjvuLibreTestsProj%"

if not [%ERRORLEVEL%]==[0] (
    echo %__MsgPrefix%Error: %__DjvuNetDjvuLibreTestsProj% build failed. Refer to the build log files for details:
    echo     !__BuildLog!
    echo     !__BuildWrn!
    echo     !__BuildErr!
    exit /b 1
)

:no_djvunet_djvulibre_tests

set _DjvuNet_Tests=%__TestOutputDir%DjvuNet.Tests.dll
set _DjvuNet_DjvuLibre_Tests=%__TestOutputDir%DjvuNet.DjvuLibre.Tests.dll
set _DjvuNet_Wavelet_Tests=%__TestOutputDir%DjvuNet.Wavelet.Tests.dll
set __TestResOutputDir=TestResults\%__Framework%\
set __DotNetCommandx86="%ProgramFiles(x86)%\dotnet\dotnet" %UserProfile%\.nuget\packages\xunit.runner.console\2.3.1\tools\netcoreapp2.0\xunit.console.dll
set __DotNetCommandx64="%ProgramFiles%\dotnet\dotnet" %UserProfile%\.nuget\packages\xunit.runner.console\2.3.1\tools\netcoreapp2.0\xunit.console.dll

if /i "%__TestFramework%" == "net471" (
    if %_MSB_Platform%==x86 set _xUnit_console=%UserProfile%\.nuget\packages\xunit.runner.console\2.3.1\tools\net452\xunit.console.x86.exe
    if %_MSB_Platform%==AnyCPU set _xUnit_console=%UserProfile%\.nuget\packages\xunit.runner.console\2.3.1\tools\net452\xunit.console.x86.exe
    if %_MSB_Platform%==x64 set _xUnit_console=%UserProfile%\.nuget\packages\xunit.runner.console\2.3.1\tools\net452\xunit.console.exe
    set __TestOutputFormat=html
)

if /i "%__TestFramework%" == "netcoreapp2.0" (
    if %_MSB_Platform%==x86 set "_xUnit_console=!__DotNetCommandx86!"
    if %_MSB_Platform%==x64 set "_xUnit_console=!__DotNetCommandx64!"
    if %_MSB_Platform%==AnyCPU set _xUnit_console=dotnet %UserProfile%\.nuget\packages\xunit.runner.console\2.3.1\tools\netcoreapp2.0\xunit.console.dll
    set _Test_Options=-notrait "Category=SkipNetCoreApp"
    set __TestOutputFormat=xml
)

if /i "%__TestFramework%" == "netstandard2.0" (
    if %_MSB_Platform%==x86 set "_xUnit_console=!__DotNetCommandx86!"
    if %_MSB_Platform%==x64 set "_xUnit_console=!__DotNetCommandx64!"
    if %_MSB_Platform%==AnyCPU set _xUnit_console=dotnet %UserProfile%\.nuget\packages\xunit.runner.console\2.3.1\tools\netcoreapp2.0\xunit.console.dll
    set _Test_Options=-notrait "Category=SkipNetCoreApp"
    set __TestOutputFormat=xml
)

set _Test_Options=%_Test_Options% -notrait "Category=Skip" -nologo -nocolor -noshadow -maxthreads 1 -%__TestOutputFormat%

:xUnit_tests
echo.
echo %__MsgPrefix%Running tests from DjvuNet.Tests assembly
echo %__MsgPrefix%calling: "!_xUnit_console! %_DjvuNet_Tests% %_Test_Options% %__TestResOutputDir%DjvuNet.Tests.%__TestOutputFormat%"
echo.
call !_xUnit_console! %_DjvuNet_Tests% %_Test_Options% %__TestResOutputDir%DjvuNet.Tests.%__TestOutputFormat%

if not [%ERRORLEVEL%]==[0] set _DjvuNet_Tests_Error=true

if defined _SkipNative goto :no_djvulibre_tests

echo.
echo %__MsgPrefix%Running tests from DjvuNet.DjvuLibre.Tests assembly
echo %__MsgPrefix%calling: %_xUnit_console% %_DjvuNet_DjvuLibre_Tests% %_Test_Options% %__TestResOutputDir%DjvuNet.DjvuLibre.Tests.%__TestOutputFormat%
echo.
call %_xUnit_console% %_DjvuNet_DjvuLibre_Tests% %_Test_Options% %__TestResOutputDir%DjvuNet.DjvuLibre.Tests.%__TestOutputFormat%

if not [%ERRORLEVEL%]==[0] set _DjvuNet_DjvuLibre_Tests_Error=true

:no_djvulibre_tests

echo.
echo %__MsgPrefix%Running tests from DjvuNet.Wavelet.Tests assembly
echo %__MsgPrefix%calling: %_xUnit_console% %_DjvuNet_Wavelet_Tests% %_Test_Options% %__TestResOutputDir%DjvuNet.Wavelet.Tests.%__TestOutputFormat%
echo.
call %_xUnit_console% %_DjvuNet_Wavelet_Tests% %_Test_Options% %__TestResOutputDir%DjvuNet.Wavelet.Tests.%__TestOutputFormat%

if not [%ERRORLEVEL%]==[0] goto test_error
if /i "%_DjvuNet_Tests_Error%" == "true" goto test_error
if /i "%_DjvuNet_DjvuLibre_Tests_Error%" == "true" goto test_error
goto test_success

:test_error
echo.
echo %__MsgPrefix%Error: tests failed
goto exit_error

:test_success
echo.
echo %__MsgPrefix%Success: tests passed
goto exit_success


:exit_success
echo,
echo %__MsgPrefix%Finished Build at %DATE% %TIME%
exit /b 0

:exit_error
echo.
echo %__MsgPrefix%Build Failed at %DATE% %TIME%
exit /b 1

:usage
echo.
echo  Build script of DjvuNet repo
echo  Usage: build [option value]
echo.
echo  Options:
echo.
echo     -Framework           defines framework target, default "netfx",
echo     -f                   allowed values [ netfx ^| netcoreapp ^| netstandard ]
echo.
echo     -Configuration       defines build configuration, default "Debug",
echo     -c                   allowed values [ Release ^| Debug ]
echo.
echo     -Platform            defines build target platform, default "x64",
echo     -p                   allowed values [ x64 ^| x86 ^| arm ^| arm64 ]
echo.
echo     -SkipNative          do not clone, build libdjvulibre, skip libdjvulibre dependent tests,
echo     -sn
echo.
echo     -Target              defines build script target, default "Rebuild",
echo     -t                   allowed values [ Build ^| Rebuild ^| Clean ]
echo.
echo     -Test                build and run tests, when not used testing is skipped
echo.
exit /b 1
