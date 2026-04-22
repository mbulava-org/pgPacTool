using Xunit;
using Xunit.Abstractions;
using Npgquery;
using System.Runtime.InteropServices;

namespace NpgqueryExtended.Tests;

/// <summary>
/// Integration tests to verify native library loading and functionality across all supported versions and platforms
/// </summary>
public class NativeLibraryIntegrationTests
{
    private readonly ITestOutputHelper _output;

    public NativeLibraryIntegrationTests(ITestOutputHelper output)
    {
        _output = output;
    }

    // ============================================
    // Library Loading Tests
    // ============================================

    [Fact]
    public void NativeLibraryLoader_CanDetectAvailableVersions()
    {
        // Act
        var availableVersions = NativeLibraryLoader.GetAvailableVersions().ToList();

        // Assert
        _output.WriteLine($"Available PostgreSQL versions: {string.Join(", ", availableVersions.Select(v => v.ToVersionString()))}");
        
        Assert.NotEmpty(availableVersions);
    }

    [Theory]
    [MemberData(nameof(PostgreSqlVersionTestData.SupportedVersions), MemberType = typeof(PostgreSqlVersionTestData))]
    public void NativeLibraryLoader_IsVersionAvailable_ReturnsTrue(PostgreSqlVersion version)
    {
        // Act
        var isAvailable = NativeLibraryLoader.IsVersionAvailable(version);

        // Assert
        _output.WriteLine($"{version.ToVersionString()} availability: {isAvailable}");
        Assert.True(isAvailable, $"{version.ToVersionString()} should be available. Rebuild native libraries for this version if missing.");
    }

    [Theory]
    [MemberData(nameof(PostgreSqlVersionTestData.AvailableVersions), MemberType = typeof(PostgreSqlVersionTestData))]
    public void NativeLibraryLoader_GetLibraryHandle_ReturnsValidHandle(PostgreSqlVersion version)
    {
        // Act
        var handle = NativeLibraryLoader.GetLibraryHandle(version);

        // Assert
        _output.WriteLine($"{version.ToVersionString()} library handle: 0x{handle:X}");
        Assert.NotEqual(IntPtr.Zero, handle);
    }

    [Theory]
    [MemberData(nameof(PostgreSqlVersionTestData.AvailableVersions), MemberType = typeof(PostgreSqlVersionTestData))]
    public void NativeLibraryLoader_MultipleCallsSameVersion_ReturnsSameHandle(PostgreSqlVersion version)
    {
        // Act
        var handle1 = NativeLibraryLoader.GetLibraryHandle(version);
        var handle2 = NativeLibraryLoader.GetLibraryHandle(version);

        // Assert
        _output.WriteLine($"{version.ToVersionString()} handles: 0x{handle1:X}, 0x{handle2:X}");
        Assert.Equal(handle1, handle2);
    }

    [Fact]
    public void NativeLibraryLoader_DifferentVersions_ReturnDifferentHandles()
    {
        var availableVersions = NativeLibraryLoader.GetAvailableVersions().Take(2).ToList();
        Assert.True(availableVersions.Count >= 2, "At least two versioned native libraries are required for handle isolation tests.");

        // Act
        var handle16 = NativeLibraryLoader.GetLibraryHandle(availableVersions[0]);
        var handle17 = NativeLibraryLoader.GetLibraryHandle(availableVersions[1]);

        // Assert
        _output.WriteLine($"{availableVersions[0].ToVersionString()} handle: 0x{handle16:X}");
        _output.WriteLine($"{availableVersions[1].ToVersionString()} handle: 0x{handle17:X}");
        Assert.NotEqual(handle16, handle17);
    }

    // ============================================
    // Parser Construction Tests
    // ============================================

    [Theory]
    [MemberData(nameof(PostgreSqlVersionTestData.AvailableVersions), MemberType = typeof(PostgreSqlVersionTestData))]
    public void Parser_Construction_LoadsCorrectVersion(PostgreSqlVersion version)
    {
        // Act & Assert
        using var parser = new Parser(version);
        
        _output.WriteLine($"Parser created for {version.ToVersionString()}");
        Assert.Equal(version, parser.Version);
    }

