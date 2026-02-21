namespace Npgquery;

/// <summary>
/// Base exception for all Parser-related errors
/// </summary>
public abstract class ParserException : Exception
{
    /// <summary>
    /// The query that caused the exception (if available)
    /// </summary>
    public string? Query { get; }

    protected ParserException(string message, string? query = null) : base(message)
    {
        Query = query;
    }

    protected ParserException(string message, Exception innerException, string? query = null) 
        : base(message, innerException)
    {
        Query = query;
    }
}

/// <summary>
/// Exception thrown when a PostgreSQL query cannot be parsed
/// </summary>
public sealed class ParseException : ParserException
{
    /// <summary>
    /// The specific parse error message from libpg_query
    /// </summary>
    public string ParseError { get; }

    public ParseException(string parseError, string? query = null) 
        : base($"Failed to parse PostgreSQL query: {parseError}", query)
    {
        ParseError = parseError;
    }

    public ParseException(string parseError, Exception innerException, string? query = null) 
        : base($"Failed to parse PostgreSQL query: {parseError}", innerException, query)
    {
        ParseError = parseError;
    }
}

/// <summary>
/// Exception thrown when the native libpg_query library cannot be loaded or accessed
/// </summary>
public sealed class NativeLibraryException : ParserException
{
    public NativeLibraryException(string message) : base(message)
    {
    }

    public NativeLibraryException(string message, Exception innerException) : base(message, innerException)
    {
    }
}

/// <summary>
/// Exception thrown when query normalization fails
/// </summary>
public sealed class NormalizationException : ParserException
{
    /// <summary>
    /// The specific normalization error message
    /// </summary>
    public string NormalizationError { get; }

    public NormalizationException(string normalizationError, string? query = null)
        : base($"Failed to normalize PostgreSQL query: {normalizationError}", query)
    {
        NormalizationError = normalizationError;
    }

    public NormalizationException(string normalizationError, Exception innerException, string? query = null)
        : base($"Failed to normalize PostgreSQL query: {normalizationError}", innerException, query)
    {
        NormalizationError = normalizationError;
    }
}

/// <summary>
/// Exception thrown when query fingerprinting fails
/// </summary>
public sealed class FingerprintException : ParserException
{
    /// <summary>
    /// The specific fingerprinting error message
    /// </summary>
    public string FingerprintError { get; }

    public FingerprintException(string fingerprintError, string? query = null)
        : base($"Failed to fingerprint PostgreSQL query: {fingerprintError}", query)
    {
        FingerprintError = fingerprintError;
    }

    public FingerprintException(string fingerprintError, Exception innerException, string? query = null)
        : base($"Failed to fingerprint PostgreSQL query: {fingerprintError}", innerException, query)
    {
        FingerprintError = fingerprintError;
    }
}

/// <summary>
/// Exception thrown when query deparsing fails
/// </summary>
public sealed class DeparseException : ParserException
{
    /// <summary>
    /// The specific deparse error message
    /// </summary>
    public string DeparseError { get; }

    public DeparseException(string deparseError, string? query = null)
        : base($"Failed to deparse PostgreSQL AST: {deparseError}", query)
    {
        DeparseError = deparseError;
    }

    public DeparseException(string deparseError, Exception innerException, string? query = null)
        : base($"Failed to deparse PostgreSQL AST: {deparseError}", innerException, query)
    {
        DeparseError = deparseError;
    }
}

/// <summary>
/// Exception thrown when query splitting fails
/// </summary>
public sealed class SplitException : ParserException
{
    /// <summary>
    /// The specific split error message
    /// </summary>
    public string SplitError { get; }

    public SplitException(string splitError, string? query = null)
        : base($"Failed to split PostgreSQL statements: {splitError}", query)
    {
        SplitError = splitError;
    }

    public SplitException(string splitError, Exception innerException, string? query = null)
        : base($"Failed to split PostgreSQL statements: {splitError}", innerException, query)
    {
        SplitError = splitError;
    }
}

/// <summary>
/// Exception thrown when query scanning fails
/// </summary>
public sealed class ScanException : ParserException
{
    /// <summary>
    /// The specific scan error message
    /// </summary>
    public string ScanError { get; }

    public ScanException(string scanError, string? query = null)
        : base($"Failed to scan PostgreSQL query: {scanError}", query)
    {
        ScanError = scanError;
    }

    public ScanException(string scanError, Exception innerException, string? query = null)
        : base($"Failed to scan PostgreSQL query: {scanError}", innerException, query)
    {
        ScanError = scanError;
    }
}

/// <summary>
/// Exception thrown when PL/pgSQL parsing fails
/// </summary>
public sealed class PlpgsqlParseException : ParserException
{
    /// <summary>
    /// The specific PL/pgSQL parse error message
    /// </summary>
    public string PlpgsqlParseError { get; }

    public PlpgsqlParseException(string plpgsqlParseError, string? query = null)
        : base($"Failed to parse PL/pgSQL code: {plpgsqlParseError}", query)
    {
        PlpgsqlParseError = plpgsqlParseError;
    }

    public PlpgsqlParseException(string plpgsqlParseError, Exception innerException, string? query = null)
        : base($"Failed to parse PL/pgSQL code: {plpgsqlParseError}", innerException, query)
    {
        PlpgsqlParseError = plpgsqlParseError;
    }
}