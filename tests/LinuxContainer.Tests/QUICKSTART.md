# Quick Start: Linux Container Tests

## Prerequisites
1. Install **Docker Desktop** and ensure it's running
2. Verify: `docker --version && docker ps`

## Run Tests

### Quick Test (Single Project)
```bash
# Test DAC library in Linux container (~2 minutes)
dotnet test tests/LinuxContainer.Tests --filter "BuildAndTest_DacTests_InLinuxContainer"
```

### Protobuf Issue Test
```bash
# Specifically test the protobuf deparse fix
dotnet test tests/LinuxContainer.Tests --filter "ProtobufDeparse_ShouldGenerateValidSQL"
```

### Native Library Test
```bash
# Verify libpg_query.so loads correctly
dotnet test tests/LinuxContainer.Tests --filter "NativeLibrary_ShouldLoadWithoutErrors"
```

### Complete CI Simulation
```bash
# Run full CI simulation (~10 minutes) - marked [Explicit]
dotnet test tests/LinuxContainer.Tests --filter "CompleteCI_Simulation"
```

## What Each Test Does

### LinuxContainerTestRunner
- **BuildAndTest_DacTests_InLinuxContainer**: Runs DAC tests in Ubuntu container
- **BuildAndTest_ProjectExtractTests_InLinuxContainer**: Runs extraction tests in Ubuntu container  
- **BuildAndTest_AllProjects_InLinuxContainer**: Runs ALL test projects sequentially
- **Verify_NativeLibraries_LoadOnLinux**: Checks libpg_query.so is valid and loads

### LinuxIssueTests (New!)
- **ProtobufDeparse_ShouldGenerateValidSQL_NotGarbage**: Tests the protobuf corruption fix
- **NativeLibrary_ShouldLoadWithoutErrors_OnLinux**: Comprehensive native library validation
- **AstSqlGenerator_ShouldUseJsonExtraction_NotProtobuf**: Verifies JSON extraction workaround
- **CompleteCI_Simulation_AllTestsShouldPass**: Full GitHub Actions workflow simulation

## Expected Output

### ✅ Success
```
📦 Building Linux container for: mbulava.PostgreSql.Dac.Tests
🐳 Starting container: pgpactool-linux-test-...
⏳ Waiting for container to complete...
📄 Container output:
    Test Run Successful.
    Total tests: 120
         Passed: 120
🏁 Container exit code: 0
✅ Test passed
```

### ❌ Failure (Catches Linux Bugs)
```
📦 Building Linux container for: mbulava.PostgreSql.Dac.Tests
🐳 Starting container: pgpactool-linux-test-...
📄 Container output:
    Failed Generate_ColumnTypeChange_CreatesAlterType
    Error: Output contains \u0012\u0006 (protobuf garbage)
🏁 Container exit code: 1
❌ Test failed
```

## Troubleshooting

### "Docker is not available"
```bash
# Windows: Start Docker Desktop
# Linux: sudo systemctl start docker
# Verify: docker ps
```

### "Container timeout"
- Default timeout: 10 minutes
- Increase in `LinuxContainerTestBase.cs` if needed

### Permission denied (Linux)
```bash
sudo usermod -aG docker $USER
newgrp docker
```

## Performance

| Test | Duration | Image Pull (First Time) |
|------|----------|------------------------|
| Single Project | ~2-3 min | +2-3 min (one-time) |
| Protobuf Test | ~1-2 min | +2-3 min (one-time) |
| Complete CI | ~8-10 min | +2-3 min (one-time) |

**Note:** After first run, Docker images are cached locally.

## When to Run

### Before Pushing to GitHub
```bash
# Catch Linux issues locally
dotnet test tests/LinuxContainer.Tests --filter "ProtobufDeparse"
git push origin feature-branch
```

### After Modifying AST Code
```bash
# Verify AST SQL generation works on Linux
dotnet test tests/LinuxContainer.Tests --filter "AstSqlGenerator"
```

### Weekly/CI Integration
```bash
# Full validation (takes ~10 minutes)
dotnet test tests/LinuxContainer.Tests --filter "CompleteCI_Simulation"
```

## Files

- **LinuxContainerTestBase.cs** - Base class with Docker/container helpers
- **LinuxContainerTestRunner.cs** - Tests for running test projects
- **LinuxIssueTests.cs** - Tests for specific known issues (protobuf, etc.)
- **README.md** - Comprehensive documentation

## Example: Catching Protobuf Bug

**Before Fix:**
```bash
dotnet test tests/LinuxContainer.Tests --filter "ProtobufDeparse"
# ❌ FAILED: Output contains \u0012\u0006PUBLIC
```

**After Fix:**
```bash
dotnet test tests/LinuxContainer.Tests --filter "ProtobufDeparse"
# ✅ PASSED: Generated SQL: ALTER TABLE public.users...
```

---

**Last Updated:** 2026-03-01  
**See Also:** `README.md` for complete documentation
