# Feature: Complete JSON-to-SQL Extractor

**Branch**: `feature/complete-json-sql-extractor`  
**Date**: March 3, 2025  
**Goal**: Fix all 38 failing tests by completing JSON-to-SQL extractor

---

## 🎯 Objective

Complete the JSON-to-SQL extractor in `AstSqlGenerator.cs` to support all PostgreSQL statement types needed by tests.

**Current Status**: 38 tests failing with "statement type not yet supported"  
**Target**: All 38 tests passing

---

## 📋 Task Breakdown

### Phase 1: Analyze Failing Tests (30 min)
- [ ] Run all failing tests and capture error details
- [ ] Group by statement type
- [ ] Identify AST patterns for each type
- [ ] Prioritize by frequency

### Phase 2: Implement Statement Types (3-4 hours)
- [ ] ALTER TABLE variants
  - [ ] ALTER COLUMN with DEFAULT
  - [ ] ALTER COLUMN with NOT NULL
  - [ ] ADD CONSTRAINT
  - [ ] ALTER COLUMN TYPE
  - [ ] DROP CONSTRAINT
  - [ ] ALTER OWNER
- [ ] CREATE statements
  - [ ] CREATE TABLE (simple)
  - [ ] CREATE INDEX with options
  - [ ] CREATE INDEX with UNIQUE
  - [ ] CREATE INDEX multi-column
- [ ] GRANT/REVOKE
  - [ ] GRANT statement
  - [ ] REVOKE statement
- [ ] COMMENT ON
- [ ] DROP TRIGGER
- [ ] Complex scenarios
  - [ ] Round-trip scenarios

### Phase 3: Testing & Validation (1-2 hours)
- [ ] Run all 38 failing tests
- [ ] Verify they all pass
- [ ] Check for regressions in passing tests
- [ ] Update code coverage

### Phase 4: Documentation (30 min)
- [ ] Update ISSUE_36_STATUS.md
- [ ] Update OPEN_ISSUES.md
- [ ] Add inline documentation
- [ ] Create PR description

---

## 🔍 Analysis: What Needs Implementation

### From Previous Analysis

**Failed Tests** (sample from Issue #36 and test runs):
1. `AlterTableAddColumn_WithDefault_GeneratesValidSQL`
2. `AlterTableAddColumn_WithNotNull_GeneratesValidSQL`
3. `AlterTableAddConstraint_GeneratesValidSQL`
4. `AlterTableAlterColumnDropDefault_GeneratesValidSQL`
5. `AlterTableAlterColumnSetDefault_GeneratesValidSQL`
6. `AlterTableAlterColumnType_GeneratesValidSQL`
7. `AlterTableDropConstraint_GeneratesValidSQL`
8. `AlterTableOwner_GeneratesValidSQL`
9. `CommentOn_GeneratesValidSQL`
10. `CreateIndex_GeneratesValidSQL`
11. `CreateIndex_WithMultipleColumns_GeneratesValidSQL`
12. `CreateIndex_WithUnique_GeneratesValidSQL`
13. `CreateTableSimple_GeneratesValidSQL`
14. `DropTrigger_GeneratesValidSQL`
15. `Grant_GeneratesValidSQL`
16. `Revoke_GeneratesValidSQL`
17. `RoundTrip_ComplexScenario_PreservesSemantics`
... and 21 more

---

## 🛠️ Implementation Approach

### File to Modify
`src/libs/mbulava.PostgreSql.Dac/Compile/Ast/AstSqlGenerator.cs`

### Strategy
1. **Analyze AST structure** for each failing test
2. **Add pattern matching** in `TryExtractSqlFromAstJson`
3. **Create helper methods** for each statement type
4. **Test incrementally** after each implementation

### Example Pattern

```csharp
// In TryExtractSqlFromAstJson
if (stmtElement.TryGetProperty("GrantStmt", out var grantStmt))
{
    return GenerateSqlFromGrantStmt(grantStmt);
}

// New helper method
private static string? GenerateSqlFromGrantStmt(JsonElement grantStmt)
{
    // Extract grant details from AST
    // Reconstruct SQL: GRANT privileges ON object TO role
    return sql;
}
```

---

## ✅ Success Criteria

1. **All 38 tests pass** ✅
2. **No regressions** in previously passing tests
3. **Code coverage** maintained or improved
4. **Clean, maintainable code** with good documentation
5. **Issue #36** can be closed

---

## 📊 Estimated Effort

| Phase | Time | Complexity |
|-------|------|------------|
| Analysis | 30 min | Low |
| Implementation | 3-4 hours | Medium |
| Testing | 1-2 hours | Low |
| Documentation | 30 min | Low |
| **Total** | **5-7 hours** | **Medium** |

---

## 🚀 Getting Started

### Step 1: Run Failing Tests
```powershell
dotnet test tests\mbulava.PostgreSql.Dac.Tests\mbulava.PostgreSql.Dac.Tests.csproj `
  --no-build --verbosity detailed `
  2>&1 | Select-String "Failed" -Context 2,1
```

### Step 2: Analyze One Test
```powershell
# Pick one test to start with
dotnet test tests\mbulava.PostgreSql.Dac.Tests\mbulava.PostgreSql.Dac.Tests.csproj `
  --filter "CreateTableSimple_GeneratesValidSQL" `
  --verbosity detailed
```

### Step 3: Parse SQL and Examine AST
```csharp
using var parser = new Parser();
var sql = "CREATE TABLE users (id INT PRIMARY KEY)";
var result = parser.Parse(sql);
var json = result.ParseTree.RootElement.GetRawText();
Console.WriteLine(json); // Examine structure
```

---

## 📝 Notes

- All changes in one file: `AstSqlGenerator.cs`
- Protobuf deparse stays disabled
- JSON extraction is the only path
- Each statement type is independent
- Can implement incrementally

---

*Created*: March 3, 2025  
*Branch*: feature/complete-json-sql-extractor  
*Status*: Ready to start
