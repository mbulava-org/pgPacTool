using FluentAssertions;
using System.CommandLine;
using System.CommandLine.IO;
using System.CommandLine.Parsing;

namespace mbulava.PostgreSql.Dac.Tests.Cli;

/// <summary>
/// Tests for the postgresPacTools CLI interface.
/// These tests verify command parsing, option validation, and basic command structure.
/// </summary>
[TestFixture]
public class CliCommandTests
{
    [Test]
    public void RootCommand_HasCorrectDescription()
    {
        // Arrange & Act
        var rootCommand = CreateRootCommand();

        // Assert
        rootCommand.Description.Should().Contain("PostgreSQL Data-Tier Application Tools");
    }

    [Test]
    public void RootCommand_HasAllExpectedCommands()
    {
        // Arrange & Act
        var rootCommand = CreateRootCommand();

        // Assert
        var commandNames = rootCommand.Subcommands.Select(c => c.Name).ToList();
        commandNames.Should().Contain(new[] { "extract", "publish", "script", "compile", "deploy-report" });
    }

    [Test]
    public void ExtractCommand_HasRequiredOptions()
    {
        // Arrange
        var rootCommand = CreateRootCommand();
        var extractCommand = rootCommand.Subcommands.First(c => c.Name == "extract");

        // Assert
        var optionNames = extractCommand.Options.Select(o => o.Name).ToList();
        optionNames.Should().Contain("source-connection-string");
        optionNames.Should().Contain("target-file");
        optionNames.Should().Contain("database-name");
    }

    [Test]
    public void ExtractCommand_SourceConnectionStringIsRequired()
    {
        // Arrange
        var rootCommand = CreateRootCommand();
        var extractCommand = rootCommand.Subcommands.First(c => c.Name == "extract");
        var sourceOption = extractCommand.Options.First(o => o.Name == "source-connection-string");

        // Assert
        sourceOption.IsRequired.Should().BeTrue();
    }

    [Test]
    public void ExtractCommand_TargetFileIsRequired()
    {
        // Arrange
        var rootCommand = CreateRootCommand();
        var extractCommand = rootCommand.Subcommands.First(c => c.Name == "extract");
        var targetOption = extractCommand.Options.First(o => o.Name == "target-file");

        // Assert
        targetOption.IsRequired.Should().BeTrue();
    }

    [Test]
    public void ExtractCommand_HasShortAliases()
    {
        // Arrange
        var rootCommand = CreateRootCommand();
        var extractCommand = rootCommand.Subcommands.First(c => c.Name == "extract");

        // Assert
        var sourceOption = extractCommand.Options.First(o => o.Name == "source-connection-string");
        sourceOption.Aliases.Should().Contain("-scs");

        var targetOption = extractCommand.Options.First(o => o.Name == "target-file");
        targetOption.Aliases.Should().Contain("-tf");
    }

    [Test]
    public void PublishCommand_HasAllRequiredOptions()
    {
        // Arrange
        var rootCommand = CreateRootCommand();
        var publishCommand = rootCommand.Subcommands.First(c => c.Name == "publish");

        // Assert
        var optionNames = publishCommand.Options.Select(o => o.Name).ToList();
        optionNames.Should().Contain("source-file");
        optionNames.Should().Contain("target-connection-string");
        optionNames.Should().Contain("variables");
        optionNames.Should().Contain("drop-objects-not-in-source");
        optionNames.Should().Contain("transactional");
        optionNames.Should().Contain("script-output");
    }

    [Test]
    public void PublishCommand_SourceFileIsRequired()
    {
        // Arrange
        var rootCommand = CreateRootCommand();
        var publishCommand = rootCommand.Subcommands.First(c => c.Name == "publish");
        var sourceOption = publishCommand.Options.First(o => o.Name == "source-file");

        // Assert
        sourceOption.IsRequired.Should().BeTrue();
    }

    [Test]
    public void PublishCommand_VariablesAllowMultiple()
    {
        // Arrange
        var rootCommand = CreateRootCommand();
        var publishCommand = rootCommand.Subcommands.First(c => c.Name == "publish");
        var variablesOption = publishCommand.Options.First(o => o.Name == "variables");

        // Assert
        variablesOption.AllowMultipleArgumentsPerToken.Should().BeTrue();
    }

