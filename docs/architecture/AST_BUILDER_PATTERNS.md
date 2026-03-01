# AST Builder Design Patterns

## Overview
When building PostgreSQL AST structures programmatically, we face two key challenges:
1. **Complex nested JSON structures** (DropStmt, AlterTableStmt, etc.)
2. **Type system complexity** (TypeName, ColumnDef with type modifiers)

This document describes the patterns we use to handle this complexity.

## Pattern 1: Direct JSON Construction (Simple Operations)

**Use When**: The AST structure is simple and well-defined with no complex type parsing needed.

**Example**: DROP TABLE, DROP COLUMN, SET NOT NULL

```csharp
public static JsonElement DropTable(string schema, string tableName, bool ifExists = true, bool cascade = false)
{
    var stmt = new
    {
        DropStmt = new
        {
            objects = new[]
            {
                new
                {
                    List = new
                    {
                        items = new object[]
                        {
                            new { String = new { sval = schema } },
                            new { String = new { sval = tableName } }
                        }
                    }
                }
            },
            removeType = "OBJECT_TABLE",
            behavior = cascade ? "DROP_CASCADE" : "DROP_RESTRICT",
            missing_ok = ifExists
        }
    };
    
    return WrapStatement(stmt);
}
```

**Pros**:
- Fastest (no parsing)
- Most explicit
- Easy to debug

**Cons**:
- Requires deep knowledge of AST structure
- Must manually construct every nested object

**Complexity**: Low

---

## Pattern 2: Parse-Extract-Rebuild (Complex Types)

**Use When**: You need to handle complex type definitions (varchar(255), numeric(10,2), etc.) or expressions.

**Example**: ALTER COLUMN TYPE

```csharp
public static JsonElement AlterTableAlterColumnType(string schema, string tableName, string columnName, string newDataType)
{
    // Step 1: Parse a temp statement to extract the TypeName structure
    var tempSql = $"ALTER TABLE temp.temp ALTER COLUMN temp TYPE {newDataType};";
    
    using var parser = new Npgquery.Parser();
    var parseResult = parser.Parse(tempSql);
    
    if (!parseResult.IsSuccess || parseResult.ParseTree == null)
    {
        throw new InvalidOperationException($"Failed to parse type definition: {newDataType}");
    }
    
    // Step 2: Navigate to the TypeName node
    var root = parseResult.ParseTree.RootElement;
    JsonElement typeNameElement = default;
    
    if (root.TryGetProperty("stmts", out var stmts) && stmts.GetArrayLength() > 0)
    {
        var stmtItem = stmts[0];
        if (stmtItem.TryGetProperty("stmt", out var stmtObj) &&
            stmtObj.TryGetProperty("AlterTableStmt", out var alterStmt) &&
            alterStmt.TryGetProperty("cmds", out var cmds) && cmds.GetArrayLength() > 0)
        {
            var cmd = cmds[0];
            if (cmd.TryGetProperty("AlterTableCmd", out var alterCmd) &&
                alterCmd.TryGetProperty("def", out var def) &&
                def.TryGetProperty("ColumnDef", out var colDef) &&
                colDef.TryGetProperty("typeName", out var typeName))
            {
                typeNameElement = typeName;
            }
        }
    }
    
    // Step 3: Clean location fields (not needed for generation)
    var typeNameObj = CleanLocationFields(typeNameElement);
    
    // Step 4: Build our AST with the extracted TypeName
    var stmt = new
    {
        AlterTableStmt = new
        {
            relation = new { schemaname = schema, relname = tableName, ... },
            cmds = new[]
            {
                new
                {
                    AlterTableCmd = new
                    {
                        subtype = "AT_AlterColumnType",
                        name = columnName,
                        def = new
                        {
                            ColumnDef = new
                            {
                                typeName = typeNameObj  // ← Extracted from parsed SQL
                            }
                        },
                        ...
                    }
                }
            },
            ...
        }
    };
    
    return WrapStatement(stmt);
}
```

**Pros**:
- Handles complex type definitions correctly
- Leverages PostgreSQL's parser for type system knowledge
- Works with any valid PostgreSQL type

**Cons**:
- Slightly slower (~2-3ms for parsing)
- More complex code
- Requires understanding both parsing and AST navigation

**Complexity**: Medium-High

**When to Use**:
- Type definitions with modifiers: `varchar(255)`, `numeric(10,2)`
- Array types: `integer[]`, `text[][]`
- Custom types: `my_custom_type`
- Complex expressions in DEFAULT values

---

## Pattern 3: Hybrid (Common Case Optimization)

**Use When**: 90% of cases are simple, but need to handle complex cases too.

**Example**: Constraint definitions

```csharp
private static object ParseConstraintDefinition(string constraintName, string definition)
{
    // Fast path for simple constraints (90% of cases)
    var defUpper = definition.ToUpper().Trim();
    
    if (defUpper.StartsWith("UNIQUE"))
    {
        // Extract column names
        var columns = ExtractColumnNames(definition);
        
        return new
        {
            contype = "CONSTR_UNIQUE",
            conname = constraintName,
            keys = columns.Select(col => new { String = new { sval = col } }).ToArray()
        };
    }
    
    // Slow path for complex constraints (10% of cases)
    // Fall back to parse-extract pattern
    var tempSql = $"ALTER TABLE temp.temp ADD CONSTRAINT {constraintName} {definition};";
    using var parser = new Npgquery.Parser();
    var result = parser.Parse(tempSql);
    // ... extract and return constraint AST
}
```

