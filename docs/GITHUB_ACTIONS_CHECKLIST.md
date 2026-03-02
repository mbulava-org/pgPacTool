# GitHub Actions Execution Checklist

**Goal:** Build and deploy native libpg_query libraries for PostgreSQL 16 & 17

---

## ✅ Pre-Flight Checklist

- [x] Branch merged to `main`
- [x] All local changes committed
- [x] Build passing locally
- [x] Ready to trigger GitHub Actions

---

## 🚀 Execution Steps

### 1. Trigger Build Native Libraries Workflow

**URL:** https://github.com/mbulava-org/pgPacTool/actions/workflows/build-native-libraries.yml

**Steps:**
- [ ] Navigate to Actions tab
- [ ] Select "Build Native libpg_query Libraries"
- [ ] Click "Run workflow"
- [ ] Branch: `main`
- [ ] PostgreSQL versions: `16,17`
- [ ] Force rebuild: `false`
- [ ] Click "Run workflow"

**Expected Duration:** 30-40 minutes

---

### 2. Monitor Build Progress

- [ ] Watch workflow run (https://github.com/mbulava-org/pgPacTool/actions)
- [ ] Verify all 6 jobs start:
  - [ ] Windows - PostgreSQL 16
  - [ ] Windows - PostgreSQL 17
  - [ ] Ubuntu - PostgreSQL 16
  - [ ] Ubuntu - PostgreSQL 17
  - [ ] macOS - PostgreSQL 16
  - [ ] macOS - PostgreSQL 17
- [ ] Wait for "Collect and create PR" job to complete

**Status Checks:**
```
✅ Build Matrix: 6/6 jobs succeeded
✅ Collect and Commit: 1/1 job succeeded
✅ PR created successfully
```

---

### 3. Review Pull Request

- [ ] Go to: https://github.com/mbulava-org/pgPacTool/pulls
- [ ] Find PR: "chore: Update native libpg_query libraries for PostgreSQL..."
- [ ] Verify files added:
  ```
  ✅ win-x64/native/libpg_query_16.dll (~9 MB)
  ✅ win-x64/native/libpg_query_17.dll (~9 MB)
  ✅ linux-x64/native/libpg_query_16.so (~9 MB)
  ✅ linux-x64/native/libpg_query_17.so (~9 MB)
  ✅ osx-arm64/native/libpg_query_16.dylib (~9 MB)
  ✅ osx-arm64/native/libpg_query_17.dylib (~9 MB)
  ```
- [ ] Check CI status: All checks should be green ✅
- [ ] Review "Files changed" tab (should show 6 binary files added)

---

### 4. Merge Pull Request

- [ ] Click "Merge pull request"
- [ ] Confirm merge
- [ ] Delete branch (automated-native-lib-update-...)

---

### 5. Verify Locally

```bash
# Pull latest main
git checkout main
git pull origin main

# Verify files exist
ls -R src/libs/Npgquery/Npgquery/runtimes/

# Expected output:
# win-x64/native/:
# libpg_query_16.dll  libpg_query_17.dll
# 
# linux-x64/native/:
# libpg_query_16.so  libpg_query_17.so
# 
# osx-arm64/native/:
# libpg_query_16.dylib  libpg_query_17.dylib
```

- [ ] Files present locally
- [ ] Build solution: `dotnet build`
- [ ] Run tests: `dotnet test`
- [ ] Run Linux container tests: `dotnet test tests/LinuxContainer.Tests --filter "NativeLibrary"`

---

### 6. Verify CI Passes

- [ ] Check latest commit on main has green checkmark
- [ ] Build and Test workflow passes
- [ ] All tests pass on Linux (GitHub Actions)

---

## 📊 Success Criteria

### ✅ All Complete
- [x] Workflow triggered
- [ ] Workflow completed successfully (30-40 min)
- [ ] PR created with 6 library files
- [ ] All CI checks green on PR
- [ ] PR reviewed and approved
- [ ] PR merged to main
- [ ] Local verification passed
- [ ] Main branch CI passing

---

## ⏱️ Timeline

| Step | Duration | Cumulative |
|------|----------|------------|
| Trigger workflow | 1 min | 1 min |
| Build libraries (6 jobs) | 30-40 min | 31-41 min |
| Create PR | 2 min | 33-43 min |
| Review PR | 5 min | 38-48 min |
| Merge PR | 1 min | 39-49 min |
| CI validation | 5-10 min | 44-59 min |

**Total:** ~45 minutes to 1 hour

---

## 🐛 If Something Goes Wrong

### Build Job Fails

**Check:**
- [ ] libpg_query repository has `{version}-latest` branch
- [ ] Build tools available on runner
- [ ] No recent breaking changes in libpg_query

**Fix:**
- Review build logs
- Check version branch exists: https://github.com/pganalyze/libpg_query/branches
- Retry workflow

### PR Not Created

**Check:**
- [ ] "Collect and commit" job completed
- [ ] Changes detected (new or modified files)
- [ ] Permissions correct (contents: write, pull-requests: write)

**Fix:**
- Check workflow logs for "No changes detected"
- Verify permissions in repository settings
- Re-run workflow with `force_rebuild: true`

### CI Fails After Merge

**Check:**
- [ ] All library files committed
- [ ] File sizes correct (~9 MB each)
- [ ] No .gitignore blocking binaries

**Fix:**
- Check main branch CI logs
- Verify native libraries in repository
- Re-run build-native-libraries workflow

---

## 📞 Next Steps After Success

1. **Update Documentation:**
   - Mark native library setup as complete
   - Update version support documentation

2. **Test Multi-Version Support:**
   ```csharp
   using var parser16 = new Parser(PostgreSqlVersion.Postgres16);
   using var parser17 = new Parser(PostgreSqlVersion.Postgres17);
   ```

3. **Run Full Test Suite:**
   ```bash
   dotnet test
   dotnet test tests/LinuxContainer.Tests
   ```

4. **Create GitHub Issues:**
   - Follow `docs/GITHUB_ISSUES_READY.md`
   - Create Issue #1 (Protobuf) and Issue #2 (GrantStmt)

---

## 🎉 Completion

When all checkboxes above are checked:
- ✅ Native libraries built for all platforms
- ✅ Multi-version support fully functional
- ✅ Linux CI/CD working
- ✅ Ready for development and deployment

---

**Document:** `docs/EXECUTE_GITHUB_ACTIONS.md` (detailed guide)  
**This File:** Quick checklist for execution  
**Created:** 2026-03-01  

🚀 **Start the workflow now!**
