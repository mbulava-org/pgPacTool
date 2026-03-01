# ⚠️ CRITICAL: Version Compatibility Requirements

## 🚨 Breaking Changes Are Expected!

**Each PostgreSQL major version CAN and LIKELY WILL introduce breaking changes in:**

- Parse tree structure (protobuf schema)
- Node types (new, removed, modified)
- C API signatures
- Field names and types
- Statement semantics

## 📋 Required Process Before Adding Any Version

### Step 1: Analysis (MANDATORY)

```powershell
# Analyze version differences
.\scripts\Analyze-VersionDifferences.ps1 -BaseVersion 16 -CompareVersion 17 -Detailed
```

**This will generate**:
- `PG17_CHANGES.md` - Summary of all changes
- `PG17_protobuf_diff.txt` - Protobuf schema differences
- `PG17_api_diff.txt` - C API changes
- `PG17_nodes_diff.txt` - Node type modifications

### Step 2: Review Analysis

**Check for**:
- ✅ New message types → Need new model classes
- ✅ Removed message types → Need deprecation handling
- ✅ Modified fields → Need version-specific properties
- ✅ API changes → Need wrapper updates
- ✅ New node types → Need parser updates

### Step 3: Update Models (If Needed)

**If protobuf changes detected**:

```csharp
// Create version-specific models
namespace Npgquery.Models.V17
{
    // New in PG 17
    public record JsonTableNode
    {
        public string Path { get; init; }
        public List<JsonTableColumn> Columns { get; init; }
    }
    
    // Modified in PG 17
    public record SelectStmt
    {
        // Existing fields...
        
        // NEW in PG 17
        public JsonTableNode? JsonTable { get; init; }
    }
}
```

### Step 4: Add Compatibility Layer

**If API or semantics changed**:

```csharp
public interface ISelectStatement
{
    string[] GetTables();
    string[] GetColumns();
}

public static class SelectStatementAdapter
{
    public static ISelectStatement Create(JsonDocument parseTree, PostgreSqlVersion version)
    {
        return version switch
        {
            PostgreSqlVersion.Postgres16 => new V16SelectAdapter(parseTree),
            PostgreSqlVersion.Postgres17 => new V17SelectAdapter(parseTree),
            _ => throw new NotSupportedException()
        };
    }
}

// Handle differences
internal class V17SelectAdapter : ISelectStatement
{
    public string[] GetTables()
    {
        // PG 17 may include JSON_TABLE in table list
        var tables = _parseTree.GetTables();
        var jsonTables = _parseTree.GetJsonTables(); // NEW in 17
        return tables.Concat(jsonTables).ToArray();
    }
}
```

### Step 5: Version-Specific Tests

```csharp
public class VersionCompatibilityTests
{
    [Theory]
    [InlineData(PostgreSqlVersion.Postgres16)]
    [InlineData(PostgreSqlVersion.Postgres17)]
    public void BasicSelect_WorksAcrossVersions(PostgreSqlVersion version)
    {
        using var parser = new Parser(version);
        var result = parser.Parse("SELECT id, name FROM users");
        Assert.True(result.IsSuccess);
    }
    
    [Fact]
    public void JsonTable_OnlyWorksInPG17()
    {
        var query = "SELECT * FROM JSON_TABLE(...)";
        
        using var parser16 = new Parser(PostgreSqlVersion.Postgres16);
        Assert.False(parser16.Parse(query).IsSuccess); // Should fail
        
        using var parser17 = new Parser(PostgreSqlVersion.Postgres17);
        Assert.True(parser17.Parse(query).IsSuccess); // Should succeed
    }
}
```

## 📊 Known Version Differences

### PostgreSQL 16 → 17 (Example)

#### Protobuf Changes
- **New**: `JsonTable`, `JsonTableColumn`, `MergeAction` messages
- **Modified**: `SelectStmt` now has `json_table` field
- **Removed**: None identified

#### API Changes
- `pg_query_fingerprint`: Now returns 128-bit hash (was 64-bit)
- Error codes expanded for JSON_TABLE syntax

#### New Features (PG 17 Only)
- JSON_TABLE support
- SQL/JSON path enhancements
- MERGE statement improvements
- Enhanced window functions

#### Compatibility Impact
- ⚠️ Parse trees differ for queries with JSON operations
- ⚠️ Fingerprints are incompatible between versions
- ✅ Basic SQL (SELECT/INSERT/UPDATE/DELETE) unchanged
- ✅ Most DDL statements unchanged

### PostgreSQL 17 → 18 (Future)

**Status**: Not yet analyzed

**Action**: Run analysis when PG 18 branch available:
```powershell
.\scripts\Analyze-VersionDifferences.ps1 -BaseVersion 17 -CompareVersion 18
```

## 🎯 Checklist for Adding New Version

