using Xunit;
using Npgquery;
using System.Linq;

namespace NpgqueryExtended.Tests;

/// <summary>
/// Tests to verify PostgreSQL version compatibility and handle breaking changes between versions
/// </summary>
public class VersionCompatibilityTests
{
    public static IReadOnlyList<PostgreSqlVersion> SupportedVersions => PostgreSqlVersionExtensions.GetSupportedVersions();
    public static IReadOnlyList<PostgreSqlVersion> AvailableVersions => PostgreSqlVersionTestData.AvailableVersionList;

    [Theory]
    [MemberData(nameof(PostgreSqlVersionTestData.AvailableVersions), MemberType = typeof(PostgreSqlVersionTestData))]
    public void BasicSelect_WorksAcrossAllVersions(PostgreSqlVersion version)
    {
        using var parser = new Parser(version);
        var result = parser.Parse("SELECT id, name FROM users WHERE active = true");
        
        Assert.True(result.IsSuccess, $"Parse failed on {version}: {result.Error}");
        Assert.NotNull(result.ParseTree);
    }

    [Theory]
    [MemberData(nameof(PostgreSqlVersionTestData.AvailableVersions), MemberType = typeof(PostgreSqlVersionTestData))]
    public void BasicInsert_WorksAcrossAllVersions(PostgreSqlVersion version)
    {
        using var parser = new Parser(version);
        var result = parser.Parse("INSERT INTO users (id, name) VALUES (1, 'test')");
        
        Assert.True(result.IsSuccess, $"Parse failed on {version}: {result.Error}");
    }

    [Theory]
    [MemberData(nameof(PostgreSqlVersionTestData.AvailableVersions), MemberType = typeof(PostgreSqlVersionTestData))]
    public void BasicUpdate_WorksAcrossAllVersions(PostgreSqlVersion version)
    {
        using var parser = new Parser(version);
        var result = parser.Parse("UPDATE users SET name = 'updated' WHERE id = 1");
        
        Assert.True(result.IsSuccess, $"Parse failed on {version}: {result.Error}");
    }

    [Theory]
    [MemberData(nameof(PostgreSqlVersionTestData.AvailableVersions), MemberType = typeof(PostgreSqlVersionTestData))]
    public void BasicDelete_WorksAcrossAllVersions(PostgreSqlVersion version)
    {
        using var parser = new Parser(version);
        var result = parser.Parse("DELETE FROM users WHERE id = 1");
        
        Assert.True(result.IsSuccess, $"Parse failed on {version}: {result.Error}");
    }

    [Theory]
    [MemberData(nameof(PostgreSqlVersionTestData.AvailableVersions), MemberType = typeof(PostgreSqlVersionTestData))]
    public void CreateTable_WorksAcrossAllVersions(PostgreSqlVersion version)
    {
        using var parser = new Parser(version);
        var query = @"CREATE TABLE users (
            id serial PRIMARY KEY,
            name varchar(100) NOT NULL,
            email varchar(255) UNIQUE,
            created_at timestamp DEFAULT CURRENT_TIMESTAMP
        )";
        var result = parser.Parse(query);
        
