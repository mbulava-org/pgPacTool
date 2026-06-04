using mbulava.PostgreSql.Dac.Extract;
using Npgsql;
using NUnit.Framework;
using Testcontainers.PostgreSql;

namespace ProjectExtract_Tests.Models;

/// <summary>
/// Tests for DEV-530 — Extract table metadata (RLS, Tablespace, FillFactor).
/// </summary>
[TestFixture]
[Category("Models")]
[Category("Integration")]
public class TableMetadataTests
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

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            CREATE SCHEMA meta_test;

            -- Plain table: no RLS, default tablespace, no fillfactor
            CREATE TABLE meta_test.plain_table (
                id SERIAL PRIMARY KEY,
                name TEXT NOT NULL
            );

            -- Table with fillfactor storage option
            CREATE TABLE meta_test.fillfactor_table (
                id SERIAL PRIMARY KEY,
                data TEXT
            ) WITH (fillfactor = 70);

            -- Table with RLS enabled
            CREATE TABLE meta_test.rls_table (
                id SERIAL PRIMARY KEY,
                secret TEXT
            );
            ALTER TABLE meta_test.rls_table ENABLE ROW LEVEL SECURITY;

            -- Table with both RLS enabled and forced
            CREATE TABLE meta_test.rls_forced_table (
                id SERIAL PRIMARY KEY,
                secret TEXT
            );
            ALTER TABLE meta_test.rls_forced_table ENABLE ROW LEVEL SECURITY;
            ALTER TABLE meta_test.rls_forced_table FORCE ROW LEVEL SECURITY;
        ";
        await cmd.ExecuteNonQueryAsync();
    }

    [Test]
    public async Task PlainTable_HasNoRlsNoFillFactor()
    {
        var extractor = new PgProjectExtractor(_connectionString);
        var project = await extractor.ExtractPgProject("testdb");

        var schema = project.Schemas.FirstOrDefault(s => s.Name == "meta_test");
        Assert.That(schema, Is.Not.Null);

        var table = schema!.Tables.FirstOrDefault(t => t.Name == "plain_table");
        Assert.That(table, Is.Not.Null);

        Assert.That(table!.RowLevelSecurity, Is.False, "plain_table should have RLS disabled");
        Assert.That(table.ForceRowLevelSecurity, Is.False, "plain_table should not have FORCE RLS");
        Assert.That(table.FillFactor, Is.Null, "plain_table should have no fillfactor");

        TestContext.Out.WriteLine($"✓ plain_table: RLS={table.RowLevelSecurity}, ForceRLS={table.ForceRowLevelSecurity}, FillFactor={table.FillFactor}");
    }

    [Test]
    public async Task FillFactorTable_HasFillFactorInModelAndDefinition()
    {
        var extractor = new PgProjectExtractor(_connectionString);
        var project = await extractor.ExtractPgProject("testdb");

        var schema = project.Schemas.FirstOrDefault(s => s.Name == "meta_test");
        var table = schema?.Tables.FirstOrDefault(t => t.Name == "fillfactor_table");
        Assert.That(table, Is.Not.Null);

        Assert.That(table!.FillFactor, Is.EqualTo(70), "fillfactor_table should have FillFactor=70");
        Assert.That(table.Definition, Does.Contain("fillfactor=70"), "Definition should contain fillfactor clause");

        TestContext.Out.WriteLine($"✓ fillfactor_table: FillFactor={table.FillFactor}");
        TestContext.Out.WriteLine($"  Definition snippet: {table.Definition.Trim()}");
    }

    [Test]
    public async Task RlsTable_HasRlsEnabledFlagSet()
    {
        var extractor = new PgProjectExtractor(_connectionString);
        var project = await extractor.ExtractPgProject("testdb");

        var schema = project.Schemas.FirstOrDefault(s => s.Name == "meta_test");
        var table = schema?.Tables.FirstOrDefault(t => t.Name == "rls_table");
        Assert.That(table, Is.Not.Null);

        Assert.That(table!.RowLevelSecurity, Is.True, "rls_table should have RLS enabled");
        Assert.That(table.ForceRowLevelSecurity, Is.False, "rls_table should NOT have FORCE RLS");

        TestContext.Out.WriteLine($"✓ rls_table: RLS={table.RowLevelSecurity}, ForceRLS={table.ForceRowLevelSecurity}");
    }

    [Test]
    public async Task RlsForcedTable_HasBothRlsFlagsSet()
    {
        var extractor = new PgProjectExtractor(_connectionString);
        var project = await extractor.ExtractPgProject("testdb");

        var schema = project.Schemas.FirstOrDefault(s => s.Name == "meta_test");
        var table = schema?.Tables.FirstOrDefault(t => t.Name == "rls_forced_table");
        Assert.That(table, Is.Not.Null);

        Assert.That(table!.RowLevelSecurity, Is.True, "rls_forced_table should have RLS enabled");
        Assert.That(table.ForceRowLevelSecurity, Is.True, "rls_forced_table should have FORCE RLS");

        TestContext.Out.WriteLine($"✓ rls_forced_table: RLS={table.RowLevelSecurity}, ForceRLS={table.ForceRowLevelSecurity}");
    }

    [Test]
    public async Task CsprojGenerator_RlsTable_EmitsAlterStatements()
    {
        var extractor = new PgProjectExtractor(_connectionString);
        var project = await extractor.ExtractPgProject("testdb");

        var tmpDir = Path.Combine(Path.GetTempPath(), $"pgpac_test_{Guid.NewGuid():N}");
        var projectPath = Path.Combine(tmpDir, "TestProject.csproj");

        try
        {
            var generator = new CsprojProjectGenerator(projectPath);
            await generator.GenerateProjectAsync(project);

            var rlsTableFile = Path.Combine(tmpDir, "meta_test", "Tables", "rls_table.sql");
            Assert.That(File.Exists(rlsTableFile), Is.True, "rls_table.sql should be generated");

            var content = await File.ReadAllTextAsync(rlsTableFile);
            Assert.That(content, Does.Contain("ENABLE ROW LEVEL SECURITY"),
                "rls_table.sql should contain ENABLE ROW LEVEL SECURITY");
            Assert.That(content, Does.Not.Contain("FORCE ROW LEVEL SECURITY"),
                "rls_table.sql should NOT contain FORCE ROW LEVEL SECURITY");

            TestContext.Out.WriteLine($"✓ rls_table.sql contains correct RLS statement");
            TestContext.Out.WriteLine(content);
        }
        finally
        {
            if (Directory.Exists(tmpDir))
                Directory.Delete(tmpDir, recursive: true);
        }
    }

    [Test]
    public async Task CsprojGenerator_RlsForcedTable_EmitsBothAlterStatements()
    {
        var extractor = new PgProjectExtractor(_connectionString);
        var project = await extractor.ExtractPgProject("testdb");

        var tmpDir = Path.Combine(Path.GetTempPath(), $"pgpac_test_{Guid.NewGuid():N}");
        var projectPath = Path.Combine(tmpDir, "TestProject.csproj");

        try
        {
            var generator = new CsprojProjectGenerator(projectPath);
            await generator.GenerateProjectAsync(project);

            var tableFile = Path.Combine(tmpDir, "meta_test", "Tables", "rls_forced_table.sql");
            Assert.That(File.Exists(tableFile), Is.True, "rls_forced_table.sql should be generated");

            var content = await File.ReadAllTextAsync(tableFile);
            Assert.That(content, Does.Contain("ENABLE ROW LEVEL SECURITY"),
                "rls_forced_table.sql should contain ENABLE ROW LEVEL SECURITY");
            Assert.That(content, Does.Contain("FORCE ROW LEVEL SECURITY"),
                "rls_forced_table.sql should contain FORCE ROW LEVEL SECURITY");

            TestContext.Out.WriteLine($"✓ rls_forced_table.sql contains both RLS statements");
            TestContext.Out.WriteLine(content);
        }
        finally
        {
            if (Directory.Exists(tmpDir))
                Directory.Delete(tmpDir, recursive: true);
        }
    }
}
