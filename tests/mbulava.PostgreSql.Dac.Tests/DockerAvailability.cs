using NUnit.Framework;

namespace mbulava.PostgreSql.Dac.Tests;

/// <summary>
/// Utility for checking Docker availability before running Testcontainers-based tests.
/// Call SkipIfUnavailable() at the top of any [SetUp] or [OneTimeSetUp] that launches a
/// container so tests are gracefully ignored rather than failing with
/// DockerUnavailableException when Docker is not present.
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
    /// Calls Assert.Ignore() if Docker is unavailable, causing the test (or the entire
    /// fixture when called from [OneTimeSetUp]) to be skipped rather than failing with
    /// DockerUnavailableException.
    /// </summary>
    public static void SkipIfUnavailable()
    {
        if (!IsAvailable)
        {
            Assert.Ignore(
                "Docker is not available in this environment. " +
                "Install Docker Desktop (or enable DinD in CI) to run container-based tests.");
        }
    }

    // ------------------------------------------------------------------
    // Private helpers
    // ------------------------------------------------------------------

    private static bool IsDockerSocketPresent()
    {
        if (OperatingSystem.IsLinux() || OperatingSystem.IsMacOS())
            return File.Exists("/var/run/docker.sock");

        if (OperatingSystem.IsWindows())
            return File.Exists(@"\\.\pipe\docker_engine");

        return false;
    }
}
