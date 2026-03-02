# Script to add ExcludeFromCodeCoverage attribute to generated protobuf files
# This ensures they don't count toward code coverage metrics

param(
    [string]$ProjectPath = "src\libs\Npgquery\Npgquery"
)

Write-Host "Adding ExcludeFromCodeCoverage to generated protobuf files..." -ForegroundColor Cyan

# Find all generated protobuf files
$protoFiles = Get-ChildItem -Path $ProjectPath -Recurse -Filter "PgQuery.cs" | Where-Object {
    $_.FullName -match "obj\\Debug\\net\d+\.\d+\\Protos"
}

if ($protoFiles.Count -eq 0) {
    Write-Host "No generated protobuf files found. Run 'dotnet build' first." -ForegroundColor Yellow
    exit 0
}

Write-Host "Found $($protoFiles.Count) generated protobuf file(s)" -ForegroundColor Green

foreach ($file in $protoFiles) {
    Write-Host "Processing: $($file.FullName)"
    
    $content = Get-Content $file.FullName -Raw
    
    # Check if already has the attribute
    if ($content -match "\[System\.Diagnostics\.CodeAnalysis\.ExcludeFromCodeCoverage\]" -or 
        $content -match "\[ExcludeFromCodeCoverage\]") {
        Write-Host "  ✓ Already has ExcludeFromCodeCoverage attribute" -ForegroundColor Gray
        continue
    }
    
    # Add using statement if not present
    if ($content -notmatch "using System\.Diagnostics\.CodeAnalysis;") {
        $content = $content -replace "(using System;)", "`$1`nusing System.Diagnostics.CodeAnalysis;"
    }
    
    # Add attribute to all class and enum declarations
    $content = $content -replace "(^\s*public\s+(sealed\s+)?(partial\s+)?(class|enum|interface)\s+)", "[ExcludeFromCodeCoverage]`n`$1"
    
    # Save the modified file
    Set-Content -Path $file.FullName -Value $content -NoNewline
    
    Write-Host "  ✓ Added ExcludeFromCodeCoverage attribute" -ForegroundColor Green
}

Write-Host "`nDone! Generated files are now excluded from code coverage." -ForegroundColor Green
Write-Host "`nNote: These changes are in the obj folder and will be regenerated on next build."
Write-Host "To make this permanent, add a custom .proto.cs template or build event."
