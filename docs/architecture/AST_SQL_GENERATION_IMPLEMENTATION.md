# AST-Based SQL Generation - Implementation Plan

## Vision
Move from string-based SQL generation to AST-based generation for reliability, testability, and type safety.

## Current State
- ✅ AST parsing (SQL → AST): **100% Complete**
- ✅ Dependency extraction from AST: **100% Complete**
- ❌ SQL generation from AST: **0% Complete** (using string templates)

## Phase 2 Goals
1. Replace all string-template SQL generation with AST-based generation
2. Use Npgquery's `Deparse()` method to convert AST → SQL
3. Build AST construction helpers for common DDL operations
4. Ensure round-trip consistency: SQL → AST → SQL

## Architecture

### Core Components

#### 1. `AstSqlGenerator.cs` (New)
Central class for AST → SQL generation using Npgquery.Deparse()

```csharp
public static class AstSqlGenerator
{
    public static string Generate(JsonDocument ast)
    {
        using var parser = new Parser();
        var result = parser.Deparse(ast);
        
        if (!result.IsSuccess)
        {
            throw new InvalidOperationException($"Failed to deparse AST: {result.Error}");
        }
        
        return result.Query;
    }
    
    public static string Generate<T>(T astNode) where T : IMessage
    {
        // Convert protobuf node to JSON
        var json = JsonSerializer.Serialize(astNode);
        var doc = JsonDocument.Parse(json);
        return Generate(doc);
    }
}
```

#### 2. `AstBuilder.cs` (New)
Fluent API for building AST nodes programmatically

```csharp
public class AstBuilder
{
    // Table operations
    public static JsonElement CreateTable(string schema, string tableName, 
        List<ColumnDefinition> columns,
        List<ConstraintDefinition> constraints = null);
    
    public static JsonElement AlterTableAddColumn(string schema, string tableName,
        ColumnDefinition column);
    
    public static JsonElement AlterTableDropColumn(string schema, string tableName,
        string columnName);
    
    public static JsonElement DropTable(string schema, string tableName,
        bool ifExists = true);
    
    // View operations
    public static JsonElement CreateView(string schema, string viewName,
        JsonElement selectQuery,
        bool orReplace = false);
    
    public static JsonElement DropView(string schema, string viewName,
        bool ifExists = true);
    
    // Sequence operations
    public static JsonElement CreateSequence(string schema, string sequenceName,
        SequenceOptions options = null);
    
    public static JsonElement AlterSequence(string schema, string sequenceName,
        SequenceOptions options);
    
    // Function operations
    public static JsonElement CreateFunction(string schema, string functionName,
        List<FunctionParameter> parameters,
        TypeName returnType,
        string body,
        string language = "plpgsql");
    
    // Trigger operations
    public static JsonElement CreateTrigger(string triggerName,
        string schema, string tableName,
        string functionName,
        TriggerTiming timing,
        TriggerEvents events);
}
```

#### 3. Refactor `PublishScriptGenerator.cs`
Replace all string templates with AST building + deparse

**Before:**
```csharp
sb.AppendLine($"ALTER TABLE {QuoteIdentifier(tableName)} ADD COLUMN {colDef};");
```

**After:**
```csharp
var ast = AstBuilder.AlterTableAddColumn(schema, tableName, columnDef);
var sql = AstSqlGenerator.Generate(ast);
sb.AppendLine(sql);
```

## Implementation Steps

### Step 1: Create Core Infrastructure (Week 1)
- [ ] Create `AstSqlGenerator.cs` with basic Deparse wrapper
- [ ] Create `AstBuilder.cs` with CREATE TABLE support
- [ ] Add unit tests for round-trip (SQL → AST → SQL)
- [ ] Verify deparse output matches expected SQL

### Step 2: Build AST Construction Helpers (Week 2)
- [ ] Table operations (CREATE, ALTER, DROP)
- [ ] View operations (CREATE OR REPLACE, DROP)
- [ ] Sequence operations (CREATE, ALTER, DROP)
- [ ] Function operations (CREATE OR REPLACE, DROP)
- [ ] Trigger operations (CREATE, DROP)
- [ ] Type operations (CREATE, ALTER, DROP)

### Step 3: Refactor PublishScriptGenerator (Week 3)
- [ ] Refactor `GenerateTableScripts()` to use AST
- [ ] Refactor `GenerateViewScripts()` to use AST
- [ ] Refactor `GenerateSequenceScripts()` to use AST
- [ ] Refactor `GenerateFunctionScripts()` to use AST
- [ ] Refactor `GenerateTriggerScripts()` to use AST
- [ ] Refactor `GenerateTypeScripts()` to use AST

### Step 4: Testing & Validation (Week 4)
- [ ] Unit tests for each AST builder method
- [ ] Integration tests for PublishScriptGenerator
- [ ] Round-trip tests (original SQL → AST → generated SQL → parse → compare)
- [ ] Performance benchmarks (string vs AST generation)

## Benefits

### Reliability ✅
- Guaranteed syntactically correct SQL
- No quoting/escaping errors
- Type-safe construction

### Testability ✅
- Test AST construction independently
- Verify SQL output programmatically
- Easy to mock/stub for testing

### Maintainability ✅
- Single source of truth (AST)
- Refactoring is safer
- Clear separation of concerns

### Future-Proof ✅
- Easy to add new SQL features
- Support for different PostgreSQL versions
- Enables SQL transformation/optimization

## Risks & Mitigation

### Risk 1: Deparse Output Formatting
**Risk:** Deparsed SQL may not match exact formatting expectations
**Mitigation:** Accept canonical formatting, use formatters if needed

### Risk 2: Complex AST Construction
**Risk:** Building AST manually is verbose
**Mitigation:** Use fluent builder pattern, create helper methods

### Risk 3: Performance
**Risk:** AST construction + deparse may be slower than string templates
**Mitigation:** Benchmark early, cache AST where possible

### Risk 4: Learning Curve
**Risk:** Team needs to learn AST structure
**Mitigation:** Comprehensive documentation, examples, helper library

## Success Criteria

- [ ] Zero string-template SQL generation remaining
- [ ] 100% AST-based generation with round-trip tests
- [ ] Performance within 20% of string-based approach
- [ ] All existing tests passing with new implementation
- [ ] Documentation complete with examples

## Timeline

- **Week 1:** Core infrastructure (AstSqlGenerator, basic AstBuilder)
- **Week 2:** Complete AstBuilder for all DDL operations
- **Week 3:** Refactor PublishScriptGenerator
- **Week 4:** Testing, documentation, optimization

**Total:** 4 weeks for complete AST-based SQL generation
