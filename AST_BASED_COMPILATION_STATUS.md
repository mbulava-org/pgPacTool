# AST-Based Compilation - Implementation Status

## Branch
`feature/AST_BASED_COMPILATION`

## 🎉 Current Achievement: 84% Complete!

### Test Results
**32/38 tests passing (84%)** 🎉

#### By Extractor:
- ✅ **ViewDependencyExtractor**: 12/12 (100%) - **COMPLETE!**
- ✅ **TriggerDependencyExtractor**: 9/9 (100%) - **COMPLETE!**
- ⚠️ **TableDependencyExtractor**: 8/9 (89%) - 1 sequence test remaining
- ⚠️ **FunctionDependencyExtractor**: 3/8 (38%) - 5 type extraction tests remaining

## 🔬 Critical Discovery: Why Protobuf Deserialization Fails

### Investigation Results

We conducted comprehensive diagnostics to understand why `JsonSerializer.Deserialize<ViewStmt>()` doesn't work. Here's what we found:

#### Finding #1: No JSON Property Name Attributes ❌
```csharp
// All protobuf properties have:
JsonPropertyName: None

// This means C# property names don't match JSON:
JSON: "fromClause", "schemaname", "funcname" (camelCase)
C#:   FromClause,   Schemaname,   Funcname   (PascalCase)
```

#### Finding #2: Enum String Conversion Failure 🔴
```csharp
// JSON has string enums:
"limitOption": "LIMIT_OPTION_DEFAULT"

// C# expects enum type:
public LimitOption LimitOption { get; set; }

// JsonSerializer can't convert without [JsonConverter(typeof(JsonStringEnumConverter))]
// Error: "The JSON value could not be converted to PgQuery.LimitOption"
```

#### Finding #3: Default Deserialization Returns Empty Objects ⚠️
```csharp
var viewStmt = JsonSerializer.Deserialize<ViewStmt>(json);
// Success: True ✓
// View: (empty) ✗
// Query: (empty) ✗
// All properties are null!
```

#### Finding #4: PropertyNameCaseInsensitive Doesn't Help
```csharp
var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
var viewStmt = JsonSerializer.Deserialize<ViewStmt>(json, options);
// Still fails on nested enum conversion
```

### Root Cause Analysis

The **Npgquery protobuf classes are NOT designed for JSON deserialization**. They would require:

1. `[JsonPropertyName("camelCase")]` attributes on every property
2. `[JsonConverter(typeof(JsonStringEnumConverter))]` on all enum properties
3. Custom converters for polymorphic `Node` types
4. Modifications to the protobuf generation process

### Why JsonElement is the RIGHT Solution ✅

Our JsonElement approach **correctly handles all these issues**:

```csharp
// ✅ Direct property access - no name mapping needed
if (stmt.TryGetProperty("fromClause", out var fromClause))

// ✅ Enum strings stay as strings - no conversion
var constraintType = contype.GetString(); // "CONSTR_FOREIGN"

// ✅ Polymorphic nodes handled naturally
if (query.TryGetProperty("SelectStmt", out var selectStmt))

// ✅ Reliable, explicit, performant
```

**Benefits:**
- ✅ No reflection-based deserialization overhead
- ✅ No hidden conversion failures
- ✅ Explicit navigation = easier to debug
- ✅ Works reliably with current Npgquery library
- ✅ JSON format IS consistent - we just navigate it directly

## 🏗️ Architecture Pattern

### Established Pattern (Used Successfully)
```csharp
public override List<PgDependency> ExtractDependencies(string sql, ...)
{
    var stmt = GetFirstStatement(sql); // Navigate to stmts[0].stmt
    if (stmt == null) return dependencies;

    try
    {
        // Extract specific statement type
        if (!stmt.Value.TryGetProperty("ViewStmt", out var viewStmt))
            return dependencies;

        // Navigate JSON directly using TryGetProperty
        if (viewStmt.TryGetProperty("query", out var query))
        {
            if (query.TryGetProperty("SelectStmt", out var selectStmt))
            {
                // Process using JsonElement
                if (selectStmt.TryGetProperty("fromClause", out var fromClause))
                {
                    foreach (var item in fromClause.EnumerateArray())
                    {
                        // Extract dependencies
                    }
                }
            }
        }
    }
    catch (JsonException)
    {
        // Return what we have
    }

    return dependencies;
}
```


- **Inheritance Dependencies**: Extracts INHERITS relationships
- **Sequence Dependencies**: Extracts DEFAULT nextval() references
- **Type Dependencies**: Extracts user-defined column types
- **Built-in Type Filtering**: Excludes PostgreSQL native types

