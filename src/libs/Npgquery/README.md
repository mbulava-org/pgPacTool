# Npgquery - PostgreSQL Query Parser for .NET

A high-performance .NET 9 C# library for parsing PostgreSQL queries using the battle-tested `libpg_query` library. This library provides the same functionality as popular wrappers in other languages like Go, Rust, Python, and JavaScript.

## Features

- **Parse PostgreSQL queries** into Abstract Syntax Trees (AST) in JSON or Protobuf format.
- **Normalize queries** by standardizing formatting and replacing constants with placeholders.
- **Generate query fingerprints** for similarity comparison.
- **Deparse AST back to SQL** (convert parse trees back to queries).
- **Split multiple statements** with location information.
- **Scan/tokenize queries** for detailed analysis.
- **Parse PL/pgSQL code** (stored procedures, functions).
- **Extract metadata** like table names and query types.
- **Async/await support** for modern .NET applications.
- **Memory-safe** native interop with automatic resource cleanup.
- **Strong typing** with nullable reference types and records.
- **High performance** with parallel processing capabilities.


## Quick Start

### Basic Parsing

```csharp
using Npgquery;

using var parser = new Parser();
var queries = new[]
{
    "SELECT * FROM users WHERE id = 1",
    "INSERT INTO posts (title, content) VALUES ('Hello', 'World')",
    "INVALID SQL SYNTAX"
};

foreach (var query in queries)
{
    var result = parser.Parse(query);
    Console.WriteLine($"Query: {query}");
    Console.WriteLine($"Valid: {result.IsSuccess}");
    if (result.IsSuccess)
    {
        Console.WriteLine($"Parse Tree Length: {result.ParseTree?.RootElement.ToString().Length ?? 0} characters");
    }
    else
    {
        Console.WriteLine($"Error: {result.Error}");
    }
    Console.WriteLine();
}
```

### Query Normalization

```csharp
using var parser = new Parser();
var queries = new[]
{
    "SELECT * FROM users /* this is a comment */ WHERE id = 1",
    "SELECT   *   FROM   users   WHERE   id   =   2  ",
    "select name, email from users where active = true"
};
foreach (var query in queries)
{
    var result = parser.Normalize(query);
    Console.WriteLine($"Original:   {query}");
    Console.WriteLine($"Normalized: {result.NormalizedQuery}");
    Console.WriteLine();
}
```

### Query Fingerprinting

```csharp
using var parser = new Parser();
var queries = new[]
{
    "SELECT * FROM users WHERE id = 1",
    "SELECT * FROM users WHERE id = 2",
    "SELECT * FROM users WHERE id = 999",
    "SELECT name FROM users WHERE id = 1"
};
var fingerprints = new List<(string query, string? fingerprint)>();
foreach (var query in queries)
{
    var result = parser.Fingerprint(query);
    fingerprints.Add((query, result.Fingerprint));
    Console.WriteLine($"Query: {query}");
    Console.WriteLine($"Fingerprint: {result.Fingerprint}");
    Console.WriteLine();
}
// Check for similar queries
for (int i = 0; i < fingerprints.Count; i++)
{
    for (int j = i + 1; j < fingerprints.Count; j++)
    {
        if (fingerprints[i].fingerprint == fingerprints[j].fingerprint)
        {
            Console.WriteLine($"Queries {i + 1} and {j + 1} have the same structure");
        }
    }
}
```

### Utility Functions

```csharp
var complexQuery = @"
    SELECT u.name, u.email, p.title, c.content
    FROM users u
    JOIN posts p ON u.id = p.user_id
    LEFT JOIN comments c ON p.id = c.post_id
    WHERE u.active = true
    AND p.published_at > '2023-01-01'
    ORDER BY p.published_at DESC
    LIMIT 10
";
// Extract table names
var tables = QueryUtils.ExtractTableNames(complexQuery);
Console.WriteLine("Tables found:");
foreach (var table in tables)
{
    Console.WriteLine($"  - {table}");
}
// Get query type
var queryType = QueryUtils.GetQueryType(complexQuery);
Console.WriteLine($"Query type: {queryType}");
// Clean query
var cleaned = QueryUtils.CleanQuery(complexQuery);
Console.WriteLine("Cleaned query:");
Console.WriteLine(cleaned);
// Validate multiple queries
var testQueries = new[]
{
    "SELECT 1",
    "INVALID SQL",
    "INSERT INTO test VALUES (1)",
    "DELETE FROM test WHERE id = 1"
};
var validationResults = QueryUtils.ValidateQueries(testQueries);
Console.WriteLine("Validation results:");
foreach (var (query, isValid) in validationResults)
{
    Console.WriteLine($"  {query}: {(isValid ? "✓ Valid" : "✗ Invalid")}");
}
```

