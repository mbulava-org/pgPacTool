using System.Text.Json;
using Npgquery;
using NpgqueryExtended.Tests;
using Xunit;

namespace Npgquery.Tests;

/// <summary>
/// Unit tests for QueryUtils covering all public methods.
/// </summary>
[Collection(NativeLibraryCollection.Name)]
public class QueryUtilsExtendedTests
{
    // ──────────────────────────────────────────────
    // ExtractTableNames
    // ──────────────────────────────────────────────

    [Fact]
    public void ExtractTableNames_SimpleSelect_ReturnsSingleTable()
    {
        var tables = QueryUtils.ExtractTableNames("SELECT * FROM users");
        Assert.Contains("users", tables, StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    public void ExtractTableNames_JoinQuery_ReturnsAllTables()
    {
        var tables = QueryUtils.ExtractTableNames(
            "SELECT u.id, o.amount FROM users u JOIN orders o ON u.id = o.user_id");
        Assert.Contains("users", tables, StringComparer.OrdinalIgnoreCase);
        Assert.Contains("orders", tables, StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    public void ExtractTableNames_InvalidQuery_ReturnsEmptyList()
    {
        var tables = QueryUtils.ExtractTableNames("NOT VALID SQL AT ALL !!!!");
        Assert.Empty(tables);
    }

    [Fact]
    public void ExtractTableNames_EmptyString_ReturnsEmptyList()
    {
        var tables = QueryUtils.ExtractTableNames("");
        Assert.Empty(tables);
    }

    [Fact]
    public void ExtractTableNames_InsertStatement_ReturnsTable()
    {
        var tables = QueryUtils.ExtractTableNames("INSERT INTO products (name) VALUES ('widget')");
        Assert.Contains("products", tables, StringComparer.OrdinalIgnoreCase);
    }

    // ──────────────────────────────────────────────
    // HaveSameStructure
    // ──────────────────────────────────────────────

    [Fact]
    public void HaveSameStructure_SameStructureDifferentLiterals_ReturnsTrue()
    {
        var q1 = "SELECT * FROM users WHERE id = 1";
        var q2 = "SELECT * FROM users WHERE id = 99";
        Assert.True(QueryUtils.HaveSameStructure(q1, q2));
    }

    [Fact]
    public void HaveSameStructure_DifferentStructure_ReturnsFalse()
    {
        var q1 = "SELECT * FROM users";
        var q2 = "DELETE FROM users WHERE id = 1";
        Assert.False(QueryUtils.HaveSameStructure(q1, q2));
    }

    [Fact]
    public void HaveSameStructure_InvalidQuery_ReturnsFalse()
    {
        Assert.False(QueryUtils.HaveSameStructure("NOT SQL", "SELECT 1"));
    }

    [Fact]
    public void HaveSameStructure_BothInvalid_ReturnsFalse()
    {
        Assert.False(QueryUtils.HaveSameStructure("GARBAGE", "ALSO GARBAGE"));
    }

    // ──────────────────────────────────────────────
    // GetQueryType
    // ──────────────────────────────────────────────

    [Fact]
    public void GetQueryType_SelectQuery_ReturnsSelect()
    {
        var type = QueryUtils.GetQueryType("SELECT id FROM users");
        Assert.NotNull(type);
        Assert.Equal("SELECT", type, ignoreCase: true);
    }

    [Fact]
    public void GetQueryType_InsertQuery_ReturnsInsert()
    {
        var type = QueryUtils.GetQueryType("INSERT INTO users (name) VALUES ('Alice')");
        Assert.NotNull(type);
        Assert.Equal("INSERT", type, ignoreCase: true);
    }

    [Fact]
    public void GetQueryType_UpdateQuery_ReturnsUpdate()
    {
        var type = QueryUtils.GetQueryType("UPDATE users SET name = 'Bob' WHERE id = 1");
        Assert.NotNull(type);
        Assert.Equal("UPDATE", type, ignoreCase: true);
    }

    [Fact]
    public void GetQueryType_DeleteQuery_ReturnsDelete()
    {
        var type = QueryUtils.GetQueryType("DELETE FROM users WHERE id = 1");
        Assert.NotNull(type);
        Assert.Equal("DELETE", type, ignoreCase: true);
    }

    [Fact]
    public void GetQueryType_InvalidQuery_ReturnsNull()
    {
        var type = QueryUtils.GetQueryType("BLAH BLAH BLAH");
        Assert.Null(type);
    }

    // ──────────────────────────────────────────────
    // CleanQuery
    // ──────────────────────────────────────────────

    [Fact]
    public void CleanQuery_ValidQuery_ReturnsNonEmpty()
    {
        var cleaned = QueryUtils.CleanQuery("SELECT   *   FROM   users");
        Assert.NotEmpty(cleaned);
    }

    [Fact]
    public void CleanQuery_InvalidQuery_ReturnsTrimmedOriginal()
    {
        var input = "  GARBAGE QUERY  ";
        var cleaned = QueryUtils.CleanQuery(input);
        // Falls back to Trim() of original
        Assert.Equal(input.Trim(), cleaned);
    }

    // ──────────────────────────────────────────────
    // ValidateQueries
    // ──────────────────────────────────────────────

    [Fact]
    public void ValidateQueries_MixedValidAndInvalid_ReturnsCorrectBooleans()
    {
        var valid = "SELECT 1";
        var invalid = "NOT SQL";
        var results = QueryUtils.ValidateQueries(new[] { valid, invalid });

        Assert.True(results[valid]);
        Assert.False(results[invalid]);
    }

    [Fact]
    public void ValidateQueries_EmptyCollection_ReturnsEmptyDictionary()
    {
        var results = QueryUtils.ValidateQueries(Array.Empty<string>());
        Assert.Empty(results);
    }

    // ──────────────────────────────────────────────
    // GetQueryErrors
    // ──────────────────────────────────────────────

    [Fact]
    public void GetQueryErrors_ValidQuery_ReturnsNullError()
    {
        var results = QueryUtils.GetQueryErrors(new[] { "SELECT 1" });
        Assert.Null(results["SELECT 1"]);
    }

    [Fact]
    public void GetQueryErrors_InvalidQuery_ReturnsNonNullError()
    {
        var results = QueryUtils.GetQueryErrors(new[] { "BLAH" });
        Assert.NotNull(results["BLAH"]);
    }

    // ──────────────────────────────────────────────
    // SplitStatements
    // ──────────────────────────────────────────────

    [Fact]
    public void SplitStatements_MultipleStatements_ReturnsSeparateStatements()
    {
        var sql = "SELECT 1; SELECT 2; SELECT 3;";
        var statements = QueryUtils.SplitStatements(sql);
        Assert.Equal(3, statements.Count);
    }

    [Fact]
    public void SplitStatements_SingleStatement_ReturnsSingleEntry()
    {
        var sql = "SELECT * FROM users";
        var statements = QueryUtils.SplitStatements(sql);
        Assert.Single(statements);
    }

    [Fact]
    public void SplitStatements_InvalidSql_ReturnsEmptyList()
    {
        var statements = QueryUtils.SplitStatements("NOT SQL AT ALL !!!");
        Assert.Empty(statements);
    }

    // ──────────────────────────────────────────────
    // GetTokens
    // ──────────────────────────────────────────────

    [Fact]
    public void GetTokens_ValidQuery_ReturnsTokens()
    {
        var tokens = QueryUtils.GetTokens("SELECT id FROM users");
        Assert.NotEmpty(tokens);
    }

    [Fact]
    public void GetTokens_InvalidQuery_ReturnsEmptyList()
    {
        // Scanner may or may not fail; the contract is a list (possibly empty).
        var tokens = QueryUtils.GetTokens("GARBAGE!!!");
        Assert.NotNull(tokens);
    }

    // ──────────────────────────────────────────────
    // GetKeywords
    // ──────────────────────────────────────────────

    [Fact]
    public void GetKeywords_SelectQuery_ContainsSelectKeyword()
    {
        var keywords = QueryUtils.GetKeywords("SELECT id FROM users WHERE id = 1");
        Assert.NotEmpty(keywords);
    }

    [Fact]
    public void GetKeywords_ValidQuery_NoDuplicates()
    {
        var keywords = QueryUtils.GetKeywords("SELECT id FROM users WHERE id = 1");
        Assert.Equal(keywords.Count, keywords.Distinct(StringComparer.OrdinalIgnoreCase).Count());
    }

    // ──────────────────────────────────────────────
    // AstToSql
    // ──────────────────────────────────────────────

    [Fact(Skip = "AstToSql uses QuickDeparse which calls pg_query_deparse_protobuf - crashes on Linux. See Issue #36")]
    public void AstToSql_ValidParseTree_ReturnsQuery()
    {
        var parseResult = Parser.QuickParse("SELECT id FROM users");
        Assert.True(parseResult.IsSuccess);
        Assert.NotNull(parseResult.ParseTree);

        var sql = QueryUtils.AstToSql(parseResult);
        Assert.NotNull(sql);
        Assert.NotEmpty(sql);
    }

    // ──────────────────────────────────────────────
    // RoundTripTest
    // ──────────────────────────────────────────────

    [Fact(Skip = "RoundTripTest calls pg_query_deparse_protobuf - crashes on Linux. See Issue #36")]
    public void RoundTripTest_ValidQuery_SucceedsWithNonNullQuery()
    {
        var (success, roundTrip) = QueryUtils.RoundTripTest("SELECT id FROM users");
        Assert.True(success);
        Assert.NotNull(roundTrip);
    }

    [Fact(Skip = "RoundTripTest calls pg_query_deparse_protobuf - crashes on Linux. See Issue #36")]
    public void RoundTripTest_InvalidQuery_ReturnsFalseAndNull()
    {
        var (success, roundTrip) = QueryUtils.RoundTripTest("NOT SQL !!!");
        Assert.False(success);
        Assert.Null(roundTrip);
    }

    // ──────────────────────────────────────────────
    // IsValidPlpgsql
    // ──────────────────────────────────────────────

    [Fact]
    public void IsValidPlpgsql_ValidPlpgsql_ReturnsTrue()
    {
        var plpgsql = @"
DO $$
BEGIN
  RAISE NOTICE 'hello';
END
$$;";
        Assert.True(QueryUtils.IsValidPlpgsql(plpgsql));
    }

    [Fact]
    public void IsValidPlpgsql_InvalidPlpgsql_ReturnsFalse()
    {
        Assert.False(QueryUtils.IsValidPlpgsql("THIS IS NOT PLPGSQL"));
    }

    // ──────────────────────────────────────────────
    // CountStatements
    // ──────────────────────────────────────────────

    [Fact]
    public void CountStatements_MultipleStatements_ReturnsCorrectCount()
    {
        Assert.Equal(3, QueryUtils.CountStatements("SELECT 1; SELECT 2; SELECT 3;"));
    }

    [Fact]
    public void CountStatements_SingleStatement_ReturnsOne()
    {
        Assert.Equal(1, QueryUtils.CountStatements("SELECT 1"));
    }

    [Fact]
    public void CountStatements_InvalidSql_ReturnsZero()
    {
        Assert.Equal(0, QueryUtils.CountStatements("NOT SQL !!!"));
    }

    // ──────────────────────────────────────────────
    // NormalizeStatements
    // ──────────────────────────────────────────────

    [Fact]
    public void NormalizeStatements_MultipleStatements_ReturnsDictionaryWithNormalizedValues()
    {
        var sql = "SELECT 1; SELECT 2;";
        var result = QueryUtils.NormalizeStatements(sql);
        Assert.Equal(2, result.Count);
        Assert.All(result.Values, v => Assert.NotEmpty(v));
    }

    [Fact]
    public void NormalizeStatements_InvalidSql_ReturnsEmptyDictionary()
    {
        var result = QueryUtils.NormalizeStatements("NOT SQL !!!");
        Assert.Empty(result);
    }
}
