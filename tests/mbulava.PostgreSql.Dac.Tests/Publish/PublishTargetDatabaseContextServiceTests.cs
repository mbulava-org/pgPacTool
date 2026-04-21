using FluentAssertions;
using mbulava.PostgreSql.Dac.Models;
using mbulava.PostgreSql.Dac.Publish;

namespace mbulava.PostgreSql.Dac.Tests.Publish;

[TestFixture]
public class PublishTargetDatabaseContextServiceTests
{
    [Test]
    public void Apply_UsesTargetConnectionDatabase_AsEffectiveDatabaseName()
    {
        var project = new PgProject
        {
            DatabaseName = "SourceDb"
        };

        var options = new PublishOptions();
        var service = new PublishTargetDatabaseContextService();

        service.Apply(project, "Host=localhost;Database=TargetDb;Username=test;Password=test", options);

        project.DatabaseName.Should().Be("TargetDb");
        options.SourceDatabase.Should().Be("SourceDb");
        options.TargetDatabase.Should().Be("TargetDb");
        options.Variables.Should().Contain(v => v.Name == "DatabaseName" && v.Value == "TargetDb");
        options.Variables.Should().Contain(v => v.Name == "TargetDatabase" && v.Value == "TargetDb");
        options.Variables.Should().Contain(v => v.Name == "SourceDatabase" && v.Value == "SourceDb");
    }

    [Test]
    public void BuildTargetConnectionString_WithTargetDatabase_RewritesDatabaseName()
    {
        var service = new PublishTargetDatabaseContextService();

        var result = service.BuildTargetConnectionString("Host=localhost;Database=SourceDb;Username=test;Password=test", "TargetDb");

        result.Should().Contain("Database=TargetDb");
        result.Should().NotContain("Database=SourceDb");
    }
}
