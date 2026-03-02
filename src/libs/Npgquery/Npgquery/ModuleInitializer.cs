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
        // Native libraries are loaded on-demand per version
        // No pre-loading needed with multi-version support
    }
}
