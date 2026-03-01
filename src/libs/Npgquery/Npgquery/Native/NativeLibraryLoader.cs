using System.Collections.Concurrent;
using System.Runtime.InteropServices;

namespace Npgquery;

/// <summary>
/// Manages loading and caching of version-specific PostgreSQL query parser native libraries
/// </summary>
public static class NativeLibraryLoader
{
    private static readonly ConcurrentDictionary<PostgreSqlVersion, IntPtr> _loadedLibraries = new();
    private static readonly ConcurrentDictionary<PostgreSqlVersion, bool> _availabilityCache = new();
    private static readonly object _loadLock = new();

    /// <summary>
    /// Gets the native library handle for a specific PostgreSQL version
    /// </summary>
    /// <param name="version">The PostgreSQL version</param>
    /// <returns>Native library handle</returns>
    /// <exception cref="PostgreSqlVersionNotAvailableException">Thrown when the requested version is not available</exception>
    public static IntPtr GetLibraryHandle(PostgreSqlVersion version)
    {
        // Check cache first
        if (_loadedLibraries.TryGetValue(version, out var handle))
        {
            return handle;
        }

        lock (_loadLock)
        {
            // Double-check after acquiring lock
            if (_loadedLibraries.TryGetValue(version, out handle))
            {
                return handle;
            }

            // Try to load the library
            var libraryName = GetLibraryName(version);

            if (!NativeLibrary.TryLoad(libraryName, out handle))
            {
                // Try with platform-specific search paths
                handle = TryLoadWithSearchPaths(libraryName, version);
            }

            if (handle == IntPtr.Zero)
            {
                var availableVersions = GetAvailableVersions();
                throw new PostgreSqlVersionNotAvailableException(
                    version, 
                    availableVersions,
                    $"Could not load native library for {version.ToVersionString()}. " +
                    $"Tried library name: {libraryName}. " +
                    $"Available versions: {string.Join(", ", availableVersions.Select(v => v.ToVersionString()))}");
            }

            _loadedLibraries[version] = handle;
            _availabilityCache[version] = true;

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
        var available = NativeLibrary.TryLoad(libraryName, out var handle) || 
                       TryLoadWithSearchPaths(libraryName, version) != IntPtr.Zero;

        if (available && handle != IntPtr.Zero)
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
    /// Gets the platform-specific library name for a PostgreSQL version
    /// </summary>
    private static string GetLibraryName(PostgreSqlVersion version)
    {
        var suffix = version.ToLibrarySuffix();
        
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return $"pg_query_{suffix}";
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            return $"libpg_query_{suffix}";
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            return $"libpg_query_{suffix}";
        }
        else
        {
            throw new PlatformNotSupportedException($"Platform {RuntimeInformation.OSDescription} is not supported");
        }
    }

    /// <summary>
    /// Attempts to load library with various search paths
    /// </summary>
    private static IntPtr TryLoadWithSearchPaths(string libraryName, PostgreSqlVersion version)
    {
        var searchPaths = GetSearchPaths(version);

        foreach (var path in searchPaths)
        {
            if (NativeLibrary.TryLoad(path, out var handle))
            {
                return handle;
            }
        }

        return IntPtr.Zero;
    }

    /// <summary>
    /// Gets potential search paths for the native library
    /// </summary>
    private static IEnumerable<string> GetSearchPaths(PostgreSqlVersion version)
    {
        var suffix = version.ToLibrarySuffix();
        var baseDir = AppContext.BaseDirectory;
        var paths = new List<string>();

        string extension;
        string prefix;

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            extension = "dll";
            prefix = "";
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            extension = "so";
            prefix = "lib";
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            extension = "dylib";
            prefix = "lib";
        }
        else
        {
            yield break;
        }

        var libraryFileName = $"{prefix}pg_query_{suffix}.{extension}";
        
        // Current directory
        paths.Add(Path.Combine(baseDir, libraryFileName));

        // Runtime-specific directory
        var rid = GetRuntimeIdentifier();
        paths.Add(Path.Combine(baseDir, "runtimes", rid, "native", libraryFileName));

        // Alternative RID patterns
        if (!string.IsNullOrEmpty(rid))
        {
            var ridParts = rid.Split('-');
            if (ridParts.Length >= 2)
            {
                var os = ridParts[0];
                var arch = ridParts[1];
                paths.Add(Path.Combine(baseDir, "runtimes", $"{os}-{arch}", "native", libraryFileName));
            }
        }

        foreach (var path in paths)
        {
            if (File.Exists(path))
            {
                yield return path;
            }
        }
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
