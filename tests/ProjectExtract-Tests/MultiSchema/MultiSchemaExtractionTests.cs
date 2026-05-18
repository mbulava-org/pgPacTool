using mbulava.PostgreSql.Dac.Extract;
using mbulava.PostgreSql.Dac.Models;
using Npgsql;
using NUnit.Framework;
using Testcontainers.PostgreSql;

namespace ProjectExtract_Tests.MultiSchema;

/// <summary>
/// Tests for multi-schema extraction — objects in schemas other than public.
/// Covers DEV-48: ProjectExtract-Tests only tested the public schema.
///
/// These tests create objects in two non-public schemas (billing, inventory)
/// and verify that extraction correctly includes both.
/// </summary>
[TestFixture]
[Category("MultiSchema")]
[Category("Integration")]
public class MultiSchemaExtractionTests
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

        // --- Schema: billing ---
        await ExecuteSqlAsync(conn, @"
            CREATE SCHEMA billing;

            CREATE TABLE billing.invoices (
                id          SERIAL PRIMARY KEY,
                amount      NUMERIC(12,2) NOT NULL,
                issued_at   DATE NOT NULL DEFAULT CURRENT_DATE,
                status      TEXT NOT NULL DEFAULT 'pending'
            );

            CREATE TABLE billing.payments (
                id          SERIAL PRIMARY KEY,
                invoice_id  INTEGER NOT NULL REFERENCES billing.invoices(id),
                paid_at     TIMESTAMP DEFAULT NOW(),
                method      TEXT
            );

            CREATE VIEW billing.unpaid_invoices AS
            SELECT id, amount, issued_at
            FROM billing.invoices
            WHERE status = 'pending';

            CREATE FUNCTION billing.mark_paid(p_invoice_id INTEGER)
            RETURNS VOID AS $$
            BEGIN
                UPDATE billing.invoices SET status = 'paid' WHERE id = p_invoice_id;
            END;
            $$ LANGUAGE plpgsql;

            CREATE TYPE billing.payment_status AS ENUM ('pending', 'paid', 'overdue', 'cancelled');

            CREATE DOMAIN billing.money_amount AS NUMERIC(12,2)
                CHECK (VALUE >= 0);
        ");

        // --- Schema: inventory ---
        await ExecuteSqlAsync(conn, @"
            CREATE SCHEMA inventory;

            CREATE TABLE inventory.products (
                id          SERIAL PRIMARY KEY,
                sku         TEXT UNIQUE NOT NULL,
                name        TEXT NOT NULL,
                quantity    INTEGER NOT NULL DEFAULT 0,
                reorder_at  INTEGER NOT NULL DEFAULT 10
            );

            CREATE TABLE inventory.stock_movements (
                id          SERIAL PRIMARY KEY,
                product_id  INTEGER NOT NULL REFERENCES inventory.products(id),
                delta       INTEGER NOT NULL,
                moved_at    TIMESTAMP DEFAULT NOW(),
                reason      TEXT
            );

            CREATE MATERIALIZED VIEW inventory.low_stock AS
            SELECT id, sku, name, quantity
            FROM inventory.products
            WHERE quantity <= reorder_at;

            CREATE TYPE inventory.address AS (
                street  TEXT,
                city    TEXT,
                country TEXT
            );

            CREATE SEQUENCE inventory.shipment_seq
                START WITH 1000
                INCREMENT BY 1
                NO MAXVALUE;
        ");
    }

    private async Task ExecuteSqlAsync(NpgsqlConnection conn, string sql)
    {
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        await cmd.ExecuteNonQueryAsync();
    }

    // -------------------------------------------------------------------------
    // Schema presence
    // -------------------------------------------------------------------------

    [Test]
    public async Task MultiSchema_BothNonPublicSchemas_AppearInProject()
    {
        var extractor = new PgProjectExtractor(_connectionString);
        var project = await extractor.ExtractPgProject("testdb");

        var schemaNames = project.Schemas.Select(s => s.Name).ToList();
        Assert.That(schemaNames, Does.Contain("billing"), "billing schema should be extracted");
        Assert.That(schemaNames, Does.Contain("inventory"), "inventory schema should be extracted");

        TestContext.Out.WriteLine($"✓ Schemas found: {string.Join(", ", schemaNames.OrderBy(n => n))}");
    }

    [Test]
    public async Task MultiSchema_PublicSchema_StillIncluded()
    {
        var extractor = new PgProjectExtractor(_connectionString);
        var project = await extractor.ExtractPgProject("testdb");

        Assert.That(project.Schemas.Any(s => s.Name == "public"),
            Is.True, "public schema should still be present alongside custom schemas");

        TestContext.Out.WriteLine("✓ public schema still present in multi-schema extraction");
    }

    // -------------------------------------------------------------------------
    // billing schema — tables
    // -------------------------------------------------------------------------

    [Test]
    public async Task MultiSchema_BillingSchema_TablesExtracted()
    {
        var extractor = new PgProjectExtractor(_connectionString);
        var project = await extractor.ExtractPgProject("testdb");

        var billing = project.Schemas.FirstOrDefault(s => s.Name == "billing");
        Assert.That(billing, Is.Not.Null);

        var tableNames = billing!.Tables.Select(t => t.Name).OrderBy(n => n).ToList();
        Assert.That(tableNames, Does.Contain("invoices"), "billing.invoices should be extracted");
        Assert.That(tableNames, Does.Contain("payments"), "billing.payments should be extracted");
        Assert.That(billing.Tables.Count, Is.EqualTo(2), "billing schema should have exactly 2 tables");

        TestContext.Out.WriteLine($"✓ billing tables: {string.Join(", ", tableNames)}");
    }

    [Test]
    public async Task MultiSchema_BillingSchema_ViewExtracted()
    {
        var extractor = new PgProjectExtractor(_connectionString);
        var project = await extractor.ExtractPgProject("testdb");

        var billing = project.Schemas.FirstOrDefault(s => s.Name == "billing");
        Assert.That(billing, Is.Not.Null);

        var view = billing!.Views.FirstOrDefault(v => v.Name == "unpaid_invoices");
        Assert.That(view, Is.Not.Null, "billing.unpaid_invoices view should be extracted");
        Assert.That(view!.IsMaterialized, Is.False, "Should be a regular view");
        Assert.That(view.Definition, Does.Contain("pending").IgnoreCase, "Definition should reference status filter");

        TestContext.Out.WriteLine($"✓ billing.unpaid_invoices view extracted");
    }

    [Test]
    public async Task MultiSchema_BillingSchema_FunctionExtracted()
    {
        var extractor = new PgProjectExtractor(_connectionString);
        var project = await extractor.ExtractPgProject("testdb");

        var billing = project.Schemas.FirstOrDefault(s => s.Name == "billing");
        Assert.That(billing, Is.Not.Null);

        var fn = billing!.Functions.FirstOrDefault(f => f.Name == "mark_paid");
        Assert.That(fn, Is.Not.Null, "billing.mark_paid function should be extracted");

        TestContext.Out.WriteLine($"✓ billing.mark_paid function extracted");
    }

    [Test]
    public async Task MultiSchema_BillingSchema_EnumTypeExtracted()
    {
        var extractor = new PgProjectExtractor(_connectionString);
        var project = await extractor.ExtractPgProject("testdb");

        var billing = project.Schemas.FirstOrDefault(s => s.Name == "billing");
        Assert.That(billing, Is.Not.Null);

        var enumType = billing!.Types.FirstOrDefault(t => t.Name == "payment_status");
        Assert.That(enumType, Is.Not.Null, "billing.payment_status enum should be extracted");
        Assert.That(enumType!.Kind, Is.EqualTo(PgTypeKind.Enum), "Should be Enum kind");
        Assert.That(enumType.EnumLabels, Is.Not.Null.And.Not.Empty, "EnumLabels should be populated");
        Assert.That(enumType.EnumLabels, Does.Contain("paid"), "Should contain 'paid' label");

        TestContext.Out.WriteLine($"✓ billing.payment_status enum extracted with labels: {string.Join(", ", enumType.EnumLabels!)}");
    }

    [Test]
    public async Task MultiSchema_BillingSchema_DomainTypeExtracted()
    {
        var extractor = new PgProjectExtractor(_connectionString);
        var project = await extractor.ExtractPgProject("testdb");

        var billing = project.Schemas.FirstOrDefault(s => s.Name == "billing");
        Assert.That(billing, Is.Not.Null);

        var domain = billing!.Types.FirstOrDefault(t => t.Name == "money_amount");
        Assert.That(domain, Is.Not.Null, "billing.money_amount domain should be extracted");
        Assert.That(domain!.Kind, Is.EqualTo(PgTypeKind.Domain), "Should be Domain kind");

        TestContext.Out.WriteLine($"✓ billing.money_amount domain extracted");
    }

    // -------------------------------------------------------------------------
    // inventory schema — tables, materialized view, composite type, sequence
    // -------------------------------------------------------------------------

    [Test]
    public async Task MultiSchema_InventorySchema_TablesExtracted()
    {
        var extractor = new PgProjectExtractor(_connectionString);
        var project = await extractor.ExtractPgProject("testdb");

        var inventory = project.Schemas.FirstOrDefault(s => s.Name == "inventory");
        Assert.That(inventory, Is.Not.Null);

        var tableNames = inventory!.Tables.Select(t => t.Name).OrderBy(n => n).ToList();
        Assert.That(tableNames, Does.Contain("products"), "inventory.products should be extracted");
        Assert.That(tableNames, Does.Contain("stock_movements"), "inventory.stock_movements should be extracted");
        Assert.That(inventory.Tables.Count, Is.EqualTo(2), "inventory schema should have exactly 2 tables");

        TestContext.Out.WriteLine($"✓ inventory tables: {string.Join(", ", tableNames)}");
    }

    [Test]
    public async Task MultiSchema_InventorySchema_MaterializedViewExtracted()
    {
        var extractor = new PgProjectExtractor(_connectionString);
        var project = await extractor.ExtractPgProject("testdb");

        var inventory = project.Schemas.FirstOrDefault(s => s.Name == "inventory");
        Assert.That(inventory, Is.Not.Null);

        var matView = inventory!.Views.FirstOrDefault(v => v.Name == "low_stock");
        Assert.That(matView, Is.Not.Null, "inventory.low_stock materialized view should be extracted");
        Assert.That(matView!.IsMaterialized, Is.True, "low_stock should be materialized");

        TestContext.Out.WriteLine($"✓ inventory.low_stock materialized view extracted (IsMaterialized={matView.IsMaterialized})");
    }

    [Test]
    public async Task MultiSchema_InventorySchema_CompositeTypeExtracted()
    {
        var extractor = new PgProjectExtractor(_connectionString);
        var project = await extractor.ExtractPgProject("testdb");

        var inventory = project.Schemas.FirstOrDefault(s => s.Name == "inventory");
        Assert.That(inventory, Is.Not.Null);

        var compositeType = inventory!.Types.FirstOrDefault(t => t.Name == "address");
        Assert.That(compositeType, Is.Not.Null, "inventory.address composite type should be extracted");
        Assert.That(compositeType!.Kind, Is.EqualTo(PgTypeKind.Composite), "Should be Composite kind");
        Assert.That(compositeType.CompositeAttributes, Is.Not.Null.And.Not.Empty, "Should have attributes");

        var attrNames = compositeType.CompositeAttributes!.Select(a => a.Name).ToList();
        Assert.That(attrNames, Does.Contain("street"), "Should have street attribute");
        Assert.That(attrNames, Does.Contain("city"), "Should have city attribute");
        Assert.That(attrNames, Does.Contain("country"), "Should have country attribute");

        TestContext.Out.WriteLine($"✓ inventory.address composite extracted with attributes: {string.Join(", ", attrNames)}");
    }

    [Test]
    public async Task MultiSchema_InventorySchema_SequenceExtracted()
    {
        var extractor = new PgProjectExtractor(_connectionString);
        var project = await extractor.ExtractPgProject("testdb");

        var inventory = project.Schemas.FirstOrDefault(s => s.Name == "inventory");
        Assert.That(inventory, Is.Not.Null);

        var seq = inventory!.Sequences.FirstOrDefault(s => s.Name == "shipment_seq");
        Assert.That(seq, Is.Not.Null, "inventory.shipment_seq sequence should be extracted");
        Assert.That(seq!.Owner, Is.Not.Empty, "Sequence should have an owner");

        TestContext.Out.WriteLine($"✓ inventory.shipment_seq sequence extracted (Owner={seq.Owner})");
    }

    // -------------------------------------------------------------------------
    // Cross-schema isolation — objects in billing must not appear in inventory
    // -------------------------------------------------------------------------

    [Test]
    public async Task MultiSchema_BillingObjects_NotPresentInInventorySchema()
    {
        var extractor = new PgProjectExtractor(_connectionString);
        var project = await extractor.ExtractPgProject("testdb");

        var inventory = project.Schemas.FirstOrDefault(s => s.Name == "inventory");
        Assert.That(inventory, Is.Not.Null);

        Assert.That(inventory!.Tables.Any(t => t.Name == "invoices"), Is.False,
            "billing.invoices must NOT appear in inventory schema");
        Assert.That(inventory.Functions.Any(f => f.Name == "mark_paid"), Is.False,
            "billing.mark_paid must NOT appear in inventory schema");

        TestContext.Out.WriteLine("✓ Cross-schema isolation correct: billing objects not in inventory");
    }

    [Test]
    public async Task MultiSchema_InventoryObjects_NotPresentInBillingSchema()
    {
        var extractor = new PgProjectExtractor(_connectionString);
        var project = await extractor.ExtractPgProject("testdb");

        var billing = project.Schemas.FirstOrDefault(s => s.Name == "billing");
        Assert.That(billing, Is.Not.Null);

        Assert.That(billing!.Tables.Any(t => t.Name == "products"), Is.False,
            "inventory.products must NOT appear in billing schema");
        Assert.That(billing.Types.Any(t => t.Name == "address"), Is.False,
            "inventory.address must NOT appear in billing schema");

        TestContext.Out.WriteLine("✓ Cross-schema isolation correct: inventory objects not in billing");
    }

    // -------------------------------------------------------------------------
    // Comprehensive summary
    // -------------------------------------------------------------------------

    [Test]
    [Category("Comprehensive")]
    public async Task MultiSchema_Comprehensive_AllSchemasAndObjectTypesPresent()
    {
        var extractor = new PgProjectExtractor(_connectionString);
        var project = await extractor.ExtractPgProject("testdb");

        TestContext.Out.WriteLine("=== Multi-Schema Extraction Summary ===");
        foreach (var schema in project.Schemas.OrderBy(s => s.Name))
        {
            TestContext.Out.WriteLine($"\nSchema: {schema.Name} (owner: {schema.Owner})");
            TestContext.Out.WriteLine($"  Tables:    {schema.Tables.Count}  [{string.Join(", ", schema.Tables.Select(t => t.Name))}]");
            TestContext.Out.WriteLine($"  Views:     {schema.Views.Count}   [{string.Join(", ", schema.Views.Select(v => v.Name))}]");
            TestContext.Out.WriteLine($"  Functions: {schema.Functions.Count}");
            TestContext.Out.WriteLine($"  Types:     {schema.Types.Count}   [{string.Join(", ", schema.Types.Select(t => $"{t.Name}({t.Kind})"))}]");
            TestContext.Out.WriteLine($"  Sequences: {schema.Sequences.Count}");
        }

        // High-level assertions
        Assert.That(project.Schemas.Count, Is.GreaterThanOrEqualTo(3),
            "At least 3 schemas (public, billing, inventory)");

        var billing = project.Schemas.First(s => s.Name == "billing");
        var inventory = project.Schemas.First(s => s.Name == "inventory");

        Assert.That(billing.Tables.Count, Is.EqualTo(2));
        Assert.That(billing.Views.Count, Is.GreaterThanOrEqualTo(1));
        Assert.That(billing.Functions.Count, Is.GreaterThanOrEqualTo(1));
        Assert.That(billing.Types.Count, Is.GreaterThanOrEqualTo(2)); // enum + domain

        Assert.That(inventory.Tables.Count, Is.EqualTo(2));
        Assert.That(inventory.Views.Count, Is.GreaterThanOrEqualTo(1)); // materialized view
        Assert.That(inventory.Types.Count, Is.GreaterThanOrEqualTo(1)); // composite
        Assert.That(inventory.Sequences.Count, Is.GreaterThanOrEqualTo(1));

        TestContext.Out.WriteLine("\n✅ Multi-schema comprehensive check passed!");
    }
}
