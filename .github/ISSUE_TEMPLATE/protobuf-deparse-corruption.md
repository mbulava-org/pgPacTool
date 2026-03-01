---
name: Protobuf Deparse Corruption on Linux
about: pg_query_deparse_protobuf returns binary garbage instead of SQL on Linux
title: '[BUG] Protobuf deparse returns corrupted output on Linux'
labels: 'bug, critical, linux, native-interop'
assignees: ''
---

## Description
The `pg_query_deparse_protobuf` native function returns corrupted binary data instead of SQL text when running on Linux (Ubuntu 24.04) in GitHub Actions CI/CD pipeline.

## Environment
- **OS:** Ubuntu 24.04 (GitHub Actions runner)
- **Runtime:** linux-x64
- **Framework:** .NET 10
- **Native Library:** libpg_query.so (9.3 MB)
- **Package:** Google.Protobuf 3.33.5

## Expected Behavior
```sql
ALTER TABLE public.users DROP COLUMN IF EXISTS old_column;
```

## Actual Behavior
```
DROP TABLESPACE IF EXISTS \u0012\u0006PUBLIC\u001a\u0005USERS \u0001*\u0001P
```

The output contains protobuf control characters (0x01-0x1F range), indicating the function is returning raw protobuf bytes instead of deparsed SQL.

## Reproduction Steps
1. Build project on Linux (Ubuntu 24.04)
2. Run test: `dotnet test --filter "PublishScriptGeneratorTests.Generate_ColumnTypeChange_CreatesAlterType"`
3. Observe corrupted SQL output

## Failed Tests
```
❌ Generate_ColumnTypeChange_CreatesAlterType
❌ Generate_DropColumn_WithDropFlag_CreatesDropColumn
```

## Root Cause
Cross-platform protobuf serialization incompatibility between:
- C# Google.Protobuf serialization
- Native libpg_query protobuf-c deserialization

## Workaround
Use JSON-based SQL extraction instead of protobuf deparse:

**File:** `src/libs/mbulava.PostgreSql.Dac/Compile/Ast/AstSqlGenerator.cs`
```csharp
public static string Generate(JsonElement astElement)
{
    var json = astElement.GetRawText();
    using var doc = JsonDocument.Parse(json);
    
    // Use JSON extraction instead of broken protobuf path
    return Generate(doc);
}
```

## Solution Required
1. Fix `Generate(JsonElement)` to delegate to `Generate(JsonDocument)` ✅ **DONE**
2. Add GrantStmt support to `TryExtractSqlFromAstJson` ⏳ **IN PROGRESS**
3. Add RevokeStmt support to `TryExtractSqlFromAstJson` ⏳ **IN PROGRESS**

## Related Files
- `src/libs/mbulava.PostgreSql.Dac/Compile/Ast/AstSqlGenerator.cs`
- `src/libs/Npgquery/Npgquery/Npgquery.cs`
- `src/libs/Npgquery/Npgquery/Native/NativeMethods.cs`
- `tests/mbulava.PostgreSql.Dac.Tests/Compare/PublishScriptGeneratorTests.cs`

## Documentation
See: `docs/KNOWN_ISSUES_PROTOBUF.md`

## Priority
🔴 **CRITICAL** - Blocks Linux CI/CD builds

## Labels
- `bug`
- `critical`
- `linux`
- `native-interop`
- `protobuf`
- `ci-cd`
