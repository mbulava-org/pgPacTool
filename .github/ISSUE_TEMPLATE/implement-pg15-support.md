# Implement PostgreSQL 15 Support in Npgquery

## Summary
Add support for PostgreSQL 15 parser to the Npgquery library, building on the existing multi-version architecture that currently supports PostgreSQL 16 and 17.

## Background
The Npgquery library currently supports PostgreSQL versions 16 and 17 with full version isolation. The native libpg_query libraries for PostgreSQL 15 can be built using our GitHub Actions workflow, but the C# code does not yet include PG15 in the enum or expose it through the public API.

## Scope

### ✅ Already Done
- GitHub Actions workflow updated to build PG15 libraries (15-latest branch)
- Build process tested and validated for PG16 & PG17
- Multi-version architecture in place
- Version isolation working correctly

### 🔨 Work Required

#### 1. Update PostgreSqlVersion Enum
**File**: `src\libs\Npgquery\Npgquery\PostgreSqlVersion.cs`

Add PG15 to the enum:
```csharp
public enum PostgreSqlVersion
{
    /// <summary>
    /// PostgreSQL 15.x - Uses libpg_query based on PostgreSQL 15 parser
    /// </summary>
    Postgres15 = 15,

    /// <summary>
    /// PostgreSQL 16.x - Uses libpg_query based on PostgreSQL 16 parser
    /// </summary>
    Postgres16 = 16,

    /// <summary>
    /// PostgreSQL 17.x - Uses libpg_query based on PostgreSQL 17 parser
    /// </summary>
    Postgres17 = 17
}
```

Update extension methods to handle PG15:
```csharp
public static string ToLibrarySuffix(this PostgreSqlVersion version)
{
    return version switch
    {
        PostgreSqlVersion.Postgres15 => "15",
        PostgreSqlVersion.Postgres16 => "16",
        PostgreSqlVersion.Postgres17 => "17",
        _ => ((int)version).ToString()
    };
}

public static string ToVersionString(this PostgreSqlVersion version)
{
    return version switch
    {
        PostgreSqlVersion.Postgres15 => "PostgreSQL 15",
        PostgreSqlVersion.Postgres16 => "PostgreSQL 16",
        PostgreSqlVersion.Postgres17 => "PostgreSQL 17",
        _ => $"PostgreSQL {(int)version}"
    };
}

public static int ToVersionNumber(this PostgreSqlVersion version)
{
    return version switch
    {
        PostgreSqlVersion.Postgres15 => 150000,
        PostgreSqlVersion.Postgres16 => 160000,
        PostgreSqlVersion.Postgres17 => 170000,
        _ => (int)version * 10000
    };
}
```

#### 2. Build PG15 Native Libraries
Run GitHub Actions workflow:
```
Actions → Build Native libpg_query Libraries → Run workflow
Input: 15
```

This will:
- ✅ Checkout libpg_query 15-latest branch
- ✅ Build for Windows (libpg_query_15.dll)
- ✅ Build for Linux (libpg_query_15.so)
- ✅ Build for macOS (libpg_query_15.dylib)
- ✅ Copy to runtimes folders
- ✅ Create PR with libraries

#### 3. Create Version Compatibility Tests
**File**: `tests\Npgquery.Tests\VersionCompatibilityTests.cs`

Add PG15 to theory test cases:
```csharp
[Theory]
[InlineData(PostgreSqlVersion.Postgres15)]
[InlineData(PostgreSqlVersion.Postgres16)]
[InlineData(PostgreSqlVersion.Postgres17)]
public void BasicSQL_WorksAcrossAllVersions(PostgreSqlVersion version)
{
    using var parser = new Parser(version);
    var result = parser.Parse("SELECT * FROM users");
    Assert.True(result.IsSuccess);
}
```

#### 4. Verify Version-Specific Feature Isolation
Test that PG16/17 features don't work in PG15:

