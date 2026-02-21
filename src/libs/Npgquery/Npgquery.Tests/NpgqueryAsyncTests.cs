using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Npgquery;

namespace Npgquery.Tests;

public class ParserAsyncTests : IDisposable
{
    private readonly Parser _parser;

    public ParserAsyncTests()
    {
        _parser = new Parser();
    }

    public void Dispose()
    {
        _parser.Dispose();
    }

    [Fact]
    public async Task ParseAsync_ValidQuery_ReturnsSuccess()
    {
        // Arrange
        var query = "SELECT * FROM users WHERE id = 1";

        // Act
        var result = await _parser.ParseAsync(query);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.ParseTree);
        Assert.Equal(query, result.Query);
    }

    [Fact]
    public async Task NormalizeAsync_ValidQuery_ReturnsNormalized()
    {
        // Arrange
        var query = "SELECT * FROM users /* comment */ WHERE id = 1";

        // Act
        var result = await _parser.NormalizeAsync(query);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.NormalizedQuery);
        //Assert.DoesNotContain("comment", result.NormalizedQuery); //todo need to check if comment is removed
    }

    [Fact]
    public async Task FingerprintAsync_ValidQuery_ReturnsFingerprint()
    {
        // Arrange
        var query = "SELECT * FROM users WHERE id = 1";

        // Act
        var result = await _parser.FingerprintAsync(query);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Fingerprint);
        Assert.NotEmpty(result.Fingerprint);
    }

    [Fact]
    public async Task ParseManyAsync_MultipleQueries_ReturnsAllResults()
    {
        // Arrange
        var queries = new[]
        {
            "SELECT 1",
            "SELECT 2",
            "SELECT 3",
            "INVALID SQL"
        };

        // Act
        var results = await _parser.ParseManyAsync(queries, maxDegreeOfParallelism: 2);

        // Assert
        Assert.Equal(4, results.Length);
        Assert.True(results[0].IsSuccess);
        Assert.True(results[1].IsSuccess);
        Assert.True(results[2].IsSuccess);
        Assert.True(results[3].IsError);
    }

    [Fact]
    public async Task IsValidAsync_ValidQuery_ReturnsTrue()
    {
        // Arrange
        var query = "SELECT 1";

        // Act
        var isValid = await _parser.IsValidAsync(query);

        // Assert
        Assert.True(isValid);
    }

    [Fact]
    public async Task QuickParseAsync_StaticMethod_Works()
    {
        // Arrange
        var query = "SELECT * FROM users";

        // Act
        var result = await ParserAsync.QuickParseAsync(query);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.ParseTree);
    }

    [Fact]
    public async Task QuickNormalizeAsync_StaticMethod_Works()
    {
        // Arrange
        var query = "SELECT * FROM users /* comment */";

        // Act
        var result = await ParserAsync.QuickNormalizeAsync(query);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.NormalizedQuery);
        //Assert.DoesNotContain("comment", result.NormalizedQuery);
    }

    [Fact]
    public async Task QuickFingerprintAsync_StaticMethod_Works()
    {
        // Arrange
        var query = "SELECT * FROM users WHERE id = 1";

        // Act
        var result = await ParserAsync.QuickFingerprintAsync(query);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Fingerprint);
    }

    [Fact]
    public async Task ParseAsAsync_ValidQuery_ReturnsTypedResult()
    {
        // Arrange
        var query = "SELECT 1";

        // Act
        var result = await _parser.ParseAsAsync<object>(query);

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public async Task ParseManyAsync_WithCancellation_CanBeCancelled()
    {
        // Arrange
        var queries = Enumerable.Repeat("SELECT pg_sleep(1)", 10);
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));

        // Act & Assert
        await Assert.ThrowsAsync<TaskCanceledException>(
            () => _parser.ParseManyAsync(queries, cancellationToken: cts.Token));
    }

    [Fact]
    public async Task QuickParsePlpgsqlAsync_StaticMethod_Works()
    {
        // Arrange
        var plpgsqlCode = "BEGIN RETURN 'test'; END;";

        // Act
        var result = await ParserAsync.QuickParsePlpgsqlAsync(plpgsqlCode);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(plpgsqlCode, result.Query);
    }

    [Fact]
    public async Task DeparseAsync_ValidAst_ReturnsQuery()
    {
        // Arrange
        var query = "SELECT * FROM users";
        var parseResult = await _parser.ParseAsync(query);

        // Act
        var deparseResult = await _parser.DeparseAsync(parseResult.ParseTree!);

        // Assert
        Assert.True(deparseResult.IsSuccess);
        Assert.NotNull(deparseResult.Query);
    }

    [Fact]
    public async Task SplitAsync_MultipleStatements_ReturnsSeparateStatements()
    {
        // Arrange
        var query = "SELECT 1; SELECT 2; SELECT 3;";

        // Act
        var result = await _parser.SplitAsync(query);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Statements);
    }

    [Fact]
    public async Task ScanAsync_ValidQuery_ReturnsTokens()
    {
        // Arrange
        var query = "SELECT COUNT(*) FROM users";

        // Act
        var result = await _parser.ScanAsync(query);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Tokens);
    }

    [Fact]
    public async Task ParsePlpgsqlAsync_ValidCode_ReturnsResult()
    {
        // Arrange
        var plpgsqlCode = "BEGIN RETURN 42; END;";

        // Act
        var result = await _parser.ParsePlpgsqlAsync(plpgsqlCode);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(plpgsqlCode, result.Query);
    }

    [Fact]
    public async Task QuickDeparseAsync_StaticMethod_Works()
    {
        // Arrange
        var query = "SELECT 1";
        var parseResult = await ParserAsync.QuickParseAsync(query);

        // Act
        var deparseResult = await ParserAsync.QuickDeparseAsync(parseResult.ParseTree!);

        // Assert
        Assert.True(deparseResult.IsSuccess);
        Assert.NotNull(deparseResult.Query);
    }

    [Fact]
    public async Task QuickSplitAsync_StaticMethod_Works()
    {
        // Arrange
        var query = "SELECT 1; INSERT INTO test VALUES (1);";

        // Act
        var result = await ParserAsync.QuickSplitAsync(query);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Statements);
    }

    [Fact]
    public async Task QuickScanAsync_StaticMethod_Works()
    {
        // Arrange
        var query = "SELECT name, email FROM users WHERE active = true";

        // Act
        var result = await ParserAsync.QuickScanAsync(query);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Tokens);
    }
}