### Async Parsing

```csharp
using Npgquery;
using var parser = new Parser();
var query = "SELECT * FROM users WHERE created_at > '2023-01-01'";
var result = await parser.ParseAsync(query);
Console.WriteLine($"Async parse successful: {result.IsSuccess}");
// Static async methods
var quickResult = await ParserAsync.QuickParseAsync("SELECT version()");
Console.WriteLine($"Quick async parse successful: {quickResult.IsSuccess}");
```

### Batch Processing Example

```csharp
var sqlQueries = new[]
{
    "-- User management queries",
    "SELECT * FROM users WHERE active = true;",
    "UPDATE users SET last_login = NOW() WHERE id = 1;",
    "-- This is an invalid query",
    "SELECT * FORM users;", // Typo: FORM instead of FROM
    "DELETE FROM users WHERE id = 999;",
    "-- Post queries",
    "SELECT p.*, u.name FROM posts p JOIN users u ON p.user_id = u.id;",
    "INSERT INTO posts (title, content, user_id) VALUES ('Test', 'Content', 1);"
};
var validQueries = new List<string>();
var invalidQueries = new List<(string query, string error)>();
using var parser = new Parser();
foreach (var query in sqlQueries)
{
    var trimmed = query.Trim();
    if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith("--"))
        continue;
    var result = parser.Parse(trimmed);
    if (result.IsSuccess)
    {
        validQueries.Add(trimmed);
        var queryType = QueryUtils.GetQueryType(trimmed);
        var tables = QueryUtils.ExtractTableNames(trimmed);
        Console.WriteLine($"✓ {queryType} query affecting tables: {string.Join(", ", tables)}");
    }
    else
    {
        invalidQueries.Add((trimmed, result.Error!));
        Console.WriteLine($"✗ Invalid query: {trimmed}");
        Console.WriteLine($"  Error: {result.Error}");
    }
}
Console.WriteLine($"Valid queries: {validQueries.Count}");
Console.WriteLine($"Invalid queries: {invalidQueries.Count}");
```

### PL/pgSQL Parsing and Validation

```csharp
using var parser = new Parser();
var plpgsqlCode = @"DO $$
DECLARE
    ret VARCHAR;
BEGIN
    ret := 'Hello, World!';
    RAISE NOTICE '%', ret;
END;
$$;";
var parseResult = parser.ParsePlpgsql(plpgsqlCode);
if (parseResult.IsSuccess)
{
    Console.WriteLine($"PL/pgSQL parse tree: {parseResult.ParseTree}");
}
else
{
    Console.WriteLine($"Error: {parseResult.Error}");
}
// Utility validation
var isValid = QueryUtils.IsValidPlpgsql(plpgsqlCode);
Console.WriteLine($"Utility validation: {(isValid ? "✓ Valid" : "✗ Invalid")}");
```

## Complete API Reference

### Core `Parser` Instance Methods

#### `Parse(query, options)`

Parse a SQL string into a JSON AST.

**Parameters:**

- `query`: The SQL query string.
- `options`: Optional parsing options.

**Returns:**

- A `ParseResult` object with `IsSuccess`, `ParseTree`, and `Error` properties.

```csharp
using var parser = new Parser();
var options = new ParseOptions { IncludeLocations = true };
var result = parser.Parse("SELECT * FROM users", options);

if (result.IsSuccess)
{
    Console.WriteLine($"Parsed AST: {result.ParseTree}");
}
else
{
    Console.WriteLine($"Parse error: {result.Error}");
}
```

#### `ParseProtobuf(query)`

Parse a SQL string into a Protobuf AST.

**Parameters:**

- `query`: The SQL query string.

**Returns:**

- A `ProtobufParseResult` object with `IsSuccess`, `ParseTree`, and `Error` properties.

#### `Normalize(query)`

Normalize the formatting of a SQL query.

**Parameters:**

- `query`: The SQL query string.

**Returns:**

- A `NormalizeResult` object with `NormalizedQuery` and `Error` properties.

```csharp
using var parser = new Parser();
var normalizeResult = parser.Normalize("SELECT   *   FROM    users  WHERE id=1");

Console.WriteLine($"Normalized query: {normalizeResult.NormalizedQuery}");
// Output: SELECT * FROM users WHERE id = $1
```

#### `Fingerprint(query)`

Generate a structural fingerprint for a SQL query.

**Parameters:**

- `query`: The SQL query string.

**Returns:**

- A `FingerprintResult` object with `Fingerprint` and `Error` properties.

