using mbulava.PostgreSql.Dac.Models;
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

            // Extract foreign key dependencies
            dependencies.AddRange(ExtractForeignKeyDependencies(createStmtElement, schemaName, objectName));

            // Extract inheritance dependencies
            dependencies.AddRange(ExtractInheritanceDependencies(createStmtElement, schemaName, objectName));

            // Extract sequence dependencies from DEFAULT expressions
            dependencies.AddRange(ExtractSequenceDependencies(createStmtElement, schemaName, objectName));

            // Extract type dependencies from columns
            dependencies.AddRange(ExtractTypeDependencies(createStmtElement, schemaName, objectName));
        }
        catch (JsonException)
        {
            // Failed to process - return what we have
        }

        return dependencies;
    }

    /// <summary>
    /// Extracts foreign key constraints from table definition.
    /// </summary>
    private List<PgDependency> ExtractForeignKeyDependencies(
        JsonElement createStmt,
        string schemaName,
        string tableName)
    {
        var dependencies = new List<PgDependency>();

        if (!createStmt.TryGetProperty("tableElts", out var tableElts))
        {
            return dependencies;
        }

        foreach (var elt in tableElts.EnumerateArray())
        {
            // Check for ColumnDef nodes with constraints
            if (elt.TryGetProperty("ColumnDef", out var columnDef))
            {
                if (columnDef.TryGetProperty("constraints", out var constraints))
                {
                    foreach (var constraintNode in constraints.EnumerateArray())
                    {
                        if (constraintNode.TryGetProperty("Constraint", out var constraint))
                        {
                            if (constraint.TryGetProperty("contype", out var contype))
                            {
                                var constraintType = contype.GetString();
                                if (constraintType == "CONSTR_FOREIGN")
                                {
                                    // Extract referenced table from pktable
                                    if (constraint.TryGetProperty("pktable", out var pktable))
                                    {
                                        var refSchema = pktable.TryGetProperty("schemaname", out var schemaEl)
                                            ? schemaEl.GetString() ?? schemaName
                                            : schemaName;
                                        var refTable = pktable.TryGetProperty("relname", out var nameEl)
                                            ? nameEl.GetString()
                                            : null;

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
                        }
                    }
                }
            }

            // Also check for table-level Constraint nodes (separate from ColumnDef)
            else if (elt.TryGetProperty("Constraint", out var constraint))
            {
                if (constraint.TryGetProperty("contype", out var contype))
                {
                    var constraintType = contype.GetString();
                    if (constraintType == "CONSTR_FOREIGN")
                    {
                        // Extract referenced table from pktable
                        if (constraint.TryGetProperty("pktable", out var pktable))
                        {
                            var refSchema = pktable.TryGetProperty("schemaname", out var schemaEl)
                                ? schemaEl.GetString() ?? schemaName
                                : schemaName;
                            var refTable = pktable.TryGetProperty("relname", out var nameEl)
                                ? nameEl.GetString()
                                : null;

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
            }
        }

        return dependencies;
    }

    /// <summary>
    /// Extracts inheritance (INHERITS) dependencies.
    /// </summary>
    private List<PgDependency> ExtractInheritanceDependencies(
        JsonElement createStmt,
        string schemaName,
        string tableName)
    {
        var dependencies = new List<PgDependency>();

        if (!createStmt.TryGetProperty("inhRelations", out var inhRelations))
        {
            return dependencies;
        }

        foreach (var parentNode in inhRelations.EnumerateArray())
        {
            if (parentNode.TryGetProperty("RangeVar", out var rangeVar))
            {
                var parentSchema = rangeVar.TryGetProperty("schemaname", out var schemaEl)
                    ? schemaEl.GetString() ?? schemaName
                    : schemaName;
                var parentTable = rangeVar.TryGetProperty("relname", out var nameEl)
                    ? nameEl.GetString()
                    : null;

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
        JsonElement createStmt,
        string schemaName,
        string tableName)
    {
        var dependencies = new List<PgDependency>();

        if (!createStmt.TryGetProperty("tableElts", out var tableElts))
        {
            return dependencies;
        }

        foreach (var elt in tableElts.EnumerateArray())
        {
            // Check for ColumnDef nodes with constraints
            if (elt.TryGetProperty("ColumnDef", out var columnDef))
            {
                if (columnDef.TryGetProperty("constraints", out var constraints))
                {
                    foreach (var constraintNode in constraints.EnumerateArray())
                    {
                        if (constraintNode.TryGetProperty("Constraint", out var constraint))
                        {
                            if (constraint.TryGetProperty("contype", out var contype))
                            {
                                var constraintType = contype.GetString();
                                if (constraintType == "CONSTR_DEFAULT")
                                {
                                    // Look for nextval() in raw_expr
                                    if (constraint.TryGetProperty("raw_expr", out var rawExpr))
                                    {
                                        var sequenceDep = ExtractSequenceFromDefaultExpr(
                                            rawExpr,
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
        JsonElement rawExpr,
        string schemaName,
        string tableName)
    {
        // Check if this is a FuncCall
        if (!rawExpr.TryGetProperty("FuncCall", out var funcCall))
        {
            return null;
        }

        // Check if function name is 'nextval'
        if (funcCall.TryGetProperty("funcname", out var funcname))
        {
            var funcNameParts = new List<string>();
            foreach (var node in funcname.EnumerateArray())
            {
                if (node.TryGetProperty("String", out var stringNode))
                {
                    if (stringNode.TryGetProperty("sval", out var sval))
                    {
                        var part = sval.GetString();
                        if (!string.IsNullOrEmpty(part))
                        {
                            funcNameParts.Add(part);
                        }
                    }
                }
            }

            var actualFuncName = funcNameParts.LastOrDefault();
            if (!actualFuncName?.Equals("nextval", StringComparison.OrdinalIgnoreCase) ?? true)
            {
                return null;
            }
        }

        // Extract sequence name from first argument
        if (funcCall.TryGetProperty("args", out var args))
        {
            foreach (var arg in args.EnumerateArray())
            {
                // First argument might be wrapped in TypeCast
                JsonElement actualArg = arg;

                // Check if it's a TypeCast node
                if (arg.TryGetProperty("TypeCast", out var typeCast))
                {
                    if (typeCast.TryGetProperty("arg", out var innerArg))
                    {
                        actualArg = innerArg;
                    }
                }

                // Now extract A_Const value
                if (actualArg.TryGetProperty("A_Const", out var aConst))
                {
                    if (aConst.TryGetProperty("sval", out var svalNode))
                    {
                        if (svalNode.TryGetProperty("sval", out var svalStr))
                        {
                            var sequenceName = svalStr.GetString();
                            if (!string.IsNullOrEmpty(sequenceName))
                            {
                                // Parse qualified name (may include schema)
                                var parts = sequenceName.Split('.');
                                var seqSchema = parts.Length > 1 ? parts[0].Trim('"', '\'') : schemaName;
                                var seqName = parts.Length > 1 ? parts[1].Trim('"', '\'') : parts[0].Trim('"', '\'');

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
                }

                // Only check first arg
                break;
            }
        }

        return null;
    }

    /// <summary>
    /// Extracts type dependencies from column definitions.
    /// </summary>
    private List<PgDependency> ExtractTypeDependencies(
        JsonElement createStmt,
        string schemaName,
        string tableName)
    {
        var dependencies = new List<PgDependency>();
        var processedTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if (!createStmt.TryGetProperty("tableElts", out var tableElts))
        {
            return dependencies;
        }

        foreach (var elt in tableElts.EnumerateArray())
        {
            if (elt.TryGetProperty("ColumnDef", out var columnDef))
            {
                if (columnDef.TryGetProperty("typeName", out var typeName))
                {
                    // Extract type name from names array
                    if (typeName.TryGetProperty("names", out var names))
                    {
                        var typeNameParts = new List<string>();
                        foreach (var node in names.EnumerateArray())
                        {
                            if (node.TryGetProperty("String", out var stringNode))
                            {
                                if (stringNode.TryGetProperty("sval", out var sval))
                                {
                                    var part = sval.GetString();
                                    if (!string.IsNullOrEmpty(part))
                                    {
                                        typeNameParts.Add(part);
                                    }
                                }
                            }
                        }

                        if (typeNameParts.Count > 0)
                        {
                            string typeSchema, type;
                            if (typeNameParts.Count == 1)
                            {
                                typeSchema = schemaName;
                                type = typeNameParts[0];
                            }
                            else
                            {
                                typeSchema = typeNameParts[0];
                                type = typeNameParts[1];
                            }

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

