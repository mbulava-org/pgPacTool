using FluentAssertions;
using mbulava.PostgreSql.Dac.Deployment;
using mbulava.PostgreSql.Dac.Models;

namespace mbulava.PostgreSql.Dac.Tests.Deployment;

[TestFixture]
public class SqlCmdVariableParserTests
{
    [Test]
    public void ExtractVariableNames_EmptyScript_ReturnsEmptyList()
    {
        // Arrange
        var script = "";

        // Act
        var result = SqlCmdVariableParser.ExtractVariableNames(script);

        // Assert
        result.Should().BeEmpty();
    }

    [Test]
    public void ExtractVariableNames_NoVariables_ReturnsEmptyList()
    {
        // Arrange
        var script = "CREATE TABLE users (id INT);";

        // Act
        var result = SqlCmdVariableParser.ExtractVariableNames(script);

        // Assert
        result.Should().BeEmpty();
    }

    [Test]
    public void ExtractVariableNames_SingleVariable_ReturnsVariable()
    {
        // Arrange
        var script = "CREATE DATABASE $(DatabaseName);";

        // Act
        var result = SqlCmdVariableParser.ExtractVariableNames(script);

        // Assert
        result.Should().ContainSingle()
            .Which.Should().Be("DatabaseName");
    }

    [Test]
    public void ExtractVariableNames_MultipleVariables_ReturnsAllVariables()
    {
        // Arrange
        var script = @"
            CREATE DATABASE $(DatabaseName);
            CREATE SCHEMA $(SchemaName);
            CREATE TABLE $(SchemaName).$(TableName) (id INT);
        ";

        // Act
        var result = SqlCmdVariableParser.ExtractVariableNames(script);

        // Assert
        result.Should().HaveCount(3);
        result.Should().Contain(new[] { "DatabaseName", "SchemaName", "TableName" });
    }

    [Test]
    public void ExtractVariableNames_DuplicateVariables_ReturnsUniqueList()
    {
        // Arrange
        var script = @"
            USE $(DatabaseName);
            GRANT ALL ON DATABASE $(DatabaseName) TO admin;
        ";

        // Act
        var result = SqlCmdVariableParser.ExtractVariableNames(script);

        // Assert
        result.Should().ContainSingle()
            .Which.Should().Be("DatabaseName");
    }

    [Test]
    public void ExtractVariableNames_VariableWithUnderscoreAndDots_ParsesCorrectly()
    {
        // Arrange
        var script = "USE $(My_Database.Name);";

        // Act
        var result = SqlCmdVariableParser.ExtractVariableNames(script);

        // Assert
        result.Should().ContainSingle()
            .Which.Should().Be("My_Database.Name");
    }

    [Test]
    public void ValidateVariables_AllDefined_ReturnsEmptyList()
    {
        // Arrange
        var script = "CREATE DATABASE $(DatabaseName);";
        var variables = new List<SqlCmdVariable>
        {
            new() { Name = "DatabaseName", Value = "testdb" }
        };

        // Act
        var result = SqlCmdVariableParser.ValidateVariables(script, variables);

        // Assert
        result.Should().BeEmpty();
    }

    [Test]
    public void ValidateVariables_MissingVariable_ReturnsUndefinedVariable()
    {
        // Arrange
        var script = "CREATE DATABASE $(DatabaseName);";
        var variables = new List<SqlCmdVariable>();

        // Act
        var result = SqlCmdVariableParser.ValidateVariables(script, variables);

        // Assert
        result.Should().ContainSingle()
            .Which.Should().Be("DatabaseName");
    }

    [Test]
    public void ValidateVariables_VariableWithoutValue_ReturnsUndefinedVariable()
    {
        // Arrange
        var script = "CREATE DATABASE $(DatabaseName);";
        var variables = new List<SqlCmdVariable>
        {
            new() { Name = "DatabaseName", Value = null, DefaultValue = null }
        };

        // Act
        var result = SqlCmdVariableParser.ValidateVariables(script, variables);

        // Assert
        result.Should().ContainSingle()
            .Which.Should().Be("DatabaseName");
    }

    [Test]
    public void ValidateVariables_VariableWithDefaultValue_ReturnsEmpty()
    {
        // Arrange
        var script = "CREATE DATABASE $(DatabaseName);";
        var variables = new List<SqlCmdVariable>
        {
            new() { Name = "DatabaseName", DefaultValue = "testdb" }
        };

        // Act
        var result = SqlCmdVariableParser.ValidateVariables(script, variables);

        // Assert
        result.Should().BeEmpty();
    }

    [Test]
    public void ReplaceVariables_SingleVariable_ReplacesCorrectly()
    {
        // Arrange
        var script = "CREATE DATABASE $(DatabaseName);";
        var variables = new List<SqlCmdVariable>
        {
            new() { Name = "DatabaseName", Value = "mydb" }
        };

        // Act
        var result = SqlCmdVariableParser.ReplaceVariables(script, variables);

        // Assert
        result.Should().Be("CREATE DATABASE mydb;");
    }

