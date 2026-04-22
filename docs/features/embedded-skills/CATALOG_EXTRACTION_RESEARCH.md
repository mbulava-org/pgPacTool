# Phase 1 / Step B — Research: `postgresql-catalog-extraction-expert`

## Purpose

This research document captures the current PostgreSQL catalog extraction behavior in pgPacTool so
`postgresql-catalog-extraction-expert` can be authored from actual extractor behavior, tests,
model mapping, and known gaps.

## Primary Code Surfaces Reviewed

### Extraction pipeline
- `src/libs/mbulava.PostgreSql.Dac/Extract/PgProjectExtractor.cs`
- `src/libs/mbulava.PostgreSql.Dac/Extract/PostgreSqlVersionChecker.cs`

### Model mapping targets
- `src/libs/mbulava.PostgreSql.Dac/Models/DbObjects.cs`

### Tests reviewed
- `tests/mbulava.PostgreSql.Dac.Tests/Extract/PgProjectExtractorVersionTests.cs`
- `tests/mbulava.PostgreSql.Dac.Tests/Extract/PostgreSqlVersionCheckerTests.cs`
- `tests/ProjectExtract-Tests/Privileges/`
- `tests/ProjectExtract-Tests/SimplePrivilegeTest.cs`

### Related gap analysis
- `EXTRACTION_COMPLETENESS_AUDIT.md`

---

## Current Extraction Pipeline

`PgProjectExtractor.ExtractPgProject(...)` currently follows this flow:

1. validate the target database exists
2. validate the PostgreSQL version using `PostgreSqlVersionChecker`
3. sanitize the source connection string
4. extract database owner
5. initialize `PgProject`
6. extract schemas
7. for each schema extract:
   - tables
   - views
   - functions/procedures
   - types
   - sequences
   - triggers
8. derive roles recursively from extracted object owners and memberships

This is the current extractor orchestration behavior.

---

## Version Gate Behavior Observed

### Hard minimum version currently enforced in code
`PostgreSqlVersionChecker.MinimumSupportedVersion = 16`

Current durable behavior:
- PostgreSQL 16 => supported
- PostgreSQL 17 => supported
- PostgreSQL 15 => rejected
- PostgreSQL 14 => rejected
- forward-compatible major versions >= 16 are accepted

Tests confirm this behavior.

### Important mismatch with newer docs
The repository now contains version-reference docs for PG 15–18 and future skill planning uses
that range, but the live extractor still enforces PostgreSQL 16+ only.

Skill implication:
- extraction guidance must distinguish **current implementation support** from **reference coverage**
- agents must not silently assume PG 15 extraction works today just because docs discuss PG 15

---

## Current Catalog Extraction Behavior by Object Type

### 1. Database project root
Extracted into `PgProject`:
- `DatabaseName`
- `PostgresVersion` (major version only)
- `SourceConnection` (sanitized)
- `DefaultOwner`
- `DefaultTablespace = "pg_default"`

### 2. Schemas
Catalogs used:
- `pg_namespace`
- `pg_roles`
- `nspacl`

Current behavior:
- excludes `pg_%` schemas and `information_schema`
- builds synthetic `CREATE SCHEMA ... AUTHORIZATION ...;`
- parses schema DDL into AST where possible
- extracts schema privileges from `nspacl`

Durable notes:
- schema extraction is tolerant of AST parse failure and can continue without AST

### 3. Tables
Catalogs used:
- `pg_class`
- `pg_namespace`
- `pg_roles`
- helper queries against `pg_attribute`, `pg_attrdef`, `pg_constraint`, `pg_index`

Current behavior:
- only `relkind = 'r'` is extracted as a table
- builds synthetic `CREATE TABLE` SQL using columns only
- extracts privileges from `relacl`
- populates columns, constraints, indexes

Important implications:
- partitioned tables (`relkind = 'p'`) are not included here
- table-level metadata such as tablespace, RLS, fillfactor, inheritance, partitioning,
  identity/generated-column semantics are not currently modeled by extractor helper queries
- `BuildCreateTableSqlAsync` only emits column definitions, `NOT NULL`, and defaults

### 4. Columns
Catalogs used:
- `pg_attribute`
- `pg_attrdef`
- `col_description`

