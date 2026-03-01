# PostgreSQL AST JSON Format Documentation

## Overview

This document describes the JSON format for PostgreSQL Abstract Syntax Trees (AST) as defined by the `pg_query` protobuf schema. This format is used by the libpg_query library and our Npgquery wrapper.

## References

- **Protobuf Schema**: `src/libs/Npgquery/Npgquery/Protos/pg_query.proto`
- **libpg_query**: https://github.com/pganalyze/libpg_query
- **pg_query Deparser**: Uses this format for SQL generation

## Root Structure

All AST documents follow this structure:

```json
{
  "version": 170004,  // PostgreSQL version (17.0.4)
  "stmts": [
    {
      "stmt": {
        // Statement node (SelectStmt, CreateStmt, DropStmt, etc.)
      },
      "stmt_len": 0  // Statement length (optional, usually 0)
    }
  ]
}
```

## Common Node Types

### String Node
```json
{
  "String": {
    "sval": "table_name"
  }
}
```

### RangeVar (Table/View Reference)
```json
{
  "RangeVar": {
    "schemaname": "public",  // Optional, omit for current schema
    "relname": "table_name",
    "inh": true,             // Inheritance flag
    "relpersistence": "p",   // 'p' = permanent, 't' = temporary
    "location": 25           // Character position in original SQL
  }
}
```

## DROP Statements

### DROP TABLE

```json
{
  "version": 170004,
  "stmts": [{
    "stmt": {
      "DropStmt": {
        "objects": [
          [
            {"String": {"sval": "public"}},
            {"String": {"sval": "users"}}
          ]
        ],
        "removeType": "OBJECT_TABLE",
        "behavior": "DROP_RESTRICT",  // or "DROP_CASCADE"
        "missing_ok": true             // IF EXISTS
      }
    }
  }]
}
```

### DROP VIEW
Same as DROP TABLE, but with `"removeType": "OBJECT_VIEW"`

### DROP SEQUENCE
Same as DROP TABLE, but with `"removeType": "OBJECT_SEQUENCE"`

### DROP FUNCTION
Same as DROP TABLE, but with `"removeType": "OBJECT_FUNCTION"`

### DROP TRIGGER
```json
{
  "version": 170004,
  "stmts": [{
    "stmt": {
      "DropStmt": {
        "objects": [
          [
            {"String": {"sval": "public"}},      // schema
            {"String": {"sval": "users"}},        // table
            {"String": {"sval": "audit_trigger"}} // trigger name
          ]
        ],
        "removeType": "OBJECT_TRIGGER",
        "behavior": "DROP_RESTRICT",
        "missing_ok": true
      }
    }
  }]
}
```

## ALTER TABLE Statements

### ALTER TABLE ADD COLUMN

```json
{
  "version": 170004,
  "stmts": [{
    "stmt": {
      "AlterTableStmt": {
        "relation": {
          "schemaname": "public",
          "relname": "users",
          "inh": true,
          "relpersistence": "p"
        },
        "cmds": [
          {
            "AlterTableCmd": {
              "subtype": "AT_AddColumn",
              "def": {
                "ColumnDef": {
                  "colname": "email",
                  "typeName": {
                    "names": [
                      {"String": {"sval": "varchar"}}
                    ],
                    "typemod": -1
                  },
                  "is_local": true,
                  "constraints": []  // For NOT NULL, DEFAULT, etc.
                }
              }
            }
          }
        ]
      }
    }
  }]
}
```

### ALTER TABLE DROP COLUMN

```json
{
  "version": 170004,
  "stmts": [{
    "stmt": {
      "AlterTableStmt": {
        "relation": {
          "schemaname": "public",
          "relname": "users"
        },
        "cmds": [
          {
            "AlterTableCmd": {
              "subtype": "AT_DropColumn",
              "name": "old_column",
              "missing_ok": true  // IF EXISTS
            }
          }
        ]
      }
    }
  }]
}
```

## CREATE Statements

### CREATE INDEX

```json
{
  "version": 170004,
  "stmts": [{
    "stmt": {
      "IndexStmt": {
        "idxname": "idx_users_email",
        "relation": {
          "schemaname": "public",
          "relname": "users"
        },
        "indexParams": [
          {
            "IndexElem": {
              "name": "email"
            }
          }
        ],
        "unique": false,
        "if_not_exists": false
      }
    }
  }]
}
```

## GRANT/REVOKE Statements

### GRANT

```json
{
  "version": 170004,
  "stmts": [{
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
              "priv_name": "SELECT"
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
  }]
}
```

## Enum Values

### ObjectType
```
OBJECT_TABLE = 42
OBJECT_VIEW = 52
OBJECT_SEQUENCE = 38
OBJECT_FUNCTION = 20
OBJECT_TRIGGER = 45
OBJECT_INDEX = 21
```

### DropBehavior
```
DROP_RESTRICT = 1
DROP_CASCADE = 2
```

### AlterTableType
```
AT_AddColumn
AT_DropColumn
AT_AlterColumnType
AT_SetNotNull
AT_DropNotNull
AT_SetDefault
AT_DropDefault
AT_AddConstraint
AT_DropConstraint
```

## Notes

1. **json_name annotations**: The protobuf uses camelCase for JSON (e.g., `removeType`, `missing_ok`)
2. **Location fields**: Usually optional, used for error reporting
3. **Version field**: Current version is 170004 (PostgreSQL 17.0.4)
4. **Arrays**: Most collections use arrays, not repeated fields in JSON
5. **Omit empty fields**: Empty/null fields can be omitted from JSON

## Building AST JSON in C#

Use System.Text.Json to build structures:

```csharp
var dropTableAst = new
{
    version = 170004,
    stmts = new[]
    {
        new
        {
            stmt = new
            {
                DropStmt = new
                {
                    objects = new[]
                    {
                        new[]
                        {
                            new { String = new { sval = "public" } },
                            new { String = new { sval = "users" } }
                        }
                    },
                    removeType = "OBJECT_TABLE",
                    behavior = "DROP_RESTRICT",
                    missing_ok = true
                }
            }
        }
    }
};

var json = JsonSerializer.Serialize(dropTableAst);
var doc = JsonDocument.Parse(json);
var sql = AstSqlGenerator.Generate(doc);  // → "DROP TABLE IF EXISTS public.users;"
```

## Validation

Always validate generated AST by:
1. Parsing with `Parser.Parse()` to verify structure
2. Deparsing with `Parser.Deparse()` to generate SQL
3. Round-trip testing: SQL → Parse → Deparse → SQL
