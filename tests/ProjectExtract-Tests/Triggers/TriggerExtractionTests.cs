using mbulava.PostgreSql.Dac.Extract;
using Npgsql;
using NUnit.Framework;
using Testcontainers.PostgreSql;

namespace ProjectExtract_Tests.Triggers;

/// <summary>
/// Tests for trigger extraction (Issue #4)
/// </summary>
[TestFixture]
[Category("Triggers")]
[Category("Integration")]
public class TriggerExtractionTests
{
    private PostgreSqlContainer _pgContainer = default!;
    private string _connectionString = default!;

    [OneTimeSetUp]
    public async Task OneTimeSetup()
    {
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
        await _pgContainer.DisposeAsync();
    }

    private async Task SeedTestDataAsync()
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();

        await ExecuteSqlAsync(conn, @"
            CREATE SCHEMA test_triggers;

            CREATE TABLE test_triggers.audit_log (
                id SERIAL PRIMARY KEY,
                action TEXT,
                timestamp TIMESTAMP DEFAULT NOW()
            );

            CREATE TABLE test_triggers.employees (
                id SERIAL PRIMARY KEY,
                name TEXT,
                updated_at TIMESTAMP
            );

            -- Trigger function
            CREATE FUNCTION test_triggers.update_timestamp()
            RETURNS TRIGGER AS $$
            BEGIN
                NEW.updated_at = NOW();
                RETURN NEW;
            END;
            $$ LANGUAGE plpgsql;

            -- Before trigger
            CREATE TRIGGER before_employee_update
            BEFORE UPDATE ON test_triggers.employees
            FOR EACH ROW
            EXECUTE FUNCTION test_triggers.update_timestamp();

            -- After trigger
            CREATE FUNCTION test_triggers.log_action()
            RETURNS TRIGGER AS $$
            BEGIN
                INSERT INTO test_triggers.audit_log (action) VALUES ('Employee modified');
                RETURN NEW;
            END;
            $$ LANGUAGE plpgsql;

            CREATE TRIGGER after_employee_update
            AFTER UPDATE ON test_triggers.employees
            FOR EACH ROW
            EXECUTE FUNCTION test_triggers.log_action();
        ");
    }

    private async Task ExecuteSqlAsync(NpgsqlConnection conn, string sql)
    {
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        await cmd.ExecuteNonQueryAsync();
    }

    [Test]
    public async Task ExtractTriggers_SimpleTriggers_ExtractsCorrectly()
    {
        // Arrange
        var extractor = new PgProjectExtractor(_connectionString);

        // Act
        var project = await extractor.ExtractPgProject("testdb");

        // Assert
        var schema = project.Schemas.FirstOrDefault(s => s.Name == "test_triggers");
        Assert.That(schema, Is.Not.Null);
        Assert.That(schema!.Triggers, Is.Not.Empty, "Should have triggers");

        TestContext.Out.WriteLine($"✓ Triggers extracted: {schema.Triggers.Count}");
        foreach (var trigger in schema.Triggers)
        {
            TestContext.Out.WriteLine($"  - {trigger.Name}");
        }
    }
}