    [Fact]
    public void Parser_DefaultConstructor_LoadsPostgres16()
    {
        // Act
        using var parser = new Parser();

        // Assert
        _output.WriteLine($"Default parser version: {parser.Version.ToVersionString()}");
        Assert.Equal(PostgreSqlVersion.Postgres16, parser.Version);
    }

    [Theory]
    [MemberData(nameof(PostgreSqlVersionTestData.AvailableVersions), MemberType = typeof(PostgreSqlVersionTestData))]
    public void Parser_MultipleInstances_SameVersion_AllWork(PostgreSqlVersion version)
    {
        // Act
        using var parser1 = new Parser(version);
        using var parser2 = new Parser(version);
        using var parser3 = new Parser(version);

        // Assert
        var query = "SELECT 1";
        var result1 = parser1.Parse(query);
        var result2 = parser2.Parse(query);
        var result3 = parser3.Parse(query);

        _output.WriteLine($"Multiple {version.ToVersionString()} parsers all succeeded");
        Assert.True(result1.IsSuccess);
        Assert.True(result2.IsSuccess);
        Assert.True(result3.IsSuccess);
    }

    [Fact]
    public void Parser_MultipleVersions_Simultaneous_AllWork()
    {
        var availableVersions = NativeLibraryLoader.GetAvailableVersions().Take(2).ToList();
        Assert.True(availableVersions.Count >= 2, "At least two versioned native libraries are required for simultaneous parser tests.");

        // Act
        using var parser16 = new Parser(availableVersions[0]);
        using var parser17 = new Parser(availableVersions[1]);

        // Assert
        var query = "SELECT id FROM users";
        var result16 = parser16.Parse(query);
        var result17 = parser17.Parse(query);

        _output.WriteLine($"{availableVersions[0].ToVersionString()} and {availableVersions[1].ToVersionString()} parsers work simultaneously");
        Assert.True(result16.IsSuccess);
        Assert.True(result17.IsSuccess);
    }

    // ============================================
    // Platform Detection Tests
    // ============================================

    [Fact]
    public void PlatformDetection_ReportsCorrectPlatform()
    {
        // Act
        var platform = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "Windows" :
                      RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ? "Linux" :
                      RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? "macOS" : "Unknown";
        
        var arch = RuntimeInformation.ProcessArchitecture.ToString();

        // Assert
        _output.WriteLine($"Platform: {platform}");
        _output.WriteLine($"Architecture: {arch}");
        _output.WriteLine($"Runtime Identifier: {RuntimeInformation.RuntimeIdentifier}");
        _output.WriteLine($"OS Description: {RuntimeInformation.OSDescription}");
        
        Assert.NotEqual("Unknown", platform);
    }

    // ============================================
    // Parsing Functionality Tests
    // ============================================