Currently extracted:
- name
- formatted data type
- `NOT NULL`
- default expression
- comment

Not currently extracted in this helper:
- ordinal `Position`
- identity metadata
- generated-column metadata
- collation
- compression/storage
- version-specific generated column kind (`stored` / `virtual`)

### 5. Constraints
Catalog used:
- `pg_constraint`

Currently extracted:
- name
- `contype`
- `pg_get_constraintdef(oid)`
- mapped constraint type
- FK referenced table/columns by parsing textual definition
- CHECK expression as raw definition for check constraints

Important implications:
- extraction relies on text parsing for FK details rather than structured catalog joins
- `NOT NULL` constraints are mapped when present in catalogs
- more advanced metadata such as `NO INHERIT`, named not-null semantics, and PG 18
  `WITHOUT OVERLAPS` are not explicitly modeled here

### 6. Indexes
Catalogs used:
- `pg_index`
- `pg_class`
- `pg_roles`
- `pg_get_indexdef()`

Current behavior:
- indexes are looked up by `indrelid`
- definition text comes from `pg_get_indexdef()`
- AST parse is required; parse failure throws
- owner is stored

Not currently modeled structurally:
- `NULLS NOT DISTINCT`
- INCLUDE columns
- expression vs simple columns
- predicate/partial-index shape
- access-method-specific semantics

### 7. Views / Materialized Views
Catalogs used:
- `pg_class`
- `pg_namespace`
- `pg_roles`
- `pg_get_viewdef()`
- `relacl`

Current behavior:
- extracts `relkind in ('v','m')`
- reconstructs `CREATE VIEW` / `CREATE MATERIALIZED VIEW` for AST parsing
- stores `pg_get_viewdef()` output as the definition text
- sets `IsMaterialized`
- extracts privileges from `relacl`
- dependencies are currently TODO and not extracted from `pg_depend`

Important implications:
- `SecurityInvoker` is not currently extracted as a structured property
- dependence on reconstructed SQL can hide version-specific clauses unless explicitly added

### 8. Functions / Procedures
Catalogs used:
- `pg_proc`
- `pg_namespace`
- `pg_roles`
- `pg_get_functiondef()`

Current behavior:
- extracts `prokind in ('f','p')`
- excludes aggregates and window functions
- stores full definition from `pg_get_functiondef()`
- attempts AST parse but continues on failure
- function/procedure privileges are TODO and currently not extracted

Important implications:
- object identity is effectively name-only later in compare logic
- argument-signature identity is not captured structurally here
- privilege extraction gap is explicitly acknowledged in code

### 9. Types
Catalogs used:
- `pg_type`
- `pg_namespace`
- `pg_roles`
- `pg_class`
- `pg_constraint`
- `pg_enum`
- `pg_attribute`

Current behavior:
- extracts domain, enum, and composite types
- reconstructs synthetic DDL for each type kind
- parses AST from reconstructed SQL
- domain extraction captures base type, `NOT NULL`, and one constraint definition
- enum extraction captures ordered labels
- composite extraction captures attributes using `format_type`

Important implications:
- range/base types are not covered
- domain extraction appears to take only one constraint row in the current read pattern
- privileges are not visibly extracted here

### 10. Sequences
Catalogs used:
- `pg_class`
- `pg_namespace`
- `pg_sequence`
- `relacl`

Current behavior:
- extracts start/increment/min/max/cache/cycle
- reconstructs `CREATE SEQUENCE` SQL via helper
- populates `SeqOption` list
- parses sequence AST if possible
- parses privileges from ACL

Important implications:
- sequence `data_type` (PG 16+) is not currently extracted
- no version-gated handling yet for PG 16+ `AS <type>`

### 11. Triggers
Catalogs used:
- `pg_trigger`
- `pg_class`
- `pg_namespace`
- `pg_get_triggerdef()`

Current behavior:
- excludes internal triggers
- stores trigger definition
- attempts AST parse but continues on failure
- hardcodes trigger owner as `postgres` with comment "Triggers inherit table owner"

Important implications:
- trigger owner handling is synthetic and not platform-aware
- this should be treated carefully in downstream ownership logic

