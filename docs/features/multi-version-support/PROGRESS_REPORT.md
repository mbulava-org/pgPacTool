# Multi-Version PostgreSQL Support - Progress Report

## Completed Work

### Branch Created
- Created feature branch: `feature/multi-postgres-version-support`

### Design Phase (Step 1 ✓)
- **Design Document**: Created comprehensive design at `docs/MULTI_VERSION_DESIGN.md`
- **Research**: Analyzed libpg_query structure (16-latest, 17-latest branches)
- **Architecture**: Defined version enum, loader strategy, directory structure
- **Requirements**: Documented functional and non-functional requirements

### Core Infrastructure (Step 2 ✓)
1. **PostgreSqlVersion Enum** (`src/libs/Npgquery/Npgquery/PostgreSqlVersion.cs`)
   - Defined `Postgres16` and `Postgres17` versions
   - Added extension methods for version metadata
   - Methods: `ToLibrarySuffix()`, `ToVersionString()`, `ToVersionNumber()`, `GetMajorVersion()`

2. **NativeLibraryLoader** (`src/libs/Npgquery/Npgquery/Native/NativeLibraryLoader.cs`)
   - Dynamic library loading with version-specific caching
   - Platform-aware library naming (Windows/Linux/macOS)
   - Search path resolution for runtime-specific directories
   - Version availability checking
   - Methods: `GetLibraryHandle()`, `IsVersionAvailable()`, `GetAvailableVersions()`

3. **Exception Handling** (`src/libs/Npgquery/Npgquery/Exceptions.cs`)
   - Changed `NativeLibraryException` from sealed to non-sealed (allow inheritance)
   - Added `PostgreSqlVersionNotAvailableException` with version context
   - Includes available versions list for better error messages

### Build Status
- ✅ **Build Successful** (only XML documentation warnings remain)
- All new code compiles without errors
- Backward compatibility maintained

## Current State

### What Works
- Version enum and extensions
- Dynamic library loader infrastructure
- Exception handling for missing versions

### What's Next
The current implementation still uses static `DllImport` which won't work for multi-version support. 
We need to:

## Next Steps (Remaining Work)

### Step 3: Refactor Native Library Infrastructure (IN PROGRESS)

#### Current Challenge
- **Static DllImport Problem**: Current NativeMethods uses:
  ```csharp
  private const string LibraryName = "pg_query";
  [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
  internal static extern PgQueryParseResult pg_query_parse(byte[] input);
  ```
  This only supports a single library name.

#### Solution Approach
Replace static DllImport with dynamic function pointers:

```csharp
// Define function pointer delegates
[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
private delegate PgQueryParseResult ParseDelegate(byte[] input);

// Cache loaded function pointers per version
private static readonly ConcurrentDictionary<PostgreSqlVersion, ParseDelegate> _parseFunctions = new();

// Dynamic function loading
internal static PgQueryParseResult pg_query_parse(byte[] input, PostgreSqlVersion version)
{
    if (!_parseFunctions.TryGetValue(version, out var func))
    {
        var handle = NativeLibraryLoader.GetLibraryHandle(version);
        var ptr = NativeLibrary.GetExport(handle, "pg_query_parse");
        func = Marshal.GetDelegateForFunctionPointer<ParseDelegate>(ptr);
        _parseFunctions[version] = func;
    }
    return func(input);
}
```

#### Implementation Plan for Step 3
1. Create delegate types for all native functions
2. Create version-aware wrapper methods
3. Add caching layer for function pointers
4. Update all call sites to pass version parameter
5. Add default version support for backward compatibility

### Step 4: Update Parser Class
```csharp
public sealed class Parser : IDisposable
{
    private readonly PostgreSqlVersion _version;
    
    public Parser(PostgreSqlVersion version = PostgreSqlVersion.Postgres16)
    {
        _version = version;
        // Ensure library is available on construction
        NativeLibraryLoader.GetLibraryHandle(_version);
    }
    
    public PostgreSqlVersion Version => _version;
    
    // All methods pass _version to NativeMethods
    public ParseResult Parse(string query, ParseOptions? options = null)
    {
        // ... existing logic but call NativeMethods.pg_query_parse(bytes, _version)
    }
}
```

### Step 5: Obtain Native Libraries
Need to build or obtain native libraries for both versions:

#### Required Files
```
runtimes/
├── win-x64/native/
│   ├── pg_query_16.dll  (from libpg_query 16-latest)
│   └── pg_query_17.dll  (from libpg_query 17-latest)
└── linux-x64/native/
    ├── libpg_query_16.so
    └── libpg_query_17.so
```

#### Build Process
1. Clone libpg_query repository
2. Build 16-latest branch
3. Rename output to `pg_query_16.dll/so`
4. Build 17-latest branch
5. Rename output to `pg_query_17.dll/so`
6. Place in appropriate runtime directories

### Step 6: Update Project File
- Update `Npgquery.csproj` to include version-specific native libraries
- Ensure proper packaging for NuGet distribution
- Update build targets for multi-version support

### Step 7: Testing
1. Unit tests for version selection
2. Tests for version switching
3. Tests for missing version error handling
4. Integration tests across versions
5. Backward compatibility tests

### Step 8: Documentation
1. Update README with version selection examples
2. Create migration guide
3. Add API documentation for new version features
4. Document build process for native libraries

## Technical Decisions Made

### Version Enum Design
- **Decision**: Use simple enum values (16, 17) instead of full version numbers (160000, 170000)
- **Rationale**: Simpler, cleaner API. Extension methods provide version number conversion when needed.
- **Alternative Considered**: Aliases (Latest, Default) - Rejected due to enum value collision issues

### Library Naming Convention
- **Windows**: `pg_query_{version}.dll` (e.g., `pg_query_16.dll`)
- **Linux**: `libpg_query_{version}.so`
- **macOS**: `libpg_query_{version}.dylib`

### Backward Compatibility
- Default version: PostgreSQL 16 (most stable, widely tested)
- Parser constructor defaults to Postgres16
- Existing code continues to work without changes

## Risks and Mitigations

### Risk: Native Library Acquisition
- **Issue**: Need to build native libraries for each platform/version
- **Mitigation**: Document build process, consider CI/CD automation

### Risk: Function Signature Changes
- **Issue**: Native function signatures might differ between PG versions
- **Mitigation**: Thorough testing, version-specific wrappers if needed

### Risk: Breaking Changes
- **Issue**: Refactoring NativeMethods could break existing functionality
- **Mitigation**: Comprehensive test suite, gradual rollout

## Success Criteria
- [ ] Both PostgreSQL 16 and 17 parsers available
- [ ] Users can select version at Parser construction
- [ ] Error messages clearly indicate version issues
- [ ] All existing tests pass
- [ ] New version-specific tests pass
- [ ] Documentation complete
- [ ] Build and package process automated

## Timeline Estimate
- Step 3 (Native refactor): 2-3 hours
- Step 4 (Parser updates): 1 hour  
- Step 5 (Native libraries): 2-4 hours (platform-dependent)
- Step 6 (Project config): 30 minutes
- Step 7 (Testing): 2-3 hours
- Step 8 (Documentation): 1-2 hours

**Total**: 8-13 hours

## Repository State
- Branch: `feature/multi-postgres-version-support`
- Build: ✅ Passing (with warnings)
- Commits: Ready to commit current progress
- Next: Continue with Step 3 refactoring

---
*Last Updated: [Current Session]*
*Status: Foundation Complete, Refactoring In Progress*
