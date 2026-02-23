# pgPacTool Compilation - Quick Reference

**Version:** v0.2.0 | **Status:** Production Ready ✅

---

## 🚀 Quick Start

### Basic Usage
```csharp
using mbulava.PostgreSql.Dac.Extract;
using mbulava.PostgreSql.Dac.Compile;

// Extract
var extractor = new PgProjectExtractor(connectionString);
var project = await extractor.ExtractPgProject("mydb");

// Compile
var compiler = new ProjectCompiler();
var result = compiler.Compile(project);

// Check
if (result.IsSuccess)
    Console.WriteLine($"✅ Ready to deploy {result.DeploymentOrder.Count} objects");
else
    Console.WriteLine($"❌ {result.Errors.Count} errors found");
```

---

## 📋 Common Operations

### Check for Issues
```csharp
if (result.HasCircularDependencies)
{
    foreach (var cycle in result.CircularDependencies)
    {
        Console.WriteLine($"{cycle.Severity}: {cycle.Description}");
        Console.WriteLine($"Fix: {cycle.Suggestion}");
    }
}
```

### Get Deployment Order
```csharp
foreach (var obj in result.DeploymentOrder)
{
    Console.WriteLine(obj); // Deploy in this order
}
```

### Parallel Deployment
```csharp
for (int i = 0; i < result.DeploymentLevels.Count; i++)
{
    Console.WriteLine($"Level {i + 1}:");
    foreach (var obj in result.DeploymentLevels[i])
    {
        Console.WriteLine($"  {obj}"); // Can deploy in parallel
    }
}
```

---

## 🎯 Key Features

| Feature | Method | Description |
|---------|--------|-------------|
| **Validate** | `compiler.CanCompile(project)` | Quick check |
| **Full Analysis** | `compiler.Compile(project)` | Complete compilation |
| **Get Order** | `result.DeploymentOrder` | Safe sequence |
| **Check Cycles** | `result.CircularDependencies` | All cycles |
| **Get Summary** | `result.GetSummary()` | Status message |

---

## 🔍 Error Codes

| Code | Meaning | Action |
|------|---------|--------|
| **CYCLE001** | Error-level cycle | Must fix before deploy |
| **CYCLE002** | Warning-level cycle | Review suggestion |
| **CYCLE003** | Info-level cycle | Allowed (self-ref) |
| **SORT001** | Cannot sort | Fix cycles first |
| **COMP001** | Compilation error | Check message |

---

## 💡 Best Practices

### DO
✅ Always check `result.IsSuccess`  
✅ Review warnings before deploying  
✅ Use `DeploymentOrder` for safe deployment  
✅ Log `CompilationTime` for monitoring

### DON'T
❌ Deploy if `HasCircularDependencies` (Error level)  
❌ Ignore warnings  
❌ Skip compilation before deployment  
❌ Deploy out of order

---

## 🎨 Result Properties

```csharp
result.IsSuccess              // bool - overall status
result.Errors                 // List<CompilerError>
result.Warnings               // List<CompilerWarning>
result.CircularDependencies   // List<CircularDependency>
result.DependencyGraph        // DependencyGraph
result.DeploymentOrder        // List<string> - sequential
result.DeploymentLevels       // List<List<string>> - parallel
result.CompilationTime        // TimeSpan
result.GetSummary()           // string - human-readable
```

---

## 🔧 Advanced Usage

### Custom Workflow
```csharp
// 1. Extract
var project = await extractor.ExtractPgProject("mydb");

// 2. Compile
var result = compiler.Compile(project);

// 3. Handle cycles
if (result.HasCircularDependencies)
{
    var errors = result.CircularDependencies
        .Where(c => c.Severity == CycleSeverity.Error);
    
    if (errors.Any())
    {
        // Block deployment
        return;
    }
}

// 4. Deploy
await DeployInOrder(result.DeploymentOrder);
```

### Performance Monitoring
```csharp
var result = compiler.Compile(project);
Console.WriteLine($"Compiled {result.DeploymentOrder.Count} objects in {result.CompilationTime.TotalMilliseconds}ms");
```

---

## 📊 Cycle Severity Levels

| Level | Meaning | Example | Action |
|-------|---------|---------|--------|
| **Info** | Allowed | Recursive function | OK to deploy |
| **Warning** | Review | Self-ref FK | Review before deploy |
| **Error** | Blocked | View cycle | Must fix |

---

## 🎯 Typical Workflow

```
1. Extract database schema
2. Compile project
3. Check for errors
4. Review warnings
5. Get deployment order
6. Deploy objects in order
7. Verify deployment
```

---

## 📚 More Information

- **Full Docs**: [MILESTONE_2_COMPLETE.md](MILESTONE_2_COMPLETE.md)
- **API Docs**: [../API_REFERENCE.md](../API_REFERENCE.md)
- **Examples**: [../USER_GUIDE.md](../USER_GUIDE.md)
- **Main README**: [../../README.md](../../README.md)

---

## 🆘 Troubleshooting

### "Compilation failed"
➜ Check `result.Errors` for specific issues

### "Circular dependency"
➜ Review `result.CircularDependencies` for paths and suggestions

### "Cannot sort"
➜ Fix error-level circular dependencies first

### "Slow compilation"
➜ Check `result.CompilationTime` - expected < 1s for 100 objects

---

## ✅ Production Checklist

Before deploying:
- [ ] `result.IsSuccess == true`
- [ ] No error-level cycles
- [ ] Warnings reviewed
- [ ] Deployment order obtained
- [ ] Backup created
- [ ] Test in staging first

---

**Quick Reference v0.2.0** | **Production Ready** ✅
