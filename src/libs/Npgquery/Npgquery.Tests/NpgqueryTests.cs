using System.Text.Json;
using Google.Protobuf;
using Npgquery;
using PgQuery;
using Xunit;

namespace Npgquery.Tests;

public class ParserTests : IDisposable
{
    private readonly Parser _parser;

    public ParserTests()
    {
        _parser = new Parser();
    }

    public void Dispose()
    {
        _parser.Dispose();
    }

    [Fact]
    public void Parse_ValidQuery_ReturnsSuccess()
    {
        // Arrange
        var query = "SELECT * FROM users WHERE id = 1";

        // Act
        var result = _parser.Parse(query);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.ParseTree);
        Assert.Null(result.Error);
        Assert.Equal(query, result.Query);
    }

    [Fact]
    public void Parse_InvalidQuery_ReturnsError()
    {
        // Arrange
        var query = "INVALID SQL SYNTAX";

        // Act
        var result = _parser.Parse(query);

        // Assert
        Assert.True(result.IsError);
        Assert.Null(result.ParseTree);
        Assert.NotNull(result.Error);
        Assert.Equal(query, result.Query);
    }

    [Fact]
    public void Normalize_ValidQuery_ReturnsNormalized()
    {
        // Arrange
        var query = "SELECT * FROM users /* comment */ WHERE id = 1";

        // Act
        var result = _parser.Normalize(query);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.NormalizedQuery);
        Assert.Null(result.Error);
        Assert.Contains("SELECT", result.NormalizedQuery);
        //Assert.DoesNotContain("comment", result.NormalizedQuery); //TODO determine if the response is accurate
    }

    [Fact]
    public void Fingerprint_SimilarQueries_ReturnsSameFingerprint()
    {
        // Arrange
        var query1 = "SELECT * FROM users WHERE id = 1";
        var query2 = "SELECT * FROM users WHERE id = 2";

        // Act
        var fp1 = _parser.Fingerprint(query1);
        var fp2 = _parser.Fingerprint(query2);

        // Assert
        Assert.True(fp1.IsSuccess);
        Assert.True(fp2.IsSuccess);
        Assert.Equal(fp1.Fingerprint, fp2.Fingerprint);
    }

    [Fact]
    public void IsValid_ValidQuery_ReturnsTrue()
    {
        // Arrange
        var query = "SELECT 1";

        // Act
        var isValid = _parser.IsValid(query);

        // Assert
        Assert.True(isValid);
    }

    [Fact]
    public void IsValid_InvalidQuery_ReturnsFalse()
    {
        // Arrange
        var query = "INVALID SQL";

        // Act
        var isValid = _parser.IsValid(query);

        // Assert
        Assert.False(isValid);
    }

    [Fact]
    public void GetError_InvalidQuery_ReturnsErrorMessage()
    {
        // Arrange
        var query = "INVALID SQL";

        // Act
        var error = _parser.GetError(query);

        // Assert
        Assert.NotNull(error);
        Assert.NotEmpty(error);
    }

    [Fact]
    public void GetError_ValidQuery_ReturnsNull()
    {
        // Arrange
        var query = "SELECT 1";

        // Act
        var error = _parser.GetError(query);

        // Assert
        Assert.Null(error);
    }

    [Theory]
    [InlineData("SELECT * FROM users", "SELECT")]
    [InlineData("INSERT INTO users (name) VALUES ('test')", "INSERT")]
    [InlineData("UPDATE users SET name = 'test'", "UPDATE")]
    [InlineData("DELETE FROM users WHERE id = 1", "DELETE")]
    public void QuickParse_StaticMethod_Works(string query, string expectedType)
    {
        // Act
        var result = Parser.QuickParse(query);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.ParseTree);
        
        // Use the expectedType parameter to verify the query type
        var actualType = QueryUtils.GetQueryType(query);
        Assert.Equal(expectedType, actualType);
    }

    [Fact]
    public void Parse_WithNullQuery_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _parser.Parse(null!));
    }

    [Fact]
    public void Parse_AfterDispose_ThrowsObjectDisposedException()
    {
        // Arrange
        _parser.Dispose();

        // Act & Assert
        Assert.Throws<ObjectDisposedException>(() => _parser.Parse("SELECT 1"));
    }
}

