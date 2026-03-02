using Google.Protobuf;
using Npgquery.Native;
using System.Text.Json;

namespace Npgquery;

/// <summary>
/// Main PostgreSQL query parser class providing parsing, normalization, and fingerprinting functionality
/// </summary>
public sealed class Parser : IDisposable
{
    private bool _disposed;
    private readonly PostgreSqlVersion _version;

    /// <summary>
    /// Creates a new parser instance with the specified PostgreSQL version
    /// </summary>
    /// <param name="version">PostgreSQL version to use for parsing (default: Postgres16)</param>
    public Parser(PostgreSqlVersion version = PostgreSqlVersion.Postgres16)
    {
        _version = version;
        // Ensure the library is loaded and available on construction
        NativeLibraryLoader.GetLibraryHandle(_version);
    }

    /// <summary>
    /// Gets the PostgreSQL version this parser is using
    /// </summary>
    public PostgreSqlVersion Version => _version;

    /// <summary>
    /// Parse a PostgreSQL query into an Abstract Syntax Tree (AST)
    /// </summary>
    public ParseResult Parse(string query, ParseOptions? options = null)
    {
        ThrowIfDisposedOrNull(query);

        return ExecuteNativeOperation(query,
            q => NativeMethods.pg_query_parse(NativeMethods.StringToUtf8Bytes(q), _version),
            (result, q) =>
            {
                var error = ExtractError(result.error);
                if (error != null)
                {
                    return new ParseResult { Query = q, Error = error };
                }

                if (result.tree != IntPtr.Zero)
                {
                    var parseTreeJson = NativeMethods.PtrToString(result.tree);
                    if (!string.IsNullOrEmpty(parseTreeJson))
                    {
                        return new ParseResult 
                        { 
                            Query = q,
                            ParseTree = JsonDocument.Parse(parseTreeJson)
                        };
                    }
                }

                return new ParseResult { Query = q, Error = "Failed to parse query: no result from parser" };
            },
            result => NativeMethods.pg_query_free_parse_result(result, _version));
    }

    /// <summary>
    /// Normalize a PostgreSQL query by removing comments and standardizing formatting
    /// </summary>
    public NormalizeResult Normalize(string query)
    {
        ThrowIfDisposedOrNull(query);

        return ExecuteNativeOperation(query, 
            q => NativeMethods.pg_query_normalize(NativeMethods.StringToUtf8Bytes(q), _version),
            (result, q) => new NormalizeResult
            {
                Query = q,
                NormalizedQuery = NativeMethods.PtrToString(result.normalized_query),
                Error = ExtractError(result.error)
            },
            result => NativeMethods.pg_query_free_normalize_result(result, _version));
    }

    /// <summary>
    /// Generate a fingerprint for a PostgreSQL query for similarity comparison
    /// </summary>
    public FingerprintResult Fingerprint(string query)
    {
        ThrowIfDisposedOrNull(query);

        return ExecuteNativeOperation(query,
            q => NativeMethods.pg_query_fingerprint(NativeMethods.StringToUtf8Bytes(q), _version),
            (result, q) => new FingerprintResult
            {
                Query = q,
                Fingerprint = NativeMethods.PtrToString(result.fingerprint_str),
                Error = ExtractError(result.error)
            },
            result => NativeMethods.pg_query_free_fingerprint_result(result, _version));
    }

    /// <summary>
    /// Split a string containing multiple PostgreSQL statements
    /// </summary>
    public SplitResult Split(string query)
    {
        ThrowIfDisposedOrNull(query);

        return ExecuteNativeOperation(query,
            q => NativeMethods.pg_query_split_with_parser(NativeMethods.StringToUtf8Bytes(q), _version),
            (result, q) =>
            {
                var stmts = NativeMethods.MarshalSplitStmts(result);
                var statements = stmts.Select(stmt => new SqlStatement
                {
                    Location = stmt.stmt_location,
                    Length = stmt.stmt_len,
                    Statement = q.Substring(stmt.stmt_location, stmt.stmt_len)
                }).ToArray();

                return new SplitResult
                {
                    Query = q,
                    Statements = statements,
                    Error = ExtractError(result.error)
                };
            },
            result => NativeMethods.pg_query_free_split_result(result, _version));
    }

