using NUnit.Framework;
using mbulava.PostgreSql.Dac.Extract;
using mbulava.PostgreSql.Dac.Compare;

namespace mbulava.PostgreSql.Dac.Tests.Integration.SampleDatabases;

/// <summary>
/// Detailed tests for the Pagila database (extended DVD rental with complex schema).
/// Pagila has views, functions, triggers - perfect for testing AST builders.
/// </summary>
[TestFixture]
[Category("SampleDatabaseIntegration")]
[Category("Pagila")]
[Category("RequiresDocker")]
public class PagilaComplexSchemaTests
{
    private string _pg16ConnectionString = null!;
    private string _pg17ConnectionString = null!;
    
    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        if (!SampleDbConfig.IsPg16Available() || !SampleDbConfig.IsPg17Available())
        {
            Assert.Ignore("PostgreSQL containers not available");
        }
        
        if (!SampleDbConfig.IsDatabaseAvailable("pagila", usePg17: false) ||
            !SampleDbConfig.IsDatabaseAvailable("pagila", usePg17: true))
        {
            Assert.Ignore("Pagila database not available in both containers");
        }
        
        _pg16ConnectionString = SampleDbConfig.GetPg16ConnectionString("pagila");
        _pg17ConnectionString = SampleDbConfig.GetPg17ConnectionString("pagila");
    }
    
    [Test]
    public void Pagila_HasExpectedTableCount()
    {
        // Arrange
        var extractor = new PgSchemaExtractor(_pg16ConnectionString);
        
        // Act
        var schema = extractor.Extract();
        
        // Assert
        Assert.That(schema.Tables.Count, Is.GreaterThanOrEqualTo(15), "Pagila should have at least 15 tables");
        
        TestContext.WriteLine($"Found {schema.Tables.Count} tables in Pagila");
        foreach (var table in schema.Tables.OrderBy(t => t.TableName))
        {
            TestContext.WriteLine($"  - {table.TableName} ({table.Columns.Count} columns)");
        }
    }
    
    [Test]
    public void Pagila_HasViews()
    {
        // Arrange
        var extractor = new PgSchemaExtractor(_pg16ConnectionString);
        
        // Act
        var schema = extractor.Extract();
        
        // Assert
        Assert.That(schema.Views.Count, Is.GreaterThan(0), "Pagila should have views");
        
        TestContext.WriteLine($"Found {schema.Views.Count} views in Pagila");
        foreach (var view in schema.Views.OrderBy(v => v.ViewName))
        {
            TestContext.WriteLine($"  - {view.ViewName}");
        }
    }
    
    [Test]
    public void Pagila_HasFunctions()
    {
        // Arrange
        var extractor = new PgSchemaExtractor(_pg16ConnectionString);
        
        // Act
        var schema = extractor.Extract();
        
        // Assert
        Assert.That(schema.Functions.Count, Is.GreaterThan(0), "Pagila should have functions");
        
        TestContext.WriteLine($"Found {schema.Functions.Count} functions in Pagila");
        foreach (var func in schema.Functions.OrderBy(f => f.FunctionName).Take(10))
        {
            TestContext.WriteLine($"  - {func.FunctionName}");
        }
    }
    
    [Test]
    public void Pagila_HasTriggers()
    {
        // Arrange
        var extractor = new PgSchemaExtractor(_pg16ConnectionString);
        
        // Act
        var schema = extractor.Extract();
        
        // Assert
        Assert.That(schema.Triggers.Count, Is.GreaterThan(0), "Pagila should have triggers");
        
        TestContext.WriteLine($"Found {schema.Triggers.Count} triggers in Pagila");
        foreach (var trigger in schema.Triggers.OrderBy(t => t.TriggerName))
        {
            TestContext.WriteLine($"  - {trigger.TriggerName} on {trigger.TableName}");
        }
    }
    
    [Test]
    public void Pagila_FilmTable_HasExpectedStructure()
    {
        // Arrange
        var extractor = new PgSchemaExtractor(_pg16ConnectionString);
        
        // Act
        var schema = extractor.Extract();
        var filmTable = schema.Tables.FirstOrDefault(t => t.TableName == "film");
        
        // Assert
        Assert.That(filmTable, Is.Not.Null, "Should have 'film' table");
        Assert.That(filmTable!.Columns.Count, Is.GreaterThan(10), "Film table should have many columns");
        
        // Check for key columns
        var columnNames = filmTable.Columns.Select(c => c.ColumnName).ToList();
        Assert.That(columnNames, Does.Contain("film_id"), "Should have film_id");
        Assert.That(columnNames, Does.Contain("title"), "Should have title");
        Assert.That(columnNames, Does.Contain("description"), "Should have description");
        
        TestContext.WriteLine($"Film table has {filmTable.Columns.Count} columns:");
        foreach (var col in filmTable.Columns)
        {
            TestContext.WriteLine($"  - {col.ColumnName} {col.DataType} {(col.IsNullable == false ? "NOT NULL" : "NULL")}");
        }
    }
    
    [Test]
    public void Pagila_Pg16VsPg17_ComparesSuccessfully()
    {
        // Arrange
        var extractor16 = new PgSchemaExtractor(_pg16ConnectionString);
        var extractor17 = new PgSchemaExtractor(_pg17ConnectionString);
        
        // Act
        var schema16 = extractor16.Extract();
        var schema17 = extractor17.Extract();
        
        TestContext.WriteLine("=== PG 16 Schema ===");
        TestContext.WriteLine($"Tables: {schema16.Tables.Count}");
        TestContext.WriteLine($"Views: {schema16.Views.Count}");
        TestContext.WriteLine($"Functions: {schema16.Functions.Count}");
        TestContext.WriteLine($"Triggers: {schema16.Triggers.Count}");
        
        TestContext.WriteLine("\n=== PG 17 Schema ===");
        TestContext.WriteLine($"Tables: {schema17.Tables.Count}");
        TestContext.WriteLine($"Views: {schema17.Views.Count}");
        TestContext.WriteLine($"Functions: {schema17.Functions.Count}");
        TestContext.WriteLine($"Triggers: {schema17.Triggers.Count}");
        
        var comparison = SchemaComparer.Compare(schema16, schema17);
        
        TestContext.WriteLine("\n=== Differences ===");
        TestContext.WriteLine($"Table Diffs: {comparison.TableDiffs.Count}");
        TestContext.WriteLine($"View Diffs: {comparison.ViewDiffs.Count}");
        TestContext.WriteLine($"Function Diffs: {comparison.FunctionDiffs.Count}");
        
        // Assert - Schemas should be very similar (same structure)
        Assert.That(schema16.Tables.Count, Is.EqualTo(schema17.Tables.Count), 
            "PG 16 and 17 should have same number of tables");
    }
    
    [Test]
    public void Pagila_ViewDropScript_UsesAstBuilder()
    {
        // This test validates that DROP VIEW operations use AST builders
        
        // Arrange
        var extractor = new PgSchemaExtractor(_pg16ConnectionString);
        var fullSchema = extractor.Extract();
        
        // Create scenario: source has no views, target has views (simulate drop)
        var sourceSchema = new Models.PgSchemaDefinition
        {
            Views = new List<Models.PgViewDefinition>() // Empty
        };
        
        var targetSchema = new Models.PgSchemaDefinition
        {
            Views = fullSchema.Views.Take(1).ToList() // One view
        };
        
        // Act
        var comparison = SchemaComparer.Compare(sourceSchema, targetSchema);
        var script = PublishScriptGenerator.Generate(comparison, new Deployment.PublishOptions
        {
            DropObjectsNotInSource = true,
            IncludeComments = false
        });
        
        // Assert
        TestContext.WriteLine("=== Generated DROP VIEW Script ===");
        TestContext.WriteLine(script);
        
        // Should contain DROP VIEW
        Assert.That(script.ToUpper(), Does.Contain("DROP"), "Should have DROP statement");
        
        // Should NOT have string template artifacts
        Assert.That(script, Does.Not.Contain("$\""), "Should not have string interpolation");
    }
}
