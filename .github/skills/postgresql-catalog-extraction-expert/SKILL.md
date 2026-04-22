# PostgreSQL Catalog Extraction Expert Skill

## Purpose

Use this skill when modifying PostgreSQL catalog extraction, model population, version-gated
catalog behavior, ACL parsing, or extraction correctness in pgPacTool.

## When To Use This Skill

Use this skill when changing any of the following:
- `src/libs/mbulava.PostgreSql.Dac/Extract/`
- `src/libs/mbulava.PostgreSql.Dac/Models/DbObjects.cs`
- extraction tests under `tests/mbulava.PostgreSql.Dac.Tests/Extract/`
- `tests/ProjectExtract-Tests/`
- extraction completeness docs and version-reference docs

## Repository Defaults

- Live extractor currently enforces PostgreSQL 16+.
- PG 15–18 docs exist as reference coverage, not identical implementation support.
- Extraction is schema-by-schema.
- Roles are discovered recursively from extracted owners and memberships.
- Some object extraction paths tolerate AST parse failure; others hard-fail.
- Privilege extraction currently uses multiple ACL parsing paths.

## Core Operating Rules

1. **Prefer structured catalog fields over reconstructed SQL when semantics matter.**
   - Use reconstructed SQL as a convenience representation, not the only source of truth.

2. **Gate extraction by PostgreSQL version when catalog shape differs.**
   - Do not query version-specific columns unconditionally.
   - Keep implementation support distinct from forward-looking documentation coverage.

3. **Preserve semantics in models, not only in `Definition` strings.**
   - When a property changes compare/publish behavior, extract it structurally.

4. **Unify ACL parsing behavior.**
   - Avoid object-type-specific privilege parsers drifting apart.
   - Preserve grant option behavior consistently.

5. **Treat extraction completeness as correctness, not formatting.**
   - Missing identity/generated/security metadata is a correctness gap.

6. **Be explicit about cloud-managed platform constraints.**
   - Reserved roles and restricted catalogs on Azure, Amazon RDS, or Aurora may affect extraction.

## Expected Implementation Behavior

### When editing extraction queries
- Prefer `pg_catalog` columns and official helper functions that preserve semantics.
- Add version gates when catalog shape changes between PG 16, 17, and 18.
- Avoid flattening modern PostgreSQL features into plain strings when a model property is needed.

### When editing model mapping
- Add explicit properties for version-sensitive semantics.
- Preserve object identity accurately:
  - functions/procedures must preserve signature identity
  - generated columns should preserve stored vs virtual
  - sequences should preserve PG 16+ data type
  - views should preserve security-invoker semantics

### When editing privilege extraction
- Keep `PUBLIC` normalization consistent.
- Preserve grant-option semantics.
- Add support for newer privilege codes such as `MAINTAIN` where appropriate.

### When editing version validation
- Be precise about the difference between:
  - what the code currently supports
  - what the docs describe for future or reference coverage

## Required Documentation Updates

If extraction behavior changes, update:
- `docs/features/embedded-skills/CATALOG_EXTRACTION_RESEARCH.md`
- `docs/version-differences/PG_ROLES_PERMISSIONS_SECURITY.md`
- `docs/version-differences/PG_DATABASE_OBJECTS.md`
- `EXTRACTION_COMPLETENESS_AUDIT.md` when relevant
- `.github/copilot-instructions.md` if durable extraction rules change

## Required Test Mindset

Add or update tests for:
- version-gated catalog behavior
- PG 16/17/18 feature extraction
- ACL parsing consistency
- structured extraction of generated/identity/security semantics
- function/procedure signature handling
- role extraction completeness
- platform-specific extraction constraints where relevant

## Do Not Do

- Do not assume PG 15 extraction is implemented just because PG 15 docs exist.
- Do not keep duplicate ACL parsers drifting apart.
- Do not hardcode provider-specific assumptions into generic extractor code.
- Do not reduce structured semantics to raw SQL text when compare/publish needs the structure.
- Do not ignore partitioned or version-specific object kinds silently.

## Skill References

- `docs/features/embedded-skills/CATALOG_EXTRACTION_RESEARCH.md`
- `docs/version-differences/PG_ROLES_PERMISSIONS_SECURITY.md`
- `docs/version-differences/PG_DATABASE_OBJECTS.md`
- `EXTRACTION_COMPLETENESS_AUDIT.md`
- `src/libs/mbulava.PostgreSql.Dac/Extract/PgProjectExtractor.cs`
- `src/libs/mbulava.PostgreSql.Dac/Extract/PostgreSqlVersionChecker.cs`
- `src/libs/mbulava.PostgreSql.Dac/Models/DbObjects.cs`