**Pros**:
- Fast for common cases
- Reliable for complex cases
- Best of both worlds

**Cons**:
- More code
- Need to identify common vs complex cases

**Complexity**: Medium

---

## Helper Functions

### WrapStatement
Wraps any statement node in the standard ParseResult structure:

```csharp
private static JsonElement WrapStatement(object stmtContent)
{
    var wrapper = new
    {
        version = 170004, // PostgreSQL 17.0.4
        stmts = new[]
        {
            new
            {
                stmt = stmtContent,
                stmt_len = 0
            }
        }
    };
    
    var json = JsonSerializer.Serialize(wrapper);
    using var doc = JsonDocument.Parse(json);
    return doc.RootElement.Clone();
}
```

### CleanLocationFields
Removes location metadata from extracted AST nodes:

```csharp
private static object CleanLocationFields(JsonElement element)
{
    if (element.ValueKind == JsonValueKind.Object)
    {
        var dict = new Dictionary<string, object>();
        foreach (var prop in element.EnumerateObject())
        {
            // Skip location fields - only used for parser error reporting
            if (prop.Name == "location")
                continue;
                
            // Recursively clean nested objects
            if (prop.Value.ValueKind == JsonValueKind.Object)
            {
                dict[prop.Name] = CleanLocationFields(prop.Value);
            }
            else if (prop.Value.ValueKind == JsonValueKind.Array)
            {
                var items = prop.Value.EnumerateArray()
                    .Select(item => item.ValueKind == JsonValueKind.Object 
                        ? CleanLocationFields(item) 
                        : JsonSerializer.Deserialize<object>(item.GetRawText())!)
                    .ToArray();
                dict[prop.Name] = items;
            }
            else
            {
                dict[prop.Name] = JsonSerializer.Deserialize<object>(prop.Value.GetRawText())!;
            }
        }
        return dict;
    }
    
    return JsonSerializer.Deserialize<object>(element.GetRawText())!;
}
```

## Decision Tree: Which Pattern to Use?

```
Is the operation a simple DROP/ADD/RENAME?
├─ YES → Use Pattern 1 (Direct JSON Construction)
└─ NO → Does it involve types or expressions?
    ├─ YES → Use Pattern 2 (Parse-Extract-Rebuild)
    └─ NO → Is it common with rare complex cases?
        ├─ YES → Use Pattern 3 (Hybrid)
        └─ NO → Use Pattern 1 or 2 based on complexity
```

## Examples by Operation

| Operation | Pattern | Reason |
|-----------|---------|--------|
| DROP TABLE | 1 | Simple structure |
| DROP COLUMN | 1 | Simple structure |
| ADD COLUMN (basic) | 1 | Simple type, no constraints |
| ADD COLUMN (complex) | 2 | varchar(255), numeric(10,2) |
| ALTER COLUMN TYPE | 2 | Must handle all PG types |
| SET NOT NULL | 1 | No type info needed |
| ADD CONSTRAINT UNIQUE | 3 | Simple 90%, complex 10% |
| ADD CONSTRAINT CHECK | 2 | Complex expressions |
| CREATE INDEX | 1 | Simple column list |

## Performance Characteristics

| Pattern | Parse Time | Build Time | Total | Use Case |
|---------|-----------|------------|-------|----------|
| 1 (Direct) | 0ms | <0.1ms | <0.1ms | Simple operations |
| 2 (Parse-Extract) | 1-2ms | <0.1ms | 1-2ms | Complex types |
| 3 (Hybrid) | 0-2ms | <0.1ms | 0-2ms | Mixed workload |

## Testing Strategy

1. **Unit test each pattern separately**
2. **Round-trip test**: SQL → Parse → Deparse → Compare
3. **Integration test**: Use in PublishScriptGenerator
4. **Case-insensitive assertions**: Deparser may use different case

**Example**:
```csharp
[Test]
public void AlterColumnType_HandlesComplexTypes()
{
    var ast = AstBuilder.AlterTableAlterColumnType("public", "users", "price", "numeric(10,2)");
    var sql = AstSqlGenerator.Generate(ast);
    
    // Case-insensitive check
    sql.ToUpper().Should().Contain("ALTER COLUMN");
    sql.ToUpper().Should().Contain("TYPE NUMERIC");
}
```

## Common Pitfalls

1. **Don't assume case**: Deparser may lowercase keywords
2. **Don't assume keyword presence**: `DROP age` vs `DROP COLUMN age` (both valid)
3. **Don't hardcode type names**: `BIGINT` becomes `int8` internally
4. **Do remove location fields**: They're not needed for generation
5. **Do validate deparse output**: Always check the generated SQL

## Future Enhancements

1. **Expression AST builder** - For CHECK constraints, computed columns
2. **Subquery AST builder** - For DEFAULT with subqueries
3. **Function call AST builder** - For DEFAULT with functions
4. **Caching parsed structures** - For repeated type definitions

---

**Key Principle**: Use the simplest pattern that reliably handles your use case. Don't over-engineer, but don't take shortcuts that compromise correctness.
