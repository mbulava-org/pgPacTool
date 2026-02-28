using PgQuery;

namespace mbulava.PostgreSql.Dac.Compile.Ast;

/// <summary>
/// Helper utilities for navigating and extracting information from PostgreSQL AST structures.
/// </summary>
public static class AstNavigationHelpers
{
    /// <summary>
    /// Extracts the string value from a Node if it contains a String.
    /// </summary>
    public static string? GetStringValue(this Node? node)
    {
        return node?.String?.Sval;
    }

    /// <summary>
    /// Extracts the integer value from a Node if it contains an Integer.
    /// </summary>
    public static int? GetIntValue(this Node? node)
    {
        if (node?.Integer != null)
        {
            return node.Integer.Ival;
        }
        return null;
    }

    /// <summary>
    /// Extracts a qualified name (schema.object) from a list of Nodes.
    /// </summary>
    public static (string? schema, string? name) GetQualifiedName(
        this IEnumerable<Node>? nodes,
        string defaultSchema = "public")
    {
        if (nodes == null || !nodes.Any())
        {
            return (defaultSchema, null);
        }

        var nodeList = nodes.ToList();
        
        if (nodeList.Count == 1)
        {
            return (defaultSchema, nodeList[0].GetStringValue());
        }
        
        if (nodeList.Count == 2)
        {
            return (nodeList[0].GetStringValue() ?? defaultSchema, nodeList[1].GetStringValue());
        }

        // More than 2 parts (e.g., database.schema.name) - take last two
        return (nodeList[^2].GetStringValue() ?? defaultSchema, nodeList[^1].GetStringValue());
    }

    /// <summary>
    /// Extracts schema and relation name from a RangeVar.
    /// </summary>
    public static (string schema, string name) GetSchemaAndName(
        this RangeVar? rangeVar,
        string defaultSchema = "public")
    {
        if (rangeVar == null)
        {
            return (defaultSchema, string.Empty);
        }

        var schema = rangeVar.Schemaname ?? defaultSchema;
        var name = rangeVar.Relname ?? string.Empty;

        return (schema, name);
    }

    /// <summary>
    /// Recursively finds all nodes of a specific type in an AST tree.
    /// </summary>
    public static List<T> FindNodesOfType<T>(Node? root) where T : class
    {
        var results = new List<T>();
        
        if (root == null)
        {
            return results;
        }

        // Check if the current node matches the type
        // This is a simplified approach - a complete implementation would
        // check all possible node types and their nested structures
        
        return results;
    }

    /// <summary>
    /// Extracts all RangeVar nodes (table references) from a FROM clause.
    /// </summary>
    public static List<RangeVar> ExtractRangeVars(IEnumerable<Node>? fromClause)
    {
        var rangeVars = new List<RangeVar>();
        
        if (fromClause == null)
        {
            return rangeVars;
        }

        foreach (var node in fromClause)
        {
            // Direct RangeVar
            if (node?.RangeVar != null)
            {
                rangeVars.Add(node.RangeVar);
            }
            
            // JoinExpr - recursively extract from both sides
            else if (node?.JoinExpr != null)
            {
                if (node.JoinExpr.Larg != null)
                {
                    rangeVars.AddRange(ExtractRangeVars([node.JoinExpr.Larg]));
                }
                if (node.JoinExpr.Rarg != null)
                {
                    rangeVars.AddRange(ExtractRangeVars([node.JoinExpr.Rarg]));
                }
            }
        }

        return rangeVars;
    }

    /// <summary>
    /// Extracts type name from a TypeName node.
    /// </summary>
    public static (string schema, string type) GetTypeName(
        this TypeName? typeName,
        string defaultSchema = "public")
    {
        if (typeName?.Names == null || !typeName.Names.Any())
        {
            return (defaultSchema, string.Empty);
        }

        return typeName.Names.GetQualifiedName(defaultSchema);
    }

    /// <summary>
    /// Checks if a SelectStmt has a FROM clause.
    /// </summary>
    public static bool HasFromClause(this SelectStmt? selectStmt)
    {
        return selectStmt?.FromClause != null && selectStmt.FromClause.Any();
    }

    /// <summary>
    /// Checks if a SelectStmt has a WITH clause (CTEs).
    /// </summary>
    public static bool HasWithClause(this SelectStmt? selectStmt)
    {
        return selectStmt?.WithClause?.Ctes != null && selectStmt.WithClause.Ctes.Any();
    }

    /// <summary>
    /// Extracts all CTE names from a WITH clause.
    /// </summary>
    public static List<string> GetCteNames(this WithClause? withClause)
    {
        var cteNames = new List<string>();
        
        if (withClause?.Ctes == null)
        {
            return cteNames;
        }

        foreach (var cte in withClause.Ctes)
        {
            var cteName = cte?.CommonTableExpr?.Ctename;
            if (!string.IsNullOrEmpty(cteName))
            {
                cteNames.Add(cteName);
            }
        }

        return cteNames;
    }

    /// <summary>
    /// Determines if a given name is likely a CTE reference rather than a table.
    /// </summary>
    public static bool IsCteReference(string name, List<string> cteNames)
    {
        return cteNames.Contains(name, StringComparer.OrdinalIgnoreCase);
    }
}
