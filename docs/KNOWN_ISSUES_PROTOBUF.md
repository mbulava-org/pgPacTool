# Known Issues - AST SQL Generation & Protobuf Deparse

**Date:** 2026-03-01  
**Component:** Npgquery Library / AST SQL Generation  
**Severity:** High (Blocks Linux CI/CD)  
**Branch:** feature/AST_BASED_COMPILATION  

---

## Issue #1: Protobuf Deparse Corruption on Linux

### Status
🔴 **CRITICAL** - Blocks Linux builds in GitHub Actions

### Description
The `pg_query_deparse_protobuf` native function in libpg_query returns corrupted output on Linux (Ubuntu 24.04) when called from .NET 10. Instead of returning SQL text, it returns raw protobuf binary data containing control characters.

### Symptoms
```
Expected: "ALTER TABLE public.users DROP COLUMN IF EXISTS old_column;"
Actual:   "DROP TABLESPACE IF EXISTS \u0012\u0006PUBLIC\u001a\u0005USERS \u0001*\u0001P"
```

The output contains protobuf field markers (0x01-0x1F range) indicating the C function is returning protobuf bytes instead of deparsed SQL.

### Root Cause
Cross-platform protobuf serialization issue between:
- **C# Side:** Google.Protobuf 3.33.5 
- **Native Side:** libpg_query (embedded protobuf-c)
- **Platform:** Linux x64 with .NET 10

The `pg_query_deparse_protobuf` function on Linux appears to:
1. Receive the protobuf structure correctly
2. Fail to deparse it properly
3. Return raw protobuf bytes instead of SQL text

### Affected Code
**File:** `src/libs/Npgquery/Npgquery/Npgquery.cs`
```csharp
public DeparseResult Deparse(JsonDocument parseTree)
{
    var json = parseTree.RootElement.GetRawText();
    var protoParseResult = ProtobufHelper.ParseResultFromJson(json);
    var protoBytes = protoParseResult.ToByteArray();
    var protoStruct = NativeMethods.AllocPgQueryProtobuf(protoBytes);

    var deparseResult = NativeMethods.pg_query_deparse_protobuf(protoStruct);
    // ^^^ Returns corrupted data on Linux
```

**File:** `src/libs/mbulava.PostgreSql.Dac/Compile/Ast/AstSqlGenerator.cs`
```csharp
public static string Generate(JsonElement astElement)
{
    var json = astElement.GetRawText();
    using var doc = JsonDocument.Parse(json);
    
    // Previously called parser.Deparse(doc) which uses protobuf path
    // Now fixed to use Generate(doc) which uses JSON extraction
```

### Failed Tests (GitHub Actions - Ubuntu 24.04)
```
❌ Generate_ColumnTypeChange_CreatesAlterType
   Expected: "ALTER COLUMN"
   Actual: Contains protobuf garbage

❌ Generate_DropColumn_WithDropFlag_CreatesDropColumn
   Expected: "old_column"
   Actual: Contains protobuf garbage

❌ Generate_PrivilegeChanges_CreatesGrantRevoke
   Expected: "GRANT SELECT"
   Error: JSON extractor doesn't support GrantStmt yet
```

### Workaround Implemented
**Status:** ✅ Partial Fix

Modified `AstSqlGenerator.Generate(JsonElement)` to use JSON-based SQL extraction instead of protobuf deparse:

```csharp
public static string Generate(JsonElement astElement)
{
    var json = astElement.GetRawText();
    using var doc = JsonDocument.Parse(json);
    
    // Use JSON extraction (reliable) instead of protobuf deparse (broken on Linux)
    return Generate(doc);
}
```

The `Generate(JsonDocument)` method uses `TryExtractSqlFromAstJson` which generates SQL by parsing the JSON AST structure directly, avoiding the protobuf path entirely.

### Current Limitations
**Status:** 🟡 Incomplete - Needs GrantStmt/RevokeStmt Support

The JSON extractor currently supports:
- ✅ **AlterTableStmt** (DROP COLUMN, ADD COLUMN, ALTER COLUMN TYPE, SET/DROP NOT NULL)
- ✅ **DropStmt** (DROP TABLE, DROP FUNCTION, etc.)
- ❌ **GrantStmt** (GRANT privileges) - **Missing**
- ❌ **RevokeStmt** (REVOKE privileges) - **Missing**

### Solution Required
Add GrantStmt and RevokeStmt support to `TryExtractSqlFromAstJson`:

1. **Add statement type checks:**
```csharp
if (stmtElement.TryGetProperty("GrantStmt", out var grantStmt))
{
    var sql = GenerateSqlFromGrantStmt(grantStmt);
    return sql;
}

if (stmtElement.TryGetProperty("RevokeStmt", out var revokeStmt))
{
    var sql = GenerateSqlFromRevokeStmt(revokeStmt);
    return sql;
}
```

2. **Implement SQL generators:**
   - `GenerateSqlFromGrantStmt(JsonElement)` - Extract privileges, object, grantee
   - `GenerateSqlFromRevokeStmt(JsonElement)` - Extract privileges, object, grantee

### AST Structure Reference
**GrantStmt Structure:**
```json
{
  "stmt": {
    "GrantStmt": {
      "is_grant": true,
      "targtype": "ACL_TARGET_OBJECT",
      "objtype": "OBJECT_TABLE",
      "objects": [
        {
          "RangeVar": {
            "schemaname": "public",
            "relname": "users",
            "inh": true
          }
        }
      ],
      "privileges": [
        {
          "AccessPriv": {
            "priv_name": "select"
          }
        }
      ],
      "grantees": [
        {
          "RoleSpec": {
            "roletype": "ROLESPEC_CSTRING",
            "rolename": "app_user"
          }
        }
      ]
    }
  }
}
```

