# postgresPacTools

**PostgreSQL Data-Tier Application CLI Tools** - SqlPackage for PostgreSQL

[![NuGet](https://img.shields.io/nuget/v/postgresPacTools.svg)](https://www.nuget.org/packages/postgresPacTools/)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](https://opensource.org/licenses/MIT)

Command-line tools for PostgreSQL database lifecycle management. Extract, compile, validate, and deploy database schemas as code.

## 🚀 Installation

### Install as Global Tool
```
dotnet tool install --global postgresPacTools
```

### Verify Installation
```
pgpac --version
Output: 1.0.0-preview1
```

## 📚 Commands

### `extract` - Export Database Schema

Extract schema from a live database to JSON or SDK-style project.

**To JSON Format:**
```
pgpac extract 
--source-connection-string "Host=localhost;Database=mydb;Username=postgres;Password=***" 
--target-file mydb.pgproj.json 
--database-name mydb
```

**To SDK-Style Project (Recommended):**
```
pgpac extract 
--source-connection-string "Host=localhost;Database=mydb;Username=postgres;Password=***" 
--target-file output/mydb/mydb.csproj 
--database-name mydb
```
**Result:**
output/mydb/
├── mydb.csproj
├── public/ 
│   ├── Tables/ 
│   │   └── users.sql 
│   ├── Views/ 
│   ├── Functions/ 
│   └── Procedures/ 
└── Security/ 
├── Roles/ 
└── Permissions/

**Options:**
- `-scs` / `--source-connection-string` - Database connection string (required)
- `-tf` / `--target-file` - Output file path (required)
- `-dn` / `--database-name` - Database name for project
- `-v` / `--verbose` - Show detailed output

### `compile` - Validate and Build Project

Compile a project file to validate dependencies and generate a `.pgpac` package.

**From SDK Project:**
```
pgpac compile 
--source-file mydb.csproj 
--target-file bin/mydb.pgpac
```
**From JSON:**
pgpac compile 
--source-file mydb.pgproj.json 
--target-file mydb.pgpac


**Options:**
- `-sf` / `--source-file` - Source project file (.csproj or .pgproj.json)
- `-tf` / `--target-file` - Output package file (.pgpac or .json)
- `-v` / `--verbose` - Show detailed validation output

**Validation Checks:**
- ✅ Dependency resolution
- ✅ Circular reference detection
- ✅ SQL syntax validation
- ✅ Object existence verification

### `publish` - Deploy to Database

Deploy a compiled package or project to a target database.
```
pgpac publish 
--source-file mydb.pgpac 
--target-connection-string "Host=prod;Database=mydb;Username=postgres;Password=***"
```


**Options:**
- `-sf` / `--source-file` - Package or project file
- `-tcs` / `--target-connection-string` - Target database connection
- `--include-transaction` - Wrap deployment in transaction (default: true)
- `-v` / `--verbose` - Show deployment progress

### `script` - Generate Deployment SQL

Generate deployment SQL script without executing it.
```
pgpac script 
--source-file mydb.pgpac 
--target-connection-string "Host=prod;Database=mydb;Username=postgres;Password=***" 
--output-file deployment.sql
```
### `deploy-report` - Preview Changes

Generate a JSON report of what would be deployed without making changes.
```
pgpac deploy-report 
--source-file mydb.pgpac 
--target-connection-string "Host=prod;Database=mydb;Username=postgres;Password=***" 
--output-file report.json
```


## 🎯 Common Workflows

### Database Version Control Workflow
1. Extract development database
pgpac extract -scs "Host=dev;Database=mydb;..." -tf mydb/mydb.csproj
2. Commit to git
git add mydb/ git commit -m "Initial database schema"
3. Build and validate
pgpac compile -sf mydb/mydb.csproj -tf mydb.pgpac
4. Deploy to staging
pgpac publish -sf mydb.pgpac -tcs "Host=staging;Database=mydb;..."
5. Generate production deployment script
pgpac script -sf mydb.pgpac -tcs "Host=prod;Database=mydb;..." -o prod-deploy.sql

### CI/CD Pipeline Workflow
GitHub Actions example
•	name: Extract Schema run: pgpac extract -scs "$CONNECTION_STRING" -tf schema.pgproj.json
•	name: Validate Schema run: pgpac compile -sf schema.pgproj.json -tf schema.pgpac
•	name: Deploy to Test run: pgpac publish -sf schema.pgpac -tcs "$TEST_CONNECTION_STRING"

### Schema Comparison Workflow
Extract both databases
pgpac extract -scs "Host=db1;Database=mydb;..." -tf db1.pgproj.json pgpac extract -scs "Host=db2;Database=mydb;..." -tf db2.pgproj.json
Generate difference report
pgpac deploy-report -sf db1.pgproj.json -tcs "Host=db2;Database=mydb;..." -o differences.json


## ⚙️ Connection String Examples

**Basic:**
Host=localhost;Database=mydb;Username=postgres;Password=secret
**With SSL:**
Host=prod.example.com;Database=mydb;Username=app_user;Password=***;SSL Mode=Require
**With Port:**
Host=localhost;Port=5433;Database=mydb;Username=postgres;Password=***


## 🔍 Verbose Mode

Add `-v` or `--verbose` to any command for detailed output:
pgpac extract -scs "..." -tf mydb.csproj -v


**Output:**
🔍 Connecting to PostgreSQL 16.12... 
✅ Connected successfully 
📊 Extracting schemas... 
✓ public (15 tables, 7 views, 23 functions) 
✓ auth (3 tables, 2 views) 
📝 Generating project files... 
✓ mydb.csproj 
✓ public/Tables/users.sql ... 
✅ Extraction complete (145 files)


## 📚 Related Packages

- **[mbulava.PostgreSql.Dac](https://www.nuget.org/packages/mbulava.PostgreSql.Dac/)** - Core library for programmatic access
- **[MSBuild.Sdk.PostgreSql](https://www.nuget.org/packages/MSBuild.Sdk.PostgreSql/)** - MSBuild SDK for database projects

## 📖 Documentation

- [GitHub Repository](https://github.com/mbulava-org/pgPacTool)
- [CLI Reference](https://github.com/mbulava-org/pgPacTool/blob/main/docs/CLI_REFERENCE.md)
- [User Guide](https://github.com/mbulava-org/pgPacTool/blob/main/docs/USER_GUIDE.md)

## 🐛 Issues & Feedback

- [Report Issues](https://github.com/mbulava-org/pgPacTool/issues)
- [Discussions](https://github.com/mbulava-org/pgPacTool/discussions)

## 🔄 Update Tool
dotnet tool update --global postgresPacTools

## 🗑️ Uninstall
dotnet tool uninstall --global postgresPacTools


## 📄 License

MIT License - see [LICENSE](https://github.com/mbulava-org/pgPacTool/blob/main/LICENSE) for details.

---

**⚠️ Preview Release** - v1.0.0-preview1 is a preview release. Please provide feedback!

**Requirements:**
- .NET 10 SDK or later
- PostgreSQL 16 or 17