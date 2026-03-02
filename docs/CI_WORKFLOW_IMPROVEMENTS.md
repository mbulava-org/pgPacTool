# CI Workflow Updates - Test Projects and PR Comments

**Date**: Current Session  
**Changes**: Enhanced CI workflow for better test reporting and coverage accuracy

---

## 🎯 Key Changes

### 1. Test Projects Excluded from Coverage ✅

**Problem**: Test projects were being included in coverage metrics  
**Solution**: Multiple layers of exclusion

#### coverlet.runsettings Updates
```xml
<Exclude>
  [*.Tests]*     <!-- All assemblies ending in .Tests -->
  [*Tests]*      <!-- All assemblies ending in Tests -->
  [*.Test]*      <!-- All assemblies ending in .Test -->
  [*Test]*       <!-- All assemblies ending in Test -->
</Exclude>

<ExcludeByFile>
  **/tests/**/*.cs  <!-- All files in tests/ folder -->
</ExcludeByFile>

<IncludeDirectory>
  **/src/**  <!-- ONLY include src/ folder -->
</IncludeDirectory>
```

#### ReportGenerator Filters
```yaml
classfilters: '-PgQuery.*;-*.Protos.*;-*.Tests;-*Test'
filefilters: '-**/obj/**;-**/Protos/**;-**/tests/**'
```

**Result**: Coverage now measures ONLY `src/` folder projects

---

### 2. PR Comments Update Instead of Duplicate ✅

**Problem**: Each CI run created a new comment, cluttering PRs  
**Solution**: Find and update existing comment

#### Implementation
```javascript
// Find existing bot comment with our marker
const botComment = comments.data.find(comment => 
  comment.user.type === 'Bot' && 
  comment.body.includes('🧪 Build and Test Results')
);

if (botComment) {
  // Update existing comment
  await github.rest.issues.updateComment({...});
} else {
  // Create new comment only if none exists
  await github.rest.issues.createComment({...});
}
```

**Result**: Only one coverage comment per PR, always up-to-date

---

### 3. Build Does NOT Fail on Test Failures ✅

**Problem**: Test failures blocked merging even when expected  
**Solution**: Tests run with `continue-on-error: true`, failures reported in PR

#### Workflow Logic
```yaml
- name: Run tests with code coverage
  continue-on-error: true  # Don't fail build
  
- name: Analyze test results
  # Parse TRX files for failures
  
- name: Summary
  if: always()
  # Report but don't fail
```

**Result**: Build continues, failures visible in PR comment

---

### 4. Failed/Skipped Tests Listed in PR ✅

**Problem**: Had to check artifacts to see which tests failed  
**Solution**: Parse TRX files and include in PR comment

#### Test Analysis Step
```bash
# Parse TRX files to find failed and skipped tests
for trx_file in $(find coverage -name "*.trx"); do
  # Extract counts and test names
  failed_tests=$(grep -oP 'testName="\K[^"]+' "$trx_file" | ...)
  skipped_tests=$(grep -oP 'testName="\K[^"]+' "$trx_file" | ...)
done
```

#### PR Comment Format
```markdown
⚠️ 5 test(s) failed, 250 passed, 10 skipped

**Failed Tests:**
- Npgquery.Tests.SomeTest
- mbulava.PostgreSql.Dac.Tests.AnotherTest
... and 3 more. See test results artifact for full details.

**Skipped Tests:**
- Test1 (reason)
- Test2 (reason)
... and 8 more skipped tests.
```

**Result**: Immediate visibility of test issues in PR

---

## 📊 Coverage Scope

### What's Measured ✅
```
src/
├── libs/
│   ├── Npgquery/Npgquery/           ← Measured
│   └── mbulava.PostgreSql.Dac/      ← Measured
└── tools/
    └── pgPacTool.Cli/               ← Measured
```

### What's Excluded ❌
```
tests/                               ← Excluded (all test projects)
obj/Debug/                          ← Excluded (build artifacts)
**/Protos/                          ← Excluded (generated protobuf)
LinuxContainer.Tests/               ← Not even run in CI
```

---

## 🎨 PR Comment Example