    /// <summary>
    /// Scan/tokenize a PostgreSQL query
    /// </summary>
    public ScanResult Scan(string query)
    {
        ThrowIfDisposedOrNull(query);

        return ExecuteNativeOperation(query,
            q => NativeMethods.pg_query_scan(NativeMethods.StringToUtf8Bytes(q), _version),
            (result, q) =>
            {
                var processed = NativeMethods.ProcessScanResult(result, q);
                return new ScanResult
                {
                    Query = q,
                    Version = processed.Version,
                    Tokens = processed.Tokens,
                    Error = processed.Error,
                    Stderr = processed.Stderr
                };
            },
            result => NativeMethods.pg_query_free_scan_result(result, _version));
    }

    /// <summary>
    /// Scan/tokenize a PostgreSQL query with enhanced protobuf support
    /// </summary>
    public EnhancedScanResult ScanWithProtobuf(string query)
    {
        ThrowIfDisposedOrNull(query);

        return ExecuteNativeOperation(query,
            q => NativeMethods.pg_query_scan(NativeMethods.StringToUtf8Bytes(q), _version),
            (result, q) =>
            {
                var processed = NativeMethods.ProcessScanResult(result, q);
                PgQuery.ScanResult? protobufResult = null;

                if (result.pbuf.data != IntPtr.Zero && result.pbuf.len != UIntPtr.Zero)
                {
                    try
                    {
                        var protobufData = ProtobufHelper.ExtractProtobufData(result.pbuf);
                        protobufResult = PgQuery.ScanResult.Parser.ParseFrom(protobufData);
                    }
                    catch { /* Ignore protobuf parsing errors */ }
                }

                return new EnhancedScanResult
                {
                    Query = q,
                    Version = processed.Version,
                    Tokens = processed.Tokens,
                    Error = processed.Error,
                    Stderr = processed.Stderr,
                    ProtobufScanResult = protobufResult
                };
            },
            result => NativeMethods.pg_query_free_scan_result(result, _version));
    }

    /// <summary>
    /// Parse PL/pgSQL code into an Abstract Syntax Tree (AST)
    /// </summary>
    public PlpgsqlParseResult ParsePlpgsql(string plpgsqlCode)
    {
        ThrowIfDisposedOrNull(plpgsqlCode);

        return ExecuteNativeOperation(plpgsqlCode,
            q => NativeMethods.pg_query_parse_plpgsql(NativeMethods.StringToUtf8Bytes(q), _version),
            (result, q) => new PlpgsqlParseResult
            {
                Query = q,
                ParseTree = NativeMethods.PtrToString(result.tree),
                Error = ExtractError(result.error)
            },
            result => NativeMethods.pg_query_free_plpgsql_parse_result(result, _version));
    }