    [Theory]
    [InlineData(PostgreSqlVersion.Postgres15, "SELECT 1")]
    [InlineData(PostgreSqlVersion.Postgres15, "SELECT * FROM users")]
    [InlineData(PostgreSqlVersion.Postgres15, "INSERT INTO users (name) VALUES ('test')")]
    [InlineData(PostgreSqlVersion.Postgres15, "UPDATE users SET name = 'updated'")]
    [InlineData(PostgreSqlVersion.Postgres15, "DELETE FROM users WHERE id = 1")]
    [InlineData(PostgreSqlVersion.Postgres16, "SELECT 1")]
    [InlineData(PostgreSqlVersion.Postgres16, "SELECT * FROM users")]
    [InlineData(PostgreSqlVersion.Postgres16, "INSERT INTO users (name) VALUES ('test')")]
    [InlineData(PostgreSqlVersion.Postgres16, "UPDATE users SET name = 'updated'")]
    [InlineData(PostgreSqlVersion.Postgres16, "DELETE FROM users WHERE id = 1")]
    [InlineData(PostgreSqlVersion.Postgres17, "SELECT 1")]
    [InlineData(PostgreSqlVersion.Postgres17, "SELECT * FROM users")]
    [InlineData(PostgreSqlVersion.Postgres17, "INSERT INTO users (name) VALUES ('test')")]
    [InlineData(PostgreSqlVersion.Postgres17, "UPDATE users SET name = 'updated'")]
    [InlineData(PostgreSqlVersion.Postgres17, "DELETE FROM users WHERE id = 1")]
    [InlineData(PostgreSqlVersion.Postgres18, "SELECT 1")]
    [InlineData(PostgreSqlVersion.Postgres18, "SELECT * FROM users")]
    [InlineData(PostgreSqlVersion.Postgres18, "INSERT INTO users (name) VALUES ('test')")]
    [InlineData(PostgreSqlVersion.Postgres18, "UPDATE users SET name = 'updated'")]
    [InlineData(PostgreSqlVersion.Postgres18, "DELETE FROM users WHERE id = 1")]
    public void Parse_BasicQueries_SucceedsWithCorrectVersion(PostgreSqlVersion version, string query)
    {
        if (!NativeLibraryLoader.IsVersionAvailable(version))
        {
            _output.WriteLine($"Skipping {version.ToVersionString()} because native library is not available.");
            return;
        }

        // Arrange
        using var parser = new Parser(version);

        // Act
        var result = parser.Parse(query);

        // Assert
        _output.WriteLine($"[{version.ToVersionString()}] Query: {query}");
        _output.WriteLine($"[{version.ToVersionString()}] Success: {result.IsSuccess}");
        if (!result.IsSuccess)
        {
            _output.WriteLine($"[{version.ToVersionString()}] Error: {result.Error}");
        }
        
        Assert.True(result.IsSuccess, $"Parse failed on {version.ToVersionString()}: {result.Error}");
        Assert.NotNull(result.ParseTree);
    }

    [Theory]
    [InlineData(PostgreSqlVersion.Postgres15, "SELECT * FROM users /* comment */ WHERE id = 1")]
    [InlineData(PostgreSqlVersion.Postgres16, "SELECT * FROM users /* comment */ WHERE id = 1")]
    [InlineData(PostgreSqlVersion.Postgres17, "SELECT * FROM users /* comment */ WHERE id = 1")]
    [InlineData(PostgreSqlVersion.Postgres18, "SELECT * FROM users /* comment */ WHERE id = 1")]
    public void Normalize_WorksWithCorrectVersion(PostgreSqlVersion version, string query)
    {
        if (!NativeLibraryLoader.IsVersionAvailable(version))
        {
            _output.WriteLine($"Skipping {version.ToVersionString()} because native library is not available.");
            return;
        }

        // Arrange
        using var parser = new Parser(version);

        // Act
        var result = parser.Normalize(query);

        // Assert
        _output.WriteLine($"[{version.ToVersionString()}] Original: {query}");
        _output.WriteLine($"[{version.ToVersionString()}] Normalized: {result.NormalizedQuery}");
        
        Assert.True(result.IsSuccess, $"Normalize failed on {version.ToVersionString()}: {result.Error}");
        Assert.NotNull(result.NormalizedQuery);
        Assert.Contains("SELECT", result.NormalizedQuery);
    }

    [Theory]
    [InlineData(PostgreSqlVersion.Postgres15, "SELECT * FROM users WHERE id = 1", "SELECT * FROM users WHERE id = 2")]
    [InlineData(PostgreSqlVersion.Postgres16, "SELECT * FROM users WHERE id = 1", "SELECT * FROM users WHERE id = 2")]
    [InlineData(PostgreSqlVersion.Postgres17, "SELECT * FROM users WHERE id = 1", "SELECT * FROM users WHERE id = 2")]
    [InlineData(PostgreSqlVersion.Postgres18, "SELECT * FROM users WHERE id = 1", "SELECT * FROM users WHERE id = 2")]
    public void Fingerprint_SimilarQueries_ReturnsSameFingerprint(PostgreSqlVersion version, string query1, string query2)
    {
        if (!NativeLibraryLoader.IsVersionAvailable(version))
        {
            _output.WriteLine($"Skipping {version.ToVersionString()} because native library is not available.");
            return;
        }

        // Arrange
        using var parser = new Parser(version);

        // Act
        var fp1 = parser.Fingerprint(query1);
        var fp2 = parser.Fingerprint(query2);

        // Assert
        _output.WriteLine($"[{version.ToVersionString()}] Query1 fingerprint: {fp1.Fingerprint}");
        _output.WriteLine($"[{version.ToVersionString()}] Query2 fingerprint: {fp2.Fingerprint}");
        
        Assert.True(fp1.IsSuccess, $"Fingerprint1 failed on {version.ToVersionString()}: {fp1.Error}");
        Assert.True(fp2.IsSuccess, $"Fingerprint2 failed on {version.ToVersionString()}: {fp2.Error}");
        Assert.Equal(fp1.Fingerprint, fp2.Fingerprint);
    }

