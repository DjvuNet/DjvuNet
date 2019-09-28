@if not defined _echo @echo off
setlocal EnableDelayedExpansion EnableExtensions

set "__MsgPrefix=BUILD: "
set "__RepoRootDir=%~dp0"

REM Short circuit to help
if /i "%1"=="-help" goto usage
if /i "%1"=="-h" goto usage
if /i "%1"=="-?" goto usage

if defined VisualStudioVersion (
    if not defined __VSVersion echo %__MsgPrefix%Detected Visual Studio %VisualStudioVersion% developer command ^prompt environment
    goto :Run
)

echo %__MsgPrefix%Searching ^for Visual Studio 2017 installation
set _VSWHERE="%ProgramFiles(x86)%\Microsoft Visual Studio\Installer\vswhere.exe"
if exist %_VSWHERE% (
    for /f "usebackq tokens=*" %%i in (`%_VSWHERE% -latest -prerelease -property installationPath`) do set _VSCOMNTOOLS=%%i\Common7\Tools
)
if not exist "%_VSCOMNTOOLS%" set _VSCOMNTOOLS=%VS160COMNTOOLS%
if not exist "%_VSCOMNTOOLS%" (
    echo %__MsgPrefix%Error: Visual Studio 2019 required.
    echo        Please see https://github.com/DjvuNet/DjvuNet for build instructions.
    exit /b 1
)

set VSCMD_START_DIR="%~dp0"
call "%_VSCOMNTOOLS%\VsDevCmd.bat"

:Run

if defined VS160COMNTOOLS (
  set "__VSToolsRoot=%VS160COMNTOOLS%"
  set "__VCToolsRoot=%VS160COMNTOOLS%\..\..\VC\Auxiliary\Build"
  set __VSVersion=vs2017
)

REM Set default values
set _MSB_Target=Build
set _MSB_Configuration=Debug
set _MSB_Platform=x64
set _Verbosity=normal
set _Processors=%NUMBER_OF_PROCESSORS%
set _OS=Windows_NT
set _SkipNative=
set _Test=
set _DefaultNetCoreApp=netcoreapp3.0
set _NetCoreAppId=.NETCoreApp
set _NetCoreAppTFM=.NETCoreApp,Version=v3.0
set _DefaultNetStandard=netstandard2.1
set _NetStandardId=.NETStandard
set _NetStandardTFM=.NETStandard,Version=v2.1
set _DefaultNetFX=net472
set _NetFXId=.NETFramework
set _NetFXTFM=.NETFramework,Version=v4.7.2
set _Framework=%_DefaultNetCoreApp%
set __GithubDjvuNetReleaseUri=https://github.com/DjvuNet/artifacts/releases/download/v0.7.0.12/

REM Parse command line

:parse
if /i "%1"=="" goto endparse

if /i "%~1"=="-Configuration"       (set "_MSB_Configuration=%2"&shift&shift&goto :parse)
if /i "%~1"=="-c"                   (set "_MSB_Configuration=%2"&shift&shift&goto :parse)
if /i "%~1"=="-Platform"            (set "_MSB_Platform=%2"&shift&shift&goto :parse)
if /i "%~1"=="-p"                   (set "_MSB_Platform=%2"&shift&shift&goto :parse)
if /i "%~1"=="-Target"              (set "_MSB_Target=%2"&shift&shift&goto :parse)
if /i "%~1"=="-t"                   (set "_MSB_Target=%2"&shift&shift&goto :parse)
if /i "%~1"=="-Test"                (set "_Test=1"&shift&goto :parse)
if /i "%~1"=="-Framework"           (set "_Framework=%2"&shift&shift&goto :parse)
if /i "%~1"=="-f"                   (set "_Framework=%2"&shift&shift&goto :parse)
if /i "%~1"=="-SkipNative"          (set "_SkipNative=1"&shift&goto :parse)
if /i "%~1"=="-sn"                  (set "_SkipNative=1"&shift&goto :parse)
if /i "%~1"=="-Verbosity"           (set "_Verbosity=%2"&shift&shift&goto :parse)
if /i "%~1"=="-v"                   (set "_Verbosity=%2"&shift&shift&goto :parse)
if /i "%~1"=="-Processors"          (set "_Processors=%2"&shift&shift&goto :parse)
if /i "%~1"=="-proc"                (set "_Processors=%2"&shift&shift&goto :parse)
if /i "%~1"=="-OS"                  (set "_OS=%2"&shift&shift&goto :parse)

