# GitHub Copilot Instructions for pgPacTool

## Project Overview

pgPacTool is a PostgreSQL Data-Tier Application tool that brings SQL Server-style database project workflow to PostgreSQL. It includes MSBuild SDK integration, CLI tools, and multi-version PostgreSQL support.

**Key Technologies**:
- .NET 10
- PostgreSQL 16+ (currently 16 and 17)
- libpg_query native library integration
- MSBuild SDK for database projects

---

## External Source Code References

⚠️ **CRITICAL**: When working on this project, you should examine these external repositories for context, implementation details, and understanding:

### 1. libpg_query (PRIMARY REFERENCE)
**Repository**: https://github.com/pganalyze/libpg_query

**Purpose**: 
- PostgreSQL query parser wrapped as a C library
- Source of native libraries we build and integrate
- Contains protobuf definitions for parse trees
- **MUST CHECK** when adding PostgreSQL versions or analyzing breaking changes

**When to Examine**:
- ✅ Before adding any new PostgreSQL version
- ✅ When analyzing version differences (breaking changes)
- ✅ When troubleshooting parsing issues
- ✅ When understanding protobuf schema changes
- ✅ When building native libraries
- ✅ When investigating API changes between versions

**Key Files to Check**:
- `protobuf/pg_query.proto` - Protobuf schema definitions
- `pg_query.h` - C API signatures
- `src/postgres/include/nodes/` - Node type definitions
- `CHANGELOG.md` - Version change history
- `{version}-latest` branches - Version-specific code

**Commands to Analyze**:
```bash
# Compare versions
git diff 16-latest..17-latest -- protobuf/
git diff 16-latest..17-latest -- pg_query.h
git diff 16-latest..17-latest -- src/postgres/include/nodes/
```

### 2. Npgquery (Original C# Wrapper)
**Repository**: https://github.com/JaredMSFT/Npgquery

**Purpose**:
- Original C# wrapper for libpg_query
- Reference implementation for interop
- Understanding .NET integration patterns

**When to Examine**:
- ✅ When understanding C# interop patterns
- ✅ When adding new native function wrappers
- ✅ When troubleshooting marshalling issues
- ✅ When implementing new features based on libpg_query APIs
- ✅ For P/Invoke signature reference

**Key Areas**:
- Native method declarations
- Marshalling strategies
- Memory management patterns
- Error handling approaches

### 3. Using External References

**Before Any Major Change**:
1. **Check libpg_query** for the specific PostgreSQL version branch
2. **Review CHANGELOG.md** for known breaking changes
3. **Compare protobuf schemas** between versions
4. **Check C API signatures** for new/removed functions
5. **Look at Npgquery** for existing implementation patterns

**Analysis Pattern**:
```
Question → Check libpg_query → Understand changes → Update our code → Document
```

**Example Workflow**:
```
Task: Add PostgreSQL 18 support

1. Visit https://github.com/pganalyze/libpg_query
2. Check if 18-latest branch exists
3. Compare: git diff 17-latest..18-latest
4. Review protobuf schema changes
5. Document breaking changes in docs/version-differences/PG18_CHANGES.md
6. Update our models and compatibility layers
7. Build native libraries
8. Test
```

---

## Documentation Requirements

### 1. PostgreSQL Version Support

**Currently Supported**:
- ✅ PostgreSQL 16 (default version)
- ✅ PostgreSQL 17 (with JSON_TABLE and new features)

**Not Yet Supported**:
- ❌ PostgreSQL 14 and 15 (may be added in future if there is demand)

**When Writing Documentation or Code Examples**:

✅ **DO**:
- Reference PostgreSQL 16 and 17 as the supported versions
- Use PostgreSQL 18 as an example of future versions
- Note that older versions (14, 15) may be added later if needed
- Always specify which versions a feature requires
- Default to PostgreSQL 16 for backward compatibility

