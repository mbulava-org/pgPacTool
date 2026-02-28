using mbulava.PostgreSql.Dac.Models;
using Npgquery;
using PgQuery;
using System.Text.Json;

namespace mbulava.PostgreSql.Dac.Compile.Ast;

/// <summary>
/// Base class for AST-based dependency extraction.
/// Provides common utilities for parsing and navigating PostgreSQL AST structures.
/// </summary>
public abstract class AstDependencyExtractor
{
    protected readonly Parser _parser = new();

    /// <summary>
    /// Extracts dependencies from a database object using AST parsing.
    /// </summary>
    /// <param name="sql">SQL definition of the object</param>
    /// <param name="schemaName">Schema name containing the object</param>
    /// <param name="objectName">Name of the object</param>
    /// <param name="objectType">Type of the object</param>
    /// <returns>List of dependencies</returns>
    public abstract List<PgDependency> ExtractDependencies(
        string sql, 
        string schemaName, 
        string objectName, 
        string objectType);

    /// <summary>
    /// Parses SQL and returns the first statement element from the AST.
    /// Navigates the structure: stmts[0].stmt
    /// </summary>
    protected JsonElement? GetFirstStatement(string sql)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sql);

        try
        {
            var result = _parser.Parse(sql);
            if (!result.IsSuccess || result.ParseTree == null)
            {
                return null;
            }

            var root = result.ParseTree.RootElement;

            // Navigate to stmts array
            if (!root.TryGetProperty("stmts", out var stmts))
            {
                return null;
            }

            // Get first statement
            if (stmts.GetArrayLength() == 0)
            {
                return null;
            }

            var firstStmt = stmts[0];

            // Navigate to stmt object
            if (!firstStmt.TryGetProperty("stmt", out var stmt))
            {
                return null;
            }

            return stmt;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Extracts schema and object name from a RangeVar node.
    /// </summary>
    protected (string schema, string name) ExtractSchemaAndName(RangeVar? rangeVar, string defaultSchema = "public")
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
    /// Extracts schema and object name from a qualified name list (e.g., schema.table).
    /// </summary>
    protected (string schema, string name) ExtractQualifiedName(
        IEnumerable<Node>? nameNodes, 
        string defaultSchema = "public")
    {
        if (nameNodes == null || !nameNodes.Any())
        {
            return (defaultSchema, string.Empty);
        }

        var nodes = nameNodes.ToList();
        
        if (nodes.Count == 1)
        {
            var name = nodes[0]?.String?.Sval ?? string.Empty;
            return (defaultSchema, name);
        }
        
        if (nodes.Count == 2)
        {
            var schema = nodes[0]?.String?.Sval ?? defaultSchema;
            var name = nodes[1]?.String?.Sval ?? string.Empty;
            return (schema, name);
        }

        // More than 2 parts: database.schema.name - we take last two
        var schemaNode = nodes[^2];
        var nameNode = nodes[^1];
        
        return (schemaNode?.String?.Sval ?? defaultSchema, nameNode?.String?.Sval ?? string.Empty);
    }

    /// <summary>
    /// Extracts column references from a Node (recursively).
    /// Useful for finding table references in WHERE clauses, JOIN conditions, etc.
    /// </summary>
    protected List<(string? schema, string? table, string column)> ExtractColumnRefs(Node? node)
    {
        var refs = new List<(string? schema, string? table, string column)>();
        
        if (node == null)
        {
            return refs;
        }

        // Check if this node is a ColumnRef
        if (node.ColumnRef != null)
        {
            var colRef = node.ColumnRef;
            var fields = colRef.Fields.ToList();

            if (fields.Count == 1)
            {
                // Just column name
                var columnName = fields[0]?.String?.Sval;
                if (columnName != null)
                {
                    refs.Add((null, null, columnName));
                }
            }
            else if (fields.Count == 2)
            {
                // table.column
                var tableName = fields[0]?.String?.Sval;
                var columnName = fields[1]?.String?.Sval;
                if (tableName != null && columnName != null)
                {
                    refs.Add((null, tableName, columnName));
                }
            }
            else if (fields.Count >= 3)
            {
                // schema.table.column
                var schemaName = fields[0]?.String?.Sval;
                var tableName = fields[1]?.String?.Sval;
                var columnName = fields[2]?.String?.Sval;
                if (schemaName != null && tableName != null && columnName != null)
                {
                    refs.Add((schemaName, tableName, columnName));
                }
            }
        }

        // Recursively search child nodes
        // Note: This is a simplified approach. A complete implementation would
        // traverse all possible node types that can contain ColumnRefs.
        
        return refs;
    }

    /// <summary>
    /// Extracts table references from a RangeVar list (FROM clause).
    /// </summary>
    protected List<(string schema, string table)> ExtractTableReferences(IEnumerable<Node>? fromClause, string defaultSchema = "public")
    {
        var tables = new List<(string schema, string table)>();
        
        if (fromClause == null)
        {
            return tables;
        }

        foreach (var node in fromClause)
        {
            if (node?.RangeVar != null)
            {
                var (schema, name) = ExtractSchemaAndName(node.RangeVar, defaultSchema);
                if (!string.IsNullOrEmpty(name))
                {
                    tables.Add((schema, name));
                }
            }
            // Handle JOINs (RangeSubselect, JoinExpr, etc.)
            // TODO: Add more comprehensive extraction for complex queries
        }

        return tables;
    }

    /// <summary>
    /// Creates a PgDependency object.
    /// </summary>
    protected PgDependency CreateDependency(
        string objectType,
        string objectSchema,
        string objectName,
        string dependsOnType,
        string dependsOnSchema,
        string dependsOnName,
        string dependencyType)
    {
        return new PgDependency
        {
            ObjectType = objectType,
            ObjectSchema = objectSchema,
            ObjectName = objectName,
            DependsOnType = dependsOnType,
            DependsOnSchema = dependsOnSchema,
            DependsOnName = dependsOnName,
            DependencyType = dependencyType
        };
    }

    /// <summary>
    /// Checks if a name is a SQL keyword (to avoid false positives in dependency detection).
    /// </summary>
    protected bool IsKeyword(string word)
    {
        var keywords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "SELECT", "FROM", "WHERE", "JOIN", "INNER", "OUTER", "LEFT", "RIGHT",
            "ON", "AS", "AND", "OR", "NOT", "NULL", "TRUE", "FALSE", "VALUES",
            "INSERT", "UPDATE", "DELETE", "CREATE", "ALTER", "DROP", "GRANT",
            "REVOKE", "BEGIN", "COMMIT", "ROLLBACK", "CASE", "WHEN", "THEN",
            "ELSE", "END", "DISTINCT", "ALL", "UNION", "INTERSECT", "EXCEPT"
        };

        return keywords.Contains(word);
    }
}
