using mbulava.PostgreSql.Dac.Extract;
using Npgsql;
using NUnit.Framework;
using Testcontainers.PostgreSql;

namespace ProjectExtract_Tests.Models;

/// <summary>
/// Integration tests for DEV-566 — Extract column comments from pg_description
/// and generate corresponding COMMENT ON COLUMN statements.
/// </summary>
[TestFixture]
[Category("Models")]
[Category("Integration")]
public class ColumnCommentExtractionTests
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
            CREATE SCHEMA comments_test;

            -- Table with column comments
            CREATE TABLE comments_test.products (
                id         SERIAL      PRIMARY KEY,
                name       TEXT        NOT NULL,
                price      NUMERIC(10,2),
                sku        VARCHAR(50)
            );

            -- Add column comments via COMMENT ON COLUMN
            COMMENT ON COLUMN comments_test.products.id    IS 'Unique product identifier';
            COMMENT ON COLUMN comments_test.products.name  IS 'Product display name';
            COMMENT ON COLUMN comments_test.products.price IS 'Retail price in USD';
            -- sku intentionally left without a comment

            -- Table with no column comments at all
            CREATE TABLE comments_test.audit_log (
                event_id   SERIAL PRIMARY KEY,
                event_type TEXT NOT NULL,
                created_at TIMESTAMPTZ DEFAULT now()
            );

            -- Table with a comment containing single quotes (escape test)
            CREATE TABLE comments_test.edge_cases (
                id   SERIAL PRIMARY KEY,
                note TEXT
            );
            COMMENT ON COLUMN comments_test.edge_cases.note IS 'It''s an escaped quote test';
        ";
        await cmd.ExecuteNonQueryAsync();
    }

    // -----------------------------------------------------------------------
    // Model-level assertions: PgColumn.Comment is populated correctly
    // -----------------------------------------------------------------------

    [Test]
    public async Task Extractor_ColumnWithComment_PopulatesCommentProperty()
    {
        var extractor = new PgProjectExtractor(_connectionString);
        var project = await extractor.ExtractPgProject("testdb");

        var schema = project.Schemas.FirstOrDefault(s => s.Name == "comments_test");
        Assert.That(schema, Is.Not.Null, "comments_test schema should be extracted");

        var table = schema!.Tables.FirstOrDefault(t => t.Name == "products");
        Assert.That(table, Is.Not.Null, "products table should be extracted");

        var idCol = table!.Columns.FirstOrDefault(c => c.Name == "id");
        Assert.That(idCol, Is.Not.Null, "id column should exist");
        Assert.That(idCol!.Comment, Is.EqualTo("Unique product identifier"),
            "id column comment should match");

        var nameCol = table.Columns.FirstOrDefault(c => c.Name == "name");
        Assert.That(nameCol, Is.Not.Null, "name column should exist");
        Assert.That(nameCol!.Comment, Is.EqualTo("Product display name"),
            "name column comment should match");

        var priceCol = table.Columns.FirstOrDefault(c => c.Name == "price");
        Assert.That(priceCol, Is.Not.Null, "price column should exist");
        Assert.That(priceCol!.Comment, Is.EqualTo("Retail price in USD"),
            "price column comment should match");

        TestContext.Out.WriteLine("✓ Column comments extracted correctly from pg_description");
    }

    [Test]
    public async Task Extractor_ColumnWithoutComment_HasNullComment()
    {
        var extractor = new PgProjectExtractor(_connectionString);
        var project = await extractor.ExtractPgProject("testdb");

        var schema = project.Schemas.FirstOrDefault(s => s.Name == "comments_test");
        var table = schema?.Tables.FirstOrDefault(t => t.Name == "products");
        Assert.That(table, Is.Not.Null);

        var skuCol = table!.Columns.FirstOrDefault(c => c.Name == "sku");
        Assert.That(skuCol, Is.Not.Null, "sku column should exist");
        Assert.That(skuCol!.Comment, Is.Null,
            "sku column has no comment — Comment property should be null");

        TestContext.Out.WriteLine("✓ Column without a comment correctly has null Comment");
    }

    [Test]
    public async Task Extractor_TableWithNoColumnComments_AllColumnsHaveNullComment()
    {
        var extractor = new PgProjectExtractor(_connectionString);
        var project = await extractor.ExtractPgProject("testdb");

        var schema = project.Schemas.FirstOrDefault(s => s.Name == "comments_test");
        var table = schema?.Tables.FirstOrDefault(t => t.Name == "audit_log");
        Assert.That(table, Is.Not.Null);

        Assert.That(table!.Columns, Is.Not.Empty, "audit_log should have columns");
        Assert.That(table.Columns.All(c => c.Comment is null), Is.True,
            "audit_log has no column comments — all Comment properties should be null");

        TestContext.Out.WriteLine("✓ Table with no comments has all-null Comments");
    }

    // -----------------------------------------------------------------------
    // Generator-level assertions: COMMENT ON COLUMN emitted in .sql file
    // -----------------------------------------------------------------------

    [Test]
    public async Task Generator_TableWithColumnComments_EmitsCommentOnColumnStatements()
    {
        var extractor = new PgProjectExtractor(_connectionString);
        var project = await extractor.ExtractPgProject("testdb");

        var tmpDir = Path.Combine(Path.GetTempPath(), $"pgpac_colcomment_{Guid.NewGuid():N}");
        var projectPath = Path.Combine(tmpDir, "TestProject.csproj");

        try
        {
            var generator = new CsprojProjectGenerator(projectPath);
            await generator.GenerateProjectAsync(project);

            var tableFile = Path.Combine(tmpDir, "comments_test", "Tables", "products.sql");
            Assert.That(File.Exists(tableFile), Is.True, "products.sql should be generated");

            var content = await File.ReadAllTextAsync(tableFile);

            Assert.That(content, Does.Contain("COMMENT ON COLUMN comments_test.products.id IS 'Unique product identifier';"),
                "products.sql should contain COMMENT ON COLUMN for id");
            Assert.That(content, Does.Contain("COMMENT ON COLUMN comments_test.products.name IS 'Product display name';"),
                "products.sql should contain COMMENT ON COLUMN for name");
            Assert.That(content, Does.Contain("COMMENT ON COLUMN comments_test.products.price IS 'Retail price in USD';"),
                "products.sql should contain COMMENT ON COLUMN for price");

            // sku has no comment — must not appear in the COMMENT ON block
            Assert.That(content, Does.Not.Contain("COMMENT ON COLUMN comments_test.products.sku"),
                "products.sql should NOT emit a COMMENT ON COLUMN for sku (uncommented column)");

            TestContext.Out.WriteLine("✓ COMMENT ON COLUMN statements generated correctly");
            TestContext.Out.WriteLine(content);
        }
        finally
        {
            if (Directory.Exists(tmpDir))
                Directory.Delete(tmpDir, recursive: true);
        }
    }

    [Test]
    public async Task Generator_TableWithNoColumnComments_OmitsCommentOnSection()
    {
        var extractor = new PgProjectExtractor(_connectionString);
        var project = await extractor.ExtractPgProject("testdb");

        var tmpDir = Path.Combine(Path.GetTempPath(), $"pgpac_colcomment_{Guid.NewGuid():N}");
        var projectPath = Path.Combine(tmpDir, "TestProject.csproj");

        try
        {
            var generator = new CsprojProjectGenerator(projectPath);
            await generator.GenerateProjectAsync(project);

            var tableFile = Path.Combine(tmpDir, "comments_test", "Tables", "audit_log.sql");
            Assert.That(File.Exists(tableFile), Is.True, "audit_log.sql should be generated");

            var content = await File.ReadAllTextAsync(tableFile);

            Assert.That(content, Does.Not.Contain("COMMENT ON COLUMN"),
                "audit_log.sql should not contain any COMMENT ON COLUMN statements");
            Assert.That(content, Does.Not.Contain("-- Column comments for"),
                "audit_log.sql should not contain the column-comments header");

            TestContext.Out.WriteLine("✓ audit_log.sql has no COMMENT ON COLUMN section");
        }
        finally
        {
            if (Directory.Exists(tmpDir))
                Directory.Delete(tmpDir, recursive: true);
        }
    }

    [Test]
    public async Task Generator_ColumnCommentWithSingleQuotes_IsProperlyEscaped()
    {
        var extractor = new PgProjectExtractor(_connectionString);
        var project = await extractor.ExtractPgProject("testdb");

        var tmpDir = Path.Combine(Path.GetTempPath(), $"pgpac_colcomment_{Guid.NewGuid():N}");
        var projectPath = Path.Combine(tmpDir, "TestProject.csproj");

        try
        {
            var generator = new CsprojProjectGenerator(projectPath);
            await generator.GenerateProjectAsync(project);

            var tableFile = Path.Combine(tmpDir, "comments_test", "Tables", "edge_cases.sql");
            Assert.That(File.Exists(tableFile), Is.True, "edge_cases.sql should be generated");

            var content = await File.ReadAllTextAsync(tableFile);

            // PostgreSQL single-quote escaping: ' -> ''
            Assert.That(content, Does.Contain("COMMENT ON COLUMN comments_test.edge_cases.note IS 'It''s an escaped quote test';"),
                "edge_cases.sql should have the single quote properly escaped as ''");

            TestContext.Out.WriteLine("✓ Single quotes in column comments are escaped correctly");
            TestContext.Out.WriteLine(content);
        }
        finally
        {
            if (Directory.Exists(tmpDir))
                Directory.Delete(tmpDir, recursive: true);
        }
    }
}