public class QueryUtilsTests
{
    [Fact]
    public void ExtractTableNames_SimpleQuery_ReturnsTableNames()
    {
        // Arrange
        var query = "SELECT * FROM users WHERE id = 1";

        // Act
        var tables = QueryUtils.ExtractTableNames(query);

        // Assert
        Assert.Contains("users", tables);
    }

    [Fact]
    public void ExtractTableNames_JoinQuery_ReturnsAllTableNames()
    {
        // Arrange
        var query = "SELECT u.name, p.title FROM users u JOIN posts p ON u.id = p.user_id";

        // Act
        var tables = QueryUtils.ExtractTableNames(query);

        // Assert
        Assert.Contains("users", tables);
        Assert.Contains("posts", tables);
    }

    [Fact]
    public void HaveSameStructure_SimilarQueries_ReturnsTrue()
    {
        // Arrange
        var query1 = "SELECT * FROM users WHERE id = 1";
        var query2 = "SELECT * FROM users WHERE id = 2";

        // Act
        var result = QueryUtils.HaveSameStructure(query1, query2);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void HaveSameStructure_DifferentQueries_ReturnsFalse()
    {
        // Arrange
        var query1 = "SELECT * FROM users";
        var query2 = "INSERT INTO users (name) VALUES ('test')";

        // Act
        var result = QueryUtils.HaveSameStructure(query1, query2);

        // Assert
        Assert.False(result);
    }

    [Theory]
    [InlineData("SELECT * FROM users", "SELECT")]
    [InlineData("INSERT INTO users (name) VALUES ('test')", "INSERT")]
    [InlineData("UPDATE users SET name = 'test'", "UPDATE")]
    [InlineData("DELETE FROM users WHERE id = 1", "DELETE")]
    public void GetQueryType_VariousQueries_ReturnsCorrectType(string query, string expectedType)
    {
        // Act
        var queryType = QueryUtils.GetQueryType(query);

        // Assert
        Assert.Equal(expectedType, queryType);
    }

    [Fact]
    public void CleanQuery_QueryWithWhitespace_ReturnsCleanedQuery()
    {
        // Arrange
        var query = "  SELECT   *   FROM   users  ";

        // Act
        var cleaned = QueryUtils.CleanQuery(query);

        // Assert
        Assert.False(cleaned.StartsWith(" "));
        Assert.False(cleaned.EndsWith(" "));
    }

    [Fact]
    public void ValidateQueries_MixedQueries_ReturnsValidationResults()
    {
        // Arrange
        var queries = new[] { "SELECT 1", "INVALID SQL", "SELECT 2" };

        // Act
        var results = QueryUtils.ValidateQueries(queries);

        // Assert
        Assert.True(results["SELECT 1"]);
        Assert.False(results["INVALID SQL"]);
        Assert.True(results["SELECT 2"]);
    }

    [Fact]
    public void Parse_SerializeToProtobuf_And_Deparse() {
        // Arrange
        using var parser = new Parser();
        const string query = "SELECT id, name FROM users WHERE active = true";

        // Act
        // Step 1: Parse the SQL query to get the parse tree
        var parseResult = parser.Parse(query);
        Assert.True(parseResult.IsSuccess);
        Assert.NotNull(parseResult.ParseTree);

        var parseTreeJson = parseResult.ParseTree;

        // Step 2: Call deparse with the parse tree
        var deparseResult = parser.Deparse(parseTreeJson);

        // Assert
        Assert.True(deparseResult.IsSuccess);
        Assert.NotNull(deparseResult.Query);
        Assert.Contains("SELECT", deparseResult.Query);
        Assert.Contains("users", deparseResult.Query);
    }

    [Fact]
    public void SimpleSelect_RoundTrip_Through_Protobuf() {
        // Arrange
        using var parser = new Parser();
        const string query = "SELECT 1";

        // Act
        var parseResult = parser.Parse(query);
        Assert.True(parseResult.IsSuccess);

        // For a simple test, we can just verify the parse tree structure
        var parseTree = parseResult.ParseTree;

        var deparseResult = parser.Deparse(parseTree);

        // Assert
        Assert.True(deparseResult.IsSuccess);
        Assert.NotNull(deparseResult.Query);
    }

    #region PL/pgSQL Tests

    [Fact]
    public void ParsePlpgsql_SimpleBlock_ReturnsSuccess()
    {

        // Arrange
        using var _parser = new Parser();
        var plpgsqlCode = @"CREATE OR REPLACE FUNCTION cs_fmt_browser_version(v_name varchar,
                                                  v_version varchar)
RETURNS varchar AS $$
BEGIN
    IF v_version IS NULL THEN
        RETURN v_name;
    END IF;
    RETURN v_name || '/' || v_version;
END;
$$ LANGUAGE plpgsql;";

        // Act
        var result = _parser.ParsePlpgsql(plpgsqlCode);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.ParseTree);
        Assert.Null(result.Error);
        Assert.Equal(plpgsqlCode, result.Query);
    }

    [Fact]
    public void ParsePlpgsql_ConditionalLogic_ReturnsSuccess()
    {
        // Arrange
        using var _parser = new Parser();
        var plpgsqlCode = @"CREATE OR REPLACE FUNCTION cs_fmt_browser_version(v_name varchar,
                                                  v_version varchar)
