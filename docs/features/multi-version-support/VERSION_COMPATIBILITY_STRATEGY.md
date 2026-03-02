# Version Difference Analysis & Compatibility Strategy

## ⚠️ Critical Requirement

**The libpg_query repository has BREAKING CHANGES between PostgreSQL versions!**

Different PostgreSQL versions have:
- Different parse tree structures
- New/removed node types
- Changed field names
- Different enum values
- API signature changes

**We MUST handle these differences to ensure postgresqlPacTool works seamlessly across versions.**

---

## 🔍 Version Difference Analysis Process

### Step 1: Analyze libpg_query Branches

For each version, examine:

```bash
# Clone and analyze differences
git clone https://github.com/pganalyze/libpg_query.git
cd libpg_query

# Compare branches
git diff 16-latest..17-latest -- src/postgres/
git diff 16-latest..17-latest -- pg_query.h
git diff 16-latest..17-latest -- protobuf/
```

### Key Files to Check

| File/Directory | Purpose | Version Impact |
|----------------|---------|----------------|
| `pg_query.h` | C API signatures | Breaking changes |
| `protobuf/pg_query.proto` | Protobuf definitions | Parse tree structure |
| `src/postgres/include/nodes/` | Node definitions | New/removed nodes |
| `CHANGELOG.md` | Version history | Breaking change documentation |

### Step 2: Document Differences

Create version-specific documentation:

**File**: `docs/version-differences/PG{version}_CHANGES.md`

Example for PostgreSQL 17:
```markdown
# PostgreSQL 17 Changes from 16

## Breaking Changes

### New Node Types
- `JsonTable` - New JSON_TABLE functionality
- `MergeAction` - Enhanced MERGE statement

### Removed Node Types
- None

### Modified Nodes
- `SelectStmt`: Added `distinctClause` improvements
- `CreateStmt`: New partitioning options

### API Changes
- `pg_query_fingerprint`: Now returns extended hash
- `pg_query_parse`: Additional error context
```

---

## 🏗️ Compatibility Architecture

### Strategy 1: Version-Specific Models (Recommended)

Create separate model namespaces per version:

```
src/libs/Npgquery/Npgquery/Models/
├── V16/
│   ├── ParseNodes.cs
│   ├── StatementTypes.cs
│   └── Extensions.cs
├── V17/
│   ├── ParseNodes.cs
│   ├── StatementTypes.cs
│   └── Extensions.cs
└── Common/
    ├── IVersionedNode.cs
    ├── IStatement.cs
    └── VersionConverter.cs
```

**Implementation**:

```csharp
namespace Npgquery.Models.V16
{
    public record SelectStmt : IVersionedNode
    {
        public PostgreSqlVersion Version => PostgreSqlVersion.Postgres16;
        // PG 16 specific fields
    }
}

namespace Npgquery.Models.V17
{
    public record SelectStmt : IVersionedNode
    {
        public PostgreSqlVersion Version => PostgreSqlVersion.Postgres17;
        // PG 17 specific fields (may differ!)
    }
}
```

### Strategy 2: Compatibility Layer

Create adapters that normalize across versions:

```csharp
public interface ISelectStatement
{
    string[] Columns { get; }
    string[] FromTables { get; }
    string WhereClause { get; }
}

public class SelectStatementAdapter : ISelectStatement
{
    public static ISelectStatement Create(JsonElement parseTree, PostgreSqlVersion version)
    {
        return version switch
        {
            PostgreSqlVersion.Postgres16 => new V16SelectStatementAdapter(parseTree),
            PostgreSqlVersion.Postgres17 => new V17SelectStatementAdapter(parseTree),
            _ => throw new NotSupportedException($"Version {version} not supported")
        };
    }
}
```

### Strategy 3: Version Detection & Automatic Handling

```csharp
public class VersionAwareParser
{
    public IStatement ParseStatement(string query)
    {
        // Try each version until one succeeds
        foreach (var version in NativeLibraryLoader.GetAvailableVersions())
        {
            try
            {
                using var parser = new Parser(version);
                var result = parser.Parse(query);
                
                if (result.IsSuccess)
                {
                    return CreateStatement(result.ParseTree, version);
                }
            }
            catch { /* Try next version */ }
        }
        
        throw new ParseException("Query could not be parsed with any available version");
    }
    
    private IStatement CreateStatement(JsonDocument parseTree, PostgreSqlVersion version)
    {
        // Create version-specific statement wrapper
        return StatementFactory.Create(parseTree, version);
    }
}
```

---

## 🔧 Implementation Plan

### Phase 1: Analysis & Documentation

**Files to Create**:
```
docs/version-differences/
├── README.md                    # Overview of version differences
├── PG16_BASELINE.md            # PostgreSQL 16 baseline
├── PG17_CHANGES.md             # Changes from 16 to 17
├── PG18_CHANGES.md             # Changes from 17 to 18 (future)
├── BREAKING_CHANGES.md         # All breaking changes catalog
└── COMPATIBILITY_MATRIX.md     # Feature compatibility across versions
```

