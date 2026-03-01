# Sample Database Integration Tests - Quick Start

## Setup (One-Time)

### 1. Pull Docker Images
```bash
docker pull mbulava/postgres-sample-dbs:16
docker pull mbulava/postgres-sample-dbs:17
```

### 2. Start Containers
```bash
# PostgreSQL 16 on port 5416
docker run -d \
  --name pg16-samples \
  -p 5416:5432 \
  -e POSTGRES_PASSWORD=postgres \
  mbulava/postgres-sample-dbs:16

# PostgreSQL 17 on port 5417
docker run -d \
  --name pg17-samples \
  -p 5417:5432 \
  -e POSTGRES_PASSWORD=postgres \
  mbulava/postgres-sample-dbs:17
```

### 3. Verify Containers are Running
```bash
docker ps | grep postgres-sample-dbs
```

Expected output:
```
CONTAINER ID   IMAGE                              PORTS                    NAMES
abc123...      mbulava/postgres-sample-dbs:17     0.0.0.0:5417->5432/tcp   pg17-samples
def456...      mbulava/postgres-sample-dbs:16     0.0.0.0:5416->5432/tcp   pg16-samples
```

### 4. Test Connectivity
```bash
# Test PG 16
psql -h localhost -p 5416 -U postgres -d chinook -c "SELECT COUNT(*) FROM artist;"

# Test PG 17
psql -h localhost -p 5417 -U postgres -d chinook -c "SELECT COUNT(*) FROM artist;"
```

## Running Tests

### Run All Integration Tests
```bash
cd tests/mbulava.PostgreSql.Dac.Tests
dotnet test --filter "Category=SampleDatabaseIntegration"
```

### Run Specific Database Tests
```bash
# Chinook database tests
dotnet test --filter "Category=Chinook"

# Pagila complex schema tests
dotnet test --filter "Category=Pagila"

# DVD Rental tests
dotnet test --filter "FullyQualifiedName~dvdrental"
```

### Run by Test Category
```bash
# Schema extraction tests only
dotnet test --filter "Category=SchemaExtraction"

# Cross-version comparison tests
dotnet test --filter "Category=CrossVersion"

# Script generation tests
dotnet test --filter "Category=ScriptGeneration"
```

### Run with Detailed Output
```bash
dotnet test --filter "Category=SampleDatabaseIntegration" \
  --logger "console;verbosity=detailed"
```

## Expected Test Results

### Quick Smoke Test
```bash
# This should pass if everything is set up correctly
dotnet test --filter "ExtractSchema_FromPg16_Succeeds&TestCase=chinook"
```

Expected output:
```
Passed!  - Failed:     0, Passed:     1, Skipped:     0, Total:     1
```

### Full Test Suite
Running all sample database integration tests (9 databases × multiple test categories):
```bash
dotnet test --filter "Category=SampleDatabaseIntegration" --logger "console;verbosity=minimal"
```

Expected: **~45-60 tests passing** (depending on which databases are available)

## Troubleshooting

### Containers Not Running
```bash
# Check status
docker ps -a | grep postgres-sample-dbs

# Start stopped containers
docker start pg16-samples pg17-samples

# Check logs
docker logs pg16-samples
docker logs pg17-samples
```

### Connection Issues
```bash
# Test basic connectivity
docker exec -it pg16-samples psql -U postgres -c "SELECT version();"

# List available databases
docker exec -it pg16-samples psql -U postgres -c "\l"
```

### Port Already in Use
If ports 5416 or 5417 are already in use:

**Option 1: Use different ports**
```bash
docker run -d --name pg16-samples -p 5432:5432 mbulava/postgres-sample-dbs:16
# Update SampleDbConfig.cs with new port
```

**Option 2: Stop conflicting services**
```bash
# Find what's using the port
lsof -i :5416  # macOS/Linux
netstat -ano | findstr :5416  # Windows

# Stop the conflicting service or use different ports
```

### Database Not Available
If a specific test fails with "database not available":

1. Check the database exists:
```bash
docker exec -it pg16-samples psql -U postgres -l | grep chinook
```

2. Try connecting directly:
```bash
docker exec -it pg16-samples psql -U postgres -d chinook -c "SELECT version();"
```

3. If database is missing, the image might be incomplete. Re-pull:
```bash
docker pull mbulava/postgres-sample-dbs:16
```

## Cleanup

### Stop Containers (Keep Data)
```bash
docker stop pg16-samples pg17-samples
```

### Remove Containers (Delete Data)
```bash
docker stop pg16-samples pg17-samples
docker rm pg16-samples pg17-samples
```

### Full Cleanup (Remove Images Too)
```bash
docker stop pg16-samples pg17-samples
docker rm pg16-samples pg17-samples
docker rmi mbulava/postgres-sample-dbs:16
docker rmi mbulava/postgres-sample-dbs:17
```

## Continuous Integration

### GitHub Actions Example
```yaml
name: Integration Tests

on: [push, pull_request]

jobs:
  test:
    runs-on: ubuntu-latest
    
    services:
      postgres16:
        image: mbulava/postgres-sample-dbs:16
        ports:
          - 5416:5432
        env:
          POSTGRES_PASSWORD: postgres
          
      postgres17:
        image: mbulava/postgres-sample-dbs:17
        ports:
          - 5417:5432
        env:
          POSTGRES_PASSWORD: postgres
    
    steps:
      - uses: actions/checkout@v3
      - uses: actions/setup-dotnet@v3
        with:
          dotnet-version: '10.0.x'
      
      - name: Wait for PostgreSQL
        run: |
          until docker exec postgres16 pg_isready -U postgres; do sleep 1; done
          until docker exec postgres17 pg_isready -U postgres; do sleep 1; done
      
      - name: Run Integration Tests
        run: |
          dotnet test --filter "Category=SampleDatabaseIntegration" \
            --logger "console;verbosity=detailed"
```

## Performance Benchmarks

Expected test execution times (on modern hardware):

| Test Category | Tests | Time |
|---------------|-------|------|
| Schema Extraction (all 9 DBs) | ~18 tests | ~5-10s |
| Cross-Version Comparison | ~6 tests | ~3-5s |
| Script Generation | ~4 tests | ~2-3s |
| Self-Comparison | ~4 tests | ~2-3s |
| **Full Suite** | **~45 tests** | **~15-25s** |

## What These Tests Validate

✅ **Schema Extraction**
- All 9 sample databases extract successfully
- Tables, views, functions, triggers captured correctly
- Works on both PG 16 and PG 17

✅ **Schema Comparison**
- Self-comparison produces zero diffs
- Cross-database comparison detects differences
- Cross-version comparison handles PG 16 vs 17

✅ **Script Generation**
- Deployment scripts generate successfully
- AST-based builders produce valid SQL
- Scripts are transactional and include proper comments

✅ **AST Validation**
- No string template artifacts in generated SQL
- All operations use proper AST builders
- Complex real-world schemas handled correctly

✅ **Cross-Version Compatibility**
- PG 16 and 17 schemas are compatible
- Version differences identified correctly
- Migration scripts work across versions

## Next Steps

After all tests pass:
1. Review test output for any warnings
2. Check generated scripts for quality
3. Run performance benchmarks
4. Integrate into CI/CD pipeline
5. Use for regression testing on schema changes

---

**Need Help?**
- Check container logs: `docker logs pg16-samples`
- Verify connectivity: `psql -h localhost -p 5416 -U postgres -d postgres`
- Review test output: Use `--logger "console;verbosity=detailed"`
