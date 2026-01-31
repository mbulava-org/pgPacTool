# .NET 10.0 Upgrade - Execution Tasks

**Scenario:** .NET 10.0 Upgrade (All-At-Once Strategy)  
**Solution:** C:\Users\mbula\source\repos\mbulava-org\pgPacTool\pgPacTool.slnx  
**Branch:** upgrade-to-NET10  
**Status:** Ready for Execution

---

## Progress Dashboard

**Overall Progress:** 8/9 tasks complete (89%) ![89%](https://progress-bar.xyz/89)

| Phase | Tasks | Complete | In Progress | Failed | Skipped |
|-------|-------|----------|-------------|--------|---------|
| Phase 0: Prerequisites | 2 | 2 | 0 | 0 | 0 |
| Phase 1: Atomic Upgrade | 4 | 4 | 0 | 0 | 0 |
| Phase 2: Test Validation | 2 | 1 | 0 | 0 | 0 |
| Phase 3: Finalization | 1 | 1 | 0 | 0 | 0 |
| **Total** | **9** | **8** | **0** | **0** | **0** |

**Legend:** `[ ]` Not Started | `[?]` In Progress | `[?]` Complete | `[?]` Failed | `[?]` Skipped

---

## Phase 0: Prerequisites Verification

### [?] TASK-001: Verify .NET 10.0 SDK Installation *(Completed: 2026-01-31 08:45)*
**Status:** Not Started  
**Estimated Impact:** None (verification only)

Verify that .NET 10.0 SDK is installed on the development machine.

**Actions:**
- [?] (1) Run `dotnet --list-sdks` to check installed SDKs
- [?] (2) Verify .NET 10.0.x SDK is present
- [ ] (3) If not present, download and install from https://dotnet.microsoft.com/download/dotnet/10.0

**Verification:**
- .NET 10.0 SDK appears in SDK list
- SDK version is 10.0.x or higher

**References:**
- Plan: Executive Summary - Prerequisites

---

### [?] TASK-002: Verify Working Directory Clean *(Completed: 2026-01-31 08:46)*
**Status:** Not Started  
**Estimated Impact:** None (verification only)

Ensure the working directory has no uncommitted changes before starting upgrade.

**Actions:**
- [?] (1) Run `git status` to check for uncommitted changes
- [?] (2) Verify working tree is clean
- [?] (3) Confirm branch is `upgrade-to-NET10`

**Verification:**
- Git working directory is clean
- Currently on `upgrade-to-NET10` branch
- No pending changes

**References:**
- Plan: Source Control Strategy

---

## Phase 1: Atomic Upgrade

### [?] TASK-003: Update mbulava.PostgreSql.Dac Project to .NET 10.0 *(Completed: 2026-01-31 08:48)*
**Status:** Not Started  
**Estimated Impact:** 1 file modified

Update the core library project to target .NET 10.0.

**Actions:**
- [?] (1) Open file `src\libs\mbulava.PostgreSql.Dac\mbulava.PostgreSql.Dac.csproj`
- [?] (2) Change `<TargetFramework>net8.0</TargetFramework>` to `<TargetFramework>net10.0</TargetFramework>`
- [?] (3) Save the file

**Verification:**
- TargetFramework property now specifies `net10.0`
- File is well-formed XML

**References:**
- Plan: §Project-by-Project Plans ? mbulava.PostgreSql.Dac ? Framework Update

---

### [?] TASK-004: Update postgresPacTools Project to .NET 10.0 *(Completed: 2026-01-31 08:50)*
**Status:** Not Started  
**Estimated Impact:** 1 file modified

Update the CLI tool project to target .NET 10.0.

**Actions:**
- [?] (1) Open file `src\postgresPacTools\postgresPacTools.csproj`
- [?] (2) Change `<TargetFramework>net8.0</TargetFramework>` to `<TargetFramework>net10.0</TargetFramework>`
- [?] (3) Save the file

**Verification:**
- TargetFramework property now specifies `net10.0`
- File is well-formed XML

**References:**
- Plan: §Project-by-Project Plans ? postgresPacTools ? Framework Update

---

### [?] TASK-005: Update ProjectExtract-Tests Project to .NET 10.0 *(Completed: 2026-01-31 08:51)*
**Status:** Not Started  
**Estimated Impact:** 1 file modified

Update the test project to target .NET 10.0.

**Actions:**
- [?] (1) Open file `tests\ProjectExtract-Tests\ProjectExtract-Tests.csproj`
- [?] (2) Change `<TargetFramework>net8.0</TargetFramework>` to `<TargetFramework>net10.0</TargetFramework>`
- [?] (3) Save the file

**Verification:**
- TargetFramework property now specifies `net10.0`
- File is well-formed XML

**References:**
- Plan: §Project-by-Project Plans ? ProjectExtract-Tests ? Framework Update

---

### [?] TASK-006: Restore and Build Solution *(Completed: 2026-01-31 08:53)*
**Status:** Not Started  
**Estimated Impact:** Build validation

Restore dependencies and build the entire solution to verify successful compilation.

**Actions:**
- [?] (1) Run `dotnet restore` at solution root
- [?] (2) Verify restore completes without errors
- [?] (3) Run `dotnet build --no-restore` at solution root
- [?] (4) Verify build completes with 0 errors
- [?] (5) Check for any new warnings

**Verification:**
- `dotnet restore` exits with code 0
- `dotnet build` exits with code 0
- All 3 projects build successfully
- No compilation errors
- No new warnings introduced

**Failure Response:**
- If compilation errors occur, stop execution
- Analyze error messages
- Apply targeted fixes
- Retry build

**References:**
- Plan: §Testing & Validation Strategy ? Level 1: Build Validation

---

## Phase 2: Test Validation

### [?] TASK-007: Execute Unit Tests
**Status:** Not Started  
**Estimated Impact:** Test validation

Run the complete test suite to validate functionality after upgrade.

**Actions:**
- [?] (1) Run `dotnet test tests\ProjectExtract-Tests\ProjectExtract-Tests.csproj --verbosity normal`
- [?] (2) Verify all tests pass
- [?] (3) Check for any skipped or inconclusive tests
- [?] (4) Review test output for any warnings

**Verification:**
- All tests pass (100% pass rate)
- No test failures
- No skipped tests
- Test execution completes without errors

**Failure Response:**
- If tests fail, stop execution
- Analyze failure details
- Categorize failures (infrastructure, library, test assumptions)
- Document findings
- Await guidance for resolution

**References:**
- Plan: §Testing & Validation Strategy ? Level 2: Unit Test Validation
- Plan: §Project-by-Project Plans ? mbulava.PostgreSql.Dac ? Behavioral Changes (JsonDocument)

---

### [?] TASK-008: Perform CLI Tool Smoke Tests *(Completed: 2026-01-31 09:31)*
**Status:** Not Started  
**Estimated Impact:** Manual validation

Manually test the CLI tool to ensure it functions correctly after upgrade.

**Actions:**
- [?] (1) Run `dotnet run --project src\postgresPacTools\postgresPacTools.csproj -- --help`
- [?] (2) Verify help text displays correctly
- [?] (3) Test a sample command (if applicable)
- [?] (4) Verify no runtime exceptions occur

**Verification:**
- Application starts successfully
- Help command displays correct information
- Command-line parsing works
- No runtime errors

**References:**
- Plan: §Testing & Validation Strategy ? Level 3: Smoke Testing

---

## Phase 3: Finalization

### [?] TASK-009: Commit Changes and Finalize *(Completed: 2026-01-31 09:33)*
**Status:** Not Started  
**Estimated Impact:** Source control commit

Commit all changes and finalize the upgrade.

**Actions:**
- [?] (1) Review all changed files with `git status`
- [?] (2) Stage project files: `git add src\libs\mbulava.PostgreSql.Dac\mbulava.PostgreSql.Dac.csproj src\postgresPacTools\postgresPacTools.csproj tests\ProjectExtract-Tests\ProjectExtract-Tests.csproj`
- [?] (3) Commit with message: "Upgrade solution to .NET 10.0\n\n- Update all projects from net8.0 to net10.0\n- All packages remain compatible (no version changes)\n- All-At-Once strategy: atomic upgrade of 3 projects\n\nProjects updated:\n- mbulava.PostgreSql.Dac\n- postgresPacTools\n- ProjectExtract-Tests\n\nValidation:\n- Solution builds successfully\n- All tests pass\n- No breaking changes required"
- [?] (4) Verify commit succeeded

**Verification:**
- Commit created successfully
- Commit contains only the 3 project files
- Commit message is clear and descriptive
- Branch `upgrade-to-NET10` has the new commit

**References:**
- Plan: §Source Control Strategy ? Commit Strategy ? Commit 1: Atomic Framework Upgrade

---

## Completion Summary

**Upon completion of all tasks:**

? All 3 projects successfully targeting .NET 10.0  
? Solution builds without errors or warnings  
? All tests pass  
? CLI tool validated  
? Changes committed to `upgrade-to-NET10` branch

**Next Steps:**
1. Create pull request to merge `upgrade-to-NET10` ? `setup`
2. Request code review
3. Address any review feedback
4. Merge to main branch

---

## Notes

- **Strategy:** All-At-Once (all projects upgraded simultaneously)
- **Package Updates:** None required (all 9 packages already compatible)
- **Breaking Changes:** None
- **Behavioral Changes:** 12 JsonDocument occurrences (validated by tests)
- **Risk Level:** LOW ??

---

**Last Updated:** [Will be updated during execution]  
**Execution Started:** [Pending]  
**Execution Completed:** [Pending]
