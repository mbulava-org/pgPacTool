# Execute GitHub Actions - Native Library Build

**Date:** 2026-03-01  
**Branch:** main (after merge)  
**Purpose:** Build native libpg_query libraries for all platforms and PostgreSQL versions

---

## 📋 Step-by-Step Guide

### Step 1: Navigate to GitHub Actions

1. Go to your repository: https://github.com/mbulava-org/pgPacTool
2. Click the **"Actions"** tab at the top
3. Look for the workflow: **"Build Native libpg_query Libraries"**

### Step 2: Trigger the Workflow

1. Click on **"Build Native libpg_query Libraries"** in the left sidebar
2. Click the **"Run workflow"** button (top right)
3. You'll see a dropdown with options:

   **Input Parameters:**
   - **Branch:** Select **`main`** (or your current branch)
   - **PostgreSQL versions:** Enter **`16,17`** (default)
   - **Force rebuild:** Leave **unchecked** (unless you want to rebuild existing files)

4. Click the green **"Run workflow"** button

### Step 3: Monitor Progress

The workflow will:
1. **Build Matrix:** Create jobs for each combination
   - Windows x PostgreSQL 16
   - Windows x PostgreSQL 17
   - Ubuntu x PostgreSQL 16
   - Ubuntu x PostgreSQL 17
   - macOS x PostgreSQL 16
   - macOS x PostgreSQL 17

2. **Each Job** (~5-10 minutes):
   - Checkout libpg_query repository
   - Build native library for that platform/version
   - Upload as artifact

3. **Collect and Commit** (~2 minutes):
   - Download all artifacts
   - Organize into `src/libs/Npgquery/Npgquery/runtimes/` structure
   - Create a Pull Request with the built libraries

**Total Time:** ~30-40 minutes for all platforms

### Step 4: Review the Pull Request

After workflow completes:
1. A Pull Request will be automatically created
2. **Title:** `chore: Update native libpg_query libraries for PostgreSQL X`
3. **Description:** Lists all built libraries

**Review checklist:**
- ✅ All 6 library files present (3 platforms × 2 versions)
- ✅ File sizes look reasonable (~9 MB for libpg_query.so)
- ✅ No merge conflicts
- ✅ CI checks pass

### Step 5: Merge the Pull Request

1. Review the changes
2. Approve the PR
3. **Merge** to main
4. Delete the branch (automated-native-lib-update-...)

---

## 📁 Expected Library Structure

After merge, you should have:

```
src/libs/Npgquery/Npgquery/runtimes/
├── win-x64/
│   └── native/
│       ├── libpg_query_16.dll
│       └── libpg_query_17.dll
├── linux-x64/
│   └── native/
│       ├── libpg_query_16.so
│       └── libpg_query_17.so
└── osx-arm64/  (or osx-x64)
    └── native/
        ├── libpg_query_16.dylib
        └── libpg_query_17.dylib
```

---

## 🔧 Workflow Details

### Workflow File
`.github/workflows/build-native-libraries.yml`

### Key Features
- ✅ **Manual Trigger** - `workflow_dispatch` with parameters
- ✅ **Multi-Platform** - Windows, Linux, macOS
- ✅ **Multi-Version** - Build any PostgreSQL versions (e.g., "16,17,18")
- ✅ **Automatic PR** - Creates PR with built libraries
- ✅ **Smart Naming** - Libraries named `libpg_query_{version}.{ext}`

### Input Parameters

#### PostgreSQL Versions
**Default:** `16,17`  
**Format:** Comma-separated list  
**Examples:**
- `16,17` - Build PG 16 and 17
- `16,17,18` - Build PG 16, 17, and 18 (if adding new version)
- `18` - Build only PG 18 (if adding a single new version)

#### Force Rebuild
**Default:** `false`  
**When to use:**
- `true` - Rebuild even if library files already exist (update/fix)
- `false` - Skip if library already exists (normal operation)

---

## 🚀 Quick Commands

### Option 1: GitHub Web UI (Recommended)
1. Navigate to: https://github.com/mbulava-org/pgPacTool/actions/workflows/build-native-libraries.yml
2. Click "Run workflow"
3. Enter: `16,17`
4. Click "Run workflow"

### Option 2: GitHub CLI
```bash
# Install GitHub CLI: https://cli.github.com/

# Trigger workflow
gh workflow run build-native-libraries.yml \
  --ref main \
  --field pg_versions="16,17" \
  --field force_rebuild=false

# Monitor progress
gh run list --workflow=build-native-libraries.yml
gh run watch
```

### Option 3: GitHub API
```bash
# Using curl
curl -X POST \
  -H "Accept: application/vnd.github+json" \
  -H "Authorization: Bearer YOUR_GITHUB_TOKEN" \
  https://api.github.com/repos/mbulava-org/pgPacTool/actions/workflows/build-native-libraries.yml/dispatches \
  -d '{"ref":"main","inputs":{"pg_versions":"16,17","force_rebuild":"false"}}'
```

---

## 📊 Expected Build Matrix

When you trigger with `pg_versions: "16,17"`:

| Platform | Arch | PG 16 | PG 17 | Output |
|----------|------|-------|-------|--------|
| Windows | x64 | ✅ | ✅ | pg_query.dll |
| Linux | x64 | ✅ | ✅ | libpg_query.so |
| macOS | arm64 | ✅ | ✅ | libpg_query.dylib |

**Total:** 6 library files

---

## ⚠️ Important Notes

### 1. Branch Must Be `main`
The workflow should run on the **`main`** branch after your merge. Make sure to select `main` when triggering.

