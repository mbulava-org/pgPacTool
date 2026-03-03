using FluentAssertions;
using mbulava.PostgreSql.Dac.Models;
using NUnit.Framework;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace mbulava.PostgreSql.Dac.Tests.Models;

/// <summary>
/// Tests for PgProject model and serialization
/// </summary>
[TestFixture]
[Category("Models")]
public class PgProjectTests
{
    [Test]
    public void PgProject_DefaultValues_AreSetCorrectly()
    {
        // Arrange & Act
        var project = new PgProject();

        // Assert
        project.DatabaseName.Should().BeEmpty();
        project.PostgresVersion.Should().BeEmpty();
        project.SourceConnection.Should().BeEmpty();
        project.DefaultOwner.Should().Be("postgres");
        project.DefaultTablespace.Should().Be("pg_default");
        project.Schemas.Should().NotBeNull().And.BeEmpty();
        project.Roles.Should().NotBeNull().And.BeEmpty();
    }

    [Test]
    public void PgProject_Properties_CanBeSet()
    {
        // Arrange
        var project = new PgProject();

        // Act
        project.DatabaseName = "testdb";
        project.PostgresVersion = "16.1";
        project.SourceConnection = "Host=localhost;Database=testdb";
        project.DefaultOwner = "custom_owner";
        project.DefaultTablespace = "custom_tablespace";

        // Assert
        project.DatabaseName.Should().Be("testdb");
        project.PostgresVersion.Should().Be("16.1");
        project.SourceConnection.Should().Be("Host=localhost;Database=testdb");
        project.DefaultOwner.Should().Be("custom_owner");
        project.DefaultTablespace.Should().Be("custom_tablespace");
    }

    [Test]
    public async Task PgProject_Save_SerializesToJson()
    {
        // Arrange
        var project = new PgProject
        {
            DatabaseName = "testdb",
            PostgresVersion = "16.1",
            Schemas = new()
            {
                new PgSchema { Name = "public", Owner = "postgres" }
            }
        };

        using var stream = new MemoryStream();

        // Act
        await PgProject.Save(project, stream);

        // Assert
        stream.Position = 0;
        using var reader = new StreamReader(stream);
        var json = await reader.ReadToEndAsync();
        
        json.Should().Contain("\"DatabaseName\": \"testdb\"");
        json.Should().Contain("\"PostgresVersion\": \"16.1\"");
        json.Should().Contain("\"Schemas\"");
    }

    [Test]
    public async Task PgProject_Load_DeserializesFromJson()
    {
        // Arrange
        var json = """
        {
            "DatabaseName": "testdb",
            "PostgresVersion": "16.1",
            "DefaultOwner": "postgres",
            "Schemas": [
                {
                    "Name": "public",
                    "Owner": "postgres"
                }
            ]
        }
        """;

        using var stream = new MemoryStream();
        using var writer = new StreamWriter(stream, leaveOpen: true);
        await writer.WriteAsync(json);
        await writer.FlushAsync();
        stream.Position = 0;

        // Act
        var project = await PgProject.Load(stream);

        // Assert
        project.Should().NotBeNull();
        project.DatabaseName.Should().Be("testdb");
        project.PostgresVersion.Should().Be("16.1");
        project.Schemas.Should().HaveCount(1);
        project.Schemas[0].Name.Should().Be("public");
    }

    [Test]
    public async Task PgProject_SaveAndLoad_RoundTrip()
    {
        // Arrange
        var original = new PgProject
        {
            DatabaseName = "testdb",
            PostgresVersion = "16.1",
            SourceConnection = "Host=localhost",
            DefaultOwner = "testowner",
            DefaultTablespace = "testtablespace",
            Schemas = new()
            {
                new PgSchema 
                { 
                    Name = "public", 
                    Owner = "postgres",
                    Tables = new()
                    {
                        new PgTable { Name = "users" }
                    }
                }
            }
        };

        using var stream = new MemoryStream();

        // Act - Save
        await PgProject.Save(original, stream);
        
        // Act - Load
        stream.Position = 0;
        var loaded = await PgProject.Load(stream);

        // Assert
        loaded.DatabaseName.Should().Be(original.DatabaseName);
        loaded.PostgresVersion.Should().Be(original.PostgresVersion);
        loaded.SourceConnection.Should().Be(original.SourceConnection);
        loaded.DefaultOwner.Should().Be(original.DefaultOwner);
        loaded.DefaultTablespace.Should().Be(original.DefaultTablespace);
        loaded.Schemas.Should().HaveCount(1);
        loaded.Schemas[0].Name.Should().Be("public");
        loaded.Schemas[0].Tables.Should().HaveCount(1);
        loaded.Schemas[0].Tables[0].Name.Should().Be("users");
    }

    [Test]
    public async Task PgProject_Load_EmptyStream_ThrowsJsonException()
    {
        // Arrange
        using var stream = new MemoryStream();

        // Act & Assert
        await FluentActions.Invoking(async () => await PgProject.Load(stream))
            .Should().ThrowAsync<JsonException>();
    }

    [Test]
    public async Task PgProject_Load_InvalidJson_ThrowsJsonException()
    {
        // Arrange
        var invalidJson = "{ invalid json }";
        using var stream = new MemoryStream();
        using var writer = new StreamWriter(stream, leaveOpen: true);
        await writer.WriteAsync(invalidJson);
        await writer.FlushAsync();
        stream.Position = 0;

        // Act & Assert
        await FluentActions.Invoking(async () => await PgProject.Load(stream))
            .Should().ThrowAsync<JsonException>();
    }

    [Test]
    public async Task PgProject_Save_WithNullValues_OmitsFromJson()
    {
        // Arrange
        var project = new PgProject
        {
            DatabaseName = "testdb",
            // Other properties left as default/empty
        };

        using var stream = new MemoryStream();

        // Act
        await PgProject.Save(project, stream);

        // Assert
        stream.Position = 0;
        using var reader = new StreamReader(stream);
        var json = await reader.ReadToEndAsync();
        
        json.Should().Contain("\"DatabaseName\": \"testdb\"");
        // Empty/default values should still appear (not null)
        json.Should().Contain("\"PostgresVersion\"");
    }
}
