# AST-Based SQL Generation Architecture

## Problem Statement

Currently, `PublishScriptGenerator` generates SQL via string concatenation:

```csharp
// ❌ CURRENT (Bad)
sb.AppendLine($"ALTER TABLE {QuoteIdentifier(diff.TableName)} ALTER COLUMN {QuoteIdentifier(colDiff.ColumnName)} TYPE {colDiff.SourceDataType};");
```

**Issues:**
- ❌ Error-prone (typos, syntax errors)
- ❌ Hard to maintain
- ❌ No syntax validation
- ❌ Doesn't leverage our AST infrastructure
- ❌ Inconsistent with extraction approach

## Proposed Architecture

### 1. AST-Based SQL Generation

Use `PgQuery` AST objects to construct SQL, then deparse back to SQL:

```csharp
// ✅ PROPOSED (Good)
var alterStmt = new AlterTableStmt
{
    Relation = new RangeVar { Relname = tableName, Schemaname = schemaName },
    Cmds = new List<AlterTableCmd>
    {
        new AlterTableCmd
        {
            Subtype = AlterTableType.AT_AlterColumnType,
            Name = columnName,
            Def = new ColumnDef
            {
                TypeName = new TypeName { Names = new List<Node> { new String { Sval = dataType } } }
            }
        }
    }
};

string sql = Deparser.Deparse(alterStmt);
```

### 2. Deparser Integration

**Option A: Use pg_query Deparser (Recommended)**
```csharp
using PgQuery;

public class AstSqlGenerator
{
    public static string GenerateAlterTable(AlterTableStmt stmt)
    {
        // Serialize AST to JSON
        var json = JsonSerializer.Serialize(stmt);
        
        // Use pg_query to deparse
        var result = RawParser.Deparse(json);
        return result.Query;
    }
}
```

**Option B: Custom Deparser**
```csharp
public class PostgreSqlDeparser
{
    public static string Deparse(Node astNode)
    {
        return astNode switch
        {
            AlterTableStmt stmt => DeparseAlterTable(stmt),
            CreateStmt stmt => DeparseCreateTable(stmt),
            DropStmt stmt => DeparseDropStatement(stmt),
            // ... etc
        };
    }
}
```

### 3. Statement Builders

Create fluent builders for common operations:

```csharp
public class AlterTableBuilder
{
    private readonly string _schema;
    private readonly string _table;
    private readonly List<AlterTableCmd> _commands = new();
    
    public AlterTableBuilder(string schema, string table)
    {
        _schema = schema;
        _table = table;
    }
    
    public AlterTableBuilder AddColumn(string name, string type, bool notNull = false, string? defaultValue = null)
    {
        _commands.Add(new AlterTableCmd
        {
            Subtype = AlterTableType.AT_AddColumn,
            Def = new ColumnDef
            {
                Colname = name,
                TypeName = CreateTypeName(type),
                Constraints = CreateConstraints(notNull, defaultValue)
            }
        });
        return this;
    }
    
    public AlterTableBuilder DropColumn(string name)
    {
        _commands.Add(new AlterTableCmd
        {
            Subtype = AlterTableType.AT_DropColumn,
            Name = name
        });
        return this;
    }
    
    public AlterTableBuilder AlterColumnType(string name, string newType)
    {
        _commands.Add(new AlterTableCmd
        {
            Subtype = AlterTableType.AT_AlterColumnType,
            Name = name,
            Def = new ColumnDef
            {
                TypeName = CreateTypeName(newType)
            }
        });
        return this;
    }
    
    public AlterTableStmt Build()
    {
        return new AlterTableStmt
        {
            Relation = new RangeVar
            {
                Schemaname = _schema,
                Relname = _table
            },
            Cmds = _commands
        };
    }
}

// Usage
var builder = new AlterTableBuilder("public", "users")
    .AddColumn("email", "VARCHAR(255)", notNull: true)
    .AlterColumnType("username", "VARCHAR(100)")
    .DropColumn("legacy_field");
    
var stmt = builder.Build();
string sql = AstSqlGenerator.GenerateAlterTable(stmt);
```

### 4. Refactored PublishScriptGenerator