```csharp
using var parser = new Parser();
var query = "SELECT * FROM users WHERE id = 1";

var fingerprintResult = parser.Fingerprint(query);
Console.WriteLine($"Query fingerprint: {fingerprintResult.Fingerprint}");
```

#### `Deparse(ast)`

Convert a JSON AST back to a SQL query.

**Parameters:**

- `ast`: The JSON AST object.

**Returns:**

- A `DeparseResult` object with `Query` and `Error` properties.

```csharp
using var parser = new Parser();
var ast = parser.Parse("SELECT * FROM users WHERE id = 1").ParseTree;

var deparseResult = parser.Deparse(ast);
Console.WriteLine($"Deparsed query: {deparseResult.Query}");
```

#### `DeparseProtobuf(protobufAst)`

Convert a Protobuf AST back to a SQL query.

**Parameters:**

- `protobufAst`: The Protobuf AST object.

**Returns:**

- A `DeparseResult` object with `Query` and `Error` properties.

#### `Split(query)`

Split a SQL string with multiple statements into individual statements.

**Parameters:**

- `query`: The SQL query string.

**Returns:**

- A `SplitResult` object with `Statements` and `Error` properties.

```csharp
using var parser = new Parser();
var multiStatementQuery = "SELECT 1; INSERT INTO users VALUES (1, 'John');";
var splitResult = parser.Split(multiStatementQuery);

if (splitResult.IsSuccess)
{
    foreach (var statement in splitResult.Statements)
    {
        Console.WriteLine($"Statement: {statement.Statement}");
    }
}
```

#### `Scan(query)`

Tokenize/scan a SQL query.

**Parameters:**

- `query`: The SQL query string.

**Returns:**

- A `ScanResult` object with `Tokens` and `Error` properties.

```csharp
using var parser = new Parser();
var scanResult = parser.Scan("SELECT id, name FROM users");

if (scanResult.IsSuccess)
{
    foreach (var token in scanResult.Tokens)
    {
        Console.WriteLine($"Token: {token.Token}, Type: {token.KeywordKind}");
    }
}
```

#### `ScanWithProtobuf(query)`

Tokenize/scan a SQL query and return tokens in Protobuf format.

**Parameters:**

- `query`: The SQL query string.

**Returns:**

- A `EnhancedScanResult` object with `Tokens` and `Error` properties.

#### `ParsePlpgsql(code)`

Parse a PL/pgSQL code block.

**Parameters:**

- `code`: The PL/pgSQL code string.

**Returns:**

- A `PlpgsqlParseResult` object with `IsSuccess`, `ParseTree`, and `Error` properties.

```csharp
using var parser = new Parser();
var plpgsqlCode = @"DO $$
DECLARE
    id INTEGER := 5;
BEGIN
    IF id > 0 THEN
        RAISE NOTICE 'ID is positive';
    ELSE
        RAISE NOTICE 'ID is not positive';
    END IF;
END;
$$;";

var plpgsqlResult = parser.ParsePlpgsql(plpgsqlCode);
if (plpgsqlResult.IsSuccess)
{
    Console.WriteLine($"PL/pgSQL AST: {plpgsqlResult.ParseTree}");
}
```

#### `IsValid(query)`

Check if a SQL query is valid.

**Parameters:**

- `query`: The SQL query string.

**Returns:**

- A boolean indicating validity.

```csharp
using var parser = new Parser();
var isValid = parser.IsValid("SELECT * FROM users WHERE id = 1");

Console.WriteLine($"Is valid SQL: {isValid}");
```

#### `GetError(query)`

Get the error message for an invalid SQL query.

**Parameters:**

- `query`: The SQL query string.

**Returns:**

- A string with the error message.

```csharp
using var parser = new Parser();
var result = parser.Parse("SELECT * FROM WHERE id = 1"); // Invalid SQL

if (!result.IsSuccess)
{
    Console.WriteLine($"Error detected: {result.Error}");
}
```

#### `ParseAs<T>(query, options)`

Parse a SQL query into a strongly-typed object.

**Parameters:**

- `query`: The SQL query string.
- `options`: Optional parsing options.

**Returns:**

- An object of type `T` representing the parsed query.

```csharp
// Parse into strongly-typed objects
using var parser = new Parser();
var mySelect = parser.ParseAs<object>("SELECT id, name FROM users");

Console.WriteLine($"Parsed object: {mySelect}");
```

### Static `Parser` Quick Methods

For one-off operations without creating a parser instance.

- `QuickParse(query, options)`
- `QuickNormalize(query)`
- `QuickFingerprint(query)`
- `QuickDeparse(ast)`
- `QuickSplit(query)`
- `QuickScan(query)`
- `QuickParsePlpgsql(code)`
- `QuickScanWithProtobuf(query)`

