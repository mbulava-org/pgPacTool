# Skill Inventory

This inventory groups the planned embedded skills by product area, value, and implementation order.

## Prioritization Principles

Skills should be prioritized by:
1. production risk reduction
2. correctness impact
3. frequency of agent interaction
4. repo-specific complexity
5. dependency on deeper reference docs

## Tranche 1 — Smallest High-Value Set

### 1. compare-and-deploy-expert
**Goal**: Guard compare semantics, deployment ordering, destructive changes, publish behavior,
script behavior, and deployment reports.

**Primary surfaces**:
- `src/libs/mbulava.PostgreSql.Dac/Compare/PgSchemaComparer.cs`
- `src/libs/mbulava.PostgreSql.Dac/Compare/PublishScriptGenerator.cs`
- `src/libs/mbulava.PostgreSql.Dac/Publish/ProjectPublisher.cs`
- `src/libs/mbulava.PostgreSql.Dac/Publish/PublishOwnershipPolicyService.cs`
- `src/libs/mbulava.PostgreSql.Dac/Publish/PublishTargetDatabaseContextService.cs`

**Primary risks**:
- semantic diff mistakes
- unsafe destructive deployment behavior
- ownership/privilege ordering mistakes
- publish vs script divergence

### 2. postgresql-catalog-extraction-expert
**Goal**: Protect extraction correctness from PostgreSQL catalogs into repository models.

**Primary surfaces**:
- `src/libs/mbulava.PostgreSql.Dac/Extract/PgProjectExtractor.cs`
- extraction helpers under `Extract/`
- `src/libs/mbulava.PostgreSql.Dac/Models/DbObjects.cs`

**Primary risks**:
- incorrect catalog assumptions
- version-gated column misuse
- lossy mapping into models
- platform-specific extraction gaps

### 3. msbuild-sdk-expert
**Goal**: Standardize SDK authoring, restore behavior, incremental build semantics, diagnostics,
package shape, and Visual Studio integration.

**Primary surfaces**:
- `src/sdk/MSBuild.Sdk.PostgreSql/Sdk/Sdk.props`
- `src/sdk/MSBuild.Sdk.PostgreSql/Sdk/Sdk.targets`
- `src/sdk/MSBuild.Sdk.PostgreSql/Tasks/CompilePgProject.cs`
- `src/sdk/MSBuild.Sdk.PostgreSql/README.md`

**Primary risks**:
- broken Visual Studio experience
- incorrect build inputs/outputs
- fragile restore behavior
- divergence from CLI compile behavior

## Tranche 2 — Platform and Tooling Depth

### 4. cli-ux-and-diagnostics-expert
### 5. native-libpg-query-integration-expert
### 6. test-matrix-expert
### 7. database-project-layout-expert

## Tranche 3 — Governance and Cross-Cloud Coverage

### 8. managed-postgresql-platforms-expert
**Scope**:
- Azure Database for PostgreSQL
- Amazon RDS for PostgreSQL
- Amazon Aurora PostgreSQL-Compatible Edition

**Focus**:
- reserved/admin roles
- blocked or restricted operations
- ownership/default privilege differences
- extension availability constraints
- restore/publish limitations
- provider-specific deployment warnings

### 9. nuget-packaging-and-release-expert
### 10. repo-doc-maintainer
### 11. performance-and-memory-expert
### 12. breaking-change-reviewer

## Analyzer-Oriented Future Skills

These can remain docs first and later evolve into analyzers:
- version-gated-postgresql-rules
- deployment-safety-review
- managed-platform-compatibility-review
- documentation-sync-review
