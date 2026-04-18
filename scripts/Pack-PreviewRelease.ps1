#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Packs pgPacTool NuGet packages for preview release testing.

.DESCRIPTION
    This script builds and packs all pgPacTool NuGet packages (mbulava.PostgreSql.Dac, 
    MSBuild.Sdk.PostgreSql, and postgresPacTools) for local testing before publishing 
    to NuGet.org.

.PARAMETER Configuration
    Build configuration (default: Release)

.PARAMETER OutputPath
    Output directory for packages (default: ./packages)

.PARAMETER Version
    Package version (default: 1.0.0-preview7)

.PARAMETER SkipBuild
    Skip building the solution before packing

.PARAMETER TestLocally
    After packing, run local installation tests

.EXAMPLE
    .\Pack-PreviewRelease.ps1
    Packs all packages with default settings

.EXAMPLE
    .\Pack-PreviewRelease.ps1 -TestLocally
    Packs all packages and tests local installation

.EXAMPLE
    .\Pack-PreviewRelease.ps1 -Version "1.0.0-preview7" -OutputPath "./preview7"
    Packs with custom version and output path
#>

[CmdletBinding()]
param(
    [Parameter()]
    [string]$Configuration = "Release",
    
    [Parameter()]
    [string]$OutputPath = "./packages",
    
    [Parameter()]
    [string]$Version = "1.0.0-preview7",
    
    [Parameter()]
    [switch]$SkipBuild,
    
    [Parameter()]
    [switch]$TestLocally
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

# Get repository root (script is in scripts/ folder)
$repoRoot = Split-Path -Parent $PSScriptRoot
Push-Location $repoRoot

try {
    Write-Host "╔══════════════════════════════════════════════════════════════╗" -ForegroundColor Cyan
    Write-Host "║         pgPacTool Preview Release Packaging Script          ║" -ForegroundColor Cyan
    Write-Host "╚══════════════════════════════════════════════════════════════╝" -ForegroundColor Cyan
    Write-Host ""

    # Step 1: Build solution (unless skipped)
    if (-not $SkipBuild) {
        Write-Host "📦 Step 1: Building solution in $Configuration mode..." -ForegroundColor Yellow
        dotnet build --configuration $Configuration
        if ($LASTEXITCODE -ne 0) {
            throw "Build failed with exit code $LASTEXITCODE"
        }
        Write-Host "✅ Build succeeded" -ForegroundColor Green
        Write-Host ""
    } else {
        Write-Host "⏭️  Step 1: Skipped (using existing build)" -ForegroundColor Gray
        Write-Host ""
    }

    # Step 2: Create output directory
    Write-Host "📁 Step 2: Creating output directory..." -ForegroundColor Yellow
    $outputDir = Join-Path $repoRoot $OutputPath
    if (Test-Path $outputDir) {
        Write-Host "   Cleaning existing packages..." -ForegroundColor Gray
        Remove-Item "$outputDir/*.nupkg" -Force -ErrorAction SilentlyContinue
    } else {
        New-Item -ItemType Directory -Path $outputDir -Force | Out-Null
    }
    Write-Host "   Output: $outputDir" -ForegroundColor Cyan
    Write-Host ""

    # Step 3: Pack packages
    Write-Host "📦 Step 3: Packing NuGet packages..." -ForegroundColor Yellow
    
    $packages = @(
        @{
            Name = "mbulava.PostgreSql.Dac"
            Path = "src/libs/mbulava.PostgreSql.Dac/mbulava.PostgreSql.Dac.csproj"
            Description = "Core DAC library with embedded Npgquery"
        },
        @{
            Name = "MSBuild.Sdk.PostgreSql"
            Path = "src/sdk/MSBuild.Sdk.PostgreSql/MSBuild.Sdk.PostgreSql.csproj"
            Description = "MSBuild SDK for PostgreSQL database projects"
        },
        @{
            Name = "postgresPacTools"
            Path = "src/postgresPacTools/postgresPacTools.csproj"
            Description = "CLI global tool (pgpac command)"
        }
    )

    $packedFiles = @()
    
    foreach ($package in $packages) {
        Write-Host "   Packing $($package.Name)..." -ForegroundColor Cyan
        Write-Host "   ➜ $($package.Description)" -ForegroundColor Gray
        
        dotnet pack $package.Path `
            --configuration $Configuration `
            --output $outputDir `
            --no-build `
            /p:Version=$Version
        
        if ($LASTEXITCODE -ne 0) {
            throw "Failed to pack $($package.Name)"
        }
        
        $nupkgFile = Join-Path $outputDir "$($package.Name).$Version.nupkg"
        if (Test-Path $nupkgFile) {
            $fileSize = [math]::Round((Get-Item $nupkgFile).Length / 1KB, 2)
            Write-Host "   ✅ Created: $($package.Name).$Version.nupkg ($fileSize KB)" -ForegroundColor Green
            $packedFiles += $nupkgFile
        } else {
            throw "Package file not found: $nupkgFile"
        }
    }
    
    Write-Host ""

    # Step 4: Summary
    Write-Host "╔══════════════════════════════════════════════════════════════╗" -ForegroundColor Green
    Write-Host "║                    ✅ PACKAGING COMPLETE                     ║" -ForegroundColor Green
    Write-Host "╚══════════════════════════════════════════════════════════════╝" -ForegroundColor Green
    Write-Host ""
    Write-Host "📦 Packages created:" -ForegroundColor Cyan
    foreach ($file in $packedFiles) {
        Write-Host "   • $file" -ForegroundColor White
    }
    Write-Host ""

    # Step 5: Verify embedded Npgquery (most important)
    Write-Host "🔍 Step 5: Verifying package contents..." -ForegroundColor Yellow
    
    $dacPackage = $packedFiles | Where-Object { $_ -like "*mbulava.PostgreSql.Dac*" } | Select-Object -First 1
    if ($dacPackage) {
        Write-Host "   Inspecting mbulava.PostgreSql.Dac package..." -ForegroundColor Cyan
        
        # Extract package to temp location for inspection
        $tempDir = Join-Path $env:TEMP "pgpac-verify-$(Get-Random)"
        Expand-Archive -Path $dacPackage -DestinationPath $tempDir -Force
        
        $hasNpgquery = Test-Path "$tempDir/lib/net10.0/Npgquery.dll"
        $nativeDllsCheck = Get-ChildItem "$tempDir/runtimes" -Recurse -Filter "pg_query.*" -ErrorAction SilentlyContinue
        $hasNativeDlls = ($nativeDllsCheck -ne $null) -and ($nativeDllsCheck.Count -gt 0)
        
        if ($hasNpgquery) {
            Write-Host "   ✅ Npgquery.dll is embedded in package" -ForegroundColor Green
        } else {
            Write-Host "   ⚠️  WARNING: Npgquery.dll not found in package!" -ForegroundColor Red
        }
        
        if ($hasNativeDlls) {
            $nativeFiles = Get-ChildItem "$tempDir/runtimes" -Recurse -Filter "pg_query.*"
            Write-Host "   ✅ Native libraries included:" -ForegroundColor Green
            foreach ($nativeFile in $nativeFiles) {
                Write-Host "      • $($nativeFile.FullName.Replace($tempDir, ''))" -ForegroundColor Gray
            }
        } else {
            Write-Host "   ⚠️  WARNING: Native libraries (pg_query.*) not found!" -ForegroundColor Red
        }
        
        # Check .nuspec for dependencies
        $nuspecFile = Get-ChildItem "$tempDir" -Filter "*.nuspec" | Select-Object -First 1
        if ($nuspecFile) {
            $nuspecContent = Get-Content $nuspecFile.FullName -Raw
            if ($nuspecContent -match '<dependency id="Npgquery"') {
                Write-Host "   ⚠️  WARNING: Package has Npgquery dependency (should be embedded)!" -ForegroundColor Red
            } else {
                Write-Host "   ✅ No Npgquery dependency (correctly embedded)" -ForegroundColor Green
            }
        }
        
        Remove-Item $tempDir -Recurse -Force -ErrorAction SilentlyContinue
    }
    Write-Host ""

    # Step 6: Local testing (optional)
    if ($TestLocally) {
        Write-Host "🧪 Step 6: Testing local installation..." -ForegroundColor Yellow
        Write-Host ""
        
        # Test 1: Install CLI tool
        Write-Host "   Test 1: Installing pgpac CLI tool..." -ForegroundColor Cyan
        
        # Uninstall if already installed
        $existing = dotnet tool list --global | Select-String "postgrespactools"
        if ($existing) {
            Write-Host "   Uninstalling existing version..." -ForegroundColor Gray
            dotnet tool uninstall --global postgresPacTools | Out-Null
        }
        
        dotnet tool install --global postgresPacTools `
            --add-source $outputDir `
            --version $Version
        
        if ($LASTEXITCODE -eq 0) {
            $toolVersion = pgpac --version
            Write-Host "   ✅ CLI tool installed: $toolVersion" -ForegroundColor Green
        } else {
            Write-Host "   ❌ CLI tool installation failed" -ForegroundColor Red
        }
        Write-Host ""
        
        # Test 2: Create test project with library
        Write-Host "   Test 2: Testing library package..." -ForegroundColor Cyan
        $testProjectDir = Join-Path $env:TEMP "pgpac-test-$(Get-Random)"
        New-Item -ItemType Directory -Path $testProjectDir -Force | Out-Null
        Push-Location $testProjectDir
        
        try {
            dotnet new console -n TestApp | Out-Null
            Push-Location TestApp
            
            dotnet add package mbulava.PostgreSql.Dac `
                --source $outputDir `
                --version $Version | Out-Null
            
            # Create simple test code
            $testCode = @'
using mbulava.PostgreSql.Dac;

Console.WriteLine("Testing mbulava.PostgreSql.Dac...");
var options = new ProjectCompilerOptions { ProjectPath = "test.csproj" };
Console.WriteLine($"Created options: {options.ProjectPath}");
Console.WriteLine("✅ Package reference works!");
'@
            Set-Content -Path "Program.cs" -Value $testCode
            
            dotnet build --verbosity quiet
            if ($LASTEXITCODE -eq 0) {
                Write-Host "   ✅ Library package works (no dependency errors)" -ForegroundColor Green
            } else {
                Write-Host "   ❌ Library package build failed" -ForegroundColor Red
            }
            
            Pop-Location
        } finally {
            Pop-Location
            Remove-Item $testProjectDir -Recurse -Force -ErrorAction SilentlyContinue
        }
        Write-Host ""
    }

    # Final instructions
    Write-Host "╔══════════════════════════════════════════════════════════════╗" -ForegroundColor Cyan
    Write-Host "║                     📋 NEXT STEPS                            ║" -ForegroundColor Cyan
    Write-Host "╚══════════════════════════════════════════════════════════════╝" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "1️⃣  Test the CLI tool:" -ForegroundColor Yellow
    Write-Host "   dotnet tool install --global postgresPacTools --add-source $outputDir --version $Version" -ForegroundColor White
    Write-Host "   pgpac --version" -ForegroundColor White
    Write-Host ""
    Write-Host "2️⃣  Test in a new project:" -ForegroundColor Yellow
    Write-Host "   dotnet new console -n TestProject" -ForegroundColor White
    Write-Host "   cd TestProject" -ForegroundColor White
    Write-Host "   dotnet add package mbulava.PostgreSql.Dac --source $outputDir --version $Version" -ForegroundColor White
    Write-Host "   dotnet build" -ForegroundColor White
    Write-Host ""
    Write-Host "3️⃣  Publish to NuGet.org:" -ForegroundColor Yellow
    Write-Host "   dotnet nuget push ""$outputDir/*.nupkg"" --source https://api.nuget.org/v3/index.json --api-key YOUR_API_KEY" -ForegroundColor White
    Write-Host ""
    Write-Host "4️⃣  Create GitHub release:" -ForegroundColor Yellow
    Write-Host "   • Tag: v$Version" -ForegroundColor White
    Write-Host "   • Title: pgPacTool v$Version" -ForegroundColor White
    Write-Host "   • Description: Use CHANGELOG.md content" -ForegroundColor White
    Write-Host "   • Attach packages from: $outputDir" -ForegroundColor White
    Write-Host ""

} catch {
    Write-Host ""
    Write-Host "❌ ERROR: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "Stack Trace:" -ForegroundColor Gray
    Write-Host $_.ScriptStackTrace -ForegroundColor Gray
    exit 1
} finally {
    Pop-Location
}
