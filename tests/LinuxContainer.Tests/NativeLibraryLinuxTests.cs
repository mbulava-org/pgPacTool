using FluentAssertions;

namespace LinuxContainer.Tests;

/// <summary>
/// Tests native library loading and functionality on Linux containers
/// This ensures our multi-version PostgreSQL support works correctly on Linux
/// </summary>
[TestFixture]
[Category("LinuxContainer")]
public class NativeLibraryLinuxTests : LinuxContainerTestBase
{
    [TestCase("15")]
    [TestCase("16")]
    [TestCase("17")]
    [TestCase("18")]
    public async Task LinuxContainer_CanLoadVersionedLibrary(string majorVersion)
    {
        if (!IsDockerAvailable)
        {
            Assert.Ignore("Docker is not available");
            return;
        }

        var script = @"
cd /workspace
dotnet test tests/NpgqueryExtended.Tests/NpgqueryExtended.Tests.csproj \
    --filter 'FullyQualifiedName~NativeLibraryIntegrationTests.NativeLibraryLoader_IsVersionAvailable_ReturnsTrue' \
    --logger 'console;verbosity=normal'
";

        var result = await RunScriptInLinuxContainerAsync($"pg{majorVersion}-load", script);
        
        result.ExitCode.Should().Be(0, $"PG{majorVersion} library should load successfully on Linux");
        result.Output.Should().Contain($"PostgreSQL {majorVersion} availability: True", $"PG{majorVersion} should be available on Linux");
    }

    [Test]
    public async Task LinuxContainer_DetectsSupportedVersions()
    {
        if (!IsDockerAvailable)
        {
            Assert.Ignore("Docker is not available");
            return;
        }

        var script = @"
cd /workspace
dotnet test tests/NpgqueryExtended.Tests/NpgqueryExtended.Tests.csproj \
    --filter 'FullyQualifiedName~NativeLibraryIntegrationTests.NativeLibraryLoader_CanDetectAvailableVersions' \
    --logger 'console;verbosity=detailed'
";

        var result = await RunScriptInLinuxContainerAsync("detect-versions", script);
        
        result.ExitCode.Should().Be(0, "Version detection should work on Linux");
        result.Output.Should().Contain("Available PostgreSQL versions", "Should report detected PostgreSQL versions");
        result.Output.Should().Contain("PostgreSQL 15", "Should detect PG15 when built");
        result.Output.Should().Contain("PostgreSQL 16", "Should detect PG16");
        result.Output.Should().Contain("PostgreSQL 17", "Should detect PG17");
        result.Output.Should().Contain("PostgreSQL 18", "Should detect PG18 when built");
    }

    [Test]
    public async Task LinuxContainer_BasicParsingWorks()
    {
        if (!IsDockerAvailable)
        {
            Assert.Ignore("Docker is not available");
            return;
        }

        var script = @"
cd /workspace
dotnet test tests/NpgqueryExtended.Tests/NpgqueryExtended.Tests.csproj \
    --filter 'FullyQualifiedName~NativeLibraryIntegrationTests.Parse_BasicQueries_SucceedsWithCorrectVersion' \
    --logger 'console;verbosity=normal'
";

        var result = await RunScriptInLinuxContainerAsync("basic-parsing", script);
        
        result.ExitCode.Should().Be(0, "Basic SQL parsing should work on Linux");
    }

    [Test]
    public async Task LinuxContainer_ComplexSQLWorks()
    {
        if (!IsDockerAvailable)
        {
            Assert.Ignore("Docker is not available");
            return;
        }

        var script = @"
cd /workspace
dotnet test tests/NpgqueryExtended.Tests/NpgqueryExtended.Tests.csproj \
    --filter 'FullyQualifiedName~NativeLibraryIntegrationTests.Parse_ComplexJoinQuery_Succeeds|FullyQualifiedName~NativeLibraryIntegrationTests.Parse_CTE_Succeeds|FullyQualifiedName~NativeLibraryIntegrationTests.Parse_WindowFunctions_Succeeds' \
    --logger 'console;verbosity=normal'
";

        var result = await RunScriptInLinuxContainerAsync("complex-sql", script);
        
        result.ExitCode.Should().Be(0, "Complex SQL should parse on Linux");
    }

    [Test]
    public async Task LinuxContainer_AllFunctionsExposed()
    {
        if (!IsDockerAvailable)
        {
            Assert.Ignore("Docker is not available");
            return;
        }

        var script = @"
cd /workspace
dotnet test tests/NpgqueryExtended.Tests/NpgqueryExtended.Tests.csproj \
    --filter 'FullyQualifiedName~FunctionalityExposureTests' \
    --logger 'console;verbosity=normal'
";

        var result = await RunScriptInLinuxContainerAsync("all-functions", script);
        
        result.ExitCode.Should().Be(0, "All exposed functions should work on Linux");
    }

