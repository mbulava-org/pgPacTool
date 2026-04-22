# Analyzer Architecture

## Purpose

This document defines the initial architecture for repository analyzers that enforce durable rules
from the embedded skills and version-reference docs.

## Recommended Implementation Style

### Primary mechanism
- **Roslyn analyzers** for C# code surfaces in the repository

### Optional supporting mechanisms
- test-backed validation helpers for non-Roslyn scenarios
- package/layout validation tests for artifact structure
- CI policy checks for docs/process synchronization where Roslyn is insufficient

## Scope by Surface

### Roslyn analyzers are a good fit for
- compare logic in C#
- extraction logic in C#
- publish/script/orchestration code in C#
- CLI output/masking patterns in C#
- SDK authoring code in C# where source patterns are analyzable

### Roslyn analyzers are a partial fit for
- `.targets` / `.props` validation
- package layout contracts
- generated asset structure

These may require supporting tests or custom validation tools in addition to analyzers.

## Proposed Project Structure

Suggested future additions:

```text
src/
  analyzers/
    pgPacTool.Analyzers/
    pgPacTool.Analyzers.CodeFixes/

tests/
  pgPacTool.Analyzers.Tests/
```

## Rule ID Strategy

Use stable prefixes by analyzer family:

- `PGP1000` range — PostgreSQL version-gated rules
- `PGP2000` range — roles / privileges / security rules
- `PGP3000` range — compare and deploy safety rules
- `PGP4000` range — catalog extraction correctness rules
- `PGP5000` range — MSBuild SDK authoring rules
- `PGP6000` range — CLI UX and diagnostics rules
- `PGP7000` range — native runtime and packaging rules
- `PGP8000` range — documentation / process sync rules

## Severity Strategy

### Error
Use when a pattern is highly likely to produce incorrect or unsafe behavior.
Examples:
- emitting built-in role DDL
- plaintext password handling
- clearly unsupported version-specific syntax path without gating

### Warning
Use when behavior is risky or incomplete but not always immediately fatal.
Examples:
- function identity by name only
- missing `SET search_path` on security-definer function logic
- SDK incremental-input drift candidates

### Info / Suggestion
Use for improvement guidance or likely drift.
Examples:
- recommend shared helper use
- recommend test/doc update pairing

## Diagnostic Message Design

Each diagnostic should include:
1. what rule was violated
2. why it matters in this repo
3. what to change next
4. when possible, a link/reference to the governing skill/doc

## Code-Fix Strategy

Only implement a code fix when:
- the remediation is deterministic
- the analyzer can safely preserve intent
- the change will not hide a design decision from the contributor

## Testing Strategy for Analyzers

Each analyzer should have:
- positive detection tests
- non-trigger tests
- severity tests when applicable
- code-fix tests if a fixer exists
- rule reference in test comments or naming

## Mapping Analyzers to Skills

Every analyzer should map back to:
- one primary embedded skill
- one primary research/reference doc
- one or more target code surfaces

Example mapping table:

| Rule ID | Skill | Primary Doc | Target Surface |
|---|---|---|---|
| `PGP3001` | `compare-and-deploy-expert` | `COMPARE_AND_DEPLOY_RESEARCH.md` | compare/publish code |
| `PGP4002` | `postgresql-catalog-extraction-expert` | `CATALOG_EXTRACTION_RESEARCH.md` | extraction code |
| `PGP5001` | `msbuild-sdk-expert` | `MSBUILD_SDK_RESEARCH.md` | SDK code |
| `PGP6001` | `cli-ux-and-diagnostics-expert` | `CLI_UX_AND_DIAGNOSTICS_RESEARCH.md` | CLI code |

## Non-Roslyn Enforcement Areas

Some rules are better enforced via tests or custom validation tooling:
- package layout contracts
- native asset inventory
- `.targets` / `.props` semantics
- generated project layout verification
- cloud-platform compatibility matrices

These should still be documented in the analyzer architecture as part of the broader rule system.

## Initial Recommended Analyzer Candidates

Start with analyzers that are:
- high value
- low ambiguity
- C#-source detectable

Recommended first candidates:
- `PGP3001` function identity must not be name-only in compare logic
- `PGP2001` built-in/reserved roles must not be user-managed in generated role DDL paths
- `PGP2002` plaintext passwords must not be emitted or compared
- `PGP6001` user-facing CLI output must not print unmasked connection strings
- `PGP5001` generated database package path must not be routed through `TargetPath`

*Last updated: Current Session*
