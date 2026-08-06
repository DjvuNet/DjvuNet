@if not defined _echo @echo off
setlocal EnableDelayedExpansion EnableExtensions

set DOTNET_CLI_TELEMETRY_OPTOUT=1
set DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1

set "__MsgPrefix=BUILD: "
set "__RepoRootDir=%~dp0"

REM Abstract submodule directory for case-sensitive filesystems
set "__DjvuLibreDir=djvulibre"

REM Delay resolving vcpkg root
set "__GlobalVcpkgRoot=(Resolving later...)"

REM Short circuit to help
if /i "%1"=="-help" goto usage
if /i "%1"=="-h" goto usage
if /i "%1"=="-?" goto usage

if defined VisualStudioVersion (
    if not defined __VSVersion echo %__MsgPrefix%Detected Visual Studio %VisualStudioVersion% developer command ^prompt environment
    goto :Run
)

echo %__MsgPrefix%Searching ^for Visual Studio 2026 installation
set _VSWHERE="%ProgramFiles(x86)%\Microsoft Visual Studio\Installer\vswhere.exe"
if exist %_VSWHERE% (
    for /f "usebackq tokens=*" %%i in (`%_VSWHERE% -latest -prerelease -property installationPath`) do set _VSCOMNTOOLS=%%i\Common7\Tools
)
if not exist "%_VSCOMNTOOLS%" set _VSCOMNTOOLS=%VS80COMNTOOLS%
if not exist "%_VSCOMNTOOLS%" (
    echo %__MsgPrefix%Error: Visual Studio 2026 required.++
    echo        Please see https://github.com/DjvuNet/DjvuNet for build instructions.
    exit /b 1
)

set VSCMD_START_DIR="%~dp0"
call "%_VSCOMNTOOLS%\VsDevCmd.bat"

:Run

if not defined _VSCOMNTOOLS (
    if defined VSINSTALLDIR (
        set "_VSCOMNTOOLS=!VSINSTALLDIR!Common7\Tools"
    ) else (
        set "_VSWHERE=!ProgramFiles(x86)!\Microsoft Visual Studio\Installer\vswhere.exe"
        if exist "!_VSWHERE!" (
            for /f "usebackq tokens=*" %%i in (`"!_VSWHERE!" -latest -prerelease -property installationPath`) do set "_VSCOMNTOOLS=%%i\Common7\Tools"
        )
    )
)

if defined _VSCOMNTOOLS (
  set "__VSToolsRoot=%_VSCOMNTOOLS%"
  set "__VCToolsRoot=%_VSCOMNTOOLS%\..\..\VC\Auxiliary\Build"
  if defined VisualStudioVersion set __VSVersion=vs!VisualStudioVersion!
)

REM Set default values
set "_MSB_Target=Build"
set "_MSB_Configuration=Debug"
set "_MSB_Platform=x64"
set "_Verbosity=normal"
set "_Processors=0"
for /f "tokens=2 delims==" %%A in ('wmic cpu get NumberOfCores /value 2^>nul') do (
    for /f "delims=" %%B in ("%%A") do set /a "_Processors+=%%B"
)
if !_Processors! EQU 0 set "_Processors=%NUMBER_OF_PROCESSORS%"
set "_TargetOS=Windows"
set "_SkipNative="
set "_CloneDeps="
set "_BuildDjvuNet=1"
set "_BuildTools="
set "_BuildTests="
set "_RunTests="
set "_Test="
set "_Pack="
set "_FastFail="
set "__FailedRestores="
set "__FailedBuilds="
set "__FailedPublishes="
set "__FailedTests="
set "__FailedClones="
set "__FailedCommands="
set "__SuccessfulRestores="
set "__SuccessfulBuilds="
set "__SuccessfulPublishes="
set "__SuccessfulTests="
set "__SuccessfulClones="
set "__SuccessfulCommands="
set "_DefaultNetCoreApp=net10.0"
set "_NetCoreAppId=.NETCoreApp"
set "_NetCoreAppTFM=.NETCoreApp,Version=v10.0"
set "_Framework=%_DefaultNetCoreApp%"
set "__ArtifactsReleaseTag=v0.13.26218.0"
set "__GithubDjvuNetReleaseUri=https://github.com/DjvuNet/artifacts/releases/download/%__ArtifactsReleaseTag%/"
set "__ArtifactsTestDataUri=https://github.com/DjvuNet/artifacts/archive/refs/tags/%__ArtifactsReleaseTag%.zip"
set "__ArtifactsDirName=artifacts-%__ArtifactsReleaseTag:v=%"
set "__LibGit2SharpRepoUri=https://github.com/4creators/libgit2sharp"

REM Parse command line

:parse
if /i "%1"=="" goto endparse

if /i "%~1"=="-Configuration"       (set "_MSB_Configuration=%2"&shift&shift&goto :parse)
if /i "%~1"=="-c"                   (set "_MSB_Configuration=%2"&shift&shift&goto :parse)
if /i "%~1"=="-Platform"            (set "_MSB_Platform=%2"&shift&shift&goto :parse)
if /i "%~1"=="-p"                   (set "_MSB_Platform=%2"&shift&shift&goto :parse)
if /i "%~1"=="-Target"              (set "_MSB_Target=%2"&shift&shift&goto :parse)
if /i "%~1"=="-t"                   (set "_MSB_Target=%2"&shift&shift&goto :parse)
if /i "%~1"=="-BuildDjvuNet"        (set "_BuildDjvuNet=1"&shift&goto :parse)
if /i "%~1"=="-DjvuNet"             (set "_BuildDjvuNet=1"&shift&goto :parse)
if /i "%~1"=="-Tools"               (set "_BuildTools=1"&shift&goto :parse)
if /i "%~1"=="-ts"                  (set "_BuildTools=1"&shift&goto :parse)
if /i "%~1"=="-BuildTests"          (set "_BuildTests=1"&shift&goto :parse)
if /i "%~1"=="-bt"                  (set "_BuildTests=1"&shift&goto :parse)
if /i "%~1"=="-RunTests"            (set "_RunTests=1"&shift&goto :parse)
if /i "%~1"=="-rt"                  (set "_RunTests=1"&shift&goto :parse)
if /i "%~1"=="-TestAll"             (set "_TestAll=1"&shift&goto :parse)
if /i "%~1"=="-ta"                  (set "_TestAll=1"&shift&goto :parse)
if /i "%~1"=="-Test"                (set "_Test=1"&shift&goto :parse)
if /i "%~1"=="-FastFail"            (set "_FastFail=1"&shift&goto :parse)
if /i "%~1"=="-ff"                  (set "_FastFail=1"&shift&goto :parse)
if /i "%~1"=="-Framework"           (set "_Framework=%2"&shift&shift&goto :parse)
if /i "%~1"=="-f"                   (set "_Framework=%2"&shift&shift&goto :parse)
if /i "%~1"=="-SkipNative"          (set "_SkipNative=1"&shift&goto :parse)
if /i "%~1"=="-sn"                  (set "_SkipNative=1"&shift&goto :parse)
if /i "%~1"=="-CloneDeps" (
    if /i "%~2"=="full" (set "_CloneDeps=full"&shift&shift&goto :parse) else (set "_CloneDeps=1"&shift&goto :parse)
)
if /i "%~1"=="-cdps" (
    if /i "%~2"=="full" (set "_CloneDeps=full"&shift&shift&goto :parse) else (set "_CloneDeps=1"&shift&goto :parse)
)
if /i "%~1"=="-Verbosity"           (set "_Verbosity=%2"&shift&shift&goto :parse)
if /i "%~1"=="-v"                   (set "_Verbosity=%2"&shift&shift&goto :parse)
if /i "%~1"=="-Processors"          (set "_Processors=%2"&shift&shift&goto :parse)
if /i "%~1"=="-proc"                (set "_Processors=%2"&shift&shift&goto :parse)
if /i "%~1"=="-OS"                  (set "_TargetOS=%2"&shift&shift&goto :parse)

echo Unknown command line parameter: %1
goto :usage
:endparse

if /i [!_MSB_Platform!] == [Arm]         (set "__ManagedPlatform=!_MSB_Platform!" & if /i [!PROCESSOR_ARCHITECTURE!] == [AMD64] (set "__SkipNativeTests=1")&goto :check_params)
if /i [!_MSB_Platform!] == [Arm64]       (set "__ManagedPlatform=!_MSB_Platform!" & if /i [!PROCESSOR_ARCHITECTURE!] == [AMD64] (set "__SkipNativeTests=1")&goto :check_params)
if /i [!_MSB_Platform!] == [AnyCPU]      (set "_MSB_Platform=x64"&set "__ManagedPlatform=AnyCPU"&goto :check_params)
if /i [!_MSB_Platform!] == [x64]         (set "_MSB_Platform=x64"&set "__ManagedPlatform=x64"&goto :check_params)

if /i [%__ManagedPlatform%] == [] set __ManagedPlatform=%_MSB_Platform%

if /i [%_MSB_Target%] == [Clean] set __SkipPublish=1

:check_params

REM Check params values

REM Accepted Framework values
if /i [%_Framework%] == [netcoreapp] (set "_Framework=%_DefaultNetCoreApp%"&set __TargetFrameworkMoniker=%_NetCoreAppTFM%&goto :end_check_framework)
if /i [%_Framework%] == [%_DefaultNetCoreApp%] (set __TargetFrameworkMoniker=%_NetCoreAppTFM%&goto :end_check_framework)

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

echo Invalid command line parameter value -c/-Configuration: %_MSB_Configuration%
goto usage

:end_check_configuration