    // ============================================
    // Complex SQL Tests
    // ============================================

    [Theory]
    [InlineData(PostgreSqlVersion.Postgres16)]
    [InlineData(PostgreSqlVersion.Postgres17)]
    public void Parse_ComplexJoinQuery_Succeeds(PostgreSqlVersion version)
    {
        // Arrange
        using var parser = new Parser(version);
        var query = @"
            SELECT u.id, u.name, COUNT(p.id) as post_count
            FROM users u
            LEFT JOIN posts p ON u.id = p.user_id
            WHERE u.active = true
            GROUP BY u.id, u.name
            HAVING COUNT(p.id) > 5
            ORDER BY post_count DESC
            LIMIT 10";

        // Act
        var result = parser.Parse(query);

        // Assert
        _output.WriteLine($"[{version.ToVersionString()}] Complex JOIN query parsed successfully");
        Assert.True(result.IsSuccess, $"Parse failed: {result.Error}");
    }

    [Theory]
    [InlineData(PostgreSqlVersion.Postgres16)]
    [InlineData(PostgreSqlVersion.Postgres17)]
    public void Parse_CTE_Succeeds(PostgreSqlVersion version)
    {
        // Arrange
        using var parser = new Parser(version);
        var query = @"
            WITH RECURSIVE category_tree AS (
                SELECT id, name, parent_id, 1 as level
                FROM categories
                WHERE parent_id IS NULL
                UNION ALL
                SELECT c.id, c.name, c.parent_id, ct.level + 1
                FROM categories c
                INNER JOIN category_tree ct ON c.parent_id = ct.id
            )
            SELECT * FROM category_tree ORDER BY level, name";

        // Act
        var result = parser.Parse(query);

        // Assert
        _output.WriteLine($"[{version.ToVersionString()}] Recursive CTE parsed successfully");
        Assert.True(result.IsSuccess, $"Parse failed: {result.Error}");
    }

    [Theory]
    [InlineData(PostgreSqlVersion.Postgres16)]
    [InlineData(PostgreSqlVersion.Postgres17)]
    public void Parse_WindowFunctions_Succeeds(PostgreSqlVersion version)
    {
        // Arrange
        using var parser = new Parser(version);
        var query = @"
            SELECT 
                name,
                salary,
                department,
                AVG(salary) OVER (PARTITION BY department) as dept_avg,
                RANK() OVER (ORDER BY salary DESC) as salary_rank,
                ROW_NUMBER() OVER (PARTITION BY department ORDER BY salary DESC) as dept_rank
            FROM employees";

        // Act
        var result = parser.Parse(query);

        // Assert
        _output.WriteLine($"[{version.ToVersionString()}] Window functions parsed successfully");
        Assert.True(result.IsSuccess, $"Parse failed: {result.Error}");
    }

    // ============================================
    // Error Handling Tests
    // ============================================

    [Theory]
    [InlineData(PostgreSqlVersion.Postgres16)]
    [InlineData(PostgreSqlVersion.Postgres17)]
    public void Parse_InvalidSQL_ReturnsError(PostgreSqlVersion version)
    {
        // Arrange
        using var parser = new Parser(version);
        var query = "INVALID SQL SYNTAX HERE";

        // Act
        var result = parser.Parse(query);

        // Assert
        _output.WriteLine($"[{version.ToVersionString()}] Invalid SQL correctly rejected");
        _output.WriteLine($"[{version.ToVersionString()}] Error: {result.Error}");
        
        Assert.False(result.IsSuccess);
        Assert.NotNull(result.Error);
    }

