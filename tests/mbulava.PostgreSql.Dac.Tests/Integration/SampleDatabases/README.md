# Sample Database Integration Tests

This directory contains integration tests that validate the AST-based compilation against real-world PostgreSQL databases.

## Docker Images

Tests use the following Docker images:
- `mbulava/postgres-sample-dbs:16` - PostgreSQL 16 with 9 sample databases
- `mbulava/postgres-sample-dbs:17` - PostgreSQL 17 with 9 sample databases

## Sample Databases

Each image includes 9 databases:
1. **chinook** - Digital media store with artists, albums, tracks, customers, invoices
2. **dvdrental** - DVD rental with films, actors, categories, rentals, payments
3. **employees** - HR database with employees, departments, salaries
4. **lego** - LEGO sets with themes, parts, colors, inventories
5. **netflix** - Netflix shows/movies with genres, ratings, cast
6. **pagila** - Extended DVD rental (larger than dvdrental)
7. **periodic_table** - Chemistry periodic table with elements, properties
8. **titanic** - Titanic passenger/crew data with demographics, survival
9. **world_happiness** - World Happiness Index with countries, scores, factors

## Test Categories

### Schema Extraction Tests
- Extract complete schema from each database
- Validate all tables, views, functions, triggers extracted
- Compare extraction between PG 16 and PG 17

### Comparison Tests  
- Compare identical databases (should have zero diffs)
- Compare PG 16 vs PG 17 versions (identify version differences)
- Compare different databases (validate diff detection)

### Script Generation Tests
- Generate deployment scripts for schema changes
- Validate AST-based SQL generation
- Test round-trip: extract → generate → apply → extract

### Cross-Version Tests
- Ensure PG 16 scripts work on PG 17
- Ensure PG 17 scripts work on PG 16 (when compatible)
- Identify version-specific features

## Running Tests

### Prerequisites
```bash
# Start PostgreSQL 16 container
docker run -d --name pg16-samples -p 5416:5432 mbulava/postgres-sample-dbs:16

# Start PostgreSQL 17 container  
docker run -d --name pg17-samples -p 5417:5432 mbulava/postgres-sample-dbs:17
```

### Run Tests
```bash
# Run all integration tests
dotnet test --filter "Category=SampleDatabaseIntegration"

# Run specific database tests
dotnet test --filter "Category=Chinook"
dotnet test --filter "Category=DVDRental"

# Run cross-version tests
dotnet test --filter "Category=CrossVersion"
```

### Cleanup
```bash
docker stop pg16-samples pg17-samples
docker rm pg16-samples pg17-samples
```

## Test Configuration

Tests use environment variables for configuration:
- `PG16_HOST` - Default: localhost
- `PG16_PORT` - Default: 5416
- `PG17_HOST` - Default: localhost
- `PG17_PORT` - Default: 5417
- `PG_USER` - Default: postgres
- `PG_PASSWORD` - Default: postgres

## Expected Results

### Schema Counts (Approximate)
| Database | Tables | Views | Functions | Triggers |
|----------|--------|-------|-----------|----------|
| chinook | 11 | 0 | 0 | 0 |
| dvdrental | 15 | 7 | 3 | 2 |
| employees | 6 | 0 | 0 | 0 |
| lego | 8 | 0 | 0 | 0 |
| netflix | 5 | 0 | 0 | 0 |
| pagila | 21 | 8 | 5 | 3 |
| periodic_table | 3 | 0 | 0 | 0 |
| titanic | 2 | 0 | 0 | 0 |
| world_happiness | 2 | 0 | 0 | 0 |

### Known Differences PG 16 vs 17
- System catalog changes
- New built-in functions in PG 17
- Performance improvements (not schema-visible)

## Troubleshooting

### Connection Issues
```bash
# Check containers are running
docker ps | grep postgres-sample-dbs

# Check logs
docker logs pg16-samples
docker logs pg17-samples

# Test connection
psql -h localhost -p 5416 -U postgres -d chinook -c "SELECT version();"
```

### Port Conflicts
If ports 5416/5417 are in use, modify docker run commands:
```bash
docker run -d --name pg16-samples -p 5432:5432 mbulava/postgres-sample-dbs:16
# Update test configuration accordingly
```

## Success Criteria

- ✅ All 9 databases extract successfully from both PG 16 and 17
- ✅ Schema comparisons produce accurate diffs
- ✅ Generated scripts apply successfully
- ✅ Round-trip produces identical schemas
- ✅ AST-based generation handles all real-world patterns
- ✅ Cross-version compatibility validated