- [ ] Run version difference analysis
- [ ] Review generated reports
- [ ] Identify breaking changes
- [ ] Create version-specific models (if needed)
- [ ] Add compatibility adapters (if needed)
- [ ] Update enum and extension methods
- [ ] Build native libraries
- [ ] Create version-specific tests
- [ ] Test backward compatibility
- [ ] Update documentation
- [ ] Update compatibility matrix

## 🔄 Automated Monitoring

**GitHub Actions workflow** automatically checks for version differences weekly:

```
Actions → Check libpg_query Version Differences → Runs weekly
```

**What it does**:
1. Compares specified versions
2. Generates analysis report
3. Creates PR with findings
4. Opens issue if breaking changes detected

**Manual trigger**:
1. Go to Actions tab
2. Select "Check libpg_query Version Differences"
3. Run workflow with version numbers
4. Review generated PR

## 📚 Documentation Structure

```
docs/
├── VERSION_COMPATIBILITY_STRATEGY.md  ← Strategy overview
└── version-differences/
    ├── README.md                      ← Index of all analyses
    ├── PG16_BASELINE.md              ← PostgreSQL 16 baseline
    ├── PG17_CHANGES.md               ← PG 16→17 analysis
    ├── PG17_protobuf_diff.txt        ← Detailed protobuf diff
    ├── PG17_api_diff.txt             ← Detailed API diff
    ├── PG17_nodes_diff.txt           ← Detailed node diff
    └── COMPATIBILITY_MATRIX.md       ← Feature matrix
```

## 🚫 Common Mistakes to Avoid

### DON'T:
❌ Add a version without running analysis first
❌ Assume versions are compatible
❌ Skip version-specific testing
❌ Ignore protobuf schema changes
❌ Use version-specific features without checks

### DO:
✅ Always analyze before adding versions
✅ Document all breaking changes
✅ Create compatibility layers
✅ Test across all supported versions
✅ Monitor upstream for changes

## 💡 Best Practices

### 1. Incremental Approach
```csharp
// Good: Version-aware with fallback
public IStatement ParseStatement(string query)
{
    foreach (var version in GetSupportedVersions().OrderByDescending(v => v))
    {
        try
        {
            using var parser = new Parser(version);
            var result = parser.Parse(query);
            if (result.IsSuccess)
                return CreateStatement(result, version);
        }
        catch { /* Try next version */ }
    }
    throw new ParseException("Could not parse with any version");
}
```

### 2. Version Detection
```csharp
// Good: Detect version from parse tree
public static PostgreSqlVersion DetectVersion(JsonDocument parseTree)
{
    // Check for version-specific fields
    if (parseTree.RootElement.TryGetProperty("version", out var versionProp))
    {
        var version = versionProp.GetInt32();
        return version switch
        {
            >= 170000 => PostgreSqlVersion.Postgres17,
            >= 160000 => PostgreSqlVersion.Postgres16,
            _ => PostgreSqlVersion.Postgres16 // fallback
        };
    }
    return PostgreSqlVersion.Postgres16; // default
}
```

### 3. Feature Detection
```csharp
// Good: Check for feature support
public static class VersionFeatures
{
    public static bool SupportsJsonTable(PostgreSqlVersion version)
    {
        return version >= PostgreSqlVersion.Postgres17;
    }
    
    public static bool SupportsMergeEnhancements(PostgreSqlVersion version)
    {
        return version >= PostgreSqlVersion.Postgres17;
    }
}

// Usage
if (!VersionFeatures.SupportsJsonTable(parser.Version))
{
    throw new NotSupportedException(
        $"JSON_TABLE requires PostgreSQL 17+, but using {parser.Version}");
}
```

## 🔍 Resources

- **Strategy Document**: `docs/VERSION_COMPATIBILITY_STRATEGY.md`
- **Analysis Script**: `scripts/Analyze-VersionDifferences.ps1`
- **Workflow**: `.github/workflows/check-version-differences.yml`
- **Quick Reference**: `docs/QUICK_REFERENCE.md`

## 📞 Support

### Questions About Version Compatibility?
1. Check `VERSION_COMPATIBILITY_STRATEGY.md`
2. Review version difference reports
3. Look at libpg_query CHANGELOG
4. Check PostgreSQL release notes

### Found Breaking Changes?
1. Document in `version-differences/PG{version}_CHANGES.md`
2. Create compatibility layer
3. Add version-specific tests
4. Update compatibility matrix

### Need Help?
1. Run the analysis script
2. Review generated reports
3. Check existing version implementations
4. Open an issue with analysis results

---

## ⚡ TL;DR

**Before adding ANY PostgreSQL version:**

1. **Run**: `.\scripts\Analyze-VersionDifferences.ps1`
2. **Review**: Generated analysis report
3. **Update**: Models and compatibility layer if needed
4. **Test**: Version-specific functionality
5. **Document**: All changes

**NEVER skip step 1!** Breaking changes WILL cause bugs in postgresqlPacTool if not handled properly.

---

*Last Updated: [Current Session]*
*Status: 🚨 CRITICAL REQUIREMENT - Must follow for all versions*