### 12. Roles
Catalogs used:
- `pg_roles`
- `pg_auth_members`

Current behavior:
- roles are discovered starting from extracted owners only
- recursive membership lookup expands the role set
- extracts:
  - name
  - superuser
  - login
  - inherit
  - replication
  - bypassrls
- builds synthetic `CREATE ROLE ... WITH ...` definition
- membership extraction populates `MemberOf`

Not currently extracted:
- `CREATEDB`
- `CREATEROLE`
- `CONNECTION LIMIT`
- `VALID UNTIL`
- comment
- built-in-role marker
- PG 16+ membership options (`ADMIN OPTION`, `INHERIT OPTION`, `SET OPTION`)

---

## ACL / Privilege Extraction Behavior

There are two ACL parsing paths:
1. `ExtractPrivilegesAsync(...)`
2. `ParseAcl(...)`

### `ExtractPrivilegesAsync(...)`
Current behavior:
- reads ACL array from a passed query
- normalizes empty grantee to `PUBLIC`
- attempts to interpret both table-style and schema-style grant-option encodings
- maps single-character privilege codes to names using `MapPrivilege`

### `ParseAcl(...)`
Current behavior:
- simpler parser used in sequence extraction
- treats uppercase letters as grantable
- does not apply the schema-style asterisk logic used in `ExtractPrivilegesAsync(...)`

Important implication:
- ACL parsing logic is duplicated and inconsistent
- a future skill should require a unified parser and version-aware privilege-code handling

### Current privilege mapping gaps
`MapPrivilege(...)` currently handles:
- table-ish codes: `r`, `w`, `a`, `d`, `D`, `x`, `t`
- schema/database-ish codes: `U`, `C`, `c`

Not currently covered explicitly:
- `m` => `MAINTAIN` (PG 17+)
- large object privileges
- parameter privileges (`s`, `A`)
- tablespace privilege semantics
- routine-specific nuance

---

## Current Durability / Resilience Behavior

### Tolerant extraction paths
Extraction continues without AST in some places:
- schema AST parse failure
- view AST parse failure
- function AST parse failure
- trigger AST parse failure
- sequence AST parse failure (with verbose warning)

### Hard-fail extraction paths
Extraction throws on some failures:
- invalid/reconstructed table SQL
- invalid/reconstructed index SQL
- AST deserialization failures for table/index
- version below minimum supported
- database missing
- connection failure during version validation or DB existence validation

Skill implication:
- extractor behavior is mixed between tolerant and strict modes depending on object type

---

## Main Gaps and Risks Found During Research

### 1. Implementation support and docs coverage are not aligned
Live extractor enforces PG 16+ only, while docs/skills now discuss PG 15–18 behavior.

Risk:
- agents may implement PG 15 assumptions against code that still rejects PG 15 entirely

### 2. Table extraction is incomplete for modern PostgreSQL semantics
Current extraction does not structurally capture:
- identity columns
- generated columns
- virtual generated columns (PG 17+)
- position
- collation
- fillfactor
- RLS flags
- inheritance
- partitioning
- tablespace

Risk:
- compare/publish may lose semantic detail
- extraction-generated project definitions can be incomplete

### 3. Partitioned tables are likely skipped
`ExtractTablesAsync` filters only `relkind = 'r'`.

Risk:
- partitioned-table metadata may be absent entirely
- downstream compare/publish will miss these objects

### 4. Function privileges are a known TODO
Code explicitly notes function/procedure privilege extraction is not implemented.

Risk:
- publish/compare semantics for routines are incomplete
- security diff accuracy is reduced

### 5. Role extraction is materially incomplete for modern rules
Current role extraction does not capture the fields needed by the role/security reference docs.

Risk:
- publish ownership and role diff behavior cannot be fully version-aware
- built-in role handling and PG 16+ membership semantics are incomplete

### 6. Sequence data type is not extracted
PG 16+ sequence `AS <type>` is not represented.

Risk:
- PG 16+ sequence semantics are flattened/lost

### 7. View security metadata is not extracted
`SecurityInvoker` is not captured as a structured property.

Risk:
- important permission semantics are lost in model compare

