# MSBuild SDK Expert Skill

## Purpose

Use this skill when modifying the MSBuild SDK project experience in pgPacTool, including SDK
props/targets, package layout, build integration, CLI-host-driven compilation, incremental build
behavior, and Visual Studio restore/load guidance.

## When To Use This Skill

Use this skill when changing any of the following:
- `src/sdk/MSBuild.Sdk.PostgreSql/Sdk/Sdk.props`
- `src/sdk/MSBuild.Sdk.PostgreSql/Sdk/Sdk.targets`
- `src/sdk/MSBuild.Sdk.PostgreSql/MSBuild.Sdk.PostgreSql.csproj`
- `src/sdk/MSBuild.Sdk.PostgreSql/README.md`
- SDK task/host entry points under `src/sdk/MSBuild.Sdk.PostgreSql/`
- root README guidance for SDK usage or Visual Studio setup

## Repository Defaults

- SQL files are auto-discovered by convention.
- Pre/post deployment scripts are configured separately from normal SQL inputs.
- SDK builds currently run through the packaged `pgpac compile` CLI host.
- `PgPacFilePath` is the package output contract.
- `ValidateOnBuild` defaults to `true`.
- `PgPacToolVerbose` defaults to `false`.
- Visual Studio support depends on successful SDK restore from an available package feed.

## Core Operating Rules

1. **Preserve SDK-style project behavior.**
   - Keep the project experience close to normal SDK-style .NET projects unless there is a clear,
     documented reason not to.

2. **Keep SQL discovery convention-based and predictable.**
   - Do not make users enumerate ordinary schema files manually.

3. **Treat pre/post deployment scripts as distinct from schema compile inputs.**
   - Do not fold them back into normal `SqlFile` compilation.

4. **Preserve CLI-host-driven compilation as the preferred architecture.**
   - Do not casually switch SDK targets back to direct DAC-library execution.

5. **Keep package layout and target assumptions in sync.**
   - The SDK package layout under `tasks/<tfm>/cli/` is a runtime contract.

6. **Treat incremental build declarations as correctness-critical.**
   - If a new property or item changes output, consider whether it must appear in target `Inputs`
     or otherwise invalidate output.

7. **Keep package output semantics on `PgPacFilePath`, not `TargetPath`.**
   - Preserve normal .NET/Visual Studio project-system behavior.

8. **Keep Visual Studio restore/load guidance accurate.**
   - SDK usability depends on package restore from `nuget.org` or a configured local feed.

## Expected Implementation Behavior

### When editing `Sdk.props`
- Keep default properties minimal, durable, and predictable.
- Preserve normal SDK default items unless there is a strong reason to change them.
- Keep SQL file inclusion and exclusions explicit.

### When editing `Sdk.targets`
- Preserve build lifecycle expectations (`Build`, `Clean`, `Rebuild`).
- Keep CLI host invocation, verbose routing, and validation behavior aligned with docs.
- Keep pre/post deployment item handling separate from `SqlFile`.

### When editing package shape
- Coordinate `MSBuild.Sdk.PostgreSql.csproj` packaging changes with `Sdk.targets` path assumptions.
- Treat layout changes as potentially breaking for all SDK consumers.

### When editing docs
- Keep SDK README and root README aligned on:
  - manual `.csproj` creation
  - restore/feed expectations
  - local feed usage
  - verbose diagnostics
  - `PgPacFilePath` usage

## Required Documentation Updates

If SDK behavior changes, update:
- `docs/features/embedded-skills/MSBUILD_SDK_RESEARCH.md`
- `src/sdk/MSBuild.Sdk.PostgreSql/README.md`
- `README.md`
- `.github/copilot-instructions.md` if durable SDK rules change

## Required Test Mindset

Add or update scenario coverage for:
- restore/load expectations
- SQL discovery behavior
- pre/post deployment item handling
- incremental build correctness
- clean/rebuild behavior
- package layout assumptions
- verbose diagnostic routing

## Do Not Do

- Do not bypass the packaged CLI host without explicit architectural approval.
- Do not casually change `tasks/<tfm>/cli/` layout.
- Do not misuse `TargetPath` for the generated `.pgpac`.
- Do not require users to enumerate ordinary SQL files manually.
- Do not document Visual Studio support without mentioning restore/feed requirements.

## Skill References

- `docs/features/embedded-skills/MSBUILD_SDK_RESEARCH.md`
- `src/sdk/MSBuild.Sdk.PostgreSql/Sdk/Sdk.props`
- `src/sdk/MSBuild.Sdk.PostgreSql/Sdk/Sdk.targets`
- `src/sdk/MSBuild.Sdk.PostgreSql/MSBuild.Sdk.PostgreSql.csproj`
- `src/sdk/MSBuild.Sdk.PostgreSql/README.md`
- `README.md`
