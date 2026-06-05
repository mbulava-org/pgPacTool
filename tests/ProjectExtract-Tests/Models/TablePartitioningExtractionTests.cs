using mbulava.PostgreSql.Dac.Extract;
using Npgsql;
using NUnit.Framework;
using Testcontainers.PostgreSql;

namespace ProjectExtract_Tests.Models;

/// <summary>
/// Integration tests for DEV-532 — Extract table partitioning (PARTITION BY).
/// Validates that RANGE, LIST, and HASH partitioned tables are correctly extracted,
/// that PartitionStrategy/PartitionExpression are populated, and that the generated
/// Definition SQL includes a valid PARTITION BY clause.
/// Tests require Docker and are skipped gracefully when Docker is unavailable.
/// </summary>
[TestFixture]
[Category("TablePartitioning")]
[Category("Integration")]
public class TablePartitioningExtractionTests
{
    private PostgreSqlContainer _pgContainer = default!;
    private string _connectionString = default!;

    [OneTimeSetUp]
    public async Task OneTimeSetup()
    {
        DockerAvailability.SkipIfUnavailable();

        _pgContainer = new PostgreSqlBuilder("postgres:16")
            .WithDatabase("partdb")
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

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            CREATE SCHEMA part_test;

            -- RANGE partitioned table (partitioned by date)
            CREATE TABLE part_test.orders (
                order_id   BIGSERIAL NOT NULL,
                order_date DATE NOT NULL,
                amount     NUMERIC(12, 2)
            ) PARTITION BY RANGE (order_date);

            -- LIST partitioned table (partitioned by region)
            CREATE TABLE part_test.customers (
                customer_id BIGSERIAL NOT NULL,
                region      TEXT NOT NULL,
                name        TEXT
            ) PARTITION BY LIST (region);

            -- HASH partitioned table
            CREATE TABLE part_test.events (
                event_id   BIGSERIAL NOT NULL,
                user_id    BIGINT NOT NULL,
                payload    JSONB
            ) PARTITION BY HASH (user_id);

            -- Plain (non-partitioned) table — should have null strategy/expression
            CREATE TABLE part_test.plain_table (
                id   SERIAL PRIMARY KEY,
                data TEXT
            );
        ";
        await cmd.ExecuteNonQueryAsync();
    }

    // ───────────────────────────────────────────────────────── RANGE ─────

    [Test]
    public async Task RangePartitionedTable_IsIncludedInExtraction()
    {
        var extractor = new PgProjectExtractor(_connectionString);
        var project = await extractor.ExtractPgProject("partdb");

        var schema = project.Schemas.FirstOrDefault(s => s.Name == "part_test");
        Assert.That(schema, Is.Not.Null, "part_test schema must exist");

        var table = schema!.Tables.FirstOrDefault(t => t.Name == "orders");
        Assert.That(table, Is.Not.Null, "orders (RANGE partitioned) must be extracted");

        TestContext.Out.WriteLine($"✓ orders found, strategy={table!.PartitionStrategy}");
    }

    [Test]
    public async Task RangePartitionedTable_HasCorrectStrategy()
    {
        var extractor = new PgProjectExtractor(_connectionString);
        var project = await extractor.ExtractPgProject("partdb");

        var schema = project.Schemas.First(s => s.Name == "part_test");
        var table = schema.Tables.First(t => t.Name == "orders");

        Assert.That(table.PartitionStrategy, Is.EqualTo("RANGE"),
            "orders should have RANGE partition strategy");

        TestContext.Out.WriteLine($"✓ orders.PartitionStrategy = {table.PartitionStrategy}");
    }

    [Test]
    public async Task RangePartitionedTable_HasPartitionExpression()
    {
        var extractor = new PgProjectExtractor(_connectionString);
        var project = await extractor.ExtractPgProject("partdb");

        var schema = project.Schemas.First(s => s.Name == "part_test");
        var table = schema.Tables.First(t => t.Name == "orders");

        Assert.That(table.PartitionExpression, Is.Not.Null.And.Not.Empty,
            "orders should have a non-empty partition expression");
        Assert.That(table.PartitionExpression, Does.Contain("order_date"),
            "orders partition expression should reference order_date");

        TestContext.Out.WriteLine($"✓ orders.PartitionExpression = {table.PartitionExpression}");
    }

