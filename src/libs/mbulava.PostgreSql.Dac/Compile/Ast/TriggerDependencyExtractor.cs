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

            // Extract table dependency from relation
            if (trigStmtElement.TryGetProperty("relation", out var relation))
            {
                var tableSchema = relation.TryGetProperty("schemaname", out var schemaEl)
                    ? schemaEl.GetString() ?? schemaName
                    : schemaName;
                var tableName = relation.TryGetProperty("relname", out var nameEl)
                    ? nameEl.GetString()
                    : null;

                if (!string.IsNullOrEmpty(tableName))
                {
                    dependencies.Add(CreateDependency(
                        "TRIGGER",
                        schemaName,
                        objectName,
                        "TABLE",
                        tableSchema,
                        tableName,
                        "TRIGGER_TABLE"
                    ));
                }
            }

            // Extract function dependency from funcname array
            if (trigStmtElement.TryGetProperty("funcname", out var funcnameArray))
            {
                var funcNameParts = new List<string>();
                foreach (var node in funcnameArray.EnumerateArray())
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

                if (funcNameParts.Count > 0)
                {
                    string funcSchema, funcName;
                    if (funcNameParts.Count == 1)
                    {
                        funcSchema = schemaName;
                        funcName = funcNameParts[0];
                    }
                    else
                    {
                        funcSchema = funcNameParts[0];
                        funcName = funcNameParts[1];
                    }

                    dependencies.Add(CreateDependency(
                        "TRIGGER",
                        schemaName,
                        objectName,
                        "FUNCTION",
                        funcSchema,
                        funcName,
                        "TRIGGER_FUNCTION"
                    ));
                }
            }
        }
        catch (JsonException)
        {
            // Failed to process - return what we have
        }

        return dependencies;
    }
}

