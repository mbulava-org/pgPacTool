@echo off
REM Launcher script - automatically finds and uses Developer Command Prompt
REM Run this from any command prompt or PowerShell

set PG_VERSION=%1
if "%PG_VERSION%"=="" set PG_VERSION=17

echo.
echo ========================================
echo Testing Windows DLL Build (PG %PG_VERSION%)
echo ========================================
echo.

REM Find Visual Studio installation
set "VSWHERE=%ProgramFiles(x86)%\Microsoft Visual Studio\Installer\vswhere.exe"

if not exist "%VSWHERE%" (
    echo ERROR: Visual Studio 2022 not found!
    echo Please install Visual Studio 2022 with C++ workload
    exit /b 1
)

REM Get VS installation path
for /f "usebackq tokens=*" %%i in (`"%VSWHERE%" -latest -requires Microsoft.VisualStudio.Component.VC.Tools.x86.x64 -property installationPath`) do (
    set VS_PATH=%%i
)

if not exist "%VS_PATH%\VC\Auxiliary\Build\vcvarsall.bat" (
    echo ERROR: vcvarsall.bat not found!
    echo VS Path: %VS_PATH%
    exit /b 1
)

echo Found Visual Studio at: %VS_PATH%
echo Setting up build environment...
echo.

REM Setup environment and run test in the same session
call "%VS_PATH%\VC\Auxiliary\Build\vcvarsall.bat" x64

if errorlevel 1 (
    echo ERROR: Failed to setup build environment
    exit /b 1
)

echo Build environment configured successfully
echo.
echo Starting build test...
echo.

REM Run the test script
call "%~dp0test-windows-build.bat" %PG_VERSION%

exit /b %errorlevel%
