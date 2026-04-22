using System.Diagnostics;
using System.IO.Compression;
using System.Runtime.CompilerServices;
using System.Xml.Linq;
using Npgsql;
using NuGet.Packaging;
using Testcontainers.PostgreSql;
using Xunit;
using Xunit.Abstractions;

namespace NugetPackage.Tests;

/// <summary>
/// Tests to ensure NuGet packages are properly constructed with all required dependencies.
/// These tests prevent missing references in pgpac tool and mbulava.PostgreSql.Dac library.
/// </summary>
public class NugetPackageValidationTests : IDisposable
{
    private const string ReadmePackageVersion = "1.0.0-preview8";

    private readonly ITestOutputHelper _output;
    private readonly string _solutionRoot;
    private readonly string _testWorkspace;
    private readonly string _localNugetFeed;
    private readonly string _nugetPackagesFolder;

    public NugetPackageValidationTests(ITestOutputHelper output)
    {
        _output = output;
        
        // Find solution root (assuming tests are in tests/NugetPackage.Tests)
        _solutionRoot = FindSolutionRoot();
        
        // Create temporary workspace for test projects
        _testWorkspace = Path.Combine(Path.GetTempPath(), $"pgpac-nuget-tests-{Guid.NewGuid()}");
        Directory.CreateDirectory(_testWorkspace);
        
        // Create temporary local NuGet feed
        _localNugetFeed = Path.Combine(_testWorkspace, "local-feed");
        Directory.CreateDirectory(_localNugetFeed);

        // Use an isolated NuGet package cache per test instance
        _nugetPackagesFolder = Path.Combine(_testWorkspace, "packages");
        Directory.CreateDirectory(_nugetPackagesFolder);
        
        _output.WriteLine($"Solution root: {_solutionRoot}");
        _output.WriteLine($"Test workspace: {_testWorkspace}");
        _output.WriteLine($"Local NuGet feed: {_localNugetFeed}");
        _output.WriteLine($"NuGet packages folder: {_nugetPackagesFolder}");
    }

    public void Dispose()
    {
        // Clean up test workspace
        try
        {
            if (Directory.Exists(_testWorkspace))
            {
                Directory.Delete(_testWorkspace, recursive: true);
            }
        }
        catch (Exception ex)
        {
            _output.WriteLine($"Warning: Failed to clean up test workspace: {ex.Message}");
        }
    }

    [Fact]
    public async Task DacLibraryPackage_ShouldContainAllRequiredFiles()
    {
        // Arrange: Build the NuGet package
        var packagePath = await BuildPackage("mbulava.PostgreSql.Dac");

        // Act: Extract and validate package contents
        using var reader = new PackageArchiveReader(packagePath);
        var files = reader.GetFiles().ToList();
        var nuspecReader = await reader.GetNuspecReaderAsync(CancellationToken.None);

        // Assert: Verify package structure
        _output.WriteLine("Package contents:");
        foreach (var file in files)
        {
            _output.WriteLine($"  {file}");
        }

        // Check for main library
        Assert.Contains(files, f => f.Contains("lib/net10.0/mbulava.PostgreSql.Dac.dll"));

        // Check for Npgquery.dll (should be included due to PrivateAssets=all)
        Assert.Contains(files, f => f.Contains("lib/net10.0/Npgquery.dll"));

        // Check for native libraries
        Assert.Contains(files, f => f.Contains("runtimes/win-x64/native/") && f.EndsWith(".dll"));
        Assert.Contains(files, f => f.Contains("runtimes/linux-x64/native/") && f.EndsWith(".so"));
        // Note: macOS libraries are in osx-arm64, not osx-x64
        Assert.Contains(files, f => f.Contains("runtimes/osx-arm64/native/") && f.EndsWith(".dylib"));

        // Check for multiple PostgreSQL versions
        var nativeFiles = files.Where(f => f.Contains("runtimes/") && f.Contains("/native/")).ToList();
        _output.WriteLine($"Native library files found: {nativeFiles.Count}");
        foreach (var nativeFile in nativeFiles)
        {
            _output.WriteLine($"  {nativeFile}");
        }

        // Verify we have libraries for both PG 16 and 17
        var winNativeFiles = nativeFiles.Where(f => f.Contains("win-x64")).ToList();
        Assert.True(winNativeFiles.Count >= 2, $"Expected at least 2 Windows native libraries (PG 16 and 17), found {winNativeFiles.Count}");

        // Check NuGet dependencies
        var dependencies = nuspecReader.GetDependencyGroups().ToList();
        _output.WriteLine("Package dependencies:");
        foreach (var depGroup in dependencies)
        {
            _output.WriteLine($"  Target: {depGroup.TargetFramework}");
            foreach (var dep in depGroup.Packages)
            {
                _output.WriteLine($"    {dep.Id} {dep.VersionRange}");
            }
        }

        // Verify expected NuGet dependencies
        var net10Deps = dependencies.FirstOrDefault(d => d.TargetFramework.Framework == ".NETCoreApp" && d.TargetFramework.Version.Major == 10);
        Assert.NotNull(net10Deps);

        var depList = net10Deps.Packages.ToList();
        Assert.Contains(depList, d => d.Id == "Google.Protobuf");
        Assert.Contains(depList, d => d.Id == "Npgsql");

        // Npgquery should NOT be a NuGet dependency (it's embedded)
        Assert.DoesNotContain(depList, d => d.Id == "Npgquery");
    }

