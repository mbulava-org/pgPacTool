# Documentation Index

See [Multi-Version Support](features/multi-version-support/README.md) for multi-version PostgreSQL support documentation.

## Embedded Repository Skill

- [PostgreSQL Expert Skill](../.github/skills/postgresql-expert/README.md) — repository-embedded
  guidance for PostgreSQL-aware Copilot work on version gates, object semantics, compare logic,
  extraction, and publish/script generation.
- [Compare and Deploy Expert Skill](../.github/skills/compare-and-deploy-expert/README.md) —
  guidance for schema comparison, publish orchestration, deployment scripts, ownership policy,
  and destructive-change handling.
- [PostgreSQL Catalog Extraction Expert Skill](../.github/skills/postgresql-catalog-extraction-expert/README.md) —
  guidance for catalog queries, extraction completeness, ACL parsing, and model mapping.
- [MSBuild SDK Expert Skill](../.github/skills/msbuild-sdk-expert/README.md) — guidance for SDK
  props/targets, packaged CLI-host builds, restore/load behavior, and incremental build semantics.
- [CLI UX and Diagnostics Expert Skill](../.github/skills/cli-ux-and-diagnostics-expert/README.md) —
  guidance for command UX, verbosity, troubleshooting output, and exit/error behavior.
- [Native libpg_query Integration Expert Skill](../.github/skills/native-libpg-query-integration-expert/README.md) —
  guidance for versioned native loading, runtime asset layout, diagnostics, and cross-platform behavior.
- [Test Matrix Expert Skill](../.github/skills/test-matrix-expert/README.md) — guidance for choosing
  the right validation layer across unit, integration, package, and Linux/container tests.
- [Database Project Layout Expert Skill](../.github/skills/database-project-layout-expert/README.md) —
  guidance for generated SDK-style project folder conventions, one-object-per-file layout, and
  security artifact organization.
- [Managed PostgreSQL Platforms Expert Skill](../.github/skills/managed-postgresql-platforms-expert/README.md) —
  guidance for Azure Database for PostgreSQL, Amazon RDS for PostgreSQL, and Aurora-specific
  PostgreSQL behavior and limitations.
- [NuGet Packaging and Release Expert Skill](../.github/skills/nuget-packaging-and-release-expert/README.md) —
  guidance for package structure, versioning, native assets, tool/SDK packaging, and release discipline.
- [Repository Documentation Maintainer Skill](../.github/skills/repo-doc-maintainer/README.md) —
  guidance for keeping README, docs indexes, and durable documentation in sync with behavior.
- [Performance and Memory Expert Skill](../.github/skills/performance-and-memory-expert/README.md) —
  guidance for performance-sensitive and allocation-sensitive repo work.
- [Breaking Change Reviewer Skill](../.github/skills/breaking-change-reviewer/README.md) —
  guidance for compatibility-sensitive changes across code, docs, tests, packages, and platforms.
- [Embedded Skills Planning](features/embedded-skills/README.md) — skill inventory, template,
  repository surface map, first-tranche roadmap, and analyzer planning.

## Version References

- [Roles, Permissions & Security (PG 15–18)](version-differences/PG_ROLES_PERMISSIONS_SECURITY.md)
- [Database Objects (PG 15–18)](version-differences/PG_DATABASE_OBJECTS.md)

## Supported Versions
- PostgreSQL 16 (default)
- PostgreSQL 17

Older versions (14, 15) may be added if needed.

## Architecture Guidance

- Prefer small focused services over large multi-responsibility classes.
- Use partial classes only for organization, not as the primary way to manage business logic complexity.
- Target class sizes under 300 lines when practical, with 500 lines as a hard warning threshold.
- Target methods under 30 lines when practical, with 60 lines as a hard warning threshold.
- Keep orchestration thin and move ownership policy, publish context handling, and CLI formatting into dedicated collaborators when behavior grows.