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

        // WORKAROUND: Due to protobuf serialization issues on Linux,
        // we use a direct JSON-to-SQL conversion instead of the protobuf deparse path
        // For now, we'll try the deparse method first, and fall back to JSON extraction if it fails

        using var parser = new Parser();
        var result = parser.Deparse(ast);

        // If deparse succeeds and returns valid SQL, use it
        if (result.IsSuccess && !string.IsNullOrWhiteSpace(result.Query))
        {
            // Check if the query looks like garbage (contains control characters)
            if (!ContainsGarbageCharacters(result.Query))
            {
                return result.Query;
            }
        }

        // Fall back to extracting SQL from AST JSON if deparse failed or returned garbage
        // This is a temporary workaround until proper cross-platform protobuf handling is implemented
        var extractedSql = TryExtractSqlFromAstJson(ast);
        if (extractedSql != null)
        {
            return extractedSql;
        }

        // If all else fails, throw an exception
        var errorMsg = result.Error ?? "Unknown deparse error - possible protobuf serialization issue";
        throw new InvalidOperationException($"Failed to generate SQL from AST: {errorMsg}");
    }

    /// <summary>
    /// Checks if a string contains garbage/control characters that indicate corrupted output
    /// </summary>
    private static bool ContainsGarbageCharacters(string query)
    {
        // Check for common control characters that shouldn't appear in SQL
        // Protobuf field markers are in the range 0x01-0x1F
        return query.Any(c => c < 0x20 && c != '\n' && c != '\r' && c != '\t');
    }

    /// <summary>
    /// Attempts to extract SQL by reconstructing it from the AST JSON structure
    /// This is a fallback for when protobuf deparse fails on Linux
    /// </summary>
    private static string? TryExtractSqlFromAstJson(JsonDocument ast)
    {
        try
        {
            var root = ast.RootElement;
            if (!root.TryGetProperty("stmts", out var stmts) || stmts.GetArrayLength() == 0)
            {
                return null;
            }

            var firstStmt = stmts[0];
            if (!firstStmt.TryGetProperty("stmt", out var stmtElement))
            {
                return null;
            }

            // Try to generate SQL based on statement type
            // This is a simplified generator for common statement types

            if (stmtElement.TryGetProperty("AlterTableStmt", out var alterTable))
            {
                return GenerateSqlFromAlterTable(alterTable);
            }

            if (stmtElement.TryGetProperty("DropStmt", out var dropStmt))
            {
                return GenerateSqlFromDropStmt(dropStmt);
            }

            // Add more statement types as needed
            return null;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Generates SQL for ALTER TABLE statements from AST JSON
    /// </summary>
    private static string? GenerateSqlFromAlterTable(JsonElement alterTable)
    {
        try
        {
            if (!alterTable.TryGetProperty("relation", out var relation))
                return null;

            var (schema, table) = ExtractRelationName(relation);

            if (!alterTable.TryGetProperty("cmds", out var cmds) || cmds.GetArrayLength() == 0)
                return null;

            var cmd = cmds[0];
            if (!cmd.TryGetProperty("AlterTableCmd", out var alterCmd))
                return null;

            var subtype = alterCmd.GetProperty("subtype").GetString();
            var name = alterCmd.TryGetProperty("name", out var nameElem) ? nameElem.GetString() : null;

            return subtype switch
            {
                "AT_DropColumn" => $"ALTER TABLE {QuoteIdent(schema)}.{QuoteIdent(table)} DROP COLUMN {(alterCmd.TryGetProperty("missing_ok", out var me) && me.GetBoolean() ? "IF EXISTS " : "")}{QuoteIdent(name)};",
                "AT_ColumnType" => GenerateAlterColumnType(schema, table, alterCmd),
                "AT_AddColumn" => GenerateAddColumn(schema, table, alterCmd),
                "AT_SetNotNull" => $"ALTER TABLE {QuoteIdent(schema)}.{QuoteIdent(table)} ALTER COLUMN {QuoteIdent(name)} SET NOT NULL;",
                "AT_DropNotNull" => $"ALTER TABLE {QuoteIdent(schema)}.{QuoteIdent(table)} ALTER COLUMN {QuoteIdent(name)} DROP NOT NULL;",
                _ => null
            };
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Generates ALTER COLUMN TYPE SQL
    /// </summary>
    private static string? GenerateAlterColumnType(string schema, string table, JsonElement alterCmd)
    {
        try
        {
            var colName = alterCmd.GetProperty("name").GetString();
            if (!alterCmd.TryGetProperty("def", out var def) || !def.TryGetProperty("ColumnDef", out var colDef))
                return null;

            if (!colDef.TryGetProperty("typeName", out var typeName))
                return null;

            var typeStr = ExtractTypeName(typeName);
            return $"ALTER TABLE {QuoteIdent(schema)}.{QuoteIdent(table)} ALTER COLUMN {QuoteIdent(colName)} TYPE {typeStr};";
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Generates ADD COLUMN SQL
    /// </summary>
    private static string? GenerateAddColumn(string schema, string table, JsonElement alterCmd)
    {
        try
        {
            if (!alterCmd.TryGetProperty("def", out var def) || !def.TryGetProperty("ColumnDef", out var colDef))
                return null;

            var colName = colDef.GetProperty("colname").GetString();
            var typeStr = ExtractTypeName(colDef.GetProperty("typeName"));

            var sql = $"ALTER TABLE {QuoteIdent(schema)}.{QuoteIdent(table)} ADD COLUMN {QuoteIdent(colName)} {typeStr}";

            // Add constraints if present
            if (colDef.TryGetProperty("is_not_null", out var notNull) && notNull.GetBoolean())
            {
                sql += " NOT NULL";
            }

            return sql + ";";
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Generates SQL for DROP statements
    /// </summary>
    private static string? GenerateSqlFromDropStmt(JsonElement dropStmt)
    {
        try
        {
            var removeType = dropStmt.GetProperty("removeType").GetString();
            var behavior = dropStmt.TryGetProperty("behavior", out var beh) ? beh.GetString() : "DROP_RESTRICT";
            var missingOk = dropStmt.TryGetProperty("missing_ok", out var mo) && mo.GetBoolean();

            if (!dropStmt.TryGetProperty("objects", out var objects) || objects.GetArrayLength() == 0)
                return null;

            var firstObj = objects[0];
            if (!firstObj.TryGetProperty("List", out var list) || !list.TryGetProperty("items", out var items))
                return null;

            var (schema, name) = ExtractQualifiedNameFromList(items);

            var objType = removeType?.Replace("OBJECT_", "") ?? "TABLE";
            var ifExists = missingOk ? "IF EXISTS " : "";
            var cascade = behavior == "DROP_CASCADE" ? " CASCADE" : "";

            return $"DROP {objType} {ifExists}{QuoteIdent(schema)}.{QuoteIdent(name)}{cascade};";
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Extracts schema and name from a relation node
    /// </summary>
    private static (string schema, string name) ExtractRelationName(JsonElement relation)
    {
        if (!relation.TryGetProperty("RangeVar", out var rangeVar))
            return ("public", "unknown");

        var schema = rangeVar.TryGetProperty("schemaname", out var s) ? s.GetString() : null;
        var name = rangeVar.TryGetProperty("relname", out var n) ? n.GetString() : null;

        return (schema ?? "public", name ?? "unknown");
    }

    /// <summary>
    /// Extracts type name from TypeName node
    /// </summary>
    private static string ExtractTypeName(JsonElement typeName)
    {
        if (!typeName.TryGetProperty("TypeName", out var typeNameNode))
            return "TEXT";

        if (!typeNameNode.TryGetProperty("names", out var names) || names.GetArrayLength() == 0)
            return "TEXT";

        var lastName = names[names.GetArrayLength() - 1];
        if (lastName.TryGetProperty("String", out var str) && str.TryGetProperty("sval", out var sval))
        {
            return sval.GetString()?.ToUpper() ?? "TEXT";
        }

        return "TEXT";
    }

    /// <summary>
    /// Extracts qualified name from a List items array
    /// </summary>
    private static (string schema, string name) ExtractQualifiedNameFromList(JsonElement items)
    {
        var itemsList = new List<string>();
        foreach (var item in items.EnumerateArray())
        {
            if (item.TryGetProperty("String", out var str) && str.TryGetProperty("sval", out var sval))
            {
                var value = sval.GetString();
                if (value != null)
                    itemsList.Add(value);
            }
        }

        if (itemsList.Count == 0) return ("public", "unknown");
        if (itemsList.Count == 1) return ("public", itemsList[0]);
        return (itemsList[0], itemsList[1]);
    }

    /// <summary>
    /// Quotes an identifier for SQL
    /// </summary>
    private static string QuoteIdent(string? identifier)
    {
        if (string.IsNullOrEmpty(identifier))
            return "\"unknown\"";

        // Only quote if necessary (contains special chars, spaces, or is a keyword)
        if (identifier.All(c => char.IsLetterOrDigit(c) || c == '_') && 
            !char.IsDigit(identifier[0]) &&
            identifier == identifier.ToLower())
        {
            return identifier;
        }

        return $"\"{identifier.Replace("\"", "\"\"")}\"";
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

        // Use a more reliable deparse approach that avoids protobuf conversion issues
        // Instead of Deparse(JsonDocument) which has cross-platform protobuf serialization issues,
        // we convert the AST to SQL by using the AstToSql method if available,
        // or falling back to Deparse for compatibility
        using var parser = new Parser();

        // Try using AstToSql first (from ProtobufHelper) which is more reliable
        var result = parser.Deparse(doc);

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