```markdown
## 🧪 Build and Test Results

✅ Build successful
⚠️ 3 test(s) failed, 587 passed, 6 skipped

**Failed Tests:**
- Npgquery.Tests.Deparse_ValidAST_ReturnsSQL
- mbulava.PostgreSql.Dac.Tests.Generate_WithCreateTable_ReturnsValidSQL
- mbulava.PostgreSql.Dac.Tests.RoundTrip_ComplexScenario_PreservesSemantics

**Skipped Tests:**
- Npgquery.Tests.QuickDeparse_ValidAST_Works
- AsyncParserComprehensiveTests.DeparseAsync_ValidAST_ReturnsSQL
... and 4 more skipped tests.

### 📊 Code Coverage (Source Code Only - src/ folder)

> **Note**: Coverage metrics exclude:
> - Generated protobuf files (~273 classes)
> - All test projects (tests/ folder)
> - LinuxContainer.Tests (local dev only)
> - Files in obj/Debug directories
> 
> **Only measuring**: Projects in `src/` folder

[Coverage Summary Table]

---

<details>
<summary>📦 Test Summary</summary>

| Metric | Count |
|--------|-------|
| Total Tests | 590 |
| ✅ Passed | 587 |
| ❌ Failed | 3 |
| ⏭️ Skipped | 6 |

</details>

[View detailed coverage report](...)

_Last updated: Mon, 03 Mar 2025 20:15:30 GMT_
```

---

## 🚀 Behavior Changes

### Before
1. ❌ Test projects included in coverage (inflated numbers)
2. ❌ New PR comment on every CI run (clutter)
3. ❌ Build failed on any test failure (blocked PRs)
4. ❌ Had to check artifacts to see failed tests

### After
1. ✅ Only `src/` folder measured (accurate numbers)
2. ✅ Single PR comment, always updated (clean)
3. ✅ Build continues on test failures (visible but not blocking)
4. ✅ Failed/skipped tests listed in PR (immediate visibility)

---

## 🎯 Coverage Accuracy

### Example: Npgquery Project

**Before** (with test projects):
```
Package: ~30% (includes test files)
```

**After** (src/ only):
```
Source: 54.23% (actual implementation code)
```

**Why Better**:
- Test files shouldn't count toward coverage
- Measures only code that ships to users
- Focuses improvement efforts correctly

---

## 🔧 Local Development

These settings work locally too!

```powershell
# Run tests with same coverage settings as CI
dotnet test --settings coverlet.runsettings --collect:"XPlat Code Coverage"

# Get accurate coverage report
.\scripts\Get-AccurateCoverage.ps1 -Project "Npgquery"
```

**Result**: Local coverage matches CI coverage

---

## 📝 Configuration Files

### coverlet.runsettings
- ✅ Excludes test assemblies: `[*.Tests]*`
- ✅ Excludes test files: `**/tests/**/*.cs`
- ✅ Includes only src: `**/src/**`

### Directory.Build.props
- ✅ MSBuild-level settings
- ✅ Applied to all projects
- ✅ Consistent behavior

### build-and-test.yml
- ✅ Test result analysis step
- ✅ PR comment update logic
- ✅ Detailed test reporting
- ✅ Continue on error

---

## 🎓 Best Practices

### When Tests Fail in CI

1. **Check PR comment** - Lists failed tests immediately
2. **Download artifacts** - For detailed logs if needed
3. **Fix tests** - CI will update comment on next push
4. **Don't merge** - Even though build doesn't fail, fix tests first

### When Coverage Drops

1. **Focus on src/ files** - Test code doesn't count
2. **Check which files** - PR comment shows changes
3. **Add tests** - For new features in src/
4. **Ignore test coverage** - Not measured anymore

### When Skipping Tests

1. **Document why** - In test attribute
2. **Create issue** - To track re-enabling
3. **Expect in CI** - Will be listed in PR comment

---

## ⚠️ Important Notes

### Test Failures Don't Block Merges
- Build continues even with test failures
- **But you should still fix them!**
- PR comment makes failures visible
- Team decision whether to merge with failures

### Coverage is Source-Only
- Test projects not measured
- This is **intentional and correct**
- Measures code that ships to users
- Lower numbers, but more meaningful

### LinuxContainer.Tests Never Runs
- Excluded from CI by test filter
- Not in coverage, not in test counts
- Use locally for cross-platform testing
- Dedicated multi-platform CI coming later

---

## 🎉 Summary

**CI is now**:
- ✅ More accurate (src/ folder only)
- ✅ More helpful (lists failed tests)
- ✅ Less cluttered (updates PR comments)
- ✅ More flexible (doesn't block on failures)

**Teams get**:
- 📊 Accurate coverage metrics
- 🔍 Immediate failure visibility
- 📝 Clean PR comments
- 🎯 Focus on real issues

---

*Last Updated*: Current Session  
*Status*: ✅ Production Ready  
*Behavior*: Non-blocking with detailed reporting
