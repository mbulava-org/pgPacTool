# PostgreSQL 17 Changes from 16

> **Generated**: Analysis Date
> **Source**: libpg_query repository
> **Branches**: 16-latest vs 17-latest

## Summary

⚠️ **BREAKING CHANGES DETECTED**

PostgreSQL 17 introduces significant changes from version 16, particularly in:
- JSON/SQL functionality (major additions)
- MERGE statement enhancements
- Window function improvements
- New utility functions

## Protobuf Schema Changes

### New Message Types (19 total)

**JSON Functionality** (Primary Addition):
- `JsonBehavior` - JSON error handling behavior
- `JsonExpr` - JSON expressions
- `JsonFuncExpr` - JSON function expressions
- `JsonTablePath` - JSON_TABLE path definitions
- `JsonTablePathScan` - JSON_TABLE scanning
- `JsonTableSiblingJoin` - JSON_TABLE joins
- `JsonTable` - JSON_TABLE statement
- `JsonTableColumn` - JSON_TABLE column definitions
- `JsonTablePathSpec` - JSON_TABLE path specifications
- `JsonArgument` - JSON function arguments
- `JsonParseExpr` - JSON parsing expressions
- `JsonScalarExpr` - JSON scalar expressions
- `JsonSerializeExpr` - JSON serialization

**MERGE Statement**:
- `MergeAction` - Enhanced MERGE functionality (redefined)
- `MergeSupportFunc` - MERGE support functions

**Window Functions**:
- `WindowFuncRunCondition` - Window function run conditions
- `SinglePartitionSpec` - Single partition specifications

**Other**:
- `SummaryResult` - Query summary results
- `Table` - Table metadata
- `Function` - Function metadata
- `FilterColumn` - Column filtering

### Field Changes

- **Added**: 723 fields across various message types
- **Removed**: 519 fields (mostly refactoring/renaming)

**Impact**: Parse trees from PG 17 will have significantly different structure for:
- Any JSON/SQL operations
- MERGE statements
- Window functions with new features

## C API Changes

### New Functions (7 total)

```c
// Deparse with options
PgQueryDeparseResult pg_query_deparse_protobuf_opts(PgQueryProtobuf parse_tree, PgQueryDeparseOpts opts);

// Comment extraction
PgQueryDeparseCommentsResult pg_query_deparse_comments_for_query(const char *input);
void pg_query_free_deparse_comments_result(PgQueryDeparseCommentsResult result);

// Utility statement detection
PgQueryIsUtilityResult pg_query_is_utility_stmt(const char *input);
void pg_query_free_is_utility_result(PgQueryIsUtilityResult result);

// Query summary
PgQuerySummaryParseResult pg_query_summary(const char *input);
void pg_query_free_summary_parse_result(PgQuerySummaryParseResult result);
```

### Structure Changes

- 9 structure definitions modified
- New result types for utility functions
- Enhanced deparse options

## Node Type Changes

- **Lines Added**: 1,585
- **Lines Removed**: 825
- **Net Change**: +760 lines

**Major Areas**:
- JSON node definitions (largest addition)
- MERGE statement nodes (enhanced)
- Window function nodes (extended)

## Breaking Changes Catalog

### 1. JSON_TABLE Feature (PG 17+)

**Impact**: ⚠️ HIGH

Queries using `JSON_TABLE` will:
- ✅ **PG 17**: Parse successfully with full JSON_TABLE AST nodes
- ❌ **PG 16**: Fail to parse (syntax error)

**Example**:
```sql
-- Works in PG 17, fails in PG 16
SELECT * FROM JSON_TABLE(
    '{"a":1}', 
    '$' COLUMNS(id int PATH '$.a')
);
```

**Required Action**:
- Add version check before using JSON_TABLE
- Create PG 17-specific model classes for JSON nodes
- Add compatibility layer to detect and handle gracefully

### 2. Enhanced MERGE Statement

**Impact**: ⚠️ MEDIUM

MERGE statements with new PG 17 features will parse differently.

**Required Action**:
- Update `MergeAction` handling
- Add version-specific MERGE tests

### 3. Window Function Enhancements

**Impact**: ⚠️ LOW

New window function features available in PG 17.

**Required Action**:
- Document as PG 17+ feature
- Test window functions across versions

### 4. New API Functions

**Impact**: ⚠️ MEDIUM

New C API functions not available in PG 16:
- `pg_query_deparse_protobuf_opts` - Enhanced deparse
- `pg_query_deparse_comments_for_query` - Comment extraction
- `pg_query_is_utility_stmt` - Utility detection
- `pg_query_summary` - Query summary

**Required Action**:
- Do NOT expose these in Npgquery unless version checked
- Document as PG 17+ features
- Consider adding wrapper with version detection

### 5. Protobuf Field Changes

**Impact**: ⚠️ HIGH

723 fields added, 519 removed means:
- Parse tree structure significantly different
- Field names/paths may have changed
- Serialization/deserialization affected

**Required Action**:
- **CRITICAL**: Test protobuf serialization round-trips
- Create version-specific deserialization if needed
- Add compatibility shims for common operations

## Compatibility Matrix

| Feature | PG 16 | PG 17 | Compatible? |
|---------|-------|-------|-------------|
| Basic SELECT/INSERT/UPDATE/DELETE | ✅ | ✅ | ✅ Yes |
| CREATE TABLE | ✅ | ✅ | ✅ Yes |
| JSON_TABLE | ❌ | ✅ | ❌ No (PG 17+) |
| Enhanced MERGE | ⚠️ Limited | ✅ Full | ⚠️ Partial |
| Window functions (basic) | ✅ | ✅ | ✅ Yes |
| Window functions (advanced) | ⚠️ | ✅ | ⚠️ Partial |
| Comment extraction | ❌ | ✅ | ❌ No (API missing) |
| Query summary API | ❌ | ✅ | ❌ No (API missing) |

