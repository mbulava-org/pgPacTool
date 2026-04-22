# PostgreSQL Database Objects — Version Reference (PG 15–18)

> **Purpose**: Authoritative reference for how pgPacTool must model, compare, and publish
> PostgreSQL database objects (tables, indexes, views, sequences, functions/procedures, triggers,
> types, schemas) across supported versions (15–18). All generated DDL and compare logic must
> follow the rules herein.
>
> **Companion document**: See `PG_ROLES_PERMISSIONS_SECURITY.md` for role, privilege, and
> security-specific version differences.
>
> **Sources**: postgresql.org/docs/{15,16,17,18}, official release notes, and direct review
> of `DbObjects.cs`.

---

## 1. Tables (`PgTable`)

### 1.1 Features Available Across All Supported Versions (PG 15+)

| Feature | Notes |
|---------|-------|
| `UNLOGGED` / `LOGGED` tables | Unlogged tables skip WAL; not replicated |
| Declarative partitioning (`RANGE`, `LIST`, `HASH`) | `PARTITION BY` clause |
| Table inheritance (`INHERITS`) | `InheritedFrom` list in model |
| Generated columns (`GENERATED ALWAYS AS ... STORED`) | `IsGenerated` + `GenerationExpression` |
| Identity columns (`GENERATED ALWAYS/BY DEFAULT AS IDENTITY`) | `IsIdentity` + `IdentityGeneration` |
| Column storage override (`STORAGE PLAIN/EXTERNAL/EXTENDED/MAIN`) | Rarely modeled; capture in Definition |
| Column compression (`COMPRESSION lz4/pglz`) | Per-column; PG 14+ |
| `FILLFACTOR` storage parameter | `FillFactor` in model |
| Row Level Security (`ENABLE/FORCE ROW LEVEL SECURITY`) | `RowLevelSecurity` + `ForceRowLevelSecurity` |
| `NULLS NOT DISTINCT` on UNIQUE constraints | PG 15+ |

### 1.2 Version-by-Version Table Changes

#### PG 15
- **`NULLS NOT DISTINCT`** added to `CREATE INDEX` and `UNIQUE` constraints. A unique constraint
  now treats NULLs as equal when `NULLS NOT DISTINCT` is specified. The default remains
  `NULLS DISTINCT` (each NULL is unique).
  - **Compare rule**: Track `NullsDistinct` boolean on `PgConstraint` (Unique). Default = `true`.
  - Scripts for PG 14 must omit this clause; it is a PG 15+ syntax.
- **`MERGE` statement** added. Not a DDL object but affects trigger testing (MERGE fires row triggers).

#### PG 16
- **`pg_dump` includes column defaults in COPY format** — no model impact but affects restore order.
- **`NOT NULL` constraints can be named** (`CONSTRAINT name NOT NULL col`) and marked `NO INHERIT`.
  - **Model impact**: `PgConstraint` should optionally track a NOT NULL constraint name.
  - `NO INHERIT` flag on constraints (CHECK, NOT NULL) prevents inheritance by child tables.
- **`COPY FROM ... DEFAULT <value>`** — not DDL; no model impact.

#### PG 17
- **`VIRTUAL` generated columns** added (`GENERATED ALWAYS AS (...) VIRTUAL`).
  Previously only `STORED` was supported.
  - **Model impact**: Add `GeneratedColumnKind` (`Stored` / `Virtual`) to `PgColumn`.
  - Virtual generated columns are not stored on disk; they are computed at query time.
  - **Compare rule**: `STORED ≠ VIRTUAL` — they are semantically different and must be flagged as diff.
  - Virtual columns cannot be part of indexes or foreign keys.
- **`NOT NULL` with `NO INHERIT`** on table-level also applies to PG 17 (confirmed).
- **`MAINTAIN` privilege** can be granted per-table (`GRANT MAINTAIN ON TABLE t TO role`).
  - Add `MAINTAIN` to the recognized `PrivilegeType` values in `PgPrivilege`.
- **Partition pruning improvements** — runtime behavior only; no DDL model impact.
- **`pg_dump` schema-only mode writes table statistics** — no model impact.

