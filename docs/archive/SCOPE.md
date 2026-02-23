# pgPacTool - Scope & Requirements

**Project:** PostgreSQL Data-Tier Application Compiler  
**Target Framework:** .NET 10  
**PostgreSQL Support:** Version 16+ Only  
**Last Updated:** 2026-01-31

---

## ?? Project Scope

### Supported Technologies

**PostgreSQL Versions:**
- ? **PostgreSQL 16** (Primary target, LTS until Nov 2028)
- ? **PostgreSQL 17+** (Future versions as released)
- ? PostgreSQL 15 and below - **NOT SUPPORTED**

**Rationale:**
- Simplified codebase (no version branching)
- Focus on latest features
- PostgreSQL 16 is LTS (5 years support)
- Reduces testing matrix complexity
- All modern features available (procedures, generated columns, etc.)

**.NET Version:**
- ? **.NET 10** (Target framework)
- Future .NET versions as released

---

## ?? PostgreSQL 16 Feature Support

### Fully Supported Features

**Database Objects:**
- ? Tables (including partitioned tables)
- ? Views (including materialized views)
- ? Functions (all languages: SQL, PL/pgSQL, Python, etc.)
- ? Stored Procedures (available since PG 11)
- ? Triggers (table and event triggers)
- ? Indexes (all types: btree, gin, gist, brin, hash, spgist, bloom)
- ? Constraints (FK, Check, Unique, Exclusion)
- ? Sequences (identity columns)
- ? Types (enums, composite types, domains, ranges)
- ? Schemas
- ? Roles and privileges
- ? Extensions

**PostgreSQL 16 Specific Features:**
- ? Logical replication improvements
- ? Performance enhancements
- ? Security improvements
- ? Monitoring improvements
- ? SQL/JSON support
- ? Regular expression improvements
- ? Aggregate function improvements

---

## ?? Out of Scope

### Unsupported PostgreSQL Versions
- ? PostgreSQL 15 and earlier
- No backward compatibility layer
- No version detection logic needed
- No conditional feature support

### Features Not Supported (Initially)
- ? Database replication configuration
- ? PostgreSQL configuration files (postgresql.conf, pg_hba.conf)
- ? Binary data (large objects, bytea content)
- ? Dynamic SQL execution monitoring
- ? Query optimization hints
- ? Custom background workers
- ? Foreign data wrappers (may be added later)
- ? Logical replication configuration (may be added later)

---

## ?? Technical Requirements

### Development Environment

**Required:**
- .NET 10 SDK
- PostgreSQL 16+ installed (local or Docker)
- Docker Desktop (for Testcontainers)
- Git

**Recommended:**
- Visual Studio 2024 or VS Code
- PostgreSQL client tools (psql)
- pgAdmin or DBeaver (for database management)

### Runtime Requirements

**Minimum:**
- .NET 10 Runtime
- Access to PostgreSQL 16+ server
- 512MB RAM (for CLI operations)
- 2GB disk space (for large databases)

**Recommended:**
- .NET 10 Runtime
- PostgreSQL 16+ server
- 2GB RAM
- 10GB disk space

---

## ?? Compatibility Matrix

### PostgreSQL 16 Compatibility

| Feature | PG 16 Support | Our Support |
|---------|---------------|-------------|
| Tables | ? | ? |
| Partitioned Tables | ? | ? |
| Views | ? | ? |
| Materialized Views | ? | ? |
| Functions | ? | ? |
| Procedures | ? | ? |
| Triggers | ? | ? |
| Event Triggers | ? | ? |
| Indexes (all types) | ? | ? |
| Constraints | ? | ? |
| Sequences | ? | ? |
| Identity Columns | ? | ? |
| Generated Columns | ? | ? |
| Enums | ? | ? |
| Composite Types | ? | ? |
| Domains | ? | ? |
| Range Types | ? | ? |
| Extensions | ? | ? |
| Row-Level Security | ? | ? |
| Inheritance | ? | ? |
| Foreign Keys | ? | ? |
| Check Constraints | ? | ? |
| Exclusion Constraints | ? | ? |

### .NET 10 Features Used

| Feature | Usage |
|---------|-------|
| Required Properties | ? Models |
| File-scoped Namespaces | ? All files |
| Global Usings | ? Enabled |
| Nullable Reference Types | ? Enabled |
| Records | ? DTOs |
| Pattern Matching | ? Parsing |
| Async/Await | ? All I/O |
| LINQ | ? Collections |
| Span<T> | ? Performance |

---

## ?? Simplified Architecture

### No Version Branching Needed

```csharp
// ? OLD WAY (Not needed anymore)
if (postgresVersion >= 11)
{
    await ExtractProceduresAsync();
}

// ? NEW WAY (PostgreSQL 16+ guaranteed)
await ExtractProceduresAsync(); // Always available
```

