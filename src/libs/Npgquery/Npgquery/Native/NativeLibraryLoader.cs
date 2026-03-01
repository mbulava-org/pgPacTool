using System.Reflection;
using System.Runtime.InteropServices;

namespace Npgquery.Native;

/// <summary>
/// Handles loading of native pg_query library with platform-specific resolution
/// </summary>
internal static class NativeLibraryLoader
{
    private static bool _resolverRegistered;
    private static readonly object _lock = new();

    /// <summary>
    /// Register the DLL import resolver for the pg_query native library
    /// </summary>
    internal static void EnsureLoaded()
    {
        if (_resolverRegistered)
            return;

        lock (_lock)
        {
            if (_resolverRegistered)
                return;

            NativeLibrary.SetDllImportResolver(typeof(NativeMethods).Assembly, DllImportResolver);
            _resolverRegistered = true;
        }
    }

    private static IntPtr DllImportResolver(string libraryName, Assembly assembly, DllImportSearchPath? searchPath)
    {
        // Only handle pg_query library
        if (libraryName != "pg_query")
            return IntPtr.Zero;

        // Try to load from runtime-specific directory
        var runtimePath = GetRuntimeSpecificPath();
        if (runtimePath != null && NativeLibrary.TryLoad(runtimePath, out var handle))
            return handle;

        // Fallback to default loading
        if (NativeLibrary.TryLoad(libraryName, assembly, searchPath, out handle))
            return handle;

        throw new DllNotFoundException(
            $"Unable to load native library '{libraryName}'. " +
            $"Please ensure the native library is present in the runtimes directory. " +
            $"Expected path: {runtimePath}");
    }

    private static string? GetRuntimeSpecificPath()
    {
        var assemblyLocation = typeof(NativeMethods).Assembly.Location;
        if (string.IsNullOrEmpty(assemblyLocation))
            return null;

        var assemblyDir = Path.GetDirectoryName(assemblyLocation);
        if (string.IsNullOrEmpty(assemblyDir))
            return null;

        var rid = GetRuntimeIdentifier();

        // Try both naming conventions for the native library
        var nativeLibNames = GetNativeLibraryNames();

        foreach (var nativeLibName in nativeLibNames)
        {
            // Try runtimes/{rid}/native/{lib} structure in assembly directory
            var runtimePath = Path.Combine(assemblyDir, "runtimes", rid, "native", nativeLibName);
            if (File.Exists(runtimePath))
                return runtimePath;

            // Try output root directory (for development/testing)
            var rootPath = Path.Combine(assemblyDir, nativeLibName);
            if (File.Exists(rootPath))
                return rootPath;

            // For test projects, check if we need to look in the bin directory where Npgquery.dll is located
            // This handles the case where the test assembly and Npgquery.dll are in the same directory
            // but the runtimes folder might be next to Npgquery.dll
            var npgqueryDir = assemblyDir; // Same directory in this case
            var testRuntimePath = Path.Combine(npgqueryDir, "runtimes", rid, "native", nativeLibName);
            if (File.Exists(testRuntimePath))
                return testRuntimePath;
        }

        return null;
    }

    private static string GetRuntimeIdentifier()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return RuntimeInformation.ProcessArchitecture switch
            {
                Architecture.X64 => "win-x64",
                Architecture.Arm64 => "win-arm64",
                _ => "win-x64"
            };
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            return RuntimeInformation.ProcessArchitecture switch
            {
                Architecture.X64 => "linux-x64",
                Architecture.Arm64 => "linux-arm64",
                _ => "linux-x64"
            };
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            return RuntimeInformation.ProcessArchitecture switch
            {
                Architecture.X64 => "osx-x64",
                Architecture.Arm64 => "osx-arm64",
                _ => "osx-x64"
            };
        }

        return "unknown";
    }

    private static string GetNativeLibraryName()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return "pg_query.dll";

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            return "libpg_query.so";

        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            return "libpg_query.dylib";

        return "pg_query";
    }

    /// <summary>
    /// Get all possible native library names to try (handles different naming conventions)
    /// </summary>
    private static string[] GetNativeLibraryNames()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return new[] { "pg_query.dll" };

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            return new[] { "libpg_query.so", "pg_query.so" }; // Try both naming conventions

        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            return new[] { "libpg_query.dylib", "pg_query.dylib" }; // Try both naming conventions

        return new[] { "pg_query" };
    }
}
