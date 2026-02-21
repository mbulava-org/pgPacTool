# Integration Tests - PostgreSQL Multi-Version Testing

This directory contains integration tests that run against real PostgreSQL instances using Docker containers via Testcontainers.

## Test Structure

```
tests/ProjectExtract-Tests/
├── Integration/
│   ├── PostgresVersionTestBase.cs       # Base class for version-specific tests
│   ├── Postgres16IntegrationTests.cs    # Tests for PostgreSQL 16 (minimum supported)
│   ├── Postgres17IntegrationTests.cs    # Tests for PostgreSQL 17
│   └── Postgres18IntegrationTests.cs    # Tests for PostgreSQL 18 (future-proofing)
├── PrivilegeExtractionTests.cs          # Detailed privilege extraction tests
└── SimplePrivilegeTest.cs               # Quick smoke test
```

## Running Tests

### Run All Tests
```bash
dotnet test
```

### Run Only Smoke Tests (Fastest)
```bash
dotnet test --filter "Category=Smoke"
```

### Run Integration Tests Only
```bash
dotnet test --filter "Category=Integration"
```

### Run Tests for Specific PostgreSQL Version
```bash
# PostgreSQL 16 tests only
dotnet test --filter "Category=Postgres16"

# PostgreSQL 17 tests only
dotnet test --filter "Category=Postgres17"

# PostgreSQL 18 tests (when available)
dotnet test --filter "Category=Postgres18"
```

### Run Without Future Version Tests
```bash
dotnet test --filter "Category!=FutureVersion"
```

## Test Categories

| Category | Description | Docker Required | Duration |
|----------|-------------|-----------------|----------|
| `Smoke` | Quick validation tests | Yes | ~5s per test |
| `Integration` | Full feature tests across versions | Yes | ~10s per test |
| `Postgres16` | PostgreSQL 16 specific tests | Yes | ~10s per test |
| `Postgres17` | PostgreSQL 17 specific tests | Yes | ~10s per test |
| `Postgres18` | PostgreSQL 18 tests (ignored until released) | Yes | N/A |
| `FutureVersion` | Future PostgreSQL versions | Yes | N/A |

## Prerequisites

- **Docker Desktop** must be running
- **Testcontainers** will automatically pull PostgreSQL images
- **First run** may take longer as Docker images are downloaded

## Test Coverage

### PostgresVersionTestBase
- Provides common infrastructure for version-specific tests
- Automatically spins up PostgreSQL container
- Seeds common test data (schemas, tables, views, functions, sequences)
- Manages container lifecycle

### Postgres16IntegrationTests (Minimum Supported Version)
- ✅ Project extraction
- ✅ Schema privilege extraction
- ✅ Table extraction with columns
- ✅ Sequence extraction
- ✅ Type extraction
- ✅ Version detection
- ✅ Public schema default privileges

### Postgres17IntegrationTests (Forward Compatibility)
- ✅ Project extraction
- ✅ Schema privilege extraction
- ✅ Version detection
- ✅ Cross-version compatibility verification

### Postgres18IntegrationTests (Future-Proofing)
- 🔜 Currently ignored (PostgreSQL 18 not released)
- 🔜 Enable tests when PostgreSQL 18 becomes available
- 🔜 Ensures forward compatibility with future versions

## Advantages of This Approach

1. **✅ No External Dependencies** - Tests spin up their own databases
2. **✅ Clean State** - Each test class gets a fresh database
3. **✅ Version Coverage** - Tests against multiple PostgreSQL versions
4. **✅ CI/CD Ready** - Works in GitHub Actions, Azure DevOps, etc.
5. **✅ Fast Feedback** - Smoke tests run in ~5 seconds
6. **✅ Future-Proof** - Easy to add new PostgreSQL versions

## Troubleshooting

### Docker Not Running
```
Error: Cannot connect to Docker daemon
```
**Solution:** Start Docker Desktop

### Port Already in Use
```
Error: Port 5432 is already allocated
```
**Solution:** Testcontainers automatically assigns random ports. If this happens, restart Docker.

### Tests Timing Out
```
Error: Test exceeded timeout of 15 seconds
```
**Solution:** 
- Check Docker Desktop has enough resources (Memory > 4GB recommended)
- Increase test timeout in test runner settings

### Image Pull Fails
```
Error: manifest unknown: manifest unknown
```
**Solution:**
- Check internet connection
- Verify Docker Hub is accessible
- Try pulling the image manually: `docker pull postgres:16`

## Adding New PostgreSQL Versions

To add support for a new PostgreSQL version (e.g., PostgreSQL 19):

1. Create `Postgres19IntegrationTests.cs`:
```csharp
[TestFixture]
[Category("Integration")]
[Category("Postgres19")]
public class Postgres19IntegrationTests : PostgresVersionTestBase
{
    protected override string PostgreSqlVersion => "postgres:19";
    
    // Add tests here
}
```

2. Run tests:
```bash
dotnet test --filter "Category=Postgres19"
```

## Best Practices

1. **Use [SetUp] for per-test isolation** - Each test gets a clean database
2. **Use [OneTimeSetUp] for shared setup** - Faster but tests share state
3. **Use Categories** - Easy to filter tests by version or type
4. **Use TestContext.Out.WriteLine** - Better than Console.WriteLine in tests
5. **Test against minimum AND latest versions** - Ensures backward and forward compatibility

## Performance Tips

- Smoke tests are fastest (~5s) - use for quick validation
- Integration tests are slower (~10s per test) - use for comprehensive validation
- Run specific categories during development: `--filter "Category=Smoke"`
- Run all tests before committing: `dotnet test`

---

**Note:** These tests use [Testcontainers](https://dotnet.testcontainers.org/) which automatically manages Docker containers. No manual Docker commands needed!
