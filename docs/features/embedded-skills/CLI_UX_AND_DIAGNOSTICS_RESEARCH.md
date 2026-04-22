# Step 9 — Research: `cli-ux-and-diagnostics-expert`

## Purpose

This document captures the current CLI UX and diagnostics behavior in `postgresPacTools` so the
future `cli-ux-and-diagnostics-expert` skill can be authored from actual command design,
console-output patterns, and operational expectations.

## Primary Surfaces Reviewed

- `src/postgresPacTools/Program.cs`
- `src/postgresPacTools/README.md`
- `src/libs/mbulava.PostgreSql.Dac/Compile/CompileObjectTreeFormatter.cs`
- `README.md`

---

## Current CLI Shape

The CLI exposes five primary commands:
- `extract`
- `publish`
- `script`
- `compile`
- `deploy-report`

The command surface is intentionally similar to SqlPackage-style workflows.

## Durable UX Patterns Observed

### 1. Banner-first command experience
Commands print a banner header before doing work.

### 2. Task-oriented emoji/status output
The CLI uses visual status markers (`📋`, `✅`, `❌`, `⚠️`, `🚀`, etc.) to communicate progress.

### 3. Human-readable progress before machine output
The CLI prioritizes readable progress summaries over structured output, except for explicit report
or file outputs.

### 4. Sensitive connection strings are masked before display
The CLI prints masked connection strings in console output.

### 5. Errors terminate with non-zero exit via `Environment.Exit(1)`
This is used repeatedly in command handlers.

### 6. `compile --verbose` exposes richer internal detail
The compile command prints object tree / deployment ordering information when verbose is enabled.

### 7. `publish` always persists a generated deployment script
The README documents this as a troubleshooting aid, even when publish executes against the target.

### 8. Command aliases are compact and repo-specific
Examples:
- `-scs`
- `-tf`
- `-sf`
- `-tcs`
- `-so`
- `-dons`

## Main Risks / Gaps

### 1. Output style is rich but inconsistent
Different commands have different degrees of detail, verbosity, and structured reporting.

### 2. Error handling is imperative and duplicated
Many handlers catch `Exception`, print a message, and call `Environment.Exit(1)`.

### 3. `deploy-report` still appears tied to `.pgproj.json` wording
This may lag behind current `.pgpac` / `.csproj` workflow guidance.

### 4. UX expectations live partly in docs and partly in code
This creates drift risk.

## Recommended Durable Rules

1. Keep command help, README examples, and actual options aligned.
2. Mask secrets in all displayed connection strings.
3. Preserve non-zero exit behavior for failures.
4. Keep verbose mode additive, not disruptive.
5. Persist deployment scripts for troubleshooting when publish runs.
6. Prefer consistent success/warning/error formatting across commands.
7. Keep user-facing wording aligned with the current recommended workflow (`.csproj` / `.pgpac`).

## Follow-On Skill Scope

The future skill should guide:
- option naming/alias consistency
- error and exit-code behavior
- verbose diagnostics behavior
- script/report output expectations
- user-facing messaging consistency

*Last updated: Current Session*
