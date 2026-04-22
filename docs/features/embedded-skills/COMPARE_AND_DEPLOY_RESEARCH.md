# Phase 1 / Step A — Research: `compare-and-deploy-expert`

## Purpose

This research document captures the current compare/deploy behavior in pgPacTool so the
`compare-and-deploy-expert` embedded skill can be authored from actual repository behavior,
existing tests, and known gaps.

## Primary Code Surfaces Reviewed

### Compare and script generation
- `src/libs/mbulava.PostgreSql.Dac/Compare/PgSchemaComparer.cs`
- `src/libs/mbulava.PostgreSql.Dac/Compare/PublishScriptGenerator.cs`
- `src/libs/mbulava.PostgreSql.Dac/Compare/CompareOptions.cs`
- `src/libs/mbulava.PostgreSql.Dac/Models/Compare.cs`

### Publish orchestration
- `src/libs/mbulava.PostgreSql.Dac/Publish/ProjectPublisher.cs`
- `src/libs/mbulava.PostgreSql.Dac/Publish/PublishOwnershipPolicyService.cs`
- `src/libs/mbulava.PostgreSql.Dac/Publish/PublishTargetDatabaseContextService.cs`
- `src/libs/mbulava.PostgreSql.Dac/Deployment/PrePostDeploymentScriptManager.cs`
- `src/libs/mbulava.PostgreSql.Dac/Models/Deployment.cs`

### Tests reviewed
- `tests/mbulava.PostgreSql.Dac.Tests/Compare/PublishScriptGeneratorTests.cs`
- `tests/mbulava.PostgreSql.Dac.Tests/Publish/PublishOwnershipPolicyServiceTests.cs`
- `tests/mbulava.PostgreSql.Dac.Tests/Publish/PublishTargetDatabaseContextServiceTests.cs`

---

## Current Compare and Deploy Pipeline

`ProjectPublisher.PublishAsync(...)` currently follows this sequence:

1. apply target database context
2. apply ownership policy
3. validate explicit source owners against source roles
4. compile source project
5. extract target database project
6. compare source schemas to target schemas
7. validate and load pre/post deployment scripts
8. generate SQL deployment script
9. optionally persist script to disk
10. optionally execute script against target database

This sequence is the current operational backbone for compare/deploy behavior.

---

## Durable Rules Observed in Current Implementation

### 1. Publish context is database-scoped before compare starts
`PublishTargetDatabaseContextService` mutates publish state before extraction/comparison.

Current durable behavior:
- `SourceDatabase` defaults to the source project database name
- `TargetDatabase` defaults from the target connection string if not explicitly provided
- source project `DatabaseName` is overwritten to the effective target database
- SQLCMD variables are injected for:
  - `DatabaseName`
  - `TargetDatabase`
  - `SourceDatabase`

### 2. Ownership enforcement is policy-driven
`PublishOwnershipPolicyService.Apply()` sets:
- `CompareOptions.CompareOwners = true` when `OwnershipMode.Enforce`
- `CompareOptions.CompareOwners = false` when `OwnershipMode.Ignore`

It also validates that any explicit owner mentioned in source objects exists in the source role set.

### 3. Compare is schema-by-schema
`ProjectPublisher` only compares schemas present in the source project.

Current behavior:
- if a source schema does not exist in target, a warning is emitted
- the schema is currently skipped rather than created
- only source-side schemas are iterated

### 4. Script generation uses a fixed dependency order
`PublishScriptGenerator.Generate(...)` emits schema changes in this order:
1. types
2. sequences
3. tables
4. views
5. functions
6. triggers

Pre-deployment scripts are emitted before this order.
Post-deployment scripts are emitted after it.

### 5. Transaction wrapping is global
When `PublishOptions.Transactional` is true:
- script starts with `BEGIN;`
- script ends with `COMMIT;`

### 6. Drop behavior is opt-in
Objects present in target but absent in source are only dropped when:
- `PublishOptions.DropObjectsNotInSource == true`

### 7. SQLCMD variable replacement is final-pass script processing
Variables are applied after the full script is assembled.