    [Fact]
    public async Task GlobalToolPackage_ShouldContainAllRequiredFiles()
    {
        // Arrange: Build the NuGet package
        var packagePath = await BuildPackage("postgresPacTools");

        // Act: Extract and validate package contents
        using var reader = new PackageArchiveReader(packagePath);
        var files = reader.GetFiles().ToList();
        var nuspecReader = await reader.GetNuspecReaderAsync(CancellationToken.None);

        // Assert: Verify package structure
        _output.WriteLine("Package contents:");
        foreach (var file in files)
        {
            _output.WriteLine($"  {file}");
        }

        // Check for tool DllEntryPoint setting (required for global tools)
        var nuspec = nuspecReader.Xml;
        var ns = nuspec.Root?.Name.Namespace ?? XNamespace.None;
        var metadata = nuspec.Root?.Element(ns + "metadata");
        var packageTypes = metadata?.Element(ns + "packageTypes");

        Assert.NotNull(packageTypes);
        var dotnetToolType = packageTypes.Elements(ns + "packageType")
            .FirstOrDefault(pt => pt.Attribute("name")?.Value == "DotnetTool");
        Assert.NotNull(dotnetToolType);

        // Check for main executable
        Assert.Contains(files, f => f.Contains("tools/net10.0/any/postgresPacTools.dll"));

        // Check for mbulava.PostgreSql.Dac.dll dependency
        Assert.Contains(files, f => f.Contains("tools/net10.0/any/mbulava.PostgreSql.Dac.dll"));

        // Check for Npgquery.dll
        Assert.Contains(files, f => f.Contains("tools/net10.0/any/Npgquery.dll"));

        // Check for managed runtime dependencies required by extraction/compile flows
        Assert.Contains(files, f => f.Contains("tools/net10.0/any/Google.Protobuf.dll"));
        Assert.Contains(files, f => f.Contains("tools/net10.0/any/Npgsql.dll"));
        Assert.Contains(files, f => f.Contains("tools/net10.0/any/System.CommandLine.dll"));

        // Check for native libraries in tools directory
        Assert.Contains(files, f => f.Contains("tools/net10.0/any/runtimes/win-x64/native/"));
        Assert.Contains(files, f => f.Contains("tools/net10.0/any/runtimes/linux-x64/native/"));
        // Note: macOS libraries are in osx-arm64, not osx-x64
        Assert.Contains(files, f => f.Contains("tools/net10.0/any/runtimes/osx-arm64/native/"));

        // Verify tool configuration
        var dotnetToolSettings = files.FirstOrDefault(f => f.EndsWith("DotnetToolSettings.xml"));
        Assert.NotNull(dotnetToolSettings);
        
        var toolSettingsStream = reader.GetStream(dotnetToolSettings);
        var toolSettingsXml = XDocument.Load(toolSettingsStream);
        var commandName = toolSettingsXml.Root?.Element("Commands")?.Element("Command")?.Attribute("Name")?.Value;
        
        Assert.Equal("pgpac", commandName);
    }

    [Fact]
    public async Task DacLibraryPackage_CanBeConsumedSuccessfully()
    {
        // Arrange: Build and publish the package
        var packagePath = await BuildPackage("mbulava.PostgreSql.Dac");
        await PublishPackageToLocalFeed(packagePath);
        var version = GetPackageVersion(packagePath);

        // Create a test console application
        var testProjectDir = Path.Combine(_testWorkspace, "TestDacConsumer");
        Directory.CreateDirectory(testProjectDir);

        // Act: Create and build a project that consumes the DAC library
        await CreateTestProject(testProjectDir, "mbulava.PostgreSql.Dac", version, includeTestCode: true);
        var buildResult = await RunDotNetCommand(testProjectDir, "build");

        // Assert: Build should succeed
        Assert.Equal(0, buildResult.ExitCode);
        Assert.Contains("Build succeeded", buildResult.Output, StringComparison.OrdinalIgnoreCase);

        // Verify the test application runs
        var runResult = await RunDotNetCommand(testProjectDir, "run");
        Assert.Equal(0, runResult.ExitCode);
        Assert.Contains("Created project: TestDatabase", runResult.Output);
    }

