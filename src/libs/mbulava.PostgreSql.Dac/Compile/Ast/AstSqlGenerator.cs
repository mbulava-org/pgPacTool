using Npgquery;
using System.Text.Json;

namespace mbulava.PostgreSql.Dac.Compile.Ast;

/// <summary>
/// Generates SQL from PostgreSQL Abstract Syntax Trees using Npgquery deparse functionality.
/// This replaces string-template SQL generation with AST-based generation for reliability and type safety.
/// </summary>
public static class AstSqlGenerator
{
    /// <summary>
    /// Generates SQL from a parsed AST JsonDocument.
    /// </summary>
    /// <param name="ast">The AST as a JsonDocument (from Parser.Parse().ParseTree)</param>
    /// <returns>Generated SQL statement</returns>
    /// <exception cref="InvalidOperationException">If deparse fails</exception>
    public static string Generate(JsonDocument ast)
    {
        ArgumentNullException.ThrowIfNull(ast);

        using var parser = new Parser();
        var result = parser.Deparse(ast);
        
        if (!result.IsSuccess)
        {
            var errorMsg = result.Error ?? "Unknown deparse error";
            throw new InvalidOperationException($"Failed to deparse AST: {errorMsg}");
        }
        
        if (string.IsNullOrWhiteSpace(result.Query))
        {
            throw new InvalidOperationException("Deparse returned empty query");
        }
        
        return result.Query;
    }

    /// <summary>
    /// Generates SQL from a JsonElement AST node.
    /// </summary>
    /// <param name="astElement">AST element</param>
    /// <returns>Generated SQL statement</returns>
    public static string Generate(JsonElement astElement)
    {
        if (astElement.ValueKind == JsonValueKind.Undefined)
        {
            throw new ArgumentException("AST element must not be an undefined JsonElement.", nameof(astElement));
        }

        // Wrap in JsonDocument structure if not already wrapped
        var json = astElement.GetRawText();
        using var doc = JsonDocument.Parse(json);
        
        return Generate(doc);
    }

    /// <summary>
    /// Validates that SQL can be round-tripped through AST.
    /// Useful for testing and validation.
    /// </summary>
    /// <param name="sql">Original SQL</param>
    /// <param name="generatedSql">Output parameter with generated SQL</param>
    /// <returns>True if round-trip is successful</returns>
    public static bool TryRoundTrip(string sql, out string? generatedSql)
    {
        generatedSql = null;
        
        try
        {
            using var parser = new Parser();
            var parseResult = parser.Parse(sql);
            
            if (!parseResult.IsSuccess || parseResult.ParseTree == null)
            {
                return false;
            }
            
            generatedSql = Generate(parseResult.ParseTree);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Parses SQL and returns the AST for manipulation.
    /// </summary>
    /// <param name="sql">SQL to parse</param>
    /// <returns>AST JsonDocument</returns>
    /// <exception cref="InvalidOperationException">If parsing fails</exception>
    public static JsonDocument ParseToAst(string sql)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sql);

        using var parser = new Parser();
        var result = parser.Parse(sql);
        
        if (!result.IsSuccess || result.ParseTree == null)
        {
            var errorMsg = result.Error ?? "Unknown parse error";
            throw new InvalidOperationException($"Failed to parse SQL: {errorMsg}");
        }
        
        // Clone the JsonDocument to return ownership to caller
        var json = result.ParseTree.RootElement.GetRawText();
        return JsonDocument.Parse(json);
    }

    /// <summary>
    /// Performs a complete round-trip: SQL → AST → SQL
    /// Useful for normalizing SQL statements.
    /// </summary>
    /// <param name="sql">Original SQL</param>
    /// <returns>Normalized SQL generated from AST</returns>
    public static string Normalize(string sql)
    {
        using var ast = ParseToAst(sql);
        return Generate(ast);
    }
}