### 8. Script metadata includes target database validation
When `TargetDatabase` is present, the generated script emits a validation block that raises
an exception if `current_database()` does not match `$(TargetDatabase)`.

### 9. Pre/post deployment scripts are validated and ordered
`PrePostDeploymentScriptManager` currently enforces:
- file existence
- duplicate-order detection within the same deployment script type
- stable ordering by `Order`
- warnings for nested transaction control inside transactional scripts
- warnings for unreplaced SQLCMD variables

---

## Object-Level Compare Semantics Currently Implemented

### Schemas
Compared for:
- owner changes
- privileges
- contained objects

Current gap:
- missing target schema creation is not yet handled during publish

### Tables
Compared for:
- owner changes
- full definition string difference
- column diffs
- constraint diffs
- index diffs
- privilege diffs

Current table script behavior:
- missing table in target => emit original `CREATE TABLE`
- extra table in target => drop only if `DropObjectsNotInSource`
- changed columns => emit `ALTER TABLE` actions
- changed constraints => drop/add pattern
- changed indexes => drop/recreate pattern

### Types
Compared for:
- type kind changes
- owner changes
- full definition changes
- enum label changes
- composite attribute changes
- privilege changes

Current script behavior:
- type changes use drop + recreate
- missing type in target => create
- extra type in target => drop only if allowed

### Sequences
Compared for:
- owner changes when enabled
- option-level differences gated by `CompareOptions`
- privilege changes

Current durable behavior:
- `START` is not compared by default (`CompareSequenceStart = false`)
- option changes emit `ALTER SEQUENCE` statements per changed option

### Views
Compared for:
- owner changes
- materialized flag changes
- definition changes
- privilege changes

Current script behavior:
- regular views use `CREATE OR REPLACE`
- materialized views use drop + recreate on definition change

### Functions
Compared for:
- owner changes
- definition changes
- privilege changes

Current script behavior:
- missing/changed functions emit source definition directly
- extra target functions are dropped only when drop mode is enabled

### Triggers
Compared for:
- owner changes
- definition changes
- identity by `(TriggerName, TableName)`

Current script behavior:
- changed triggers use drop + recreate

### Privileges
Compared as explicit ACL differences only:
- missing in target => GRANT
- extra in target => REVOKE

---

## Important Gaps and Risks Found During Research

These are the most important things the skill should guard against.

### 1. Function identity is name-only in compare
`CompareFunctions(...)` matches functions by `Name` only.

Risk:
- overloaded functions are not handled correctly
- this conflicts with the PostgreSQL object rules documented elsewhere in the repo

Skill rule needed:
- functions/procedures must be compared by full qualified signature, not bare name

### 2. Missing source schemas are warned and skipped
Current publish behavior warns when a source schema is absent in target but does not create it.

Risk:
- incomplete deployment
- downstream object creation may fail or be silently absent from the generated script

Skill rule needed:
- missing target schemas must be treated as first-class deployment work, not skipped silently

### 3. Compare is still largely string-based
Many object definitions are compared by raw SQL text:
- tables
- views
- functions
- triggers
- indexes
- constraints

Risk:
- formatting-only changes may look semantic
- semantic differences may be mixed with presentation differences

Skill rule needed:
- normalize non-semantic formatting only; never erase semantic clauses
- prefer structured compare when structured semantics matter

### 4. Schema-level target extras are not fully surfaced
`ProjectPublisher` iterates source schemas only.

Risk:
- target-only schemas and their objects are not comprehensively represented at orchestration level
- drop reporting may be incomplete for whole-schema divergence

Skill rule needed:
- deployment/report logic must explicitly account for target-only schema surfaces

### 5. Execution is all-at-once script execution
`ExecuteScriptAsync` sends the whole script as one command.

Risk:
- harder troubleshooting for partial failures
- provider/platform limitations may be harder to isolate
- very large scripts may be more fragile

Skill rule needed:
- diagnostics and failure reporting should preserve script file output and target context
- future improvements may need statement-batching or richer execution telemetry

### 6. Backup flags exist but are not implemented here
`PublishOptions` contains:
- `BackupBeforeDeployment`
- `BackupPath`

But publish flow does not currently perform backup work.

Risk:
- caller may assume safety behavior that does not happen

