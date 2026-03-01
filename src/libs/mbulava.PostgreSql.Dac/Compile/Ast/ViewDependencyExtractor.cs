using mbulava.PostgreSql.Dac.Models;
using PgQuery;
using System.Text.Json;

namespace mbulava.PostgreSql.Dac.Compile.Ast;

/// <summary>
/// Extracts dependencies from view definitions using AST parsing.
/// Handles table/view references, CTEs, and subqueries.
/// </summary>
public class ViewDependencyExtractor : AstDependencyExtractor
{
    /// <summary>
    /// Extracts dependencies from a CREATE VIEW statement.
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
            // Extract ViewStmt from the statement
            if (!stmt.Value.TryGetProperty("ViewStmt", out var viewStmtElement))
            {
                return dependencies;
            }

            // Navigate to query.SelectStmt in the JSON
            if (!viewStmtElement.TryGetProperty("query", out var queryElement))
            {
                return dependencies;
            }

            if (!queryElement.TryGetProperty("SelectStmt", out var selectStmtElement))
            {
                return dependencies;
            }

            // Extract dependencies from the SelectStmt JSON directly
            dependencies.AddRange(ExtractReferencesFromSelectStmtJson(selectStmtElement, schemaName, objectName));
        }
        catch (JsonException)
        {
            // Failed to deserialize - return empty list
        }

        return dependencies;
    }

    /// <summary>
    /// Extracts references from a SelectStmt JSON element.
    /// </summary>
    private List<PgDependency> ExtractReferencesFromSelectStmtJson(
        JsonElement selectStmtElement,
        string schemaName,
        string viewName)
    {
        var dependencies = new List<PgDependency>();
        var processedRefs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // First, collect CTE names to filter them out from fromClause
        var cteNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (selectStmtElement.TryGetProperty("withClause", out var withClause))
        {
            if (withClause.TryGetProperty("ctes", out var ctes))
            {
                foreach (var cte in ctes.EnumerateArray())
                {
                    if (cte.TryGetProperty("CommonTableExpr", out var cteExpr))
                    {
                        if (cteExpr.TryGetProperty("ctename", out var cteName))
                        {
                            var name = cteName.GetString();
                            if (!string.IsNullOrEmpty(name))
                            {
                                cteNames.Add(name);
                            }
                        }
                    }
                }
            }
        }

        // Extract from FROM clause (filtering out CTE references)
        if (selectStmtElement.TryGetProperty("fromClause", out var fromClauseElement))
        {
            foreach (var fromItem in fromClauseElement.EnumerateArray())
            {
                // Check for RangeVar (direct table reference)
                if (fromItem.TryGetProperty("RangeVar", out var rangeVarElement))
                {
                    var schema = rangeVarElement.TryGetProperty("schemaname", out var schemaEl) 
                        ? schemaEl.GetString() ?? schemaName 
                        : null; // No schema = likely a CTE
                    var tableName = rangeVarElement.TryGetProperty("relname", out var nameEl)
                        ? nameEl.GetString() 
                        : null;

                    if (!string.IsNullOrEmpty(tableName))
                    {
                        // Skip if this is a CTE reference (no schema and name matches a CTE)
                        if (schema == null && cteNames.Contains(tableName))
                        {
                            continue; // Skip CTE reference
                        }

                        // Use default schema if none specified (and not a CTE)
                        schema ??= schemaName;

                        var refKey = $"{schema}.{tableName}";
                        if (!processedRefs.Contains(refKey))
                        {
                            processedRefs.Add(refKey);

                            dependencies.Add(CreateDependency(
                                "VIEW",
                                schemaName,
                                viewName,
                                "TABLE_OR_VIEW",
                                schema,
                                tableName,
                                "VIEW_REFERENCE"
                            ));
                        }
                    }
                }
                // Handle JOINs (JoinExpr)
                else if (fromItem.TryGetProperty("JoinExpr", out var joinExpr))
                {
                    dependencies.AddRange(ExtractReferencesFromJoinExpr(joinExpr, schemaName, viewName, processedRefs));
                }
                // Handle subquery (RangeSubselect)
                else if (fromItem.TryGetProperty("RangeSubselect", out var rangeSubselect))
                {
                    if (rangeSubselect.TryGetProperty("subquery", out var subquery))
                    {
                        if (subquery.TryGetProperty("SelectStmt", out var subSelectStmt))
                        {
                            dependencies.AddRange(ExtractReferencesFromSelectStmtJson(subSelectStmt, schemaName, viewName));
                        }
                    }
                }
            }
        }

        // Extract from CTEs (WITH clause) - recurse into CTE queries to get real table dependencies
        if (selectStmtElement.TryGetProperty("withClause", out var withClause2))
        {
            if (withClause2.TryGetProperty("ctes", out var ctes2))
            {
                foreach (var cte in ctes2.EnumerateArray())
                {
                    if (cte.TryGetProperty("CommonTableExpr", out var cteExpr))
                    {
                        if (cteExpr.TryGetProperty("ctequery", out var cteQuery))
                        {
                            if (cteQuery.TryGetProperty("SelectStmt", out var cteSelectStmt))
                            {
                                // Recursively extract from CTE query to get actual table dependencies
                                dependencies.AddRange(ExtractReferencesFromSelectStmtJson(cteSelectStmt, schemaName, viewName));
                            }
                        }
                    }
                }
            }
        }

        // Handle UNION/INTERSECT/EXCEPT (larg and rarg)
        if (selectStmtElement.TryGetProperty("larg", out var larg))
        {
            dependencies.AddRange(ExtractReferencesFromSelectStmtJson(larg, schemaName, viewName));
        }
        if (selectStmtElement.TryGetProperty("rarg", out var rarg))
        {
            dependencies.AddRange(ExtractReferencesFromSelectStmtJson(rarg, schemaName, viewName));
        }

        return dependencies.DistinctBy(d => $"{d.DependsOnSchema}.{d.DependsOnName}").ToList();
    }

    /// <summary>
    /// Extracts references from a JoinExpr JSON element.
    /// </summary>
    private List<PgDependency> ExtractReferencesFromJoinExpr(
        JsonElement joinExpr,
        string schemaName,
        string viewName,
        HashSet<string> processedRefs)
    {
        var dependencies = new List<PgDependency>();

        // Left side of join - larg is directly a node (RangeVar, JoinExpr, etc.)
        if (joinExpr.TryGetProperty("larg", out var larg))
        {
            // Check if it's a RangeVar (table reference)
            if (larg.TryGetProperty("RangeVar", out var rangeVar))
            {
                var schema = rangeVar.TryGetProperty("schemaname", out var schemaEl)
                    ? schemaEl.GetString() ?? schemaName
                    : schemaName;
                var tableName = rangeVar.TryGetProperty("relname", out var nameEl)
                    ? nameEl.GetString()
                    : null;

                if (!string.IsNullOrEmpty(tableName))
                {
                    var refKey = $"{schema}.{tableName}";
                    if (!processedRefs.Contains(refKey))
                    {
                        processedRefs.Add(refKey);
                        dependencies.Add(CreateDependency("VIEW", schemaName, viewName, "TABLE_OR_VIEW", schema, tableName, "VIEW_REFERENCE"));
                    }
                }
            }
            // Check if it's a nested JoinExpr
            else if (larg.TryGetProperty("JoinExpr", out var nestedJoin))
            {
                dependencies.AddRange(ExtractReferencesFromJoinExpr(nestedJoin, schemaName, viewName, processedRefs));
            }
        }

        // Right side of join - rarg is directly a node
        if (joinExpr.TryGetProperty("rarg", out var rarg))
        {
            // Check if it's a RangeVar (table reference)
            if (rarg.TryGetProperty("RangeVar", out var rangeVar))
            {
                var schema = rangeVar.TryGetProperty("schemaname", out var schemaEl)
                    ? schemaEl.GetString() ?? schemaName
                    : schemaName;
                var tableName = rangeVar.TryGetProperty("relname", out var nameEl)
                    ? nameEl.GetString()
                    : null;

                if (!string.IsNullOrEmpty(tableName))
                {
                    var refKey = $"{schema}.{tableName}";
                    if (!processedRefs.Contains(refKey))
                    {
                        processedRefs.Add(refKey);
                        dependencies.Add(CreateDependency("VIEW", schemaName, viewName, "TABLE_OR_VIEW", schema, tableName, "VIEW_REFERENCE"));
                    }
                }
            }
            // Check if it's a nested JoinExpr
            else if (rarg.TryGetProperty("JoinExpr", out var nestedJoin))
            {
                dependencies.AddRange(ExtractReferencesFromJoinExpr(nestedJoin, schemaName, viewName, processedRefs));
            }
        }

        return dependencies;
    }
}

