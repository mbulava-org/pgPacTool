# Database Extraction Completeness Audit

## Model vs File Generation Checklist

### **PgProject Level**
| Property | Stored In | Status |
|----------|-----------|--------|
| DatabaseName | `.csproj` <DatabaseName> | ✅ |
| PostgresVersion | `.csproj` <PostgresVersion> | ✅ |
| Schemas[] | `{schema}/_schema.sql` | ✅ |
| Roles[] | `Security/Roles/{role}.sql` | ✅ |

---

### **PgSchema Level**
| Property | Stored In | Status |
|----------|-----------|--------|
| Name | `{schema}/_schema.sql` | ✅ |
| Owner | `{schema}/_schema.sql` (AUTHORIZATION clause) | ✅ |
| Definition | `{schema}/_schema.sql` | ✅ |
| Ast | Used for generation, not exported | ✅ |
| Privileges[] | `Security/Permissions/{schema}.sql` | ✅ |
| Tables[] | `{schema}/Tables/{table}.sql` | ✅ |
| Views[] | `{schema}/Views/{view}.sql` | ✅ |
| Functions[] | `{schema}/Functions/{function}.sql` | ✅ |
| Types[] | `{schema}/Types/{type}.sql` | ✅ |
| Sequences[] | `{schema}/Sequences/{sequence}.sql` | ✅ |
| Triggers[] | `{schema}/Triggers/{trigger}.sql` | ✅ |

---

### **PgTable Level**
| Property | Stored In | Status |
|----------|-----------|--------|
| Name | `{schema}/Tables/{table}.sql` | ✅ |
| Definition | `{schema}/Tables/{table}.sql` (CREATE TABLE) | ✅ |
| Ast | Used for comparison, not exported | ✅ |
| Owner | In CREATE TABLE ... OWNER TO clause (if extracted) | ⚠️ Need to verify |
| Columns[] | In CREATE TABLE definition | ✅ |
| Constraints[] | In CREATE TABLE definition | ✅ |
| **Indexes[]** | **`{schema}/Indexes/{index}.sql`** | **✅ NOW ADDED!** |
| Privileges[] | `Security/Permissions/{schema}.sql` | ✅ |
| Tablespace | Not currently extracted | ⚠️ TODO |
| RowLevelSecurity | Not currently extracted | ⚠️ TODO |
| ForceRowLevelSecurity | Not currently extracted | ⚠️ TODO |
| FillFactor | Not currently extracted | ⚠️ TODO |
| InheritedFrom[] | Not currently extracted | ⚠️ TODO |
| PartitionStrategy | Not currently extracted | ⚠️ TODO |
| PartitionExpression | Not currently extracted | ⚠️ TODO |

---

### **PgColumn Level**
| Property | Stored In | Status |
|----------|-----------|--------|
| Name | In CREATE TABLE definition | ✅ |
| DataType | In CREATE TABLE definition | ✅ |
| IsNotNull | In CREATE TABLE definition (NOT NULL clause) | ✅ |
| DefaultExpression | In CREATE TABLE definition (DEFAULT clause) | ✅ |
| Position | Implicit from order in CREATE TABLE | ✅ |
| Collation | In CREATE TABLE definition (COLLATE clause) | ⚠️ Need to verify |
| Comment | Not in CREATE TABLE | ⚠️ Need separate COMMENT statements |

---

### **PgConstraint Level**
| Property | Stored In | Status |
|----------|-----------|--------|
| Name | In CREATE TABLE definition | ✅ |
| Definition | In CREATE TABLE definition | ✅ |
| Type | Implicit from constraint type | ✅ |
| Keys[] | In constraint definition | ✅ |
| CheckExpression | In CHECK(...) clause | ✅ |
| ReferencedTable | In REFERENCES clause | ✅ |
| ReferencedColumns[] | In REFERENCES clause | ✅ |

---