#### PG 18
- **`GENERATED ... VIRTUAL`** columns are stable and fully supported (introduced in PG 17).
- **`ONLY` option for VACUUM on partitioned tables** — maintenance operation, no DDL change.
- **`WITHOUT OVERLAPS`** exclusion constraint clause for temporal tables (range/period overlap
  prevention). If modeling constraints, capture this in `PgConstraint.Definition`.
- **`AFTER` triggers execute as the role at queue time** (not commit time) — see
  `PG_ROLES_PERMISSIONS_SECURITY.md` §2.4.

### 1.3 Table Compare Rules

1. **Column order**: Compare by column name, not ordinal position (reordering is not DDL-driven).
2. **Generated column kind**: `STORED` and `VIRTUAL` are not equivalent — flag as diff.
3. **RLS**: `RowLevelSecurity` and `ForceRowLevelSecurity` are boolean table attributes, compared
   independently.
4. **Partitioning**: Compare `PartitionStrategy` and `PartitionExpression`. A change here requires
   table recreation (destructive diff — flag as warning).
5. **NULLS NOT DISTINCT**: Only valid on PG 15+. If source has it and target is PG 14, emit error.
6. **`MAINTAIN` privilege**: Track as part of `PgTable.Privileges` on PG 17+.
7. **Inherited tables**: Only diff columns/constraints that are not inherited (owned by this table).

---

## 2. Indexes (`PgIndex`)

### 2.1 Features Available Across All Supported Versions (PG 15+)

| Feature | Notes |
|---------|-------|
| B-Tree, Hash, GiST, SP-GiST, GIN, BRIN access methods | Capture in `Definition` |
| `UNIQUE` indexes | Track via `PgConstraint` (Unique) linked to index |
| `CONCURRENTLY` build | Runtime flag; not stored in DDL model |
| `INCLUDE` (covering indexes) | Non-key columns; B-Tree, GiST, SP-GiST, BRIN |
| Partial indexes (`WHERE` clause) | Capture in `Definition` |
| Expression indexes | Capture in `Definition` |
| `NULLS FIRST/LAST` per-column | Capture in `Definition` |

### 2.2 Version-by-Version Index Changes

#### PG 15
- **`NULLS NOT DISTINCT`** on unique indexes — treats NULLs as equal for uniqueness purposes.
  - **Model impact**: Add `NullsDistinct` boolean to `PgIndex` (default `true`).
  - Emit `NULLS NOT DISTINCT` only on PG 15+ scripts.

#### PG 16
- **`pg_index.indnullsnotdistinct`** system catalog column added for tracking.
- **Incremental sort performance improvements** — runtime only.

#### PG 17
- **`REINDEX` (and `CLUSTER`) can be run by roles with `pg_maintain`** membership or
  per-table `MAINTAIN` privilege — no DDL model change.
- **`CREATE INDEX ... INCLUDE` now supports BRIN indexes** (previously B-Tree, GiST, SP-GiST only).

#### PG 18
- No breaking DDL changes to indexes. `NULLS NOT DISTINCT` and `INCLUDE` remain stable.

### 2.3 Index Compare Rules

1. Compare by `Name` within a table. Index definitions are compared via normalized `Definition`.
2. `NULLS NOT DISTINCT` is a semantic difference — flag as diff.
3. Partial index `WHERE` clauses must be normalized (strip extra whitespace, lowercase keywords)
   before comparison to avoid false positives.
4. Do not diff system-created indexes (those backing PRIMARY KEY or UNIQUE constraints);
   diff the constraint instead and let the index follow.

---

## 3. Views & Materialized Views (`PgView`)

### 3.1 Features Available Across All Supported Versions (PG 15+)

| Feature | Notes |
|---------|-------|
| Regular views (`CREATE VIEW`) | `IsMaterialized = false` |
| Materialized views (`CREATE MATERIALIZED VIEW`) | `IsMaterialized = true` |
| `WITH CHECK OPTION` on updatable views | Capture in `Definition` |
| `SECURITY DEFINER` / `SECURITY INVOKER` on views | PG 15+ native; see below |

### 3.2 Version-by-Version View Changes

#### PG 15
- **`SECURITY_INVOKER` option on views** (`CREATE VIEW ... WITH (security_invoker = on)`).
  Previously all views ran with the view owner's permissions (security definer behavior).
  Now views can explicitly opt into running with the querying role's permissions.
  - **Model impact**: Add `SecurityInvoker` boolean to `PgView` (default `false`).
  - **Compare rule**: `SecurityInvoker` difference must be flagged — it changes which role's
    privileges are checked at query time.
  - Generate `WITH (security_invoker = on)` on PG 15+ only.

