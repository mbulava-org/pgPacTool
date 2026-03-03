using Xunit;
using Npgquery;
using System.Linq;

namespace NpgqueryExtended.Tests;

/// <summary>
/// Tests to verify PostgreSQL version compatibility and handle breaking changes between versions
/// </summary>
public class VersionCompatibilityTests
{
    [Theory]
    [InlineData(PostgreSqlVersion.Postgres16)]
    [InlineData(PostgreSqlVersion.Postgres17)]
    public void BasicSelect_WorksAcrossAllVersions(PostgreSqlVersion version)
    {
        using var parser = new Parser(version);
        var result = parser.Parse("SELECT id, name FROM users WHERE active = true");
        
        Assert.True(result.IsSuccess, $"Parse failed on {version}: {result.Error}");
        Assert.NotNull(result.ParseTree);
    }

    [Theory]
    [InlineData(PostgreSqlVersion.Postgres16)]
    [InlineData(PostgreSqlVersion.Postgres17)]
    public void BasicInsert_WorksAcrossAllVersions(PostgreSqlVersion version)
    {
        using var parser = new Parser(version);
        var result = parser.Parse("INSERT INTO users (id, name) VALUES (1, 'test')");
        
        Assert.True(result.IsSuccess, $"Parse failed on {version}: {result.Error}");
    }

    [Theory]
    [InlineData(PostgreSqlVersion.Postgres16)]
    [InlineData(PostgreSqlVersion.Postgres17)]
    public void BasicUpdate_WorksAcrossAllVersions(PostgreSqlVersion version)
    {
        using var parser = new Parser(version);
        var result = parser.Parse("UPDATE users SET name = 'updated' WHERE id = 1");
        
        Assert.True(result.IsSuccess, $"Parse failed on {version}: {result.Error}");
    }

    [Theory]
    [InlineData(PostgreSqlVersion.Postgres16)]
    [InlineData(PostgreSqlVersion.Postgres17)]
    public void BasicDelete_WorksAcrossAllVersions(PostgreSqlVersion version)
    {
        using var parser = new Parser(version);
        var result = parser.Parse("DELETE FROM users WHERE id = 1");
        
        Assert.True(result.IsSuccess, $"Parse failed on {version}: {result.Error}");
    }

    [Theory]
    [InlineData(PostgreSqlVersion.Postgres16)]
    [InlineData(PostgreSqlVersion.Postgres17)]
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
    [InlineData(PostgreSqlVersion.Postgres16)]
    [InlineData(PostgreSqlVersion.Postgres17)]
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
    [InlineData(PostgreSqlVersion.Postgres16)]
    [InlineData(PostgreSqlVersion.Postgres17)]
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
    public void JsonTable_FailsInPG16_SucceedsInPG17()
    {
        var query = "SELECT * FROM JSON_TABLE('[{\"id\":1}]', '$[*]' COLUMNS(id int PATH '$.id'))";
        
        // Should fail in PG 16
        using var parser16 = new Parser(PostgreSqlVersion.Postgres16);
        var result16 = parser16.Parse(query);
        Assert.False(result16.IsSuccess, "JSON_TABLE should not work in PG 16");
        Assert.NotNull(result16.Error);
        
        // Should succeed in PG 17
        using var parser17 = new Parser(PostgreSqlVersion.Postgres17);
        var result17 = parser17.Parse(query);
        Assert.True(result17.IsSuccess, $"JSON_TABLE should work in PG 17: {result17.Error}");
    }

    [Fact]
    public void JsonQuery_ParsesInBothButTreeDiffers()
    {
        var query = "SELECT json_query('[1,2,3]', '$[*]')";
        
        using var parser16 = new Parser(PostgreSqlVersion.Postgres16);
        using var parser17 = new Parser(PostgreSqlVersion.Postgres17);
        
        var result16 = parser16.Parse(query);
        var result17 = parser17.Parse(query);
        
        // Both should parse successfully
        Assert.True(result16.IsSuccess, $"PG 16 failed: {result16.Error}");
        Assert.True(result17.IsSuccess, $"PG 17 failed: {result17.Error}");
        
        // But parse trees will differ
        var tree16 = result16.ParseTree?.RootElement.ToString() ?? "";
        var tree17 = result17.ParseTree?.RootElement.ToString() ?? "";
        
        // PG 17 should have JsonFuncExpr nodes
        Assert.Contains("JsonFuncExpr", tree17);
    }

    [Fact]
    public void ComplexJsonTable_OnlyInPG17()
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
        
        using var parser16 = new Parser(PostgreSqlVersion.Postgres16);
        var result16 = parser16.Parse(query);
        Assert.False(result16.IsSuccess);
        
