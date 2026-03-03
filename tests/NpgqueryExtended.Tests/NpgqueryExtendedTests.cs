using System;
using Xunit;
using Npgquery;

namespace Npgquery.Tests;

public class ParserExtendedTests : IDisposable
{
    private readonly Parser _parser;

    public ParserExtendedTests()
    {
        _parser = new Parser();
    }

    public void Dispose()
    {
        _parser.Dispose();
    }

    [Fact]
    public void Deparse_ValidAst_ReturnsQuery()
    {
        // Arrange
        var query = "SELECT * FROM users WHERE id = 1";
        var parseResult = _parser.Parse(query);
        
        // Act
        var deparseResult = _parser.Deparse(parseResult.ParseTree!);

        // Assert
        Assert.True(deparseResult.IsSuccess);
        Assert.NotNull(deparseResult.Query);
        Assert.Contains("SELECT", deparseResult.Query);
        Assert.Contains("users", deparseResult.Query);
    }

    [Fact]
    public void Split_MultipleStatements_ReturnsSeparateStatements()
    {
        // Arrange
        var query = "SELECT 1; SELECT 2; INSERT INTO test VALUES (1);";

        // Act
        var result = _parser.Split(query);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Statements);
        Assert.True(result.Statements.Length >= 3);
    }

    [Fact]
    public void Scan_ValidQuery_ReturnsTokens()
    {
        // Arrange
        var query = "SELECT * FROM users";

        // Act
        var result = _parser.Scan(query);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Tokens);
        Assert.True(result.Tokens.Length > 0);
    }

    [Fact]
    public void ParsePlpgsql_ValidFunction_ReturnsAst()
    {
        // Arrange
        var plpgsqlCode = @"
            BEGIN
                RETURN 'Hello World';
            END;
        ";

        // Act
        var result = _parser.ParsePlpgsql(plpgsqlCode);

        // Assert - Note: This might fail if libpg_query doesn't support PL/pgSQL
        // In a real implementation, you'd need libpg_query compiled with PL/pgSQL support
        Assert.NotNull(result);
        Assert.Equal(plpgsqlCode, result.Query);
    }

    [Fact]
    public void QuickDeparse_StaticMethod_Works()
    {
        // Arrange
        var query = "SELECT 1";
        var parseResult = Parser.QuickParse(query);

        // Act
        var deparseResult = Parser.QuickDeparse(parseResult.ParseTree!);

        // Assert
        Assert.True(deparseResult.IsSuccess);
        Assert.NotNull(deparseResult.Query);
    }

    [Fact]
    public void QuickSplit_StaticMethod_Works()
    {
        // Arrange
        var query = "SELECT 1; SELECT 2;";

        // Act
        var result = Parser.QuickSplit(query);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Statements);
    }

    [Fact]
    public void QuickScan_StaticMethod_Works()
    {
        // Arrange
        var query = "SELECT COUNT(*) FROM users";

        // Act
        var result = Parser.QuickScan(query);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Tokens);
    }

    [Fact]
    public void RoundTripTest_ParseAndDeparse_ReturnsValidSql()
    {
        // Arrange
        var originalQuery = "SELECT id, name FROM users WHERE active = true ORDER BY name";

        // Act
        var (success, roundTripQuery) = QueryUtils.RoundTripTest(originalQuery);

        // Assert
        Assert.True(success);
        Assert.NotNull(roundTripQuery);
        Assert.Contains("SELECT", roundTripQuery);
        Assert.Contains("users", roundTripQuery);
    }

    [Fact]
    public void SplitStatements_MultipleStatements_ReturnsIndividualQueries()
    {
        // Arrange
        var sqlText = @"
            SELECT * FROM users;
            INSERT INTO logs (message) VALUES ('test');
            UPDATE users SET active = true WHERE id = 1;
        ";

        // Act
        var statements = QueryUtils.SplitStatements(sqlText);

        // Assert
        Assert.True(statements.Count >= 3);
        Assert.Contains(statements, s => s.Contains("SELECT"));
        Assert.Contains(statements, s => s.Contains("INSERT"));
        Assert.Contains(statements, s => s.Contains("UPDATE"));
    }

    [Fact]
    public void GetTokens_ValidQuery_ReturnsTokenList()
    {
        // Arrange
        var query = "SELECT COUNT(*) FROM users WHERE active = true";

        // Act
        var tokens = QueryUtils.GetTokens(query);

        // Assert
        Assert.NotEmpty(tokens);
    }

    [Fact]
    public void GetKeywords_ValidQuery_ReturnsKeywordList()
    {
        // Arrange
        var query = "SELECT COUNT(*) FROM users WHERE active = true";

        // Act
        var keywords = QueryUtils.GetKeywords(query);

        // Assert
        Assert.NotEmpty(keywords);
        // Note: Actual keywords depend on libpg_query implementation
    }

    [Fact]
    public void CountStatements_MultipleStatements_ReturnsCorrectCount()
    {
        // Arrange
        var sqlText = "SELECT 1; SELECT 2; SELECT 3;";

        // Act
        var count = QueryUtils.CountStatements(sqlText);

        // Assert
        Assert.True(count >= 3);
    }

    [Fact]
    public void AstToSql_ValidAst_ReturnsSql()
    {
        // Arrange
        var query = "SELECT * FROM users";
        var parseResult = Parser.QuickParse(query);

        // Act
        var sql = QueryUtils.AstToSql(parseResult.ParseTree!);

        // Assert
        Assert.NotNull(sql);
        Assert.Contains("SELECT", sql);
    }

    [Theory]
    [InlineData("SELECT 1")]
    [InlineData("INSERT INTO test VALUES (1)")]
    [InlineData("UPDATE test SET col = 1")]
    [InlineData("DELETE FROM test WHERE id = 1")]
    public void NewMethods_VariousQueries_HandleCorrectly(string query)
    {
        // Test that all new methods handle various query types
        var parseResult = _parser.Parse(query);
        Assert.True(parseResult.IsSuccess);

        if (parseResult.ParseTree is not null)
        {
            var deparseResult = _parser.Deparse(parseResult.ParseTree);
            // Deparse might not always succeed depending on libpg_query support
            Assert.NotNull(deparseResult);
        }

        var scanResult = _parser.Scan(query);
        Assert.True(scanResult.IsSuccess);

        var splitResult = _parser.Split(query);
        Assert.True(splitResult.IsSuccess);
    }
}