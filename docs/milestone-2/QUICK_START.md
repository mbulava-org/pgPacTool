# Milestone 2 - Quick Start Guide

**For developers starting work on Milestone 2**

---

## 🚀 Getting Started

### 1. Checkout Branch
```bash
git checkout feature/milestone-2-compilation-validation
git pull origin feature/milestone-2-compilation-validation
```

### 2. Review Documentation
- [ ] Read [Implementation Plan](IMPLEMENTATION_PLAN.md)
- [ ] Review [Progress Tracker](PROGRESS.md)
- [ ] Understand current [DependencyGraph](../../src/libs/mbulava.PostgreSql.Dac/Models/DbObjects.cs) implementation

### 3. Build and Test
```bash
# Restore and build
dotnet restore
dotnet build

# Run existing tests
dotnet test

# Verify everything passes
```

---

## 📋 First Tasks (Phase 1.1)

### Task: Enhance DependencyGraph

**File:** `src/libs/mbulava.PostgreSql.Dac/Models/DbObjects.cs`

**What exists:**
```csharp
public class DependencyGraph
{
    public void AddObject(string qualifiedName, string objectType);
    public void AddDependency(string from, string to);
    public List<string> TopologicalSort();
}
```

**What to add:**
```csharp
public class DependencyGraph
{
    // NEW: Get all dependencies of an object
    public List<string> GetDependencies(string objectName);
    
    // NEW: Get all objects that depend on this object (reverse)
    public List<string> GetDependents(string objectName);
    
    // NEW: Find all paths from one object to another
    public List<List<string>> GetAllPaths(string from, string to);
    
    // NEW: Check if path exists
    public bool HasPath(string from, string to);
    
    // NEW: Get object type
    public string GetObjectType(string objectName);
    
    // NEW: Get all objects
    public List<string> GetAllObjects();
}
```

---

## 🧪 TDD Workflow

### 1. Write Failing Test
```csharp
[Test]
public void GetDependencies_WithSimpleDependency_ReturnsList()
{
    // Arrange
    var graph = new DependencyGraph();
    graph.AddObject("public.users", "TABLE");
    graph.AddObject("public.orders", "TABLE");
    graph.AddDependency("public.orders", "public.users");
    
    // Act
    var deps = graph.GetDependencies("public.orders");
    
    // Assert
    Assert.That(deps, Has.Count.EqualTo(1));
    Assert.That(deps[0], Is.EqualTo("public.users"));
}
```

### 2. Run Test (Should Fail)
```bash
dotnet test --filter "GetDependencies_WithSimpleDependency_ReturnsList"
# Expected: Red (test fails)
```

### 3. Implement Feature
```csharp
public List<string> GetDependencies(string objectName)
{
    if (!_dependencies.ContainsKey(objectName))
        return new List<string>();
    
    return _dependencies[objectName].ToList();
}
```

### 4. Run Test (Should Pass)
```bash
dotnet test --filter "GetDependencies_WithSimpleDependency_ReturnsList"
# Expected: Green (test passes)
```

### 5. Refactor (If Needed)
- Clean up code
- Add documentation
- Optimize if necessary

---

## 📝 Test Database Schemas

### Simple Test Schema (For Unit Tests)
```sql
-- Create in memory or test database
CREATE SCHEMA test_schema;

CREATE TABLE test_schema.users (
    id SERIAL PRIMARY KEY,
    name VARCHAR(100)
);

CREATE TABLE test_schema.orders (
    id SERIAL PRIMARY KEY,
    user_id INTEGER REFERENCES test_schema.users(id)
);

CREATE VIEW test_schema.user_orders AS
SELECT u.name, o.id as order_id
FROM test_schema.users u
JOIN test_schema.orders o ON o.user_id = u.id;
```

### Complex Test Schema (For Integration Tests)
See [Test Schemas](../../tests/ProjectExtract-Tests/TestSchemas/) folder (to be created)

---

## 🔧 Development Tools

