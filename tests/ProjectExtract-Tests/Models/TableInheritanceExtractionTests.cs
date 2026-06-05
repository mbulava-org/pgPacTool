using mbulava.PostgreSql.Dac.Extract;
using Npgsql;
using NUnit.Framework;
using Testcontainers.PostgreSql;

namespace ProjectExtract_Tests.Models;

/// <summary>
/// Integration tests for DEV-541 — Implement table inheritance extraction (INHERITS clause).
/// Validates that parent-child inheritance relationships are correctly extracted from pg_inherits,
/// that PgTable.InheritedFrom is populated, that the generated Definition SQL includes a valid
/// INHERITS (...) clause, and that inherited columns are omitted from the child table definition.
/// Tests require Docker and are skipped gracefully when Docker is unavailable.
/// </summary>
[TestFixture]
[Category("TableInheritance")]
[Category("Integration")]
public class TableInheritanceExtractionTests
{
    private PostgreSqlContainer _pgContainer = default!;
    private string _connectionString = default!;

    [OneTimeSetUp]
    public async Task OneTimeSetup()
    {
        DockerAvailability.SkipIfUnavailable();

        _pgContainer = new PostgreSqlBuilder("postgres:16")
            .WithDatabase("inheritdb")
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
            CREATE SCHEMA inherit_test;

            -- Parent table: common employee columns
            CREATE TABLE inherit_test.employees (
                id          SERIAL PRIMARY KEY,
                name        TEXT NOT NULL,
                hire_date   DATE NOT NULL
            );

            -- Child table: single-parent inheritance
            CREATE TABLE inherit_test.managers (
                department TEXT NOT NULL
            ) INHERITS (inherit_test.employees);

            -- Second child: verify sibling inheritance (also inherits employees)
            CREATE TABLE inherit_test.contractors (
                agency TEXT NOT NULL
            ) INHERITS (inherit_test.employees);

            -- Plain table: should have empty InheritedFrom
            CREATE TABLE inherit_test.plain_table (
                id   SERIAL PRIMARY KEY,
                data TEXT
            );
        ";
        await cmd.ExecuteNonQueryAsync();
    }

    // ─────────────────────────────── InheritedFrom population ────────────

    [Test]
    public async Task ChildTable_InheritedFrom_IsPopulated()
    {
        var extractor = new PgProjectExtractor(_connectionString);
        var project = await extractor.ExtractPgProject("inheritdb");

        var schema = project.Schemas.FirstOrDefault(s => s.Name == "inherit_test");
        Assert.That(schema, Is.Not.Null, "inherit_test schema must exist");

        var managersTable = schema!.Tables.FirstOrDefault(t => t.Name == "managers");
        Assert.That(managersTable, Is.Not.Null, "managers table must be extracted");

        Assert.That(managersTable!.InheritedFrom, Is.Not.Null.And.Not.Empty,
            "managers.InheritedFrom must not be empty");

        TestContext.Out.WriteLine($"✓ managers.InheritedFrom = [{string.Join(", ", managersTable.InheritedFrom)}]");
    }

    [Test]
    public async Task ChildTable_InheritedFrom_ContainsCorrectParent()
    {
        var extractor = new PgProjectExtractor(_connectionString);
        var project = await extractor.ExtractPgProject("inheritdb");

        var schema = project.Schemas.First(s => s.Name == "inherit_test");
        var managersTable = schema.Tables.First(t => t.Name == "managers");

        Assert.That(managersTable.InheritedFrom, Has.Count.EqualTo(1),
            "managers should have exactly one parent");
        Assert.That(managersTable.InheritedFrom[0], Is.EqualTo("inherit_test.employees"),
            "managers parent should be inherit_test.employees");

        TestContext.Out.WriteLine($"✓ managers.InheritedFrom[0] = {managersTable.InheritedFrom[0]}");
    }

    [Test]
    public async Task SecondChildTable_InheritedFrom_ContainsCorrectParent()
    {
        var extractor = new PgProjectExtractor(_connectionString);
        var project = await extractor.ExtractPgProject("inheritdb");

        var schema = project.Schemas.First(s => s.Name == "inherit_test");
        var contractorsTable = schema.Tables.First(t => t.Name == "contractors");

        Assert.That(contractorsTable.InheritedFrom, Has.Count.EqualTo(1),
            "contractors should have exactly one parent");
        Assert.That(contractorsTable.InheritedFrom[0], Is.EqualTo("inherit_test.employees"),
            "contractors parent should be inherit_test.employees");

        TestContext.Out.WriteLine($"✓ contractors.InheritedFrom[0] = {contractorsTable.InheritedFrom[0]}");
    }

    [Test]
    public async Task ParentTable_InheritedFrom_IsEmpty()
    {
        var extractor = new PgProjectExtractor(_connectionString);
        var project = await extractor.ExtractPgProject("inheritdb");

        var schema = project.Schemas.First(s => s.Name == "inherit_test");
        var employeesTable = schema.Tables.First(t => t.Name == "employees");

        Assert.That(employeesTable.InheritedFrom, Is.Empty,
            "employees (parent) should have an empty InheritedFrom list");

        TestContext.Out.WriteLine("✓ employees.InheritedFrom is empty (expected for parent table)");
    }

