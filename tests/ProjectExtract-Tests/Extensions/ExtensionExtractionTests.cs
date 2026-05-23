using mbulava.PostgreSql.Dac.Extract;
using mbulava.PostgreSql.Dac.Models;
using Npgsql;
using NUnit.Framework;
using Testcontainers.PostgreSql;

namespace ProjectExtract_Tests.Extensions;

/// <summary>
/// Extraction tests for PostgreSQL Extensions (CREATE EXTENSION ...).
///
/// NOTE: As of the current implementation, PgSchema does not have an Extensions
/// property and PgProjectExtractor does not query pg_extension. These tests
/// document the EXPECTED behaviour once extension extraction is implemented
/// (DEV-48 gap). Tests marked [Ignore] will be activated when the feature lands.
///
/// To implement: add PgExtension model, Extensions property to PgSchema,
/// and an ExtractExtensionsAsync method to PgProjectExtractor.
/// </summary>
[TestFixture]
[Category("Extensions")]
[Category("Integration")]
public class ExtensionExtractionTests
{
    private PostgreSqlContainer _pgContainer = default!;
    private string _connectionString = default!;

    [OneTimeSetUp]
    public async Task OneTimeSetup()
    {
        DockerAvailability.SkipIfUnavailable();

        _pgContainer = new PostgreSqlBuilder("postgres:16")
            .WithDatabase("testdb")
            .WithUsername("postgres")
            .WithPassword("testpass")
            .Build();

        await _pgContainer.StartAsync();

        var builder = new NpgsqlConnectionStringBuilder(_pgContainer.GetConnectionString())
        {
            MaxPoolSize = 25,
            MinPoolSize = 0,
            ConnectionIdleLifetime = 30,
            Timeout = 30
        };
        _connectionString = builder.ToString();

        await SeedTestDataAsync();
    }

    [OneTimeTearDown]
    public async Task OneTimeTeardown()
    {
        NpgsqlConnection.ClearAllPools();
        if (_pgContainer is not null)
            await _pgContainer.DisposeAsync();
    }

    private async Task SeedTestDataAsync()
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();

        // Install extensions available in standard postgres:16 image
        await ExecuteSqlAsync(conn, @"
            CREATE EXTENSION IF NOT EXISTS ""uuid-ossp"";
            CREATE EXTENSION IF NOT EXISTS ""pg_trgm"";
        ");
    }

    private async Task ExecuteSqlAsync(NpgsqlConnection conn, string sql)
    {
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        await cmd.ExecuteNonQueryAsync();
    }

    /// <summary>
    /// Verifies directly via pg_extension that the extensions were installed,
    /// proving the test environment is correct regardless of extractor support.
    /// </summary>
    [Test]
    public async Task ExtractExtensions_DatabaseHasExtensions_PgExtensionTableConfirms()
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT extname FROM pg_extension WHERE extname IN ('uuid-ossp','pg_trgm') ORDER BY extname;";
        await using var reader = await cmd.ExecuteReaderAsync();

        var names = new List<string>();
        while (await reader.ReadAsync())
            names.Add(reader.GetString(0));

        Assert.That(names, Does.Contain("pg_trgm"), "pg_trgm extension should be installed");
        Assert.That(names, Does.Contain("uuid-ossp"), "uuid-ossp extension should be installed");

        TestContext.Out.WriteLine($"✓ Extensions confirmed via pg_extension: {string.Join(", ", names)}");
    }

    /// <summary>
    /// GAP TEST: Verifies that the extractor exposes installed extensions.
    /// Will fail until PgSchema.Extensions and ExtractExtensionsAsync are implemented.
    /// Activate by removing [Ignore] once the feature is in place.
    /// </summary>
    [Test]
    [Ignore("Extension extraction not yet implemented — DEV-48 gap. Remove [Ignore] once PgSchema.Extensions and ExtractExtensionsAsync are added.")]
    public async Task ExtractExtensions_InstalledExtensions_AppearInProject()
    {
        var extractor = new PgProjectExtractor(_connectionString);
        var project = await extractor.ExtractPgProject("testdb");

        // Expected API once implemented:
        // project.Extensions should be a List<PgExtension> at database level,
        // or each schema should expose its own extensions.
        // Adjust the assertion to match the actual model shape when implemented.
        Assert.Fail("Implement: project.Extensions should contain 'uuid-ossp' and 'pg_trgm'.");
        await Task.CompletedTask;
    }

    /// <summary>
    /// GAP TEST: Verifies uuid-ossp extension is extractable with correct metadata.
    /// Activate by removing [Ignore] once the feature is in place.
    /// </summary>
    [Test]
    [Ignore("Extension extraction not yet implemented — DEV-48 gap.")]
    public async Task ExtractExtensions_UuidOssp_HasCorrectName()
    {
        var extractor = new PgProjectExtractor(_connectionString);
        var project = await extractor.ExtractPgProject("testdb");

        // Replace with: var ext = project.Extensions.FirstOrDefault(e => e.Name == "uuid-ossp");
        // Assert.That(ext, Is.Not.Null, "uuid-ossp extension should be extracted");
        // Assert.That(ext!.Schema, Is.EqualTo("public"), "uuid-ossp installs in public");
        Assert.Fail("Implement once PgExtension model exists.");
        await Task.CompletedTask;
    }

    /// <summary>
    /// GAP TEST: Verifies pg_trgm extension is extractable.
    /// Activate by removing [Ignore] once the feature is in place.
    /// </summary>
    [Test]
    [Ignore("Extension extraction not yet implemented — DEV-48 gap.")]
    public async Task ExtractExtensions_PgTrgm_HasCorrectName()
    {
        var extractor = new PgProjectExtractor(_connectionString);
        var project = await extractor.ExtractPgProject("testdb");

        // Replace with: var ext = project.Extensions.FirstOrDefault(e => e.Name == "pg_trgm");
        // Assert.That(ext, Is.Not.Null, "pg_trgm extension should be extracted");
        Assert.Fail("Implement once PgExtension model exists.");
        await Task.CompletedTask;
    }
}
