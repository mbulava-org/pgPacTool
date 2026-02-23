# Milestone 2: Compilation & Validation - Implementation Plan

**Branch:** `feature/milestone-2-compilation-validation`  
**Version Target:** v0.2.0  
**Status:** Planning Phase  
**Created:** 2026-01-31

---

## 🎯 Milestone Goals

Build a compilation and validation system that:
1. **Validates dependencies** between database objects
2. **Detects circular dependencies** before deployment
3. **Generates build artifacts** for deployment
4. **Provides compiler errors** with detailed context
5. **Enables safe refactoring** with dependency tracking

---

## 📋 Feature Overview

### Core Features

| Feature | Description | Priority | Complexity |
|---------|-------------|----------|------------|
| **Dependency Graph** | Build complete object dependency graph | P0 | High |
| **Circular Detection** | Detect and report circular dependencies | P0 | Medium |
| **Topological Sort** | Order objects for safe deployment | P0 | Medium |
| **Compiler Validation** | Validate references and dependencies | P1 | High |
| **Error Reporting** | Clear, actionable error messages | P1 | Medium |
| **Build Artifacts** | Generate deployment scripts | P2 | High |

---

## 🏗️ Architecture

### Components to Build

```
mbulava.PostgreSql.Dac.Compile/
├── DependencyAnalyzer.cs       # Extract dependencies from objects
├── DependencyGraph.cs          # Graph structure and algorithms
├── CircularDependencyDetector.cs  # Detect cycles
├── TopologicalSorter.cs        # Sort for deployment order
├── ProjectCompiler.cs          # Main compilation orchestrator
├── CompilerError.cs            # Error model (exists)
├── CompilerResult.cs           # Result model (exists)
└── Validators/
    ├── ReferenceValidator.cs   # Validate object references
    ├── TypeValidator.cs        # Validate type usage
    ├── PrivilegeValidator.cs   # Validate permissions
    └── SchemaValidator.cs      # Validate schema consistency
```

### Data Flow

```
PgProject (Extracted Schema)
    ↓
DependencyAnalyzer
    ↓
DependencyGraph (Built)
    ↓
CircularDependencyDetector ──→ Errors (if cycles found)
    ↓
TopologicalSorter
    ↓
Validators (Reference, Type, Privilege, Schema)
    ↓
CompilerResult (Success + Order OR Errors)
```

---

## 📝 Detailed Tasks

### Phase 1: Dependency Analysis (Week 1-2)

#### Task 1.1: Enhance DependencyGraph
**File:** `src/libs/mbulava.PostgreSql.Dac/Models/DbObjects.cs` (existing)

**What to implement:**
- ✅ Basic structure exists, enhance it
- Add `GetDependencies(string objectName)` method
- Add `GetDependents(string objectName)` method (reverse)
- Add `GetAllPaths(string from, string to)` method
- Add `HasPath(string from, string to)` method

**Tests:**
- Create dependency graph with 10+ objects
- Test path finding algorithms
- Test reverse dependency lookup

#### Task 1.2: Build DependencyAnalyzer
**File:** `src/libs/mbulava.PostgreSql.Dac/Compile/DependencyAnalyzer.cs` (new)

**What to implement:**
```csharp
public class DependencyAnalyzer
{
    public DependencyGraph AnalyzeProject(PgProject project);
    public List<PgDependency> ExtractTableDependencies(PgTable table);
    public List<PgDependency> ExtractViewDependencies(PgView view);
    public List<PgDependency> ExtractFunctionDependencies(PgFunction function);
    public List<PgDependency> ExtractTriggerDependencies(PgTrigger trigger);
}
```

**Dependency Rules:**
- **Tables:**
  - Foreign keys → Referenced tables
  - Inherited tables → Parent tables
  - Sequences (via DEFAULT nextval) → Sequences
- **Views:**
  - SELECT from → Tables/Views
  - Functions in expressions → Functions
- **Functions:**
  - Return type → Types
  - Parameter types → Types
  - SQL body references → Tables/Views/Functions
- **Triggers:**
  - Trigger on → Table
  - Executes → Function
  - References → Tables (in function body)

