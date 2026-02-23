# pgPacTool User Guide

**Version:** 0.1.0  
**Last Updated:** 2026-01-31

---

## Table of Contents

- [Introduction](#introduction)
- [Installation](#installation)
- [Getting Started](#getting-started)
- [Current Capabilities](#current-capabilities)
- [Usage Scenarios](#usage-scenarios)
- [Limitations](#limitations)
- [Troubleshooting](#troubleshooting)

---

## Introduction

pgPacTool is a .NET library for extracting, comparing, and managing PostgreSQL database schemas. Think of it as "SQL Server Data Tools (.sqlproj) for PostgreSQL."

### What Can It Do Now?

✅ **Milestone 1 Complete** - Core Extraction Functionality

- Extract complete database schemas with all metadata
- Support for tables, views, functions, triggers, sequences, and types
- Privilege and role extraction
- Schema comparison (basic)
- Save/load schema snapshots as JSON

### What's Coming?

🚧 Future milestones will add:

- Automated migration script generation
- Deployment automation
- NuGet package distribution
- MSBuild SDK integration
- CLI tool

---

## Installation

### Requirements

- **.NET 10 SDK** or higher
- **PostgreSQL 16+** database server
- **Npgsql** (included as dependency)

### Add to Your Project

```bash
# Not yet published to NuGet - currently source-only
# Clone the repository and reference the project:

git clone https://github.com/mbulava-org/pgPacTool.git
cd pgPacTool
```

Add project reference to your application:

```xml
<ItemGroup>
  <ProjectReference Include="..\path\to\mbulava.PostgreSql.Dac\mbulava.PostgreSql.Dac.csproj" />
</ItemGroup>
```

---

## Getting Started

### Quick Start: Extract a Database

```csharp
using mbulava.PostgreSql.Dac.Extract;
using mbulava.PostgreSql.Dac.Models;

// 1. Create connection string
var connectionString = "Host=localhost;Database=mydb;Username=postgres;Password=secret";

// 2. Create extractor
var extractor = new PgProjectExtractor(connectionString);

// 3. Extract database
var project = await extractor.ExtractPgProject("mydb");

// 4. Save to file
await using var file = File.Create("mydb.pgpac");
await PgProject.Save(project, file);

Console.WriteLine($"Extracted {project.Schemas.Count} schemas");
```

### Understanding the Output

The `.pgpac` file is a JSON document containing:

```json
{
  "DatabaseName": "mydb",
  "PostgresVersion": "16.1",
  "Schemas": [
    {
      "Name": "public",
      "Owner": "postgres",
      "Tables": [...],
      "Views": [...],
      "Functions": [...],
      "Types": [...],
      "Sequences": [...],
      "Triggers": [...]
    }
  ],
  "Roles": [...]
}
```

Each object includes:
- **Original SQL definition**
- **Parsed AST (Abstract Syntax Tree)**
- **Metadata** (owner, privileges, etc.)

---

## Current Capabilities

### 1. Database Extraction

Extract a complete snapshot of your database:

```csharp
var extractor = new PgProjectExtractor(connectionString);
var project = await extractor.ExtractPgProject("mydb");
```

**Extracted Information:**

| Object Type | Information Captured |
|-------------|---------------------|
| **Schemas** | Name, owner, privileges, AST |
| **Tables** | Columns, constraints, indexes, partitioning, RLS, privileges |
| **Views** | Definition, owner, privileges |
| **Functions** | Body, parameters, return type, language, privileges |
| **Procedures** | Same as functions (PG treats them the same) |
| **Triggers** | Event, timing, function reference |
| **Sequences** | Start, increment, min/max, cycle, cache |
| **Types** | Domain, enum, composite definitions |
| **Roles** | Attributes, memberships, privileges |

### 2. Version Detection

Automatically detect and validate PostgreSQL version:

```csharp
var version = await extractor.DetectPostgresVersion();
Console.WriteLine($"PostgreSQL {version}");
// Output: "PostgreSQL 16.1" or throws if < 16
```

### 3. Schema Comparison

Compare two database snapshots:

```csharp
using mbulava.PostgreSql.Dac.Compare;

var comparer = new PgSchemaComparer();
var result = comparer.CompareDatabases(sourceProject, targetProject);

// Check for differences
if (result.SchemaDifferences.Any())
{
    Console.WriteLine("Schemas changed");
}

if (result.TableDifferences.Any())
{
    Console.WriteLine("Tables changed");
}
```

**Detected Changes:**

- Missing or extra schemas
- Schema ownership changes
- Table structure changes (columns, constraints, indexes)
- Privilege modifications
- View, function, type, sequence changes

### 4. Privilege Analysis

Analyze database privileges:

```csharp
// Find all privileges for a table
var table = schema.Tables.First(t => t.Name == "users");
foreach (var priv in table.Privileges)
{
    Console.WriteLine($"{priv.Grantee}: {priv.PrivilegeType}" +
                     (priv.IsGrantable ? " (can grant)" : ""));
}

// Find objects a role can access
var userTables = schema.Tables
    .Where(t => t.Privileges.Any(p => p.Grantee == "app_user"))
    .Select(t => t.Name);
```

### 5. Dependency Analysis

Understand table relationships:

```csharp
// Find all foreign keys
foreach (var table in schema.Tables)
{
    foreach (var fk in table.ForeignKeys)
    {
        Console.WriteLine($"{table.Name} -> {fk.ReferencedTable}");
    }
}

// Get primary key
var pk = table.PrimaryKey;
if (pk != null)
{
    Console.WriteLine($"Primary key: {pk.Name}");
}
```

---

## Usage Scenarios

### Scenario 1: Database Backup/Snapshot

Create a versioned snapshot of your database schema:

```csharp
var extractor = new PgProjectExtractor(connectionString);
var project = await extractor.ExtractPgProject("production_db");

var filename = $"production_db_{DateTime.Now:yyyyMMdd_HHmmss}.pgpac";
await using var file = File.Create(filename);
await PgProject.Save(project, file);

Console.WriteLine($"Snapshot saved: {filename}");
```

**Use case:** Store in version control to track schema changes over time.

### Scenario 2: Environment Comparison

Compare dev, staging, and production environments:

```csharp
// Extract from multiple environments
var devProject = await devExtractor.ExtractPgProject("dev_db");
var prodProject = await prodExtractor.ExtractPgProject("prod_db");

// Compare
var comparer = new PgSchemaComparer();
var differences = comparer.CompareDatabases(prodProject, devProject);

// Report
Console.WriteLine($"Dev is {differences.TableDifferences.Count} tables behind prod");
```

### Scenario 3: Migration Planning

Understand what needs to change:

```csharp
var current = await currentExtractor.ExtractPgProject("current_db");
var target = await PgProject.Load(File.OpenRead("target_schema.pgpac"));

var differences = comparer.CompareDatabases(target, current);

// Analyze changes needed
foreach (var diff in differences.TableDifferences)
{
    Console.WriteLine($"Table {diff.ObjectName}:");
    Console.WriteLine($"  - {diff.ColumnChanges.Count} column changes");
    Console.WriteLine($"  - {diff.ConstraintChanges.Count} constraint changes");
    Console.WriteLine($"  - {diff.IndexChanges.Count} index changes");
}
```

### Scenario 4: Security Audit

Review database permissions:

```csharp
var project = await extractor.ExtractPgProject("secure_db");

// Find all PUBLIC grants
foreach (var schema in project.Schemas)
{
    foreach (var table in schema.Tables)
    {
        var publicPrivs = table.Privileges.Where(p => p.Grantee == "PUBLIC");
        if (publicPrivs.Any())
        {
            Console.WriteLine($"⚠️ {schema.Name}.{table.Name} has PUBLIC grants:");
            foreach (var priv in publicPrivs)
            {
                Console.WriteLine($"   - {priv.PrivilegeType}");
            }
        }
    }
}

// Find all superusers
var superusers = project.Roles.Where(r => r.IsSuperUser);
Console.WriteLine($"\nSuperusers: {string.Join(", ", superusers.Select(r => r.Name))}");
```

### Scenario 5: Documentation Generation

Generate schema documentation:

```csharp
var project = await extractor.ExtractPgProject("mydb");

foreach (var schema in project.Schemas)
{
    Console.WriteLine($"# Schema: {schema.Name}\n");
    Console.WriteLine($"**Owner:** {schema.Owner}\n");
    
    Console.WriteLine("## Tables\n");
    foreach (var table in schema.Tables)
    {
        Console.WriteLine($"### {table.Name}\n");
        Console.WriteLine("| Column | Type | Nullable | Default |");
        Console.WriteLine("|--------|------|----------|---------|");
        
        foreach (var col in table.Columns)
        {
            Console.WriteLine($"| {col.Name} | {col.DataType} | " +
                            $"{(col.IsNotNull ? "NOT NULL" : "NULL")} | " +
                            $"{col.DefaultExpression ?? "-"} |");
        }
        Console.WriteLine();
    }
}
```

---

## Limitations

### Current Limitations (v0.1.0)

⚠️ **Not Yet Supported:**

- **Automated migration scripts** - Manual diff review only (coming in Milestone 3)
- **Deployment automation** - No `publish` command yet (coming in Milestone 4)
- **CLI tool** - Currently library-only (coming in later milestones)
- **MSBuild integration** - No `.pgproj` file support yet (coming in Milestone 6)
- **NuGet packages** - Not published yet (coming in Milestone 5)
- **Incremental updates** - Full extraction only
- **PostgreSQL < 16** - Not supported (by design)

### Performance Considerations

- **Large databases** (1000+ tables): Extraction may take several minutes
- **Memory usage**: Entire schema loaded into memory
- **Network latency**: Each object type requires database queries
- **Connection pooling**: Automatically handled by Npgsql

### Known Issues

1. **Empty Program.cs** - CLI tool is placeholder only
2. **Comparison is basic** - No script generation yet
3. **No rollback support** - Coming in future milestones

---

## Troubleshooting

### Error: "PostgreSQL version 15.x is not supported"

**Cause:** Database is running PostgreSQL < 16

**Solution:**
1. Upgrade to PostgreSQL 16 or higher
2. pgPacTool requires PG 16+ for modern catalog queries

### Error: "Connection refused"

**Cause:** Cannot connect to database

**Solution:**
1. Verify connection string
2. Check PostgreSQL is running: `pg_isready`
3. Verify firewall rules
4. Check `pg_hba.conf` for authentication settings

### Error: "Permission denied for table pg_..."

**Cause:** User lacks privileges to read system catalogs

**Solution:**
1. Grant `CONNECT` privilege: `GRANT CONNECT ON DATABASE mydb TO user;`
2. Ensure user can read system catalogs (default for all users)
3. For best results, use a superuser for extraction

### Error: "Invalid SQL for table..."

**Cause:** SQL parser failed to parse generated SQL

**Solution:**
1. This is likely a bug - please report it
2. Include the table definition in your bug report
3. Check for unsupported PostgreSQL features

### Slow Extraction Performance

**Symptoms:** Extraction takes > 5 minutes

**Possible Causes:**
1. Very large database (1000+ tables)
2. Network latency
3. Database under heavy load

**Solutions:**
1. Run during off-peak hours
2. Use a local database for testing
3. Consider filtering to specific schemas (future feature)

### Out of Memory Errors

**Symptoms:** `OutOfMemoryException` during extraction

**Possible Causes:**
1. Extremely large database
2. Large text columns in definitions
3. 32-bit process (should use 64-bit)

**Solutions:**
1. Increase available memory
2. Run on 64-bit .NET
3. Contact maintainers for guidance on large databases

---

## Best Practices

### 1. Version Control Your Schemas

```bash
# Extract schema
dotnet run -- extract -c "Host=localhost;Database=mydb;..." -o mydb.pgpac

# Commit to git
git add mydb.pgpac
git commit -m "Schema snapshot 2026-01-31"
```

### 2. Regular Snapshots

Create automated snapshots:

```csharp
// Schedule this to run daily
var filename = $"backup_{DateTime.Now:yyyyMMdd}.pgpac";
var project = await extractor.ExtractPgProject("prod_db");
await using var file = File.Create($"/backups/{filename}");
await PgProject.Save(project, file);
```

### 3. Compare Before Deployment

Always compare before deploying:

```csharp
var current = await prodExtractor.ExtractPgProject("production");
var target = await PgProject.Load(File.OpenRead("v2.0-schema.pgpac"));

var diff = comparer.CompareDatabases(target, current);
if (diff.HasChanges())
{
    Console.WriteLine("⚠️ Schema changes detected - review before deploying");
}
```

### 4. Security Audits

Regularly audit permissions:

```csharp
var project = await extractor.ExtractPgProject("secure_db");

// Check for PUBLIC grants
var publicGrants = project.Schemas
    .SelectMany(s => s.Tables)
    .Where(t => t.Privileges.Any(p => p.Grantee == "PUBLIC"))
    .ToList();

if (publicGrants.Any())
{
    Console.WriteLine($"⚠️ {publicGrants.Count} tables have PUBLIC grants");
}
```

### 5. Use Connection Pooling

Npgsql automatically pools connections:

```csharp
// Reuse extractor for multiple operations
var extractor = new PgProjectExtractor(connectionString);

var db1 = await extractor.ExtractPgProject("db1");
var db2 = await extractor.ExtractPgProject("db2");
var db3 = await extractor.ExtractPgProject("db3");
```

---

## Examples

### Example 1: Simple Extraction

```csharp
using mbulava.PostgreSql.Dac.Extract;
using mbulava.PostgreSql.Dac.Models;

var connStr = "Host=localhost;Database=mydb;Username=postgres;Password=secret";
var extractor = new PgProjectExtractor(connStr);
var project = await extractor.ExtractPgProject("mydb");

Console.WriteLine($"Database: {project.DatabaseName}");
Console.WriteLine($"Version: {project.PostgresVersion}");
Console.WriteLine($"Schemas: {project.Schemas.Count}");
```

### Example 2: Find All Foreign Keys

```csharp
var project = await extractor.ExtractPgProject("mydb");

foreach (var schema in project.Schemas)
{
    foreach (var table in schema.Tables)
    {
        if (table.ForeignKeys.Any())
        {
            Console.WriteLine($"\n{schema.Name}.{table.Name}:");
            foreach (var fk in table.ForeignKeys)
            {
                Console.WriteLine($"  → {fk.ReferencedTable}");
            }
        }
    }
}
```

### Example 3: Export to CSV

```csharp
var project = await extractor.ExtractPgProject("mydb");

using var csv = new StreamWriter("tables.csv");
csv.WriteLine("Schema,Table,Columns,Indexes,Owner");

foreach (var schema in project.Schemas)
{
    foreach (var table in schema.Tables)
    {
        csv.WriteLine($"{schema.Name},{table.Name}," +
                     $"{table.Columns.Count},{table.Indexes.Count}," +
                     $"{table.Owner}");
    }
}
```

---

## Testing

### Running Tests

```bash
# All tests
dotnet test

# Integration tests only (requires PostgreSQL)
dotnet test --filter "Category=Integration"

# Smoke tests (quick validation)
dotnet test --filter "Category=Smoke"

# Specific PostgreSQL version
dotnet test --filter "Category=Postgres16"
```

### Setting Up Test Database

```bash
# Using Docker
docker run --name pgpac-test \
  -e POSTGRES_PASSWORD=testpass \
  -p 5432:5432 \
  -d postgres:16

# Create test database
psql -U postgres -h localhost -c "CREATE DATABASE testdb;"
```

---

## Getting Help

### Resources

- **Documentation**: See `/docs` folder
- **API Reference**: [API_REFERENCE.md](API_REFERENCE.md)
- **Examples**: `/examples` folder (coming soon)
- **Issues**: https://github.com/mbulava-org/pgPacTool/issues

### Reporting Bugs

Please include:
1. PostgreSQL version
2. .NET version
3. Connection string (redact credentials)
4. Error message and stack trace
5. Sample database schema (if possible)

### Feature Requests

Check the [roadmap](../.github/ROADMAP.md) first - your feature may already be planned!

---

## What's Next?

### Upcoming Milestones

**Milestone 2** (v0.2.0) - Compilation & Validation
- Dependency validation
- Circular dependency detection
- Build artifacts

**Milestone 3** (v0.3.0) - Schema Comparison & Scripts
- Automated migration script generation
- Pre/post deployment scripts
- SQLCMD variables

**Milestone 4** (v0.4.0) - Deployment
- Deployment automation
- Rollback support
- Publishing profiles

**Milestone 5** (v0.5.0) - Packaging
- NuGet package distribution
- Package references
- System database references

**Milestone 6** (v1.0.0) - MSBuild SDK
- `.pgproj` file support
- MSBuild integration
- Project templates

See [ROADMAP.md](../.github/ROADMAP.md) for full timeline.

---

## Contributing

Contributions welcome! See [CONTRIBUTING.md](../CONTRIBUTING.md) (coming soon).

---

## License

MIT License - see [LICENSE](../LICENSE) for details.

---

**Last Updated:** 2026-01-31  
**Version:** 0.1.0  
**Status:** Milestone 1 Complete

**Happy database management! 🐘🔧**
