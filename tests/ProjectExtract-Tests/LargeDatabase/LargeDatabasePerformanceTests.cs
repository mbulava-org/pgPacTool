using mbulava.PostgreSql.Dac.Extract;
using Npgsql;
using NUnit.Framework;
using System.Diagnostics;
using Testcontainers.PostgreSql;

namespace ProjectExtract_Tests.LargeDatabase;

/// <summary>
/// Integration tests for large databases with 1,000+ objects.
/// Verifies that extraction completes in reasonable time and returns all objects.
/// Addresses DEV-485: Expand integration test coverage.
/// </summary>
[TestFixture]
[Category("LargeDatabase")]
[Category("Integration")]
public class LargeDatabasePerformanceTests
{
    private PostgreSqlContainer _pgContainer = default!;
    private string _connectionString = default!;

    // 10 schemas × (50 tables + 20 views + 10 functions + 5 sequences + 5 types)
    // = 10 schemas × 90 objects = 900 + 10 schemas themselves = ~910 objects
    // plus indexes, sequences on serial cols, etc.
    private const int SchemaCount = 10;
    private const int TablesPerSchema = 50;
    private const int ViewsPerSchema = 20;
    private const int FunctionsPerSchema = 10;
    private const int SequencesPerSchema = 5;
    private const int TypesPerSchema = 5;

    [OneTimeSetUp]
    public async Task OneTimeSetup()
    {
        DockerAvailability.SkipIfUnavailable();

        _pgContainer = new PostgreSqlBuilder("postgres:16")
            .WithDatabase("largedb")
            .WithUsername("postgres")
            .WithPassword("testpass")
            .Build();

        await _pgContainer.StartAsync();

        var builder = new NpgsqlConnectionStringBuilder(_pgContainer.GetConnectionString())
        {
            MaxPoolSize = 25,
            MinPoolSize = 0,
            ConnectionIdleLifetime = 30,
            Timeout = 60,
            CommandTimeout = 300
        };
        _connectionString = builder.ToString();

        await SeedLargeDatabaseAsync();
    }

    [OneTimeTearDown]
    public async Task OneTimeTeardown()
    {
        NpgsqlConnection.ClearAllPools();
        if (_pgContainer is not null)
            await _pgContainer.DisposeAsync();
    }