## Required Implementation Changes

### 1. Immediate (Before Release)

- [ ] **Create version-specific namespace**: `Npgquery.Models.V17`
- [ ] **Add JSON node models**: JsonTable, JsonExpr, etc.
- [ ] **Version guard for new APIs**: Prevent calls to PG 17-only functions from PG 16 parser
- [ ] **Add version-specific tests**: JSON_TABLE tests marked PG 17+
- [ ] **Document breaking changes**: Update main README

### 2. Short Term

- [ ] **Compatibility layer**: Create adapters for common operations
- [ ] **Feature detection**: `VersionFeatures.SupportsJsonTable(version)`
- [ ] **Migration guide**: Help users understand differences
- [ ] **Example queries**: Show version-specific features

### 3. Long Term

- [ ] **Auto version detection**: Detect from query features
- [ ] **Query rewriting**: Convert PG 17 features to PG 16 equivalents where possible
- [ ] **Performance comparison**: Benchmark across versions

## Testing Strategy

### Version-Specific Tests

```csharp
[Theory]
[InlineData(PostgreSqlVersion.Postgres16)]
[InlineData(PostgreSqlVersion.Postgres17)]
public void BasicSQL_WorksAcrossVersions(PostgreSqlVersion version)
{
    var queries = new[]
    {
        "SELECT * FROM users",
        "INSERT INTO users VALUES (1, 'test')",
        "UPDATE users SET name = 'updated'",
        "DELETE FROM users WHERE id = 1"
    };
    
    using var parser = new Parser(version);
    foreach (var query in queries)
    {
        var result = parser.Parse(query);
        Assert.True(result.IsSuccess, $"Failed on {version}: {query}");
    }
}

[Fact]
public void JsonTable_OnlyWorksInPG17()
{
    var query = "SELECT * FROM JSON_TABLE('{}', '$' COLUMNS(id int PATH '$.id'))";
    
    // PG 16 should fail
    using var parser16 = new Parser(PostgreSqlVersion.Postgres16);
    var result16 = parser16.Parse(query);
    Assert.False(result16.IsSuccess);
    Assert.Contains("syntax error", result16.Error, StringComparison.OrdinalIgnoreCase);
    
    // PG 17 should succeed
    using var parser17 = new Parser(PostgreSqlVersion.Postgres17);
    var result17 = parser17.Parse(query);
    Assert.True(result17.IsSuccess);
}

[Fact]
public void ParseTree_DiffersForJsonOperations()
{
    var query = "SELECT json_query('[1,2,3]', '$[*]')";
    
    using var parser16 = new Parser(PostgreSqlVersion.Postgres16);
    using var parser17 = new Parser(PostgreSqlVersion.Postgres17);
    
    var result16 = parser16.Parse(query);
    var result17 = parser17.Parse(query);
    
    // Both should parse, but trees will differ
    Assert.True(result16.IsSuccess);
    Assert.True(result17.IsSuccess);
    
    var tree16Json = result16.ParseTree.RootElement.ToString();
    var tree17Json = result17.ParseTree.RootElement.ToString();
    
    // PG 17 will have JsonFuncExpr nodes
    Assert.Contains("JsonFuncExpr", tree17Json);
}
```

## Migration Guide for Users

### If Using JSON Functions

```csharp
// Before (assumed PG 16)
using var parser = new Parser();
var result = parser.Parse(jsonQuery);

// After (version-aware)
var version = DetectRequiredVersion(jsonQuery);
using var parser = new Parser(version);
var result = parser.Parse(jsonQuery);

// Helper
PostgreSqlVersion DetectRequiredVersion(string query)
{
    if (query.Contains("JSON_TABLE", StringComparison.OrdinalIgnoreCase))
        return PostgreSqlVersion.Postgres17;
    return PostgreSqlVersion.Postgres16;
}
```

### If Parsing Protobuf

```csharp
// Version-aware deserialization
public IStatement ParseStatement(JsonDocument parseTree, PostgreSqlVersion version)
{
    return version switch
    {
        PostgreSqlVersion.Postgres16 => V16.StatementParser.Parse(parseTree),
        PostgreSqlVersion.Postgres17 => V17.StatementParser.Parse(parseTree),
        _ => throw new NotSupportedException($"Version {version} not supported")
    };
}
```

## Resources

- **libpg_query Changelog**: https://github.com/pganalyze/libpg_query/blob/master/CHANGELOG.md
- **PostgreSQL 17 Release Notes**: https://www.postgresql.org/docs/17/release-17.html
- **JSON_TABLE Documentation**: https://www.postgresql.org/docs/17/functions-json.html

## Conclusion

⚠️ **PostgreSQL 17 introduces significant breaking changes**, primarily around:

1. **JSON/SQL** - Major new functionality (JSON_TABLE, enhanced JSON functions)
2. **MERGE** - Enhanced with new capabilities  
3. **API** - New functions for comments, summaries, utilities

**Action Required**: 
- Create PG 17-specific models
- Add version checks for new features
- Test thoroughly across both versions
- Document limitations and differences

**Backward Compatibility**:
- Basic SQL (SELECT/INSERT/UPDATE/DELETE) unchanged
- Most DDL unchanged
- Core functionality compatible

**Upgrade Path**:
- Can support both versions simultaneously
- Version selection at parser construction
- Graceful degradation for unsupported features

---

*Last Updated*: [Current Session]
*Status*: ⚠️ **CRITICAL - Breaking changes require implementation updates**
