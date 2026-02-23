using FluentAssertions;
using mbulava.PostgreSql.Dac.Deployment;
using mbulava.PostgreSql.Dac.Models;

namespace mbulava.PostgreSql.Dac.Tests.Deployment;

[TestFixture]
public class PrePostDeploymentScriptManagerTests
{
    private string _testDirectory = null!;
    private PrePostDeploymentScriptManager _manager = null!;

    [SetUp]
    public void Setup()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), $"pgpac_test_{Guid.NewGuid()}");
        Directory.CreateDirectory(_testDirectory);
        _manager = new PrePostDeploymentScriptManager(_testDirectory);
    }

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(_testDirectory))
        {
            Directory.Delete(_testDirectory, recursive: true);
        }
    }

    [Test]
    public async Task LoadScriptAsync_ValidScript_LoadsContent()
    {
        // Arrange
        var scriptPath = Path.Combine(_testDirectory, "test.sql");
        var content = "CREATE TABLE test (id INT);";
        await File.WriteAllTextAsync(scriptPath, content);

        var script = new DeploymentScript
        {
            FilePath = "test.sql",
            Type = DeploymentScriptType.PreDeployment,
            Order = 1
        };

        // Act
        var result = await _manager.LoadScriptAsync(script);

        // Assert
        result.Content.Should().Be(content);
    }

    [Test]
    public void LoadScriptAsync_FileNotFound_ThrowsException()
    {
        // Arrange
        var script = new DeploymentScript
        {
            FilePath = "nonexistent.sql",
            Type = DeploymentScriptType.PreDeployment,
            Order = 1
        };

        // Act & Assert
        var act = async () => await _manager.LoadScriptAsync(script);
        act.Should().ThrowAsync<FileNotFoundException>();
    }

    [Test]
    public async Task LoadScriptsAsync_MultipleScripts_LoadsAll()
    {
        // Arrange
        await File.WriteAllTextAsync(Path.Combine(_testDirectory, "script1.sql"), "SQL 1");
        await File.WriteAllTextAsync(Path.Combine(_testDirectory, "script2.sql"), "SQL 2");

        var scripts = new List<DeploymentScript>
        {
            new() { FilePath = "script1.sql", Type = DeploymentScriptType.PreDeployment, Order = 1 },
            new() { FilePath = "script2.sql", Type = DeploymentScriptType.PostDeployment, Order = 2 }
        };

        // Act
        var result = await _manager.LoadScriptsAsync(scripts);

        // Assert
        result.Should().HaveCount(2);
        result[0].Content.Should().Be("SQL 1");
        result[1].Content.Should().Be("SQL 2");
    }

    [Test]
    public void ValidateScripts_AllValid_ReturnsEmpty()
    {
        // Arrange
        var scriptPath = Path.Combine(_testDirectory, "valid.sql");
        File.WriteAllText(scriptPath, "SELECT 1;");

        var scripts = new List<DeploymentScript>
        {
            new() { FilePath = "valid.sql", Type = DeploymentScriptType.PreDeployment, Order = 1 }
        };

        // Act
        var result = _manager.ValidateScripts(scripts);

        // Assert
        result.Should().BeEmpty();
    }

    [Test]
    public void ValidateScripts_EmptyFilePath_ReturnsError()
    {
        // Arrange
        var scripts = new List<DeploymentScript>
        {
            new() { FilePath = "", Type = DeploymentScriptType.PreDeployment, Order = 1 }
        };

        // Act
        var result = _manager.ValidateScripts(scripts);

        // Assert
        result.Should().ContainSingle()
            .Which.Should().Contain("empty FilePath");
    }

    [Test]
    public void ValidateScripts_FileNotFound_ReturnsError()
    {
        // Arrange
        var scripts = new List<DeploymentScript>
        {
            new() { FilePath = "missing.sql", Type = DeploymentScriptType.PreDeployment, Order = 1 }
        };

        // Act
        var result = _manager.ValidateScripts(scripts);

        // Assert
        result.Should().ContainSingle()
            .Which.Should().Contain("not found");
    }

    [Test]
    public void ValidateScripts_DuplicateOrders_ReturnsError()
    {
        // Arrange
        var scriptPath1 = Path.Combine(_testDirectory, "script1.sql");
        var scriptPath2 = Path.Combine(_testDirectory, "script2.sql");
        File.WriteAllText(scriptPath1, "SQL 1");
        File.WriteAllText(scriptPath2, "SQL 2");

        var scripts = new List<DeploymentScript>
        {
            new() { FilePath = "script1.sql", Type = DeploymentScriptType.PreDeployment, Order = 1 },
            new() { FilePath = "script2.sql", Type = DeploymentScriptType.PreDeployment, Order = 1 } // Duplicate
        };

        // Act
        var result = _manager.ValidateScripts(scripts);

        // Assert
        result.Should().ContainSingle()
            .Which.Should().Contain("duplicate order");
    }

    [Test]
    public void OrderScripts_UnorderedList_ReturnsSorted()
    {
        // Arrange
        var scripts = new List<DeploymentScript>
        {
            new() { FilePath = "c.sql", Order = 3 },
            new() { FilePath = "a.sql", Order = 1 },
            new() { FilePath = "b.sql", Order = 2 }
        };

        // Act
        var result = PrePostDeploymentScriptManager.OrderScripts(scripts);

        // Assert
        result.Should().HaveCount(3);
        result[0].FilePath.Should().Be("a.sql");
        result[1].FilePath.Should().Be("b.sql");
        result[2].FilePath.Should().Be("c.sql");
    }

    [Test]
    public void CombineScripts_MultipleScripts_CombinesWithComments()
    {
        // Arrange
        var scripts = new List<DeploymentScript>
        {
            new()
            {
                FilePath = "script1.sql",
                Order = 1,
                Type = DeploymentScriptType.PreDeployment,
                Content = "CREATE TABLE t1 (id INT);",
                Description = "Create table 1"
            },
            new()
            {
                FilePath = "script2.sql",
                Order = 2,
                Type = DeploymentScriptType.PreDeployment,
                Content = "CREATE TABLE t2 (id INT);",
                Description = "Create table 2"
            }
        };

        // Act
        var result = PrePostDeploymentScriptManager.CombineScripts(scripts, includeComments: true);

        // Assert
        result.Should().Contain("script1.sql");
        result.Should().Contain("script2.sql");
        result.Should().Contain("Create table 1");
        result.Should().Contain("Create table 2");
        result.Should().Contain("CREATE TABLE t1 (id INT);");
        result.Should().Contain("CREATE TABLE t2 (id INT);");
    }

    [Test]
    public void CombineScripts_NoComments_CombinesWithoutComments()
    {
        // Arrange
        var scripts = new List<DeploymentScript>
        {
            new()
            {
                FilePath = "script1.sql",
                Order = 1,
                Content = "SELECT 1;"
            }
        };

        // Act
        var result = PrePostDeploymentScriptManager.CombineScripts(scripts, includeComments: false);

        // Assert
        result.Should().NotContain("script1.sql");
        result.Should().Contain("SELECT 1;");
    }

    [Test]
    public void ApplyVariables_ValidVariables_ReplacesInAllScripts()
    {
        // Arrange
        var scripts = new List<DeploymentScript>
        {
            new() { FilePath = "s1.sql", Content = "USE $(DB);", Order = 1, Type = DeploymentScriptType.PreDeployment },
            new() { FilePath = "s2.sql", Content = "CREATE TABLE $(DB).t1 (id INT);", Order = 2, Type = DeploymentScriptType.PreDeployment }
        };

        var variables = new List<SqlCmdVariable>
        {
            new() { Name = "DB", Value = "testdb" }
        };

        // Act
        var result = PrePostDeploymentScriptManager.ApplyVariables(scripts, variables);

        // Assert
        result.Should().HaveCount(2);
        result[0].Content.Should().Be("USE testdb;");
        result[1].Content.Should().Be("CREATE TABLE testdb.t1 (id INT);");
    }

    [Test]
    public void DiscoverScripts_ExistingDirectory_FindsScripts()
    {
        // Arrange
        var scriptDir = Path.Combine(_testDirectory, "scripts");
        Directory.CreateDirectory(scriptDir);
        File.WriteAllText(Path.Combine(scriptDir, "001_init.sql"), "SQL 1");
        File.WriteAllText(Path.Combine(scriptDir, "002_data.sql"), "SQL 2");

        // Act
        var result = _manager.DiscoverScripts("scripts", DeploymentScriptType.PreDeployment);

        // Assert
        result.Should().HaveCount(2);
        result[0].Order.Should().Be(0);
        result[1].Order.Should().Be(1);
    }

    [Test]
    public void DiscoverScripts_NonExistentDirectory_ReturnsEmpty()
    {
        // Act
        var result = _manager.DiscoverScripts("nonexistent", DeploymentScriptType.PreDeployment);

        // Assert
        result.Should().BeEmpty();
    }

    [Test]
    public void ValidateScriptContent_EmptyScript_ReturnsWarning()
    {
        // Arrange
        var script = new DeploymentScript
        {
            FilePath = "empty.sql",
            Content = ""
        };

        // Act
        var result = PrePostDeploymentScriptManager.ValidateScriptContent(script);

        // Assert
        result.Should().ContainSingle()
            .Which.Should().Contain("empty");
    }

    [Test]
    public void ValidateScriptContent_TransactionalWithBegin_ReturnsWarning()
    {
        // Arrange
        var script = new DeploymentScript
        {
            FilePath = "test.sql",
            Content = "BEGIN; SELECT 1; COMMIT;",
            Transactional = true
        };

        // Act
        var result = PrePostDeploymentScriptManager.ValidateScriptContent(script);

        // Assert
        result.Should().Contain(w => w.Contains("BEGIN"));
    }

    [Test]
    public void ValidateScriptContent_TransactionalWithCommit_ReturnsWarning()
    {
        // Arrange
        var script = new DeploymentScript
        {
            FilePath = "test.sql",
            Content = "SELECT 1; COMMIT;",
            Transactional = true
        };

        // Act
        var result = PrePostDeploymentScriptManager.ValidateScriptContent(script);

        // Assert
        result.Should().Contain(w => w.Contains("COMMIT"));
    }

    [Test]
    public void ValidateScriptContent_UnreplacedVariables_ReturnsWarning()
    {
        // Arrange
        var script = new DeploymentScript
        {
            FilePath = "test.sql",
            Content = "USE $(DatabaseName);"
        };

        // Act
        var result = PrePostDeploymentScriptManager.ValidateScriptContent(script);

        // Assert
        result.Should().ContainSingle()
            .Which.Should().Contain("$(DatabaseName)");
    }

    [Test]
    public void ValidateScriptContent_ValidScript_ReturnsEmpty()
    {
        // Arrange
        var script = new DeploymentScript
        {
            FilePath = "test.sql",
            Content = "CREATE TABLE test (id INT);",
            Transactional = true
        };

        // Act
        var result = PrePostDeploymentScriptManager.ValidateScriptContent(script);

        // Assert
        result.Should().BeEmpty();
    }
}
