# Quick Test Commands Reference

## Run Tests

### Fastest - Smoke Test Only (~5s)
```bash
dotnet test --filter "Category=Smoke"
```

### All Integration Tests (~38s)
```bash
dotnet test --filter "Category=Integration"
```

### PostgreSQL 16 Tests Only
```bash
dotnet test --filter "Category=Postgres16"
```

### PostgreSQL 17 Tests Only
```bash
dotnet test --filter "Category=Postgres17"
```

### All Tests Except Future Versions
```bash
dotnet test --filter "Category!=FutureVersion"
```

### All Tests (including ignored PG18 tests)
```bash
dotnet test
```

### Run Specific Test
```bash
dotnet test --filter "FullyQualifiedName~SmokeTest_PrivilegeExtraction_Works"
```

### Verbose Output
```bash
dotnet test --filter "Category=Smoke" --logger "console;verbosity=detailed"
```

## Build Commands

### Build Solution
```bash
dotnet build
```

### Clean and Rebuild
```bash
dotnet clean
dotnet build
```

### Restore NuGet Packages
```bash
dotnet restore
```

## Git Commands

### Check Current Branch
```bash
git branch --show-current
```

### Check Status
```bash
git status
```

### Stage All Changes
```bash
git add -A
```

### Commit
```bash
git commit -m "fix: Issue #7 - Fix privilege extraction and add multi-version tests"
```

### Push
```bash
git push -u origin feature/issue-7-fix-privilege-extraction
```

### View Commit Log
```bash
git log --oneline -5
```

## Docker Commands (if needed)

### Check Docker Status
```bash
docker ps
```

### List All Containers (including stopped)
```bash
docker ps -a
```

### Remove All Stopped Containers
```bash
docker container prune
```

### List Docker Images
```bash
docker images
```

### Remove Unused Images
```bash
docker image prune
```

## Useful Test Filters

### By Category
```bash
--filter "Category=Smoke"
--filter "Category=Integration"
--filter "Category=Postgres16"
--filter "Category=Postgres17"
```

### By Test Name
```bash
--filter "FullyQualifiedName~ExtractProject"
--filter "FullyQualifiedName~Privilege"
```

### Exclude Categories
```bash
--filter "Category!=FutureVersion"
--filter "Category!=Smoke"
```

### Combine Filters
```bash
--filter "Category=Integration&Category!=FutureVersion"
```

## Quick Validation Workflow

```bash
# 1. Build
dotnet build

# 2. Quick smoke test (5s)
dotnet test --filter "Category=Smoke"

# 3. Full integration tests (40s)
dotnet test --filter "Category=Integration"

# 4. If all pass, commit
git add -A
git commit -m "fix: Issue #7 - Description"
git push
```

## One-Liner: Full Test Suite
```bash
dotnet build && dotnet test --filter "Category!=FutureVersion"
```
