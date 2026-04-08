# pgPacTool - MSBuild Integration

**Integration Model:** MSBuild SDK Extension (like MSBuild.Sdk.SqlProj)

---

## Project Type

**NO custom `.pgproj` file type!**

pgPacTool integrates into standard `.csproj` files using MSBuild SDK pattern.

---

## How It Works

### Standard .csproj with SDK

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <Sdk Name="MSBuild.Sdk.PostgreSql" Version="1.0.0-preview1" />

  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <PostgresVersion>16</PostgresVersion>
    <DatabaseName>MyDatabase</DatabaseName>
  </PropertyGroup>
</Project>
```

### Similar to MSBuild.Sdk.SqlProj

Just like [MSBuild.Sdk.SqlProj](https://github.com/rr-wfm/MSBuild.Sdk.SqlProj) does for SQL Server:
- ✅ Uses standard `.csproj` format
- ✅ Integrates with MSBuild
- ✅ Works with existing tools (VS, VS Code, CLI)
- ✅ No custom project system needed

---

## Integration Points

### 1. MSBuild SDK Package

```bash
dotnet add package MSBuild.Sdk.PostgreSql --version 1.0.0-preview1
```

### 2. Build Tasks

The SDK provides MSBuild tasks for:
- Extracting schemas
- Compiling projects
- Validating dependencies
- Generating deployment scripts

### 3. Build Process

```
dotnet build MyDatabase.csproj
  ↓
MSBuild invokes PgPacTool tasks
  ↓
Compile, validate, generate artifacts
  ↓
Output deployment package
```

---

## Benefits

### Standard Tooling
- ✅ Works with `dotnet build`
- ✅ Works with Visual Studio
- ✅ Works with VS Code
- ✅ Works with any MSBuild-based tool

### CI/CD Integration
- ✅ Standard `dotnet build` in pipelines
- ✅ No custom build infrastructure
- ✅ Standard package management

### Developer Experience
- ✅ Familiar `.csproj` format
- ✅ IntelliSense support
- ✅ Standard project references
- ✅ Works with existing workflows

---

## Project Structure

```
MyDatabase/
├── MyDatabase.csproj          ← Standard csproj with PgPacTool SDK
├── schemas/
│   ├── public/
│   │   ├── tables/
│   │   │   ├── users.sql
│   │   │   └── orders.sql
│   │   ├── views/
│   │   │   └── user_orders.sql
│   │   └── functions/
│   │       └── calculate_total.sql
│   └── auth/
│       └── tables/
│           └── sessions.sql
└── bin/
    └── Debug/
        └── net10.0/
            └── MyDatabase.pgpac    ← Compiled output
```

---

## Comparison

### ❌ What We're NOT Doing

```xml
<!-- NOT a custom project type -->
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <ProjectTypeGuids>{custom-guid}</ProjectTypeGuids>  ← NO
  </PropertyGroup>
</Project>
```

### ✅ What We ARE Doing

```xml
<!-- Standard csproj with our SDK -->
<Project Sdk="MSBuild.Sdk.PgPacTool/1.0.0">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <DatabaseName>MyDatabase</DatabaseName>
  </PropertyGroup>
</Project>
```

---

## MSBuild Tasks (Future)

### PgPacTool.Extract
Extracts schema from existing database

### PgPacTool.Compile
Compiles and validates project

### PgPacTool.Package
Generates deployment package

### PgPacTool.Deploy
Deploys to target database

---

## Current Status

**Milestone 2 Complete:**
- ✅ Core extraction (`PgProjectExtractor`)
- ✅ Core compilation (`ProjectCompiler`)
- ✅ Dependency analysis
- ✅ Cycle detection
- ✅ Deployment ordering

**Future (Milestone 3+):**
- ⬜ MSBuild SDK package
- ⬜ MSBuild tasks
- ⬜ .csproj templates
- ⬜ NuGet package

---

## Usage Today (Milestone 2)

### Via Code

```csharp
using mbulava.PostgreSql.Dac.Extract;
using mbulava.PostgreSql.Dac.Compile;

// Extract
var extractor = new PgProjectExtractor(connectionString);
var project = await extractor.ExtractPgProject("mydb");

// Compile
var compiler = new ProjectCompiler();
var result = compiler.Compile(project);

// Use results
if (result.IsSuccess)
{
    // Deploy in safe order
    foreach (var obj in result.DeploymentOrder)
    {
        Deploy(obj);
    }
}
```

### Via CLI (Future)

```bash
# Will work once MSBuild SDK is built
dotnet build MyDatabase.csproj
dotnet pack MyDatabase.csproj
dotnet publish MyDatabase.csproj
```

---

## Key Takeaways

1. **No `.pgproj` files** - Standard `.csproj` with our SDK
2. **MSBuild integration** - Like MSBuild.Sdk.SqlProj
3. **Standard tooling** - Works with existing build systems
4. **Current milestone** - Core library complete, SDK is future work

---

**Documentation Status:** Updated to reflect MSBuild SDK approach  
**Next Steps:** Build MSBuild SDK package (Milestone 3+)
