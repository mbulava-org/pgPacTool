# PostgreSQL Expert Skill for pgPacTool

## Purpose

This skill makes repository-aware Copilot agents behave like PostgreSQL experts when working in
pgPacTool. Use it whenever modifying extraction, model definitions, compare logic, publish/script
generation, SQL project compilation, or documentation related to PostgreSQL database objects.

## When To Use This Skill

Use this skill when work involves any of the following:

- PostgreSQL versions 15, 16, 17, or 18
- Roles, memberships, grants, default privileges, or RLS
- Schemas, tables, columns, constraints, indexes, views, functions, procedures, sequences,
  triggers, types, extensions, or ownership
- Compare logic, deployment report logic, or publish/script generation
- Extraction queries against PostgreSQL catalogs
- Documentation or tests for PostgreSQL object/version behavior

## Repository Defaults

- **Product target**: PostgreSQL database-as-code tooling
- **Current implementation baseline**: PostgreSQL 16 and 17 are the primary supported versions
- **Version reference coverage**: PG 15–18 rules are documented for compatibility and future work
- **Default parser version in examples**: PostgreSQL 16
- **Do not imply PG 14 support**

## Core Operating Rules

1. **Check version gates before changing code.**
   - PostgreSQL features are not safely portable across major versions.
   - Do not assume syntax or catalog shape is unchanged.

2. **Use repo references as the source of truth.**
   - Roles/security: `docs/version-differences/PG_ROLES_PERMISSIONS_SECURITY.md`
   - Database objects: `docs/version-differences/PG_DATABASE_OBJECTS.md`
   - Multi-version strategy: `docs/features/multi-version-support/VERSION_COMPATIBILITY_STRATEGY.md`

3. **Never generate DDL for built-in or reserved roles.**
   - Skip all `pg_*` roles and Azure reserved roles such as `azure_pg_admin`, `azuresu`,
     `replication`, and `localadmin`.

4. **Preserve semantic version differences.**
   - Examples:
     - `STORED` vs `VIRTUAL` generated columns are different.
     - `NULLS DISTINCT` vs `NULLS NOT DISTINCT` are different.
     - `SECURITY INVOKER` vs default view security are different.
     - `FUNCTION name(args...)` identity is signature-based, not name-only.

5. **Prefer explicit warnings over silent coercion.**
   - If a source feature does not exist on a target version, warn or fail.
   - Do not silently downgrade PG 17+/18 features to PG 15/16 behavior.

6. **Keep security-sensitive behavior explicit.**
   - Never emit plaintext passwords in DDL.
   - Warn if `SECURITY DEFINER` functions lack `SET search_path`.
   - Treat RLS/table security flags as first-class compare properties.

7. **Back rule changes with tests and documentation.**
   - If behavior changes, update docs and add or adjust tests.

## Version-Gated Quick Rules

### Roles / Security
- `BYPASSRLS` requires superuser on PG 15; allowed for non-superuser admins on PG 16+
- `WITH INHERIT` / `WITH SET` role membership options are PG 16+
- `pg_create_subscription` is PG 16+
- `pg_maintain` is PG 17+
- `pg_use_reserved_connections` is PG 17+
- `pg_signal_autovacuum_worker` is PG 18+
- `ALTER DEFAULT PRIVILEGES ... ON LARGE OBJECTS` is PG 18+

### Database Objects
- `NULLS NOT DISTINCT` on unique constraints/indexes is PG 15+
- Named `NOT NULL` constraints and `NO INHERIT` constraints are PG 16+
- Sequence `AS <type>` is PG 16+
- `VIRTUAL` generated columns are PG 17+
- `MAINTAIN` table privilege is PG 17+
- `INCLUDE` on BRIN indexes is PG 17+
- `ALTER DEFAULT PRIVILEGES ON ROUTINES` is reliable on PG 17+
- `WITHOUT OVERLAPS` is PG 18+
- PG 18 changes AFTER trigger execution role behavior

## Expected Implementation Behavior

### When editing models
- Add explicit properties for version-sensitive semantics rather than burying them in `Definition`
  when structured comparison is needed.
- Examples:
  - `PgView.SecurityInvoker`
  - `PgSequence.DataType`
  - `PgColumn.GeneratedColumnKind`
  - Role membership option objects for PG 16+

### When editing compare logic
- Normalize whitespace and non-semantic formatting only.
- Do **not** normalize away semantic differences.
- Treat object identity correctly:
  - functions/procedures: signature-based
  - triggers: table + trigger name
  - constraints: object-local identity

### When editing script generation
- Emit version-correct syntax only.
- Use ordered DDL emission for roles and privileges.
- Fail or warn on unsupported target-version features.

### When editing extraction queries
- Prefer catalog columns that explicitly expose semantics.
- Gate catalog usage by version when columns differ across versions.
- Example: sequence `data_type` from `pg_sequences` is PG 16+

## Required Documentation Updates

If implementation changes affect PostgreSQL behavior, also update:

- `.github/copilot-instructions.md` for concise durable rules
- relevant `docs/version-differences/*.md` references
- `README.md` when user-visible capabilities or positioning changes

## Required Test Mindset

Add or update tests for:

- version-gated syntax/behavior
- compare semantics
- publish/script generation
- extraction/catalog parsing
- built-in-role exclusion
- destructive diff warnings when applicable

## Do Not Do

- Do not claim support for PostgreSQL 14
- Do not generate `CREATE ROLE` for `pg_*` or Azure reserved roles
- Do not strip semantic clauses just to make diffs look smaller
- Do not assume PostgreSQL major versions are interchangeable
- Do not update behavior without updating the reference docs

## Skill References

- `docs/version-differences/PG_ROLES_PERMISSIONS_SECURITY.md`
- `docs/version-differences/PG_DATABASE_OBJECTS.md`
- `docs/features/multi-version-support/VERSION_COMPATIBILITY_STRATEGY.md`
- `.github/copilot-instructions.md`
