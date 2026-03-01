using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using System.Text;

namespace Npgquery.Native;

/// <summary>
/// Native interop for libpg_query with multi-version support
/// </summary>
internal static unsafe class NativeMethods
{
    #region Native Structures

    [StructLayout(LayoutKind.Sequential)]
    internal struct PgQueryError
    {
        public IntPtr message;      // char*
        public IntPtr funcname;     // char*
        public IntPtr filename;     // char*
        public int lineno;
        public int cursorpos;
        public IntPtr context;      // char*
    }

    /// <summary>
    /// Result structure from libpg_query
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    internal struct PgQueryParseResult
    {
        public IntPtr tree;
        public IntPtr stderr_buffer;
        public IntPtr error;
    }

    /// <summary>
    /// Normalize result structure from libpg_query
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    internal struct PgQueryNormalizeResult
    {
        public IntPtr normalized_query;
        public IntPtr error;
    }

    /// <summary>
    /// Fingerprint result structure from libpg_query
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    internal struct PgQueryFingerprintResult
    {
        public ulong fingerprint;
        public IntPtr fingerprint_str;
        public IntPtr stderr_buffer;
        public IntPtr error;
    }

    /// <summary>
    /// Deparse result structure from libpg_query
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    internal struct PgQueryDeparseResult
    {
        public IntPtr query;
        public IntPtr error;
    }

    /// <summary>
    /// Split statement structure from libpg_query
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    internal struct PgQuerySplitStmt
    {
        public int stmt_location;
        public int stmt_len;
    }

    /// <summary>
    /// Split result structure from libpg_query
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    internal struct PgQuerySplitResult
    {
        public IntPtr stmts;
        public int n_stmts;
        public IntPtr stderr_buffer;
        public IntPtr error;
    }

    /// <summary>
    /// Scan result structure from libpg_query
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    internal struct PgQueryScanResult
    {
        public PgQueryProtobuf pbuf;
        public IntPtr stderr_buffer;
        public IntPtr error;
    }

    /// <summary>
    /// PL/pgSQL parse result structure
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    internal struct PgQueryPlpgsqlParseResult
    {
        public IntPtr tree;
        public IntPtr error;
    }

    /// <summary>
    /// Internal processed scan result for native operations
    /// </summary>
    internal struct NativeScanResult
    {
        public int? Version { get; set; }
        public SqlToken[]? Tokens { get; set; }
        public string? Error { get; set; }
        public string? Stderr { get; set; }
    }

    #endregion

    #region Function Pointer Delegates

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate PgQueryParseResult ParseDelegate(byte[] input);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate PgQueryNormalizeResult NormalizeDelegate(byte[] input);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate PgQueryFingerprintResult FingerprintDelegate(byte[] input);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate PgQueryDeparseResult DeparseDelegate(byte[] input);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate PgQueryDeparseResult DeparseProtobufDelegate(PgQueryProtobuf parseTree);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate PgQuerySplitResult SplitDelegate(byte[] input);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate PgQueryScanResult ScanDelegate(byte[] input);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate PgQueryPlpgsqlParseResult ParsePlpgsqlDelegate(byte[] input);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate PgQueryProtobufParseResult ParseProtobufDelegate(byte[] input);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void FreeParseResultDelegate(PgQueryParseResult result);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void FreeNormalizeResultDelegate(PgQueryNormalizeResult result);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void FreeFingerprintResultDelegate(PgQueryFingerprintResult result);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void FreeDeparseResultDelegate(PgQueryDeparseResult result);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void FreeSplitResultDelegate(PgQuerySplitResult result);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void FreeScanResultDelegate(PgQueryScanResult result);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void FreePlpgsqlParseResultDelegate(PgQueryPlpgsqlParseResult result);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void FreeProtobufParseResultDelegate(PgQueryProtobufParseResult result);

    #endregion

    #region Function Pointer Caches

