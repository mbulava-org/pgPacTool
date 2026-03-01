# Analyze libpg_query Version Differences
# Compares two versions of libpg_query and generates detailed reports

<#
.SYNOPSIS
    Analyzes differences between libpg_query versions.

.DESCRIPTION
    Clones libpg_query repository and compares two version branches to identify:
    - Protobuf schema changes
    - C API changes
    - Node type additions/removals
    - Breaking changes

.PARAMETER BaseVersion
    Base PostgreSQL version (e.g., "16")

.PARAMETER CompareVersion
    Version to compare against base (e.g., "17")

.PARAMETER LibPgQueryPath
    Path where libpg_query will be cloned (default: temp directory)

.PARAMETER OutputPath
    Path where analysis reports will be saved

.PARAMETER Detailed
    Generate detailed diff files

.EXAMPLE
    .\Analyze-VersionDifferences.ps1 -BaseVersion 16 -CompareVersion 17

.EXAMPLE
    .\Analyze-VersionDifferences.ps1 -BaseVersion 16 -CompareVersion 17 -Detailed
#>

[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$BaseVersion,
    
    [Parameter(Mandatory = $true)]
    [string]$CompareVersion,
    
    [Parameter(Mandatory = $false)]
    [string]$LibPgQueryPath = (Join-Path $env:TEMP "libpg_query_analysis"),
    
    [Parameter(Mandatory = $false)]
    [string]$OutputPath = "docs\version-differences",
    
    [Parameter(Mandatory = $false)]
    [switch]$Detailed
)

$ErrorActionPreference = "Stop"

Write-Host "=== libpg_query Version Difference Analyzer ===" -ForegroundColor Cyan
Write-Host ""
Write-Host "Comparing: PostgreSQL $BaseVersion → $CompareVersion" -ForegroundColor Yellow
Write-Host ""

# Create output directory
if (-not (Test-Path $OutputPath)) {
    New-Item -ItemType Directory -Path $OutputPath -Force | Out-Null
}

# Clone or update libpg_query
if (Test-Path $LibPgQueryPath) {
    Write-Host "Using existing clone at: $LibPgQueryPath" -ForegroundColor Green
    Push-Location $LibPgQueryPath
    git fetch origin
} else {
    Write-Host "Cloning libpg_query..." -ForegroundColor Yellow
    git clone https://github.com/pganalyze/libpg_query.git $LibPgQueryPath
    Push-Location $LibPgQueryPath
}

