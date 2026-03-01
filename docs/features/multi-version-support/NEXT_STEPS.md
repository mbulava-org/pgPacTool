# Next Steps: Acquiring Native Libraries

## ⚡ Quick Start (Automated - Recommended)

We now have **full automation** for building native libraries! No need for manual builds.

### Option 1: GitHub Actions (All Platforms)

1. Go to the **Actions** tab in GitHub
2. Select **"Build Native libpg_query Libraries"**
3. Click **"Run workflow"**
4. Enter versions (e.g., `16,17` or `14,15,16,17,18`)
5. Click **"Run workflow"**

The workflow will:
- ✅ Build for Windows, Linux, macOS (Intel & ARM)
- ✅ Automatically organize libraries in runtime directories  
- ✅ Create a Pull Request with the changes
- ✅ Ready to merge and test!

### Option 2: Local Build (Your Platform Only)

```powershell
# Build default versions (16, 17)
.\scripts\Build-NativeLibraries.ps1

# Build specific versions
.\scripts\Build-NativeLibraries.ps1 -Versions "14,15,16,17,18"

# Force rebuild
.\scripts\Build-NativeLibraries.ps1 -Force
```

**See**: `docs/NATIVE_LIBRARY_AUTOMATION.md` for full documentation.

---

## 📚 Manual Process (If Automation Fails)

If you need to build manually, follow these steps:

## Prerequisites

### Tools Needed
- **Git**: For cloning libpg_query repository
- **C Compiler**: 
  - Windows: Visual Studio 2019+ or Build Tools
  - Linux: GCC or Clang
  - macOS: Xcode Command Line Tools
- **Make**: Build automation
  - Windows: Use `nmake` (comes with Visual Studio)
  - Linux/macOS: Standard `make`

## Step-by-Step Guide

### 1. Clone libpg_query Repository

```bash
# Clone the repository
git clone https://github.com/pganalyze/libpg_query.git
cd libpg_query
```

### 2. Build PostgreSQL 16 Version

```bash
# Checkout 16-latest branch
git checkout 16-latest

# Build for your platform

# Windows:
nmake /F Makefile.msvc

# Linux/macOS:
make

# Output will be in the current directory:
# - Windows: pg_query.dll
# - Linux: libpg_query.so
# - macOS: libpg_query.dylib
```

### 3. Build PostgreSQL 17 Version

```bash
# Checkout 17-latest branch
git checkout 17-latest

# Clean previous build
make clean  # or: nmake /F Makefile.msvc clean

# Build
# Windows:
nmake /F Makefile.msvc

# Linux/macOS:
make
```

### 4. Rename and Organize Libraries

Create the following directory structure in your Npgquery project:

```
src/libs/Npgquery/Npgquery/runtimes/
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

**Renaming Command Examples**:

```powershell
# Windows (PowerShell)
Copy-Item .\libpg_query\pg_query.dll src\libs\Npgquery\Npgquery\runtimes\win-x64\native\pg_query_16.dll

# Linux/macOS
cp libpg_query/libpg_query.so src/libs/Npgquery/Npgquery/runtimes/linux-x64/native/libpg_query_16.so
```

### 5. Cross-Platform Builds

For complete package, you'll need binaries for all platforms:

#### Option A: Build on Each Platform
- Build on Windows machine → get Windows DLLs
- Build on Linux machine → get Linux SOs
- Build on macOS machine (Intel) → get x64 dylibs
- Build on macOS machine (ARM) → get arm64 dylibs

#### Option B: Use CI/CD
Set up GitHub Actions to build on multiple platforms:

```yaml
# .github/workflows/build-native.yml
name: Build Native Libraries

on: [workflow_dispatch]

jobs:
  build:
    strategy:
      matrix:
        os: [windows-latest, ubuntu-latest, macos-latest]
        pg-version: [16, 17]
    
    runs-on: ${{ matrix.os }}
    
    steps:
      - name: Checkout libpg_query
        uses: actions/checkout@v3
        with:
          repository: pganalyze/libpg_query
          ref: ${{ matrix.pg-version }}-latest
      
      - name: Build (Windows)
        if: runner.os == 'Windows'
        run: nmake /F Makefile.msvc
      
      - name: Build (Unix)
        if: runner.os != 'Windows'
        run: make
      
      - name: Upload artifacts
        uses: actions/upload-artifact@v3
        with:
          name: native-${{ runner.os }}-pg${{ matrix.pg-version }}
          path: |
            *.dll
            *.so
            *.dylib