**Tests:**
- Extract dependencies from sample schema
- Verify all dependency types detected
- Test with complex nested dependencies

#### Task 1.3: Extract Dependencies from AST
**File:** `src/libs/mbulava.PostgreSql.Dac/Compile/AstDependencyExtractor.cs` (new)

**What to implement:**
- Parse AST to find object references
- Handle qualified names (schema.object)
- Handle unqualified names (search path)
- Extract from WHERE clauses, JOIN conditions
- Extract from function bodies

**Tests:**
- Parse CREATE VIEW with multiple table references
- Parse CREATE FUNCTION with type references
- Handle schema-qualified vs unqualified names

---

### Phase 2: Circular Dependency Detection (Week 2-3)

#### Task 2.1: Build CircularDependencyDetector
**File:** `src/libs/mbulava.PostgreSql.Dac/Compile/CircularDependencyDetector.cs` (new)

**What to implement:**
```csharp
public class CircularDependencyDetector
{
    public List<CircularDependency> DetectCycles(DependencyGraph graph);
    public bool HasCycles(DependencyGraph graph);
    public List<List<string>> FindAllCycles(DependencyGraph graph);
}

public class CircularDependency
{
    public List<string> Cycle { get; set; }  // A → B → C → A
    public DependencyType Type { get; set; }
    public string Description { get; set; }
}
```

**Algorithm:**
- Use Tarjan's strongly connected components (SCC)
- Or use DFS with cycle detection
- Report all cycles, not just first one
- Provide human-readable cycle paths

**Tests:**
- Detect simple cycle (A → B → A)
- Detect complex cycle (A → B → C → D → B)
- Detect multiple independent cycles
- No false positives on DAG

#### Task 2.2: Special Case Handling
**What to handle:**

1. **Self-referencing allowed:**
   - Recursive functions (allowed in PostgreSQL)
   - Tables with self-referential FKs (allowed)

2. **Mutually recursive:**
   - Two functions calling each other (need forward declarations)
   - Two views referencing each other (not allowed)

3. **Breaking cycles:**
   - Suggest removing/altering dependencies
   - Suggest using forward declarations

**Tests:**
- Allow recursive functions
- Allow self-referential FKs
- Reject circular view dependencies
- Suggest fixes for cycles

---

### Phase 3: Topological Sorting (Week 3-4)

#### Task 3.1: Build TopologicalSorter
**File:** `src/libs/mbulava.PostgreSql.Dac/Compile/TopologicalSorter.cs` (new)

**What to implement:**
```csharp
public class TopologicalSorter
{
    public List<string> Sort(DependencyGraph graph);
    public List<List<string>> SortInLevels(DependencyGraph graph);
    public bool CanSort(DependencyGraph graph);
}
```

**Features:**
- Kahn's algorithm or DFS-based topological sort
- Return objects in dependency order (dependencies first)
- Optionally group by "levels" (objects that can be created in parallel)

**Tests:**
- Sort simple DAG
- Sort complex schema (50+ objects)
- Handle disconnected components
- Verify dependencies always come first

#### Task 3.2: Deployment Order
**File:** `src/libs/mbulava.PostgreSql.Dac/Compile/DeploymentOrderer.cs` (new)

**What to implement:**
- Group objects by type (schemas, types, tables, views, functions, triggers)
- Within each type, order by dependencies
- Handle cross-type dependencies

**Object Creation Order:**
1. Schemas
2. Types (domains, enums, composites)
3. Sequences
4. Tables (ordered by FK dependencies)
5. Views (ordered by view dependencies)
6. Functions/Procedures
7. Triggers
8. Constraints (deferred)
9. Indexes

**Tests:**
- Verify correct order for sample schema
- Test with cross-type dependencies
- Handle complex FK graphs

---

### Phase 4: Validation (Week 4-5)

#### Task 4.1: ReferenceValidator
**File:** `src/libs/mbulava.PostgreSql.Dac/Compile/Validators/ReferenceValidator.cs` (new)

**What to validate:**
- All referenced objects exist in project
- Referenced columns exist in tables
- Referenced types exist
- Referenced functions have correct signatures

**Errors to detect:**
- Missing table reference
- Missing column reference
- Missing type reference
- Function signature mismatch

