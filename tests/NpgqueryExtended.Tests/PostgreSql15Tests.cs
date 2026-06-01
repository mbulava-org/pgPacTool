using Xunit;
using Npgquery;

namespace NpgqueryExtended.Tests;

/// <summary>
/// Integration tests specifically for PostgreSQL 15 support in Npgquery.
/// Validates that the pg15 native library loads correctly and handles all standard SQL plus
/// PG15-introduced syntax (MERGE). Deparse round-trip tests satisfy the acceptance criteria
/// for DEV-483.
/// </summary>
[Collection(NativeLibraryCollection.Name)]
public class PostgreSql15Tests
{
    // ============================================
    // Library availability and metadata (always run)
    // ============================================

    [Fact]
    public void Postgres15_IsInSupportedVersionsList()
    {
        var supported = PostgreSqlVersionExtensions.GetSupportedVersions();
        Assert.Contains(PostgreSqlVersion.Postgres15, supported);
    }

    [Fact]
    public void Postgres15_LibraryIsAvailable()
    {
        // Asserts that the libpg_query_15 native library is loadable on this platform.
        // On linux-x64 and osx-arm64 the binary is shipped; Windows requires a separate CI step.
        Assert.True(NativeLibraryLoader.IsVersionAvailable(PostgreSqlVersion.Postgres15),
            "libpg_query_15 native library was not found. " +
            "On linux-x64 and osx-arm64 the library should be present in runtimes/<rid>/native/.");
    }

    [Fact]
    public void Postgres15_VersionMetadata_IsCorrect()
    {
        Assert.Equal("15", PostgreSqlVersion.Postgres15.ToLibrarySuffix());
        Assert.Equal("PostgreSQL 15", PostgreSqlVersion.Postgres15.ToVersionString());
        Assert.Equal(150000, PostgreSqlVersion.Postgres15.ToVersionNumber());
        Assert.Equal(15, PostgreSqlVersion.Postgres15.GetMajorVersion());
    }

    [Fact]
    public void Postgres15_FeatureGating_IsCorrect()
    {
        // PG15 predates all the newer features; all should return false.
        Assert.False(PostgreSqlVersion.Postgres15.SupportsJsonTable(), "JSON_TABLE requires PG17+");
        Assert.False(PostgreSqlVersion.Postgres15.SupportsNamedNotNullConstraints(), "Named NOT NULL requires PG16+");
        Assert.False(PostgreSqlVersion.Postgres15.SupportsNormalizeUtility(), "NormalizeUtility requires PG16+");
        Assert.False(PostgreSqlVersion.Postgres15.SupportsUtilityStatementDetection(), "IsUtilityStatement requires PG17+");
        Assert.False(PostgreSqlVersion.Postgres15.SupportsSummaryApi(), "Summary API requires PG17+");
        Assert.False(PostgreSqlVersion.Postgres15.SupportsVirtualGeneratedColumns(), "Virtual generated columns require PG17+");
        Assert.False(PostgreSqlVersion.Postgres15.SupportsWithoutOverlaps(), "WITHOUT OVERLAPS requires PG18+");
    }

    // ============================================
    // Basic parsing (uses AvailableVersions filter so only runs when pg15 lib is present)
    // ============================================

    [Theory]
    [MemberData(nameof(Pg15OnlyVersionData))]
    public void Postgres15_ParsesSimpleSelect(PostgreSqlVersion version)
    {
        using var parser = new Parser(version);
        var result = parser.Parse("SELECT id, name FROM users WHERE active = true");

        Assert.True(result.IsSuccess, $"PG15 parse failed: {result.Error}");
        Assert.NotNull(result.ParseTree);
    }

    [Theory]
    [MemberData(nameof(Pg15OnlyVersionData))]
    public void Postgres15_ParsesInsert(PostgreSqlVersion version)
    {
        using var parser = new Parser(version);
        var result = parser.Parse("INSERT INTO users (id, name) VALUES (1, 'Alice')");
        Assert.True(result.IsSuccess, $"PG15 INSERT parse failed: {result.Error}");
    }