    /// <summary>
    /// Deparse a PostgreSQL AST back to SQL
    /// </summary>
    public DeparseResult Deparse(JsonDocument parseTree)
    {
        if (_disposed) throw new ObjectDisposedException(nameof(Parser));
        if (parseTree is null) throw new ArgumentNullException(nameof(parseTree));

        try
        {
            var json = parseTree.RootElement.GetRawText();
            var protoParseResult = ProtobufHelper.ParseResultFromJson(json);
            var protoBytes = protoParseResult.ToByteArray();
            var protoStruct = NativeMethods.AllocPgQueryProtobuf(protoBytes);

            try
            {
                var deparseResult = NativeMethods.pg_query_deparse_protobuf(protoStruct, _version);
                try
                {
                    return new DeparseResult
                    {
                        Ast = parseTree.RootElement.ToString(),
                        Query = NativeMethods.PtrToString(deparseResult.query),
                        Error = ExtractError(deparseResult.error)
                    };
                }
                finally
                {
                    NativeMethods.pg_query_free_deparse_result(deparseResult, _version);
                }
            }
            finally
            {
                NativeMethods.FreePgQueryProtobuf(protoStruct);
            }
        }
        catch (Exception ex)
        {
            return new DeparseResult
            {
                Ast = parseTree.RootElement.ToString(),
                Error = $"Native library error: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// Parse a PostgreSQL query into protobuf format
    /// </summary>
    public ProtobufParseResult ParseProtobuf(string query)
    {
        ThrowIfDisposedOrNull(query);

        try
        {
            var result = NativeMethods.pg_query_parse_protobuf(NativeMethods.StringToUtf8Bytes(query));
            try
            {
                var error = ExtractError(result.error);

                if (error != null)
                {
                    return new ProtobufParseResult
                    {
                        Query = query,
                        ProtobufData = null,
                        Error = error
                    };
                }

                // Copy the protobuf data from native memory immediately
                var protobufData = ProtobufHelper.ExtractProtobufData(result.parse_tree);

                return new ProtobufParseResult
                {
                    Query = query,
                    ProtobufData = protobufData,
                    Error = null
                };
            }
            finally
            {
                // Free the native result immediately after copying data
                NativeMethods.pg_query_free_protobuf_parse_result(result);
            }
        }
        catch (Exception ex)
        {
            return new ProtobufParseResult
            {
                Query = query,
                Error = $"Native library error: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// Deparse a PostgreSQL protobuf parse result back to SQL
    /// </summary>
    public DeparseResult DeparseProtobuf(ProtobufParseResult parseResult)
    {
        if (_disposed) throw new ObjectDisposedException(nameof(Parser));
        if (parseResult is null) throw new ArgumentNullException(nameof(parseResult));

        if (parseResult.IsError || parseResult.ProtobufData == null || parseResult.ProtobufData.Length == 0)
        {
            return new DeparseResult
            {
                Ast = "",
                Error = parseResult.Error ?? "Cannot deparse an error result or null parse tree"
            };
        }

        try
        {
            // Allocate a new PgQueryProtobuf structure from our byte array
            var protoStruct = NativeMethods.AllocPgQueryProtobuf(parseResult.ProtobufData);
            try
            {
                var deparseResult = NativeMethods.pg_query_deparse_protobuf(protoStruct);
                try
                {
                    return new DeparseResult
                    {
                        Ast = "",
                        Query = NativeMethods.PtrToString(deparseResult.query),
                        Error = ExtractError(deparseResult.error)
                    };
                }
                finally
                {
                    NativeMethods.pg_query_free_deparse_result(deparseResult);
                }
            }
            finally
            {
                // Free our allocated protobuf structure
                NativeMethods.FreePgQueryProtobuf(protoStruct);
            }
        }
        catch (Exception ex)
        {
            return new DeparseResult
            {
                Ast = "",
                Error = $"Native library error: {ex.Message}"
            };
        }
    }

    // Convenience methods
    public T? ParseAs<T>(string query, ParseOptions? options = null) where T : class
    {
        var result = Parse(query, options);
        return result.IsError || result.ParseTree is null ? null : result.ParseTree as T;
    }

    public bool IsValid(string query) => Parse(query).IsSuccess;
    public string? GetError(string query) => Parse(query).Error;
    public void Dispose() => _disposed = true;

    // Static factory methods
    public static ParseResult QuickParse(string query, ParseOptions? options = null) => 
        ExecuteWithInstance(parser => parser.Parse(query, options));

    public static NormalizeResult QuickNormalize(string query) => 
        ExecuteWithInstance(parser => parser.Normalize(query));

    public static FingerprintResult QuickFingerprint(string query) => 
        ExecuteWithInstance(parser => parser.Fingerprint(query));

    public static DeparseResult QuickDeparse(JsonDocument parseTree) => 
        ExecuteWithInstance(parser => parser.Deparse(parseTree));

    public static SplitResult QuickSplit(string query) => 
        ExecuteWithInstance(parser => parser.Split(query));

    public static ScanResult QuickScan(string query) => 
        ExecuteWithInstance(parser => parser.Scan(query));

    public static PlpgsqlParseResult QuickParsePlpgsql(string plpgsqlCode) => 
        ExecuteWithInstance(parser => parser.ParsePlpgsql(plpgsqlCode));

    public static EnhancedScanResult QuickScanWithProtobuf(string query) => 
        ExecuteWithInstance(parser => parser.ScanWithProtobuf(query));

    // Helper methods
    private void ThrowIfDisposedOrNull(string parameter)
    {
        if (_disposed) throw new ObjectDisposedException(nameof(Parser));
        if (parameter is null) throw new ArgumentNullException(nameof(parameter));
    }

    private static string? ExtractError(IntPtr errorPtr)
    {
        if (errorPtr == IntPtr.Zero) return null;
        
        var errorStruct = NativeMethods.MarshalError(errorPtr);
        return errorStruct?.message != IntPtr.Zero 
            ? NativeMethods.PtrToString(errorStruct.Value.message) ?? "Unknown error"
            : "Unknown error";
    }

    private static T ExecuteWithInstance<T>(Func<Parser, T> action)
    {
        using var parser = new Parser();
        return action(parser);
    }

    private T ExecuteNativeOperation<TNative, T>(
        string query,
        Func<string, TNative> nativeCall,
        Func<TNative, string, T> resultBuilder,
        Action<TNative> freeResult) where T : QueryResultBase, new()
    {
        try
        {
            var result = nativeCall(query);
            try
            {
                return resultBuilder(result, query);
            }
            finally
            {
                freeResult(result);
            }
        }
        catch (Exception ex)
        {
            return new T { Query = query, Error = $"Native library error: {ex.Message}" };
        }
    }
}
