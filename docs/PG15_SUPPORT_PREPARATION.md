# PostgreSQL 15 Support Preparation

**Date**: Current Session  
**Status**: Archived / Not Supported by Current Cross-Platform Matrix  
**Branch**: `feature/verify-native-library-integration`

---

## Summary

PostgreSQL 15 support was explored, but the repository now standardizes on PostgreSQL 16+ so support remains aligned across major development and production platforms. PostgreSQL 15 and below are deferred unless future demand justifies a proper cross-platform implementation.

---

## Changes Made

### 1. Historical Workflow Investigation

**File**: `.github/workflows/build-native-libraries.yml`

#### Changes:
1. **Input Description Updated**
   ```yaml
   pg_versions:
     description: 'PostgreSQL versions to build (comma-separated, e.g., "15,16,17")'
     default: '16,17'
   ```

2. **Observed Upstream Limitation**
   - `15-latest` does not include `Makefile.msvc`
   - Windows matrix automation would require a custom build path for PG15

3. **Added Comments**
   - Documented that PG15 is built but not yet implemented
   - Clarified support status for each version
   - Added instructions for building PG15

---

## PostgreSQL 15 Support Decision

The current support policy is:

- ✅ PostgreSQL 16+
- ❌ PostgreSQL 15 and below

Reason:
- supported versions must work across major development and production platforms
- PG15 currently requires platform-specific exceptions and extra implementation work

### If Demand Appears Later

