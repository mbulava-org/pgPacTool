# 🎯 Solution: Pre-Compiled Native Libraries for Npgquery

## Problem

Npgquery wraps **libpg_query**, which requires compiling native code from PostgreSQL source. This is:
- ⏱️ **Time-consuming** (5-10 minutes per build)
- 🔧 **Complex** (requires C compiler, PostgreSQL dependencies)
- 💻 **Platform-specific** (Windows, Linux, macOS)
- 🚫 **Unnecessary** for normal development

## Solution Architecture

### ✅ What We'll Do

1. **Include pre-compiled native binaries** in the repository
2. **Only build C# wrapper code** on each build (fast!)
3. **Copy native binaries to output** directory automatically
4. **Support multiple platforms** (Windows, Linux, macOS)
5. **Only rebuild native code** when updating PostgreSQL version (rare)

### 📁 Proposed Structure

```
src/libs/Npgquery/
├── Npgquery/
│   ├── Npgquery.csproj                    # C# wrapper project
│   ├── Native/                            # C# P/Invoke code
│   ├── Protos/                            # Protocol buffers
│   └── runtimes/                          # ← PRE-COMPILED NATIVE LIBRARIES
│       ├── win-x64/
│       │   └── native/
│       │       └── pg_query.dll           # Windows x64 (PRE-BUILT)
│       ├── win-arm64/
│       │   └── native/
│       │       └── pg_query.dll           # Windows ARM64 (PRE-BUILT)
│       ├── linux-x64/
│       │   └── native/
│       │       └── pg_query.so            # Linux x64 (PRE-BUILT)
│       ├── linux-arm64/
│       │   └── native/
│       │       └── pg_query.so            # Linux ARM64 (PRE-BUILT)
│       ├── osx-x64/
│       │   └── native/
│       │       └── libpg_query.dylib      # macOS Intel (PRE-BUILT)
│       └── osx-arm64/
│           └── native/
│               └── libpg_query.dylib      # macOS Apple Silicon (PRE-BUILT)
├── Npgquery.Tests/
└── Examples/
```

## Implementation Steps

### Step 1: Copy Native Libraries from NuGet Package

```bash
# Create runtimes structure
mkdir -p src/libs/Npgquery/Npgquery/runtimes/win-x64/native
mkdir -p src/libs/Npgquery/Npgquery/runtimes/linux-x64/native

# Copy from NuGet cache
copy "$env:USERPROFILE\.nuget\packages\mbulava-org.npgquery\1.0.0.41-beta\runtimes\win-x64\native\pg_query.dll" src/libs/Npgquery/Npgquery/runtimes/win-x64/native/
copy "$env:USERPROFILE\.nuget\packages\mbulava-org.npgquery\1.0.0.41-beta\runtimes\linux-x64\native\pg_query.so" src/libs/Npgquery/Npgquery/runtimes/linux-x64/native/
```

### Step 2: Update Npgquery.csproj

Add these items to copy native libraries to output:

```xml
<ItemGroup>
  <!-- Include all native libraries from runtimes folder -->
  <Content Include="runtimes\**\*">
    <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
    <Link>runtimes\%(RecursiveDir)%(FileName)%(Extension)</Link>
  </Content>
</ItemGroup>

<ItemGroup>
  <!-- Ensure native libraries are included in publish -->
  <None Include="runtimes\**\*" Pack="true" PackagePath="runtimes\" />
</ItemGroup>
```

### Step 3: Commit Native Libraries to Git

```bash
git add src/libs/Npgquery/Npgquery/runtimes/
git commit -m "Add pre-compiled native libpg_query libraries"
```

## Build Process

### Fast Builds (99% of the time) ⚡
```
1. Compile C# code                         (~5 seconds)
2. Copy pre-compiled native libraries      (~instant)
3. Done!                                   (~5 seconds total)
```

### When to Rebuild Native Libraries (rare) 🔨

