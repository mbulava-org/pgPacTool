# SDK-Style PostgreSQL Projects

pgPacTool supports SDK-style `.csproj` projects, similar to [MSBuild.Sdk.SqlProj](https://github.com/rr-wfm/MSBuild.Sdk.SqlProj) for SQL Server.

---

## 📋 Overview

With SDK-style projects, you can:
- ✅ **Extract existing databases** to SDK-style projects automatically
- ✅ Organize SQL files in a standard .NET project structure
- ✅ Version control your database schema alongside your application
- ✅ Use standard .NET build tools and CI/CD pipelines
- ✅ Compile and validate dependencies before deployment
- ✅ Generate deployment scripts from your SQL files

---

## 🎯 Two Ways to Create Projects

### Option 1: Extract from Existing Database (Recommended)

**Instantly convert any PostgreSQL database to an SDK-style project:**

```bash
# Using CLI (easiest)
postgresPacTools extract \
  --source-connection-string "Host=localhost;Database=mydb;Username=postgres;Password=***" \
  --target-file output/mydb/mydb.csproj
```

Or programmatically:

```csharp
using mbulava.PostgreSql.Dac.Extract;

var connectionString = "Host=localhost;Database=mydb;Username=postgres;Password=secret";
var extractor = new PgProjectExtractor(connectionString);

// Extract database schema
var project = await extractor.ExtractPgProject("mydb");

// Generate SDK-style project
var generator = new CsprojProjectGenerator("output/mydb/mydb.csproj");
await generator.GenerateProjectAsync(project);
```

**What gets extracted:**
- ✅ All tables with columns, constraints, indexes
- ✅ All views with their definitions
- ✅ All functions and procedures
- ✅ All custom types (ENUMs, COMPOSITEs, DOMAINs)
- ✅ All sequences
- ✅ All triggers
- ✅ All roles and permissions
- ✅ Proper folder structure by object type
- ✅ Ready-to-compile .csproj file

**Real Examples:**

```bash
# Simple database → 9 SQL files
postgresPacTools extract \
  -scs "Host=localhost;Database=world_happiness;Username=postgres;Password=***" \
  -tf output/world_happiness/world_happiness.csproj

# Medium complexity → 107 SQL files
postgresPacTools extract \
  -scs "Host=localhost;Database=dvdrental;Username=postgres;Password=***" \
  -tf output/dvdrental/dvdrental.csproj

# Large database → 145 SQL files
postgresPacTools extract \
  -scs "Host=localhost;Database=pagila;Username=postgres;Password=***" \
  -tf output/pagila/pagila.csproj --verbose
```

**Generated Folder Structure:**

```
mydb/
├── mydb.csproj                    # SDK-style project file
├── public/                        # Schema folder
│   ├── _schema.sql                # CREATE SCHEMA statement
│   ├── _owners.sql                # Ownership statements (if needed)
│   ├── Tables/
│   │   ├── users.sql
│   │   └── orders.sql
│   ├── Views/
│   │   └── active_orders.sql
│   ├── Functions/
│   │   └── calculate_total.sql
│   ├── Types/
│   │   └── order_status.sql
│   ├── Sequences/
│   │   └── user_id_seq.sql
│   ├── Indexes/
│   │   └── idx_users_email.sql
│   └── Triggers/
│       └── update_timestamp.sql
└── Security/                      # Security objects
    ├── Roles/
    │   └── app_user.sql
    └── Permissions/
        └── public.sql
```

**Benefits:**
- 🚀 Instant migration to version control
- 📝 Each object in its own editable file
- ✅ Validated and ready to compile
- 🔄 Perfect for starting database DevOps
- 👥 Team-friendly structure

---

### Option 2: Create from Scratch (Manual)

For new databases or when you want full control, you can create the project structure manually.

---

## 🚀 Quick Start (Manual Creation)

### 1. Create Project Structure

```
MyDatabase/
├── MyDatabase.csproj
├── Tables/
│   ├── Users.sql
│   ├── Orders.sql
│   └── OrderItems.sql
├── Views/
│   └── ActiveOrders.sql
├── Functions/
│   └── CalculateTotal.sql
├── Types/
│   └── OrderStatus.sql
└── Sequences/
    └── order_id_seq.sql
```

### 2. Create .csproj File

**Simple and Clean - Convention Over Configuration!**

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <OutputType>Library</OutputType>
    <IsPackable>false</IsPackable>

    <!-- PostgreSQL Project Settings -->
    <DatabaseName>MyPostgresDB</DatabaseName>
    <DefaultSchema>public</DefaultSchema>
  </PropertyGroup>

  <!-- 
    That's it! All .sql files are automatically included.
    Just organize them in folders and they'll be discovered.

    Only Pre/Post deployment scripts need explicit configuration:
  -->
  <ItemGroup>
    <PreDeploy Include="Scripts\PreDeployment\BackupData.sql" />
    <PostDeploy Include="Scripts\PostDeployment\SeedData.sql" />
  </ItemGroup>

</Project>
```

**Key Point:** 🎯 **All `.sql` files in your project directory are automatically included!** No need for `<Content Include="**\*.sql" />` declarations.

### 3. Create SQL Files

**Tables/Users.sql:**
```sql
CREATE TABLE users (
    id SERIAL PRIMARY KEY,
    username VARCHAR(50) NOT NULL UNIQUE,
    email VARCHAR(255) NOT NULL,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);
```

**Views/ActiveOrders.sql:**
```sql
CREATE VIEW active_orders AS
SELECT 
    o.id,
    u.username,
    o.order_date,
    o.total
FROM orders o
JOIN users u ON o.user_id = u.id
WHERE o.status = 'active';
```

**Functions/CalculateTotal.sql:**
```sql
CREATE FUNCTION calculate_order_total(order_id INT)
RETURNS DECIMAL(10,2)
LANGUAGE SQL
AS $$
    SELECT SUM(quantity * price) 
    FROM order_items 
    WHERE order_id = $1;
$$;
```

### 4. Compile the Project

```bash
# Default: Generates .pgpac file (PostgreSQL Data-tier Application Package)
postgresPacTools compile --source-file MyDatabase.csproj

# With verbose output
postgresPacTools compile --source-file MyDatabase.csproj --verbose

# Generate JSON instead of .pgpac
postgresPacTools compile --source-file MyDatabase.csproj --output-format json

# Specify custom output path
postgresPacTools compile --source-file MyDatabase.csproj --output-path ../artifacts/MyDB.pgpac
```

Output (.pgpac format - default):
```
╔════════════════════════════════════════════════════════════╗
║  PostgreSQL Project Compilation                            ║
╚════════════════════════════════════════════════════════════╝

📋 Source: MyDatabase.csproj

📖 Loading .csproj project (SDK-style)...
✅ Loaded 1 schema(s) from SDK project

📦 Generating output (DacPac)...
✅ Generated: bin/Debug/net10.0/MyPostgresDB.pgpac

⚙️  Compiling and validating...

✅ Compilation successful!
   📊 Objects: 5
   📦 Levels: 3
   ⏱️  Time: 45ms

📋 Deployment order:
   1. public.users
   2. public.order_status
   3. public.orders
   4. public.active_orders
   5. public.calculate_order_total

📦 Output:
   💾 File: bin/Debug/net10.0/MyPostgresDB.pgpac
   📊 Size: 12,345 bytes
   📁 Format: .pgpac (ZIP archive)
   📄 Contains: content.json
```

**What is .pgpac?**

A `.pgpac` (PostgreSQL Data-tier Application Package) is:
- 📦 A **ZIP file** containing your database schema
- 📄 Single `content.json` file with serialized `PgProject`
- 🔒 **Portable** - distribute one file instead of many SQL files
- ✅ **Easy deployment** - same format as SQL Server's `.dacpac`
- 🚀 **Ready for CI/CD** - build once, deploy anywhere

---

## 📂 Project Organization

### Recommended Directory Structure

```
MyDatabase.csproj
├── Tables/           # Table definitions
│   ├── Users.sql
│   ├── Orders.sql
│   └── Products.sql
├── Views/            # View definitions
│   └── CustomerOrders.sql
├── Functions/        # Function definitions
│   └── GetUserStats.sql
├── Procedures/       # Stored procedure definitions (if using)
│   └── UpdateInventory.sql
├── Types/            # Custom type definitions
│   ├── OrderStatus.sql (ENUM)
│   └── Address.sql (COMPOSITE)
├── Sequences/        # Sequence definitions
│   └── order_id_seq.sql
├── Triggers/         # Trigger definitions
│   └── update_timestamp.sql
└── Scripts/          # Pre/Post deployment scripts
    ├── PreDeployment/  # Run before schema changes
    │   └── BackupData.sql
    └── PostDeployment/ # Run after schema changes
        └── SeedData.sql
```

### 🎯 Auto-Discovery (Convention Over Configuration)

**All `.sql` files are automatically discovered recursively!**

pgPacTool scans your entire project directory for `.sql` files, automatically excluding:
- `bin/` and `obj/` directories
- `.vs/` directory
- Hidden directories (starting with `.`)
- Files explicitly marked as `<PreDeploy>` or `<PostDeploy>`

**No explicit file includes needed!** Just organize your SQL files logically and they'll be found.

---

## 🔧 .csproj Configuration

### Minimal Configuration (Recommended)

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <OutputType>Library</OutputType>
    <DatabaseName>MyDB</DatabaseName>
  </PropertyGroup>

  <!-- All .sql files are automatically included! -->
  <!-- Only specify Pre/Post deployment scripts: -->
  <ItemGroup>
    <PreDeploy Include="Scripts\PreDeployment\*.sql" />
    <PostDeploy Include="Scripts\PostDeployment\*.sql" />
  </ItemGroup>
</Project>
```

### With Pre/Post Deployment Scripts

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <OutputType>Library</OutputType>
  </PropertyGroup>

  <!-- Specify deployment scripts -->
  <ItemGroup>
    <PreDeploy Include="Scripts\PreDeployment\BackupTables.sql" />
    <PreDeploy Include="Scripts\PreDeployment\DisableTriggers.sql" />
    <PostDeploy Include="Scripts\PostDeployment\SeedReferenceData.sql" />
    <PostDeploy Include="Scripts\PostDeployment\EnableTriggers.sql" />
  </ItemGroup>
</Project>
```

---

## 📦 Output Formats

### .pgpac (PostgreSQL Data-tier Application Package) - Default

The `.pgpac` format is the default output for compiled projects:

**Structure:**
```
MyDatabase.pgpac (ZIP file)
└── content.json (serialized PgProject)
```

**Advantages:**
- ✅ **Single file distribution** - entire schema in one package
- ✅ **Portable** - easy to version, share, and deploy
- ✅ **Compressed** - ZIP compression reduces file size
- ✅ **SQL Server compatible** - similar to `.dacpac` format
- ✅ **CI/CD friendly** - build artifact you can deploy anywhere

**Usage:**
```bash
# Generate .pgpac (default)
postgresPacTools compile -sf MyDatabase.csproj

# Deploy the .pgpac
postgresPacTools publish -sf MyDatabase.pgpac -tcs "Host=prod;Database=mydb;..."
```

### .pgproj.json - Alternative Format

Plain JSON format for human readability:

```bash
# Generate .pgproj.json
postgresPacTools compile -sf MyDatabase.csproj --output-format json

# Output: bin/Debug/net10.0/MyDatabase.pgproj.json
```

**Use cases:**
- 🔍 Inspecting schema structure
- 📝 Version control diffing
- 🔧 Debugging and troubleshooting

---

## 🎯 Usage Examples

### Compile and Validate

```bash
# Basic compile (generates .pgpac)
postgresPacTools compile -sf MyDatabase.csproj

# Verbose output with deployment order
postgresPacTools compile -sf MyDatabase.csproj --verbose

# Generate JSON format instead
postgresPacTools compile -sf MyDatabase.csproj --output-format json

# Custom output location
postgresPacTools compile -sf MyDatabase.csproj -o ../artifacts/MyDB.pgpac
```

### Generate Deployment Script

```bash
# You can also use .csproj with other commands (coming soon)
postgresPacTools script \
  -sf MyDatabase.csproj \
  -tcs "Host=prod;Database=mydb;Username=postgres" \
  -of deploy.sql
```

---

## 🆚 Comparison with MSBuild.Sdk.SqlProj

| Feature | MSBuild.Sdk.SqlProj (SQL Server) | pgPacTool (PostgreSQL) |
|---------|----------------------------------|------------------------|
| **Project Type** | .sqlproj / .csproj | .csproj |
| **SDK Style** | ✅ | ✅ |
| **SQL File Organization** | ✅ | ✅ |
| **Dependency Analysis** | ✅ | ✅ |
| **Circular Reference Detection** | ✅ | ✅ |
| **Output Format** | .dacpac | .pgproj.json |
| **Deployment** | SqlPackage | postgresPacTools |
| **Pre/Post Scripts** | ✅ | ✅ |
| **SQLCMD Variables** | ✅ | ✅ |

---

## 💡 Best Practices

### 1. **One Object Per File**
```
✅ Good:
   Tables/Users.sql
   Tables/Orders.sql
   
❌ Bad:
   Tables/AllTables.sql (multiple tables)
```

### 2. **Use Descriptive File Names**
```
✅ Good:
   Functions/CalculateOrderTotal.sql
   Views/ActiveUserOrders.sql
   
❌ Bad:
   Functions/Func1.sql
   Views/View1.sql
```

### 3. **Organize by Object Type**
```
✅ Good:
   Tables/
   Views/
   Functions/
   
❌ Bad:
   All SQL files in root directory
```

### 4. **Include CREATE Statements**
Each SQL file should contain the full CREATE statement:

```sql
-- Users.sql
CREATE TABLE users (
    id SERIAL PRIMARY KEY,
    username VARCHAR(50) NOT NULL
);
```

### 5. **Use OR REPLACE for Functions/Views**
```sql
-- Safe for recompilation
CREATE OR REPLACE VIEW active_users AS
SELECT * FROM users WHERE active = true;

CREATE OR REPLACE FUNCTION get_user(user_id INT)
RETURNS users
LANGUAGE SQL
AS $$
    SELECT * FROM users WHERE id = user_id;
$$;
```

---

## 🔄 Migration from .pgproj.json

If you have an existing `.pgproj.json` file, you can migrate to a `.csproj`:

### 1. Extract SQL to Files

```bash
# Create directory structure
mkdir -p MyDatabase/{Tables,Views,Functions,Types}

# Extract each object to its own file
# (Manual process or write a script)
```

### 2. Create .csproj

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <OutputType>Library</OutputType>
  </PropertyGroup>
  <ItemGroup>
    <Content Include="**\*.sql" />
  </ItemGroup>
</Project>
```

### 3. Test Compilation

```bash
postgresPacTools compile -sf MyDatabase.csproj --verbose
```

---

## 🚧 Limitations

### Current Limitations

1. **Single Schema**: Currently defaults to `public` schema
2. **No Multi-Database**: One project = one database
3. **Simple Parsing**: Basic regex-based SQL parsing
4. **No DACPAC Output**: Generates `.pgproj.json` internally

### Coming Soon

- [ ] Multi-schema support
- [ ] Advanced SQL parsing with AST
- [ ] Custom build targets
- [ ] MSBuild integration
- [ ] NuGet package for SDK

---

## 🛠️ CI/CD Integration

### GitHub Actions

```yaml
name: Database CI

on: [push, pull_request]

jobs:
  compile:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3
      
      - name: Setup .NET
        uses: actions/setup-dotnet@v3
        with:
          dotnet-version: '10.0.x'
      
      - name: Install postgresPacTools
        run: dotnet tool install -g postgresPacTools
      
      - name: Compile Database Project
        run: postgresPacTools compile -sf MyDatabase.csproj --verbose
```

### Azure DevOps

```yaml
steps:
  - task: UseDotNet@2
    inputs:
      version: '10.0.x'
  
  - script: |
      dotnet tool install -g postgresPacTools
      postgresPacTools compile -sf $(Build.SourcesDirectory)/MyDatabase.csproj --verbose
    displayName: 'Compile Database'
```

---

## 📚 See Also

- [CLI Reference](CLI_REFERENCE.md)
- [User Guide](USER_GUIDE.md)
- [MSBuild.Sdk.SqlProj Documentation](https://github.com/rr-wfm/MSBuild.Sdk.SqlProj)