#### PG 16
- No breaking view DDL changes.

#### PG 17
- **`REFRESH MATERIALIZED VIEW`** can now be run by roles with `pg_maintain` or per-table
  `MAINTAIN` privilege. No DDL model change.
- **`CREATE VIEW` with `RECURSIVE`** improvements — behavior stable since PG 14.

#### PG 18
- No breaking view DDL changes. `SECURITY_INVOKER` remains stable.

### 3.3 View Compare Rules

1. Compare view definitions after normalization (strip trailing semicolons, normalize whitespace).
2. `IsMaterialized` is a hard type difference — a regular view and a materialized view with the
   same name are treated as completely different objects requiring drop+recreate.
3. `SecurityInvoker = true` vs `false` must be flagged as diff on PG 15+.
4. Materialized views have storage (like tables): compare `Tablespace` and `FillFactor` if tracked.

---

## 4. Functions & Procedures (`PgFunction`)

### 4.1 Features Available Across All Supported Versions (PG 15+)

| Feature | Notes |
|---------|-------|
| `CREATE FUNCTION` | Returns a value |
| `CREATE PROCEDURE` | No return value; can use `COMMIT`/`ROLLBACK` |
| `LANGUAGE` (plpgsql, sql, c, etc.) | Capture in `Definition` |
| `SECURITY DEFINER` / `SECURITY INVOKER` | Default: `SECURITY INVOKER` |
| `VOLATILITY` (`VOLATILE`, `STABLE`, `IMMUTABLE`) | Affects query planning and caching |
| `PARALLEL` (`UNSAFE`, `RESTRICTED`, `SAFE`) | Parallelism safety |
| `COST`, `ROWS` hints | Planner hints |
| `SET search_path = ...` | Important for security-definer functions |

### 4.2 Version-by-Version Function/Procedure Changes

#### PG 15
- **`search_path` safety for security definer functions**: Functions referencing objects by
  unqualified name in non-default schemas must fix `search_path` in their definition.
  pgPacTool should warn if a `SECURITY DEFINER` function does not have an explicit
  `SET search_path` clause.

#### PG 16
- **`GRANT EXECUTE ON ROUTINE`** syntax now works for both functions and procedures uniformly.
  - **Impact**: Treat `FUNCTION` and `PROCEDURE` as interchangeable targets in privilege compare.
- **SQL-body functions** (using `RETURN <expr>` without `BEGIN`/`END`) are now more stable.
  Normalize body comparison to avoid whitespace false positives.

#### PG 17
- **`ALTER DEFAULT PRIVILEGES ... ON ROUTINES`** now correctly applies to both functions and
  procedures (was unreliable before PG 17 for procedures).
  - **Impact**: On PG 17+, `ROUTINE` is a valid target in `ALTER DEFAULT PRIVILEGES`.
  - On PG 15–16, only `FUNCTION` is reliable; `PROCEDURE` may not behave as expected.
- **`MAINTAIN` via `pg_maintain`** does not affect functions; this is table-specific.
- **Security**: Functions used by expression indexes must reference non-default schemas via
  qualified names (enforced during index creation for security). Warn if unqualified names
  are used in index expressions.

#### PG 18
- **`search_path` enforcement for expression indexes / materialized views** is tightened:
  maintenance operations (`ANALYZE`, `CLUSTER`, `CREATE INDEX`, `REINDEX`,
  `CREATE MATERIALIZED VIEW`, `REFRESH MATERIALIZED VIEW`) now prevent unsafe access if
  functions reference non-default schemas without a fixed `search_path`.
  - **Impact**: When generating or validating function DDL for expression indexes, verify that
    `SET search_path` is included in any `SECURITY DEFINER` function body.

### 4.3 Function Compare Rules

1. Functions are identified by **name + argument types** (overloading is supported). The
   qualified signature is the compare key, not just the name.
2. `SECURITY DEFINER` vs `SECURITY INVOKER` must be flagged as a diff.
3. `VOLATILITY` differences must be flagged (affects index usage and caching).
4. `PARALLEL` differences must be flagged.
5. Body normalization: strip leading/trailing whitespace and normalize line endings before
   comparing `Definition` strings.
