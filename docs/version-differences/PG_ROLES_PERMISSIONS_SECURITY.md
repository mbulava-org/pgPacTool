# PostgreSQL Roles, Permissions & Security — Version Reference (PG 15–18)

> **Purpose**: This document is the authoritative reference for how pgPacTool must model, compare,
> and publish PostgreSQL role/permission/security objects across supported versions (15–18). All
> generated tests and code must follow the rules herein.
>
> **Sources**: postgresql.org/docs/{15,16,17,18}, Microsoft Learn Azure Database for PostgreSQL
> security docs, and direct review of `DbObjects.cs`, `Compare.cs`, and `PgSchemaComparer.cs`.

---

## 1. Core Concepts (All Versions)

### 1.1 Roles vs. Users
- PostgreSQL has **no separate "user" type** — `CREATE USER` is syntactic sugar for `CREATE ROLE ... LOGIN`.
- Every identity in the system is a **role**. A role with `LOGIN` attribute can be treated as a user.
- Always model identities as `PgRole`; never distinguish "user" vs. "role" at the model layer.

### 1.2 Role Attributes (All Versions, pg_roles columns)

| Attribute      | pg_roles column   | Default     | Description |
|----------------|-------------------|-------------|-------------|
| `SUPERUSER`    | `rolsuper`        | `false`     | Bypasses all permission checks |
| `LOGIN`        | `rolcanlogin`     | `false`     | Can establish a database connection |
| `INHERIT`      | `rolinherit`      | `true`      | Inherits privileges of member roles |
| `CREATEDB`     | `rolcreatedb`     | `false`     | Can create new databases |
| `CREATEROLE`   | `rolcreaterole`   | `false`     | Can create/manage other roles (see version notes) |
| `REPLICATION`  | `rolreplication`  | `false`     | Can initiate streaming replication |
| `BYPASSRLS`    | `rolbypassrls`    | `false`     | Bypasses Row Level Security policies (see version notes) |
| `CONNECTION LIMIT` | `rolconnlimit` | `-1` (unlimited) | Max concurrent connections |
| `PASSWORD`     | `rolpassword`     | `null`      | Hashed credential; never store plaintext |
| `VALID UNTIL`  | `rolvaliduntil`   | `null`      | Password/login expiry timestamp |

### 1.3 Object-Level Privileges

| Object Type   | Grantable Privileges |
|---------------|----------------------|
| DATABASE      | `CONNECT`, `CREATE`, `TEMPORARY` |
| SCHEMA        | `USAGE`, `CREATE` |
| TABLE / VIEW / MATERIALIZED VIEW | `SELECT`, `INSERT`, `UPDATE`, `DELETE`, `TRUNCATE`, `REFERENCES`, `TRIGGER`, `MAINTAIN` (PG 17+) |
| SEQUENCE      | `USAGE`, `SELECT`, `UPDATE` |
| FUNCTION / PROCEDURE | `EXECUTE` |
| TYPE / DOMAIN | `USAGE` |
| FOREIGN DATA WRAPPER | `USAGE` |
| FOREIGN SERVER | `USAGE` |
| LARGE OBJECT  | `SELECT`, `UPDATE` |
| PARAMETER (GUC) | `SET`, `ALTER SYSTEM` |
| TABLESPACE    | `CREATE` |

### 1.4 Special Grantees
- **`PUBLIC`** — implicit role that every role is a member of. Granting to `PUBLIC` gives access to all.
- **`WITH GRANT OPTION`** — allows the grantee to re-grant the privilege.
- **`WITH ADMIN OPTION`** — on role membership, allows the member to grant that role membership to others.

---

## 2. Version-by-Version Breaking Changes

### 2.1 PostgreSQL 15 — Key Security Changes

#### Public Schema Ownership (BREAKING)
- **Change**: Ownership of the `public` schema transferred from `postgres` (superuser) to the new
  built-in role **`pg_database_owner`**, which dynamically resolves to the current database owner.
- **Impact on compare/publish**: When comparing schemas, do **not** flag `public` schema ownership
  as a diff if either side is `pg_database_owner` vs. the actual database owner role — they are
  semantically equivalent.
- **Azure Database for PostgreSQL exception**: Azure overrides this; `public` is always owned by
  `azure_pg_admin`. Treat as a known variance — suppress Azure-side ownership diffs for `public`.

