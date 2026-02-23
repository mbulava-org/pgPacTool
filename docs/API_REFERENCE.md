# pgPacTool API Reference

**Version:** 0.1.0 (Milestone 1 Complete)  
**Last Updated:** 2026-01-31

---

## Table of Contents

- [Overview](#overview)
- [Core Components](#core-components)
- [Extraction API](#extraction-api)
- [Comparison API](#comparison-api)
- [Compilation API](#compilation-api)
- [Data Models](#data-models)
- [Usage Examples](#usage-examples)

---

## Overview

pgPacTool provides a comprehensive API for extracting, comparing, and managing PostgreSQL database schemas. The library is designed to work with PostgreSQL 16+ and provides strong typing, AST-based parsing, and privilege management.

### Key Features

- ✅ **Schema Extraction** - Extract complete database schemas with metadata
- ✅ **Object Types** - Tables, Views, Functions, Triggers, Sequences, Types
- ✅ **Privilege Management** - Full support for PostgreSQL privileges and grants
- ✅ **AST Parsing** - Detailed Abstract Syntax Tree for all database objects
- ✅ **Schema Comparison** - Compare schemas and identify differences
- ⚠️ **Script Generation** - Generate migration scripts (in development)

---

## Core Components

### Namespaces

```csharp
mbulava.PostgreSql.Dac.Extract   // Schema extraction
mbulava.PostgreSql.Dac.Models    // Data models
mbulava.PostgreSql.Dac.Compare   // Schema comparison
mbulava.PostgreSql.Dac.Compile   // Project compilation (future)
```

---

## Extraction API

### PgProjectExtractor

The main class for extracting PostgreSQL database schemas.

#### Constructor

```csharp
public PgProjectExtractor(string connectionString)
```

**Parameters:**
- `connectionString` - Standard Npgsql connection string

**Example:**
```csharp
var extractor = new PgProjectExtractor(
    "Host=localhost;Database=mydb;Username=postgres;Password=secret"
);
```

#### Methods

##### ExtractPgProject

Extracts the complete database schema including all objects.

```csharp
public async Task<PgProject> ExtractPgProject(string databaseName)
```

**Parameters:**
- `databaseName` - Name of the database to extract

**Returns:** `PgProject` containing all schemas and database objects

**Throws:**
- `NotSupportedException` - If PostgreSQL version < 16
- `InvalidOperationException` - If SQL parsing fails

**Example:**
```csharp
var project = await extractor.ExtractPgProject("mydb");
Console.WriteLine($"Extracted {project.Schemas.Count} schemas");
```

##### DetectPostgresVersion

Validates and returns the PostgreSQL server version.

```csharp
public async Task<string> DetectPostgresVersion()
```

**Returns:** Version string (e.g., "16.1", "17.2")

**Throws:** `NotSupportedException` if version < 16

**Example:**
```csharp
var version = await extractor.DetectPostgresVersion();
Console.WriteLine($"PostgreSQL version: {version}");
```

#### Supported Object Types

The extractor supports the following PostgreSQL object types:

| Object Type | Status | Method | Notes |
|-------------|--------|--------|-------|
| **Schemas** | ✅ Complete | `ExtractSchemasAsync()` | Includes owner, privileges, AST |
| **Tables** | ✅ Complete | `ExtractTablesAsync()` | Columns, constraints, indexes, privileges |
| **Views** | ✅ Complete | `ExtractViewsAsync()` | Definition, dependencies, privileges |
| **Functions** | ✅ Complete | `ExtractFunctionsAsync()` | Body, parameters, return type |
| **Procedures** | ✅ Complete | `ExtractFunctionsAsync()` | Procedures are functions in PG |
| **Triggers** | ✅ Complete | `ExtractTriggersAsync()` | Event, timing, function reference |
| **Sequences** | ✅ Complete | `ExtractSequencesAsync()` | Start, increment, min/max, cache |
| **Types** | ✅ Complete | `ExtractTypesAsync()` | Domain, Enum, Composite types |
| **Roles** | ✅ Complete | `ExtractRolesForProjectAsync()` | Attributes, memberships |
| **Privileges** | ✅ Complete | `ExtractPrivilegesAsync()` | All object-level privileges |

---

## Comparison API

### PgSchemaComparer

Compares two PostgreSQL schemas and identifies differences.

#### Constructor

```csharp
public PgSchemaComparer()
```

#### Methods

##### CompareDatabases

Compares two complete database projects.

```csharp
public PgComparisonResult CompareDatabases(
    PgProject source,
    PgProject target
)
```

**Parameters:**
- `source` - Source database project (desired state)
- `target` - Target database project (current state)

**Returns:** `PgComparisonResult` with all differences

**Example:**
```csharp
var comparer = new PgSchemaComparer();
var result = comparer.CompareDatabases(sourceProject, targetProject);

Console.WriteLine($"Found {result.SchemaDifferences.Count} schema differences");
Console.WriteLine($"Found {result.TableDifferences.Count} table differences");
```

#### Detected Differences

The comparer can detect:

- ✅ Missing schemas
- ✅ Schema owner changes
- ✅ Missing tables
- ✅ Table column differences (added, removed, modified)
- ✅ Constraint differences
- ✅ Index differences
- ✅ Privilege changes
- ✅ Missing or modified views
- ✅ Function signature changes
- ✅ Type differences
- ✅ Sequence parameter changes

---

## Compilation API

### ProjectCompiler

Validates project dependencies and generates build artifacts (in development).

```csharp
public class ProjectCompiler
{
    public CompilerResult Compile(PgProject project);
}
```

**Status:** 🚧 In Development (Milestone 2)

---

## Data Models

### PgProject

Root container for a complete database schema.

```csharp
public class PgProject
{
    public string DatabaseName { get; set; }
    public string PostgresVersion { get; set; }
    public List<PgSchema> Schemas { get; set; }
    public List<PgRole> Roles { get; set; }
    
    // Serialization
    public static async Task Save(PgProject project, Stream output);
    public static async Task<PgProject> Load(Stream input);
}
```

### PgSchema

Represents a PostgreSQL schema with all contained objects.

```csharp
public class PgSchema
{
    public string Name { get; set; }
    public string Owner { get; set; }
    public CreateSchemaStmt Ast { get; set; }
    public string? AstJson { get; set; }
    public List<PgPrivilege> Privileges { get; set; }
    
    // Object collections
    public List<PgTable> Tables { get; set; }
    public List<PgView> Views { get; set; }
    public List<PgFunction> Functions { get; set; }
    public List<PgType> Types { get; set; }
    public List<PgSequence> Sequences { get; set; }
    public List<PgTrigger> Triggers { get; set; }
}
```

### PgTable

Represents a PostgreSQL table with complete metadata.

```csharp
public class PgTable
{
    public string Name { get; set; }
    public string Definition { get; set; }
    public CreateStmt? Ast { get; set; }
    public string? AstJson { get; set; }
    public string Owner { get; set; }
    public string? Tablespace { get; set; }
    public bool RowLevelSecurity { get; set; }
    public bool ForceRowLevelSecurity { get; set; }
    public int? FillFactor { get; set; }
    public List<string> InheritedFrom { get; set; }
    public string? PartitionStrategy { get; set; }
    public string? PartitionExpression { get; set; }
    
    public List<PgColumn> Columns { get; set; }
    public List<PgConstraint> Constraints { get; set; }
    public List<PgIndex> Indexes { get; set; }
    public List<PgPrivilege> Privileges { get; set; }
    
    // Helpers
    public List<PgConstraint> ForeignKeys { get; }
    public List<PgConstraint> CheckConstraints { get; }
    public List<PgConstraint> UniqueConstraints { get; }
    public PgConstraint? PrimaryKey { get; }
}
```

### PgColumn

Represents a table column.

```csharp
public class PgColumn
{
    public string Name { get; set; }
    public string DataType { get; set; }
    public bool IsNotNull { get; set; }
    public string? DefaultExpression { get; set; }
    public int Position { get; set; }
    public bool IsIdentity { get; set; }
    public string? IdentityGeneration { get; set; }
    public bool IsGenerated { get; set; }
    public string? GenerationExpression { get; set; }
    public string? Collation { get; set; }
    public string? Comment { get; set; }
}
```

### PgConstraint

Represents a table constraint.

```csharp
public class PgConstraint
{
    public string Name { get; set; }
    public string Definition { get; set; }
    public ConstrType Type { get; set; }
    public string? CheckExpression { get; set; }
    public string? ReferencedTable { get; set; }
    public List<string>? ReferencedColumns { get; set; }
}

public enum ConstrType
{
    ConstrPrimary,    // PRIMARY KEY
    ConstrUnique,     // UNIQUE
    ConstrForeign,    // FOREIGN KEY
    ConstrCheck,      // CHECK
    ConstrExclusion,  // EXCLUSION
    ConstrNotnull,    // NOT NULL
    Undefined
}
```

### PgPrivilege

Represents a database privilege grant.

```csharp
public class PgPrivilege
{
    public string Grantee { get; set; }        // Role or "PUBLIC"
    public string PrivilegeType { get; set; }   // SELECT, INSERT, UPDATE, etc.
    public bool IsGrantable { get; set; }       // WITH GRANT OPTION
    public string Grantor { get; set; }         // Role that granted privilege
}
```

**Supported Privilege Types:**

| Privilege | Code | Tables | Schemas | Functions | Sequences |
|-----------|------|--------|---------|-----------|-----------|
| SELECT | r | ✅ | ❌ | ❌ | ✅ |
| INSERT | a | ✅ | ❌ | ❌ | ❌ |
| UPDATE | w | ✅ | ❌ | ❌ | ✅ |
| DELETE | d | ✅ | ❌ | ❌ | ❌ |
| TRUNCATE | D | ✅ | ❌ | ❌ | ❌ |
| REFERENCES | x/X | ✅ | ❌ | ❌ | ❌ |
| TRIGGER | t/T | ✅ | ❌ | ❌ | ❌ |
| USAGE | U | ❌ | ✅ | ❌ | ✅ |
| CREATE | C | ❌ | ✅ | ❌ | ❌ |
| EXECUTE | X | ❌ | ❌ | ✅ | ❌ |

### PgView

Represents a database view.

```csharp
public class PgView
{
    public string Name { get; set; }
    public string Definition { get; set; }
    public ViewStmt? Ast { get; set; }
    public string? AstJson { get; set; }
    public string Owner { get; set; }
    public List<PgPrivilege> Privileges { get; set; }
}
```

### PgFunction

Represents a function or procedure.

```csharp
public class PgFunction
{
    public string Name { get; set; }
    public string Definition { get; set; }
    public CreateFunctionStmt? Ast { get; set; }
    public string? AstJson { get; set; }
    public string Owner { get; set; }
    public string Language { get; set; }
    public string? ReturnType { get; set; }
    public List<PgParameter> Parameters { get; set; }
    public List<PgPrivilege> Privileges { get; set; }
}
```

### PgType

Represents a user-defined type (domain, enum, or composite).

```csharp
public class PgType
{
    public string Name { get; set; }
    public string Owner { get; set; }
    public PgTypeKind Kind { get; set; }
    public string Definition { get; set; }
    public string? AstJson { get; set; }
    
    // Domain-specific
    public CreateDomainStmt? AstDomain { get; set; }
    
    // Enum-specific
    public CreateEnumStmt? AstEnum { get; set; }
    public List<string> EnumLabels { get; set; }
    
    // Composite-specific
    public CompositeTypeStmt? AstComposite { get; set; }
    public List<PgAttribute> Attributes { get; set; }
}

public enum PgTypeKind
{
    Domain,
    Enum,
    Composite
}
```

### PgSequence

Represents a sequence object.

```csharp
public class PgSequence
{
    public string Name { get; set; }
    public string Definition { get; set; }
    public CreateSeqStmt? Ast { get; set; }
    public string? AstJson { get; set; }
    public string Owner { get; set; }
    public long StartValue { get; set; }
    public long IncrementBy { get; set; }
    public long? MinValue { get; set; }
    public long? MaxValue { get; set; }
    public long CacheSize { get; set; }
    public bool Cycle { get; set; }
    public List<PgPrivilege> Privileges { get; set; }
}
```

### PgTrigger

Represents a trigger.

```csharp
public class PgTrigger
{
    public string Name { get; set; }
    public string Definition { get; set; }
    public CreateTrigStmt? Ast { get; set; }
    public string? AstJson { get; set; }
    public string Owner { get; set; }
    public string TableName { get; set; }
    public string FunctionName { get; set; }
    public string Timing { get; set; }  // BEFORE, AFTER, INSTEAD OF
    public List<string> Events { get; set; }  // INSERT, UPDATE, DELETE
}
```

### PgRole

Represents a database role.

```csharp
public class PgRole
{
    public string Name { get; set; }
    public bool IsSuperUser { get; set; }
    public bool CanLogin { get; set; }
    public bool Inherit { get; set; }
    public bool Replication { get; set; }
    public bool BypassRLS { get; set; }
    public List<string> MemberOf { get; set; }
}
```

---

## Usage Examples

### Extract a Complete Database

```csharp
using mbulava.PostgreSql.Dac.Extract;
using mbulava.PostgreSql.Dac.Models;

var connectionString = "Host=localhost;Database=mydb;Username=postgres;Password=secret";
var extractor = new PgProjectExtractor(connectionString);

// Validate version
var version = await extractor.DetectPostgresVersion();
Console.WriteLine($"PostgreSQL version: {version}");

// Extract complete project
var project = await extractor.ExtractPgProject("mydb");

Console.WriteLine($"Database: {project.DatabaseName}");
Console.WriteLine($"Schemas: {project.Schemas.Count}");
Console.WriteLine($"Roles: {project.Roles.Count}");

foreach (var schema in project.Schemas)
{
    Console.WriteLine($"\nSchema: {schema.Name} (Owner: {schema.Owner})");
    Console.WriteLine($"  Tables: {schema.Tables.Count}");
    Console.WriteLine($"  Views: {schema.Views.Count}");
    Console.WriteLine($"  Functions: {schema.Functions.Count}");
    Console.WriteLine($"  Types: {schema.Types.Count}");
    Console.WriteLine($"  Sequences: {schema.Sequences.Count}");
    Console.WriteLine($"  Triggers: {schema.Triggers.Count}");
}
```

### Save and Load Projects

```csharp
// Save to file
await using var fileStream = File.Create("mydb.pgpac");
await PgProject.Save(project, fileStream);

// Load from file
await using var inputStream = File.OpenRead("mydb.pgpac");
var loadedProject = await PgProject.Load(inputStream);
```

### Compare Two Databases

```csharp
using mbulava.PostgreSql.Dac.Compare;

// Extract source and target
var sourceProject = await sourceExtractor.ExtractPgProject("source_db");
var targetProject = await targetExtractor.ExtractPgProject("target_db");

// Compare
var comparer = new PgSchemaComparer();
var result = comparer.CompareDatabases(sourceProject, targetProject);

// Report differences
if (result.SchemaDifferences.Any())
{
    Console.WriteLine("Schema Differences:");
    foreach (var diff in result.SchemaDifferences)
    {
        Console.WriteLine($"  - {diff.Type}: {diff.ObjectName}");
    }
}

if (result.TableDifferences.Any())
{
    Console.WriteLine("\nTable Differences:");
    foreach (var diff in result.TableDifferences)
    {
        Console.WriteLine($"  - {diff.Type}: {diff.SchemaName}.{diff.ObjectName}");
        if (diff.ColumnChanges.Any())
        {
            Console.WriteLine($"    Column changes: {diff.ColumnChanges.Count}");
        }
    }
}
```

### Working with Privileges

```csharp
// Get all privileges for a table
var table = schema.Tables.First(t => t.Name == "users");
foreach (var priv in table.Privileges)
{
    Console.WriteLine($"{priv.Grantee} has {priv.PrivilegeType}" +
                     $"{(priv.IsGrantable ? " WITH GRANT OPTION" : "")}");
}

// Find all objects accessible by a role
var roleName = "app_user";
var accessibleTables = schema.Tables
    .Where(t => t.Privileges.Any(p => p.Grantee == roleName))
    .ToList();
```

### Analyze Dependencies

```csharp
// Find all foreign key relationships
foreach (var table in schema.Tables)
{
    if (table.ForeignKeys.Any())
    {
        Console.WriteLine($"\nTable: {table.Name}");
        foreach (var fk in table.ForeignKeys)
        {
            Console.WriteLine($"  FK: {fk.Name} -> {fk.ReferencedTable}");
        }
    }
}

// Find all tables with check constraints
var tablesWithChecks = schema.Tables
    .Where(t => t.CheckConstraints.Any())
    .ToList();
```

---

## Error Handling

### Common Exceptions

| Exception | Cause | Resolution |
|-----------|-------|------------|
| `NotSupportedException` | PostgreSQL version < 16 | Upgrade to PostgreSQL 16+ |
| `InvalidOperationException` | SQL parsing failed | Check definition syntax |
| `NpgsqlException` | Connection/query failed | Verify connection string, credentials |
| `JsonException` | AST deserialization failed | Report as bug |

### Best Practices

```csharp
try
{
    var project = await extractor.ExtractPgProject("mydb");
    await PgProject.Save(project, outputStream);
}
catch (NotSupportedException ex)
{
    Console.WriteLine($"Version error: {ex.Message}");
}
catch (NpgsqlException ex)
{
    Console.WriteLine($"Database error: {ex.Message}");
}
catch (Exception ex)
{
    Console.WriteLine($"Unexpected error: {ex.Message}");
    // Log for troubleshooting
}
```

---

## Version Support

| Component | Minimum Version | Recommended |
|-----------|----------------|-------------|
| .NET | 10.0 | 10.0 |
| PostgreSQL | 16.0 | 16.x or 17.x |
| Npgsql | Latest | Latest |

---

## Performance Considerations

- **Large Databases**: Extraction time scales with object count
- **Connection Pooling**: Enabled by default through Npgsql
- **Memory Usage**: Full project loaded into memory
- **Async Operations**: All I/O operations are async

### Optimization Tips

1. **Filter schemas** if you only need specific ones
2. **Use connection pooling** for multiple operations
3. **Save to disk** to avoid re-extraction
4. **Parallel extraction** not currently supported

---

## Future Enhancements

Planned for upcoming milestones:

- 🚧 Script generation for schema changes (Milestone 3)
- 🚧 Deployment automation (Milestone 4)
- 🚧 NuGet package support (Milestone 5)
- 🚧 MSBuild integration (Milestone 6)

---

## Support

- **Documentation**: See `/docs` folder
- **Issues**: https://github.com/mbulava-org/pgPacTool/issues
- **License**: MIT

---

**Last Updated:** 2026-01-31  
**API Version:** 0.1.0  
**Milestone:** 1 (Core Extraction) - Complete
