using System.Runtime.CompilerServices;
using Npgquery.Native;

namespace Npgquery;

/// <summary>
/// Module initializer to set up native library loading
/// </summary>
internal static class ModuleInitializer
{
    [ModuleInitializer]
    internal static void Initialize()
    {
        NativeLibraryLoader.EnsureLoaded();
    }
}
