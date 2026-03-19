using Npgquery;
using Xunit;
using Xunit.Abstractions;

namespace NpgqueryExtended.Tests;

/// <summary>
/// Tests for native library loading diagnostics
/// </summary>
public class NativeLibraryDiagnosticsTests
{
    private readonly ITestOutputHelper _output;

    public NativeLibraryDiagnosticsTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void PrintDiagnostics_ShowsSystemInfo()
    {
        // This test always runs and prints diagnostic info
        // Useful for troubleshooting CI/CD issues
        _output.WriteLine("=== Native Library Diagnostics ===");
        
        NativeLibraryLoader.PrintDiagnostics();
        
        // Also capture to test output
        foreach (var log in NativeLibraryLoader.DiagnosticLog)
        {
            _output.WriteLine(log);
        }
    }

    [Theory]
    [InlineData(PostgreSqlVersion.Postgres16)]
    [InlineData(PostgreSqlVersion.Postgres17)]
    public void LibraryHandle_CanLoad_ForSupportedVersions(PostgreSqlVersion version)
    {
        _output.WriteLine($"Testing library load for {version.ToVersionString()}");
        
        // This will throw if library cannot be loaded
        var handle = NativeLibraryLoader.GetLibraryHandle(version);
        
        Assert.NotEqual(IntPtr.Zero, handle);
        _output.WriteLine($"Successfully loaded library, handle: 0x{handle:X}");
        
        // Print diagnostic log for this load attempt
        _output.WriteLine("\nDiagnostic log:");
        foreach (var log in NativeLibraryLoader.DiagnosticLog.TakeLast(20))
        {
            _output.WriteLine(log);
        }
    }

    [Fact]
    public void GetAvailableVersions_ReturnsAtLeastOne()
    {
        var available = NativeLibraryLoader.GetAvailableVersions().ToList();
        
        _output.WriteLine($"Available versions: {available.Count}");
        foreach (var version in available)
        {
            _output.WriteLine($"  - {version.ToVersionString()}");
        }
        
        Assert.NotEmpty(available);
        
        // Should have at least PG 16 or 17
        Assert.Contains(available, v => 
            v == PostgreSqlVersion.Postgres16 || v == PostgreSqlVersion.Postgres17);
    }
}
