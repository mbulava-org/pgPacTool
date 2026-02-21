using Google.Protobuf;
using PgQuery;
using System.Runtime.InteropServices;
using System.Text.Json;
namespace Npgquery.Native;

/// <summary>
/// Helper class for protobuf operations with libpg_query
/// </summary>
internal static class ProtobufHelper
{
    /// <summary>
    /// Deserialize scan result from protobuf data
    /// </summary>
    /// <param name="protobufData">Raw protobuf data</param>
    /// <param name="originalQuery">Original query string for text extraction</param>
    /// <returns>Processed scan result</returns>
    internal static NativeMethods.NativeScanResult DeserializeScanResult(byte[] protobufData, string originalQuery)
    {
        try
        {
            var scanResult = PgQuery.ScanResult.Parser.ParseFrom(protobufData);
            
            return new NativeMethods.NativeScanResult
            {
                Version = scanResult.Version,
                Tokens = ConvertProtobufTokensToSqlTokens(scanResult.Tokens, originalQuery),
                Error = null,
                Stderr = null
            };
        }
        catch (Exception ex)
        {
            return new NativeMethods.NativeScanResult
            {
                Version = null,
                Tokens = null,
                Error = $"Failed to deserialize protobuf scan result: {ex.Message}",
                Stderr = null
            };
        }
    }

