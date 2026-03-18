using System.Collections.Concurrent;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Loader;

namespace Npgquery;

/// <summary>
/// Manages loading and caching of version-specific PostgreSQL query parser native libraries
/// with comprehensive diagnostics for troubleshooting
/// </summary>
public static class NativeLibraryLoader
{
    private static readonly ConcurrentDictionary<PostgreSqlVersion, IntPtr> _loadedLibraries = new();
    private static readonly ConcurrentDictionary<PostgreSqlVersion, bool> _availabilityCache = new();
    private static readonly object _loadLock = new();
    private static readonly List<string> _diagnosticLog = [];
    private static bool _resolverInitialized = false;

    /// <summary>
    /// Gets diagnostic information about native library loading attempts
    /// </summary>
    public static IReadOnlyList<string> DiagnosticLog => _diagnosticLog.AsReadOnly();

    /// <summary>
    /// Enables or disables diagnostic logging to console (default: enabled in Debug, disabled in Release)
    /// </summary>
    public static bool EnableConsoleLogging { get; set; } =
#if DEBUG
        true;
#else
        false;
#endif

    static NativeLibraryLoader()
    {
        InitializeDllImportResolver();
    }

    /// <summary>
    /// Initializes the DllImport resolver for explicit native library loading control
    /// </summary>
    private static void InitializeDllImportResolver()
    {
        if (_resolverInitialized)
            return;

        lock (_loadLock)
        {
            if (_resolverInitialized)
                return;

            LogDiagnostic("=== Initializing Native Library Resolver ===");
            LogDiagnostic($"OS: {RuntimeInformation.OSDescription}");
            LogDiagnostic($"Platform: {RuntimeInformation.OSArchitecture}");
            LogDiagnostic($"Process Architecture: {RuntimeInformation.ProcessArchitecture}");
            LogDiagnostic($"Framework: {RuntimeInformation.FrameworkDescription}");
            LogDiagnostic($"Base Directory: {AppContext.BaseDirectory}");
            LogDiagnostic($"Runtime Identifier: {GetRuntimeIdentifier()}");

            // Set up the DllImport resolver for the Npgquery assembly
            NativeLibrary.SetDllImportResolver(
                typeof(NativeLibraryLoader).Assembly,
                DllImportResolver);

            _resolverInitialized = true;
            LogDiagnostic("DllImport resolver initialized successfully");
        }
    }

    /// <summary>
    /// Custom DllImport resolver to handle version-specific native library loading
    /// </summary>
    private static IntPtr DllImportResolver(string libraryName, Assembly assembly, DllImportSearchPath? searchPath)
    {
        LogDiagnostic($"DllImport resolver called for: {libraryName}");

        // We only handle our own native libraries (libpg_query variants)
        if (!libraryName.StartsWith("libpg_query", StringComparison.OrdinalIgnoreCase) &&
            !libraryName.StartsWith("pg_query", StringComparison.OrdinalIgnoreCase))
        {
            LogDiagnostic($"Not a pg_query library, using default resolution");
            return IntPtr.Zero; // Use default resolution
        }

        // For now, we'll use the default version (this is called from old-style P/Invoke declarations)
        // The version-specific loading happens through GetLibraryHandle
        LogDiagnostic($"Resolving pg_query library with default version");

        try
        {
            return GetLibraryHandle(PostgreSqlVersion.Postgres16);
        }
        catch (Exception ex)
        {
            LogDiagnostic($"ERROR: Failed to resolve library: {ex.Message}");
            return IntPtr.Zero;
        }
    }

    /// <summary>
    /// Logs diagnostic information
    /// </summary>
    private static void LogDiagnostic(string message)
    {
        var timestamped = $"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss.fff}] {message}";
        _diagnosticLog.Add(timestamped);

        if (EnableConsoleLogging)
        {
            Console.WriteLine($"[NativeLibraryLoader] {timestamped}");
        }

        Debug.WriteLine($"[NativeLibraryLoader] {timestamped}");
    }

