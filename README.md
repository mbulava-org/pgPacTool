# pgPacTool - PostgreSQL Data-Tier Application Tools

**Build PostgreSQL databases like SQL Server SSDT!** MSBuild SDK + CLI tools for database-as-code.

[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4)](https://dotnet.microsoft.com/)
[![PostgreSQL](https://img.shields.io/badge/PostgreSQL-16%2B-336791)](https://www.postgresql.org/)
[![License](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)
![Tests](https://img.shields.io/badge/tests-201%20passing-success)
[![Version](https://img.shields.io/badge/version-1.0.0--preview8-orange)](https://github.com/mbulava-org/pgPacTool/releases)

> **📦 Current Preview Target (v1.0.0-preview8)**:
> - ✅ **MSBuild SDK** - Package version ready: `MSBuild.Sdk.PostgreSql/1.0.0-preview8`
> - ✅ **CLI Tool (pgpac)** - Package version ready: `postgresPacTools/1.0.0-preview8`

> - ✅ **Core Library** - Package version ready: `mbulava.PostgreSql.Dac/1.0.0-preview8` (includes Npgquery)
>
> **💡 PostgreSQL Version Support**: Currently supports **PostgreSQL 16 and 17**. Older versions (14, 15) may be added in the future based on demand. See [Multi-Version Support Documentation](docs/features/multi-version-support/README.md) for details.
>
> **🧠 Embedded Copilot Skill**: Repository-aware PostgreSQL guidance is available in the
> [PostgreSQL Expert Skill](.github/skills/postgresql-expert/README.md), backed by the
> [Roles/Security](docs/version-differences/PG_ROLES_PERMISSIONS_SECURITY.md) and
> [Database Objects](docs/version-differences/PG_DATABASE_OBJECTS.md) version references.

---

## 🚀 What is pgPacTool?

pgPacTool brings **SQL Server-style database project workflow** to PostgreSQL. Inspired by SqlPackage and SSDT, it enables:

- 📦 **MSBuild SDK** - Build database projects with `dotnet build`
- 🔧 **CLI Tool** - Extract, compile, and deploy schemas
- 📚 **Core Library** - Programmatic schema operations
- ✅ **Validation** - Dependency checking and circular reference detection
- 🔄 **CI/CD Ready** - Perfect for DevOps pipelines

---

## ✨ Current Features (v1.0.0-preview8)

### 🎯 Complete Functionality

#### **MSBuild SDK Integration** 
- ✅ `MSBuild.Sdk.PostgreSql` - SDK for database projects
- ✅ Convention-based project structure
- ✅ Automatic SQL file discovery
- ✅ Build integration with `dotnet build`
- ✅ Generates `.pgpac` packages
- ✅ Incremental build support
- ✅ Visual Studio compatible via the published SDK package

#### **SDK-Style Project Extraction** 
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

### Prerequisites

- **.NET 10 SDK** - [Download here](https://dotnet.microsoft.com/download/dotnet/10.0)
- **PostgreSQL 16 or 17** - Local or remote instance

### Visual Studio / Solution Setup

- **No separate project template install is required for `preview1`**. Create the `.csproj` manually or generate one with `pgpac extract --target-file output/mydb/mydb.csproj`.
- **To load the custom SDK project in Visual Studio**, the SDK package must be restorable from a package source available to the solution:
  - published preview: use `nuget.org`
  - local SDK testing: add a `nuget.config` that points to your local package feed before opening the solution
- If Visual Studio opens the project but cannot resolve `MSBuild.Sdk.PostgreSql`, run `dotnet restore` from the solution or project directory, then reload the project.

**Example `nuget.config` for local SDK validation:**

```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <add key="LocalFeed" value="C:\LocalNuGet" />
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
  </packageSources>
</configuration>
```

---

### 🎯 Option 1: Create New Database Project (MSBuild SDK)

Perfect for **new databases** or starting fresh with infrastructure-as-code:

#### Step 1: Create Project File

```powershell
# Create a new directory for your database project
mkdir MyDatabase
cd MyDatabase

# Create MyDatabase.csproj with SDK reference
```

```xml
<!-- MyDatabase.csproj -->
<Project Sdk="Microsoft.NET.Sdk">

  <Sdk Name="MSBuild.Sdk.PostgreSql" Version="1.0.0-preview8" />

  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <DatabaseName>MyDatabase</DatabaseName>
    <PostgresVersion>16</PostgresVersion>
    <DefaultSchema>public</DefaultSchema>
    <OutputFormat>pgpac</OutputFormat>
  </PropertyGroup>

</Project>
```

> **Visual Studio note:** open `MyDatabase.csproj` directly or add it to an existing `.sln` after restore succeeds. No separate template installation is required for the published SDK.

#### Step 2: Add SQL Files

Organize your schema using convention-based folders:

```
MyDatabase/
├── MyDatabase.csproj
├── Tables/
│   ├── Users.sql
│   └── Orders.sql
├── Views/
│   └── CustomerOrders.sql
└── Functions/
    └── GetActiveUsers.sql
```

**Tables/Users.sql:**
```sql
CREATE TABLE public.users (
    id SERIAL PRIMARY KEY,
    username VARCHAR(50) NOT NULL,
    email VARCHAR(100) NOT NULL,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);
```

**Views/CustomerOrders.sql:**
```sql
CREATE VIEW public.customer_orders AS
SELECT u.username, COUNT(o.id) as order_count
FROM users u
LEFT JOIN orders o ON u.id = o.user_id
GROUP BY u.username;
```

#### Step 3: Build Project

```powershell
# Build the database package
dotnet build

# ✅ Output: bin/Debug/net10.0/MyDatabase.pgpac
```

**What happens:**
- ✅ SQL files are discovered automatically
- ✅ Dependencies are analyzed (orders matter!)
- ✅ Objects are sorted topologically
- ✅ `.pgpac` package is generated

#### Step 4: Deploy

```powershell
# Deploy to PostgreSQL
pgpac publish -sf bin/Debug/net10.0/MyDatabase.pgpac \
  -tcs "Host=localhost;Database=mydb;Username=postgres;Password=***"
```

---

### 📥 Option 2: Extract Existing Database ⭐ RECOMMENDED FOR MIGRATION

Perfect for **bringing existing databases** under version control:

#### Step 1: Install CLI Tool

```bash
# Install globally from NuGet
dotnet tool install -g postgresPacTools --version 1.0.0-preview8
```

#### Step 2: Extract Your Database

```bash
# Extract to SDK-style .csproj project
pgpac extract \
  --source-connection-string "Host=localhost;Database=mydb;Username=postgres;Password=***" \
  --target-file output/mydb/mydb.csproj \
  --verbose
```

**Result: Complete project with individual SQL files and an SDK-style `.csproj` that Visual Studio can restore and load.**

```
output/mydb/
├── mydb.csproj                    # ← MSBuild SDK project
├── public/
│   ├── Tables/
│   │   ├── users.sql
│   │   ├── orders.sql
│   │   └── products.sql
│   ├── Views/
│   │   ├── customer_orders.sql
│   │   └── product_summary.sql
│   ├── Functions/
│   │   ├── get_active_users.sql
│   │   └── calculate_total.sql
│   └── Indexes/
│       └── idx_users_email.sql
└── Security/
    ├── Schemas/
    ├── Roles/
    └── Extensions/
```

#### Step 3: Version Control & Build

```bash
cd output/mydb
git init
git add .
git commit -m "Initial database schema extraction"

# Build the project
dotnet build
# ✅ Output: bin/Debug/net10.0/mydb.pgpac
```

**What you get:**
- ✅ **One SQL file per database object** (version control friendly!)
- ✅ **Organized by schema and object type**
- ✅ **Editable in any text editor or IDE**
- ✅ **Compilable with `dotnet build`**
- ✅ **Dependency-sorted** (automatic topological ordering)

**Real-World Examples:**

| Database | Objects | Generated Files | Build Time |
|----------|---------|-----------------|------------|
| **world_happiness** | 1 table, basic | 9 files | < 1s |
| **dvdrental** | 15 tables, 7 views, functions | 107 files | < 2s |
| **pagila** | 21 tables, 54 indexes, complex | 145 files | < 3s |

---

### 🔧 Option 3: CLI Tool (Ad-Hoc Operations)

**Install globally:**

```powershell
# Install from NuGet
dotnet tool install --global postgresPacTools

# Extract schema to JSON format
pgpac extract -scs "Host=localhost;Database=mydb;..." -tf mydb.pgproj.json

# Extract schema to SDK-style project
pgpac extract -scs "Host=localhost;Database=mydb;..." -tf mydb/mydb.csproj

# Compile project
pgpac compile -sf MyDatabase.csproj

# Generate deployment script (preview changes)
pgpac script -sf MyDatabase.pgpac -tcs "Host=prod;..." -o deploy.sql

# Deploy to database
pgpac publish -sf MyDatabase.pgpac -tcs "Host=prod;..."

# Generate deployment report (JSON)
pgpac deploy-report -sf MyDatabase.pgpac -tcs "Host=prod;..." -o report.json
```

---

### 🛠️ Option 4: Core Library (For Custom Tooling)

**Published to NuGet!**

```powershell
# Install core DAC library (includes Npgquery parser)
dotnet add package mbulava.PostgreSql.Dac --version 1.0.0-preview8
```

```csharp
using mbulava.PostgreSql.Dac.Extract;
using mbulava.PostgreSql.Dac.Compile;
using Npgquery;

// Extract schema from live database
var connectionString = "Host=localhost;Database=mydb;Username=postgres;Password=***";
var extractor = new PgProjectExtractor(connectionString);
var project = await extractor.ExtractPgProject("mydb");

// Generate SDK-style .csproj project with individual SQL files
var generator = new CsprojProjectGenerator("output/mydb/mydb.csproj");
await generator.GenerateProjectAsync(project);

// Or compile and validate in-memory
var compiler = new ProjectCompiler();
var result = compiler.Compile(project);

if (result.Errors.Count == 0)
{
    Console.WriteLine($"✅ {result.DeploymentOrder.Count} objects validated");
    Console.WriteLine("Deployment order:");
    foreach (var obj in result.DeploymentOrder)
    {
        Console.WriteLine($"  → {obj.Type} {obj.Name}");
    }
}
```

**Or parse PostgreSQL SQL directly with Npgquery:**

```csharp
using Npgquery;

// Parse SQL and get AST
using var parser = new Parser(PostgreSqlVersion.Postgres16);
var result = parser.Parse("SELECT * FROM users WHERE id = 1");

if (result.IsSuccess)
{
    Console.WriteLine("SQL is valid!");
    Console.WriteLine($"Parse tree: {result.Tree}");
}
```

**Use cases:**
- Custom database migration tools
- Schema comparison utilities
- CI/CD pipeline integrations
- Database documentation generators
- Schema validation services
- SQL parsers and linters
- Query analyzers

---

## 🔍 What Happens During Build?

When you run `dotnet build` on a PostgreSQL database project:

### 1. **SQL File Discovery** 📂
- SDK automatically finds all `.sql` files in project directory
- Follows convention-based folder structure
- No manual ItemGroup entries needed (like C# with `*.cs` files)

### 2. **Parsing & AST Generation** 🌳
- Each SQL file parsed using **libpg_query** (PostgreSQL's official parser)
- Generates Abstract Syntax Tree (AST)
- Validates SQL syntax against PostgreSQL 16/17 grammar
- **Syntax errors = build failures** (fail fast!)

### 3. **Dependency Analysis** 🔗
- Extracts object references from AST
- Builds dependency graph:
  - `customer_orders` VIEW → depends on → `users`, `orders` TABLES
  - `calculate_tax()` FUNCTION → depends on → `tax_rates` TABLE
- Detects circular references (e.g., View A → View B → View A)

### 4. **Topological Sorting** 📊
- Determines correct deployment order
- Dependencies deployed first:
  ```
  1. Extensions (uuid-ossp, pg_trgm)
  2. Schemas (public, auth, api)
  3. Types (ENUM, composite)
  4. Tables (base tables first, FK tables after)
  5. Views (simple first, dependent after)
  6. Functions & Procedures
  7. Triggers
  8. Indexes & Constraints
  ```

### 5. **Package Generation** 📦
- Creates `.pgpac` file (PostgreSQL DACPAC equivalent)
- Contains:
  - ✅ All SQL definitions (sorted)
  - ✅ Deployment order manifest
  - ✅ Dependency graph
  - ✅ Pre/Post deployment scripts
  - ✅ SQLCMD variables
  - ✅ Project metadata

### 6. **Validation** ✅
- **Dependency validation**: All referenced objects exist?
- **Circular reference detection**: Any dependency loops?
- **Syntax validation**: All SQL parses correctly?
- **Schema validation**: All objects have valid definitions?

**Build Output:**
```
MSBuild version 18.4.1+...
  Compiling PostgreSQL Database Project: MyDatabase
    📁 Project: C:\projects\MyDatabase\MyDatabase.csproj
    📄 SQL files: 47
    ⚙️  Loading project...
    ✅ Loaded 1 schema(s)
    📊 Objects extracted: 47
    🔗 Building dependency graph...
    ✅ Dependency analysis complete: 47 objects, 0 errors
    📦 Creating package...
  ✅ Build succeeded

Build succeeded.
    0 Warning(s)
    0 Error(s)

Time Elapsed 00:00:02.15
```

---

## 📋 Common Workflows

### Workflow 1: New Database Project from Scratch

**Scenario:** You're starting a new microservice and need a database.

```powershell
# 1. Create project
mkdir UserService.Database
cd UserService.Database

# Create UserService.Database.csproj
@"
<Project Sdk="MSBuild.Sdk.PostgreSql/1.0.0-preview8">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <DatabaseName>UserService</DatabaseName>
  </PropertyGroup>
</Project>
"@ | Out-File -FilePath UserService.Database.csproj

# 2. Add schema files
mkdir Tables, Views, Functions, Security

# Tables/users.sql
@"
CREATE TABLE public.users (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    email VARCHAR(255) NOT NULL UNIQUE,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);
"@ | Out-File -FilePath Tables/users.sql

# 3. Build and validate
dotnet build

# 4. Version control
git init
git add .
git commit -m "Initial database schema"
```

**Result:** ✅ Database schema as code, ready for deployment!

---

### Workflow 2: Migrate Existing Database to Version Control

**Scenario:** You have a production database with 50 tables, need to get it under version control.

```powershell
# 1. Install pgpac CLI tool
dotnet tool install -g postgresPacTools

# 2. Extract your production schema
pgpac extract \
  --source-connection-string "Host=prod.company.com;Database=maindb;Username=readonly;Password=***" \
  --target-file maindb-repo/maindb.csproj \
  --verbose

# 3. Review extracted files
cd maindb-repo
tree /F  # Windows
# or: find . -type f  # Linux/Mac

# 4. Initialize git repository
git init
git add .
git commit -m "Initial extraction from production (2026-05-15)"

# 5. Push to your repo
git remote add origin https://github.com/yourorg/maindb.git
git push -u origin main

# 6. Build to validate
dotnet build
```

**Result:** ✅ Production database now under version control with 145+ individual SQL files!

---

### Workflow 3: Make Schema Changes in Dev Environment

**Scenario:** Add new feature requiring schema changes.

```powershell
# 1. Create feature branch
git checkout -b feature/add-orders-table

# 2. Add new table
@"
CREATE TABLE public.orders (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id UUID NOT NULL REFERENCES users(id),
    total DECIMAL(10,2) NOT NULL,
    status VARCHAR(20) DEFAULT 'pending',
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);
"@ | Out-File -FilePath Tables/orders.sql

# 3. Add supporting view
@"
CREATE VIEW public.user_orders AS
SELECT u.email, o.id AS order_id, o.total, o.status
FROM users u
LEFT JOIN orders o ON u.id = o.user_id;
"@ | Out-File -FilePath Views/user_orders.sql

# 4. Build (validates syntax & dependencies)
dotnet build

# ✅ Build checks:
#    - SQL syntax valid?
#    - users.id referenced by orders.user_id exists?
#    - Deployment order correct? (users → orders → user_orders)

# 5. Commit changes
git add .
git commit -m "feat: Add orders table and user_orders view"
git push origin feature/add-orders-table

# 6. Create PR for review
```

**Result:** ✅ Schema changes validated, tested, and ready for review!

---

### Workflow 4: Deploy Schema Changes to Staging/Production

**Scenario:** Reviewed PR merged, deploy changes to staging then production.

```powershell
# 1. Pull latest changes
git checkout main
git pull origin main

# 2. Build package
dotnet build --configuration Release
# Output: bin/Release/net10.0/MyDatabase.pgpac

# 3. Preview changes before deployment
pgpac script \
  -sf bin/Release/net10.0/MyDatabase.pgpac \
  -tcs "Host=staging;Database=mydb;..." \
  -o preview-staging.sql

# 4. Review SQL script
cat preview-staging.sql
# Expected:
# -- Create table orders
# CREATE TABLE public.orders (...);
# -- Create view user_orders
# CREATE VIEW public.user_orders AS ...;

# 5. Deploy to staging
pgpac publish \
  -sf bin/Release/net10.0/MyDatabase.pgpac \
  -tcs "Host=staging;Database=mydb;Username=deploy;Password=***"

# 6. Test in staging environment
# ... run integration tests ...

# 7. Deploy to production (if tests pass)
pgpac publish \
  -sf bin/Release/net10.0/MyDatabase.pgpac \
  -tcs "Host=prod;Database=mydb;Username=deploy;Password=***"
```

**Result:** ✅ Controlled, validated deployment to production!

---

### Workflow 5: CI/CD Pipeline Integration

**Scenario:** Automate build and deployment with GitHub Actions.

```yaml
# .github/workflows/database-ci.yml
name: Database CI/CD

on:
  push:
    branches: [ main ]
  pull_request:
    branches: [ main ]

jobs:
  build:
    runs-on: ubuntu-latest

    steps:
    - uses: actions/checkout@v3

    - name: Setup .NET 10
      uses: actions/setup-dotnet@v3
      with:
        dotnet-version: '10.0.x'

    - name: Build database project
      run: dotnet build --configuration Release

    - name: Upload .pgpac artifact
      uses: actions/upload-artifact@v3
      with:
        name: database-package
        path: bin/Release/net10.0/*.pgpac

  deploy-staging:
    needs: build
    runs-on: ubuntu-latest
    if: github.ref == 'refs/heads/main'

    steps:
    - uses: actions/download-artifact@v3
      with:
        name: database-package

    - name: Deploy to staging
      run: |
        pgpac publish \
          -sf MyDatabase.pgpac \
          -tcs "${{ secrets.STAGING_CONNECTION_STRING }}"
```

**Result:** ✅ Automated database deployments with full audit trail!

---

## 💻 Local Development (Build from Source)

### Prerequisites

- ✅ **.NET 10 SDK** - https://dotnet.microsoft.com/download/dotnet/10.0
- ✅ **PostgreSQL 16 or 17** - For testing (Docker recommended)
- ✅ **Git** - Source control
- ✅ **Visual Studio 2026** or **VS Code** (optional)

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

### Run CLI Locally (For Development)

**Note:** For normal use, install from NuGet: `dotnet tool install -g postgresPacTools`

```powershell
# For development/debugging: Build CLI from source
dotnet build src/postgresPacTools/postgresPacTools.csproj

# Run without installing
dotnet run --project src/postgresPacTools/postgresPacTools.csproj -- --help

# Extract example
pgpac extract \
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
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
  </packageSources>
</configuration>
"@ | Out-File -FilePath nuget.config

# Update .csproj to use local version
# Restore so Visual Studio/dotnet can resolve the SDK from the local feed
dotnet restore

# Open SampleDatabase.csproj or SampleDatabase.sln in Visual Studio if desired
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

1. Open `pgPacTool.sln` in Visual Studio 2026+
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

**Preview 1 publishing and install validation**
- ✅ Published `mbulava.PostgreSql.Dac` to NuGet
- ✅ Published `MSBuild.Sdk.PostgreSql` to NuGet
- ✅ Published `postgresPacTools` as a .NET global tool
- ✅ Validated README Quick Start flows against clean package installs
- ✅ Validated SDK `.csproj` builds from restored NuGet packages
- ✅ Validated CLI `compile` and `extract` workflows end-to-end

**Packaging and build reliability improvements**
- ✅ MSBuild SDK package now carries its runtime/task assets correctly
- ✅ SDK build output path defaults align with README examples
- ✅ Extracted SDK-style projects build successfully after generation
- ✅ NuGet package tests cover published package consumption scenarios

---

### 📦 Post-Publish Priorities

**Current release line:** `1.0.0-preview8`

- [ ] **Release hardening**
  - [ ] Expand package-consumption and upgrade-path validation
  - [ ] Reduce remaining warnings in build and test output
  - [ ] Add more end-to-end coverage for publish/script/deploy-report flows

- [ ] **Documentation polish**
  - [ ] Refresh walkthroughs and screenshots for the published preview
  - [ ] Add troubleshooting for SDK restore/build/package scenarios
  - [ ] Keep version support guidance explicit for PostgreSQL 16 and 17

- [ ] **Release automation**
  - [ ] Finalize repeatable versioning/release workflow
  - [ ] Add publish verification gates for NuGet packages and tool install
  - [ ] Attach release artifacts and notes automatically

- [ ] **Product polish for v1**
  - [ ] Improve multi-schema support
  - [ ] Enhance deployment script discovery/configuration
  - [ ] Continue Visual Studio workflow improvements

**See:** [docs/NUGET_PUBLISHING_PLAN.md](docs/NUGET_PUBLISHING_PLAN.md)

---

### 🎯 v1.0.0 Features (Planned)

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

## 🔧 Troubleshooting

### Build Errors

#### "SDK 'MSBuild.Sdk.PostgreSql/1.0.0-preview8' not found"

**Problem:** MSBuild cannot find the SDK package.

**Solution:**
```powershell
# Option 1: Refresh package caches and restore from NuGet
dotnet nuget locals all --clear
dotnet restore --force

# Option 2: For local SDK validation, pack locally and use a local package source
# (see "Test MSBuild SDK Locally" section)
```

#### "Could not load file or assembly 'libpg_query_16.dll'"

**Problem:** Native PostgreSQL parser libraries not found.

**Solution:**
```powershell
# Ensure you're on a supported platform:
# - Windows x64
# - Linux x64
# - macOS ARM64 (M1/M2/M3)

# If running on Windows, ensure Visual C++ Redistributable installed:
# https://learn.microsoft.com/en-us/cpp/windows/latest-supported-vc-redist
```

#### "Circular dependency detected"

**Problem:** Your database has circular view/function references.

**Solution:**
```sql
-- Example: View A depends on View B, View B depends on View A
-- Solution: Combine into single view or refactor logic

-- Instead of:
CREATE VIEW view_a AS SELECT * FROM view_b;
CREATE VIEW view_b AS SELECT * FROM view_a;  -- ❌ Circular!

-- Do:
CREATE VIEW combined_view AS
SELECT ... -- combined logic
```

#### "Table 'xyz' not found during dependency analysis"

**Problem:** SQL references an object that doesn't exist in project.

**Solution:**
```powershell
# 1. Check if file exists in project directory
dir -Recurse -Filter "*.sql" | Select-String "CREATE TABLE xyz"

# 2. If missing, add the SQL file:
#    Tables/xyz.sql with CREATE TABLE statement

# 3. If it's an external dependency (another database), consider:
#    - Adding placeholder files
#    - Using SQLCMD variables
#    - Documenting external dependencies
```

---

### Extract Errors

#### "Connection to database failed"

**Problem:** Cannot connect to PostgreSQL.

**Solution:**
```powershell
# Test connection manually:
psql "Host=localhost;Database=mydb;Username=postgres;Password=secret"

# Common issues:
# 1. PostgreSQL not running: service postgresql status
# 2. Wrong port: Default is 5432
# 3. Firewall blocking connection
# 4. pg_hba.conf not allowing connection
```

#### "Database 'mydb' does not exist"

**Problem:** Target database doesn't exist.

**Solution:**
```bash
# Create database first:
createdb mydb

# Or in psql:
CREATE DATABASE mydb;
```

#### "Permission denied for schema public"

**Problem:** User doesn't have read permissions.

**Solution:**
```sql
-- Grant read permissions:
GRANT USAGE ON SCHEMA public TO your_user;
GRANT SELECT ON ALL TABLES IN SCHEMA public TO your_user;
```

---

### Runtime Errors

#### "Parser failed with unsupported SQL syntax"

**Problem:** SQL contains syntax not supported by PostgreSQL 16/17 parser.

**Solution:**
```sql
-- Check PostgreSQL version compatibility
SELECT version();

-- If using PG 14/15 specific syntax:
-- 1. Update SQL to PG 16+ syntax
-- 2. Or wait for PG 14/15 support (see roadmap)

-- Example: Old syntax
SELECT * FROM json_populate_recordset(...);  -- PG 12-15

-- New syntax:
SELECT * FROM JSON_TABLE(...);  -- PG 17+
```

---

### Performance Issues

#### "Extraction taking too long (large database)"

**Problem:** Database has 1000+ objects, extraction slow.

**Current workarounds:**
```powershell
# 1. Extract specific schemas:
#    (Feature coming soon - currently extracts all schemas)

# 2. Split into multiple projects by schema

# 3. Use --verbose to see progress
dotnet run --project src/postgresPacTools -- extract ... --verbose
```

**Future improvement:** Parallel extraction (on roadmap)

---

### Platform-Specific Issues

#### Windows: "MSB4062: Task could not be loaded"

**Problem:** Native DLL loading issue on Windows.

**Solution:**
```powershell
# Install Visual C++ Redistributable:
# https://aka.ms/vs/17/release/vc_redist.x64.exe

# Or install via winget:
winget install Microsoft.VCRedist.2015+.x64
```

#### Linux: "libpg_query_16.so not found"

**Problem:** Native library not found or missing dependencies.

**Solution:**
```bash
# Install dependencies:
sudo apt-get install libicu-dev  # Ubuntu/Debian
sudo yum install libicu           # RHEL/CentOS

# Verify library:
ldd path/to/libpg_query_16.so
```

#### macOS: "dyld: Library not loaded"

**Problem:** Native library not signed or architecture mismatch.

**Solution:**
```bash
# M1/M2/M3 (ARM64) required
uname -m  # Should show: arm64

# If using Rosetta (x86_64), install native .NET 10:
# https://dotnet.microsoft.com/download/dotnet/10.0
```

---

### Getting Help

If you encounter issues not listed here:

1. **Check Existing Issues**: [GitHub Issues](https://github.com/mbulava-org/pgPacTool/issues)
2. **Enable Verbose Logging**: Add `--verbose` to CLI commands
3. **Create New Issue**: Include:
   - OS and .NET version (`dotnet --info`)
   - PostgreSQL version (`SELECT version();`)
   - Full error message and stack trace
   - Steps to reproduce
   - Sample SQL (if applicable)

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

### Current Release Status

| Area | Status | Notes |
|------|--------|-------|
| Core DAC library | ✅ Ready | `mbulava.PostgreSql.Dac` `1.0.0-preview8` |
| MSBuild SDK | ✅ Ready | `MSBuild.Sdk.PostgreSql/1.0.0-preview8` |
| CLI tool | ✅ Ready | `dotnet tool install -g postgresPacTools --version 1.0.0-preview8` |
| PostgreSQL support | ✅ Active | Supported versions: PostgreSQL 16 and 17 |
| Current development branch | ✅ Active | `preview1` |

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
| **mbulava.PostgreSql.Dac** | 1.0.0-preview8 | ✅ Published to NuGet |
| **MSBuild.Sdk.PostgreSql** | 1.0.0-preview8 | ✅ Published to NuGet |
| **postgresPacTools** | 1.0.0-preview8 | ✅ Published to NuGet |

**Publication:** Published for the `preview1` release line and validated with README install/build flows.

**Install:**
```bash
# CLI tool
dotnet tool install --global postgresPacTools --version 1.0.0-preview8

# Library
dotnet add package mbulava.PostgreSql.Dac --version 1.0.0-preview8
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
| **[🔌 API Reference](docs/API_REFERENCE.md)** | Core library API documentation with code examples |
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

### ✅ Milestone 4: Packaging & Distribution (COMPLETE FOR PREVIEW 1)
- NuGet packages published
- Global tool packaging published
- Package install validation completed
- README quick start validation added

### ✅ Milestone 5: MSBuild SDK (COMPLETE FOR PREVIEW 1)
- SDK-style `.csproj` support
- MSBuild integration
- Clean package restore/build validation
- Extracted project build support

### 📋 Milestone 6: Post-Publish Hardening (CURRENT)
- Deployment automation improvements
- Rollback support
- Publishing profiles and release polish
- Expanded end-to-end publish/script validation

### 📋 Milestone 7: Developer Experience
- Project templates
- Additional Visual Studio workflow improvements
- Documentation and troubleshooting polish

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

**Status:** Preview 6 ready for local validation ✅  
**Version:** 1.0.0-preview8  
**Last Updated:** 2026-04-21