### Simplified Testing

```csharp
// ? OLD WAY (Not needed)
[TestCase("12")]
[TestCase("13")]
[TestCase("14")]
[TestCase("15")]
[TestCase("16")]
public void Test_MultipleVersions(string version) { }

// ? NEW WAY (Single version)
[Test]
public void Test_PostgreSQL16()
{
    // Only test PostgreSQL 16
}
```

---

## ?? Updated Testing Strategy

### Integration Tests

**Before (Multi-version):**
```csharp
[TestFixture]
[TestCase("12")]
[TestCase("13")]
[TestCase("14")]
[TestCase("15")]
[TestCase("16")]
public class ExtractionTests(string version)
{
    // Test against 5 versions
}
```

**After (Single version):**
```csharp
[TestFixture]
public class ExtractionTests
{
    private const string PostgreSqlVersion = "16";
    
    [SetUp]
    public async Task Setup()
    {
        _container = new PostgreSqlBuilder()
            .WithImage("postgres:16")
            .Build();
    }
}
```

**Benefits:**
- ? Faster test execution (5x faster)
- ? Simpler test infrastructure
- ? No version-specific edge cases
- ? Clearer test results

---

## ?? Version Detection

### Simplified Version Checking

```csharp
public async Task<string> DetectPostgresVersion()
{
    using var cmd = new NpgsqlCommand("SHOW server_version;", CreateConnection());
    var version = (string)await cmd.ExecuteScalarAsync();
    
    // Extract major version
    var major = int.Parse(version.Split('.')[0]);
    
    // Enforce minimum version
    if (major < 16)
    {
        throw new NotSupportedException(
            $"PostgreSQL {major} is not supported. " +
            "Please use PostgreSQL 16 or higher.");
    }
    
    return version;
}
```

---

## ?? Migration Path for Users

### For Users on PostgreSQL 15 or Earlier

**Option 1: Upgrade PostgreSQL**
```bash
# Backup current database
pg_dump mydb > mydb_backup.sql

# Install PostgreSQL 16
# See: https://www.postgresql.org/download/

# Restore to PostgreSQL 16
psql -U postgres -d mydb < mydb_backup.sql

# Now use pgPacTool
pgpac extract --connection "Host=localhost;Database=mydb"
```

**Option 2: Use pg_upgrade**
```bash
pg_upgrade \
  --old-datadir /var/lib/postgresql/15/data \
  --new-datadir /var/lib/postgresql/16/data \
  --old-bindir /usr/lib/postgresql/15/bin \
  --new-bindir /usr/lib/postgresql/16/bin
```

**Option 3: Manual Migration**
- Extract schema using pg_dump (older PostgreSQL)
- Review and adjust any deprecated features
- Import to PostgreSQL 16
- Use pgPacTool for ongoing management

---

## ?? Documentation Updates

### User-Facing Messages

**CLI Error Message:**
```
Error: PostgreSQL version 15 detected.

pgPacTool requires PostgreSQL 16 or higher.

To upgrade your PostgreSQL instance:
1. Backup your data: pg_dump mydb > backup.sql
2. Install PostgreSQL 16: https://www.postgresql.org/download/
3. Restore your data: psql -d mydb < backup.sql

For help: https://github.com/mbulava-org/pgPacTool/wiki/postgresql-upgrade
```

**README.md Requirements:**
```markdown
## Requirements

- PostgreSQL 16 or higher (required)
- .NET 10 SDK
- Docker Desktop (for development/testing)

Note: PostgreSQL 15 and earlier are not supported.
```

---

## ?? Benefits of PostgreSQL 16+ Only

### Development
- ? Simpler codebase (no version checks)
- ? Use latest PostgreSQL features
- ? No legacy compatibility code
- ? Faster development velocity

### Testing
- ? Single test matrix
- ? Faster test execution
- ? Simpler CI/CD pipeline
- ? No version-specific bugs

### Maintenance
- ? Fewer edge cases
- ? Easier to support
- ? Clear error messages
- ? Focus on features, not compatibility

### Performance
- ? Optimize for modern PostgreSQL
- ? Use latest performance features
- ? No fallback code paths
- ? Smaller binary size

---

## ?? Support Timeline

### PostgreSQL 16 LTS
- Released: September 2023
- End of Life: November 2028
- **5 years of support** ?

### Future Versions
- PostgreSQL 17: Will be supported when released (2024)
- PostgreSQL 18+: Will be supported as released
- Always support latest + LTS versions

---

## ?? Review Schedule

- **Monthly:** Check for PostgreSQL 17 release
- **Quarterly:** Review feature support
- **Annually:** Evaluate version support policy

---

**Document Version:** 1.0  
**Last Updated:** 2026-01-31  
**PostgreSQL Version Policy:** 16+ Only  
**Next Review:** After PostgreSQL 17 release
