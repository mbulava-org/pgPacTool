# pgPacTool - PostgreSQL Data-Tier Application Tools

**Build PostgreSQL databases like SQL Server SSDT!** MSBuild SDK + CLI tools for database-as-code.

[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4)](https://dotnet.microsoft.com/)
[![PostgreSQL](https://img.shields.io/badge/PostgreSQL-16%2B-336791)](https://www.postgresql.org/)
[![License](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)
![Tests](https://img.shields.io/badge/tests-201%20passing-success)

> **💡 PostgreSQL Version Support**: Currently supports **PostgreSQL 16 and 17**. Older versions (14, 15) may be added in the future based on demand. See [Multi-Version Support Documentation](docs/features/multi-version-support/README.md) for details.

---

## 🚀 What is pgPacTool?

pgPacTool brings **SQL Server-style database project workflow** to PostgreSQL. Inspired by SqlPackage and SSDT, it enables:

- 📦 **MSBuild SDK** - Build database projects with `dotnet build`
- 🔧 **CLI Tool** - Extract, compile, and deploy schemas
- 📚 **Core Library** - Programmatic schema operations
- ✅ **Validation** - Dependency checking and circular reference detection
- 🔄 **CI/CD Ready** - Perfect for DevOps pipelines

---

## ✨ Current Features (v1.0.0-preview1)

### 🎯 Complete Functionality

#### **MSBuild SDK Integration** ⭐ NEW!
- ✅ `MSBuild.Sdk.PostgreSql` - SDK for database projects
- ✅ Convention-based project structure
- ✅ Automatic SQL file discovery
- ✅ Build integration with `dotnet build`
- ✅ Generates `.pgpac` packages
- ✅ Incremental build support
- ✅ Visual Studio compatible (when packaged)

#### **SDK-Style Project Extraction** ⭐ NEW!
- ✅ Extract databases directly to `.csproj` format
- ✅ Automatic folder structure generation by object type
- ✅ Individual SQL files per database object
- ✅ Version control friendly (one object = one file)
- ✅ Visual Studio integration ready
- ✅ Convention-based organization
- ✅ Supports simple to complex databases (1-145+ files)

#### **CLI Tool (postgresPacTools)**
- ✅ `extract` - Export schema from live database to `.pgproj.json` or `.csproj`
- ✅ `compile` - Validate and build projects (.csproj → .pgpac)
- ✅ `publish` - Deploy changes to target database
- ✅ `script` - Generate deployment SQL without executing
- ✅ `deploy-report` - Preview changes as JSON report

#### **Core DAC Library**
- ✅ Schema extraction with Npgquery AST parsing
- ✅ SDK-style project generation (CsprojProjectGenerator)
- ✅ Dependency analysis and topological sorting
- ✅ Circular reference detection
- ✅ Migration script generation
- ✅ Pre/Post deployment scripts
- ✅ SQLCMD variable substitution
- ✅ `.pgpac` package format (PostgreSQL DACPAC)

#### **Database Object Support**
- ✅ Tables (with indexes, constraints)
- ✅ Views (regular and materialized)
- ✅ Functions (all languages)
- ✅ Stored procedures
- ✅ Types (ENUM, composite, domains)
- ✅ Sequences
- ✅ Triggers
- ✅ Schemas
- ✅ Roles and permissions
- ✅ Extensions
- ⚠️ Multi-schema (limited - improvements planned)
- ❌ Aggregate functions (excluded from extraction by design)

#### **Quality & Testing**
- ✅ **201 tests passing** (100% success rate)
  - 183 unit tests
  - 18 integration tests
  - CLI integration tests
  - Round-trip validation tests
- ✅ Tested with real databases:
  - world_happiness (9 SQL files)
  - dvdrental (107 SQL files)
  - pagila (145 SQL files)

---

## 🚀 Quick Start

### Option 1: Extract Existing Database to SDK-Style Project ⭐ RECOMMENDED

**For existing databases, instantly create a version-controllable project:**

```bash
# Build the CLI from source
git clone https://github.com/mbulava-org/pgPacTool.git
cd pgPacTool
dotnet build

# Extract your database to SDK-style .csproj
dotnet run --project src/postgresPacTools/postgresPacTools.csproj -- extract \
  --source-connection-string "Host=localhost;Database=mydb;Username=postgres;Password=***" \
  --target-file output/mydb/mydb.csproj \
  --verbose

# Result: Complete project with individual SQL files!
# output/mydb/
#   ├── mydb.csproj
#   ├── public/
#   │   ├── Tables/
#   │   ├── Views/
#   │   ├── Functions/
#   │   └── ...
#   └── Security/
```

**What you get:**
- ✅ One SQL file per database object
- ✅ Organized by schema and object type
- ✅ Ready for version control (git-friendly)
- ✅ Editable in Visual Studio
- ✅ Compilable with `dotnet build` (once SDK is published)

**Real Examples:**
```bash
# Simple database (1 table) → 9 files
extract -scs "..." -tf world_happiness/world_happiness.csproj

# Medium database (15 tables, 7 views) → 107 files
extract -scs "..." -tf dvdrental/dvdrental.csproj

# Complex database (21 tables, 54 indexes) → 145 files
extract -scs "..." -tf pagila/pagila.csproj
```

---

### Option 2: MSBuild SDK (For New Projects)

**Coming Soon!** Once published to NuGet.org:

```xml
<!-- MyDatabase.csproj -->
<Project Sdk="MSBuild.Sdk.PostgreSql/1.0.0-preview1">

  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <DatabaseName>MyDatabase</DatabaseName>
  </PropertyGroup>

</Project>
```

```powershell
# Organize SQL files
MyDatabase/
├── MyDatabase.csproj
├── Tables/
│   ├── Users.sql
│   └── Orders.sql
└── Views/
    └── CustomerOrders.sql

# Build!
dotnet build
# Output: bin/Debug/net10.0/MyDatabase.pgpac ✅
```

---

### Option 3: CLI Tool (For Ad-Hoc Operations)

**Coming Soon!** Once published:

```powershell
# Install globally
dotnet tool install --global postgresPacTools

# Extract schema to JSON
pgpac extract -scs "Host=localhost;Database=mydb;..." -tf mydb.pgproj.json

# Extract schema to SDK project
pgpac extract -scs "Host=localhost;Database=mydb;..." -tf mydb/mydb.csproj

# Compile project
pgpac compile -sf MyDatabase.csproj

# Deploy
pgpac publish -sf MyDatabase.pgpac -tcs "Host=prod;..."
```

---

### Option 4: Core Library (For Custom Tools)

**Coming Soon!** Once published:

```powershell
dotnet add package mbulava.PostgreSql.Dac
```

```csharp
using mbulava.PostgreSql.Dac.Extract;
using mbulava.PostgreSql.Dac.Compile;

// Extract schema
var extractor = new PgProjectExtractor(connectionString);
var project = await extractor.ExtractPgProject("mydb");

// Generate SDK-style project
var generator = new CsprojProjectGenerator("output/mydb/mydb.csproj");
await generator.GenerateProjectAsync(project);

// Or compile and validate
var compiler = new ProjectCompiler();
var result = compiler.Compile(project);

if (result.Errors.Count == 0)
{
    Console.WriteLine($"✅ {result.DeploymentOrder.Count} objects validated");
}
```

---

## 💻 Local Development (Build from Source)

### Prerequisites

- ✅ **.NET 10 SDK** - https://dotnet.microsoft.com/download/dotnet/10.0
- ✅ **PostgreSQL 12+** - For testing (Docker recommended)
- ✅ **Git** - Source control
- ✅ **Visual Studio 2022** or **VS Code** (optional)

### Clone and Build

```powershell
# Clone repository
git clone https://github.com/mbulava-org/pgPacTool.git
cd pgPacTool

# Restore dependencies
dotnet restore

# Build all projects
dotnet build

# Run tests
dotnet test

# Expected: ✅ 201 tests passing
```

### Project Structure

```
pgPacTool/
├── src/
│   ├── libs/
│   │   ├── mbulava.PostgreSql.Dac/          # Core DAC library
│   │   └── Npgquery/                        # SQL parser (Npgquery wrapper)
│   ├── postgresPacTools/                     # CLI tool
│   └── sdk/
│       └── MSBuild.Sdk.PostgreSql/          # MSBuild SDK
├── tests/
│   ├── mbulava.PostgreSql.Dac.Tests/        # Unit & integration tests
│   ├── ProjectExtract-Tests/                # Additional tests
│   └── TestProjects/                         # Sample database projects
│       ├── SampleDatabase/                   # E-commerce example
│       └── MultiSchemaDatabase/              # Multi-schema example
└── docs/                                      # Documentation
```

### Run CLI Locally

```powershell
# Build CLI
dotnet build src/postgresPacTools/postgresPacTools.csproj

# Run without installing
dotnet run --project src/postgresPacTools/postgresPacTools.csproj -- --help

# Extract example
dotnet run --project src/postgresPacTools/postgresPacTools.csproj -- extract \
  -scs "Host=localhost;Database=mydb;Username=postgres;Password=secret" \
  -tf mydb.pgproj.json
```

### Test MSBuild SDK Locally

```powershell
# Build SDK
dotnet build src/sdk/MSBuild.Sdk.PostgreSql/MSBuild.Sdk.PostgreSql.csproj

# Pack to local NuGet feed
dotnet pack src/sdk/MSBuild.Sdk.PostgreSql/MSBuild.Sdk.PostgreSql.csproj -o $HOME/LocalNuGet

# Add local feed
dotnet nuget add source $HOME/LocalNuGet --name LocalFeed

# Test with sample project
cd tests/TestProjects/SampleDatabase

# Configure to use local feed
@"
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <add key="LocalFeed" value="$HOME/LocalNuGet" />
  </packageSources>
</configuration>
"@ | Out-File -FilePath nuget.config

# Update .csproj to use local version
# Then build
dotnet build
```

### Run Integration Tests with Docker

```powershell
# Start PostgreSQL with Docker
docker run --name pgpac-test -e POSTGRES_PASSWORD=postgres -p 5432:5432 -d postgres:16

# Run integration tests
dotnet test tests/mbulava.PostgreSql.Dac.Tests/ --filter "Category=Integration"

# Cleanup
docker stop pgpac-test
docker rm pgpac-test
```

### Debug in Visual Studio

1. Open `pgPacTool.sln` in Visual Studio 2022+
2. Set startup project:
   - For CLI: `postgresPacTools`
   - For tests: `mbulava.PostgreSql.Dac.Tests`
3. Press **F5** to debug
4. Set breakpoints as needed

### VS Code Configuration

```json
// .vscode/launch.json
{
  "version": "0.2.0",
  "configurations": [
    {
      "name": "CLI: Extract",
      "type": "coreclr",
      "request": "launch",
      "program": "${workspaceFolder}/src/postgresPacTools/bin/Debug/net10.0/postgresPacTools.dll",
      "args": [
        "extract",
        "-scs", "Host=localhost;Database=test;Username=postgres;Password=postgres",
        "-tf", "test.pgproj.json"
      ],
      "cwd": "${workspaceFolder}",
      "console": "internalConsole"
    },
    {
      "name": "Run Tests",
      "type": "coreclr",
      "request": "launch",
      "program": "dotnet",
      "args": ["test"],
      "cwd": "${workspaceFolder}"
    }
  ]
}
```

---

## 📚 Documentation

| Document | Description |
|----------|-------------|
| [CLI Reference](docs/CLI_REFERENCE.md) | Complete CLI command reference with SDK extraction examples |
| [User Guide](docs/USER_GUIDE.md) | Getting started, SDK-style projects, troubleshooting |
| [SDK Guide](docs/SDK_PROJECT_GUIDE.md) | MSBuild SDK usage and project structure guide |
| [Publishing Plan](docs/NUGET_PUBLISHING_PLAN.md) | NuGet publication roadmap |
| [API Documentation](docs/API_REFERENCE.md) | Core library API documentation |

---

## 🗺️ Roadmap & Next Steps

### ✅ Recently Completed

**SDK-Style Project Extraction** (Completed January 2025)
- ✅ Extract databases directly to `.csproj` format
- ✅ Automatic folder structure generation
- ✅ Individual SQL files per object
- ✅ CLI help menus updated
- ✅ Comprehensive documentation added
- ✅ Tested with 3 production-like databases (9-145 files)
- ✅ Fixed null reference bugs in sequence extraction
- ✅ Fixed aggregate function handling in function extraction
- ✅ Added graceful error handling for missing databases

**Bug Fixes & Improvements:**
- ✅ Null safety in `ExtractSequencesAsync` (parse result validation)
- ✅ Aggregate functions excluded from extraction (prevent errors)
- ✅ Database existence validation with clear error messages
- ✅ Enhanced exception handling with verbose stack traces
- ✅ PostgreSQL version checker with better error context

---

### 📦 Publishing (Next Priority)

**Branch:** `feature/msbuild-sdk-integration`

- [ ] **Package metadata configuration**
  - [ ] Add LICENSE.txt to all projects
  - [ ] Create package README files
  - [ ] Configure global tool settings
  - [ ] Add package icons (optional)

- [ ] **Local testing**
  - [ ] Build and pack all packages
  - [ ] Test SDK with sample projects
  - [ ] Test CLI tool installation
  - [ ] Validate .pgpac generation

- [ ] **NuGet.org publication**
  - [ ] Create NuGet.org account
  - [ ] Generate API key
  - [ ] Publish packages (mbulava.PostgreSql.Dac → MSBuild.Sdk.PostgreSql → postgresPacTools)
  - [ ] Verify installation from NuGet.org

- [ ] **CI/CD automation**
  - [ ] GitHub Actions workflow for releases
  - [ ] Automated testing on publish
  - [ ] Version management

**Timeline:** 2-3 weeks  
**See:** [docs/NUGET_PUBLISHING_PLAN.md](docs/NUGET_PUBLISHING_PLAN.md)

---

### 🎯 v1.1.0 Features (Planned)

#### **Multi-Schema Improvements**
- [ ] Full multi-schema support
- [ ] Cross-schema dependency tracking
- [ ] Schema-specific deployment
- [ ] Schema comparison

#### **Pre/Post Deployment Enhancement**
- [ ] Auto-discovery of deployment scripts
- [ ] Script ordering configuration
- [ ] Deployment script validation

#### **Visual Studio Integration**
- [ ] Project templates
- [ ] IntelliSense for .csproj
- [ ] Solution Explorer integration
- [ ] Build output window

#### **Performance & Scale**
- [ ] Large database optimization (10,000+ objects)
- [ ] Parallel extraction
- [ ] Incremental comparison
- [ ] Memory optimization

---

### 🚀 v2.0.0 Ideas (Future)

- [ ] **Azure DevOps Tasks** - Build/release pipeline tasks
- [ ] **GitHub Actions** - Ready-made workflow actions
- [ ] **Docker Images** - Containerized CLI
- [ ] **Web UI** - Browser-based schema comparison
- [ ] **VS Code Extension** - Lightweight editor support
- [ ] **Schema Drift Detection** - Compare deployed vs source
- [ ] **Rollback Support** - Generate undo scripts
- [ ] **Data Migration** - Seed data and static data tables
- [ ] **Schema Documentation** - Auto-generate markdown docs

---

## 🤝 Contributing

### How to Contribute

1. **Fork** the repository
2. **Create** a feature branch: `git checkout -b feature/my-feature`
3. **Make** your changes
4. **Write** tests (maintain 100% pass rate)
5. **Run** tests: `dotnet test`
6. **Commit**: `git commit -m "Add my feature"`
7. **Push**: `git push origin feature/my-feature`
8. **Open** a Pull Request

### Development Guidelines

- ✅ Follow existing code style
- ✅ Add tests for new features
- ✅ Update documentation
- ✅ Keep tests passing (201/201)
- ✅ Write clear commit messages
- ✅ One feature per PR

### Areas Needing Help

- 🐛 Bug fixes
- 📝 Documentation improvements
- 🧪 More test coverage
- 🌐 Multi-schema support
- 🎨 UI/UX for CLI output
- 🚀 Performance optimizations

---

## 🚀 Publishing & Releases

### Automated Publishing

pgPacTool uses **GitHub Actions** to automatically publish packages to NuGet.org:

- **Preview Releases** - Automatically published from `preview1` branch
- **Stable Releases** - Will be published from `main` branch (coming soon)

**How it works:**

1. Push to `preview1` branch → Workflow triggers automatically
2. Builds solution and runs tests
3. Packs all 3 NuGet packages
4. Verifies Npgquery embedding
5. Publishes to NuGet.org
6. Creates GitHub release with packages attached

**For maintainers:**

See [docs/PUBLISHING.md](docs/PUBLISHING.md) for detailed publishing instructions and workflow setup.

**Manual packaging:**

```powershell
# Pack all packages locally
.\scripts\Pack-PreviewRelease.ps1 -TestLocally

# Packages created in ./packages/
```

---

## 📊 Project Status

### Current Branch Status

| Branch | Status | Tests | Purpose |
|--------|--------|-------|---------|
| `main` | ✅ Stable | 183/183 | Production-ready features |
| `feature/cli-implementation` | ✅ Complete | 201/201 | CLI tool + integration tests |
| `feature/msbuild-sdk-integration` | 🚧 Active | N/A | MSBuild SDK + NuGet prep |

### Test Coverage

```
Total Tests: 201
├─ Unit Tests: 183 ✅
│  ├─ CLI Commands: 23
│  ├─ Schema Extraction: ~50
│  ├─ Compilation: ~40
│  ├─ Comparison: ~30
│  ├─ Publishing: ~20
│  └─ Other: ~20
└─ Integration Tests: 18 ✅
   ├─ CsprojIntegration: 10
   └─ CliIntegration: 8

Status: 100% Passing ✅
```

### Package Status

| Package | Version | Status |
|---------|---------|--------|
| **mbulava.PostgreSql.Dac** | 1.0.0-preview1 | ✅ Published to NuGet |
| **MSBuild.Sdk.PostgreSql** | 1.0.0-preview1 | ✅ Published to NuGet |
| **postgresPacTools** | 1.0.0-preview1 | ✅ Published to NuGet |

**Publication:** Automated via GitHub Actions from `preview1` branch

**Install:**
```bash
# CLI tool
dotnet tool install --global postgresPacTools --version 1.0.0-preview1

# Library
dotnet add package mbulava.PostgreSql.Dac --version 1.0.0-preview1
```

---

## 🙏 Acknowledgments

- **Inspired by:**
  - [MSBuild.Sdk.SqlProj](https://github.com/rr-wfm/MSBuild.Sdk.SqlProj) - MSBuild SDK for SQL Server
  - [SqlPackage](https://learn.microsoft.com/sql/tools/sqlpackage/) - Microsoft's database deployment tool

- **Built with:**
  - [Npgquery](https://github.com/JaredMSFT/Npgquery) - PostgreSQL query parser
  - [Npgsql](https://www.npgsql.org/) - PostgreSQL .NET client
  - [System.CommandLine](https://github.com/dotnet/command-line-api) - Modern CLI framework

---

## 📄 License

MIT License - see [LICENSE](LICENSE) file for details.

---

## 📧 Contact & Support

- **Issues:** https://github.com/mbulava-org/pgPacTool/issues
- **Discussions:** https://github.com/mbulava-org/pgPacTool/discussions
- **Repository:** https://github.com/mbulava-org/pgPacTool

---

**Build PostgreSQL databases like a pro! 🐘🚀**
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

## Features Summary

### Milestone 1: Extraction ✅
| Object Type | Extraction | AST Parsing | Privileges | SDK Export |
|-------------|------------|-------------|------------|------------|
| **Schemas** | ✅ | ✅ | ✅ | ✅ |
| **Tables** | ✅ | ✅ | ✅ | ✅ |
| **Views** | ✅ | ✅ | ✅ | ✅ |
| **Functions** | ✅ | ✅ | ✅ | ✅ |
| **Procedures** | ✅ | ✅ | ✅ | ✅ |
| **Triggers** | ✅ | ✅ | ❌ | ✅ |
| **Sequences** | ✅ | ✅ | ✅ | ✅ |
| **Types** | ✅ | ✅ | ✅ | ✅ |
| **Roles** | ✅ | N/A | N/A | ✅ |
| **Constraints** | ✅ | ✅ | N/A | ✅ |
| **Indexes** | ✅ | ✅ | N/A | ✅ |
| **Permissions** | ✅ | N/A | ✅ | ✅ |

**SDK Export:** Individual SQL files organized by schema and object type (`.csproj` format)

### Milestone 2: Compilation ✅
| Feature | Status | Description |
|---------|--------|-------------|
| **Dependency Analysis** | ✅ | Extracts all object dependencies |
| **Cycle Detection** | ✅ | Smart detection with severity levels |
| **Deployment Ordering** | ✅ | Topological sort for safe deployment |
| **Parallel Deployment** | ✅ | Groups objects by deployment level |
| **Error Reporting** | ✅ | Clear, actionable error messages |
| **Validation** | ✅ | Comprehensive project validation |
| **SDK Compilation** | ✅ | Compile .csproj to .pgpac |

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
| **[📖 User Guide](docs/USER_GUIDE.md)** | Getting started, SDK projects, troubleshooting |
| **[🔧 CLI Reference](docs/CLI_REFERENCE.md)** | Complete CLI command reference with SDK extraction |
| **[📦 SDK Guide](docs/SDK_PROJECT_GUIDE.md)** | MSBuild SDK and project structure guide |
| **[🔌 API Reference](docs/API_REFERENCE.md)** | Core library API documentation |
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