    [Test]
    public void PublishCommand_ScriptOutputIsOptional()
    {
        // Arrange
        var rootCommand = CreateRootCommand();
        var publishCommand = rootCommand.Subcommands.First(c => c.Name == "publish");
        var scriptOutputOption = publishCommand.Options.First(o => o.Name == "script-output");

        // Assert
        scriptOutputOption.IsRequired.Should().BeFalse();
        scriptOutputOption.Aliases.Should().Contain("-so");
    }

    [Test]
    public void ScriptCommand_HasAllRequiredOptions()
    {
        // Arrange
        var rootCommand = CreateRootCommand();
        var scriptCommand = rootCommand.Subcommands.First(c => c.Name == "script");

        // Assert
        var optionNames = scriptCommand.Options.Select(o => o.Name).ToList();
        optionNames.Should().Contain("source-file");
        optionNames.Should().Contain("target-connection-string");
        optionNames.Should().Contain("output-file");
        optionNames.Should().Contain("variables");
        optionNames.Should().Contain("drop-objects-not-in-source");
    }

    [Test]
    public void ScriptCommand_OutputFileIsRequired()
    {
        // Arrange
        var rootCommand = CreateRootCommand();
        var scriptCommand = rootCommand.Subcommands.First(c => c.Name == "script");
        var outputOption = scriptCommand.Options.First(o => o.Name == "output-file");

        // Assert
        outputOption.IsRequired.Should().BeTrue();
    }

    [Test]
    public void CompileCommand_HasRequiredOptions()
    {
        // Arrange
        var rootCommand = CreateRootCommand();
        var compileCommand = rootCommand.Subcommands.First(c => c.Name == "compile");

        // Assert
        var optionNames = compileCommand.Options.Select(o => o.Name).ToList();
        optionNames.Should().Contain("source-file");
        optionNames.Should().Contain("verbose");
    }

    [Test]
    public void CompileCommand_VerboseIsOptional()
    {
        // Arrange
        var rootCommand = CreateRootCommand();
        var compileCommand = rootCommand.Subcommands.First(c => c.Name == "compile");
        var verboseOption = compileCommand.Options.First(o => o.Name == "verbose");

        // Assert
        verboseOption.IsRequired.Should().BeFalse();
    }

    [Test]
    public void DeployReportCommand_HasAllRequiredOptions()
    {
        // Arrange
        var rootCommand = CreateRootCommand();
        var reportCommand = rootCommand.Subcommands.First(c => c.Name == "deploy-report");

        // Assert
        var optionNames = reportCommand.Options.Select(o => o.Name).ToList();
        optionNames.Should().Contain("source-file");
        optionNames.Should().Contain("target-connection-string");
        optionNames.Should().Contain("output-file");
    }

    [Test]
    public void ExtractCommand_ParseValidArguments_Succeeds()
    {
        // Arrange
        var rootCommand = CreateRootCommand();
        var args = new[]
        {
            "extract",
            "--source-connection-string", "Host=localhost;Database=test",
            "--target-file", "test.pgproj.json"
        };

        // Act
        var parseResult = rootCommand.Parse(args);

        // Assert
        parseResult.Errors.Should().BeEmpty();
    }

    [Test]
    public void PublishCommand_ParseValidArgumentsWithScriptOutput_Succeeds()
    {
        // Arrange
        var rootCommand = CreateRootCommand();
        var args = new[]
        {
            "publish",
            "--source-file", "test.pgpac",
            "--target-connection-string", "Host=localhost;Database=test",
            "--script-output", "deployment_test_20260101_010203.sql"
        };

        // Act
        var parseResult = rootCommand.Parse(args);

        // Assert
        parseResult.Errors.Should().BeEmpty();
    }

    [Test]
    public void ExtractCommand_MissingRequiredOption_ReturnsError()
    {
        // Arrange
        var rootCommand = CreateRootCommand();
        var args = new[]
        {
            "extract",
            "--source-connection-string", "Host=localhost;Database=test"
            // Missing --target-file
        };

        // Act
        var parseResult = rootCommand.Parse(args);

        // Assert
        parseResult.Errors.Should().NotBeEmpty();
        parseResult.Errors.Should().Contain(e => e.Message.Contains("target-file"));
    }

