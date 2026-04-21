using FluentAssertions;
using mbulava.PostgreSql.Dac.Compile;

namespace mbulava.PostgreSql.Dac.Tests.Compile;

[TestFixture]
public class CompileObjectTreeFormatterTests
{
    private string _outputDir = string.Empty;

    [SetUp]
    public void SetUp()
    {
        _outputDir = Path.Combine(Path.GetTempPath(), $"pgPacTool_FormatterTests_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_outputDir);
    }

    [Test]
    public async Task Format_IncludesObjectTypesAndSourceLocations()
    {
        var projectPath = Path.Combine(_outputDir, "FormatterProject.csproj");
        await File.WriteAllTextAsync(projectPath,
            """
            <Project Sdk="Microsoft.NET.Sdk">
              <Sdk Name="MSBuild.Sdk.PostgreSql" Version="1.0.0-preview7" />
              <PropertyGroup>
                <TargetFramework>net10.0</TargetFramework>
                <PostgresVersion>17</PostgresVersion>
              </PropertyGroup>
            </Project>
            """);

        Directory.CreateDirectory(Path.Combine(_outputDir, "Sequences"));
        Directory.CreateDirectory(Path.Combine(_outputDir, "Tables"));
        Directory.CreateDirectory(Path.Combine(_outputDir, "Functions"));
        Directory.CreateDirectory(Path.Combine(_outputDir, "Triggers"));

        await File.WriteAllTextAsync(Path.Combine(_outputDir, "Sequences", "users_seq.sql"), "CREATE SEQUENCE core.users_seq START 1;");
        await File.WriteAllTextAsync(Path.Combine(_outputDir, "Tables", "users.sql"), "CREATE TABLE core.users (id integer PRIMARY KEY);");
        await File.WriteAllTextAsync(Path.Combine(_outputDir, "Functions", "refresh_users.sql"), "CREATE FUNCTION core.refresh_users() RETURNS void LANGUAGE plpgsql AS $$ BEGIN END $$;");
        await File.WriteAllTextAsync(Path.Combine(_outputDir, "Triggers", "users_audit.sql"), "CREATE TRIGGER users_audit BEFORE INSERT ON core.users FOR EACH ROW EXECUTE FUNCTION core.refresh_users();");

        var loader = new CsprojProjectLoader(projectPath);
        var project = await loader.LoadProjectAsync();

        var output = CompileObjectTreeFormatter.Format(project);

        output.Should().Contain("Schema: core");
        output.Should().Contain($"Sequence: core.users_seq [Sequences{Path.DirectorySeparatorChar}users_seq.sql]");
        output.Should().Contain($"Table: core.users [Tables{Path.DirectorySeparatorChar}users.sql]");
        output.Should().Contain($"Function: core.refresh_users [Functions{Path.DirectorySeparatorChar}refresh_users.sql]");
        output.Should().Contain($"Trigger: core.users.users_audit [Triggers{Path.DirectorySeparatorChar}users_audit.sql]");
    }
}