**Tests:**
- Detect missing table in FK
- Detect missing column in FK
- Detect missing type in function parameter
- Allow optional system objects

#### Task 4.2: TypeValidator
**File:** `src/libs/mbulava.PostgreSql.Dac/Compile/Validators/TypeValidator.cs` (new)

**What to validate:**
- Type compatibility in assignments
- Domain constraints
- Enum value validity
- Composite type structure

**Tests:**
- Detect type mismatch in DEFAULT
- Validate domain constraints
- Validate enum values

#### Task 4.3: PrivilegeValidator
**File:** `src/libs/mbulava.PostgreSql.Dac/Compile/Validators/PrivilegeValidator.cs` (new)

**What to validate:**
- All granted roles exist
- Grantee exists
- Privilege type is valid for object type
- Circular role membership

**Tests:**
- Detect missing grantee
- Detect invalid privilege type
- Detect circular role membership

#### Task 4.4: SchemaValidator
**File:** `src/libs/mbulava.PostgreSql.Dac/Compile/Validators/SchemaValidator.cs` (new)

**What to validate:**
- No duplicate object names in same schema
- Schema names are valid
- Object names follow conventions
- Reserved names not used

**Tests:**
- Detect duplicate table names
- Detect invalid schema name
- Allow case-sensitive duplicates (if configured)

---

### Phase 5: Compiler Integration (Week 5-6)

#### Task 5.1: Enhance ProjectCompiler
**File:** `src/libs/mbulava.PostgreSql.Dac/Compile/ProjectCompiler.cs` (exists)

**Current implementation:**
```csharp
public class ProjectCompiler
{
    public CompilerResult Compile(PgProject project);
}
```

**Enhance to:**
```csharp
public class ProjectCompiler
{
    private readonly DependencyAnalyzer _analyzer;
    private readonly CircularDependencyDetector _cycleDetector;
    private readonly TopologicalSorter _sorter;
    private readonly List<IValidator> _validators;
    
    public CompilerResult Compile(PgProject project)
    {
        var result = new CompilerResult();
        
        // 1. Build dependency graph
        var graph = _analyzer.AnalyzeProject(project);
        
        // 2. Detect circular dependencies
        var cycles = _cycleDetector.DetectCycles(graph);
        if (cycles.Any())
        {
            result.AddErrors(cycles);
            return result; // Stop if cycles found
        }
        
        // 3. Topological sort
        var order = _sorter.Sort(graph);
        result.DeploymentOrder = order;
        
        // 4. Run validators
        foreach (var validator in _validators)
        {
            var errors = validator.Validate(project, graph);
            result.AddErrors(errors);
        }
        
        // 5. Generate build artifacts (if no errors)
        if (result.IsSuccess)
        {
            result.BuildArtifacts = GenerateArtifacts(project, order);
        }
        
        return result;
    }
}
```

#### Task 5.2: Enhance CompilerResult
**File:** `src/libs/mbulava.PostgreSql.Dac/Compile/CompilerResult.cs` (exists)

**Add properties:**
```csharp
public class CompilerResult
{
    public bool IsSuccess { get; set; }
    public List<CompilerError> Errors { get; set; }
    public List<CompilerWarning> Warnings { get; set; }
    
    // NEW:
    public List<string> DeploymentOrder { get; set; }
    public DependencyGraph DependencyGraph { get; set; }
    public List<CircularDependency> CircularDependencies { get; set; }
    public Dictionary<string, BuildArtifact> BuildArtifacts { get; set; }
    public TimeSpan CompilationTime { get; set; }
}
```

#### Task 5.3: Build Artifacts
**What to generate:**
- Deployment SQL script (in dependency order)
- Rollback SQL script
- Dependency report (text/JSON)
- Validation report

**Format:**
```sql
-- Generated by pgPacTool v0.2.0
-- Deployment Order: Based on dependency analysis
-- Total Objects: 45

-- Phase 1: Schemas
CREATE SCHEMA IF NOT EXISTS public;

-- Phase 2: Types
CREATE TYPE public.status_enum AS ENUM ('active', 'inactive');

-- Phase 3: Tables
CREATE TABLE public.users (...);
CREATE TABLE public.orders (...);  -- Depends on users

-- etc.
```