    private static readonly ConcurrentDictionary<PostgreSqlVersion, ParseDelegate> _parseFunctions = new();
    private static readonly ConcurrentDictionary<PostgreSqlVersion, NormalizeDelegate> _normalizeFunctions = new();
    private static readonly ConcurrentDictionary<PostgreSqlVersion, FingerprintDelegate> _fingerprintFunctions = new();
    private static readonly ConcurrentDictionary<PostgreSqlVersion, DeparseDelegate> _deparseFunctions = new();
    private static readonly ConcurrentDictionary<PostgreSqlVersion, DeparseProtobufDelegate> _deparseProtobufFunctions = new();
    private static readonly ConcurrentDictionary<PostgreSqlVersion, SplitDelegate> _splitParserFunctions = new();
    private static readonly ConcurrentDictionary<PostgreSqlVersion, SplitDelegate> _splitScannerFunctions = new();
    private static readonly ConcurrentDictionary<PostgreSqlVersion, ScanDelegate> _scanFunctions = new();
    private static readonly ConcurrentDictionary<PostgreSqlVersion, ParsePlpgsqlDelegate> _parsePlpgsqlFunctions = new();
    private static readonly ConcurrentDictionary<PostgreSqlVersion, ParseProtobufDelegate> _parseProtobufFunctions = new();

    private static readonly ConcurrentDictionary<PostgreSqlVersion, FreeParseResultDelegate> _freeParseResultFunctions = new();
    private static readonly ConcurrentDictionary<PostgreSqlVersion, FreeNormalizeResultDelegate> _freeNormalizeResultFunctions = new();
    private static readonly ConcurrentDictionary<PostgreSqlVersion, FreeFingerprintResultDelegate> _freeFingerprintResultFunctions = new();
    private static readonly ConcurrentDictionary<PostgreSqlVersion, FreeDeparseResultDelegate> _freeDeparseResultFunctions = new();
    private static readonly ConcurrentDictionary<PostgreSqlVersion, FreeSplitResultDelegate> _freeSplitResultFunctions = new();
    private static readonly ConcurrentDictionary<PostgreSqlVersion, FreeScanResultDelegate> _freeScanResultFunctions = new();
    private static readonly ConcurrentDictionary<PostgreSqlVersion, FreePlpgsqlParseResultDelegate> _freePlpgsqlParseResultFunctions = new();
    private static readonly ConcurrentDictionary<PostgreSqlVersion, FreeProtobufParseResultDelegate> _freeProtobufParseResultFunctions = new();

    #endregion

    #region Version-Aware Native Methods

    internal static PgQueryParseResult pg_query_parse(byte[] input, PostgreSqlVersion version = PostgreSqlVersion.Postgres16)
    {
        var func = _parseFunctions.GetOrAdd(version, v =>
        {
            var handle = NativeLibraryLoader.GetLibraryHandle(v);
            var ptr = NativeLibrary.GetExport(handle, "pg_query_parse");
            return Marshal.GetDelegateForFunctionPointer<ParseDelegate>(ptr);
        });
        return func(input);
    }

    internal static PgQueryNormalizeResult pg_query_normalize(byte[] input, PostgreSqlVersion version = PostgreSqlVersion.Postgres16)
    {
        var func = _normalizeFunctions.GetOrAdd(version, v =>
        {
            var handle = NativeLibraryLoader.GetLibraryHandle(v);
            var ptr = NativeLibrary.GetExport(handle, "pg_query_normalize");
            return Marshal.GetDelegateForFunctionPointer<NormalizeDelegate>(ptr);
        });
        return func(input);
    }

    internal static PgQueryFingerprintResult pg_query_fingerprint(byte[] input, PostgreSqlVersion version = PostgreSqlVersion.Postgres16)
    {
        var func = _fingerprintFunctions.GetOrAdd(version, v =>
        {
            var handle = NativeLibraryLoader.GetLibraryHandle(v);
            var ptr = NativeLibrary.GetExport(handle, "pg_query_fingerprint");
            return Marshal.GetDelegateForFunctionPointer<FingerprintDelegate>(ptr);
        });
        return func(input);
    }