### Async Support (`ParserAsync` extensions)

All core methods have async counterparts.

```csharp
using Npgquery;

// Async parsing on an instance
using var parser = new Parser();
var result = await parser.ParseAsync("SELECT * FROM users WHERE id = 1");

// Static async method for quick one-off parsing
var quickResult = await ParserAsync.QuickParseAsync("SELECT * FROM users");
```

- `ParseAsync(query, options)`
- `NormalizeAsync(query)`
- `FingerprintAsync(query)`
- `DeparseAsync(ast)`
- `SplitAsync(query)`
- `ScanAsync(query)`
- `ParsePlpgsqlAsync(code)`
- `ParseAsAsync<T>(query, options)`
- `IsValidAsync(query)`
- `ParseManyAsync(queries, options, maxDegreeOfParallelism)`: Parse multiple queries in parallel.

### Static Async Quick Methods (`ParserAsync`)

- `QuickParseAsync(query, options)`
- `QuickNormalizeAsync(query)`
- `QuickFingerprintAsync(query)`
- `QuickDeparseAsync(ast)`
- `QuickSplitAsync(query)`
- `QuickScanAsync(query)`
- `QuickParsePlpgsqlAsync(code)`

### Utility Functions (`QueryUtils`)

A static class with helper methods for common tasks.

- `ExtractTableNames(query)`: Get a list of all table names from a query.
- `GetQueryType(query)`: Get the statement type (e.g., "SELECT", "INSERT").
- `SplitStatements(sqlText)`: Split a string into a list of individual statements.
- `GetTokens(query)`: Get a list of all tokens from a query.
- `GetKeywords(query)`: Get a list of unique keywords from a query.
- `CountStatements(sqlText)`: Count the number of statements in a string.
- `CleanQuery(query)`: A convenient alias for `Parser.QuickNormalize(query)`.
- `NormalizeStatements(sqlText)`: Splits a multi-statement string and normalizes each one.
- `HaveSameStructure(query1, query2)`: Check if two queries have the same fingerprint.
- `AstToSql(parseTree)`: Convert a JSON AST back to an SQL string.
- `RoundTripTest(query)`: A utility to parse a query and deparse it back, checking for consistency.
- `IsValidPlpgsql(plpgsqlCode)`: Check if a string of PL/pgSQL code is valid.
- `ValidateQueries(queries)`: Validate a collection of queries.
- `GetQueryErrors(queries)`: Get detailed errors for a collection of queries.

## Advanced Usage

### Parse Options

The `ParseOptions` class provides several configuration options to customize the parsing behavior:

#### Available Options

**`IncludeLocations`** (boolean, default: `false`)
- When set to `true`, the resulting Abstract Syntax Tree (AST) will include location information for each node
- Location information shows the character position in the original query where each element was found
- Useful for analysis tools, syntax highlighting, or error reporting that need to map back to the original query text
- Note: Including locations increases the size of the parse tree output

**`PostgreSqlVersion`** (integer, default: `160000`)
- Specifies the PostgreSQL version number for the parser to target
- Format: Major version × 10000 + Minor version × 100 + Patch version
- Examples:
  - `170000` = PostgreSQL 17.0
  - `160000` = PostgreSQL 16.0 (default)
  - `150000` = PostgreSQL 15.0
  - `140000` = PostgreSQL 14.0
- Useful for ensuring compatibility with specific PostgreSQL versions
- Parser behavior may vary slightly between versions for edge cases

#### Usage Examples

```csharp
using Npgquery;

// Basic usage with default options
using var parser = new Parser();
var result = parser.Parse("SELECT * FROM users");

// Include location information in parse tree
var optionsWithLocations = new ParseOptions
{
    IncludeLocations = true
};
var resultWithLocations = parser.Parse("SELECT * FROM users WHERE id = 1", optionsWithLocations);

// Target a specific PostgreSQL version
var optionsForPg15 = new ParseOptions
{
    PostgreSqlVersion = 150000 // PostgreSQL 15
};
var resultForPg15 = parser.Parse("SELECT * FROM users", optionsForPg15);

// Combine multiple options
var combinedOptions = new ParseOptions
{
    IncludeLocations = true,
    PostgreSqlVersion = 140000 // PostgreSQL 14
};
var combinedResult = parser.Parse("SELECT * FROM users", combinedOptions);

// Using with static methods
var quickResult = Parser.QuickParse("SELECT * FROM users", combinedOptions);

// Using with async methods
var asyncResult = await parser.ParseAsync("SELECT * FROM users", combinedOptions);
```

