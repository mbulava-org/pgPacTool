using mbulava.PostgreSql.Dac.Extract;
using mbulava.PostgreSql.Dac.Models;
using Npgsql;
using NUnit.Framework;
using Testcontainers.PostgreSql;

namespace ProjectExtract_Tests.Types;

/// <summary>
/// Extraction tests for Domain types (CREATE DOMAIN ... AS ...)
/// Covers DEV-48: No extraction tests for Domain types
/// </summary>
[TestFixture]
[Category("Types")]
[Category("DomainTypes")]
[Category("Integration")]
public class DomainTypeExtractionTests
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
        if (_pgContainer is not null) await _pgContainer.DisposeAsync();
    }

    private async Task SeedTestDataAsync()
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();

        await ExecuteSqlAsync(conn, @"
            CREATE SCHEMA test_domains;

            -- Simple domain: email
            CREATE DOMAIN test_domains.email_address AS VARCHAR(255)
                CHECK (VALUE ~ '^[a-zA-Z0-9._%+\-]+@[a-zA-Z0-9.\-]+\.[a-zA-Z]{2,}$');

            -- Domain with NOT NULL constraint
            CREATE DOMAIN test_domains.positive_int AS INTEGER
                NOT NULL
                CHECK (VALUE > 0);

            -- Domain without constraints (just a type alias)
            CREATE DOMAIN test_domains.short_text AS VARCHAR(50);

            -- Domain over numeric with a range check
            CREATE DOMAIN test_domains.percentage AS NUMERIC(5,2)
                CHECK (VALUE >= 0.0 AND VALUE <= 100.0);
        ");
    }

    private async Task ExecuteSqlAsync(NpgsqlConnection conn, string sql)
    {
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        await cmd.ExecuteNonQueryAsync();
    }

    [Test]
    public async Task ExtractDomainTypes_SimpleDomain_ExtractedWithKindDomain()
    {
        var extractor = new PgProjectExtractor(_connectionString);
        var project = await extractor.ExtractPgProject("testdb");

        var schema = project.Schemas.FirstOrDefault(s => s.Name == "test_domains");
        Assert.That(schema, Is.Not.Null, "test_domains schema should exist");

        var emailDomain = schema!.Types.FirstOrDefault(t => t.Name == "email_address");
        Assert.That(emailDomain, Is.Not.Null, "email_address domain should be extracted");
        Assert.That(emailDomain!.Kind, Is.EqualTo(PgTypeKind.Domain), "Kind should be Domain");
        Assert.That(emailDomain.Owner, Is.Not.Empty, "Owner should be set");
        Assert.That(emailDomain.Definition, Is.Not.Null.And.Not.Empty, "Definition should not be empty");

        TestContext.Out.WriteLine($"✓ Domain type extracted: {emailDomain.Name}");
        TestContext.Out.WriteLine($"  Kind: {emailDomain.Kind}");
        TestContext.Out.WriteLine($"  Owner: {emailDomain.Owner}");
        TestContext.Out.WriteLine($"  Definition: {emailDomain.Definition}");
    }

    [Test]
    public async Task ExtractDomainTypes_AllDomains_CountIsCorrect()
    {
        var extractor = new PgProjectExtractor(_connectionString);
        var project = await extractor.ExtractPgProject("testdb");

        var schema = project.Schemas.FirstOrDefault(s => s.Name == "test_domains");
        Assert.That(schema, Is.Not.Null);

        var domains = schema!.Types.Where(t => t.Kind == PgTypeKind.Domain).ToList();
        Assert.That(domains.Count, Is.EqualTo(4), "Should extract all 4 domain types");

        var names = domains.Select(t => t.Name).OrderBy(n => n).ToList();
        TestContext.Out.WriteLine($"✓ Extracted {domains.Count} domain types: {string.Join(", ", names)}");
    }

    [Test]
    public async Task ExtractDomainTypes_DomainDefinition_ContainsBaseType()
    {
        var extractor = new PgProjectExtractor(_connectionString);
        var project = await extractor.ExtractPgProject("testdb");

        var schema = project.Schemas.FirstOrDefault(s => s.Name == "test_domains");
        var shortText = schema?.Types.FirstOrDefault(t => t.Name == "short_text");

        Assert.That(shortText, Is.Not.Null);
        Assert.That(shortText!.Definition, Does.Contain("character varying").Or.Contains("varchar").IgnoreCase,
            "Definition should reference the base type varchar");

        TestContext.Out.WriteLine($"✓ Domain definition contains base type:");
        TestContext.Out.WriteLine($"  {shortText.Definition}");
    }

    [Test]
    public async Task ExtractDomainTypes_DomainWithCheckConstraint_DefinitionContainsCheck()
    {
        var extractor = new PgProjectExtractor(_connectionString);
        var project = await extractor.ExtractPgProject("testdb");

        var schema = project.Schemas.FirstOrDefault(s => s.Name == "test_domains");
        var percentageDomain = schema?.Types.FirstOrDefault(t => t.Name == "percentage");

        Assert.That(percentageDomain, Is.Not.Null);
        Assert.That(percentageDomain!.Definition, Does.Contain("CHECK").IgnoreCase,
            "Definition should contain CHECK clause");

        TestContext.Out.WriteLine($"✓ Domain with CHECK constraint extracted:");
        TestContext.Out.WriteLine($"  {percentageDomain.Definition}");
    }

    [Test]
    public async Task ExtractDomainTypes_DomainWithNotNull_DefinitionContainsNotNull()
    {
        var extractor = new PgProjectExtractor(_connectionString);
        var project = await extractor.ExtractPgProject("testdb");

        var schema = project.Schemas.FirstOrDefault(s => s.Name == "test_domains");
        var positiveInt = schema?.Types.FirstOrDefault(t => t.Name == "positive_int");

        Assert.That(positiveInt, Is.Not.Null);
        Assert.That(positiveInt!.Definition, Does.Contain("NOT NULL").IgnoreCase,
            "Definition should contain NOT NULL");

        TestContext.Out.WriteLine($"✓ Domain with NOT NULL constraint extracted:");
        TestContext.Out.WriteLine($"  {positiveInt.Definition}");
    }

    [Test]
    public async Task ExtractDomainTypes_AstDomain_IsPopulated()
    {
        var extractor = new PgProjectExtractor(_connectionString);
        var project = await extractor.ExtractPgProject("testdb");

        var schema = project.Schemas.FirstOrDefault(s => s.Name == "test_domains");
        var emailDomain = schema?.Types.FirstOrDefault(t => t.Name == "email_address");

        Assert.That(emailDomain, Is.Not.Null);
        Assert.That(emailDomain!.AstDomain, Is.Not.Null, "AstDomain should be populated for domain types");
        Assert.That(emailDomain.AstComposite, Is.Null, "AstComposite should be null for domain types");
        Assert.That(emailDomain.AstEnum, Is.Null, "AstEnum should be null for domain types");

        TestContext.Out.WriteLine("✓ AstDomain populated, other AST fields null as expected");
    }

    [Test]
    public async Task ExtractDomainTypes_NoDomainInSchema_TypesListEmpty()
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "CREATE SCHEMA empty_domain_schema;";
        await cmd.ExecuteNonQueryAsync();

        var extractor = new PgProjectExtractor(_connectionString);
        var project = await extractor.ExtractPgProject("testdb");

        var schema = project.Schemas.FirstOrDefault(s => s.Name == "empty_domain_schema");
        Assert.That(schema, Is.Not.Null, "empty_domain_schema should exist");
        Assert.That(schema!.Types, Is.Empty, "Should have no types in empty schema");

        TestContext.Out.WriteLine("✓ Empty schema has no domain types");
    }
}
