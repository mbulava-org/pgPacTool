# Native Library Loading - Implementation Summary

## What We've Done

We've implemented a **permanent, diagnostic-first solution** for native library loading that eliminates the cycle of repeated workarounds and provides full visibility into what's happening.

### Core Changes

#### 1. **Enhanced NativeLibraryLoader.cs**
- Added `NativeLibrary.SetDllImportResolver` for explicit P/Invoke control
- Comprehensive diagnostic logging at every step
- `PrintDiagnostics()` method for easy troubleshooting
- Version-aware library resolution for PostgreSQL 12-18
- Static constructor ensures resolver initializes before any P/Invoke calls

**Key Features**:
```csharp
// Explicit resolver intercepts all P/Invoke declarations
NativeLibrary.SetDllImportResolver(assembly, DllImportResolver);

// Detailed logging of every load attempt
LogDiagnostic($"Trying: {path}");
LogDiagnostic($"Exists: {File.Exists(path)}");
LogDiagnostic($"SUCCESS: Loaded from {path}, handle: 0x{handle:X}");

// Public API for troubleshooting
NativeLibraryLoader.PrintDiagnostics();
var logs = NativeLibraryLoader.DiagnosticLog;
```

#### 2. **Simplified Npgquery.targets**
- **Removed flat copying workaround** (no more copying to output root)
- Copy **ONLY** to runtime-specific folders: `runtimes/{rid}/native/`
- Support all platforms (Windows, Linux, macOS) with `*.dll`, `*.so`, `*.dylib`
- Clear diagnostic messages during build

**Before** (workaround approach):
```xml
<!-- Copied to TWO locations hoping one would work -->
<Copy SourceFiles="..." DestinationFolder="$(OutputPath)" />
<Copy SourceFiles="..." DestinationFolder="$(OutputPath)runtimes\linux-x64\native\" />
```

**After** (standard .NET approach):
```xml
<!-- Copy ONLY to runtime-specific folders -->
<Copy SourceFiles="@(NativeLibraryLinux)" 
      DestinationFolder="$(OutputPath)runtimes\linux-x64\native\" />
```

#### 3. **New Diagnostic Tests**
Created `NativeLibraryDiagnosticsTests.cs` to:
- Always run in CI/CD (no category filters)
- Print system info, runtime identifier, base directory
- Show all attempted load paths
- Display available PostgreSQL versions
- Capture diagnostic logs for troubleshooting

**Tests**:
- `PrintDiagnostics_ShowsSystemInfo` - Always prints full diagnostic info
- `LibraryHandle_CanLoad_ForSupportedVersions` - Verifies each version loads
- `GetAvailableVersions_ReturnsAtLeastOne` - Ensures detection works

#### 4. **Comprehensive Documentation**
- `docs/NATIVE_LIBRARY_SOLUTION.md` - Complete solution architecture
- Troubleshooting guide with specific commands
- Migration notes for developers and CI/CD
- Future-proof design for PostgreSQL 12-18

---

## Why This Works

### The Problem with Workarounds

Previous attempts failed because they didn't address the **root cause**:
- **File location** wasn't the issue (files were there)
- **.NET automatic probing** wasn't finding version-specific libraries
- **No explicit control** over which library loads
- **No diagnostics** to see what was happening

### The Permanent Solution

**Explicit Control + Diagnostics**:

1. **SetDllImportResolver** gives us complete control over P/Invoke loading
2. **Diagnostic logging** shows exactly what's happening at every step
3. **Runtime-specific folders** follow standard .NET conventions
4. **Version-aware resolution** supports multiple PostgreSQL versions
5. **Graceful error handling** with clear messages and available versions

**No Guessing. Full Visibility. Complete Control.**

---

## Testing Results

### Local Windows Testing ✅

```
dotnet test --filter "FullyQualifiedName~NativeLibraryDiagnosticsTests"

=== Initializing Native Library Resolver ===
OS: Microsoft Windows 10.0.19045
Platform: X64
Process Architecture: X64
Runtime Identifier: win-x64
Base Directory: C:\...\bin\Release\net10.0\

SUCCESS: Loaded from ...runtimes\win-x64\native\libpg_query_17.dll
SUCCESS: Loaded from ...runtimes\win-x64\native\libpg_query_16.dll

Available versions: 2
  - PostgreSQL 16
  - PostgreSQL 17

Test Run Successful.
Total tests: 4
     Passed: 4
```

### CI/CD Linux Testing 🔄

Workflow triggered: https://github.com/mbulava-org/pgPacTool/actions