### 8. ACL parsing is duplicated and incomplete
There are two parsers with different grant-option handling.

Risk:
- inconsistent privilege extraction between object types
- PG 17+/18 privilege additions can be missed

### 9. Trigger owner is synthetic
Triggers are assigned owner `postgres` rather than being derived from owning table or skipped as
non-independent ownership.

Risk:
- compare/publish ownership semantics can become inaccurate
- cloud-managed platforms may make this worse

### 10. Dependencies are not fully extracted for views
Views set `Dependencies = new List<string>() // TODO`

Risk:
- extraction model loses dependency detail that could inform compare/deploy ordering or docs

### 11. Domain extraction may under-capture multiple constraints
The current domain query/reader flow appears to read a single constraint row.

Risk:
- domain definitions may be incomplete for multi-constraint domains

---

## Current Test Signals

### What tests confirm
- PostgreSQL version gating behavior
- PG 16 success / PG 17 forward compatibility
- PG 15/14 rejection
- connection/version parsing behavior

### What visible tests do not yet strongly confirm
- object-level extraction completeness for modern PG features
- PG 17/18 object-specific extraction semantics
- ACL parsing for `MAINTAIN`
- function/procedure privilege extraction
- sequence type extraction
- view security invoker extraction
- partitioned-table extraction
- managed platform differences (Azure/RDS/Aurora)

---

## Recommended Durable Rules for `postgresql-catalog-extraction-expert`

### Core rules
1. Prefer structured catalog fields over reverse-parsed SQL whenever semantics matter.
2. When SQL is reconstructed, treat it as a convenience representation, not the source of truth.
3. Gate extraction logic by PostgreSQL major version whenever catalog shape or semantics differ.
4. Preserve semantics in the model instead of flattening everything into `Definition` strings.
5. Use one consistent ACL parser for all object types.
6. Treat missing extraction of semantic properties as a correctness gap, not a formatting issue.
7. Keep implementation support separate from forward-looking documentation coverage.
8. Update tests and docs together when extraction adds or changes semantic fields.

### Specific rules this skill should encode
- PG 16+ sequence `data_type` must be extracted when supported.
- PG 17+ generated-column kind (`stored` vs `virtual`) must be extracted structurally.
- Function/procedure identity must preserve argument signature.
- Function/procedure privileges should be extracted from `proacl`.
- View `SecurityInvoker` should be extracted structurally.
- Partitioned tables must not be silently omitted from extraction.
- Role extraction must evolve to include the fields required by the role/security reference docs.
- Cloud-managed PostgreSQL variants must be treated as potential extraction-shape and permissions
  constraints, especially for role discovery and reserved-role behavior.

---

## Analyzer Opportunities from This Research

Future analyzer or validation ideas:
- detect duplicated ACL parsing implementations
- detect missing extraction of structured model properties when docs require them
- detect version-gated fields queried unconditionally
- detect compare logic using bare function names instead of signatures
- detect hardcoded trigger ownership assumptions

---

## What the Skill Should Reference

When authored, `postgresql-catalog-extraction-expert` should reference:
- `docs/version-differences/PG_ROLES_PERMISSIONS_SECURITY.md`
- `docs/version-differences/PG_DATABASE_OBJECTS.md`
- `docs/features/embedded-skills/CATALOG_EXTRACTION_RESEARCH.md`
- `src/libs/mbulava.PostgreSql.Dac/Extract/PgProjectExtractor.cs`
- `src/libs/mbulava.PostgreSql.Dac/Extract/PostgreSqlVersionChecker.cs`
- `src/libs/mbulava.PostgreSql.Dac/Models/DbObjects.cs`

---

## Follow-On Work After This Research

1. author `.github/skills/postgresql-catalog-extraction-expert/SKILL.md`
2. add a short skill `README.md`
3. link the skill from `.github/copilot-instructions.md` and `docs/README.md`
4. create follow-up issues or analyzer candidates for:
   - unified ACL parsing
   - PG 16+ sequence type extraction
   - PG 17+ generated column extraction
   - function/procedure privilege extraction
   - partitioned table extraction
   - structured view security extraction
   - complete role extraction for PG 16+/17+/18 rules

---

*Last updated: Current Session*
