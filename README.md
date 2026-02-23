# pgPacTool - PostgreSQL Data-Tier Application Compiler

A modern .NET 10 tool providing SQL Server Data Tools (.sqlproj) functionality for PostgreSQL databases.

**Project Model:** MSBuild SDK integration (like [MSBuild.Sdk.SqlProj](https://github.com/rr-wfm/MSBuild.Sdk.SqlProj)) using standard `.csproj` files.

[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4)](https://dotnet.microsoft.com/)
[![PostgreSQL](https://img.shields.io/badge/PostgreSQL-16%2B-336791)](https://www.postgresql.org/)
[![License](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)

---

## 🎉 Milestone 3 Complete!

✅ **Schema Comparison & Migration Scripts** - Full deployment script generation with pre/post scripts and SQLCMD variables!

**What's Working:**
- ✅ **Milestone 1**: Complete schema extraction with AST parsing
- ✅ **Milestone 2**: Dependency analysis, cycle detection, deployment ordering
- ✅ **Milestone 3**: Schema comparison, migration scripts, pre/post deployment, SQLCMD variables
- ✅ **158 Tests Passing** (132 unit + 26 integration) - Comprehensive coverage
- ✅ **Production Ready** - Enterprise-grade code quality
- ✅ **Full Deployment Pipeline** - Extract, compare, generate, and deploy

**[📚 Full Documentation](docs/README.md)** | **[🎯 Milestone 3 Details](docs/milestone-3/)**

---

## Quick Start

### Extract, Compare & Deploy Database Changes
```csharp
using mbulava.PostgreSql.Dac.Extract;
using mbulava.PostgreSql.Dac.Compile;
using mbulava.PostgreSql.Dac.Publish;

var sourceConnection = "Host=localhost;Database=dev_db;Username=postgres";
var targetConnection = "Host=prod;Database=prod_db;Username=postgres";

// Extract source database schema
var extractor = new PgProjectExtractor(sourceConnection);
var sourceProject = await extractor.ExtractPgProject("dev_db");

// Compile and validate
var compiler = new ProjectCompiler();
var compileResult = compiler.Compile(sourceProject);

if (compileResult.IsSuccess)
{
    // Publish changes to target
    var publisher = new ProjectPublisher();
    var publishOptions = new PublishOptions
    {
        ConnectionString = targetConnection,
        GenerateScriptOnly = true,  // Generate script without executing
        OutputScriptPath = "deployment.sql",
        IncludeComments = true,
        Transactional = true,
        Variables = new()
        {
            new() { Name = "DatabaseName", Value = "prod_db" }
        }
    };

    var publishResult = await publisher.PublishAsync(
        sourceProject, 
        targetConnection, 
        publishOptions);

    if (publishResult.Success)
    {
        Console.WriteLine($"✅ Success! {publishResult.ObjectsCreated} created, " +
                          $"{publishResult.ObjectsAltered} altered");
        Console.WriteLine($"Script saved to: {publishResult.ScriptFilePath}");
    }
}
```

### Extract & Compile a Database
```csharp
using mbulava.PostgreSql.Dac.Extract;
using mbulava.PostgreSql.Dac.Compile;

var connectionString = "Host=localhost;Database=mydb;Username=postgres;Password=secret";

// Extract database schema
var extractor = new PgProjectExtractor(connectionString);
var project = await extractor.ExtractPgProject("mydb");

// Compile and validate
var compiler = new ProjectCompiler();
var result = compiler.Compile(project);

if (result.IsSuccess)
{
    Console.WriteLine($"✅ Success! {result.DeploymentOrder.Count} objects ready");

    // Deploy in safe order
    foreach (var objectName in result.DeploymentOrder)
    {
        Console.WriteLine($"  - {objectName}");
    }
}
else
{
    Console.WriteLine($"❌ {result.Errors.Count} errors found:");
    foreach (var error in result.Errors)
    {
        Console.WriteLine($"  {error.Code}: {error.Message}");
    }
}
```

**[📖 More Examples](docs/USER_GUIDE.md)** | **[🔧 API Reference](docs/API_REFERENCE.md)**

---

## Features

### Milestone 1: Extraction ✅
| Object Type | Extraction | AST Parsing | Privileges |
|-------------|------------|-------------|------------|
| **Schemas** | ✅ | ✅ | ✅ |
| **Tables** | ✅ | ✅ | ✅ |
| **Views** | ✅ | ✅ | ✅ |
| **Functions** | ✅ | ✅ | ✅ |
| **Procedures** | ✅ | ✅ | ✅ |
| **Triggers** | ✅ | ✅ | ❌ |
| **Sequences** | ✅ | ✅ | ✅ |
| **Types** | ✅ | ✅ | ✅ |
| **Roles** | ✅ | N/A | N/A |
| **Constraints** | ✅ | ✅ | N/A |
| **Indexes** | ✅ | ✅ | N/A |

### Milestone 2: Compilation ✅
| Feature | Status | Description |
|---------|--------|-------------|
| **Dependency Analysis** | ✅ | Extracts all object dependencies |
| **Cycle Detection** | ✅ | Smart detection with severity levels |
| **Deployment Ordering** | ✅ | Topological sort for safe deployment |
| **Parallel Deployment** | ✅ | Groups objects by deployment level |
| **Error Reporting** | ✅ | Clear, actionable error messages |
| **Validation** | ✅ | Comprehensive project validation |

### Milestone 3: Schema Comparison & Migration ✅
| Feature | Status | Description |
|---------|--------|-------------|
| **Schema Comparison** | ✅ | Compare all object types (tables, views, functions, triggers, types, sequences) |
| **Migration Scripts** | ✅ | Generate CREATE/DROP/ALTER statements |
| **Pre-Deployment Scripts** | ✅ | Custom scripts before schema changes |
| **Post-Deployment Scripts** | ✅ | Custom scripts after schema changes |
| **SQLCMD Variables** | ✅ | Variable replacement $(VarName) syntax |
| **Transaction Support** | ✅ | Wrap deployment in transactions |
| **Privilege Management** | ✅ | GRANT/REVOKE script generation |
| **Script Validation** | ✅ | Validate scripts before deployment |

---
## Documentation

| Document | Description |
|----------|-------------|
| **[📚 Documentation Hub](docs/README.md)** | Complete documentation index |
| **[📖 User Guide](docs/USER_GUIDE.md)** | Getting started, examples, troubleshooting |
| **[🔧 API Reference](docs/API_REFERENCE.md)** | Complete API with code examples |
| **[⚙️ Workflows](docs/WORKFLOWS.md)** | CI/CD, testing, code coverage |

---

## Roadmap

### ✅ Milestone 1: Core Extraction (COMPLETE)
- Database schema extraction
- All major object types
- Privilege management
- AST parsing

### ✅ Milestone 2: Compilation & Validation (COMPLETE)
- Dependency validation
- Circular dependency detection
- Build artifacts

### ✅ Milestone 3: Schema Comparison & Scripts (COMPLETE)
- Migration script generation
- Pre/post deployment scripts
- SQLCMD variables
- Full publish pipeline

### 📋 Milestone 4: Deployment (Next)
- Deployment automation
- Rollback support
- Publishing profiles

### 📋 Milestone 5: Packaging
- NuGet packages
- Package references

### 📋 Milestone 6: MSBuild SDK
- .pgproj file support
- MSBuild integration
- Project templates

**[📅 Full Roadmap](docs/README.md)**

---

## Requirements

- **.NET 10 SDK** or higher
- **PostgreSQL 16+** (17 supported)
- **Npgsql** (included as dependency)

---

## Testing

### Run Tests
```bash
# All tests
dotnet test

# Smoke tests (fast validation)
dotnet test --filter "Category=Smoke"

# Integration tests (requires PostgreSQL)
dotnet test --filter "Category=Integration"

# With coverage
dotnet test --collect:"XPlat Code Coverage"
```

### Setup Test Database
```bash
# Using Docker
docker run --name pgpac-test \
  -e POSTGRES_PASSWORD=testpass \
  -p 5432:5432 \
  -d postgres:16
```

**[🧪 Testing Guide](docs/WORKFLOWS.md#postgresql-testing)**

---

## Features

- **Database Version Control** - Store your database schema as code
- **Schema Comparison** - Compare databases and identify differences
- **Full Metadata** - Extract complete object definitions with AST
- **Privilege Management** - Track all grants and role memberships
- **Dependency Analysis** - Automatic dependency graph building ✨ NEW!
- **Cycle Detection** - Smart circular dependency detection ✨ NEW!
- **Deployment Ordering** - Safe deployment order generation ✨ NEW!
- **PostgreSQL 16+** - Built for modern PostgreSQL
- **.NET 10** - Latest .NET technology
- **Type Safe** - Full IntelliSense support

**Project Integration:**
- **MSBuild SDK** - Integrates into standard `.csproj` files (like [MSBuild.Sdk.SqlProj](https://github.com/rr-wfm/MSBuild.Sdk.SqlProj))
- **No Custom Project Type** - Uses familiar `.csproj` format with our SDK
- **Standard Tooling** - Works with `dotnet build`, Visual Studio, VS Code

**Future:**
- **MSBuild Tasks** - Build targets for extract, compile, deploy
- **Migration Scripts** - Generate deployment scripts automatically
- **Package Distribution** - Share databases as NuGet packages
- **CI/CD Templates** - Ready-to-use pipeline templates

**[📘 MSBuild Integration Details](docs/MSBUILD_INTEGRATION.md)**

---

## Contributing

Contributions welcome! See [docs/WORKFLOWS.md](docs/WORKFLOWS.md) for development setup.

---

## License

MIT License - see [LICENSE](LICENSE) for details.

### Third-Party Licenses

**pg_query.dll** - Licensed under the PostgreSQL License (similar to MIT). 
- [PostgreSQL License](https://www.postgresql.org/about/licence/)
- Building multi-platform versions (Windows, Linux, macOS) for pgPacTool

---

## Support

- **Documentation**: [docs/](docs/)
- **Issues**: [GitHub Issues](https://github.com/mbulava-org/pgPacTool/issues)
- **Discussions**: [GitHub Discussions](https://github.com/mbulava-org/pgPacTool/discussions)

---

**Status:** Milestone 1 Complete ✅  
**Version:** 0.1.0  
**Last Updated:** 2026-01-31
