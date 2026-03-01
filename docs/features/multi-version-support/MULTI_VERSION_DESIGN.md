# Multi-Version PostgreSQL Support - Design Document

## Overview
This document outlines the design for supporting multiple PostgreSQL versions (16, 17, and future versions) in Npgquery.

## Background

### libpg_query Architecture
- **Repository**: https://github.com/pganalyze/libpg_query
- **Branch Strategy**: Each PostgreSQL major version has its own branch (e.g., `16-latest`, `17-latest`)
- **Versioning**: Uses format `{major}-{minor}.{patch}.{revision}` (e.g., `17-6.2.2`)
- **Breaking Changes**: Each major PostgreSQL version can introduce parser changes that break compatibility

### Current State (Npgquery)
- **Single Version**: Currently hardcoded to PostgreSQL 16 (`PostgreSqlVersion = 160000`)
- **Native Libraries**: Single `pg_query.dll`/`pg_query.so` per platform
- **Static Linking**: DllImport uses constant library name "pg_query"

## Requirements

### Functional Requirements
1. **Multi-Version Support**: Support PostgreSQL versions 16, 17, and be extensible for future versions
2. **Runtime Selection**: Allow users to specify which PostgreSQL version to use at runtime
3. **Backward Compatibility**: Existing code using default version should continue to work
4. **Version Detection**: Provide utilities to detect which version is being used
5. **Graceful Degradation**: Handle missing versions gracefully with clear error messages

### Non-Functional Requirements
1. **Performance**: Version selection should have minimal overhead
2. **Memory**: Load only the required version's native library
3. **Distribution**: Package should include multiple versions without bloat
4. **Developer Experience**: Simple API for version selection

## Design

### 1. Version Identification

#### Version Enum
```csharp
public enum PostgreSqlVersion
{
    /// <summary>PostgreSQL 16.x</summary>
    Postgres16 = 160000,
    
    /// <summary>PostgreSQL 17.x</summary>
    Postgres17 = 170000,
    
    /// <summary>Use the latest stable version (currently 17)</summary>
    Latest = Postgres17,
    
    /// <summary>Use the default version (currently 16 for stability)</summary>
    Default = Postgres16
}
```

#### Version Helper Class
```csharp
public static class PostgreSqlVersionExtensions
{
    public static string ToLibraryName(this PostgreSqlVersion version);
    public static string ToVersionString(this PostgreSqlVersion version);
    public static int ToVersionNumber(this PostgreSqlVersion version);
}
```

### 2. Native Library Organization

#### Directory Structure
```
runtimes/
├── win-x64/
│   └── native/
│       ├── pg_query_16.dll
│       └── pg_query_17.dll
├── linux-x64/
│   └── native/
│       ├── libpg_query_16.so
│       └── libpg_query_17.so
├── osx-x64/
│   └── native/
│       ├── libpg_query_16.dylib
│       └── libpg_query_17.dylib
└── osx-arm64/
    └── native/
        ├── libpg_query_16.dylib
        └── libpg_query_17.dylib
```

#### Library Naming Convention
- **Windows**: `pg_query_{version}.dll` (e.g., `pg_query_16.dll`)
- **Linux**: `libpg_query_{version}.so` (e.g., `libpg_query_16.so`)
- **macOS**: `libpg_query_{version}.dylib` (e.g., `libpg_query_16.dylib`)

### 3. Dynamic Library Loading

#### Native Library Loader
```csharp
internal sealed class NativeLibraryLoader
{
    private static readonly Dictionary<PostgreSqlVersion, IntPtr> _loadedLibraries = new();
    private static readonly object _loadLock = new();
    
    public static IntPtr GetLibraryHandle(PostgreSqlVersion version);
    public static bool IsVersionAvailable(PostgreSqlVersion version);
    public static IEnumerable<PostgreSqlVersion> GetAvailableVersions();
}
```

#### Native Methods Refactoring
Replace static `DllImport` with dynamic function pointer loading:

