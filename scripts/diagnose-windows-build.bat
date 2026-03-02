@echo off
REM Deep diagnostic script to understand WHY symbols are missing
REM Run this from Developer Command Prompt for VS 2022

set PG_VERSION=%1
if "%PG_VERSION%"=="" set PG_VERSION=16

echo ========================================
echo DEEP DIAGNOSTIC: Windows DLL Build (PG %PG_VERSION%)
echo ========================================
echo.

cd /d "%~dp0..\libpg_query"

REM Make sure we're on the right branch
git checkout %PG_VERSION%-latest
git pull

REM Clean and build
echo Cleaning...
nmake /F Makefile.msvc clean 2>nul

echo.
echo Building static library...
nmake /F Makefile.msvc

if errorlevel 1 (
    echo ERROR: nmake failed
    pause
    exit /b 1
)

echo.
echo ========================================
echo DIAGNOSTIC 1: What's in pg_query.lib?
echo ========================================
lib /LIST pg_query.lib

echo.
echo ========================================
echo DIAGNOSTIC 2: Looking for missing functions
echo ========================================
echo.
echo Searching for pg_query_is_utility_stmt:
lib /LIST pg_query.lib | findstr /i "is_utility"

echo.
echo Searching for pg_query_split:
lib /LIST pg_query.lib | findstr /i "split"

echo.
echo Searching for pg_query_summary:
lib /LIST pg_query.lib | findstr /i "summary"

echo.
echo ========================================
echo DIAGNOSTIC 3: Check if functions exist in object files
echo ========================================
echo.
echo Checking pg_query_is_utility_stmt.obj:
if exist pg_query_is_utility_stmt.obj (
    dumpbin /SYMBOLS pg_query_is_utility_stmt.obj | findstr /i "pg_query_is_utility_stmt"
) else (
    echo File pg_query_is_utility_stmt.obj does NOT exist
)

echo.
echo Checking pg_query_split.obj:
if exist pg_query_split.obj (
    dumpbin /SYMBOLS pg_query_split.obj | findstr /i "pg_query_split"
) else (
    echo File pg_query_split.obj does NOT exist  
)

echo.
echo Checking pg_query_summary.obj:
if exist pg_query_summary.obj (
    dumpbin /SYMBOLS pg_query_summary.obj | findstr /i "pg_query_summary"
) else (
    echo File pg_query_summary.obj does NOT exist
)

echo.
echo ========================================
echo DIAGNOSTIC 4: Check source files
echo ========================================
echo.
echo Looking for source files:
dir /b src\pg_query_is_utility_stmt.c 2>nul || echo src\pg_query_is_utility_stmt.c NOT FOUND
dir /b src\pg_query_split.c 2>nul || echo src\pg_query_split.c NOT FOUND  
dir /b src\pg_query_summary.c 2>nul || echo src\pg_query_summary.c NOT FOUND

echo.
echo ========================================
echo DIAGNOSTIC 5: Check Makefile for these files
echo ========================================
findstr /i "is_utility_stmt split summary" Makefile.msvc

echo.
echo ========================================
echo DIAGNOSTIC 6: Try to build specific object
echo ========================================
if exist src\pg_query_is_utility_stmt.c (
    echo Trying to compile pg_query_is_utility_stmt.c manually...
    cl /c /I. /I.\vendor /I.\src\postgres\include /I.\src\include src\pg_query_is_utility_stmt.c
)

echo.
echo ========================================
echo RESULTS
echo ========================================
echo.
echo The issue is likely one of these:
echo  1. The source files don't exist in PG %PG_VERSION%
echo  2. The Makefile doesn't compile them
echo  3. The functions are in different files
echo.
pause
