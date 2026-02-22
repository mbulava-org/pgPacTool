using mbulava.PostgreSql.Dac.Extract;
using Npgsql;
using NUnit.Framework;
using Testcontainers.PostgreSql;

namespace ProjectExtract_Tests.Functions;

/// <summary>
/// Tests for function and procedure extraction (Issues #2 and #3)
/// </summary>
[TestFixture]
[Category("Functions")]
[Category("Integration")]
public class FunctionExtractionTests
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
            CREATE SCHEMA test_functions;

            -- Simple function
            CREATE FUNCTION test_functions.add_numbers(a INTEGER, b INTEGER)
            RETURNS INTEGER AS $$
            BEGIN
                RETURN a + b;
            END;
            $$ LANGUAGE plpgsql;

            -- Function with OUT parameters
            CREATE FUNCTION test_functions.get_stats(OUT total INT, OUT avg_val NUMERIC)
            AS $$
            BEGIN
                total := 100;
                avg_val := 50.5;
            END;
            $$ LANGUAGE plpgsql;

            -- Procedure (PostgreSQL 11+)
            CREATE PROCEDURE test_functions.update_timestamp()
            LANGUAGE plpgsql
            AS $$
            BEGIN
                -- Procedure logic
                RAISE NOTICE 'Timestamp updated';
            END;
            $$;

            -- SQL function
            CREATE FUNCTION test_functions.square(x INTEGER)
            RETURNS INTEGER AS $$
                SELECT x * x;
            $$ LANGUAGE sql IMMUTABLE;
        ");
    }

    private async Task ExecuteSqlAsync(NpgsqlConnection conn, string sql)
    {
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        await cmd.ExecuteNonQueryAsync();
    }

    [Test]
    public async Task ExtractFunctions_SimpleFunctions_ExtractsCorrectly()
    {
        // Arrange
        var extractor = new PgProjectExtractor(_connectionString);

        // Act
        var project = await extractor.ExtractPgProject("testdb");

        // Assert
        var schema = project.Schemas.FirstOrDefault(s => s.Name == "test_functions");
        Assert.That(schema, Is.Not.Null);
        Assert.That(schema!.Functions, Is.Not.Empty, "Should have functions");

        var addFunc = schema.Functions.FirstOrDefault(f => f.Name == "add_numbers");
        Assert.That(addFunc, Is.Not.Null, "add_numbers function should exist");

        TestContext.Out.WriteLine($"✓ Functions extracted: {schema.Functions.Count}");
        foreach (var func in schema.Functions)
        {
            TestContext.Out.WriteLine($"  - {func.Name}");
        }
    }

    [Test]
    public async Task ExtractFunctions_Procedures_ExtractsCorrectly()
    {
        // Arrange
        var extractor = new PgProjectExtractor(_connectionString);

        // Act
        var project = await extractor.ExtractPgProject("testdb");

        // Assert
        var schema = project.Schemas.FirstOrDefault(s => s.Name == "test_functions");
        var procedure = schema?.Functions.FirstOrDefault(f => f.Name == "update_timestamp");

        Assert.That(procedure, Is.Not.Null, "Procedure should be extracted as function");

        TestContext.Out.WriteLine($"✓ Procedure extracted: {procedure!.Name}");
    }
}
