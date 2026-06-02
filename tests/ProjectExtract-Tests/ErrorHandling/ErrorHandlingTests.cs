using mbulava.PostgreSql.Dac.Extract;
using Npgsql;
using NUnit.Framework;
using Testcontainers.PostgreSql;

namespace ProjectExtract_Tests.ErrorHandling;

/// <summary>
/// Integration tests for error handling scenarios:
/// - Invalid / malformed connection strings
/// - Missing permissions (restricted user)
/// - Non-existent database
/// Addresses DEV-485: Expand integration test coverage.
/// </summary>
[TestFixture]
[Category("ErrorHandling")]
[Category("Integration")]
public class ErrorHandlingTests
{
    private PostgreSqlContainer _pgContainer = default!;
    private string _connectionString = default!;
    private string _restrictedConnectionString = default!;

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
            MaxPoolSize = 10,
            MinPoolSize = 0,
            ConnectionIdleLifetime = 30,
            Timeout = 30
        };
        _connectionString = builder.ToString();

        await SeedTestDataAndCreateRestrictedUserAsync();
    }

    [OneTimeTearDown]
    public async Task OneTimeTeardown()
    {
        NpgsqlConnection.ClearAllPools();
        if (_pgContainer is not null)
            await _pgContainer.DisposeAsync();
    }

    private async Task SeedTestDataAndCreateRestrictedUserAsync()
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            CREATE TABLE public.orders (
                id      SERIAL PRIMARY KEY,
                amount  NUMERIC(10,2) NOT NULL
            );

            -- Create a restricted user with minimal privileges
            CREATE ROLE restricted_user LOGIN PASSWORD 'restricted_pass';
            -- Explicitly revoke CONNECT to restrict DB access.
            -- Must also revoke from PUBLIC; otherwise the PUBLIC grant allows any login role to connect.
            REVOKE CONNECT ON DATABASE testdb FROM PUBLIC;
            REVOKE CONNECT ON DATABASE testdb FROM restricted_user;
        ";
        await cmd.ExecuteNonQueryAsync();

        // Build a connection string for the restricted user
        var restrictedBuilder = new NpgsqlConnectionStringBuilder(_pgContainer.GetConnectionString())
        {
            Username = "restricted_user",
            Password = "restricted_pass",
            MaxPoolSize = 5,
            Timeout = 10
        };
        _restrictedConnectionString = restrictedBuilder.ToString();
    }

    // -------------------------------------------------------------------------
    // Invalid connection strings
    // -------------------------------------------------------------------------

    [Test]
    public void InvalidConnectionString_WrongPort_ThrowsException()
    {
        // Use a port that's almost certainly not listening
        var badConnStr = "Host=127.0.0.1;Port=19999;Database=testdb;Username=postgres;Password=testpass;Timeout=3;";
        var extractor = new PgProjectExtractor(badConnStr);

        Assert.That(
            async () => await extractor.ExtractPgProject("testdb"),
            Throws.Exception,
            "Should throw when the port is unreachable");

        TestContext.Out.WriteLine("✓ Unreachable port raises exception as expected");
    }

    [Test]
    public void InvalidConnectionString_WrongPassword_ThrowsException()
    {
        var badConnStr = new NpgsqlConnectionStringBuilder(_connectionString)
        {
            Password = "completely_wrong_password",
            Timeout = 5
        }.ToString();

        var extractor = new PgProjectExtractor(badConnStr);

        Assert.That(
            async () => await extractor.ExtractPgProject("testdb"),
            Throws.Exception,
            "Should throw when the password is wrong");

        TestContext.Out.WriteLine("✓ Wrong password raises exception as expected");
    }

    [Test]
    public void InvalidConnectionString_NonExistentDatabase_ThrowsException()
    {
        var badConnStr = new NpgsqlConnectionStringBuilder(_connectionString)
        {
            Database = "this_database_does_not_exist_at_all",
            Timeout = 5
        }.ToString();

        var extractor = new PgProjectExtractor(badConnStr);

        Assert.That(
            async () => await extractor.ExtractPgProject("this_database_does_not_exist_at_all"),
            Throws.Exception,
            "Should throw when the database does not exist");

        TestContext.Out.WriteLine("✓ Non-existent database raises exception as expected");
    }

    [Test]
    public void InvalidConnectionString_WrongHost_ThrowsException()
    {
        var badConnStr = "Host=this.host.does.not.exist.invalid;Port=5432;Database=testdb;Username=postgres;Password=testpass;Timeout=3;";
        var extractor = new PgProjectExtractor(badConnStr);

        Assert.That(
            async () => await extractor.ExtractPgProject("testdb"),
            Throws.Exception,
            "Should throw when the host cannot be resolved");

        TestContext.Out.WriteLine("✓ Unresolvable host raises exception as expected");
    }

    // -------------------------------------------------------------------------
    // Missing / insufficient permissions
    // -------------------------------------------------------------------------

    [Test]
    public void MissingPermissions_RestrictedUser_CannotConnect()
    {
        // The restricted_user has CONNECT revoked, so opening a connection must fail
        Assert.That(
            async () =>
            {
                await using var conn = new NpgsqlConnection(_restrictedConnectionString);
                await conn.OpenAsync();
            },
            Throws.Exception,
            "Restricted user with revoked CONNECT should not be able to open a connection");

        TestContext.Out.WriteLine("✓ Restricted user (CONNECT revoked) cannot connect as expected");
    }

    [Test]
    public void MissingPermissions_ExtractWithRestrictedUser_ThrowsException()
    {
        var extractor = new PgProjectExtractor(_restrictedConnectionString);

        Assert.That(
            async () => await extractor.ExtractPgProject("testdb"),
            Throws.Exception,
            "PgProjectExtractor should throw when the user has no CONNECT privilege");

        TestContext.Out.WriteLine("✓ PgProjectExtractor raises exception for restricted user as expected");
    }

    // -------------------------------------------------------------------------
    // Version detection errors
    // -------------------------------------------------------------------------

    [Test]
    public void DetectVersion_UnreachableServer_ThrowsException()
    {
        var badConnStr = "Host=127.0.0.1;Port=19999;Database=testdb;Username=postgres;Password=testpass;Timeout=3;";
        var extractor = new PgProjectExtractor(badConnStr);

        Assert.That(
            async () => await extractor.DetectPostgresVersion(),
            Throws.Exception,
            "DetectPostgresVersion should throw when the server is unreachable");

        TestContext.Out.WriteLine("✓ DetectPostgresVersion raises exception for unreachable server");
    }

    // -------------------------------------------------------------------------
    // Valid connection — sanity check that tests above are isolated
    // -------------------------------------------------------------------------

    [Test]
    public async Task ValidConnection_ExtractsSuccessfully()
    {
        var extractor = new PgProjectExtractor(_connectionString);
        var project = await extractor.ExtractPgProject("testdb");

        Assert.That(project, Is.Not.Null);
        Assert.That(project.Schemas, Is.Not.Empty);
        Assert.That(project.Schemas.Any(s => s.Name == "public"), Is.True);

        TestContext.Out.WriteLine($"✓ Valid connection extracts successfully ({project.Schemas.Count} schemas)");
    }
}