    internal static PgQueryDeparseResult pg_query_deparse(byte[] input, PostgreSqlVersion version = PostgreSqlVersion.Postgres16)
    {
        var func = _deparseFunctions.GetOrAdd(version, v =>
        {
            var handle = NativeLibraryLoader.GetLibraryHandle(v);
            var ptr = NativeLibrary.GetExport(handle, "pg_query_deparse");
            return Marshal.GetDelegateForFunctionPointer<DeparseDelegate>(ptr);
        });
        return func(input);
    }

    internal static PgQueryDeparseResult pg_query_deparse_protobuf(PgQueryProtobuf parseTree, PostgreSqlVersion version = PostgreSqlVersion.Postgres16)
    {
        var func = _deparseProtobufFunctions.GetOrAdd(version, v =>
        {
            var handle = NativeLibraryLoader.GetLibraryHandle(v);
            var ptr = NativeLibrary.GetExport(handle, "pg_query_deparse_protobuf");
            return Marshal.GetDelegateForFunctionPointer<DeparseProtobufDelegate>(ptr);
        });
        return func(parseTree);
    }

    internal static PgQuerySplitResult pg_query_split_with_parser(byte[] input, PostgreSqlVersion version = PostgreSqlVersion.Postgres16)
    {
        var func = _splitParserFunctions.GetOrAdd(version, v =>
        {
            var handle = NativeLibraryLoader.GetLibraryHandle(v);
            var ptr = NativeLibrary.GetExport(handle, "pg_query_split_with_parser");
            return Marshal.GetDelegateForFunctionPointer<SplitDelegate>(ptr);
        });
        return func(input);
    }

    internal static PgQuerySplitResult pg_query_split_with_scanner(byte[] input, PostgreSqlVersion version = PostgreSqlVersion.Postgres16)
    {
        var func = _splitScannerFunctions.GetOrAdd(version, v =>
        {
            var handle = NativeLibraryLoader.GetLibraryHandle(v);
            var ptr = NativeLibrary.GetExport(handle, "pg_query_split_with_scanner");
            return Marshal.GetDelegateForFunctionPointer<SplitDelegate>(ptr);
        });
        return func(input);
    }

    internal static PgQueryScanResult pg_query_scan(byte[] input, PostgreSqlVersion version = PostgreSqlVersion.Postgres16)
    {
        var func = _scanFunctions.GetOrAdd(version, v =>
        {
            var handle = NativeLibraryLoader.GetLibraryHandle(v);
            var ptr = NativeLibrary.GetExport(handle, "pg_query_scan");
            return Marshal.GetDelegateForFunctionPointer<ScanDelegate>(ptr);
        });
        return func(input);
    }

    internal static PgQueryPlpgsqlParseResult pg_query_parse_plpgsql(byte[] input, PostgreSqlVersion version = PostgreSqlVersion.Postgres16)
    {
        var func = _parsePlpgsqlFunctions.GetOrAdd(version, v =>
        {
            var handle = NativeLibraryLoader.GetLibraryHandle(v);
            var ptr = NativeLibrary.GetExport(handle, "pg_query_parse_plpgsql");
            return Marshal.GetDelegateForFunctionPointer<ParsePlpgsqlDelegate>(ptr);
        });
        return func(input);
    }

    internal static PgQueryProtobufParseResult pg_query_parse_protobuf(byte[] input, PostgreSqlVersion version = PostgreSqlVersion.Postgres16)
    {
        var func = _parseProtobufFunctions.GetOrAdd(version, v =>
        {
            var handle = NativeLibraryLoader.GetLibraryHandle(v);
            var ptr = NativeLibrary.GetExport(handle, "pg_query_parse_protobuf");
            return Marshal.GetDelegateForFunctionPointer<ParseProtobufDelegate>(ptr);
        });
        return func(input);
    }

    internal static void pg_query_free_parse_result(PgQueryParseResult result, PostgreSqlVersion version = PostgreSqlVersion.Postgres16)
    {
        var func = _freeParseResultFunctions.GetOrAdd(version, v =>
        {
            var handle = NativeLibraryLoader.GetLibraryHandle(v);
            var ptr = NativeLibrary.GetExport(handle, "pg_query_free_parse_result");
            return Marshal.GetDelegateForFunctionPointer<FreeParseResultDelegate>(ptr);
        });
        func(result);
    }

