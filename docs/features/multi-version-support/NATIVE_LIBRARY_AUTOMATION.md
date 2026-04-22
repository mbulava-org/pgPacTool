# Native Library Build Automation

This directory contains automation tools for building and managing native `libpg_query` libraries for multiple PostgreSQL versions.

## Overview

The Npgquery project supports multiple PostgreSQL versions through version-specific native libraries. Instead of manually building libraries on each platform, we use:

1. **GitHub Actions** - Automated builds for all platforms
2. **PowerShell Script** - Local builds for development/testing

## Quick Start

### Option 1: GitHub Actions (Recommended)

**For Production/Release Builds**

1. Go to **Actions** tab in GitHub
2. Select **"Build Native libpg_query Libraries"**
3. Click **"Run workflow"**
4. Enter PostgreSQL versions (e.g., `16,17` or `16,17,18` for future versions)
5. Click **"Run workflow"**

> **Supported Versions**: Currently PostgreSQL 16 and 17. Older versions (14, 15) may be added in the future if needed.

The workflow will:
- Build libraries for Windows, Linux, macOS (Intel & ARM)
- Organize them in runtime directories
- Create a Pull Request with the changes

### Option 2: Local Build Script

**For Development/Testing**

```powershell
# Build PostgreSQL 16 and 17 (default)
.\scripts\Build-NativeLibraries.ps1

# Build specific versions (example with future version 18)
.\scripts\Build-NativeLibraries.ps1 -Versions "16,17,18"

# Force rebuild existing libraries
.\scripts\Build-NativeLibraries.ps1 -Force

# Clean up after building
.\scripts\Build-NativeLibraries.ps1 -Clean
```

**Note**: Local script only builds for your current platform. Use GitHub Actions for complete multi-platform builds.

> **Version Support**: Only PostgreSQL 16+ is currently supported. If you need older versions (14, 15), they can be added following the same process - see [MULTI_VERSION_DESIGN.md](MULTI_VERSION_DESIGN.md) for details.

## Adding New PostgreSQL Versions

### Example: Adding PostgreSQL 18

1. **Update the Enum** (`src/libs/Npgquery/Npgquery/PostgreSqlVersion.cs`):
   ```csharp
   public enum PostgreSqlVersion
   {
       Postgres16 = 16,
       Postgres17 = 17,
       Postgres18 = 18  // Add this
   }
   ```

2. **Update Extension Methods** in the same file:
   ```csharp
   public static string ToLibrarySuffix(this PostgreSqlVersion version)
   {
       return version switch
       {
           PostgreSqlVersion.Postgres16 => "16",
           PostgreSqlVersion.Postgres17 => "17",
           PostgreSqlVersion.Postgres18 => "18",  // Add this
           _ => throw new ArgumentOutOfRangeException(...)
       };
   }
   ```

3. **Run the Build Automation**:
   - **GitHub Actions**: Run workflow with versions `16,17,18`
   - **Local Script**: `.\scripts\Build-NativeLibraries.ps1 -Versions "16,17,18"`

4. **Test**:
   ```csharp
   using var parser = new Parser(PostgreSqlVersion.Postgres18);
   var result = parser.Parse("SELECT version()");
   Assert.True(result.IsSuccess);
   ```

That's it! The infrastructure handles the rest.

## Directory Structure

After building, libraries are organized as:

```
src/libs/Npgquery/Npgquery/runtimes/
├── win-x64/native/
│   ├── pg_query_16.dll
│   ├── pg_query_17.dll
│   └── pg_query_18.dll
├── linux-x64/native/
│   ├── libpg_query_16.so
│   ├── libpg_query_17.so
│   └── libpg_query_18.so
├── osx-x64/native/
│   ├── libpg_query_16.dylib
│   ├── libpg_query_17.dylib
│   └── libpg_query_18.dylib
└── osx-arm64/native/
    ├── libpg_query_16.dylib
    ├── libpg_query_17.dylib
    └── libpg_query_18.dylib
```

## GitHub Actions Workflow Details

**File**: `.github/workflows/build-native-libraries.yml`

### Triggers
- **Manual**: Workflow dispatch with version selection
- **Automatic**: When workflow file or build scripts change

### Build Matrix
- **Platforms**: Windows, Linux, macOS (Intel), macOS (ARM)
- **Versions**: Configurable via input parameter
- **Output**: Version-specific libraries for each platform

### Process
1. **Build**: Each platform builds specified PostgreSQL versions
2. **Artifact Upload**: Libraries uploaded as artifacts
3. **Collection**: All artifacts downloaded and organized
4. **Pull Request**: Automated PR with updated libraries

### Environment Variables
- `NPGQUERY_PROJECT_PATH`: Path to Npgquery project in repo

### Workflow Outputs
- Build summary in GitHub Actions UI
- Artifacts available for 7 days
- Pull Request with organized libraries

## Local Build Script Details

**File**: `scripts/Build-NativeLibraries.ps1`

### Prerequisites
- **Windows**: Visual Studio 2019+ or Build Tools
- **Linux/macOS**: GCC/Clang and Make
- **All**: Git

### Parameters

| Parameter | Description | Default |
|-----------|-------------|---------|
| `-Versions` | Comma-separated PG versions | `"16,17"` |
| `-LibPgQueryPath` | Clone location | `$env:TEMP\libpg_query_build` |
| `-Force` | Force rebuild | `false` |
| `-Clean` | Remove clone after build | `false` |

### What It Does
1. Detects your platform and architecture
2. Clones libpg_query repository (if needed)
3. For each version:
   - Checks out version-specific branch
   - Builds native library
   - Copies to runtime directory with proper naming