    [Test]
    public void ReplaceVariables_MultipleOccurrences_ReplacesAll()
    {
        // Arrange
        var script = "USE $(DB); GRANT ALL ON $(DB) TO user;";
        var variables = new List<SqlCmdVariable>
        {
            new() { Name = "DB", Value = "testdb" }
        };

        // Act
        var result = SqlCmdVariableParser.ReplaceVariables(script, variables);

        // Assert
        result.Should().Be("USE testdb; GRANT ALL ON testdb TO user;");
    }

    [Test]
    public void ReplaceVariables_ValueOverridesDefault_UsesValue()
    {
        // Arrange
        var script = "CREATE DATABASE $(DatabaseName);";
        var variables = new List<SqlCmdVariable>
        {
            new() { Name = "DatabaseName", DefaultValue = "default", Value = "override" }
        };

        // Act
        var result = SqlCmdVariableParser.ReplaceVariables(script, variables);

        // Assert
        result.Should().Be("CREATE DATABASE override;");
    }

    [Test]
    public void ReplaceVariables_NoValueWithDefault_UsesDefault()
    {
        // Arrange
        var script = "CREATE DATABASE $(DatabaseName);";
        var variables = new List<SqlCmdVariable>
        {
            new() { Name = "DatabaseName", DefaultValue = "defaultdb" }
        };

        // Act
        var result = SqlCmdVariableParser.ReplaceVariables(script, variables);

        // Assert
        result.Should().Be("CREATE DATABASE defaultdb;");
    }

    [Test]
    public void ReplaceVariables_UndefinedVariable_ThrowsException()
    {
        // Arrange
        var script = "CREATE DATABASE $(DatabaseName);";
        var variables = new List<SqlCmdVariable>();

        // Act & Assert
        var act = () => SqlCmdVariableParser.ReplaceVariables(script, variables, throwOnUndefined: true);
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Undefined SQLCMD variables*DatabaseName*");
    }

    [Test]
    public void ReplaceVariables_UndefinedVariableNoThrow_LeavesOriginal()
    {
        // Arrange
        var script = "CREATE DATABASE $(DatabaseName);";
        var variables = new List<SqlCmdVariable>();

        // Act
        var result = SqlCmdVariableParser.ReplaceVariables(script, variables, throwOnUndefined: false);

        // Assert
        result.Should().Be("CREATE DATABASE $(DatabaseName);");
    }

    [Test]
    public void ReplaceVariables_CaseInsensitive_ReplacesCorrectly()
    {
        // Arrange
        var script = "CREATE DATABASE $(DatabaseName);";
        var variables = new List<SqlCmdVariable>
        {
            new() { Name = "databasename", Value = "mydb" } // lowercase
        };

        // Act
        var result = SqlCmdVariableParser.ReplaceVariables(script, variables);

        // Assert
        result.Should().Be("CREATE DATABASE mydb;");
    }

    [Test]
    public void ReplaceVariablesWithResult_Success_ReturnsSuccessResult()
    {
        // Arrange
        var script = "CREATE DATABASE $(DatabaseName);";
        var variables = new List<SqlCmdVariable>
        {
            new() { Name = "DatabaseName", Value = "mydb" }
        };

        // Act
        var result = SqlCmdVariableParser.ReplaceVariablesWithResult(script, variables);

        // Assert
        result.Success.Should().BeTrue();
        result.ProcessedScript.Should().Be("CREATE DATABASE mydb;");
        result.Warnings.Should().BeEmpty();
        result.VariablesUsed.Should().ContainSingle().Which.Should().Be("DatabaseName");
    }

    [Test]
    public void ReplaceVariablesWithResult_UndefinedVariable_ReturnsWarning()
    {
        // Arrange
        var script = "CREATE DATABASE $(DatabaseName);";
        var variables = new List<SqlCmdVariable>();

        // Act
        var result = SqlCmdVariableParser.ReplaceVariablesWithResult(script, variables);

        // Assert
        result.Success.Should().BeTrue();
        result.ProcessedScript.Should().Be("CREATE DATABASE $(DatabaseName);");
        result.Warnings.Should().ContainSingle()
            .Which.Should().Contain("DatabaseName");
        result.VariablesUsed.Should().BeEmpty();
    }

    [Test]
    public void EscapeVariable_ValidName_ReturnsEscaped()
    {
        // Act
        var result = SqlCmdVariableParser.EscapeVariable("DatabaseName");

        // Assert
        result.Should().Be("$$(DatabaseName)");
    }

    [Test]
    public void UnescapeVariables_EscapedVariables_Unescapes()
    {
        // Arrange
        var script = "-- This is $$(DatabaseName) literal";

        // Act
        var result = SqlCmdVariableParser.UnescapeVariables(script);

        // Assert
        result.Should().Be("-- This is $(DatabaseName) literal");
    }
}