    [Test]
    public async Task RangePartitionedTable_DefinitionContainsPartitionByClause()
    {
        var extractor = new PgProjectExtractor(_connectionString);
        var project = await extractor.ExtractPgProject("partdb");

        var schema = project.Schemas.First(s => s.Name == "part_test");
        var table = schema.Tables.First(t => t.Name == "orders");

        Assert.That(table.Definition, Does.Contain("PARTITION BY RANGE"),
            "orders Definition SQL must contain PARTITION BY RANGE");

        TestContext.Out.WriteLine("✓ orders Definition SQL:");
        TestContext.Out.WriteLine(table.Definition);
    }

    // ───────────────────────────────────────────────────────── LIST ──────

    [Test]
    public async Task ListPartitionedTable_HasCorrectStrategy()
    {
        var extractor = new PgProjectExtractor(_connectionString);
        var project = await extractor.ExtractPgProject("partdb");

        var schema = project.Schemas.First(s => s.Name == "part_test");
        var table = schema.Tables.First(t => t.Name == "customers");

        Assert.That(table.PartitionStrategy, Is.EqualTo("LIST"),
            "customers should have LIST partition strategy");
        Assert.That(table.PartitionExpression, Does.Contain("region"),
            "customers partition expression should reference region");
        Assert.That(table.Definition, Does.Contain("PARTITION BY LIST"),
            "customers Definition SQL must contain PARTITION BY LIST");

        TestContext.Out.WriteLine($"✓ customers.PartitionStrategy={table.PartitionStrategy}, expr={table.PartitionExpression}");
    }

    // ───────────────────────────────────────────────────────── HASH ──────

    [Test]
    public async Task HashPartitionedTable_HasCorrectStrategy()
    {
        var extractor = new PgProjectExtractor(_connectionString);
        var project = await extractor.ExtractPgProject("partdb");

        var schema = project.Schemas.First(s => s.Name == "part_test");
        var table = schema.Tables.First(t => t.Name == "events");

        Assert.That(table.PartitionStrategy, Is.EqualTo("HASH"),
            "events should have HASH partition strategy");
        Assert.That(table.PartitionExpression, Does.Contain("user_id"),
            "events partition expression should reference user_id");
        Assert.That(table.Definition, Does.Contain("PARTITION BY HASH"),
            "events Definition SQL must contain PARTITION BY HASH");

        TestContext.Out.WriteLine($"✓ events.PartitionStrategy={table.PartitionStrategy}, expr={table.PartitionExpression}");
    }

    // ─────────────────────────────────────────────── NON-PARTITIONED ─────

    [Test]
    public async Task PlainTable_HasNullPartitionFields()
    {
        var extractor = new PgProjectExtractor(_connectionString);
        var project = await extractor.ExtractPgProject("partdb");

        var schema = project.Schemas.First(s => s.Name == "part_test");
        var table = schema.Tables.First(t => t.Name == "plain_table");

        Assert.That(table.PartitionStrategy, Is.Null,
            "plain_table should have null PartitionStrategy");
        Assert.That(table.PartitionExpression, Is.Null,
            "plain_table should have null PartitionExpression");
        Assert.That(table.Definition, Does.Not.Contain("PARTITION BY"),
            "plain_table Definition SQL must NOT contain PARTITION BY");

        TestContext.Out.WriteLine("✓ plain_table has no partition fields");
    }

    // ─────────────────────────────────────────────── DEPLOYMENT (basic) ─

    [Test]
    public async Task PartitionedTable_DefinitionParsesWithoutError()
    {
        var extractor = new PgProjectExtractor(_connectionString);
        var project = await extractor.ExtractPgProject("partdb");

        var schema = project.Schemas.First(s => s.Name == "part_test");
        var partitionedTables = schema.Tables
            .Where(t => t.PartitionStrategy != null)
            .ToList();

        Assert.That(partitionedTables, Has.Count.EqualTo(3),
            "Exactly 3 partitioned tables (orders, customers, events) should be extracted");

        foreach (var t in partitionedTables)
        {
            Assert.That(t.Definition, Is.Not.Null.And.Not.Empty,
                $"Definition for {t.Name} must not be empty");
            Assert.That(t.Definition, Does.Contain("PARTITION BY"),
                $"Definition for {t.Name} must contain PARTITION BY");
            TestContext.Out.WriteLine($"✓ {t.Name}: {t.PartitionStrategy} ({t.PartitionExpression})");
            TestContext.Out.WriteLine(t.Definition);
        }
    }
}