❌ **DON'T**:
- Show code examples with unsupported versions (14, 15)
- Assume version compatibility without testing
- Reference PostgreSQL versions without availability context
- Imply that 14 or 15 are currently available

**Code Example Standards**:

```csharp
// ✅ GOOD - Shows supported versions
using var parser = new Parser(PostgreSqlVersion.Postgres16); // Default
using var parser17 = new Parser(PostgreSqlVersion.Postgres17); // PG 17 features

// ❌ BAD - Shows unsupported versions
using var parser14 = new Parser(PostgreSqlVersion.Postgres14); // NOT SUPPORTED
```

### 2. Documentation Organization

**File Structure**:
```
pgPacTool/
├── README.md (main project overview with version support note)
├── docs/
│   ├── README.md (documentation index)
│   ├── features/
│   │   └── {feature-name}/
│   │       ├── README.md (feature navigation)
│   │       └── (feature-specific docs)
│   ├── version-differences/
│   │   └── PG{version}_CHANGES.md
│   └── DOCUMENTATION_UPDATE_SUMMARY.md
```

**Rules**:
1. **Feature Documentation** → Put in `docs/features/{feature-name}/`
2. **Tracking Documents** → Keep in feature folders, not root `docs/`
3. **Version Analysis** → Put in `docs/version-differences/`
4. **Root README** → Should link to feature documentation

**Current Features**:
- `docs/features/multi-version-support/` - Multi-version PostgreSQL support

### 3. Version Compatibility Analysis

**Before Adding Any PostgreSQL Version**:

1. **ALWAYS run version difference analysis first**:
   ```powershell
   .\scripts\Analyze-VersionDifferences.ps1 -BaseVersion 16 -CompareVersion 17 -Detailed
   ```

2. **Review generated reports** in `docs/version-differences/`

3. **Check for breaking changes**:
   - Protobuf schema changes
   - New/removed node types
   - API signature changes
   - Field name modifications

4. **Update models if needed**:
   - Create version-specific models if protobuf changed
   - Add compatibility layers for API changes
   - Create version-specific tests

5. **Document all differences** in `docs/version-differences/PG{version}_CHANGES.md`

**Key Resource**: See `docs/features/multi-version-support/VERSION_COMPATIBILITY_STRATEGY.md`

### 4. Code Documentation Standards

**XML Comments**:
- All public APIs must have XML documentation
- Include `<summary>`, `<param>`, and `<returns>` tags
- Document exceptions with `<exception>` tags
- Include examples in `<example>` tags for complex features

**Version-Specific Features**:
```csharp
/// <summary>
/// Parses PostgreSQL queries using the specified version parser.
/// </summary>
/// <param name="query">SQL query to parse</param>
/// <param name="version">PostgreSQL version (default: 16)</param>
/// <returns>Parse result with AST</returns>
/// <remarks>
/// <para>Supported versions: PostgreSQL 16 and 17</para>
/// <para>Some features are version-specific (e.g., JSON_TABLE in PG 17+)</para>
/// </remarks>
/// <example>
/// <code>
/// using var parser = new Parser(PostgreSqlVersion.Postgres17);
/// var result = parser.Parse("SELECT * FROM JSON_TABLE(...)");
/// </code>
/// </example>
```

### 5. Testing Requirements

**Version Compatibility Tests**:
- Test all features across supported versions (16, 17)
- Mark version-specific tests with attributes
- Test backward compatibility
- Verify error handling for missing versions

**Example**:
```csharp
[Theory]
[InlineData(PostgreSqlVersion.Postgres16)]
[InlineData(PostgreSqlVersion.Postgres17)]
public void BasicSQL_WorksAcrossAllVersions(PostgreSqlVersion version)
{
    // Test implementation
}

[Fact]
public void JsonTable_OnlyWorksInPG17()
{
    // PG 16 should fail
    using var parser16 = new Parser(PostgreSqlVersion.Postgres16);
    Assert.False(parser16.Parse(jsonTableQuery).IsSuccess);
    
    // PG 17 should succeed
    using var parser17 = new Parser(PostgreSqlVersion.Postgres17);
    Assert.True(parser17.Parse(jsonTableQuery).IsSuccess);
}
```