---

## 🧪 Testing Strategy

### Unit Tests

**DependencyAnalyzer Tests:**
```csharp
[TestFixture]
public class DependencyAnalyzerTests
{
    [Test]
    public void AnalyzeProject_WithTableFK_DetectsDependency()
    {
        // Arrange
        var project = CreateProjectWithFK();
        var analyzer = new DependencyAnalyzer();
        
        // Act
        var graph = analyzer.AnalyzeProject(project);
        
        // Assert
        Assert.That(graph.HasDependency("orders", "users"), Is.True);
    }
    
    [Test]
    public void ExtractTableDependencies_WithMultipleFK_ReturnsAllDependencies()
    {
        // Test with table having 3 FKs
    }
    
    [Test]
    public void ExtractViewDependencies_WithJoins_DetectsAllTables()
    {
        // Test view with 5 joined tables
    }
}
```

**CircularDependencyDetector Tests:**
```csharp
[TestFixture]
public class CircularDependencyDetectorTests
{
    [Test]
    public void DetectCycles_WithSimpleCycle_ReturnsCycle()
    {
        // A → B → A
    }
    
    [Test]
    public void DetectCycles_WithComplexCycle_ReturnsAllCycles()
    {
        // Multiple cycles in same graph
    }
    
    [Test]
    public void DetectCycles_WithDAG_ReturnsNoCycles()
    {
        // Acyclic graph
    }
    
    [Test]
    public void DetectCycles_AllowsRecursiveFunctions()
    {
        // Function calling itself (allowed)
    }
}
```

**TopologicalSorter Tests:**
```csharp
[TestFixture]
public class TopologicalSorterTests
{
    [Test]
    public void Sort_WithLinearDependencies_ReturnsCorrectOrder()
    {
        // A → B → C → D
    }
    
    [Test]
    public void Sort_WithDiamondDependency_ReturnsValidOrder()
    {
        //     A
        //    / \
        //   B   C
        //    \ /
        //     D
    }
    
    [Test]
    public void SortInLevels_GroupsIndependentObjects()
    {
        // Objects with no dependencies can be in same level
    }
}
```

### Integration Tests

**End-to-End Compilation:**
```csharp
[TestFixture]
[Category("Integration")]
public class CompilerIntegrationTests
{
    [Test]
    public async Task Compile_ComplexSchema_Succeeds()
    {
        // Arrange: Extract real database
        var extractor = new PgProjectExtractor(connectionString);
        var project = await extractor.ExtractPgProject("testdb");
        
        // Act: Compile
        var compiler = new ProjectCompiler();
        var result = compiler.Compile(project);
        
        // Assert
        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Errors, Is.Empty);
        Assert.That(result.DeploymentOrder, Is.Not.Empty);
    }
    
    [Test]
    public async Task Compile_WithCircularDependency_ReturnsError()
    {
        // Create schema with circular view dependencies
        // Compile should fail with clear error
    }
}
```

### Test Database Schemas

**Create test schemas:**

1. **Simple Schema** (10 objects):
   - 2 tables with FK
   - 1 view
   - 1 function
   - 1 trigger

2. **Complex Schema** (50+ objects):
   - 10 tables with multiple FKs
   - 5 views (some referencing other views)
   - 10 functions
   - 3 triggers
   - 5 types

3. **Circular Schema** (intentionally broken):
   - View A references View B
   - View B references View A
   - Should fail compilation

4. **Diamond Schema**:
   - Table A
   - Table B depends on A
   - Table C depends on A
   - Table D depends on B and C

---

## 📊 Success Criteria

### Functional Requirements

- [ ] Can build dependency graph from any extracted project
- [ ] Detects all circular dependencies
- [ ] Provides topological sort for deployment
- [ ] Validates all object references
- [ ] Generates deployment SQL in correct order
- [ ] Reports clear, actionable errors

### Performance Requirements

- [ ] Analyze 1000 objects in < 5 seconds
- [ ] Detect cycles in graph with 1000 nodes in < 2 seconds
- [ ] Topological sort 1000 objects in < 1 second

### Quality Requirements