REM Accepted Target values
if /i [%_MSB_Target%] == [Build] goto :end_check_target
if /i [%_MSB_Target%] == [Clean] goto :end_check_target
if /i [%_MSB_Target%] == [Rebuild] goto :end_check_target
if /i [%_MSB_Target%] == [Pack] goto :end_check_target

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
if /i [!_TargetOS!] == [Windows] goto :end_check_os
if /i [!_TargetOS!] == [Linux] goto :end_check_os
if /i [!_TargetOS!] == [OSX] goto :end_check_os
if /i [!_TargetOS!] == [macOS] (
    set "_TargetOS=OSX"
    goto :end_check_os
)

echo Invalid command line parameter value -OS: !_TargetOS!
goto usage

:end_check_os

:end_check_params

if defined _TestAll (
    if defined _Test (
        echo %__MsgPrefix%WARNING: Both -Test and -TestAll were provided.
        echo %__MsgPrefix%WARNING: -TestAll takes precedence. Forcing unified test execution.
        set "_Test="
    )
    if defined _SkipNative (
        echo %__MsgPrefix%WARNING: Both -TestAll and -SkipNative were provided.
        echo %__MsgPrefix%WARNING: -SkipNative has no effect with -TestAll for now.
    )
    set "_BuildDjvuNet=1"
    set "_BuildTests=1"
    set "_RunTests=1"
    set "_SkipNative="
)

if defined _Test (
    set "_BuildDjvuNet=1"
    set "_BuildTests=1"
    set "_RunTests=1"
)

if defined _BuildTests set _BuildDjvuNet=1

if defined _RunTests (
    if not defined _BuildTests (
        if not defined _Test (
            set "_BuildDjvuNet="
        )
    )
)

set "__RootBuildDir=%__RepoRootDir%build\bin\"

echo %__MsgPrefix%Starting Build of DjvuNet at %DATE% %TIME%
echo %__MsgPrefix%Repository Root:       %__RepoRootDir%
echo %__MsgPrefix%Target OS:             %_TargetOS%
echo %__MsgPrefix%Build Target:          %_MSB_Target%
echo %__MsgPrefix%Configuration:         %_MSB_Configuration%
echo %__MsgPrefix%Native Platform:       %_MSB_Platform%
echo %__MsgPrefix%Managed Platform:      %__ManagedPlatform%
echo %__MsgPrefix%Framework:             %_Framework%
echo %__MsgPrefix%Vcpkg Root:            %__GlobalVcpkgRoot%
echo %__MsgPrefix%VS Common Tools:       %_VSCOMNTOOLS%
echo %__MsgPrefix%VS C++ Tools:          %__VCToolsRoot%

if defined _BuildDjvuNet (
    echo %__MsgPrefix%Build DjvuNet:         True
) else (
    echo %__MsgPrefix%Build DjvuNet:         False
)

if defined _BuildTests (
    echo %__MsgPrefix%Build Tests:           True
) else (
    echo %__MsgPrefix%Build Tests:           False
)

if defined _RunTests (
    echo %__MsgPrefix%Run Tests:             True
) else (
    echo %__MsgPrefix%Run Tests:             False
)

set "TargetFramework=%_Framework%"


if not exist .\DjvuNet.sln (
     echo %__MsgPrefix%Error: Missing DjvuNet.sln file in repository root directory %cd%
     goto exit_error
)

REM Detect preferred PowerShell
set "__PSCmd=powershell"
where pwsh >nul 2>nul
if %ERRORLEVEL% equ 0 set "__PSCmd=pwsh"

REM Download ready to use DjvuNet build tools

set "__BuildToolsUri=!__GithubDjvuNetReleaseUri!Tools.zip"

if not exist ".\Tools\" (
    echo %__MsgPrefix%Downloading DjvuNet.Build.Tools from !__BuildToolsUri!
    call :download_retry "!__BuildToolsUri!" "Tools.zip"
    if not [!ERRORLEVEL!]==[0] (
        echo %__MsgPrefix%Error: Failed to download DjvuNet.Build.Tools from !__BuildToolsUri!
        if defined _FastFail goto exit_error
    ) else (
        mkdir Tools
        tar.exe -xf Tools.zip -C Tools
        del /f /q Tools.zip
    )
) else (
    echo %__MsgPrefix%DjvuNet.Build.Tools already restored
)

REM Download native build and test deps

set "__NativeDepsUri=!__GithubDjvuNetReleaseUri!deps.zip"

if not exist ".\deps\" (
    echo %__MsgPrefix%Downloading custom System.Drawing.Common dependencies from !__NativeDepsUri!
    call :download_retry "!__NativeDepsUri!" "deps.zip"
    if not [!ERRORLEVEL!]==[0] (
        echo %__MsgPrefix%Error: Failed to download custom System.Drawing.Common dependencies from !__NativeDepsUri!
        if defined _FastFail goto exit_error
    ) else (
        mkdir deps
        tar.exe -xf deps.zip -C deps
        del /f /q deps.zip
    )
) else (
    echo %__MsgPrefix%Custom System.Drawing.Common dependencies already restored
)

REM Download and initialize our own .NETCore SDK

set "__GlobalJson=%__RepoRootDir%global.json"
set "__TmpPsScript=%TEMP%\GetLatestSdk_%RANDOM%.ps1"
if not exist "%__RootBuildDir%" mkdir "%__RootBuildDir%"

(
    echo $globalJsonPath = '%__GlobalJson%'
    echo $globalJson = Get-Content $globalJsonPath ^| ConvertFrom-Json
    echo $sdkVersion = $globalJson.sdk.version
    echo $sdkChannel = $sdkVersion.Substring^(0, $sdkVersion.LastIndexOf^('.'^)^)
    echo Write-Output "VERSION:$sdkVersion"
    echo Write-Output "CHANNEL:$sdkChannel"
    echo $latestPatchUrl = "https://dotnetcli.blob.core.windows.net/dotnet/Sdk/$sdkChannel/latest.version"
    echo try {
    echo     $latestAvailable = ^(Invoke-RestMethod -Uri $latestPatchUrl^).Trim^(^)
    echo     Write-Output "LATEST:$latestAvailable"
    echo } catch {
    echo     Write-Output "LATEST:$sdkVersion"
    echo }
) > "%__TmpPsScript%"

for /f "usebackq tokens=1,2 delims=:" %%i in (`%__PSCmd% -NoProfile -ExecutionPolicy ByPass -File "%__TmpPsScript%"`) do (
    if "%%i"=="VERSION" set "__SdkVersion=%%j"
    if "%%i"=="CHANNEL" set "__SdkChannel=%%j"
    if "%%i"=="LATEST" set "__LatestAvailable=%%j"
)
if exist "%__TmpPsScript%" del "%__TmpPsScript%"

echo %__MsgPrefix%Target .NET SDK Channel resolved to !__SdkChannel!

set "__UseSystemDotnetSdk=0"
set "__SystemDotnetVer="
for /f "usebackq tokens=*" %%v in (`dotnet --version 2^>nul`) do set "__SystemDotnetVer=%%v"

if defined __LatestAvailable (
    if "!__SystemDotnetVer!"=="!__LatestAvailable!" set "__UseSystemDotnetSdk=1"
)

if "!__UseSystemDotnetSdk!"=="1" (
    set "__DotNetCmd=dotnet.exe"
    echo %__MsgPrefix%Globally installed System .NET SDK is up-to-date with latest secure patch: !__LatestAvailable!
) else (
    echo %__MsgPrefix%======================================================================
    if defined __SystemDotnetVer (
        echo %__MsgPrefix%WARNING: System .NET SDK ^(!__SystemDotnetVer!^) is OUTDATED.
    ) else (
        echo %__MsgPrefix%WARNING: System .NET SDK is MISSING.
    )
    echo %__MsgPrefix%         The latest secure patch for channel !__SdkChannel! is !__LatestAvailable!.
    echo %__MsgPrefix%         Falling back to isolated local tools to ensure build security.
    echo %__MsgPrefix%
    echo %__MsgPrefix%STATUS:  A secure, isolated .NET SDK [!__LatestAvailable!] is fully provisioned
    echo %__MsgPrefix%         within the repository context ^(Tools\coreclr\dotnetcli^).
    echo %__MsgPrefix%         All compilation and tool execution will map to this local instance
    echo %__MsgPrefix%         to maintain hermetic build guarantees and prevent CI state bleed.
    echo %__MsgPrefix%======================================================================

    REM ARCHITECTURAL NOTE: The official .NET SDK payloads for Windows are strictly compiled
    REM targeting the MSVC Application Binary Interface (ABI) and Universal C Runtime (UCRT).
    REM As discussed in early coreclr/runtime GitHub issues, it is structurally impossible to
    REM build the core Windows .NET runtime with alternative compilers unless they perfectly
    REM emulate MSVC (e.g., clang-cl.exe masquerading as cl.exe).
    REM Because the payload binary contract is immutable, we statically isolate it under 'msvc'.
    set "__OSName=win"
    set "__Libc=msvc"
    set "__ArchName=%PROCESSOR_ARCHITECTURE%"
    if /i "!__ArchName!"=="AMD64" set "__ArchName=x64"
    if /i "!__ArchName!"=="ARM64" set "__ArchName=arm64"

    set "__LocalDotNetDir=!__RepoRootDir!Tools\coreclr\dotnetcli\!__OSName!\!__Libc!\!__ArchName!"

    if defined __LatestAvailable (
        if not "!__LatestAvailable!"=="!__SdkVersion!" (
            echo %__MsgPrefix%Updating repository global.json to track target SDK version !__LatestAvailable! before initialization
            %__PSCmd% -NoProfile -Command "$g = Get-Content '%__GlobalJson%' | ConvertFrom-Json; $g.sdk.version = '!__LatestAvailable!'; $g | ConvertTo-Json -Depth 10 | Set-Content '%__GlobalJson%'"
        )
    )

    call "%__RepoRootDir%init-tools.cmd" %_MSB_Platform% "!__LocalDotNetDir!"

    if not [!ERRORLEVEL!]==[0] (
        goto exit_error
    )
    set "__DotNetCmd=!__LocalDotNetDir!\dotnet.exe"
    set "DOTNET_ROOT=!__LocalDotNetDir!"
    set "PATH=!__LocalDotNetDir!;!PATH!"
)

