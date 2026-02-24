using FluentAssertions;
using mbulava.PostgreSql.Dac.Compile;
using mbulava.PostgreSql.Dac.Models;
using System.IO.Compression;
using System.Text.Json;

namespace mbulava.PostgreSql.Dac.Tests.Integration;

/// <summary>
/// Integration tests for SDK-style .csproj projects.
/// Tests the full workflow: .csproj → compile → .pgpac → use in other commands.
/// </summary>
[TestFixture]
public class CsprojIntegrationTests
{
    private string _testProjectsDir = null!;
    private string _outputDir = null!;

    [SetUp]
    public void Setup()
    {
        // Get test projects directory
        var currentDir = TestContext.CurrentContext.TestDirectory;
        _testProjectsDir = Path.Combine(currentDir, "..", "..", "..", "..", "TestProjects");
        _testProjectsDir = Path.GetFullPath(_testProjectsDir);

        // Create output directory for test artifacts
        _outputDir = Path.Combine(Path.GetTempPath(), "pgPacTool_Tests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(_outputDir);
    }

    [TearDown]
    public void Teardown()
    {
        // Cleanup test outputs
        if (Directory.Exists(_outputDir))
        {
            try
            {
                Directory.Delete(_outputDir, true);
            }
            catch
            {
                // Ignore cleanup errors
            }
        }
    }

    [Test]
    public async Task SampleDatabase_CanCompileToPgpac()
    {
        // Arrange
        var projectPath = Path.Combine(_testProjectsDir, "SampleDatabase", "SampleDatabase.csproj");
        var outputPath = Path.Combine(_outputDir, "SampleDatabase.pgpac");
        
        if (!File.Exists(projectPath))
        {
            Assert.Ignore($"Test project not found: {projectPath}");
            return;
        }

        // Act
        var loader = new CsprojProjectLoader(projectPath);
        var actualOutputPath = await loader.CompileAndGenerateOutputAsync(outputPath, OutputFormat.DacPac);

        // Assert
        actualOutputPath.Should().Be(outputPath);
        File.Exists(outputPath).Should().BeTrue("pgpac file should be created");
        
        var fileInfo = new FileInfo(outputPath);
        fileInfo.Length.Should().BeGreaterThan(0, "pgpac file should not be empty");
    }

    [Test]
    public async Task SampleDatabase_PgpacContainsValidContent()
    {
        // Arrange
        var projectPath = Path.Combine(_testProjectsDir, "SampleDatabase", "SampleDatabase.csproj");
        var outputPath = Path.Combine(_outputDir, "SampleDatabase.pgpac");
        
        if (!File.Exists(projectPath))
        {
            Assert.Ignore($"Test project not found: {projectPath}");
            return;
        }

        // Act
        var loader = new CsprojProjectLoader(projectPath);
        await loader.CompileAndGenerateOutputAsync(outputPath, OutputFormat.DacPac);

        // Extract and validate content.json
        using var archive = ZipFile.OpenRead(outputPath);
        var contentEntry = archive.GetEntry("content.json");

        // Assert
        contentEntry.Should().NotBeNull("pgpac should contain content.json");
        
        await using var stream = contentEntry!.Open();
        var project = await PgProject.Load(stream);

        project.Should().NotBeNull();
        project.DatabaseName.Should().Be("SampleDatabase");
        project.Schemas.Should().HaveCount(1);
        
        var schema = project.Schemas[0];
        schema.Name.Should().Be("public");
        schema.Tables.Should().HaveCountGreaterThan(0, "should have tables");
        schema.Views.Should().HaveCountGreaterThan(0, "should have views");
        schema.Functions.Should().HaveCountGreaterThan(0, "should have functions");
    }

    [Test]
    public async Task SampleDatabase_ExtractsCorrectObjectCounts()
    {
        // Arrange
        var projectPath = Path.Combine(_testProjectsDir, "SampleDatabase", "SampleDatabase.csproj");
        
        if (!File.Exists(projectPath))
        {
            Assert.Ignore($"Test project not found: {projectPath}");
            return;
        }

        // Act
        var loader = new CsprojProjectLoader(projectPath);
        var project = await loader.LoadProjectAsync();

        // Assert - based on our test project structure
        var schema = project.Schemas[0];
        schema.Tables.Should().HaveCount(4, "users, orders, products, order_items");
        schema.Views.Should().HaveCount(2, "active_orders, user_order_summary");
        schema.Functions.Should().HaveCount(2, "calculate_order_total, update_order_total");
        schema.Types.Should().HaveCount(1, "order_status");
        schema.Sequences.Should().HaveCount(1, "order_number_seq");
        schema.Triggers.Should().HaveCount(1, "trg_order_items_update_total");
    }

    [Test]
    public async Task SampleDatabase_CanCompileToJson()
    {
        // Arrange
        var projectPath = Path.Combine(_testProjectsDir, "SampleDatabase", "SampleDatabase.csproj");
        var outputPath = Path.Combine(_outputDir, "SampleDatabase.pgproj.json");
        
        if (!File.Exists(projectPath))
        {
            Assert.Ignore($"Test project not found: {projectPath}");
            return;
        }

        // Act
        var loader = new CsprojProjectLoader(projectPath);
        var actualOutputPath = await loader.CompileAndGenerateOutputAsync(outputPath, OutputFormat.Json);

        // Assert
        actualOutputPath.Should().Be(outputPath);
        File.Exists(outputPath).Should().BeTrue("json file should be created");
        
        // Validate JSON content
        await using var fileStream = File.OpenRead(outputPath);
        var project = await PgProject.Load(fileStream);
        
        project.Should().NotBeNull();
        project.DatabaseName.Should().Be("SampleDatabase");
    }

    [Test]
    public async Task MultiSchemaDatabase_SupportsMultipleSchemas()
    {
        // Arrange
        var projectPath = Path.Combine(_testProjectsDir, "MultiSchemaDatabase", "MultiSchemaDatabase.csproj");
        
        if (!File.Exists(projectPath))
        {
            Assert.Ignore($"Test project not found: {projectPath}");
            return;
        }

        // Act
        var loader = new CsprojProjectLoader(projectPath);
        var project = await loader.LoadProjectAsync();

        // Assert
        // Note: Currently we only support single schema (public)
        // This test documents current limitation
        project.Schemas.Should().HaveCount(1);
        project.Schemas[0].Name.Should().Be("public");
        
        // All objects from both folders are loaded into public schema
        var schema = project.Schemas[0];
        schema.Tables.Should().HaveCountGreaterThan(0);
    }

    [Test]
    public async Task SampleDatabase_PgpacCanBeReloaded()
    {
        // Arrange
        var projectPath = Path.Combine(_testProjectsDir, "SampleDatabase", "SampleDatabase.csproj");
        var pgpacPath = Path.Combine(_outputDir, "SampleDatabase.pgpac");
        
        if (!File.Exists(projectPath))
        {
            Assert.Ignore($"Test project not found: {projectPath}");
            return;
        }

        // Act - Generate pgpac
        var loader = new CsprojProjectLoader(projectPath);
        await loader.CompileAndGenerateOutputAsync(pgpacPath, OutputFormat.DacPac);

        // Act - Reload from pgpac
        using var archive = ZipFile.OpenRead(pgpacPath);
        var contentEntry = archive.GetEntry("content.json");
        await using var stream = contentEntry!.Open();
        var reloadedProject = await PgProject.Load(stream);

        // Assert - Should match original
        var originalProject = await loader.LoadProjectAsync();
        
        reloadedProject.DatabaseName.Should().Be(originalProject.DatabaseName);
        reloadedProject.Schemas.Should().HaveCount(originalProject.Schemas.Count);
        
        var originalSchema = originalProject.Schemas[0];
        var reloadedSchema = reloadedProject.Schemas[0];
        
        reloadedSchema.Tables.Count.Should().Be(originalSchema.Tables.Count);
        reloadedSchema.Views.Count.Should().Be(originalSchema.Views.Count);
        reloadedSchema.Functions.Count.Should().Be(originalSchema.Functions.Count);
        reloadedSchema.Types.Count.Should().Be(originalSchema.Types.Count);
        reloadedSchema.Sequences.Count.Should().Be(originalSchema.Sequences.Count);
        reloadedSchema.Triggers.Count.Should().Be(originalSchema.Triggers.Count);
    }

    [Test]
    public async Task SampleDatabase_AllSqlFilesHaveDefinitions()
    {
        // Arrange
        var projectPath = Path.Combine(_testProjectsDir, "SampleDatabase", "SampleDatabase.csproj");
        
        if (!File.Exists(projectPath))
        {
            Assert.Ignore($"Test project not found: {projectPath}");
            return;
        }

        // Act
        var loader = new CsprojProjectLoader(projectPath);
        var project = await loader.LoadProjectAsync();

        // Assert - Every object should have a definition
        var schema = project.Schemas[0];
        
        foreach (var table in schema.Tables)
        {
            table.Definition.Should().NotBeNullOrWhiteSpace($"Table {table.Name} should have definition");
            table.Definition.Should().Contain("CREATE TABLE", $"Table {table.Name} definition should contain CREATE TABLE");
        }

        foreach (var view in schema.Views)
        {
            view.Definition.Should().NotBeNullOrWhiteSpace($"View {view.Name} should have definition");
            view.Definition.Should().ContainAny("CREATE VIEW", "CREATE OR REPLACE VIEW");
        }

        foreach (var function in schema.Functions)
        {
            function.Definition.Should().NotBeNullOrWhiteSpace($"Function {function.Name} should have definition");
            function.Definition.Should().ContainAny("CREATE FUNCTION", "CREATE OR REPLACE FUNCTION");
        }

        foreach (var type in schema.Types)
        {
            type.Definition.Should().NotBeNullOrWhiteSpace($"Type {type.Name} should have definition");
            type.Definition.Should().Contain("CREATE TYPE");
        }

        foreach (var sequence in schema.Sequences)
        {
            sequence.Definition.Should().NotBeNullOrWhiteSpace($"Sequence {sequence.Name} should have definition");
            sequence.Definition.Should().Contain("CREATE SEQUENCE");
        }

        foreach (var trigger in schema.Triggers)
        {
            trigger.Definition.Should().NotBeNullOrWhiteSpace($"Trigger {trigger.Name} should have definition");
            trigger.Definition.Should().Contain("CREATE TRIGGER");
        }
    }

    [Test]
    public async Task SampleDatabase_ObjectsHaveCorrectNames()
    {
        // Arrange
        var projectPath = Path.Combine(_testProjectsDir, "SampleDatabase", "SampleDatabase.csproj");
        
        if (!File.Exists(projectPath))
        {
            Assert.Ignore($"Test project not found: {projectPath}");
            return;
        }

        // Act
        var loader = new CsprojProjectLoader(projectPath);
        var project = await loader.LoadProjectAsync();
        var schema = project.Schemas[0];

        // Assert - Check specific object names
        schema.Tables.Select(t => t.Name).Should().Contain(new[] { "users", "orders", "products", "order_items" });
        schema.Views.Select(v => v.Name).Should().Contain(new[] { "active_orders", "user_order_summary" });
        schema.Functions.Select(f => f.Name).Should().Contain(new[] { "calculate_order_total", "update_order_total" });
        schema.Types.Select(t => t.Name).Should().Contain("order_status");
        schema.Sequences.Select(s => s.Name).Should().Contain("order_number_seq");
        schema.Triggers.Select(t => t.Name).Should().Contain("trg_order_items_update_total");
    }

    [Test]
    public async Task SampleDatabase_TypeIsCorrectlyIdentifiedAsEnum()
    {
        // Arrange
        var projectPath = Path.Combine(_testProjectsDir, "SampleDatabase", "SampleDatabase.csproj");
        
        if (!File.Exists(projectPath))
        {
            Assert.Ignore($"Test project not found: {projectPath}");
            return;
        }

        // Act
        var loader = new CsprojProjectLoader(projectPath);
        var project = await loader.LoadProjectAsync();
        var schema = project.Schemas[0];

        // Assert
        var orderStatusType = schema.Types.FirstOrDefault(t => t.Name == "order_status");
        orderStatusType.Should().NotBeNull();
        orderStatusType!.Kind.Should().Be(PgTypeKind.Enum);
    }

    [Test]
    public async Task SampleDatabase_CanCompileWithDefaultOutputPath()
    {
        // Arrange
        var projectPath = Path.Combine(_testProjectsDir, "SampleDatabase", "SampleDatabase.csproj");
        
        if (!File.Exists(projectPath))
        {
            Assert.Ignore($"Test project not found: {projectPath}");
            return;
        }

        // Act - Use default output path
        var loader = new CsprojProjectLoader(projectPath);
        var actualOutputPath = await loader.CompileAndGenerateOutputAsync(null, OutputFormat.DacPac);

        // Assert
        actualOutputPath.Should().NotBeNullOrEmpty();
        File.Exists(actualOutputPath).Should().BeTrue();
        actualOutputPath.Should().EndWith(".pgpac");
        actualOutputPath.Should().Contain("SampleDatabase");
    }
}
