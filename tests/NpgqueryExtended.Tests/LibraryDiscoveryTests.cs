using Xunit;
using Xunit.Abstractions;
using System.Runtime.InteropServices;

namespace NpgqueryExtended.Tests;

/// <summary>
/// Tests to diagnose native library discovery and path issues
/// </summary>
public class LibraryDiscoveryTests
{
    private readonly ITestOutputHelper _output;

    public LibraryDiscoveryTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void PrintAllPaths()
    {
        _output.WriteLine($"BaseDirectory: {AppContext.BaseDirectory}");
        _output.WriteLine($"Current Directory: {Directory.GetCurrentDirectory()}");
        _output.WriteLine($"RuntimeIdentifier: {RuntimeInformation.RuntimeIdentifier}");
        _output.WriteLine($"OS: {RuntimeInformation.OSDescription}");
        _output.WriteLine($"Architecture: {RuntimeInformation.ProcessArchitecture}");

        var baseDir = AppContext.BaseDirectory;
        
        // List all DLL/SO/DYLIB files
        var patterns = new[] { "*.dll", "*.so", "*.dylib" };
        foreach (var pattern in patterns)
        {
            _output.WriteLine($"\n--- Files matching {pattern} ---");
            foreach (var file in Directory.GetFiles(baseDir, pattern, SearchOption.AllDirectories))
            {
                _output.WriteLine(file);
            }
        }

        // Check runtime directories
        var runtimeDir = Path.Combine(baseDir, "runtimes");
        if (Directory.Exists(runtimeDir))
        {
            _output.WriteLine($"\n--- Runtime directories ---");
            foreach (var dir in Directory.GetDirectories(runtimeDir, "*", SearchOption.AllDirectories))
            {
                _output.WriteLine(dir);
            }
        }
    }

    [Fact]
    public void TestDirectLoad()
    {
        var baseDir = AppContext.BaseDirectory;
        var libraryFileName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? "libpg_query_16.dll"
            : RuntimeInformation.IsOSPlatform(OSPlatform.Linux)
                ? "libpg_query_16.so"
                : "libpg_query_16.dylib";

        var runtimePath = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? Path.Combine(baseDir, "runtimes", "win-x64", "native", libraryFileName)
            : RuntimeInformation.IsOSPlatform(OSPlatform.Linux)
                ? Path.Combine(baseDir, "runtimes", "linux-x64", "native", libraryFileName)
                : Path.Combine(baseDir, "runtimes", "osx-arm64", "native", libraryFileName);

        var paths = new[]
        {
            Path.Combine(baseDir, libraryFileName),
            runtimePath,
        };

        foreach (var path in paths)
        {
            var exists = File.Exists(path);
            _output.WriteLine($"Path: {path}");
            _output.WriteLine($"  Exists: {exists}");
            
            if (exists)
            {
                if (NativeLibrary.TryLoad(path, out var handle))
                {
                    _output.WriteLine($"  Load SUCCESS! Handle: 0x{handle:X}");
                    NativeLibrary.Free(handle);
                }
                else
                {
                    _output.WriteLine("  Load FAILED!");
                }
            }
            _output.WriteLine("");
        }
    }

    [Fact]
    public void TestNativeLibraryTryLoad()
    {
        var names = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? new[]
            {
                "libpg_query_16",
                "libpg_query_16.dll",
                "pg_query_16",
                "pg_query_16.dll"
            }
            : RuntimeInformation.IsOSPlatform(OSPlatform.Linux)
                ? new[]
                {
                    "libpg_query_16",
                    "libpg_query_16.so",
                    "pg_query_16",
                    "pg_query_16.so"
                }
                : new[]
                {
                    "libpg_query_16",
                    "libpg_query_16.dylib",
                    "pg_query_16",
                    "pg_query_16.dylib"
                };

        foreach (var name in names)
        {
            if (NativeLibrary.TryLoad(name, out var handle))
            {
                _output.WriteLine($"SUCCESS: '{name}' loaded with handle 0x{handle:X}");
                NativeLibrary.Free(handle);
            }
            else
            {
                _output.WriteLine($"FAILED: '{name}' could not be loaded");
            }
        }
    }
}
