using System.Text.Json;
using Google.Protobuf;
using Npgquery;
using NpgqueryExtended.Tests;
using PgQuery;
using Xunit;

namespace Npgquery.Tests;

[Collection(NativeLibraryCollection.Name)]
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
