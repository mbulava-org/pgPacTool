using mbulava.PostgreSql.Dac.Extract;
using mbulava.PostgreSql.Dac.Models;
using Npgsql;
using NUnit.Framework;
using Testcontainers.PostgreSql;

namespace ProjectExtract_Tests.Types;

/// <summary>
/// Extraction tests for Composite types (CREATE TYPE foo AS (...))
/// Covers DEV-48: No extraction tests for Composite types
/// </summary>
[TestFixture]
[Category("Types")]
[Category("CompositeTypes")]
[Category("Integration")]
public class CompositeTypeExtractionTests
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
            CREATE SCHEMA test_composite;

            -- Simple composite: address
            CREATE TYPE test_composite.address AS (
                street  TEXT,
                city    TEXT,
                zip     VARCHAR(10)
            );

            -- Composite with numeric fields
            CREATE TYPE test_composite.money_amount AS (
                amount   NUMERIC(12,2),
                currency VARCHAR(3)
            );

            -- Composite with multiple data types including timestamp
            CREATE TYPE test_composite.audit_entry AS (
                changed_by  TEXT,
                changed_at  TIMESTAMP,
                old_value   TEXT,
                new_value   TEXT
            );
        ");
    }

    private async Task ExecuteSqlAsync(NpgsqlConnection conn, string sql)
    {
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        await cmd.ExecuteNonQueryAsync();
    }

    [Test]
    public async Task ExtractCompositeTypes_SimpleComposite_ExtractedWithKindComposite()
    {
        var extractor = new PgProjectExtractor(_connectionString);
        var project = await extractor.ExtractPgProject("testdb");

        var schema = project.Schemas.FirstOrDefault(s => s.Name == "test_composite");
        Assert.That(schema, Is.Not.Null, "test_composite schema should exist");

        var addressType = schema!.Types.FirstOrDefault(t => t.Name == "address");
        Assert.That(addressType, Is.Not.Null, "address type should be extracted");
        Assert.That(addressType!.Kind, Is.EqualTo(PgTypeKind.Composite), "Kind should be Composite");
        Assert.That(addressType.Owner, Is.Not.Empty, "Owner should be set");
        Assert.That(addressType.Definition, Is.Not.Null.And.Not.Empty, "Definition should not be empty");

        TestContext.Out.WriteLine($"✓ Composite type extracted: {addressType.Name}");
        TestContext.Out.WriteLine($"  Kind: {addressType.Kind}");
        TestContext.Out.WriteLine($"  Owner: {addressType.Owner}");
    }

    [Test]
    public async Task ExtractCompositeTypes_AllComposites_CountIsCorrect()
    {
        var extractor = new PgProjectExtractor(_connectionString);
        var project = await extractor.ExtractPgProject("testdb");

        var schema = project.Schemas.FirstOrDefault(s => s.Name == "test_composite");
        Assert.That(schema, Is.Not.Null);

        var composites = schema!.Types.Where(t => t.Kind == PgTypeKind.Composite).ToList();
        Assert.That(composites.Count, Is.EqualTo(3), "Should extract all 3 composite types");

        var names = composites.Select(t => t.Name).OrderBy(n => n).ToList();
        TestContext.Out.WriteLine($"✓ Extracted {composites.Count} composite types: {string.Join(", ", names)}");
    }

    [Test]
    public async Task ExtractCompositeTypes_CompositeAttributes_ExtractedWithCorrectNames()
    {
        var extractor = new PgProjectExtractor(_connectionString);
        var project = await extractor.ExtractPgProject("testdb");

        var schema = project.Schemas.FirstOrDefault(s => s.Name == "test_composite");
        var addressType = schema?.Types.FirstOrDefault(t => t.Name == "address");

        Assert.That(addressType, Is.Not.Null);
        Assert.That(addressType!.CompositeAttributes, Is.Not.Null, "CompositeAttributes should be populated");
        Assert.That(addressType.CompositeAttributes!.Count, Is.EqualTo(3), "address should have 3 attributes");

        var attrNames = addressType.CompositeAttributes.Select(a => a.Name).ToList();
        Assert.That(attrNames, Does.Contain("street"), "Should contain 'street' attribute");
        Assert.That(attrNames, Does.Contain("city"), "Should contain 'city' attribute");
        Assert.That(attrNames, Does.Contain("zip"), "Should contain 'zip' attribute");

        TestContext.Out.WriteLine($"✓ Composite attributes extracted: {string.Join(", ", attrNames)}");
    }

    [Test]
    public async Task ExtractCompositeTypes_CompositeAttributes_DataTypesAreCorrect()
    {
        var extractor = new PgProjectExtractor(_connectionString);
        var project = await extractor.ExtractPgProject("testdb");

        var schema = project.Schemas.FirstOrDefault(s => s.Name == "test_composite");
        var moneyType = schema?.Types.FirstOrDefault(t => t.Name == "money_amount");

        Assert.That(moneyType, Is.Not.Null);
        Assert.That(moneyType!.CompositeAttributes, Is.Not.Null);

        var amountAttr = moneyType.CompositeAttributes!.FirstOrDefault(a => a.Name == "amount");
        var currencyAttr = moneyType.CompositeAttributes!.FirstOrDefault(a => a.Name == "currency");

        Assert.That(amountAttr, Is.Not.Null, "amount attribute should exist");
        Assert.That(currencyAttr, Is.Not.Null, "currency attribute should exist");
        Assert.That(amountAttr!.DataType, Does.Contain("numeric").IgnoreCase, "amount should be numeric");
        Assert.That(currencyAttr!.DataType, Does.Contain("character varying").Or.Contains("varchar").IgnoreCase, "currency should be varchar");

        TestContext.Out.WriteLine($"✓ Composite attribute data types correct:");
        TestContext.Out.WriteLine($"  amount: {amountAttr.DataType}");
        TestContext.Out.WriteLine($"  currency: {currencyAttr.DataType}");
    }

    [Test]
    public async Task ExtractCompositeTypes_AstComposite_IsPopulated()
    {
        var extractor = new PgProjectExtractor(_connectionString);
        var project = await extractor.ExtractPgProject("testdb");

        var schema = project.Schemas.FirstOrDefault(s => s.Name == "test_composite");
        var addressType = schema?.Types.FirstOrDefault(t => t.Name == "address");

        Assert.That(addressType, Is.Not.Null);
        Assert.That(addressType!.AstComposite, Is.Not.Null, "AstComposite should be populated for composite types");
        Assert.That(addressType.AstDomain, Is.Null, "AstDomain should be null for composite types");
        Assert.That(addressType.AstEnum, Is.Null, "AstEnum should be null for composite types");

        TestContext.Out.WriteLine("✓ AstComposite populated, other AST fields null as expected");
    }

    [Test]
    public async Task ExtractCompositeTypes_MultipleAttributes_AuditEntry_HasFourAttributes()
    {
        var extractor = new PgProjectExtractor(_connectionString);
        var project = await extractor.ExtractPgProject("testdb");

        var schema = project.Schemas.FirstOrDefault(s => s.Name == "test_composite");
        var auditType = schema?.Types.FirstOrDefault(t => t.Name == "audit_entry");

        Assert.That(auditType, Is.Not.Null);
        Assert.That(auditType!.CompositeAttributes, Is.Not.Null);
        Assert.That(auditType.CompositeAttributes!.Count, Is.EqualTo(4), "audit_entry should have 4 attributes");

        TestContext.Out.WriteLine($"✓ audit_entry composite has {auditType.CompositeAttributes.Count} attributes:");
        foreach (var attr in auditType.CompositeAttributes)
        {
            TestContext.Out.WriteLine($"  {attr.Name}: {attr.DataType}");
        }
    }
}