4. Provides build summary

### Example Output
```
=== libpg_query Native Library Builder ===

PostgreSQL versions to build: 16, 17

Platform: Windows
Runtime Identifier: win-x64
Library naming: pg_query_XX.dll

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
Building PostgreSQL 16
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

✓ Checked out 16-latest
✓ Clean complete
✓ Build complete
✓ Success!
  File: pg_query_16.dll
  Size: 3.45 MB

[... similar for version 17 ...]

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
Build Summary
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

Libraries in win-x64/native:
  ✓ pg_query_16.dll (3.45 MB)
  ✓ pg_query_17.dll (3.52 MB)

Total: 2 libraries

=== Build Complete ===
```

## Troubleshooting

### Build Fails on GitHub Actions

**Check**:
1. Branch exists for the requested version (`{version}-latest` for most versions, `18-latest-dev` for PostgreSQL 18)
2. libpg_query repository structure hasn't changed
3. Build logs in Actions tab for specific errors

**Solutions**:
- For new versions, verify branch exists in libpg_query repo
- Check if Makefile.msvc or Makefile changed upstream
- Review GitHub Actions logs for compiler errors

### Local Build Fails

**Common Issues**:

1. **"Branch not found"**
   - Version doesn't exist yet in libpg_query
   - Wait for libpg_query to create the branch

2. **"Compiler not found"** (Windows)
   ```powershell
   # Install Visual Studio Build Tools
   # Or set up environment:
   & "C:\Program Files\Microsoft Visual Studio\2022\Community\Common7\Tools\Launch-VsDevShell.ps1"
   ```

3. **"Make failed"** (Linux/macOS)
   ```bash
   # Install build tools
   # Ubuntu/Debian:
   sudo apt-get install build-essential
   
   # macOS:
   xcode-select --install
   ```

4. **"Permission denied"** (Linux/macOS)
   ```bash
   chmod +x scripts/Build-NativeLibraries.ps1
   ```

### Library Not Loading

**Verify**:
```csharp
// Check available versions
var versions = NativeLibraryLoader.GetAvailableVersions();
Console.WriteLine($"Available: {string.Join(", ", versions)}");

// Try to load specific version
try
{
    using var parser = new Parser(PostgreSqlVersion.Postgres17);
    Console.WriteLine("✓ PG 17 loaded successfully");
}
catch (PostgreSqlVersionNotAvailableException ex)
{
    Console.WriteLine($"✗ Error: {ex.Message}");
    Console.WriteLine($"Available: {string.Join(", ", ex.AvailableVersions)}");
}
```

## CI/CD Integration

### Automatic Updates

Set up a scheduled workflow to check for new libpg_query releases:

```yaml
# .github/workflows/check-libpg-updates.yml
on:
  schedule:
    - cron: '0 0 * * 0'  # Weekly on Sunday
  workflow_dispatch:

jobs:
  check-updates:
    runs-on: ubuntu-latest
    steps:
      - name: Check for new releases
        # ... check libpg_query tags
      - name: Trigger build workflow
        # ... if new version found
```

### Release Process

1. **On New PostgreSQL Major Version**:
   - Add enum value
   - Update extension methods
   - Run build automation
   - Test thoroughly
   - Update documentation

2. **On Library Updates** (minor/patch):
   - Run build automation with `-Force`
   - Verify no breaking changes
   - Update package version

## Best Practices

1. **Version Management**
   - Keep at least 2 recent major versions (current + previous)
   - Drop versions older than 3 major releases
   - Document version support in README

2. **Testing**
   - Test version switching after adding new versions
   - Verify backward compatibility
   - Run full test suite against each version

3. **Documentation**
   - Update main README when adding versions
   - Document any version-specific quirks
   - Keep CHANGELOG updated

4. **Release Cadence**
   - Rebuild libraries when libpg_query updates
   - Check for updates monthly
   - Rebuild on PostgreSQL major releases

## FAQ

**Q: Why not include all versions by default?**
A: Package size. Each version adds ~3-5 MB × 4 platforms = 12-20 MB. We include stable recent versions.

**Q: Can I build only specific platforms?**
A: Yes, for local development. For releases, use GitHub Actions for complete builds.

**Q: How do I know which libpg_query version to use?**
A: Use the `{version}-latest` branch (e.g., `17-latest` for PostgreSQL 17). These track the latest stable release.

**Q: What about older versions like PostgreSQL 12-15?**
A: They can be added the same way. Just update the enum and run the build automation with those versions.

**Q: Do I need to rebuild after every libpg_query update?**
A: Not necessarily. Rebuild when:
- New PostgreSQL major version released
- libpg_query has bug fixes you need
- Preparing a new Npgquery release

## Resources

- **libpg_query Repository**: https://github.com/pganalyze/libpg_query
- **PostgreSQL Versions**: https://www.postgresql.org/support/versioning/
- **GitHub Actions Docs**: https://docs.github.com/actions
- **Runtime Identifiers**: https://learn.microsoft.com/dotnet/core/rid-catalog

## Contributing

When contributing library updates:

1. Use the automation tools (don't manually build)
2. Test all available versions
3. Update documentation if adding new versions
4. Include build logs in PR description

## Support

For issues with:
- **Build automation**: Open issue in this repository
- **libpg_query build**: Check libpg_query repository
- **Native library loading**: Check NativeLibraryLoader code
- **Version-specific issues**: Indicate which PostgreSQL version

---

**Last Updated**: [Current Date]
**Supported Versions**: 16, 17 (more can be added easily)
**Platforms**: Windows x64, Linux x64, macOS x64, macOS ARM64
