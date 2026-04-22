# Native libpg_query Integration Expert Skill

## Purpose

Use this skill when modifying native libpg_query loading, versioned parser support, runtime asset
layout, native diagnostics, or package/runtime validation for Npgquery.

## When To Use This Skill

Use this skill when changing:
- `src/libs/Npgquery/`
- native loader/search-path logic
- runtime asset layout under `runtimes/`
- package validation for native assets
- multi-version parser support docs/tests
- Linux container compatibility behavior

## Repository Defaults

- Native libraries are version-specific by `PostgreSqlVersion`.
- Runtime-specific native assets are preferred over base-directory fallback.
- Diagnostics are a first-class part of the loader behavior.
- Linux behavior is important enough to justify dedicated Linux container tests.
- Package validation expects Windows, Linux, and macOS native assets.

## Core Operating Rules

1. **Keep runtime asset layout and loader expectations aligned.**
2. **Preserve version-specific native library identity.**
3. **Preserve detailed diagnostics for native loading failures.**
4. **Treat Linux/container validation as essential for native changes.**
5. **Update native assets, docs, and tests together when parser-version support changes.**
6. **Do not assume fallback loading alone is sufficient.**

## Expected Implementation Behavior

### When editing loader logic
- Preserve runtime-specific search path behavior.
- Preserve diagnostic logging and `PrintDiagnostics()` usefulness.
- Keep thread-safe caching/initialization semantics intact.

### When editing version support
- Align enum/version declarations, packaged assets, docs, and tests.
- Treat parser version additions as repo-wide changes.

### When editing package/runtime layout
- Keep package validation expectations in sync with actual runtime search paths.
- Verify Linux/macOS/Windows packaging assumptions together.

## Required Documentation Updates

If native integration behavior changes, update:
- `docs/features/embedded-skills/NATIVE_LIBPG_QUERY_RESEARCH.md`
- `docs/features/multi-version-support/README.md`
- `src/libs/Npgquery/PACKAGE_README.md`
- `.github/copilot-instructions.md` if durable native-loading rules change

## Required Test Mindset

Add or update tests for:
- version availability
- runtime-specific path loading
- diagnostics behavior
- package validation
- Linux container compatibility
- real parser load/use behavior

## Do Not Do

- Do not change runtime asset layout casually.
- Do not remove diagnostics that make native failures explainable.
- Do not add parser versions without corresponding native assets.
- Do not assume success on Windows means success on Linux/macOS.

## Skill References

- `docs/features/embedded-skills/NATIVE_LIBPG_QUERY_RESEARCH.md`
- `docs/features/multi-version-support/README.md`
- `src/libs/Npgquery/Npgquery/Native/NativeLibraryLoader.cs`
- `src/libs/Npgquery/PACKAGE_README.md`
- `tests/NpgqueryExtended.Tests/`
- `tests/LinuxContainer.Tests/README.md`
- `tests/NugetPackage.Tests/README.md`