#### `ViewDependencyExtractor.cs` 🚧
- **Basic Structure**: Implemented with proper AST navigation
- **Table/View References**: Extraction logic in place
- **JOINs**: Recursive extraction implemented
- **CTEs**: WITH clause handling implemented
- **Subqueries**: Recursive extraction implemented
- **UNION/INTERSECT/EXCEPT**: Set operations handling
- **Status**: Needs debugging - Query node structure requires investigation

#### `FunctionDependencyExtractor.cs` ✅
- **Parameter Type Dependencies**: Extracts parameter types
- **Return Type Dependencies**: Extracts return type
- **User-Defined Type Filtering**: Excludes built-in types
- **Duplicate Removal**: DistinctBy on type references
- **Status**: Body dependency extraction marked as TODO

#### `TriggerDependencyExtractor.cs` ✅
- **Table Dependencies**: Extracts trigger table
- **Function Dependencies**: Extracts EXECUTE FUNCTION/PROCEDURE
- **Cross-Schema Support**: Handles qualified names

### 3. Integration with DependencyAnalyzer
- **Backward Compatible**: Falls back to regex when AST fails
- **Optional AST Extraction**: Constructor parameter `useAstExtraction`
- **All Methods Updated**: Table, View, Function, Trigger extraction

### 4. Test Suite
Created comprehensive unit tests (33 total):
- **TableDependencyExtractorTests**: 9 tests
- **ViewDependencyExtractorTests**: 11 tests  
- **FunctionDependencyExtractorTests**: 9 tests
- **TriggerDependencyExtractorTests**: 8 tests

**Test Results**: 7 passing, 26 failing (ViewStmt extraction needs fixes)

## 🚧 Known Issues

### ViewStmt Query Extraction
**Problem**: `ViewStmt.Query` node navigation returns empty dependencies

**Likely Causes**:
1. Node structure wrapping - Query may need unwrapping
2. SelectStmt deserialization - may need different approach
3. FromClause navigation - RangeVar access pattern

**Next Steps**:
```csharp
// Need to verify actual structure:
var query = viewStmt.Query;
// Check if query is wrapped in another node
// Verify SelectStmt properties are accessible
// Debug FromClause structure
```

### Sequence/Type Extraction in Tables
Some table tests may be failing due to:
- nextval() function call parsing
- Type name extraction from ColumnDef
- Constraint node structure

## 📝 Remaining Work

### High Priority
1. **Fix ViewDependencyExtractor**
   - Debug Query node structure
   - Verify SelectStmt.FromClause access
   - Test with actual parsed AST output
   - Add logging/debugging to extraction methods

2. **Fix Table Sequence/Type Extraction**
   - Verify DEFAULT expression structure
   - Check ColumnDef.TypeName access
   - Test with actual CREATE TABLE statements

3. **Complete Function Body Extraction**
   - Parse PL/pgSQL function bodies
   - Extract table references from SQL functions
   - Handle function-to-function calls

### Medium Priority
4. **Add AST Extraction for Additional Objects**
   - Index dependencies
   - Constraint dependencies  
   - Partition dependencies

5. **Performance Optimization**
   - Cache parsed ASTs
   - Parallel extraction for large projects
   - Reduce JSON deserialization overhead

### Low Priority
6. **Documentation**
   - Update AST_BASED_COMPILATION.md with final design
   - Add examples of each extractor
   - Document AST navigation patterns

7. **Integration Testing**
   - Test with real-world projects
   - Verify circular dependency detection with AST
   - Compare AST vs regex accuracy

## 🎯 Success Criteria

- [ ] All 33 unit tests passing
- [ ] Integration tests passing
- [ ] No regressions in existing tests
- [ ] Documentation updated
- [ ] Code reviewed and merged

## 📊 Current Status

**Phase**: Implementation
**Completion**: ~70%
**Blockers**: ViewStmt Query extraction
**ETA**: 4-8 hours to resolve remaining issues

## 🔧 How to Continue

1. **Debug ViewStmt**:
   ```bash
   # Run single test with detailed output
   dotnet test --filter "ExtractDependencies_WithSimpleSelect" --logger "console;verbosity=detailed"
   ```

2. **Add Debug Output**:
   ```csharp
   // In ViewDependencyExtractor
   Console.WriteLine($"ViewStmt.Query: {viewStmt.Query}");
   Console.WriteLine($"Query JSON: {JsonSerializer.Serialize(viewStmt.Query)}");
   ```

3. **Test with Parser**:
   ```csharp
   var parser = new Parser();
   var result = parser.Parse("CREATE VIEW test AS SELECT * FROM users");
   Console.WriteLine(result.ParseTree.RootElement.GetRawText());
   ```

## 📚 References
- AST_BASED_COMPILATION.md - Original design document
- Npgquery parser documentation
- PgQuery protobuf definitions
- CsprojProjectLoader.cs - Reference implementation
