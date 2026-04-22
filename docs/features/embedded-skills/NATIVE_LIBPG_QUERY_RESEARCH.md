# Step 9 — Research: `native-libpg-query-integration-expert`

## Purpose

This document captures the current native libpg_query integration behavior in Npgquery so the
future `native-libpg-query-integration-expert` skill can be authored from actual loader,
packaging, diagnostics, and multi-version behavior.

## Primary Surfaces Reviewed

- `src/libs/Npgquery/Npgquery/Native/NativeLibraryLoader.cs`
- `src/libs/Npgquery/PACKAGE_README.md`
- `docs/features/multi-version-support/README.md`
- `tests/NpgqueryExtended.Tests/NativeLibraryDiagnosticsTests.cs`
- `tests/NpgqueryExtended.Tests/VersionCompatibilityTests.cs`
- `tests/NpgqueryExtended.Tests/LibraryDiscoveryTests.cs`
- `tests/LinuxContainer.Tests/README.md`
- `tests/NugetPackage.Tests/README.md`

---

## Current Native Loading Model

### Durable behavior observed
1. Libraries are version-specific and keyed by `PostgreSqlVersion`.
2. Loading is cached in dictionaries for handles and availability.
3. A module-initialization path ensures the resolver is installed early.
4. Search order prefers runtime-specific native asset paths:
   - `runtimes/<rid>/native/<library>`
   - base directory fallback
5. Diagnostics are a first-class feature:
   - in-memory diagnostic log
   - optional console logging
   - detailed `PrintDiagnostics()` output

## Platform Behavior

The loader explicitly handles:
- Windows (`.dll`)
- Linux (`.so`)
- macOS (`.dylib`)

The runtime identifier is derived from OS + architecture and used to build runtime search paths.

## Packaging Signals

From package/test docs:
- native libraries must be included for Windows, Linux, and macOS
- multiple PostgreSQL versions are expected in package validation
- Linux CI compatibility is important enough to justify dedicated Linux container tests

## Main Risks / Gaps

1. Package layout changes can silently break native loading.
2. Search path logic and package structure are tightly coupled.
3. Version availability assumptions must stay aligned with packaged native assets.
4. Platform issues often surface only in Linux CI/container runs.
5. Debugging experience depends on preserving diagnostic logging behavior.

## Recommended Durable Rules

1. Keep runtime-specific native asset layout stable unless all dependent packaging/loader logic is updated.
2. Preserve detailed diagnostics for native loading failures.
3. Treat Linux container verification as essential, not optional.
4. Keep versioned native assets aligned with declared PostgreSQL parser support.
5. Do not assume base-directory fallback alone is sufficient.
6. When changing version support, update native assets, loader expectations, docs, and tests together.

## Follow-On Skill Scope

The future skill should guide:
- native asset naming/layout
- search path strategy
- resolver initialization
- diagnostics and failure reporting
- package/test validation expectations

*Last updated: Current Session*