    [Test]
    public async Task LinuxContainer_MultiVersionSupport()
    {
        if (!IsDockerAvailable)
        {
            Assert.Ignore("Docker is not available");
            return;
        }

        var script = @"
cd /workspace
dotnet test tests/NpgqueryExtended.Tests/NpgqueryExtended.Tests.csproj \
    --filter 'FullyQualifiedName~NativeLibraryIntegrationTests.Parser_MultipleVersions_Simultaneous_AllWork' \
    --logger 'console;verbosity=detailed'
";

        var result = await RunScriptInLinuxContainerAsync("multi-version", script);
        
        result.ExitCode.Should().Be(0, "Multiple versions should work simultaneously on Linux");
        result.Output.Should().Contain("parsers work simultaneously", "Should confirm multiple versions work together");
    }

    [Test]
    public async Task LinuxContainer_VersionSpecificFeatures()
    {
        if (!IsDockerAvailable)
        {
            Assert.Ignore("Docker is not available");
            return;
        }

        var script = @"
cd /workspace
dotnet test tests/NpgqueryExtended.Tests/NpgqueryExtended.Tests.csproj \
    --filter 'FullyQualifiedName~VersionCompatibilityTests.JsonTable_FailsBeforePG17_SucceedsInPG17AndLater' \
    --logger 'console;verbosity=detailed'
";

        var result = await RunScriptInLinuxContainerAsync("version-features", script);
        
        result.ExitCode.Should().Be(0, "Version-specific features should be properly differentiated");
        result.Output.Should().Contain("Passed", "JSON_TABLE test should pass");
    }

    [Test]
    public async Task LinuxContainer_LibraryPathsCorrect()
    {
        if (!IsDockerAvailable)
        {
            Assert.Ignore("Docker is not available");
            return;
        }

        var script = @"
cd /workspace
dotnet test tests/NpgqueryExtended.Tests/NpgqueryExtended.Tests.csproj \
    --filter 'FullyQualifiedName~NativeLibraryIntegrationTests.LibraryPaths_ExpectedFilesExist' \
    --logger 'console;verbosity=detailed'
";

        var result = await RunScriptInLinuxContainerAsync("library-paths", script);
        
        result.ExitCode.Should().Be(0, "Library paths should be correct on Linux");
        result.Output.Should().Contain("libpg_query_15.so", "Should find PG15 library when built");
        result.Output.Should().Contain("libpg_query_16.so", "Should find PG16 library");
        result.Output.Should().Contain("libpg_query_17.so", "Should find PG17 library");
        result.Output.Should().Contain("libpg_query_18.so", "Should find PG18 library when built");
    }

    [Test]
    public async Task LinuxContainer_PlatformDetection()
    {
        if (!IsDockerAvailable)
        {
            Assert.Ignore("Docker is not available");
            return;
        }

        var script = @"
cd /workspace
dotnet test tests/NpgqueryExtended.Tests/NpgqueryExtended.Tests.csproj \
    --filter 'FullyQualifiedName~NativeLibraryIntegrationTests.PlatformDetection_ReportsCorrectPlatform' \
    --logger 'console;verbosity=detailed'
";

        var result = await RunScriptInLinuxContainerAsync("platform-detection", script);
        
        result.ExitCode.Should().Be(0, "Platform detection should work");
        result.Output.Should().Contain("Linux", "Should detect Linux platform");
    }

    [Test]
    public async Task LinuxContainer_AllIntegrationTestsPass()
    {
        if (!IsDockerAvailable)
        {
            Assert.Ignore("Docker is not available");
            return;
        }

        var script = @"
cd /workspace
echo '=== Building Solution ==='
dotnet build --configuration Release

echo ''
echo '=== Running All Npgquery Integration Tests ==='
dotnet test tests/NpgqueryExtended.Tests/NpgqueryExtended.Tests.csproj \
    --filter 'FullyQualifiedName~NativeLibraryIntegrationTests' \
    --logger 'console;verbosity=normal' \
    --configuration Release

echo ''
echo '=== Running Version Compatibility Tests ==='
dotnet test tests/NpgqueryExtended.Tests/NpgqueryExtended.Tests.csproj \
    --filter 'FullyQualifiedName~VersionCompatibilityTests' \
    --logger 'console;verbosity=normal' \
    --configuration Release
";

        var result = await RunScriptInLinuxContainerAsync("all-tests", script);
        
        result.ExitCode.Should().Be(0, "All integration tests should pass on Linux");
        result.Output.Should().Contain("Test Run Successful", "Test run should be successful");
        result.Output.Should().NotContain("Failed:", "Should have no failures");
    }
}