#### Public Schema CREATE Revoked (BREAKING)
- **Change**: `CREATE` privilege on the `public` schema was **revoked from `PUBLIC`** by default.
  Previously any role could create objects in `public`; now explicit grants are required.
- **Impact on compare/publish**:
  - Do not auto-generate `GRANT CREATE ON SCHEMA public TO PUBLIC` during publish unless it is
    explicitly present in the source project.
  - Flag as a warning if the source project assumes the old default (i.e., no explicit grants on
    `public` but objects exist there).

#### New Built-in Roles in PG 15

| Role | Purpose |
|------|---------|
| `pg_database_owner` | Dynamic role: resolves to the owner of the current database |
| `pg_read_all_data`  | `SELECT` on all tables, views, sequences in all schemas |
| `pg_write_all_data` | `INSERT`, `UPDATE`, `DELETE` on all tables, views, sequences |
| `pg_read_all_settings` | Read all GUC configuration settings |
| `pg_read_all_stats`    | Read `pg_stat_*` views |
| `pg_stat_scan_tables`  | Execute `pg_stat_get_*` functions on tables |
| `pg_monitor`           | Combined: `pg_read_all_settings` + `pg_read_all_stats` + `pg_stat_scan_tables` |

> **Rule**: Never generate `CREATE ROLE` DDL for any `pg_*` built-in role. They are system-owned.
> During compare, skip built-in roles from diff output. See Section 4.1 for the definitive list.

#### BYPASSRLS Behavior in PG 15
- Only a `SUPERUSER` can grant `BYPASSRLS` to another role.
- Non-superuser administrators **cannot** create roles with `BYPASSRLS`.
- **Azure PG 15**: `azure_pg_admin` members cannot grant `BYPASSRLS` to non-admin roles.

---

### 2.2 PostgreSQL 16 — Key Security Changes

#### CREATEROLE Tightened (BREAKING)
- **Old behavior (PG ≤ 15)**: A role with `CREATEROLE` could grant **any** role membership to
  anyone, effectively acting like a superuser for role management.
- **New behavior (PG 16+)**: A role with `CREATEROLE` can only grant memberships in roles for
  which it holds **`WITH ADMIN OPTION`**. It cannot grant arbitrary role memberships.
- **Drop restriction**: A `CREATEROLE` role can only `DROP ROLE` for roles it **itself created**.
  Attempting to drop a role created by a different principal raises `ERROR: must be owner of role`.
- **Impact on compare/publish**:
  - When generating `GRANT <role> TO <member>` statements, validate that the executing role has
    `ADMIN OPTION` on `<role>`, or requires a superuser context.
  - When generating `DROP ROLE` in rollback/cleanup scripts, note the originator restriction.

#### BYPASSRLS Behavior in PG 16+ (BREAKING vs. PG 15)
- **Change**: The requirement for superuser to set `BYPASSRLS` was **removed**. Non-superuser
  administrative roles (those with `CREATEROLE`) can now create roles with `BYPASSRLS`.
- **Azure PG 16+**: `azure_pg_admin` members can grant `BYPASSRLS` to roles they create.
- **Impact**: `BYPASSRLS` grants in publish scripts are valid for PG 16+ without superuser context.
  For PG 15, flag a warning that superuser elevation is required.

#### New Built-in Role in PG 16

| Role | Purpose |
|------|---------|
| `pg_create_subscription` | Allows non-superusers to create logical replication subscriptions |

#### Role Membership: INHERIT vs. SET (PG 16+)
- `GRANT <role> TO <member>` now supports:
  - `WITH INHERIT TRUE/FALSE` — controls whether privileges are automatically inherited.
  - `WITH SET TRUE/FALSE` — controls whether `SET ROLE` can be used to switch to that role.
- **Default**: `INHERIT TRUE`, `SET TRUE` (preserves backward-compatible behavior).
- **Impact on compare**: The `PgRole.MemberOf` list must evolve to track inherit/set options per
  membership entry. See Section 5 for required model updates.

---

### 2.3 PostgreSQL 17 — Key Security Changes

#### New Built-in Role: pg_maintain (NEW)

| Role | Purpose |
|------|---------|
| `pg_maintain` | Can run `VACUUM`, `ANALYZE`, `REINDEX`, `CLUSTER`, `REFRESH MATERIALIZED VIEW`, `LOCK TABLE` on any relation, without owning it |

