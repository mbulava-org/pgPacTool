namespace NugetPackage.Tests;

/// <summary>
/// Utility for checking Docker availability before running Testcontainers-based tests.
/// Call <see cref="SkipIfUnavailable"/> at the top of <c>IAsyncLifetime.InitializeAsync</c>
/// (or any individual test) so tests are gracefully skipped rather than failing with
/// <c>DockerUnavailableException</c> when Docker is not present in the environment.
/// </summary>
internal static class DockerAvailability
{
    private static bool? _isAvailable;

    /// <summary>
    /// Returns true if Docker appears to be available in this environment.
    /// Result is cached after the first check.
    /// </summary>
    public static bool IsAvailable
    {
        get
        {
            if (_isAvailable.HasValue)
                return _isAvailable.Value;

            // Respect explicit opt-out (useful in CI pipelines that lack DinD)
            var skip = Environment.GetEnvironmentVariable("SKIP_DOCKER_TESTS");
            if (!string.IsNullOrEmpty(skip) &&
                (skip == "1" || skip.Equals("true", StringComparison.OrdinalIgnoreCase)))
            {
                _isAvailable = false;
                return false;
            }

            _isAvailable = IsDockerSocketPresent();
            return _isAvailable.Value;
        }
    }

    /// <summary>
    /// Throws <see cref="Xunit.SkipException"/> if Docker is unavailable, causing the
    /// xunit test (or the entire fixture when called from <c>IAsyncLifetime.InitializeAsync</c>)
    /// to be skipped rather than failing with <c>DockerUnavailableException</c>.
    /// </summary>
    public static void SkipIfUnavailable()
    {
        if (!IsAvailable)
        {
            throw new Xunit.SkipException(
                "Docker is not available in this environment. " +
                "Install Docker Desktop (or enable DinD in CI) to run container-based tests.");
        }
    }

    // ------------------------------------------------------------------
    // Private helpers
    // ------------------------------------------------------------------

    private static bool IsDockerSocketPresent()
    {
        // Linux / macOS — default socket path
        if (OperatingSystem.IsLinux() || OperatingSystem.IsMacOS())
            return File.Exists("/var/run/docker.sock");

        // Windows — check Docker named pipe
        if (OperatingSystem.IsWindows())
            return File.Exists(@"\\.\pipe\docker_engine");

        return false;
    }
}
