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

Extracts database schema to a `.pgproj.json` file or SDK-style `.csproj` project.

#### Syntax
```bash
postgresPacTools extract [options]
```

#### Options

| Option | Alias | Required | Description |
|--------|-------|----------|-------------|
| `--source-connection-string` | `-scs` | ✅ | Source PostgreSQL database connection string |
| `--target-file` | `-tf` | ✅ | Path to output file (`.pgproj.json` or `.csproj`) |
| `--database-name` | `-dn` | ❌ | Database name (overrides connection string) |
| `--verbose` | `-v` | ❌ | Show detailed extraction progress (default: `false`) |

**Output Formats:**
- **`.pgproj.json`** - Single JSON file containing complete PgProject model
- **`.csproj`** - SDK-style project with folder structure (editable in Visual Studio)

The output format is automatically determined by the file extension of `--target-file`.

#### Examples

**Example 1: Extract to JSON file (traditional format)**
```bash
postgresPacTools extract \
  -scs "Host=localhost;Database=myapp;Username=postgres;Password=pass123" \
  -tf myapp.pgproj.json
```

**Example 2: Extract to SDK-style .csproj project**
```bash
postgresPacTools extract \
  -scs "Host=localhost;Database=dvdrental;Username=postgres;Password=pass123" \
  -tf output/dvdrental/dvdrental.csproj
```

**Example 3: Extract with verbose output**
```bash
postgresPacTools extract \
  -scs "Host=localhost;Database=pagila;Username=postgres;Password=pass123" \
  -tf output/pagila/pagila.csproj \
  --verbose
```

**Example 4: Extract with explicit database name**
```bash
postgresPacTools extract \
  -scs "Host=localhost;Username=postgres;Password=pass123" \
  -tf myapp.pgproj.json \
  -dn myapp
```

#### Output Example (JSON Format)
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

#### Output Example (SDK-Style .csproj)
```
╔════════════════════════════════════════════════════════════╗
║  PostgreSQL Schema Extraction                              ║
╚════════════════════════════════════════════════════════════╝

📋 Source: Host=localhost;Database=dvdrental;Username=postgres;Password=****
💾 Target: output/dvdrental/dvdrental.csproj

🔍 Extracting schema from database 'dvdrental'...
   🔍 Found schema: public (owner: pg_database_owner)
   ✅ Total schemas found: 1
✅ Extracted 1 schema(s)
   📁 public: 15 tables, 7 views, 9 functions, 24 types

📦 Generating SDK-style project...
✅ Generated SDK-style project in: output\dvdrental
   📁 Schemas: 1
   👤 Roles: 2
   📄 SQL files created
   📦 Project file: dvdrental.csproj

📊 Project structure:
   📁 Schemas: 1
   📄 Tables: 15
   📄 Views: 7
   📄 Functions: 9
   📄 Types: 24
   📄 Sequences: 13
   📄 Triggers: 15
   📄 Indexes: 32
   👤 Roles: 2
   🔐 Permission files: 1
   📝 Total SQL files: 107

💡 Open output/dvdrental/dvdrental.csproj in Visual Studio to edit!

✅ Extraction completed successfully!
```

#### Generated SDK-Style Project Structure

When extracting to `.csproj`, the following folder structure is created:

```
{DatabaseName}/
├── {DatabaseName}.csproj           # SDK-style project file
├── {schema}/                       # One folder per schema
│   ├── _schema.sql                 # CREATE SCHEMA statement
│   ├── _owners.sql                 # ALTER OWNER statements (if needed)
│   ├── Tables/
│   │   ├── users.sql
│   │   ├── orders.sql
│   │   └── products.sql
│   ├── Views/
│   │   ├── active_users.sql
│   │   └── order_summary.sql
│   ├── Functions/
│   │   ├── calculate_total.sql
│   │   └── get_user_stats.sql
│   ├── Types/
│   │   ├── order_status.sql       # ENUM types
│   │   └── address.sql            # COMPOSITE types
│   ├── Sequences/
│   │   └── user_id_seq.sql
│   ├── Indexes/
│   │   ├── idx_users_email.sql
│   │   └── idx_orders_date.sql
│   └── Triggers/
│       └── update_timestamp.sql
└── Security/                       # Security objects
    ├── Roles/
    │   ├── app_user.sql
    │   └── app_admin.sql
    └── Permissions/
        └── {schema}.sql            # GRANT statements per schema
```

