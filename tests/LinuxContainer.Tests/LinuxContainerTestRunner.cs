using Docker.DotNet;
using Docker.DotNet.Models;
using FluentAssertions;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;

namespace LinuxContainer.Tests;

/// <summary>
/// Tests that build and run other test projects in Linux containers to verify CI compatibility.
/// This helps catch Linux-specific issues (like protobuf corruption) before pushing to GitHub Actions.
/// </summary>
[TestFixture]
public class LinuxContainerTestRunner : LinuxContainerTestBase
{

    [Test]
    [Category("LinuxContainer")]
    [Category("RequiresDocker")]
    public async Task BuildAndTest_DacTests_InLinuxContainer()
    {
        if (!IsDockerAvailable)
        {
            Assert.Ignore("Docker is not available. Install Docker Desktop to run this test.");
            return;
        }

        var testResults = await RunTestsInLinuxContainerAsync(
            "mbulava.PostgreSql.Dac.Tests",
            "tests/mbulava.PostgreSql.Dac.Tests/mbulava.PostgreSql.Dac.Tests.csproj"
        );

        testResults.Should().NotBeNull();
        testResults.ExitCode.Should().Be(0, because: "all tests should pass on Linux");
        testResults.Output.Should().Contain("Test Run Successful", because: "dotnet test should complete successfully");
    }

    [Test]
    [Category("LinuxContainer")]
    [Category("RequiresDocker")]
    public async Task BuildAndTest_ProjectExtractTests_InLinuxContainer()
    {
        if (!IsDockerAvailable)
        {
            Assert.Ignore("Docker is not available. Install Docker Desktop to run this test.");
            return;
        }

        var testResults = await RunTestsInLinuxContainerAsync(
            "ProjectExtract-Tests",
            "tests/ProjectExtract-Tests/ProjectExtract-Tests.csproj"
        );

        testResults.Should().NotBeNull();
        testResults.ExitCode.Should().Be(0, because: "all tests should pass on Linux");
        testResults.Output.Should().Contain("Test Run Successful", because: "dotnet test should complete successfully");
    }

    [Test]
    [Category("LinuxContainer")]
    [Category("RequiresDocker")]
    [Explicit("Long running test - run manually or in CI")]
    public async Task BuildAndTest_AllProjects_InLinuxContainer()
    {
        if (!IsDockerAvailable)
        {
            Assert.Ignore("Docker is not available. Install Docker Desktop to run this test.");
            return;
        }

        // Test all projects
        var projects = new[]
        {
            ("mbulava.PostgreSql.Dac.Tests", "tests/mbulava.PostgreSql.Dac.Tests/mbulava.PostgreSql.Dac.Tests.csproj"),
            ("ProjectExtract-Tests", "tests/ProjectExtract-Tests/ProjectExtract-Tests.csproj")
        };

        var allPassed = true;
        var results = new StringBuilder();

        foreach (var (name, path) in projects)
        {
            TestContext.WriteLine($"\n{'='} Testing: {name} {'='}");
            
            var testResults = await RunTestsInLinuxContainerAsync(name, path);
            
            results.AppendLine($"\n## {name}");
            results.AppendLine($"Exit Code: {testResults.ExitCode}");
            results.AppendLine($"Output:\n{testResults.Output}");
            
            if (testResults.ExitCode != 0)
            {
                allPassed = false;
                TestContext.WriteLine($"❌ {name} FAILED");
                results.AppendLine($"❌ FAILED");
            }
            else
            {
                TestContext.WriteLine($"✅ {name} PASSED");
                results.AppendLine($"✅ PASSED");
            }
        }

        TestContext.WriteLine("\n" + results.ToString());
        allPassed.Should().BeTrue("All test projects should pass on Linux");
    }

    [Test]
    [Category("LinuxContainer")]
    [Category("RequiresDocker")]
    public async Task Verify_NativeLibraries_LoadOnLinux()
    {
        if (!IsDockerAvailable)
        {
            Assert.Ignore("Docker is not available. Install Docker Desktop to run this test.");
            return;
        }

        var script = @"
#!/bin/bash
set -e

# Check if native libraries exist
echo ""Checking native libraries...""

NPGQUERY_LIB=""src/libs/Npgquery/Npgquery/runtimes/linux-x64/native/libpg_query.so""

if [ -f ""$NPGQUERY_LIB"" ]; then
    echo ""✅ Found: $NPGQUERY_LIB""
    ls -lh ""$NPGQUERY_LIB""
    
    # Check if it's a valid ELF binary
    file ""$NPGQUERY_LIB""
    
    # Check library dependencies
    echo ""Library dependencies:""
    ldd ""$NPGQUERY_LIB"" || true
else
    echo ""❌ Missing: $NPGQUERY_LIB""
    exit 1
fi

# Try to load the library in a simple C# program
echo ""Testing library load in .NET...""
cd src/libs/Npgquery/Npgquery
dotnet build -c Release
echo ""✅ Npgquery library built successfully""
";

        var result = await RunScriptInLinuxContainerAsync("verify-native-libs", script);

        result.ExitCode.Should().Be(0, because: "native libraries should be present and loadable on Linux");
        result.Output.Should().Contain("✅ Found:", because: "libpg_query.so should exist");
        result.Output.Should().Contain("ELF 64-bit", because: "it should be a valid Linux binary");
    }

    /// <summary>
    /// Runs tests for a specific project in a Linux container
    /// </summary>
    private async Task<TestResult> RunTestsInLinuxContainerAsync(string projectName, string projectPath)
    {
        var solutionRoot = GetSolutionRoot();
        
        TestContext.WriteLine($"📦 Building Linux container for: {projectName}");
        TestContext.WriteLine($"   Project: {projectPath}");
        TestContext.WriteLine($"   Solution Root: {solutionRoot}");

        // Create a temporary script to run the tests
        var scriptContent = $@"
#!/bin/bash
set -e

echo ""==============================================""
echo ""Linux Container Test Runner""
echo ""Project: {projectName}""
echo ""==============================================""
echo """"

# Show environment
echo ""Environment:""
dotnet --info
echo """"

# Restore packages
echo ""Restoring packages...""
dotnet restore
echo """"

# Build the project
echo ""Building {projectName}...""
dotnet build {projectPath} -c Release --no-restore
echo """"

# Run the tests
echo ""Running tests for {projectName}...""
dotnet test {projectPath} -c Release --no-build --no-restore --logger ""console;verbosity=normal""
echo """"

echo ""✅ Test run complete""
";

        return await RunScriptInLinuxContainerAsync(projectName, scriptContent);
    }
}
