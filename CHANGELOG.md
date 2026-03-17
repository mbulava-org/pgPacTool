# Changelog

All notable changes to pgPacTool will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

---

## [Unreleased]

### Planned for v1.0.0 (Stable Release)
- Complete multi-schema improvements
- Visual Studio project templates
- Performance optimizations for large databases (10,000+ objects)
- Enhanced error messages and validation

---

## [1.0.0-preview1] - 2026-03-17

**⚠️ PREVIEW RELEASE** - Not recommended for production use. Please test and provide feedback!

### 🎉 Initial Preview Release

This is the first public preview of pgPacTool, bringing SQL Server-style database project workflow to PostgreSQL.

### ✨ Features

#### Core Library (mbulava.PostgreSql.Dac)
- **Schema Extraction**
  - Extract complete database schemas with full metadata
  - Support for all major PostgreSQL object types
  - AST-based parsing using libpg_query
  - Privilege and permission extraction
  - Column comments and descriptions

- **Object Types Supported**
  - ✅ Tables (with columns, constraints, indexes)
  - ✅ Views (regular and materialized)
  - ✅ Functions (all languages: SQL, PL/pgSQL, C)
  - ✅ Stored Procedures
  - ✅ Types (ENUM, composite, domains)
  - ✅ Sequences
  - ✅ Triggers
  - ✅ Schemas
  - ✅ Roles and permissions
  - ✅ Extensions
  - ⚠️ Multi-schema support (basic implementation)

- **Compilation & Validation**
  - Dependency analysis and resolution
  - Circular dependency detection with severity levels
  - Topological sorting for deployment ordering
  - Parallel deployment grouping
  - Comprehensive error reporting

- **Schema Comparison**
  - Compare databases and identify differences
  - Generate CREATE/DROP/ALTER migration scripts
  - Pre-deployment and post-deployment script support
  - SQLCMD variable substitution ($(VarName) syntax)
  - Transaction-wrapped deployments

- **Project Formats**
  - JSON format (`.pgproj.json`) for data exchange
  - SDK-style project format (`.csproj`) for version control
  - DACPAC-style package format (`.pgpac`) for deployment

#### CLI Tool (postgresPacTools)
- **Global Tool Installation**
  - Install via `dotnet tool install --global postgresPacTools`
  - Command: `pgpac`

- **Commands**
  - `extract` - Export schema from database to JSON or .csproj
  - `compile` - Validate and build projects into .pgpac packages
  - `publish` - Deploy packages to target databases
  - `script` - Generate deployment SQL scripts
  - `deploy-report` - Preview changes as JSON report

- **Features**
  - Verbose mode for detailed output (`-v`)
  - Connection string support for all PostgreSQL configurations
  - Color-coded terminal output
  - Progress indicators for long operations

#### MSBuild SDK (MSBuild.Sdk.PostgreSql)
- **SDK Integration**
  - Standard `.csproj` format (no custom project types)
  - Convention-based SQL file discovery
  - Automatic dependency resolution
  - Incremental build support
  - Generates `.pgpac` deployment packages

- **Project Structure**
  - Organize SQL files by schema and object type
  - Individual file per database object
  - Git-friendly (easy diffing and merging)
  - Visual Studio compatible

### 🔧 PostgreSQL Version Support

- ✅ **PostgreSQL 16** (default version)
- ✅ **PostgreSQL 17** (full support)
- ❌ PostgreSQL 14, 15 (not currently supported, may add if demand exists)

Multi-version support includes:
- Version-aware parser selection
- Automatic library loading
- Version-specific feature detection
- Clear error messages for unsupported versions

### 🧪 Testing & Quality

- **201 tests** with 100% pass rate (171 unit + 30 Docker integration tests)
- Tested with real-world databases:
  - Simple: `world_happiness` (9 SQL files)
  - Medium: `dvdrental` (107 SQL files)
  - Complex: `pagila` (145 SQL files)