### Recommended Extensions
- **Visual Studio:** ReSharper or Rider
- **VS Code:** C# extensions
- **Git GUI:** GitKraken, SourceTree, or built-in

### Useful Commands
```bash
# Run specific test category
dotnet test --filter "Category=DependencyAnalysis"

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"

# Watch mode (auto-run tests)
dotnet watch test

# Build specific project
dotnet build src/libs/mbulava.PostgreSql.Dac/mbulava.PostgreSql.Dac.csproj
```

---

## 📚 Reference Materials

### Algorithms to Study
1. **Topological Sort:**
   - [Khan's Algorithm](https://en.wikipedia.org/wiki/Topological_sorting#Kahn's_algorithm)
   - [DFS-based approach](https://en.wikipedia.org/wiki/Topological_sorting#Depth-first_search)

2. **Cycle Detection:**
   - [DFS with colors](https://www.geeksforgeeks.org/detect-cycle-in-a-graph/)
   - [Tarjan's SCC](https://en.wikipedia.org/wiki/Tarjan%27s_strongly_connected_components_algorithm)

3. **Graph Traversal:**
   - [BFS](https://en.wikipedia.org/wiki/Breadth-first_search)
   - [DFS](https://en.wikipedia.org/wiki/Depth-first_search)

### PostgreSQL Dependencies
- [pg_depend catalog](https://www.postgresql.org/docs/current/catalog-pg-depend.html)
- [Object dependencies](https://www.postgresql.org/docs/current/ddl-depend.html)

---

## 🎯 Success Checklist

### Before Committing
- [ ] All new tests pass
- [ ] Existing tests still pass
- [ ] Code coverage maintained (90%+)
- [ ] Code reviewed (self-review at minimum)
- [ ] Documentation updated
- [ ] No compiler warnings

### Before PR
- [ ] Branch up to date with main
- [ ] All commits squashed/cleaned
- [ ] Descriptive commit messages
- [ ] PROGRESS.md updated
- [ ] Ready for code review

---

## 💡 Tips

### Graph Algorithm Tips
1. **Start simple** - Test with 3-5 nodes before scaling
2. **Visualize** - Draw graphs on paper
3. **Edge cases** - Test empty graph, single node, disconnected components
4. **Performance** - Use HashSet for O(1) lookups

### Testing Tips
1. **Red-Green-Refactor** - Stick to TDD cycle
2. **Descriptive names** - Test names should explain scenario
3. **One assert per test** - Keep tests focused
4. **AAA pattern** - Arrange, Act, Assert

### Code Quality Tips
1. **SOLID principles** - Single responsibility, etc.
2. **DRY** - Don't repeat yourself
3. **YAGNI** - You aren't gonna need it (yet)
4. **Document** - XML comments for public APIs

---

## 🆘 Getting Help

### Resources
1. **Implementation Plan:** Full details in [IMPLEMENTATION_PLAN.md](IMPLEMENTATION_PLAN.md)
2. **API Docs:** See [docs/API_REFERENCE.md](../API_REFERENCE.md)
3. **Existing Code:** Review Milestone 1 extraction code

### Questions?
- Check implementation plan first
- Review similar code in project
- Ask in team chat
- Open discussion in GitHub

---

## 📊 Progress Tracking

### Update PROGRESS.md
When completing a task:
```markdown
| 1.1: Enhance DependencyGraph | ✅ Complete | Your Name | Implemented all methods |
```

### Commit Message Format
```
feat(compile): add GetDependencies method to DependencyGraph

- Implemented GetDependencies method
- Added unit tests with 100% coverage
- Updated documentation

Addresses: Phase 1, Task 1.1
```

---

## 🎉 Ready to Start!

**Your first goal:** Complete Phase 1, Task 1.1

1. Write tests for `GetDependencies` method
2. Implement the method
3. Verify all tests pass
4. Update PROGRESS.md
5. Commit and push

**Estimated time:** 2-4 hours

**Good luck! 🚀**

---

**Last Updated:** 2026-01-31  
**Version:** 1.0  
**Next Review:** After Phase 1 completion
