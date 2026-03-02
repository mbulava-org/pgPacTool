# Linux Container Tests

This test project runs all other test projects in Linux containers to verify CI/CD compatibility before pushing to GitHub Actions.

## Purpose

The pgPacTool project experienced Linux-specific issues (like protobuf corruption) that only manifested in GitHub Actions CI builds. This test project helps catch these issues locally by:

1. **Running tests in Linux containers** - Simulates the Ubuntu 24.04 GitHub Actions environment
2. **Verifying native library loading** - Ensures libpg_query.so loads correctly on Linux
3. **Testing cross-platform behavior** - Catches platform-specific bugs before CI

## Prerequisites

### Required
- **Docker Desktop** - Must be installed and running
- **.NET 10 SDK** - For building and running tests

### Installation

**Windows:**
```powershell
# Install Docker Desktop from:
# https://www.docker.com/products/docker-desktop

# Verify Docker is running
docker --version
docker ps
```

**Linux:**
```bash
# Install Docker Engine
curl -fsSL https://get.docker.com -o get-docker.sh
sudo sh get-docker.sh

# Start Docker service
sudo systemctl start docker
sudo systemctl enable docker

# Add user to docker group (optional, avoids sudo)
sudo usermod -aG docker $USER
newgrp docker
```

**macOS:**
```bash
# Install Docker Desktop from:
# https://www.docker.com/products/docker-desktop

# Verify Docker is running
docker --version
docker ps
```

## Usage

### Run All Linux Container Tests
```bash
# From solution root
dotnet test tests/LinuxContainer.Tests/LinuxContainer.Tests.csproj

# Or with filter
dotnet test --filter "Category=LinuxContainer"
```

### Run Specific Test Projects

**Test mbulava.PostgreSql.Dac.Tests in Linux:**
```bash
dotnet test tests/LinuxContainer.Tests --filter "BuildAndTest_DacTests_InLinuxContainer"
```

**Test ProjectExtract-Tests in Linux:**
```bash
dotnet test tests/LinuxContainer.Tests --filter "BuildAndTest_ProjectExtractTests_InLinuxContainer"
```

**Test All Projects (Explicit - Long Running):**
```bash
dotnet test tests/LinuxContainer.Tests --filter "BuildAndTest_AllProjects_InLinuxContainer"
```

### Verify Native Library Loading
```bash
dotnet test tests/LinuxContainer.Tests --filter "Verify_NativeLibraries_LoadOnLinux"
```

## Test Categories

### `LinuxContainer`
All tests that run in Linux containers. Use this to run the full suite:
```bash
dotnet test --filter "Category=LinuxContainer"
```

### `RequiresDocker`
Tests that require Docker to be installed and running. Skipped automatically if Docker is unavailable.

## How It Works

### Container Setup
1. Uses `mcr.microsoft.com/dotnet/sdk:10.0` image (matches GitHub Actions)
2. Mounts solution directory as `/workspace` in container
3. Runs bash scripts to build and test projects
4. Captures stdout/stderr and exit codes
5. Cleans up containers automatically

### Test Flow
```
┌─────────────────────────────────────────────┐
│ 1. Create Linux Container                  │
│    - Image: dotnet/sdk:10.0                │
│    - Mount: Solution → /workspace          │
└─────────────────────────────────────────────┘
                    ↓
┌─────────────────────────────────────────────┐
│ 2. Run Build & Test Script                 │
│    - dotnet restore                        │
│    - dotnet build                          │
│    - dotnet test                           │
└─────────────────────────────────────────────┘
                    ↓
┌─────────────────────────────────────────────┐
│ 3. Capture Results                         │
│    - Exit code                             │
│    - Output logs                           │
│    - Test results                          │
└─────────────────────────────────────────────┘
                    ↓
┌─────────────────────────────────────────────┐
│ 4. Assert & Report                         │
│    - Exit code should be 0                 │
│    - Should contain "Test Run Successful"  │
│    - Display logs in TestContext           │
└─────────────────────────────────────────────┘
```

## Example Output