    internal static void pg_query_free_normalize_result(PgQueryNormalizeResult result, PostgreSqlVersion version = PostgreSqlVersion.Postgres16)
    {
        var func = _freeNormalizeResultFunctions.GetOrAdd(version, v =>
        {
            var handle = NativeLibraryLoader.GetLibraryHandle(v);
            var ptr = NativeLibrary.GetExport(handle, "pg_query_free_normalize_result");
            return Marshal.GetDelegateForFunctionPointer<FreeNormalizeResultDelegate>(ptr);
        });
        func(result);
    }

    internal static void pg_query_free_fingerprint_result(PgQueryFingerprintResult result, PostgreSqlVersion version = PostgreSqlVersion.Postgres16)
    {
        var func = _freeFingerprintResultFunctions.GetOrAdd(version, v =>
        {
            var handle = NativeLibraryLoader.GetLibraryHandle(v);
            var ptr = NativeLibrary.GetExport(handle, "pg_query_free_fingerprint_result");
            return Marshal.GetDelegateForFunctionPointer<FreeFingerprintResultDelegate>(ptr);
        });
        func(result);
    }

    internal static void pg_query_free_deparse_result(PgQueryDeparseResult result, PostgreSqlVersion version = PostgreSqlVersion.Postgres16)
    {
        var func = _freeDeparseResultFunctions.GetOrAdd(version, v =>
        {
            var handle = NativeLibraryLoader.GetLibraryHandle(v);
            var ptr = NativeLibrary.GetExport(handle, "pg_query_free_deparse_result");
            return Marshal.GetDelegateForFunctionPointer<FreeDeparseResultDelegate>(ptr);
        });
        func(result);
    }

    internal static void pg_query_free_split_result(PgQuerySplitResult result, PostgreSqlVersion version = PostgreSqlVersion.Postgres16)
    {
        var func = _freeSplitResultFunctions.GetOrAdd(version, v =>
        {
            var handle = NativeLibraryLoader.GetLibraryHandle(v);
            var ptr = NativeLibrary.GetExport(handle, "pg_query_free_split_result");
            return Marshal.GetDelegateForFunctionPointer<FreeSplitResultDelegate>(ptr);
        });
        func(result);
    }

    internal static void pg_query_free_scan_result(PgQueryScanResult result, PostgreSqlVersion version = PostgreSqlVersion.Postgres16)
    {
        var func = _freeScanResultFunctions.GetOrAdd(version, v =>
        {
            var handle = NativeLibraryLoader.GetLibraryHandle(v);
            var ptr = NativeLibrary.GetExport(handle, "pg_query_free_scan_result");
            return Marshal.GetDelegateForFunctionPointer<FreeScanResultDelegate>(ptr);
        });
        func(result);
    }

    internal static void pg_query_free_plpgsql_parse_result(PgQueryPlpgsqlParseResult result, PostgreSqlVersion version = PostgreSqlVersion.Postgres16)
    {
        var func = _freePlpgsqlParseResultFunctions.GetOrAdd(version, v =>
        {
            var handle = NativeLibraryLoader.GetLibraryHandle(v);
            var ptr = NativeLibrary.GetExport(handle, "pg_query_free_plpgsql_parse_result");
            return Marshal.GetDelegateForFunctionPointer<FreePlpgsqlParseResultDelegate>(ptr);
        });
        func(result);
    }

    internal static void pg_query_free_protobuf_parse_result(PgQueryProtobufParseResult result, PostgreSqlVersion version = PostgreSqlVersion.Postgres16)
    {
        var func = _freeProtobufParseResultFunctions.GetOrAdd(version, v =>
        {
            var handle = NativeLibraryLoader.GetLibraryHandle(v);
            var ptr = NativeLibrary.GetExport(handle, "pg_query_free_protobuf_parse_result");
            return Marshal.GetDelegateForFunctionPointer<FreeProtobufParseResultDelegate>(ptr);
        });
        func(result);
    }

    #endregion

    #region Native Helper Methods