Only rebuild native libraries when:
- Updating to a new PostgreSQL version
- Fixing a bug in libpg_query
- Adding support for a new platform

**Frequency:** Once per year or less

## Benefits

### ✅ For Developers
- **Fast builds** (~5 seconds vs 5-10 minutes)
- **No native tooling required** (no C compiler, CMake, etc.)
- **Works on any machine** immediately after clone
- **Cross-platform support** included

### ✅ For CI/CD
- **Fast pipelines** (no native compilation step)
- **Reproducible builds** (same binaries every time)
- **Smaller build agents** (no C compiler needed)
- **Parallel builds** work reliably

### ✅ For Public Release
- **All dependencies included** in repository
- **Self-contained** - no external dependencies
- **Easy to clone and build**
- **Professional structure**

## Platform Support

| Platform | Architecture | Binary | Size | Status |
|----------|-------------|--------|------|--------|
| Windows | x64 | pg_query.dll | ~3.5 MB | ✅ Available |
| Windows | ARM64 | pg_query.dll | ~3.5 MB | 🔄 Need to source |
| Linux | x64 | pg_query.so | ~3 MB | ✅ Available |
| Linux | ARM64 | pg_query.so | ~3 MB | 🔄 Need to source |
| macOS | x64 | libpg_query.dylib | ~3 MB | 🔄 Need to source |
| macOS | ARM64 | libpg_query.dylib | ~3 MB | 🔄 Need to source |

**Note:** We can start with Windows x64 and Linux x64 (most common), then add others as needed.

## Size Impact

### Repository Size
- **Per platform:** ~3-3.5 MB
- **All 6 platforms:** ~20 MB total
- **Acceptable?** ✅ Yes (modern repos commonly have binary assets)

### Benefits vs. Cost
- **Cost:** +20 MB repo size
- **Benefit:** -5-10 minutes per build × unlimited builds
- **ROI:** Excellent!

## Updating Native Libraries

### When PostgreSQL Updates (Rare)

```bash
# 1. Build new native libraries (on appropriate platform)
cd src/libs/Npgquery/Native
./build-native.sh --platform linux-x64 --pg-version 17

# 2. Replace old binaries
cp build/linux-x64/pg_query.so ../Npgquery/runtimes/linux-x64/native/

# 3. Test
dotnet test

# 4. Commit
git add src/libs/Npgquery/Npgquery/runtimes/
git commit -m "Update libpg_query to PostgreSQL 17"
```

## Alternative Considered: Git LFS

We could use Git LFS (Large File Storage) for the binaries, but:
- ❌ Adds complexity (requires LFS setup)
- ❌ Requires LFS on CI/CD
- ❌ Not necessary for ~20 MB
- ✅ Our approach is simpler

## Documentation

### For Contributors

Create `NATIVE_LIBRARIES.md`:

```markdown
# Native Libraries

## What Are These?

The `runtimes/` folder contains pre-compiled libpg_query libraries.
These are **NOT** rebuilt on every build.

## When to Update

Only update these when:
1. Upgrading to a new PostgreSQL version
2. Fixing a security vulnerability
3. Adding a new platform

## How to Update

See NATIVE_SETUP.md for build instructions.
```

## Verification

After implementing, verify:

```bash
# 1. Clean build
dotnet clean
dotnet build

# 2. Check output directory
ls src/libs/Npgquery/Npgquery/bin/Debug/net10.0/runtimes/

# 3. Should see:
# runtimes/
#   win-x64/native/pg_query.dll
#   linux-x64/native/pg_query.so

# 4. Run tests
dotnet test

# 5. Build should be FAST (< 10 seconds)
```

## Summary

✅ **Include pre-compiled native libraries in repo**  
✅ **Fast builds (only C# compilation)**  
✅ **No native tooling required**  
✅ **Cross-platform support**  
✅ **Self-contained repository**  
✅ **Public-release ready**  

---

**Next Action:** Copy native libraries from NuGet package and update .csproj
