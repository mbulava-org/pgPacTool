# Analyzer Roadmap

## Purpose

This roadmap translates the embedded repository skills into future machine-detectable diagnostics,
warnings, and code-fix opportunities. The goal is to move the highest-value durable rules from
human guidance into repeatable enforcement.

## Principles

1. **Start with correctness and safety rules before optimization rules.**
2. **Prefer analyzers for durable repo rules, not temporary implementation details.**
3. **Link every analyzer family back to at least one embedded skill and one source-of-truth doc.**
4. **Make diagnostics actionable, not merely descriptive.**
5. **Add code fixes only where the remediation is safe and predictable.**

## Analyzer Families

### Family A — PostgreSQL Version-Gated Rules
**Goal:** detect code or generated-behavior assumptions that violate PG 15–18 version rules.

Possible diagnostics:
- Use of PG 17+ object features without version gating
- Use of PG 18-only clauses without target-version guard
- Missing support for version-sensitive model properties when docs require them
- Hard-coded assumptions that PG major versions are interchangeable

Source skills/docs:
- `postgresql-expert`
- `postgresql-catalog-extraction-expert`
- `docs/version-differences/PG_DATABASE_OBJECTS.md`
- `docs/version-differences/PG_ROLES_PERMISSIONS_SECURITY.md`

### Family B — Roles, Privileges, and Security Rules
**Goal:** catch role/security mistakes that lead to incorrect compare/publish behavior.

Possible diagnostics:
- Built-in or reserved roles being treated as user-managed roles
- Missing model fields needed for role/version semantics
- Missing handling for PG 16+ membership options
- Missing handling for PG 17+ / PG 18+ predefined roles
- Plaintext password emission or password comparison misuse

Source skills/docs:
- `postgresql-expert`
- `managed-postgresql-platforms-expert` (future)
- `docs/version-differences/PG_ROLES_PERMISSIONS_SECURITY.md`

### Family C — Compare and Deploy Safety Rules
**Goal:** detect unsafe or incomplete compare/publish behavior.

Possible diagnostics:
- Function identity compared by name instead of signature
- Target-only object/schema handling omitted in destructive workflows
- Missing destructive-change warnings/reporting
- Publish/script divergence risks
- Ownership enforcement drift

Source skills/docs:
- `compare-and-deploy-expert`
- `COMPARE_AND_DEPLOY_RESEARCH.md`

### Family D — Catalog Extraction Correctness Rules
**Goal:** catch lossy or incomplete extraction logic.

Possible diagnostics:
- Duplicate ACL parsing logic drifting across object types
- Missing extraction of structured semantics required by docs
- Version-specific catalog fields used without guards
- Hardcoded trigger owner assumptions
- Missing function privilege extraction when a model expects it

Source skills/docs:
- `postgresql-catalog-extraction-expert`
- `CATALOG_EXTRACTION_RESEARCH.md`

### Family E — MSBuild SDK Authoring Rules
**Goal:** protect SDK restore/build/package behavior.

Possible diagnostics:
- New compile-relevant inputs not represented in incremental build invalidation
- Package layout assumptions drifting from target paths
- CLI-host-driven SDK build path being bypassed
- README/docs drift from actual SDK defaults
- Misuse of `TargetPath` for generated database packages

Source skills/docs:
- `msbuild-sdk-expert`
- `MSBUILD_SDK_RESEARCH.md`

### Family F — CLI UX and Diagnostics Rules
**Goal:** keep command-line behavior consistent and safe.

Possible diagnostics:
- Unmasked connection strings in console output
- README option drift from actual command options
- Missing non-zero failure behavior
- Inconsistent verbose output routing

Source skills/docs:
- `cli-ux-and-diagnostics-expert`
- `CLI_UX_AND_DIAGNOSTICS_RESEARCH.md`

### Family G — Native Runtime and Packaging Rules
**Goal:** catch native-library and packaging regressions.

Possible diagnostics:
- Runtime asset layout changes not reflected in loader logic
- Version support declarations without matching native assets
- Package/runtime assumptions inconsistent with tests/docs

Source skills/docs:
- `native-libpg-query-integration-expert`
- `NATIVE_LIBPG_QUERY_RESEARCH.md`

### Family H — Documentation and Process Sync Rules
**Goal:** ensure durable behavior changes update docs/tests together.

Possible diagnostics:
- Changes to version-sensitive model/compare/extract code without corresponding doc updates
- Changes in key behavior surfaces without associated tests
- README/footer metadata drift where explicitly required

Source skills/docs:
- `repo-doc-maintainer` (future)
- `breaking-change-reviewer` (future)
- embedded-skills research docs

## Suggested Initial Analyzer Backlog

### Wave 1 — High Value, Low Ambiguity
1. Function identity must not be name-only in compare logic
2. Built-in/reserved roles must not be emitted as user-managed DDL
3. Plaintext password handling must not be introduced
4. SDK must not use `TargetPath` for `.pgpac`
5. Unmasked connection strings must not be written to user-facing CLI output

### Wave 2 — Medium Complexity
6. Duplicate ACL parser implementations should be consolidated or justified
7. Version-specific catalog fields require explicit gating
8. Destructive compare/deploy behavior must emit a clear warning/report signal
9. Missing `SET search_path` on `SECURITY DEFINER` functions should trigger a warning

### Wave 3 — Higher Complexity / Cross-Cutting
10. Model/doc parity for version-sensitive properties
11. Cloud-managed platform compatibility rules (Azure, RDS, Aurora)
12. Package layout validation for native and SDK assets
13. Documentation/test synchronization checks

## Optimization-Oriented Future Rules

After correctness/safety rules stabilize, analyzers can also suggest:
- extraction query reuse opportunities
- excessive string-based compare logic where structured compare exists
- repeated diagnostic formatting helpers for CLI consistency
- performance-sensitive hot paths in extraction/compare/publish code

## Code-Fix Candidates

Good candidates for automatic fixes:
- add missing version guards in simple conditionals
- normalize use of approved helper methods
- replace unsafe direct output with masking helpers
- replace duplicated analyzer-detectable patterns with shared helpers

Poor candidates for automatic fixes:
- complex compare semantics
- destructive deployment behavior
- cloud-provider-specific policy behavior
- major package layout or build-orchestration changes

## Success Criteria

The analyzer roadmap is successful when:
- high-risk repo rules become machine-detectable
- docs/skills/tests stay aligned more easily
- contributors get earlier feedback than CI/runtime failures
- false positives remain low enough to keep analyzers trusted

*Last updated: Current Session*