echo Unknown command line parameter: %1
goto :usage
:endparse

if /i [%_MSB_Platform%] == [Arm]         (set "__ManagedPlatform=AnyCPU" & if /i [%PROCESSOR_ARCHITECTURE%] == [AMD64] (set "__SkipNativeTests=1")&goto :check_params)
if /i [%_MSB_Platform%] == [Arm64]       (set "__ManagedPlatform=AnyCPU" & if /i [%PROCESSOR_ARCHITECTURE%] == [AMD64] (set "__SkipNativeTests=1")&goto :check_params)
if /i [%_MSB_Platform%] == [AnyCPU]      (set "_MSB_Platform=x64"&set "__ManagedPlatform=AnyCPU"&goto :check_params)
if /i [%_MSB_Platform%] == [x64]         (set "_MSB_Platform=x64"&set "__ManagedPlatform=x64"&goto :check_params)

if /i [%__ManagedPlatform%] == [] set __ManagedPlatform=%_MSB_Platform%

if /i [%_MSB_Target%] == [Clean] set __SkipPublish=1

:check_params

REM Check params values

REM Accepted Framework values
if /i [%_Framework%] == [netcoreapp] (set "_Framework=%_DefaultNetCoreApp%"&set __TargetFrameworkMoniker=%_NetCoreAppTFM%&goto :end_check_framework)
if /i [%_Framework%] == [%_DefaultNetCoreApp%] (set __TargetFrameworkMoniker=%_NetCoreAppTFM%&goto :end_check_framework)
if /i [%_Framework%] == [netstandard] (set "_Framework=%_DefaultNetStandard%"&set __TargetFrameworkMoniker=%_NetStandardTFM%&goto :end_check_framework)
if /i [%_Framework%] == [%_DefaultNetStandard%] (set TargetFrameworkIdentifier=.NETStandard&set __TargetFrameworkMoniker=%_NetStandardTFM%&goto :end_check_framework)
if /i [%_Framework%] == [netfx] (set "_Framework=%_DefaultNetFX%"&set __TargetFrameworkMoniker=%_NetFXTFM%&goto :end_check_framework)
if /i [%_Framework%] == [%_DefaultNetFX%] (set __TargetFrameworkMoniker=%_NetFXTFM%&goto :end_check_framework)

echo Invalid command line parameter -f/-Framework: %_Framework%
goto usage

:end_check_framework

REM Accepted Platform values
if /i [%_MSB_Platform%] == [x64] goto :end_check_platform
if /i [%_MSB_Platform%] == [x86] goto :end_check_platform
if /i [%_MSB_Platform%] == [arm] goto :end_check_platform
if /i [%_MSB_Platform%] == [arm64] goto :end_check_platform

echo Invalid command line parameter value -p/-Platform: %_MSB_Platform%
goto usage

:end_check_platform

REM Accepted Configuration values
if /i [%_MSB_Configuration%] == [Release] goto :end_check_configuration
if /i [%_MSB_Configuration%] == [Debug] goto :end_check_configuration
if /i [%_MSB_Configuration%] == [Checked] goto :end_check_configuration

echo Invalid command line parameter value -c/-Configuration: %_MSB_Configuration%
goto usage

:end_check_configuration

REM Accepted Target values
if /i [%_MSB_Target%] == [Build] goto :end_check_target
if /i [%_MSB_Target%] == [Clean] goto :end_check_target
if /i [%_MSB_Target%] == [Rebuild] goto :end_check_target

echo Invalid command line parameter value -t/-Target: %_MSB_Target%
goto usage

:end_check_target

