# Repository Documentation Maintainer Skill

## Purpose

Use this skill when modifying durable repository behavior that should be reflected in README,
docs indexes, feature docs, or instruction files.

## When To Use This Skill

Use this skill when changing:
- architecture or implementation that affects documentation
- package/version/support statements
- docs navigation/indexes
- user-visible workflow guidance
- durable rules in `.github/copilot-instructions.md`
- feature-folder content under `docs/features/`

## Repository Defaults

- Documentation should evolve with the code.
- Feature-specific docs belong in `docs/features/<feature-name>/`.
- Version-reference docs belong in `docs/version-differences/`.
- Root README should link to important feature docs.
- README metadata such as `Status`, `Version`, and `Last Updated` must stay in sync where present.

## Core Operating Rules

1. **Update docs when durable behavior changes.**
2. **Keep README, docs index, and instruction files aligned.**
3. **Place docs in the correct folder based on purpose.**
4. **Prefer concise durable guidance in instructions and deeper detail in docs.**
5. **Keep support/version statements accurate.**
6. **Do not leave example workflows behind actual implementation reality.**

## Expected Implementation Behavior

### When changing behavior
- Decide which README/docs pages are affected.
- Update feature docs or version-reference docs as needed.

### When changing repo rules
- Update `.github/copilot-instructions.md` if the rule is durable and frequently reused.

### When changing user-visible workflows
- Update root README and package/tool README examples.

## Required Documentation Updates

Commonly affected files:
- `README.md`
- `docs/README.md`
- `.github/copilot-instructions.md`
- relevant `docs/features/*`
- relevant `docs/version-differences/*`

## Required Test Mindset

When docs encode durable workflow or behavior assumptions, ensure tests still support those claims.

## Do Not Do

- Do not change behavior and leave docs stale.
- Do not place feature docs in the wrong folder.
- Do not leave outdated support/version statements in user-facing docs.
- Do not forget README metadata/footer updates when required.

## Skill References

- `README.md`
- `docs/README.md`
- `.github/copilot-instructions.md`
- `docs/features/embedded-skills/ADOPTION_CHECKLIST.md`
