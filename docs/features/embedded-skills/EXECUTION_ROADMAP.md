# Embedded Skills Execution Roadmap

## Phase 1 — First-Tranche Implementation

### Step A. Research `compare-and-deploy-expert`
Capture:
- compare semantics by object type
- destructive vs non-destructive changes
- publish ordering rules
- publish vs script divergence
- ownership and target-context rules
- required tests and docs

Status:
- Research captured in `COMPARE_AND_DEPLOY_RESEARCH.md`

### Step B. Research `postgresql-catalog-extraction-expert`
Capture:
- extraction query structure
- catalog column version gates
- mapping rules into `DbObjects.cs`
- normalization behavior
- provider-specific extraction caveats
- required tests and docs

Status:
- Research captured in `CATALOG_EXTRACTION_RESEARCH.md`

### Step C. Research `msbuild-sdk-expert`
Capture:
- SQL discovery rules
- pre/post deployment configuration rules
- CLI host integration rules
- Visual Studio and restore expectations
- incremental build expectations
- diagnostics and verbose behavior

Status:
- Research captured in `MSBUILD_SDK_RESEARCH.md`

### Step D. Author the first three skills
Create:
- `.github/skills/compare-and-deploy-expert/`
- `.github/skills/postgresql-catalog-extraction-expert/`
- `.github/skills/msbuild-sdk-expert/`

Then update:
- `.github/copilot-instructions.md`
- `docs/README.md`
- this feature folder index as needed

## Phase 2 — Supporting Skill Set

Implement:
- `cli-ux-and-diagnostics-expert`
- `native-libpg-query-integration-expert`
- `test-matrix-expert`
- `database-project-layout-expert`

Research captured in:
- `CLI_UX_AND_DIAGNOSTICS_RESEARCH.md`
- `NATIVE_LIBPG_QUERY_RESEARCH.md`
- `TEST_MATRIX_RESEARCH.md`
- `DATABASE_PROJECT_LAYOUT_RESEARCH.md`

## Phase 3 — Cross-Cloud and Governance

Implement:
- `managed-postgresql-platforms-expert`
- `nuget-packaging-and-release-expert`
- `repo-doc-maintainer`
- `performance-and-memory-expert`
- `breaking-change-reviewer`

## Analyzer Roadmap

### Analyzer themes
- version-gated PostgreSQL syntax misuse
- provider-specific unsupported operations
- built-in/reserved role misuse
- unsafe compare/deploy changes
- missing `SET search_path` on security-definer functions
- missing docs/tests for semantic changes
- SDK authoring mistakes

### Target implementation style
- Roslyn analyzers for C# surfaces
- test-backed rule IDs
- optional code fixes for common safe remediations
- documentation-linked diagnostics

Detailed planning lives in:
- `ANALYZER_ROADMAP.md`
- `ANALYZER_ARCHITECTURE.md`
- `ADOPTION_CHECKLIST.md`

## Adoption Rules

When adding or changing significant repository behavior:
1. consult the relevant embedded skill
2. update the reference docs
3. add or adjust tests
4. decide whether a future analyzer rule should enforce the behavior
