# Changelog

All notable changes to pgPacTool will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

---

## [Unreleased]

---

## [1.0.0] - 2026-05-31

This is the stable 1.0.0 release of pgPacTool, consolidating all improvements from preview1 through preview10.

### Added

- **PostgreSQL 18 support** — bundled `libpg_query_18` native libraries and version-aware parser selection ([DEV-79](/DEV/issues/DEV-79))
- **Security, roles, and permissions extraction** — full GrantStmt/RevokeStmt support; GrantStmt/RevokeStmt protobuf issues resolved ([DEV-416](/DEV/issues/DEV-416))
- **Multi-version native library distribution** — all-platform native libs included in `MSBuild.Sdk.PostgreSql` NuGet package ([DEV-76](/DEV/issues/DEV-76))
- **AST compilation API** — fixed and tested for CI ([DEV-320](/DEV/issues/DEV-320))
- **ProjectPublisher integration tests** — end-to-end publisher coverage ([DEV-46](/DEV/issues/DEV-46))
- **44 edge/corner-case schema comparer tests** — covering column, constraint, index, and multi-schema diffs ([DEV-188](/DEV/issues/DEV-188))
- **CI test discovery floor check** — gate requires ≥ 266 discovered tests ([DEV-392](/DEV/issues/DEV-392))
- **CI native library loading verification steps** ([DEV-361](/DEV/issues/DEV-361))
- **Hard-gate SQL generation consistency step** in CI pipeline
- `NativeLibraryLoader.EnsureLoaded` public API for explicit native library initialization
- Extraction tests for Composite types, Domain types, Extensions, and multi-schema databases
- `DockerAvailability.SkipIfUnavailable()` guards on all Testcontainer-based tests
- `[FactRequiresDocker]` attribute for Docker-dependent facts
- SDK builds routed through `pgpac` CLI (preview5)
- Visual Studio-compatible `.csproj` project format with convention-based SQL file discovery

### Fixed

