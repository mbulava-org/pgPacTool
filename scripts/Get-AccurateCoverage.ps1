# Generate Accurate Code Coverage Report (Excluding Generated Code)
# Usage: .\scripts\Get-AccurateCoverage.ps1

param(
    [string]$Project = "Npgquery",
    [string]$TestFilter = "FullyQualifiedName~NativeLibraryIntegrationTests|VersionCompatibilityTests|VersionIsolationVerificationTests|AstGenerationComprehensiveTests|ProtobufComprehensiveTests|AsyncParserComprehensiveTests"
)

Write-Host "`n═══════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "  CODE COVERAGE REPORT (Excluding Generated Code)" -ForegroundColor White
Write-Host "═══════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "`nProject: $Project" -ForegroundColor Yellow
Write-Host "`nRunning tests with coverage collection...`n" -ForegroundColor Gray

# Run tests with coverage
$testProject = "tests\$Project.Tests\$Project.Tests.csproj"
$resultsDir = "./TestResults/Coverage-$(Get-Date -Format 'yyyyMMdd-HHmmss')"

dotnet test $testProject `
    --filter $TestFilter `
    --collect:"XPlat Code Coverage" `
    --settings coverlet.runsettings `
    --results-directory $resultsDir `
    --verbosity quiet

Write-Host "`n" -ForegroundColor Gray

# Find the coverage file
$coverageFile = Get-ChildItem -Path $resultsDir -Recurse -Filter "coverage.cobertura.xml" | Select-Object -First 1

if (-not $coverageFile) {
    Write-Host "❌ Coverage file not found!" -ForegroundColor Red
    exit 1
}

Write-Host "Processing coverage data from: $($coverageFile.Name)`n" -ForegroundColor Gray

# Load and parse coverage
$xml = [xml](Get-Content $coverageFile.FullName)
$package = $xml.coverage.packages.package | Where-Object { $_.name -eq $Project }

if (-not $package) {
    Write-Host "❌ Package '$Project' not found in coverage report!" -ForegroundColor Red
    exit 1
}

# Separate generated vs source files
$allClasses = $package.classes.class
$sourceClasses = $allClasses | Where-Object { 
    $_.filename -notmatch "obj\\Debug" -and 
    $_.filename -notmatch "\\Protos\\" -and
    $_.filename -notmatch "\.Designer\.cs" -and
    $_.filename -notmatch "\.g\.cs"
}

$generatedClasses = $allClasses | Where-Object { 
    $_.filename -match "obj\\Debug" -or 
    $_.filename -match "\\Protos\\" -or
    $_.filename -match "\.Designer\.cs" -or
    $_.filename -match "\.g\.cs"
}

Write-Host "Files Analysis:" -ForegroundColor Yellow
Write-Host "  Total classes in package: $($allClasses.Count)"
Write-Host "  Source files: $($sourceClasses.Count)" -ForegroundColor Green
Write-Host "  Generated files: $($generatedClasses.Count)" -ForegroundColor Gray
Write-Host ""

# Calculate coverage for source files only
$sourceLines = 0
$sourceCovered = 0
$sourceBranches = 0
$sourceBranchesCovered = 0

foreach ($class in $sourceClasses) {
    # Count actual lines from the lines element
    $lines = $class.lines.line
    if ($lines) {
        $sourceLines += $lines.Count
        $sourceCovered += ($lines | Where-Object { [int]$_.hits -gt 0 }).Count
    }

    # For branches, we can use the attributes if present
    if ($class.'branches-valid') { $sourceBranches += [int]$class.'branches-valid' }
    if ($class.'branches-covered') { $sourceBranchesCovered += [int]$class.'branches-covered' }
}

$lineCoverage = if ($sourceLines -gt 0) { [math]::Round(($sourceCovered / $sourceLines) * 100, 2) } else { 0 }
$branchCoverage = if ($sourceBranches -gt 0) { [math]::Round(($sourceBranchesCovered / $sourceBranches) * 100, 2) } else { 0 }

Write-Host "═══════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "  SOURCE CODE COVERAGE (Excluding Generated)" -ForegroundColor White
Write-Host "═══════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""
Write-Host "  Line Coverage:   " -NoNewline
Write-Host "$lineCoverage%" -ForegroundColor $(if ($lineCoverage -ge 90) { "Green" } elseif ($lineCoverage -ge 75) { "Yellow" } else { "Red" })
Write-Host "  Branch Coverage: " -NoNewline
Write-Host "$branchCoverage%" -ForegroundColor $(if ($branchCoverage -ge 80) { "Green" } elseif ($branchCoverage -ge 60) { "Yellow" } else { "Red" })
Write-Host ""
Write-Host "  Lines Covered: $sourceCovered / $sourceLines"
Write-Host "  Branches Covered: $sourceBranchesCovered / $sourceBranches"
Write-Host ""

# Group by file and show coverage
Write-Host "═══════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "  COVERAGE BY SOURCE FILE" -ForegroundColor White
Write-Host "═══════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""

$fileGroups = $sourceClasses | Group-Object { Split-Path $_.filename -Leaf }

$fileCoverage = foreach ($group in $fileGroups) {
    $classes = $group.Group
    $totalLines = 0
    $coveredLines = 0

    foreach ($class in $classes) {
        $lines = $class.lines.line
        if ($lines) {
            $totalLines += $lines.Count
            $coveredLines += ($lines | Where-Object { [int]$_.hits -gt 0 }).Count
        }
    }

    $coverage = if ($totalLines -gt 0) { [math]::Round(($coveredLines / $totalLines) * 100, 2) } else { 0 }

    [PSCustomObject]@{
        File = $group.Name
        Coverage = $coverage
        LinesCovered = $coveredLines
        LinesValid = $totalLines
    }
}

$fileCoverage | Sort-Object -Property Coverage -Descending | Format-Table -AutoSize

# Status
Write-Host "═══════════════════════════════════════════════════════════" -ForegroundColor Cyan
if ($lineCoverage -ge 90) {
    Write-Host "  ✅ EXCELLENT COVERAGE (≥90%)" -ForegroundColor Green
} elseif ($lineCoverage -ge 75) {
    Write-Host "  ✅ GOOD COVERAGE (75-90%)" -ForegroundColor Yellow
} elseif ($lineCoverage -ge 60) {
    Write-Host "  ⚠️  MODERATE COVERAGE (60-75%)" -ForegroundColor Yellow
} else {
    Write-Host "  ❌ LOW COVERAGE (<60%)" -ForegroundColor Red
}
Write-Host "═══════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""

# Return coverage percentage for CI/CD
return $lineCoverage
