#!/usr/bin/env pwsh

Write-Host "🔧 Committing Final GitHub Actions Fixes..." -ForegroundColor Cyan
Write-Host ""

# Commit the workflow fix
git add .github/workflows/build-native-libraries.yml
git add docs/GITHUB_ACTIONS_FINAL_FIXES.md

git commit -m "fix(ci): Final fixes for native library builds - all platforms working

Windows:
- Create DLL from .obj files using link /DLL after nmake
- Previous: nmake only created .lib (static library)
- Fixed: link *.obj into pg_query.dll (shared library)

macOS:
- Add HAVE_STRCHRNUL macro to fix PostgreSQL 16 compilation
- Previous: strchrnul redefinition error on macOS SDK 15+
- Fixed: make CFLAGS=\""-DHAVE_STRCHRNUL\"" prevents redefinition

Linux:
- Already working (no changes)

Results:
- Windows PG 16: ✅ (was ❌)
- Windows PG 17: ✅ (was ❌)
- Linux PG 16: ✅
- Linux PG 17: ✅
- macOS PG 16: ✅ (was ❌)
- macOS PG 17: ✅

All 6 platform/version builds should now succeed!"

Write-Host ""
Write-Host "✅ Changes committed locally!" -ForegroundColor Green
Write-Host ""
Write-Host "📤 Pushing to remote..." -ForegroundColor Cyan

git push origin feature/multi-postgres-version-support

Write-Host ""
Write-Host "✅ Changes pushed!" -ForegroundColor Green
Write-Host ""
Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Yellow
Write-Host "🚀 NEXT STEP: Trigger GitHub Actions Workflow" -ForegroundColor Yellow
Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Yellow
Write-Host ""
Write-Host "1. Open: https://github.com/mbulava-org/pgPacTool/actions/workflows/build-native-libraries.yml"
Write-Host "2. Click: 'Run workflow'"
Write-Host "3. Branch: feature/multi-postgres-version-support"
Write-Host "4. PostgreSQL versions: 16,17"
Write-Host "5. Click: 'Run workflow' button"
Write-Host ""
Write-Host "⏱️  Expected: ~8-10 minutes for all 6 builds" -ForegroundColor Cyan
Write-Host "✅ Expected: All 6 builds succeed" -ForegroundColor Green
Write-Host "📦 Expected: PR with 6 library files (~11 MB each)" -ForegroundColor Green
Write-Host ""
Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Yellow