#### When to Use Parse Options

- **Include Locations**: Enable when building tools that need to:
  - Highlight syntax in editors
  - Show precise error locations
  - Generate source maps for query transformations
  - Build refactoring tools that modify specific parts of queries

- **PostgreSQL Version**: Specify when:
  - Working with legacy systems running older PostgreSQL versions
  - Ensuring compatibility across different PostgreSQL deployments
  - Testing queries against specific PostgreSQL feature sets
  - Building tools that need to support multiple PostgreSQL versions

#### Performance Considerations

- Including locations adds overhead to parsing and increases memory usage
- Version-specific parsing differences are minimal for most common queries
- For high-throughput scenarios, consider reusing the same `ParseOptions` instance

### Custom Parse Options

```csharp
using var parser = new Parser();
var options = new ParseOptions
{
    IncludeLocations = true,
    PostgreSqlVersion = 160000 // PostgreSQL 16
};

var result = parser.Parse("SELECT * FROM users", options);
```

### Strongly-Typed AST

```csharp
// Parse into strongly-typed objects
using var parser = new Parser();
var ast = parser.ParseAs<object>("SELECT * FROM users");
```

### Batch Processing

```csharp
var queries = File.ReadAllLines("queries.sql");

// Validate all queries
var validationResults = QueryUtils.ValidateQueries(queries);

// Get errors for invalid queries
var errors = QueryUtils.GetQueryErrors(queries);

// Split and normalize all statements
foreach (var query in queries)
{
    var statements = QueryUtils.SplitStatements(query);
    var normalized = QueryUtils.NormalizeStatements(query);
}
```

### Error Handling

The library uses custom exception types for different operations.

```csharp
using var parser = new Parser();
try
{
    var result = parser.Parse("INVALID SQL");
    if (result.IsError)
    {
        throw new ParseException(result.Error!, "INVALID SQL");
    }
}
catch (ParseException ex)
{
    Console.WriteLine($"Parse error in query '{ex.Query}': {ex.ParseError}");
}
catch (DeparseException ex)
{
    Console.WriteLine($"Deparse error: {ex.DeparseError}");
}
catch (SplitException ex)
{
    Console.WriteLine($"Split error: {ex.SplitError}");
}
catch (ScanException ex)
{
    Console.WriteLine($"Scan error: {ex.ScanError}");
}
catch (PlpgsqlParseException ex)
{
    Console.WriteLine($"PL/pgSQL parse error: {ex.PlpgsqlParseError}");
}
catch (NativeLibraryException ex)
{
    Console.WriteLine($"Native library error: {ex.Message}");
}
```

## Performance Tips

1.  **Reuse parser instances**: Avoid creating new `Parser` instances for each operation.
2.  **Use async methods**: Offload work to a background thread for better responsiveness in UI or server applications.
3.  **Process in parallel**: Use `ParseManyAsync` for high-throughput batch processing.
4.  **Dispose properly**: Use `using` statements or call `Dispose()` to release resources.
5.  **Use static Quick methods**: Ideal for infrequent, one-off operations.
6.  **Cache results**: Cache parsing results for frequently seen queries.

## Native Dependencies

This library requires the `libpg_query` native library. The NuGet package includes pre-compiled binaries for:

- Windows (x64, ARM64)
- Linux (x64, ARM64)
- macOS (x64, ARM64)

## Supported Features

### ✅ Implemented (Core Features)
- SQL parsing to AST (JSON & Protobuf)
- Query normalization
- Query fingerprinting
- AST deparsing to SQL
- Multi-statement splitting
- Query tokenization/scanning
- PL/pgSQL parsing
- Comprehensive error handling
- Async operations
- Batch processing

### ✨ .NET-Specific Enhancements
- Strong typing with nullable reference types
- Record types for immutable data models
- Extension methods for a fluent async API
- Comprehensive XML documentation
- Advanced error handling with custom exceptions
- Static utility class (`QueryUtils`) for common operations
- Performance optimizations for high-throughput scenarios

## Thread Safety

The `Parser` class is **not thread-safe**. Create separate instances for each thread or use proper synchronization. The static `Quick*` methods and `QueryUtils` methods are thread-safe.

## Contributing

Contributions are welcome! Please ensure that:

1. All tests pass.
2. Code follows .NET conventions.
3. Public APIs are documented with XML comments.
4. Memory management is handled properly.

## License

MIT License - see the LICENSE file for details.

## Acknowledgments

This library is built on top of the excellent [libpg_query](https://github.com/pganalyze/libpg_query) project, which embeds the PostgreSQL parser.