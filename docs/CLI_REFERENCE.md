# postgresPacTools CLI Reference

Command-line interface for PostgreSQL Data-Tier Application operations. Inspired by [SqlPackage](https://learn.microsoft.com/en-us/sql/tools/sqlpackage/cli-reference).

---

## 📋 Overview

**postgresPacTools** provides a complete CLI for:
- 📤 **Extracting** database schemas
- 🚀 **Publishing** schema changes
- 📝 **Generating** deployment scripts
- ✅ **Compiling** and validating projects
- 📊 **Reporting** deployment changes

---

## 🚀 Quick Start

### Installation

```bash
# Build the CLI
dotnet build src/postgresPacTools/postgresPacTools.csproj

# Run directly
dotnet run --project src/postgresPacTools/postgresPacTools.csproj -- <command> [options]

# Or publish and add to PATH
dotnet publish src/postgresPacTools/postgresPacTools.csproj -c Release -o ./tools
export PATH=$PATH:$(pwd)/tools
postgresPacTools --help
```

### Basic Usage

```bash
# Extract schema
postgresPacTools extract \
  --source-connection-string "Host=localhost;Database=mydb;Username=postgres;Password=secret" \
  --target-file mydb.pgproj.json

# Publish changes
postgresPacTools publish \
  --source-file mydb.pgproj.json \
  --target-connection-string "Host=prod;Database=mydb;Username=postgres;Password=secret"

# Generate script without executing
postgresPacTools script \
  --source-file mydb.pgproj.json \
  --target-connection-string "Host=prod;Database=mydb;Username=postgres;Password=secret" \
  --output-file deploy.sql
```

---

## 📖 Commands

### `extract`

Extracts database schema to a `.pgproj.json` file.

#### Syntax
```bash
postgresPacTools extract [options]
```

#### Options

| Option | Alias | Required | Description |
|--------|-------|----------|-------------|
| `--source-connection-string` | `-scs` | ✅ | Source PostgreSQL database connection string |
| `--target-file` | `-tf` | ✅ | Path to output `.pgproj.json` file |
| `--database-name` | `-dn` | ❌ | Database name (overrides connection string) |

#### Examples

```bash
# Extract from local database
postgresPacTools extract \
  -scs "Host=localhost;Database=myapp;Username=postgres;Password=pass123" \
  -tf myapp.pgproj.json

# Extract with explicit database name
postgresPacTools extract \
  -scs "Host=localhost;Username=postgres;Password=pass123" \
  -tf myapp.pgproj.json \
  -dn myapp
```

#### Output
```
╔════════════════════════════════════════════════════════════╗
║  PostgreSQL Schema Extraction                              ║
╚════════════════════════════════════════════════════════════╝

📋 Source: Host=localhost;Database=myapp;Username=postgres;Password=****
💾 Target: myapp.pgproj.json

🔍 Extracting schema from database 'myapp'...
✅ Extracted 2 schema(s)
   📁 public: 15 tables, 3 views, 8 functions, 2 types
   📁 auth: 5 tables, 1 views, 2 functions, 0 types

💾 Saving to myapp.pgproj.json...

✅ Extraction completed successfully!
```

---

### `publish`

Publishes schema changes to a target database.

#### Syntax
```bash
postgresPacTools publish [options]
```

#### Options

| Option | Alias | Required | Description |
|--------|-------|----------|-------------|
| `--source-file` | `-sf` | ✅ | Source `.pgproj.json` file |
| `--target-connection-string` | `-tcs` | ✅ | Target PostgreSQL database connection string |
| `--variables` | `-v` | ❌ | SQLCMD variables in format `Name=Value` (can specify multiple) |
| `--drop-objects-not-in-source` | `-dons` | ❌ | Drop objects in target that don't exist in source (default: `false`) |
| `--transactional` | | ❌ | Execute deployment in a transaction (default: `true`) |

#### Examples

```bash
# Basic publish
postgresPacTools publish \
  -sf myapp.pgproj.json \
  -tcs "Host=prod;Database=myapp;Username=deploy;Password=secret"

# Publish with variables
postgresPacTools publish \
  -sf myapp.pgproj.json \
  -tcs "Host=prod;Database=myapp;Username=deploy;Password=secret" \
  -v DatabaseName=prod_myapp \
  -v Environment=production

# Publish and drop extra objects
postgresPacTools publish \
  -sf myapp.pgproj.json \
  -tcs "Host=prod;Database=myapp;Username=deploy;Password=secret" \
  --drop-objects-not-in-source

# Non-transactional publish
postgresPacTools publish \
  -sf myapp.pgproj.json \
  -tcs "Host=prod;Database=myapp;Username=deploy;Password=secret" \
  --transactional false
```

#### Output
```
╔════════════════════════════════════════════════════════════╗
║  PostgreSQL Schema Publishing                              ║
╚════════════════════════════════════════════════════════════╝

📋 Source: myapp.pgproj.json
🎯 Target: Host=prod;Database=myapp;Username=deploy;Password=****
🔄 Transactional: True
🗑️  Drop extra objects: False

📖 Loading source project...
✅ Loaded 2 schema(s)

🚀 Publishing changes...

✅ Deployment successful!
   📊 Created: 3
   🔄 Altered: 12
   🗑️  Dropped: 0
   ⏱️  Time: 2.45s
```

---

### `script`

Generates a SQL deployment script without executing it.

#### Syntax
```bash
postgresPacTools script [options]
```

#### Options

| Option | Alias | Required | Description |
|--------|-------|----------|-------------|
| `--source-file` | `-sf` | ✅ | Source `.pgproj.json` file |
| `--target-connection-string` | `-tcs` | ✅ | Target PostgreSQL database connection string |
| `--output-file` | `-of` | ✅ | Path to output SQL script file |
| `--variables` | `-v` | ❌ | SQLCMD variables in format `Name=Value` |
| `--drop-objects-not-in-source` | `-dons` | ❌ | Drop objects in target that don't exist in source (default: `false`) |

#### Examples

```bash
# Generate deployment script
postgresPacTools script \
  -sf myapp.pgproj.json \
  -tcs "Host=prod;Database=myapp;Username=postgres;Password=secret" \
  -of deploy.sql

# Generate with variables
postgresPacTools script \
  -sf myapp.pgproj.json \
  -tcs "Host=prod;Database=myapp;Username=postgres;Password=secret" \
  -of deploy.sql \
  -v DatabaseName=prod_myapp \
  -v TableSpace=pg_default
```

#### Output
```
╔════════════════════════════════════════════════════════════╗
║  PostgreSQL Script Generation                              ║
╚════════════════════════════════════════════════════════════╝

📋 Source: myapp.pgproj.json
🎯 Target: Host=prod;Database=myapp;Username=postgres;Password=****
💾 Output: deploy.sql

📖 Loading source project...
⚙️  Generating deployment script...

✅ Script generated successfully!
   💾 File: deploy.sql
   📊 Changes: 3 created, 12 altered, 0 dropped
```

---

### `compile`

Compiles and validates a project, checking dependencies and circular references.

#### Syntax
```bash
postgresPacTools compile [options]
```

#### Options

| Option | Alias | Required | Description |
|--------|-------|----------|-------------|
| `--source-file` | `-sf` | ✅ | Source `.pgproj.json` file |
| `--verbose` | `-v` | ❌ | Show detailed compilation output (default: `false`) |

#### Examples

```bash
# Basic compile
postgresPacTools compile -sf myapp.pgproj.json

# Verbose compile with deployment order
postgresPacTools compile -sf myapp.pgproj.json --verbose
```

#### Output
```
╔════════════════════════════════════════════════════════════╗
║  PostgreSQL Project Compilation                            ║
╚════════════════════════════════════════════════════════════╝

📋 Source: myapp.pgproj.json

📖 Loading project...
✅ Loaded 2 schema(s)

⚙️  Compiling and validating...

✅ Compilation successful!
   📊 Objects: 45
   📦 Levels: 8
   ⏱️  Time: 127ms

📋 Deployment order:
   1. public.user_type
   2. public.status_enum
   3. public.users
   4. public.orders
   ... and 41 more
```

---

### `deploy-report`

Generates a report of deployment changes that would be made.

#### Syntax
```bash
postgresPacTools deploy-report [options]
```

#### Options

| Option | Alias | Required | Description |
|--------|-------|----------|-------------|
| `--source-file` | `-sf` | ✅ | Source `.pgproj.json` file |
| `--target-connection-string` | `-tcs` | ✅ | Target PostgreSQL database connection string |
| `--output-file` | `-of` | ✅ | Path to output report file (JSON) |

#### Examples

```bash
# Generate deployment report
postgresPacTools deploy-report \
  -sf myapp.pgproj.json \
  -tcs "Host=prod;Database=myapp;Username=postgres;Password=secret" \
  -of report.json
```

#### Output
```
╔════════════════════════════════════════════════════════════╗
║  PostgreSQL Deployment Report                              ║
╚════════════════════════════════════════════════════════════╝

📋 Source: myapp.pgproj.json
🎯 Target: Host=prod;Database=myapp;Username=postgres;Password=****
💾 Output: report.json

🔍 Analyzing target database...
⚙️  Comparing schemas...

✅ Report generated successfully!
   💾 File: report.json
   📊 Schemas analyzed: 2
```

#### Report Format

The report is generated as JSON:

```json
{
  "GeneratedAt": "2025-01-24T10:30:00.000Z",
  "SourceFile": "myapp.pgproj.json",
  "TargetConnection": "Host=prod;Database=myapp;Username=postgres;Password=****",
  "TotalChanges": 2,
  "Changes": [
    {
      "SchemaName": "public",
      "Tables": 3,
      "Views": 1,
      "Functions": 2,
      "Triggers": 0,
      "Types": 1,
      "Sequences": 0
    }
  ]
}
```

---

## 🔧 SQLCMD Variables

Use variables to parameterize your deployments.

### Syntax

Variables are specified using the `--variables` (or `-v`) option:

```bash
--variables Name1=Value1 Name2=Value2
```

### In Scripts

Variables use SqlPackage-compatible syntax: `$(VariableName)`

```sql
CREATE DATABASE $(DatabaseName);
CREATE SCHEMA $(SchemaName);
GRANT ALL ON DATABASE $(DatabaseName) TO $(AppUser);
```

### Example

```bash
postgresPacTools publish \
  -sf myapp.pgproj.json \
  -tcs "Host=prod;Database=myapp;Username=postgres;Password=secret" \
  -v DatabaseName=production_db \
  -v SchemaName=app \
  -v AppUser=app_service
```

---

## 🎯 CI/CD Integration

### Azure DevOps Pipeline

```yaml
steps:
  - task: UseDotNet@2
    inputs:
      version: '10.0.x'

  - script: |
      postgresPacTools publish \
        -sf $(Build.SourcesDirectory)/database/myapp.pgproj.json \
        -tcs "$(TargetConnectionString)" \
        -v Environment=$(Environment) \
        -v BuildNumber=$(Build.BuildNumber)
    displayName: 'Deploy Database Changes'
```

### GitHub Actions

```yaml
- name: Deploy Database
  run: |
    postgresPacTools publish \
      --source-file database/myapp.pgproj.json \
      --target-connection-string "${{ secrets.DB_CONNECTION_STRING }}" \
      --variables Environment=production BuildNumber=${{ github.run_number }}
```

### GitLab CI

```yaml
deploy-database:
  script:
    - postgresPacTools publish \
        -sf database/myapp.pgproj.json \
        -tcs "$TARGET_CONNECTION" \
        -v Environment=$CI_ENVIRONMENT_NAME \
        -v BuildNumber=$CI_PIPELINE_ID
```

---

## 📊 Exit Codes

| Code | Meaning |
|------|---------|
| `0` | Success |
| `1` | Error (compilation failed, deployment failed, etc.) |

---

## 💡 Best Practices

### 1. **Version Control Your Schema**
```bash
# Extract schema after every change
postgresPacTools extract \
  -scs "Host=localhost;Database=mydev;Username=postgres;Password=dev" \
  -tf schema/myapp.pgproj.json

# Commit to git
git add schema/myapp.pgproj.json
git commit -m "Update database schema"
```

### 2. **Always Test Scripts First**
```bash
# Generate script first
postgresPacTools script -sf myapp.pgproj.json -tcs "$TARGET" -of deploy.sql

# Review the script
cat deploy.sql

# Then publish
postgresPacTools publish -sf myapp.pgproj.json -tcs "$TARGET"
```

### 3. **Use Variables for Environment-Specific Values**
```bash
# Development
postgresPacTools publish -sf app.pgproj.json -tcs "$DEV_DB" -v Env=dev -v LogLevel=debug

# Production
postgresPacTools publish -sf app.pgproj.json -tcs "$PROD_DB" -v Env=prod -v LogLevel=warn
```

### 4. **Compile Before Publishing**
```bash
# Validate first
postgresPacTools compile -sf myapp.pgproj.json --verbose

# If successful, publish
postgresPacTools publish -sf myapp.pgproj.json -tcs "$TARGET"
```

---

## 🔗 Comparison with SqlPackage

| Feature | SqlPackage | postgresPacTools |
|---------|-----------|------------------|
| **Extract** | ✅ .dacpac | ✅ .pgproj.json |
| **Publish** | ✅ | ✅ |
| **Script** | ✅ | ✅ |
| **DeployReport** | ✅ XML | ✅ JSON |
| **Compile** | ❌ | ✅ (Unique feature!) |
| **Variables** | ✅ SQLCMD | ✅ SQLCMD-compatible |
| **Pre/Post Scripts** | ✅ | ✅ |
| **Transaction Control** | ✅ | ✅ |

---

## 📚 See Also

- [API Reference](API_REFERENCE.md)
- [User Guide](USER_GUIDE.md)
- [Milestone 3 Documentation](milestone-3/)
- [SqlPackage Documentation](https://learn.microsoft.com/en-us/sql/tools/sqlpackage/cli-reference)