try {
    # Branch names
    $BaseBranch = "$BaseVersion-latest"
    $CompareBranch = "$CompareVersion-latest"
    
    Write-Host "Branches: $BaseBranch vs $CompareBranch" -ForegroundColor Green
    Write-Host ""
    
    # Verify branches exist
    git rev-parse --verify "origin/$BaseBranch" 2>&1 | Out-Null
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Branch $BaseBranch not found"
        return
    }
    
    git rev-parse --verify "origin/$CompareBranch" 2>&1 | Out-Null
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Branch $CompareBranch not found"
        return
    }
    
    # === Protobuf Schema Analysis ===
    Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Cyan
    Write-Host "Analyzing Protobuf Schema Changes" -ForegroundColor Cyan
    Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Cyan
    Write-Host ""
    
    $ProtobufDiff = git diff "origin/$BaseBranch..origin/$CompareBranch" -- "protobuf/"
    
    if ($ProtobufDiff) {
        Write-Host "✓ Protobuf changes detected" -ForegroundColor Yellow
        
        # Save detailed diff
        if ($Detailed) {
            $ProtobufDiffFile = Join-Path $OutputPath "PG${CompareVersion}_protobuf_diff.txt"
            $ProtobufDiff | Out-File -FilePath $ProtobufDiffFile -Encoding UTF8
            Write-Host "  Saved: $ProtobufDiffFile" -ForegroundColor Gray
        }
        
        # Analyze new messages
        $NewMessages = $ProtobufDiff | Select-String "^\+\s*message\s+(\w+)" | ForEach-Object {
            $_.Matches[0].Groups[1].Value
        }
        
        if ($NewMessages) {
            Write-Host ""
            Write-Host "  New message types:" -ForegroundColor Green
            $NewMessages | ForEach-Object { Write-Host "    + $_" -ForegroundColor Green }
        }
        
        # Analyze removed messages
        $RemovedMessages = $ProtobufDiff | Select-String "^-\s*message\s+(\w+)" | ForEach-Object {
            $_.Matches[0].Groups[1].Value
        }
        
        if ($RemovedMessages) {
            Write-Host ""
            Write-Host "  Removed message types:" -ForegroundColor Red
            $RemovedMessages | ForEach-Object { Write-Host "    - $_" -ForegroundColor Red }
        }
        
        # Analyze modified fields
        $AddedFields = ($ProtobufDiff | Select-String "^\+\s+\w+\s+\w+\s+=").Count
        $RemovedFields = ($ProtobufDiff | Select-String "^-\s+\w+\s+\w+\s+=").Count
        
        if ($AddedFields -gt 0 -or $RemovedFields -gt 0) {
            Write-Host ""
            Write-Host "  Field changes:" -ForegroundColor Yellow
            Write-Host "    Added fields: $AddedFields" -ForegroundColor Green
            Write-Host "    Removed fields: $RemovedFields" -ForegroundColor Red
        }
    } else {
        Write-Host "✓ No protobuf schema changes" -ForegroundColor Green
    }
    
    Write-Host ""
    
    # === C API Analysis ===
    Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Cyan
    Write-Host "Analyzing C API Changes" -ForegroundColor Cyan
    Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Cyan
    Write-Host ""
    
    $ApiDiff = git diff "origin/$BaseBranch..origin/$CompareBranch" -- "pg_query.h"
    
    if ($ApiDiff) {
        Write-Host "✓ API changes detected" -ForegroundColor Yellow
        
        if ($Detailed) {
            $ApiDiffFile = Join-Path $OutputPath "PG${CompareVersion}_api_diff.txt"
            $ApiDiff | Out-File -FilePath $ApiDiffFile -Encoding UTF8
            Write-Host "  Saved: $ApiDiffFile" -ForegroundColor Gray
        }
        
        # Analyze function signature changes
        $NewFunctions = $ApiDiff | Select-String "^\+.*\s+pg_query_\w+\(" | ForEach-Object {
            if ($_ -match "pg_query_(\w+)") { $matches[1] }
        }
        
        if ($NewFunctions) {
            Write-Host ""
            Write-Host "  New functions:" -ForegroundColor Green
            $NewFunctions | ForEach-Object { Write-Host "    + pg_query_$_" -ForegroundColor Green }
        }
        
        $RemovedFunctions = $ApiDiff | Select-String "^-.*\s+pg_query_\w+\(" | ForEach-Object {
            if ($_ -match "pg_query_(\w+)") { $matches[1] }
        }
        
        if ($RemovedFunctions) {
            Write-Host ""
            Write-Host "  Removed functions:" -ForegroundColor Red
            $RemovedFunctions | ForEach-Object { Write-Host "    - pg_query_$_" -ForegroundColor Red }
        }
        
        # Struct changes
        $StructChanges = ($ApiDiff | Select-String "typedef struct").Count
        if ($StructChanges -gt 0) {
            Write-Host ""
            Write-Host "  Structure changes detected: $StructChanges" -ForegroundColor Yellow
        }
    } else {
        Write-Host "✓ No C API changes" -ForegroundColor Green
    }
    
    Write-Host ""
    
    # === Node Type Analysis ===
    Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Cyan
    Write-Host "Analyzing Node Type Changes" -ForegroundColor Cyan
    Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Cyan
    Write-Host ""
    
    $NodeDiff = git diff "origin/$BaseBranch..origin/$CompareBranch" -- "src/postgres/include/nodes/"
    
    if ($NodeDiff) {
        Write-Host "✓ Node type changes detected" -ForegroundColor Yellow
        
        if ($Detailed) {
            $NodeDiffFile = Join-Path $OutputPath "PG${CompareVersion}_nodes_diff.txt"
            $NodeDiff | Out-File -FilePath $NodeDiffFile -Encoding UTF8
            Write-Host "  Saved: $NodeDiffFile" -ForegroundColor Gray
        }
        
        # Count changes
        $AddedLines = ($NodeDiff | Select-String "^\+").Count
        $RemovedLines = ($NodeDiff | Select-String "^-").Count
        
        Write-Host ""
        Write-Host "  Lines added: $AddedLines" -ForegroundColor Green
        Write-Host "  Lines removed: $RemovedLines" -ForegroundColor Red
    } else {
        Write-Host "✓ No node type changes" -ForegroundColor Green
    }
    
    Write-Host ""
    
    # === Generate Summary Report ===
    Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Cyan
    Write-Host "Generating Summary Report" -ForegroundColor Cyan
    Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Cyan
    Write-Host ""
    
    $ReportFile = Join-Path $OutputPath "PG${CompareVersion}_CHANGES.md"
    
    $Report = @"
# PostgreSQL $CompareVersion Changes from $BaseVersion

> **Generated**: $(Get-Date -Format "yyyy-MM-dd HH:mm:ss")
> **Source**: libpg_query repository
> **Branches**: $BaseBranch vs $CompareBranch

## Summary

