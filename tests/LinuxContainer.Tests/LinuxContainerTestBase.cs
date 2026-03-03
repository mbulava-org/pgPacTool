using Docker.DotNet;
using System.Text;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;

namespace LinuxContainer.Tests;

/// <summary>
/// Base class for Linux container tests with common functionality.
/// </summary>
public abstract class LinuxContainerTestBase
{
    private DockerClient? _dockerClient;
    protected bool IsDockerAvailable { get; private set; }

    [OneTimeSetUp]
    public async Task BaseOneTimeSetUp()
    {
        // Check if Docker is available
        try
        {
            _dockerClient = new DockerClientConfiguration().CreateClient();
            await _dockerClient.System.PingAsync();
            IsDockerAvailable = true;
            TestContext.WriteLine("✅ Docker is available");
        }
        catch (Exception ex)
        {
            IsDockerAvailable = false;
            TestContext.WriteLine($"⚠️  Docker is not available: {ex.Message}");
            TestContext.WriteLine("Skipping Linux container tests. Install Docker Desktop to run these tests.");
        }
    }

    [OneTimeTearDown]
    public void BaseOneTimeTearDown()
    {
        _dockerClient?.Dispose();
    }

    /// <summary>
    /// Runs a bash script in a Linux container
    /// </summary>
    protected async Task<TestResult> RunScriptInLinuxContainerAsync(string name, string scriptContent)
    {
        var solutionRoot = GetSolutionRoot();
        var output = new StringBuilder();
        var exitCode = -1;

        try
        {
            // Normalize line endings to Unix (LF) for Linux bash compatibility
            // Windows uses CRLF (\r\n) by default, but Linux bash requires LF (\n)
            var normalizedScript = scriptContent.Replace("\r\n", "\n").Replace("\r", "\n");

            // Create container
            var container = new ContainerBuilder()
                .WithImage("mcr.microsoft.com/dotnet/sdk:10.0")
                .WithName($"pgpactool-linux-test-{name}-{Guid.NewGuid():N}"[..63]) // Docker name limit
                .WithBindMount(solutionRoot, "/workspace")
                .WithWorkingDirectory("/workspace")
                .WithCommand("/bin/bash", "-c", normalizedScript)
                .WithCleanUp(true)
                .Build();

            // Start container (Name is only available after StartAsync in 4.x)
            TestContext.WriteLine($"🐳 Starting Linux container...");
            await container.StartAsync();

            TestContext.WriteLine($"   Container: {container.Name}");
            TestContext.WriteLine("⏳ Container completed, collecting results...");

            // Get exit code (available after container stops)
            exitCode = (int)await container.GetExitCodeAsync();

            // Get logs (must be called before disposal)
            var logs = await GetContainerLogsAsync(container);
            output.AppendLine(logs);

            TestContext.WriteLine($"📄 Container output:");
            TestContext.WriteLine(logs);
            TestContext.WriteLine($"🏁 Container exit code: {exitCode}");

            // Cleanup
            await container.StopAsync();
            await container.DisposeAsync();
        }
        catch (Exception ex)
        {
            output.AppendLine($"ERROR: {ex.Message}");
            output.AppendLine(ex.StackTrace);
            TestContext.WriteLine($"❌ Error running container: {ex.Message}");
            throw;
        }

        return new TestResult
        {
            ExitCode = exitCode,
            Output = output.ToString()
        };
    }

    /// <summary>
    /// Gets container logs (stdout + stderr)
    /// </summary>
    private async Task<string> GetContainerLogsAsync(IContainer container)
    {
        var logs = new StringBuilder();

        var (stdout, stderr) = await container.GetLogsAsync(DateTime.MinValue, DateTime.MaxValue);

        logs.AppendLine(stdout);

        if (!string.IsNullOrEmpty(stderr))
        {
            logs.AppendLine("=== STDERR ===");
            logs.AppendLine(stderr);
        }

        return logs.ToString();
    }

    /// <summary>
    /// Gets the solution root directory
    /// </summary>
    protected string GetSolutionRoot()
    {
        var currentDir = Directory.GetCurrentDirectory();
        var dir = new DirectoryInfo(currentDir);

        // Walk up until we find the solution file
        while (dir != null && !Directory.GetFiles(dir.FullName, "*.slnx").Any())
        {
            dir = dir.Parent;
        }

        if (dir == null)
        {
            throw new InvalidOperationException("Could not find solution root directory");
        }

        return dir.FullName;
    }

    protected class TestResult
    {
        public int ExitCode { get; set; }
        public string Output { get; set; } = string.Empty;
    }
}