    /// <summary>
    /// Gets the native library handle for a specific PostgreSQL version
    /// </summary>
    /// <param name="version">The PostgreSQL version</param>
    /// <returns>Native library handle</returns>
    /// <exception cref="PostgreSqlVersionNotAvailableException">Thrown when the requested version is not available</exception>
    public static IntPtr GetLibraryHandle(PostgreSqlVersion version)
    {
        LogDiagnostic($"=== GetLibraryHandle called for {version.ToVersionString()} ===");

        // Check cache first
        if (_loadedLibraries.TryGetValue(version, out var handle))
        {
            LogDiagnostic($"Library already loaded from cache: 0x{handle:X}");
            return handle;
        }

        lock (_loadLock)
        {
            // Double-check after acquiring lock
            if (_loadedLibraries.TryGetValue(version, out handle))
            {
                LogDiagnostic($"Library loaded by another thread, using cached handle: 0x{handle:X}");
                return handle;
            }

            // Try to load the library
            var libraryName = GetLibraryName(version);
            LogDiagnostic($"Library name: {libraryName}");

            // Try standard loading first
            if (NativeLibrary.TryLoad(libraryName, out handle))
            {
                LogDiagnostic($"SUCCESS: Loaded library using standard name: 0x{handle:X}");
            }
            else
            {
                LogDiagnostic($"Standard name failed, trying search paths...");
                // Try with platform-specific search paths
                handle = TryLoadWithSearchPaths(libraryName, version);
            }

            if (handle == IntPtr.Zero)
            {
                var availableVersions = GetAvailableVersions();
                var errorMessage = $"Could not load native library for {version.ToVersionString()}. " +
                    $"Tried library name: {libraryName}. " +
                    $"Available versions: {string.Join(", ", availableVersions.Select(v => v.ToVersionString()))}";

                LogDiagnostic($"FATAL ERROR: {errorMessage}");
                LogDiagnostic("=== Diagnostic Log Dump ===");
                foreach (var log in _diagnosticLog.TakeLast(50))
                {
                    Console.WriteLine(log);
                }

                throw new PostgreSqlVersionNotAvailableException(
                    version,
                    availableVersions,
                    errorMessage);
            }

            _loadedLibraries[version] = handle;
            _availabilityCache[version] = true;

            LogDiagnostic($"Library successfully loaded and cached: 0x{handle:X}");
            return handle;
        }
    }

    /// <summary>
    /// Checks if a specific PostgreSQL version is available
    /// </summary>
    /// <param name="version">The PostgreSQL version to check</param>
    /// <returns>True if the version is available, false otherwise</returns>
    public static bool IsVersionAvailable(PostgreSqlVersion version)
    {
        if (_availabilityCache.TryGetValue(version, out var isAvailable))
        {
            return isAvailable;
        }

        var libraryName = GetLibraryName(version);
        IntPtr handle;

        if (!NativeLibrary.TryLoad(libraryName, out handle))
        {
            // Try with platform-specific search paths
            handle = TryLoadWithSearchPaths(libraryName, version);
        }

        var available = handle != IntPtr.Zero;

        if (available)
        {
            _loadedLibraries[version] = handle;
        }

        _availabilityCache[version] = available;
        return available;
    }

    /// <summary>
    /// Gets all available PostgreSQL versions
    /// </summary>
    /// <returns>Enumerable of available versions</returns>
    public static IEnumerable<PostgreSqlVersion> GetAvailableVersions()
    {
        var versions = new List<PostgreSqlVersion>();

        foreach (PostgreSqlVersion version in Enum.GetValues(typeof(PostgreSqlVersion)))
        {
            if (IsVersionAvailable(version))
            {
                versions.Add(version);
            }
        }

        return versions;
    }

    /// <summary>
    /// Unloads all loaded native libraries. Primarily for testing purposes.
    /// </summary>
    internal static void UnloadAll()
    {
        lock (_loadLock)
        {
            foreach (var kvp in _loadedLibraries)
            {
                if (kvp.Value != IntPtr.Zero)
                {
                    try
                    {
                        NativeLibrary.Free(kvp.Value);
                    }
                    catch
                    {
                        // Ignore errors during cleanup
                    }
                }
            }

            _loadedLibraries.Clear();
            _availabilityCache.Clear();
        }
    }

    /// <summary>
    /// Prints comprehensive diagnostic information to console
    /// Useful for troubleshooting native library loading issues
    /// </summary>
    public static void PrintDiagnostics()
    {
        Console.WriteLine("=== Npgquery Native Library Diagnostics ===");
        Console.WriteLine($"OS: {RuntimeInformation.OSDescription}");
        Console.WriteLine($"Platform: {RuntimeInformation.OSArchitecture}");
        Console.WriteLine($"Process Architecture: {RuntimeInformation.ProcessArchitecture}");
        Console.WriteLine($"Framework: {RuntimeInformation.FrameworkDescription}");
        Console.WriteLine($"Base Directory: {AppContext.BaseDirectory}");
        Console.WriteLine($"Runtime Identifier: {GetRuntimeIdentifier()}");
        Console.WriteLine();

        Console.WriteLine("Loaded Libraries:");
        foreach (var kvp in _loadedLibraries)
        {
            Console.WriteLine($"  {kvp.Key.ToVersionString()}: 0x{kvp.Value:X}");
        }
        Console.WriteLine();

        Console.WriteLine("Available Versions:");
        var available = GetAvailableVersions().ToList();
        if (available.Count == 0)
        {
            Console.WriteLine("  WARNING: No versions available!");
        }
        else
        {
            foreach (var version in available)
            {
                Console.WriteLine($"  ✓ {version.ToVersionString()}");
            }
        }
        Console.WriteLine();

        Console.WriteLine("Diagnostic Log (last 50 entries):");
        foreach (var log in _diagnosticLog.TakeLast(50))
        {
            Console.WriteLine($"  {log}");
        }
        Console.WriteLine("===========================================");
    }

