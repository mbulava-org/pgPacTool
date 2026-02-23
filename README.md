# pgPacTool - PostgreSQL Data-Tier Application Compiler

A modern .NET 10 tool providing SQL Server Data Tools (.sqlproj) functionality for PostgreSQL databases.

[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4)](https://dotnet.microsoft.com/)
[![PostgreSQL](https://img.shields.io/badge/PostgreSQL-16%2B-336791)](https://www.postgresql.org/)
[![License](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)

---

## 🎉 Milestone 1 Complete!

✅ **Core Extraction Functionality** - All major database objects can be extracted with full metadata and AST parsing.

**What's Working:**
- Extract complete database schemas with all metadata
- Tables, views, functions, triggers, sequences, and types
- Full privilege and role extraction
- AST parsing for all objects
- Schema comparison (basic)
- Save/load as JSON

**[📚 Full Documentation](docs/README.md)**

---

## Quick Start

### Installation
```bash
# Clone the repository
git clone https://github.com/mbulava-org/pgPacTool.git
cd pgPacTool

# Add project reference
dotnet add reference path/to/mbulava.PostgreSql.Dac.csproj
```

### Extract a Database
```csharp
using mbulava.PostgreSql.Dac.Extract;
using mbulava.PostgreSql.Dac.Models;

var connectionString = "Host=localhost;Database=mydb;Username=postgres;Password=secret";
var extractor = new PgProjectExtractor(connectionString);

// Extract complete project
var project = await extractor.ExtractPgProject("mydb");

// Save to file
await using var file = File.Create("mydb.pgpac");
await PgProject.Save(project, file);

Console.WriteLine($"Extracted {project.Schemas.Count} schemas");
```

**[📖 More Examples](docs/USER_GUIDE.md)**

---

## Supported Objects

| Object Type | Extraction | AST Parsing | Privileges |
|-------------|------------|-------------|------------|
| **Schemas** | ✅ | ✅ | ✅ |
| **Tables** | ✅ | ✅ | ✅ |
| **Views** | ✅ | ✅ | ✅ |
| **Functions** | ✅ | ✅ | ✅ |
| **Procedures** | ✅ | ✅ | ✅ |
| **Triggers** | ✅ | ✅ | ❌ |
| **Sequences** | ✅ | ✅ | ✅ |
| **Types (Domain)** | ✅ | ✅ | ✅ |
| **Types (Enum)** | ✅ | ✅ | ✅ |
| **Types (Composite)** | ✅ | ✅ | ✅ |
| **Roles** | ✅ | N/A | N/A |
| **Constraints** | ✅ | ✅ | N/A |
| **Indexes** | ✅ | ✅ | N/A |

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

### 🚧 Milestone 2: Compilation & Validation (Next)
- Dependency validation
- Circular dependency detection
- Build artifacts

### 📋 Milestone 3: Schema Comparison & Scripts
- Migration script generation
- Pre/post deployment scripts
- SQLCMD variables

### 📋 Milestone 4: Deployment
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
- **PostgreSQL 16+** - Built for modern PostgreSQL
- **.NET 10** - Latest .NET technology
- **Type Safe** - Full IntelliSense support

**Future:**
- **Migration Scripts** - Generate deployment scripts automatically
- **Deployment Automation** - Deploy schema changes safely
- **Package Distribution** - Share databases as NuGet packages
- **MSBuild Integration** - Build databases like any other .NET project

**Inspired by:** [MSBuild.Sdk.SqlProj](https://github.com/rr-wfm/MSBuild.Sdk.SqlProj) but designed specifically for PostgreSQL.

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
