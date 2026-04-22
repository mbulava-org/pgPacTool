# Database Project Layout Expert Skill

## Purpose

Use this skill when modifying the generated or documented SDK-style database project layout in
pgPacTool, including folder conventions, file naming, security organization, and csproj defaults.

## When To Use This Skill

Use this skill when changing:
- `src/libs/mbulava.PostgreSql.Dac/Extract/CsprojProjectGenerator.cs`
- generated schema/object/security file layout
- `.csproj` generation defaults
- README examples that show extracted/generated project structure
- security roles/permissions output layout

## Repository Defaults

Generated projects currently use stable conventions:
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
- optional `{schema}/_owners.sql`

Additional defaults:
- one object = one SQL file
- generated project `PostgresVersion` is normalized to major version only
- missing version defaults to `16`
- missing default schema defaults to `public`

## Core Operating Rules

1. **Preserve one-object-per-file organization.**
2. **Preserve stable schema/object/security folder conventions.**
3. **Keep `_schema.sql` first-class and early-sorting.**
4. **Generate `_owners.sql` only when needed.**
5. **Keep security artifacts centralized under `Security/`.**
6. **Normalize `PostgresVersion` to major version only in generated project files.**
7. **Default `DefaultSchema` to `public` when absent.**
8. **Keep generated layout aligned with README examples and SDK auto-discovery behavior.**

## Expected Implementation Behavior

### When editing generator behavior
- Keep file naming predictable and version-control friendly.
- Preserve one-object-per-file outputs unless there is a clear repo-level decision otherwise.
- Treat overloaded function naming changes carefully because they affect layout stability.

### When editing generated `.csproj`
- Preserve SDK-style defaults and comments that explain conventions.
- Keep generated metadata aligned with current supported workflow.

### When editing security layout
- Keep role and permission artifacts centralized and discoverable.
- Keep ownership statements explicit and separate when they are not part of base object definitions.

## Required Documentation Updates

If project layout changes, update:
- `docs/features/embedded-skills/DATABASE_PROJECT_LAYOUT_RESEARCH.md`
- `README.md`
- `src/postgresPacTools/README.md`
- `.github/copilot-instructions.md` if durable layout rules change

## Required Test Mindset

Add or update tests for:
- generated folder structure
- generated csproj defaults
- version/default-schema normalization
- owner/security artifact generation
- file-count or structure expectations where appropriate

## Do Not Do

- Do not casually change folder conventions.
- Do not break SDK auto-discovery expectations.
- Do not let README examples drift from generated reality.
- Do not make object output less version-control friendly without clear justification.

## Skill References

- `docs/features/embedded-skills/DATABASE_PROJECT_LAYOUT_RESEARCH.md`
- `src/libs/mbulava.PostgreSql.Dac/Extract/CsprojProjectGenerator.cs`
- `tests/mbulava.PostgreSql.Dac.Tests/Extract/CsprojProjectGeneratorTests.cs`
- `README.md`
- `src/postgresPacTools/README.md`
