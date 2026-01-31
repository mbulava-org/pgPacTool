# pgPacTool - PostgreSQL Data-Tier Application Compiler

A modern .NET 10 tool providing SQL Server Data Tools (.sqlproj) functionality for PostgreSQL databases.

[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4)](https://dotnet.microsoft.com/)
[![PostgreSQL](https://img.shields.io/badge/PostgreSQL-16%2B-336791)](https://www.postgresql.org/)
[![License](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)

---

## ?? What is pgPacTool?

pgPacTool brings the power of SQL Server Data Tools (SSDT) to PostgreSQL, enabling:

- **Database Version Control** - Store your database schema as code
- **Schema Comparison** - Compare databases and generate migration scripts
- **Deployment Automation** - Deploy schema changes safely and reliably
- **Package Distribution** - Share databases as NuGet packages
- **MSBuild Integration** - Build databases like any other .NET project

**Inspired by:** [MSBuild.Sdk.SqlProj](https://github.com/rr-wfm/MSBuild.Sdk.SqlProj) but designed specifically for PostgreSQL.

---

## ? Quick Start

### Prerequisites

**Required:**
- ? **PostgreSQL 16 or higher** (Required - [Why?](#postgresql-version-requirement))
- ? **.NET 10 SDK** or higher
- ? **Docker Desktop** (for development/testing)

**Optional:**
- Visual Studio 2024 or VS Code
- pgAdmin or DBeaver

### Installation

```bash
# Install as global tool
dotnet tool install --global mbulava.PostgresPacTools

# Or add to your project
dotnet add package MSBuild.Sdk.PgProj
```

### Basic Usage

```bash
# Extract database schema
pgpac extract --connection "Host=localhost;Database=mydb;Username=postgres" --output ./MyDatabase

# Build .pgpac package
cd MyDatabase
dotnet build

# Publish to target database
pgpac publish --source ./bin/Debug/MyDatabase.pgpac --target "Host=prod;Database=mydb"
```

---

## ?? Features

### Currently Available
- ? Table extraction (basic)
- ? Type extraction (enums, composites)
- ? Sequence extraction
- ? Schema extraction

### In Development (See [ROADMAP](.github/ROADMAP.md))
- ?? View extraction
- ?? Function extraction
- ?? Stored procedure extraction
- ?? Trigger extraction
- ?? Index extraction
- ?? Constraint extraction
- ?? Privilege/ACL extraction
- ?? Schema comparison
- ?? Deployment script generation
- ?? MSBuild SDK integration

---

## ?? Documentation

### Getting Started
- **[Quick Start Guide](.github/SCOPE.md)** - Get up and running in 5 minutes
- **[PostgreSQL Upgrade Guide](.github/POSTGRESQL_UPGRADE_GUIDE.md)** - Upgrading from older versions
- **[Project Structure](.github/ROADMAP.md)** - Understanding pgPacTool projects

### Development
- **[Issues Tracker](.github/ISSUES.md)** - All 25 planned features
- **[Development Roadmap](.github/ROADMAP.md)** - 7 milestones to v1.0
- **[Testing Strategy](.github/TESTING_STRATEGY.md)** - 90%+ code coverage standards
- **[Contributing Guide](CONTRIBUTING.md)** - How to contribute

### Reference
- **[Dependency Diagrams](.github/DEPENDENCIES.md)** - Visual issue dependencies
- **[Project Board](.github/PROJECT_BOARD.md)** - GitHub Project setup
- **[Scope Document](.github/SCOPE.md)** - Project scope and requirements

---

## ?? Project Structure

```
pgPacTool/
??? src/
?   ??? libs/
?   ?   ??? mbulava.PostgreSql.Dac/      # Core DAC library
?   ?       ??? Extract/                  # Database extraction
?   ?       ??? Compile/                  # SQL compilation
?   ?       ??? Compare/                  # Schema comparison
?   ?       ??? Publish/                  # Deployment
?   ?       ??? Models/                   # Data models
?   ??? postgresPacTools/                 # CLI tool
??? tests/
?   ??? mbulava.PostgreSql.Dac.Tests/    # Unit tests
?   ??? mbulava.PostgreSql.Dac.Integration.Tests/  # Integration tests
??? .github/
    ??? ISSUES.md                         # Issue tracker
    ??? ROADMAP.md                        # Development roadmap
    ??? ...                               # Additional docs
```

---

## ?? PostgreSQL Version Requirement

**pgPacTool requires PostgreSQL 16 or higher.**

### Why PostgreSQL 16+?

1. **Simplified Codebase** - No version branching or conditional logic
2. **Modern Features** - Use latest PostgreSQL capabilities
3. **Better Testing** - Single version to test (80% fewer test runs)
4. **LTS Support** - PostgreSQL 16 supported until November 2028
5. **All Features Available** - No need to check for procedures, generated columns, etc.

### What if I have PostgreSQL 15 or earlier?

You'll need to upgrade your PostgreSQL instance. See our [PostgreSQL Upgrade Guide](.github/POSTGRESQL_UPGRADE_GUIDE.md) for step-by-step instructions.

**Quick Upgrade:**
```bash
# 1. Backup your data
pg_dump mydb > mydb_backup.sql

# 2. Install PostgreSQL 16
# Download from: https://www.postgresql.org/download/

# 3. Restore your data
psql -U postgres -d mydb < mydb_backup.sql

# 4. Use pgPacTool
pgpac extract --connection "Host=localhost;Database=mydb"
```

For more details, see [SCOPE.md](.github/SCOPE.md).

---

## ?? Development Status

| Component | Status | Coverage |
|-----------|--------|----------|
| **Extraction** | ?? Partial | 45% |
| **Compilation** | ?? Not Started | 0% |
| **Comparison** | ?? Partial | 30% |
| **Publishing** | ?? Not Started | 0% |
| **CLI** | ?? Partial | 20% |
| **SDK** | ?? Not Started | 0% |
| **Overall** | ?? **Pre-Alpha** | **~25%** |

**Current Version:** v0.0.1 (Pre-Alpha)  
**Target MVP:** v0.1.0 (Core Extraction)  
**Target Production:** v1.0.0 (Q4 2024)

See [ROADMAP.md](.github/ROADMAP.md) for detailed timeline.

---

## ?? Contributing

We welcome contributions! Here's how to get started:

1. **Check Open Issues** - See [ISSUES.md](.github/ISSUES.md) for available work
2. **Look for `good first issue`** - Issue #1 (View Extraction) is a great starting point
3. **Read Contributing Guide** - See [CONTRIBUTING.md](CONTRIBUTING.md)
4. **Follow Standards** - 90%+ code coverage required ([TESTING_STRATEGY.md](.github/TESTING_STRATEGY.md))

### Development Environment Setup

```bash
# Clone the repository
git clone https://github.com/mbulava-org/pgPacTool.git
cd pgPacTool

# Restore dependencies
dotnet restore

# Build
dotnet build

# Run tests
dotnet test

# Run with coverage
dotnet test /p:CollectCoverage=true
```

---

## ?? Project Metrics

- **Total Issues:** 25 planned features
- **Story Points:** 213 total (89 for MVP)
- **Timeline:** 28-32 weeks to v1.0
- **Test Coverage Target:** ?90%
- **PostgreSQL Versions:** 16+ only

---

## ??? Roadmap

### Milestone 1: Core Extraction (v0.1.0) - Weeks 1-8
- [x] Fix privilege extraction (Issue #7)
- [ ] Extract views, functions, procedures, triggers
- [ ] Extract indexes and constraints
- [ ] Integration test infrastructure

### Milestone 2: Compilation (v0.2.0) - Weeks 9-12
- [ ] Reference validation
- [ ] Circular dependency detection

### Milestone 3: Comparison (v0.3.0) - Weeks 13-16
- [ ] Schema comparison
- [ ] Diff reports

### Milestone 4: Deployment (v0.4.0) - Weeks 17-20
- [ ] Script generation
- [ ] Pre/post deployment scripts
- [ ] CLI publish command

### Milestone 5-7: Packaging & SDK (v1.0.0) - Weeks 21-32
- [ ] DacPackage format
- [ ] NuGet packaging
- [ ] MSBuild SDK
- [ ] Container publishing

See full roadmap: [ROADMAP.md](.github/ROADMAP.md)

---

## ?? License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

---

## ?? Links

- **Documentation:** [.github/](.github/)
- **Issues:** [ISSUES.md](.github/ISSUES.md)
- **Roadmap:** [ROADMAP.md](.github/ROADMAP.md)
- **Testing:** [TESTING_STRATEGY.md](.github/TESTING_STRATEGY.md)
- **PostgreSQL:** [https://www.postgresql.org/](https://www.postgresql.org/)
- **MSBuild.Sdk.SqlProj:** [https://github.com/rr-wfm/MSBuild.Sdk.SqlProj](https://github.com/rr-wfm/MSBuild.Sdk.SqlProj)

---

## ? FAQ

### Why not support PostgreSQL 15 and earlier?

Supporting PostgreSQL 16+ only significantly simplifies development, testing, and maintenance. All modern features are available, and PostgreSQL 16 is LTS until 2028. See [SCOPE.md](.github/SCOPE.md) for full rationale.

### Can I use this in production?

Not yet - pgPacTool is in pre-alpha development. Target production readiness is Q4 2024 (v1.0.0).

### How does this differ from MSBuild.Sdk.SqlProj?

MSBuild.Sdk.SqlProj is for SQL Server; pgPacTool is designed specifically for PostgreSQL with PostgreSQL-specific features, types, and syntax support.

### Can I contribute?

Yes! See [CONTRIBUTING.md](CONTRIBUTING.md) and [ISSUES.md](.github/ISSUES.md) for available work.

---

**Built with ?? for the PostgreSQL community**

**Status:** ?? Pre-Alpha Development  
**Version:** 0.0.1  
**Last Updated:** 2026-01-31