### 2. Permissions Required
The workflow needs:
- ✅ `contents: write` - To create branch and commit
- ✅ `pull-requests: write` - To create PR

These are typically available by default for repository maintainers.

### 3. First Run Takes Longer
- **First time:** ~40-50 minutes (building libpg_query from source)
- **Subsequent runs:** ~30-40 minutes (faster builds)

### 4. Artifacts Retention
Built libraries are kept as artifacts for **7 days** before the PR is created.

---

## ✅ Verification Steps

### After Workflow Completes

1. **Check Pull Request:**
   - Go to: https://github.com/mbulava-org/pgPacTool/pulls
   - Look for: "chore: Update native libpg_query libraries..."

2. **Verify Files in PR:**
   ```
   ✅ src/libs/Npgquery/Npgquery/runtimes/win-x64/native/libpg_query_16.dll
   ✅ src/libs/Npgquery/Npgquery/runtimes/win-x64/native/libpg_query_17.dll
   ✅ src/libs/Npgquery/Npgquery/runtimes/linux-x64/native/libpg_query_16.so
   ✅ src/libs/Npgquery/Npgquery/runtimes/linux-x64/native/libpg_query_17.so
   ✅ src/libs/Npgquery/Npgquery/runtimes/osx-arm64/native/libpg_query_16.dylib
   ✅ src/libs/Npgquery/Npgquery/runtimes/osx-arm64/native/libpg_query_17.dylib
   ```

3. **Check File Sizes:**
   - Each library should be ~8-10 MB
   - Suspiciously small files (<1 MB) indicate build issues

4. **Verify CI Passes:**
   - The PR should have green checkmarks
   - Build and test workflows should pass

### After Merging PR

1. **Pull Latest Changes:**
   ```bash
   git checkout main
   git pull origin main
   ```

2. **Verify Locally:**
   ```bash
   # Check files exist
   ls -R src/libs/Npgquery/Npgquery/runtimes/
   
   # Build solution
   dotnet build
   
   # Run tests
   dotnet test
   ```

3. **Test Linux Container Tests:**
   ```bash
   dotnet test tests/LinuxContainer.Tests --filter "NativeLibrary"
   ```

---

## 🐛 Troubleshooting

### Workflow Fails on Build Step

**Symptom:** Build fails with compilation errors

**Solutions:**
- Check libpg_query version compatibility
- Verify build tools are installed correctly
- Check build logs for specific errors

### Workflow Succeeds but No PR Created

**Symptom:** Workflow completes but no PR appears

**Causes:**
- No changes detected (libraries already up-to-date)
- Branch protection rules blocking PR creation
- Permission issues

**Check:**
```bash
# See workflow run logs
gh run list --workflow=build-native-libraries.yml
gh run view {run-id} --log
```

### Library Files Missing After Merge

**Symptom:** PR merged but files not in repository

**Solution:**
- Check if PR was actually merged (not closed)
- Pull latest main branch
- Check .gitignore doesn't exclude native libraries

---

## 🔄 Adding a New PostgreSQL Version

For PostgreSQL 18 builds:

1. **Trigger workflow with new version:**
   ```
   pg_versions: "16,17,18"
   ```

2. **Workflow will:**
   - Build libpg_query from `18-latest-dev` branch
   - Generate 3 new library files (win/linux/mac)
   - Add to existing runtimes structure
   - Create PR with all 9 files (3 platforms × 3 versions)

3. **Then validate wrapper parity and tests:**
   - Verify exported methods match the .NET wrapper expectations
   - Rebuild native assets into runtime folders
   - Run version-specific tests for PostgreSQL 18

---

## 📱 Real-Time Monitoring

### Watch Progress
```bash
# Using GitHub CLI
gh run watch

# Using GitHub Web UI
# Navigate to: https://github.com/mbulava-org/pgPacTool/actions
# Click on the running workflow to see live logs
```

### Check Status
```bash
# List recent runs
gh run list --workflow=build-native-libraries.yml --limit 5

# View specific run
gh run view {run-id}
```

---

## 📞 What To Do Now

### Immediate Action
1. ✅ Go to: https://github.com/mbulava-org/pgPacTool/actions
2. ✅ Click: "Build Native libpg_query Libraries"
3. ✅ Click: "Run workflow"
4. ✅ Enter: `16,17`
5. ✅ Click: "Run workflow"
6. ⏳ Wait ~30-40 minutes
7. ✅ Review and merge the PR

### While Waiting
- ☕ Grab coffee (30-40 minutes)
- 📖 Review documentation
- 🧪 Run local tests
- 📝 Plan next features

### After PR Merged
```bash
git pull origin main
dotnet build
dotnet test
dotnet test tests/LinuxContainer.Tests --filter "NativeLibrary"
```

---

## 🎯 Success Criteria

✅ **Workflow completes successfully**  
✅ **PR created with 6+ library files**  
✅ **All files ~8-10 MB each**  
✅ **CI checks pass on PR**  
✅ **PR merged to main**  
✅ **Local build and tests pass**  
✅ **Linux container tests pass**

---

## 📚 Related Documentation

- **Workflow File:** `.github/workflows/build-native-libraries.yml`
- **Multi-Version Support:** `docs/features/multi-version-support/README.md`
- **Native Setup:** `src/libs/Npgquery/NATIVE_SETUP.md`
- **Quick Reference:** `docs/features/multi-version-support/QUICK_REFERENCE.md`

---

**Status:** ✅ READY TO EXECUTE  
**Action Required:** Trigger the workflow on GitHub  
**Estimated Time:** 30-40 minutes  

🚀 **Go trigger that workflow!**
