# Step 9 — Research: `test-matrix-expert`

## Purpose

This document captures the current multi-layer test strategy in pgPacTool so the future
`test-matrix-expert` skill can be authored from actual repository testing patterns.

## Primary Surfaces Reviewed

- `tests/ProjectExtract-Tests/Integration/README.md`
- `tests/ProjectExtract-Tests/Privileges/README.md`
- `tests/NugetPackage.Tests/README.md`
- `tests/LinuxContainer.Tests/README.md`
- `README.md`

---

## Current Test Matrix Layers

### 1. Unit tests
Used across core DAC, compare, compile, and extract behavior.

### 2. Integration tests against real PostgreSQL via Testcontainers
Versioned test strategy currently documents:
- PostgreSQL 16
- PostgreSQL 17
- PostgreSQL 18 future-proofing

### 3. Privilege-focused extraction tests
Dedicated suite for GRANT/REVOKE coverage across:
- schema
- table
- sequence
- function
- view
- PUBLIC
- role/user grantees
- grant option / revoke cases

### 4. NuGet/package validation tests
Covers:
- package structure
- dependency correctness
- native library inclusion
- global tool install/use
- full Pagila extract/compile/publish round-trip

### 5. Linux container tests
Runs other tests inside Linux containers to catch CI-specific issues.

## Durable Test Strategy Signals

1. The repo values real-environment testing, not only unit tests.
2. Docker/Testcontainers are a core testing capability.
3. Package validation is treated as product-surface validation, not just build success.
4. Linux behavior is important enough to validate locally before CI.
5. Version coverage is explicit and categorized.

## Main Risks / Gaps

1. Skill-driven rules are growing faster than explicit test coverage for some new semantics.
2. PG 18 future-proofing exists in docs/tests, but implementation support varies by surface.
3. Cloud-managed PostgreSQL matrix coverage is not yet visible.
4. Some repo skills may need scenario tests rather than unit tests alone.

## Recommended Durable Rules

1. Prefer adding the smallest test that proves the semantic rule.
2. Use Testcontainers when behavior depends on real PostgreSQL runtime behavior.
3. Use package/integration tests for SDK/tool/package surface changes.
4. Use Linux container tests for native-library or cross-platform risk areas.
5. Keep version-category coverage explicit when version-specific behavior changes.
6. Treat docs and tests as paired updates when durable repo rules change.

## Follow-On Skill Scope

The future skill should guide:
- choosing the right test layer
- version-matrix expectations
- package/runtime validation expectations
- Docker/Testcontainers usage
- Linux validation expectations

*Last updated: Current Session*