**Key Features of SDK-Style Projects:**
- ✅ **Convention-based**: All `.sql` files are automatically included
- ✅ **Version control friendly**: Each object in its own file
- ✅ **Visual Studio integration**: Edit in familiar IDE
- ✅ **Compilable**: Use `compile` command to validate and generate `.pgpac`
- ✅ **Dependency ordering**: Automatically determined during compilation
- ✅ **Merge-friendly**: Reduces git conflicts

**Generated .csproj File:**
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <Sdk Name="MSBuild.Sdk.PostgreSql" Version="1.0.0-preview6" />
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <DatabaseName>dvdrental</DatabaseName>
    <PostgresVersion>16</PostgresVersion>
    <DefaultOwner>postgres</DefaultOwner>
    <DefaultTablespace>pg_default</DefaultTablespace>
  </PropertyGroup>

  <!-- All .sql files automatically included -->
  <!-- Pre/Post deployment scripts can be added here -->
  <ItemGroup>
    <!-- <PreDeploy Include="Scripts\PreDeployment\*.sql" /> -->
    <!-- <PostDeploy Include="Scripts\PostDeployment\*.sql" /> -->
  </ItemGroup>
</Project>
```

#### Real-World Examples

**Small Database (world_happiness):**
```bash
postgresPacTools extract \
  -scs "Host=localhost;Database=world_happiness;Username=postgres;Password=***" \
  -tf output/world_happiness/world_happiness.csproj

# Result: 9 SQL files (1 table, 1 type, 1 sequence, 1 index, 2 roles)
```

**Medium Database (dvdrental):**
```bash
postgresPacTools extract \
  -scs "Host=localhost;Database=dvdrental;Username=postgres;Password=***" \
  -tf output/dvdrental/dvdrental.csproj

# Result: 107 SQL files (15 tables, 7 views, 9 functions, 24 types, 32 indexes)
```

**Large Database (pagila):**
```bash
postgresPacTools extract \
  -scs "Host=localhost;Database=pagila;Username=postgres;Password=***" \
  -tf output/pagila/pagila.csproj --verbose

# Result: 145 SQL files (21 tables, 8 views, 9 functions, 33 types, 54 indexes)
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
| `--script-output` | `-so` | ❌ | Override the generated deployment script path |

#### Examples

```bash
# Basic publish
postgresPacTools publish \
  -sf myapp.pgpac \
  -tcs "Host=prod;Database=myapp;Username=deploy;Password=secret"

# Publish with variables
postgresPacTools publish \
  -sf myapp.pgpac \
  -tcs "Host=prod;Database=myapp;Username=deploy;Password=secret" \
  -v DatabaseName=prod_myapp \
  -v Environment=production

# Publish and drop extra objects
postgresPacTools publish \
  -sf myapp.pgpac \
  -tcs "Host=prod;Database=myapp;Username=deploy;Password=secret" \
  --drop-objects-not-in-source

# Non-transactional publish
postgresPacTools publish \
  -sf myapp.pgpac \
  -tcs "Host=prod;Database=myapp;Username=deploy;Password=secret" \
  --transactional false

# Publish with an explicit script output path
postgresPacTools publish \
  -sf myapp.pgpac \
  -tcs "Host=prod;Database=myapp;Username=deploy;Password=secret" \
  -so artifacts/deployment_prod_myapp.sql

Every publish writes the generated deployment SQL to disk. If `--script-output` is omitted, the script is written beside the source package using `deployment_{TargetDatabase}_{TimeStamp}.sql`.
```