for /f "usebackq tokens=*" %%v in (`!__DotNetCmd! --version`) do set __UsedDotNetVersion=%%v
if "!__UseSystemDotnetSdk!"=="0" echo %__MsgPrefix%Using Isolated Repository-Local .NET SDK: !__UsedDotNetVersion!

REM Set target specific environment values
if /i "%_Framework%" == "%_DefaultNetCoreApp%" (
    set DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1
    set DOTNET_MULTILEVEL_LOOKUP=0
    set "__RestoreCmd=!__DotNetCmd! msbuild /t:Restore"
    set "__BuildCommand=!__DotNetCmd! msbuild"
    set "__Framework=%_DefaultNetCoreApp%"
    set "__BuildLibDjvuLibre=0"
    if /i [%_TargetOS%] == [Windows] set "__RuntimeIdentifier=win-"
    if /i [%_TargetOS%] == [Linux] set "__RuntimeIdentifier=linux-"
    if /i [%_TargetOS%] == [OSX] set "__RuntimeIdentifier=osx-"
    set "__RuntimeIdentifier=!__RuntimeIdentifier!!_MSB_Platform!"
)

set "__SystemAttrProj=System.Attributes/System.Attributes.csproj"
set "__LibGit2SharpProj=eng/tools/libgit2sharp/LibGit2Sharp/LibGit2Sharp.csproj"
set "__DjvuNetGitTasksProj=eng/tools/DjvuNet.Build.Tasks/DjvuNet.Build.Tasks.csproj"
set "__DjvuNetProj=DjvuNet/DjvuNet.csproj"
set "__DjvuNetDjvuLibreProj=DjvuNet.DjvuLibre/DjvuNet.DjvuLibre.csproj"

if defined _BuildTools (
    if not exist "!__RepoRootDir!!__LibGit2SharpProj!" (
        echo !__MsgPrefix!Setting up libgit2sharp
        set "__Lg2sArchiveUrl=!__LibGit2SharpRepoUri!/archive/refs/tags/!__ArtifactsReleaseTag!.tar.gz"

        set "_ForceCloneLibGit2Sharp="
        if defined _CloneDeps (
            set "_ForceCloneLibGit2Sharp=1"
        ) else (
            echo !__MsgPrefix!Downloading release archive of libgit2sharp for tag !__ArtifactsReleaseTag!
            call :download_retry "!__Lg2sArchiveUrl!" "libgit2sharp.tar.gz"
            if !ERRORLEVEL! EQU 0 (
                echo !__MsgPrefix!Extracting libgit2sharp archive
                if not exist "!__RepoRootDir!eng\tools\libgit2sharp" mkdir "!__RepoRootDir!eng\tools\libgit2sharp"
                tar.exe -xzf libgit2sharp.tar.gz -C "!__RepoRootDir!eng\tools\libgit2sharp" --strip-components=1
                del /f /q libgit2sharp.tar.gz
            ) else (
                set "_ForceCloneLibGit2Sharp=1"
            )
        )

        if defined _ForceCloneLibGit2Sharp (
            echo !__MsgPrefix!Download bypassed or failed, falling back to git clone
            set "__CloneArgs=-c core.autocrlf=false"
            if not "!_CloneDeps!"=="full" set "__CloneArgs=--depth 1 !__CloneArgs!"
            call :git_clone_retry ^
                "!__LibGit2SharpRepoUri!.git" ^
                "eng\tools\libgit2sharp" ^
                "!__CloneArgs!"
        )
    )
)

set "__OutputDir=!__RootBuildDir!!_TargetOS!.!__ManagedPlatform!.!_MSB_Configuration!/binaries/!__Framework!/"
set "__PublishDir=!__OutputDir!!__RuntimeIdentifier!/publish/"
set "__LogsDir=!__RootBuildDir!!_TargetOS!.!__ManagedPlatform!.!_MSB_Configuration!/logs/!__Framework!/"
if not exist "!__LogsDir!" md "!__LogsDir!"

echo %__MsgPrefix%__OutputDir [!__OutputDir!]
echo %__MsgPrefix%__PublishDir [!__PublishDir!]
echo %__MsgPrefix%__LogsDir [!__LogsDir!]

call :get_time __BuildStartTime

if defined _BuildTools (
    REM 1. Standard net10.0 Tools Restore
    set "__BuildCommandArgs=-property:Configuration=!_MSB_Configuration! -property:Platform=!__ManagedPlatform! -property:TargetFramework=!__Framework! -p:RuntimeIdentifier=!__RuntimeIdentifier! -v:!_Verbosity! -m:!_Processors! -nologo -nr:false"
    REM Omit TargetFramework during restore to allow multi-targeted projects to generate unified project.assets.json
    set "__RestoreCmdArgs=-property:Configuration=!_MSB_Configuration! -property:Platform=!__ManagedPlatform! -p:RuntimeIdentifier=!__RuntimeIdentifier! -v:!_Verbosity! -m:!_Processors! -nologo -nr:false"

    call :restore_dotnet_proj !__SystemAttrProj! System.Attributes.csproj
    if defined _FastFail if not !ERRORLEVEL! EQU 0 goto exit_error

    call :restore_dotnet_proj !__LibGit2SharpProj! LibGit2Sharp_!!__Framework!.csproj
    if defined _FastFail if not !ERRORLEVEL! EQU 0 goto exit_error

    call :restore_dotnet_proj !__DjvuNetGitTasksProj! DjvuNet.Build.Tasks_!!__Framework!.csproj
    if defined _FastFail if not !ERRORLEVEL! EQU 0 goto exit_error

    REM 2. Standard net10.0 Tools Build
    call :build_dotnet_proj !__SystemAttrProj! System.Attributes.csproj
    if defined _FastFail if not !ERRORLEVEL! EQU 0 goto exit_error

    call :build_dotnet_proj !__LibGit2SharpProj! LibGit2Sharp_!!__Framework!.csproj
    if defined _FastFail if not !ERRORLEVEL! EQU 0 goto exit_error

    call :build_dotnet_proj !__DjvuNetGitTasksProj! DjvuNet.Build.Tasks_!!__Framework!.csproj
    if defined _FastFail if not !ERRORLEVEL! EQU 0 goto exit_error

    REM 3. Visual Studio net472 Tools Build (Only on Windows)
    if /i "!_TargetOS!" == "Windows" (
        echo %__MsgPrefix%======================================================================
        echo %__MsgPrefix% Building Tools for Visual Studio in-process execution ^(net472^)
        echo %__MsgPrefix%======================================================================

        REM Backup original environment
        set "__OrigFramework=!__Framework!"
        set "__OrigManagedPlatform=!__ManagedPlatform!"
        set "__OrigBuildCommandArgs=!__BuildCommandArgs!"
        set "__OrigRestoreCmdArgs=!__RestoreCmdArgs!"
        set "__OrigOutputDir=!__OutputDir!"
        set "__OrigPublishDir=!__PublishDir!"
        set "__OrigLogsDir=!__LogsDir!"

        REM Set net472 environment
        set "__Framework=net472"
        set "__ManagedPlatform=AnyCPU"

        set "__BuildCommandArgs=-property:Configuration=!_MSB_Configuration! -property:Platform=!__ManagedPlatform! -property:TargetFramework=!__Framework! -v:!_Verbosity! -m:!_Processors! -nologo -nr:false"
        set "__RestoreCmdArgs=!__BuildCommandArgs!"

        REM Recalculate paths exactly matching the tools output architecture
        set "__OutputDir=!__RootBuildDir!!_TargetOS!.!__ManagedPlatform!.!_MSB_Configuration!/tools/binaries/!__Framework!/"
        set "__PublishDir=!__RepoRootDir!Tools/DjvuNet/!__Framework!/"
        set "__LogsDir=!__RootBuildDir!!_TargetOS!.!__ManagedPlatform!.!_MSB_Configuration!/logs/!__Framework!/"
        if not exist "!__LogsDir!" md "!__LogsDir!"

        echo %__MsgPrefix%Switched Framework to: !__Framework!
        echo %__MsgPrefix%Updated OutputDir:   !__OutputDir!
        echo %__MsgPrefix%Updated PublishDir:  !__PublishDir!
        echo %__MsgPrefix%Updated LogsDir:     !__LogsDir!

        REM Restore phase skipped for net472. Multi-targeted project.assets.json was generated during the net10.0 restore phase.

        call :build_dotnet_proj !__SystemAttrProj! System.Attributes.csproj
        if defined _FastFail if not !ERRORLEVEL! EQU 0 goto exit_error

        call :build_dotnet_proj !__LibGit2SharpProj! LibGit2Sharp_!!__Framework!.csproj
        if defined _FastFail if not !ERRORLEVEL! EQU 0 goto exit_error

        call :build_dotnet_proj !__DjvuNetGitTasksProj! DjvuNet.Build.Tasks_!!__Framework!.csproj
        if defined _FastFail if not !ERRORLEVEL! EQU 0 goto exit_error

        echo %__MsgPrefix%Restoring original environment properties...
        set "__Framework=!__OrigFramework!"
        set "__ManagedPlatform=!__OrigManagedPlatform!"
        set "__BuildCommandArgs=!__OrigBuildCommandArgs!"
        set "__RestoreCmdArgs=!__OrigRestoreCmdArgs!"
        set "__OutputDir=!__OrigOutputDir!"
        set "__PublishDir=!__OrigPublishDir!"
        set "__LogsDir=!__OrigLogsDir!"
    )

    REM 4. Package Tools
    if defined __SuccessfulBuilds (
        if defined __SuccessfulPublishes (
            if not "!__SuccessfulBuilds:DjvuNet.Build.Tasks_%_DefaultNetCoreApp%.csproj=!"=="!__SuccessfulBuilds!" (
                if not "!__SuccessfulPublishes:DjvuNet.Build.Tasks_%_DefaultNetCoreApp%.csproj=!"=="!__SuccessfulPublishes!" (
                    set "__TmpPackageScript=!TEMP!\Package_!RANDOM!.ps1"
                    (
                        echo $ErrorActionPreference = 'Stop'
                        echo ^& '!__RepoRootDir!eng\scripts\PackageTools.ps1' -RepoRoot '!__RepoRootDir!' -Configuration '!_MSB_Configuration!'
                        echo exit $LASTEXITCODE
                    ) > "!__TmpPackageScript!"
                    call :run_custom_command "!__PSCmd! -NoProfile -ExecutionPolicy Bypass -File !__TmpPackageScript!" "PackageTools.ps1"
                    if exist "!__TmpPackageScript!" del "!__TmpPackageScript!"
                )
            )
        )
    )
)

