# PostgreSQL 16+ Only - Implementation Simplifications

**Decision:** Support PostgreSQL 16+ only  
**Impact:** Significant code and test simplification  
**Last Updated:** 2026-01-31

---

## ?? Summary of Changes

### Before (Multi-Version Support)
- Support PostgreSQL 12-16 (5 versions)
- Version detection and branching
- Conditional feature support
- Multi-version testing (5x test matrix)
- Complex compatibility layer

### After (PostgreSQL 16+ Only)
- Support PostgreSQL 16+ (1 baseline)
- No version detection needed
- All features always available
- Single-version testing
- Simplified codebase

---

## ?? Code Simplifications

### 1. No Version Detection Logic

**Before:**
```csharp
private async Task<int> GetPostgreSqlMajorVersion()
{
    using var cmd = new NpgsqlCommand("SHOW server_version;", CreateConnection());
    var version = (string)await cmd.ExecuteScalarAsync();
    var versionParts = version.Split(' ')[0].Split('.');
    return int.Parse(versionParts[0]);
}

public async Task<PgProject> ExtractPgProject(string databaseName, string postgresVersion)
{
    var project = new PgProject
    {
        DatabaseName = databaseName,
        PostgresVersion = postgresVersion,
    };
    
    var majorVersion = await GetPostgreSqlMajorVersion();
    
    // Conditional extraction based on version
    if (majorVersion >= 11)
    {
        pgSchema.Procedures.AddRange(await ExtractProceduresAsync(schema.Name));
    }
    
    if (majorVersion >= 12)
    {
        // Extract generated columns
    }
    
    // ... more version checks
}
```

**After:**
```csharp
public async Task<PgProject> ExtractPgProject(string databaseName)
{
    var project = new PgProject
    {
        DatabaseName = databaseName,
        PostgresVersion = await GetPostgresVersionAsync(), // For metadata only
    };
    
    // All features always available - no version checks!
    pgSchema.Procedures.AddRange(await ExtractProceduresAsync(schema.Name));
    // No conditional logic needed
}

private async Task<string> GetPostgresVersionAsync()
{
    using var cmd = new NpgsqlCommand("SHOW server_version;", CreateConnection());
    var version = (string)await cmd.ExecuteScalarAsync();
    
    // Enforce minimum version
    var major = int.Parse(version.Split('.')[0]);
    if (major < 16)
    {
        throw new NotSupportedException(
            $"PostgreSQL {major} is not supported. Please upgrade to PostgreSQL 16 or higher.");
    }
    
    return version;
}
```

**Lines Saved:** ~50-100 lines of version checking code

---

### 2. Simplified Procedure Extraction