    [Test]
    public void PublishCommand_ParseValidArgumentsWithVariables_Succeeds()
    {
        // Arrange
        var rootCommand = CreateRootCommand();
        var args = new[]
        {
            "publish",
            "--source-file", "test.pgproj.json",
            "--target-connection-string", "Host=localhost;Database=test",
            "--variables", "Var1=Value1", "Var2=Value2"
        };

        // Act
        var parseResult = rootCommand.Parse(args);

        // Assert
        parseResult.Errors.Should().BeEmpty();
    }

    [Test]
    public void PublishCommand_UseShortAliases_Succeeds()
    {
        // Arrange
        var rootCommand = CreateRootCommand();
        var args = new[]
        {
            "publish",
            "-sf", "test.pgproj.json",
            "-tcs", "Host=localhost;Database=test"
        };

        // Act
        var parseResult = rootCommand.Parse(args);

        // Assert
        parseResult.Errors.Should().BeEmpty();
    }

    [Test]
    public void ScriptCommand_ParseValidArguments_Succeeds()
    {
        // Arrange
        var rootCommand = CreateRootCommand();
        var args = new[]
        {
            "script",
            "--source-file", "test.pgproj.json",
            "--target-connection-string", "Host=localhost;Database=test",
            "--output-file", "deploy.sql"
        };

        // Act
        var parseResult = rootCommand.Parse(args);

        // Assert
        parseResult.Errors.Should().BeEmpty();
    }

    [Test]
    public void CompileCommand_ParseValidArguments_Succeeds()
    {
        // Arrange
        var rootCommand = CreateRootCommand();
        var args = new[]
        {
            "compile",
            "--source-file", "test.pgproj.json",
            "--verbose"
        };

        // Act
        var parseResult = rootCommand.Parse(args);

        // Assert
        parseResult.Errors.Should().BeEmpty();
    }

    [Test]
    public void DeployReportCommand_ParseValidArguments_Succeeds()
    {
        // Arrange
        var rootCommand = CreateRootCommand();
        var args = new[]
        {
            "deploy-report",
            "--source-file", "test.pgproj.json",
            "--target-connection-string", "Host=localhost;Database=test",
            "--output-file", "report.json"
        };

        // Act
        var parseResult = rootCommand.Parse(args);

        // Assert
        parseResult.Errors.Should().BeEmpty();
    }

    [Test]
    public void RootCommand_NoArguments_ShowsHelp()
    {
        // Arrange
        var rootCommand = CreateRootCommand();
        var console = new TestConsole();
        var args = Array.Empty<string>();

        // Act
        var result = rootCommand.Invoke(args, console);

        // Assert
        var output = console.Out.ToString();
        output.Should().Contain("Usage:");
        output.Should().Contain("Commands:");
    }

    [Test]
    public void ExtractCommand_WithHelpFlag_ShowsHelp()
    {
        // Arrange
        var rootCommand = CreateRootCommand();
        var console = new TestConsole();
        var args = new[] { "extract", "--help" };

        // Act
        var result = rootCommand.Invoke(args, console);

        // Assert
        var output = console.Out.ToString();
        output.Should().Contain("Extract database schema");
        output.Should().Contain("source-connection-string");
        output.Should().Contain("target-file");
    }

