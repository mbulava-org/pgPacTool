@echo off
REM Test script to verify Windows DLL build logic
REM Run this from a "Developer Command Prompt for VS 2022"

set PG_VERSION=%1
if "%PG_VERSION%"=="" set PG_VERSION=17

echo Testing Windows DLL build for PostgreSQL %PG_VERSION%
echo.

REM Navigate to libpg_query directory
cd /d "%~dp0..\libpg_query"

REM Checkout the correct version
echo Checking out %PG_VERSION%-latest branch...
git checkout %PG_VERSION%-latest
git pull

REM Build the static library
echo.
echo Building static library with nmake...
nmake /F Makefile.msvc

if errorlevel 1 (
    echo ERROR: nmake failed
    exit /b 1
)

echo Static library built successfully
echo.

REM Create DEF file
echo Creating pg_query.def...
(
echo LIBRARY pg_query
echo EXPORTS
echo     pg_query_deparse_protobuf
echo     pg_query_fingerprint
echo     pg_query_free_deparse_result
echo     pg_query_free_fingerprint_result
echo     pg_query_free_normalize_result
echo     pg_query_free_parse_result
echo     pg_query_free_plpgsql_parse_result
echo     pg_query_free_scan_result
echo     pg_query_free_split_result
echo     pg_query_is_utility_stmt
echo     pg_query_normalize
echo     pg_query_parse
echo     pg_query_parse_plpgsql
echo     pg_query_scan
echo     pg_query_split
echo     pg_query_summary
) > pg_query.def

REM Test the extraction and linking logic
echo.
echo Testing extraction and linking logic...
echo.

REM Clean up old object files
del /Q *.obj 2>nul

REM List library contents
echo Library contents:
lib /NOLOGO /LIST pg_query.lib > lib_contents.txt
for /f %%i in ('find /c /v "" ^< lib_contents.txt') do set OBJ_COUNT=%%i
echo Found %OBJ_COUNT% object files in library

REM Extract all object files
echo Extracting object files...
for /f "delims=" %%f in (lib_contents.txt) do (
    lib /NOLOGO /EXTRACT:%%f pg_query.lib 2>nul
)

REM Count extracted files
dir /b *.obj > extracted.txt
for /f %%i in ('find /c /v "" ^< extracted.txt') do set EXTRACTED_COUNT=%%i
echo Extracted %EXTRACTED_COUNT% object files

if %EXTRACTED_COUNT%==0 (
    echo ERROR: No object files extracted!
    exit /b 1
)

REM Create response file with quotes
echo Creating response file...
del objs.rsp 2>nul
for /f "delims=" %%f in ('dir /b *.obj') do (
    echo "%%~ff" >> objs.rsp
)

echo First 10 object files:
powershell -Command "Get-Content objs.rsp | Select-Object -First 10"

REM Link the DLL
echo.
echo Linking DLL...
link /DLL /OUT:pg_query.dll /DEF:pg_query.def @objs.rsp MSVCRT.lib

if errorlevel 1 (
    echo ERROR: Link failed with exit code %errorlevel%
    echo.
    echo Response file contents:
    powershell -Command "Get-Content objs.rsp | Select-Object -First 5"
    exit /b 1
)

REM Check the DLL
if exist pg_query.dll (
    echo.
    echo SUCCESS! Built pg_query.dll
    dir pg_query.dll
    echo.
    echo Checking exports...
    dumpbin /EXPORTS pg_query.dll | findstr pg_query_
    echo.
    echo ===============================================
    echo SUCCESS! The build logic works correctly.
    echo You can safely commit and push your changes.
    echo ===============================================
) else (
    echo ERROR: pg_query.dll was not created
    exit /b 1
)
