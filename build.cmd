@echo off

setlocal
:parse
if "%~1"=="" goto endparse
if "%~1"=="Debug" set _MSB_Configuration=Debug
if "%~1"=="Release" set _MSB_Configuration=Release
if "%~1"=="x86" set _MSB_Platform=x86
if "%~1"=="x64" set _MSB_Platform=x64
if "%~1"=="AnyCPU" set _MSB_Platform=AnyCPU
if "%~1"=="Build" set _MSB_Target=Build
if "%~1"=="Rebuild" set _MSB_Target=Rebuild
if "%~1"=="Clean" set _MSB_Target=Clean
if "%~1"=="Test" set _Test=1
if "%~1"=="Target"(
    shift
    if "%~1"=="netfx" set _Framework=netfx
    if "%~1"=="core" set _Framework=core
)

SHIFT
GOTO parse
:endparse

if not defined _MSB_Target set _MSB_Target=Rebuild
if not defined _MSB_Configuration set _MSB_Configuration=Debug
if not defined _MSB_Platform set _MSB_Platform=x86
if not defined _Framework set _Framework=netfx

@echo:
@echo Starting Build of DjvuNet at %DATE% %TIME%
@echo:
@echo Build Target:   %_MSB_Target%
@echo Configuration:  %_MSB_Configuration%
@echo Platform:       %_MSB_Platform%
@echo Framework:      %_Framework%

if defined _Test (
    @echo Run Tests:      True
) else (
    @echo Run Tests:      False
)

if not exist .\DjvuNet.sln (
     @echo:
     @echo Error: DjvuNet.sln file not found in build directory %cd%
     goto exit_error
)

if not exist .\DjVuLibre\win32\djvulibre\libdjvulibre\libdjvulibre.vcxproj (
    @echo:
    @echo Cloning DjVuLibre
    call git clone https://github.com/DjvuNet/DjVuLibre.git
    if not %ERRORLEVEL%==0 (
        @echo:
        @echo Error: git clone https://github.com/DjvuNet/DjVuLibre.git returned error
        goto exit_error
    )
) else (
    @echo:
    @echo DjvuLibre already cloned 
)


@echo,
@echo Restoring nuget packages
call %cd%\Tools\nuget.exe restore DjvuNet.sln -verbosity detailed 

if not %ERRORLEVEL%==0 (
    @echo:
    @echo Error: nuget restore DjvuNet.sln returned error
    goto exit_error
)

@echo,
@echo Building DjvuNet.sln
call MSBuild /p:Configuration=%_MSB_Configuration% /p:Platform=%_MSB_Platform% /t:%_MSB_Target% /v:normal /m /nologo DjvuNet.sln

if not %ERRORLEVEL%==0 (
    @echo Error: build failed
    goto exit_error
)

if not defined _Test (
    goto exit_success
) else if not exist .\artifacts\test001C.djvu (
    @echo:
    @echo Cloning test data from https://github.com/DjvuNet/artifacts.git
    call git clone --depth 1 https://github.com/DjvuNet/artifacts.git
    if not %ERRORLEVEL%==0 (
        @echo:
        @echo Error: git clone returned error
        goto exit_error
    )
)

set _DjvuNet_Tests=DjvuNet.Tests\bin\%_MSB_Platform%\%_MSB_Configuration%\DjvuNet.Tests.dll
set _DjvuNet_DjvuLibre_Tests=DjvuNet.DjvuLibre.Tests\bin\%_MSB_Platform%\%_MSB_Configuration%\DjvuNet.DjvuLibre.Tests.dll
set _DjvuNet_Wavelet_Tests=DjvuNet.Wavelet.Tests\bin\%_MSB_Platform%\%_MSB_Configuration%\DjvuNet.Wavelet.Tests.dll
if %_MSB_Platform%==x86 set _xUnit_console=%cd%\packages\xunit.runner.console.2.2.0\tools\xunit.console.x86.exe 
if %_MSB_Platform%==x64 set _xUnit_console=%cd%\packages\xunit.runner.console.2.2.0\tools\xunit.console.exe
set _Test_Options=-notrait "Category=Skip" -nologo -diagnostics

:xUnit_tests
@echo:
@echo Running tests from DjvuNet.Tests.dll assembly with console %_xUnit_console% options %_Test_Options%
@echo:
call %_xUnit_console% %_DjvuNet_Tests% %_Test_Options%

if not %ERRORLEVEL%==0 set _DjvuNet_Tests_Error=true 

@echo:
@echo Running tests from DjvuNet.DjvuLibre.Tests.dll assembly with console %_xUnit_console% options %_Test_Options%
@echo:
call %_xUnit_console% %_DjvuNet_DjvuLibre_Tests%  %_Test_Options%

if not %ERRORLEVEL%==0 set _DjvuNet_DjvuLibre_Tests_Error=true

@echo:
@echo Running tests from DjvuNet.Wavelet.Tests.dll assembly with console %_xUnit_console% options %_Test_Options%
@echo:
call %_xUnit_console% %_DjvuNet_Wavelet_Tests%  %_Test_Options%

if %_DjvuNet_Tests_Error%==true goto test_error
if %_DjvuNet_DjvuLibre_Tests_Error%==true goto test_error
if not %ERRORLEVEL%==0 goto test_error
goto test_success

:test_error
@echo:
@echo Error: tests failed
goto exit_error

:test_success
@echo:
@echo Success: tests passed
goto exit_success


:exit_success
@echo, 
@echo Finished Build at %DATE% %TIME%
exit /b 0

:exit_error
@echo:
@echo Build Failed at %DATE% %TIME%
exit /b 1
