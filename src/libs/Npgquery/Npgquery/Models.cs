using System.Text.Json;
using System.Text.Json.Serialization;
using Npgquery.Native;

namespace Npgquery;

/// <summary>
/// Base result type for all query operations
/// </summary>
public abstract record QueryResultBase
{
    /// <summary>
    /// The original query/input that was processed
    /// </summary>
    [JsonPropertyName("query")]
    public string Query { get; init; } = string.Empty;

    /// <summary>
    /// Any error that occurred during the operation
    /// </summary>
    [JsonPropertyName("error")]
    public string? Error { get; init; }

    /// <summary>
    /// Indicates whether the operation was successful
    /// </summary>
    [JsonIgnore]
    public bool IsSuccess => string.IsNullOrEmpty(Error);

    /// <summary>
    /// Indicates whether the operation failed
    /// </summary>
    [JsonIgnore]
    public bool IsError => !IsSuccess;
}

/// <summary>
/// Represents the result of parsing a PostgreSQL query
/// </summary>
public sealed record ParseResult : QueryResultBase
{
    /// <summary>
    /// The parsed query as a JSON document representing the parse tree
    /// </summary>
    [JsonPropertyName("parse_tree")]
    public JsonDocument? ParseTree { get; init; }
}

/// <summary>
/// Represents the result of normalizing a PostgreSQL query
/// </summary>
public sealed record NormalizeResult : QueryResultBase
{
    /// <summary>
    /// The normalized query string
    /// </summary>
    [JsonPropertyName("normalized_query")]
    public string? NormalizedQuery { get; init; }
}

/// <summary>
/// Represents the result of fingerprinting a PostgreSQL query
/// </summary>
public sealed record FingerprintResult : QueryResultBase
{
    /// <summary>
    /// The fingerprint hash of the query
    /// </summary>
    [JsonPropertyName("fingerprint")]
    public string? Fingerprint { get; init; }
}

/// <summary>
/// Options for parsing PostgreSQL queries
/// </summary>
public sealed record ParseOptions
{
    /// <summary>
    /// Whether to include location information in the parse tree
    /// </summary>
    public bool IncludeLocations { get; init; } = false;

    /// <summary>
    /// The PostgreSQL version to use for parsing (default is latest)
    /// </summary>
    public int PostgreSqlVersion { get; init; } = 160000; // PostgreSQL 16

    /// <summary>
    /// Default parse options
    /// </summary>
    public static readonly ParseOptions Default = new();
}

/// <summary>
/// Represents the result of deparsing a PostgreSQL AST back to SQL
/// </summary>
public sealed record DeparseResult
{
    /// <summary>
    /// The deparsed SQL query
    /// </summary>
    [JsonPropertyName("query")]
    public string? Query { get; init; }

    /// <summary>
    /// The original AST that was deparsed
    /// </summary>
    [JsonPropertyName("ast")]
    public string Ast { get; init; } = string.Empty;

    /// <summary>
    /// Any error that occurred during the operation
    /// </summary>
    [JsonPropertyName("error")]
    public string? Error { get; init; }

    /// <summary>
    /// Indicates whether the operation was successful
    /// </summary>
    [JsonIgnore]
    public bool IsSuccess => string.IsNullOrEmpty(Error);

    /// <summary>
    /// Indicates whether the operation failed
    /// </summary>
    [JsonIgnore]
    public bool IsError => !IsSuccess;
}

/// <summary>
/// Represents a single SQL statement from splitting
/// </summary>
public sealed record SqlStatement
{
    /// <summary>
    /// The SQL statement text
    /// </summary>
    [JsonPropertyName("stmt")]
    public string? Statement { get; init; }

    /// <summary>
    /// Starting position in the original text
    /// </summary>
    [JsonPropertyName("stmt_location")]
    public int Location { get; init; }

    /// <summary>
    /// Length of the statement
    /// </summary>
    [JsonPropertyName("stmt_len")]
    public int Length { get; init; }
}

/// <summary>
/// Represents the result of splitting multiple PostgreSQL statements
/// </summary>
public sealed record SplitResult : QueryResultBase
{
    /// <summary>
    /// The individual SQL statements
    /// </summary>
    [JsonPropertyName("stmts")]
    public SqlStatement[]? Statements { get; init; }
}

/// <summary>
/// Represents a single token from scanning
/// </summary>
public sealed record SqlToken
{
    /// <summary>
    /// Token type (numeric identifier)
    /// </summary>
    [JsonPropertyName("token")]
    public int Token { get; init; }

    /// <summary>
    /// Token type name (e.g., "SELECT", "IDENT", etc.)
    /// </summary>
    [JsonPropertyName("token_kind")]
    public string? TokenKind { get; init; }

    /// <summary>
    /// Keyword kind (e.g., "RESERVED_KEYWORD", "UNRESERVED_KEYWORD", etc.)
    /// </summary>
    [JsonPropertyName("keyword_kind")]
    public string? KeywordKind { get; init; }

    /// <summary>
    /// Starting position in the original text
    /// </summary>
    [JsonPropertyName("start")]
    public int Start { get; init; }

    /// <summary>
    /// Ending position in the original text
    /// </summary>
    [JsonPropertyName("end")]
    public int End { get; init; }

    /// <summary>
    /// The actual text of the token
    /// </summary>
    [JsonPropertyName("text")]
    public string? Text { get; init; }
}

/// <summary>
/// Represents the result of scanning/tokenizing a PostgreSQL query
/// </summary>
public record ScanResult : QueryResultBase
{
    /// <summary>
    /// The PostgreSQL version number
    /// </summary>
    [JsonPropertyName("version")]
    public int? Version { get; init; }

    /// <summary>
    /// The tokens found in the query
    /// </summary>
    [JsonPropertyName("tokens")]
    public SqlToken[]? Tokens { get; init; }

    /// <summary>
    /// Standard error output from the scanner
    /// </summary>
    [JsonPropertyName("stderr")]
    public string? Stderr { get; init; }
}

/// <summary>
/// Represents the result of parsing PL/pgSQL code
/// </summary>
public sealed record PlpgsqlParseResult : QueryResultBase
{
    /// <summary>
    /// The parsed PL/pgSQL as a JSON string representing the parse tree
    /// </summary>
    [JsonPropertyName("parse_tree")]
    public string? ParseTree { get; init; }
}

/// <summary>
/// Enhanced scan result that includes both processed tokens and raw protobuf data
/// </summary>
public sealed record EnhancedScanResult : ScanResult
{
    /// <summary>
    /// Raw protobuf scan result for advanced processing
    /// </summary>
    [JsonIgnore]
    public PgQuery.ScanResult? ProtobufScanResult { get; init; }

    /// <summary>
    /// Convert the protobuf scan result to JSON
    /// </summary>
    /// <param name="formatted">Whether to format the JSON with indentation</param>
    /// <returns>JSON representation of the scan result</returns>
    public string? ToProtobufJson(bool formatted = false) =>
        ProtobufScanResult != null 
            ? ProtobufHelper.ToJson(ProtobufScanResult, formatted) 
            : null;
}

/// <summary>
/// Represents the result of parsing a PostgreSQL query to protobuf format
/// </summary>
public sealed record ProtobufParseResult : QueryResultBase
{
    /// <summary>
    /// The protobuf parse tree (for internal use)
    /// </summary>
    internal PgQueryProtobuf? ParseTree { get; init; }
    
    /// <summary>
    /// The native result (for cleanup)
    /// </summary>
    internal PgQueryProtobufParseResult? NativeResult { get; init; }
}