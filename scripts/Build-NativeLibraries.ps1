# Build and Update Native libpg_query Libraries
# This script builds libpg_query for specified PostgreSQL versions and organizes them in the runtime directories

<#
.SYNOPSIS
    Builds libpg_query native libraries for multiple PostgreSQL versions.

.DESCRIPTION
    Clones libpg_query repository, builds libraries for specified PostgreSQL versions,
    and organizes them in the appropriate runtime directories for the Npgquery project.

.PARAMETER Versions
    Comma-separated list of PostgreSQL major versions to build (e.g., "16,17" or "14,15,16,17")

.PARAMETER LibPgQueryPath
    Path where libpg_query repository will be cloned (default: temp directory)

.PARAMETER Clean
    Remove libpg_query clone after building

.PARAMETER Force
    Force rebuild even if libraries already exist

.EXAMPLE
    .\Build-NativeLibraries.ps1 -Versions "16,17"

.EXAMPLE
    .\Build-NativeLibraries.ps1 -Versions "14,15,16,17,18" -Force

.EXAMPLE
    .\Build-NativeLibraries.ps1 -Versions "16,17" -LibPgQueryPath "C:\Temp\libpg_query" -Clean
#>

[CmdletBinding()]
param(
    [Parameter(Mandatory = $false)]
    [string]$Versions = "16,17",
    
    [Parameter(Mandatory = $false)]
    [string]$LibPgQueryPath = (Join-Path $env:TEMP "libpg_query_build"),
    
    [Parameter(Mandatory = $false)]
    [switch]$Clean,
    
    [Parameter(Mandatory = $false)]
    [switch]$Force
)

$ErrorActionPreference = "Stop"

# Determine script and project paths
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$ProjectRoot = Split-Path -Parent $ScriptDir
$NpgqueryProjectPath = Join-Path $ProjectRoot "src\libs\Npgquery\Npgquery"
$RuntimesPath = Join-Path $NpgqueryProjectPath "runtimes"

Write-Host "=== libpg_query Native Library Builder ===" -ForegroundColor Cyan
Write-Host ""

# Parse versions
$VersionList = $Versions -split "," | ForEach-Object { $_.Trim() }
Write-Host "PostgreSQL versions to build: $($VersionList -join ', ')" -ForegroundColor Yellow
Write-Host ""

# Detect platform and architecture
$IsWindows = $IsWindows -or ($PSVersionTable.PSVersion.Major -lt 6)
$Platform = if ($IsWindows) { "Windows" } elseif ($IsMacOS) { "macOS" } else { "Linux" }

if ($IsWindows) {
    $RID = "win-x64"
    $LibExtension = ".dll"
    $LibPrefix = ""
} elseif ($IsMacOS) {
    # Detect architecture
    $Arch = uname -m
    $RID = if ($Arch -eq "arm64") { "osx-arm64" } else { "osx-x64" }
    $LibExtension = ".dylib"
    $LibPrefix = "lib"
} else {
    # Linux
    $RID = "linux-x64"
    $LibExtension = ".so"
    $LibPrefix = "lib"
}

Write-Host "Platform: $Platform" -ForegroundColor Green
Write-Host "Runtime Identifier: $RID" -ForegroundColor Green
Write-Host "Library naming: ${LibPrefix}pg_query_XX${LibExtension}" -ForegroundColor Green
Write-Host ""

# Create runtimes directory structure
$TargetDir = Join-Path $RuntimesPath "$RID\native"
if (-not (Test-Path $TargetDir)) {
    Write-Host "Creating directory: $TargetDir" -ForegroundColor Yellow
    New-Item -ItemType Directory -Path $TargetDir -Force | Out-Null
}

# Clone or update libpg_query
if (Test-Path $LibPgQueryPath) {
    if ($Force) {
        Write-Host "Removing existing libpg_query clone..." -ForegroundColor Yellow
        Remove-Item -Path $LibPgQueryPath -Recurse -Force
    } else {
        Write-Host "Using existing libpg_query at: $LibPgQueryPath" -ForegroundColor Green
        Write-Host "(Use -Force to rebuild)" -ForegroundColor Gray
        Write-Host ""
    }
}

if (-not (Test-Path $LibPgQueryPath)) {
    Write-Host "Cloning libpg_query repository..." -ForegroundColor Yellow
    git clone https://github.com/pganalyze/libpg_query.git $LibPgQueryPath
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Failed to clone libpg_query repository"
        exit 1
    }
    Write-Host "✓ Clone complete" -ForegroundColor Green
    Write-Host ""
}