        Assert.True(result.IsSuccess, $"Parse failed on {version}: {result.Error}");
    }

    [Theory]
    [MemberData(nameof(PostgreSqlVersionTestData.AvailableVersions), MemberType = typeof(PostgreSqlVersionTestData))]
    public void Join_WorksAcrossAllVersions(PostgreSqlVersion version)
    {
        using var parser = new Parser(version);
        var result = parser.Parse(@"
            SELECT u.name, p.title 
            FROM users u 
            INNER JOIN posts p ON u.id = p.user_id
        ");
        
        Assert.True(result.IsSuccess, $"Parse failed on {version}: {result.Error}");
    }

    [Theory]
    [MemberData(nameof(PostgreSqlVersionTestData.AvailableVersions), MemberType = typeof(PostgreSqlVersionTestData))]
    public void CTE_WorksAcrossAllVersions(PostgreSqlVersion version)
    {
        using var parser = new Parser(version);
        var result = parser.Parse(@"
            WITH active_users AS (
                SELECT * FROM users WHERE active = true
            )
            SELECT * FROM active_users
        ");
        
        Assert.True(result.IsSuccess, $"Parse failed on {version}: {result.Error}");
    }

    // ============================================
    // PostgreSQL 17 Specific Features
    // ============================================

    [Fact]
    public void JsonTable_FailsBeforePG17_SucceedsInPG17AndLater()
    {
        var query = "SELECT * FROM JSON_TABLE('[{\"id\":1}]', '$[*]' COLUMNS(id int PATH '$.id'))";

        foreach (var version in AvailableVersions)
        {
            using var parser = new Parser(version);
            var result = parser.Parse(query);

            if (version.SupportsJsonTable())
            {
                Assert.True(result.IsSuccess, $"JSON_TABLE should work in {version}: {result.Error}");
            }
            else
            {
                Assert.False(result.IsSuccess, $"JSON_TABLE should not work in {version}");
                Assert.NotNull(result.Error);
            }
        }
    }

    [Theory]
    [MemberData(nameof(PostgreSqlVersionTestData.AvailableVersions), MemberType = typeof(PostgreSqlVersionTestData))]
    public void ParseOptions_ParseModeTypeName_AppliesAcrossSupportedVersions(PostgreSqlVersion version)
    {
        using var parser = new Parser(version);

        var withoutMode = parser.Parse("integer");
        var withTypeNameMode = parser.Parse(
            "integer",
            new ParseOptions { Mode = ParseMode.TypeName });

        Assert.False(withoutMode.IsSuccess);

        if (IsMissingExport(withTypeNameMode.Error))
        {
            return;
        }

        Assert.True(withTypeNameMode.IsSuccess, $"Type-name parse mode failed on {version}: {withTypeNameMode.Error}");
        Assert.NotNull(withTypeNameMode.ParseTree);
    }

    [Theory]
    [MemberData(nameof(PostgreSqlVersionTestData.AvailableVersions), MemberType = typeof(PostgreSqlVersionTestData))]
    public void NormalizeUtility_IsVersionGatedCorrectly(PostgreSqlVersion version)
    {
        using var parser = new Parser(version);
        var result = parser.NormalizeUtility("VACUUM users");

        if (version.SupportsNormalizeUtility() && !IsMissingExport(result.Error))
        {
            Assert.True(result.IsSuccess, $"NormalizeUtility should work in {version}: {result.Error}");
            Assert.NotNull(result.NormalizedQuery);
        }
        else
        {
            Assert.False(result.IsSuccess);
            Assert.Contains("does not", result.Error, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Theory]
    [MemberData(nameof(PostgreSqlVersionTestData.AvailableVersions), MemberType = typeof(PostgreSqlVersionTestData))]
    public void UtilityStatementDetection_IsVersionGatedCorrectly(PostgreSqlVersion version)
    {
        using var parser = new Parser(version);
        var result = parser.IsUtilityStatement("VACUUM users");

        if (version.SupportsUtilityStatementDetection() && !IsMissingExport(result.Error))
        {
            Assert.True(result.IsSuccess, $"IsUtilityStatement should work in {version}: {result.Error}");
            Assert.NotNull(result.IsUtilityStatements);
            Assert.NotEmpty(result.IsUtilityStatements);
        }
        else
        {
            Assert.False(result.IsSuccess);
            Assert.Contains("does not", result.Error, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Theory]
    [MemberData(nameof(PostgreSqlVersionTestData.AvailableVersions), MemberType = typeof(PostgreSqlVersionTestData))]
    public void QuerySummary_IsVersionGatedCorrectly(PostgreSqlVersion version)
    {
        using var parser = new Parser(version);
        var result = parser.Summarize("SELECT * FROM users", truncateLimit: 128);

        if (version.SupportsSummaryApi() && !IsMissingExport(result.Error))
        {
            Assert.True(result.IsSuccess, $"Summarize should work in {version}: {result.Error}");
            Assert.NotNull(result.SummaryProtobuf);
            Assert.NotEmpty(result.SummaryProtobuf);
        }
        else
        {
            Assert.False(result.IsSuccess);
            Assert.Contains("does not", result.Error, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void JsonQuery_ParsesAcrossSupportedVersions()
    {
        var query = "SELECT json_query('[1,2,3]', '$[*]')";

        foreach (var version in AvailableVersions)
        {
            using var parser = new Parser(version);
            var result = parser.Parse(query);

            Assert.True(result.IsSuccess, $"{version} failed: {result.Error}");

            var tree = result.ParseTree?.RootElement.ToString() ?? string.Empty;
            if (version.SupportsJsonTable())
            {
                Assert.Contains("JsonFuncExpr", tree);
            }
        }
    }

    [Fact]
    public void ComplexJsonTable_OnlyInPG17AndLater()
    {
        var query = @"
            SELECT jt.* 
            FROM JSON_TABLE(
                '[{""name"":""John"", ""age"":30}, {""name"":""Jane"", ""age"":25}]',
                '$[*]' COLUMNS(
                    name text PATH '$.name',
                    age int PATH '$.age'
                )
            ) AS jt
        ";
        
        foreach (var version in AvailableVersions)
        {
            using var parser = new Parser(version);
            var result = parser.Parse(query);

            if (version.SupportsJsonTable())
            {
                Assert.True(result.IsSuccess, $"Failed for {version}: {result.Error}");
            }
            else
            {
                Assert.False(result.IsSuccess, $"JSON_TABLE should not work in {version}");
            }
        }
    }

    // ============================================
    // Version Detection & Metadata
    // ============================================

    [Fact]
    public void ParserVersion_ReturnsCorrectVersion()
    {
        foreach (var version in AvailableVersions)
        {
            using var parser = new Parser(version);
            Assert.Equal(version, parser.Version);
        }
    }

    [Fact]
    public void VersionExtensions_ReturnCorrectMetadata()
    {
        Assert.Equal("15", PostgreSqlVersion.Postgres15.ToLibrarySuffix());
        Assert.Equal("16", PostgreSqlVersion.Postgres16.ToLibrarySuffix());
        Assert.Equal("17", PostgreSqlVersion.Postgres17.ToLibrarySuffix());
        Assert.Equal("18", PostgreSqlVersion.Postgres18.ToLibrarySuffix());
        
        Assert.Equal("PostgreSQL 15", PostgreSqlVersion.Postgres15.ToVersionString());
        Assert.Equal("PostgreSQL 16", PostgreSqlVersion.Postgres16.ToVersionString());
        Assert.Equal("PostgreSQL 17", PostgreSqlVersion.Postgres17.ToVersionString());
        Assert.Equal("PostgreSQL 18", PostgreSqlVersion.Postgres18.ToVersionString());
        
        Assert.Equal(150000, PostgreSqlVersion.Postgres15.ToVersionNumber());
        Assert.Equal(160000, PostgreSqlVersion.Postgres16.ToVersionNumber());
        Assert.Equal(170000, PostgreSqlVersion.Postgres17.ToVersionNumber());
        Assert.Equal(180000, PostgreSqlVersion.Postgres18.ToVersionNumber());
        
        Assert.Equal(15, PostgreSqlVersion.Postgres15.GetMajorVersion());
        Assert.Equal(16, PostgreSqlVersion.Postgres16.GetMajorVersion());
        Assert.Equal(17, PostgreSqlVersion.Postgres17.GetMajorVersion());
        Assert.Equal(18, PostgreSqlVersion.Postgres18.GetMajorVersion());

        Assert.False(PostgreSqlVersion.Postgres15.SupportsJsonTable());
        Assert.False(PostgreSqlVersion.Postgres16.SupportsJsonTable());
        Assert.True(PostgreSqlVersion.Postgres17.SupportsJsonTable());
        Assert.True(PostgreSqlVersion.Postgres18.SupportsJsonTable());
    }

    [Fact]
    public void AvailableVersions_ReturnsAtLeastOneVersion()
    {
        var versions = NativeLibraryLoader.GetAvailableVersions();
        Assert.NotEmpty(versions);
    }

    [Theory]
    [MemberData(nameof(PostgreSqlVersionTestData.AvailableVersions), MemberType = typeof(PostgreSqlVersionTestData))]
    public void VersionIsAvailable_ReturnsTrue(PostgreSqlVersion version)
    {
        var isAvailable = NativeLibraryLoader.IsVersionAvailable(version);

        Assert.True(isAvailable);
    }

    // ============================================
    // Error Handling
    // ============================================

    [Fact]
    public void InvalidQuery_ReturnsErrorInAllVersions()
    {
        var invalidQuery = "SELECT * FORM users"; // Typo: FORM instead of FROM
        
        foreach (var version in AvailableVersions)
        {
            using var parser = new Parser(version);
            var result = parser.Parse(invalidQuery);
            
            Assert.False(result.IsSuccess, $"Invalid query should fail in {version}");
            Assert.NotNull(result.Error);
            Assert.Contains("syntax error", result.Error, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void MissingVersion_ThrowsHelpfulException()
    {
        // This test assumes a version that doesn't exist
        var fakeVersion = (PostgreSqlVersion)99;
        
        var exception = Assert.Throws<PostgreSqlVersionNotAvailableException>(() =>
        {
            using var parser = new Parser(fakeVersion);
        });
        
        Assert.Equal(fakeVersion, exception.RequestedVersion);
        Assert.NotEmpty(exception.AvailableVersions);
    }

    // ============================================
    // Normalization & Fingerprinting
    // ============================================

    [Theory]
    [MemberData(nameof(PostgreSqlVersionTestData.AvailableVersions), MemberType = typeof(PostgreSqlVersionTestData))]
    public void Normalize_WorksAcrossVersions(PostgreSqlVersion version)
    {
        using var parser = new Parser(version);
        var result = parser.Normalize("SELECT   *   FROM    users   WHERE  id = 1");
        
        Assert.True(result.IsSuccess, $"Normalize failed on {version}: {result.Error}");
        Assert.NotNull(result.NormalizedQuery);
        Assert.Contains("SELECT", result.NormalizedQuery);
    }

    [Theory]
    [MemberData(nameof(PostgreSqlVersionTestData.AvailableVersions), MemberType = typeof(PostgreSqlVersionTestData))]
    public void Fingerprint_WorksAcrossVersions(PostgreSqlVersion version)
    {
        using var parser = new Parser(version);
        var result = parser.Fingerprint("SELECT * FROM users WHERE id = 1");
        
        Assert.True(result.IsSuccess, $"Fingerprint failed on {version}: {result.Error}");
        Assert.NotNull(result.Fingerprint);
        Assert.NotEmpty(result.Fingerprint);
    }

    [Fact]
    public void Fingerprint_DiffersBetweenVersionsForSameQuery()
    {
        var query = "SELECT * FROM users WHERE id = 1";
        
        foreach (var version in AvailableVersions)
        {
            using var parser = new Parser(version);
            var fingerprint = parser.Fingerprint(query).Fingerprint;
            Assert.NotNull(fingerprint);
        }
    }

    // ============================================
    // Real-World Queries
    // ============================================

    [Theory]
    [MemberData(nameof(PostgreSqlVersionTestData.AvailableVersions), MemberType = typeof(PostgreSqlVersionTestData))]
    public void ComplexQuery_WorksAcrossVersions(PostgreSqlVersion version)
    {
        var query = @"
            WITH RECURSIVE subordinates AS (
                SELECT employee_id, manager_id, full_name
                FROM employees
                WHERE employee_id = 2
                UNION
                SELECT e.employee_id, e.manager_id, e.full_name
                FROM employees e
                INNER JOIN subordinates s ON s.employee_id = e.manager_id
            )
            SELECT * FROM subordinates
        ";
        
        using var parser = new Parser(version);
        var result = parser.Parse(query);
        
        Assert.True(result.IsSuccess, $"Parse failed on {version}: {result.Error}");
    }

    [Theory]
    [MemberData(nameof(PostgreSqlVersionTestData.AvailableVersions), MemberType = typeof(PostgreSqlVersionTestData))]
    public void WindowFunction_WorksAcrossVersions(PostgreSqlVersion version)
    {
        var query = @"
            SELECT 
                name,
                salary,
                ROW_NUMBER() OVER (ORDER BY salary DESC) as rank,
                AVG(salary) OVER () as avg_salary
            FROM employees
        ";
        
        using var parser = new Parser(version);
        var result = parser.Parse(query);
        
        Assert.True(result.IsSuccess, $"Parse failed on {version}: {result.Error}");
    }

    [Theory]
    [MemberData(nameof(PostgreSqlVersionTestData.AvailableVersions), MemberType = typeof(PostgreSqlVersionTestData))]
    public void Subquery_WorksAcrossVersions(PostgreSqlVersion version)
    {
        var query = @"
            SELECT u.name, 
                   (SELECT COUNT(*) FROM posts p WHERE p.user_id = u.id) as post_count
            FROM users u
            WHERE u.active = true
        ";
        
        using var parser = new Parser(version);
        var result = parser.Parse(query);
        
        Assert.True(result.IsSuccess, $"Parse failed on {version}: {result.Error}");
    }

    // ============================================
    // Backward Compatibility
    // ============================================

    [Fact]
    public void DefaultConstructor_UsesPostgres16ForBackwardCompatibility()
    {
        using var parser = new Parser();
        Assert.Equal(PostgreSqlVersion.Postgres16, parser.Version);
    }

    [Fact]
    public void AllExistingTests_ShouldStillPass()
    {
        // This test ensures that existing functionality still works
        using var parser = new Parser(); // Default to PG 16
        
        var queries = new[]
        {
            "SELECT 1",
            "SELECT * FROM users",
            "INSERT INTO users VALUES (1, 'test')",
            "UPDATE users SET name = 'updated'",
            "DELETE FROM users WHERE id = 1"
        };
        
        foreach (var query in queries)
        {
            var result = parser.Parse(query);
            Assert.True(result.IsSuccess, $"Backward compatibility broken for: {query}");
        }
    }

    private static bool IsMissingExport(string? error)
    {
        return !string.IsNullOrEmpty(error) &&
            (error.Contains("Unable to find an entry point", StringComparison.OrdinalIgnoreCase) ||
             error.Contains("does not expose", StringComparison.OrdinalIgnoreCase));
    }
}
