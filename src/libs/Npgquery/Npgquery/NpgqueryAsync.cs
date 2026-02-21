using System.Text.Json;

namespace Npgquery;

/// <summary>
/// Async extensions for PostgreSQL query parsing
/// </summary>
public static class ParserAsync
{
    /// <summary>
    /// Asynchronously parse a PostgreSQL query into an Abstract Syntax Tree (AST)
    /// </summary>
    public static Task<ParseResult> ParseAsync(this Parser parser, string query, 
        ParseOptions? options = null, CancellationToken cancellationToken = default) =>
        Task.Run(() => parser.Parse(query, options), cancellationToken);

    /// <summary>
    /// Asynchronously normalize a PostgreSQL query
    /// </summary>
    public static Task<NormalizeResult> NormalizeAsync(this Parser parser, string query, 
        CancellationToken cancellationToken = default) =>
        Task.Run(() => parser.Normalize(query), cancellationToken);

    /// <summary>
    /// Asynchronously generate a fingerprint for a PostgreSQL query
    /// </summary>
    public static Task<FingerprintResult> FingerprintAsync(this Parser parser, string query, 
        CancellationToken cancellationToken = default) =>
        Task.Run(() => parser.Fingerprint(query), cancellationToken);

    /// <summary>
    /// Asynchronously parse a PostgreSQL query and return the AST as a strongly-typed object
    /// </summary>
    public static Task<T?> ParseAsAsync<T>(this Parser parser, string query, 
        ParseOptions? options = null, CancellationToken cancellationToken = default) where T : class =>
        Task.Run(() => parser.ParseAs<T>(query, options), cancellationToken);

    /// <summary>
    /// Asynchronously validate that a PostgreSQL query has valid syntax
    /// </summary>
    public static Task<bool> IsValidAsync(this Parser parser, string query, 
        CancellationToken cancellationToken = default) =>
        Task.Run(() => parser.IsValid(query), cancellationToken);

    /// <summary>
    /// Asynchronously deparse a PostgreSQL AST back to SQL
    /// </summary>
    public static Task<DeparseResult> DeparseAsync(this Parser parser, JsonDocument parseTree, 
        CancellationToken cancellationToken = default) =>
        Task.Run(() => parser.Deparse(parseTree), cancellationToken);

    /// <summary>
    /// Asynchronously split multiple PostgreSQL statements
    /// </summary>
    public static Task<SplitResult> SplitAsync(this Parser parser, string query, 
        CancellationToken cancellationToken = default) =>
        Task.Run(() => parser.Split(query), cancellationToken);

    /// <summary>
    /// Asynchronously scan/tokenize a PostgreSQL query
    /// </summary>
    public static Task<ScanResult> ScanAsync(this Parser parser, string query, 
        CancellationToken cancellationToken = default) =>
        Task.Run(() => parser.Scan(query), cancellationToken);

    /// <summary>
    /// Asynchronously parse PL/pgSQL code
    /// </summary>
    public static Task<PlpgsqlParseResult> ParsePlpgsqlAsync(this Parser parser, string plpgsqlCode, 
        CancellationToken cancellationToken = default) =>
        Task.Run(() => parser.ParsePlpgsql(plpgsqlCode), cancellationToken);

    /// <summary>
    /// Process multiple queries in parallel
    /// </summary>
    public static async Task<ParseResult[]> ParseManyAsync(this Parser parser, IEnumerable<string> queries,
        ParseOptions? options = null, int maxDegreeOfParallelism = 4, 
        CancellationToken cancellationToken = default)
    {
        using var semaphore = new SemaphoreSlim(maxDegreeOfParallelism);
        
        var tasks = queries.Select(async query =>
        {
            await semaphore.WaitAsync(cancellationToken);
            try
            {
                return await parser.ParseAsync(query, options, cancellationToken);
            }
            finally
            {
                semaphore.Release();
            }
        });

        return await Task.WhenAll(tasks);
    }

    /// <summary>
    /// Static async method for quick one-off parsing
    /// </summary>
    /// <param name="query">The SQL query to parse</param>
    /// <param name="options">Parse options (optional)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Parse result</returns>
    public static async Task<ParseResult> QuickParseAsync(string query, ParseOptions? options = null, 
        CancellationToken cancellationToken = default)
    {
        using var parser = new Parser();
        return await parser.ParseAsync(query, options, cancellationToken);
    }

    /// <summary>
    /// Static async method for quick one-off normalization
    /// </summary>
    /// <param name="query">The SQL query to normalize</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Normalize result</returns>
    public static async Task<NormalizeResult> QuickNormalizeAsync(string query, 
        CancellationToken cancellationToken = default)
    {
        using var parser = new Parser();
        return await parser.NormalizeAsync(query, cancellationToken);
    }

    /// <summary>
    /// Static async method for quick one-off fingerprinting
    /// </summary>
    /// <param name="query">The SQL query to fingerprint</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Fingerprint result</returns>
    public static async Task<FingerprintResult> QuickFingerprintAsync(string query, 
        CancellationToken cancellationToken = default)
    {
        using var parser = new Parser();
        return await parser.FingerprintAsync(query, cancellationToken);
    }

    /// <summary>
    /// Static async method for quick one-off deparsing
    /// </summary>
    /// <param name="parseTree">The AST JSON to deparse</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Deparse result</returns>
    public static async Task<DeparseResult> QuickDeparseAsync(JsonDocument parseTree, 
        CancellationToken cancellationToken = default)
    {
        using var parser = new Parser();
        return await parser.DeparseAsync(parseTree, cancellationToken);
    }

    /// <summary>
    /// Static async method for quick one-off splitting
    /// </summary>
    /// <param name="query">The SQL string to split</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Split result</returns>
    public static async Task<SplitResult> QuickSplitAsync(string query, 
        CancellationToken cancellationToken = default)
    {
        using var parser = new Parser();
        return await parser.SplitAsync(query, cancellationToken);
    }

    /// <summary>
    /// Static async method for quick one-off scanning
    /// </summary>
    /// <param name="query">The SQL query to scan</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Scan result</returns>
    public static async Task<ScanResult> QuickScanAsync(string query, 
        CancellationToken cancellationToken = default)
    {
        using var parser = new Parser();
        return await parser.ScanAsync(query, cancellationToken);
    }

    /// <summary>
    /// Static async method for quick one-off PL/pgSQL parsing
    /// </summary>
    /// <param name="plpgsqlCode">The PL/pgSQL code to parse</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>PL/pgSQL parse result</returns>
    public static async Task<PlpgsqlParseResult> QuickParsePlpgsqlAsync(string plpgsqlCode, 
        CancellationToken cancellationToken = default)
    {
        using var parser = new Parser();
        return await parser.ParsePlpgsqlAsync(plpgsqlCode, cancellationToken);
    }
}