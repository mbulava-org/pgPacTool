# Issue #7: Task Checklist

## ? Setup (Complete)
- [x] Created feature branch: `feature/issue-7-fix-privilege-extraction`
- [x] Created implementation guide: `ISSUE_7_GUIDE.md`
- [x] Ready to start coding

---

## ?? Implementation Tasks

### Day 1: Core Fix (4-6 hours)

#### Morning (2-3 hours)
- [ ] **Task 1.1:** Uncomment line 133 in `PgProjectExtractor.cs`
  - File: `src/libs/mbulava.PostgreSql.Dac/Extract/PgProjectExtractor.cs`
  - Change: Uncomment `Privileges = await ExtractPrivilegesAsync(...)`
  - Test: `dotnet build` - should compile without errors
  
- [ ] **Task 1.2:** Add missing EXECUTE privilege code
  - File: `src/libs/mbulava.PostgreSql.Dac/Extract/PgProjectExtractor.cs`
  - Method: `MapPrivilege(char ch)` (around line 178)
  - Add: `'X' => "EXECUTE"`
  - Test: `dotnet build`

- [ ] **Task 1.3:** Initial smoke test
  - Run existing tests: `dotnet test`
  - Check if privilege extraction works at all
  - Document any errors found

#### Afternoon (2-3 hours)
- [ ] **Task 1.4:** Create test file
  - Location: `tests/mbulava.PostgreSql.Dac.Tests/Extract/PrivilegeExtractionTests.cs`
  - Copy template from `ISSUE_7_GUIDE.md`
  - Verify it compiles

- [ ] **Task 1.5:** Implement first 3 tests
  - [ ] Test: `ExtractSchemaPrivileges_WithUsageGrant_ExtractsCorrectly`
  - [ ] Test: `ExtractSchemaPrivileges_WithCreateGrant_ExtractsCorrectly`
  - [ ] Test: `ExtractSchemaPrivileges_WithGrantOption_SetsIsGrantableTrue`
  - Run: `dotnet test --filter "FullyQualifiedName~PrivilegeExtraction"`

---

### Day 2: Testing & Edge Cases (4-6 hours)

#### Morning (2-3 hours)
- [ ] **Task 2.1:** Implement remaining 5 tests
  - [ ] Test: `ExtractSchemaPrivileges_PublicGrant_RecognizesPublic`
  - [ ] Test: `ExtractSchemaPrivileges_NoExplicitGrants_ReturnsEmptyList`
  - [ ] Test: `ExtractSchemaPrivileges_MultiplePrivileges_ExtractsAll`
  - [ ] Test: `ExtractSchemaPrivileges_MultipleGrantees_ExtractsAll`
  - [ ] Test: Add any custom test for found edge cases

- [ ] **Task 2.2:** Debug and fix failing tests
  - Review error messages
  - Fix ACL parsing issues
  - Re-run tests until all pass
  - Target: 100% test pass rate

#### Afternoon (2-3 hours)
- [ ] **Task 2.3:** Test with real PostgreSQL data
  - Connect to test database
  - Extract schema with various privileges
  - Verify extracted data is correct
  - Document any issues

- [ ] **Task 2.4:** Code cleanup and documentation
  - Add XML comments to methods
  - Clean up any TODO comments
  - Update README if needed

---

### Day 3: Polish & PR (2-4 hours)

#### Morning (1-2 hours)
- [ ] **Task 3.1:** Final testing
  - Run all tests: `dotnet test`
  - Run with coverage: `dotnet test /p:CollectCoverage=true`
  - Verify no regressions
  - Check code coverage ? 90%

- [ ] **Task 3.2:** Code review prep
  - Self-review all changes
  - Check for console.log/debug statements
  - Verify naming conventions
  - Check for TODOs

#### Afternoon (1-2 hours)
- [ ] **Task 3.3:** Commit and push
  - Stage changes: `git add -A`
  - Commit: `git commit -m "fix: Issue #7 - Fix privilege extraction ACL parsing"`
  - Push: `git push -u origin feature/issue-7-fix-privilege-extraction`

- [ ] **Task 3.4:** Create Pull Request
  - Open PR on GitHub
  - Fill in PR template
  - Link to Issue #7
  - Request reviewers
  - Add labels: `bug`, `high-priority`, `blocker`

---

## ?? Progress Tracking

### Completion Percentage
- Setup: 100% ?
- Day 1 Tasks: 0%
- Day 2 Tasks: 0%
- Day 3 Tasks: 0%
- **Overall: 25%** (setup complete)

### Time Tracking
| Day | Planned | Actual | Notes |
|-----|---------|--------|-------|
| Day 1 | 4-6h | - | Core implementation |
| Day 2 | 4-6h | - | Testing & fixes |
| Day 3 | 2-4h | - | Polish & PR |
| **Total** | **10-16h** | **-** | **~2-3 days** |

---

## ?? Success Criteria

Before marking as complete, verify:
- [ ] All 8+ tests pass
- [ ] Schema privileges extract correctly
- [ ] Grant options detected (uppercase letters)
- [ ] PUBLIC grants recognized (empty grantee)
- [ ] NULL ACL handled (returns empty list)
- [ ] Code coverage ? 90% on new code
- [ ] No regressions in existing tests
- [ ] PR approved by reviewers
- [ ] Merged to main branch

---

## ?? Quick Commands

```bash
# Build
dotnet build

# Run all tests
dotnet test

# Run privilege tests only
dotnet test --filter "FullyQualifiedName~PrivilegeExtraction"

# Run with detailed output
dotnet test --logger "console;verbosity=detailed"

# Run with coverage
dotnet test /p:CollectCoverage=true

# Check current branch
git branch --show-current

# Commit changes
git add -A
git commit -m "fix: Issue #7 - Fix privilege extraction"

# Push to remote
git push -u origin feature/issue-7-fix-privilege-extraction
```

---

## ?? Notes

Add notes as you work:
- 

---

**Status:** Ready to Start ??  
**Next Action:** Begin Task 1.1 - Uncomment line 133
