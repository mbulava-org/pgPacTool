# Organize Native libpg_query Libraries
# This script organizes downloaded CI artifacts into the runtime directories for the Npgquery project

<#
.SYNOPSIS
    Organizes native libpg_query libraries from CI artifacts into runtime directories.

.DESCRIPTION
    Takes downloaded CI artifacts (structured as libpg_query-{rid}-pg{version} directories)
    and copies the native libraries into the appropriate runtime directories for the
    Npgquery project. Mirrors the artifact organization done by the build-native-libraries
    GitHub Actions workflow.

.PARAMETER ArtifactsPath
    Path to the downloaded artifacts directory containing libpg_query-{rid}-pg{version}
    subdirectories (default: ./artifacts)

.PARAMETER Force
    Overwrite existing library files in the destination directories

.EXAMPLE
    .\organize-native-libs.ps1

.EXAMPLE
    .\organize-native-libs.ps1 -ArtifactsPath "C:\Downloads\artifacts" -Force

.EXAMPLE
    # After downloading CI artifacts to ./artifacts:
    .\organize-native-libs.ps1 -ArtifactsPath "./artifacts"
#>

[CmdletBinding()]
param(
    [Parameter(Mandatory = $false)]
    [string]$ArtifactsPath = (Join-Path (Get-Location) "artifacts"),

    [Parameter(Mandatory = $false)]
    [switch]$Force
)

$ErrorActionPreference = "Stop"

# Determine script and project paths
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$ProjectRoot = Split-Path -Parent $ScriptDir
$NpgqueryProjectPath = Join-Path $ProjectRoot "src\libs\Npgquery\Npgquery"
$RuntimesPath = Join-Path $NpgqueryProjectPath "runtimes"

Write-Host "=== Native Library Organizer ===" -ForegroundColor Cyan
Write-Host ""

# Validate artifacts path
if (-not (Test-Path $ArtifactsPath)) {
    Write-Error "Artifacts directory not found: $ArtifactsPath`nDownload CI artifacts first and specify the path with -ArtifactsPath."
    exit 1
}

Write-Host "Artifacts path : $ArtifactsPath" -ForegroundColor Yellow
Write-Host "Destination    : $RuntimesPath" -ForegroundColor Yellow
Write-Host ""

# Create base runtimes directory
if (-not (Test-Path $RuntimesPath)) {
    Write-Host "Creating runtimes directory: $RuntimesPath" -ForegroundColor Yellow
    New-Item -ItemType Directory -Path $RuntimesPath -Force | Out-Null
}

# Find artifact directories matching the expected pattern: libpg_query-{rid}-pg{version}
$ArtifactPattern = "libpg_query-*"
$ArtifactDirs = Get-ChildItem -Path $ArtifactsPath -Directory -Filter $ArtifactPattern

if ($ArtifactDirs.Count -eq 0) {
    Write-Warning "No artifact directories matching '$ArtifactPattern' found in: $ArtifactsPath"
    Write-Host "Expected directory names like: libpg_query-win-x64-pg16, libpg_query-linux-x64-pg17" -ForegroundColor Gray
    exit 1
}

Write-Host "Found $($ArtifactDirs.Count) artifact director$(if ($ArtifactDirs.Count -eq 1) {'y'} else {'ies'}):" -ForegroundColor Green
foreach ($Dir in $ArtifactDirs) {
    Write-Host "  - $($Dir.Name)" -ForegroundColor Gray
}
Write-Host ""

$CopiedCount = 0
$SkippedCount = 0
$ErrorCount = 0

foreach ($ArtifactDir in $ArtifactDirs) {
    $ArtifactName = $ArtifactDir.Name

    # Parse artifact name: libpg_query-{rid}-pg{version}
    # RID examples: win-x64, linux-x64, osx-x64, osx-arm64
    if ($ArtifactName -notmatch '^libpg_query-(.+)-pg(\d+)$') {
        Write-Warning "Skipping directory with unexpected name format: $ArtifactName"
        $ErrorCount++
        continue
    }

    $RID     = $Matches[1]
    $Version = $Matches[2]

    Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Cyan
    Write-Host "Processing: $ArtifactName" -ForegroundColor Cyan
    Write-Host "  Runtime ID : $RID" -ForegroundColor Gray
    Write-Host "  PG Version : $Version" -ForegroundColor Gray

    # Create target directory
    $TargetDir = Join-Path $RuntimesPath "$RID\native"
    if (-not (Test-Path $TargetDir)) {
        Write-Host "  Creating   : $TargetDir" -ForegroundColor Yellow
        New-Item -ItemType Directory -Path $TargetDir -Force | Out-Null
    }

    # Copy all files from the artifact directory
    $Files = Get-ChildItem -Path $ArtifactDir.FullName -File
    if ($Files.Count -eq 0) {
        Write-Warning "  No files found in artifact directory: $($ArtifactDir.FullName)"
        $ErrorCount++
        continue
    }

    foreach ($File in $Files) {
        $DestFile = Join-Path $TargetDir $File.Name

        if ((Test-Path $DestFile) -and -not $Force) {
            $ExistingSize = [Math]::Round((Get-Item $DestFile).Length / 1MB, 2)
            Write-Host "  ⟳ Skipped  : $($File.Name) (already exists, $ExistingSize MB - use -Force to overwrite)" -ForegroundColor Gray
            $SkippedCount++
            continue
        }

        Copy-Item $File.FullName $DestFile -Force
        $FileSize = [Math]::Round((Get-Item $DestFile).Length / 1MB, 2)
        Write-Host "  ✓ Copied   : $($File.Name) ($FileSize MB)" -ForegroundColor Green
        $CopiedCount++
    }

    Write-Host ""
}

# Summary
Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Cyan
Write-Host "Summary" -ForegroundColor Cyan
Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Cyan
Write-Host ""
Write-Host "  Copied  : $CopiedCount file$(if ($CopiedCount -ne 1) { 's' })" -ForegroundColor Green
if ($SkippedCount -gt 0) {
    Write-Host "  Skipped : $SkippedCount file$(if ($SkippedCount -ne 1) { 's' }) (already exists)" -ForegroundColor Gray
}
if ($ErrorCount -gt 0) {
    Write-Host "  Errors  : $ErrorCount" -ForegroundColor Red
}
Write-Host ""

# Show final directory structure
Write-Host "=== Final directory structure ===" -ForegroundColor Cyan
$AllLibraries = Get-ChildItem -Path $RuntimesPath -Recurse -File
if ($AllLibraries.Count -eq 0) {
    Write-Warning "No files found in $RuntimesPath"
} else {
    foreach ($Lib in $AllLibraries) {
        $RelPath = $Lib.FullName.Substring($RuntimesPath.Length).TrimStart('\', '/')
        $Size = [Math]::Round($Lib.Length / 1MB, 2)
        Write-Host "  $RelPath ($Size MB)" -ForegroundColor Green
    }
}
Write-Host ""

if ($ErrorCount -gt 0) {
    Write-Warning "Completed with $ErrorCount error$(if ($ErrorCount -ne 1) { 's' }). Check output above."
    exit 1
} else {
    Write-Host "=== Organization Complete ===" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "Next steps:" -ForegroundColor Yellow
    Write-Host "1. Run tests: dotnet test" -ForegroundColor White
    Write-Host "2. Verify version switching works with the updated libraries" -ForegroundColor White
    Write-Host "3. Commit the updated runtime libraries" -ForegroundColor White
    Write-Host ""
}