if defined _SkipNative goto :no_djvulibre

REM Clone libdjvulibre if needed

REM ---------------------------------------------------------------------------
REM 1. Clone libdjvulibre FIRST so we can read its vcpkg.json
REM ---------------------------------------------------------------------------
if not exist ".\%__DjvuLibreDir%\win32\djvulibre\libdjvulibre\libdjvulibre.vcxproj" (
    echo %__MsgPrefix%Setting up DjVuLibre

    set "__ArchiveUrl=https://github.com/DjvuNet/DjVuLibre/archive/refs/tags/!__ArtifactsReleaseTag!.tar.gz"

    set "_ForceCloneDjvuLibre="
    if defined _CloneDeps (
        set "_ForceCloneDjvuLibre=1"
    ) else (
        echo %__MsgPrefix%Downloading release archive of DjVuLibre for tag !__ArtifactsReleaseTag!
        call :download_retry "!__ArchiveUrl!" "djvulibre.tar.gz"
        if !ERRORLEVEL! EQU 0 (
             echo %__MsgPrefix%Extracting DjVuLibre archive
             if not exist "%__DjvuLibreDir%" mkdir "%__DjvuLibreDir%"
             tar -xzf djvulibre.tar.gz -C "%__DjvuLibreDir%" --strip-components=1
             del /f /q djvulibre.tar.gz
        ) else (
            set "_ForceCloneDjvuLibre=1"
        )
    )

    if defined _ForceCloneDjvuLibre (
        echo %__MsgPrefix%Bypassing archive download, executing git clone
        set "__CloneArgs=-c core.autocrlf=false"
        if not "!_CloneDeps!"=="full" set "__CloneArgs=--depth 1 !__CloneArgs!"
        call :git_clone_retry ^
            "https://github.com/DjvuNet/DjVuLibre.git" ^
            "%__DjvuLibreDir%" ^
            "!__CloneArgs!"
    )

    if not [!ERRORLEVEL!]==[0] goto :skip_native_setup
) else (
    echo %__MsgPrefix%DjVuLibre already cloned
)

REM ---------------------------------------------------------------------------
REM 2. Extract builtin-baseline from vcpkg.json cleanly
REM ---------------------------------------------------------------------------
set "__VcpkgBaseline="
set "__VcpkgJsonPath=%__RepoRootDir%%__DjvuLibreDir%\vcpkg.json"

if exist "%__VcpkgJsonPath%" (
    set "__PsCommand=(Get-Content '%__VcpkgJsonPath%' | ConvertFrom-Json).'builtin-baseline'"

    for /f "usebackq tokens=*" %%i in (`powershell -NoProfile -Command "!__PsCommand!" 2^>nul`) do (
        set "__VcpkgBaseline=%%i"
    )
)

REM ---------------------------------------------------------------------------
REM 3. Define the VCPKG validation subroutine
REM ---------------------------------------------------------------------------
goto :skip_vcpkg_validation
:is_valid_vcpkg
    set "__TestDir=%~1"

    if not exist "%__TestDir%\vcpkg.exe" exit /b 1
    if not exist "%__TestDir%\.git\"     exit /b 1

    if defined __VcpkgBaseline (
        git -C "%__TestDir%" cat-file -e "%__VcpkgBaseline%^{commit}" >nul 2>&1
        if not !ERRORLEVEL!==0 exit /b 1
    )

    exit /b 0
:skip_vcpkg_validation

REM ---------------------------------------------------------------------------
REM 4. Resolve GlobalVcpkgRoot safely against the baseline
REM ---------------------------------------------------------------------------
set "__GlobalVcpkgRoot="

REM Check explicit environment variable first
if defined VCPKG_ROOT (
    call :is_valid_vcpkg "%VCPKG_ROOT%"
    if !ERRORLEVEL!==0 (
        set "__GlobalVcpkgRoot=%VCPKG_ROOT%"
        goto :vcpkg_found
    )
)

REM Fallback to checking the system PATH
for /f "delims=" %%i in ('where vcpkg 2^>nul') do (
    call :is_valid_vcpkg "%%~dpi."
    if !ERRORLEVEL!==0 (
        set "__GlobalVcpkgRoot=%%~dpi"
        goto :vcpkg_found
    )
)

REM Fallback to the local repository clone
set "__GlobalVcpkgRoot=%__RepoRootDir%vcpkg"

:vcpkg_found
if "%__GlobalVcpkgRoot:~-1%"=="\" (
    set "__GlobalVcpkgRoot=%__GlobalVcpkgRoot:~0,-1%"
)
echo %__MsgPrefix%Resolved Vcpkg Root: !__GlobalVcpkgRoot!

REM ---------------------------------------------------------------------------
REM 5. Setup vcpkg baseline if local fallback was used
REM ---------------------------------------------------------------------------
if /i "%__GlobalVcpkgRoot%"=="%__RepoRootDir%vcpkg" (
    set "__CloneValid=0"
    if exist "%__GlobalVcpkgRoot%\.git\" (
        if exist "%__GlobalVcpkgRoot%\bootstrap-vcpkg.bat" (
            if exist "%__GlobalVcpkgRoot%\vcpkg.exe" (
                if defined __VcpkgBaseline (
                    git -C "%__GlobalVcpkgRoot%" cat-file -e "%__VcpkgBaseline%^{commit}" >nul 2>&1
                    if !ERRORLEVEL!==0 set "__CloneValid=1"
                ) else (
                    set "__CloneValid=1"
                )
            )
        )
    )

    if "!__CloneValid!"=="0" (
        if exist "%__GlobalVcpkgRoot%\" (
            echo %__MsgPrefix%Removing broken vcpkg baseline at %__GlobalVcpkgRoot%
            rmdir /s /q "%__GlobalVcpkgRoot%"
        )

        echo %__MsgPrefix%Cloning local Microsoft vcpkg baseline
        call :git_clone_retry ^
            "https://github.com/Microsoft/vcpkg.git" ^
            "vcpkg" ^
            "-c core.autocrlf=false"

        if not [!ERRORLEVEL!]==[0] goto :skip_native_setup
    )

    if not exist "%__GlobalVcpkgRoot%\vcpkg.exe" (
        echo %__MsgPrefix%Bootstrapping vcpkg
        call :run_custom_command ^
            "%__GlobalVcpkgRoot%\bootstrap-vcpkg.bat -disableMetrics" ^
            "vcpkg_bootstrap"

        if not [!ERRORLEVEL!]==[0] goto :skip_native_setup
    )
)

goto :no_djvulibre

:skip_native_setup
set "_SkipNative=1"
:no_djvulibre

if /i "%_MSB_Target%" == "Clean" goto :end_dotnet_restore
if not defined _BuildDjvuNet goto :skip_djvulibre_build

if /i "%_Framework%" == "%_DefaultNetCoreApp%" goto :dotnet_restore

goto :end_dotnet_restore

:dotnet_restore

REM Reset args for standard repository restore
set "__BuildCommandArgs=-property:Configuration=!_MSB_Configuration! -property:Platform=!__ManagedPlatform! -property:TargetFramework=!__Framework! -p:RuntimeIdentifier=!__RuntimeIdentifier! -v:!_Verbosity! -m:!_Processors! -nologo -nr:false"
set "__RestoreCmdArgs=!__BuildCommandArgs!"

if not defined _BuildTools (
    call :restore_dotnet_proj !__SystemAttrProj! System.Attributes.csproj
    if defined _FastFail if not !ERRORLEVEL! EQU 0 goto exit_error
)

call :restore_dotnet_proj !__DjvuNetProj! DjvuNet.csproj
if defined _FastFail if not !ERRORLEVEL! EQU 0 goto exit_error

:end_dotnet_restore

if defined _SkipNative goto :no_djvulibre_build