**Script to Analyze**:
```powershell
# scripts/Analyze-VersionDifferences.ps1
# Compares libpg_query branches and generates reports
```

### Phase 2: Version-Specific Models

**Update ParseResult**:
```csharp
public sealed record ParseResult : QueryResultBase
{
    public JsonDocument? ParseTree { get; init; }
    
    // NEW: Version-specific parsed statement
    public IVersionedStatement? Statement { get; init; }
    
    // NEW: Version used for parsing
    public PostgreSqlVersion Version { get; init; }
}
```

**Create Version Factories**:
```csharp
public static class StatementFactory
{
    public static IVersionedStatement Create(JsonDocument parseTree, PostgreSqlVersion version)
    {
        return version switch
        {
            PostgreSqlVersion.Postgres16 => V16.StatementParser.Parse(parseTree),
            PostgreSqlVersion.Postgres17 => V17.StatementParser.Parse(parseTree),
            _ => throw new ArgumentException($"Unsupported version: {version}")
        };
    }
}
```

### Phase 3: Compatibility Testing

**Create Tests**:
```csharp
[Theory]
[InlineData(PostgreSqlVersion.Postgres16)]
[InlineData(PostgreSqlVersion.Postgres17)]
public void SelectStatement_ShouldParseSimilarly(PostgreSqlVersion version)
{
    using var parser = new Parser(version);
    var result = parser.Parse("SELECT id, name FROM users WHERE active = true");
    
    Assert.True(result.IsSuccess);
    
    var statement = result.Statement as ISelectStatement;
    Assert.NotNull(statement);
    Assert.Contains("users", statement.FromTables);
}

[Fact]
public void PG17_NewFeatures_ShouldOnlyWorkWithPG17()
{
    // JSON_TABLE is PG 17+
    var query = "SELECT * FROM JSON_TABLE(...)";
    
    using var parser16 = new Parser(PostgreSqlVersion.Postgres16);
    var result16 = parser16.Parse(query);
    Assert.False(result16.IsSuccess); // Should fail
    
    using var parser17 = new Parser(PostgreSqlVersion.Postgres17);
    var result17 = parser17.Parse(query);
    Assert.True(result17.IsSuccess); // Should succeed
}
```

### Phase 4: Automated Difference Detection

**GitHub Action**:
```yaml
# .github/workflows/check-version-differences.yml
name: Check Version Differences

on:
  schedule:
    - cron: '0 0 * * 0'  # Weekly
  workflow_dispatch:

jobs:
  analyze:
    runs-on: ubuntu-latest
    steps:
      - name: Clone libpg_query
        run: |
          git clone https://github.com/pganalyze/libpg_query.git
          cd libpg_query
          
          # Compare versions
          git diff 16-latest..17-latest -- protobuf/ > pg17-proto-diff.txt
          git diff 16-latest..17-latest -- pg_query.h > pg17-api-diff.txt
      
      - name: Analyze differences
        run: |
          # Parse diffs and create report
          ./scripts/Analyze-VersionDifferences.ps1 -BaseBranch 16-latest -CompareBranch 17-latest
      
      - name: Create issue if changes detected
        if: steps.analyze.outputs.has_changes == 'true'
        uses: actions/create-github-issue@v1
        with:
          title: "⚠️ libpg_query version differences detected"
          body: "New changes detected in libpg_query. Review and update compatibility layer."
```

---

## 📊 Known Version Differences

### PostgreSQL 16 → 17

#### Protobuf Changes
```diff
// New in PG 17
+ message JsonTable { ... }
+ message JsonTableColumn { ... }
+ message MergeAction { ... }

// Modified in PG 17
  message SelectStmt {
    ...
+   JsonTable json_table = XX;
  }
```

#### API Changes
- `pg_query_fingerprint`: Returns 128-bit hash (was 64-bit in 16)
- New error codes for JSON_TABLE syntax

#### New Features (PG 17 only)
- JSON_TABLE support
- SQL/JSON path enhancements
- MERGE statement improvements
- New aggregate functions

#### Removed Features
- (None identified yet)

### PostgreSQL 15 → 16

(To be documented if we add PG 15 support)

---

## 🛠️ Development Workflow

### When Adding a New PostgreSQL Version

1. **Analyze libpg_query**
   ```bash
   # Compare branches
   git diff {previous}-latest..{new}-latest
   ```

2. **Document Changes**
   - Create `PG{version}_CHANGES.md`
   - Update `BREAKING_CHANGES.md`
   - Update `COMPATIBILITY_MATRIX.md`

3. **Create Version Models**
   ```csharp
   // src/libs/Npgquery/Npgquery/Models/V{version}/
   // Copy from previous version and modify
   ```