RETURNS varchar AS $$
BEGIN
    IF v_version IS NULL THEN
        RETURN v_name;
    END IF;
    RETURN v_name || '/' || v_version;
END;
$$ LANGUAGE plpgsql;";

        // Act
        var result = _parser.ParsePlpgsql(plpgsqlCode);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.ParseTree);
        Assert.Null(result.Error);
        Assert.Contains("PLpgSQL_stmt_return", result.ParseTree);
    }

    [Fact]
    public void ParsePlpgsql_LoopStatement_ReturnsSuccess()
    {
        // Arrange
        using var _parser = new Parser();
        var plpgsqlCode = @"CREATE OR REPLACE FUNCTION example_function()
RETURNS INTEGER AS $$
DECLARE
    i INTEGER := 1;
BEGIN
    WHILE i <= 10 LOOP
        RAISE NOTICE 'Current value: %', i;
        i := i + 1;
    END LOOP;
    RETURN i;
END;
$$ LANGUAGE plpgsql;";

        // Act
        var result = _parser.ParsePlpgsql(plpgsqlCode);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.ParseTree);
        Assert.Null(result.Error);
    }

    [Fact]
    public void ParsePlpgsql_ExceptionHandling_ReturnsSuccess()
    {
        // Arrange
        using var _parser = new Parser();
        var plpgsqlCode = @"DO $$
BEGIN
    INSERT INTO users (name, email) VALUES ('John', 'john@example.com');
    RAISE NOTICE 'User created successfully';
EXCEPTION
    WHEN unique_violation THEN
        RAISE NOTICE 'User already exists';
    WHEN OTHERS THEN
        RAISE NOTICE 'An error occurred';
END;
$$;";

        // Act
        var result = _parser.ParsePlpgsql(plpgsqlCode);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.ParseTree);
        Assert.Null(result.Error);
    }

    [Fact]
    public void ParsePlpgsql_ComplexFunction_ReturnsSuccess()
    {
        // Arrange
        using var _parser = new Parser();
        var plpgsqlCode = @"DO $$
        DECLARE
                total_count INTEGER := 0;
                user_rec RECORD;
            BEGIN
                FOR user_rec IN SELECT * FROM users WHERE active = true LOOP
                    total_count := total_count + 1;
                    UPDATE users SET last_accessed = NOW() WHERE id = user_rec.id;
                END LOOP;
                
                IF total_count > 100 THEN
                    RAISE WARNING 'High user count: %', total_count;
                END IF;
                
                RETURN total_count;
            EXCEPTION
                WHEN OTHERS THEN
                    RAISE EXCEPTION 'Error processing users: %', SQLERRM;
            END;
            $$";

        // Act
        var result = _parser.ParsePlpgsql(plpgsqlCode);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.ParseTree);
        Assert.Null(result.Error);
    }

    [Fact]
    public void ParsePlpgsql_EmptyBlock_ReturnsSuccess()
    {
        // Arrange
        using var _parser = new Parser();
        var plpgsqlCode = "DO $$ BEGIN END; $$";

        // Act
        var result = _parser.ParsePlpgsql(plpgsqlCode);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.ParseTree);
        Assert.Null(result.Error);
    }

    [Fact]
    public void ParsePlpgsql_InvalidSyntax_ReturnsError()
    {
        // Arrange
        using var _parser = new Parser();
        var plpgsqlCode = @"BEGIN
                INVALID SYNTAX HERE
            END;";

        // Act
        var result = _parser.ParsePlpgsql(plpgsqlCode);

        // Assert
        Assert.True(result.IsError);
        Assert.Null(result.ParseTree);
        Assert.NotNull(result.Error);
        Assert.Equal(plpgsqlCode, result.Query);
    }

    [Fact]
    public void ParsePlpgsql_MissingEnd_ReturnsError()
    {
        // Arrange
        using var _parser = new Parser();
        var plpgsqlCode = @"BEGIN
                RETURN 42;";

        // Act
        var result = _parser.ParsePlpgsql(plpgsqlCode);

        // Assert
        Assert.True(result.IsError);
        Assert.Null(result.ParseTree);
        Assert.NotNull(result.Error);
    }

    [Fact]
    public void ParsePlpgsql_NullInput_ThrowsException()
    {
        // Act & Assert
        using var _parser = new Parser();
        Assert.Throws<ArgumentNullException>(() => _parser.ParsePlpgsql(null!));
    }

    [Theory]
    [InlineData("DO $$ BEGIN RETURN 42; END; $$")]
    [InlineData(@"DO $$ DECLARE x INTEGER; BEGIN x := 1; RETURN x; END; $$")]
    [InlineData(@"DO $$ 
DECLARE
    result TEXT;
BEGIN 
    IF TRUE THEN 
        result := 'yes'; 
    ELSE 
        result := 'no'; 
    END IF; 
    RAISE NOTICE 'Result: %', result;
END; 
$$;")]
    public void ParsePlpgsql_ValidCodes_ReturnSuccess(string plpgsqlCode)
    {
        // Act
        using var _parser = new Parser();
        var result = _parser.ParsePlpgsql(plpgsqlCode);

        // Assert
        Assert.True(result.IsSuccess, $"Expected success for: {plpgsqlCode}");
        Assert.NotNull(result.ParseTree);
        Assert.Null(result.Error);
    }

    [Theory]
    [InlineData("INVALID")]
    [InlineData("BEGIN INVALID SYNTAX END;")]
    [InlineData("BEGIN RETURN; END")]  // Missing semicolon before END
    public void ParsePlpgsql_InvalidCodes_ReturnError(string plpgsqlCode)
    {
        // Act
        using var _parser = new Parser();
        var result = _parser.ParsePlpgsql(plpgsqlCode);

        // Assert
        Assert.True(result.IsError, $"Expected error for: {plpgsqlCode}");
        Assert.Null(result.ParseTree);
        Assert.NotNull(result.Error);
    }

    #endregion

    #region Utility Tests for PL/pgSQL

    [Fact]
    public void QueryUtils_IsValidPlpgsql_ValidCode_ReturnsTrue()
    {
        // Arrange
        var validPlpgsqlCode = @"DO $$ BEGIN
                RETURN 'test';
            END; $$";

        // Act
        var isValid = QueryUtils.IsValidPlpgsql(validPlpgsqlCode);

        // Assert
        Assert.True(isValid);
    }

    [Fact]
    public void QueryUtils_IsValidPlpgsql_InvalidCode_ReturnsFalse()
    {
        // Arrange
        var invalidPlpgsqlCode = @"BEGIN
                INVALID SYNTAX
            END;";

        // Act
        var isValid = QueryUtils.IsValidPlpgsql(invalidPlpgsqlCode);

        // Assert
        Assert.False(isValid);
    }

    [Theory]
    [InlineData("DO $$ BEGIN RETURN 42; END; $$", true)]
    [InlineData("DO $$ BEGIN NULL; END; $$", true)]
    [InlineData("INVALID", false)]
    [InlineData("BEGIN", true)]
    [InlineData("", true)]
    public void QueryUtils_IsValidPlpgsql_VariousCodes_ReturnsExpected(string plpgsqlCode, bool expected)
    {
        // Act
        var isValid = QueryUtils.IsValidPlpgsql(plpgsqlCode);

        // Assert
        Assert.Equal(expected, isValid);
    }

    #endregion

}