# Build each version
foreach ($Version in $VersionList) {
    $Branch = "$Version-latest"
    $OutputFile = "${LibPrefix}pg_query_${Version}${LibExtension}"
    $TargetFile = Join-Path $TargetDir $OutputFile
    
    Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Cyan
    Write-Host "Building PostgreSQL $Version" -ForegroundColor Cyan
    Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Cyan
    Write-Host ""
    
    # Check if already exists
    if ((Test-Path $TargetFile) -and -not $Force) {
        Write-Host "✓ Library already exists: $OutputFile" -ForegroundColor Green
        Write-Host "  Size: $([Math]::Round((Get-Item $TargetFile).Length / 1MB, 2)) MB" -ForegroundColor Gray
        Write-Host "  (Use -Force to rebuild)" -ForegroundColor Gray
        Write-Host ""
        continue
    }
    
    # Checkout branch
    Write-Host "Checking out branch: $Branch" -ForegroundColor Yellow
    Push-Location $LibPgQueryPath
    try {
        git fetch origin
        git checkout $Branch
        git pull origin $Branch
        
        if ($LASTEXITCODE -ne 0) {
            Write-Warning "Failed to checkout branch $Branch - skipping version $Version"
            continue
        }
        
        Write-Host "✓ Checked out $Branch" -ForegroundColor Green
        Write-Host ""
        
        # Clean previous build
        Write-Host "Cleaning previous build..." -ForegroundColor Yellow
        if ($IsWindows) {
            if (Test-Path "pg_query.dll") { Remove-Item "pg_query.dll" -Force }
            nmake /F Makefile.msvc clean 2>&1 | Out-Null
        } else {
            make clean 2>&1 | Out-Null
        }
        Write-Host "✓ Clean complete" -ForegroundColor Green
        Write-Host ""
        
        # Build
        Write-Host "Building library..." -ForegroundColor Yellow
        if ($IsWindows) {
            nmake /F Makefile.msvc
            $SourceFile = "pg_query.dll"
        } else {
            make
            $SourceFile = "libpg_query${LibExtension}"
        }
        
        if ($LASTEXITCODE -ne 0) {
            Write-Error "Build failed for PostgreSQL $Version"
            continue
        }
        
        if (-not (Test-Path $SourceFile)) {
            Write-Error "Build output not found: $SourceFile"
            continue
        }
        
        Write-Host "✓ Build complete" -ForegroundColor Green
        Write-Host ""
        
        # Copy to target
        Write-Host "Copying to: $TargetFile" -ForegroundColor Yellow
        Copy-Item $SourceFile $TargetFile -Force
        
        # Verify
        $FileSize = [Math]::Round((Get-Item $TargetFile).Length / 1MB, 2)
        Write-Host "✓ Success!" -ForegroundColor Green
        Write-Host "  File: $OutputFile" -ForegroundColor Green
        Write-Host "  Size: $FileSize MB" -ForegroundColor Green
        Write-Host "  Path: $TargetFile" -ForegroundColor Gray
        Write-Host ""
        
    } finally {
        Pop-Location
    }
}

# Summary
Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Cyan
Write-Host "Build Summary" -ForegroundColor Cyan
Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Cyan
Write-Host ""

# List all libraries in target directory
$Libraries = Get-ChildItem -Path $TargetDir -Filter "*pg_query_*${LibExtension}"
if ($Libraries.Count -eq 0) {
    Write-Warning "No libraries found in $TargetDir"
} else {
    Write-Host "Libraries in $RID/native:" -ForegroundColor Green
    foreach ($Lib in $Libraries) {
        $Size = [Math]::Round($Lib.Length / 1MB, 2)
        Write-Host "  ✓ $($Lib.Name) ($Size MB)" -ForegroundColor Green
    }
    Write-Host ""
    Write-Host "Total: $($Libraries.Count) librar$(if ($Libraries.Count -eq 1) { 'y' } else { 'ies' })" -ForegroundColor Green
}

# Clean up
if ($Clean) {
    Write-Host ""
    Write-Host "Cleaning up libpg_query clone..." -ForegroundColor Yellow
    Remove-Item -Path $LibPgQueryPath -Recurse -Force
    Write-Host "✓ Cleanup complete" -ForegroundColor Green
}

Write-Host ""
Write-Host "=== Build Complete ===" -ForegroundColor Cyan
Write-Host ""
Write-Host "Next steps:" -ForegroundColor Yellow
Write-Host "1. Run tests: dotnet test" -ForegroundColor White
Write-Host "2. Verify version switching works with your new libraries" -ForegroundColor White
Write-Host "3. Commit the updated runtime libraries" -ForegroundColor White
Write-Host ""

# Check if all requested versions were built
$BuiltVersions = $Libraries | ForEach-Object {
    if ($_.Name -match "pg_query_(\d+)") {
        $matches[1]
    }
}

$MissingVersions = $VersionList | Where-Object { $_ -notin $BuiltVersions }
if ($MissingVersions.Count -gt 0) {
    Write-Warning "Some versions were not built: $($MissingVersions -join ', ')"
    Write-Host "Check the build output above for errors." -ForegroundColor Gray
}
