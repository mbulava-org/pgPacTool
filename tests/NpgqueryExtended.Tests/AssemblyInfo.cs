using Xunit;

// Disable test parallelization across all collections in this assembly.
// NativeLibraryLoader handles are shared process-wide; running teardown in parallel
// causes a process crash after ~300+ tests when Parser.Dispose() races with other
// tests still in flight. This mirrors the fix applied to mbulava.PostgreSql.Dac.Tests (DEV-49).
[assembly: CollectionBehavior(DisableTestParallelization = true, MaxParallelThreads = 1)]