- [ ] 90%+ code coverage on compiler components
- [ ] All edge cases tested (cycles, diamonds, disconnected graphs)
- [ ] Clear error messages with context
- [ ] Documentation for all public APIs

---

## 📅 Timeline

### Week 1: Dependency Analysis
- Days 1-2: Enhance DependencyGraph
- Days 3-4: Build DependencyAnalyzer
- Day 5: AST dependency extraction

### Week 2: Circular Detection
- Days 1-2: CircularDependencyDetector
- Days 3-4: Special case handling
- Day 5: Testing and refinement

### Week 3: Topological Sorting
- Days 1-2: TopologicalSorter
- Days 3-4: DeploymentOrderer
- Day 5: Level grouping

### Week 4: Validation
- Day 1: ReferenceValidator
- Day 2: TypeValidator
- Day 3: PrivilegeValidator
- Day 4: SchemaValidator
- Day 5: Integration

### Week 5: Compiler Integration
- Days 1-2: Enhance ProjectCompiler
- Days 3-4: Build artifacts generation
- Day 5: Error reporting

### Week 6: Testing & Documentation
- Days 1-3: Comprehensive testing
- Days 4-5: Documentation updates

---

## 🎓 Learning Resources

### Algorithms
- **Topological Sort**: Kahn's algorithm, DFS-based
- **Cycle Detection**: DFS with colors, Tarjan's SCC
- **Graph Theory**: DAG properties, strongly connected components

### PostgreSQL
- Dependency system (`pg_depend`)
- Object creation order
- Forward declarations
- Recursive functions

---

## 🚀 Quick Start (After Implementation)

```csharp
// Extract schema
var extractor = new PgProjectExtractor(connectionString);
var project = await extractor.ExtractPgProject("mydb");

// Compile and validate
var compiler = new ProjectCompiler();
var result = compiler.Compile(project);

if (result.IsSuccess)
{
    Console.WriteLine($"✅ Compilation successful!");
    Console.WriteLine($"Deployment order ({result.DeploymentOrder.Count} objects):");
    foreach (var obj in result.DeploymentOrder)
    {
        Console.WriteLine($"  - {obj}");
    }
    
    // Generate deployment script
    var sql = result.BuildArtifacts["deployment.sql"];
    await File.WriteAllTextAsync("deploy.sql", sql);
}
else
{
    Console.WriteLine($"❌ Compilation failed with {result.Errors.Count} errors:");
    foreach (var error in result.Errors)
    {
        Console.WriteLine($"  {error.Code}: {error.Message}");
        Console.WriteLine($"    at {error.Location}");
    }
}
```

---

## 📦 Deliverables

### Code
- [ ] DependencyAnalyzer implementation
- [ ] CircularDependencyDetector implementation
- [ ] TopologicalSorter implementation
- [ ] Validator implementations (4 validators)
- [ ] Enhanced ProjectCompiler
- [ ] Build artifact generation

### Tests
- [ ] Unit tests (90%+ coverage)
- [ ] Integration tests with real schemas
- [ ] Test schemas (simple, complex, circular, diamond)
- [ ] Performance benchmarks

### Documentation
- [ ] API documentation updates
- [ ] User guide for compilation
- [ ] Examples of compiler usage
- [ ] Error code reference

---

## ⚠️ Risks & Mitigation

| Risk | Impact | Probability | Mitigation |
|------|--------|-------------|------------|
| Complex circular dependencies hard to detect | High | Medium | Use proven algorithms (Tarjan's SCC) |
| AST parsing incomplete | High | Low | Fallback to definition text parsing |
| Performance issues with large schemas | Medium | Medium | Optimize graph algorithms, add caching |
| Ambiguous dependency resolution | Medium | Low | Use search_path rules, warn on ambiguity |

---

## 📝 Notes

- Start with simple test cases and build up complexity
- Use TDD approach for graph algorithms
- Keep error messages user-friendly
- Consider performance from the start
- Document all assumptions about PostgreSQL behavior

---

**Status:** Ready to Start  
**Next Step:** Begin Phase 1 - Dependency Analysis  
**Estimated Completion:** 6 weeks

**Created by:** GitHub Copilot  
**Date:** 2026-01-31