### 6. Build and CI/CD

**GitHub Actions**:
- Manual workflow trigger only for native library builds
- Requires version input (e.g., "16,17")
- Builds for Windows, Linux, macOS
- Creates PR with built libraries

**Local Development**:
```powershell
# Build native libraries locally
.\scripts\Build-NativeLibraries.ps1 -Versions "16,17"

# Build project
dotnet build

# Run tests
dotnet test
```

### 7. Linking Between Documents

**From Root README**:
```markdown
See [Multi-Version Support](docs/features/multi-version-support/README.md)
```

**From Feature Docs**:
```markdown
See main [README](../../../README.md)
```

**Within Feature Docs**:
```markdown
See [Quick Reference](QUICK_REFERENCE.md)
```

### 8. Version Reference Examples

**In Documentation**:

✅ **Good Examples**:
- "Supported versions: PostgreSQL 16 and 17"
- "Future versions (18+) can be added easily"
- "Older versions (14, 15) may be added if there is demand"

❌ **Bad Examples**:
- "Supports PostgreSQL 14, 15, 16, 17" (14-15 not supported)
- "Works with all PostgreSQL versions" (not tested)
- Showing code with `Postgres14` or `Postgres15` enum values

**In Code**:

✅ **Good**:
```csharp
// Currently supported: PostgreSQL 16 (default) and 17
public Parser(PostgreSqlVersion version = PostgreSqlVersion.Postgres16)
```

❌ **Bad**:
```csharp
// Supports all versions 14-17
public Parser(PostgreSqlVersion version = PostgreSqlVersion.Postgres14) // ❌ 14 not supported
```

---

## Multi-Version Support Deep Analysis Requirement

⚠️ **CRITICAL**: When working on multi-version support, ALWAYS:

1. **Check libpg_query repository** at https://github.com/pganalyze/libpg_query
2. **Analyze version differences** before adding new versions
3. **Expect breaking changes** between PostgreSQL versions
4. **Create version-specific models** if protobuf schemas differ
5. **Add compatibility layers** for API differences
6. **Test across all supported versions**

**Key Files**:
- `docs/features/multi-version-support/VERSION_COMPATIBILITY_STRATEGY.md`
- `docs/features/multi-version-support/VERSION_COMPATIBILITY_CRITICAL.md`
- `docs/version-differences/PG17_CHANGES.md`

---

## Quick Reference

**Documentation Navigation**:
- Main: `README.md`
- Docs Index: `docs/README.md`
- Multi-Version: `docs/features/multi-version-support/README.md`
- Quick Ref: `docs/features/multi-version-support/QUICK_REFERENCE.md`

**Supported PostgreSQL Versions**:
- 16 (default)
- 17

**Not Supported** (but can be added):
- 14, 15 (if demand exists)

**Build Commands**:
```powershell
# Project build
dotnet build

# Run tests
dotnet test

# Build native libraries (manual)
.\scripts\Build-NativeLibraries.ps1 -Versions "16,17"
```

**GitHub Actions**:
- Actions → Build Native libpg_query Libraries → Run workflow
- Manual trigger only
- Specify versions: "16,17"

---

## For Future AI Sessions

When working on this project:

1. ✅ **Always check version compatibility first**
2. ✅ **Reference only supported versions (16, 17)**
3. ✅ **Run analysis before adding versions**
4. ✅ **Document in feature folders**
5. ✅ **Test across all versions**
6. ✅ **Update version references accurately**
7. ✅ **Link documentation clearly**

**Key Principle**: PostgreSQL versions have breaking changes - always analyze and test!

---

*Last Updated*: Current Session
*Maintained By*: pgPacTool Contributors
