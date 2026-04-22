# Step 9 — Research: `database-project-layout-expert`

## Purpose

This document captures the current generated/extracted database project layout conventions in
pgPacTool so the future `database-project-layout-expert` skill can be authored from actual
project-generation behavior and user-facing workflow docs.

## Primary Surfaces Reviewed

- `src/libs/mbulava.PostgreSql.Dac/Extract/CsprojProjectGenerator.cs`
- `tests/mbulava.PostgreSql.Dac.Tests/Extract/CsprojProjectGeneratorTests.cs`
- `README.md`
- `src/postgresPacTools/README.md`

---

## Current Layout Model

Generated SDK-style projects are folder-based and convention-driven.

### Core structure
- `{schema}/_schema.sql`
- `{schema}/Tables/`
- `{schema}/Indexes/`
- `{schema}/Views/`
- `{schema}/Functions/`
- `{schema}/Types/`
- `{schema}/Sequences/`
- `{schema}/Triggers/`
- `Security/Roles/`
- `Security/Permissions/`

### Additional generated files
- `{schema}/_owners.sql` when object owners differ from schema owner
- one `.sql` file per object
- one permissions file per schema when needed
- one role file per role
- one SDK-style `.csproj`

## Durable Generation Rules Observed

1. One object = one file.
2. `_schema.sql` sorts early and establishes schema creation.
3. `_owners.sql` is generated only when needed.
4. Security artifacts are centralized under `Security/`.
5. Project file uses `MSBuild.Sdk.PostgreSql` and `TargetFramework=net10.0`.
6. Extracted project `PostgresVersion` is normalized to major version only.
7. Missing/blank extracted version defaults to 16 in generated project files.
8. Missing/blank default schema defaults to `public`.

## Main Risks / Gaps

1. Layout conventions are durable product behavior and should not drift casually.
2. File naming for overloaded functions may become problematic if signature-aware naming is introduced.
3. Role/security generation must evolve with richer privilege and role metadata.
4. Folder conventions and docs must stay aligned with extraction output.

## Recommended Durable Rules

1. Preserve one-object-per-file organization.
2. Preserve stable schema/object/security folder conventions.
3. Keep `_schema.sql` and `_owners.sql` semantics explicit.
4. Normalize PostgreSQL version to major version in generated project files.
5. Default `DefaultSchema` to `public` when absent.
6. Keep generated project structure aligned with user-facing docs and SDK auto-discovery behavior.

## Follow-On Skill Scope

The future skill should guide:
- folder conventions
- naming conventions
- generated file responsibilities
- security artifact organization
- csproj metadata defaults
- extraction-to-project mapping stability

*Last updated: Current Session*
