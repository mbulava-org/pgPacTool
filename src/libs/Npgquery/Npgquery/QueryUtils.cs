using System.Text.Json;

namespace Npgquery;

/// <summary>
/// Utility methods for working with PostgreSQL queries
/// </summary>
public static class QueryUtils
{
    /// <summary>
    /// Extract table names from a PostgreSQL query
    /// </summary>
    public static List<string> ExtractTableNames(string query)
    {
        var result = Parser.QuickParse(query);
        if (result.IsError || result.ParseTree is null)
            return new List<string>();

        try
        {
            var tables = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            ExtractTablesFromJson(result.ParseTree.RootElement, tables);
            return tables.ToList();
        }
        catch
        {
            return new List<string>();
        }
    }

    /// <summary>
    /// Check if two queries have the same structure (same fingerprint)
    /// </summary>
    public static bool HaveSameStructure(string query1, string query2)
    {
        var fp1 = Parser.QuickFingerprint(query1);
        var fp2 = Parser.QuickFingerprint(query2);

        return fp1.IsSuccess && fp2.IsSuccess && 
               string.Equals(fp1.Fingerprint, fp2.Fingerprint, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Get query type (SELECT, INSERT, UPDATE, DELETE, etc.)
    /// </summary>
    public static string? GetQueryType(string query)
    {
        var result = Parser.QuickParse(query);
        if (result.IsError || result.ParseTree is null)
            return null;

        try
        {
            return ExtractQueryTypeFromJson(result.ParseTree.RootElement);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Clean and format a PostgreSQL query
    /// </summary>
    public static string CleanQuery(string query)
    {
        var normalized = Parser.QuickNormalize(query);
        return normalized.IsSuccess && !string.IsNullOrEmpty(normalized.NormalizedQuery) 
            ? normalized.NormalizedQuery 
            : query.Trim();
    }

    /// <summary>
    /// Validate multiple queries and return validation results
    /// </summary>
    public static Dictionary<string, bool> ValidateQueries(IEnumerable<string> queries)
    {
        using var parser = new Parser();
        return queries.ToDictionary(q => q, parser.IsValid);
    }

    /// <summary>
    /// Get detailed error information for invalid queries
    /// </summary>
    public static Dictionary<string, string?> GetQueryErrors(IEnumerable<string> queries)
    {
        using var parser = new Parser();
        return queries.ToDictionary(q => q, parser.GetError);
    }

    /// <summary>
    /// Split a multi-statement SQL string into individual statements
    /// </summary>
    public static List<string> SplitStatements(string sqlText)
    {
        var result = Parser.QuickSplit(sqlText);
        return result.IsSuccess && result.Statements != null
            ? result.Statements
                .Where(s => !string.IsNullOrWhiteSpace(s.Statement))
                .Select(s => s.Statement!)
                .ToList()
            : new List<string>();
    }

    /// <summary>
    /// Get all tokens from a PostgreSQL query
    /// </summary>
    public static List<SqlToken> GetTokens(string query)
    {
        var result = Parser.QuickScan(query);
        return result.IsSuccess && result.Tokens != null
            ? result.Tokens.ToList()
            : new List<SqlToken>();
    }

    /// <summary>
    /// Get all keywords from a PostgreSQL query
    /// </summary>
    public static List<string> GetKeywords(string query)
    {
        return GetTokens(query)
            .Where(t => !string.IsNullOrEmpty(t.KeywordKind))
            .Select(t => t.KeywordKind!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    /// <summary>
    /// Convert AST back to SQL query
    /// </summary>
    public static string? AstToSql(JsonDocument parseTree)
    {
        var result = Parser.QuickDeparse(parseTree);
        return result.IsSuccess ? result.Query : null;
    }

    /// <summary>
    /// Round-trip test: parse a query and deparse it back
    /// </summary>
    public static (bool Success, string? RoundTripQuery) RoundTripTest(string query)
    {
        var parseResult = Parser.QuickParse(query);
        if (parseResult.IsError || parseResult.ParseTree is null)
            return (false, null);

        var deparseResult = Parser.QuickDeparse(parseResult.ParseTree);
        return deparseResult.IsError ? (false, null) : (true, deparseResult.Query);
    }

    /// <summary>
    /// Check if PL/pgSQL code is valid
    /// </summary>
    public static bool IsValidPlpgsql(string plpgsqlCode) => 
        Parser.QuickParsePlpgsql(plpgsqlCode).IsSuccess;

    /// <summary>
    /// Count the number of statements in a SQL string
    /// </summary>
    public static int CountStatements(string sqlText)
    {
        var result = Parser.QuickSplit(sqlText);
        return result.IsSuccess && result.Statements != null 
            ? result.Statements.Count(s => !string.IsNullOrWhiteSpace(s.Statement))
            : 0;
    }

    /// <summary>
    /// Normalize multiple statements individually
    /// </summary>
    public static Dictionary<string, string> NormalizeStatements(string sqlText)
    {
        return SplitStatements(sqlText)
            .ToDictionary(statement => statement, CleanQuery);
    }

    private static void ExtractTablesFromJson(JsonElement element, HashSet<string> tables)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                foreach (var property in element.EnumerateObject())
                {
                    if (property.Name.Equals("relname", StringComparison.OrdinalIgnoreCase) && 
                        property.Value.ValueKind == JsonValueKind.String)
                    {
                        var tableName = property.Value.GetString();
                        if (!string.IsNullOrEmpty(tableName))
                            tables.Add(tableName);
                    }
                    else if (property.Value.ValueKind is JsonValueKind.Object or JsonValueKind.Array)
                    {
                        ExtractTablesFromJson(property.Value, tables);
                    }
                }
                break;
            
            case JsonValueKind.Array:
                foreach (var item in element.EnumerateArray())
                    ExtractTablesFromJson(item, tables);
                break;
        }
    }

    private static string? ExtractQueryTypeFromJson(JsonElement element)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                foreach (var property in element.EnumerateObject())
                {
                    if (!property.Name.Equals("stmt", StringComparison.OrdinalIgnoreCase) && property.Name.EndsWith("Stmt", StringComparison.OrdinalIgnoreCase))
                    {
                        // Replace("Stmt", "", StringComparison.OrdinalIgnoreCase) is unavailable on older TFMs; trim suffix manually.
                        var name = property.Name;
                        if (name.Length >= 4 && name.EndsWith("Stmt", StringComparison.OrdinalIgnoreCase))
                        {
                            name = name.Substring(0, name.Length - 4);
                        }
                        return name.ToUpperInvariant();
                    }

                    var result = property.Value.ValueKind switch
                    {
                        JsonValueKind.Object => ExtractQueryTypeFromJson(property.Value),
                        JsonValueKind.Array => property.Value.EnumerateArray()
                            .Select(ExtractQueryTypeFromJson)
                            .FirstOrDefault(r => r != null),
                        _ => null
                    };

                    if (result != null) return result;
                }
                break;

            case JsonValueKind.Array:
                return element.EnumerateArray()
                    .Select(ExtractQueryTypeFromJson)
                    .FirstOrDefault(r => r != null);
        }

        return null;
    }
}