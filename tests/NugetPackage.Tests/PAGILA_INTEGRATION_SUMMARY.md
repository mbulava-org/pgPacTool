# Pagila Integration Tests - Implementation Summary

## Overview

Added comprehensive integration tests that validate the complete **MSBuild.Sdk.PostgreSql** workflow using a real PostgreSQL database and the Pagila sample schema.

## Key Design Decisions

### 1. **Docker Requirement is Intentional**
- Tests **WILL FAIL** if Docker is not available
- This is **by design** - no graceful skipping
- Ensures CI/CD pipelines properly configure Docker support
- Prevents silent test skips that could hide infrastructure issues

### 2. **Focus on MSBuild SDK Workflow**
- Tests extract to **`.csproj`** format using `MSBuild.Sdk.PostgreSql` SDK
- Tests compile to **`.pgpac`** package format
- Tests publish from `.pgpac` files
- **JSON files are obsolete** - only used as temporary format

### 3. **Production-Ready Workflow**
The tests validate the recommended user workflow:
```
Live Database → Extract → .csproj (MSBuild Project)
                         ↓
                     Compile → .pgpac (Package)
                         ↓
                     Publish → Target Database
```

## Tests Implemented

### Test 1: `PagilaDatabase_CanBeExtractedToMSBuildProject`
**Purpose**: Validate extraction from live database to MSBuild project

**Workflow**:
1. Deploy Pagila schema (14 tables) to PostgreSQL
2. Extract using: `pgpac extract --source-connection-string "{conn}" --target-file "{project.csproj}"`
3. Verify `.csproj` file is created
4. Verify it contains `MSBuild.Sdk.PostgreSql` SDK reference
5. Verify it contains `<DatabaseName>` property

**What It Tests**:
- CLI `extract` command with `.csproj` target
- MSBuild SDK integration
- Project structure generation

### Test 2: `PagilaDatabase_MSBuildProject_CanBeCompiled`
**Purpose**: Validate MSBuild project compilation to `.pgpac` format

**Workflow**:
1. Deploy Pagila schema
2. Extract to `.csproj` MSBuild project
3. Compile using: `pgpac compile --source-file "{project.csproj}" --verbose`
4. Verify `.pgpac` file is created at `bin/Debug/net10.0/pagila.pgpac`

**What It Tests**:
- CLI `compile` command
- MSBuild SDK build process
- `.pgpac` package generation
- Dependency resolution
- Circular reference detection

### Test 3: `PagilaDatabase_ExtractCompilePublish_RoundTripWorks`
**Purpose**: Validate complete end-to-end workflow

**Workflow**:
1. Deploy Pagila schema (14 tables)
2. Extract to `.csproj` MSBuild project
3. Compile `.csproj` → `.pgpac`
4. Drop all tables (simulate clean target)
5. Publish using: `pgpac publish --source-file "{package.pgpac}" --target-connection-string "{conn}"`
6. Verify table count matches original

**What It Tests**:
- Complete round-trip: Database → Project → Package → Database
- Schema preservation
- Deployment accuracy
- Production workflow validity

## Infrastructure

### PostgreSQL Container
```csharp
new PostgreSqlBuilder("postgres:16")
    .WithDatabase("pagila")
    .WithUsername("postgres")
    .WithPassword("postgres123")
    .WithCleanUp(true)
    .Build();
```

### Test Data
- **Location**: `tests/NugetPackage.Tests/TestData/pagila-schema.sql`
- **Content**: Simplified Pagila schema (14 tables, foreign keys, view, function)
- **Complexity**: Tests circular FKs (store ↔ staff), composite FKs, cascades

### Test Workspace
- Each test uses unique temp directory: `%TEMP%/pgpac-pagila-{guid}`
- Automatic cleanup after test completion
- Isolated from other tests

## CLI Commands Tested

### Extract
```bash
pgpac extract --source-connection-string "Host=localhost;..." --target-file "Project.csproj"
```

### Compile
```bash
pgpac compile --source-file "Project.csproj" --verbose
```

### Publish
```bash
pgpac publish --source-file "package.pgpac" --target-connection-string "Host=localhost;..."
```

## Differences from Original Plan

### ❌ Removed: Docker Availability Checking
**Original**: Tests checked `IsDockerAvailableAsync()` and skipped if Docker was unavailable

**Changed To**: Tests **fail** if Docker is not available

**Reason**: User requirement - "The Tests that require docker should fail if docker isn't available... Don't let these be skipped"

### ❌ Removed: JSON File Testing
**Original**: Tests extracted to `.pgproj.json` files