REM Accepted Verbosity values
:: [ q[uiet], m[inimal], n[ormal], d[etailed], and diag[nostic] ]
if /i [%_Verbosity%] == [q] goto :end_check_verbosity
if /i [%_Verbosity%] == [quiet] (set _Verbosity=q&goto :end_check_verbosity)
if /i [%_Verbosity%] == [minimal] (set _Verbosity=m&goto :end_check_verbosity)
if /i [%_Verbosity%] == [m] goto :end_check_verbosity
if /i [%_Verbosity%] == [normal] (set _Verbosity=n&goto :end_check_verbosity)
if /i [%_Verbosity%] == [n] goto :end_check_verbosity
if /i [%_Verbosity%] == [detailed] (set _Verbosity=d&goto :end_check_verbosity)
if /i [%_Verbosity%] == [d] goto :end_check_verbosity
if /i [%_Verbosity%] == [diagnostic] (set _Verbosity=diag&goto :end_check_verbosity)
if /i [%_Verbosity%] == [diag] goto :end_check_verbosity

echo Invalid command line parameter value -v/-Verbosity: %_Verbosity%
goto usage

:end_check_verbosity

REM Accepted OS values
if /i [%_OS%] == [Windows_NT] goto :end_check_os
if /i [%_OS%] == [Linux] goto :end_check_os
if /i [%_OS%] == [OSX] goto :end_check_os

echo Invalid command line parameter value -OS: %_OS%
goto usage

:end_check_os

:end_check_params

set __RootBuildDir=%__RepoRootDir%build\bin\

echo %__MsgPrefix%Starting Build of DjvuNet at %DATE% %TIME%
echo %__MsgPrefix%Build Target:          %_MSB_Target%
echo %__MsgPrefix%Configuration:         %_MSB_Configuration%
echo %__MsgPrefix%Native Platform:       %_MSB_Platform%
echo %__MsgPrefix%Managed Platform:      %__ManagedPlatform%
echo %__MsgPrefix%Framework:             %_Framework%

if defined _Test (
    echo %__MsgPrefix%Run Tests:             True
) else (
    echo %__MsgPrefix%Run Tests:             False
)

set TargetFramework=%_Framework%


if not exist .\DjvuNet.sln (
     echo %__MsgPrefix%Error: Missing DjvuNet.sln file in repository root directory %cd%
     goto exit_error
)

REM Download ready to use DjvuNet build tools

set __BuildToolsUri=!__GithubDjvuNetReleaseUri!Tools.zip

call powershell -NoProfile -ExecutionPolicy ByPass -NoLogo -NonInteractive -File DjvuNet.Build\Get-Tools.ps1 %__BuildToolsUri% Tools.zip Tools BuildTools {314FA7B0-6864-4842-B539-5728CBC73F27} %__MsgPrefix%
if not [%ERRORLEVEL%]==[0] (
    echo %__MsgPrefix%Error: Failed to download build tools from %__BuildToolsUri%
    goto exit_error
)

REM Download native build and test deps

set __NativeDepsUri=!__GithubDjvuNetReleaseUri!deps.zip

call powershell -NoProfile -ExecutionPolicy ByPass -NoLogo -NonInteractive -File DjvuNet.Build/Get-Tools.ps1 %__NativeDepsUri% deps.zip deps NativeDependencies {87E5AD66-912F-477C-BDA5-52F7785AE705} %__MsgPrefix%
if not [%ERRORLEVEL%]==[0] (
    echo %__MsgPrefix%Error: Failed to download native dependencies from %__NativeDepsUri%
    goto exit_error
)

REM Download and initialize out own .NETCore SDK

call .\init-tools.cmd %_MSB_Platform%

if not [%ERRORLEVEL%]==[0] (
    goto exit_error
)

REM Clone libdjvulibre if needed

if defined _SkipNative goto :no_djvulibre

if not exist .\DjVuLibre\win32\djvulibre\libdjvulibre\libdjvulibre.vcxproj (
    echo %__MsgPrefix%Cloning DjVuLibre
    call git clone https://github.com/DjvuNet/DjVuLibre.git
    if not [%ERRORLEVEL%]==[0] (
        echo %__MsgPrefix%Error: git clone https://github.com/DjvuNet/DjVuLibre.git returned error
        goto exit_error
    )
) else (
    echo %__MsgPrefix%DjvuLibre already cloned
)

:no_djvulibre

REM Set target specific environment values

