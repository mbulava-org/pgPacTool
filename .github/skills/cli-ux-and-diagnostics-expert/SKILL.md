# CLI UX and Diagnostics Expert Skill

## Purpose

Use this skill when modifying the `postgresPacTools` CLI surface, command help, diagnostics,
verbosity behavior, exit-code handling, or user-facing workflow messaging.

## When To Use This Skill

Use this skill when changing:
- `src/postgresPacTools/Program.cs`
- CLI command options or aliases
- publish/script/compile command output
- error handling or exit behavior
- `src/postgresPacTools/README.md`
- root README CLI workflow examples

## Repository Defaults

- The CLI currently exposes: `extract`, `publish`, `script`, `compile`, and `deploy-report`.
- Commands use banner-style console output.
- Sensitive connection details are masked before display.
- Failure paths currently return non-zero process exit codes.
- Publish writes a deployment script to disk for troubleshooting.
- Verbose compile output exposes richer project/object information.

## Core Operating Rules

1. **Keep CLI docs and actual options aligned.**
2. **Mask secrets in all displayed connection strings.**
3. **Preserve clear non-zero exit behavior on failure.**
4. **Keep verbose mode additive, not disruptive.**
5. **Prefer consistent success, warning, and error formatting across commands.**
6. **Keep user-facing workflow guidance aligned with current recommended `.csproj` / `.pgpac` usage.**
7. **Preserve troubleshooting value in publish/script output.**

## Expected Implementation Behavior

### When editing commands
- Keep option names, aliases, and README examples consistent.
- Avoid introducing one-off output patterns unless there is a strong reason.

### When editing diagnostics
- Keep progress output readable first.
- Make verbose output reveal more detail without changing command semantics.
- Preserve script/report output paths in success messages when relevant.

### When editing errors
- Keep messages actionable.
- Prefer explaining what failed and what the user should check next.
- Preserve non-zero failure behavior.

## Required Documentation Updates

If CLI behavior changes, update:
- `docs/features/embedded-skills/CLI_UX_AND_DIAGNOSTICS_RESEARCH.md`
- `src/postgresPacTools/README.md`
- `README.md` if the recommended workflow changes
- `.github/copilot-instructions.md` if durable CLI rules change

## Required Test Mindset

Add or update tests for:
- option/alias behavior
- exit codes
- verbose output expectations
- script output persistence
- masking of secrets in displayed connection strings

## Do Not Do

- Do not leak credentials in logs.
- Do not let README examples drift from actual command behavior.
- Do not make verbose mode required for normal usability.
- Do not change recommended workflow messaging casually.

## Skill References

- `docs/features/embedded-skills/CLI_UX_AND_DIAGNOSTICS_RESEARCH.md`
- `src/postgresPacTools/Program.cs`
- `src/postgresPacTools/README.md`
- `README.md`
