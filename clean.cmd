@if not defined _echo @echo off
setlocal EnableDelayedExpansion

set __ProjectDir=%~dp0
set NO_DASHES_ARG=%1

REM Check if VBCSCompiler.exe is running
tasklist /fi "imagename eq VBCSCompiler.exe" |find ":" > nul
:: Compiler is running if errorlevel == 1
if errorlevel 1 (
	echo Stop VBCSCompiler.exe execution.
	for /f "tokens=2 delims=," %%F in ('tasklist /nh /fi "imagename eq VBCSCompiler.exe" /fo csv') do taskkill /f /PID %%~F
)

REM Check if dotnet.exe is running
tasklist /fi "imagename eq dotnet.exe" |find ":" > nul
:: dotnet.exe is running if errorlevel == 1
if errorlevel 1 (
	echo Stop VBCSCompiler.exe execution.
	for /f "tokens=2 delims=," %%F in ('tasklist /nh /fi "imagename eq dotnet.exe" /fo csv') do taskkill /f /PID %%~F
)

if not defined NO_DASHES_ARG goto no_args
if /I [%NO_DASHES_ARG:-=%] == [all] (
  echo Cleaning entire repo with: git clean -xdf %__ProjectDir%
  call git clean -xdf %__ProjectDir%
  exit /b !ERRORLEVEL!
)

:no_args

if exist "%__ProjectDir%obj" rd /s /q "%__ProjectDir%obj"
if exist "%__ProjectDir%packages" rd /s /q "%__ProjectDir%packages"
if exist init-tools.log del /q *.log
if exist "%__ProjectDir%build\bin" rd /s /q "%__ProjectDir%build\bin"

exit /b %errorlevel%
