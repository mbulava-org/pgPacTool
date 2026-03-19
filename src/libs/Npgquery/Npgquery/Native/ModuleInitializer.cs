using System.Runtime.CompilerServices;

namespace Npgquery.Native;

/// <summary>
/// Module initializer that sets up native library loading BEFORE any code in this assembly runs
/// </summary>
internal static class ModuleInitializer
{
    /// <summary>
    /// This method is called by the runtime before any other code in this assembly executes
    /// </summary>
    [ModuleInitializer]
    public static void Initialize()
    {
        // Force the native library loader to initialize its DllImport resolver
        // This MUST happen before any P/Invoke declarations are accessed
        NativeLibraryLoader.EnsureInitialized();
    }
}