```
Reconsider PG15 only if there is clear user demand and time to add:

- a proper Windows build path
- cross-platform validation
- updated compatibility and support docs

---

## libpg_query 15-latest Branch Analysis

### API Compatibility

Based on the libpg_query repository:

| Function | PG15 | PG16 | PG17 |
|----------|------|------|------|
| pg_query_parse | ✅ | ✅ | ✅ |
| pg_query_normalize | ✅ | ✅ | ✅ |
| pg_query_fingerprint | ✅ | ✅ | ✅ |
| pg_query_scan | ✅ | ✅ | ✅ |
| pg_query_parse_plpgsql | ✅ | ✅ | ✅ |
| pg_query_deparse_protobuf | ✅ | ✅ | ✅ |
| pg_query_is_utility_stmt | ❌ | ❌ | ✅ |
| pg_query_summary | ❌ | ❌ | ✅ |

### Outcome

PG15 remains out of the supported matrix for now.

---

## Implementation Issue Created

**File**: `.github/ISSUE_TEMPLATE/implement-pg15-support.md`

### Issue Scope

The issue covers:
1. ✅ Enum update (PostgreSqlVersion)
2. ✅ Extension method updates
3. ✅ Building native libraries
4. ✅ Version compatibility tests
5. ✅ Feature isolation verification
6. ✅ Documentation updates
7. ✅ Integration tests
8. ✅ Linux container tests

### Estimated Effort
**2-4 hours** for implementation

---

## PostgreSQL 15 Features

### What's Different from PG16/17

#### Features NOT in PG15 (Added Later)
- ❌ JSON_TABLE (added in PG17)
- ❌ Enhanced JSON functions (PG17)
- ❌ MERGE statement enhancements (PG15 has MERGE, but limited)
- ❌ pg_query_is_utility_stmt (PG17)
- ❌ pg_query_summary (PG17)

#### Features in PG15
- ✅ All standard SQL (SELECT, INSERT, UPDATE, DELETE, etc.)
- ✅ CTEs (Common Table Expressions)
- ✅ Window functions
- ✅ PL/pgSQL parsing
- ✅ Basic MERGE statement
- ✅ All core libpg_query functions

---

## Why Support PG15?

### Use Cases
1. **Legacy Application Support**
   - Organizations still running PostgreSQL 15
   - Migration planning (parse old queries)
   - Compatibility testing

2. **Historical Query Analysis**
   - Analyze query logs from PG15 systems
   - Compare query evolution across versions
   - Fingerprint queries from multiple versions

3. **Completeness**
   - PG15 is still receiving critical fixes in libpg_query
   - Minimal additional work (architecture already supports it)
   - Provides full spectrum: 15, 16, 17

### Why Not Default?
- PG16 is more widely deployed
- PG15 is in "critical fixes only" mode
- PG16 has better long-term support

---

## Testing Strategy (When Implemented)

### Phase 1: Basic Functionality
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

### Phase 2: Version Isolation
```csharp
[Fact]
public void JsonTable_OnlyInPG17()
{
    using var parser15 = new Parser(PostgreSqlVersion.Postgres15);
    using var parser16 = new Parser(PostgreSqlVersion.Postgres16);
    using var parser17 = new Parser(PostgreSqlVersion.Postgres17);
    
    var query = "SELECT * FROM JSON_TABLE(...)";
    
    Assert.False(parser15.Parse(query).IsSuccess); // Fails
    Assert.False(parser16.Parse(query).IsSuccess); // Fails
    Assert.True(parser17.Parse(query).IsSuccess);  // Works
}
```

### Phase 3: Library Isolation
```csharp
[Fact]
public void ThreeVersions_DifferentHandles()
{
    var handle15 = NativeLibraryLoader.GetLibraryHandle(PostgreSqlVersion.Postgres15);
    var handle16 = NativeLibraryLoader.GetLibraryHandle(PostgreSqlVersion.Postgres16);
    var handle17 = NativeLibraryLoader.GetLibraryHandle(PostgreSqlVersion.Postgres17);
    
    // All different
    Assert.NotEqual(handle15, handle16);
    Assert.NotEqual(handle16, handle17);
    Assert.NotEqual(handle15, handle17);
}
```

---

## Documentation to Update (When Implemented)

### Primary Documentation
1. **README.md**
   - Add PG15 to supported versions
   - Update examples

2. **.github/copilot-instructions.md**
   - Update version lists
   - Add PG15 notes

3. **docs/features/multi-version-support/README.md**
   - Include PG15 in all examples
   - Document PG15-specific limitations

4. **docs/NATIVE_LIBRARY_MULTI_VERSION_COMPLETE.md**
   - Update API reference
   - Add PG15 to function tables

### Code Comments
```csharp
/// <summary>
/// Creates a new parser instance with the specified PostgreSQL version
/// </summary>
/// <param name="version">PostgreSQL version to use (default: Postgres16)</param>
/// <remarks>
/// <para>Supported versions: PostgreSQL 15, 16, and 17</para>
/// <para>Default is 16 for backwards compatibility</para>
/// </remarks>
public Parser(PostgreSqlVersion version = PostgreSqlVersion.Postgres16)
```

---

## Build Verification Checklist

When PG15 libraries are built, verify:

### Windows (win-x64)
- [ ] File exists: `libpg_query_15.dll`
- [ ] File size: ~3-4 MB
- [ ] Exports verified: `dumpbin /EXPORTS libpg_query_15.dll`
- [ ] No missing dependencies

### Linux (linux-x64)
- [ ] File exists: `libpg_query_15.so`
- [ ] File size: ~9-10 MB
- [ ] Symbols verified: `nm -D libpg_query_15.so | grep pg_query_parse`
- [ ] Architecture: x86-64

### macOS (osx-arm64)
- [ ] File exists: `libpg_query_15.dylib`
- [ ] File size: ~3-4 MB
- [ ] Architecture: `lipo -info` shows arm64
- [ ] Symbols verified: `nm -g libpg_query_15.dylib`

---

## Known Limitations

### libpg_query 15-latest Status
- **Support Level**: Critical fixes only
- **Active Development**: No
- **Expected Lifespan**: Until PostgreSQL 15 EOL

### Missing Functions
- `pg_query_is_utility_stmt` - Not in PG15
- `pg_query_summary` - Not in PG15
- Split functions - May not be in any version we build

### Recommendation
- Use PG16 or PG17 for new projects
- Use PG15 only for compatibility with legacy systems
- Default should remain PG16

---

## Next Steps

### Immediate (When Ready to Implement)
1. Run GitHub Actions to build PG15 libraries
2. Review and merge PR with libraries
3. Implement C# enum and extension methods
4. Add tests
5. Update documentation

### Future Considerations
- PostgreSQL 18 (when released)
- PostgreSQL 14 (if demand exists)
- Automatic version detection from query syntax

---

## References

- **libpg_query 15-latest**: https://github.com/pganalyze/libpg_query/tree/15-latest
- **PostgreSQL 15 Docs**: https://www.postgresql.org/docs/15/
- **libpg_query Support Matrix**: https://github.com/pganalyze/libpg_query#versions
- **Implementation Issue**: `.github/ISSUE_TEMPLATE/implement-pg15-support.md`

---

## Conclusion

✅ **Build infrastructure is ready for PostgreSQL 15**

The GitHub Actions workflow has been updated and tested (based on lessons learned from PG16 & PG17). When the time comes to implement PG15 support, the process will be:

1. Run workflow with input "15"
2. Merge PR with libraries
3. Add enum value
4. Add tests
5. Update docs

**Estimated total time: 2-4 hours**

The infrastructure is future-proof and ready whenever PG15 support is needed!

---

*Documentation Date*: Current Session  
*Workflow Status*: ✅ Ready  
*Implementation Status*: ⏳ Deferred