```csharp
public static class PublishScriptGenerator
{
    private readonly IAstSqlGenerator _generator;
    
    public static string Generate(PgSchemaDiff diff, PublishOptions? options = null)
    {
        var statements = new List<Node>();
        
        // Build AST statements from diffs
        statements.AddRange(GenerateTypeStatements(diff.TypeDiffs, options));
        statements.AddRange(GenerateSequenceStatements(diff.SequenceDiffs, options));
        statements.AddRange(GenerateTableStatements(diff.TableDiffs, options));
        statements.AddRange(GenerateViewStatements(diff.ViewDiffs, options));
        statements.AddRange(GenerateFunctionStatements(diff.FunctionDiffs, options));
        statements.AddRange(GenerateTriggerStatements(diff.TriggerDiffs, options));
        
        // Generate SQL from AST
        var sqlStatements = statements.Select(stmt => _generator.Deparse(stmt));
        
        // Combine with comments and wrapping
        return CombineStatements(sqlStatements, options);
    }
    
    private static IEnumerable<Node> GenerateTableStatements(List<PgTableDiff> diffs, PublishOptions options)
    {
        foreach (var diff in diffs)
        {
            // Build ALTER TABLE statement
            var builder = new AlterTableBuilder(diff.SchemaName, diff.TableName);
            
            foreach (var colDiff in diff.ColumnDiffs)
            {
                if (colDiff.SourceDataType == null && colDiff.TargetDataType != null)
                {
                    if (options.DropObjectsNotInSource)
                        builder.DropColumn(colDiff.ColumnName);
                }
                else if (colDiff.SourceDataType != null && colDiff.TargetDataType == null)
                {
                    builder.AddColumn(
                        colDiff.ColumnName,
                        colDiff.SourceDataType,
                        colDiff.SourceIsNotNull ?? false,
                        colDiff.SourceDefault);
                }
                else if (colDiff.SourceDataType != colDiff.TargetDataType)
                {
                    builder.AlterColumnType(colDiff.ColumnName, colDiff.SourceDataType);
                }
            }
            
            yield return builder.Build();
        }
    }
}
```

## Implementation Plan

### Phase 1: Infrastructure (Week 1)
- [ ] Create `IAstSqlGenerator` interface
- [ ] Implement pg_query deparser wrapper
- [ ] Add Npgquery deparse support
- [ ] Create base statement builders

### Phase 2: Core Builders (Week 2)
- [ ] `AlterTableBuilder`
- [ ] `CreateTableBuilder`
- [ ] `DropStatementBuilder`
- [ ] `CreateViewBuilder`
- [ ] `CreateFunctionBuilder`
- [ ] `CreateTriggerBuilder`

### Phase 3: Integration (Week 3)
- [ ] Refactor `PublishScriptGenerator` to use builders
- [ ] Update unit tests
- [ ] Integration testing
- [ ] Performance benchmarking

### Phase 4: Comments & Formatting (Week 4)
- [ ] Comment builder for SQL comments
- [ ] Formatting options
- [ ] Pretty-printing
- [ ] Statement organization

## Benefits

✅ **Type Safety:** Compile-time checking of SQL structure
✅ **Correctness:** Guaranteed syntactically valid SQL
✅ **Maintainability:** Clean, fluent API
✅ **Testability:** Easy to unit test AST construction
✅ **Consistency:** Same approach as extraction
✅ **Extensibility:** Easy to add new statement types

## Example Comparison

### Before (String-based)
```csharp
sb.AppendLine($"ALTER TABLE {QuoteIdentifier(diff.TableName)} ALTER COLUMN {QuoteIdentifier(colDiff.ColumnName)} TYPE {colDiff.SourceDataType};");
sb.AppendLine($"ALTER TABLE {QuoteIdentifier(diff.TableName)} ALTER COLUMN {QuoteIdentifier(colDiff.ColumnName)} SET NOT NULL;");
```

### After (AST-based)
```csharp
var stmt = new AlterTableBuilder("public", "users")
    .AlterColumnType("username", "VARCHAR(100)")
    .SetNotNull("username")
    .Build();
    
string sql = generator.Deparse(stmt);
```

**Result:**
```sql
ALTER TABLE public.users 
    ALTER COLUMN username TYPE VARCHAR(100),
    ALTER COLUMN username SET NOT NULL;
```

## Testing Strategy

```csharp
[Test]
public void AlterTableBuilder_Should_Generate_Correct_Sql()
{
    // Arrange
    var builder = new AlterTableBuilder("public", "users")
        .AddColumn("email", "VARCHAR(255)", notNull: true);
    
    // Act
    var stmt = builder.Build();
    var sql = generator.Deparse(stmt);
    
    // Assert
    Assert.That(sql, Does.Contain("ALTER TABLE public.users"));
    Assert.That(sql, Does.Contain("ADD COLUMN email VARCHAR(255) NOT NULL"));
}
```

## Migration Strategy

1. **Create new infrastructure** alongside existing code
2. **Migrate one object type at a time** (start with simple ones like sequences)
3. **Keep existing tests passing** during migration
4. **Add new AST-based tests** for each migrated section
5. **Remove old string-based code** once validated

## Technical Debt Resolution

This refactoring addresses:
- ❌ Manual SQL string building
- ❌ Identifier quoting bugs
- ❌ Inconsistent SQL formatting
- ❌ Lack of syntax validation
- ❌ Duplication of SQL generation logic

## References

- [pg_query Documentation](https://github.com/pganalyze/pg_query)
- [PostgreSQL AST Reference](https://github.com/pganalyze/libpg_query/blob/main/protobuf/pg_query.proto)
- [Npgquery Library](https://github.com/launchbadge/pg_query.net)