    internal static string? PtrToString(IntPtr ptr)
    {
        if (ptr == IntPtr.Zero) return null;
#if NET472
        return PtrToStringUtf8Compat(ptr);
#else
        return Marshal.PtrToStringUTF8(ptr);
#endif
    }

#if NET472
    // Fallback implementation for UTF8 pointer -> string conversion (net472 lacks Marshal.PtrToStringUTF8)
    private static string? PtrToStringUtf8Compat(IntPtr ptr)
    {
        if (ptr == IntPtr.Zero) return null;
        byte* bytes = (byte*)ptr;
        int len = 0;
        while (bytes[len] != 0) len++;
        return Encoding.UTF8.GetString(bytes, len);
    }
#endif

    internal static byte[] StringToUtf8Bytes(string input)
    {
        var bytes = Encoding.UTF8.GetBytes(input);
        Array.Resize(ref bytes, bytes.Length + 1); // Add null terminator
        return bytes;
    }

    internal static PgQueryError? MarshalError(IntPtr errorPtr)
    {
        if (errorPtr == IntPtr.Zero)
            return null;

        return Marshal.PtrToStructure<PgQueryError>(errorPtr);
    }

    internal static PgQuerySplitStmt[] MarshalSplitStmts(PgQuerySplitResult result)
    {
        if (result.n_stmts == 0 || result.stmts == IntPtr.Zero)
            return Array.Empty<PgQuerySplitStmt>();

        var stmts = new PgQuerySplitStmt[result.n_stmts];
        int ptrSize = Marshal.SizeOf<IntPtr>();
        for (int i = 0; i < result.n_stmts; i++)
        {
            IntPtr stmtPtr = Marshal.ReadIntPtr(result.stmts, i * ptrSize);
            stmts[i] = Marshal.PtrToStructure<PgQuerySplitStmt>(stmtPtr);
        }
        return stmts;
    }

    internal static NativeScanResult ProcessScanResult(PgQueryScanResult nativeResult, string originalQuery)
    {
        if (nativeResult.error != IntPtr.Zero)
        {
            // Use MarshalError for proper error handling
            string? errorMessage = null;
            var errorStruct = MarshalError(nativeResult.error);
            if (errorStruct?.message != IntPtr.Zero)
            {
                errorMessage = PtrToString(errorStruct.Value.message);
            }

            return new NativeScanResult
            {
                Error = errorMessage ?? "Scan error",
                Stderr = PtrToString(nativeResult.stderr_buffer)
            };
        }

        var stderr = PtrToString(nativeResult.stderr_buffer);

        if (nativeResult.pbuf.data != IntPtr.Zero && nativeResult.pbuf.len != UIntPtr.Zero)
        {
            try
            {
                var protobufData = ProtobufHelper.ExtractProtobufData(nativeResult.pbuf);
                var result = ProtobufHelper.DeserializeScanResult(protobufData, originalQuery);
                result.Stderr = stderr;
                return result;
            }
            catch (Exception ex)
            {
                return new NativeScanResult
                {
                    Error = $"Failed to process protobuf data: {ex.Message}",
                    Stderr = stderr
                };
            }
        }
        else
        {
            return new NativeScanResult
            {
                Error = "No protobuf data available",
                Stderr = stderr
            };
        }
    }

    /// <summary>
    /// Allocates unmanaged memory for a PgQueryProtobuf from a byte array
    /// </summary>
    internal static PgQueryProtobuf AllocPgQueryProtobuf(byte[] protoBytes)
    {
        var protoStruct = new PgQueryProtobuf
        {
            len = (UIntPtr)protoBytes.Length,
            data = Marshal.AllocHGlobal(protoBytes.Length)
        };
        Marshal.Copy(protoBytes, 0, protoStruct.data, protoBytes.Length);
        return protoStruct;
    }

    /// <summary>
    /// Frees unmanaged memory for a PgQueryProtobuf
    /// </summary>
    internal static void FreePgQueryProtobuf(PgQueryProtobuf protoStruct)
    {
        if (protoStruct.data != IntPtr.Zero)
        {
            Marshal.FreeHGlobal(protoStruct.data);
        }
    }

    #endregion
}