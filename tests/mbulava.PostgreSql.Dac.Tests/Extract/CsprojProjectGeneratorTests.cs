using FluentAssertions;
using mbulava.PostgreSql.Dac.Extract;
using mbulava.PostgreSql.Dac.Models;
using System.Xml.Linq;

namespace mbulava.PostgreSql.Dac.Tests.Extract;

[TestFixture]
public class CsprojProjectGeneratorTests
{
    private string _tempDirectory = null!;

    [SetUp]
    public void SetUp()
    {
        _tempDirectory = Path.Combine(Path.GetTempPath(), "pgPacTool_Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDirectory);
    }

    [TearDown]
    public void TearDown()
    {
        if (!Directory.Exists(_tempDirectory))
        {
            return;
        }

        try
        {
            Directory.Delete(_tempDirectory, true);
        }
        catch
        {
        }
    }

    [Test]
    public async Task GenerateProjectAsync_WhenPostgresVersionMissing_WritesDefaultMajorVersion()
    {
        var projectPath = Path.Combine(_tempDirectory, "Generated", "Generated.csproj");
        var generator = new CsprojProjectGenerator(projectPath);
        var project = CreateProject(postgresVersion: string.Empty);

        await generator.GenerateProjectAsync(project);

        var document = XDocument.Load(projectPath);
        var postgresVersion = document.Descendants().Single(element => element.Name.LocalName == "PostgresVersion").Value;

        postgresVersion.Should().Be("16");
    }

    [Test]
    public async Task GenerateProjectAsync_WhenDefaultSchemaMissing_WritesPublic()
    {
        var projectPath = Path.Combine(_tempDirectory, "GeneratedSchema", "GeneratedSchema.csproj");
        var generator = new CsprojProjectGenerator(projectPath);
        var project = CreateProject(postgresVersion: "16", defaultSchema: string.Empty);

        await generator.GenerateProjectAsync(project);

        var document = XDocument.Load(projectPath);
        var defaultSchema = document.Descendants().Single(element => element.Name.LocalName == "DefaultSchema").Value;

        defaultSchema.Should().Be("public");
    }

    [Test]
    public async Task GenerateProjectAsync_WhenPostgresVersionIncludesMinor_WritesMajorVersionOnly()
    {
        var projectPath = Path.Combine(_tempDirectory, "GeneratedMinor", "GeneratedMinor.csproj");
        var generator = new CsprojProjectGenerator(projectPath);
        var project = CreateProject(postgresVersion: "17.4");

        await generator.GenerateProjectAsync(project);

        var document = XDocument.Load(projectPath);
        var postgresVersion = document.Descendants().Single(element => element.Name.LocalName == "PostgresVersion").Value;

        postgresVersion.Should().Be("17");
    }

    private static PgProject CreateProject(string postgresVersion, string defaultSchema = "public")
    {
        return new PgProject
        {
            DatabaseName = "Generated",
            PostgresVersion = postgresVersion,
            DefaultSchema = defaultSchema,
            Schemas =
            [
                new PgSchema
                {
                    Name = "public",
                    Owner = "postgres"
                }
            ]
        };
    }
}
