@echo off

setlocal
:parse
if "%~1"=="" goto endparse
if "%~1"=="Debug" set _MSB_Configuration=Debug
if "%~1"=="Release" set _MSB_Configuration=Release
if "%~1"=="x86" set _MSB_Platform=x86
if "%~1"=="x64" set _MSB_Platform=x64
if "%~1"=="Build" set _MSB_Target=Build
if "%~1"=="Rebuild" set _MSB_Target=Rebuild
if "%~1"=="Clean" set _MSB_Target=Clean
if "%~1"=="test" set _Test=1
SHIFT
GOTO parse
:endparse

if not defined _MSB_Target set _MSB_Target=Rebuild
if not defined _MSB_Configuration set _MSB_Configuration=Debug
if not defined _MSB_Platform set _MSB_Platform=x86

@echo:
@echo Starting Build of DjvuNet at %DATE% %TIME%
@echo Build Target: %_MSB_Target% Configuration: %_MSB_Configuration% Platform: %_MSB_Platform% 

if defined _Test (
    @echo Run Tests = true
) else (
    @echo Run Tests = false
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
	@echo Error: git clone returned error
	goto exit_error
	)
) else (
	@echo:
	@echo DjvuLibre already cloned 
)


@echo,
@echo Restoring nuget packages
call %cd%\Tools\nuget.exe restore  -verbosity detailed

if not %ERRORLEVEL%==0 (
	@echo Error: nuget restore returned error
	goto exit_error
)

@echo,
@echo Building DjvuNet.sln
call MSBuild /p:Configuration=%_MSB_Configuration% /p:Platform=%_MSB_Platform% DjvuNet.sln

if not %ERRORLEVEL%==0 (
	@echo Error: build failed
	goto exit_error
)

set _DjvuNet_Tests=DjvuNet.Tests\bin\%_MSB_Platform%\%_MSB_Configuration%\DjvuNet.Tests.dll
set _DjvuNet_DjvuLibre_Tests=DjvuNet.DjvuLibre.Tests\bin\%_MSB_Platform%\%_MSB_Configuration%\DjvuNet.DjvuLibre.Tests.dll
set _xUnit_console_x86=%cd%\packages\xunit.runner.console.2.2.0\tools\xunit.console.x86.exe 
set _xUnit_console_x64=%cd%\packages\xunit.runner.console.2.2.0\tools\xunit.console.exe
set _Test_Options=-notrait "Category=Skip" -nologo -diagnostics

if defined _Test (
	if %_MSB_Platform%==x64 (
		goto x64_tests
	) else if %_MSB_Platform%==x86 (
		goto x86_tests
	)
) else (
	goto exit_success
)

:x86_tests
@echo:
@echo Running tests from DjvuNet.Tests.dll assembly with options %_Test_Options%
@echo:
call %_xUnit_console_x86% %_DjvuNet_Tests% %_Test_Options% 

@echo:
@echo Running tests from DjvuNet.DjvuLibre.Tests.dll assembly with options %_Test_Options%
@echo:
call %_xUnit_console_x86% %_DjvuNet_DjvuLibre_Tests%  %_Test_Options%

if not %ERRORLEVEL%==0 (
	@echo:
	@echo Error: tests failed
	goto exit_error
) else (
	@echo:
	@echo Success: tests passed
	goto exit_success
)

:x64_tests
@echo:
@echo Running tests from DjvuNet.Tests.dll assembly with options %_Test_Options%
@echo:
call %_xUnit_console_x64% %_DjvuNet_Tests% %_Test_Options% 

@echo:
@echo Running tests from DjvuNet.DjvuLibre.Tests.dll assembly with options %_Test_Options%
@echo:
call %_xUnit_console_x64% %_DjvuNet_DjvuLibre_Tests%  %_Test_Options%

if not %ERRORLEVEL%==0 (
	@echo:
	@echo Error: tests failed
	goto exit_error
) else (
	@echo:
	@echo Success: tests passed
	goto exit_success
)

:exit_success
@echo, 
@echo Finished Build at %DATE% %TIME%
exit /b 0

:exit_error
@echo:
@echo Build Failed at %DATE% %TIME%
exit /b 1
