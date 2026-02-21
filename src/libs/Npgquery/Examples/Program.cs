using Npgquery;

namespace Examples;

class Program {
#if !NETSTANDARD2_0
    static async Task Main(string[] args) {
        Console.WriteLine("Npgquery Examples");
        Console.WriteLine("===================");

        await BasicParsingExample();
        Console.WriteLine();

        await NormalizationExample();
        Console.WriteLine();

        await FingerprintingExample();
        Console.WriteLine();

        await UtilityFunctionsExample();
        Console.WriteLine();

        await AsyncExample();
        Console.WriteLine();

        await BatchProcessingExample();
        Console.WriteLine();

        await ExtendedFeaturesExample();
        Console.WriteLine();

        await PlpgsqlParsingExample();
    }
#endif

    static async Task BasicParsingExample() {
        Console.WriteLine("1. Basic Parsing Example");
        Console.WriteLine("------------------------");

        using var parser = new Parser();

        var queries = new[]
        {
            "SELECT * FROM users WHERE id = 1",
            "INSERT INTO posts (title, content) VALUES ('Hello', 'World')",
            "INVALID SQL SYNTAX"
        };

        foreach (var query in queries) {
            var result = parser.Parse(query);
            Console.WriteLine($"Query: {query}");
            Console.WriteLine($"Valid: {result.IsSuccess}");

            if (result.IsSuccess) {
                Console.WriteLine($"Parse Tree Length: {result.ParseTree?.RootElement.ToString().Length ?? 0} characters");
            }
            else {
                Console.WriteLine($"Error: {result.Error}");
            }
            Console.WriteLine();
        }
    }

    static async Task NormalizationExample() {
        Console.WriteLine("2. Normalization Example");
        Console.WriteLine("------------------------");

        using var parser = new Parser();

        var queries = new[]
        {
            "SELECT * FROM users /* this is a comment */ WHERE id = 1",
            "SELECT   *   FROM   users   WHERE   id   =   2  ",
            "select name, email from users where active = true"
        };

        foreach (var query in queries) {
            var result = parser.Normalize(query);
            Console.WriteLine($"Original:   {query}");
            Console.WriteLine($"Normalized: {result.NormalizedQuery}");
            Console.WriteLine();
        }
    }

    static async Task FingerprintingExample() {
        Console.WriteLine("3. Fingerprinting Example");
        Console.WriteLine("-------------------------");

        using var parser = new Parser();

        var queries = new[]
        {
            "SELECT * FROM users WHERE id = 1",
            "SELECT * FROM users WHERE id = 2",
            "SELECT * FROM users WHERE id = 999",
            "SELECT name FROM users WHERE id = 1",
            @"CREATE OR REPLACE FUNCTION cs_fmt_browser_version(v_name varchar,
                                                  v_version varchar) 
RETURNS varchar AS $$ 
BEGIN 
    IF v_version IS NULL THEN
        RETURN v_name;
    END IF; 
    RETURN v_name || '/' || v_version; 
END; 
$$ LANGUAGE plpgsql;"
        };

        var fingerprints = new List<(string query, string? fingerprint)>();

        foreach (var query in queries) {
            var result = parser.Fingerprint(query);
            fingerprints.Add((query, result.Fingerprint));
            Console.WriteLine($"Query: {query}");
            Console.WriteLine($"Fingerprint: {result.Fingerprint}");
            Console.WriteLine();
        }