    [Fact]
    public async Task GlobalTool_CanBeInstalledAndExecuted()
    {
        // Arrange: Build and publish the package
        var packagePath = await BuildPackage("postgresPacTools");
        await PublishPackageToLocalFeed(packagePath);

        var version = GetPackageVersion(packagePath);
        ClearNuGetPackageCache("postgresPacTools");

        // Act: Install the global tool
        var installResult = await RunDotNetCommand(
            _testWorkspace,
            $"tool install postgresPacTools --version {version} --tool-path \"{Path.Combine(_testWorkspace, "tools")}\" --add-source \"{_localNugetFeed}\""
        );

        // Assert: Installation should succeed
        Assert.Equal(0, installResult.ExitCode);

        // Verify the tool can be executed
        var toolDirectory = Path.Combine(_testWorkspace, "tools");
        var toolPath = Path.Combine(_testWorkspace, "tools", "pgpac");
        if (OperatingSystem.IsWindows())
        {
            toolPath += ".exe";
        }

        Assert.True(File.Exists(toolPath), $"Tool executable not found at: {toolPath}");

        // With --tool-path installs, DLLs are stored under .store/<pkg>/<ver>/<pkg>/<ver>/tools/<tfm>/any/
        // not directly in the tool-path root (which only contains the shim executable).
        var packageId = "postgrespactools"; // NuGet normalises to lower-case
        var storeDllDirectory = Path.Combine(toolDirectory, ".store", packageId, version, packageId, version, "tools", "net10.0", "any");
        Assert.True(Directory.Exists(storeDllDirectory), $"Tool store directory not found at: {storeDllDirectory}");
        Assert.True(File.Exists(Path.Combine(storeDllDirectory, "Google.Protobuf.dll")), "Google.Protobuf.dll should be in the tool store");
        Assert.True(File.Exists(Path.Combine(storeDllDirectory, "Npgsql.dll")), "Npgsql.dll should be in the tool store");
        Assert.True(File.Exists(Path.Combine(storeDllDirectory, "Npgquery.dll")), "Npgquery.dll should be in the tool store");
        Assert.True(File.Exists(Path.Combine(storeDllDirectory, "mbulava.PostgreSql.Dac.dll")), "mbulava.PostgreSql.Dac.dll should be in the tool store");
        Assert.True(File.Exists(Path.Combine(storeDllDirectory, "System.CommandLine.dll")), "System.CommandLine.dll should be in the tool store");
        Assert.True(File.Exists(Path.Combine(storeDllDirectory, "postgresPacTools.deps.json")), "The tool deps file should be in the tool store");
        Assert.True(File.Exists(Path.Combine(storeDllDirectory, "postgresPacTools.runtimeconfig.json")), "The tool runtimeconfig should be in the tool store");

        var versionResult = await RunCommand(toolPath, "--version");
        Assert.Equal(0, versionResult.ExitCode);

        var helpResult = await RunCommand(toolPath, "--help");
        Assert.Equal(0, helpResult.ExitCode);
        // Check for either the tool command name or the description
        Assert.True(
            helpResult.Output.Contains("pgpac", StringComparison.OrdinalIgnoreCase) ||
            helpResult.Output.Contains("PostgreSQL", StringComparison.OrdinalIgnoreCase),
            "Help output should contain 'pgpac' or 'PostgreSQL'"
        );
    }

    [Fact]
    public async Task MsBuildSdkPackage_ReadmeQuickStartProject_CanBuildSuccessfully()
    {
        var packagePath = await BuildPackage("MSBuild.Sdk.PostgreSql");
        await PublishPackageToLocalFeed(packagePath);

        var version = GetPackageVersion(packagePath);
        Assert.Equal(ReadmePackageVersion, version);

        ClearNuGetPackageCache("MSBuild.Sdk.PostgreSql");

        var projectDirectory = Path.Combine(_testWorkspace, "MyDatabase");
        Directory.CreateDirectory(projectDirectory);

        await CreateQuickStartNuGetConfigAsync(projectDirectory);
        await CreateReadmeQuickStartProjectAsync(projectDirectory, version);

        var buildResult = await RunDotNetCommand(projectDirectory, "build");

        Assert.Equal(0, buildResult.ExitCode);
        Assert.Contains("Build succeeded", buildResult.Output, StringComparison.OrdinalIgnoreCase);

        var packageOutputPath = Path.Combine(projectDirectory, "bin", "Debug", "net10.0", "MyDatabase.pgpac");
        Assert.True(File.Exists(packageOutputPath), $"Expected quick start package output at {packageOutputPath}");
    }