if /i "%_Framework%" == "%_DefaultNetCoreApp%" (
    set DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1
    set DOTNET_MULTILEVEL_LOOKUP=0
    set __RestoreCmd=Tools\coreclr\dotnetcli\dotnet.exe msbuild /t:Restore
    set __BuildCommand=Tools\coreclr\dotnetcli\dotnet.exe msbuild
    set __Framework=%_DefaultNetCoreApp%
    set __BuildLibDjvuLibre=0
    if /i [%_OS%] == [Windows_NT] set "__RuntimeIdentifier=win-"
    if /i [%_OS%] == [Linux] set "__RuntimeIdentifier=linux-"
    if /i [%_OS%] == [OSX] set "__RuntimeIdentifier=osx-"
    set "__RuntimeIdentifier=!__RuntimeIdentifier!!_MSB_Platform!"
)
if /i "%_Framework%" == "%_DefaultNetStandard%" (
    set DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1
    set DOTNET_MULTILEVEL_LOOKUP=0
    set __RestoreCmd=Tools\coreclr\dotnetcli\dotnet.exe msbuild /t:Restore
    set __BuildCommand=Tools\coreclr\dotnetcli\dotnet.exe msbuild
    set __Framework=%_DefaultNetStandard%
    set __BuildLibDjvuLibre=0
)
if /i "%_Framework%" == "%_DefaultNetFX%" (
    set __RestoreCmd=msbuild /t:Restore
    set __BuildCommand=msbuild
    set __Framework=%_DefaultNetFX%
    if /i [%__ManagedPlatform%] neq [AnyCPU] set __BuildLibDjvuLibre=1
    if /i [%__ManagedPlatform%] == [AnyCPU] set __SkipNativeTests=1
)

set __SystemAttrProj=System.Attributes/System.Attributes.csproj
set __DjvuNetGitTasksProj=build/tools/DjvuNet.Git.Tasks/DjvuNet.Git.Tasks.csproj
set __DjvuNetProj=DjvuNet/DjvuNet.csproj
set __DjvuNetDjvuLibreProj=DjvuNet.DjvuLibre/DjvuNet.DjvuLibre.csproj

set __OutputDir=!__RootBuildDir!!OS!.!__ManagedPlatform!.!_MSB_Configuration!/binaries/!__Framework!/
if /i "%_Framework%" neq "%_DefaultNetFX%" set __PublishDir=!__OutputDir!!__RuntimeIdentifier!/publish/
if /i "%_Framework%" == "%_DefaultNetFX%" set __PublishDir=!__OutputDir!publish/

echo %__MsgPrefix%__OutputDir [!__OutputDir!]
echo %__MsgPrefix%__PublishDir [!__PublishDir!]

if /i "%_MSB_Target%" == "Clean" goto :end_dotnet_restore

if /i "%_Framework%" == "%_DefaultNetFX%" (
    set "__BuildCommandArgs=-p:Configuration=!_MSB_Configuration! -p:Platform=!__ManagedPlatform! -p:TargetFramework=!__Framework! -p:PublishDir=!__PublishDir! -v:!_Verbosity! -m:!_Processors! -nologo -nr:false"
    set __RestoreCmdArgs=!__BuildCommandArgs!
    set __DjvuTargetSolution=DjvuNet.sln

    set __OutputDir=%__RootBuildDir%%OS%.%__ManagedPlatform%.%_MSB_Configuration%\binaries\%__Framework%\

    echo %__MsgPrefix%Restoring nuget packages
    echo %__MsgPrefix%Calling: !__RestoreCmd! !__RestoreCmdArgs! !__DjvuTargetSolution!
    call !__RestoreCmd! !__RestoreCmdArgs! !__DjvuTargetSolution!

    if not [%ERRORLEVEL%]==[0] (
        echo %__MsgPrefix%Error: nuget restore of !__DjvuTargetSolution! returned error
        goto exit_error
    ) else (
        echo %__MsgPrefix%Success: nuget restore of !__DjvuTargetSolution! finished
    )
    goto end_dotnet_restore
)

if /i "%_Framework%" == "%_DefaultNetStandard%" goto :dotnet_restore
if /i "%_Framework%" == "%_DefaultNetCoreApp%" goto :dotnet_restore

goto :end_dotnet_restore
:dotnet_restore

set "__BuildCommandArgs=-p:Configuration=!_MSB_Configuration! -p:Platform=!__ManagedPlatform! -p:TargetFramework=!__Framework! -p:RuntimeIdentifier=!__RuntimeIdentifier! -p:PublishDir=!__PublishDir! -v:!_Verbosity! -m:!_Processors! -nologo -nr:false"
set "__RestoreCmdArgs=!__BuildCommandArgs!"