Skill rule needed:
- do not imply backup protection exists unless actually implemented in the publish pipeline

### 7. Owner validation is source-role-based only
Explicit owner validation checks that owner names exist in source project roles.

Risk:
- no provider/platform awareness yet
- Azure/AWS reserved-role behavior is not enforced here

Skill rule needed:
- ownership enforcement must align with provider/platform constraints once cross-cloud platform
  skill work is added

### 8. Drop semantics are object-type-specific and incomplete
Current object-drop counting/reporting includes only some object kinds.

Risk:
- deployment reports may undercount or misclassify destructive changes

Skill rule needed:
- destructive changes must be surfaced explicitly and consistently across all supported object types

---

## Compare Options That Matter for the Skill

From `CompareOptions`:
- `CompareOwners`
- `ComparePrivileges`
- `CompareSequenceStart` (default false)
- `CompareSequenceIncrement`
- `CompareSequenceMinValue`
- `CompareSequenceMaxValue`
- `CompareSequenceCache`
- `CompareSequenceCycle`
- `CompareColumns`
- `CompareConstraints`
- `CompareIndexes`
- `CompareEnumLabels`
- `CompareCompositeAttributes`

Skill implication:
- the skill must document that compare/deploy is policy-driven and not every difference is meant to
  be acted on by default
- sequence reseeding is intentionally conservative

---

## Current Test Coverage Signals

Existing tests confirm:
- script header/comment behavior
- transaction wrapping
- add/drop/alter column script generation
- view/function/trigger/type/sequence script generation
- pre/post deployment script inclusion
- SQLCMD replacement
- target database validation block generation
- ownership-mode toggling
- target database context rewriting

Current gaps in visible tests:
- overloaded function identity
- missing schema creation behavior
- destructive-change warning/report behavior
- publish vs script parity at orchestration level
- target-only schema/object reporting completeness
- provider-specific deploy constraints

---

## Recommended Durable Rules for `compare-and-deploy-expert`

### Core rules
1. Treat compare semantics as **deployment semantics**, not just string differences.
2. Never hide destructive operations behind neutral wording.
3. Use policy toggles intentionally; do not silently override them.
4. Preserve dependency order in generated scripts.
5. Keep publish and script-generation behavior aligned.
6. Validate publish context before extraction and before execution.
7. Require explicit handling for target-only objects when drop mode is enabled.
8. Treat ownership handling as policy- and platform-sensitive.
9. Prefer durable warnings when behavior is incomplete (for example, skipped schema creation).
10. Update docs and tests whenever compare semantics change.

### Specific rules this skill should encode
- Function identity must be signature-based.
- Missing target schemas must not be silently skipped in long-term behavior.
- Materialized view changes require drop + recreate semantics.
- Sequence `START` comparison should remain opt-in unless a deliberate reseeding policy is chosen.
- Pre/post deployment scripts must be validated before merge into the final script.
- `DropObjectsNotInSource` is a destructive switch and must be treated as such in docs/tests.
- Target-database validation metadata should remain in generated scripts when a target is known.

---

## What the Skill Should Reference

When authored, `compare-and-deploy-expert` should reference:
- `docs/version-differences/PG_ROLES_PERMISSIONS_SECURITY.md`
- `docs/version-differences/PG_DATABASE_OBJECTS.md`
- `docs/features/embedded-skills/COMPARE_AND_DEPLOY_RESEARCH.md`
- `src/libs/mbulava.PostgreSql.Dac/Compare/PgSchemaComparer.cs`
- `src/libs/mbulava.PostgreSql.Dac/Compare/PublishScriptGenerator.cs`
- `src/libs/mbulava.PostgreSql.Dac/Publish/ProjectPublisher.cs`

---

## Follow-On Work After This Research

1. author `.github/skills/compare-and-deploy-expert/SKILL.md`
2. add a short skill `README.md`
3. link the skill from `.github/copilot-instructions.md` and `docs/README.md`
4. create follow-up issues or analyzer candidates for:
   - overloaded function identity
   - missing schema creation during publish
   - destructive change reporting
   - publish/script parity validation

---

*Last updated: Current Session*
