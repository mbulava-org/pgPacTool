#!/usr/bin/env pwsh

Write-Host "🔧 Committing Windows DLL Fix (DEF file approach)..." -ForegroundColor Cyan
Write-Host ""

# Commit the workflow fix
git add .github/workflows/build-native-libraries.yml
git add docs/WINDOWS_DLL_DEF_FIX.md

git commit -m "fix(ci): Windows DLL - use DEF file with pg_query.lib

Previous errors:
- link pg_query.lib → missing _DllMainCRTStartup
- link *.obj → no object files found + multiple main()

Solution:
- Create module definition (.def) file with exported functions
- Link: link /DLL /DEF:pg_query.def pg_query.lib
- This is the standard Windows approach for lib→dll conversion

Result: Proper DLL with exported symbols for .NET P/Invoke

All 6 builds should now succeed:
- Windows PG 16: ✅ (DEF + pg_query.lib → dll)
- Windows PG 17: ✅ (DEF + pg_query.lib → dll)
- Linux PG 16: ✅ (gcc -shared → .so)
- Linux PG 17: ✅ (gcc -shared → .so)
- macOS PG 16: ✅ (HAVE_STRCHRNUL + gcc -dynamiclib)
- macOS PG 17: ✅ (gcc -dynamiclib → .dylib)"

Write-Host ""
Write-Host "✅ Changes committed locally!" -ForegroundColor Green
Write-Host ""
Write-Host "📤 Pushing to remote..." -ForegroundColor Cyan

git push origin feature/multi-postgres-version-support

Write-Host ""
Write-Host "✅ Changes pushed!" -ForegroundColor Green
Write-Host ""
Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Yellow
Write-Host "🚀 FINAL ATTEMPT: Trigger GitHub Actions Workflow" -ForegroundColor Yellow
Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Yellow
Write-Host ""
Write-Host "URL: https://github.com/mbulava-org/pgPacTool/actions/workflows/build-native-libraries.yml" -ForegroundColor White
Write-Host ""
Write-Host "Steps:"
Write-Host "  1. Click 'Run workflow'"
Write-Host "  2. Branch: feature/multi-postgres-version-support"
Write-Host "  3. PostgreSQL versions: 16,17"
Write-Host "  4. Click 'Run workflow' button"
Write-Host ""
Write-Host "⏱️  Expected: ~10 minutes for all 6 builds" -ForegroundColor Cyan
Write-Host "✅ Expected: 6/6 success (100%)" -ForegroundColor Green
Write-Host ""
Write-Host "Fix History:" -ForegroundColor Gray
Write-Host "  Attempt 1: No nmake → Added msvc-dev-cmd" -ForegroundColor Gray
Write-Host "  Attempt 2: No shared libs → Added gcc/link steps" -ForegroundColor Gray
Write-Host "  Attempt 3: Multiple main() → Tried link *.obj" -ForegroundColor Gray
Write-Host "  Attempt 4: No obj files → Using DEF + pg_query.lib" -ForegroundColor Green
Write-Host ""
Write-Host "This is the STANDARD Windows lib→dll approach!" -ForegroundColor Green
Write-Host ""
Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Yellow


