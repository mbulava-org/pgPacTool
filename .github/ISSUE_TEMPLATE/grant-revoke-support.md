---
name: Add GrantStmt/RevokeStmt Support to JSON SQL Extractor
about: Complete JSON-based SQL generation for GRANT and REVOKE statements
title: '[FEATURE] Add GrantStmt and RevokeStmt support to JSON SQL extractor'
labels: 'enhancement, json-extractor, sql-generation'
assignees: ''
---

## Description
The JSON-based SQL extractor (`TryExtractSqlFromAstJson`) currently lacks support for `GrantStmt` and `RevokeStmt`, causing test failures when generating privilege change scripts.

## Context
This is a dependency for fixing Issue #[protobuf-corruption]. The JSON extractor is a workaround for the broken protobuf deparse on Linux, but it only supports a subset of statement types.

## Current Support
- ✅ AlterTableStmt (DROP COLUMN, ADD COLUMN, ALTER COLUMN TYPE, etc.)
- ✅ DropStmt (DROP TABLE, DROP FUNCTION, etc.)
- ❌ GrantStmt (GRANT privileges)
- ❌ RevokeStmt (REVOKE privileges)

## Failed Tests
```
❌ Generate_PrivilegeChanges_CreatesGrantRevoke
   Error: JSON-to-SQL extraction failed. 
   Statement type GrantStmt is not yet supported.
```

## Acceptance Criteria
- [ ] Add `GenerateSqlFromGrantStmt(JsonElement)` method
- [ ] Add `GenerateSqlFromRevokeStmt(JsonElement)` method
- [ ] Extract privilege list from AST (e.g., SELECT, INSERT, UPDATE)
- [ ] Extract object type and name (TABLE, FUNCTION, etc.)
- [ ] Extract grantee/role name
- [ ] Generate valid SQL: `GRANT {privs} ON {type} {schema}.{name} TO {role};`
- [ ] Generate valid SQL: `REVOKE {privs} ON {type} {schema}.{name} FROM {role};`
- [ ] All PublishScriptGeneratorTests pass
- [ ] Add unit tests for GrantStmt/RevokeStmt SQL generation

## Implementation

### 1. Add Statement Type Checks
**File:** `src/libs/mbulava.PostgreSql.Dac/Compile/Ast/AstSqlGenerator.cs`

In `TryExtractSqlFromAstJson`, after the DropStmt check (~line 96):
```csharp
if (stmtElement.TryGetProperty("GrantStmt", out var grantStmt))
{
    var sql = GenerateSqlFromGrantStmt(grantStmt);
    System.Diagnostics.Debug.WriteLine($"TryExtractSqlFromAstJson: GrantStmt generated: {sql ?? \"NULL\"}");
    return sql;
}

if (stmtElement.TryGetProperty("RevokeStmt", out var revokeStmt))
{
    var sql = GenerateSqlFromRevokeStmt(revokeStmt);
    System.Diagnostics.Debug.WriteLine($"TryExtractSqlFromAstJson: RevokeStmt generated: {sql ?? \"NULL\"}");
    return sql;
}
```

### 2. Implement SQL Generators

Add these methods after `GenerateSqlFromDropStmt` (~line 229):

