# GitHub Issues - Ready to Create

**Generated:** 2026-03-01  
**Status:** ✅ Ready for GitHub  
**Templates Location:** `.github/ISSUE_TEMPLATE/`

---

## 🔴 Issue #1: Critical - Protobuf Deparse Corruption on Linux

### Create Issue
**Template:** `.github/ISSUE_TEMPLATE/protobuf-deparse-corruption.md`  
**URL:** https://github.com/mbulava-org/pgPacTool/issues/new

### Title
```
[BUG] Protobuf deparse returns corrupted output on Linux
```

### Labels
```
bug, critical, linux, native-interop, protobuf, ci-cd
```

### Quick Summary
The `pg_query_deparse_protobuf` native function returns binary garbage instead of SQL on Linux (Ubuntu 24.04), blocking all GitHub Actions CI/CD builds. Returns `\u0012\u0006PUBLIC` instead of `ALTER TABLE public.users...`.

### Status
- ✅ Root cause identified: Cross-platform protobuf serialization issue
- ✅ Workaround implemented: JSON-based SQL extraction
- ⏳ Needs: GrantStmt/RevokeStmt support (Issue #2)

### Priority
🔴 **CRITICAL** - Blocks Linux CI/CD

### Assignee
Suggest: Maintainer who handles native interop

---

## 🟡 Issue #2: Enhancement - Add GrantStmt/RevokeStmt Support

### Create Issue
**Template:** `.github/ISSUE_TEMPLATE/grant-revoke-support.md`  
**URL:** https://github.com/mbulava-org/pgPacTool/issues/new

### Title
```
[FEATURE] Add GrantStmt and RevokeStmt support to JSON SQL extractor
```

### Labels
```
enhancement, json-extractor, sql-generation, privileges
```

### Quick Summary
The JSON-based SQL extractor (workaround for Issue #1) needs GrantStmt and RevokeStmt support to pass all PublishScriptGeneratorTests. Implementation code is provided in the issue template.

### Status
- ✅ Implementation ready (code provided)
- ✅ AST structure documented
- ✅ Test cases identified
- ⏳ Just needs to be applied and tested

### Priority
🟡 **HIGH** - Blocks completion of Issue #1

### Dependencies
- **Blocks:** Issue #1 resolution
- **Required for:** Complete privilege change script generation

### Effort Estimate
~2 hours (implementation provided, just needs testing)

---

## 📝 Quick Copy-Paste for GitHub

### Issue #1 - Copy/Paste Body

```markdown
## Description
The `pg_query_deparse_protobuf` native function returns corrupted binary data instead of SQL text when running on Linux (Ubuntu 24.04).

## Environment
- OS: Ubuntu 24.04
- Runtime: linux-x64
- Framework: .NET 10
- Native Library: libpg_query.so

## Expected vs Actual
**Expected:** `ALTER TABLE public.users DROP COLUMN IF EXISTS old_column;`  
**Actual:** `DROP TABLESPACE IF EXISTS \u0012\u0006PUBLIC\u001a\u0005USERS \u0001*\u0001P`

## Root Cause
Cross-platform protobuf serialization incompatibility between C# Google.Protobuf 3.33.5 and native libpg_query protobuf-c.

## Workaround Implemented
Modified `AstSqlGenerator.Generate(JsonElement)` to use JSON extraction instead of protobuf deparse.

## Remaining Work
Add GrantStmt/RevokeStmt support to JSON extractor (see Issue #2)

## Documentation
- `docs/KNOWN_ISSUES_PROTOBUF.md`
- `docs/GITHUB_ISSUES_SUMMARY.md`
```

### Issue #2 - Copy/Paste Body

```markdown
## Description
Add GrantStmt and RevokeStmt support to the JSON-based SQL extractor to complete the workaround for the protobuf corruption issue on Linux.

## Current Support
- ✅ AlterTableStmt (DROP COLUMN, ADD COLUMN, ALTER COLUMN TYPE)
- ✅ DropStmt (DROP TABLE, DROP FUNCTION, etc.)
- ❌ GrantStmt (GRANT privileges) - **Missing**
- ❌ RevokeStmt (REVOKE privileges) - **Missing**

## Implementation
Complete code implementation provided in issue template. 

**File to modify:** `src/libs/mbulava.PostgreSql.Dac/Compile/Ast/AstSqlGenerator.cs`

## Testing
Run: `dotnet test --filter PublishScriptGeneratorTests`

## Dependencies
Blocks resolution of Issue #1 (protobuf corruption)

## Documentation
- `.github/ISSUE_TEMPLATE/grant-revoke-support.md` (full implementation)
- `docs/KNOWN_ISSUES_PROTOBUF.md`
```

---

## 🔗 Issue Relationships

```
Issue #1: Protobuf Deparse Corruption (CRITICAL)
    ↓
    Requires
    ↓
Issue #2: Add GrantStmt/RevokeStmt Support (HIGH)
    ↓
    Enables
    ↓
Complete fix for Linux CI/CD failures
```

---

## ✅ Verification Steps

### After Creating Issues

1. **Label them properly** (critical, enhancement, etc.)
2. **Link them** (Issue #2 should reference Issue #1)
3. **Assign milestone** (e.g., v0.2.0 or "Linux Support")
4. **Add to project board** (if using GitHub Projects)

### After Implementing Issue #2

1. Apply code from `.github/ISSUE_TEMPLATE/grant-revoke-support.md`
2. Run tests:
   ```bash
   dotnet test --filter PublishScriptGeneratorTests
   ```
3. Run Linux container tests:
   ```bash
   dotnet test tests/LinuxContainer.Tests --filter "ProtobufDeparse"
   ```
4. Push to GitHub and verify Actions pass
5. Close both Issue #1 and Issue #2

---

## 📦 Deliverables Checklist

- [x] Comprehensive issue documentation
- [x] GitHub issue templates
- [x] Implementation code (Issue #2)
- [x] Test cases identified
- [x] Linux container tests working
- [x] Build passing
- [x] Quick reference guides
- [ ] GitHub issues created (manual step)
- [ ] GrantStmt/RevokeStmt code applied (manual step)
- [ ] Linux CI passing (after fixes applied)

---

## 🎓 Lessons Learned

### Testcontainers 4.x API Changes
1. `container.Name` only available after `StartAsync()`
2. `GetLogsAsync()` requires DateTime parameters
3. `InspectAsync()` removed - use `GetExitCodeAsync()` instead
4. Exit code is `long`, needs cast to `int`

### Multi-Version Native Library
1. Module initializer shouldn't pre-load libraries
2. Libraries loaded on-demand per PostgreSQL version
3. `NativeLibraryLoader.GetLibraryHandle(version)` is the new API

---

## 📞 Support

**Documentation:**
- Issue Details: `docs/KNOWN_ISSUES_PROTOBUF.md`
- Test Guide: `tests/LinuxContainer.Tests/QUICKSTART.md`
- Summary: `docs/GITHUB_ISSUES_SUMMARY.md`

**Questions:**
- Create GitHub Discussion
- Or add comments to the issues after creation

---

**Status:** ✅ READY FOR GITHUB ISSUE CREATION  
**Next Action:** Create Issue #1 and #2 on GitHub  
**Blocker:** None - All prep work complete!

🎯 **Go create those issues!** 🚀