### **PgIndex Level**
| Property | Stored In | Status |
|----------|-----------|--------|
| Name | `{schema}/Indexes/{index}.sql` | ✅ |
| Definition | `{schema}/Indexes/{index}.sql` (CREATE INDEX) | ✅ |
| Owner | In CREATE INDEX ... OWNER TO (if extracted) | ⚠️ Need to verify |

---

### **PgView Level**
| Property | Stored In | Status |
|----------|-----------|--------|
| Name | `{schema}/Views/{view}.sql` | ✅ |
| Definition | `{schema}/Views/{view}.sql` (CREATE VIEW) | ✅ |
| Ast | Used for comparison, not exported | ✅ |
| Owner | In CREATE VIEW definition | ⚠️ Need to verify |

---

### **PgFunction Level**
| Property | Stored In | Status |
|----------|-----------|--------|
| Name | `{schema}/Functions/{function}.sql` | ✅ |
| Definition | `{schema}/Functions/{function}.sql` (CREATE FUNCTION) | ✅ |
| Ast | Used for comparison, not exported | ✅ |
| Owner | In CREATE FUNCTION definition | ⚠️ Need to verify |
| Privileges[] | `Security/Permissions/{schema}.sql` | ⚠️ TODO - Add function privileges |

---

### **PgType Level**
| Property | Stored In | Status |
|----------|-----------|--------|
| Name | `{schema}/Types/{type}.sql` | ✅ |
| Definition | `{schema}/Types/{type}.sql` (CREATE TYPE) | ✅ |
| Ast | Used for comparison, not exported | ✅ |
| Owner | In CREATE TYPE definition | ⚠️ Need to verify |

---

### **PgSequence Level**
| Property | Stored In | Status |
|----------|-----------|--------|
| Name | `{schema}/Sequences/{sequence}.sql` | ✅ |
| Definition | `{schema}/Sequences/{sequence}.sql` (CREATE SEQUENCE) | ✅ |
| Ast | Used for comparison, not exported | ✅ |
| Owner | In CREATE SEQUENCE ... OWNED BY clause | ⚠️ Need to verify |

---

### **PgTrigger Level**
| Property | Stored In | Status |
|----------|-----------|--------|
| Name | `{schema}/Triggers/{trigger}.sql` | ✅ |
| TableName | In CREATE TRIGGER ... ON clause | ✅ |
| Definition | `{schema}/Triggers/{trigger}.sql` (CREATE TRIGGER) | ✅ |
| Ast | Used for comparison, not exported | ✅ |
| Owner | Implicit (table owner) | ✅ |

---

### **PgRole Level**
| Property | Stored In | Status |
|----------|-----------|--------|
| Name | `Security/Roles/{role}.sql` | ✅ |
| IsSuperUser | CREATE ROLE ... SUPERUSER/NOSUPERUSER | ✅ |
| CanLogin | CREATE ROLE ... LOGIN/NOLOGIN | ✅ |
| Inherit | CREATE ROLE ... INHERIT/NOINHERIT | ✅ |
| Replication | CREATE ROLE ... REPLICATION/NOREPLICATION | ✅ |
| BypassRLS | CREATE ROLE ... BYPASSRLS/NOBYPASSRLS | ✅ |
| Password | Not exported (security) | ✅ (intentional) |
| MemberOf[] | GRANT {parent_role} TO {role}; | ✅ |
| Definition | `Security/Roles/{role}.sql` | ✅ |

---

### **PgPrivilege Level**
| Property | Stored In | Status |
|----------|-----------|--------|
| Grantee | GRANT ... TO {grantee}; | ✅ |
| PrivilegeType | GRANT {privilege_type} ... | ✅ |
| IsGrantable | WITH GRANT OPTION clause | ✅ |
| Grantor | Not in GRANT statement | ⚠️ May need BY {grantor} |

---

## Missing/Incomplete Items

### **🔴 High Priority** (Needed for exact match)