if defined __BuildLibDjvuLibre (
    REM Scope environment changes start {
    setlocal

    echo %__MsgPrefix%Building native libdjvulibre.vcxproj

    set "__HostArch=%PROCESSOR_ARCHITECTURE%"
    if /i "!__HostArch!" == "AMD64" set "__HostArch=x64"

    if /i "!__HostArch!" == "%_MSB_Platform%" (
        set "__VCBuildArch=!__HostArch!"
    ) else (
        set "__VCBuildArch=!__HostArch!_%_MSB_Platform%"
    )

    echo %__MsgPrefix%Using environment: "%__VCToolsRoot%\vcvarsall.bat" !__VCBuildArch!
    call                                 "%__VCToolsRoot%\vcvarsall.bat" !__VCBuildArch!

    set "__NativeLogsDir=!__RootBuildDir!!_TargetOS!.!_MSB_Platform!.!_MSB_Configuration!/logs/native/msvc-!VCToolsVersion!/"
    if not exist "!__NativeLogsDir!" md "!__NativeLogsDir!"

    set "__BuildLogRootName=libdjvulibre"
    set "__BuildLog=!__NativeLogsDir!!__BuildLogRootName!.log"
    set "__BuildWrn=!__NativeLogsDir!!__BuildLogRootName!.wrn"
    set "__BuildErr=!__NativeLogsDir!!__BuildLogRootName!.err"
    set "__MsbuildLog="-flp:Verbosity=diag;LogFile=!__BuildLog!""
    set "__MsbuildWrn="/flp1:WarningsOnly;LogFile=!__BuildWrn!""
    set "__MsbuildErr="/flp2:ErrorsOnly;LogFile=!__BuildErr!""
    set "__MsbuildLogging=!__MsbuildLog! !__MsbuildWrn! !__MsbuildErr!"

    set "__VcpkgRootArg="
    if defined __GlobalVcpkgRoot set "__VcpkgRootArg=/p:VcpkgRoot=!__GlobalVcpkgRoot!"
    set "__VcpkgManifestArg=/p:VcpkgManifestDir=%__RepoRootDir%%__DjvuLibreDir%"

    set "__PlatformToolsetArg="
    if defined VisualStudioVersion (
        if "!VisualStudioVersion:~0,3!"=="17." (
            echo %__MsgPrefix%WARNING: Falling back to Visual Studio 2022 ^(v143^) toolset. This is an unsupported build configuration.
            set "__PlatformToolsetArg=/p:PlatformToolset=v143"
        )
        if "!VisualStudioVersion:~0,3!"=="18." set "__PlatformToolsetArg=/p:PlatformToolset=v145"
    )

    echo %__MsgPrefix%Calling: msbuild /p:Configuration=%_MSB_Configuration% /p:Platform=%_MSB_Platform% /p:TargetFramework=%__Framework% /p:VcpkgEnableManifest=true !__VcpkgManifestArg! !__VcpkgRootArg! !__PlatformToolsetArg! /t:%_MSB_Target% /v:%_Verbosity% /m:%_Processors% /nologo /nr:false !__MsbuildLogging! "%__RepoRootDir%%__DjvuLibreDir%\win32\djvulibre\libdjvulibre\libdjvulibre.vcxproj"
    call msbuild /p:Configuration=%_MSB_Configuration% /p:Platform=%_MSB_Platform% /p:TargetFramework=%__Framework% /p:VcpkgEnableManifest=true !__VcpkgManifestArg! !__VcpkgRootArg! !__PlatformToolsetArg! /t:%_MSB_Target% /v:%_Verbosity% /m:%_Processors% /nologo /nr:false !__MsbuildLogging! "%__RepoRootDir%%__DjvuLibreDir%\win32\djvulibre\libdjvulibre\libdjvulibre.vcxproj"

    if not [!ERRORLEVEL!]==[0] (
        echo %__MsgPrefix%Error: native libdjvulibre library build failed. Refer to the build log files for details:
        echo     !__BuildLog!
        echo     !__BuildWrn!
        echo     !__BuildErr!
        set "_NativeFailed=1"
    ) else (
        set "_NativeFailed=0"
    )

REM } Scope environment changes end
    for %%F in ("!_NativeFailed!") do (
        endlocal
        set "_NativeFailed=%%~F"
    )

    if "!_NativeFailed!"=="1" (
        set "__FailedBuilds=!__FailedBuilds! libdjvulibre"
        set "_SkipNative=1"
        if defined _FastFail goto exit_error
    ) else (
        set "__SuccessfulBuilds=!__SuccessfulBuilds! libdjvulibre"
    )
)

:no_djvulibre_build

set "__LogsDir=!__RootBuildDir!!_TargetOS!.!_MSB_Platform!.!_MSB_Configuration!/logs/!__Framework!/"
if not exist "!__LogsDir!" md "!__LogsDir!"

if not defined _BuildTools (
    call :build_dotnet_proj !__SystemAttrProj! System.Attributes.csproj
    if defined _FastFail if not !ERRORLEVEL! EQU 0 goto exit_error
)

call :build_dotnet_proj !__DjvuNetProj! DjvuNet.csproj
if defined _FastFail if not !ERRORLEVEL! EQU 0 goto exit_error

if defined _SkipNative (
    if "!_NativeFailed!"=="1" (
        set "__FailedRestores=!__FailedRestores! DjvuNet.DjvuLibre.csproj"
        set "__FailedBuilds=!__FailedBuilds! DjvuNet.DjvuLibre.csproj"
    )
    goto skip_djvulibre_build
)

echo %__MsgPrefix%Restoring DjvuNet.DjvuLibre project:
call :restore_dotnet_proj !__DjvuNetDjvuLibreProj! DjvuNet.DjvuLibre.csproj
if defined _FastFail if not !ERRORLEVEL! EQU 0 goto exit_error
echo.

echo %__MsgPrefix%Building DjvuNet.DjvuLibre project:
call :build_dotnet_proj !__DjvuNetDjvuLibreProj! DjvuNet.DjvuLibre.csproj
if defined _FastFail if not !ERRORLEVEL! EQU 0 goto exit_error

:skip_djvulibre_build

if not defined _Test (
    if not defined _BuildTests (
        if defined _RunTests (
            echo %__MsgPrefix%Preparing to run tests on %_DefaultNetCoreApp%
        ) else (
            if not "!__FailedCommands!" == "" goto exit_error
            if not "!__FailedRestores!" == "" goto exit_error
            if not "!__FailedBuilds!" == "" goto exit_error
            if not "!__FailedPublishes!" == "" goto exit_error
            goto exit_success
        )
    ) else (
        echo %__MsgPrefix%Preparing to build tests
    )
) else (
        echo %__MsgPrefix%Preparing to build and run tests
)

if not exist .\artifacts\test001C.djvu (
    echo.
    set "_ForceCloneArtifacts="
    if defined _CloneDeps (
        set "_ForceCloneArtifacts=1"
    ) else (
        echo !__MsgPrefix!Downloading release archive of artifacts for tag !__ArtifactsReleaseTag!
        call :download_retry "!__ArtifactsTestDataUri!" "artifacts.tar.gz"
        if !ERRORLEVEL! EQU 0 (
            if exist artifacts\ rmdir /s /q artifacts
            mkdir artifacts
            tar.exe -xzf artifacts.tar.gz -C artifacts --strip-components=1
            if not [!ERRORLEVEL!]==[0] (
                echo.
                echo !__MsgPrefix!Error: artifacts extraction returned error
                goto exit_error
            )
            del artifacts.tar.gz
        ) else (
            set "_ForceCloneArtifacts=1"
        )
    )

    if defined _ForceCloneArtifacts (
        echo !__MsgPrefix!Download bypassed or failed, executing git clone for artifacts
        if exist artifacts\ rmdir /s /q artifacts
        set "__CloneArgs=-c core.autocrlf=false"
        if not "!_CloneDeps!"=="full" set "__CloneArgs=--depth 1 !__CloneArgs!"
        call :git_clone_retry ^
            "https://github.com/DjvuNet/artifacts.git" ^
            "artifacts" ^
            "!__CloneArgs!"

        if not [!ERRORLEVEL!]==[0] (
            echo.
            echo !__MsgPrefix!Error: artifacts git clone returned error
            goto exit_error
        )
    )
    echo.
)

REM Setup test environment

:test_environment_setup

set "__TestFramework=%_DefaultNetCoreApp%"

set "__TestOutputDir=!__PublishDir!"
set "__DjvuNetTestsProj=DjvuNet.Tests/DjvuNet.Tests.csproj"
set "__DjvuNetWaveletTestsProj=DjvuNet.Wavelet.Tests/DjvuNet.Wavelet.Tests.csproj"
set "__DjvuNetTestExeProj=DjvuNetTest/DjvuNetTest.csproj"
set "__DjvuNetBenchmarksProj=DjvuNet.Benchmarks/DjvuNet.Benchmarks.csproj"
set "__DjvuNetDjvuLibreTestsProj=DjvuNet.DjvuLibre.Tests/DjvuNet.DjvuLibre.Tests.csproj"
set "__DjvuNetDjvuLibreCompatTestsProj=DjvuNet.DjvuLibre.Compatibility.Tests/DjvuNet.DjvuLibre.Compatibility.Tests.csproj"
set "__DjvuNetAllTestsProj=DjvuNet.All.Tests/DjvuNet.All.Tests.csproj"

if not defined _Test (
    if not defined _BuildTests (
        if defined _RunTests (
            goto run_tests
        )
    )
)

REM Restore test projects

if defined _TestAll (
    call :restore_dotnet_proj !__DjvuNetAllTestsProj! DjvuNet.All.Tests.csproj
    if defined _FastFail if not !ERRORLEVEL! EQU 0 goto exit_error
    goto :skip_djvunet_tests_restore
)

call :restore_dotnet_proj !__DjvuNetTestsProj! DjvuNet.Tests.csproj
if defined _FastFail if not !ERRORLEVEL! EQU 0 goto exit_error

call :restore_dotnet_proj !__DjvuNetWaveletTestsProj! DjvuNet.Wavelet.Tests.csproj
if defined _FastFail if not !ERRORLEVEL! EQU 0 goto exit_error

