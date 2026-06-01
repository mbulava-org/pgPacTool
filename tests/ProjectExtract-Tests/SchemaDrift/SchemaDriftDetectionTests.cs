using mbulava.PostgreSql.Dac.Extract;
using Npgsql;
using NUnit.Framework;
using Testcontainers.PostgreSql;

namespace ProjectExtract_Tests.SchemaDrift;

/// <summary>
/// Integration tests for schema drift detection:
/// - Extract baseline, apply changes, re-extract, verify differences
/// - Newly added objects appear in subsequent extraction
/// - Dropped objects disappear from subsequent extraction
/// - Column additions / modifications reflected in re-extraction
/// Addresses DEV-485: Expand integration test coverage.
/// </summary>
[TestFixture]
[Category("SchemaDrift")]
[Category("Integration")]
public class SchemaDriftDetectionTests
{
    private PostgreSqlContainer _pgContainer = default!;
    private string _connectionString = default!;

    [OneTimeSetUp]
    public async Task OneTimeSetup()
    {
        DockerAvailability.SkipIfUnavailable();

        _pgContainer = new PostgreSqlBuilder("postgres:16")
            .WithDatabase("driftdb")
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

        await SeedBaselineAsync();
    }

    [OneTimeTearDown]
    public async Task OneTimeTeardown()
    {
        NpgsqlConnection.ClearAllPools();
        if (_pgContainer is not null)
            await _pgContainer.DisposeAsync();
    }

    private async Task SeedBaselineAsync()
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            CREATE SCHEMA drift_schema;

            CREATE TABLE drift_schema.employees (
                id      SERIAL PRIMARY KEY,
                name    TEXT NOT NULL,
                dept    TEXT
            );

            CREATE TABLE drift_schema.departments (
                id      SERIAL PRIMARY KEY,
                name    TEXT UNIQUE NOT NULL
            );

            CREATE VIEW drift_schema.employee_summary AS
            SELECT e.id, e.name, e.dept
            FROM drift_schema.employees e;

