# Managed PostgreSQL Platforms Expert Skill

## Purpose

Use this skill when modifying behavior that depends on managed PostgreSQL platform constraints,
including Azure Database for PostgreSQL, Amazon RDS for PostgreSQL, and Amazon Aurora
PostgreSQL-Compatible Edition.

## When To Use This Skill

Use this skill when changing:
- reserved/admin role handling
- ownership assumptions in extract/compare/publish
- extension assumptions
- deployment scripts that may require superuser-like capabilities
- docs that discuss platform portability
- analyzer/adoption rules for cloud-specific behavior

## Repository Defaults

- The repo must not be Azure-only in its platform assumptions.
- Managed-platform guidance should consider:
  - Azure Database for PostgreSQL
  - Amazon RDS for PostgreSQL
  - Amazon Aurora PostgreSQL-Compatible Edition
- Platform guidance currently exists partly inside role/security and object reference docs.
- Provider-specific caveats should be documented when behavior is not portable.

## Core Operating Rules

1. **Do not assume self-hosted PostgreSQL privileges on managed services.**
2. **Treat reserved/admin roles as platform-managed and non-user-owned.**
3. **Document provider-specific caveats when deployment behavior is not portable.**
4. **Check role, ownership, extension, and maintenance assumptions across Azure, RDS, and Aurora.**
5. **Prefer portable defaults when cross-cloud behavior differs.**
6. **Avoid encoding Azure-only behavior into generic PostgreSQL logic.**

## Platform-Gated Quick Rules

- Azure has reserved roles such as `azure_pg_admin`, `azuresu`, `replication`, and `localadmin`.
- Azure-specific ownership behavior for `public` schema must not be generalized to all platforms.
- Provider-managed environments may restrict superuser-only operations, extension availability,
  replication capabilities, or grant behavior.
- When provider behavior differs, emit a warning or document the limitation rather than assuming
  portability.

## Expected Implementation Behavior

### When editing role/security logic
- Check whether role creation, grants, or ownership assumptions differ by platform.
- Keep built-in/reserved roles excluded from user-managed DDL.

### When editing compare/publish logic
- Avoid scripts that require unsupported admin/superuser capabilities unless explicitly gated.
- Treat platform-specific failures as diagnosable and documentable, not mysterious runtime errors.

### When editing docs
- Be explicit about whether guidance is generic PostgreSQL, Azure-specific, RDS-specific, or Aurora-specific.

## Required Documentation Updates

If managed-platform behavior changes, update:
- `docs/version-differences/PG_ROLES_PERMISSIONS_SECURITY.md`
- `docs/version-differences/PG_DATABASE_OBJECTS.md` when relevant
- `docs/features/embedded-skills/EXECUTION_ROADMAP.md` if the durable platform scope changes
- `.github/copilot-instructions.md` if durable platform rules change

## Required Test Mindset

Add or update tests for:
- reserved/admin role handling
- provider-specific unsupported operations
- docs/analyzers that guard against Azure-only assumptions in generic code

## Do Not Do

- Do not assume Azure rules apply to RDS or Aurora.
- Do not assume RDS/Aurora permit self-hosted superuser workflows.
- Do not hide platform-specific limitations from docs or diagnostics.
- Do not hardcode provider-specific behavior into generic code without gating.

## Skill References

- `docs/version-differences/PG_ROLES_PERMISSIONS_SECURITY.md`
- `docs/version-differences/PG_DATABASE_OBJECTS.md`
- `docs/features/embedded-skills/EXECUTION_ROADMAP.md`
- `.github/skills/postgresql-expert/SKILL.md`