- **Impact**: `pg_maintain` is now the preferred way to delegate maintenance tasks without granting
  ownership. Compare logic must not flag missing ownership when `pg_maintain` membership is present.
- Do not generate `CREATE ROLE pg_maintain` — it is a system role.

#### New Built-in Role: pg_use_reserved_connections (NEW)

| Role | Purpose |
|------|---------|
| `pg_use_reserved_connections` | Allows use of connection slots reserved by `reserved_connections` GUC |

- **Impact**: Only roles with `ADMIN OPTION` on `pg_use_reserved_connections` can grant it further.
  In Azure, this requires the server admin; restore scripts may encounter errors — suppress and warn.

#### New Built-in Role: pg_checkpoint (NEW)

| Role | Purpose |
|------|---------|
| `pg_checkpoint` | Can execute `CHECKPOINT` without being a superuser |

#### DEFAULT PRIVILEGES Improvements (PG 17)
- `ALTER DEFAULT PRIVILEGES` now works correctly for procedures and routines (not just functions).
- **Impact on compare/publish**: Include `PROCEDURE` and `ROUTINE` targets when generating or
  comparing `ALTER DEFAULT PRIVILEGES` statements on PG 17+.

---

### 2.4 PostgreSQL 18 — Key Security Changes

#### OAuth Authentication Support (NEW)
- PG 18 introduces native **OAuth 2.0 validator module** support (`pg_hba.conf` method: `oauth`).
- This is authentication-layer only; role/privilege model is unchanged.
- **Impact**: pgPacTool does not currently model `pg_hba.conf`; no model changes required.
  Document as a known gap for future work.

#### New Predefined Role: pg_signal_autovacuum_worker (NEW)