```csharp
[Fact]
public void JsonTable_DoesNotWorkInPG15()
{
    using var parser = new Parser(PostgreSqlVersion.Postgres15);
    var query = "SELECT * FROM JSON_TABLE('[{\"id\":1}]', '$[*]' COLUMNS(id int PATH '$.id'))";
    var result = parser.Parse(query);
    
    Assert.False(result.IsSuccess, "JSON_TABLE should NOT work in PostgreSQL 15");
}
```

#### 5. Update Documentation

**Files to Update**:
- `README.md` - Add PG15 to supported versions
- `docs\features\multi-version-support\README.md` - Include PG15
- `docs\NATIVE_LIBRARY_MULTI_VERSION_COMPLETE.md` - Update API reference
- `.github\copilot-instructions.md` - Update version lists

Example changes:
```markdown
**Currently Supported**:
- ✅ PostgreSQL 15
- ✅ PostgreSQL 16 (default version)
- ✅ PostgreSQL 17
```

#### 6. Integration Tests
Add PG15 to integration test suite:
- `NativeLibraryIntegrationTests.cs`
- `FunctionalityExposureTests.cs`
- `VersionIsolationVerificationTests.cs`

#### 7. Linux Container Tests
Update `LinuxContainer.Tests\NativeLibraryLinuxTests.cs` to include PG15.

## PostgreSQL 15 Feature Analysis

Based on libpg_query 15-latest branch:

### Core Functions (Available)
- ✅ pg_query_parse
- ✅ pg_query_normalize
- ✅ pg_query_fingerprint
- ✅ pg_query_scan
- ✅ pg_query_parse_plpgsql
- ✅ pg_query_deparse_protobuf
- ✅ All memory free functions

### Features NOT in PG15 (but in 16/17)
- ❌ JSON_TABLE (added in PG17)
- ❌ Enhanced JSON functions (JSON_EXISTS, JSON_QUERY, JSON_VALUE)
- ❌ MERGE statement enhancements
- ❌ pg_query_is_utility_stmt (added in PG17)
- ❌ pg_query_summary (added in PG17)

## Testing Strategy

### Phase 1: Unit Tests
1. Add PG15 to all existing theory tests
2. Verify library loading
3. Test basic SQL parsing
4. Verify version isolation

### Phase 2: Integration Tests
1. Test simultaneous PG15/16/17 parsers
2. Verify no cross-contamination
3. Test version-specific features
4. Confirm backwards compatibility

### Phase 3: Linux Container Tests
1. Run in Docker container
2. Verify Linux library loading
3. Test all core functionality

## Definition of Done

- [ ] PostgreSqlVersion enum includes Postgres15
- [ ] All extension methods handle PG15
- [ ] Native libraries built and committed (win-x64, linux-x64, osx-arm64)
- [ ] All existing tests pass with PG15 included
- [ ] Version isolation verified (PG15/16/17 don't interfere)
- [ ] Documentation updated
- [ ] New tests added for PG15-specific behavior
- [ ] Linux container tests pass
- [ ] README updated with PG15 support

## Estimated Effort
**~2-4 hours** (assuming libraries build successfully on first try)

## Dependencies
- ✅ GitHub Actions workflow updated
- ✅ Multi-version architecture in place
- ✅ Testing framework established
- ⏳ PG15 libraries to be built

## References
- libpg_query 15-latest: https://github.com/pganalyze/libpg_query/tree/15-latest
- PostgreSQL 15 Release Notes: https://www.postgresql.org/docs/15/release-15.html
- Current Implementation: See `docs\NATIVE_LIBRARY_MULTI_VERSION_COMPLETE.md`
- Version Isolation Tests: `tests\Npgquery.Tests\VersionIsolationVerificationTests.cs`

## Notes
- PG15 is on "Critical fixes only" support in libpg_query (not actively developed)
- Primary use case: Support legacy applications still on PG15
- Default version should remain PG16 for backwards compatibility
- All core parsing functionality should work identically across 15/16/17

---

**Labels**: enhancement, good first issue, multi-version-support  
**Milestone**: Future Enhancement  
**Priority**: Low (nice to have, not critical)
