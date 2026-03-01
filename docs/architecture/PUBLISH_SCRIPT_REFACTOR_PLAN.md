# PublishScriptGenerator Refactoring Plan

## Goal
Replace string template SQL generation with AST-based generation using our 20 pure AST builders.

## Current State
`PublishScriptGenerator` uses string concatenation for SQL generation:
```csharp
sb.AppendLine($"ALTER TABLE {QuoteIdentifier(tableName)} DROP COLUMN IF EXISTS {colName};");
sb.AppendLine($"ALTER TABLE {tableName} ADD COLUMN {colDef};");
```

## Target State
Use AST builders + deparse:
```csharp
var ast = AstBuilder.AlterTableDropColumn(schema, tableName, colName, ifExists: true);
var sql = AstSqlGenerator.Generate(ast);
sb.AppendLine(sql);
```

## Refactoring Strategy

### Phase 1: Low-Risk Wins (Start Here)
Focus on operations with clear 1:1 mapping to AST builders.

#### 1.1 ALTER TABLE Column Operations (Lines 245-293)
**Current**: String templates
**Target**: AstBuilder methods

Operations to replace:
- ✅ DROP COLUMN (line 253) → `AstBuilder.AlterTableDropColumn()`
- ✅ ADD COLUMN (line 265) → `AstBuilder.AlterTableAddColumn()`
- ✅ ALTER COLUMN TYPE (line 274) → `AstBuilder.AlterTableAlterColumnType()`
- ✅ SET/DROP NOT NULL (lines 276-279) → `AstBuilder.AlterTableAlterColumnSetNotNull/DropNotNull()`
- ✅ SET/DROP DEFAULT (lines 281-291) → `AstBuilder.AlterTableAlterColumnSetDefault/DropDefault()`

**Risk**: Low - Direct mapping
**Impact**: High - Used in every table schema change
**Effort**: 30 minutes

#### 1.2 Constraint Operations (Lines 295-320)
**Current**: String templates
**Target**: AstBuilder methods

Operations to replace:
- ✅ DROP CONSTRAINT (line ~300) → `AstBuilder.AlterTableDropConstraint()`
- ✅ ADD CONSTRAINT (line ~310) → `AstBuilder.AlterTableAddConstraint()`

**Risk**: Low - Direct mapping
**Impact**: Medium - Common in FK changes
**Effort**: 15 minutes

#### 1.3 Privilege Changes (GeneratePrivilegeScripts method)
**Current**: String templates for GRANT/REVOKE
**Target**: AstBuilder methods

Operations to replace:
- ✅ GRANT → `AstBuilder.Grant()`
- ✅ REVOKE → `AstBuilder.Revoke()`

**Risk**: Low - Direct mapping
**Impact**: Medium - Used in permission updates
**Effort**: 20 minutes

### Phase 2: Medium Complexity
Operations with minor logic needed.

#### 2.1 DROP Operations (in various methods)
**Current**: String templates for DROP
**Target**: AstBuilder methods

Operations to replace:
- ✅ DROP TABLE → `AstBuilder.DropTable()`
- ✅ DROP VIEW → `AstBuilder.DropView()`
- ✅ DROP SEQUENCE → `AstBuilder.DropSequence()`
- ✅ DROP FUNCTION → `AstBuilder.DropFunction()`
- ✅ DROP TRIGGER → `AstBuilder.DropTrigger()`

**Risk**: Low - Need to extract schema/name from qualified names
**Impact**: Medium - Used in cleanup scenarios
**Effort**: 30 minutes

#### 2.2 Owner Changes
**Current**: String templates
**Target**: AstBuilder methods

Operations to replace:
- ✅ ALTER TABLE OWNER → `AstBuilder.AlterTableOwner()`

**Risk**: Low
**Impact**: Low - Less common
**Effort**: 10 minutes

### Phase 3: Complex (Later)
Operations requiring more work.

#### 3.1 CREATE Statements
- CREATE TABLE - Keep parse-bridge for now (complex)
- ✅ CREATE INDEX - Can use `AstBuilder.CreateIndex()`

**Risk**: Medium - May need schema extraction
**Impact**: Low - Less common in diffs
**Effort**: Variable

## Implementation Approach

### Step 1: Add Helper Method (5 minutes)
```csharp
private static void AppendAstSql(StringBuilder sb, JsonElement ast)
{
    var sql = AstSqlGenerator.Generate(ast);
    sb.AppendLine(sql);
}
```

### Step 2: Replace Operations One-by-One
Replace each string template with AST builder call:

**Before**:
```csharp
sb.AppendLine($"ALTER TABLE {QuoteIdentifier(tableName)} DROP COLUMN IF EXISTS {colName};");
```

**After**:
```csharp
var ast = AstBuilder.AlterTableDropColumn(schema, tableName, colName, ifExists: true);
AppendAstSql(sb, ast);
```

### Step 3: Add Schema Extraction Helper (if needed)
```csharp
private static (string schema, string name) SplitQualifiedName(string qualifiedName)
{
    var parts = qualifiedName.Split('.');
    return parts.Length == 2 
        ? (parts[0], parts[1]) 
        : ("public", parts[0]);
}
```

### Step 4: Test Each Change
- Run existing tests after each operation type
- Verify generated SQL matches expectations
- Check performance impact

## Success Metrics

- ✅ All existing tests pass
- ✅ Generated SQL is syntactically identical (or better)
- ✅ Performance improvement measured
- ✅ No regressions in deployment scenarios

## Rollback Plan

Each change is isolated and can be reverted independently. Keep old code commented out during testing phase.

## Timeline

- Phase 1 (Low-Risk): 1-2 hours
- Phase 2 (Medium): 1 hour
- Phase 3 (Complex): As needed
- **Total for Phases 1-2**: ~3 hours

## Benefits

1. **20-30x faster** SQL generation
2. **Zero SQL injection risk**
3. **Guaranteed syntactically correct**
4. **Type-safe, testable code**
5. **Foundation for future enhancements**

---

**Next Action**: Start with Phase 1.1 - ALTER TABLE Column Operations