4. **Update Compatibility Layer**
   ```csharp
   // Add new version to switch statements
   // Update StatementFactory
   // Add version-specific adapters
   ```

5. **Create Tests**
   ```csharp
   // Test version-specific features
   // Test backward compatibility
   // Test version detection
   ```

6. **Update Documentation**
   - Main README
   - Migration guide
   - API documentation

### Continuous Monitoring

**Set up monitoring for**:
- New libpg_query releases
- PostgreSQL release announcements
- Breaking changes in upstream

**Automation**:
- Weekly diff checks
- Automated issue creation
- Version compatibility reports

---

## 📋 Compatibility Matrix

### Statement Types

| Statement | PG 16 | PG 17 | PG 18 | Notes |
|-----------|-------|-------|-------|-------|
| SELECT | ✅ | ✅ | ? | Core functionality |
| INSERT | ✅ | ✅ | ? | Core functionality |
| UPDATE | ✅ | ✅ | ? | Core functionality |
| DELETE | ✅ | ✅ | ? | Core functionality |
| CREATE TABLE | ✅ | ✅ | ? | Partitioning differs |
| ALTER TABLE | ✅ | ✅ | ? | New options in 17 |
| JSON_TABLE | ❌ | ✅ | ? | New in PG 17 |
| MERGE (enhanced) | ⚠️ | ✅ | ? | Limited in 16, full in 17 |

**Legend**:
- ✅ Fully supported
- ⚠️ Partially supported
- ❌ Not supported
- ? Not yet evaluated

### Parse Tree Node Types

| Node Type | PG 16 | PG 17 | Changes |
|-----------|-------|-------|---------|
| SelectStmt | ✅ | ✅ | Added json_table field |
| InsertStmt | ✅ | ✅ | No changes |
| UpdateStmt | ✅ | ✅ | No changes |
| DeleteStmt | ✅ | ✅ | No changes |
| JsonTable | ❌ | ✅ | New in 17 |
| MergeAction | ⚠️ | ✅ | Enhanced in 17 |

---

## 🔍 Investigation Checklist

When analyzing a new version:

- [ ] Clone libpg_query repository
- [ ] Checkout both version branches
- [ ] Compare `protobuf/pg_query.proto`
- [ ] Compare `pg_query.h` API
- [ ] Check `CHANGELOG.md` for breaking changes
- [ ] Review PostgreSQL release notes
- [ ] Test parse tree differences with sample queries
- [ ] Document all differences
- [ ] Update compatibility matrix
- [ ] Create version-specific models (if needed)
- [ ] Update adapters/converters
- [ ] Create comprehensive tests
- [ ] Update documentation

---

## 📚 Resources

### Official Sources
- **PostgreSQL Release Notes**: https://www.postgresql.org/docs/release/
- **libpg_query Repository**: https://github.com/pganalyze/libpg_query
- **libpg_query Changelog**: https://github.com/pganalyze/libpg_query/blob/master/CHANGELOG.md

### Analysis Tools
- **Protobuf Diff**: Compare `.proto` files
- **Git Diff**: Compare source branches
- **Parse Tree Inspector**: Compare actual parse outputs

### Documentation
- `docs/version-differences/` - All version-specific docs
- `docs/COMPATIBILITY_MATRIX.md` - Feature matrix
- `docs/MIGRATION_GUIDE.md` - How to handle breaking changes

---

## 🎯 Action Items

### Immediate (Before releasing multi-version support)
1. ✅ Create this document
2. ⏳ Analyze PG 16 vs PG 17 differences
3. ⏳ Create version-specific model namespaces
4. ⏳ Implement compatibility layer
5. ⏳ Add version-specific tests
6. ⏳ Document all breaking changes

### Short Term
1. Create automated difference detection script
2. Set up weekly monitoring
3. Build version comparison tool
4. Create migration guide for users

### Long Term
1. Automated model generation from protobuf
2. Version translation layer (convert PG 16 → 17)
3. Query compatibility checker
4. Performance comparison across versions

---

## 💡 Best Practices

### DO
✅ Document every version difference
✅ Create version-specific tests
✅ Use compatibility layers for common operations
✅ Monitor upstream for changes
✅ Maintain backward compatibility when possible

### DON'T
❌ Assume versions are compatible
❌ Use version-specific features without checks
❌ Ignore breaking changes
❌ Skip testing across versions
❌ Forget to update documentation

---

## 🚨 Critical Warning

**BREAKING CHANGES WILL OCCUR!**

Each PostgreSQL major version can introduce:
- New node types
- Removed node types
- Changed field names
- Different semantics
- API incompatibilities

**We MUST handle these to ensure postgresqlPacTool works correctly!**

---

*This document must be updated with each PostgreSQL version added!*
*Last Updated: [Current Session]*
*Status: ⚠️ ACTIVE REQUIREMENT - Must complete before production release*
