# QA Summary for AST Based Compilation PR

**Commit:** b51e9b9 (latest as of DEV-369 resolution)

## Overall Status

- **Local Environment:** All tests pass locally.
- **CI Environment:** Expected to pass after fixes in DEV-369. No live CI log was accessible, but all known failure causes have been addressed.

## Issues Resolved (DEV-369 investigation)

1. **Native Memory Crashes (DEV-371):** Previously fixed by changing native memory handling. No regression found.
2. **Test Discovery (DEV-372):** CI workflow correctly excludes `LinuxContainer.Tests` and `NugetPackage.Tests` from the main test run. `NugetPackage.Tests` runs standalone to avoid MSB4166 crashes.
3. **SQL Generation Edge Cases (DEV-373):** CHECK and FOREIGN KEY constraint SQL generation fixed in b51e9b9. All 20 `AstSqlGeneration`-category tests pass. CI hard-gate step added.
4. **Protobuf Deparse Crash (regression in b51e9b9, fixed in DEV-369):** Commit b51e9b9 accidentally removed `[Fact(Skip=...)]` from 5 protobuf-dependent tests that crash the Linux test host process (`pg_query_deparse_protobuf` bug, Issue #36). These Skip attributes have been restored:
   - `AstToSql_ValidParseTree_ReturnsQuery`
   - `RoundTripTest_ValidQuery_SucceedsWithNonNullQuery`
   - `RoundTripTest_InvalidQuery_ReturnsFalseAndNull`
   - `DeparseAsync_ValidAst_ReturnsQuery`
   - `QuickDeparseAsync_StaticMethod_Works`

## Known Limitations

- `pg_query_deparse_protobuf` is broken/crashes on Linux (upstream Issue #36). The 5 tests above must remain skipped until that upstream issue is resolved.
- A `catch (Exception ex)` guard was added in `DeparseProtobuf`, but native process crashes (SIGABRT/SIGSEGV from C code) are not catchable in .NET managed code.

## CI Workflow Summary

- Main test run: `dotnet test --filter "FullyQualifiedName!~LinuxContainer.Tests&FullyQualifiedName!~NugetPackage.Tests"`
- Hard gate before coverage: `--filter "Category=AstSqlGeneration"` (must pass for build to succeed)
- `NugetPackage.Tests` runs standalone in a separate step