    [Fact]
    public async Task MsBuildSdkPackage_ShouldContainCliCompileHost()
    {
        var packagePath = await BuildPackage("MSBuild.Sdk.PostgreSql");

        using var reader = new PackageArchiveReader(packagePath);
        var files = reader.GetFiles().ToList();

        Assert.Contains(files, f => f.Contains("tasks/net10.0/mbulava.PostgreSql.Dac.dll", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(files, f => f.Contains("tasks/net10.0/Google.Protobuf.dll", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(files, f => f.Contains("tasks/net10.0/Npgsql.dll", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(files, f => f.Contains("tasks/net10.0/Npgquery.dll", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(files, f => f.Contains("tasks/net10.0/cli/postgresPacTools.dll", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(files, f => f.Contains("tasks/net10.0/cli/postgresPacTools.deps.json", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(files, f => f.Contains("tasks/net10.0/cli/postgresPacTools.runtimeconfig.json", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(files, f => f.Contains("tasks/net10.0/cli/mbulava.PostgreSql.Dac.dll", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(files, f => f.Contains("tasks/net10.0/cli/Google.Protobuf.dll", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(files, f => f.Contains("tasks/net10.0/cli/Npgsql.dll", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(files, f => f.Contains("tasks/net10.0/cli/Npgquery.dll", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(files, f => f.Contains("tasks/net10.0/cli/System.CommandLine.dll", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task MsBuildSdkPackage_LeavesTargetPathForProjectSystem_WhileUsingPgPacFilePath()
    {
        var packagePath = await BuildPackage("MSBuild.Sdk.PostgreSql");
        await PublishPackageToLocalFeed(packagePath);

        var version = GetPackageVersion(packagePath);

        ClearNuGetPackageCache("MSBuild.Sdk.PostgreSql");

        var projectDirectory = Path.Combine(_testWorkspace, "TargetPathRegression");
        Directory.CreateDirectory(projectDirectory);

        await CreateQuickStartNuGetConfigAsync(projectDirectory);
        await CreateReadmeQuickStartProjectAsync(projectDirectory, version);

        var projectFilePath = Path.Combine(projectDirectory, "MyDatabase.csproj");
        var projectContent = await File.ReadAllTextAsync(projectFilePath);
        projectContent = projectContent.Replace(
            "</Project>",
            """
              <Target Name="PrintResolvedOutputPaths">
                <Message Text="ResolvedTargetPath=$(TargetPath)" Importance="high" />
                <Message Text="ResolvedPgPacFilePath=$(PgPacFilePath)" Importance="high" />
              </Target>
            </Project>
            """);
        await File.WriteAllTextAsync(projectFilePath, projectContent);

        var buildResult = await RunDotNetCommand(projectDirectory, "build");
        Assert.Equal(0, buildResult.ExitCode);

        var inspectResult = await RunDotNetCommand(projectDirectory, "msbuild MyDatabase.csproj -target:PrintResolvedOutputPaths");
        Assert.Equal(0, inspectResult.ExitCode);
        Assert.Contains("ResolvedTargetPath=", inspectResult.Output, StringComparison.Ordinal);
        Assert.Contains("ResolvedPgPacFilePath=", inspectResult.Output, StringComparison.Ordinal);
        Assert.Contains("ResolvedTargetPath=" + Path.Combine(projectDirectory, "bin", "Debug", "net10.0", "MyDatabase.dll"), inspectResult.Output, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("ResolvedTargetPath=" + Path.Combine(projectDirectory, "bin", "Debug", "net10.0", "MyDatabase.pgpac"), inspectResult.Output, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("MyDatabase.pgpac", inspectResult.Output, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GlobalToolPackage_ReadmeQuickStartProject_CanCompileSuccessfully()
    {
        var toolPackagePath = await BuildPackage("postgresPacTools");
        await PublishPackageToLocalFeed(toolPackagePath);

        var toolVersion = GetPackageVersion(toolPackagePath);
        Assert.Equal(ReadmePackageVersion, toolVersion);

        var toolPath = await InstallToolAsync(toolVersion);

        var projectDirectory = Path.Combine(_testWorkspace, "MyDatabase");
        Directory.CreateDirectory(projectDirectory);
        await CreateReadmeQuickStartProjectAsync(projectDirectory, ReadmePackageVersion);

        var projectPath = Path.Combine(projectDirectory, "MyDatabase.csproj");
        var compileResult = await RunCommand(toolPath, $"compile --source-file \"{projectPath}\" --verbose", projectDirectory);

        Assert.Equal(0, compileResult.ExitCode);

        var packageOutputPath = Path.Combine(projectDirectory, "bin", "Debug", "net10.0", "MyDatabase.pgpac");
        Assert.True(File.Exists(packageOutputPath), $"Expected quick start compile output at {packageOutputPath}");
    }

    [Fact]
    public async Task GlobalToolPackage_ReadmeExtractExample_CanExtractAndBuildProject()
    {
        var sdkPackagePath = await BuildPackage("MSBuild.Sdk.PostgreSql");
        await PublishPackageToLocalFeed(sdkPackagePath);

        var toolPackagePath = await BuildPackage("postgresPacTools");
        await PublishPackageToLocalFeed(toolPackagePath);

        var sdkVersion = GetPackageVersion(sdkPackagePath);
        var toolVersion = GetPackageVersion(toolPackagePath);

        Assert.Equal(ReadmePackageVersion, sdkVersion);
        Assert.Equal(ReadmePackageVersion, toolVersion);

        var toolPath = await InstallToolAsync(toolVersion);

        await using var container = new PostgreSqlBuilder("postgres:16")
            .WithDatabase("mydb")
            .WithUsername("postgres")
            .WithPassword("postgres123")
            .WithCleanUp(true)
            .Build();

        await container.StartAsync();
        await SeedQuickStartDatabaseAsync(container.GetConnectionString());

        var outputDirectory = Path.Combine(_testWorkspace, "output", "mydb");
        Directory.CreateDirectory(outputDirectory);
        await CreateQuickStartNuGetConfigAsync(outputDirectory);

        var projectPath = Path.Combine(outputDirectory, "mydb.csproj");
        var extractResult = await RunCommand(
            toolPath,
            $"extract --source-connection-string \"{container.GetConnectionString()}\" --target-file \"{projectPath}\" --verbose",
            outputDirectory);

        Assert.Equal(0, extractResult.ExitCode);
        Assert.True(File.Exists(projectPath), $"Expected extracted project at {projectPath}");

        var projectContent = await File.ReadAllTextAsync(projectPath);
        Assert.Contains("<Project Sdk=\"Microsoft.NET.Sdk\">", projectContent, StringComparison.Ordinal);
        Assert.Contains($"<Sdk Name=\"MSBuild.Sdk.PostgreSql\" Version=\"{ReadmePackageVersion}\" />", projectContent, StringComparison.Ordinal);
        Assert.Contains("<PostgresVersion>16</PostgresVersion>", projectContent, StringComparison.Ordinal);
        Assert.Contains("<DefaultSchema>public</DefaultSchema>", projectContent, StringComparison.Ordinal);

        Assert.True(File.Exists(Path.Combine(outputDirectory, "public", "Tables", "users.sql")));
        Assert.True(File.Exists(Path.Combine(outputDirectory, "public", "Tables", "orders.sql")));

        ClearNuGetPackageCache("MSBuild.Sdk.PostgreSql");
        var buildResult = await RunDotNetCommand(outputDirectory, "build");

        Assert.Equal(0, buildResult.ExitCode);
        Assert.Contains("Build succeeded", buildResult.Output, StringComparison.OrdinalIgnoreCase);

        var expectedPackageOutputs = new[]
        {
            Path.Combine(outputDirectory, "bin", "Debug", "net10.0", "mydb.pgpac"),
            Path.Combine(outputDirectory, "mydb.pgpac")
        };

        Assert.True(expectedPackageOutputs.Any(File.Exists), $"Expected extracted project package output at one of: {string.Join(", ", expectedPackageOutputs)}");
    }

    [Fact]
    public async Task DacLibraryPackage_NativeLibrariesLoadCorrectly()
    {
        // Arrange: Build and publish the package
        var packagePath = await BuildPackage("mbulava.PostgreSql.Dac");
        await PublishPackageToLocalFeed(packagePath);
        var version = GetPackageVersion(packagePath);

        // Create a test project that actually uses the parser (which requires native libraries)
        var testProjectDir = Path.Combine(_testWorkspace, "TestNativeLibLoading");
        Directory.CreateDirectory(testProjectDir);

        // Create test code that uses Npgquery parser
        var testCode = @"
using mbulava.PostgreSql.Dac.Models;
using Npgquery;

// This will fail if native libraries are not properly included
try
{
    using var parser = new Parser(PostgreSqlVersion.Postgres16);
    var result = parser.Parse(""SELECT 1;"");
    
    if (result.IsSuccess)
    {
        Console.WriteLine(""Parser works! Native libraries loaded successfully."");
        return 0;
    }
    else
    {
        Console.WriteLine($""Parse failed: {result.Error}"");
        return 1;
    }
}
catch (DllNotFoundException ex)
{
    Console.WriteLine($""Native library not found: {ex.Message}"");
    return 2;
}
catch (Exception ex)
{
    Console.WriteLine($""Unexpected error: {ex.Message}"");
    return 3;
}
";

        // Act: Create, build, and run the test
        await CreateTestProject(testProjectDir, "mbulava.PostgreSql.Dac", version, includeTestCode: false, customCode: testCode);
        var buildResult = await RunDotNetCommand(testProjectDir, "build");
        Assert.Equal(0, buildResult.ExitCode);

        var runResult = await RunDotNetCommand(testProjectDir, "run");

        // Assert: Should successfully load native libraries and parse SQL
        Assert.Equal(0, runResult.ExitCode);
        Assert.Contains("Parser works! Native libraries loaded successfully", runResult.Output);
    }

    #region Helper Methods

    private string FindSolutionRoot([CallerFilePath] string? sourceFilePath = null)
    {
        // Since this project doesn't have a .sln file, we'll look for characteristic directories
        // like 'src', 'tests', 'scripts' which indicate the root

        // If we have the source file path, use that as a starting point
        if (!string.IsNullOrEmpty(sourceFilePath))
        {
            var currentDir = Path.GetDirectoryName(sourceFilePath);

            while (currentDir != null)
            {
                // Check if this directory has the typical structure (src, tests, scripts)
                if (Directory.Exists(Path.Combine(currentDir, "src")) &&
                    Directory.Exists(Path.Combine(currentDir, "tests")) &&
                    Directory.Exists(Path.Combine(currentDir, "scripts")))
                {
                    _output.WriteLine($"Solution root found (by structure): {currentDir}");
                    return currentDir;
                }
                currentDir = Directory.GetParent(currentDir)?.FullName;
            }
        }

        // Fallback: Start from the test assembly location
        var testAssemblyPath = typeof(NugetPackageValidationTests).Assembly.Location;
        var assemblyDir = Path.GetDirectoryName(testAssemblyPath);

        while (assemblyDir != null)
        {
            // Check if this directory has the typical structure
            if (Directory.Exists(Path.Combine(assemblyDir, "src")) &&
                Directory.Exists(Path.Combine(assemblyDir, "tests")) &&
                Directory.Exists(Path.Combine(assemblyDir, "scripts")))
            {
                _output.WriteLine($"Solution root found (by structure): {assemblyDir}");
                return assemblyDir;
            }
            assemblyDir = Directory.GetParent(assemblyDir)?.FullName;
        }

        throw new InvalidOperationException($"Could not find solution root directory. Test assembly location: {testAssemblyPath}, Source file: {sourceFilePath}");
    }

    private async Task<string> BuildPackage(string projectName)
    {
        var projectPath = projectName switch
        {
            "mbulava.PostgreSql.Dac" => Path.Combine(_solutionRoot, "src", "libs", "mbulava.PostgreSql.Dac"),
            "MSBuild.Sdk.PostgreSql" => Path.Combine(_solutionRoot, "src", "sdk", "MSBuild.Sdk.PostgreSql"),
            "postgresPacTools" => Path.Combine(_solutionRoot, "src", "postgresPacTools"),
            _ => throw new ArgumentException($"Unknown project: {projectName}")
        };

        _output.WriteLine($"Building package for: {projectPath}");
        var projectFilePath = Path.Combine(projectPath, $"{projectName}.csproj");

        // Restore first because these tests use an isolated NuGet package cache per run.
        var restoreResult = await RunDotNetCommand(projectPath, $"restore \"{projectFilePath}\"");
        Assert.Equal(0, restoreResult.ExitCode);

        // Clean previous builds
        var cleanResult = await RunDotNetCommand(projectPath, $"clean \"{projectFilePath}\" -c Release");
        Assert.Equal(0, cleanResult.ExitCode);

        var packOutputDirectory = Path.Combine(_testWorkspace, "packed", projectName);
        Directory.CreateDirectory(packOutputDirectory);

        // Build and pack in Release mode (don't use --no-build since we just cleaned)
        var packResult = await RunDotNetCommand(projectPath, $"pack \"{projectFilePath}\" -c Release -o \"{packOutputDirectory}\"");
        Assert.Equal(0, packResult.ExitCode);

        // Find the generated .nupkg file
        var nupkgFiles = Directory.GetFiles(packOutputDirectory, "*.nupkg", SearchOption.TopDirectoryOnly);
        
        Assert.NotEmpty(nupkgFiles);
        var packagePath = nupkgFiles.OrderByDescending(File.GetLastWriteTime).First();
        
        _output.WriteLine($"Package created: {packagePath}");
        return packagePath;
    }

    private async Task PublishPackageToLocalFeed(string packagePath)
    {
        var fileName = Path.GetFileName(packagePath);
        var destPath = Path.Combine(_localNugetFeed, fileName);
        
        File.Copy(packagePath, destPath, overwrite: true);
        _output.WriteLine($"Published package to local feed: {destPath}");
        
        await Task.CompletedTask;
    }

    private string GetPackageVersion(string packagePath)
    {
        using var reader = new PackageArchiveReader(packagePath);
        return reader.NuspecReader.GetVersion().ToString();
    }

    private async Task CreateTestProject(string projectDir, string packageName, string packageVersion, bool includeTestCode, string? customCode = null)
    {
        // Create new console project targeting net10.0
        var newResult = await RunDotNetCommand(projectDir, "new console --force --framework net10.0");
        Assert.Equal(0, newResult.ExitCode);

        // Create nuget.config to use both local feed and nuget.org (for dependencies like Npgquery)
        var nugetConfig = $@"<?xml version=""1.0"" encoding=""utf-8""?>
<configuration>
  <packageSources>
    <clear />
    <add key=""LocalFeed"" value=""{_localNugetFeed}"" />
    <add key=""nuget.org"" value=""https://api.nuget.org/v3/index.json"" />
  </packageSources>
</configuration>";

        await File.WriteAllTextAsync(Path.Combine(projectDir, "nuget.config"), nugetConfig);

        ClearNuGetPackageCache(packageName);

        // Add package reference
        var addResult = await RunDotNetCommand(
            projectDir,
            $"add package {packageName} --version {packageVersion}"
        );

        if (addResult.ExitCode != 0)
        {
            _output.WriteLine($"Failed to add package. Output:");
            _output.WriteLine(addResult.Output);
            Assert.Fail($"Failed to add package {packageName}. Exit code: {addResult.ExitCode}");
        }

        // Write test code
        var programPath = Path.Combine(projectDir, "Program.cs");
        
        string code;
        if (customCode != null)
        {
            code = customCode;
        }
        else if (includeTestCode)
        {
            code = @"
using mbulava.PostgreSql.Dac.Models;

var project = new PgProject { DatabaseName = ""TestDatabase"" };
Console.WriteLine($""Created project: {project.DatabaseName}"");
";
        }
        else
        {
            code = "Console.WriteLine(\"Hello, World!\");";
        }

        await File.WriteAllTextAsync(programPath, code);
    }

    private async Task CreateQuickStartNuGetConfigAsync(string projectDir)
    {
        var nugetConfig = $"""
            <?xml version="1.0" encoding="utf-8"?>
            <configuration>
              <packageSources>
                <clear />
                <add key="LocalFeed" value="{_localNugetFeed}" />
                <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
              </packageSources>
            </configuration>
            """;

        await File.WriteAllTextAsync(Path.Combine(projectDir, "nuget.config"), nugetConfig);
    }

    private async Task CreateReadmeQuickStartProjectAsync(string projectDir, string sdkVersion)
    {
        var projectFilePath = Path.Combine(projectDir, "MyDatabase.csproj");

        var projectContent = $"""
            <Project Sdk="Microsoft.NET.Sdk">

              <Sdk Name="MSBuild.Sdk.PostgreSql" Version="{sdkVersion}" />

              <PropertyGroup>
                <TargetFramework>net10.0</TargetFramework>
                <DatabaseName>MyDatabase</DatabaseName>
                <PostgresVersion>16</PostgresVersion>
                <DefaultSchema>public</DefaultSchema>
                <OutputFormat>pgpac</OutputFormat>
              </PropertyGroup>

            </Project>
            """;

        await File.WriteAllTextAsync(projectFilePath, projectContent);

        var tablesDirectory = Path.Combine(projectDir, "Tables");
        var viewsDirectory = Path.Combine(projectDir, "Views");
        var functionsDirectory = Path.Combine(projectDir, "Functions");

        Directory.CreateDirectory(tablesDirectory);
        Directory.CreateDirectory(viewsDirectory);
        Directory.CreateDirectory(functionsDirectory);

        await File.WriteAllTextAsync(
            Path.Combine(tablesDirectory, "Users.sql"),
            """
            CREATE TABLE public.users (
                id SERIAL PRIMARY KEY,
                username VARCHAR(50) NOT NULL,
                email VARCHAR(100) NOT NULL,
                created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
            );
            """);

        await File.WriteAllTextAsync(
            Path.Combine(tablesDirectory, "Orders.sql"),
            """
            CREATE TABLE public.orders (
                id SERIAL PRIMARY KEY,
                user_id INTEGER NOT NULL REFERENCES public.users(id),
                ordered_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
            );
            """);

        await File.WriteAllTextAsync(
            Path.Combine(viewsDirectory, "CustomerOrders.sql"),
            """
            CREATE VIEW public.customer_orders AS
            SELECT u.username, COUNT(o.id) AS order_count
            FROM users u
            LEFT JOIN orders o ON u.id = o.user_id
            GROUP BY u.username;
            """);

        await File.WriteAllTextAsync(
            Path.Combine(functionsDirectory, "GetActiveUsers.sql"),
            """
            CREATE FUNCTION public.get_active_users()
            RETURNS TABLE(id integer, username varchar)
            LANGUAGE sql
            AS $$
                SELECT u.id, u.username
                FROM public.users u;
            $$;
            """);
    }

    private async Task<string> InstallToolAsync(string version)
    {
        ClearNuGetPackageCache("postgresPacTools");

        var toolDirectory = Path.Combine(_testWorkspace, "tools");
        Directory.CreateDirectory(toolDirectory);

        var installResult = await RunDotNetCommand(
            _testWorkspace,
            $"tool install postgresPacTools --version {version} --tool-path \"{toolDirectory}\" --add-source \"{_localNugetFeed}\"");

        Assert.Equal(0, installResult.ExitCode);

        var toolPath = Path.Combine(toolDirectory, OperatingSystem.IsWindows() ? "pgpac.exe" : "pgpac");
        Assert.True(File.Exists(toolPath), $"Tool executable not found at: {toolPath}");

        return toolPath;
    }

    private static async Task SeedQuickStartDatabaseAsync(string connectionString)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = """
            CREATE TABLE public.users (
                id SERIAL PRIMARY KEY,
                username VARCHAR(50) NOT NULL,
                email VARCHAR(100) NOT NULL
            );

            CREATE TABLE public.orders (
                id SERIAL PRIMARY KEY,
                user_id INTEGER NOT NULL REFERENCES public.users(id)
            );
            """;

        await command.ExecuteNonQueryAsync();
    }

    private async Task<(int ExitCode, string Output)> RunDotNetCommand(string workingDirectory, string arguments)
    {
        return await RunCommand("dotnet", arguments, workingDirectory);
    }

    private void ClearNuGetPackageCache(string packageName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(packageName);

        var packageCachePath = Path.Combine(_nugetPackagesFolder, packageName.ToLowerInvariant());
        if (!Directory.Exists(packageCachePath))
        {
            return;
        }

        _output.WriteLine($"Clearing NuGet cache for {packageName}: {packageCachePath}");
        Directory.Delete(packageCachePath, recursive: true);
    }

    private async Task<(int ExitCode, string Output)> RunCommand(string command, string arguments, string? workingDirectory = null)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = command,
            Arguments = arguments,
            WorkingDirectory = workingDirectory ?? _testWorkspace,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        startInfo.Environment["NUGET_PACKAGES"] = _nugetPackagesFolder;

        _output.WriteLine($"Running: {command} {arguments}");
        _output.WriteLine($"Working directory: {startInfo.WorkingDirectory}");

        using var process = new Process { StartInfo = startInfo };
        var outputBuilder = new System.Text.StringBuilder();
        var errorBuilder = new System.Text.StringBuilder();

        process.OutputDataReceived += (_, e) =>
        {
            if (e.Data != null)
            {
                outputBuilder.AppendLine(e.Data);
                _output.WriteLine(e.Data);
            }
        };

        process.ErrorDataReceived += (_, e) =>
        {
            if (e.Data != null)
            {
                errorBuilder.AppendLine(e.Data);
                _output.WriteLine($"ERROR: {e.Data}");
            }
        };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        await process.WaitForExitAsync();

        var allOutput = outputBuilder.ToString() + errorBuilder.ToString();
        return (process.ExitCode, allOutput);
    }

    #endregion
}