call :restore_dotnet_proj !__DjvuNetTestExeProj! DjvuNetTest.csproj
if defined _FastFail if not !ERRORLEVEL! EQU 0 goto exit_error

if defined _SkipNative (
    if "!_NativeFailed!"=="1" set "__FailedRestores=!__FailedRestores! DjvuNet.DjvuLibre.Tests.csproj"
    if "!_NativeFailed!"=="1" set "__FailedRestores=!__FailedRestores! DjvuNet.DjvuLibre.Compatibility.Tests.csproj"
    if "!_NativeFailed!"=="1" set "__FailedRestores=!__FailedRestores! DjvuNet.Benchmarks.csproj"
    goto :skip_djvunet_tests_restore
)

call :restore_dotnet_proj !__DjvuNetDjvuLibreTestsProj! DjvuNet.DjvuLibre.Tests.csproj
if defined _FastFail if not !ERRORLEVEL! EQU 0 goto exit_error

call :restore_dotnet_proj !__DjvuNetDjvuLibreCompatTestsProj! DjvuNet.DjvuLibre.Compatibility.Tests.csproj
if defined _FastFail if not !ERRORLEVEL! EQU 0 goto exit_error

call :restore_dotnet_proj !__DjvuNetBenchmarksProj! DjvuNet.Benchmarks.csproj
if defined _FastFail if not !ERRORLEVEL! EQU 0 goto exit_error

:skip_djvunet_tests_restore

REM Build and publish tests

if defined _TestAll (
    call :build_dotnet_proj !__DjvuNetAllTestsProj! DjvuNet.All.Tests.csproj
    if defined _FastFail if not !ERRORLEVEL! EQU 0 goto exit_error
    goto skip_djvulibre_tests_proj
)

call :build_dotnet_proj !__DjvuNetTestsProj! DjvuNet.Tests.csproj
if defined _FastFail if not !ERRORLEVEL! EQU 0 goto exit_error

call :build_dotnet_proj !__DjvuNetWaveletTestsProj! DjvuNet.Wavelet.Tests.csproj
if defined _FastFail if not !ERRORLEVEL! EQU 0 goto exit_error

call :build_dotnet_proj !__DjvuNetTestExeProj! DjvuNetTest.csproj
if defined _FastFail if not !ERRORLEVEL! EQU 0 goto exit_error

call :build_dotnet_proj !__DjvuNetBenchmarksProj! DjvuNet.Benchmarks.csproj
if defined _FastFail if not !ERRORLEVEL! EQU 0 goto exit_error

if defined _SkipNative (
    if "!_NativeFailed!"=="1" set "__FailedBuilds=!__FailedBuilds! DjvuNet.DjvuLibre.Tests.csproj"
    if "!_NativeFailed!"=="1" set "__FailedBuilds=!__FailedBuilds! DjvuNet.DjvuLibre.Compatibility.Tests.csproj"
    goto skip_djvulibre_tests_proj
)
call :build_dotnet_proj !__DjvuNetDjvuLibreTestsProj! DjvuNet.DjvuLibre.Tests.csproj
if defined _FastFail if not !ERRORLEVEL! EQU 0 goto exit_error

call :build_dotnet_proj !__DjvuNetDjvuLibreCompatTestsProj! DjvuNet.DjvuLibre.Compatibility.Tests.csproj
if defined _FastFail if not !ERRORLEVEL! EQU 0 goto exit_error

:skip_djvulibre_tests_proj

if defined _RunTests goto run_tests
if not defined _Test (
    if not "!__FailedRestores!" == "" goto exit_error
    if not "!__FailedBuilds!" == "" goto exit_error
    if not "!__FailedPublishes!" == "" goto exit_error
    goto exit_success
)

REM Prepare for running tests

:run_tests
call :print_build_summary

echo.
echo %__MsgPrefix%======================================================================
echo %__MsgPrefix%                           STARTING TESTS
echo %__MsgPrefix%======================================================================
echo.

call :get_time __TestPhaseStartTime

set "_DjvuNet_Tests=%__TestOutputDir%DjvuNet.Tests.exe"
set "_DjvuNet_DjvuLibre_Tests=%__TestOutputDir%DjvuNet.DjvuLibre.Tests.exe"
set "_DjvuNet_DjvuLibreCompat_Tests=%__TestOutputDir%DjvuNet.DjvuLibre.Compatibility.Tests.exe"
set "_DjvuNet_Wavelet_Tests=%__TestOutputDir%DjvuNet.Wavelet.Tests.exe"
set "_DjvuNet_All_Tests=%__TestOutputDir%DjvuNet.All.Tests.exe"
set "__TestResOutputDir=TestResults/!__Framework!/"
set "__DotNetCommandx64=!__DotNetCmd!"

if /i "%__TestFramework%" == "%_DefaultNetCoreApp%" (
    set DOTNET_ROLL_FORWARD=Major
    if [%__ManagedPlatform%] == [x86] set _xUnit_console="!__DotNetCommandx86!" "!__OutputDir!xunit.console.dll"
    if [%__ManagedPlatform%] == [x64] set _xUnit_console="!__DotNetCommandx64!" "!__OutputDir!xunit.console.dll"
    if [%__ManagedPlatform%] == [AnyCPU] set _xUnit_console="!__DotNetCommandx64!" "!__OutputDir!xunit.console.dll"
    set "_Test_Options=-trait- "Category=skip-netcoreapp""
    set "__TestOutputFormat=xml"
)

if /i "%__TestFramework%" == "%_DefaultNetStandard%" (
    if [%__ManagedPlatform%] == [x86] set _xUnit_console="!__DotNetCommandx86!" "!__OutputDir!xunit.console.dll"
    if [%__ManagedPlatform%] == [x64] set _xUnit_console="!__DotNetCommandx64!" "!__OutputDir!xunit.console.dll"
    if [%__ManagedPlatform%] == [AnyCPU] set _xUnit_console="!__DotNetCommandx64!" "!__OutputDir!xunit.console.dll"
    set "_Test_Options=-trait- "Category=skip-netcoreapp""
    set "__TestOutputFormat=xml"
)

if /i [%_Verbosity%] == [d] set _Test_Options=!_Test_Options! -verbose
if /i [%_Verbosity%] == [diag] set _Test_Options=!_Test_Options! -verbose -internaldiagnostics
set "_Test_Options=!_Test_Options! -trait- "Category=Skip" -nologo -nocolor"

set "__XunitConfig=xunit.runner.json"

REM Run tests

:xUnit_tests
if defined _TestAll (
    call :run_dotnet_test "!_DjvuNet_All_Tests!" "DjvuNet.All.Tests"
    if defined _FastFail if not !ERRORLEVEL! EQU 0 goto exit_error
    goto :skip_granular_tests_run
)

call :run_dotnet_test "!_DjvuNet_Tests!" "DjvuNet.Tests"
if defined _FastFail if not !ERRORLEVEL! EQU 0 goto exit_error

if defined _SkipNative goto :no_djvulibre_tests
if defined __SkipNativeTests goto :no_djvulibre_tests

call :run_dotnet_test "!_DjvuNet_DjvuLibre_Tests!" "DjvuNet.DjvuLibre.Tests"
if defined _FastFail if not !ERRORLEVEL! EQU 0 goto exit_error

call :run_dotnet_test "!_DjvuNet_DjvuLibreCompat_Tests!" "DjvuNet.DjvuLibre.Compatibility.Tests"
if defined _FastFail if not !ERRORLEVEL! EQU 0 goto exit_error

:no_djvulibre_tests

call :run_dotnet_test "!_DjvuNet_Wavelet_Tests!" "DjvuNet.Wavelet.Tests"
if defined _FastFail if not !ERRORLEVEL! EQU 0 goto exit_error

:skip_granular_tests_run

call :get_time __TestPhaseEndTime
call :calc_duration !__TestPhaseStartTime! !__TestPhaseEndTime! __TestPhaseDuration

if not "!__FailedBuilds!" == "" goto test_error
if not "!__FailedTests!" == "" goto test_error
goto test_success

REM Utility functions

:test_error
goto exit_error

:test_success
goto exit_success

:exit_success
call :print_full_summary
echo.
if defined _RunTests (
    echo %__MsgPrefix%Success: Build and tests passed at %DATE% %TIME%
) else (
    echo %__MsgPrefix%Success: Build passed at %DATE% %TIME%
)
call :print_build_options
exit /b 0

:exit_error
call :print_full_summary
echo.
echo %__MsgPrefix%Error: Build Failed at %DATE% %TIME%
call :print_build_options
exit /b 1

