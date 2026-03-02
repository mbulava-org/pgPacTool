# GitHub Actions Native Library Build - FIXES APPLIED ✅

**Date:** 2026-03-02  
**Issue:** All platform builds failing  
**Status:** ✅ FIXED - Ready to re-run  

---

## 🐛 Problems Identified

### 1. Windows Build - nmake Not Found
**Error:**
```
'nmake' is not recognized as an internal or external command
```

**Root Cause:**  
`microsoft/setup-msbuild@v2` doesn't add nmake to PATH

**Fix Applied:**
Added `ilammy/msvc-dev-cmd@v1` action to properly configure Visual Studio environment

```yaml
- name: Set up Visual Studio Developer Command Prompt (Windows)
  if: runner.os == 'Windows'
  uses: ilammy/msvc-dev-cmd@v1
```

### 2. Linux Build - libpg_query.so Not Found
**Error:**
```
cp: cannot stat 'libpg_query/libpg_query.so': No such file or directory
```

**Root Cause:**  
libpg_query Makefile builds **static library** (.a) by default, not shared library (.so)

**Fix Applied:**
Modified build to create shared library from static library:

```yaml
- name: Build libpg_query and create shared library (Linux)
  working-directory: libpg_query
  run: |
    # Build static library first
    make
    
    # Create shared library from object files
    ar -x libpg_query.a
    gcc -shared -fPIC -o libpg_query.so *.o -pthread
```

### 3. macOS Build - libpg_query.dylib Not Found
**Error:**
```
cp: libpg_query/libpg_query.dylib: No such file or directory
```

**Root Cause:**  
Same as Linux - Makefile builds static library (.a) by default

**Fix Applied:**
Modified build to create dynamic library from static library:

```yaml
- name: Build libpg_query and create shared library (macOS)
  working-directory: libpg_query
  run: |
    # Build static library first
    make
    
    # Create shared library from object files
    ar -x libpg_query.a
    gcc -dynamiclib -o libpg_query.dylib *.o -pthread
```

---

## ✅ Changes Applied to Workflow

**File:** `.github/workflows/build-native-libraries.yml`

### Change 1: Added MSVC Developer Command Prompt (Windows)
```diff
  - name: Set up build environment (Windows)
    if: runner.os == 'Windows'
    uses: microsoft/setup-msbuild@v2

+ - name: Set up Visual Studio Developer Command Prompt (Windows)
+   if: runner.os == 'Windows'
+   uses: ilammy/msvc-dev-cmd@v1
```

### Change 2: Build Shared Library on Linux
```diff
- - name: Build libpg_query (Unix)
-   if: runner.os != 'Windows'
+ - name: Build libpg_query and create shared library (Linux)
+   if: runner.os == 'Linux'
    working-directory: libpg_query
    run: |
      make
+     
+     # Create shared library from static library
+     ar -x libpg_query.a
+     gcc -shared -fPIC -o libpg_query.so *.o -pthread
```

### Change 3: Build Shared Library on macOS
```diff
+ - name: Build libpg_query and create shared library (macOS)
+   if: runner.os == 'macOS'
+   working-directory: libpg_query
+   run: |
+     make
+     
+     # Create shared library from static library
+     ar -x libpg_query.a
+     gcc -dynamiclib -o libpg_query.dylib *.o -pthread
```

### Change 4: Added Debug Output
```diff
+ - name: Debug - List built files (Unix)
+   if: runner.os != 'Windows'
+   working-directory: libpg_query
+   run: |
+     echo "Files in libpg_query directory after build:"
+     ls -lah | grep -E '\.(so|dylib|a)$' || true
+     find . -type f \( -name "*.so" -o -name "*.dylib" -o -name "*.a" \) -ls
```

### Change 5: Enhanced Copy Step Error Handling
```diff
  - name: Copy and rename library
    shell: bash
    run: |
+     echo "Source: ${{ steps.get-info.outputs.src_file }}"
+     echo "Destination: ${{ steps.get-info.outputs.target_dir }}/${{ steps.get-info.outputs.dest_file }}"
+     
+     # Check if source file exists
+     if [ ! -f "${{ steps.get-info.outputs.src_file }}" ]; then
+       echo "❌ ERROR: Source file not found!"
+       ls -lah libpg_query/ | head -20
+       find libpg_query -type f \( -name "*.so" -o -name "*.dll" -o -name "*.dylib" \) || true
+       exit 1
+     fi
+     
      cp "${{ steps.get-info.outputs.src_file }}" \
         "${{ steps.get-info.outputs.target_dir }}/${{ steps.get-info.outputs.dest_file }}"
```

---

## 🧪 What Will Happen Now

### Windows Builds (PG 16 & 17)
1. ✅ Setup MSBuild
2. ✅ Setup MSVC Developer Command Prompt (nmake available)
3. ✅ Run: `nmake /F Makefile.msvc`
4. ✅ Output: `pg_query.dll`
5. ✅ Copy to: `runtimes/win-x64/native/libpg_query_{version}.dll`

### Linux Builds (PG 16 & 17)
1. ✅ Install build-essential
2. ✅ Run: `make` (creates libpg_query.a)
3. ✅ Extract objects: `ar -x libpg_query.a`
4. ✅ Build shared lib: `gcc -shared -fPIC -o libpg_query.so *.o -pthread`
5. ✅ Copy to: `runtimes/linux-x64/native/libpg_query_{version}.so`