- **Test Coverage**
  - Schema extraction for all object types
  - Dependency resolution and cycle detection
  - SDK-style project generation
  - CLI command integration
  - Round-trip validation (extract → compile → deploy)

### 🐛 Bug Fixes

- Fixed null reference in sequence extraction
- Fixed aggregate function handling in function extraction
- Added database existence validation with clear errors
- Enhanced exception handling with verbose stack traces
- Improved connection pool management

### 📚 Documentation

- Comprehensive README with quick start guide
- CLI reference documentation
- User guide with workflows
- API reference for library usage
- SDK project guide
- Multi-version support documentation

### ⚠️ Known Limitations

1. **Multi-Schema Support**
   - Basic implementation present
   - Cross-schema dependency tracking limited
   - Improvements planned for v1.1.0

2. **Large Databases**
   - Not yet optimized for 10,000+ objects
   - Parallel extraction planned for v1.1.0

3. **Deployment Features**
   - Rollback support not yet implemented
   - Data migration tools not included
   - Schema drift detection planned

4. **Visual Studio Integration**
   - No project templates yet
   - Limited IntelliSense support
   - Planned for v1.1.0

### 🔄 Breaking Changes

N/A - Initial release

### 📦 Package Information

- **mbulava.PostgreSql.Dac** v1.0.0-preview1
  - Core library for programmatic access
  - Target Framework: .NET 10
  - License: MIT

- **postgresPacTools** v1.0.0-preview1
  - Global CLI tool
  - Command: `pgpac`
  - Target Framework: .NET 10
  - License: MIT

- **MSBuild.Sdk.PostgreSql** v1.0.0-preview1
  - MSBuild SDK for database projects
  - Target Framework: .NET 10
  - License: MIT

### 🙏 Acknowledgments

Inspired by:
- [MSBuild.Sdk.SqlProj](https://github.com/rr-wfm/MSBuild.Sdk.SqlProj) - MSBuild SDK for SQL Server
- [SqlPackage](https://learn.microsoft.com/sql/tools/sqlpackage/) - Microsoft's database deployment tool

Built with:
- [Npgquery](https://github.com/launchbadge/pg_query.net) - PostgreSQL query parser
- [Npgsql](https://www.npgsql.org/) - PostgreSQL .NET client
- [System.CommandLine](https://github.com/dotnet/command-line-api) - Modern CLI framework

---

## Future Releases

### [1.0.0-preview2] - Planned

**Focus: Community Feedback & Bug Fixes**

#### Planned Improvements
- Address community feedback from preview1
- Performance optimizations based on testing
- Enhanced error messages
- Documentation improvements
- Additional test coverage

### [1.1.0] - Planned

**Focus: Multi-Schema & Pre/Post Deployment**

#### Planned Features
- Full multi-schema support
- Cross-schema dependency tracking
- Auto-discovery of deployment scripts
- Script ordering configuration
- Large database optimizations (10,000+ objects)
- Parallel extraction

### [2.0.0] - Future Ideas

**Focus: Ecosystem Integration**

#### Ideas Under Consideration
- Azure DevOps pipeline tasks
- GitHub Actions workflows
- Docker images for CI/CD
- Web UI for schema comparison
- VS Code extension
- Schema drift detection
- Rollback support
- Data migration tools
- Auto-generated documentation

---

## Getting Help

- **Documentation**: https://github.com/mbulava-org/pgPacTool/tree/main/docs
- **Issues**: https://github.com/mbulava-org/pgPacTool/issues
- **Discussions**: https://github.com/mbulava-org/pgPacTool/discussions

---

## Contributing

We welcome contributions! See [CONTRIBUTING.md](CONTRIBUTING.md) for guidelines.

---

[Unreleased]: https://github.com/mbulava-org/pgPacTool/compare/v1.0.0-preview1...HEAD
[1.0.0-preview1]: https://github.com/mbulava-org/pgPacTool/releases/tag/v1.0.0-preview1