**Changed To**: Tests extract to `.csproj` MSBuild projects

**Reason**: User requirement - "we should zero in on testing extraction to the csproj type using the MSBuild.Sdk.PostgreSql project type, and pgpac types ONLY. The raw json file should be considered obsolete, or a temporary file"

### ✅ Added: MSBuild Compilation Step
**Why**: The proper workflow is Extract → **Compile** → Publish, not Extract → Publish directly

### ✅ Added: .pgpac Package Testing
**Why**: `.pgpac` is the compiled output format that should be published, not raw JSON

## Running the Tests

### All Tests
```powershell
dotnet test tests/NugetPackage.Tests/NugetPackage.Tests.csproj
```

### Only Pagila Tests (Docker Required)
```powershell
dotnet test tests/NugetPackage.Tests/NugetPackage.Tests.csproj --filter "FullyQualifiedName~PagilaIntegrationTests"
```

### Only Package Tests (No Docker)
```powershell
dotnet test tests/NugetPackage.Tests/NugetPackage.Tests.csproj --filter "FullyQualifiedName~NugetPackageValidationTests"
```

## Expected Results

### When Docker is Available
- ✅ All 3 Pagila tests pass
- ✅ PostgreSQL container starts
- ✅ Pagila schema deploys
- ✅ Extract/Compile/Publish workflow succeeds

### When Docker is NOT Available
- ❌ All 3 Pagila tests fail with container startup exception
- This is **intentional** and **correct behavior**
- CI/CD should configure Docker support properly

## CI/CD Considerations

### Prerequisites
- Docker Desktop or Docker Engine must be running
- .NET 10 SDK must be installed
- PostgreSQL 16 image will be pulled automatically

### Recommended CI/CD Setup
```yaml
# GitHub Actions example
services:
  postgres:
    image: postgres:16
    
steps:
  - name: Start Docker
    run: systemctl start docker  # Linux
    # or
    run: Start-Service docker  # Windows
    
  - name: Run Tests
    run: dotnet test tests/NugetPackage.Tests/NugetPackage.Tests.csproj
```

## Files Changed

### New Files
- ✅ `tests/NugetPackage.Tests/PagilaIntegrationTests.cs` - 3 integration tests
- ✅ `tests/NugetPackage.Tests/TestData/pagila-schema.sql` - Sample database schema
- ✅ `tests/NugetPackage.Tests/PAGILA_INTEGRATION_SUMMARY.md` - This document

### Modified Files
- ✅ `tests/NugetPackage.Tests/NugetPackage.Tests.csproj` - Removed Docker.DotNet dependency
- ✅ `tests/NugetPackage.Tests/README.md` - Added Pagila test documentation

### Removed Dependencies
- ❌ `Docker.DotNet` package (no longer needed for availability checking)

## Test Coverage

### What Is Tested ✅
- MSBuild.Sdk.PostgreSql project creation
- `.csproj` extraction format
- `.pgpac` compilation format
- CLI command syntax and arguments
- Round-trip schema preservation
- PostgreSQL 16 compatibility
- Complex schema with circular FKs

### What Is NOT Tested ❌
- Multiple PostgreSQL versions (only 16 tested)
- Pre/Post deployment scripts
- SQLCMD variables
- Transactional deployment options
- Drop objects not in source option
- Different output formats (only pgpac tested)

## Future Enhancements

1. **Add PostgreSQL 17 Testing**
   - Test version-specific features
   - Verify cross-version compatibility

2. **Test Pre/Post Deployment Scripts**
   - Add tests for `<PreDeploy>` items
   - Add tests for `<PostDeploy>` items

3. **Test Publishing Options**
   - `--drop-objects-not-in-source` flag
   - `--transactional` flag
   - `--variables` for SQLCMD vars

4. **Performance Testing**
   - Large schema extraction time
   - Compilation speed
   - Deployment performance

## Summary

✅ **Three comprehensive integration tests** validate the complete MSBuild.Sdk.PostgreSql workflow

✅ **Docker requirement is enforced** - tests fail intentionally if Docker is unavailable

✅ **Proper workflow tested**: Extract → `.csproj` → Compile → `.pgpac` → Publish

✅ **JSON files are obsolete** - only MSBuild projects and `.pgpac` packages are tested

✅ **Real PostgreSQL database** - tests use Testcontainers with PostgreSQL 16

✅ **Production-ready** - tests validate the exact workflow users should follow

---

*Last Updated*: Current Session  
*Author*: pgPacTool Contributors  
*Purpose*: Ensure pgPacTool MSBuild SDK integration works end-to-end