6. On PG 17+, `ROUTINE` is a valid `ALTER DEFAULT PRIVILEGES` target; on PG 15–16, use `FUNCTION`.

---

## 5. Sequences (`PgSequence`)

### 5.1 Features Available Across All Supported Versions (PG 15+)

| Feature | `SeqOption.OptionName` | Notes |
|---------|------------------------|-------|
| `START` | `START` | Starting value |
| `INCREMENT` | `INCREMENT` | Step value |
| `MINVALUE` / `NO MINVALUE` | `MINVALUE` | Minimum value |
| `MAXVALUE` / `NO MAXVALUE` | `MAXVALUE` | Maximum value |
| `CACHE` | `CACHE` | Preallocated values per session |
| `CYCLE` / `NO CYCLE` | `CYCLE` | Wrap-around behavior |
| `OWNED BY` | Tracked separately | Links sequence to a column |

### 5.2 Version-by-Version Sequence Changes

#### PG 15
- No breaking changes to sequence DDL.

#### PG 16
- **`SEQUENCE ... AS <type>`** allows specifying the data type (`smallint`, `integer`, `bigint`).
  Default is `bigint`.
  - **Model impact**: Add `DataType` (`string`, default `"bigint"`) to `PgSequence`.
  - **Compare rule**: `smallint` vs `bigint` sequences must be flagged as diff.
- **`pg_sequences` view** exposes `data_type` — use this for extraction queries.

#### PG 17
- No breaking changes to sequence DDL.
- **`nextval`/`setval` require `UPDATE` privilege** (unchanged, but clarified in docs).

#### PG 18
- No breaking changes to sequence DDL.

### 5.3 Sequence Compare Rules

1. Compare all `SeqOption` entries by `OptionName` (case-insensitive).
2. `OWNED BY` changes are treated as a column linkage diff, not a sequence structural diff.
3. On PG 16+, compare `DataType`. When extracting from PG 15, assume `bigint`.
4. `NO CYCLE` == absent `CYCLE` — normalize before comparison.

---

## 6. Triggers (`PgTrigger`)

### 6.1 Features Available Across All Supported Versions (PG 15+)

| Feature | Notes |
|---------|-------|
| `BEFORE` / `AFTER` / `INSTEAD OF` | Timing |
| Row-level vs statement-level | `FOR EACH ROW` vs `FOR EACH STATEMENT` |
| `WHEN` condition | Filtered trigger |
| `REFERENCING` (transition tables) | For statement-level triggers |
| `DEFERRABLE` / `INITIALLY DEFERRED` | Constraint triggers only |

### 6.2 Version-by-Version Trigger Changes

#### PG 15
- No breaking DDL changes.
- **`MERGE` statement fires row-level triggers** for INSERT/UPDATE/DELETE sub-commands.
  Test scenarios should verify trigger behavior with MERGE.

#### PG 16
- No breaking DDL changes to triggers.

#### PG 17
- No breaking DDL changes to triggers.
- `REFRESH MATERIALIZED VIEW` fires no triggers (unchanged).

#### PG 18 — BREAKING BEHAVIOR CHANGE
- **AFTER trigger executor role**: AFTER triggers (including deferred) now execute as the role
  that was **active when the DML was performed** (trigger queued), not the role at `COMMIT`.
  - **Impact**: Generated trigger test scenarios must NOT rely on `SET ROLE` between DML and commit
    to affect which security context the trigger body runs in.
  - Stored `SECURITY DEFINER` triggers are unaffected (they always run as the owner).
  - **Compare rule**: No DDL model change needed; behavioral note only.

### 6.3 Trigger Compare Rules

1. Triggers are identified by `(TableName, Name)`. Trigger names must be unique per table.
2. Compare `Definition` strings after normalization (whitespace, keyword case).
3. `DEFERRABLE` + `INITIALLY DEFERRED` are separate boolean states to compare.
4. `WHEN` clause changes must be flagged as diff.

---

## 7. Types (`PgType`)

### 7.1 Supported Type Kinds

| `PgTypeKind` | DDL | Notes |
|--------------|-----|-------|
| `Domain` | `CREATE DOMAIN` | Subtype of an existing type with constraints |
| `Enum` | `CREATE TYPE AS ENUM` | Ordered set of labels |
| `Composite` | `CREATE TYPE AS (...)` | Row type with named attributes |

