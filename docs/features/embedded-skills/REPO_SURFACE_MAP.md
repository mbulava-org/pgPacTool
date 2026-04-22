# Repository Surface Map for Embedded Skills

This map connects planned skills to the code, docs, and tests they govern.

## PostgreSQL Object / Version Semantics

### Existing references
- `.github/skills/postgresql-expert/SKILL.md`
- `docs/version-differences/PG_ROLES_PERMISSIONS_SECURITY.md`
- `docs/version-differences/PG_DATABASE_OBJECTS.md`
- `docs/features/multi-version-support/VERSION_COMPATIBILITY_STRATEGY.md`

### Primary code surfaces
- `src/libs/mbulava.PostgreSql.Dac/Models/DbObjects.cs`
- `src/libs/mbulava.PostgreSql.Dac/Models/Compare.cs`
- `src/libs/mbulava.PostgreSql.Dac/Compare/PgSchemaComparer.cs`
- `src/libs/mbulava.PostgreSql.Dac/Extract/PgProjectExtractor.cs`

## Compare and Deploy

### Primary code surfaces
- `src/libs/mbulava.PostgreSql.Dac/Compare/PgSchemaComparer.cs`
- `src/libs/mbulava.PostgreSql.Dac/Compare/PublishScriptGenerator.cs`
- `src/libs/mbulava.PostgreSql.Dac/Publish/ProjectPublisher.cs`
- `src/libs/mbulava.PostgreSql.Dac/Publish/PublishOwnershipPolicyService.cs`
- `src/libs/mbulava.PostgreSql.Dac/Publish/PublishTargetDatabaseContextService.cs`
- `src/libs/mbulava.PostgreSql.Dac/Deployment/PrePostDeploymentScriptManager.cs`

### Primary tests
- `tests/mbulava.PostgreSql.Dac.Tests/Compare/`
- `tests/mbulava.PostgreSql.Dac.Tests/Publish/`
- `tests/mbulava.PostgreSql.Dac.Tests/Deployment/`

## Catalog Extraction

### Primary code surfaces
- `src/libs/mbulava.PostgreSql.Dac/Extract/PgProjectExtractor.cs`
- related extraction helpers in `Extract/`
- `src/libs/mbulava.PostgreSql.Dac/Models/DbObjects.cs`

### Primary tests
- `tests/mbulava.PostgreSql.Dac.Tests/Extract/`
- `tests/ProjectExtract-Tests/`

## MSBuild SDK

### Primary code surfaces
- `src/sdk/MSBuild.Sdk.PostgreSql/Sdk/Sdk.props`
- `src/sdk/MSBuild.Sdk.PostgreSql/Sdk/Sdk.targets`
- `src/sdk/MSBuild.Sdk.PostgreSql/Tasks/CompilePgProject.cs`
- `src/sdk/MSBuild.Sdk.PostgreSql/Program.cs`
- `src/sdk/MSBuild.Sdk.PostgreSql/README.md`

### Related references
- `README.md`
- `src/postgresPacTools/README.md`

## CLI UX and Diagnostics

### Primary code surfaces
- `src/postgresPacTools/`
- command handlers and diagnostics formatting surfaces
- publish/compile/script entry points

## Native libpg_query Integration

### Primary code surfaces
- `src/libs/Npgquery/`
- native asset packaging and load paths
- multi-version loading docs under `docs/features/multi-version-support/`

## Managed PostgreSQL Platforms

### Primary references to create or extend
- platform matrix doc under `docs/version-differences/`
- `.github/skills/managed-postgresql-platforms-expert/`

### Cross-cloud focus
- Azure Database for PostgreSQL
- Amazon RDS for PostgreSQL
- Amazon Aurora PostgreSQL-Compatible Edition

## Documentation Maintenance

### Primary docs
- `README.md`
- `docs/README.md`
- `docs/features/`
- `docs/version-differences/`
- `.github/copilot-instructions.md`