1. **Column Comments**
   - Extracted: ❓ Need to check
   - Generated: ❌ Not generated
   - Should be: `{schema}/Tables/{table}_comments.sql` or inline

2. **Function/View/Type Privileges**
   - Extracted: ✅ (privileges system is general)
   - Generated: ⚠️ Only schema and table privileges are generated
   - Should be: Added to `Security/Permissions/{schema}.sql`

3. **Table Owner**
   - Extracted: ✅ (Owner property exists)
   - Generated: ⚠️ Need `ALTER TABLE ... OWNER TO ...` statement
   - Should be: `{schema}/Tables/{table}_owner.sql` or in definition

---

### **🟡 Medium Priority** (Advanced features)

4. **Table Metadata** (if extracted)
   - Tablespace: `ALTER TABLE ... SET TABLESPACE ...`
   - RLS: `ALTER TABLE ... ENABLE ROW LEVEL SECURITY;`
   - FillFactor: `ALTER TABLE ... SET (fillfactor = ...);`
   - Should be: `{schema}/Tables/{table}_alter.sql`

5. **Table Inheritance**
   - InheritedFrom[]: In CREATE TABLE ... INHERITS(...)
   - Might already be in Definition

6. **Table Partitioning**
   - PartitionStrategy/Expression: In CREATE TABLE ... PARTITION BY ...
   - Might already be in Definition

---

### **🟢 Low Priority** (Rarely used)

7. **Index Owner**
   - Extracted: ✅ (Owner property exists)
   - Generated: ⚠️ May need ALTER INDEX ... OWNER TO
   
8. **Grantor in Privileges**
   - Currently not preserved
   - May need for audit trails

---

## Recommended Actions

### **Action 1: Verify Current Extraction**
Run extraction with verbose to see what's actually being captured:
```powershell
postgresPacTools extract ... --verbose
```

### **Action 2: Add Missing Owner Statements**
For tables, views, functions, etc. that have Owner != schema.Owner:
```sql
ALTER TABLE {schema}.{table} OWNER TO {owner};
ALTER VIEW {schema}.{view} OWNER TO {owner};
ALTER FUNCTION {schema}.{function}(...) OWNER TO {owner};
```

### **Action 3: Add Column Comments**
If comments are extracted:
```sql
COMMENT ON COLUMN {schema}.{table}.{column} IS '{comment}';
```

### **Action 4: Add All Privilege Types**
Extend `GenerateRolesAndPermissionsAsync()` to include:
- Function privileges
- View privileges
- Sequence privileges
- Type privileges

### **Action 5: Add Table Metadata**
If Tablespace/RLS/FillFactor are extracted, generate:
```sql
ALTER TABLE {schema}.{table} SET TABLESPACE {tablespace};
ALTER TABLE {schema}.{table} ENABLE ROW LEVEL SECURITY;
ALTER TABLE {schema}.{table} SET (fillfactor = {value});
```

---

## Current Status Summary

### ✅ **Currently Exported**
- Schemas (CREATE SCHEMA)
- Tables (CREATE TABLE with columns, constraints)
- Indexes (CREATE INDEX) **← JUST ADDED!**
- Views (CREATE VIEW)
- Functions (CREATE FUNCTION)
- Types (CREATE TYPE)
- Sequences (CREATE SEQUENCE)
- Triggers (CREATE TRIGGER)
- Roles (CREATE ROLE with attributes and memberships)
- Permissions (GRANT for schemas and tables)

### ⚠️ **Potentially Missing**
- Column comments
- Function/View/Sequence/Type privileges
- Table/View/Function/Index owner ALTER statements
- Table metadata (Tablespace, RLS, FillFactor)

### ❓ **Need to Verify in Extraction**
Let's check if these are even being extracted from the database first before adding them to file generation.

---

**Next Step:** Run audit to see what's actually in the extracted `PgProject` object.
