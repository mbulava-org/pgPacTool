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

    /// <summary>
    /// Initializes a new instance of the ParserException class.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="query">The query that caused the exception (optional).</param>
    protected ParserException(string message, string? query = null) : base(message)
    {
        Query = query;
    }

    /// <summary>
    /// Initializes a new instance of the ParserException class with an inner exception.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The inner exception.</param>
    /// <param name="query">The query that caused the exception (optional).</param>
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

    /// <summary>
    /// Initializes a new instance of the ParseException class.
    /// </summary>
    /// <param name="parseError">The parse error message from libpg_query.</param>
    /// <param name="query">The query that failed to parse (optional).</param>
    public ParseException(string parseError, string? query = null) 
        : base($"Failed to parse PostgreSQL query: {parseError}", query)
    {
        ParseError = parseError;
    }

    /// <summary>
    /// Initializes a new instance of the ParseException class with an inner exception.
    /// </summary>
    /// <param name="parseError">The parse error message from libpg_query.</param>
    /// <param name="innerException">The inner exception.</param>
    /// <param name="query">The query that failed to parse (optional).</param>
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
    /// <summary>
    /// Initializes a new instance of the NativeLibraryException class.
    /// </summary>
    /// <param name="message">The error message.</param>
    public NativeLibraryException(string message) : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the NativeLibraryException class with an inner exception.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The inner exception.</param>
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

    /// <summary>
    /// Initializes a new instance of the NormalizationException class.
    /// </summary>
    /// <param name="normalizationError">The normalization error message.</param>
    /// <param name="query">The query that failed to normalize (optional).</param>
    public NormalizationException(string normalizationError, string? query = null)
        : base($"Failed to normalize PostgreSQL query: {normalizationError}", query)
    {
        NormalizationError = normalizationError;
    }

    /// <summary>
    /// Initializes a new instance of the NormalizationException class with an inner exception.
    /// </summary>
    /// <param name="normalizationError">The normalization error message.</param>
    /// <param name="innerException">The inner exception.</param>
    /// <param name="query">The query that failed to normalize (optional).</param>
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

    /// <summary>
    /// Initializes a new instance of the FingerprintException class.
    /// </summary>
    /// <param name="fingerprintError">The fingerprinting error message.</param>
    /// <param name="query">The query that failed to fingerprint (optional).</param>
    public FingerprintException(string fingerprintError, string? query = null)
        : base($"Failed to fingerprint PostgreSQL query: {fingerprintError}", query)
    {
        FingerprintError = fingerprintError;
    }

    /// <summary>
    /// Initializes a new instance of the FingerprintException class with an inner exception.
    /// </summary>
    /// <param name="fingerprintError">The fingerprinting error message.</param>
    /// <param name="innerException">The inner exception.</param>
    /// <param name="query">The query that failed to fingerprint (optional).</param>
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

    /// <summary>
    /// Initializes a new instance of the DeparseException class.
    /// </summary>
    /// <param name="deparseError">The deparse error message.</param>
    /// <param name="query">The query that failed to deparse (optional).</param>
    public DeparseException(string deparseError, string? query = null)
        : base($"Failed to deparse PostgreSQL AST: {deparseError}", query)
    {
        DeparseError = deparseError;
    }

    /// <summary>
    /// Initializes a new instance of the DeparseException class with an inner exception.
    /// </summary>
    /// <param name="deparseError">The deparse error message.</param>
    /// <param name="innerException">The inner exception.</param>
    /// <param name="query">The query that failed to deparse (optional).</param>
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

    /// <summary>
    /// Initializes a new instance of the SplitException class.
    /// </summary>
    /// <param name="splitError">The split error message.</param>
    /// <param name="query">The query that failed to split (optional).</param>
    public SplitException(string splitError, string? query = null)
        : base($"Failed to split PostgreSQL statements: {splitError}", query)
    {
        SplitError = splitError;
    }

    /// <summary>
    /// Initializes a new instance of the SplitException class with an inner exception.
    /// </summary>
    /// <param name="splitError">The split error message.</param>
    /// <param name="innerException">The inner exception.</param>
    /// <param name="query">The query that failed to split (optional).</param>
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

    /// <summary>
    /// Initializes a new instance of the ScanException class.
    /// </summary>
    /// <param name="scanError">The scan error message.</param>
    /// <param name="query">The query that failed to scan (optional).</param>
    public ScanException(string scanError, string? query = null)
        : base($"Failed to scan PostgreSQL query: {scanError}", query)
    {
        ScanError = scanError;
    }

    /// <summary>
    /// Initializes a new instance of the ScanException class with an inner exception.
    /// </summary>
    /// <param name="scanError">The scan error message.</param>
    /// <param name="innerException">The inner exception.</param>
    /// <param name="query">The query that failed to scan (optional).</param>
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

    /// <summary>
    /// Initializes a new instance of the PlpgsqlParseException class.
    /// </summary>
    /// <param name="plpgsqlParseError">The PL/pgSQL parse error message.</param>
    /// <param name="query">The query that failed to parse (optional).</param>
    public PlpgsqlParseException(string plpgsqlParseError, string? query = null)
        : base($"Failed to parse PL/pgSQL code: {plpgsqlParseError}", query)
    {
        PlpgsqlParseError = plpgsqlParseError;
    }

    /// <summary>
    /// Initializes a new instance of the PlpgsqlParseException class with an inner exception.
    /// </summary>
    /// <param name="plpgsqlParseError">The PL/pgSQL parse error message.</param>
    /// <param name="innerException">The inner exception.</param>
    /// <param name="query">The query that failed to parse (optional).</param>
    public PlpgsqlParseException(string plpgsqlParseError, Exception innerException, string? query = null)
        : base($"Failed to parse PL/pgSQL code: {plpgsqlParseError}", innerException, query)
    {
        PlpgsqlParseError = plpgsqlParseError;
    }
}