    private async Task SeedLargeDatabaseAsync()
    {
        TestContext.Out.WriteLine($"Seeding large database: {SchemaCount} schemas, {TablesPerSchema} tables each...");
        var sw = Stopwatch.StartNew();

        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();

        for (int s = 0; s < SchemaCount; s++)
        {
            var schemaName = $"schema_{s:D3}";
            var sql = new System.Text.StringBuilder();

            sql.AppendLine($"CREATE SCHEMA {schemaName};");

            // Tables
            for (int t = 0; t < TablesPerSchema; t++)
                sql.AppendLine($@"
                    CREATE TABLE {schemaName}.table_{t:D3} (
                        id          SERIAL PRIMARY KEY,
                        col_text    TEXT NOT NULL DEFAULT 'value',
                        col_int     INTEGER NOT NULL DEFAULT 0,
                        col_ts      TIMESTAMP DEFAULT NOW(),
                        col_bool    BOOLEAN DEFAULT FALSE
                    );");

            // Views (reference first table)
            for (int v = 0; v < ViewsPerSchema; v++)
                sql.AppendLine($@"
                    CREATE VIEW {schemaName}.view_{v:D3} AS
                    SELECT id, col_text, col_int FROM {schemaName}.table_{v:D3}
                    WHERE col_bool = FALSE;");

            // Functions
            for (int f = 0; f < FunctionsPerSchema; f++)
                sql.AppendLine($@"
                    CREATE FUNCTION {schemaName}.func_{f:D3}(p_id INTEGER)
                    RETURNS TEXT AS $$
                    BEGIN
                        RETURN (SELECT col_text FROM {schemaName}.table_{f:D3} WHERE id = p_id);
                    END;
                    $$ LANGUAGE plpgsql;");

            // Sequences
            for (int q = 0; q < SequencesPerSchema; q++)
                sql.AppendLine($@"
                    CREATE SEQUENCE {schemaName}.seq_{q:D3} START WITH {q * 100 + 1} INCREMENT BY 1;");

            // Enum types
            for (int tp = 0; tp < TypesPerSchema; tp++)
                sql.AppendLine($@"
                    CREATE TYPE {schemaName}.status_{tp:D3} AS ENUM ('active', 'inactive', 'pending');");

            await using var cmd = conn.CreateCommand();
            cmd.CommandText = sql.ToString();
            cmd.CommandTimeout = 120;
            await cmd.ExecuteNonQueryAsync();
        }

        sw.Stop();
        TestContext.Out.WriteLine($"Seeding complete in {sw.ElapsedMilliseconds}ms");
    }

    [Test]
    public async Task LargeDatabase_Extract_CompletesWithinReasonableTime()
    {
        var extractor = new PgProjectExtractor(_connectionString);
        var sw = Stopwatch.StartNew();

        var project = await extractor.ExtractPgProject("largedb");

        sw.Stop();
        TestContext.Out.WriteLine($"Extraction completed in {sw.ElapsedMilliseconds}ms");
        TestContext.Out.WriteLine($"Total schemas extracted: {project.Schemas.Count}");

        // Should complete within 120 seconds
        Assert.That(sw.Elapsed.TotalSeconds, Is.LessThan(120),
            "Extraction should complete within 120 seconds even for large databases");

        Assert.That(project, Is.Not.Null);
        Assert.That(project.Schemas, Is.Not.Empty);
    }

    [Test]
    public async Task LargeDatabase_Extract_AllSchemasPresent()
    {
        var extractor = new PgProjectExtractor(_connectionString);
        var project = await extractor.ExtractPgProject("largedb");

        for (int s = 0; s < SchemaCount; s++)
        {
            var schemaName = $"schema_{s:D3}";
            Assert.That(project.Schemas.Any(sc => sc.Name == schemaName),
                Is.True, $"Schema {schemaName} should be present");
        }

        TestContext.Out.WriteLine($"✓ All {SchemaCount} schemas found in extracted project");
    }

    [Test]
    public async Task LargeDatabase_Extract_TablesCountCorrect()
    {
        var extractor = new PgProjectExtractor(_connectionString);
        var project = await extractor.ExtractPgProject("largedb");

        var totalTables = project.Schemas
            .Where(s => s.Name.StartsWith("schema_"))
            .Sum(s => s.Tables.Count);

        var expectedMinimum = SchemaCount * TablesPerSchema;
        Assert.That(totalTables, Is.GreaterThanOrEqualTo(expectedMinimum),
            $"Should extract at least {expectedMinimum} tables across all schemas");

        TestContext.Out.WriteLine($"✓ Total tables extracted: {totalTables} (expected >= {expectedMinimum})");
    }

    [Test]
    public async Task LargeDatabase_Extract_ViewsCountCorrect()
    {
        var extractor = new PgProjectExtractor(_connectionString);
        var project = await extractor.ExtractPgProject("largedb");

        var totalViews = project.Schemas
            .Where(s => s.Name.StartsWith("schema_"))
            .Sum(s => s.Views.Count);

        var expectedMinimum = SchemaCount * ViewsPerSchema;
        Assert.That(totalViews, Is.GreaterThanOrEqualTo(expectedMinimum),
            $"Should extract at least {expectedMinimum} views across all schemas");

        TestContext.Out.WriteLine($"✓ Total views extracted: {totalViews} (expected >= {expectedMinimum})");
    }

    [Test]
    public async Task LargeDatabase_Extract_TotalObjectCountExceeds1000()
    {
        var extractor = new PgProjectExtractor(_connectionString);
        var project = await extractor.ExtractPgProject("largedb");

        var appSchemas = project.Schemas.Where(s => s.Name.StartsWith("schema_")).ToList();

        var totalObjects = appSchemas.Sum(s =>
            s.Tables.Count +
            s.Views.Count +
            s.Functions.Count +
            s.Sequences.Count +
            s.Types.Count);

        TestContext.Out.WriteLine($"✓ Total objects extracted: {totalObjects}");
        TestContext.Out.WriteLine($"  Tables:    {appSchemas.Sum(s => s.Tables.Count)}");
        TestContext.Out.WriteLine($"  Views:     {appSchemas.Sum(s => s.Views.Count)}");
        TestContext.Out.WriteLine($"  Functions: {appSchemas.Sum(s => s.Functions.Count)}");
        TestContext.Out.WriteLine($"  Sequences: {appSchemas.Sum(s => s.Sequences.Count)}");
        TestContext.Out.WriteLine($"  Types:     {appSchemas.Sum(s => s.Types.Count)}");

        Assert.That(totalObjects, Is.GreaterThan(1000),
            "Total object count should exceed 1,000 for a large database");
    }

    [Test]
    public async Task LargeDatabase_Extract_ReturnsCorrectPostgresVersion()
    {
        var extractor = new PgProjectExtractor(_connectionString);
        var project = await extractor.ExtractPgProject("largedb");

        Assert.That(project.PostgresVersion, Does.StartWith("16"),
            "PostgreSQL version should be 16.x");

        TestContext.Out.WriteLine($"✓ PostgreSQL version: {project.PostgresVersion}");
    }
}
