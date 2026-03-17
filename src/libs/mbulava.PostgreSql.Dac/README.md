# mbulava.PostgreSql.Dac

**PostgreSQL Data-Tier Application Library** - Build database-as-code with .NET

[![NuGet](https://img.shields.io/nuget/v/mbulava.PostgreSql.Dac.svg)](https://www.nuget.org/packages/mbulava.PostgreSql.Dac/)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](https://opensource.org/licenses/MIT)

Core library for PostgreSQL database projects. Provides schema extraction, comparison, validation, and deployment capabilities with dependency tracking and circular reference detection.

## ✨ Features

- 📤 **Schema Extraction** - Export database schemas with full metadata
- 🔍 **Schema Comparison** - Identify differences between databases
- ✅ **Dependency Analysis** - Automatic dependency graph building
- 🔄 **Deployment Scripts** - Generate safe migration SQL
- 📦 **DACPAC Format** - `.pgpac` package format for PostgreSQL
- 🎯 **Multi-Version** - Supports PostgreSQL 16 and 17

## 🚀 Quick Start

### Installation
dotnet add package mbulava.PostgreSql.Dac

### Extract Schema from Database
```
using mbulava.PostgreSql.Dac.Extract;
var connectionString = "Host=localhost;Database=mydb;Username=postgres;Password=***"; 
var extractor = new PgProjectExtractor(connectionString);
// Extract complete schema var project = await extractor.ExtractPgProject("mydb");
Console.WriteLine($"Extracted {project.Schemas.Count} schemas"); 
Console.WriteLine($"  Tables: {project.Schemas.Sum(s => s.Tables.Count)}"); 
Console.WriteLine($"  Views: {project.Schemas.Sum(s => s.Views.Count)}"); 
Console.WriteLine($"  Functions: {project.Schemas.Sum(s => s.Functions.Count)}");
```

### Compile and Validate Project
```
using mbulava.PostgreSql.Dac.Compile;
var compiler = new ProjectCompiler(); 
var result = compiler.Compile(project);
if (result.Errors.Count == 0) { 
    Console.WriteLine($"✅ Validation successful!"); 
    Console.WriteLine($"Deployment order: {result.DeploymentOrder.Count} objects");
    foreach (var obj in result.DeploymentOrder.Take(5))
    {
        Console.WriteLine($"  - {obj}");
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

### Generate SDK-Style Project
```
using mbulava.PostgreSql.Dac.Compile;
// Generate .csproj with individual SQL files 
var generator = new CsprojProjectGenerator("output/mydb/mydb.csproj"); 
await generator.GenerateProjectAsync(project);
// Result: 
// output/mydb/ 
//   ├── mydb.csproj 
//   ├── public/ 
//   │   ├── Tables/ 
//   │   │   └── users.sql 
//   │   ├── Views/ 
//   │   └── Functions/ 
//   └── Security/
```

### Compare Schemas
```
using mbulava.PostgreSql.Dac.Compare;
var sourceProject = await extractor1.ExtractPgProject("source_db"); 
var targetProject = await extractor2.ExtractPgProject("target_db");
var comparer = new SchemaComparer(); 
var differences = comparer.Compare(sourceProject, targetProject);
Console.WriteLine($"Differences found: {differences.Count}"); 
foreach (var diff in differences) 
{ 
    Console.WriteLine($"  {diff.ChangeType}: {diff.ObjectType} {diff.ObjectName}"); 
}

```

### Generate Migration Script

```
using mbulava.PostgreSql.Dac.Compare;
var generator = new PublishScriptGenerator(); 
var script = generator.GenerateScript(sourceProject, targetProject);
// Output SQL migration script File.WriteAllText("migration.sql", script);
```

## 📦 Supported Database Objects

- ✅ Tables (with columns, constraints, indexes)
- ✅ Views (regular and materialized)
- ✅ Functions (all languages)
- ✅ Stored Procedures
- ✅ Types (ENUM, composite, domains)
- ✅ Sequences
- ✅ Triggers
- ✅ Schemas
- ✅ Roles and Permissions
- ✅ Extensions

## 🔧 Advanced Features

### Circular Dependency Detection

```
var result = compiler.Compile(project);
if (result.CircularDependencies.Count > 0) 
{ 
    Console.WriteLine("⚠️ Circular dependencies detected:"); 
    foreach (var cycle in result.CircularDependencies) 
    { 
        Console.WriteLine($"  {string.Join(" → ", cycle)} → {cycle[0]}"); 
    } 
}
```


### Pre/Post Deployment Scripts
```
project.PreDeploymentScript = @" -- Run before schema changes PRINT 'Starting deployment...'; ";
project.PostDeploymentScript = @" -- Run after schema changes REFRESH MATERIALIZED VIEW my_view; ";
```

### SQLCMD Variable Substitution

```
var variables = new Dictionary<string, string> { ["DatabaseName"] = "mydb", ["Environment"] = "production" };
var script = generator.GenerateScript(source, target, variables); // $(DatabaseName) and $(Environment) will be replaced
```

## 📚 Related Packages

- **[postgresPacTools](https://www.nuget.org/packages/postgresPacTools/)** - CLI tool (`pgpac` command)
- **[MSBuild.Sdk.PostgreSql](https://www.nuget.org/packages/MSBuild.Sdk.PostgreSql/)** - MSBuild SDK for database projects

## 📖 Documentation

- [GitHub Repository](https://github.com/mbulava-org/pgPacTool)
- [User Guide](https://github.com/mbulava-org/pgPacTool/blob/main/docs/USER_GUIDE.md)
- [API Reference](https://github.com/mbulava-org/pgPacTool/blob/main/docs/API_REFERENCE.md)

## 🐛 Issues & Feedback

- [Report Issues](https://github.com/mbulava-org/pgPacTool/issues)
- [Discussions](https://github.com/mbulava-org/pgPacTool/discussions)

## 📄 License

MIT License - see [LICENSE](https://github.com/mbulava-org/pgPacTool/blob/main/LICENSE) for details.

---

**⚠️ Preview Release** - v1.0.0-preview1 is a preview release. Not recommended for production use yet. Please provide feedback!