*Range types and base types are not yet modeled. Document as a known gap.*

### 7.2 Version-by-Version Type Changes

#### PG 15
- **`MERGE` can use domain columns** — no DDL impact.
- **`multirange` types**: Each range type automatically gets a corresponding multirange type
  (introduced PG 14). No DDL model change but be aware when scanning `pg_type`.

#### PG 16
- No breaking changes to type DDL.

#### PG 17
- No breaking changes to type DDL.

#### PG 18
- No breaking changes to type DDL.

### 7.3 Type Compare Rules

1. `PgTypeKind` changes (e.g., enum → composite) require drop + recreate.
2. **Enum labels**: Adding labels is non-destructive (`ALTER TYPE ... ADD VALUE`). Removing or
   reordering labels requires drop + recreate. Flag label removal as destructive.
3. **Domain constraints**: Compare `CHECK` expression strings after normalization.
4. **Composite attributes**: Compare by attribute name and type; order matters for catalog identity
   but `ALTER TYPE ... ALTER ATTRIBUTE` can change types.

---

## 8. Schemas (`PgSchema`)

### 8.1 Version-Specific Schema Rules

See `PG_ROLES_PERMISSIONS_SECURITY.md` §2.1 for `public` schema ownership and privilege changes.

#### All Versions (PG 15+)
- Schemas hold tables, views, functions, sequences, types, and triggers.
- Schema `USAGE` privilege = "can see / use objects within".
- Schema `CREATE` privilege = "can create new objects within".

#### PG 15 (BREAKING)
- `CREATE` on `public` revoked from `PUBLIC` by default.
- `public` schema owner is `pg_database_owner` (not `postgres`).

#### PG 16
- No breaking schema DDL changes.

#### PG 17
- No breaking schema DDL changes.

#### PG 18
- No breaking schema DDL changes.

### 8.2 Schema Compare Rules

1. `public` schema ownership: `pg_database_owner` ≡ the actual DB owner — no diff.
2. `USAGE` and `CREATE` schema privileges are tracked independently.
3. Schema owner changes are flagged as diff (except `public` → `pg_database_owner` equivalence).

---

## 9. ACL / Privilege Abbreviations (PG 15–18)

These abbreviations appear in `pg_class.relacl`, `pg_namespace.nspacl`, etc.
Use these when parsing ACL strings from system catalogs.

| Abbreviation | Privilege | Object Types |
|---|---|---|
| `r` | `SELECT` | TABLE, VIEW, SEQUENCE, LARGE OBJECT |
| `a` | `INSERT` | TABLE |
| `w` | `UPDATE` | TABLE, SEQUENCE, LARGE OBJECT |
| `d` | `DELETE` | TABLE |
| `D` | `TRUNCATE` | TABLE |
| `x` | `REFERENCES` | TABLE |
| `t` | `TRIGGER` | TABLE |
| `m` | `MAINTAIN` | TABLE (PG 17+) |
| `X` | `EXECUTE` | FUNCTION, PROCEDURE |
| `U` | `USAGE` | SCHEMA, SEQUENCE, TYPE, FDW, SERVER, LANGUAGE |
| `C` | `CREATE` | DATABASE, SCHEMA, TABLESPACE |
| `c` | `CONNECT` | DATABASE |
| `T` | `TEMPORARY` | DATABASE |
| `s` | `SET` | PARAMETER (GUC) |
| `A` | `ALTER SYSTEM` | PARAMETER (GUC) |

> **`m` (MAINTAIN) is PG 17+ only.** Do not parse or emit this abbreviation when working with
> PG 15–16 servers.

---

## 10. Version Support Matrix — Database Objects

