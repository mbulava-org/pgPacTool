using FluentAssertions;
using mbulava.PostgreSql.Dac.Compile;
using mbulava.PostgreSql.Dac.Models;
using NUnit.Framework;

namespace mbulava.PostgreSql.Dac.Tests.Compile;

/// <summary>
/// Tests for error-path coverage (Area F):
/// 1. Invalid SQL in a .sql file → compile should error (not silently skip)
/// 2. Real circular dependency between two .sql files
/// 3. Undeclared extension dependency behaviour
/// </summary>
[TestFixture]
[Category("Unit")]
[Category("ErrorPath")]
public class ErrorPathTests
{
    private string _tempDir = null!;

    [SetUp]
    public void Setup()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "pgPacTool_ErrorPathTests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempDir);
    }

    [TearDown]
    public void Teardown()
    {
        if (Directory.Exists(_tempDir))
        {
            try { Directory.Delete(_tempDir, true); } catch { /* ignore cleanup failures */ }
        }
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Gap 1: Invalid SQL in a .sql file → compile should return a parse error
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// When a .sql file in the project contains unparseable SQL, the compiler
    /// should surface a parse error rather than silently ignoring the file.
    ///
    /// Current behaviour: CsprojProjectLoader.ParseAndClassifySqlFileAsync returns
    /// null for unparseable files and skips them with a Console.WriteLine warning.
    /// The resulting PgProject is missing those objects entirely, so downstream
    /// compilation succeeds when it should not.
    ///
    /// This test documents the expected behaviour.  It will FAIL until
    /// CsprojProjectLoader is fixed to propagate parse errors into a
    /// CompilerResult / exception rather than silently dropping files.
    /// </summary>
    [Test]
    public async Task Compile_SqlFileWithInvalidSyntax_ReturnsParseError()
    {
        // Arrange – write a minimal .csproj and an invalid .sql file
        var projectDir = Path.Combine(_tempDir, "InvalidSqlProject");
        var tablesDir = Path.Combine(projectDir, "Tables");
        Directory.CreateDirectory(tablesDir);

        await File.WriteAllTextAsync(
            Path.Combine(projectDir, "InvalidSqlProject.csproj"),
            """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFramework>net10.0</TargetFramework>
                <DatabaseName>InvalidSqlProject</DatabaseName>
                <PostgresVersion>16</PostgresVersion>
              </PropertyGroup>
            </Project>
            """);

        // A syntactically broken SQL file
        await File.WriteAllTextAsync(
            Path.Combine(tablesDir, "bad_table.sql"),
            "CREATE TABLE @@@INVALID SYNTAX HERE%%%;");

        var loader = new CsprojProjectLoader(Path.Combine(projectDir, "InvalidSqlProject.csproj"));

        // Act
        // Expected: loader surfaces a parse error (exception or non-empty Errors list on result).
        // If CsprojProjectLoader is fixed to throw on parse failure, adjust the assertion below.
        Func<Task> act = async () =>
        {
            var project = await loader.LoadProjectAsync();

            // If no exception was thrown, the project should be empty (object was silently dropped).
            // That means the file parse error was NOT surfaced – this assertion documents the gap.
            project.Schemas.SelectMany(s => s.Tables).Should().BeEmpty(
                because: "a project with only an invalid .sql file should have no tables, " +
                         "AND a parse error should have been reported to the caller");
        };

        // Assert – currently the loader swallows the error; the test captures the expectation
        // that an InvalidOperationException (or equivalent) is thrown:
        await act.Should().ThrowAsync<InvalidOperationException>(
            because: "CsprojProjectLoader should not silently discard files with invalid SQL; " +
                     "it must propagate a parse error so callers know compilation is incomplete");
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Gap 2: Two real .sql files that reference each other → circular dep error
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Two real .sql view files that SELECT from each other create a circular
    /// dependency.  The compiler should detect the cycle and return an error –
    /// not silently produce a broken deployment order.
    /// </summary>
    [Test]
    public async Task Compile_TwoSqlFilesWithCircularViewDependency_ReturnsCircularDepError()
    {
        // Arrange – write a minimal project where view_a references view_b and vice-versa
        var projectDir = Path.Combine(_tempDir, "CircularViewProject");
        var viewsDir = Path.Combine(projectDir, "Views");
        Directory.CreateDirectory(viewsDir);

        await File.WriteAllTextAsync(
            Path.Combine(projectDir, "CircularViewProject.csproj"),
            """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFramework>net10.0</TargetFramework>
                <DatabaseName>CircularViewProject</DatabaseName>
                <PostgresVersion>16</PostgresVersion>
              </PropertyGroup>
            </Project>
            """);

        // view_a depends on view_b
        await File.WriteAllTextAsync(
            Path.Combine(viewsDir, "view_a.sql"),
            "CREATE VIEW view_a AS SELECT * FROM view_b;");

        // view_b depends on view_a  (creates the cycle)
        await File.WriteAllTextAsync(
            Path.Combine(viewsDir, "view_b.sql"),
            "CREATE VIEW view_b AS SELECT * FROM view_a;");

        var loader = new CsprojProjectLoader(Path.Combine(projectDir, "CircularViewProject.csproj"));
        var project = await loader.LoadProjectAsync();

        // Act – compile the loaded project
        var compiler = new ProjectCompiler();
        var result = compiler.Compile(project);

        // Assert
        result.IsSuccess.Should().BeFalse(
            because: "two views that reference each other create a circular dependency");

        result.HasCircularDependencies.Should().BeTrue(
            because: "a VIEW → VIEW cycle must be detected");

        result.Errors.Should().NotBeEmpty(
            because: "a CYCLE error must be reported");

        result.Errors.Should().Contain(e => e.Code.StartsWith("CYCLE"),
            because: "the error code should start with CYCLE per the existing convention");
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Gap 3: Undeclared extension dependency (e.g. CREATE EXTENSION postgis)
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Documents the current behaviour when a .sql file uses an extension
    /// (CREATE EXTENSION …) that was not declared in the project.
    ///
    /// Current behaviour: Extensions are not modelled in SqlObjectType, PgProject,
    /// or the dependency graph at all.  A .sql file containing CREATE EXTENSION is
    /// classified as an unknown object and silently dropped by the loader, meaning
    /// the extension is never included in the deployment package and no error is
    /// reported.
    ///
    /// Expected behaviour (future fix): The compiler should either
    ///   (a) detect that an object depends on an extension that is not declared
    ///       and emit a warning/error, or
    ///   (b) at minimum include CREATE EXTENSION statements in the deployment order.
    ///
    /// This test locks down the CURRENT (incorrect) behaviour so any accidental
    /// regression is visible, and explicitly describes what should change.
    /// </summary>
    [Test]
    public async Task Compile_SqlFileWithUndeclaredExtension_CurrentlyDropsExtensionSilently()
    {
        // Arrange – project with a view that uses a postgis function (depends on the
        // postgis extension), and a CREATE EXTENSION postgis file.
        var projectDir = Path.Combine(_tempDir, "ExtensionProject");
        var extDir = Path.Combine(projectDir, "Extensions");
        var viewsDir = Path.Combine(projectDir, "Views");
        Directory.CreateDirectory(extDir);
        Directory.CreateDirectory(viewsDir);

        await File.WriteAllTextAsync(
            Path.Combine(projectDir, "ExtensionProject.csproj"),
            """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFramework>net10.0</TargetFramework>
                <DatabaseName>ExtensionProject</DatabaseName>
                <PostgresVersion>16</PostgresVersion>
              </PropertyGroup>
            </Project>
            """);

        // Extension declaration
        await File.WriteAllTextAsync(
            Path.Combine(extDir, "postgis.sql"),
            "CREATE EXTENSION IF NOT EXISTS postgis;");

        // A standalone view that does NOT reference any other objects,
        // so compilation succeeds independently of whether the extension is present.
        await File.WriteAllTextAsync(
            Path.Combine(viewsDir, "spatial_summary.sql"),
            "CREATE VIEW spatial_summary AS SELECT 1 AS id;");

        var loader = new CsprojProjectLoader(Path.Combine(projectDir, "ExtensionProject.csproj"));
        var project = await loader.LoadProjectAsync();

        // Assert current behaviour: extension is not modelled, so it is silently dropped.
        // The deployment package will NOT contain a CREATE EXTENSION statement.
        //
        // This assertion documents the known gap. Change it when extension support is added.
        var schemaObjectNames = project.Schemas.SelectMany(s =>
            s.Tables.Select(t => t.Name)
            .Concat(s.Views.Select(v => v.Name))
            .Concat(s.Functions.Select(f => f.Name)));

        schemaObjectNames.Should().NotContain(
            n => n.Contains("postgis", StringComparison.OrdinalIgnoreCase),
            because: "KNOWN GAP: CREATE EXTENSION statements are not yet modelled; " +
                     "extensions are silently dropped from the compiled project. " +
                     "Fix: add Extension to SqlObjectType and PgProject, then update " +
                     "ProjectCompiler to validate that declared extensions are present.");

        // Compile should succeed (extension absence is not caught today)
        var compiler = new ProjectCompiler();
        var result = compiler.Compile(project);

        result.IsSuccess.Should().BeTrue(
            because: "KNOWN GAP: missing extension dependency is not currently detected; " +
                     "compilation succeeds even though a required extension is absent. " +
                     "Fix: add extension dependency validation to ProjectCompiler so this " +
                     "returns IsSuccess=false with an appropriate error code.");
    }
}
