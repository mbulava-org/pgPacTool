using mbulava.PostgreSql.Dac.Models;
using PgQuery;
using System.Text.Json;

namespace mbulava.PostgreSql.Dac.Compile.Ast;

/// <summary>
/// Extracts dependencies from table definitions using AST parsing.
/// Handles foreign keys, inheritance, sequences (DEFAULT nextval), and type dependencies.
/// </summary>
public class TableDependencyExtractor : AstDependencyExtractor
{
    /// <summary>
    /// Extracts dependencies from a CREATE TABLE statement.
    /// </summary>
    public override List<PgDependency> ExtractDependencies(
        string sql,
        string schemaName,
        string objectName,
        string objectType)
    {
        var dependencies = new List<PgDependency>();

        var stmt = GetFirstStatement(sql);
        if (stmt == null)
        {
            return dependencies;
        }

        try
        {
            // Extract CreateStmt from the statement
            if (!stmt.Value.TryGetProperty("CreateStmt", out var createStmtElement))
            {
                return dependencies;
            }

            var astJson = createStmtElement.GetRawText();
            var createStmt = JsonSerializer.Deserialize<CreateStmt>(astJson);

            if (createStmt == null)
            {
                return dependencies;
            }

            // Extract foreign key dependencies
            dependencies.AddRange(ExtractForeignKeyDependencies(createStmt, schemaName, objectName));

            // Extract inheritance dependencies
            dependencies.AddRange(ExtractInheritanceDependencies(createStmt, schemaName, objectName));

            // Extract sequence dependencies from DEFAULT expressions
            dependencies.AddRange(ExtractSequenceDependencies(createStmt, schemaName, objectName));

            // Extract type dependencies from columns
            dependencies.AddRange(ExtractTypeDependencies(createStmt, schemaName, objectName));
        }
        catch (JsonException)
        {
            // Failed to deserialize - return empty list
        }

        return dependencies;
    }

    /// <summary>
    /// Extracts foreign key constraints from table definition.
    /// </summary>
    private List<PgDependency> ExtractForeignKeyDependencies(
        CreateStmt createStmt,
        string schemaName,
        string tableName)
    {
        var dependencies = new List<PgDependency>();

        if (createStmt.TableElts == null)
        {
            return dependencies;
        }

        foreach (var elt in createStmt.TableElts)
        {
            // Check for Constraint nodes
            if (elt?.Constraint?.Contype == ConstrType.ConstrForeign)
            {
                var constraint = elt.Constraint;
                
                // Extract referenced table from Pktable
                if (constraint.Pktable != null)
                {
                    var (refSchema, refTable) = ExtractSchemaAndName(constraint.Pktable, schemaName);
                    
                    if (!string.IsNullOrEmpty(refTable))
                    {
                        dependencies.Add(CreateDependency(
                            "TABLE",
                            schemaName,
                            tableName,
                            "TABLE",
                            refSchema,
                            refTable,
                            "FOREIGN_KEY"
                        ));
                    }
                }
            }
        }

        return dependencies;
    }

    /// <summary>
    /// Extracts inheritance (INHERITS) dependencies.
    /// </summary>
    private List<PgDependency> ExtractInheritanceDependencies(
        CreateStmt createStmt,
        string schemaName,
        string tableName)
    {
        var dependencies = new List<PgDependency>();

        if (createStmt.InhRelations == null)
        {
            return dependencies;
        }

        foreach (var parentNode in createStmt.InhRelations)
        {
            if (parentNode?.RangeVar != null)
            {
                var (parentSchema, parentTable) = ExtractSchemaAndName(parentNode.RangeVar, schemaName);

                if (!string.IsNullOrEmpty(parentTable))
                {
                    dependencies.Add(CreateDependency(
                        "TABLE",
                        schemaName,
                        tableName,
                        "TABLE",
                        parentSchema,
                        parentTable,
                        "INHERITANCE"
                    ));
                }
            }
        }

        return dependencies;
    }

    /// <summary>
    /// Extracts sequence dependencies from DEFAULT nextval() expressions.
    /// </summary>
    private List<PgDependency> ExtractSequenceDependencies(
        CreateStmt createStmt,
        string schemaName,
        string tableName)
    {
        var dependencies = new List<PgDependency>();

        if (createStmt.TableElts == null)
        {
            return dependencies;
        }

        foreach (var elt in createStmt.TableElts)
        {
            // Check for ColumnDef nodes with DEFAULT expressions
            if (elt?.ColumnDef?.Constraints != null)
            {
                foreach (var constraint in elt.ColumnDef.Constraints)
                {
                    if (constraint?.Constraint?.Contype == ConstrType.ConstrDefault)
                    {
                        // Look for nextval() function calls in the default expression
                        var sequenceDep = ExtractSequenceFromDefaultExpr(
                            constraint.Constraint.RawExpr,
                            schemaName,
                            tableName);
                        
                        if (sequenceDep != null)
                        {
                            dependencies.Add(sequenceDep);
                        }
                    }
                }
            }
        }

        return dependencies;
    }

