using System.Diagnostics;
using System.IO.Compression;
using System.Runtime.CompilerServices;
using System.Xml.Linq;
using NuGet.Packaging;
using Xunit;
using Xunit.Abstractions;

namespace NugetPackage.Tests;

/// <summary>
/// Tests to ensure NuGet packages are properly constructed with all required dependencies.
/// These tests prevent missing references in pgpac tool and mbulava.PostgreSql.Dac library.
/// </summary>
public class NugetPackageValidationTests : IDisposable
{
    private readonly ITestOutputHelper _output;
    private readonly string _solutionRoot;
    private readonly string _testWorkspace;
    private readonly string _localNugetFeed;

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
        
        _output.WriteLine($"Solution root: {_solutionRoot}");
        _output.WriteLine($"Test workspace: {_testWorkspace}");
        _output.WriteLine($"Local NuGet feed: {_localNugetFeed}");
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
        var toolPath = Path.Combine(_testWorkspace, "tools", "pgpac");
        if (OperatingSystem.IsWindows())
        {
            toolPath += ".exe";
        }

        Assert.True(File.Exists(toolPath), $"Tool executable not found at: {toolPath}");

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
            "postgresPacTools" => Path.Combine(_solutionRoot, "src", "postgresPacTools"),
            _ => throw new ArgumentException($"Unknown project: {projectName}")
        };

        _output.WriteLine($"Building package for: {projectPath}");

        // Clean previous builds
        var cleanResult = await RunDotNetCommand(projectPath, "clean -c Release");
        Assert.Equal(0, cleanResult.ExitCode);

        // Build and pack in Release mode (don't use --no-build since we just cleaned)
        var packResult = await RunDotNetCommand(projectPath, "pack -c Release");
        Assert.Equal(0, packResult.ExitCode);

        // Find the generated .nupkg file
        var binPath = Path.Combine(projectPath, "bin", "Release");
        var nupkgFiles = Directory.GetFiles(binPath, "*.nupkg", SearchOption.TopDirectoryOnly);
        
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

    private async Task<(int ExitCode, string Output)> RunDotNetCommand(string workingDirectory, string arguments)
    {
        return await RunCommand("dotnet", arguments, workingDirectory);
    }

    private void ClearNuGetPackageCache(string packageName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(packageName);

        var globalPackagesPath = Environment.GetEnvironmentVariable("NUGET_PACKAGES");
        if (string.IsNullOrWhiteSpace(globalPackagesPath))
        {
            var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            if (string.IsNullOrWhiteSpace(userProfile))
            {
                throw new InvalidOperationException("Unable to determine the NuGet global packages folder.");
            }

            globalPackagesPath = Path.Combine(userProfile, ".nuget", "packages");
        }

        var packageCachePath = Path.Combine(globalPackagesPath, packageName.ToLowerInvariant());
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
