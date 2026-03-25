using FluentAssertions;
using mbulava.PostgreSql.Dac.Compile;

namespace mbulava.PostgreSql.Dac.Tests.Compile;

[TestFixture]
public class CsprojProjectLoaderTests
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
    public async Task LoadProjectAsync_WithoutPostgresVersion_ThrowsInvalidOperationException()
    {
        var projectPath = await CreateProjectAsync(postgresVersion: null, sql: "CREATE TABLE public.users (id integer);", projectName: "MissingVersion");
        var loader = new CsprojProjectLoader(projectPath);

        Func<Task> act = () => loader.LoadProjectAsync();

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*PostgresVersion*")
            .WithMessage("*project file*");
    }

    [Test]
    public async Task LoadProjectAsync_UsesProjectPostgresVersionToSelectParser()
    {
        const string pg17OnlySql = """
            CREATE VIEW public.json_table_view AS
            SELECT *
            FROM JSON_TABLE('[{"id":1}]', '$[*]' COLUMNS(id int PATH '$.id'));
            """;

        var postgres16ProjectPath = await CreateProjectAsync("16", pg17OnlySql, "Postgres16Project");
        var postgres17ProjectPath = await CreateProjectAsync("17", pg17OnlySql, "Postgres17Project");

        var postgres16Loader = new CsprojProjectLoader(postgres16ProjectPath);
        var postgres17Loader = new CsprojProjectLoader(postgres17ProjectPath);

        var postgres16Project = await postgres16Loader.LoadProjectAsync();
        var postgres17Project = await postgres17Loader.LoadProjectAsync();

        postgres16Project.Schemas.Should().BeEmpty();
        postgres17Project.Schemas.Should().ContainSingle();
        postgres17Project.Schemas[0].Views.Should().ContainSingle(view => view.Name == "json_table_view");
    }

    [Test]
    public async Task LoadProjectAsync_UsesDefaultSchemaForUnqualifiedObjects()
    {
        var projectPath = await CreateProjectAsync(
            postgresVersion: "16",
            sql: "CREATE TABLE users (id integer);",
            projectName: "ReportingProject",
            defaultSchema: "reporting");

        var loader = new CsprojProjectLoader(projectPath);

        var project = await loader.LoadProjectAsync();

        project.DefaultSchema.Should().Be("reporting");
        project.Schemas.Should().ContainSingle();
        project.Schemas[0].Name.Should().Be("reporting");
        project.Schemas[0].Tables.Should().ContainSingle(table => table.Name == "users");
    }

    private async Task<string> CreateProjectAsync(string? postgresVersion, string sql, string projectName, string? defaultSchema = null)
    {
        var projectDirectory = Path.Combine(_tempDirectory, projectName);
        var schemaDirectory = Path.Combine(projectDirectory, "public", "Views");
        Directory.CreateDirectory(schemaDirectory);

        var projectPath = Path.Combine(projectDirectory, $"{projectName}.csproj");
        var sqlPath = Path.Combine(schemaDirectory, "json_table_view.sql");

        var postgresVersionProperty = postgresVersion is null
            ? string.Empty
            : $"    <PostgresVersion>{postgresVersion}</PostgresVersion>{Environment.NewLine}";
        var defaultSchemaProperty = defaultSchema is null
            ? string.Empty
            : $"    <DefaultSchema>{defaultSchema}</DefaultSchema>{Environment.NewLine}";

        var projectXml = $"""
            <Project Sdk="MSBuild.Sdk.PostgreSql/1.0.0">
              <PropertyGroup>
                <TargetFramework>net10.0</TargetFramework>
                <DatabaseName>{projectName}</DatabaseName>
            {postgresVersionProperty}{defaultSchemaProperty}  </PropertyGroup>
            </Project>
            """;

        await File.WriteAllTextAsync(projectPath, projectXml);
        await File.WriteAllTextAsync(sqlPath, sql);

        return projectPath;
    }
}
