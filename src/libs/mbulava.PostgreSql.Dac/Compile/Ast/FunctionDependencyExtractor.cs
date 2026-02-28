using mbulava.PostgreSql.Dac.Models;
using PgQuery;
using System.Text.Json;

namespace mbulava.PostgreSql.Dac.Compile.Ast;

/// <summary>
/// Extracts dependencies from function/procedure definitions using AST parsing.
/// Handles table references, type dependencies from parameters/returns, and function calls.
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

            var astJson = funcStmtElement.GetRawText();
            var funcStmt = JsonSerializer.Deserialize<CreateFunctionStmt>(astJson);

            if (funcStmt == null)
            {
                return dependencies;
            }

            // Extract type dependencies from parameters
            dependencies.AddRange(ExtractParameterTypeDependencies(funcStmt, schemaName, objectName));

            // Extract type dependencies from return type
            dependencies.AddRange(ExtractReturnTypeDependencies(funcStmt, schemaName, objectName));

            // Extract table and function references from function body
            // Note: Function body is typically a string, so we'll need to parse it
            dependencies.AddRange(ExtractBodyDependencies(funcStmt, schemaName, objectName));
        }
        catch (JsonException)
        {
            // Failed to deserialize - return empty list
        }

        return dependencies.DistinctBy(d => $"{d.DependsOnType}.{d.DependsOnSchema}.{d.DependsOnName}").ToList();
    }

    /// <summary>
    /// Extracts type dependencies from function parameters.
    /// </summary>
    private List<PgDependency> ExtractParameterTypeDependencies(
        CreateFunctionStmt funcStmt,
        string schemaName,
        string functionName)
    {
        var dependencies = new List<PgDependency>();

        if (funcStmt.Parameters == null)
        {
            return dependencies;
        }

        var processedTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var param in funcStmt.Parameters)
        {
            if (param?.FunctionParameter?.ArgType != null)
            {
                var typeName = param.FunctionParameter.ArgType;
                var (typeSchema, type) = ExtractQualifiedName(typeName.Names, schemaName);

                // Only add dependency for user-defined types (not built-in types)
                if (!IsBuiltInType(type))
                {
                    var typeKey = $"{typeSchema}.{type}";
                    if (!processedTypes.Contains(typeKey))
                    {
                        processedTypes.Add(typeKey);
                        
                        dependencies.Add(CreateDependency(
                            "FUNCTION",
                            schemaName,
                            functionName,
                            "TYPE",
                            typeSchema,
                            type,
                            "PARAMETER_TYPE"
                        ));
                    }
                }
            }
        }

        return dependencies;
    }

    /// <summary>
    /// Extracts type dependencies from return type.
    /// </summary>
    private List<PgDependency> ExtractReturnTypeDependencies(
        CreateFunctionStmt funcStmt,
        string schemaName,
        string functionName)
    {
        var dependencies = new List<PgDependency>();

        if (funcStmt.ReturnType != null)
        {
            var (typeSchema, type) = ExtractQualifiedName(funcStmt.ReturnType.Names, schemaName);

            // Only add dependency for user-defined types
            if (!IsBuiltInType(type) && !type.Equals("void", StringComparison.OrdinalIgnoreCase))
            {
                dependencies.Add(CreateDependency(
                    "FUNCTION",
                    schemaName,
                    functionName,
                    "TYPE",
                    typeSchema,
                    type,
                    "RETURN_TYPE"
                ));
            }
        }

        return dependencies;
    }

    /// <summary>
    /// Extracts table and function references from function body.
    /// This is a simplified approach that looks for common patterns.
    /// A complete implementation would parse the function body SQL.
    /// </summary>
    private List<PgDependency> ExtractBodyDependencies(
        CreateFunctionStmt funcStmt,
        string schemaName,
        string functionName)
    {
        var dependencies = new List<PgDependency>();

        // Function body is typically in Options (e.g., as_clause)
        // For PL/pgSQL functions, the body is a string that would need separate parsing
        // For SQL functions, we could potentially parse the body

        // TODO: Implement comprehensive body parsing
        // For now, we'll rely on the existing regex-based approach as a fallback
        // or implement basic pattern matching

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
            
            // Special types
            "void", "record", "trigger", "event_trigger", "any", "anyelement",
            "anyarray", "anynonarray", "anyenum", "anyrange",
            
            // System types
            "oid", "regproc", "regprocedure", "regoper", "regoperator",
            "regclass", "regtype", "regrole", "regnamespace", "regconfig",
            "regdictionary", "pg_lsn", "txid_snapshot", "pg_snapshot"
        };

        return builtInTypes.Contains(typeName);
    }
}