    [Theory]
    [MemberData(nameof(Pg15OnlyVersionData))]
    public void Postgres15_ParsesCreateTable(PostgreSqlVersion version)
    {
        using var parser = new Parser(version);
        var query = @"CREATE TABLE orders (
            id serial PRIMARY KEY,
            customer_id int NOT NULL,
            total numeric(12,2),
            created_at timestamptz DEFAULT now()
        )";
        var result = parser.Parse(query);
        Assert.True(result.IsSuccess, $"PG15 CREATE TABLE failed: {result.Error}");
    }

    [Theory]
    [MemberData(nameof(Pg15OnlyVersionData))]
    public void Postgres15_ParsesCTE(PostgreSqlVersion version)
    {
        using var parser = new Parser(version);
        var query = @"
            WITH recent AS (
                SELECT * FROM orders WHERE created_at > now() - interval '30 days'
            )
            SELECT customer_id, count(*) FROM recent GROUP BY customer_id
        ";
        var result = parser.Parse(query);
        Assert.True(result.IsSuccess, $"PG15 CTE failed: {result.Error}");
    }

    [Theory]
    [MemberData(nameof(Pg15OnlyVersionData))]
    public void Postgres15_Normalize_Works(PostgreSqlVersion version)
    {
        using var parser = new Parser(version);
        var result = parser.Normalize("SELECT   *  FROM   users  WHERE  id = 42");
        Assert.True(result.IsSuccess, $"PG15 Normalize failed: {result.Error}");
        Assert.NotNull(result.NormalizedQuery);
        Assert.Contains("SELECT", result.NormalizedQuery);
    }

    [Theory]
    [MemberData(nameof(Pg15OnlyVersionData))]
    public void Postgres15_Fingerprint_Works(PostgreSqlVersion version)
    {
        using var parser = new Parser(version);
        var result = parser.Fingerprint("SELECT * FROM users WHERE id = 1");
        Assert.True(result.IsSuccess, $"PG15 Fingerprint failed: {result.Error}");
        Assert.NotNull(result.Fingerprint);
        Assert.NotEmpty(result.Fingerprint);
    }

    [Theory]
    [MemberData(nameof(Pg15OnlyVersionData))]
    public void Postgres15_Parser_ReturnsCorrectVersion(PostgreSqlVersion version)
    {
        using var parser = new Parser(version);
        Assert.Equal(PostgreSqlVersion.Postgres15, parser.Version);
    }

    // ============================================
    // Deparse round-trip (acceptance criteria for DEV-483)
    // ============================================

    [Theory]
    [MemberData(nameof(Pg15OnlyVersionData))]
    public void Postgres15_Deparse_RoundTrip_SimpleSelect(PostgreSqlVersion version)
    {
        using var parser = new Parser(version);
        var deparseResult = parser.Deparse("SELECT id, name FROM users WHERE active = TRUE");

        Assert.Null(deparseResult.Error);
        Assert.NotNull(deparseResult.Query);
        Assert.False(string.IsNullOrWhiteSpace(deparseResult.Query), "Deparsed query must not be empty");
        Assert.Contains("users", deparseResult.Query, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [MemberData(nameof(Pg15OnlyVersionData))]
    public void Postgres15_Deparse_RoundTrip_Insert(PostgreSqlVersion version)
    {
        using var parser = new Parser(version);
        var deparseResult = parser.Deparse("INSERT INTO orders (customer_id, total) VALUES (42, 99.99)");

        Assert.Null(deparseResult.Error);
        Assert.NotNull(deparseResult.Query);
        Assert.Contains("orders", deparseResult.Query, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [MemberData(nameof(Pg15OnlyVersionData))]
    public void Postgres15_Deparse_RoundTrip_WithCTE(PostgreSqlVersion version)
    {
        using var parser = new Parser(version);
        var deparseResult = parser.Deparse("WITH recent AS (SELECT id FROM orders) SELECT * FROM recent");

        Assert.Null(deparseResult.Error);
        Assert.NotNull(deparseResult.Query);
        Assert.Contains("recent", deparseResult.Query, StringComparison.OrdinalIgnoreCase);
    }

    // ============================================
    // Version-gated features return proper errors for PG15
    // ============================================

    [Theory]
    [MemberData(nameof(Pg15OnlyVersionData))]
    public void Postgres15_NormalizeUtility_ReturnsVersionError(PostgreSqlVersion version)
    {
        using var parser = new Parser(version);
        var result = parser.NormalizeUtility("VACUUM users");
        Assert.False(result.IsSuccess);
        Assert.Contains("does not support", result.Error, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [MemberData(nameof(Pg15OnlyVersionData))]
    public void Postgres15_IsUtilityStatement_ReturnsVersionError(PostgreSqlVersion version)
    {
        using var parser = new Parser(version);
        var result = parser.IsUtilityStatement("VACUUM users");
        Assert.False(result.IsSuccess);
        Assert.Contains("does not support", result.Error, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [MemberData(nameof(Pg15OnlyVersionData))]
    public void Postgres15_Summarize_ReturnsVersionError(PostgreSqlVersion version)
    {
        using var parser = new Parser(version);
        var result = parser.Summarize("SELECT * FROM users");
        Assert.False(result.IsSuccess);
        Assert.Contains("does not support", result.Error, StringComparison.OrdinalIgnoreCase);
    }

    // ============================================
    // PG15-introduced SQL syntax
    // ============================================

    /// <summary>
    /// MERGE (SQL:2003) was introduced in PostgreSQL 15. This validates that the PG15 parser
    /// handles it correctly — a PG14 or older parser would reject this statement.
    /// </summary>
    [Theory]
    [MemberData(nameof(Pg15OnlyVersionData))]
    public void Postgres15_MergeStatement_Parses(PostgreSqlVersion version)
    {
        using var parser = new Parser(version);
        var query = @"
            MERGE INTO target_table t
            USING source_table s ON t.id = s.id
            WHEN MATCHED THEN UPDATE SET t.value = s.value
            WHEN NOT MATCHED THEN INSERT (id, value) VALUES (s.id, s.value)
        ";

        var result = parser.Parse(query);
        Assert.True(result.IsSuccess, $"PG15 MERGE failed: {result.Error}");
        Assert.NotNull(result.ParseTree);
    }

    // ============================================
    // Member data helper
    // ============================================

    public static TheoryData<PostgreSqlVersion> Pg15OnlyVersionData
    {
        get
        {
            var data = new TheoryData<PostgreSqlVersion>();
            if (NativeLibraryLoader.IsVersionAvailable(PostgreSqlVersion.Postgres15))
            {
                data.Add(PostgreSqlVersion.Postgres15);
            }
            return data;
        }
    }
}
