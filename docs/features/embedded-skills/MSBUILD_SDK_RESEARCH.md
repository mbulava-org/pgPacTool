# Phase 1 / Step C — Research: `msbuild-sdk-expert`

## Purpose

This research document captures the current MSBuild SDK behavior in pgPacTool so the
`msbuild-sdk-expert` embedded skill can be authored from actual SDK props/targets, packaging,
host execution behavior, and README guidance.

## Primary Code Surfaces Reviewed

### SDK build behavior
- `src/sdk/MSBuild.Sdk.PostgreSql/Sdk/Sdk.props`
- `src/sdk/MSBuild.Sdk.PostgreSql/Sdk/Sdk.targets`
- `src/sdk/MSBuild.Sdk.PostgreSql/MSBuild.Sdk.PostgreSql.csproj`

### Task / host surfaces
- `src/sdk/MSBuild.Sdk.PostgreSql/Tasks/CompilePgProject.cs`
- `src/sdk/MSBuild.Sdk.PostgreSql/Program.cs`

### Documentation
- `src/sdk/MSBuild.Sdk.PostgreSql/README.md`
- `README.md`

---

## Current SDK Model

The repository currently uses an SDK package approach where:
- `Sdk.props` establishes project defaults and item discovery
- `Sdk.targets` performs build-time integration
- the packed SDK includes runtime assets under the `tasks/` folder
- the main build path executes the packaged `postgresPacTools` CLI host using `dotnet exec`

This is important: the current preferred architecture is **CLI-host-driven SDK builds**, not
calling the DAC library directly from MSBuild targets.

---

## Current Durable Behavior Observed

### 1. SDK projects are marked as PostgreSQL database projects
`Sdk.props` sets:
- `IsDatabaseProject=true`
- `IsPostgreSqlProject=true`

This gives the project a durable repository-specific identity.

### 2. SDK projects keep normal SDK defaults for IDE experience
`EnableDefaultItems` remains enabled by default.

Durable behavior:
- Visual Studio can continue to show folders/files naturally in Solution Explorer
- database projects stay close to normal SDK-style project-system behavior

### 3. SQL files are auto-discovered by convention
`Sdk.props` auto-includes:
- `**\*.sql`
- `**\*.pgsql`

Excluded by default:
- `bin/**`
- `obj/**`
- `.vs/**`
- normal SDK default excludes via `DefaultItemExcludes`

This is a core SDK behavior and should remain highly predictable.

### 4. Pre/post deployment scripts are removed from normal SQL compilation items
`Sdk.targets` moves configured values from:
- `PreDeploymentScript` -> `PreDeploy`
- `PostDeploymentScript` -> `PostDeploy`

Durable behavior:
- these scripts are not treated as normal schema compile inputs
- they are validated and injected separately later in the compile/publish flow

### 5. Build runs through packaged CLI host
`Sdk.targets` uses:
- `dotnet exec`
- packaged deps/runtimeconfig
- packaged `postgresPacTools.dll`

Durable behavior:
- MSBuild compile path is intentionally aligned with CLI compile behavior
- verbose output is routed through CLI switches

This is reinforced by the SDK README:
- “SDK builds now run through the packaged `pgpac compile` host instead of calling the DAC library directly.”

### 6. Incremental build is input/output based
`CompilePostgreSqlProject` target uses:
- `Inputs="@(SqlFile);@(PreDeploy);@(PostDeploy);$(MSBuildProjectFullPath)"`
- `Outputs="$(PgPacFilePath)"`

Durable behavior:
- SQL files, deployment scripts, and project file changes can invalidate output
- the final package path is the incremental-build output contract

### 7. Build output path is intentionally separate from `TargetPath`
The README explicitly says:
- use `PgPacFilePath` to control package location
- do not override `TargetPath`

Durable behavior:
- database package path is a repo-specific output concept
- IDE / .NET project system behavior remains stable

### 8. Validation is a first-class build switch
Key property:
- `ValidateOnBuild` (default `true`)