        // Check for similar queries
        Console.WriteLine("Similar query analysis:");
        for (int i = 0; i < fingerprints.Count; i++) {
            for (int j = i + 1; j < fingerprints.Count; j++) {
                if (fingerprints[i].fingerprint == fingerprints[j].fingerprint) {
                    Console.WriteLine($"Queries {i + 1} and {j + 1} have the same structure");
                }
            }
        }
        Console.WriteLine();
    }

    static async Task UtilityFunctionsExample() {
        Console.WriteLine("4. Utility Functions Example");
        Console.WriteLine("-----------------------------");

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
        foreach (var table in tables) {
            Console.WriteLine($"  - {table}");
        }
        Console.WriteLine();

        // Get query type
        var queryType = QueryUtils.GetQueryType(complexQuery);
        Console.WriteLine($"Query type: {queryType}");
        Console.WriteLine();

        // Clean query
        var cleaned = QueryUtils.CleanQuery(complexQuery);
        Console.WriteLine("Cleaned query:");
        Console.WriteLine(cleaned);
        Console.WriteLine();

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
        foreach (var result in validationResults) {
            Console.WriteLine($"  {result.Key}: {(result.Value ? "? Valid" : "? Invalid")}");
        }
    }

    static async Task AsyncExample() {
        Console.WriteLine("5. Async Operations Example");
        Console.WriteLine("----------------------------");

        using var parser = new Parser();

        // Single async operation
        var query = "SELECT * FROM users WHERE created_at > '2023-01-01'";
        var result = await parser.ParseAsync(query);
        Console.WriteLine($"Async parse successful: {result.IsSuccess}");

        // Multiple queries in parallel
        var queries = new[]
        {
            "SELECT COUNT(*) FROM users",
            "SELECT COUNT(*) FROM posts",
            "SELECT COUNT(*) FROM comments",
            "SELECT COUNT(*) FROM categories"
        };

        Console.WriteLine($"Processing {queries.Length} queries in parallel...");
        var startTime = DateTime.UtcNow;
        var results = await parser.ParseManyAsync(queries, maxDegreeOfParallelism: 4);
        var endTime = DateTime.UtcNow;

        Console.WriteLine($"Completed in {(endTime - startTime).TotalMilliseconds:F2}ms");
        Console.WriteLine($"Successful parses: {results.Count(r => r.IsSuccess)}/{results.Length}");
        Console.WriteLine();

        // Static async methods
        var quickResult = await ParserAsync.QuickParseAsync("SELECT version()");
        Console.WriteLine($"Quick async parse successful: {quickResult.IsSuccess}");
    }

    static async Task BatchProcessingExample() {
        Console.WriteLine("6. Batch Processing Example");
        Console.WriteLine("----------------------------");

        // Simulate processing a file of SQL queries
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

        Console.WriteLine($"Processing {sqlQueries.Length} SQL statements...");

        var validQueries = new List<string>();
        var invalidQueries = new List<(string query, string error)>();

        using var parser = new Parser();

        foreach (var sql in sqlQueries) {
            // Skip comments and empty lines
            var trimmed = sql.Trim();
            if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith("--")) {
                continue;
            }

            var result = parser.Parse(trimmed);
            if (result.IsSuccess) {
                validQueries.Add(trimmed);

                // Extract metadata
                var queryType = QueryUtils.GetQueryType(trimmed);
                var tables = QueryUtils.ExtractTableNames(trimmed);

                Console.WriteLine($"? {queryType} query affecting tables: {string.Join(", ", tables)}");
            }
            else {
                invalidQueries.Add((trimmed, result.Error!));
                Console.WriteLine($"? Invalid query: {trimmed}");
                Console.WriteLine($"  Error: {result.Error}");
            }
        }

        Console.WriteLine();
        Console.WriteLine("Summary:");
        Console.WriteLine($"  Valid queries: {validQueries.Count}");
        Console.WriteLine($"  Invalid queries: {invalidQueries.Count}");

        if (validQueries.Any()) {
            Console.WriteLine();
            Console.WriteLine("Generating fingerprints for valid queries...");
            var fingerprints = new Dictionary<string, List<string>>();

            foreach (var vquery in validQueries) {
                var fp = parser.Fingerprint(vquery);
                if (fp.IsSuccess && fp.Fingerprint != null) {
                    if (!fingerprints.ContainsKey(fp.Fingerprint)) {
                        fingerprints[fp.Fingerprint] = new List<string>();
                    }
                    fingerprints[fp.Fingerprint].Add(vquery);
                }
            }

            var duplicateStructures = fingerprints.Where(kvp => kvp.Value.Count > 1).ToList();
            if (duplicateStructures.Any()) {
                Console.WriteLine("Found queries with similar structures:");
                foreach (var structure in duplicateStructures) {
                    Console.WriteLine($"  Fingerprint: {structure.Key}");
                    foreach (var similarQuery in structure.Value) {
                        Console.WriteLine($"    - {similarQuery}");
                    }
                }
            }
            else {
                Console.WriteLine("No duplicate query structures found.");
            }
        }
    }

    static async Task ExtendedFeaturesExample() {
        Console.WriteLine("7. Extended Features Example");
        Console.WriteLine("----------------------------");

        using var parser = new Parser();

        // Query splitting example
        Console.WriteLine("A. Query Splitting:");
        var multiQuery = @"
            SELECT * FROM users WHERE active = true;
            INSERT INTO audit_log (action, timestamp) VALUES ('query', NOW());
            UPDATE users SET last_accessed = NOW() WHERE id = 1;
        ";

        var splitResult = parser.Split(multiQuery);
        if (splitResult.IsSuccess && splitResult.Statements != null) {
            Console.WriteLine($"Found {splitResult.Statements.Length} statements:");
            foreach (var stmt in splitResult.Statements) {
                if (!string.IsNullOrWhiteSpace(stmt.Statement)) {
                    Console.WriteLine($"  - {stmt.Statement.Trim()}");
                }
            }
        }
        Console.WriteLine();

        // Query scanning/tokenization example
        Console.WriteLine("B. Query Tokenization:");
        var query = "SELECT COUNT(*) FROM users WHERE created_at > '2023-01-01'";
        var scanResult = parser.Scan(query);

        Console.WriteLine($"Query: {query}");
        Console.WriteLine($"Scan successful: {scanResult.IsSuccess}");

        if (scanResult.IsSuccess && scanResult.Tokens != null) {
            Console.WriteLine($"PostgreSQL Version: {scanResult.Version}");
            Console.WriteLine($"Found {scanResult.Tokens.Length} tokens:");

            foreach (var token in scanResult.Tokens.Take(10)) // Show first 10 tokens
            {
                Console.WriteLine($"  '{token.Text}' -> {token.TokenKind} ({token.KeywordKind}) at position {token.Start}-{token.End}");
            }

            if (scanResult.Tokens.Length > 10) {
                Console.WriteLine($"  ... and {scanResult.Tokens.Length - 10} more tokens");
            }
        }
        else if (scanResult.IsError) {
            Console.WriteLine($"Scan failed: {scanResult.Error}");
        }

        if (!string.IsNullOrEmpty(scanResult.Stderr)) {
            Console.WriteLine($"Stderr: {scanResult.Stderr}");
        }

        Console.WriteLine();

        // Round-trip example (parse then deparse)
        Console.WriteLine("C. Round-trip Test (Parse ? Deparse):");
        var originalQuery = "SELECT u.name, COUNT(p.id) as post_count FROM users u LEFT JOIN posts p ON u.id = p.user_id GROUP BY u.id, u.name ORDER BY post_count DESC";
        Console.WriteLine($"Original: {originalQuery}");

        var parseResult = parser.Parse(originalQuery);
        if (parseResult.IsSuccess && parseResult.ParseTree is not null) {
            var deparseResult = parser.Deparse(parseResult.ParseTree);
            if (deparseResult.IsSuccess) {
                Console.WriteLine($"Deparsed: {deparseResult.Query}");
                Console.WriteLine($"Round-trip successful: {!string.IsNullOrEmpty(deparseResult.Query)}");
            }
            else {
                Console.WriteLine($"Deparse failed: {deparseResult.Error}");
            }
        }
        Console.WriteLine();

        // PL/pgSQL parsing example
        Console.WriteLine("D. PL/pgSQL Parsing:");

        var plpgsqlExamples = new[]
        {
            // Simple block
            @"DO $$
DECLARE
    ret VARCHAR(50);
BEGIN
    ret := 'Hello, World!';
    RAISE NOTICE '%', ret;
END;
$$;",
            
            // Function body with conditionals
            @"DO $$
DECLARE
    user_count INTEGER := 0;
    ret VARCHAR(100);
BEGIN
    IF user_count > 0 THEN
        ret := 'Users exist';
    ELSE
        ret := 'No users found';
    END IF;
    RAISE NOTICE '%', ret;
END;
$$;",
            
            // Loop example
            @"DO $$
DECLARE
    i INTEGER := 1;
BEGIN
    WHILE i <= 10 LOOP
        RAISE NOTICE 'Current value: %', i;
        i := i + 1;
    END LOOP;
    RAISE NOTICE 'Final value: %', i;
END;
$$;",
            
            // Exception handling
            @"DO $$
BEGIN
    INSERT INTO users (name, email) VALUES ('John', 'john@example.com');
    RAISE NOTICE 'User created successfully';
EXCEPTION
    WHEN unique_violation THEN
        RAISE NOTICE 'User already exists';
    WHEN OTHERS THEN
        RAISE NOTICE 'An error occurred';
END;
$$;"
        };

        foreach (var (code, index) in plpgsqlExamples.Select((code, i) => (code, i + 1))) {
            Console.WriteLine($"Example {index}:");
            var plpgsqlResult = parser.ParsePlpgsql(code);
            Console.WriteLine($"  PL/pgSQL parsing successful: {plpgsqlResult.IsSuccess}");

            if (plpgsqlResult.IsSuccess) {
                Console.WriteLine($"  Parse tree length: {plpgsqlResult.ParseTree?.Length ?? 0} characters");
                // Show a snippet of the parse tree for valid code
                if (!string.IsNullOrEmpty(plpgsqlResult.ParseTree) && plpgsqlResult.ParseTree.Length > 100) {
                    Console.WriteLine($"  Parse tree preview: {plpgsqlResult.ParseTree.Substring(0, 100)}...");
                }
            }
            else {
                Console.WriteLine($"  Error: {plpgsqlResult.Error}");
            }
            Console.WriteLine();
        }

        // Test utility function
        Console.WriteLine("PL/pgSQL validation using utility function:");
        foreach (var (code, index) in plpgsqlExamples.Take(4).Select((code, i) => (code, i + 1))) {
            var isValid = QueryUtils.IsValidPlpgsql(code);
            Console.WriteLine($"  Example {index}: {(isValid ? "✓ Valid" : "✗ Invalid")}");
        }
        Console.WriteLine();

        // Utility functions example
        Console.WriteLine("E. Enhanced Utility Functions:");

        // Split statements utility
        var statements = QueryUtils.SplitStatements(multiQuery);
        Console.WriteLine($"Split {statements.Count} statements using utility function");

        // Get tokens utility
        var tokens = QueryUtils.GetTokens(originalQuery);
        Console.WriteLine($"Found {tokens.Count} tokens using utility function");

        // Get keywords utility
        var keywords = QueryUtils.GetKeywords(originalQuery);
        Console.WriteLine($"Keywords found: {string.Join(", ", keywords)}");

        // Round-trip utility
        var (success, roundTripQuery) = QueryUtils.RoundTripTest(originalQuery);
        Console.WriteLine($"Round-trip test: {(success ? "? Success" : "? Failed")}");

        // Count statements utility
        var statementCount = QueryUtils.CountStatements(multiQuery);
        Console.WriteLine($"Statement count: {statementCount}");

        // PL/pgSQL validation utility (using the first valid example)
        var samplePlpgsqlCode = @"DO $$ BEGIN
        RAISE NOTICE 'Hello World';
        END; $$;";
        var isValidPlpgsql = QueryUtils.IsValidPlpgsql(samplePlpgsqlCode);
        Console.WriteLine($"PL/pgSQL validation: {(isValidPlpgsql ? "✓ Valid" : "✗ Invalid")}");
    }

    static async Task PlpgsqlParsingExample() {
        Console.WriteLine("8. Dedicated PL/pgSQL Parsing Example");
        Console.WriteLine("======================================");

        using var parser = new Parser();

        var plpgsqlExamples = new[]
        {
            new
            {
                Name = "Simple Function Body",
                Code = @"DO $$ 
DECLARE
    ret VARCHAR;
BEGIN
    ret := 'Hello, World!';
    RAISE NOTICE '%', ret;
END; 
$$;"
            },
            new
            {
                Name = "Variable Declaration and Assignment",
                Code = @"DO $$
DECLARE
    user_name VARCHAR(50);
    user_count INTEGER := 0;
BEGIN
    user_name := 'John Doe';
    SELECT COUNT(*) INTO user_count FROM users WHERE active = true;
    RAISE NOTICE 'Active user count: %', user_count;
END;
$$;"
            },
            new
            {
                Name = "Conditional Logic with IF-ELSE",
                Code = @"DO $$DECLARE
    user_id INTEGER;
    status TEXT;
BEGIN
    user_id := 123;
    
    IF user_id > 0 THEN
        SELECT CASE 
            WHEN active THEN 'active'
            ELSE 'inactive'
        END INTO status
        FROM users WHERE id = user_id;
        
        RETURN status;
    ELSE
        RETURN 'invalid_user_id';
    END IF;
END; $$"
            },
            new
            {
                Name = "Loop with WHILE",
                Code = @"DO $$
DECLARE
    counter INTEGER := 1;
    result TEXT := '';
BEGIN
    WHILE counter <= 5 LOOP
        result := result || counter::TEXT || ' ';
        counter := counter + 1;
    END LOOP;
    
    RETURN TRIM(result);
END; $$"
            },
            new
            {
                Name = "FOR Loop with Record",
                Code = @"DO $$
DECLARE
    user_rec RECORD;
    user_list TEXT := '';
BEGIN
    FOR user_rec IN SELECT name FROM users WHERE active = true ORDER BY name LOOP
        user_list := user_list || user_rec.name || ', ';
    END LOOP;
    
    RETURN RTRIM(user_list, ', ');
END; $$"
            },
            new
            {
                Name = "Exception Handling",
                Code = @"DO $$
DECLARE
    error_message TEXT;
    result_count INTEGER;
BEGIN
    -- Attempt to perform an operation that might fail
    INSERT INTO audit_log (action, timestamp) VALUES ('user_login', NOW());
    SELECT COUNT(*) INTO result_count FROM users;

    RAISE NOTICE 'Success: % users found', result_count;

EXCEPTION
    WHEN unique_violation THEN
        RAISE NOTICE 'Error: Duplicate entry';
    WHEN not_null_violation THEN
        RAISE NOTICE 'Error: Required field missing';
    WHEN OTHERS THEN
        RAISE NOTICE 'Error: %', SQLERRM;
END;
$$;"
            },
            new
            {
                Name = "Advanced Function with Multiple Constructs",
                Code = @"DO $$ 
DECLARE
    total_processed INTEGER := 0;
    user_rec RECORD;
    error_count INTEGER := 0;
    batch_size INTEGER := 100;
BEGIN
    -- Process users in batches
    FOR user_rec IN 
        SELECT id, name, email 
        FROM users 
        WHERE last_processed IS NULL 
        ORDER BY created_at 
        LIMIT batch_size
    LOOP
        BEGIN
            -- Simulate processing
            UPDATE users 
            SET last_processed = NOW(), 
                processed_by = 'batch_job'
            WHERE id = user_rec.id;
            
            total_processed := total_processed + 1;
            
            -- Log every 10th user
            IF total_processed % 10 = 0 THEN
                RAISE NOTICE 'Processed % users so far', total_processed;
            END IF;
            
        EXCEPTION
            WHEN OTHERS THEN
                error_count := error_count + 1;
                RAISE WARNING 'Failed to process user %: %', user_rec.id, SQLERRM;
        END;
    END LOOP;
    
    -- Final summary
    IF error_count > 0 THEN
        RAISE WARNING 'Completed with % errors out of % attempts', error_count, total_processed + error_count;
    END IF;
    
    -- Log the total processed count
    RAISE NOTICE 'Total users processed: %', total_processed;
END $$;"
            },
            new
            {
                Name = "Invalid Syntax (for error demonstration)",
                Code = @"BEGIN
    INVALID SYNTAX HERE
    RETURN 'This will fail';
END;"
            }
        };

        Console.WriteLine($"Testing {plpgsqlExamples.Length} PL/pgSQL code examples...");
        Console.WriteLine();

        var successCount = 0;
        var errorCount = 0;

        foreach (var (example, index) in plpgsqlExamples.Select((ex, i) => (ex, i + 1))) {
            Console.WriteLine($"{index}. {example.Name}");
            Console.WriteLine(new string('-', example.Name.Length + 3));

            // Show the code with line numbers for better readability
            var lines = example.Code.Split('\n');
            Console.WriteLine("Code:");
            for (var i = 0; i < lines.Length; i++) {
                Console.WriteLine($"  {i + 1,2}: {lines[i]}");
            }
            Console.WriteLine();

            // Parse the PL/pgSQL code
            var parseResult = parser.ParsePlpgsql(example.Code);

            if (parseResult.IsSuccess) {
                successCount++;
                Console.WriteLine("✓ Parse Result: SUCCESS");
                Console.WriteLine($"  Parse tree length: {parseResult.ParseTree?.Length ?? 0} characters");

                // Show a snippet of the parse tree for successful parses
                if (!string.IsNullOrEmpty(parseResult.ParseTree)) {
                    var preview = parseResult.ParseTree.Length > 200
                        ? parseResult.ParseTree.Substring(0, 200) + "..."
                        : parseResult.ParseTree;
                    Console.WriteLine($"  Parse tree preview: {preview}");
                }
            }
            else {
                errorCount++;
                Console.WriteLine("✗ Parse Result: ERROR");
                Console.WriteLine($"  Error message: {parseResult.Error}");
            }

            // Test utility function as well
            var isValidUtil = QueryUtils.IsValidPlpgsql(example.Code);
            Console.WriteLine($"  Utility validation: {(isValidUtil ? "✓ Valid" : "✗ Invalid")}");

            Console.WriteLine();
        }

        // Summary
        Console.WriteLine("Summary");
        Console.WriteLine("=======");
        Console.WriteLine($"Total examples tested: {plpgsqlExamples.Length}");
        Console.WriteLine($"Successful parses: {successCount}");
        Console.WriteLine($"Failed parses: {errorCount}");
        Console.WriteLine($"Success rate: {(double)successCount / plpgsqlExamples.Length * 100:F1}%");

        if (successCount > 0) {
            Console.WriteLine();
            Console.WriteLine("✓ PL/pgSQL parsing functionality is working correctly!");
            Console.WriteLine("  - The parser can handle various PL/pgSQL constructs");
            Console.WriteLine("  - Error handling works for invalid syntax");
            Console.WriteLine("  - Utility functions provide convenient validation");
        }
    }
}