using System.Runtime.CompilerServices;

namespace Npgquery;

/// <summary>
/// Module initializer for Npgquery library
/// </summary>
internal static class ModuleInitializer
{
    [ModuleInitializer]
    internal static void Initialize()
    {
        // Native libraries are loaded on-demand per version.
        // No pre-loading needed with multi-version support.

        // Register a ProcessExit handler to clear cached native-function delegates
        // before the CLR's shutdown GC pass.  Without this, the finalizer thread
        // can fault when it tries to finalize delegates that wrap pointers into a
        // native library whose address space is already being reclaimed, producing
        // "Test host process crashed" after all tests complete.
        AppDomain.CurrentDomain.ProcessExit += static (_, _) =>
        {
            try
            {
                Npgquery.Native.NativeMethods.ClearDelegateCache();
            }
            catch
            {
                // Swallow: we are in process exit; best-effort only.
            }
        };
    }
}
