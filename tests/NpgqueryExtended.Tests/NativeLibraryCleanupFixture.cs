using Xunit;
using Npgquery;

namespace NpgqueryExtended.Tests;

/// <summary>
/// Assembly-scoped collection fixture that acts as a synchronization point for
/// native library teardown in <c>NpgqueryExtended.Tests</c>.
///
/// Native library handles loaded by <see cref="NativeLibraryLoader"/> are process-wide
/// singletons backed by unmanaged memory.  Calling <see cref="NativeLibrary.Free"/> on
/// them while the .NET runtime is still running (e.g. during xUnit teardown) is unsafe
/// because finalizers and background threads can still issue P/Invoke calls into those
/// handles after they are freed, causing a process crash.
///
/// The safe strategy is to let the OS reclaim native library memory at process exit.
/// This fixture therefore does NOT free the handles explicitly.  Instead it exists to:
/// 1. Force all test classes that use the <c>NativeLibrary</c> collection into the same
///    xUnit collection, which prevents concurrent teardown of test classes that share
///    native state.
/// 2. Provide a deterministic join point if future teardown logic is needed.
///
/// Pattern mirrors the fix applied to mbulava.PostgreSql.Dac.Tests in DEV-49.
/// </summary>
public sealed class NativeLibraryCleanupFixture : IAsyncLifetime
{
    public Task InitializeAsync() => Task.CompletedTask;

    public Task DisposeAsync()
    {
        // Intentionally do NOT call NativeLibraryLoader.UnloadAll() here.
        // Freeing native handles during xUnit teardown is unsafe: the .NET runtime
        // may still have finalizers or background threads that call into those handles
        // after Free(), causing a native access violation / process crash.
        // The OS will reclaim all native library memory when the test process exits.
        return Task.CompletedTask;
    }
}

/// <summary>
/// xUnit collection definition that attaches <see cref="NativeLibraryCleanupFixture"/>
/// to the default test collection so the fixture lifecycle wraps the entire assembly run.
/// </summary>
[CollectionDefinition(NativeLibraryCollection.Name)]
public sealed class NativeLibraryCollection : ICollectionFixture<NativeLibraryCleanupFixture>
{
    /// <summary>Name used in <c>[Collection]</c> attributes on test classes.</summary>
    public const string Name = "NativeLibrary";
}