"@
    
    $HasChanges = $false
    
    if ($ProtobufDiff) {
        $HasChanges = $true
        $Report += @"

### Protobuf Schema Changes

**Status**: ⚠️ Changes detected

"@
        if ($NewMessages) {
            $Report += @"

**New Message Types**:
$(($NewMessages | ForEach-Object { "- ``$_``" }) -join "`n")

"@
        }
        
        if ($RemovedMessages) {
            $Report += @"

**Removed Message Types**:
$(($RemovedMessages | ForEach-Object { "- ``$_``" }) -join "`n")

"@
        }
        
        $Report += @"

**Field Changes**:
- Added: $AddedFields field(s)
- Removed: $RemovedFields field(s)

"@
    } else {
        $Report += @"

### Protobuf Schema Changes

**Status**: ✅ No changes

"@
    }
    
    if ($ApiDiff) {
        $HasChanges = $true
        $Report += @"

### C API Changes

**Status**: ⚠️ Changes detected

"@
        if ($NewFunctions) {
            $Report += @"

**New Functions**:
$(($NewFunctions | ForEach-Object { "- ``pg_query_$_()``" }) -join "`n")

"@
        }
        
        if ($RemovedFunctions) {
            $Report += @"

**Removed Functions**:
$(($RemovedFunctions | ForEach-Object { "- ``pg_query_$_()``" }) -join "`n")

"@
        }
    } else {
        $Report += @"

### C API Changes

**Status**: ✅ No changes

"@
    }
    
    if ($NodeDiff) {
        $HasChanges = $true
        $Report += @"

### Node Type Changes

**Status**: ⚠️ Changes detected

- Lines added: $AddedLines
- Lines removed: $RemovedLines

See detailed diff in ``PG${CompareVersion}_nodes_diff.txt`` (if generated)

"@
    } else {
        $Report += @"

### Node Type Changes

**Status**: ✅ No changes

"@
    }
    
    $Report += @"

## Impact Assessment

"@
    
    if ($HasChanges) {
        $Report += @"

⚠️ **Breaking Changes Detected**

**Required Actions**:
1. Update ``PostgreSqlVersion`` enum in ``PostgreSqlVersion.cs``
2. Review protobuf changes and update models if needed
3. Test all parsing functionality with version $CompareVersion
4. Update compatibility layer if API changes detected
5. Add version-specific tests
6. Update documentation

**Compatibility**:
- Existing code may need updates
- Version-specific handling may be required
- Test thoroughly before release

"@
    } else {
        $Report += @"

✅ **No Breaking Changes**

PostgreSQL $CompareVersion appears to be compatible with version $BaseVersion.

**Recommended Actions**:
1. Still test thoroughly with version $CompareVersion
2. Verify parse tree outputs match expectations
3. Check for subtle semantic changes
4. Update documentation to list $CompareVersion as supported

"@
    }
    
    $Report += @"

## Next Steps

1. [ ] Review this report
2. [ ] Check detailed diffs (if generated)
3. [ ] Update code for breaking changes
4. [ ] Create version-specific models (if needed)
5. [ ] Add tests for version $CompareVersion
6. [ ] Update documentation
7. [ ] Run full test suite

## Resources

- [libpg_query CHANGELOG](https://github.com/pganalyze/libpg_query/blob/master/CHANGELOG.md)
- [PostgreSQL $CompareVersion Release Notes](https://www.postgresql.org/docs/$CompareVersion/release-$CompareVersion.html)
- [Detailed diffs](./): See ``PG${CompareVersion}_*_diff.txt`` files

---

*This analysis is automated. Manual review is required to catch subtle changes.*
"@
    
    $Report | Out-File -FilePath $ReportFile -Encoding UTF8
    
    Write-Host "✓ Report saved: $ReportFile" -ForegroundColor Green
    Write-Host ""
    
    # === Output Summary ===
    Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Cyan
    Write-Host "Analysis Complete" -ForegroundColor Cyan
    Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Cyan
    Write-Host ""
    
    if ($HasChanges) {
        Write-Host "⚠️ BREAKING CHANGES DETECTED!" -ForegroundColor Red
        Write-Host ""
        Write-Host "Action required:" -ForegroundColor Yellow
        Write-Host "1. Review report: $ReportFile" -ForegroundColor White
        if ($Detailed) {
            Write-Host "2. Check detailed diffs in: $OutputPath" -ForegroundColor White
        }
        Write-Host "3. Update code to handle version differences" -ForegroundColor White
        Write-Host "4. Add version-specific tests" -ForegroundColor White
        Write-Host "5. Update documentation" -ForegroundColor White
    } else {
        Write-Host "✅ No breaking changes detected" -ForegroundColor Green
        Write-Host ""
        Write-Host "Version $CompareVersion appears compatible with $BaseVersion" -ForegroundColor Green
        Write-Host "Review report for details: $ReportFile" -ForegroundColor White
    }
    
    Write-Host ""
    
    # Set output for GitHub Actions
    if ($env:GITHUB_OUTPUT) {
        Add-Content -Path $env:GITHUB_OUTPUT -Value "has_changes=$($HasChanges.ToString().ToLower())"
    }
    
} finally {
    Pop-Location
}