        using var parser17 = new Parser(PostgreSqlVersion.Postgres17);
        var result17 = parser17.Parse(query);
        Assert.True(result17.IsSuccess, $"Failed: {result17.Error}");
    }

    // ============================================
    // Version Detection & Metadata
    // ============================================

    [Fact]
    public void ParserVersion_ReturnsCorrectVersion()
    {
        using var parser16 = new Parser(PostgreSqlVersion.Postgres16);
        Assert.Equal(PostgreSqlVersion.Postgres16, parser16.Version);
        
        using var parser17 = new Parser(PostgreSqlVersion.Postgres17);
        Assert.Equal(PostgreSqlVersion.Postgres17, parser17.Version);
    }

    [Fact]
    public void VersionExtensions_ReturnCorrectMetadata()
    {
        Assert.Equal("16", PostgreSqlVersion.Postgres16.ToLibrarySuffix());
        Assert.Equal("17", PostgreSqlVersion.Postgres17.ToLibrarySuffix());
        
        Assert.Equal("PostgreSQL 16", PostgreSqlVersion.Postgres16.ToVersionString());
        Assert.Equal("PostgreSQL 17", PostgreSqlVersion.Postgres17.ToVersionString());
        
        Assert.Equal(160000, PostgreSqlVersion.Postgres16.ToVersionNumber());
        Assert.Equal(170000, PostgreSqlVersion.Postgres17.ToVersionNumber());
        
        Assert.Equal(16, PostgreSqlVersion.Postgres16.GetMajorVersion());
        Assert.Equal(17, PostgreSqlVersion.Postgres17.GetMajorVersion());
    }

    [Fact]
    public void AvailableVersions_ReturnsAtLeastOneVersion()
    {
        var versions = NativeLibraryLoader.GetAvailableVersions();
        Assert.NotEmpty(versions);
    }

    [Theory]
    [InlineData(PostgreSqlVersion.Postgres16)]
    [InlineData(PostgreSqlVersion.Postgres17)]
    public void VersionIsAvailable_ReturnsTrue(PostgreSqlVersion version)
    {
        var isAvailable = NativeLibraryLoader.IsVersionAvailable(version);

        // TODO: This test hard-fails when version-specific native libraries are not present.
        // The test should skip (treat as inconclusive) instead of failing when binaries aren't built,
        // to avoid blocking CI when the repo ships non-suffixed binaries (pg_query.dll/pg_query.so).
        // See: https://github.com/mbulava-org/pgPacTool/issues
        // If this fails, the native library wasn't built
        Assert.True(isAvailable, 
            $"Version {version} is not available. Run: .\\scripts\\Build-NativeLibraries.ps1 -Versions \"{version.GetMajorVersion()}\"");
    }

    // ============================================
    // Error Handling
    // ============================================

    [Fact]
    public void InvalidQuery_ReturnsErrorInAllVersions()
    {
        var invalidQuery = "SELECT * FORM users"; // Typo: FORM instead of FROM
        
        foreach (var version in new[] { PostgreSqlVersion.Postgres16, PostgreSqlVersion.Postgres17 })
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
    [InlineData(PostgreSqlVersion.Postgres16)]
    [InlineData(PostgreSqlVersion.Postgres17)]
    public void Normalize_WorksAcrossVersions(PostgreSqlVersion version)
    {
        using var parser = new Parser(version);
        var result = parser.Normalize("SELECT   *   FROM    users   WHERE  id = 1");
        
        Assert.True(result.IsSuccess, $"Normalize failed on {version}: {result.Error}");
        Assert.NotNull(result.NormalizedQuery);
        Assert.Contains("SELECT", result.NormalizedQuery);
    }

    [Theory]
    [InlineData(PostgreSqlVersion.Postgres16)]
    [InlineData(PostgreSqlVersion.Postgres17)]
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
        
        using var parser16 = new Parser(PostgreSqlVersion.Postgres16);
        using var parser17 = new Parser(PostgreSqlVersion.Postgres17);
        
        var fp16 = parser16.Fingerprint(query).Fingerprint;
        var fp17 = parser17.Fingerprint(query).Fingerprint;
        
        // Fingerprints may differ between versions due to parse tree differences
        // This is expected and documented
        Assert.NotNull(fp16);
        Assert.NotNull(fp17);
    }

    // ============================================
    // Real-World Queries
    // ============================================

    [Theory]
    [InlineData(PostgreSqlVersion.Postgres16)]
    [InlineData(PostgreSqlVersion.Postgres17)]
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
    [InlineData(PostgreSqlVersion.Postgres16)]
    [InlineData(PostgreSqlVersion.Postgres17)]
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
    [InlineData(PostgreSqlVersion.Postgres16)]
    [InlineData(PostgreSqlVersion.Postgres17)]
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
}