            CREATE FUNCTION drift_schema.count_employees()
            RETURNS BIGINT AS $$
            BEGIN
                RETURN (SELECT COUNT(*) FROM drift_schema.employees);
            END;
            $$ LANGUAGE plpgsql;
        ";
        await cmd.ExecuteNonQueryAsync();
    }

    private async Task ExecuteSqlAsync(string sql)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        await cmd.ExecuteNonQueryAsync();
    }

    // -------------------------------------------------------------------------
    // Baseline
    // -------------------------------------------------------------------------

    [Test]
    public async Task SchemaDrift_Baseline_InitialExtractionMatchesExpected()
    {
        var extractor = new PgProjectExtractor(_connectionString);
        var project = await extractor.ExtractPgProject("driftdb");

        var schema = project.Schemas.FirstOrDefault(s => s.Name == "drift_schema");
        Assert.That(schema, Is.Not.Null, "drift_schema should exist");

        Assert.That(schema!.Tables.Any(t => t.Name == "employees"), Is.True, "employees table should exist");
        Assert.That(schema.Tables.Any(t => t.Name == "departments"), Is.True, "departments table should exist");
        Assert.That(schema.Views.Any(v => v.Name == "employee_summary"), Is.True, "employee_summary view should exist");
        Assert.That(schema.Functions.Any(f => f.Name == "count_employees"), Is.True, "count_employees function should exist");

        TestContext.Out.WriteLine("✓ Baseline extraction matches expected schema structure");
    }

    // -------------------------------------------------------------------------
    // New table appears after re-extraction
    // -------------------------------------------------------------------------

    [Test]
    public async Task SchemaDrift_NewTable_AppearsAfterReExtraction()
    {
        // Baseline — new table does not exist yet
        var before = await new PgProjectExtractor(_connectionString).ExtractPgProject("driftdb");
        var schemaBefore = before.Schemas.First(s => s.Name == "drift_schema");
        Assert.That(schemaBefore.Tables.Any(t => t.Name == "projects"), Is.False,
            "projects table should NOT exist before drift");

        // Apply drift: add a new table
        await ExecuteSqlAsync(@"
            CREATE TABLE IF NOT EXISTS drift_schema.projects (
                id          SERIAL PRIMARY KEY,
                title       TEXT NOT NULL,
                owner_id    INTEGER REFERENCES drift_schema.employees(id)
            );
        ");

        // Re-extract
        var after = await new PgProjectExtractor(_connectionString).ExtractPgProject("driftdb");
        var schemaAfter = after.Schemas.First(s => s.Name == "drift_schema");

        Assert.That(schemaAfter.Tables.Any(t => t.Name == "projects"), Is.True,
            "projects table SHOULD appear after re-extraction");

        TestContext.Out.WriteLine("✓ New table 'projects' detected after schema drift");
    }

    // -------------------------------------------------------------------------
    // New column appears on existing table
    // -------------------------------------------------------------------------

    [Test]
    public async Task SchemaDrift_NewColumn_AppearsAfterReExtraction()
    {
        // Ensure the column doesn't already exist
        var before = await new PgProjectExtractor(_connectionString).ExtractPgProject("driftdb");
        var empBefore = before.Schemas.First(s => s.Name == "drift_schema").Tables.First(t => t.Name == "employees");
        var hadSalary = empBefore.Columns.Any(c => c.Name == "salary");

        if (!hadSalary)
        {
            await ExecuteSqlAsync("ALTER TABLE drift_schema.employees ADD COLUMN IF NOT EXISTS salary NUMERIC(12,2);");
        }

        var after = await new PgProjectExtractor(_connectionString).ExtractPgProject("driftdb");
        var empAfter = after.Schemas.First(s => s.Name == "drift_schema").Tables.First(t => t.Name == "employees");

        Assert.That(empAfter.Columns.Any(c => c.Name == "salary"), Is.True,
            "salary column should appear on employees after ALTER TABLE");

        TestContext.Out.WriteLine("✓ New column 'salary' on employees detected after schema drift");
    }

    // -------------------------------------------------------------------------
    // New view appears after re-extraction
    // -------------------------------------------------------------------------

    [Test]
    public async Task SchemaDrift_NewView_AppearsAfterReExtraction()
    {
        var before = await new PgProjectExtractor(_connectionString).ExtractPgProject("driftdb");
        var schemaBefore = before.Schemas.First(s => s.Name == "drift_schema");
        Assert.That(schemaBefore.Views.Any(v => v.Name == "senior_employees"), Is.False,
            "senior_employees view should NOT exist before drift");

        await ExecuteSqlAsync(@"
            CREATE OR REPLACE VIEW drift_schema.senior_employees AS
            SELECT id, name FROM drift_schema.employees WHERE dept = 'Engineering';
        ");

        var after = await new PgProjectExtractor(_connectionString).ExtractPgProject("driftdb");
        var schemaAfter = after.Schemas.First(s => s.Name == "drift_schema");

        Assert.That(schemaAfter.Views.Any(v => v.Name == "senior_employees"), Is.True,
            "senior_employees view should appear after re-extraction");

        TestContext.Out.WriteLine("✓ New view 'senior_employees' detected after schema drift");
    }

    // -------------------------------------------------------------------------
    // Dropped object disappears from re-extraction
    // -------------------------------------------------------------------------

    [Test]
    public async Task SchemaDrift_DroppedTable_DisappearsAfterReExtraction()
    {
        // Create a throwaway table first
        await ExecuteSqlAsync(@"
            CREATE TABLE IF NOT EXISTS drift_schema.temp_audit (
                id      SERIAL PRIMARY KEY,
                entry   TEXT
            );
        ");

        // Verify it exists
        var before = await new PgProjectExtractor(_connectionString).ExtractPgProject("driftdb");
        var schemaBefore = before.Schemas.First(s => s.Name == "drift_schema");
        Assert.That(schemaBefore.Tables.Any(t => t.Name == "temp_audit"), Is.True,
            "temp_audit should exist before drop");

        // Drop it
        await ExecuteSqlAsync("DROP TABLE IF EXISTS drift_schema.temp_audit;");

        // Re-extract
        var after = await new PgProjectExtractor(_connectionString).ExtractPgProject("driftdb");
        var schemaAfter = after.Schemas.First(s => s.Name == "drift_schema");

        Assert.That(schemaAfter.Tables.Any(t => t.Name == "temp_audit"), Is.False,
            "temp_audit should disappear after DROP TABLE");

        TestContext.Out.WriteLine("✓ Dropped table 'temp_audit' correctly absent from re-extraction");
    }

    // -------------------------------------------------------------------------
    // New schema appears after re-extraction
    // -------------------------------------------------------------------------

    [Test]
    public async Task SchemaDrift_NewSchema_AppearsAfterReExtraction()
    {
        var before = await new PgProjectExtractor(_connectionString).ExtractPgProject("driftdb");
        Assert.That(before.Schemas.Any(s => s.Name == "analytics_schema"), Is.False,
            "analytics_schema should NOT exist before drift");

        await ExecuteSqlAsync(@"
            CREATE SCHEMA IF NOT EXISTS analytics_schema;
            CREATE TABLE analytics_schema.events (
                id      SERIAL PRIMARY KEY,
                event   TEXT,
                ts      TIMESTAMP DEFAULT NOW()
            );
        ");

        var after = await new PgProjectExtractor(_connectionString).ExtractPgProject("driftdb");
        Assert.That(after.Schemas.Any(s => s.Name == "analytics_schema"), Is.True,
            "analytics_schema should appear after CREATE SCHEMA");

        var analyticsSchema = after.Schemas.First(s => s.Name == "analytics_schema");
        Assert.That(analyticsSchema.Tables.Any(t => t.Name == "events"), Is.True,
            "events table should be present in new analytics_schema");

        TestContext.Out.WriteLine("✓ New schema 'analytics_schema' with table detected after schema drift");
    }
}