### Testing
**Test File:** `tests/mbulava.PostgreSql.Dac.Tests/Compare/PublishScriptGeneratorTests.cs`

**Failing Test:**
```csharp
[Test]
public void Generate_PrivilegeChanges_CreatesGrantRevoke()
{
    var diff = new PgSchemaDiff
    {
        TableDiffs = new List<PgTableDiff>
        {
            new()
            {
                PrivilegeChanges = new List<PgPrivilegeDiff>
                {
                    new()
                    {
                        Grantee = "app_user",
                        PrivilegeType = "SELECT",
                        ChangeType = PrivilegeChangeType.MissingInTarget
                    }
                }
            }
        }
    };

    var result = PublishScriptGenerator.Generate(diff, options);
    
    // Currently fails with: "JSON-to-SQL extraction failed"
    result.ToUpper().Should().Contain("GRANT SELECT");
}
```

### Dependencies
- **Package:** Google.Protobuf 3.33.5
- **Native Library:** libpg_query.so (Linux x64)
- **Framework:** .NET 10

### Environment
- **OS:** Ubuntu 24.04 (GitHub Actions)
- **Runtime:** linux-x64
- **Native Library Path:** `runtimes/linux-x64/native/libpg_query.so`

### Next Steps
1. ✅ Fix `Generate(JsonElement)` to use JSON extraction (DONE)
2. ⏳ Add GrantStmt support to JSON extractor (IN PROGRESS)
3. ⏳ Add RevokeStmt support to JSON extractor (IN PROGRESS)
4. ⏳ Run tests on Linux to verify fix
5. ⏳ Document JSON extraction patterns for future statement types

### Related Files
```
src/libs/mbulava.PostgreSql.Dac/Compile/Ast/AstSqlGenerator.cs
src/libs/Npgquery/Npgquery/Npgquery.cs
src/libs/Npgquery/Npgquery/Native/NativeMethods.cs
src/libs/Npgquery/Npgquery/Native/ProtobufHelper.cs
tests/mbulava.PostgreSql.Dac.Tests/Compare/PublishScriptGeneratorTests.cs
```

---

## Issue #2: Native Library Version Mismatch

### Status
🟡 **MEDIUM** - Potential compatibility issue

### Description
The libpg_query native library bundled in `runtimes/linux-x64/native/libpg_query.so` may be using a different protobuf-c version than expected by the .NET Google.Protobuf serialization.

### Investigation Needed
- Check libpg_query version
- Verify protobuf-c compatibility
- Consider rebuilding native library with matching protobuf version

### Workaround
Use JSON-based SQL extraction (Issue #1 solution) to avoid protobuf path entirely.

---

## Issue #3: Incomplete Statement Type Coverage

### Status
🟡 **MEDIUM** - Limits functionality

### Description
The JSON-based SQL extraction (`TryExtractSqlFromAstJson`) only supports a subset of PostgreSQL statement types. Any unsupported statement type will throw an exception.

### Currently Supported
- AlterTableStmt (various subtypes)
- DropStmt

### Missing Support
- **GrantStmt** (HIGH priority - blocks tests)
- **RevokeStmt** (HIGH priority - blocks tests)
- CreateStmt (table creation)
- CreateFunctionStmt
- CreateTriggerStmt
- CreateViewStmt
- CreateIndexStmt
- And many others...

### Solution
Incrementally add support for statement types as needed. Priority:
1. GrantStmt / RevokeStmt (blocks tests)
2. CreateStmt (needed for schema deployment)
3. CreateFunctionStmt / CreateTriggerStmt (needed for full DDL support)

---

## Build Log Reference

**File:** `build-logs/0_Build and Test (.NET 10).txt`

**Error Excerpts:**
```
Failed Generate_ColumnTypeChange_CreatesAlterType [535 ms]
Error Message:
 Expected string "BEGIN;

DROP TABLESPACE IF EXISTS \u0012\u0006PUBLIC\u001a\u0005USERS \u0001*\u0001P


COMMIT;
" to contain "ALTER COLUMN".
```

**Token Count:** 128,952 (exceeded 128,000 limit in Copilot session)

---

## Recommendations

### Short Term
1. Complete GrantStmt/RevokeStmt JSON extraction support
2. Verify all PublishScriptGeneratorTests pass on Linux
3. Document JSON extraction pattern for future contributors

### Medium Term
1. Investigate libpg_query protobuf-c version compatibility
2. Add more statement type support to JSON extractor
3. Create comprehensive test suite for all supported statement types

### Long Term
1. Consider alternative approaches:
   - Use pg_query_deparse(sql) instead of pg_query_deparse_protobuf()
   - Build custom deparser using AST traversal
   - Contribute fix to libpg_query upstream
2. Evaluate switching to pure C# PostgreSQL parser (no native dependencies)

---

## References

- **libpg_query:** https://github.com/pganalyze/libpg_query
- **Google.Protobuf:** https://www.nuget.org/packages/Google.Protobuf/
- **PostgreSQL AST Docs:** https://www.postgresql.org/docs/current/parser-stage.html

---

**Document Version:** 1.0  
**Last Updated:** 2026-03-01  
**Author:** AI Assistant (GitHub Copilot)