```

### 6. Update Project File

The `Npgquery.csproj` already includes runtime files, but verify:

```xml
<ItemGroup>
  <None Include="runtimes\**\*">
    <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
    <Link>runtimes\%(RecursiveDir)%(FileName)%(Extension)</Link>
    <Pack>true</Pack>
    <PackagePath>runtimes\%(RecursiveDir)</PackagePath>
    <Visible>false</Visible>
  </None>
</ItemGroup>
```

### 7. Test the Integration

Create a simple test to verify both versions work:

```csharp
using Npgquery;

// Test PostgreSQL 16
using (var parser16 = new Parser(PostgreSqlVersion.Postgres16))
{
    var result = parser16.Parse("SELECT version()");
    Console.WriteLine($"PG 16: {(result.IsSuccess ? "✓ Success" : $"✗ Error: {result.Error}")}");
}

// Test PostgreSQL 17
using (var parser17 = new Parser(PostgreSqlVersion.Postgres17))
{
    var result = parser17.Parse("SELECT version()");
    Console.WriteLine($"PG 17: {(result.IsSuccess ? "✓ Success" : $"✗ Error: {result.Error}")}");
}

// List available versions
var available = NativeLibraryLoader.GetAvailableVersions();
Console.WriteLine($"Available versions: {string.Join(", ", available)}");
```

### 8. Run Tests

```bash
dotnet test
```

Existing tests should still pass (using default PG 16).

## Troubleshooting

### Library Not Found Error
```
PostgreSqlVersionNotAvailableException: Could not load native library for PostgreSQL 16.
```

**Solution**:
1. Verify file exists in correct path
2. Check filename matches naming convention
3. Ensure proper permissions (Linux/macOS: `chmod +x`)
4. Check platform-specific extension (.dll/.so/.dylib)

### Wrong Architecture
```
BadImageFormatException or similar
```

**Solution**:
- Windows: Build as x64 (not x86)
- macOS: Match your Mac's architecture (Intel vs ARM)
- Linux: Match your system architecture

### Missing Dependencies

**Windows**:
- Install Visual C++ Redistributable if needed

**Linux**:
```bash
ldd libpg_query_16.so  # Check dependencies
```

**macOS**:
```bash
otool -L libpg_query_16.dylib  # Check dependencies
```

## Verification Checklist

Before committing:
- [ ] Both PG 16 and 17 libraries built for your platform
- [ ] Libraries placed in correct runtime directories
- [ ] Filenames follow naming convention
- [ ] Test application runs successfully
- [ ] Both versions can parse queries
- [ ] Version selection works correctly
- [ ] Error messages clear when version missing
- [ ] All existing tests still pass

## After Integration

1. **Update Documentation**
   - Add version selection examples to README
   - Document platform-specific notes
   - Create troubleshooting guide

2. **Create Examples**
   - Version comparison example
   - Migration guide from single to multi-version
   - Error handling examples

3. **Performance Testing**
   - Benchmark version loading overhead
   - Test concurrent version usage
   - Verify memory usage

4. **Package and Release**
   - Update package version
   - Add release notes
   - Publish to NuGet

## Helpful Commands

### Check Current Native Libraries
```powershell
# Windows PowerShell
Get-ChildItem -Recurse -Include *.dll src\libs\Npgquery\Npgquery\runtimes

# Linux/macOS bash
find src/libs/Npgquery/Npgquery/runtimes -name "*.so" -o -name "*.dylib"
```

### Verify Library Architecture
```powershell
# Windows
dumpbin /HEADERS pg_query_16.dll | findstr machine

# Linux  
file libpg_query_16.so

# macOS
lipo -info libpg_query_16.dylib
```

### Test Single Version
```bash
# Set environment to test specific version
dotnet run --project Examples/VersionTest/
```

## Resources

- **libpg_query Repository**: https://github.com/pganalyze/libpg_query
- **Build Documentation**: https://github.com/pganalyze/libpg_query/wiki/Building
- **Release Tags**: https://github.com/pganalyze/libpg_query/releases
- **PostgreSQL Versions**: https://www.postgresql.org/support/versioning/

## Support

If you encounter issues:
1. Check this document's troubleshooting section
2. Review libpg_query build documentation
3. Check GitHub Issues in libpg_query repository
4. Verify your compiler/build tools are up to date

---

Good luck! The infrastructure is ready - just need those native libraries! 🚀