| Feature / Behavior | PG 15 | PG 16 | PG 17 | PG 18 |
|---|---|---|---|---|
| `NULLS NOT DISTINCT` on UNIQUE / index | ✅ | ✅ | ✅ | ✅ |
| Named `NOT NULL` constraints | ❌ | ✅ | ✅ | ✅ |
| `NOT NULL ... NO INHERIT` | ❌ | ✅ | ✅ | ✅ |
| `VIRTUAL` generated columns | ❌ | ❌ | ✅ | ✅ |
| `SECURITY_INVOKER` views | ✅ | ✅ | ✅ | ✅ |
| `MAINTAIN` privilege on table | ❌ | ❌ | ✅ | ✅ |
| `INCLUDE` on BRIN indexes | ❌ | ❌ | ✅ | ✅ |
| `ALTER DEFAULT PRIVILEGES ON ROUTINES` (reliable) | ❌ | ❌ | ✅ | ✅ |
| `ALTER DEFAULT PRIVILEGES ON LARGE OBJECTS` | ❌ | ❌ | ❌ | ✅ |
| Sequence `AS <type>` (data type) | ❌ | ✅ | ✅ | ✅ |
| AFTER trigger runs as enqueueing role | ❌ | ❌ | ❌ | ✅ |
| `WITHOUT OVERLAPS` on exclusion constraints | ❌ | ❌ | ❌ | ✅ |
| `search_path` enforcement for expression indexes | ❌ | ❌ | ⚠️ partial | ✅ |

---

## 11. Catalog Queries for Extraction

### Tables
```sql
SELECT
    c.relname AS name,
    n.nspname AS schema,
    r.rolname AS owner,
    c.relrowsecurity AS row_level_security,
    c.relforcerowsecurity AS force_row_level_security,
    c.reltablespace,
    c.relkind,          -- 'r'=table, 'p'=partitioned table
    c.relispartition,
    pg_get_partkeydef(c.oid) AS partition_key
FROM pg_class c
JOIN pg_namespace n ON n.oid = c.relnamespace
JOIN pg_roles r ON r.oid = c.relowner
WHERE c.relkind IN ('r', 'p')
  AND n.nspname NOT IN ('pg_catalog', 'information_schema', 'pg_toast')
ORDER BY n.nspname, c.relname;
```

### Columns (including generated columns)
```sql
SELECT
    a.attname AS name,
    pg_catalog.format_type(a.atttypid, a.atttypmod) AS data_type,
    a.attnotnull,
    a.attnum AS position,
    a.attidentity,        -- 'a'=always, 'd'=by_default, ''=not identity
    a.attgenerated,       -- 's'=stored, 'v'=virtual (PG 17+), ''=not generated
    pg_get_expr(d.adbin, d.adrelid) AS default_expression,
    pg_get_expr(a.attcompression, a.attrelid) AS compression_method,
    col_description(a.attrelid, a.attnum) AS comment
FROM pg_attribute a
JOIN pg_class c ON c.oid = a.attrelid
LEFT JOIN pg_attrdef d ON d.adrelid = a.attrelid AND d.adnum = a.attnum
WHERE a.attnum > 0 AND NOT a.attisdropped
ORDER BY a.attnum;
```

> **Note**: `attgenerated = 'v'` (virtual) is only present on PG 17+. On PG 15/16, only `'s'`
> (stored) exists.

### Sequences (PG 16+: includes data_type)
```sql
SELECT
    s.sequencename,
    s.schemaname,
    s.sequenceowner,
    s.data_type,      -- PG 16+; column absent on PG 15
    s.start_value,
    s.min_value,
    s.max_value,
    s.increment_by,
    s.cycle,
    s.cache_size
FROM pg_sequences s
ORDER BY s.schemaname, s.sequencename;
```

### Indexes
```sql
SELECT
    i.relname AS index_name,
    ix.indisunique,
    ix.indisprimary,
    ix.indnullsnotdistinct,   -- PG 15+
    pg_get_indexdef(ix.indexrelid) AS definition,
    t.relname AS table_name,
    n.nspname AS schema
FROM pg_index ix
JOIN pg_class i ON i.oid = ix.indexrelid
JOIN pg_class t ON t.oid = ix.indrelid
JOIN pg_namespace n ON n.oid = t.relnamespace
WHERE n.nspname NOT IN ('pg_catalog', 'information_schema', 'pg_toast')
  AND NOT ix.indisprimary    -- Primary keys handled via constraints
ORDER BY n.nspname, t.relname, i.relname;
```

---

*Last updated: reviewed against PostgreSQL 15.17, 16.13, 17.9, 18.3 official documentation (February 2026).*
*See also: [`PG_ROLES_PERMISSIONS_SECURITY.md`](PG_ROLES_PERMISSIONS_SECURITY.md)*
