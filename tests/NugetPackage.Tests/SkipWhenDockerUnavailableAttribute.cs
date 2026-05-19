using Xunit;

namespace NugetPackage.Tests;

/// <summary>
/// Drop-in replacement for <see cref="FactAttribute"/> that marks the test as skipped
/// (not failed) when Docker is unavailable in the current environment.
///
/// Usage: replace <c>[Fact]</c> with <c>[FactRequiresDocker]</c> on any test that
/// starts a Testcontainers container.
///
/// Unlike throwing <see cref="Xunit.Sdk.SkipException"/> at runtime (which is an
/// internal xUnit v3 protocol not reliably surfaced by the VSTest adapter in xUnit v2),
/// this approach sets the static <see cref="FactAttribute.Skip"/> reason at construction
/// time, which xUnit v2 always renders as a proper "Skipped" result.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public sealed class FactRequiresDockerAttribute : FactAttribute
{
    public FactRequiresDockerAttribute()
    {
        if (!DockerAvailability.IsAvailable)
        {
            Skip = "Docker is not available in this environment. " +
                   "Install Docker Desktop (or enable DinD in CI) to run container-based tests.";
        }
    }
}