    // Helper method to create root command (mirrors Program.cs structure)
    private static RootCommand CreateRootCommand()
    {
        var rootCommand = new RootCommand("PostgreSQL Data-Tier Application Tools - Extract, compile, and publish PostgreSQL schemas");

        // Extract command
        var extractCommand = new Command("extract", "Extract database schema to a .pgproj.json file");
        var extractSourceOption = new Option<string>("--source-connection-string", "Source PostgreSQL database connection string") { IsRequired = true };
        extractSourceOption.AddAlias("-scs");
        var extractTargetOption = new Option<string>("--target-file", "Path to output .pgproj.json file") { IsRequired = true };
        extractTargetOption.AddAlias("-tf");
        var extractDbOption = new Option<string>("--database-name", "Database name (overrides connection string)");
        extractDbOption.AddAlias("-dn");
        extractCommand.AddOption(extractSourceOption);
        extractCommand.AddOption(extractTargetOption);
        extractCommand.AddOption(extractDbOption);
        rootCommand.AddCommand(extractCommand);

        // Publish command
        var publishCommand = new Command("publish", "Publish schema changes to target database");
        var publishSourceOption = new Option<string>("--source-file", "Source .pgproj.json file") { IsRequired = true };
        publishSourceOption.AddAlias("-sf");
        var publishTargetOption = new Option<string>("--target-connection-string", "Target PostgreSQL database connection string") { IsRequired = true };
        publishTargetOption.AddAlias("-tcs");
        var publishVariablesOption = new Option<string[]>("--variables", "SQLCMD variables in format Name=Value") { AllowMultipleArgumentsPerToken = true };
        publishVariablesOption.AddAlias("-v");
        var publishDropOption = new Option<bool>("--drop-objects-not-in-source", () => false);
        publishDropOption.AddAlias("-dons");
        var publishTransOption = new Option<bool>("--transactional", () => true);
        var publishScriptOutputOption = new Option<string?>("--script-output", "Optional path for the generated deployment script");
        publishScriptOutputOption.AddAlias("-so");
        publishCommand.AddOption(publishSourceOption);
        publishCommand.AddOption(publishTargetOption);
        publishCommand.AddOption(publishVariablesOption);
        publishCommand.AddOption(publishDropOption);
        publishCommand.AddOption(publishTransOption);
        publishCommand.AddOption(publishScriptOutputOption);
        rootCommand.AddCommand(publishCommand);

        // Script command
        var scriptCommand = new Command("script", "Generate deployment script without executing");
        var scriptSourceOption = new Option<string>("--source-file", "Source .pgproj.json file") { IsRequired = true };
        scriptSourceOption.AddAlias("-sf");
        var scriptTargetOption = new Option<string>("--target-connection-string", "Target PostgreSQL database connection string") { IsRequired = true };
        scriptTargetOption.AddAlias("-tcs");
        var scriptOutputOption = new Option<string>("--output-file", "Path to output SQL script file") { IsRequired = true };
        scriptOutputOption.AddAlias("-of");
        var scriptVariablesOption = new Option<string[]>("--variables", "SQLCMD variables in format Name=Value") { AllowMultipleArgumentsPerToken = true };
        scriptVariablesOption.AddAlias("-v");
        var scriptDropOption = new Option<bool>("--drop-objects-not-in-source", () => false);
        scriptDropOption.AddAlias("-dons");
        scriptCommand.AddOption(scriptSourceOption);
        scriptCommand.AddOption(scriptTargetOption);
        scriptCommand.AddOption(scriptOutputOption);
        scriptCommand.AddOption(scriptVariablesOption);
        scriptCommand.AddOption(scriptDropOption);
        rootCommand.AddCommand(scriptCommand);

        // Compile command
        var compileCommand = new Command("compile", "Compile and validate project dependencies");
        var compileSourceOption = new Option<string>("--source-file", "Source .pgproj.json file") { IsRequired = true };
        compileSourceOption.AddAlias("-sf");
        var compileVerboseOption = new Option<bool>("--verbose", () => false);
        compileVerboseOption.AddAlias("-v");
        compileCommand.AddOption(compileSourceOption);
        compileCommand.AddOption(compileVerboseOption);
        rootCommand.AddCommand(compileCommand);

        // Deploy-report command
        var reportCommand = new Command("deploy-report", "Generate report of deployment changes");
        var reportSourceOption = new Option<string>("--source-file", "Source .pgproj.json file") { IsRequired = true };
        reportSourceOption.AddAlias("-sf");
        var reportTargetOption = new Option<string>("--target-connection-string", "Target PostgreSQL database connection string") { IsRequired = true };
        reportTargetOption.AddAlias("-tcs");
        var reportOutputOption = new Option<string>("--output-file", "Path to output report file (JSON)") { IsRequired = true };
        reportOutputOption.AddAlias("-of");
        reportCommand.AddOption(reportSourceOption);
        reportCommand.AddOption(reportTargetOption);
        reportCommand.AddOption(reportOutputOption);
        rootCommand.AddCommand(reportCommand);

        return rootCommand;
    }
}
