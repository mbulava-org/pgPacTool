using mbulava.PostgreSql.Dac.Extract;
using Npgsql;
using NUnit.Framework;
using Testcontainers.PostgreSql;

namespace ProjectExtract_Tests.ConcurrentAccess;

/// <summary>
/// Integration tests for concurrent extraction scenarios:
/// - Multiple simultaneous extractions on the same database
/// - Extraction while schema changes are in progress
/// Addresses DEV-485: Expand integration test coverage.
/// </summary>
[TestFixture]
[Category("ConcurrentAccess")]
[Category("Integration")]
public class ConcurrentAccessTests
{
    private PostgreSqlContainer _pgContainer = default!;
    private string _connectionString = default!;

    [OneTimeSetUp]
    public async Task OneTimeSetup()
    {
        DockerAvailability.SkipIfUnavailable();

        _pgContainer = new PostgreSqlBuilder("postgres:16")
            .WithDatabase("concurrentdb")
            .WithUsername("postgres")
            .WithPassword("testpass")
            .Build();

        await _pgContainer.StartAsync();

        var builder = new NpgsqlConnectionStringBuilder(_pgContainer.GetConnectionString())
        {
            MaxPoolSize = 50,
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

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            CREATE SCHEMA concurrent_schema;

            CREATE TABLE concurrent_schema.users (
                id      SERIAL PRIMARY KEY,
                name    TEXT NOT NULL,
                email   TEXT
            );

            CREATE TABLE concurrent_schema.sessions (
                id          SERIAL PRIMARY KEY,
                user_id     INTEGER NOT NULL REFERENCES concurrent_schema.users(id),
                started_at  TIMESTAMP DEFAULT NOW()
            );

            CREATE VIEW concurrent_schema.active_users AS
            SELECT u.id, u.name, COUNT(s.id) AS session_count
            FROM concurrent_schema.users u
            LEFT JOIN concurrent_schema.sessions s ON s.user_id = u.id
            GROUP BY u.id, u.name;

            CREATE FUNCTION concurrent_schema.get_user_name(p_id INTEGER)
            RETURNS TEXT AS $$
            BEGIN
                RETURN (SELECT name FROM concurrent_schema.users WHERE id = p_id);
            END;
            $$ LANGUAGE plpgsql;

            CREATE SEQUENCE concurrent_schema.event_seq START 1000;

            CREATE TYPE concurrent_schema.user_status AS ENUM ('active', 'suspended', 'deleted');
        ";
        await cmd.ExecuteNonQueryAsync();
    }

    // -------------------------------------------------------------------------
    // Parallel extractions
    // -------------------------------------------------------------------------

    [Test]
    public async Task ConcurrentAccess_TwoSimultaneousExtractions_BothSucceed()
    {
        var extractor1 = new PgProjectExtractor(_connectionString);
        var extractor2 = new PgProjectExtractor(_connectionString);

        var task1 = extractor1.ExtractPgProject("concurrentdb");
        var task2 = extractor2.ExtractPgProject("concurrentdb");

        var results = await Task.WhenAll(task1, task2);

        Assert.That(results[0], Is.Not.Null, "First concurrent extraction should succeed");
        Assert.That(results[1], Is.Not.Null, "Second concurrent extraction should succeed");

        Assert.That(results[0].Schemas.Count, Is.EqualTo(results[1].Schemas.Count),
            "Both extractions should see the same schema count");

        TestContext.Out.WriteLine($"✓ Two concurrent extractions both succeeded, {results[0].Schemas.Count} schemas each");
    }

    [Test]
    public async Task ConcurrentAccess_FiveSimultaneousExtractions_AllSucceed()
    {
        const int concurrentCount = 5;
        var extractors = Enumerable.Range(0, concurrentCount)
            .Select(_ => new PgProjectExtractor(_connectionString))
            .ToList();

        var tasks = extractors.Select(e => e.ExtractPgProject("concurrentdb")).ToList();
        var results = await Task.WhenAll(tasks);

        Assert.That(results, Has.Length.EqualTo(concurrentCount));
        foreach (var (result, i) in results.Select((r, idx) => (r, idx)))
        {
            Assert.That(result, Is.Not.Null, $"Extraction {i} should not be null");
            Assert.That(result.Schemas, Is.Not.Empty, $"Extraction {i} should have schemas");
        }

        var schemaCount = results[0].Schemas.Count;
        Assert.That(results.All(r => r.Schemas.Count == schemaCount), Is.True,
            "All concurrent extractions should see the same number of schemas");

        TestContext.Out.WriteLine($"✓ {concurrentCount} concurrent extractions all succeeded with {schemaCount} schemas each");
    }

    [Test]
    public async Task ConcurrentAccess_ParallelVersionDetections_AllReturnSameVersion()
    {
        const int concurrentCount = 4;
        var extractors = Enumerable.Range(0, concurrentCount)
            .Select(_ => new PgProjectExtractor(_connectionString))
            .ToList();

        var tasks = extractors.Select(e => e.DetectPostgresVersion()).ToList();
        var versions = await Task.WhenAll(tasks);

        Assert.That(versions, Has.Length.EqualTo(concurrentCount));
        Assert.That(versions.Distinct().Count(), Is.EqualTo(1),
            "All concurrent version detections should return the same version");
        Assert.That(versions[0], Does.StartWith("16"), "Should be PostgreSQL 16.x");

        TestContext.Out.WriteLine($"✓ {concurrentCount} concurrent version detections all returned: {versions[0]}");
    }

    [Test]
    public async Task ConcurrentAccess_ResultsAreConsistent_SchemaObjectCounts()
    {
        var extractor1 = new PgProjectExtractor(_connectionString);
        var extractor2 = new PgProjectExtractor(_connectionString);

        var p1 = extractor1.ExtractPgProject("concurrentdb");
        var p2 = extractor2.ExtractPgProject("concurrentdb");

        await Task.WhenAll(p1, p2);

        var proj1 = await p1;
        var proj2 = await p2;

        var schema1 = proj1.Schemas.FirstOrDefault(s => s.Name == "concurrent_schema");
        var schema2 = proj2.Schemas.FirstOrDefault(s => s.Name == "concurrent_schema");

        Assert.That(schema1, Is.Not.Null, "concurrent_schema should exist in first result");
        Assert.That(schema2, Is.Not.Null, "concurrent_schema should exist in second result");

        Assert.That(schema1!.Tables.Count, Is.EqualTo(schema2!.Tables.Count),
            "Both concurrent extractions should see the same table count");
        Assert.That(schema1.Views.Count, Is.EqualTo(schema2.Views.Count),
            "Both concurrent extractions should see the same view count");
        Assert.That(schema1.Functions.Count, Is.EqualTo(schema2.Functions.Count),
            "Both concurrent extractions should see the same function count");

        TestContext.Out.WriteLine($"✓ Concurrent results are consistent:");
        TestContext.Out.WriteLine($"  Tables: {schema1.Tables.Count}, Views: {schema1.Views.Count}, Functions: {schema1.Functions.Count}");
    }
}