**Before (Issue #3):**
```csharp
public async Task<List<PgProcedure>> ExtractProceduresAsync(string schemaName)
{
    // Check version first
    var version = await GetPostgreSqlMajorVersion();
    if (version < 11)
    {
        // Log warning
        return new List<PgProcedure>(); // Empty list for old versions
    }
    
    // Extract procedures (PG 11+)
    var sql = @"
        SELECT p.proname, ...
        FROM pg_proc p
        WHERE p.prokind = 'p'  -- Only in PG 11+
    ";
    
    // ... extraction logic
}
```

**After (Issue #3):**
```csharp
public async Task<List<PgProcedure>> ExtractProceduresAsync(string schemaName)
{
    // No version check - PostgreSQL 16+ guaranteed!
    var sql = @"
        SELECT p.proname, ...
        FROM pg_proc p
        WHERE p.prokind = 'p'
    ";
    
    // ... extraction logic
}
```

**Lines Saved:** ~10-15 lines per feature check

---

### 3. No Conditional Test Logic

**Before:**
```csharp
[TestFixture]
[TestCase("12")]
[TestCase("13")]
[TestCase("14")]
[TestCase("15")]
[TestCase("16")]
public class ExtractionTests
{
    [Test]
    public async Task ExtractProcedures_PostgreSql_ExtractsCorrectly(string version)
    {
        await using var container = new PostgreSqlBuilder()
            .WithImage($"postgres:{version}")
            .Build();
        
        await container.StartAsync();
        
        var extractor = new PgProjectExtractor(container.GetConnectionString());
        var project = await extractor.ExtractPgProject("testdb", version);
        
        // Version-specific assertions
        if (int.Parse(version) >= 11)
        {
            Assert.That(project.Schemas[0].Procedures, Is.Not.Empty);
        }
        else
        {
            Assert.That(project.Schemas[0].Procedures, Is.Empty);
        }
    }
}
```

**After:**
```csharp
[TestFixture]
public class ExtractionTests
{
    private const string PostgreSqlVersion = "16";
    
    [Test]
    public async Task ExtractProcedures_ExtractsCorrectly()
    {
        await using var container = new PostgreSqlBuilder()
            .WithImage($"postgres:{PostgreSqlVersion}")
            .Build();
        
        await container.StartAsync();
        
        var extractor = new PgProjectExtractor(container.GetConnectionString());
        var project = await extractor.ExtractPgProject("testdb");
        
        // Simple assertion - procedures always available
        Assert.That(project.Schemas[0].Procedures, Is.Not.Empty);
    }
}
```

**Tests Reduced:** 5x fewer test permutations

---

## ?? Story Point Reductions

### Issue Story Point Adjustments

| Issue | Before | After | Savings | Reason |
|-------|--------|-------|---------|--------|
| #3 Procedures | 5 SP | 3 SP | 2 SP | No version detection |
| #11 Integration Tests | 13 SP | 8 SP | 5 SP | Single version testing |
| #2 Functions | 8 SP | 8 SP | 0 SP | No version-specific features |
| #1 Views | 5 SP | 5 SP | 0 SP | No version-specific features |
| **Total** | **31 SP** | **24 SP** | **7 SP** | **23% reduction** |

---

## ?? Testing Simplifications

### Test Matrix Reduction

**Before:**
```
Unit Tests:        350 tests
Integration Tests: 125 tests × 5 versions = 625 test runs
E2E Tests:         25 tests × 5 versions = 125 test runs
?????????????????????????????????????????????????????????
Total Test Runs:   1,100 runs
Execution Time:    ~15 minutes
```

**After:**
```
Unit Tests:        350 tests
Integration Tests: 125 tests × 1 version = 125 test runs
E2E Tests:         25 tests × 1 version = 25 test runs
?????????????????????????????????????????????????????????
Total Test Runs:   500 runs (55% reduction)
Execution Time:    ~5 minutes (67% faster)
```

### CI/CD Pipeline Simplification

**Before (Multi-version):**
```yaml
strategy:
  matrix:
    postgres-version: [12, 13, 14, 15, 16]

steps:
  - name: Test PostgreSQL ${{ matrix.postgres-version }}
    run: |
      dotnet test --filter PostgreSqlVersion=${{ matrix.postgres-version }}
```

**After (Single version):**
```yaml
steps:
  - name: Test PostgreSQL 16
    run: |
      dotnet test
```

---

## ?? Feature Support Simplifications

### Always-Available Features (PostgreSQL 16+)

| Feature | PostgreSQL Version | Our Support |
|---------|-------------------|-------------|
| Procedures | 11+ | ? Always |
| Generated Columns | 12+ | ? Always |
| JSON Path | 12+ | ? Always |
| Trusted Extensions | 13+ | ? Always |
| Parallelized Vacuuming | 13+ | ? Always |
| Incremental Sort | 13+ | ? Always |
| Multirange Types | 14+ | ? Always |
| Subscripting Arrays | 14+ | ? Always |
| SQL/JSON | 15+ | ? Always |
| MERGE Command | 15+ | ? Always |
| Logical Replication | 16+ | ? Always |

**No conditional code needed for any of these!**

---

## ??? Code to Remove

### Files Not Needed

```
src/
??? VersionDetection/           ? DELETE
?   ??? PostgreSqlVersionChecker.cs
?   ??? FeatureCompatibility.cs
??? Compatibility/              ? DELETE
?   ??? VersionSpecificHandlers.cs
```

### Methods to Simplify

```csharp
// ? REMOVE
public bool SupportsProced ures(int version) => version >= 11;
public bool SupportsGeneratedColumns(int version) => version >= 12;
public bool SupportsMerge(int version) => version >= 15;

// ? KEEP (but simplified)
public async Task<string> GetPostgresVersionAsync()
{
    // Only for metadata and minimum version enforcement
}
```

---

## ?? Updated Issue Requirements

### Issue #3: Stored Procedures (Simplified)

**Acceptance Criteria Changes:**

**REMOVED:**
- ~~Add PostgreSQL version detection~~
- ~~Skip procedure extraction for PostgreSQL < 11~~
- ~~Add warning message when procedures exist but version < 11~~
- ~~Integration test with PostgreSQL 10 (verify graceful skip)~~

**KEPT (Simplified):**
- ? Implement `ExtractProceduresAsync(string schemaName)` method
- ? Extract procedure signature and body
- ? Create `PgProcedure` model class
- ? Test with PostgreSQL 16

**Story Points:** 5 ? **3** (40% reduction)

---

### Issue #11: Integration Tests (Simplified)

**Acceptance Criteria Changes:**

**REMOVED:**
- ~~Create test fixtures for PostgreSQL versions 12-15~~
- ~~Parameterized tests to run against all versions~~
- ~~Test with PostgreSQL 10 (verify graceful skip)~~

**KEPT (Simplified):**
- ? Fix existing Testcontainers setup
- ? Create test fixtures for **PostgreSQL 16**
- ? Test forward compatibility with PostgreSQL 17+ (when available)
- ? All extraction tests against single version

**Story Points:** 13 ? **8** (38% reduction)

---

## ?? Benefits Summary

### Development Speed
- ? **23% fewer story points** in extraction phase
- ? **Faster development** (no version branching)
- ? **Simpler code reviews** (no version logic to review)

### Testing Speed
- ? **55% fewer test runs** (single version)
- ? **67% faster CI/CD** (5 min vs 15 min)
- ? **Simpler test maintenance** (no version matrix)

### Code Quality
- ? **Lower complexity** (no conditional logic)
- ? **Fewer bugs** (no version-specific edge cases)
- ? **Better maintainability** (clearer code paths)

### User Experience
- ? **Clear requirements** (PostgreSQL 16+)
- ? **Better error messages** (version enforcement)
- ? **Modern features** (no legacy constraints)

---

## ?? Migration Guide for Codebase

### Step 1: Remove Version Detection

```bash
# Find all version checks
git grep -n "GetPostgreSqlMajorVersion"
git grep -n "if.*version.*>="
git grep -n "PostgreSqlVersion"

# Remove or simplify
```

### Step 2: Simplify Tests

```bash
# Find parameterized version tests
git grep -n "TestCase.*12\|13\|14\|15"
git grep -n "matrix.*postgres-version"

# Replace with single PostgreSQL 16
```

### Step 3: Update Documentation

```bash
# Update README
# Update CONTRIBUTING
# Update issue templates
# Update CI/CD workflows
```

### Step 4: Add Version Enforcement

```csharp
// Add to connection initialization
if (postgresVersion < 16)
{
    throw new NotSupportedException(
        $"PostgreSQL {postgresVersion} is not supported. " +
        "Please upgrade to PostgreSQL 16 or higher. " +
        "See: https://github.com/mbulava-org/pgPacTool/wiki/upgrade-guide");
}
```

---

## ?? Estimated Impact

### Time Savings

| Phase | Before | After | Savings |
|-------|--------|-------|---------|
| Development | 8 weeks | 6 weeks | 25% |
| Testing | 4 weeks | 2 weeks | 50% |
| Maintenance | Ongoing | Ongoing | 30% less |

### Cost Savings

| Metric | Before | After | Savings |
|--------|--------|-------|---------|
| CI/CD Minutes | 100 min/build | 35 min/build | 65% |
| Test Infrastructure | 5 containers | 1 container | 80% |
| Developer Time | High | Low | Significant |

---

## ? Action Items

- [x] Create SCOPE.md document defining PostgreSQL 16+ only
- [ ] Update ROADMAP.md version support section
- [ ] Update ISSUES.md to remove version checks from Issue #3
- [ ] Update ISSUES.md to simplify Issue #11 testing
- [ ] Update TESTING_STRATEGY.md to single version
- [ ] Update README.md with PostgreSQL 16+ requirement
- [ ] Add version enforcement code
- [ ] Update CI/CD workflows
- [ ] Create user migration guide
- [ ] Update project templates

---

**Decision Made:** 2026-01-31  
**Document Version:** 1.0  
**Status:** ? Approved
