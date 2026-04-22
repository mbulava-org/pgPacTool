# Quick Reference: Multi-Version Support

## ⚠️ IMPORTANT: Version Compatibility

**Each PostgreSQL version may have breaking changes!**

Before adding a new version:
1. Run `.\scripts\Analyze-VersionDifferences.ps1 -BaseVersion 16 -CompareVersion 17`
2. Review generated report in `docs/version-differences/`
3. Update models if protobuf schema changed
4. Add compatibility layer if API changed
5. Create version-specific tests

**See**: `docs/VERSION_COMPATIBILITY_STRATEGY.md` for full details.

---

## Adding a New PostgreSQL Version (e.g., PostgreSQL 18)

### 0. **FIRST: Analyze Version Differences** ⚠️

```powershell
# Analyze breaking changes
.\scripts\Analyze-VersionDifferences.ps1 -BaseVersion 17 -CompareVersion 18 -Detailed

# Review the report
code docs\version-differences\PG18_CHANGES.md
```

**If breaking changes found**: Update models and compatibility layer BEFORE proceeding.

### 1. Update Code (3 changes)

**File**: `src/libs/Npgquery/Npgquery/PostgreSqlVersion.cs`

```csharp
public enum PostgreSqlVersion
{
    Postgres16 = 16,
    Postgres17 = 17,
    Postgres18 = 18  // ← Add this
}
```

Update all switch statements in extension methods:
```csharp
public static string ToLibrarySuffix(this PostgreSqlVersion version)
{
    return version switch
    {
        PostgreSqlVersion.Postgres16 => "16",
        PostgreSqlVersion.Postgres17 => "17",
        PostgreSqlVersion.Postgres18 => "18",  // ← Add this
        _ => throw new ArgumentOutOfRangeException(...)
    };
}
```

Repeat for `ToVersionString()` and `ToVersionNumber()`.

### 2. Build Libraries

**GitHub Actions** (recommended):
```
Actions → Build Native libpg_query Libraries → Run workflow
Versions: 16,17,18
```

**Local** (your platform only):
```powershell
.\scripts\Build-NativeLibraries.ps1 -Versions "16,17,18"
```

### 3. Test

```csharp
using var parser = new Parser(PostgreSqlVersion.Postgres18);
var result = parser.Parse("SELECT version()");
Assert.True(result.IsSuccess);
```

### 4. Commit

```bash
git add .
git commit -m "feat: Add PostgreSQL 18 support"
git push
```

Done! 🎉

---

## Common Tasks

### Check Available Versions
```csharp
var versions = NativeLibraryLoader.GetAvailableVersions();
Console.WriteLine($"Available: {string.Join(", ", versions)}");
```

### Use Specific Version
```csharp
using var parser = new Parser(PostgreSqlVersion.Postgres17);
```

### Backward Compatible (Default PG 16)
```csharp
using var parser = new Parser();  // Uses PostgreSQL 16
```

### Handle Missing Version
```csharp
try
{
    var parser = new Parser(PostgreSqlVersion.Postgres18);
}
catch (PostgreSqlVersionNotAvailableException ex)
{
    Console.WriteLine($"Version {ex.RequestedVersion} not available");
    Console.WriteLine($"Available: {string.Join(", ", ex.AvailableVersions)}");
}
```

---

## File Locations

| Purpose | Location |
|---------|----------|
| Version enum | `src/libs/Npgquery/Npgquery/PostgreSqlVersion.cs` |
| Native loader | `src/libs/Npgquery/Npgquery/Native/NativeLibraryLoader.cs` |
| Parser class | `src/libs/Npgquery/Npgquery/Npgquery.cs` |
| Build script | `scripts/Build-NativeLibraries.ps1` |
| GitHub workflow | `.github/workflows/build-native-libraries.yml` |
| Libraries | `src/libs/Npgquery/Npgquery/runtimes/{rid}/native/` |

---

## Native Library Naming

| Platform | Pattern | Example |
|----------|---------|---------|
| Windows | `libpg_query_{ver}.dll` | `libpg_query_16.dll` |
| Linux | `libpg_query_{ver}.so` | `libpg_query_16.so` |
| macOS | `libpg_query_{ver}.dylib` | `libpg_query_16.dylib` |

---

## Runtime Identifiers (RID)

- `win-x64` - Windows 64-bit
- `linux-x64` - Linux 64-bit
- `osx-x64` - macOS Intel
- `osx-arm64` - macOS Apple Silicon

---

## Build Automation Commands

### GitHub Actions
```
Manual trigger with version selection in GitHub UI
```

### Local Script
```powershell
# Default (16, 17)
.\scripts\Build-NativeLibraries.ps1

# Supported versions
.\scripts\Build-NativeLibraries.ps1 -Versions "15,16,17,18"

# Force rebuild
.\scripts\Build-NativeLibraries.ps1 -Force

# Clean after build
.\scripts\Build-NativeLibraries.ps1 -Clean
```

> **Note**: Currently only PostgreSQL 16 and 17 are supported. Older versions (14, 15) may be added in the future if there is demand.

---

## Testing Checklist

After adding/updating libraries:

- [ ] Build succeeds: `dotnet build`
- [ ] Tests pass: `dotnet test`
- [ ] Version detection works
- [ ] Can parse with each version
- [ ] Error handling for missing versions
- [ ] Libraries exist for all platforms (if release)

---

## Troubleshooting

### "Version not available"
→ Check library exists in `runtimes/{rid}/native/`
→ Verify filename: `{lib}pg_query_{ver}.{ext}`

### Build fails
→ Check build logs in Actions tab
→ Verify branch exists: `{ver}-latest` in libpg_query

### Wrong version used
→ Check Parser constructor parameter
→ Verify `_version` field in debugger

---

## Documentation Files

- **MULTI_VERSION_DESIGN.md** - Architecture
- **IMPLEMENTATION_COMPLETE.md** - What was built
- **NATIVE_LIBRARY_AUTOMATION.md** - Full automation docs
- **NEXT_STEPS.md** - Getting started guide
- **QUICK_REFERENCE.md** - This file

---

**Need Help?** 
- Check `docs/NATIVE_LIBRARY_AUTOMATION.md` for detailed docs
- Open an issue with build logs
- Review libpg_query repository for version availability