    [Test]
    public async Task PlainTable_InheritedFrom_IsEmpty()
    {
        var extractor = new PgProjectExtractor(_connectionString);
        var project = await extractor.ExtractPgProject("inheritdb");

        var schema = project.Schemas.First(s => s.Name == "inherit_test");
        var plainTable = schema.Tables.First(t => t.Name == "plain_table");

        Assert.That(plainTable.InheritedFrom, Is.Empty,
            "plain_table should have an empty InheritedFrom list");

        TestContext.Out.WriteLine("✓ plain_table.InheritedFrom is empty (expected for non-inheriting table)");
    }

    // ─────────────────────────────── Definition SQL (INHERITS clause) ────

    [Test]
    public async Task ChildTable_Definition_ContainsInheritsClause()
    {
        var extractor = new PgProjectExtractor(_connectionString);
        var project = await extractor.ExtractPgProject("inheritdb");

        var schema = project.Schemas.First(s => s.Name == "inherit_test");
        var managersTable = schema.Tables.First(t => t.Name == "managers");

        Assert.That(managersTable.Definition, Does.Contain("INHERITS"),
            "managers Definition SQL must contain INHERITS keyword");
        Assert.That(managersTable.Definition, Does.Contain("\"employees\"").Or.Contain("employees"),
            "managers Definition SQL must reference the employees parent table");

        TestContext.Out.WriteLine("✓ managers Definition SQL:");
        TestContext.Out.WriteLine(managersTable.Definition);
    }

    [Test]
    public async Task ParentTable_Definition_DoesNotContainInheritsClause()
    {
        var extractor = new PgProjectExtractor(_connectionString);
        var project = await extractor.ExtractPgProject("inheritdb");

        var schema = project.Schemas.First(s => s.Name == "inherit_test");
        var employeesTable = schema.Tables.First(t => t.Name == "employees");

        Assert.That(employeesTable.Definition, Does.Not.Contain("INHERITS"),
            "employees (parent) Definition SQL must NOT contain INHERITS");

        TestContext.Out.WriteLine("✓ employees Definition SQL (no INHERITS):");
        TestContext.Out.WriteLine(employeesTable.Definition);
    }

    // ────────────────────────── Inherited column filtering ───────────────

    [Test]
    public async Task ChildTable_Definition_DoesNotRepeatInheritedColumns()
    {
        var extractor = new PgProjectExtractor(_connectionString);
        var project = await extractor.ExtractPgProject("inheritdb");

        var schema = project.Schemas.First(s => s.Name == "inherit_test");
        var managersTable = schema.Tables.First(t => t.Name == "managers");

        // Inherited columns (id, name, hire_date) must NOT appear in child's CREATE TABLE body.
        // The only own column is "department".
        Assert.That(managersTable.Definition, Does.Not.Contain("\"id\"").And.Not.Contain(" id ").And.Not.Contain("\nid"),
            "managers Definition must not redeclare the inherited 'id' column");
        Assert.That(managersTable.Definition, Does.Not.Contain("\"name\"").And.Not.Contain(" name ").And.Not.Contain("\nname"),
            "managers Definition must not redeclare the inherited 'name' column");
        Assert.That(managersTable.Definition, Does.Not.Contain("\"hire_date\"").And.Not.Contain(" hire_date ").And.Not.Contain("\nhire_date"),
            "managers Definition must not redeclare the inherited 'hire_date' column");
        Assert.That(managersTable.Definition, Does.Contain("department"),
            "managers Definition must include its own 'department' column");

        TestContext.Out.WriteLine("✓ managers Definition does not repeat inherited columns");
        TestContext.Out.WriteLine(managersTable.Definition);
    }

    // ────────────────────────── Deployment ordering ──────────────────────

    [Test]
    public async Task AllInheritanceTables_AreExtracted()
    {
        var extractor = new PgProjectExtractor(_connectionString);
        var project = await extractor.ExtractPgProject("inheritdb");

        var schema = project.Schemas.First(s => s.Name == "inherit_test");

        var tableNames = schema.Tables.Select(t => t.Name).ToHashSet();
        Assert.That(tableNames, Does.Contain("employees"), "employees (parent) must be extracted");
        Assert.That(tableNames, Does.Contain("managers"), "managers (child) must be extracted");
        Assert.That(tableNames, Does.Contain("contractors"), "contractors (child) must be extracted");
        Assert.That(tableNames, Does.Contain("plain_table"), "plain_table must be extracted");

        TestContext.Out.WriteLine($"✓ All {schema.Tables.Count} tables extracted from inherit_test");
        foreach (var t in schema.Tables)
        {
            TestContext.Out.WriteLine($"   {t.Name}: InheritedFrom=[{string.Join(", ", t.InheritedFrom)}]");
        }
    }
}
