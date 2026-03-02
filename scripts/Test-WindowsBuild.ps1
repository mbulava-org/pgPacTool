# Test script to verify Windows DLL build logic before committing
# This simulates what the GitHub Actions workflow does

param(
    [Parameter(Mandatory=$false)]
    [string]$Version = "17"
)

Write-Host "Testing Windows DLL build for PostgreSQL $Version" -ForegroundColor Cyan

# Navigate to libpg_query directory (clone if needed)
$libpgQueryDir = Join-Path $PSScriptRoot "..\libpg_query"

if (-not (Test-Path $libpgQueryDir)) {
    Write-Host "Cloning libpg_query repository..." -ForegroundColor Yellow
    git clone https://github.com/pganalyze/libpg_query.git $libpgQueryDir
}

Set-Location $libpgQueryDir

# Checkout the correct version branch
Write-Host "Checking out $Version-latest branch..." -ForegroundColor Yellow
git fetch origin
git checkout "$Version-latest"
git pull

# Setup MSVC environment (you need to adjust this path to your VS installation)
Write-Host "Setting up MSVC environment..." -ForegroundColor Yellow
$vsPath = & "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe" -latest -property installationPath
if ($vsPath) {
    Push-Location "$vsPath\VC\Auxiliary\Build"
    cmd /c "vcvarsall.bat x64 & set" | ForEach-Object {
        if ($_ -match "=") {
            $v = $_.split("=", 2)
            [Environment]::SetEnvironmentVariable($v[0], $v[1])
        }
    }
    Pop-Location
    Write-Host "✓ MSVC environment configured" -ForegroundColor Green
} else {
    Write-Host "❌ Visual Studio not found" -ForegroundColor Red
    exit 1
}

# Build the static library
Write-Host "`nBuilding static library with nmake..." -ForegroundColor Yellow
nmake /F Makefile.msvc

if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ nmake failed" -ForegroundColor Red
    exit 1
}

Write-Host "✓ Static library built" -ForegroundColor Green

# Create DEF file
Write-Host "`nCreating pg_query.def..." -ForegroundColor Yellow
$defContent = @'
LIBRARY pg_query
EXPORTS
    pg_query_deparse_protobuf
    pg_query_fingerprint
    pg_query_free_deparse_result
    pg_query_free_fingerprint_result
    pg_query_free_normalize_result
    pg_query_free_parse_result
    pg_query_free_plpgsql_parse_result
    pg_query_free_scan_result
    pg_query_free_split_result
    pg_query_is_utility_stmt
    pg_query_normalize
    pg_query_parse
    pg_query_parse_plpgsql
    pg_query_scan
    pg_query_split
    pg_query_summary
'@
$defContent | Out-File -FilePath pg_query.def -Encoding ASCII

# Extract object files from library then link (TESTING THE NEW LOGIC)
Write-Host "`nExtracting object files from pg_query.lib..." -ForegroundColor Yellow
Remove-Item *.obj -ErrorAction SilentlyContinue

$libContents = & lib /NOLOGO /LIST pg_query.lib
Write-Host "Library contains $($libContents.Count) object files"

foreach ($objFile in $libContents) {
    if ($objFile -match '\.obj$') {
        & lib /NOLOGO "/EXTRACT:$objFile" pg_query.lib 2>&1 | Out-Null
    }
}

$extractedObjs = (Get-ChildItem -Filter "*.obj").FullName
Write-Host "Extracted $($extractedObjs.Count) files"

if ($extractedObjs.Count -eq 0) {
    Write-Host "❌ No object files extracted!" -ForegroundColor Red
    exit 1
}

Write-Host "Creating response file..." -ForegroundColor Yellow
$extractedObjs | ForEach-Object { "`"$_`"" } | Out-File -FilePath objs.rsp -Encoding ASCII

Write-Host "`nFirst 10 object files:" -ForegroundColor Cyan
Get-Content objs.rsp | Select-Object -First 10

Write-Host "`nLinking DLL..." -ForegroundColor Yellow
& link /DLL /OUT:pg_query.dll /DEF:pg_query.def "@objs.rsp" MSVCRT.lib

if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Link failed with exit code $LASTEXITCODE" -ForegroundColor Red
    Write-Host "`nResponse file contents:"
    Get-Content objs.rsp | Select-Object -First 5
    exit 1
}

# Check the DLL
if (Test-Path pg_query.dll) {
    $dllInfo = Get-Item pg_query.dll
    Write-Host "`n✓ Built pg_query.dll" -ForegroundColor Green
    Write-Host "   Size: $($dllInfo.Length) bytes" -ForegroundColor Green
    
    if ($dllInfo.Length -eq 0) {
        Write-Host "❌ DLL is empty (0 bytes)!" -ForegroundColor Red
        exit 1
    }
    
    # Check exports
    Write-Host "`nChecking DLL exports..." -ForegroundColor Yellow
    dumpbin /EXPORTS pg_query.dll | Select-String "pg_query_" | Select-Object -First 10
    
    Write-Host "`n✅ SUCCESS! The build logic works correctly." -ForegroundColor Green
    Write-Host "You can safely commit and push your changes." -ForegroundColor Green
} else {
    Write-Host "❌ Failed to build pg_query.dll" -ForegroundColor Red
    exit 1
}
