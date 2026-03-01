# GitHub Issues to Create

**Date:** 2026-03-01  
**Context:** AST SQL Generation & Linux Protobuf Issues  
**Branch:** feature/AST_BASED_COMPILATION

---

## Issue #1: Protobuf Deparse Corruption on Linux

**Title:** `[BUG] Protobuf deparse returns corrupted output on Linux`

**Labels:** `bug`, `critical`, `linux`, `native-interop`, `protobuf`, `ci-cd`

**Priority:** 🔴 CRITICAL

**Summary:**  
The `pg_query_deparse_protobuf` native function returns binary garbage instead of SQL text on Linux (Ubuntu 24.04), blocking all GitHub Actions CI/CD builds.

**Quick Facts:**
- **Environment:** Ubuntu 24.04, .NET 10, libpg_query.so
- **Expected:** `ALTER TABLE public.users DROP COLUMN old_column;`
- **Actual:** `\u0012\u0006PUBLIC\u001a\u0005USERS \u0001*\u0001P` (protobuf bytes)
- **Root Cause:** Cross-platform protobuf serialization incompatibility
- **Workaround:** JSON-based SQL extraction (partially implemented)
- **Status:** Fix in progress, needs GrantStmt/RevokeStmt support

**Failed Tests:**
- `Generate_ColumnTypeChange_CreatesAlterType`
- `Generate_DropColumn_WithDropFlag_CreatesDropColumn`
- `Generate_PrivilegeChanges_CreatesGrantRevoke`

**Template:** `.github/ISSUE_TEMPLATE/protobuf-deparse-corruption.md`

**Create Issue URL:**
```
https://github.com/mbulava-org/pgPacTool/issues/new?template=protobuf-deparse-corruption.md
```

---

## Issue #2: Add GrantStmt/RevokeStmt Support to JSON SQL Extractor

**Title:** `[FEATURE] Add GrantStmt and RevokeStmt support to JSON SQL extractor`

**Labels:** `enhancement`, `json-extractor`, `sql-generation`, `privileges`

**Priority:** 🟡 HIGH

**Summary:**  
The JSON-based SQL extractor needs GrantStmt and RevokeStmt support to complete the workaround for Issue #1 (protobuf corruption).

**Quick Facts:**
- **Currently Supported:** AlterTableStmt, DropStmt
- **Missing:** GrantStmt, RevokeStmt
- **Impact:** 3 test failures in PublishScriptGeneratorTests
- **Effort:** ~2 hours (implementation provided in issue template)

**Implementation Ready:**
- Code snippets provided in issue template
- AST structure documented
- Test cases identified
- Just needs to be applied and tested

**Dependencies:**
- Blocks completion of Issue #1

**Template:** `.github/ISSUE_TEMPLATE/grant-revoke-support.md`

**Create Issue URL:**
```
https://github.com/mbulava-org/pgPacTool/issues/new?template=grant-revoke-support.md
```

---

## Additional Documentation Created

### 1. Comprehensive Issue Documentation
**File:** `docs/KNOWN_ISSUES_PROTOBUF.md`

Contains:
- Detailed root cause analysis
- Symptoms and error messages
- Affected code paths
- AST structure examples
- Solution implementation details
- Testing guidelines
- Build log references

### 2. GitHub Issue Templates
**Files:**
- `.github/ISSUE_TEMPLATE/protobuf-deparse-corruption.md`
- `.github/ISSUE_TEMPLATE/grant-revoke-support.md`

Both templates include:
- Structured problem description
- Environment details
- Reproduction steps
- Implementation code
- Related files
- Priority and labels

---

## Next Steps

### 1. Create GitHub Issues
1. Navigate to: https://github.com/mbulava-org/pgPacTool/issues/new/choose
2. Select template: "Protobuf Deparse Corruption on Linux"
3. Review and create Issue #1
4. Select template: "Add GrantStmt/RevokeStmt Support"
5. Review and create Issue #2

### 2. Complete Implementation (Issue #2)
Apply the code from `.github/ISSUE_TEMPLATE/grant-revoke-support.md`:

**File:** `src/libs/mbulava.PostgreSql.Dac/Compile/Ast/AstSqlGenerator.cs`

1. Add GrantStmt/RevokeStmt checks in `TryExtractSqlFromAstJson` (~line 96)
2. Add `GenerateSqlFromGrantStmt` method (~line 230)
3. Add `GenerateSqlFromRevokeStmt` method
4. Run tests: `dotnet test --filter PublishScriptGeneratorTests`
5. Verify all 17 tests pass

### 3. Verify on Linux
1. Push changes to GitHub
2. Trigger GitHub Actions workflow
3. Verify Linux build succeeds
4. Close Issue #1 and Issue #2

### 4. Update Documentation
- Mark issues as resolved in `docs/KNOWN_ISSUES_PROTOBUF.md`
- Add "RESOLVED" badges
- Document the JSON extraction pattern for future statement types

---

## Files Changed Summary

### New Files Created:
```
✅ docs/KNOWN_ISSUES_PROTOBUF.md
✅ .github/ISSUE_TEMPLATE/protobuf-deparse-corruption.md
✅ .github/ISSUE_TEMPLATE/grant-revoke-support.md
✅ docs/GITHUB_ISSUES_SUMMARY.md (this file)
```

### Files To Be Modified:
```
⏳ src/libs/mbulava.PostgreSql.Dac/Compile/Ast/AstSqlGenerator.cs
   - Add GrantStmt check (~line 96)
   - Add RevokeStmt check (~line 96)
   - Add GenerateSqlFromGrantStmt method (~line 230)
   - Add GenerateSqlFromRevokeStmt method (~line 240)
```

### Already Modified:
```
✅ src/libs/mbulava.PostgreSql.Dac/Compile/Ast/AstSqlGenerator.cs
   - Generate(JsonElement) now delegates to Generate(JsonDocument)
   - Uses JSON extraction instead of broken protobuf deparse
```

---

## Quick Command Reference

### Run Specific Tests
```bash
# Run all PublishScriptGeneratorTests
dotnet test --filter "FullyQualifiedName~PublishScriptGeneratorTests"

# Run privilege test specifically
dotnet test --filter "Generate_PrivilegeChanges_CreatesGrantRevoke"
```

### Check Native Library
```bash
# Verify Linux library exists
ls -lh src/libs/Npgquery/Npgquery/runtimes/linux-x64/native/libpg_query.so

# Check file size (should be ~9.3 MB)
du -h src/libs/Npgquery/Npgquery/runtimes/linux-x64/native/libpg_query.so
```

### View Build Logs
```bash
# Local build logs
cat build-logs/0_Build\ and\ Test\ \(.NET\ 10\).txt | grep -A 10 "Failed"

# GitHub Actions logs
# Navigate to: https://github.com/mbulava-org/pgPacTool/actions
```

---

## Contact & Support

**Documentation Location:**
- Main: `docs/KNOWN_ISSUES_PROTOBUF.md`
- Templates: `.github/ISSUE_TEMPLATE/`

**Related Branch:**
- `feature/AST_BASED_COMPILATION`

**CI/CD:**
- GitHub Actions: `.github/workflows/`

---

**Document Version:** 1.0  
**Created:** 2026-03-01  
**Status:** Ready for GitHub issue creation
