using FluentAssertions;
using mbulava.PostgreSql.Dac.Compile;

namespace mbulava.PostgreSql.Dac.Tests.Integration;

/// <summary>
/// Tests for convention-based SQL file discovery in .csproj projects.
/// Covers edge cases: missing directories, unexpected folder layouts, flat structures.
/// </summary>
[TestFixture]
public class ConventionDiscoveryTests
{
    private string _tempDir = null!;

    [SetUp]
    public void Setup()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "pgPacTool_ConventionTests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempDir);
    }

    [TearDown]
    public void Teardown()
    {
        if (Directory.Exists(_tempDir))
        {
            try { Directory.Delete(_tempDir, true); } catch { /* ignore */ }
        }
    }

    private string CreateMinimalCsproj(string projectDir, string? databaseName = null)
    {
        var name = databaseName ?? Path.GetFileName(projectDir);
        var csprojPath = Path.Combine(projectDir, $"{name}.csproj");
        var dbNameElement = databaseName != null ? $"\n    <DatabaseName>{databaseName}</DatabaseName>" : string.Empty;
        File.WriteAllText(csprojPath, $"""
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFramework>net10.0</TargetFramework>{dbNameElement}
              </PropertyGroup>
            </Project>
            """);
        return csprojPath;
    }

    [Test]
    public async Task Project_WithNoSqlFiles_LoadsEmptyProject()
    {
        // Arrange — project directory exists but has no .sql files at all
        var projectDir = Path.Combine(_tempDir, "EmptyProject");
        Directory.CreateDirectory(projectDir);
        var csprojPath = CreateMinimalCsproj(projectDir, "EmptyProject");

        // Act
        var loader = new CsprojProjectLoader(csprojPath);
        var project = await loader.LoadProjectAsync();

        // Assert — should load without throwing, with no schemas (or empty schemas)
        project.Should().NotBeNull();
        project.DatabaseName.Should().Be("EmptyProject");
        // No SQL = nothing to group into schemas
        project.Schemas.Should().BeEmpty("no SQL files means no schema groups");
    }

    [Test]
    public async Task Project_WithFlatSqlLayout_DiscoversAllFiles()
    {
        // Arrange — all .sql files in the project root (no subdirectories)
        var projectDir = Path.Combine(_tempDir, "FlatProject");
        Directory.CreateDirectory(projectDir);
        var csprojPath = CreateMinimalCsproj(projectDir, "FlatProject");

        File.WriteAllText(Path.Combine(projectDir, "users.sql"),
            "CREATE TABLE users (id SERIAL PRIMARY KEY, name VARCHAR(100) NOT NULL);");
        File.WriteAllText(Path.Combine(projectDir, "products.sql"),
            "CREATE TABLE products (id SERIAL PRIMARY KEY, title VARCHAR(200) NOT NULL);");

        // Act
        var loader = new CsprojProjectLoader(csprojPath);
        var project = await loader.LoadProjectAsync();

        // Assert
        project.Should().NotBeNull();
        var schema = project.Schemas.FirstOrDefault(s => s.Name == "public");
        schema.Should().NotBeNull("flat SQL files default to public schema");
        schema!.Tables.Should().HaveCount(2);
        schema.Tables.Select(t => t.Name).Should().BeEquivalentTo(new[] { "users", "products" });
    }

    [Test]
    public async Task Project_WithUnexpectedSubfolderLayout_StillDiscoversAllSqlFiles()
    {
        // Arrange — SQL files in non-standard subdirectories (not Tables/, Views/ etc.)
        var projectDir = Path.Combine(_tempDir, "WeirdLayout");
        Directory.CreateDirectory(projectDir);
        var csprojPath = CreateMinimalCsproj(projectDir, "WeirdLayout");

        var customDir = Path.Combine(projectDir, "db_objects", "core");
        Directory.CreateDirectory(customDir);
        File.WriteAllText(Path.Combine(customDir, "accounts.sql"),
            "CREATE TABLE accounts (id SERIAL PRIMARY KEY, owner VARCHAR(100) NOT NULL);");

        var anotherDir = Path.Combine(projectDir, "more_stuff");
        Directory.CreateDirectory(anotherDir);
        File.WriteAllText(Path.Combine(anotherDir, "account_view.sql"),
            "CREATE OR REPLACE VIEW account_summary AS SELECT id, owner FROM accounts;");

        // Act
        var loader = new CsprojProjectLoader(csprojPath);
        var project = await loader.LoadProjectAsync();

        // Assert — convention discovers all .sql recursively regardless of folder name
        project.Should().NotBeNull();
        var schema = project.Schemas.FirstOrDefault(s => s.Name == "public");
        schema.Should().NotBeNull("objects without schema qualifier default to public");
        schema!.Tables.Should().ContainSingle(t => t.Name == "accounts");
        schema.Views.Should().ContainSingle(v => v.Name == "account_summary");
    }

    [Test]
    public async Task Project_WithBinObjDirectories_ExcludesBuildArtifacts()
    {
        // Arrange — stale .sql files inside bin/ or obj/ should be ignored
        var projectDir = Path.Combine(_tempDir, "ArtifactProject");
        Directory.CreateDirectory(projectDir);
        var csprojPath = CreateMinimalCsproj(projectDir, "ArtifactProject");

        // Legitimate file
        File.WriteAllText(Path.Combine(projectDir, "real_table.sql"),
            "CREATE TABLE real_table (id SERIAL PRIMARY KEY);");

        // Stale artifact files that should be excluded
        var binDir = Path.Combine(projectDir, "bin", "Debug", "net10.0");
        Directory.CreateDirectory(binDir);
        File.WriteAllText(Path.Combine(binDir, "stale_copy.sql"),
            "CREATE TABLE stale_table (id SERIAL PRIMARY KEY);");

        var objDir = Path.Combine(projectDir, "obj");
        Directory.CreateDirectory(objDir);
        File.WriteAllText(Path.Combine(objDir, "generated.sql"),
            "CREATE TABLE generated_table (id SERIAL PRIMARY KEY);");

        // Act
        var loader = new CsprojProjectLoader(csprojPath);
        var project = await loader.LoadProjectAsync();

        // Assert — only real_table, not stale or generated
        project.Should().NotBeNull();
        var allTables = project.Schemas.SelectMany(s => s.Tables).ToList();
        allTables.Should().ContainSingle(t => t.Name == "real_table");
        allTables.Select(t => t.Name).Should().NotContain("stale_table");
        allTables.Select(t => t.Name).Should().NotContain("generated_table");
    }

    [Test]
    public async Task Project_WithMixedValidAndInvalidSql_LoadsValidObjects()
    {
        // Arrange — one valid and one unparseable SQL file
        var projectDir = Path.Combine(_tempDir, "MixedProject");
        Directory.CreateDirectory(projectDir);
        var csprojPath = CreateMinimalCsproj(projectDir, "MixedProject");

        File.WriteAllText(Path.Combine(projectDir, "good_table.sql"),
            "CREATE TABLE good_table (id SERIAL PRIMARY KEY, value TEXT);");

        // A SQL file with unrecognized/unsupported syntax — should be skipped with a warning, not throw
        File.WriteAllText(Path.Combine(projectDir, "unknown_stmt.sql"),
            "-- This file intentionally has no CREATE statements\nSELECT 1;");

        // Act — must not throw
        var loader = new CsprojProjectLoader(csprojPath);
        Func<Task> act = async () => await loader.LoadProjectAsync();
        await act.Should().NotThrowAsync("loader should warn and skip, not throw on unrecognized SQL");

        var project = await loader.LoadProjectAsync();
        project.Should().NotBeNull();
        var schema = project.Schemas.FirstOrDefault(s => s.Name == "public");
        schema.Should().NotBeNull();
        schema!.Tables.Should().ContainSingle(t => t.Name == "good_table");
    }
}
