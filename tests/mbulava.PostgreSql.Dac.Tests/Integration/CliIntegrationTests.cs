using FluentAssertions;
using mbulava.PostgreSql.Dac.Compile;
using mbulava.PostgreSql.Dac.Models;
using System.Diagnostics;
using System.IO.Compression;

namespace mbulava.PostgreSql.Dac.Tests.Integration;

/// <summary>
/// End-to-end CLI integration tests.
/// Tests the full workflow through the CLI executable.
/// </summary>
[TestFixture]
[Category("CLI")]
public class CliIntegrationTests
{
    private string _testProjectsDir = null!;
    private string _outputDir = null!;
    private string _cliPath = null!;

    [SetUp]
    public void Setup()
    {
        // Get test projects directory
        var currentDir = TestContext.CurrentContext.TestDirectory;
        _testProjectsDir = Path.Combine(currentDir, "..", "..", "..", "..", "TestProjects");
        _testProjectsDir = Path.GetFullPath(_testProjectsDir);

        // Get CLI executable path
        _cliPath = Path.Combine(currentDir, "..", "..", "..", "..", "..", "src", "postgresPacTools", "bin", "Debug", "net10.0", "postgresPacTools.dll");
        _cliPath = Path.GetFullPath(_cliPath);

        // Create output directory
        _outputDir = Path.Combine(Path.GetTempPath(), "pgPacTool_CLI_Tests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(_outputDir);
    }

    [TearDown]
    public void Teardown()
    {
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

    private (int exitCode, string output, string error) RunCli(string arguments)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"\"{_cliPath}\" {arguments}",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = Process.Start(startInfo);
        if (process == null)
        {
            throw new InvalidOperationException("Failed to start CLI process");
        }

        var output = process.StandardOutput.ReadToEnd();
        var error = process.StandardError.ReadToEnd();
        process.WaitForExit();

        return (process.ExitCode, output, error);
    }

    [Test]
    public void Compile_SampleDatabase_Succeeds()
    {
        // Arrange
        var projectPath = Path.Combine(_testProjectsDir, "SampleDatabase", "SampleDatabase.csproj");
        var outputPath = Path.Combine(_outputDir, "SampleDatabase.pgpac");
        
        if (!File.Exists(projectPath) || !File.Exists(_cliPath))
        {
            Assert.Ignore("Test project or CLI not found");
            return;
        }

        // Act
        var (exitCode, output, error) = RunCli($"compile -sf \"{projectPath}\" -o \"{outputPath}\"");

        // Assert
        exitCode.Should().Be(0, $"CLI should exit successfully. Error: {error}");
        output.Should().Contain("Compilation successful");
        File.Exists(outputPath).Should().BeTrue();
    }

    [Test]
    public void Compile_SampleDatabase_VerboseMode_ShowsDetails()
    {
        // Arrange
        var projectPath = Path.Combine(_testProjectsDir, "SampleDatabase", "SampleDatabase.csproj");
        var outputPath = Path.Combine(_outputDir, "SampleDatabase_Verbose.pgpac");
        
        if (!File.Exists(projectPath) || !File.Exists(_cliPath))
        {
            Assert.Ignore("Test project or CLI not found");
            return;
        }

        // Act
        var (exitCode, output, error) = RunCli($"compile -sf \"{projectPath}\" -o \"{outputPath}\" --verbose");

        // Assert
        exitCode.Should().Be(0);
        output.Should().Contain("Compilation successful");
        output.Should().Contain("Deployment order", "verbose mode should show deployment order");
        output.Should().Contain("Objects:");
        output.Should().Contain("Levels:");
    }

    [Test]
    public void Compile_SampleDatabase_JsonFormat_CreatesJsonFile()
    {
        // Arrange
        var projectPath = Path.Combine(_testProjectsDir, "SampleDatabase", "SampleDatabase.csproj");
        var outputPath = Path.Combine(_outputDir, "SampleDatabase.pgproj.json");
        
        if (!File.Exists(projectPath) || !File.Exists(_cliPath))
        {
            Assert.Ignore("Test project or CLI not found");
            return;
        }

        // Act
        var (exitCode, output, error) = RunCli($"compile -sf \"{projectPath}\" -o \"{outputPath}\" --output-format json");

        // Assert
        exitCode.Should().Be(0);
        File.Exists(outputPath).Should().BeTrue();
        outputPath.Should().EndWith(".pgproj.json");
        
        // Verify it's valid JSON
        var jsonContent = File.ReadAllText(outputPath);
        jsonContent.Should().Contain("\"DatabaseName\"");
        jsonContent.Should().Contain("\"Schemas\"");
    }

    [Test]
    public void Compile_InvalidProject_ReturnsError()
    {
        // Arrange
        var invalidPath = Path.Combine(_outputDir, "NonExistent.csproj");
        
        if (!File.Exists(_cliPath))
        {
            Assert.Ignore("CLI not found");
            return;
        }

        // Act
        var (exitCode, output, error) = RunCli($"compile -sf \"{invalidPath}\"");

        // Assert
        exitCode.Should().NotBe(0, "should fail for invalid project");
        output.Should().Contain("Error", "should show error message");
    }

    [Test]
    public void Compile_Help_ShowsUsage()
    {
        // Arrange
        if (!File.Exists(_cliPath))
        {
            Assert.Ignore("CLI not found");
            return;
        }

        // Act
        var (exitCode, output, error) = RunCli("compile --help");

        // Assert
        exitCode.Should().Be(0);
        output.Should().Contain("Usage:");
        output.Should().Contain("--source-file");
        output.Should().Contain("--output-path");
        output.Should().Contain("--output-format");
        output.Should().Contain("pgpac");
        output.Should().Contain("json");
    }

    [Test]
    public void RootCommand_ShowsAvailableCommands()
    {
        // Arrange
        if (!File.Exists(_cliPath))
        {
            Assert.Ignore("CLI not found");
            return;
        }

        // Act
        var (exitCode, output, error) = RunCli("--help");

        // Assert
        exitCode.Should().Be(0);
        output.Should().Contain("extract");
        output.Should().Contain("publish");
        output.Should().Contain("script");
        output.Should().Contain("compile");
        output.Should().Contain("deploy-report");
    }

    [Test]
    public async Task Compile_PgpacFile_CanBeExtractedAndValidated()
    {
        // Arrange
        var projectPath = Path.Combine(_testProjectsDir, "SampleDatabase", "SampleDatabase.csproj");
        var pgpacPath = Path.Combine(_outputDir, "SampleDatabase_Extract.pgpac");

        if (!File.Exists(projectPath) || !File.Exists(_cliPath))
        {
            Assert.Ignore("Test project or CLI not found");
            return;
        }

        // Act - Compile
        var (compileExitCode, compileOutput, compileError) = RunCli($"compile -sf \"{projectPath}\" -o \"{pgpacPath}\"");

        // Assert - Compilation
        compileExitCode.Should().Be(0);
        File.Exists(pgpacPath).Should().BeTrue();

        // Assert - Can extract content from pgpac
        using var archive = ZipFile.OpenRead(pgpacPath);
        var contentEntry = archive.GetEntry("content.json");
        contentEntry.Should().NotBeNull("pgpac should contain content.json");

        // Assert - Content is valid
        await using var stream = contentEntry!.Open();
        var action = async () => await PgProject.Load(stream);
        await action.Should().NotThrowAsync("content.json should be valid PgProject");
    }

    [Test]
    public async Task Compile_RoundTrip_PreservesAllData()
    {
        // Arrange
        var projectPath = Path.Combine(_testProjectsDir, "SampleDatabase", "SampleDatabase.csproj");
        var pgpacPath = Path.Combine(_outputDir, "SampleDatabase_RoundTrip.pgpac");
        
        if (!File.Exists(projectPath) || !File.Exists(_cliPath))
        {
            Assert.Ignore("Test project or CLI not found");
            return;
        }

        // Act - Load original
        var loader = new CsprojProjectLoader(projectPath);
        var originalProject = await loader.LoadProjectAsync();

        // Act - Compile to pgpac via CLI
        var (exitCode, output, error) = RunCli($"compile -sf \"{projectPath}\" -o \"{pgpacPath}\"");
        exitCode.Should().Be(0);

        // Act - Load from pgpac
        using var archive = ZipFile.OpenRead(pgpacPath);
        var contentEntry = archive.GetEntry("content.json");
        await using var stream = contentEntry!.Open();
        var roundTrippedProject = await PgProject.Load(stream);

        // Assert - Data preserved
        roundTrippedProject.DatabaseName.Should().Be(originalProject.DatabaseName);
        roundTrippedProject.Schemas.Count.Should().Be(originalProject.Schemas.Count);

        var originalSchema = originalProject.Schemas[0];
        var roundTrippedSchema = roundTrippedProject.Schemas[0];

        roundTrippedSchema.Tables.Count.Should().Be(originalSchema.Tables.Count);
        roundTrippedSchema.Views.Count.Should().Be(originalSchema.Views.Count);
        roundTrippedSchema.Functions.Count.Should().Be(originalSchema.Functions.Count);
        roundTrippedSchema.Types.Count.Should().Be(originalSchema.Types.Count);
        roundTrippedSchema.Sequences.Count.Should().Be(originalSchema.Sequences.Count);
        roundTrippedSchema.Triggers.Count.Should().Be(originalSchema.Triggers.Count);

        // Assert - Names preserved
        originalSchema.Tables.Select(t => t.Name).Should().BeEquivalentTo(
            roundTrippedSchema.Tables.Select(t => t.Name));
    }
}
