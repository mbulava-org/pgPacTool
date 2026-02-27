# Issue: Refactor PublishScriptGenerator to Use AST-Based SQL Generation

## 🐛 Problem

The `PublishScriptGenerator` currently generates SQL via string concatenation, which is:

1. **Error-prone** - Typos and syntax errors not caught at compile-time
2. **Inconsistent** - Doesn't align with our AST-based extraction approach
3. **Hard to maintain** - Scattered SQL string building logic
4. **Lacks validation** - No guarantee of syntactically correct SQL
5. **Fragile** - Identifier quoting bugs and edge cases

### Current Code Example
```csharp
// ❌ String concatenation (current approach)
sb.AppendLine($"ALTER TABLE {QuoteIdentifier(diff.TableName)} ALTER COLUMN {QuoteIdentifier(colDiff.ColumnName)} TYPE {colDiff.SourceDataType};");
```

## ✅ Proposed Solution

Refactor to use AST-based SQL generation with Npgquery/pg_query deparser:

```csharp
// ✅ AST-based (proposed approach)
var builder = new AlterTableBuilder("public", "users")
    .AlterColumnType("username", "VARCHAR(100)")
    .SetNotNull("username");
    
var stmt = builder.Build();
string sql = AstSqlGenerator.Deparse(stmt);
```

## 📋 Implementation Plan

### Phase 1: Infrastructure
- [ ] Create `IAstSqlGenerator` interface
- [ ] Implement pg_query deparser wrapper
- [ ] Add unit tests for basic deparse operations
- [ ] Document deparser API

### Phase 2: Statement Builders
- [ ] `AlterTableBuilder` (columns, constraints)
- [ ] `CreateTableBuilder`
- [ ] `CreateViewBuilder`
- [ ] `CreateFunctionBuilder`
- [ ] `CreateTriggerBuilder`
- [ ] `DropStatementBuilder`
- [ ] `GrantRevokeBuilder` (privileges)

### Phase 3: Integration
- [ ] Refactor `GenerateTableScripts()` to use builders
- [ ] Refactor `GenerateViewScripts()` to use builders
- [ ] Refactor `GenerateFunctionScripts()` to use builders
- [ ] Refactor `GenerateSequenceScripts()` to use builders
- [ ] Refactor `GenerateTypeScripts()` to use builders
- [ ] Refactor `GenerateTriggerScripts()` to use builders

### Phase 4: Testing & Validation
- [ ] Update existing unit tests
- [ ] Add AST-based integration tests
- [ ] Performance benchmarking
- [ ] Validate generated SQL against real databases

## 🎯 Benefits

| Aspect | Before (String) | After (AST) |
|--------|----------------|-------------|
| **Type Safety** | ❌ Runtime errors | ✅ Compile-time checking |
| **Correctness** | ❌ Manual validation | ✅ Guaranteed valid SQL |
| **Maintainability** | ❌ Scattered logic | ✅ Clean, fluent API |
| **Testability** | ❌ Hard to mock | ✅ Easy unit testing |
| **Consistency** | ❌ Different approach from extraction | ✅ Same AST approach |

## 📊 Scope

**Files to Modify:**
- `src/libs/mbulava.PostgreSql.Dac/Compare/PublishScriptGenerator.cs` (~600 lines)
- Add: `src/libs/mbulava.PostgreSql.Dac/Ast/Builders/*.cs` (new)
- Add: `src/libs/mbulava.PostgreSql.Dac/Ast/IAstSqlGenerator.cs` (new)

**Estimated Effort:** 2-3 weeks

**Breaking Changes:** None (API remains the same, implementation changes)

## 🔗 Related Issues

- Extraction uses AST parsing (#extraction-ast)
- Identifier quoting bugs (#quoting-issues)
- SQL syntax validation (#validation)

## 📚 References

- [Architecture Plan](docs/architecture/AST_SQL_GENERATION_PLAN.md)
- [pg_query Documentation](https://github.com/pganalyze/pg_query)
- [Npgquery Library](https://github.com/launchbadge/pg_query.net)

## 🚦 Priority

**HIGH** - This is a fundamental architectural issue that should be addressed before v1.0 release.

## 👥 Acceptance Criteria

- [ ] All SQL generation uses AST objects
- [ ] No string concatenation for SQL (except comments)
- [ ] All existing tests pass
- [ ] Generated SQL is validated against PostgreSQL
- [ ] Documentation updated
- [ ] Performance is equal or better than current approach

## 📝 Notes

- Start with simple object types (sequences, types)
- Migrate one object type at a time
- Keep existing code working during migration
- Can be done incrementally across multiple PRs