Current CLI invocation appends:
- `--skip-validation` when validation is disabled

Durable behavior:
- validation is opt-out, not opt-in

### 9. Verbose diagnostics are a first-class build switch
Key property:
- `PgPacToolVerbose` (default `false`)

Current behavior:
- appends `--verbose` when enabled
- increases MSBuild stdout importance to `high`

Durable behavior:
- normal builds stay quiet enough
- detailed compile diagnostics can be surfaced consistently when needed

### 10. SDK package includes both task assets and CLI host assets
`MSBuild.Sdk.PostgreSql.csproj` packs:
- SDK files under `Sdk/`
- build output under `tasks/`
- runtime dependencies
- CLI host files under `tasks/<tfm>/cli/`

Durable behavior:
- the SDK package is self-contained for its build execution path
- this is why local feed / restore behavior matters for Visual Studio loading

---

## Current Properties and Their Meaning

From `Sdk.props` and README, key durable properties are:
- `DatabaseName`
- `DefaultSchema`
- `OutputFormat`
- `PgPacFileName`
- `PgPacFilePath`
- `ValidateOnBuild`
- `PgPacToolVerbose`
- `EnableDefaultItems`
- `PreDeploymentScript`
- `PostDeploymentScript`

Skill implication:
- these are the canonical SDK authoring knobs
- agents should prefer these over inventing new custom properties unless there is a clear need

---

## Current Build Target Behavior

### Target override strategy
`Sdk.targets` overrides `CoreBuildDependsOn` to route build through:
- `BuildOnlySettings`
- `PrepareForBuild`
- `PreBuildEvent`
- `CompilePostgreSqlProject`
- `PostBuildEvent`

Durable behavior:
- compilation is a build-time target wired into standard build orchestration

### Main compile target
`CompilePostgreSqlProject`:
- logs project/output/format/validation/verbosity information
- executes packaged CLI compile host
- reports success message on completion

### Clean / Rebuild
SDK also defines:
- `Clean` => deletes `$(PgPacFilePath)`
- `Rebuild` => depends on `Clean;Build`

Skill implication:
- database projects are expected to behave like normal SDK-style projects for build lifecycle

---

## Current Documentation Guidance Observed

The SDK README and root README emphasize:
- no separate VS project template installer is required
- create `.csproj` manually or via `pgpac extract`
- Visual Studio requires the SDK package to be restorable from `nuget.org` or a local feed
- for local SDK validation, add a `nuget.config` before opening the project
- run `dotnet restore` if the SDK cannot be resolved

This is durable operational guidance and should be reflected in the skill.

---

## Current Risks and Gaps Found During Research

### 1. There are two compile surfaces, but one is the preferred path
The repository contains:
- `Tasks/CompilePgProject.cs`
- `Program.cs`
- CLI-host-driven `Sdk.targets`

But the current build path in `Sdk.targets` uses the packaged CLI host, not the MSBuild task class.

Risk:
- future changes may update one compile surface and forget the others
- agents may mistakenly wire targets back to the older direct-task path

Skill rule needed:
- preserve CLI-host-driven SDK builds as the preferred architecture unless there is an explicit
  repo-level decision to change it

### 2. Task/host drift is possible
`CompilePgProject.cs` and `Program.cs` still encode compile logic patterns that may diverge from the
CLI compile path.

Risk:
- diagnostics and validation behavior can drift
- package/runtime expectations can drift

Skill rule needed:
- when compile behavior changes, confirm which host is authoritative and keep all surviving entry
  points aligned or intentionally retired

### 3. There are no obvious dedicated SDK tests surfaced in this scan
No dedicated SDK-specific test project appeared in the current search results.

Risk:
- props/targets/packaging regressions may go undetected
- Visual Studio load/restore assumptions are only documented, not strongly enforced by tests

Skill rule needed:
- SDK changes should be backed by scenario tests where practical (restore/build/clean/output path)

### 4. SDK package shape is tightly coupled to output layout
`Sdk.targets` assumes CLI host files exist under:
- `tasks/<tfm>/cli/`

