# Embedded Repository Skills

This folder tracks the repository-embedded skill system for pgPacTool.

## Purpose

Embedded skills give Copilot agents durable, repo-specific guidance for PostgreSQL-aware work,
MSBuild SDK behavior, deployment semantics, extraction correctness, cloud-managed PostgreSQL
platform differences, and future analyzer-backed validation.

## Initial Goals

- Standardize how skills are authored and referenced
- Prioritize the highest-value skills first
- Connect each skill to its source-of-truth docs, code surfaces, and test areas
- Build toward analyzer and code-fix components that can detect common mistakes automatically

## Documents

- [Skill Inventory](SKILL_INVENTORY.md)
- [Skill Template](SKILL_TEMPLATE.md)
- [Repository Surface Map](REPO_SURFACE_MAP.md)
- [Execution Roadmap](EXECUTION_ROADMAP.md)
- [Compare and Deploy Research](COMPARE_AND_DEPLOY_RESEARCH.md)
- [Catalog Extraction Research](CATALOG_EXTRACTION_RESEARCH.md)
- [MSBuild SDK Research](MSBUILD_SDK_RESEARCH.md)
- [CLI UX and Diagnostics Research](CLI_UX_AND_DIAGNOSTICS_RESEARCH.md)
- [Native libpg_query Research](NATIVE_LIBPG_QUERY_RESEARCH.md)
- [Test Matrix Research](TEST_MATRIX_RESEARCH.md)
- [Database Project Layout Research](DATABASE_PROJECT_LAYOUT_RESEARCH.md)
- [Analyzer Roadmap](ANALYZER_ROADMAP.md)
- [Analyzer Architecture](ANALYZER_ARCHITECTURE.md)
- [Adoption Checklist](ADOPTION_CHECKLIST.md)

## Planned Skill Areas

- PostgreSQL object and version semantics
- Compare and deploy behavior
- PostgreSQL catalog extraction
- MSBuild SDK authoring and diagnostics
- CLI UX and diagnostics
- Native libpg_query integration
- Test matrix design
- Database project layout
- Managed PostgreSQL platforms (Azure, Amazon RDS, Aurora)
- NuGet packaging and release discipline
- Repository documentation maintenance
- Performance and memory guidance
- Breaking change review
- Analyzer and code-fix roadmap