```csharp
internal static class NativeMethods
{
    // Function pointer delegates
    private delegate PgQueryParseResult ParseDelegate(byte[] input);
    
    // Cache of loaded function pointers per version
    private static readonly Dictionary<PostgreSqlVersion, ParseDelegate> _parseFunctions = new();
    
    public static PgQueryParseResult pg_query_parse(byte[] input, PostgreSqlVersion version)
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
}
```

### 4. API Design

#### Updated Parser Class
```csharp
public sealed class Parser : IDisposable
{
    private readonly PostgreSqlVersion _version;
    
    public Parser(PostgreSqlVersion version = PostgreSqlVersion.Default)
    {
        _version = version;
        EnsureVersionAvailable(version);
    }
    
    public PostgreSqlVersion Version => _version;
    
    // All methods use _version internally
    public ParseResult Parse(string query, ParseOptions? options = null) { ... }
}
```

#### Updated ParseOptions
```csharp
public sealed record ParseOptions
{
    public bool IncludeLocations { get; init; } = false;
    
    /// <summary>
    /// PostgreSQL version to use for parsing.
    /// If not specified, uses the version from the Parser instance.
    /// </summary>
    public PostgreSqlVersion? Version { get; init; }
    
    [Obsolete("Use Version property instead")]
    public int PostgreSqlVersion { get; init; } = 160000;
}
```

### 5. Migration Path

#### Phase 1: Internal Infrastructure (Current Phase)
- Add version enums and constants
- Implement dynamic library loader
- Refactor NativeMethods for multi-version support
- Maintain backward compatibility with version parameter defaulting

#### Phase 2: API Enhancement
- Add version parameter to Parser constructor
- Update ParseOptions to use enum
- Add version detection utilities
- Update documentation and examples

#### Phase 3: Deprecation (Future)
- Mark old `PostgreSqlVersion` int property as obsolete
- Provide migration guide
- Eventually remove in next major version

### 6. Error Handling

#### Version Not Available
```csharp
public class PostgreSqlVersionNotAvailableException : NativeLibraryException
{
    public PostgreSqlVersion RequestedVersion { get; }
    public IEnumerable<PostgreSqlVersion> AvailableVersions { get; }
}
```

#### Version Mismatch
Handle cases where parse tree from one version is passed to another version's deparser.

### 7. Testing Strategy

#### Unit Tests
- Test each version independently
- Test version switching
- Test error handling for missing versions
- Test backward compatibility

#### Integration Tests
- Parse same query with different versions
- Compare parse trees across versions
- Test version-specific features

### 8. Build Process

#### Acquiring Native Libraries
1. Clone libpg_query repository
2. Checkout version-specific branches
3. Build for each platform
4. Rename and organize binaries
5. Package with project

#### Automation
- Create build script for fetching and building multiple versions
- Add to CI/CD pipeline
- Version matrix testing

## Implementation Checklist

- [x] Create design document
- [ ] Create PostgreSqlVersion enum
- [ ] Implement NativeLibraryLoader class
- [ ] Refactor NativeMethods for dynamic loading
- [ ] Update Parser constructor
- [ ] Update ParseOptions model
- [ ] Add version detection utilities
- [ ] Obtain and package PG 16 native libraries
- [ ] Obtain and package PG 17 native libraries
- [ ] Update project file for multi-version builds
- [ ] Create native library build scripts
- [ ] Add comprehensive tests
- [ ] Update documentation
- [ ] Update examples
- [ ] Create migration guide

## Future Enhancements

### Version Auto-Detection
Detect PostgreSQL version from query hints or server metadata.

### Version Translation Layer
Automatically translate queries between versions where possible.

### Performance Profiling
Compare performance across versions.

### Custom Builds
Allow users to include only specific versions to reduce package size.

## References

- [libpg_query Repository](https://github.com/pganalyze/libpg_query)
- [Npgquery Source](https://github.com/JaredMSFT/Npgquery)
- [PostgreSQL Version Policy](https://www.postgresql.org/support/versioning/)
- [.NET Native Interop](https://docs.microsoft.com/en-us/dotnet/standard/native-interop/)