Risk:
- packaging changes can silently break build behavior
- TFM or layout changes require coordinated updates across pack and targets

Skill rule needed:
- package layout changes must be treated as breaking SDK behavior unless carefully coordinated

### 5. `SuppressDependenciesWhenPacking` and manual packaging make this SDK specialized
The SDK package is intentionally curated, not a simple standard library package.

Risk:
- agents may simplify packaging in ways that break restore/build runtime behavior

Skill rule needed:
- preserve the specialized package structure needed by SDK consumers

### 6. Incremental build depends on correct Inputs/Outputs discipline
If new relevant inputs are introduced and not added to `Inputs`, build correctness can drift.

Risk:
- stale `.pgpac` output
- changes not picked up by build

Skill rule needed:
- any new compile-relevant items/properties must be considered for incremental-build invalidation

### 7. Output path semantics are easy to break
The docs explicitly warn against using `TargetPath` for the database package.

Risk:
- IDE/project system behavior may degrade if agents try to treat `.pgpac` like normal assembly output

Skill rule needed:
- keep `PgPacFilePath` as the package path contract

### 8. Visual Studio experience is restore-dependent
This SDK only works cleanly when the package can be restored before VS evaluates the project.

Risk:
- contributors may think the SDK is broken when it is actually a feed/restore issue

Skill rule needed:
- docs and troubleshooting should always mention restore/feed prerequisites for SDK load issues

---

## Recommended Durable Rules for `msbuild-sdk-expert`

### Core rules
1. Preserve SDK-style project behavior; do not make the project system feel non-standard unless required.
2. Keep SQL discovery convention-based and predictable.
3. Treat pre/post deployment scripts as distinct from schema compile inputs.
4. Preserve CLI-host-driven build execution as the preferred architecture.
5. Keep SDK package layout and target expectations in sync.
6. Treat incremental build inputs/outputs as correctness-critical.
7. Keep package path behavior on `PgPacFilePath`, not `TargetPath`.
8. Keep Visual Studio restore/load guidance accurate whenever packaging or feed behavior changes.
9. Keep verbose diagnostics opt-in but easy to enable.
10. Back SDK behavior changes with restore/build/package scenario validation.

### Specific rules this skill should encode
- Do not remove `EnableDefaultItems` support without accounting for Solution Explorer experience.
- Do not fold pre/post deployment scripts back into `SqlFile` compilation.
- Do not bypass the packaged CLI host without explicit architectural approval.
- Do not change the `tasks/<tfm>/cli/` package layout casually.
- Add new compile inputs to target `Inputs` when they affect output.
- Keep README guidance aligned with actual package/restore behavior.

---

## Analyzer Opportunities from This Research

Potential future checks:
- detect SDK target changes that stop using the CLI host path
- detect compile-relevant properties/items not reflected in incremental inputs
- detect output-path misuse (for example shifting logic to `TargetPath`)
- detect package layout changes without corresponding target updates

---

## What the Skill Should Reference

When authored, `msbuild-sdk-expert` should reference:
- `docs/features/embedded-skills/MSBUILD_SDK_RESEARCH.md`
- `src/sdk/MSBuild.Sdk.PostgreSql/Sdk/Sdk.props`
- `src/sdk/MSBuild.Sdk.PostgreSql/Sdk/Sdk.targets`
- `src/sdk/MSBuild.Sdk.PostgreSql/MSBuild.Sdk.PostgreSql.csproj`
- `src/sdk/MSBuild.Sdk.PostgreSql/README.md`
- `README.md`

---

## Follow-On Work After This Research

1. author `.github/skills/msbuild-sdk-expert/SKILL.md`
2. add a short skill `README.md`
3. link the skill from `.github/copilot-instructions.md` and `docs/README.md`
4. create follow-up issues or analyzer candidates for:
   - SDK package layout validation
   - incremental-input drift detection
   - CLI-host path enforcement
   - restore/load scenario coverage

---

*Last updated: Current Session*
