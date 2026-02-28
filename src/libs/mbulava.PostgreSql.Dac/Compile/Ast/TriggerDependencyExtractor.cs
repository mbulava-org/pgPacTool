using mbulava.PostgreSql.Dac.Models;
using PgQuery;
using System.Text.Json;

namespace mbulava.PostgreSql.Dac.Compile.Ast;

/// <summary>
/// Extracts dependencies from trigger definitions using AST parsing.
/// Handles table dependencies and trigger function references.
/// </summary>
public class TriggerDependencyExtractor : AstDependencyExtractor
{
    /// <summary>
    /// Extracts dependencies from a CREATE TRIGGER statement.
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
            // Extract CreateTrigStmt from the statement
            if (!stmt.Value.TryGetProperty("CreateTrigStmt", out var trigStmtElement))
            {
                return dependencies;
            }

            var astJson = trigStmtElement.GetRawText();
            var triggerStmt = JsonSerializer.Deserialize<CreateTrigStmt>(astJson);

            if (triggerStmt == null)
            {
                return dependencies;
            }

            // Extract table dependency (trigger is always on a table)
            dependencies.AddRange(ExtractTableDependency(triggerStmt, schemaName, objectName));

            // Extract function dependency (EXECUTE FUNCTION/PROCEDURE)
            dependencies.AddRange(ExtractFunctionDependency(triggerStmt, schemaName, objectName));
        }
        catch (JsonException)
        {
            // Failed to deserialize - return empty list
        }

        return dependencies;
    }

    /// <summary>
    /// Extracts the table that this trigger is attached to.
    /// </summary>
    private List<PgDependency> ExtractTableDependency(
        CreateTrigStmt triggerStmt,
        string schemaName,
        string triggerName)
    {
        var dependencies = new List<PgDependency>();

        if (triggerStmt.Relation != null)
        {
            var (tableSchema, tableName) = ExtractSchemaAndName(triggerStmt.Relation, schemaName);
            
            if (!string.IsNullOrEmpty(tableName))
            {
                dependencies.Add(CreateDependency(
                    "TRIGGER",
                    schemaName,
                    triggerName,
                    "TABLE",
                    tableSchema,
                    tableName,
                    "TRIGGER_TABLE"
                ));
            }
        }

        return dependencies;
    }

    /// <summary>
    /// Extracts the trigger function that executes when the trigger fires.
    /// </summary>
    private List<PgDependency> ExtractFunctionDependency(
        CreateTrigStmt triggerStmt,
        string schemaName,
        string triggerName)
    {
        var dependencies = new List<PgDependency>();

        if (triggerStmt.Funcname != null && triggerStmt.Funcname.Any())
        {
            var (funcSchema, funcName) = ExtractQualifiedName(triggerStmt.Funcname, schemaName);
            
            if (!string.IsNullOrEmpty(funcName))
            {
                dependencies.Add(CreateDependency(
                    "TRIGGER",
                    schemaName,
                    triggerName,
                    "FUNCTION",
                    funcSchema,
                    funcName,
                    "TRIGGER_FUNCTION"
                ));
            }
        }

        return dependencies;
    }
}
