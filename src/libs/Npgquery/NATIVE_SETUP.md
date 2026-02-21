# Npgquery - Native Library Setup

## Overview

Npgquery requires the native `libpg_query` library to function. This library embeds the PostgreSQL parser and provides the core functionality for parsing SQL queries.

## Native Library Requirements

### Windows
- `pg_query.dll` (x64 or ARM64)
- Visual C++ Redistributable 2019 or later

### Linux  
- `libpg_query.so` (x64 or ARM64)
- glibc 2.17 or later

### macOS
- `libpg_query.dylib` (x64 or ARM64)
- macOS 10.15 or later

## Installation Options


### Manual Installation

1. Download the appropriate native library for your platform from the [libpg_query releases](https://github.com/pganalyze/libpg_query/releases)
2. Place the library file in one of these locations:
   - Same directory as your executable
   - A directory in your system's PATH
   - A directory specified in your app's configuration

## Runtime Structure

When distributed, your application should have this structure:

```
YourApp/
??? YourApp.exe (or .dll)
??? Npgquery.dll
??? runtimes/
    ??? win-x64/
    ?   ??? native/
    ?       ??? pg_query.dll
    ??? win-arm64/
    ?   ??? native/
    ?       ??? pg_query.dll
    ??? linux-x64/
    ?   ??? native/
    ?       ??? libpg_query.so
    ??? linux-arm64/
    ?   ??? native/
    ?       ??? libpg_query.so
    ??? osx-x64/
    ?   ??? native/
    ?       ??? libpg_query.dylib
    ??? osx-arm64/
        ??? native/
            ??? libpg_query.dylib
```

## Troubleshooting

### DllNotFoundException on Windows
- Ensure Visual C++ Redistributable 2019 or later is installed
- Check that `pg_query.dll` is in the same directory as your executable
- Verify the architecture matches (x64 vs ARM64)

### Library not found on Linux
- Install required dependencies: `sudo apt-get install libc6-dev` (Ubuntu/Debian)
- Ensure `libpg_query.so` has execute permissions: `chmod +x libpg_query.so`
- Check that glibc version is 2.17 or later: `ldd --version`

### Library not found on macOS
- Ensure macOS 10.15 or later
- Check that `libpg_query.dylib` is not quarantined: `xattr -d com.apple.quarantine libpg_query.dylib`
- Verify the architecture matches your application (x64 vs ARM64)

### Loading Issues
If you encounter native library loading issues, you can:

1. **Check library dependencies**:
   - Windows: Use `dumpbin /dependents pg_query.dll`
   - Linux: Use `ldd libpg_query.so`
   - macOS: Use `otool -L libpg_query.dylib`

2. **Enable native library logging**:
   ```csharp
   // Add this before using Npgquery
   AppContext.SetSwitch("System.Runtime.InteropServices.EnableConsoleLogging", true);
   ```

3. **Specify custom library path**:
   ```csharp
   // Set before first use
   Environment.SetEnvironmentVariable("LD_LIBRARY_PATH", "/path/to/lib");
   ```

## Building from Source

If you need to build the native libraries yourself:

1. Clone libpg_query: `git clone https://github.com/pganalyze/libpg_query.git`
2. Follow the build instructions in the libpg_query repository
3. Copy the resulting library files to your project's runtime directories

## Supported PostgreSQL Versions

The embedded PostgreSQL parser supports syntax from PostgreSQL versions 10 through 16. The library automatically uses the latest supported version unless specified otherwise in `ParseOptions`.

## Performance Considerations

- The native library is thread-safe at the C level
- Each `Npgquery` instance should be used by only one thread at a time
- For multi-threaded scenarios, create separate instances or use proper synchronization
- Native calls have minimal overhead, making the library suitable for high-performance scenarios

## License and Attribution

- Npgquery: MIT License
- libpg_query: 3-Clause BSD License
- PostgreSQL Parser: PostgreSQL License

When distributing applications using Npgquery, you must include appropriate license attributions for all components.