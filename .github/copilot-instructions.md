# GitHub Copilot Instructions for pgPacTool

## Project Overview

pgPacTool is a PostgreSQL Data-Tier Application tool that brings SQL Server-style database project workflow to PostgreSQL. It includes MSBuild SDK integration, CLI tools, and multi-version PostgreSQL support.

**Key Technologies**:
- .NET 10
- PostgreSQL 15–18 (15 and 16 are baseline; 17 adds pg_maintain + VIRTUAL columns; 18 adds OAuth auth + pg_signal_autovacuum_worker)
- libpg_query native library integration
- MSBuild SDK for database projects

---


## Documentation Requirements

### 1. PostgreSQL Version Support

**Currently Supported**:
- ✅ PostgreSQL 15 (roles/security baseline)
- ✅ PostgreSQL 16 (default version)
- ✅ PostgreSQL 17 (with JSON_TABLE, pg_maintain, and new features)
- ✅ PostgreSQL 18 (OAuth auth, latest built-in roles)

**Not Yet Supported**:
- ❌ PostgreSQL 14 and earlier (may be added in future if there is demand)

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

## Roles, Permissions & Security — Version Rules

> **Full reference**: [`docs/version-differences/PG_ROLES_PERMISSIONS_SECURITY.md`](../docs/version-differences/PG_ROLES_PERMISSIONS_SECURITY.md)

### Quick Rules for Code Generation

1. **Never generate `CREATE ROLE` for any `pg_*` built-in role or Azure reserved role.**
   - Built-in roles to skip: `pg_monitor`, `pg_read_all_data`, `pg_write_all_data`,
     `pg_database_owner`, `pg_maintain` (17+), `pg_use_reserved_connections` (17+),
     `pg_create_subscription` (16+), `pg_checkpoint`, `pg_signal_backend`,
     `pg_signal_autovacuum_worker` (18+), and all other `pg_*` prefixed roles.
   - Azure-reserved roles to skip: `azure_pg_admin`, `azuresu`, `replication`, `localadmin`.

2. **`CREATEROLE` behavior changed in PG 16** — on PG 16+, only roles with `ADMIN OPTION` can
   grant that role's membership to others. Scripts must reflect this; do not assume unrestricted
   grant ability.

3. **`BYPASSRLS` is version-gated**:
   - PG 15: requires superuser to grant — emit a warning if non-superuser context is used.
   - PG 16+: non-superuser admins can grant `BYPASSRLS`.

4. **`WITH INHERIT`/`WITH SET` on GRANT** is PG 16+ only — omit these clauses in scripts
   targeting PG 15.

5. **`pg_maintain` (PG 17+)** — the correct way to delegate `VACUUM`/`ANALYZE`/`REINDEX`
   without ownership. Use it; do not grant ownership as a workaround.

6. **Public schema on PG 15+**: `CREATE` is revoked from `PUBLIC` by default. New DB setup
   scripts must include `REVOKE CREATE ON SCHEMA public FROM PUBLIC`.

7. **Password handling**: Never store or emit plaintext passwords in DDL. `PgRole.Password`
   must always be `null` in compare output.

8. **`pg_write_all_data` is blocked on Azure** — do not generate grants for it on Azure targets.

9. **Role DDL emit order**: `CREATE ROLE` → memberships (`GRANT role TO member`) → db-level
   grants → schema-level grants → object-level grants → default privileges → RLS → policies.

10. **`PgRole` model requires `CreateDb`, `CreateRole`, `ConnectionLimit`, `ValidUntil`,
    `IsBuiltIn`, `Comment`, and `Memberships` (list of `PgRoleMembership`) properties.**
    See full model spec in the reference document above.

---

## Database Objects — Version Rules

> **Full reference**: [`docs/version-differences/PG_DATABASE_OBJECTS.md`](../docs/version-differences/PG_DATABASE_OBJECTS.md)

### Quick Rules for Database Object Code Generation

1. **`VIRTUAL` generated columns are PG 17+ only.**
   - PG 15–16 only support `STORED` generated columns.
   - `STORED` and `VIRTUAL` are semantically different; flag as diff, never silently coerce.
   - Virtual columns cannot be indexed or used as foreign key columns.

2. **`NULLS NOT DISTINCT` on UNIQUE constraints/indexes is PG 15+ only.**
   - Omit this clause when generating scripts for PG 14 or earlier.

3. **Named `NOT NULL` constraints and `NO INHERIT` on constraints are PG 16+ only.**
   - On PG 15, `NOT NULL` is a column attribute, not a named constraint.

4. **`SECURITY_INVOKER` on views is PG 15+ only.**
   - Default for all versions is security-definer semantics (view owner's privileges).
   - Always compare `SecurityInvoker` as part of view diff.

5. **`MAINTAIN` privilege on tables is PG 17+ only.**
   - ACL abbreviation is `m`. Do not parse or emit `m` against PG 15–16 servers.
   - Per-table: `GRANT MAINTAIN ON TABLE t TO role`.
   - Per-role: grant `pg_maintain` membership.

6. **`INCLUDE` on BRIN indexes is PG 17+ only** (B-Tree/GiST/SP-GiST covering indexes work on all supported versions).

7. **`ALTER DEFAULT PRIVILEGES ON ROUTINES` is reliable only on PG 17+.**
   - On PG 15–16, use `FUNCTION` only; `PROCEDURE` behavior was unreliable.
   - On PG 18+, also support `LARGE OBJECTS` in `ALTER DEFAULT PRIVILEGES`.

8. **Sequence `AS <type>` (data type) is PG 16+ only.**
   - On PG 15, sequences are always `bigint`. Do not emit `AS bigint` on PG 15.
   - Extract `data_type` from `pg_sequences.data_type` on PG 16+.

9. **`WITHOUT OVERLAPS` exclusion constraints are PG 18+ only.**
   - Capture in `PgConstraint.Definition`; omit when targeting PG 15–17.

10. **`SECURITY DEFINER` functions must have `SET search_path`.**
    - PG 18 tightens enforcement: maintenance operations block unsafe `search_path` access.
    - Warn during compile/compare if a `SECURITY DEFINER` function lacks an explicit
      `SET search_path` clause.

11. **Function identity = name + argument types.** Overloaded functions are distinct objects.
    Use the full qualified signature as the compare key, not just the function name.

12. **`PgView.SecurityInvoker`, `PgSequence.DataType`, `PgColumn.GeneratedColumnKind`
    (`Stored`/`Virtual`) must be tracked as model properties.** See full model requirements
    in the reference document above.

---

## README.md Maintenance

When updating `README.md` in this repo, always keep the footer metadata lines for `Status`, `Version`, and `Last Updated` in sync with the current state of the document and release.

---

## Preferred Architecture

In this repo, the preferred architecture is for `MSBuild.Sdk.PostgreSql` to invoke `postgresPacTool.exe` rather than call `mbulava.PostgreSql.Dac.dll` directly, so verbose logging can be used consistently for compile, compare, and script generation diagnostics.

### Compile Operation Requirements
- The `compile` operation should print each database object's name, type, and source definition location.
- On compile errors, it should identify impacted source files and provide a useful schema/type object count summary at the end.

---

*Last Updated*: Current Session
*Maintained By*: pgPacTool Contributors