    /// <summary>
    /// Extracts sequence name from a DEFAULT nextval() expression.
    /// </summary>
    private PgDependency? ExtractSequenceFromDefaultExpr(
        Node? rawExpr,
        string schemaName,
        string tableName)
    {
        if (rawExpr?.FuncCall == null)
        {
            return null;
        }

        var funcCall = rawExpr.FuncCall;
        
        // Check if function name is 'nextval'
        var funcName = ExtractQualifiedName(funcCall.Funcname);
        if (funcName.name.Equals("nextval", StringComparison.OrdinalIgnoreCase))
        {
            // Extract sequence name from first argument
            if (funcCall.Args != null && funcCall.Args.Any())
            {
                var firstArg = funcCall.Args.First();
                
                // Argument is typically an A_Const with String value
                if (firstArg?.AConst?.Sval != null)
                {
                    var sequenceName = firstArg.AConst.Sval.Sval;
                    
                    // Parse qualified name (may include schema)
                    var parts = sequenceName.Split('.');
                    var seqSchema = parts.Length > 1 ? parts[0].Trim('"') : schemaName;
                    var seqName = parts.Length > 1 ? parts[1].Trim('"') : parts[0].Trim('"');
                    
                    return CreateDependency(
                        "TABLE",
                        schemaName,
                        tableName,
                        "SEQUENCE",
                        seqSchema,
                        seqName,
                        "SEQUENCE_DEFAULT"
                    );
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Extracts type dependencies from column definitions.
    /// </summary>
    private List<PgDependency> ExtractTypeDependencies(
        CreateStmt createStmt,
        string schemaName,
        string tableName)
    {
        var dependencies = new List<PgDependency>();
        var processedTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if (createStmt.TableElts == null)
        {
            return dependencies;
        }

        foreach (var elt in createStmt.TableElts)
        {
            if (elt?.ColumnDef?.TypeName != null)
            {
                var typeName = elt.ColumnDef.TypeName;
                
                // Extract type name from qualified name list
                var (typeSchema, type) = ExtractQualifiedName(typeName.Names, schemaName);
                
                // Only add dependency for user-defined types (not built-in types)
                if (!IsBuiltInType(type))
                {
                    var typeKey = $"{typeSchema}.{type}";
                    if (!processedTypes.Contains(typeKey))
                    {
                        processedTypes.Add(typeKey);
                        
                        dependencies.Add(CreateDependency(
                            "TABLE",
                            schemaName,
                            tableName,
                            "TYPE",
                            typeSchema,
                            type,
                            "COLUMN_TYPE"
                        ));
                    }
                }
            }
        }

        return dependencies;
    }

    /// <summary>
    /// Checks if a type name is a PostgreSQL built-in type.
    /// </summary>
    private bool IsBuiltInType(string typeName)
    {
        var builtInTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            // Numeric types
            "smallint", "integer", "int", "int2", "int4", "int8", "bigint",
            "decimal", "numeric", "real", "double", "float", "float4", "float8",
            "smallserial", "serial", "bigserial", "money",
            
            // Character types
            "char", "character", "varchar", "character varying", "text",
            "bpchar", "name",
            
            // Binary types
            "bytea",
            
            // Date/time types
            "timestamp", "timestamptz", "timestamp with time zone",
            "timestamp without time zone", "date", "time", "timetz",
            "time with time zone", "time without time zone", "interval",
            
            // Boolean
            "boolean", "bool",
            
            // Geometric types
            "point", "line", "lseg", "box", "path", "polygon", "circle",
            
            // Network types
            "cidr", "inet", "macaddr", "macaddr8",
            
            // Bit string types
            "bit", "bit varying", "varbit",
            
            // Text search types
            "tsvector", "tsquery",
            
            // UUID
            "uuid",
            
            // XML
            "xml",
            
            // JSON
            "json", "jsonb",
            
            // Arrays (suffix)
            "array",
            
            // Range types
            "int4range", "int8range", "numrange", "tsrange", "tstzrange", "daterange",
            
            // Other
            "oid", "regproc", "regprocedure", "regoper", "regoperator",
            "regclass", "regtype", "regrole", "regnamespace", "regconfig",
            "regdictionary", "pg_lsn", "txid_snapshot", "pg_snapshot"
        };

        return builtInTypes.Contains(typeName);
    }
}
