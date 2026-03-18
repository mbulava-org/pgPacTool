# Workflow Test Filter Fix

## Issue

The GitHub Actions workflow was failing because it was trying to run `LinuxContainer.Tests` which are designed for Windows developers to test Linux compatibility during development, not for CI/CD execution.

### Root Cause

The workflow was NOT filtering tests correctly. It should:
1. ✅ **RUN Integration tests** - These use Docker/Testcontainers and SHOULD run in CI/CD
2. ❌ **SKIP LinuxContainer.Tests** - These are for Windows developers testing Linux compatibility

The `LinuxContainer.Tests` project is marked with `[Category("LinuxContainer")]` and is designed for local development use to ensure cross-platform compatibility when developing on Windows.

### Failed Tests Example

```
Setup failed for test fixture ProjectExtract_Tests.Integration.Postgres16IntegrationTests
SetUp : Docker.DotNet.DockerImageNotFoundException : Docker API responded with status code=NotFound, 
response={"message":"No such image: postgres:16"}
```

This was caused by the runner being Windows-based and Docker not being properly configured.

## Solution

1. **Changed test filter** to skip only `LinuxContainer` tests:
   ```yaml
   --filter "Category!=LinuxContainer"
   ```

2. **Changed runner to ubuntu-latest** which has Docker pre-installed and works better with Testcontainers:
   ```yaml
   runs-on: ubuntu-latest
   ```

This allows Integration tests (PostgreSQL 16/17 tests) to run while skipping LinuxContainer tests.

## Changes Made

### 1. Workflow File (.github/workflows/publish-preview.yml)

**Before:**
```yaml
runs-on: windows-latest
- name: Run unit tests (non-Docker)
  run: dotnet test --filter "Category!=Integration"
```

**After:**
```yaml
runs-on: ubuntu-latest
- name: Run tests (include Docker, skip LinuxContainer)
  run: dotnet test --filter "Category!=LinuxContainer"
```

## Test Categories in pgPacTool

### Unit Tests (Run in CI/CD)
- No category or categories like `Functions`, `Views`, `Triggers`
- Run in-memory without external dependencies
- Fast execution (~100ms each)
- Examples: parsing, validation, compilation logic

### Integration Tests (Run in CI/CD) ✅ NOW INCLUDED
- `[Category("Integration")]`
- `[Category("Postgres16")]` or `[Category("Postgres17")]`
- Require Docker/Testcontainers
- Spin up actual PostgreSQL containers
- Test real database operations
- Examples:
  - `Postgres16IntegrationTests`
  - `Postgres17IntegrationTests`
  - Tests in `ProjectExtract-Tests/Integration/`

### LinuxContainer Tests (Skipped in CI/CD) ❌ EXCLUDED
- `[Category("LinuxContainer")]`
- `[Category("Linux")]` + `[Category("Container")]`
- Designed for Windows developers to test Linux compatibility
- Run full test suite inside Linux Docker containers
- Not suitable for CI/CD
- Examples: Tests in `LinuxContainer.Tests/` project

### How to Run Locally

**Unit + Integration tests (CI/CD mode):**
```bash
dotnet test --filter "Category!=LinuxContainer"
```

**All tests including LinuxContainer:**
```bash
# Requires Docker Desktop
dotnet test
```

**Only Integration tests:**
```bash
dotnet test --filter "Category=Integration"
```

**Only LinuxContainer tests:**
```bash
dotnet test --filter "Category=LinuxContainer"
```

## Expected Test Results

### Before Fix (Workflow Failed)
- Windows runner couldn't pull Docker images properly
- Integration tests failed with Docker errors
- Workflow marked as failed

### After Fix (Workflow Succeeds)
- Ubuntu runner with pre-installed Docker
- Integration tests run (~20 tests with Postgres 16/17)
- Unit tests run (~416 tests)
- LinuxContainer tests skipped (~30 tests)
- Total: ~436 tests run, ~30 skipped
- Workflow completes successfully
- Packages published to NuGet.org

## Verification

To verify the fix works:

1. **Push to preview1 branch:**
   ```bash
   git add .
   git commit -m "fix: run Integration tests with Docker, skip LinuxContainer tests"
   git push origin preview1
   ```

2. **Check workflow:**
   - Go to https://github.com/mbulava-org/pgPacTool/actions
   - Verify "Build and Test" job passes on ubuntu-latest
   - Check test output shows Integration tests running
   - Verify LinuxContainer tests are skipped

3. **Expected output:**
   ```
   Test summary: total: ~466, failed: 0, succeeded: ~436, skipped: ~30
   ```

## Benefits

✅ **CI/CD tests real scenarios** - Integration tests run against actual PostgreSQL  
✅ **Validates PostgreSQL 16 & 17 support** - Multi-version testing in CI/CD  
✅ **Better coverage** - Real database operations tested, not just mocks  
✅ **Reliable** - Ubuntu runners have stable Docker support  
✅ **Fast** - Ubuntu runners boot quickly  
✅ **Developers can still test Linux compatibility** - LinuxContainer tests available locally  

## Why This Design?

**Integration Tests (Run in CI/CD):**
- Validate core functionality against real PostgreSQL
- Test schema extraction from Postgres 16 and 17
- Ensure Testcontainers integration works
- Critical for quality assurance before release

**LinuxContainer Tests (Local development only):**
- Used by Windows developers to ensure Linux compatibility
- Run entire test suite inside Linux Docker containers
- Test native library loading on Linux
- Verify no Windows-specific dependencies
- Too resource-intensive for CI/CD (spin up full Linux containers)

---

**Status**: ✅ Fixed and ready for testing  
**Next Action**: Push to preview1 to verify Integration tests run successfully