:print_build_summary
REM Capture build end time only once, exactly when the first report is printed
if not defined __BuildEndTime (
    call :get_time __BuildEndTime
    call :calc_duration !__BuildStartTime! !__BuildEndTime! __BuildDuration
)
echo.
echo %__MsgPrefix%======================================================================
echo %__MsgPrefix%                           BUILD SUMMARY
echo %__MsgPrefix%======================================================================
if not "!__SuccessfulClones!" == "" (
    echo %__MsgPrefix%Successfully cloned:
    for %%A in (!__SuccessfulClones!) do echo %__MsgPrefix%  - %%A
)
if not "!__SuccessfulCommands!" == "" (
    echo %__MsgPrefix%Successfully executed commands:
    for %%A in (!__SuccessfulCommands!) do echo %__MsgPrefix%  - %%A
)
if not "!__SuccessfulRestores!" == "" (
    echo %__MsgPrefix%Successfully restored:
    for %%A in (!__SuccessfulRestores!) do echo %__MsgPrefix%  - %%A
)
if not "!__SuccessfulBuilds!" == "" (
    echo %__MsgPrefix%Successfully built:
    for %%A in (!__SuccessfulBuilds!) do echo %__MsgPrefix%  - %%A
)
if not "!__SuccessfulPublishes!" == "" (
    echo %__MsgPrefix%Successfully published:
    for %%A in (!__SuccessfulPublishes!) do echo %__MsgPrefix%  - %%A
)
if not "!__FailedClones!" == "" (
    echo %__MsgPrefix%Failed to clone:
    for %%A in (!__FailedClones!) do echo %__MsgPrefix%  - %%A
)
if not "!__FailedCommands!" == "" (
    echo %__MsgPrefix%Failed to execute commands:
    for %%A in (!__FailedCommands!) do echo %__MsgPrefix%  - %%A
)
if not "!__FailedRestores!" == "" (
    echo %__MsgPrefix%Failed to restore:
    for %%A in (!__FailedRestores!) do echo %__MsgPrefix%  - %%A
)
if not "!__FailedBuilds!" == "" (
    echo %__MsgPrefix%Failed to build:
    for %%A in (!__FailedBuilds!) do echo %__MsgPrefix%  - %%A
)
if not "!__FailedPublishes!" == "" (
    echo %__MsgPrefix%Failed to publish:
    for %%A in (!__FailedPublishes!) do echo %__MsgPrefix%  - %%A
)
goto :eof

:print_full_summary
call :print_build_summary
if not "!__SuccessfulTests!" == "" (
    echo %__MsgPrefix%Successfully tested:
    for %%A in (!__SuccessfulTests!) do echo %__MsgPrefix%  - %%A
)
if not "!__FailedTests!" == "" (
    echo %__MsgPrefix%Failed tests:
    for %%A in (!__FailedTests!) do echo %__MsgPrefix%  - %%A
)
echo.
echo %__MsgPrefix%======================================================================
echo %__MsgPrefix%                           TIMING SUMMARY
echo %__MsgPrefix%======================================================================
set "bp=Build Phase:                                     "
set "bp=!bp:~0,41!"
set "bdur=         !__BuildDuration!"
set "bdur=!bdur:~-9!"
echo %__MsgPrefix%!bp!!bdur!
if defined __TestPhaseDuration (
    set "tp=Test Phase (with overhead):                      "
    set "tp=!tp:~0,41!"
    set "tdur=         !__TestPhaseDuration!"
    set "tdur=!tdur:~-9!"
    echo %__MsgPrefix%!tp!!tdur!
)
if not "!__TestTimings!" == "" (
    echo %__MsgPrefix%Individual Tests:
    for %%T in (!__TestTimings!) do (
        for /f "tokens=1,2,3,4 delims=|" %%A in ("%%T") do (
            set "tname=  - %%A                                        "
            set "tname=!tname:~0,41!"
            set "tdur=         %%B"
            set "tdur=!tdur:~-9!"
            echo %__MsgPrefix%!tname!!tdur!
        )
    )
)
call :get_time __TotalEndTime
call :calc_duration !__BuildStartTime! !__TotalEndTime! __TotalDuration
echo %__MsgPrefix%----------------------------------------------------------------------
set "tot=Total Duration:                                  "
set "tot=!tot:~0,41!"
set "totdur=         !__TotalDuration!"
set "totdur=!totdur:~-9!"
echo %__MsgPrefix%!tot!!totdur!
echo %__MsgPrefix%======================================================================
echo.
goto :eof

:print_build_options
set "__CmdLine=build.cmd"
set "__CmdLine=!__CmdLine! -c !_MSB_Configuration!"
set "__CmdLine=!__CmdLine! -p !_MSB_Platform!"
set "__CmdLine=!__CmdLine! -t !_MSB_Target!"
set "__CmdLine=!__CmdLine! -f !_Framework!"
set "__CmdLine=!__CmdLine! -OS !_TargetOS!"
set "__CmdLine=!__CmdLine! -v !_Verbosity!"
set "__CmdLine=!__CmdLine! -proc !_Processors!"
if defined _BuildDjvuNet set "__CmdLine=!__CmdLine! -DjvuNet"
if defined _BuildTools set "__CmdLine=!__CmdLine! -ts"
if defined _BuildTests set "__CmdLine=!__CmdLine! -bt"
if defined _RunTests set "__CmdLine=!__CmdLine! -rt"
if defined _SkipNative set "__CmdLine=!__CmdLine! -sn"
if defined _CloneDeps (
    if "!_CloneDeps!"=="full" (
        set "__CmdLine=!__CmdLine! -cdps full"
    ) else (
        set "__CmdLine=!__CmdLine! -cdps"
    )
)
if defined _FastFail set "__CmdLine=!__CmdLine! -ff"

echo.
echo %__MsgPrefix%Executed Command: !__CmdLine!
goto :eof

:restore_dotnet_proj

set "__DjvuTargetProject=%~1"
set "__DjvuTargetProjectName=%~2"

set "__RestoreLogRootName=!__DjvuTargetProjectName!.Restore"
set "__RestoreLog=!__LogsDir!!__RestoreLogRootName!.log"
set "__RestoreWrn=!__LogsDir!!__RestoreLogRootName!.wrn"
set "__RestoreErr=!__LogsDir!!__RestoreLogRootName!.err"
set "__MsbuildLog="/flp:Verbosity=diag;LogFile=!__RestoreLog!""
set "__MsbuildWrn="/flp1:WarningsOnly;LogFile=!__RestoreWrn!""
set "__MsbuildErr="/flp2:ErrorsOnly;LogFile=!__RestoreErr!""
set "__MsbuildLogging=!__MsbuildLog! !__MsbuildWrn! !__MsbuildErr!"

echo %__MsgPrefix%Restoring %__DjvuTargetProject%
echo %__MsgPrefix%Calling: !__RestoreCmd! !__RestoreCmdArgs! !__MsbuildLogging! !__DjvuTargetProject!
call !__RestoreCmd! !__RestoreCmdArgs! !__MsbuildLogging! !__DjvuTargetProject!

if not [%ERRORLEVEL%]==[0] (
    echo %__MsgPrefix%Error: nuget restore of %__DjvuTargetProject% returned error
    set "__FailedRestores=!__FailedRestores! %__DjvuTargetProject%"
    if defined _FastFail exit /b 1
) else (
    echo %__MsgPrefix%Success: nuget restore of %__DjvuTargetProject% finished
    set "__SuccessfulRestores=!__SuccessfulRestores! %__DjvuTargetProject%"
)

echo.
goto :eof
echo.
goto :eof

:build_dotnet_proj

set "__BuildProj=%~1"
set "__BuildProjName=%~2"

set "__BuildLogRootName=!__BuildProjName!.!_MSB_Target!"
set "__BuildLog=!__LogsDir!!__BuildLogRootName!.log"
set "__BuildWrn=!__LogsDir!!__BuildLogRootName!.wrn"
set "__BuildErr=!__LogsDir!!__BuildLogRootName!.err"
set "__MsbuildLog="/flp:Verbosity=diag;LogFile=!__BuildLog!""
set "__MsbuildWrn="/flp1:WarningsOnly;LogFile=!__BuildWrn!""
set "__MsbuildErr="/flp2:ErrorsOnly;LogFile=!__BuildErr!""
set "__MsbuildLogging=!__MsbuildLog! !__MsbuildWrn! !__MsbuildErr!"

echo.
echo %__MsgPrefix%Building %__BuildProj%
echo %__MsgPrefix%calling !__BuildCommand! !__BuildCommandArgs! -restore:false -t:%_MSB_Target% !__MsbuildLogging! "!__RepoRootDir!!__BuildProj!"
call !__BuildCommand! !__BuildCommandArgs! -restore:false -t:%_MSB_Target% !__MsbuildLogging! "!__RepoRootDir!!__BuildProj!"

if not [%ERRORLEVEL%]==[0] (
    echo %__MsgPrefix%Error: %__BuildProj% build failed. Refer to the build log files ^for details:
    echo     !__BuildLog!
    echo     !__BuildWrn!
    echo     !__BuildErr!
    set "__FailedBuilds=!__FailedBuilds! %__BuildProjName%"
    if defined _FastFail exit /b 1
    goto :eof
) else (
    set "__SuccessfulBuilds=!__SuccessfulBuilds! %__BuildProjName%"
)

if not defined __SkipPublish (
    REM Scope environment changes start {
    setlocal
    set "__PublishLogRootName=!__BuildProjName!.Publish"
    set "__PublishLog=!__LogsDir!!__PublishLogRootName!.log"
    set "__PublishWrn=!__LogsDir!!__PublishLogRootName!.wrn"
    set "__PublishErr=!__LogsDir!!__PublishLogRootName!.err"
    set "__MsbuildPubLog="-flp:Verbosity=diag;LogFile=!__PublishLog!""
    set "__MsbuildPubWrn="-flp1:WarningsOnly;LogFile=!__PublishWrn!""
    set "__MsbuildPubErr="-flp2:ErrorsOnly;LogFile=!__PublishErr!""
    set "__MsbuildLogging=!__MsbuildPubLog! !__MsbuildPubWrn! !__MsbuildPubErr!"

    echo.
    echo %__MsgPrefix%Publishing %__BuildProj%
    echo %__MsgPrefix%calling !__BuildCommand! !__BuildCommandArgs! -restore:false -t:Publish !__MsbuildLogging! "!__RepoRootDir!!__BuildProj!"
    call !__BuildCommand! !__BuildCommandArgs! -restore:false -t:Publish !__MsbuildLogging! "!__RepoRootDir!!__BuildProj!"

    if not [%ERRORLEVEL%]==[0] (
        echo %__MsgPrefix%Error: %__BuildProj% publish failed. Refer to the publish log files ^for details:
        echo     !__PublishLog!
        echo     !__PublishWrn!
        echo     !__PublishErr!
        for %%F in ("%__BuildProjName%") do (
            endlocal
            set "__FailedPublishes=!__FailedPublishes! %%~F"
        )
        if defined _FastFail goto exit_error
        goto :eof
    )

    for %%F in ("%__BuildProjName%") do (
        endlocal
        set "__SuccessfulPublishes=!__SuccessfulPublishes! %%~F"
    )
    REM } Scope environment changes end
)