    /// <summary>
    /// Gets the platform-specific library name for a PostgreSQL version
    /// </summary>
    private static string GetLibraryName(PostgreSqlVersion version)
    {
        var suffix = version.ToLibrarySuffix();

        // All platforms use the same libpg_query_{version} naming convention
        return $"libpg_query_{suffix}";
    }

    /// <summary>
    /// Attempts to load library with various search paths
    /// </summary>
    private static IntPtr TryLoadWithSearchPaths(string libraryName, PostgreSqlVersion version)
    {
        var searchPaths = GetSearchPaths(version).ToList();
        LogDiagnostic($"Attempting to load from {searchPaths.Count} search paths:");

        foreach (var path in searchPaths)
        {
            LogDiagnostic($"  Trying: {path}");
            LogDiagnostic($"  Exists: {File.Exists(path)}");

            if (File.Exists(path))
            {
                try
                {
                    if (NativeLibrary.TryLoad(path, out var handle))
                    {
                        LogDiagnostic($"  SUCCESS: Loaded from {path}, handle: 0x{handle:X}");
                        return handle;
                    }
                    else
                    {
                        LogDiagnostic($"  FAILED: TryLoad returned false for {path}");
                    }
                }
                catch (Exception ex)
                {
                    LogDiagnostic($"  EXCEPTION: {ex.GetType().Name}: {ex.Message}");
                }
            }
            else
            {
                LogDiagnostic($"  SKIPPED: File does not exist");
            }
        }

        LogDiagnostic("All search paths exhausted without success");
        return IntPtr.Zero;
    }

    /// <summary>
    /// Gets potential search paths for the native library
    /// </summary>
    private static IEnumerable<string> GetSearchPaths(PostgreSqlVersion version)
    {
        var suffix = version.ToLibrarySuffix();
        var baseDir = AppContext.BaseDirectory;

        LogDiagnostic($"Generating search paths for version {version.ToVersionString()}, suffix: {suffix}");
        LogDiagnostic($"Base directory: {baseDir}");

        string extension;

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            extension = "dll";
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            extension = "so";
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            extension = "dylib";
        }
        else
        {
            LogDiagnostic("WARNING: Unknown platform, no extension determined");
            yield break;
        }

        LogDiagnostic($"Platform extension: {extension}");

        // All platforms use libpg_query_{version}.{extension}
        var libraryFileName = $"libpg_query_{suffix}.{extension}";
        LogDiagnostic($"Library file name: {libraryFileName}");

        // Strategy 1: Runtime-specific directory (STANDARD .NET APPROACH - PRIORITY)
        var rid = GetRuntimeIdentifier();
        if (!string.IsNullOrEmpty(rid))
        {
            var runtimePath = Path.Combine(baseDir, "runtimes", rid, "native", libraryFileName);
            LogDiagnostic($"Runtime-specific path (RID: {rid}): {runtimePath}");
            yield return runtimePath;
        }

        // Strategy 2: Base directory (fallback for compatibility)
        var basePath = Path.Combine(baseDir, libraryFileName);
        LogDiagnostic($"Base directory path: {basePath}");
        yield return basePath;
    }

    /// <summary>
    /// Gets the runtime identifier for the current platform
    /// </summary>
    private static string GetRuntimeIdentifier()
    {
        string os;
        string arch;

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            os = "win";
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            os = "linux";
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            os = "osx";
        }
        else
        {
            return string.Empty;
        }

        arch = RuntimeInformation.ProcessArchitecture switch
        {
            Architecture.X64 => "x64",
            Architecture.X86 => "x86",
            Architecture.Arm64 => "arm64",
            Architecture.Arm => "arm",
            _ => string.Empty
        };

        return $"{os}-{arch}";
    }
}
