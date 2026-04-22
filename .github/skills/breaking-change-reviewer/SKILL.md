# Breaking Change Reviewer Skill

## Purpose

Use this skill when a change may alter durable repository behavior, break compatibility, or create
drift between code, docs, tests, packages, or platform expectations.

## When To Use This Skill

Use this skill when changing:
- version-sensitive PostgreSQL behavior
- compare/publish semantics
- extraction model semantics
- generated project structure
- package/runtime layout
- CLI workflow contracts
- support/platform statements
- analyzer-enforced durable rules

## Repository Defaults

- Durable changes should be reflected in docs, tests, and often skills.
- Breaking behavior can exist at multiple layers:
  - model semantics
  - compare/deploy behavior
  - extraction completeness
  - package/runtime layout
  - SDK behavior
  - CLI workflow
  - cloud-platform assumptions

## Core Operating Rules

1. **Assume durable behavior changes need review beyond code compilation.**
2. **Check whether the change affects docs, tests, packages, generated output, and platform behavior.**
3. **Be explicit about destructive or compatibility-affecting behavior.**
4. **Prefer additive changes unless there is a strong reason to break compatibility.**
5. **Use the adoption checklist when behavior may affect multiple repo surfaces.**

## Expected Implementation Behavior

### When a breaking-risk change is introduced
- identify the affected surface area
- identify affected docs
- identify affected tests
- identify whether a future analyzer should enforce the rule
- document provider/version caveats if portability changes

### When updating workflows or package surfaces
- update README/docs/examples
- validate real consumption or runtime behavior where possible

## Required Documentation Updates

If a breaking-risk change is made, review/update:
- `docs/features/embedded-skills/ADOPTION_CHECKLIST.md`
- `docs/features/embedded-skills/ANALYZER_ROADMAP.md`
- relevant research doc(s)
- `README.md`
- `docs/README.md`
- `.github/copilot-instructions.md` if durable rules change

## Required Test Mindset

Use the correct validation layer for the risk:
- unit
- integration
- package validation
- Linux/container
- documentation/process sync where applicable

## Do Not Do

- Do not treat build success as proof that a breaking-risk change is safe.
- Do not leave durable behavior changes undocumented.
- Do not change public workflows without validating user-facing examples.
- Do not ignore Azure/RDS/Aurora implications for platform-sensitive behavior.

## Skill References

- `docs/features/embedded-skills/ADOPTION_CHECKLIST.md`
- `docs/features/embedded-skills/ANALYZER_ROADMAP.md`
- `.github/copilot-instructions.md`
- `.github/skills/test-matrix-expert/SKILL.md`
- `.github/skills/managed-postgresql-platforms-expert/SKILL.md`
