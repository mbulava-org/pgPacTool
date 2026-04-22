# NuGet Packaging and Release Expert Skill

## Purpose

Use this skill when modifying package metadata, package contents, preview-version discipline,
tool/SDK packaging, or package-consumption validation for pgPacTool artifacts.

## When To Use This Skill

Use this skill when changing:
- package metadata (`PackageId`, `Version`, readme/license/repository metadata)
- package structure and included files
- native asset packaging
- global tool packaging
- SDK package layout
- release messaging in README/docs
- package validation tests

## Repository Defaults

The repository ships multiple package surfaces:
- `postgresPacTools`
- `mbulava.PostgreSql.Dac`
- `MSBuild.Sdk.PostgreSql`

Current durable signals:
- version is currently coordinated at `1.0.0-preview8`
- package validation tests are a required quality signal
- native assets are expected in package outputs where relevant
- README/license files are included in packages

## Core Operating Rules

1. **Keep package versioning consistent across shipped surfaces unless divergence is intentional and documented.**
2. **Treat package structure as a product contract, not just a build artifact.**
3. **Preserve native asset inclusion for runtime-dependent packages.**
4. **Use package validation tests when package structure or release behavior changes.**
5. **Keep release messaging and README examples aligned with current preview/stable state.**
6. **Do not ship packaging changes without considering restore, install, and consumption scenarios.**

## Expected Implementation Behavior

### When editing package metadata
- Keep repository/readme/license metadata complete.
- Keep preview/stable messaging aligned across README and package metadata.

### When editing package contents
- Preserve required runtime files, native assets, and SDK/tool structure.
- Treat global tool and SDK package layout as behaviorally significant.

### When editing release docs
- Keep package names, versions, and support statements aligned with actual shipped artifacts.

## Required Documentation Updates

If packaging/release behavior changes, update:
- `README.md`
- package-specific READMEs
- `tests/NugetPackage.Tests/README.md`
- `.github/copilot-instructions.md` if durable packaging rules change

## Required Test Mindset

Add or update tests for:
- package structure/content
- native asset inclusion
- package consumption
- global tool install/use
- SDK package runtime/layout behavior

## Do Not Do

- Do not change package structure without package validation coverage.
- Do not let package versions drift silently across shipped artifacts.
- Do not forget native asset implications for runtime-dependent packages.
- Do not update release messaging without updating the actual package metadata or vice versa.

## Skill References

- `tests/NugetPackage.Tests/README.md`
- `src/postgresPacTools/postgresPacTools.csproj`
- `src/libs/mbulava.PostgreSql.Dac/mbulava.PostgreSql.Dac.csproj`
- `src/sdk/MSBuild.Sdk.PostgreSql/MSBuild.Sdk.PostgreSql.csproj`
- `README.md`
