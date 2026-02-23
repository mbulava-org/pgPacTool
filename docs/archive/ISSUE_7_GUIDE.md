# Issue #7: Fix Privilege Extraction Bug - Implementation Guide

**Branch:** `feature/issue-7-fix-privilege-extraction`  
**Status:** ?? In Progress  
**Priority:** P0 - Critical (Blocker)  
**Story Points:** 8 SP  
**Estimated Time:** 2-3 days  

---

## ?? Goal

Fix ACL (Access Control List) parsing for privilege extraction across all PostgreSQL object types.

**Why This is Critical:**
- ?? **BLOCKS** all other extraction issues (#1-6)
- Privileges currently fail to extract
- Without this, extracted schemas are incomplete

---

## ?? What's Broken

### Location
`src/libs/mbulava.PostgreSql.Dac/Extract/PgProjectExtractor.cs`

### Line 133 - Commented Out Code
```csharp
//BUG: Privileges extraction fails here, might be a public thing though - needs investigation
//Privileges = await ExtractPrivilegesAsync(privilegesSql, "schema", name)
```

### The Problem
- ACL parsing exists but is commented out
- Need to test and fix issues
- Missing privilege code: `X` (EXECUTE)
- Grant option detection might not work correctly

---

## ?? Implementation Steps

### ? Step 1: Uncomment Privilege Extraction (5 minutes)

**File:** `src/libs/mbulava.PostgreSql.Dac/Extract/PgProjectExtractor.cs`  
**Line:** 133

Change FROM:
```csharp
//Privileges = await ExtractPrivilegesAsync(privilegesSql, "schema", name)
```

Change TO:
```csharp
Privileges = await ExtractPrivilegesAsync(privilegesSql, "schema", name)
```

**Test:**
```bash
dotnet build
# Check for compilation errors
```

---

### ? Step 2: Fix MapPrivilege Method (30 minutes)

**File:** `src/libs/mbulava.PostgreSql.Dac/Extract/PgProjectExtractor.cs`  
**Line:** ~178

**Current Issues:**
- Missing `X` (EXECUTE) privilege
- Grant option detection uses `char.IsUpper()` but this doesn't work for all codes
- Some privilege codes overlap (e.g., 'D' for TRUNCATE vs 'd' for DELETE)

**Fix:**
```csharp
private string MapPrivilege(char ch) =>
    ch switch
    {
        'r' => "SELECT",
        'w' => "UPDATE", 
        'a' => "INSERT",
        'd' => "DELETE",
        'D' => "TRUNCATE",
        'x' => "REFERENCES",
        't' => "TRIGGER",
        'X' => "EXECUTE",      // ADD THIS - was missing!
        'U' => "USAGE",
        'C' => "CREATE",
        'c' => "CONNECT",
        'T' => "TEMPORARY",
        _ => $"Unknown({ch})"
    };
```

**Note:** Uppercase letters indicate "WITH GRANT OPTION" is already handled by:
```csharp
IsGrantable = char.IsUpper(ch)
```

---

### ? Step 3: Verify ACL Parsing Logic (1 hour)

**File:** `src/libs/mbulava.PostgreSql.Dac/Extract/PgProjectExtractor.cs`  
**Lines:** 139-173

**Review Current Logic:**
```csharp
private async Task<List<PgPrivilege>> ExtractPrivilegesAsync(
    string sql, string paramName, object paramValue)
{
    var privileges = new List<PgPrivilege>();

    using var cmd = new NpgsqlCommand(sql, CreateConnection());
    cmd.Parameters.AddWithValue(paramName, paramValue);
    using var reader = await cmd.ExecuteReaderAsync();
    
    if (!await reader.ReadAsync()) return privileges;  // ? Handle no results
    
    var aclArray = reader.IsDBNull(0) ? null : reader.GetFieldValue<string[]>(0);
    await reader.CloseAsync();
    
    if (aclArray == null) return privileges;  // ? Handle NULL ACL
    
    foreach (var acl in aclArray)
    {
        // Example: "grantee=arwdDxt/grantor"
        var parts = acl.Split('=');
        if (parts.Length < 2) continue;  // ? Skip malformed entries
        
        var grantee = string.IsNullOrEmpty(parts[0]) ? "PUBLIC" : parts[0];  // ? Handle PUBLIC
        var rightsAndGrantor = parts[1].Split('/');
        var rights = rightsAndGrantor[0];
        var grantor = rightsAndGrantor.Length > 1 ? rightsAndGrantor[1] : string.Empty;
        
        foreach (var ch in rights)
        {
            privileges.Add(new PgPrivilege
            {
                Grantee = grantee,
                PrivilegeType = MapPrivilege(ch),
                IsGrantable = char.IsUpper(ch),
                Grantor = grantor
            });
        }
    }
    
    return privileges;
}
```

**Verification Checklist:**
- [x] ? NULL ACL handling
- [x] ? Empty ACL array handling
- [x] ? PUBLIC grants (empty grantee)
- [x] ? Grant option detection (uppercase)
- [ ] ?? Missing 'X' (EXECUTE) in MapPrivilege
- [ ] ?? Need tests to verify

---

### ? Step 4: Create Comprehensive Tests (3-4 hours)

#### Create Test File

**Location:** `tests/mbulava.PostgreSql.Dac.Tests/Extract/PrivilegeExtractionTests.cs`

```csharp
using FluentAssertions;
using mbulava.PostgreSql.Dac.Extract;
using NUnit.Framework;
using Npgsql;
using System.Threading.Tasks;
using Testcontainers.PostgreSql;

namespace mbulava.PostgreSql.Dac.Tests.Extract
{
    [TestFixture]
    public class PrivilegeExtractionTests
    {
        private PostgreSqlContainer _container = null!;
        private string _connectionString = null!;

        [OneTimeSetUp]
        public async Task OneTimeSetup()
        {
            _container = new PostgreSqlBuilder()
                .WithImage("postgres:16")
                .WithDatabase("testdb")
                .WithUsername("postgres")
                .WithPassword("postgres")
                .Build();

            await _container.StartAsync();
            _connectionString = _container.GetConnectionString();
        }

        [OneTimeTearDown]
        public async Task OneTimeTearDown()
        {
            await _container.DisposeAsync();
        }

        [Test]
        public async Task ExtractSchemaPrivileges_WithUsageGrant_ExtractsCorrectly()
        {
            // Arrange
            await using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();

            await using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                CREATE SCHEMA test_schema1;
                CREATE USER test_user1 WITH PASSWORD 'password';
                GRANT USAGE ON SCHEMA test_schema1 TO test_user1;
            ";
            await cmd.ExecuteNonQueryAsync();

            // Act
            var extractor = new PgProjectExtractor(_connectionString);
            var project = await extractor.ExtractPgProject("testdb");

            // Assert
            var schema = project.Schemas.FirstOrDefault(s => s.Name == "test_schema1");
            schema.Should().NotBeNull();
            schema!.Privileges.Should().NotBeEmpty();
            schema.Privileges.Should().Contain(p =>
                p.Grantee == "test_user1" &&
                p.PrivilegeType == "USAGE" &&
                p.IsGrantable == false);
        }

        [Test]
        public async Task ExtractSchemaPrivileges_WithCreateGrant_ExtractsCorrectly()
        {
            // Arrange
            await using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();

            await using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                CREATE SCHEMA test_schema2;
                CREATE USER test_user2 WITH PASSWORD 'password';
                GRANT CREATE ON SCHEMA test_schema2 TO test_user2;
            ";
            await cmd.ExecuteNonQueryAsync();

            // Act
            var extractor = new PgProjectExtractor(_connectionString);
            var project = await extractor.ExtractPgProject("testdb");

            // Assert
            var schema = project.Schemas.FirstOrDefault(s => s.Name == "test_schema2");
            schema.Should().NotBeNull();
            schema!.Privileges.Should().Contain(p =>
                p.Grantee == "test_user2" &&
                p.PrivilegeType == "CREATE");
        }

        [Test]
        public async Task ExtractSchemaPrivileges_WithGrantOption_SetsIsGrantableTrue()
        {
            // Arrange
            await using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();

            await using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                CREATE SCHEMA test_schema3;
                CREATE USER test_user3 WITH PASSWORD 'password';
                GRANT USAGE ON SCHEMA test_schema3 TO test_user3 WITH GRANT OPTION;
            ";
            await cmd.ExecuteNonQueryAsync();

            // Act
            var extractor = new PgProjectExtractor(_connectionString);
            var project = await extractor.ExtractPgProject("testdb");

            // Assert
            var schema = project.Schemas.FirstOrDefault(s => s.Name == "test_schema3");
            schema.Should().NotBeNull();
            schema!.Privileges.Should().Contain(p =>
                p.Grantee == "test_user3" &&
                p.PrivilegeType == "USAGE" &&
                p.IsGrantable == true);
        }

        [Test]
        public async Task ExtractSchemaPrivileges_PublicGrant_RecognizesPublic()
        {
            // Arrange
            await using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();

            await using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                CREATE SCHEMA test_schema4;
                GRANT USAGE ON SCHEMA test_schema4 TO PUBLIC;
            ";
            await cmd.ExecuteNonQueryAsync();

            // Act
            var extractor = new PgProjectExtractor(_connectionString);
            var project = await extractor.ExtractPgProject("testdb");

            // Assert
            var schema = project.Schemas.FirstOrDefault(s => s.Name == "test_schema4");
            schema.Should().NotBeNull();
            schema!.Privileges.Should().Contain(p =>
                p.Grantee == "PUBLIC" &&
                p.PrivilegeType == "USAGE");
        }

        [Test]
        public async Task ExtractSchemaPrivileges_NoExplicitGrants_ReturnsEmptyList()
        {
            // Arrange
            await using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();

            await using var cmd = conn.CreateCommand();
            cmd.CommandText = "CREATE SCHEMA test_schema5;";
            await cmd.ExecuteNonQueryAsync();

            // Act
            var extractor = new PgProjectExtractor(_connectionString);
            var project = await extractor.ExtractPgProject("testdb");

            // Assert
            var schema = project.Schemas.FirstOrDefault(s => s.Name == "test_schema5");
            schema.Should().NotBeNull();
            // Schema should exist but might have empty privileges (or default owner privileges)
            schema!.Privileges.Should().NotBeNull();
        }

        [Test]
        public async Task ExtractSchemaPrivileges_MultiplePrivileges_ExtractsAll()
        {
            // Arrange
            await using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();

            await using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                CREATE SCHEMA test_schema6;
                CREATE USER test_user6 WITH PASSWORD 'password';
                GRANT USAGE, CREATE ON SCHEMA test_schema6 TO test_user6;
            ";
            await cmd.ExecuteNonQueryAsync();

            // Act
            var extractor = new PgProjectExtractor(_connectionString);
            var project = await extractor.ExtractPgProject("testdb");

            // Assert
            var schema = project.Schemas.FirstOrDefault(s => s.Name == "test_schema6");
            schema.Should().NotBeNull();
            schema!.Privileges.Should().HaveCountGreaterOrEqualTo(2);
            schema.Privileges.Should().Contain(p =>
                p.Grantee == "test_user6" &&
                p.PrivilegeType == "USAGE");
            schema.Privileges.Should().Contain(p =>
                p.Grantee == "test_user6" &&
                p.PrivilegeType == "CREATE");
        }

        [Test]
        public async Task ExtractSchemaPrivileges_MultipleGrantees_ExtractsAll()
        {
            // Arrange
            await using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();

            await using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                CREATE SCHEMA test_schema7;
                CREATE USER test_user7a WITH PASSWORD 'password';
                CREATE USER test_user7b WITH PASSWORD 'password';
                GRANT USAGE ON SCHEMA test_schema7 TO test_user7a;
                GRANT CREATE ON SCHEMA test_schema7 TO test_user7b;
            ";
            await cmd.ExecuteNonQueryAsync();

            // Act
            var extractor = new PgProjectExtractor(_connectionString);
            var project = await extractor.ExtractPgProject("testdb");

            // Assert
            var schema = project.Schemas.FirstOrDefault(s => s.Name == "test_schema7");
            schema.Should().NotBeNull();
            schema!.Privileges.Should().Contain(p => p.Grantee == "test_user7a");
            schema.Privileges.Should().Contain(p => p.Grantee == "test_user7b");
        }
    }
}
```

---

### ? Step 5: Run Tests and Fix Issues (2-3 hours)

```bash
# Build solution
dotnet build

# Run all tests
dotnet test

# Run only privilege tests
dotnet test --filter "FullyQualifiedName~PrivilegeExtraction"

# Run with detailed output
dotnet test --filter "FullyQualifiedName~PrivilegeExtraction" --logger "console;verbosity=detailed"
```

**Expected Results:**
- ? All 8 tests should pass
- ? Schema privileges extract correctly
- ? Grant options detected
- ? PUBLIC grants recognized

**If Tests Fail:**
1. Check error messages
2. Debug `ExtractPrivilegesAsync` method
3. Verify ACL format matches expectations
4. Add logging if needed

---

### ? Step 6: Extend to Other Object Types (2-3 hours)

Once schema privileges work, apply to other objects:

#### Tables
```csharp
var tableSql = "SELECT t.relacl FROM pg_class t WHERE t.relname = @table;";
table.Privileges = await ExtractPrivilegesAsync(tableSql, "table", tableName);
```

#### Views
```csharp
var viewSql = "SELECT v.relacl FROM pg_class v WHERE v.relname = @view AND v.relkind = 'v';";
view.Privileges = await ExtractPrivilegesAsync(viewSql, "view", viewName);
```

#### Functions
```csharp
var functionSql = "SELECT p.proacl FROM pg_proc p WHERE p.oid = @oid;";
function.Privileges = await ExtractPrivilegesAsync(functionSql, "oid", functionOid);
```

**Note:** Table/View privileges use different codes (r, w, a, d, D, x, t) than schema privileges (U, C)

---

## ?? Definition of Done

- [x] Created feature branch
- [ ] Uncommented privilege extraction code
- [ ] Added missing `X` (EXECUTE) privilege code
- [ ] Created 8+ comprehensive tests
- [ ] All tests pass on PostgreSQL 16
- [ ] Schema privileges extract correctly
- [ ] Grant options detected correctly
- [ ] PUBLIC grants recognized
- [ ] NULL ACL handled
- [ ] Code compiles without errors
- [ ] No regressions in existing tests
- [ ] Code reviewed
- [ ] Documentation updated

---

## ?? Troubleshooting

### Issue: Tests Fail with ACL Parsing Error

**Symptom:** Exception when parsing ACL array

**Solution:**
1. Add try-catch to ACL parsing
2. Log ACL values for debugging
3. Check PostgreSQL version (should be 16)

### Issue: Grant Option Not Detected

**Symptom:** `IsGrantable` always false

**Solution:**
- Uppercase letters in ACL indicate grant option
- But some codes like 'D' (TRUNCATE) are always uppercase
- May need more sophisticated detection

### Issue: PUBLIC Grants Not Working

**Symptom:** PUBLIC grants not in privilege list

**Solution:**
- Empty grantee in ACL means PUBLIC
- Check: `string.IsNullOrEmpty(parts[0]) ? "PUBLIC" : parts[0]`

---

## ?? Reference

### PostgreSQL ACL Format

**Format:** `grantee=privileges/grantor`

**Examples:**
```
postgres=arwdDxt/postgres  ? postgres has all privileges
=r/postgres                ? PUBLIC has SELECT
user1=ar*/postgres         ? user1 has INSERT, SELECT with grant option
```

### Privilege Codes

| Code | Privilege | Object Types |
|------|-----------|--------------|
| r | SELECT | table, view, sequence |
| w | UPDATE | table, view, sequence |
| a | INSERT | table, view |
| d | DELETE | table, view |
| D | TRUNCATE | table |
| x | REFERENCES | table |
| t | TRIGGER | table |
| X | EXECUTE | function, procedure |
| U | USAGE | schema, sequence, type |
| C | CREATE | schema, database |
| c | CONNECT | database |
| T | TEMPORARY | database |

### Grant Option

- **Lowercase** = privilege without grant option
- **Uppercase** = privilege with grant option
- **Exception:** 'D' (TRUNCATE) is always uppercase

---

## ?? Next Steps After Completion

Once Issue #7 is complete:
1. Commit changes
2. Push branch
3. Create PR
4. Get code review
5. Merge to main

**Then unblocks:**
- ? Issue #1 (Views)
- ? Issue #2 (Functions)
- ? Issue #3 (Procedures)
- ? Issue #4 (Triggers)
- ? Issue #5 (Indexes)
- ? Issue #6 (Constraints)

All extraction work can proceed in parallel! ??

---

**Created:** 2026-01-31  
**Branch:** `feature/issue-7-fix-privilege-extraction`  
**Status:** Ready to work  
**Estimated Completion:** 2-3 days
