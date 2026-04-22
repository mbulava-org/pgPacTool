# Performance and Memory Expert Skill

## Purpose

Use this skill when modifying code paths in pgPacTool that may become performance bottlenecks or
allocation hotspots, especially for large schemas or package/runtime operations.

## When To Use This Skill

Use this skill when changing:
- extraction loops over many schemas/objects
- compare or script generation over large projects
- package creation/inspection paths
- parser/native-loading hot paths
- code that repeats filesystem or database operations heavily

## Repository Defaults

- Correctness comes before optimization.
- Large-schema workflows matter in this repo.
- Native-loading and packaging behavior can affect runtime performance and diagnostics.
- Some paths are explicitly CI/runtime sensitive (Linux, package consumption, Testcontainers).

## Core Operating Rules

1. **Keep correctness first; optimize proven hot paths.**
2. **Avoid repeated work inside large object loops when results can be reused.**
3. **Be cautious with string-heavy and file-heavy workflows.**
4. **Keep diagnostics useful without turning normal paths into log-heavy bottlenecks.**
5. **Prefer measurable improvements over speculative complexity.**
6. **Do not trade away debuggability or correctness for marginal speed gains.**

## Expected Implementation Behavior

### When editing hot paths
- Watch for repeated connections, repeated parsing, repeated allocations, and repeated IO.
- Consider batching or caching when it does not obscure correctness.

### When editing diagnostics
- Keep verbose diagnostics opt-in.
- Avoid flooding default paths with unnecessary output.

### When editing native/package code
- Consider startup cost, load-path checks, and repeated file existence checks.

## Required Documentation Updates

If performance-sensitive behavior or guidance changes, update:
- `docs/features/embedded-skills/ANALYZER_ROADMAP.md` when analyzers should later enforce the pattern
- relevant README/docs if user-visible performance behavior changes

## Required Test Mindset

Add or update tests when optimization changes could affect:
- correctness
- determinism
- runtime/platform behavior

## Do Not Do

- Do not introduce complexity without clear benefit.
- Do not optimize away important diagnostics by default.
- Do not make performance assumptions without considering large-schema scenarios.
- Do not regress package/runtime validation behavior while chasing speed.

## Skill References

- `docs/features/embedded-skills/ANALYZER_ROADMAP.md`
- `.github/copilot-instructions.md`
- `tests/NugetPackage.Tests/README.md`
- `tests/LinuxContainer.Tests/README.md`