call :restore_dotnet_proj !__SystemAttrProj!
call :restore_dotnet_proj !__DjvuNetGitTasksProj!
call :restore_dotnet_proj !__DjvuNetProj!

if defined _SkipNative goto :end_dotnet_restore
call :restore_dotnet_proj !__DjvuNetDjvuLibreProj!

:end_dotnet_restore

if defined _SkipNative goto :no_djvulibre_build

if defined __BuildLibDjvuLibre (
    REM Scope environment changes start {
    setlocal

    set __NativeLogsDir=!__RootBuildDir!!OS!.!_MSB_Platform!.!_MSB_Configuration!\logs\native\
    set __BuildLogRootName=libdjvulibre
    set __BuildLog="!__NativeLogsDir!!__BuildLogRootName!.log"
    set __BuildWrn="!__NativeLogsDir!!__BuildLogRootName!.wrn"
    set __BuildErr="!__NativeLogsDir!!__BuildLogRootName!.err"
    set "__MsbuildLog=-flp:Verbosity=diag;LogFile=!__BuildLog!"
    set "__MsbuildWrn=/flp1:WarningsOnly;LogFile=!__BuildWrn!"
    set "__MsbuildErr=/flp2:ErrorsOnly;LogFile=!__BuildErr!"
    set "__MsbuildLogging=!__MsbuildLog! !__MsbuildPubWrn! !__MsbuildErr!"

    echo %__MsgPrefix%Building native libdjvulibre.vcxproj

    set __VCBuildArch=x86_x64

    if /i "%_MSB_Platform%" == "x86" set __VCBuildArch=x86
    if /i "%_MSB_Platform%" == "arm" set __VCBuildArch=x86_arm
    if /i "%_MSB_Platform%" == "arm64" set __VCBuildArch=x86_arm64

    echo %__MsgPrefix%Using environment: "%__VCToolsRoot%\vcvarsall.bat" !__VCBuildArch!
    call                                 "%__VCToolsRoot%\vcvarsall.bat" !__VCBuildArch!

    echo %__MsgPrefix%Calling: msbuild /p:Configuration=%_MSB_Configuration% /p:Platform=%_MSB_Platform% /p:TargetFramework=%__Framework% /t:%_MSB_Target% /v:%_Verbosity% /m:%_Processors% /nologo !__MsbuildLogging! "%__RepoRootDir%DjVuLibre\win32\djvulibre\libdjvulibre\libdjvulibre.vcxproj"
    call msbuild /p:Configuration=%_MSB_Configuration% /p:Platform=%_MSB_Platform% /p:TargetFramework=%__Framework% /t:%_MSB_Target% /v:%_Verbosity% /m:%_Processors% /nologo /nr:false !__MsbuildLogging! "%__RepoRootDir%DjVuLibre\win32\djvulibre\libdjvulibre\libdjvulibre.vcxproj"

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

set __LogsDir=!__RootBuildDir!!OS!.!_MSB_Platform!.!_MSB_Configuration!\logs\

call :build_dotnet_proj !__SystemAttrProj! System.Attributes.csproj
call :build_dotnet_proj !__DjvuNetGitTasksProj! DjvuNet.Git.Tasks.csproj
call :build_dotnet_proj !__DjvuNetProj! DjvuNet.csproj

if defined _SkipNative goto skip_djvulibre_build
call :build_dotnet_proj !__DjvuNetDjvuLibreProj! DjvuNet.DjvuLibre.csproj

:skip_djvulibre_build

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

REM Setup test environment

if /i "%_Framework%" == "%_DefaultNetStandard%" (
    set __TestFramework=%_DefaultNetCoreApp%
)

if /i "%_Framework%" == "%_DefaultNetCoreApp%" (
    set __TestFramework=%_DefaultNetCoreApp%
)

if /i "%_Framework%" == "%_DefaultNetFX%" (
    set __TestFramework=%_DefaultNetFX%
)

set __TestOutputDir=%__OutputDir%
set __DjvuNetTestsProj=DjvuNet.Tests/DjvuNet.Tests.csproj
set __DjvuNetWaveletTestsProj=DjvuNet.Wavelet.Tests/DjvuNet.Wavelet.Tests.csproj
set __DjvuNetTestExeProj=DjvuNetTest/DjvuNetTest.csproj
set __DjvuNetDjvuLibreTestsProj=DjvuNet.DjvuLibre.Tests/DjvuNet.DjvuLibre.Tests.csproj

REM Restore test projects - only netcoreapp and netstandard targets

if /i "%_Framework%" == "%_DefaultNetFX%" goto :skip_djvunet_tests_restore

call :restore_dotnet_proj !__DjvuNetTestsProj!
call :restore_dotnet_proj !__DjvuNetWaveletTestsProj!
call :restore_dotnet_proj !__DjvuNetTestExeProj!

if defined _SkipNative goto :skip_djvunet_tests_restore

:skip_djvunet_tests_restore

REM Build and publish tests

call :build_dotnet_proj !__DjvuNetTestsProj! DjvuNet.Tests.csproj
call :build_dotnet_proj !__DjvuNetWaveletTestsProj! DjvuNet.Wavelet.Tests.csproj
call :build_dotnet_proj !__DjvuNetTestExeProj! DjvuNetTest.exe.csproj

if defined _SkipNative goto skip_djvulibre_tests_proj
call :build_dotnet_proj !__DjvuNetDjvuLibreTestsProj! DjvuNet.DjvuLibre.Tests.csproj
:skip_djvulibre_tests_proj

REM Prepare for running tests

set _DjvuNet_Tests=%__TestOutputDir%DjvuNet.Tests.dll
set _DjvuNet_DjvuLibre_Tests=%__TestOutputDir%DjvuNet.DjvuLibre.Tests.dll
set _DjvuNet_Wavelet_Tests=%__TestOutputDir%DjvuNet.Wavelet.Tests.dll
set __TestResOutputDir=TestResults\!__Framework!\
set __DotNetCommandx86=%ProgramFiles(x86)%\dotnet\dotnet
set __DotNetCommandx64=!__RepoRootDir!Tools\coreclr\dotnetcli\dotnet

if /i "%__TestFramework%" == "%_DefaultNetFX%" (
    if [%__ManagedPlatform%] == [x86] set _xUnit_console=!UserProfile!\.nuget\packages\xunit.runner.console\2.4.0\tools\!__TestFramework!\xunit.console.x86.exe
    if [%__ManagedPlatform%] == [AnyCPU] set _xUnit_console="%UserProfile%\.nuget\packages\xunit.runner.console\2.4.0\tools\%__TestFramework%\xunit.console.exe"
    if [%__ManagedPlatform%] == [x64] set _xUnit_console=!UserProfile!\.nuget\packages\xunit.runner.console\2.4.0\tools\!__TestFramework!\xunit.console.exe
    set __TestOutputFormat=html
)

if /i "%__TestFramework%" == "%_DefaultNetCoreApp%" (
    if [%__ManagedPlatform%] == [x86] set _xUnit_console="!__DotNetCommandx86!" "!__PublishDir!xunit.console.dll"
    if [%__ManagedPlatform%] == [x64] set _xUnit_console="!__DotNetCommandx64!" "!__PublishDir!xunit.console.dll"
    if [%__ManagedPlatform%] == [AnyCPU] set _xUnit_console="!__DotNetCommandx64!" "!__PublishDir!xunit.console.dll"
    set _Test_Options=-notrait "Category=SkipNetCoreApp"
    set __TestOutputFormat=xml
)

if /i "%__TestFramework%" == "%_DefaultNetStandard%" (
    if [%__ManagedPlatform%] == [x86] set _xUnit_console="!__DotNetCommandx86!" "!__PublishDir!xunit.console.dll"
    if [%__ManagedPlatform%] == [x64] set _xUnit_console="!__DotNetCommandx64!" "!__PublishDir!xunit.console.dll"
    if [%__ManagedPlatform%] == [AnyCPU] set _xUnit_console="!__DotNetCommandx64!" "!__PublishDir!xunit.console.dll"
    set _Test_Options=-notrait "Category=SkipNetCoreApp"
    set __TestOutputFormat=xml
)

if /i [%_Verbosity%] == [d] set _Test_Options=!_Test_Options! -verbose
if /i [%_Verbosity%] == [diag] set _Test_Options=!_Test_Options! -verbose -internaldiagnostics
set _Test_Options=!_Test_Options! -notrait "Category=Skip" -nologo -nocolor -!__TestOutputFormat!

set "__XunitConfig=xunit.runner.json"

REM Run tests

:xUnit_tests
echo.
echo %__MsgPrefix%Running tests from DjvuNet.Tests assembly
echo %__MsgPrefix%calling: !_xUnit_console! "!_DjvuNet_Tests!" "!__PublishDir!DjvuNet.Tests.!__XunitConfig!" !_Test_Options! "!__TestResOutputDir!DjvuNet.Tests.!__TestOutputFormat!"
echo.
call !_xUnit_console! "!_DjvuNet_Tests!" "!__PublishDir!DjvuNet.Tests.!__XunitConfig!" !_Test_Options! "!__TestResOutputDir!DjvuNet.Tests.!__TestOutputFormat!

if not [%ERRORLEVEL%]==[0] set _DjvuNet_Tests_Error=true

if defined _SkipNative goto :no_djvulibre_tests
if defined __SkipNativeTests goto :no_djvulibre_tests

echo.
echo %__MsgPrefix%Running tests from DjvuNet.DjvuLibre.Tests assembly
echo %__MsgPrefix%calling: !_xUnit_console! "!_DjvuNet_DjvuLibre_Tests!" "!__PublishDir!DjvuNet.DjvuLibre.Tests.!__XunitConfig!" !_Test_Options! "!__TestResOutputDir!DjvuNet.DjvuLibre.Tests.!__TestOutputFormat!"
echo.
call !_xUnit_console! "!_DjvuNet_DjvuLibre_Tests!" "!__PublishDir!DjvuNet.DjvuLibre.Tests.!__XunitConfig!" !_Test_Options! "!__TestResOutputDir!DjvuNet.DjvuLibre.Tests.!__TestOutputFormat!"

if not [%ERRORLEVEL%]==[0] set _DjvuNet_DjvuLibre_Tests_Error=true

:no_djvulibre_tests

echo.
echo %__MsgPrefix%Running tests from DjvuNet.Wavelet.Tests assembly
echo %__MsgPrefix%calling: !_xUnit_console! "!_DjvuNet_Wavelet_Tests!" "!__PublishDir!DjvuNet.Wavelet.Tests.!__XunitConfig!" -serialize !_Test_Options! "!__TestResOutputDir!DjvuNet.Wavelet.Tests.!__TestOutputFormat!"
echo.
call !_xUnit_console! "!_DjvuNet_Wavelet_Tests!" "!__PublishDir!DjvuNet.Wavelet.Tests.!__XunitConfig!" -serialize !_Test_Options! "!__TestResOutputDir!DjvuNet.Wavelet.Tests.!__TestOutputFormat!"

if not [%ERRORLEVEL%]==[0] goto test_error
if /i "%_DjvuNet_Tests_Error%" == "true" goto test_error
if /i "%_DjvuNet_DjvuLibre_Tests_Error%" == "true" goto test_error
goto test_success

REM Utility functions

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

:restore_dotnet_proj

set __DjvuTargetProject=%1

echo %__MsgPrefix%Restoring %__DjvuTargetProject%
echo %__MsgPrefix%Calling: !__RestoreCmd! !__RestoreCmdArgs! !__DjvuTargetProject!
call !__RestoreCmd! !__RestoreCmdArgs! !__DjvuTargetProject!

if not [%ERRORLEVEL%]==[0] (
    echo %__MsgPrefix%Error: nuget restore of %__DjvuTargetProject% returned error
    goto exit_error
) else (
    echo %__MsgPrefix%Success: nuget restore of %__DjvuTargetProject% finished
)

goto :eof

:build_dotnet_proj

set __BuildProj=%1
set __BuildProjName=%2

set "__BuildLogRootName=!__BuildProjName!.!_MSB_Target!"
set __BuildLog="!__LogsDir!!__BuildLogRootName!.log"
set __BuildWrn="!__LogsDir!!__BuildLogRootName!.wrn"
set __BuildErr="!__LogsDir!!__BuildLogRootName!.err"
set __MsbuildLog=/flp:Verbosity=diag;LogFile=!__BuildLog!
set __MsbuildWrn=/flp1:WarningsOnly;LogFile=!__BuildWrn!
set __MsbuildErr=/flp2:ErrorsOnly;LogFile=!__BuildErr!
set "__MsbuildLogging=!__MsbuildLog! !__MsbuildPubWrn! !__MsbuildErr!"

echo.
echo %__MsgPrefix%Building %__BuildProj%
echo %__MsgPrefix%calling !__BuildCommand! !__BuildCommandArgs! -t:%_MSB_Target% !__MsbuildLogging! "!__RepoRootDir!!__BuildProj!"
call !__BuildCommand! !__BuildCommandArgs! -t:%_MSB_Target% !__MsbuildLogging! "!__RepoRootDir!!__BuildProj!"

if not [%ERRORLEVEL%]==[0] (
    echo %__MsgPrefix%Error: %__BuildProj% build failed. Refer to the build log files ^for details:
    echo     !__BuildLog!
    echo     !__BuildWrn!
    echo     !__BuildErr!
    exit /b 1
)

if not defined __SkipPublish (
    REM Scope environment changes start {
    setlocal
    set "__PublishLogRootName=!__BuildProjName!.Publish"
    set __PublishLog="!__LogsDir!!__PublishLogRootName!.log"
    set __PublishWrn="!__LogsDir!!__PublishLogRootName!.wrn"
    set __PublishErr="!__LogsDir!!__PublishLogRootName!.err"
    set __MsbuildPubLog=-flp:Verbosity=diag;LogFile=!__PublishLog!
    set __MsbuildPubWrn=-flp1:WarningsOnly;LogFile=!__PublishWrn!
    set __MsbuildPubErr=-flp2:ErrorsOnly;LogFile=!__PublishErr!
    set "__MsbuildLogging=!__MsbuildPubLog! !__MsbuildPubWrn! !__MsbuildPubErr!"

    echo.
    echo %__MsgPrefix%Publishing %__BuildProj%
    echo %__MsgPrefix%calling !__BuildCommand! !__BuildCommandArgs! -t:Publish !__MsbuildLogging! "!__RepoRootDir!!__BuildProj!"
    call !__BuildCommand! !__BuildCommandArgs! -t:Publish !__MsbuildLogging! "!__RepoRootDir!!__BuildProj!"

    if not [%ERRORLEVEL%]==[0] (
        echo %__MsgPrefix%Error: %__BuildProj% publish failed. Refer to the publish log files ^for details:
        echo     !__PublishLog!
        echo     !__PublishWrn!
        echo     !__PublishErr!
        exit /b 1
    )
    endlocal
    REM } Scope environment changes end
)

goto :eof

:usage
echo.
echo  Build script of DjvuNet repo
echo  Usage: build [option value]
echo.
echo  Options:
echo.
echo     -Framework           defines framework target, default "%_DefaultNetCoreApp%",
echo     -f                   allowed values [ %_DefaultNetFX% ^| netfx ^| %_DefaultNetCoreApp% ^| netcoreapp ^| %_DefaultNetStandard% ^| netstandard ]
echo.
echo     -Configuration       defines build configuration, default "Debug",
echo     -c                   allowed values [ Release ^| Debug ]
echo.
echo     -Platform            defines build target platform, default "x64",
echo     -p                   allowed values [ x64 ^| x86 ^| arm ^| arm64 ^| AnyCPU ]
echo.
echo     -OS                  defines target OS, allowed values [ Windows_NT ^| Linux ^| OSX ]
echo.
echo     -SkipNative          do not clone, build libdjvulibre, skip libdjvulibre dependent tests,
echo     -sn
echo.
echo     -Target              defines build script target, default "Rebuild",
echo     -t                   allowed values [ Build ^| Rebuild ^| Clean ]
echo.
echo     -Verbosity           defines output verbosity, default "normal",
echo     -v                   allowed values [ q[uiet], m[inimal], n[ormal], d[etailed], and diag[nostic] ]
echo.
echo     -Processors          defines number of processes which should be used ^for build parallelization,
echo     -proc                default on this machine "%NUMBER_OF_PROCESSORS%"
echo.
echo     -Test                build and run tests, when not used testing is skipped
echo.
exit /b 1