| Role | Purpose |
|------|--------|
| `pg_signal_autovacuum_worker` | Allows sending signals to autovacuum workers (cancel current table's vacuum or terminate the worker session) |

- **Impact**: Add to built-in role exclusion list. Do not generate `CREATE ROLE` DDL for this role.
  Compare logic must skip it in diffs.

#### MD5 Password Deprecation Warning (PG 18)
- `CREATE ROLE ... PASSWORD '...'` using MD5 now emits a **deprecation warning**.
- `ALTER ROLE ... PASSWORD '...'` with MD5 also warns.
- The server variable `md5_password_warnings` (default `on`) controls whether warnings appear.
- **Impact on compare/publish**: pgPacTool never stores passwords in compare output (`PgRole.Password = null`).
  No model changes needed, but if future versions expose password format, prefer `scram-sha-256`.

#### MAINTAIN Privilege — Per-Table Grant (PG 17+, clarified in PG 18 docs)
- The `MAINTAIN` privilege can be granted on a **per-table basis** (`GRANT MAINTAIN ON TABLE t TO role`).
- The `pg_maintain` predefined role grants this implicitly on **all** tables.
- `MAINTAIN` allows: `VACUUM`, `ANALYZE`, `CLUSTER`, `REINDEX`, `REFRESH MATERIALIZED VIEW`,
  `LOCK TABLE`, and statistics manipulation functions.
- **Impact on compare**: Track per-table `MAINTAIN` grants in `PgTable.Privileges` alongside
  `SELECT`, `INSERT`, etc. The abbreviation is `m` in ACL output.

#### ALTER DEFAULT PRIVILEGES — Large Object Support (PG 18+)
- `ALTER DEFAULT PRIVILEGES` now supports `LARGE OBJECTS` as a target object type.
- **Impact on compare/publish**: On PG 18+, include `LARGE OBJECT` in default-privilege scanning.
  `SELECT` and `UPDATE` are the relevant privileges for large objects.

#### AFTER Trigger Execution Role (PG 18, BREAKING behavior clarification)
- AFTER triggers (including deferred triggers) now execute as the **role that was active when the
  trigger event was queued**, not the role active at `COMMIT` time.
- Previously, a `SET ROLE` between enqueue and commit could change which role ran the trigger.
- **Impact**: No DDL model change, but generated trigger test scripts should not rely on role
  switching between DML and commit to affect trigger executor identity.

---

## 3. Built-in / System Roles — Never Generate DDL For These

### 3.1 Complete Built-in Role List (PG 15–18)

These roles exist in every PostgreSQL instance of the indicated version and above.
**Never emit `CREATE ROLE`, `DROP ROLE`, or `ALTER ROLE` for any of these.**
**Always exclude from compare diffs.**

| Role | First Available |
|------|----------------|
| `pg_monitor`                    | PG 10+ |
| `pg_read_all_settings`          | PG 10+ |
| `pg_read_all_stats`             | PG 10+ |
| `pg_stat_scan_tables`           | PG 10+ |
| `pg_signal_backend`             | PG 10+ |
| `pg_read_server_files`          | PG 11+ |
| `pg_write_server_files`         | PG 11+ |
| `pg_execute_server_program`     | PG 11+ |
| `pg_database_owner`             | **PG 15+** |
| `pg_read_all_data`              | **PG 14+** |
| `pg_write_all_data`             | **PG 14+** |
| `pg_checkpoint`                 | **PG 15+** |
| `pg_create_subscription`        | **PG 16+** |
| `pg_maintain`                   | **PG 17+** |
| `pg_use_reserved_connections`   | **PG 17+** |
| `pg_signal_autovacuum_worker`   | **PG 18+** |

### 3.2 Azure-Specific Reserved Roles
These roles exist only in Azure Database for PostgreSQL and must also never be generated or diffed:

| Role | Notes |
|------|-------|
| `azure_pg_admin`   | Pseudo-superuser; system-managed, cannot be altered |
| `azuresu`          | True superuser; Microsoft-only, never user-accessible |
| `replication`      | Azure internal replication role |
| `localadmin`       | Azure infrastructure role |

---

## 4. Compare Rules

### 4.1 Role Comparison Rules

1. **Skip built-in roles**: Filter out all roles in the list from Section 3 before comparing.
2. **Normalize `pg_database_owner`**: When `public` schema owner is `pg_database_owner`, treat it
   as equivalent to the actual DB owner for diff purposes.
3. **Password comparison**: Never compare password hashes. `PgRole.Password` should always be
   `null` in compare output. Treat password as always-matched.
4. **VALID UNTIL**: Compare as timestamp values; null == "no expiry". Flag if one side has expiry
   and the other does not.
5. **MemberOf / Role Memberships**:
   - On PG 16+, compare `inherit` and `set` options per membership entry (see Section 5).
   - On PG 15, only compare membership presence (no inherit/set options).
6. **CREATEROLE scoping (PG 16+)**: When flagging `GRANT <role> TO <member>` in diffs, annotate
   if the granting role requires `ADMIN OPTION` that may not be present.
7. **BYPASSRLS version gate**: If source has `BYPASSRLS = true` and target version is PG 15,
   emit a warning that superuser elevation is required — do not silently skip.
8. **Connection limit**: `rolconnlimit = -1` (unlimited) must compare equal to an absent value.

### 4.2 Privilege Comparison Rules

1. **Normalize `PUBLIC` grantee**: `PUBLIC` is always lowercase in comparison keys.
2. **Grant option**: `WITH GRANT OPTION` is a distinct state. `SELECT` ≠ `SELECT WITH GRANT OPTION`.
3. **Default privileges** (`ALTER DEFAULT PRIVILEGES`):
   - These are **per-schema, per-grantor** settings stored in `pg_default_acl`.
   - Must be compared separately from object-level ACLs.
   - On PG 17+, include `PROCEDURE`/`ROUTINE` targets.
4. **Schema-level USAGE + CREATE**: Both must be independently tracked. Do not collapse.
5. **Implicit privileges**: Table owners implicitly hold all privileges on their objects. Do not
   emit `GRANT ALL ON TABLE t TO owner` — it is redundant and pollutes diffs.
6. **Inherited privileges**: Privileges inherited through role membership are not in the object
   ACL. Compare only **explicit** grants stored in `pg_class.relacl`, `pg_namespace.nspacl`, etc.
7. **RLS policies**: `ENABLE ROW LEVEL SECURITY` / `FORCE ROW LEVEL SECURITY` are boolean table
   attributes, not privileges. They are part of `PgTable`, not `PgPrivilege`. Compare them as
   table-level boolean diffs.

### 4.3 Schema Ownership Rules

| Scenario | Compare Behavior |
|----------|-----------------|
| `public` schema, source=`pg_database_owner`, target=actual DB owner | **No diff** — semantically equivalent |
| `public` schema on Azure, source=`azure_pg_admin`, target=`azure_pg_admin` | **No diff** |
| Any non-`public` schema with owner change | **Flag as diff** |
| Schema owner is a built-in role (e.g., `pg_database_owner`) | **No diff if DB owner matches** |

---

## 5. Model Requirements (PgRole Enhancements)

The current `PgRole` model in `DbObjects.cs` is missing fields required for full PG 16+ support.
The following properties **must be added**:

```csharp
public class PgRole
{
    // === Existing fields (keep) ===
    public string Name { get; set; } = string.Empty;
    public bool IsSuperUser { get; set; }
    public bool CanLogin { get; set; }
    public bool Inherit { get; set; }           // Role-level INHERIT attribute
    public bool Replication { get; set; }
    public bool BypassRLS { get; set; }
    public string? Password { get; set; }        // Always null in compare output
    public List<string> MemberOf { get; set; } = new();  // Simple list (backward compat)
    public string Definition { get; set; } = string.Empty;

    // === Required additions ===
    public bool CreateDb { get; set; }           // CREATEDB attribute (was missing)
    public bool CreateRole { get; set; }         // CREATEROLE attribute (was missing)
    public int ConnectionLimit { get; set; } = -1; // -1 = unlimited
    public DateTime? ValidUntil { get; set; }    // null = no expiry
    public bool IsBuiltIn { get; set; }          // True for pg_* and azure_* system roles
    public string? Comment { get; set; }         // COMMENT ON ROLE

    // PG 16+: per-membership inherit/set options
    public List<PgRoleMembership> Memberships { get; set; } = new();
}

// Required new type for PG 16+ role membership tracking
public class PgRoleMembership
{
    public string RoleName { get; set; } = string.Empty;  // The role being a member of
    public bool Inherit { get; set; } = true;             // WITH INHERIT TRUE/FALSE (PG 16+)
    public bool CanSet { get; set; } = true;              // WITH SET TRUE/FALSE (PG 16+)
    public bool AdminOption { get; set; }                 // WITH ADMIN OPTION
}
```

---

## 6. Publish / Script Generation Rules

### 6.1 Role DDL Generation Order
Scripts must be emitted in this sequence to respect dependencies:

```
1. CREATE ROLE statements (non-login roles / groups first)
2. CREATE ROLE / CREATE USER statements (login roles)
3. GRANT <role> TO <member> membership grants
4. REVOKE <role> FROM <member> membership revocations
5. ALTER ROLE ... SET parameter assignments
6. Database-level GRANTs (GRANT CONNECT, CREATE, TEMPORARY ON DATABASE)
7. Schema-level GRANTs (GRANT USAGE, CREATE ON SCHEMA)
8. Table/view/sequence/function-level GRANTs
9. ALTER DEFAULT PRIVILEGES statements
10. ROW LEVEL SECURITY enables (ALTER TABLE ... ENABLE ROW LEVEL SECURITY)
11. CREATE POLICY statements
```

### 6.2 Version-Gated DDL Rules

| Statement | Version Gate | Rule |
|-----------|-------------|------|
| `GRANT ... WITH INHERIT TRUE/FALSE` | PG 16+ only | Omit `WITH INHERIT` clause on PG 15 |
| `GRANT ... WITH SET TRUE/FALSE` | PG 16+ only | Omit `WITH SET` clause on PG 15 |
| `GRANT pg_maintain TO ...` | PG 17+ only | Error/skip if target version < 17 |
| `GRANT MAINTAIN ON TABLE ... TO ...` | PG 17+ only | Error/skip if target version < 17 |
| `ALTER DEFAULT PRIVILEGES ... GRANT ... ON LARGE OBJECTS` | PG 18+ only | Omit on PG 15–17 |
| `GRANT pg_use_reserved_connections TO ...` | PG 17+ only | Warn if target version < 17 |
| `GRANT pg_signal_autovacuum_worker TO ...` | PG 18+ only | Error/skip if target version < 18 |
| `GRANT pg_create_subscription TO ...` | PG 16+ only | Error/skip if target version < 16 |
| `GRANT pg_checkpoint TO ...` | PG 15+ only | Error/skip if target version < 15 |
| `BYPASSRLS` in `CREATE ROLE` | PG 16+ without superuser | PG 15 requires superuser context; emit warning |
| `REVOKE CREATE ON SCHEMA public FROM PUBLIC` | PG 15+ | Always include in new DB setup scripts |
| `GRANT CREATE ON SCHEMA public TO PUBLIC` | Any | Flag as security warning before emitting |

### 6.3 Never Generate
- `CREATE ROLE` for any role in the built-in role list (Section 3.1)
- `CREATE ROLE` for any Azure reserved role (Section 3.2)
- `GRANT pg_write_all_data TO <role>` on Azure Database for PostgreSQL (blocked by Azure)
- Plaintext `PASSWORD` values in any generated DDL
- `GRANT ALL ON SCHEMA public TO PUBLIC` without an explicit security warning
- `ALTER ROLE azuresu ...` or `GRANT ... TO azure_pg_admin` (Azure safeguard error)

### 6.4 Default Privileges Generation
Always emit `ALTER DEFAULT PRIVILEGES` for new schemas with:

```sql
-- PG 15+: Secure public schema baseline
REVOKE CREATE ON SCHEMA public FROM PUBLIC;
REVOKE ALL ON DATABASE <dbname> FROM PUBLIC;

-- Then grant explicitly per role
ALTER DEFAULT PRIVILEGES IN SCHEMA <schema>
    GRANT SELECT ON TABLES TO <readonly_role>;

ALTER DEFAULT PRIVILEGES IN SCHEMA <schema>
    GRANT SELECT, INSERT, UPDATE, DELETE ON TABLES TO <readwrite_role>;

ALTER DEFAULT PRIVILEGES IN SCHEMA <schema>
    GRANT USAGE ON SEQUENCES TO <readwrite_role>;

-- PG 17+ only: include procedures
ALTER DEFAULT PRIVILEGES IN SCHEMA <schema>
    GRANT EXECUTE ON ROUTINES TO <app_role>;
```

---

## 7. Testing Requirements

### 7.1 Role Model Tests

Every test class working with roles must cover:

```csharp
// ✅ REQUIRED test scenarios for PgRole
[Theory]
[InlineData("pg_monitor")]
[InlineData("pg_maintain")]            // PG 17+
[InlineData("pg_create_subscription")] // PG 16+
[InlineData("pg_database_owner")]      // PG 15+
[InlineData("azure_pg_admin")]
public void BuiltInRole_MustBeExcludedFromDiff(string roleName)

[Fact]
public void PgRole_Password_IsNeverIncludedInCompare()

[Fact]
public void PgRole_ConnectionLimit_NegativeOne_EqualsUnlimited()

[Theory]
[InlineData(15, false)] // PG 15: BYPASSRLS requires superuser
[InlineData(16, true)]  // PG 16+: BYPASSRLS allowed without superuser
[InlineData(17, true)]
[InlineData(18, true)]
public void BypassRLS_VersionGating_IsCorrect(int pgVersion, bool allowsNonSuperuser)
```

### 7.2 Privilege Compare Tests

```csharp
// ✅ REQUIRED test scenarios for privilege comparison
[Fact]
public void GrantWithGrantOption_IsDistinctFromGrantWithout()

[Fact]
public void PublicGrantee_IsNormalizedToLowercase()

[Fact]
public void ImplicitOwnerPrivileges_AreNotFlaggedAsDiff()

[Fact]
public void SchemaUsage_AndCreate_AreTrackedIndependently()

[Fact]
public void RLSEnabled_IsTableAttribute_NotPrivilege()
```

### 7.3 Version-Specific Tests

```csharp
// ✅ REQUIRED version-gated compare tests
[Fact]
public void PG15_PublicSchema_PgDatabaseOwner_EqualsActualOwner_NoDiff()

[Fact]
public void PG15_PublicSchema_CreateRevoked_IsNotAutoGranted()

[Fact]
public void PG16_CreateroleCanOnlyGrantWithAdminOption()

[Fact]
public void PG16_RoleMembership_InheritAndSet_TrackedPerEntry()

[Fact]
public void PG17_PgMaintain_ExcludedFromGeneratedDDL()

[Fact]
public void PG17_DefaultPrivileges_IncludesRoutines()

[Fact]
public void Azure_PublicSchema_OwnedByAzurePgAdmin_SuppressedInDiff()
```

### 7.4 Script Generation Tests

```csharp
// ✅ REQUIRED script generation tests
[Fact]
public void GeneratedScript_NeverContainsPlaintextPassword()

[Fact]
public void GeneratedScript_DoesNotCreate_BuiltInRoles()

[Fact]
public void GeneratedScript_PG15_OmitsWithInheritClause()

[Fact]
public void GeneratedScript_PG16Plus_IncludesWithInheritClause_WhenNonDefault()

[Fact]
public void GeneratedScript_NewDatabase_RevokesPublicCreateOnPublicSchema()

[Fact]
public void Azure_GeneratedScript_DoesNotGrant_PgWriteAllData()
```

---

## 8. Version Support Matrix

| Feature / Behavior | PG 15 | PG 16 | PG 17 | PG 18 |
|---|---|---|---|---|
| `public` schema owned by `pg_database_owner` | ✅ | ✅ | ✅ | ✅ |
| `CREATE` on `public` revoked from `PUBLIC` by default | ✅ | ✅ | ✅ | ✅ |
| `CREATEROLE` can grant any role membership | ❌ | ❌ | ❌ | ❌ |
| `CREATEROLE` restricted to roles with ADMIN OPTION | ❌ | ✅ | ✅ | ✅ |
| `BYPASSRLS` requires superuser to grant | ✅ | ❌ | ❌ | ❌ |
| `GRANT ... WITH INHERIT/SET` options | ❌ | ✅ | ✅ | ✅ |
| `pg_create_subscription` built-in role | ❌ | ✅ | ✅ | ✅ |
| `pg_maintain` built-in role | ❌ | ❌ | ✅ | ✅ |
| `pg_use_reserved_connections` built-in role | ❌ | ❌ | ✅ | ✅ |
| `pg_checkpoint` built-in role | ✅ | ✅ | ✅ | ✅ |
| `pg_signal_autovacuum_worker` built-in role | ❌ | ❌ | ❌ | ✅ |
| Default privileges for ROUTINE type | ❌ | ❌ | ✅ | ✅ |
| Default privileges for LARGE OBJECT type | ❌ | ❌ | ❌ | ✅ |
| Per-table `MAINTAIN` privilege grant | ❌ | ❌ | ✅ | ✅ |
| OAuth authentication support | ❌ | ❌ | ❌ | ✅ |
| MD5 password deprecation warning | ❌ | ❌ | ❌ | ✅ |
| AFTER trigger runs as enqueueing role | ❌ | ❌ | ❌ | ✅ |

---

## 9. Quick Reference: pg_roles Query for Audit

Use this to enumerate all non-built-in roles for compare/publish operations:

```sql
SELECT
    r.rolname,
    r.rolsuper,
    r.rolinherit,
    r.rolcreaterole,
    r.rolcreatedb,
    r.rolcanlogin,
    r.rolreplication,
    r.rolconnlimit,
    r.rolvaliduntil,
    r.rolbypassrls,
    ARRAY(
        SELECT b.rolname
        FROM pg_auth_members m
        JOIN pg_roles b ON b.oid = m.roleid
        WHERE m.member = r.oid
    ) AS member_of,
    -- PG 16+: inherit/set per membership (join pg_auth_members directly for this)
    pg_catalog.shobj_description(r.oid, 'pg_authid') AS comment
FROM pg_roles r
WHERE r.rolname NOT LIKE 'pg_%'
  AND r.rolname NOT IN ('azure_pg_admin','azuresu','replication','localadmin')
ORDER BY r.rolname;
```

For PG 16+ membership inherit/set options:

```sql
-- PG 16+ only
SELECT
    m.member::regrole::text AS member_role,
    m.roleid::regrole::text AS group_role,
    m.admin_option,
    m.inherit_option,   -- PG 16+
    m.set_option        -- PG 16+
FROM pg_auth_members m
ORDER BY member_role, group_role;
```

---

*Last updated: reviewed against PostgreSQL 15.17, 16.13, 17.9, 18.3 official documentation (February 2026).*
*See also: [`docs/features/multi-version-support/VERSION_COMPATIBILITY_STRATEGY.md`](../features/multi-version-support/VERSION_COMPATIBILITY_STRATEGY.md)*
