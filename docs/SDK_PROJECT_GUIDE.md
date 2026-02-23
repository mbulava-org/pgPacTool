# SDK-Style PostgreSQL Projects

pgPacTool supports SDK-style `.csproj` projects, similar to [MSBuild.Sdk.SqlProj](https://github.com/rr-wfm/MSBuild.Sdk.SqlProj) for SQL Server.

---

## 📋 Overview

With SDK-style projects, you can:
- ✅ Organize SQL files in a standard .NET project structure
- ✅ Version control your database schema alongside your application
- ✅ Use standard .NET build tools and CI/CD pipelines
- ✅ Compile and validate dependencies before deployment
- ✅ Generate deployment scripts from your SQL files

---

## 🚀 Quick Start

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

  <ItemGroup>
    <!-- Include all SQL files -->
    <Content Include="Tables\**\*.sql" />
    <Content Include="Views\**\*.sql" />
    <Content Include="Functions\**\*.sql" />
    <Content Include="Types\**\*.sql" />
    <Content Include="Sequences\**\*.sql" />
  </ItemGroup>

</Project>
```

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
postgresPacTools compile --source-file MyDatabase.csproj --verbose
```

Output:
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
   ⏱️  Time: 45ms

📋 Deployment order:
   1. public.users
   2. public.order_status
   3. public.orders
   4. public.active_orders
   5. public.calculate_order_total
```

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
└── Scripts/          # Misc scripts
    ├── Seed/        # Data seeding scripts
    └── Migrations/  # Manual migration scripts
```

### Auto-Discovery

If your .csproj doesn't explicitly include SQL files, pgPacTool will automatically scan these directories:
- `Tables/`
- `Views/`
- `Functions/`
- `Procedures/`
- `Types/`
- `Sequences/`
- `Schemas/`
- `Scripts/`
- `SQL/`

---

## 🔧 .csproj Configuration

### Basic Configuration

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <OutputType>Library</OutputType>
    <DatabaseName>MyDB</DatabaseName>
  </PropertyGroup>

  <ItemGroup>
    <Content Include="**\*.sql" />
  </ItemGroup>
</Project>
```

### Explicit File Inclusion

```xml
<ItemGroup>
  <Content Include="Tables\Users.sql" />
  <Content Include="Tables\Orders.sql" />
  <Content Include="Views\ActiveOrders.sql" />
</ItemGroup>
```

### Wildcard Patterns

```xml
<ItemGroup>
  <!-- Include all SQL files recursively -->
  <Content Include="**\*.sql" />
  
  <!-- Include only specific directories -->
  <Content Include="Tables\**\*.sql" />
  <Content Include="Views\**\*.sql" />
  
  <!-- Exclude certain files -->
  <Content Include="**\*.sql" Exclude="bin\**;obj\**;Tests\**" />
</ItemGroup>
```

### Using None Instead of Content

```xml
<ItemGroup>
  <!-- Using None for SQL files -->
  <None Include="Tables\**\*.sql" />
  <None Include="Views\**\*.sql" />
</ItemGroup>
```

---

## 🎯 Usage Examples

### Compile and Validate

```bash
# Basic compile
postgresPacTools compile -sf MyDatabase.csproj

# Verbose output with deployment order
postgresPacTools compile -sf MyDatabase.csproj --verbose
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
