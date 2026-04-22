# Test Matrix Expert Skill

## Purpose

Use this skill when deciding how to validate changes in pgPacTool across unit, integration,
package, runtime, and Linux/container test layers.

## When To Use This Skill

Use this skill when changing:
- version-gated PostgreSQL behavior
- compare/publish semantics
- extraction semantics
- package/runtime behavior
- native loading behavior
- CI-sensitive or Linux-sensitive functionality

## Repository Defaults

- The repo uses multiple test layers, not just unit tests.
- Testcontainers are a core integration testing tool.
- Package validation tests are used for real package-consumption scenarios.
- Linux container tests are used to catch CI/platform-specific issues.
- Version coverage is explicitly categorized in integration tests.

## Core Operating Rules

1. **Pick the smallest test that proves the semantic rule.**
2. **Use Testcontainers when behavior depends on real PostgreSQL runtime semantics.**
3. **Use package tests for packaging, tool, SDK, and native-asset changes.**
4. **Use Linux container tests for native or CI-sensitive changes.**
5. **Keep version-specific behavior covered by explicit version-category tests.**
6. **Treat docs and tests as paired updates for durable repo rules.**

## Expected Implementation Behavior

### When changing core logic
- Prefer unit tests first.
- Add integration tests when catalog/runtime/PostgreSQL behavior matters.

### When changing packaging or SDK/tool distribution
- Use package validation tests.
- Verify install/restore/consume paths, not just build success.

### When changing native loading or platform-sensitive code
- Include Linux/container validation.
- Consider cross-platform packaging implications.

## Required Documentation Updates

If testing expectations change, update:
- `docs/features/embedded-skills/TEST_MATRIX_RESEARCH.md`
- relevant test README files
- `.github/copilot-instructions.md` if durable testing rules change

## Required Test Mindset

Add or update coverage in the correct layer:
- unit
- Testcontainers integration
- privilege extraction/revoke suites
- package validation
- Linux container validation

## Do Not Do

- Do not rely on unit tests alone for runtime PostgreSQL semantics.
- Do not skip Linux/container validation for native-sensitive changes.
- Do not treat package structure as validated just because projects build locally.
- Do not add durable repo rules without deciding what test layer should enforce them.

## Skill References

- `docs/features/embedded-skills/TEST_MATRIX_RESEARCH.md`
- `tests/ProjectExtract-Tests/Integration/README.md`
- `tests/ProjectExtract-Tests/Privileges/README.md`
- `tests/NugetPackage.Tests/README.md`
- `tests/LinuxContainer.Tests/README.md`