- `CompareOwners=false` not respected at schema/table/type level
- DROP TRIGGER statements now idempotent with `IF EXISTS` and correct syntax ([DEV-294](/DEV/issues/DEV-294))
- `AT_DropConstraint` now emits `IF EXISTS` in generated SQL ([DEV-324](/DEV/issues/DEV-324))
- CHECK and FOREIGN KEY constraint SQL generation implemented ([DEV-337](/DEV/issues/DEV-337))
- `CsprojProjectLoader` now correctly detects `CREATE OR REPLACE VIEW`, `CREATE MATERIALIZED VIEW`, `CREATE OR REPLACE FUNCTION`, and `CREATE OR REPLACE PROCEDURE` SQL variants
- `CsprojProjectLoader` now propagates SQL parse errors instead of silently discarding them
- `PostgresVersion` is optional in `CsprojProjectLoader`, defaulting to 16
- `ContainerBuilder` updated to Testcontainers v4.x image constructor API ([DEV-312](/DEV/issues/DEV-312))
- `NpgqueryExtended.Tests` re-enabled on Linux (Issue #37 resolved)
- `NpgqueryExtended.Tests` host process crash on exit fixed ([DEV-78](/DEV/issues/DEV-78))
- `Google.Protobuf` explicit reference added to `postgresPacTools` CLI to fix missing assembly on publish ([DEV-52](/DEV/issues/DEV-52))
- `osx-arm64` RID and `.gitattributes` line-ending normalization ([DEV-413](/DEV/issues/DEV-413))
- CI TRX overwrite fixed to prevent incorrect test counts in PR comments
- `NugetPackage.Tests` TRX included in test analysis
- Parallel extraction disabled in `NpgqueryExtended.Tests` with crash-safe cleanup ([DEV-69](/DEV/issues/DEV-69))
- `PagilaIntegrationTests` Docker-skip now shows `Skipped` instead of `Failed` ([DEV-64](/DEV/issues/DEV-64))
- `PostgreSqlVersion` enum usage conflicts and duplicate class conflicts resolved
- Null reference in sequence extraction
- Connection pool management improvements

### Changed

- `PostgreSQL 15` support removed (Windows-only limitation)
- NuGet packaging switched to `--no-restore` (removed `--no-build` flag) to allow `CopyNativeLibraries` target to run correctly
- `AssemblyVersion` in Npgquery restricted to numeric-only segments (CS7034 fix)

### Supported PostgreSQL Versions

- ✅ **PostgreSQL 16** (default)
- ✅ **PostgreSQL 17**
- ✅ **PostgreSQL 18**
- ❌ PostgreSQL 14, 15 (not supported)

### Package Information

- **mbulava.PostgreSql.Dac** v1.0.0 — Core library for programmatic access (.NET 10, MIT)
- **postgresPacTools** v1.0.0 — Global CLI tool (`pgpac`) (.NET 10, MIT)
- **MSBuild.Sdk.PostgreSql** v1.0.0 — MSBuild SDK for database projects (.NET 10, MIT)

---

## [1.0.0-preview9] - 2026-05-18

### Fixed

- Merged all recent test coverage improvements from main branch
- Fixed `DockerAvailability.SkipIfUnavailable()` guards in Testcontainer-based tests (Extensions, MultiSchema, CompositeType, DomainType, PostgresVersionTestBase)
- Fixed `CsprojProjectLoader` to correctly detect `CREATE OR REPLACE VIEW`, `CREATE MATERIALIZED VIEW`, `CREATE OR REPLACE FUNCTION`, and `CREATE OR REPLACE PROCEDURE` SQL statement variants
- Added `Google.Protobuf` explicit reference to `postgresPacTools` CLI to fix missing assembly on publish ([DEV-52](/DEV/issues/DEV-52))
- Improved test coverage: ProjectPublisher integration tests, PgSchemaComparerTests column/constraint/index/multi-schema diffs, error path tests
- Resolved merge conflicts between main and preview1 branches

---

## [1.0.0-preview1] - 2026-03-17

### Added

#### Core Library (mbulava.PostgreSql.Dac)

- **Schema Extraction** — extract complete database schemas with full metadata
- Support for all major PostgreSQL object types (Tables, Views, Functions, Stored Procedures, Types, Sequences, Triggers, Schemas, Roles, Extensions)
- AST-based parsing using libpg_query
- Privilege and permission extraction
- Column comments and descriptions
- Dependency analysis, circular dependency detection, topological sorting for deployment ordering
- Parallel deployment grouping
- Schema comparison: generate CREATE/DROP/ALTER migration scripts
- Pre/post-deployment script support with SQLCMD variable substitution
- Transaction-wrapped deployments
- JSON format (`.pgproj.json`) and SDK-style project format (`.csproj`)
- DACPAC-style package format (`.pgpac`) for deployment

#### CLI Tool (postgresPacTools)
- `extract`, `compile`, `publish`, `script`, `deploy-report` commands
- Verbose mode (`-v`), color-coded terminal output, progress indicators

#### MSBuild SDK (MSBuild.Sdk.PostgreSql)
- Standard `.csproj` format with convention-based SQL file discovery
- Automatic dependency resolution, incremental build support
- Generates `.pgpac` deployment packages

### Testing

- 201 tests with 100% pass rate (171 unit + 30 Docker integration)
- Tested against `world_happiness`, `dvdrental`, and `pagila` databases

### Fixed

- Null reference in sequence extraction
- Aggregate function handling in function extraction
- Database existence validation with clear errors
- Connection pool management

---

## Future Releases

### [1.1.0] - Planned

**Focus: Multi-Schema & Large Database Performance**

- Full multi-schema support with cross-schema dependency tracking
- Auto-discovery and ordering of deployment scripts
- Large database optimizations (10,000+ objects) and parallel extraction
- Rollback support
- Visual Studio project templates and enhanced IntelliSense

### [2.0.0] - Future Ideas

**Focus: Ecosystem Integration**

- Azure DevOps pipeline tasks
- GitHub Actions workflows
- Docker images for CI/CD
- VS Code extension
- Schema drift detection
- Data migration tools

---

## Getting Help

- **Documentation**: https://github.com/mbulava-org/pgPacTool/tree/main/docs
- **Issues**: https://github.com/mbulava-org/pgPacTool/issues
- **Discussions**: https://github.com/mbulava-org/pgPacTool/discussions

---

## Contributing

We welcome contributions! See [CONTRIBUTING.md](CONTRIBUTING.md) for guidelines.

---

[Unreleased]: https://github.com/mbulava-org/pgPacTool/compare/v1.0.0...HEAD
[1.0.0]: https://github.com/mbulava-org/pgPacTool/compare/v1.0.0-preview1...v1.0.0
[1.0.0-preview9]: https://github.com/mbulava-org/pgPacTool/compare/v1.0.0-preview1...v1.0.0-preview9
[1.0.0-preview1]: https://github.com/mbulava-org/pgPacTool/releases/tag/v1.0.0-preview1