    /// <summary>
    /// Deserialize parse result from protobuf data
    /// </summary>
    /// <param name="protobufData">Raw protobuf data</param>
    /// <returns>Deserialized parse result</returns>
    internal static PgQuery.ParseResult? DeserializeParseResult(byte[] protobufData)
    {
        try
        {
            return PgQuery.ParseResult.Parser.ParseFrom(protobufData);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Convert protobuf ScanTokens to SqlTokens
    /// </summary>
    /// <param name="protobufTokens">Protobuf scan tokens</param>
    /// <param name="originalQuery">Original query for text extraction</param>
    /// <returns>Array of SqlToken objects</returns>
    private static SqlToken[] ConvertProtobufTokensToSqlTokens(
        Google.Protobuf.Collections.RepeatedField<ScanToken> protobufTokens, 
        string originalQuery)
    {
        var tokens = new SqlToken[protobufTokens.Count];
        
        for (int i = 0; i < protobufTokens.Count; i++)
        {
            var protobufToken = protobufTokens[i];
            
            tokens[i] = new SqlToken
            {
                Token = (int)protobufToken.Token,
                TokenKind = protobufToken.Token.ToString(),
                KeywordKind = protobufToken.KeywordKind.ToString(),
                Start = protobufToken.Start,
                End = protobufToken.End,
                Text = ExtractTokenText(originalQuery, protobufToken.Start, protobufToken.End)
            };
        }
        
        return tokens;
    }

    /// <summary>
    /// Extract token text from the original query using start and end positions
    /// </summary>
    /// <param name="query">Original query string</param>
    /// <param name="start">Start position</param>
    /// <param name="end">End position</param>
    /// <returns>Extracted token text</returns>
    private static string ExtractTokenText(string query, int start, int end)
    {
        if (start < 0 || end < 0 || start >= query.Length || end > query.Length || start >= end)
        {
            return string.Empty;
        }
        
        return query.Substring(start, end - start);
    }

    /// <summary>
    /// Convert protobuf data pointer to byte array
    /// </summary>
    /// <param name="protobuf">Native protobuf structure</param>
    /// <returns>Byte array containing protobuf data</returns>
    internal static byte[] ExtractProtobufData(PgQueryProtobuf protobuf)
    {
        if (protobuf.data == IntPtr.Zero || protobuf.len == UIntPtr.Zero)
        {
            return Array.Empty<byte>();
        }

        var length = (int)protobuf.len;
        var data = new byte[length];
        Marshal.Copy(protobuf.data, data, 0, length);
        return data;
    }

    /// <summary>
    /// Convert a protobuf ParseResult to JSON string
    /// </summary>
    /// <param name="parseResult">The protobuf ParseResult</param>
    /// <param name="formatted">Whether to format the JSON with indentation</param>
    /// <returns>JSON representation of the parse tree</returns>

    public static string ToJson(PgQuery.ParseResult parseResult, bool formatted = false) {
        var jsonFormatter = new JsonFormatter(JsonFormatter.Settings.Default.WithFormatDefaultValues(true));
        var json = jsonFormatter.Format(parseResult);

        if (formatted) {
            var document = JsonDocument.Parse(json);
            return JsonSerializer.Serialize(document, new JsonSerializerOptions { WriteIndented = true });
        }

        return json;
    }

    /// <summary>
    /// Convert a protobuf ScanResult to JSON string
    /// </summary>
    /// <param name="scanResult">The protobuf ScanResult</param>
    /// <param name="formatted">Whether to format the JSON with indentation</param>
    /// <returns>JSON representation of the scan result</returns>
    public static string ToJson(PgQuery.ScanResult scanResult, bool formatted = false) {
        var jsonFormatter = new JsonFormatter(JsonFormatter.Settings.Default.WithFormatDefaultValues(true));
        var json = jsonFormatter.Format(scanResult);

        if (formatted) {
            var document = JsonDocument.Parse(json);
            return JsonSerializer.Serialize(document, new JsonSerializerOptions { WriteIndented = true });
        }

        return json;
    }

    /// <summary>
    /// Parse JSON string back to ParseResult
    /// </summary>
    /// <param name="json">JSON representation of parse tree</param>
    /// <returns>Protobuf ParseResult object</returns>
    public static PgQuery.ParseResult ParseResultFromJson(string json) {
        var parser = new JsonParser(JsonParser.Settings.Default);
        return parser.Parse<PgQuery.ParseResult>(json);
    }

    /// <summary>
    /// Parse JSON string back to ScanResult
    /// </summary>
    /// <param name="json">JSON representation of scan result</param>
    /// <returns>Protobuf ScanResult object</returns>
    public static PgQuery.ScanResult ScanResultFromJson(string json) {
        var parser = new JsonParser(JsonParser.Settings.Default);
        return parser.Parse<PgQuery.ScanResult>(json);
    }

    /// <summary>
    /// Extract all SELECT statements from a ParseResult
    /// </summary>
    /// <param name="parseResult">The parse result to search</param>
    /// <returns>Collection of SelectStmt objects</returns>
    public static IEnumerable<SelectStmt> ExtractSelectStatements(PgQuery.ParseResult parseResult) {
        foreach (var stmt in parseResult.Stmts) {
            if (stmt.Stmt?.SelectStmt != null) {
                yield return stmt.Stmt.SelectStmt;
            }
        }
    }

    /// <summary>
    /// Extract all table names from a ParseResult
    /// </summary>
    /// <param name="parseResult">The parse result to search</param>
    /// <returns>Collection of table names</returns>
    public static IEnumerable<string> ExtractTableNames(PgQuery.ParseResult parseResult) {
        var tableNames = new HashSet<string>();

        foreach (var stmt in parseResult.Stmts) {
            ExtractTableNamesFromNode(stmt.Stmt, tableNames);
        }

        return tableNames;
    }

    /// <summary>
    /// Recursively extract table names from a node
    /// </summary>
    /// <param name="node">Node to search</param>
    /// <param name="tableNames">Set to collect table names</param>
    private static void ExtractTableNamesFromNode(Node node, HashSet<string> tableNames) {
        // This is a simplified implementation - a full implementation would need to
        // traverse all possible node types that could contain table references
        switch (node.NodeCase) {
            case Node.NodeOneofCase.RangeVar:
                if (!string.IsNullOrEmpty(node.RangeVar.Relname)) {
                    tableNames.Add(node.RangeVar.Relname);
                }
                break;

            case Node.NodeOneofCase.SelectStmt:
                var selectStmt = node.SelectStmt;
                foreach (var fromItem in selectStmt.FromClause) {
                    ExtractTableNamesFromNode(fromItem, tableNames);
                }
                break;

            case Node.NodeOneofCase.RawStmt:
                if (node.RawStmt.Stmt != null) {
                    ExtractTableNamesFromNode(node.RawStmt.Stmt, tableNames);
                }
                break;
        }
    }

    /// <summary>
    /// Get statement type from a RawStmt
    /// </summary>
    /// <param name="rawStmt">The statement to analyze</param>
    /// <returns>Statement type as string</returns>
    public static string GetStatementType(RawStmt rawStmt) {
        if (rawStmt.Stmt == null) return "UNKNOWN";

        return rawStmt.Stmt.NodeCase switch {
            Node.NodeOneofCase.SelectStmt => "SELECT",
            Node.NodeOneofCase.InsertStmt => "INSERT",
            Node.NodeOneofCase.UpdateStmt => "UPDATE",
            Node.NodeOneofCase.DeleteStmt => "DELETE",
            Node.NodeOneofCase.CreateStmt => "CREATE",
            Node.NodeOneofCase.AlterTableStmt => "ALTER",
            Node.NodeOneofCase.DropStmt => "DROP",
            Node.NodeOneofCase.MergeStmt => "MERGE",
            Node.NodeOneofCase.CallStmt => "CALL",
            Node.NodeOneofCase.DoStmt => "DO",
            _ => rawStmt.Stmt.NodeCase.ToString().Replace("Stmt", "").ToUpperInvariant()
        };
    }

    /// <summary>
    /// Count the number of statements in a ParseResult
    /// </summary>
    /// <param name="parseResult">The parse result</param>
    /// <returns>Number of statements</returns>
    public static int CountStatements(PgQuery.ParseResult parseResult) {
        return parseResult.Stmts.Count;
    }

    /// <summary>
    /// Check if a ParseResult contains any DDL statements
    /// </summary>
    /// <param name="parseResult">The parse result to check</param>
    /// <returns>True if contains DDL statements</returns>
    public static bool ContainsDdlStatements(PgQuery.ParseResult parseResult) {
        return parseResult.Stmts.Any(stmt => IsDdlStatement(stmt));
    }

    /// <summary>
    /// Check if a statement is a DDL statement
    /// </summary>
    /// <param name="rawStmt">The statement to check</param>
    /// <returns>True if it's a DDL statement</returns>
    private static bool IsDdlStatement(RawStmt rawStmt) {
        if (rawStmt.Stmt == null) return false;

        return rawStmt.Stmt.NodeCase switch {
            Node.NodeOneofCase.CreateStmt => true,
            Node.NodeOneofCase.AlterTableStmt => true,
            Node.NodeOneofCase.DropStmt => true,
            Node.NodeOneofCase.CreateSchemaStmt => true,
            Node.NodeOneofCase.CreateSeqStmt => true,
            Node.NodeOneofCase.CreateFunctionStmt => true,
            Node.NodeOneofCase.CreateTrigStmt => true,
            Node.NodeOneofCase.IndexStmt => true,
            _ => false
        };
    }
}

/// <summary>
/// Native protobuf structure for libpg_query
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct PgQueryProtobuf {
    public UIntPtr len;
    public IntPtr data;
}

/// <summary>
/// Native protobuf parse result structure for libpg_query
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct PgQueryProtobufParseResult {
    public PgQueryProtobuf parse_tree;
    public IntPtr error;
}