The diagnostic tests will run first and show:
- Linux OS and platform info
- Runtime identifier (linux-x64)
- All attempted load paths
- Which paths exist
- Success/failure for each path

**If it works**: Tests pass, Integration tests run, publish to NuGet  
**If it fails**: Diagnostic logs show exactly what went wrong

---

## Future-Proof Architecture

### Multi-Version PostgreSQL Support

This design supports PostgreSQL 12-18 (7 versions):

```csharp
// Each version uses its own native library
using var parser12 = new Parser(PostgreSqlVersion.Postgres12);  // libpg_query_12.so
using var parser16 = new Parser(PostgreSqlVersion.Postgres16);  // libpg_query_16.so  
using var parser17 = new Parser(PostgreSqlVersion.Postgres17);  // libpg_query_17.so
using var parser18 = new Parser(PostgreSqlVersion.Postgres18);  // libpg_query_18.so

// Runtime detection
var available = NativeLibraryLoader.GetAvailableVersions();
```

### Scaling Considerations

**Current**: 2 versions × 3 platforms = 6 native libraries  
**Future**: 7 versions × 3 platforms = 21 native libraries

The architecture handles this automatically:
- Version-specific loading with `GetLibraryHandle(version)`
- Automatic detection with `GetAvailableVersions()`
- Diagnostic logging for each version
- No code changes needed to add new versions

---

## How to Troubleshoot

If issues occur in CI/CD:

### 1. Check Diagnostic Test Output

The `NativeLibraryDiagnosticsTests` test always runs and shows:
- OS and platform info
- Base directory and runtime identifier  
- All attempted load paths with existence checks
- Success/failure with handle addresses
- Available PostgreSQL versions

### 2. Run Diagnostics Programmatically

```csharp
NativeLibraryLoader.EnableConsoleLogging = true;
NativeLibraryLoader.PrintDiagnostics();

// Access diagnostic log
foreach (var log in NativeLibraryLoader.DiagnosticLog)
{
    Console.WriteLine(log);
}
```

### 3. Verify Runtime Folders

```bash
ls -R bin/Release/net10.0/runtimes/

# Should show:
runtimes/
  linux-x64/native/
    libpg_query_16.so
    libpg_query_17.so
```

### 4. Check Build Messages

```
[Npgquery] Copying native libraries to runtime-specific folders...
[Npgquery] Copied Linux native libraries: libpg_query_16.so, libpg_query_17.so
[Npgquery] Native library deployment complete
```

---

## Benefits

### For Developers
- ✅ No code changes required
- ✅ Automatic diagnostics in test output  
- ✅ Clear error messages with available versions
- ✅ Easy troubleshooting with `PrintDiagnostics()`

### For CI/CD
- ✅ Standard .NET runtime-specific folders
- ✅ Diagnostic tests run first to verify setup
- ✅ All load attempts logged automatically
- ✅ Clear visibility into what's happening

### For Future Maintenance
- ✅ No more repeated workarounds
- ✅ Easy to add new PostgreSQL versions
- ✅ Diagnostic-first approach catches issues early
- ✅ Comprehensive documentation for troubleshooting

---

## Next Steps

1. ✅ **Local testing complete** - Windows verified, all tests passing
2. 🔄 **CI/CD testing in progress** - Workflow triggered on preview1 branch
3. ⏳ **Awaiting Linux results** - Diagnostic tests will show what happens
4. 📝 **Document results** - Update docs based on CI/CD outcome

### If CI/CD Succeeds
- ✅ Publish preview release to NuGet.org
- ✅ Update documentation with success confirmation
- ✅ Close the issue - permanent solution implemented

### If CI/CD Still Fails
- 🔍 Review diagnostic test output in GitHub Actions
- 📊 Analyze which paths were tried and why they failed
- 🛠️ Adjust loader strategy based on specific Linux behavior
- 📝 Document findings and iterate

**The difference**: Now we'll know EXACTLY what's happening instead of guessing.

---

## Summary

We've moved from **workarounds to architecture**:

**Old Approach**: Try different file locations, hope something works  
**New Approach**: Explicit control + comprehensive diagnostics

**Old Result**: Repeated issues, no visibility  
**New Result**: Clear understanding, easy troubleshooting

**The Key Insight**: It's not about WHERE files are, it's about HOW .NET loads them and SEEING what happens.

---

*Commit*: `8093fba`  
*Branch*: `preview1`  
*Status*: ✅ Implemented, 🔄 Testing  
*Documentation*: `docs/NATIVE_LIBRARY_SOLUTION.md`