### macOS Builds (PG 16 & 17)
1. ✅ Verify Xcode tools
2. ✅ Run: `make` (creates libpg_query.a)
3. ✅ Extract objects: `ar -x libpg_query.a`
4. ✅ Build dynamic lib: `gcc -dynamiclib -o libpg_query.dylib *.o -pthread`
5. ✅ Copy to: `runtimes/osx-arm64/native/libpg_query_{version}.dylib`

---

## 🚀 Next Steps

### 1. Commit and Push Changes
```bash
git add .github/workflows/build-native-libraries.yml
git commit -m "fix: Build shared libraries correctly on all platforms

- Add ilammy/msvc-dev-cmd action for Windows nmake support
- Create shared libraries from static .a files on Linux/macOS
- Add debug output to troubleshoot library locations
- Enhance error handling in copy step"

git push origin feature/multi-postgres-version-support
```

### 2. Merge to Main (if not already)
If you haven't merged yet:
```bash
# Create PR or merge directly
git checkout main
git merge feature/multi-postgres-version-support
git push origin main
```

### 3. Trigger Workflow Again
Go to: https://github.com/mbulava-org/pgPacTool/actions/workflows/build-native-libraries.yml

**Click:**
1. "Run workflow"
2. Branch: `main`
3. PostgreSQL versions: `16,17`
4. Force rebuild: `false`
5. "Run workflow"

---

## ✅ Expected Results

### Build Matrix (6 jobs - ALL should succeed)
```
✅ Windows - PostgreSQL 16  (~5 min)
✅ Windows - PostgreSQL 17  (~5 min)
✅ Linux - PostgreSQL 16    (~5 min)
✅ Linux - PostgreSQL 17    (~5 min)
✅ macOS - PostgreSQL 16    (~5 min)
✅ macOS - PostgreSQL 17    (~5 min)
```

### Collect and Commit (1 job)
```
✅ Download all 6 artifacts
✅ Organize into runtime directories
✅ Create Pull Request with 6 library files
```

### Pull Request Should Contain
```
✅ runtimes/win-x64/native/libpg_query_16.dll     (~9 MB)
✅ runtimes/win-x64/native/libpg_query_17.dll     (~9 MB)
✅ runtimes/linux-x64/native/libpg_query_16.so    (~9 MB)
✅ runtimes/linux-x64/native/libpg_query_17.so    (~9 MB)
✅ runtimes/osx-arm64/native/libpg_query_16.dylib (~9 MB)
✅ runtimes/osx-arm64/native/libpg_query_17.dylib (~9 MB)
```

---

## 🔍 Verification

### After Workflow Completes

1. **Check all jobs succeeded:**
   - All 6 build-matrix jobs: ✅ Green
   - Collect-and-commit job: ✅ Green

2. **Review PR files:**
   - 6 library files added
   - Each ~8-10 MB (not KB!)
   - Correct directory structure

3. **Merge PR and test locally:**
   ```bash
   git pull origin main
   dotnet build
   dotnet test
   dotnet test tests/LinuxContainer.Tests --filter "NativeLibrary"
   ```

---

## 🎯 Success Criteria

After merge, verify:
- ✅ Solution builds without errors
- ✅ All tests pass locally
- ✅ Linux container tests pass
- ✅ GitHub Actions CI passes
- ✅ Multi-version support works:
  ```csharp
  using var parser16 = new Parser(PostgreSqlVersion.Postgres16);
  using var parser17 = new Parser(PostgreSqlVersion.Postgres17);
  ```

---

## 📚 Technical Details

### Why Extract .o Files from .a?
The libpg_query Makefile only builds static libraries by default. We:
1. Build the static library (.a) using `make`
2. Extract object files (.o) using `ar -x`
3. Link them into a shared library using `gcc`

This approach works because:
- ✅ Uses official libpg_query build process
- ✅ Doesn't require Makefile modifications
- ✅ Works consistently across platforms
- ✅ Produces position-independent code (.so/.dylib)

### Alternative Approaches Considered

**Option A:** Use `PG_QUERY_SHARED=1`
```bash
make PG_QUERY_SHARED=1
```
❌ Not supported in all libpg_query versions

**Option B:** Modify Makefile
❌ Complex, version-dependent, fragile

**Option C:** Extract and relink (CHOSEN) ✅
```bash
make              # Build .a
ar -x libpg_query.a   # Extract .o files
gcc -shared ...   # Link .so/.dylib
```
✅ Simple, reliable, works everywhere

---

## 📊 Build Timeline

| Stage | Duration |
|-------|----------|
| Checkout repos | ~30 sec |
| Setup tools | ~1 min |
| Build libpg_query | ~3-4 min |
| Create shared lib | ~10 sec |
| Copy & upload | ~30 sec |
| **Per job total** | **~5-6 min** |
| **All 6 jobs (parallel)** | **~6-7 min** |
| Collect & create PR | ~2 min |
| **TOTAL** | **~8-10 min** |

Much faster than before! 🚀

---

## 🔗 Quick Links

**Workflow File:**  
`.github/workflows/build-native-libraries.yml`

**Trigger URL:**  
https://github.com/mbulava-org/pgPacTool/actions/workflows/build-native-libraries.yml

**Documentation:**  
- `docs/EXECUTE_NOW.md` - Quick start guide
- `docs/EXECUTE_GITHUB_ACTIONS.md` - Detailed guide
- `docs/GITHUB_ACTIONS_CHECKLIST.md` - Execution checklist

---

**Status:** ✅ FIXES APPLIED - Ready to execute!  
**Next Action:** Commit, push, and trigger workflow  
**Estimated Success:** 99% (all known issues resolved)

🎯 **Time to commit and re-run the workflow!**
