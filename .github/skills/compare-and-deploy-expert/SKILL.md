# Compare and Deploy Expert Skill

## Purpose

Use this skill when modifying schema compare behavior, publish orchestration, deployment script
generation, destructive-change handling, ownership enforcement, or deployment reporting in
pgPacTool.

## When To Use This Skill

Use this skill when changing any of the following:
- `src/libs/mbulava.PostgreSql.Dac/Compare/`
- `src/libs/mbulava.PostgreSql.Dac/Publish/`
- `src/libs/mbulava.PostgreSql.Dac/Deployment/`
- compare diff models in `Models/Compare.cs`
- publish options/result behavior in `Models/Deployment.cs`
- tests under `tests/mbulava.PostgreSql.Dac.Tests/Compare/`
- tests under `tests/mbulava.PostgreSql.Dac.Tests/Publish/`
- tests under `tests/mbulava.PostgreSql.Dac.Tests/Deployment/`

## Repository Defaults

- Compare/deploy is currently schema-by-schema.
- Ownership enforcement is policy-driven through `OwnershipMode`.
- Drop behavior is opt-in through `DropObjectsNotInSource`.
- Script generation currently emits objects in this order:
  1. types
  2. sequences
  3. tables
  4. views
  5. functions
  6. triggers
- Target database validation metadata is emitted when `TargetDatabase` is known.
- Pre/post deployment scripts are validated and combined outside normal schema compilation.

## Core Operating Rules

1. **Treat compare semantics as deployment semantics.**
   - Do not optimize only for smaller diffs.
   - Preserve the meaning of a change, especially when it affects drop/recreate behavior.

2. **Keep destructive changes explicit.**
   - `DropObjectsNotInSource` is a destructive switch.
   - Never hide object drops behind neutral wording or silent coercion.

3. **Preserve dependency order in generated scripts.**
   - Types before sequences before tables before views before functions before triggers.
   - Pre-deployment scripts stay before schema changes; post-deployment scripts stay after.

4. **Keep publish and script generation aligned.**
   - Changes to publish flow must not drift from generated-script behavior without an explicit,
     documented reason.

5. **Validate publish context before extraction and execution.**
   - Preserve `SourceDatabase`, `TargetDatabase`, and `DatabaseName` SQLCMD variables.
   - Preserve target database validation block generation when target DB is known.

6. **Preserve policy-driven ownership behavior.**
   - `OwnershipMode.Ignore` must disable owner comparison.
   - `OwnershipMode.Enforce` must enable owner comparison.
   - Explicit source owners must be validated against source roles.

7. **Prefer durable warnings over silent skipping.**
   - If behavior is incomplete (for example missing target schema creation), warn clearly.

## Expected Implementation Behavior

### When editing compare logic
- Normalize only non-semantic formatting.
- Do not normalize away semantic clauses.
- Function/procedure identity should use the full qualified signature, not just the bare name.
- Trigger identity is `(table, trigger)`.
- Materialized view changes should keep drop/recreate semantics.

### When editing script generation
- Preserve transaction wrapping behavior under `Transactional`.
- Preserve SQLCMD variable replacement as a final pass.
- Use drop/recreate only where semantics require it.
- Keep owner and privilege emission consistent with compare results.

### When editing publish orchestration
- Keep source compile before target compare.
- Keep target extraction tied to the effective target database context.
- Keep pre/post deployment script validation before script generation.
- Preserve generated script persistence when `OutputScriptPath` is set.

### When editing deployment reporting
- Count destructive changes consistently across object types.
- Do not under-report target-only object drops when drop mode is enabled.

## Required Documentation Updates

If compare/deploy behavior changes, update:
- `docs/features/embedded-skills/COMPARE_AND_DEPLOY_RESEARCH.md`
- `docs/version-differences/PG_ROLES_PERMISSIONS_SECURITY.md` if role/security semantics change
- `docs/version-differences/PG_DATABASE_OBJECTS.md` if object diff or script semantics change
- `.github/copilot-instructions.md` if durable rules change

## Required Test Mindset

Add or update tests for:
- destructive vs non-destructive behavior
- publish vs script parity
- owner comparison policy
- target database context propagation
- SQLCMD variable replacement
- object-ordering in generated scripts
- schema-missing behavior
- overloaded function identity behavior

## Do Not Do

- Do not compare overloaded functions by name only in new work.
- Do not silently skip target-only or source-only objects without a warning or policy decision.
- Do not bypass target database validation metadata without documented reason.
- Do not make drop behavior implicit.
- Do not change script ordering casually.

## Skill References

- `docs/features/embedded-skills/COMPARE_AND_DEPLOY_RESEARCH.md`
- `docs/version-differences/PG_ROLES_PERMISSIONS_SECURITY.md`
- `docs/version-differences/PG_DATABASE_OBJECTS.md`
- `src/libs/mbulava.PostgreSql.Dac/Compare/PgSchemaComparer.cs`
- `src/libs/mbulava.PostgreSql.Dac/Compare/PublishScriptGenerator.cs`
- `src/libs/mbulava.PostgreSql.Dac/Publish/ProjectPublisher.cs`
- `src/libs/mbulava.PostgreSql.Dac/Publish/PublishOwnershipPolicyService.cs`
- `src/libs/mbulava.PostgreSql.Dac/Publish/PublishTargetDatabaseContextService.cs`
