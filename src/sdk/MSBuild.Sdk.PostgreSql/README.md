# MSBuild.Sdk.PostgreSql

**MSBuild SDK for PostgreSQL Database Projects**

Build SQL Server-style database projects for PostgreSQL! This SDK enables you to organize your PostgreSQL schema as code in a `.csproj` file, and automatically compile it to a deployable `.pgpac` package during build.

## Features

✅ **Convention over Configuration** - Auto-discovers SQL files  
✅ **MSBuild Integration** - Works with `dotnet build` and Visual Studio  
✅ **Incremental Builds** - Only rebuilds when SQL files change  
✅ **Validation** - Checks dependencies and circular references  
✅ **Portable Packages** - Generates `.pgpac` files (PostgreSQL Data-tier Application Package)  
✅ **CI/CD Ready** - Perfect for DevOps pipelines  

## Quick Start

### 1. Create a Database Project

```xml
<Project Sdk="MSBuild.Sdk.PostgreSql/1.0.0">

  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <DatabaseName>MyDatabase</DatabaseName>
  </PropertyGroup>

</Project>
```

### 2. Organize SQL Files

```
MyDatabase/
├── MyDatabase.csproj
├── Tables/
│   ├── Users.sql
│   └── Orders.sql
├── Views/
│   └── ActiveOrders.sql
└── Functions/
    └── CalculateTotal.sql
```

### 3. Build

```bash
dotnet build
```

**Output:**
```
bin/Debug/net10.0/MyDatabase.pgpac
```

## Project Structure

### Automatic SQL Discovery

All `.sql` files are automatically included! Organize them however you want:

```
MyDatabase/
├── Tables/          ✅ Discovered
├── Views/           ✅ Discovered  
├── Functions/       ✅ Discovered
├── Types/           ✅ Discovered
├── Sequences/       ✅ Discovered
├── Triggers/        ✅ Discovered
└── YourFolder/      ✅ Discovered
```

### Pre/Post Deployment Scripts

Only these need explicit configuration:

```xml
<ItemGroup>
  <PreDeploy Include="Scripts\PreDeployment\*.sql" />
  <PostDeploy Include="Scripts\PostDeployment\*.sql" />
</ItemGroup>
```

## Configuration

### Properties

| Property | Default | Description |
|----------|---------|-------------|
| `DatabaseName` | Project name | Database name in .pgpac |
| `OutputFormat` | `pgpac` | Output format: `pgpac` or `json` |
| `ValidateOnBuild` | `true` | Validate SQL during build |
| `PgPacFileName` | `{DatabaseName}.pgpac` | Output file name |

### Example

```xml
<Project Sdk="MSBuild.Sdk.PostgreSql/1.0.0">

  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <DatabaseName>ProductionDB</DatabaseName>
    <OutputFormat>pgpac</OutputFormat>
    <ValidateOnBuild>true</ValidateOnBuild>
  </PropertyGroup>

  <!-- Optional: Pre/Post deployment -->
  <ItemGroup>
    <PreDeploy Include="Scripts\PreDeployment\BackupData.sql" />
    <PostDeploy Include="Scripts\PostDeployment\SeedData.sql" />
  </ItemGroup>

</Project>
```

## Build Integration

### dotnet CLI

```bash
# Build
dotnet build

# Clean
dotnet clean

# Rebuild
dotnet rebuild
```

### Visual Studio

- Open the `.csproj` in Visual Studio
- Press **Ctrl+Shift+B** to build
- Output appears in `bin\Debug\net10.0\`

### CI/CD

```yaml
# GitHub Actions
- name: Build Database
  run: dotnet build MyDatabase/MyDatabase.csproj
  
- name: Upload Package
  uses: actions/upload-artifact@v3
  with:
    name: database
    path: MyDatabase/bin/Debug/net10.0/*.pgpac
```

## What is .pgpac?

A `.pgpac` (PostgreSQL Data-tier Application Package) is:

- 📦 ZIP file containing your database schema
- 📄 Single `content.json` file inside
- 🚀 Deployable with `postgresPacTools publish`
- ✅ Version controllable artifact

## Deployment

After building, deploy with:

```bash
postgresPacTools publish \
  -sf bin/Debug/net10.0/MyDatabase.pgpac \
  -tcs "Host=server;Database=mydb;Username=user;Password=pass"
```

## Comparison with MSBuild.Sdk.SqlProj

If you're familiar with SQL Server database projects:

| Feature | SQL Server | PostgreSQL (this SDK) |
|---------|------------|----------------------|
| **SDK** | MSBuild.Sdk.SqlProj | MSBuild.Sdk.PostgreSql |
| **Output** | `.dacpac` | `.pgpac` |
| **SQL Discovery** | ✅ Auto | ✅ Auto |
| **MSBuild** | ✅ | ✅ |
| **Validation** | ✅ | ✅ |
| **Incremental** | ✅ | ✅ |

## Advanced Usage

### Custom Output Path

```xml
<PropertyGroup>
  <PgPacFilePath>$(MSBuildProjectDirectory)\dist\$(DatabaseName).pgpac</PgPacFilePath>
</PropertyGroup>
```

### JSON Output

```xml
<PropertyGroup>
  <OutputFormat>json</OutputFormat>
  <PgPacFileName>$(DatabaseName).pgproj.json</PgPacFileName>
</PropertyGroup>
```

### Disable Validation

```xml
<PropertyGroup>
  <ValidateOnBuild>false</ValidateOnBuild>
</PropertyGroup>
```

## Requirements

- .NET 10 SDK or later
- PostgreSQL database for deployment

## Links

- **GitHub**: https://github.com/mbulava-org/pgPacTool
- **NuGet**: https://www.nuget.org/packages/MSBuild.Sdk.PostgreSql
- **Documentation**: https://github.com/mbulava-org/pgPacTool/tree/main/docs

## License

MIT License - see LICENSE file

## Contributing

Contributions welcome! See CONTRIBUTING.md

---

**Build PostgreSQL databases like a pro! 🐘🚀**
