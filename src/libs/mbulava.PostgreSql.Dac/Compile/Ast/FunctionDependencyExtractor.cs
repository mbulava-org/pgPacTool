using mbulava.PostgreSql.Dac.Models;
using System.Text.Json;

namespace mbulava.PostgreSql.Dac.Compile.Ast;

/// <summary>
/// Extracts dependencies from function/procedure definitions using AST parsing.
/// Handles parameter types and return types.
/// </summary>
public class FunctionDependencyExtractor : AstDependencyExtractor
{
    /// <summary>
    /// Extracts dependencies from a CREATE FUNCTION/PROCEDURE statement.
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
            // Extract CreateFunctionStmt from the statement
            if (!stmt.Value.TryGetProperty("CreateFunctionStmt", out var funcStmtElement))
            {
                return dependencies;
            }

            // Extract type dependencies from parameters
            dependencies.AddRange(ExtractParameterTypeDependencies(funcStmtElement, schemaName, objectName));

            // Extract type dependencies from return type
            dependencies.AddRange(ExtractReturnTypeDependencies(funcStmtElement, schemaName, objectName));
        }
        catch (JsonException)
        {
            // Failed to process - return what we have
        }

        return dependencies.DistinctBy(d => $"{d.DependsOnType}.{d.DependsOnSchema}.{d.DependsOnName}").ToList();
    }

    /// <summary>
    /// Extracts parameter type dependencies.
    /// </summary>
    private List<PgDependency> ExtractParameterTypeDependencies(
        JsonElement funcStmt,
        string schemaName,
        string functionName)
    {
        var dependencies = new List<PgDependency>();

        if (!funcStmt.TryGetProperty("parameters", out var parameters))
        {
            return dependencies;
        }

        foreach (var param in parameters.EnumerateArray())
        {
            if (param.TryGetProperty("FunctionParameter", out var funcParam))
            {
                if (funcParam.TryGetProperty("argType", out var argType))
                {
                    // Extract type name from names array
                    if (argType.TryGetProperty("names", out var names))
                    {
                        var typeNameParts = ExtractStringArray(names);
                        
                        if (typeNameParts.Count > 0)
                        {
                            string typeSchema, typeName;
                            if (typeNameParts.Count == 1)
                            {
                                typeSchema = schemaName;
                                typeName = typeNameParts[0];
                            }
                            else
                            {
                                typeSchema = typeNameParts[0];
                                typeName = typeNameParts[1];
                            }

                            // Only add dependency for user-defined types (not built-in types)
                            if (!IsBuiltInType(typeName))
                            {
                                dependencies.Add(CreateDependency(
                                    "FUNCTION",
                                    schemaName,
                                    functionName,
                                    "TYPE",
                                    typeSchema,
                                    typeName,
                                    "PARAMETER_TYPE"
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
    /// Extracts return type dependencies.
    /// </summary>
    private List<PgDependency> ExtractReturnTypeDependencies(
        JsonElement funcStmt,
        string schemaName,
        string functionName)
    {
        var dependencies = new List<PgDependency>();

        if (!funcStmt.TryGetProperty("returnType", out var returnType))
        {
            return dependencies;
        }

        // Extract type name from names array
        if (returnType.TryGetProperty("names", out var names))
        {
            var typeNameParts = ExtractStringArray(names);
            
            if (typeNameParts.Count > 0)
            {
                string typeSchema, typeName;
                if (typeNameParts.Count == 1)
                {
                    typeSchema = schemaName;
                    typeName = typeNameParts[0];
                }
                else
                {
                    typeSchema = typeNameParts[0];
                    typeName = typeNameParts[1];
                }

                // Only add dependency for user-defined types (not built-in types)
                // Skip 'void' return type
                if (!typeName.Equals("void", StringComparison.OrdinalIgnoreCase) && !IsBuiltInType(typeName))
                {
                    dependencies.Add(CreateDependency(
                        "FUNCTION",
                        schemaName,
                        functionName,
                        "TYPE",
                        typeSchema,
                        typeName,
                        "RETURN_TYPE"
                    ));
                }
            }
        }

        return dependencies;
    }

    /// <summary>
    /// Extracts string array from names JSON structure.
    /// </summary>
    private List<string> ExtractStringArray(JsonElement names)
    {
        var result = new List<string>();
        
        foreach (var node in names.EnumerateArray())
        {
            if (node.TryGetProperty("String", out var stringNode))
            {
                if (stringNode.TryGetProperty("sval", out var sval))
                {
                    var value = sval.GetString();
                    if (!string.IsNullOrEmpty(value))
                    {
                        result.Add(value);
                    }
                }
            }
        }
        
        return result;
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
            "void", "oid", "regproc", "regprocedure", "regoper", "regoperator",
            "regclass", "regtype", "regrole", "regnamespace", "regconfig",
            "regdictionary", "pg_lsn", "txid_snapshot", "pg_snapshot"
        };

        return builtInTypes.Contains(typeName);
    }
}