goto :eof

:run_dotnet_test
set "__DjvuTargetTestExe=%~1"
set "__DjvuTargetTestName=%~2"

echo.
if not exist "!__DjvuTargetTestExe!" (
    echo %__MsgPrefix%Skipping !__DjvuTargetTestName! because assembly is missing.
    set "__FailedTests=!__FailedTests! !__DjvuTargetTestName!"
) else (
    echo %__MsgPrefix%Running tests from !__DjvuTargetTestName! assembly
    echo %__MsgPrefix%calling: "!__DjvuTargetTestExe!" !_Test_Options! "-!__TestOutputFormat!" "!__TestResOutputDir!!__DjvuTargetTestName!.!__TestOutputFormat!"
    call :get_time __TestAsmStart
    call "!__DjvuTargetTestExe!" !_Test_Options! "-!__TestOutputFormat!" "!__TestResOutputDir!!__DjvuTargetTestName!.!__TestOutputFormat!"
    set "__TestErr=!ERRORLEVEL!"
    call :get_time __TestAsmEnd

    set "__ExtractedDuration="
    if /i "!__TestOutputFormat!"=="xml" (
        set "__TmpPsScript=!TEMP!\ExtractTime_!RANDOM!.ps1"
        (
            echo $xmlPath = '!__TestResOutputDir!!__DjvuTargetTestName!.!__TestOutputFormat!'
            echo $xml = Select-Xml -Path $xmlPath -XPath '//assembly'
            echo Write-Output $xml.Node.time
        ) > "!__TmpPsScript!"

        for /f "usebackq" %%d in (`!__PSCmd! -NoProfile -ExecutionPolicy ByPass -File "!__TmpPsScript!" 2^>nul`) do (
            set "__ExtractedDuration=%%ds"
        )
        if exist "!__TmpPsScript!" del "!__TmpPsScript!"
    )

    if defined __ExtractedDuration (
        set "__TestAsmDuration=!__ExtractedDuration!"
    ) else (
        call :calc_duration !__TestAsmStart! !__TestAsmEnd! __TestAsmDuration
    )

    echo %__MsgPrefix%Test assembly !__DjvuTargetTestName! completed in !__TestAsmDuration!
    set "__TestTimings=!__TestTimings! !__DjvuTargetTestName!|!__TestAsmDuration!|!__TestAsmStart!|!__TestAsmEnd!"
    if not [!__TestErr!]==[0] (
        set "__FailedTests=!__FailedTests! !__DjvuTargetTestName!"
        if defined _FastFail exit /b 1
    ) else (
        set "__SuccessfulTests=!__SuccessfulTests! !__DjvuTargetTestName!"
    )
)
goto :eof

:usage
echo.
echo Usage: build.cmd [options]
echo.
echo Options:
echo.
echo   -c, -Configuration [config]     Build configuration (Debug, Release, Checked). Default: Debug.
echo.
echo   -p, -Platform [platform]        Build platform (x64, x86, arm, arm64, AnyCPU). Default: x64.
echo.
echo   -t, -Target [target]            MSBuild target (Build, Rebuild, Clean, Pack). Default: Build.
echo.
echo   -f, -Framework [tfm]            Target framework (net10.0, netstandard2.1, net472). Default: net10.0.
echo.
echo   -DjvuNet, -BuildDjvuNet          Build the core DjvuNet managed projects. Default: True.
echo.
echo   -ts, -Tools                      Build the custom DjvuNet build tasks and
echo                                    package them into cross-platform archives.
echo                                    Default: False.
echo.
echo   -bt, -BuildTests                 Build the test projects. Default: False.
echo.
echo   -rt, -RunTests                   Run the test projects (does not build them). Default: False.
echo.
echo   -Test                            Build and run the test projects (equivalent to -bt and -rt).
echo.
echo   -ff, -FastFail                   Fail immediately on the first error.
echo                                    Default is False (Late Fail): collects all project restore,
echo                                    build, publish and test failures and reports them at the end.
echo.
echo   -sn, -SkipNative                 Skip cloning, building, and testing of native components
echo                                    (libdjvulibre) and its managed wrapper (DjvuNet.DjvuLibre)
echo                                    and tests depending on it.
echo                                    When omitted, native dependencies are processed (SkipNative=False).
echo.
echo   -cdps, -CloneDeps [full]         Bypass tar.gz snapshot downloads for dependencies (DjVuLibre,
echo                                    artifacts, libgit2sharp) and use git clone instead.
echo                                    Defaults to shallow clone (--depth 1). Pass 'full' for full history.
echo.
echo   -v, -Verbosity [n]               Verbosity (q[uiet], m[inimal], n[ormal], d[etailed], diag[nostic]). Default: normal.
echo.
echo   -proc, -Processors [8]           Number of build processes. Default: !NUMBER_OF_PROCESSORS!
echo.
echo   -OS [OSX]                        Target OS (Windows, Linux, OSX). Default: Windows.
echo.
echo   -h, -?, -help                    Show this usage message.
echo.
exit /b 1

:get_time
set "t=%TIME: =0%"
set /a "h=1%t:~0,2%-100", "m=1%t:~3,2%-100", "s=1%t:~6,2%-100", "c=1%t:~9,2%-100"
set /a "%~1=(h*360000)+(m*6000)+(s*100)+c"
goto :eof

:calc_duration
set /a "diff=%~2 - %~1"
if !diff! LSS 0 set /a "diff+=8640000"
set /a "sec=diff / 100", "cs=diff %% 100"
if !cs! LSS 10 set "cs=0!cs!"
set "%~3=!sec!.!cs!0s"
goto :eof

:run_custom_command
set "__CommandToRun=%~1"
set "__CommandName=%~2"

echo %__MsgPrefix%Running: !__CommandName!
echo %__MsgPrefix%Calling: !__CommandToRun!
call !__CommandToRun!

if not [!ERRORLEVEL!]==[0] (
    echo %__MsgPrefix%Error: !__CommandName! returned error code !ERRORLEVEL!
    set "__FailedCommands=!__FailedCommands! !__CommandName!"
    if defined _FastFail goto exit_error
    exit /b 1
) else (
    set "__SuccessfulCommands=!__SuccessfulCommands! !__CommandName!"
)
goto :eof

:download_retry
setlocal
set "url=%~1"
set "dest=%~2"
set "max_attempts=5"
set "attempt=1"
set "delay=10"
set "base_timeout_sec=120"

:download_loop
set /a "current_timeout_sec=!base_timeout_sec! * !attempt!"
echo %__MsgPrefix%download attempt !attempt! of !max_attempts! for !url! (Timeout: !current_timeout_sec!s)...
curl.exe -L -s -o "!dest!" --max-time !current_timeout_sec! "!url!"
if [!ERRORLEVEL!]==[0] (
    for %%F in ("!dest!") do (
        endlocal
        set "__SuccessfulCommands=!__SuccessfulCommands! download_%%~F"
    )
    exit /b 0
)

if !attempt! GEQ !max_attempts! (
    echo %__MsgPrefix%Error: download failed or timed out after !max_attempts! attempts.
    for %%F in ("!dest!") do (
        endlocal
        set "__FailedCommands=!__FailedCommands! download_%%~F"
    )
    if defined _FastFail goto exit_error
    exit /b 1
)

echo %__MsgPrefix%download failed. Retrying in !delay! seconds...
ping 127.0.0.1 -n !delay! > nul
set /a "delay=!delay! * 2"
set /a "attempt=!attempt! + 1"
goto download_loop

:git_clone_retry
setlocal
set "url=%~1"
set "dest=%~2"
set "extra_args=%~3"
set "max_attempts=5"
set "attempt=1"
set "delay=10"
set "base_timeout_ms=120000"

:git_clone_loop
set /a "current_timeout_ms=!base_timeout_ms! * !attempt!"
set /a "current_timeout_s=!current_timeout_ms! / 1000"
echo %__MsgPrefix%git clone attempt !attempt! of !max_attempts! for !url! (Timeout: !current_timeout_s!s)...
set "CLONE_CMD=clone !extra_args! !url! !dest!"
powershell -NoProfile -ExecutionPolicy ByPass -Command "$p = Start-Process git -ArgumentList '!CLONE_CMD!' -PassThru -NoNewWindow; if (-not $p.WaitForExit(!current_timeout_ms!)) { $p.Kill(); exit 1 } else { exit $p.ExitCode }"
if [!ERRORLEVEL!]==[0] (
    for %%F in ("!dest!") do (
        endlocal
        set "__SuccessfulClones=!__SuccessfulClones! %%~F"
    )
    exit /b 0
)

if !attempt! GEQ !max_attempts! (
    echo %__MsgPrefix%Error: git clone failed or timed out after !max_attempts! attempts.
    for %%F in ("!dest!") do (
        endlocal
        set "__FailedClones=!__FailedClones! %%~F"
    )
    if defined _FastFail goto exit_error
    exit /b 1
)

echo %__MsgPrefix%git clone failed. Retrying in !delay! seconds...
ping 127.0.0.1 -n !delay! > nul
set /a "delay=!delay! * 2"
set /a "attempt=!attempt! + 1"
goto git_clone_loop