```csharp
/// <summary>
/// Generates SQL for GRANT statements
/// </summary>
private static string? GenerateSqlFromGrantStmt(JsonElement grantStmt)
{
    try
    {
        // Extract privileges
        if (!grantStmt.TryGetProperty("privileges", out var privileges) || 
            privileges.GetArrayLength() == 0)
            return null;

        var privList = new List<string>();
        foreach (var priv in privileges.EnumerateArray())
        {
            if (priv.TryGetProperty("AccessPriv", out var accessPriv) &&
                accessPriv.TryGetProperty("priv_name", out var privName))
            {
                var name = privName.GetString();
                if (name != null)
                    privList.Add(name.ToUpper());
            }
        }

        if (privList.Count == 0)
            return null;

        // Extract object type
        var objectType = grantStmt.TryGetProperty("objtype", out var objType)
            ? objType.GetString()?.Replace("OBJECT_", "") ?? "TABLE"
            : "TABLE";

        // Extract object name
        if (!grantStmt.TryGetProperty("objects", out var objects) || 
            objects.GetArrayLength() == 0)
            return null;

        var firstObj = objects[0];
        if (!firstObj.TryGetProperty("RangeVar", out var rangeVar))
            return null;

        var (schema, name) = ExtractRelationName(
            JsonDocument.Parse("{\"RangeVar\":" + rangeVar.GetRawText() + "}").RootElement
        );

        // Extract grantee
        if (!grantStmt.TryGetProperty("grantees", out var grantees) || 
            grantees.GetArrayLength() == 0)
            return null;

        var firstGrantee = grantees[0];
        string granteeName = "public";
        if (firstGrantee.TryGetProperty("RoleSpec", out var roleSpec) &&
            roleSpec.TryGetProperty("rolename", out var rolename))
        {
            granteeName = rolename.GetString() ?? "public";
        }

        var privStr = string.Join(", ", privList);
        return $"GRANT {privStr} ON {objectType} {QuoteIdent(schema)}.{QuoteIdent(name)} TO {QuoteIdent(granteeName)};";
    }
    catch
    {
        return null;
    }
}

/// <summary>
/// Generates SQL for REVOKE statements
/// </summary>
private static string? GenerateSqlFromRevokeStmt(JsonElement revokeStmt)
{
    // Similar implementation to GrantStmt but with REVOKE ... FROM syntax
    try
    {
        // Extract privileges
        if (!revokeStmt.TryGetProperty("privileges", out var privileges) || 
            privileges.GetArrayLength() == 0)
            return null;

        var privList = new List<string>();
        foreach (var priv in privileges.EnumerateArray())
        {
            if (priv.TryGetProperty("AccessPriv", out var accessPriv) &&
                accessPriv.TryGetProperty("priv_name", out var privName))
            {
                var name = privName.GetString();
                if (name != null)
                    privList.Add(name.ToUpper());
            }
        }

        if (privList.Count == 0)
            return null;

        // Extract object type
        var objectType = revokeStmt.TryGetProperty("objtype", out var objType)
            ? objType.GetString()?.Replace("OBJECT_", "") ?? "TABLE"
            : "TABLE";

        // Extract object name
        if (!revokeStmt.TryGetProperty("objects", out var objects) || 
            objects.GetArrayLength() == 0)
            return null;

        var firstObj = objects[0];
        if (!firstObj.TryGetProperty("RangeVar", out var rangeVar))
            return null;

        var (schema, name) = ExtractRelationName(
            JsonDocument.Parse("{\"RangeVar\":" + rangeVar.GetRawText() + "}").RootElement
        );

        // Extract grantee
        if (!revokeStmt.TryGetProperty("grantees", out var grantees) || 
            grantees.GetArrayLength() == 0)
            return null;

        var firstGrantee = grantees[0];
        string granteeName = "public";
        if (firstGrantee.TryGetProperty("RoleSpec", out var roleSpec) &&
            roleSpec.TryGetProperty("rolename", out var rolename))
        {
            granteeName = rolename.GetString() ?? "public";
        }

        var privStr = string.Join(", ", privList);
        return $"REVOKE {privStr} ON {objectType} {QuoteIdent(schema)}.{QuoteIdent(name)} FROM {QuoteIdent(granteeName)};";
    }
    catch
    {
        return null;
    }
}
```

## AST Structure Reference

**GrantStmt Example:**
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
            "relname": "users"
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

## Testing

### Test Case
**File:** `tests/mbulava.PostgreSql.Dac.Tests/Compare/PublishScriptGeneratorTests.cs`

```csharp
[Test]
public void Generate_PrivilegeChanges_CreatesGrantRevoke()
{
    var diff = new PgSchemaDiff
    {
        SchemaName = "public",
        TableDiffs = new List<PgTableDiff>
        {
            new()
            {
                TableName = "users",
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
    
    result.ToUpper().Should().Contain("GRANT SELECT");
    result.Should().Contain("app_user");
}
```

## Related Files
- `src/libs/mbulava.PostgreSql.Dac/Compile/Ast/AstSqlGenerator.cs`
- `src/libs/mbulava.PostgreSql.Dac/Compare/PublishScriptGenerator.cs`
- `tests/mbulava.PostgreSql.Dac.Tests/Compare/PublishScriptGeneratorTests.cs`

## Documentation
- `docs/KNOWN_ISSUES_PROTOBUF.md`
- `docs/architecture/AST_SQL_GENERATION_PLAN.md`

## Priority
🟡 **HIGH** - Required for complete privilege change script generation

## Dependencies
- Related to Issue #[protobuf-corruption]

## Labels
- `enhancement`
- `json-extractor`
- `sql-generation`
- `privileges`