#### Output
```
╔════════════════════════════════════════════════════════════╗
║  PostgreSQL Schema Publishing                              ║
╚════════════════════════════════════════════════════════════╝

📋 Source: myapp.pgpac
🎯 Target: Host=prod;Database=myapp;Username=deploy;Password=****
🔄 Transactional: True
🗑️  Drop extra objects: False
💾 Deployment script: .\deployment_myapp_20260325_053015.sql

📖 Loading source project...
✅ Loaded 2 schema(s)

🚀 Publishing changes...

💾 Deployment script saved: .\deployment_myapp_20260325_053015.sql

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
| `--source-file` | `-sf` | ✅ | Source `.pgproj.json` or `.csproj` file |
| `--output-path` | `-o` | ❌ | Output file path (default: `bin/Debug/net10.0/{DatabaseName}.pgpac`) |
| `--output-format` | `-of` | ❌ | Output format: `pgpac` (default) or `json` |
| `--verbose` | `-v` | ❌ | Show detailed compilation output (default: `false`) |
| `--skip-validation` |  | ❌ | Skip dependency validation and only load/generate project output |

**Note:** Output options only apply to `.csproj` projects. For `.pgproj.json` files, only validation is performed.

#### Examples

```bash
# Compile .pgproj.json (validation only)
postgresPacTools compile -sf myapp.pgproj.json

# Compile .csproj - generates .pgpac (default)
postgresPacTools compile -sf MyDatabase.csproj

# Generate JSON format instead
postgresPacTools compile -sf MyDatabase.csproj --output-format json

# Custom output location
postgresPacTools compile -sf MyDatabase.csproj -o ../artifacts/MyDB.pgpac

# Verbose output
postgresPacTools compile -sf MyDatabase.csproj --verbose

# Generate output without validation
postgresPacTools compile -sf MyDatabase.csproj --skip-validation
```

#### Output (.csproj → .pgpac)
```
╔════════════════════════════════════════════════════════════╗
║  PostgreSQL Project Compilation                            ║
╚════════════════════════════════════════════════════════════╝

📋 Source: MyDatabase.csproj

📖 Loading .csproj project (SDK-style)...
✅ Loaded 1 schema(s) from SDK project

📦 Generating output (DacPac)...
✅ Generated: bin/Debug/net10.0/MyDatabase.pgpac

⚙️  Compiling and validating...

✅ Compilation successful!
   📊 Objects: 15
   📦 Levels: 4
   ⏱️  Time: 85ms

📦 Output:
   💾 File: bin/Debug/net10.0/MyDatabase.pgpac
   📊 Size: 23,456 bytes
   📁 Format: .pgpac (ZIP archive)
   📄 Contains: content.json
```

#### Output (.pgproj.json)
```
╔════════════════════════════════════════════════════════════╗
║  PostgreSQL Project Compilation                            ║
╚════════════════════════════════════════════════════════════╝

📋 Source: myapp.pgproj.json

📖 Loading .pgproj.json project...
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

## 📦 SDK-Style Projects (.csproj)

postgresPacTools supports SDK-style `.csproj` projects, similar to [MSBuild.Sdk.SqlProj](https://github.com/rr-wfm/MSBuild.Sdk.SqlProj) for SQL Server.

### Benefits

- ✅ **Standard .NET Project Structure** - Use familiar .csproj format
- ✅ **File Organization** - Organize SQL files in folders (Tables/, Views/, etc.)
- ✅ **Version Control Friendly** - One object per file
- ✅ **CI/CD Integration** - Works with standard .NET build pipelines
- ✅ **Dependency Validation** - Compile validates all dependencies

### Quick Example

**MyDatabase.csproj:**
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <OutputType>Library</OutputType>
  </PropertyGroup>

  <ItemGroup>
    <Content Include="Tables\**\*.sql" />
    <Content Include="Views\**\*.sql" />
    <Content Include="Functions\**\*.sql" />
  </ItemGroup>
</Project>
```

**Tables/Users.sql:**
```sql
CREATE TABLE users (
    id SERIAL PRIMARY KEY,
    username VARCHAR(50) NOT NULL,
    email VARCHAR(255) NOT NULL
);
```

**Compile:**
```bash
postgresPacTools compile -sf MyDatabase.csproj --verbose
```

**Output:**
```
╔════════════════════════════════════════════════════════════╗
║  PostgreSQL Project Compilation                            ║
╚════════════════════════════════════════════════════════════╝

📋 Source: MyDatabase.csproj

📖 Loading .csproj project (SDK-style)...
✅ Loaded 1 schema(s) from SDK project

⚙️  Compiling and validating...

✅ Compilation successful!
   📊 Objects: 5
   📦 Levels: 3
```

### 📚 Full Documentation

See [SDK Project Guide](SDK_PROJECT_GUIDE.md) for complete documentation on:
- Project structure
- File organization
- Build configuration
- CI/CD integration
- Migration from .pgproj.json

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
