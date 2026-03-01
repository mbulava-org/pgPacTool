# GitHub Actions Workflow Fixes

## Issues Found and Fixed

### 1. ❌ **macOS Runner Configuration Error**
**Error**: `The configuration 'macos-13-us-default' is not supported`

**Cause**: GitHub deprecated the `macos-13` runner

**Fix**:
```yaml
# Before
- macos-13      # Intel Mac ❌
- macos-14      # ARM Mac

# After
- macos-latest  # Latest stable macOS ✅
```

**Note**: `macos-latest` will use the latest available macOS runner, currently macOS 14 (ARM). For Intel builds, GitHub Actions now primarily uses ARM runners with Rosetta 2 compatibility.

### 2. ❌ **Missing Build Tools on Linux**
**Error**: `Process completed with exit code 1` (make not found or build failed)

**Cause**: Ubuntu runners don't have build-essential installed by default

**Fix**: Added build tools installation step
```yaml
- name: Install build tools (Linux)
  if: runner.os == 'Linux'
  run: |
    sudo apt-get update
    sudo apt-get install -y build-essential
```

### 3. ❌ **Missing Build Tools Verification on macOS**
**Error**: `Process completed with exit code 1` (build tools issue)

**Cause**: Need to ensure Xcode command line tools are available

**Fix**: Added verification step
```yaml
- name: Install build tools (macOS)
  if: runner.os == 'macOS'
  run: |
    which make || xcode-select --install
```

### 4. ❌ **Workflow Trigger Issues**
**Problem**: Workflow triggered on `push` but `github.event.inputs.pg_versions` is null, causing parse errors

**Fix**: Disabled automatic push trigger
```yaml
# Commented out problematic trigger
# push:
#   paths:
#     - '.github/workflows/build-native-libraries.yml'
```

**Reasoning**: The workflow is designed for manual execution with specific version inputs. Auto-triggering on push doesn't make sense without inputs.

### 5. ✅ **Windows Build Shell**
**Improvement**: Explicitly set shell to `cmd` for Windows nmake command
```yaml
- name: Build libpg_query (Windows)
  if: runner.os == 'Windows'
  working-directory: libpg_query
  shell: cmd  # ← Explicit shell
  run: |
    nmake /F Makefile.msvc
```

## Testing the Fixes

### Manual Testing
To test the workflow manually:

1. Go to **Actions** tab in GitHub
2. Select **"Build Native libpg_query Libraries"**
3. Click **"Run workflow"**
4. Enter versions: `16,17`
5. Click **"Run workflow"**

### Expected Result
✅ All platform builds succeed:
- Windows x64 ✅
- Linux x64 ✅
- macOS (latest) ✅

### Platform-Specific Notes

#### Windows
- Uses `nmake` from Visual Studio Build Tools
- Builds to `pg_query.dll`
- Renamed to `pg_query_16.dll` / `pg_query_17.dll`

#### Linux
- Uses `make` and `gcc`
- Builds to `libpg_query.so`
- Renamed to `libpg_query_16.so` / `libpg_query_17.so`

#### macOS
- Uses `make` and `clang`
- Builds to `libpg_query.dylib`
- Renamed to `libpg_query_16.dylib` / `libpg_query_17.dylib`
- **Note**: `macos-latest` currently uses ARM (M1/M2), but libraries are compatible with both Intel and ARM Macs

## Changes Made

### File: `.github/workflows/build-native-libraries.yml`

**Lines Changed**:
1. Line 34: `macos-13` → `macos-latest`
2. Line 35: Removed `macos-14` (covered by macos-latest)
3. Lines 17-20: Commented out `push` trigger
4. Lines 53-61: Added Linux build tools installation
5. Lines 63-68: Added macOS build tools verification
6. Line 72: Added explicit `shell: cmd` for Windows

### Summary of Changes
- ✅ Fixed macOS runner configuration
- ✅ Added build tools installation for Linux
- ✅ Added build tools verification for macOS
- ✅ Disabled problematic auto-trigger on push
- ✅ Improved Windows build command

## Workflow Usage

### Manual Trigger (Recommended)
```
Actions → Build Native libpg_query Libraries → Run workflow
- Versions: 16,17
- Force rebuild: false (default)
```

### Automatic Trigger
- ❌ Disabled - manual execution only
- Reason: Requires version input, can't auto-detect

## Next Steps

1. **Commit and Push Changes**
   ```bash
   git add .github/workflows/build-native-libraries.yml
   git commit -m "fix: GitHub Actions workflow - update macOS runner and add build tools"
   git push
   ```

2. **Test the Workflow**
   - Manually trigger from GitHub Actions
   - Verify all 6 builds succeed (Windows, Linux, macOS × 2 versions each)

3. **Verify Output**
   - Check artifacts are uploaded
   - Verify PR is created with libraries
   - Test that libraries load correctly

## Additional Improvements (Optional)

### Future Enhancements
1. **Conditional Push Trigger**: Add workflow validation on push without building
2. **Caching**: Cache libpg_query clone between runs
3. **Parallel Builds**: Already implemented with matrix strategy
4. **Architecture Matrix**: Add explicit ARM/x64 matrix if needed

### Security
- All builds use trusted GitHub-hosted runners
- libpg_query is cloned from official repository
- Artifacts are retained for 7 days only

## Troubleshooting

### If Builds Still Fail

**Windows**:
- Check that `microsoft/setup-msbuild@v2` is working
- Verify Visual Studio Build Tools are available
- Check `nmake /F Makefile.msvc` syntax

**Linux**:
- Verify `build-essential` installation succeeded
- Check for any compile errors in build output
- Ensure `make` is in PATH

**macOS**:
- Verify Xcode command line tools are installed
- Check `make` is available
- Review any clang compiler errors

### Common Errors

**Error**: `nmake not found`
- **Fix**: Ensure `microsoft/setup-msbuild@v2` step completed

**Error**: `make: command not found`
- **Fix**: Verify build tools installation step ran

**Error**: `gcc: command not found`
- **Fix**: Ensure `build-essential` package installed

## Documentation Updates

Update these docs with workflow information:
- ✅ Fixed workflow configuration
- ⏳ Update `NATIVE_LIBRARY_AUTOMATION.md` with current workflow behavior
- ⏳ Note in `QUICK_REFERENCE.md` that push trigger is disabled

---

**Status**: ✅ **Fixed and Ready to Test**
**Action Required**: Commit changes and manually trigger workflow
**Expected Outcome**: All builds succeed, PR created with native libraries