```
✅ Docker is available
📦 Building Linux container for: mbulava.PostgreSql.Dac.Tests
   Project: tests/mbulava.PostgreSql.Dac.Tests/mbulava.PostgreSql.Dac.Tests.csproj
   Solution Root: C:\Users\user\source\repos\pgPacTool

🐳 Starting container: pgpactool-linux-test-mbulava.PostgreSql.Dac.Tests-abc123

⏳ Waiting for container to complete...

📄 Container output:
==============================================
Linux Container Test Runner
Project: mbulava.PostgreSql.Dac.Tests
==============================================

Environment:
.NET SDK (reflecting any global.json):
 Version:   10.0.0
 Commit:    ...

Runtime Environment:
 OS Name:     ubuntu
 OS Version:  24.04
 OS Platform: Linux
 RID:         linux-x64

Restoring packages...
  Restore completed in 2.5 sec

Building mbulava.PostgreSql.Dac.Tests...
  Build succeeded.
      0 Warning(s)
      0 Error(s)

Running tests for mbulava.PostgreSql.Dac.Tests...
Test run for mbulava.PostgreSql.Dac.Tests.dll (.NET 10.0)
Microsoft (R) Test Execution Command Line Tool Version 18.3.0

Starting test execution, please wait...
  Passed ExtractCommand_MissingRequiredOption_ReturnsError [12 ms]
  Passed Generate_ColumnTypeChange_CreatesAlterType [8 ms]
  Passed Generate_DropColumn_WithDropFlag_CreatesDropColumn [5 ms]
  ...

Test Run Successful.
Total tests: 17
     Passed: 17
     Failed: 0
  Skipped: 0
  Total time: 1.2 seconds

✅ Test run complete

🏁 Container exit code: 0
```

## Troubleshooting

### Docker Not Available
```
⚠️  Docker is not available: Cannot connect to Docker daemon
Skipping Linux container tests. Install Docker Desktop to run these tests.
```

**Solution:**
1. Install Docker Desktop
2. Start Docker Desktop
3. Wait for Docker to be ready (check system tray icon)
4. Run tests again

### Container Timeout
```
❌ Error running container: Container did not exit within 10 minutes
```

**Solution:**
- Check Docker Desktop has enough resources (CPU/Memory)
- Increase timeout in `RunScriptInLinuxContainerAsync` if needed
- Check if container is stuck: `docker ps -a`

### Native Library Missing
```
❌ Missing: src/libs/Npgquery/Npgquery/runtimes/linux-x64/native/libpg_query.so
```

**Solution:**
```bash
# Rebuild native libraries for Linux
.\scripts\Build-NativeLibraries.ps1 -Versions "16,17"

# Or trigger GitHub Actions workflow to build them
```

### Permission Denied (Linux/macOS)
```
ERROR: permission denied while trying to connect to the Docker daemon socket
```

**Solution:**
```bash
# Add user to docker group
sudo usermod -aG docker $USER
newgrp docker

# Or run with sudo (not recommended)
sudo dotnet test tests/LinuxContainer.Tests
```

## CI/CD Integration

### GitHub Actions
These tests run automatically in GitHub Actions to verify Linux compatibility:

```yaml
- name: Run Linux Container Tests
  run: |
    dotnet test tests/LinuxContainer.Tests \
      --filter "Category=LinuxContainer" \
      --logger "console;verbosity=normal"
```

### Local Pre-Push Check
Before pushing changes, run:
```bash
# Quick check
dotnet test tests/LinuxContainer.Tests --filter "BuildAndTest_DacTests"

# Full check (if time permits)
dotnet test tests/LinuxContainer.Tests --filter "Category=LinuxContainer"
```

## Known Issues

### Protobuf Deparse Corruption
**Issue:** `pg_query_deparse_protobuf` returns corrupted output on Linux

**Status:** Workaround implemented (JSON-based SQL extraction)

**Test Coverage:**
- `BuildAndTest_DacTests_InLinuxContainer` - Verifies fix works on Linux
- `Verify_NativeLibraries_LoadOnLinux` - Ensures native library loads correctly

**See:** `docs/KNOWN_ISSUES_PROTOBUF.md`

## Performance

- **Quick Test (Single Project):** ~2-3 minutes
- **Full Test (All Projects):** ~5-10 minutes
- **Native Library Verification:** ~30 seconds

**Note:** First run downloads Docker image (~500 MB), subsequent runs are faster.

## Contributing

When adding new test projects:

1. Add project reference to `LinuxContainer.Tests.csproj`
2. Add test method in `LinuxContainerTestRunner.cs`:
   ```csharp
   [Test]
   [Category("LinuxContainer")]
   [Category("RequiresDocker")]
   public async Task BuildAndTest_NewProject_InLinuxContainer()
   {
       if (!_dockerAvailable)
       {
           Assert.Ignore("Docker is not available.");
           return;
       }

       var testResults = await RunTestsInLinuxContainerAsync(
           "NewProject.Tests",
           "tests/NewProject.Tests/NewProject.Tests.csproj"
       );

       testResults.Should().NotBeNull();
       testResults.ExitCode.Should().Be(0);
       testResults.Output.Should().Contain("Test Run Successful");
   }
   ```

## References

- **Docker.DotNet:** https://github.com/dotnet/Docker.DotNet
- **Testcontainers:** https://dotnet.testcontainers.org/
- **.NET Container Images:** https://hub.docker.com/_/microsoft-dotnet-sdk
- **GitHub Actions Ubuntu:** https://github.com/actions/runner-images/blob/main/images/ubuntu/Ubuntu2404-Readme.md

---

**Project:** pgPacTool  
**Component:** Linux Container Tests  
**Version:** 1.0  
**Last Updated:** 2026-03-01