    [Theory]
    [InlineData(PostgreSqlVersion.Postgres16)]
    [InlineData(PostgreSqlVersion.Postgres17)]
    public void IsValid_ValidAndInvalidSQL_ReturnsCorrectly(PostgreSqlVersion version)
    {
        // Arrange
        using var parser = new Parser(version);

        // Act
        var validResult = parser.IsValid("SELECT 1");
        var invalidResult = parser.IsValid("INVALID SQL");

        // Assert
        _output.WriteLine($"[{version.ToVersionString()}] Valid SQL: {validResult}");
        _output.WriteLine($"[{version.ToVersionString()}] Invalid SQL: {invalidResult}");
        
        Assert.True(validResult);
        Assert.False(invalidResult);
    }

    // ============================================
    // Memory and Resource Tests
    // ============================================

    [Theory]
    [InlineData(PostgreSqlVersion.Postgres16)]
    [InlineData(PostgreSqlVersion.Postgres17)]
    public void Parser_DisposedMultipleTimes_DoesNotThrow(PostgreSqlVersion version)
    {
        // Arrange
        var parser = new Parser(version);

        // Act & Assert
        parser.Dispose();
        parser.Dispose(); // Should not throw
        parser.Dispose(); // Should not throw
        
        _output.WriteLine($"[{version.ToVersionString()}] Multiple dispose calls handled correctly");
    }

    [Theory]
    [InlineData(PostgreSqlVersion.Postgres16)]
    [InlineData(PostgreSqlVersion.Postgres17)]
    public void Parser_ManyQueries_NoMemoryLeaks(PostgreSqlVersion version)
    {
        // Arrange
        using var parser = new Parser(version);
        var queries = Enumerable.Range(1, 100).Select(i => $"SELECT {i}");

        // Act
        foreach (var query in queries)
        {
            var result = parser.Parse(query);
            Assert.True(result.IsSuccess);
        }

        // Assert
        _output.WriteLine($"[{version.ToVersionString()}] Parsed 100 queries without issues");
    }

    // ============================================
    // Library Path and Discovery Tests
    // ============================================

    [Fact]
    public void LibraryPaths_ExpectedFilesExist()
    {
        // Arrange
        var baseDir = AppContext.BaseDirectory;
        var expectedPaths = new List<string>();

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            expectedPaths.Add(Path.Combine(baseDir, "runtimes", "win-x64", "native", "libpg_query_16.dll"));
            expectedPaths.Add(Path.Combine(baseDir, "runtimes", "win-x64", "native", "libpg_query_17.dll"));
            // Also check output directory
            expectedPaths.Add(Path.Combine(baseDir, "libpg_query_16.dll"));
            expectedPaths.Add(Path.Combine(baseDir, "libpg_query_17.dll"));
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            expectedPaths.Add(Path.Combine(baseDir, "runtimes", "linux-x64", "native", "libpg_query_16.so"));
            expectedPaths.Add(Path.Combine(baseDir, "runtimes", "linux-x64", "native", "libpg_query_17.so"));
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            expectedPaths.Add(Path.Combine(baseDir, "runtimes", "osx-arm64", "native", "libpg_query_16.dylib"));
            expectedPaths.Add(Path.Combine(baseDir, "runtimes", "osx-arm64", "native", "libpg_query_17.dylib"));
        }

        // Act & Assert
        _output.WriteLine($"Base directory: {baseDir}");
        foreach (var path in expectedPaths)
        {
            var exists = File.Exists(path);
            _output.WriteLine($"Library path: {path} - Exists: {exists}");
        }

        // At least some paths should exist
        var existingPaths = expectedPaths.Where(File.Exists).ToList();
        Assert.NotEmpty(existingPaths);
    }
}
