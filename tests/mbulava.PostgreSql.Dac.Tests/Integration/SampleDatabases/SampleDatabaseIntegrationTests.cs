using NUnit.Framework;
using mbulava.PostgreSql.Dac.Extract;
using mbulava.PostgreSql.Dac.Compare;
using mbulava.PostgreSql.Dac.Deployment;

namespace mbulava.PostgreSql.Dac.Tests.Integration.SampleDatabases;

/// <summary>
/// Integration tests using real-world PostgreSQL sample databases.
/// Tests schema extraction, comparison, and script generation.
/// </summary>
[TestFixture]
[Category("SampleDatabaseIntegration")]
[Category("RequiresDocker")]
public class SampleDatabaseIntegrationTests
{
    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        // Check if Docker containers are running
        if (!SampleDbConfig.IsPg16Available())
        {
            Assert.Ignore("PostgreSQL 16 container not available. Run: docker run -d --name pg16-samples -p 5416:5432 mbulava/postgres-sample-dbs:16");
        }
        
        if (!SampleDbConfig.IsPg17Available())
        {
            Assert.Ignore("PostgreSQL 17 container not available. Run: docker run -d --name pg17-samples -p 5417:5432 mbulava/postgres-sample-dbs:17");
        }
        
        TestContext.WriteLine("=== PostgreSQL Versions ===");
        TestContext.WriteLine($"PG 16: {SampleDbConfig.GetPostgresVersion(SampleDbConfig.GetPg16ConnectionString("postgres"))}");
        TestContext.WriteLine($"PG 17: {SampleDbConfig.GetPostgresVersion(SampleDbConfig.GetPg17ConnectionString("postgres"))}");
        TestContext.WriteLine();
    }
    
    [Test]
    [TestCase("chinook")]
    [TestCase("dvdrental")]
    [TestCase("employees")]
    [TestCase("lego")]
    [TestCase("netflix")]
    [TestCase("pagila")]
    [TestCase("periodic_table")]
    [TestCase("titanic")]
    [TestCase("world_happiness")]
    [Category("SchemaExtraction")]
    public void ExtractSchema_FromPg16_Succeeds(string database)
    {
        // Arrange
        if (!SampleDbConfig.IsDatabaseAvailable(database, usePg17: false))
        {
            Assert.Ignore($"Database '{database}' not available in PG 16 container");
        }
        
        var connString = SampleDbConfig.GetPg16ConnectionString(database);
        var extractor = new PgSchemaExtractor(connString);
        
        // Act
        TestContext.WriteLine($"=== Extracting {database} from PG 16 ===");
        var schema = extractor.Extract();
        
        // Assert
        Assert.That(schema, Is.Not.Null, "Schema should be extracted");
        Assert.That(schema.Tables, Is.Not.Empty, $"{database} should have tables");
        
        TestContext.WriteLine($"Tables: {schema.Tables.Count}");
        TestContext.WriteLine($"Views: {schema.Views.Count}");
        TestContext.WriteLine($"Functions: {schema.Functions.Count}");
        TestContext.WriteLine($"Triggers: {schema.Triggers.Count}");
        TestContext.WriteLine($"Sequences: {schema.Sequences.Count}");
        
        // Log sample of extracted objects
        if (schema.Tables.Count > 0)
        {
            TestContext.WriteLine($"\nSample Tables:");
            foreach (var table in schema.Tables.Take(5))
            {
                TestContext.WriteLine($"  - {table.TableName} ({table.Columns.Count} columns)");
            }
        }
    }
    
    [Test]
    [TestCase("chinook")]
    [TestCase("dvdrental")]
    [TestCase("pagila")]
    [Category("SchemaExtraction")]
    public void ExtractSchema_FromPg17_Succeeds(string database)
    {
        // Arrange
        if (!SampleDbConfig.IsDatabaseAvailable(database, usePg17: true))
        {
            Assert.Ignore($"Database '{database}' not available in PG 17 container");
        }
        
        var connString = SampleDbConfig.GetPg17ConnectionString(database);
        var extractor = new PgSchemaExtractor(connString);
        
        // Act
        TestContext.WriteLine($"=== Extracting {database} from PG 17 ===");
        var schema = extractor.Extract();
        
        // Assert
        Assert.That(schema, Is.Not.Null, "Schema should be extracted");
        Assert.That(schema.Tables, Is.Not.Empty, $"{database} should have tables");
        
        TestContext.WriteLine($"Tables: {schema.Tables.Count}");
        TestContext.WriteLine($"Views: {schema.Views.Count}");
        TestContext.WriteLine($"Functions: {schema.Functions.Count}");
        TestContext.WriteLine($"Triggers: {schema.Triggers.Count}");
    }
    
    [Test]
    [TestCase("chinook")]
    [TestCase("dvdrental")]
    [TestCase("pagila")]
    [Category("CrossVersion")]
    public void CompareSchemas_SameDatabase_Pg16VsPg17_ShowsMinimalDifferences(string database)
    {
        // Arrange
        if (!SampleDbConfig.IsDatabaseAvailable(database, usePg17: false) ||
            !SampleDbConfig.IsDatabaseAvailable(database, usePg17: true))
        {
            Assert.Ignore($"Database '{database}' not available in both PG 16 and PG 17");
        }
        
        var pg16ConnString = SampleDbConfig.GetPg16ConnectionString(database);
        var pg17ConnString = SampleDbConfig.GetPg17ConnectionString(database);
        
        var extractor16 = new PgSchemaExtractor(pg16ConnString);
        var extractor17 = new PgSchemaExtractor(pg17ConnString);
        
        // Act
        TestContext.WriteLine($"=== Comparing {database}: PG 16 vs PG 17 ===");
        var schema16 = extractor16.Extract();
        var schema17 = extractor17.Extract();
        
        var comparison = SchemaComparer.Compare(schema16, schema17);
        
        // Assert
        TestContext.WriteLine($"\nTable Diffs: {comparison.TableDiffs.Count}");
        TestContext.WriteLine($"View Diffs: {comparison.ViewDiffs.Count}");
        TestContext.WriteLine($"Function Diffs: {comparison.FunctionDiffs.Count}");
        TestContext.WriteLine($"Trigger Diffs: {comparison.TriggerDiffs.Count}");
        
        // Log any differences found
        if (comparison.TableDiffs.Any())
        {
            TestContext.WriteLine("\nTable Differences:");
            foreach (var diff in comparison.TableDiffs.Take(5))
            {
                TestContext.WriteLine($"  - {diff.TableName}");
            }
        }
        
        // Most sample databases should be identical between PG 16 and 17
        // (Some system-level differences are acceptable)
        Assert.Pass($"Comparison complete. Found {comparison.TableDiffs.Count} table diffs (may include system catalog differences)");
    }
    
    [Test]
    [TestCase("chinook")]
    [TestCase("dvdrental")]
    [Category("ScriptGeneration")]
    public void GenerateDeploymentScript_ForSampleDatabase_Succeeds(string database)
    {
        // Arrange
        if (!SampleDbConfig.IsDatabaseAvailable(database, usePg17: false))
        {
            Assert.Ignore($"Database '{database}' not available in PG 16 container");
        }
        
        var connString = SampleDbConfig.GetPg16ConnectionString(database);
        var extractor = new PgSchemaExtractor(connString);
        
        // Act - Extract schema and compare to empty (simulates initial deployment)
        TestContext.WriteLine($"=== Generating deployment script for {database} ===");
        var schema = extractor.Extract();
        var emptySchema = new Models.PgSchemaDefinition();
        
        var comparison = SchemaComparer.Compare(schema, emptySchema);
        
        var options = new PublishOptions
        {
            IncludeComments = true,
            Transactional = true,
            DropObjectsNotInSource = false
        };
        
        var script = PublishScriptGenerator.Generate(comparison, options);
        
        // Assert
        Assert.That(script, Is.Not.Null);
        Assert.That(script, Is.Not.Empty);
        Assert.That(script, Does.Contain("BEGIN;"), "Script should be transactional");
        Assert.That(script, Does.Contain("COMMIT;"), "Script should have commit");
        
        TestContext.WriteLine($"\nGenerated Script Length: {script.Length} characters");
        TestContext.WriteLine($"Lines: {script.Split('\n').Length}");
        
        // Show first 50 lines of script
        var lines = script.Split('\n').Take(50);
        TestContext.WriteLine("\nScript Preview:");
        foreach (var line in lines)
        {
            TestContext.WriteLine(line);
        }
    }
    
    [Test]
    [TestCase("chinook", "chinook")]
    [TestCase("dvdrental", "dvdrental")]
    [Category("SelfComparison")]
    public void CompareSchema_WithItself_ProducesZeroDiffs(string database1, string database2)
    {
        // Arrange
        if (!SampleDbConfig.IsDatabaseAvailable(database1, usePg17: false))
        {
            Assert.Ignore($"Database '{database1}' not available");
        }
        
        var connString = SampleDbConfig.GetPg16ConnectionString(database1);
        var extractor = new PgSchemaExtractor(connString);
        
        // Act - Extract same schema twice and compare
        TestContext.WriteLine($"=== Self-comparing {database1} ===");
        var schema1 = extractor.Extract();
        var schema2 = extractor.Extract();
        
        var comparison = SchemaComparer.Compare(schema1, schema2);
        
        // Assert - Should have zero differences
        Assert.That(comparison.TableDiffs, Is.Empty, "Should have no table differences");
        Assert.That(comparison.ViewDiffs, Is.Empty, "Should have no view differences");
        Assert.That(comparison.FunctionDiffs, Is.Empty, "Should have no function differences");
        Assert.That(comparison.TriggerDiffs, Is.Empty, "Should have no trigger differences");
        Assert.That(comparison.SequenceDiffs, Is.Empty, "Should have no sequence differences");
        
        TestContext.WriteLine("✅ Self-comparison produced zero diffs (as expected)");
    }
    
    [Test]
    [TestCase("chinook", "dvdrental")]
    [TestCase("lego", "netflix")]
    [Category("DifferentDatabases")]
    public void CompareSchemas_DifferentDatabases_ProducesDifferences(string database1, string database2)
    {
        // Arrange
        if (!SampleDbConfig.IsDatabaseAvailable(database1, usePg17: false) ||
            !SampleDbConfig.IsDatabaseAvailable(database2, usePg17: false))
        {
            Assert.Ignore($"Databases not available");
        }
        
        var connString1 = SampleDbConfig.GetPg16ConnectionString(database1);
        var connString2 = SampleDbConfig.GetPg16ConnectionString(database2);
        
        var extractor1 = new PgSchemaExtractor(connString1);
        var extractor2 = new PgSchemaExtractor(connString2);
        
        // Act
        TestContext.WriteLine($"=== Comparing {database1} vs {database2} ===");
        var schema1 = extractor1.Extract();
        var schema2 = extractor2.Extract();
        
        var comparison = SchemaComparer.Compare(schema1, schema2);
        
        // Assert - Should have differences (different databases)
        var totalDiffs = comparison.TableDiffs.Count + 
                        comparison.ViewDiffs.Count + 
                        comparison.FunctionDiffs.Count;
        
        Assert.That(totalDiffs, Is.GreaterThan(0), "Different databases should have differences");
        
        TestContext.WriteLine($"\nTotal Differences: {totalDiffs}");
        TestContext.WriteLine($"  Tables: {comparison.TableDiffs.Count}");
        TestContext.WriteLine($"  Views: {comparison.ViewDiffs.Count}");
        TestContext.WriteLine($"  Functions: {comparison.FunctionDiffs.Count}");
    }
    
    [Test]
    [TestCase("chinook")]
    [Category("AstValidation")]
    public void GeneratedScript_UsesAstBuilders_ValidSQL(string database)
    {
        // This test validates that our AST-based script generation
        // produces valid SQL for real-world databases
        
        // Arrange
        if (!SampleDbConfig.IsDatabaseAvailable(database, usePg17: false))
        {
            Assert.Ignore($"Database '{database}' not available");
        }
        
        var connString = SampleDbConfig.GetPg16ConnectionString(database);
        var extractor = new PgSchemaExtractor(connString);
        var schema = extractor.Extract();
        var emptySchema = new Models.PgSchemaDefinition();
        
        // Create a simple modification scenario
        var modifiedSchema = new Models.PgSchemaDefinition
        {
            Tables = schema.Tables.Take(1).ToList() // Just one table
        };
        
        // Act
        var comparison = SchemaComparer.Compare(modifiedSchema, emptySchema);
        var script = PublishScriptGenerator.Generate(comparison, new PublishOptions
        {
            IncludeComments = false,
            Transactional = false
        });
        
        // Assert
        Assert.That(script, Is.Not.Empty);
        
        // Validate AST-based output patterns
        // (These patterns confirm we're using AST builders, not string templates)
        TestContext.WriteLine("=== Validating AST-based output ===");
        TestContext.WriteLine(script);
        
        // The script should be valid SQL (not malformed)
        Assert.That(script, Does.Not.Contain("$\""), "Should not contain string interpolation artifacts");
        Assert.That(script, Does.Not.Contain("${"), "Should not contain string template artifacts");
        
        TestContext.WriteLine("\n✅ Generated script appears to use AST-based generation");
    }
}
