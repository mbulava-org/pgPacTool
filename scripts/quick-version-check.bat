@echo off
REM Quick test: Check if functions exist in pg_query.lib
REM Run from Developer Command Prompt

cd /d "%~dp0..\libpg_query"

echo Testing PostgreSQL 16...
git checkout 16-latest 2>nul
nmake /F Makefile.msvc clean >nul 2>&1
nmake /F Makefile.msvc >nul 2>&1

echo.
echo === PG 16 Library Contents ===
lib /LIST pg_query.lib | findstr /i "split utility summary"

echo.
echo Testing PostgreSQL 17...
git checkout 17-latest 2>nul  
nmake /F Makefile.msvc clean >nul 2>&1
nmake /F Makefile.msvc >nul 2>&1

echo.
echo === PG 17 Library Contents ===
lib /LIST pg_query.lib | findstr /i "split utility summary"

echo.
echo ========================================
echo If you see different results above,
echo that confirms the functions don't exist
echo in all versions!
echo ========================================
pause
