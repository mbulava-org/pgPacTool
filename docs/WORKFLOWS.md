# GitHub Workflows Documentation

**Last Updated:** 2026-01-31

---

## Overview

pgPacTool uses GitHub Actions for continuous integration and deployment. The workflows automatically build, test, and validate code quality on every push and pull request.

---

## Workflows

### 1. Build and Test (`build-and-test.yml`)

**Triggers:**
- Push to `main` or `develop` branches
- Pull requests to `main` or `develop`
- Manual workflow dispatch

**Jobs:**

#### build-and-test
Builds the solution and runs all tests with code coverage reporting.

**Features:**
- ✅ Builds with .NET 10
- ✅ Runs all tests (unit, integration, smoke)
- ✅ Generates code coverage reports
- ✅ Uploads coverage artifacts
- ✅ Posts coverage summary to PRs
- ✅ Enforces 70% coverage threshold
- ✅ Tests against PostgreSQL 16 (in Docker)

**Artifacts:**
- `coverage-report` - HTML coverage report (30 days retention)
- `test-results` - TRX test results (30 days retention)

**Coverage Tools:**
- **Collector:** Coverlet (XPlat Code Coverage)
- **Format:** OpenCover XML
- **Reporter:** ReportGenerator
- **Outputs:** HTML, Markdown, Badges

#### multi-version-test
Tests the solution against multiple PostgreSQL versions.

**Matrix:**
- PostgreSQL 16
- PostgreSQL 17

**Purpose:** Ensure compatibility across supported PostgreSQL versions.

---

### 2. PR Validation (`pr-validation.yml`)

**Triggers:**
- Pull requests opened, synchronized, or reopened
- Target branches: `main` or `develop`

**Jobs:**

#### pr-checks
Performs quality checks on pull requests.

**Checks Performed:**

| Check | Purpose | Failure Impact |
|-------|---------|----------------|
| **Code Formatting** | Verifies `dotnet format` compliance | Warning only |
| **Build** | Ensures solution compiles | Blocks PR |
| **Unit Tests** | Runs fast tests | Blocks PR |
| **Version Check** | Validates PostgreSQL 16+ requirement | Blocks PR |
| **PR Size** | Warns if PR is too large (>50 files) | Warning only |
| **Breaking Changes** | Detects potential API breaks | Warning only |
| **Dependencies** | Flags dependency modifications | Warning only |

**PR Comment:**
Automatically posts a detailed validation summary to the PR, including:
- Build and test status
- PR statistics (files, lines changed)
- Warnings and recommendations

#### smoke-test
Runs smoke tests against a real PostgreSQL 16 database.

**Features:**
- Runs in parallel with `pr-checks`
- 5-minute timeout
- PostgreSQL in Docker service
- Validates core functionality quickly

---

## Environment Variables

All workflows use these common environment variables:

```yaml
env:
  DOTNET_VERSION: '10.0.x'      # .NET SDK version
  CONFIGURATION: Release         # Build configuration
  POSTGRES_VERSION: '16'         # Default PostgreSQL version
```

---

## Code Coverage

### Configuration

Code coverage is collected using Coverlet with the following settings:

```yaml
--collect:"XPlat Code Coverage"
--results-directory ./coverage
-- DataCollectionRunSettings.DataCollectors.DataCollector.Configuration.Format=opencover
```

### Coverage Threshold

**Minimum Required:** 70%

The build will fail if line coverage drops below 70%.

### Coverage Reports

**Generated Formats:**
1. **HTML Report** - Detailed interactive report
2. **Markdown Summary** - Posted to PR comments
3. **Badges** - Coverage percentage badges

**Access Reports:**
1. Go to Actions tab
2. Select the workflow run
3. Download `coverage-report` artifact
4. Open `index.html` in a browser

### Coverage by Component

Target coverage goals by component:

| Component | Target | Current |
|-----------|--------|---------|
| Extract | 90% | TBD |
| Models | 95% | TBD |
| Compare | 85% | TBD |
| Compile | 80% | TBD |

---

## PostgreSQL Testing

### Service Configuration

PostgreSQL runs as a GitHub Actions service:

```yaml
services:
  postgres:
    image: postgres:16  # or postgres:17
    env:
      POSTGRES_PASSWORD: testpassword
      POSTGRES_USER: postgres
      POSTGRES_DB: testdb
    options: >-
      --health-cmd pg_isready
      --health-interval 10s
      --health-timeout 5s
      --health-retries 5
    ports:
      - 5432:5432
```

### Connection String

Tests use this connection string:

```
Host=localhost;Port=5432;Database=testdb;Username=postgres;Password=testpassword
```

Set via environment variable:
```yaml
env:
  ConnectionStrings__TestDatabase: "Host=localhost;Port=5432;Database=testdb;Username=postgres;Password=testpassword"
```

---

## Test Categories

Tests are organized by category for selective execution:

| Category | Purpose | When to Run |
|----------|---------|-------------|
| `Unit` | Fast, no dependencies | Every PR |
| `Integration` | Requires PostgreSQL | Every push |
| `Smoke` | Quick validation | Every PR |
| `Postgres16` | PG 16 specific | Matrix job |
| `Postgres17` | PG 17 specific | Matrix job |

**Run specific categories:**

```bash
# Unit tests only
dotnet test --filter "Category=Unit"

# Integration tests
dotnet test --filter "Category=Integration"

# Smoke tests
dotnet test --filter "Category=Smoke"
```

---

## Workflow Outputs

### Build Artifacts

| Artifact | Contents | Retention |
|----------|----------|-----------|
| `coverage-report` | HTML coverage report | 30 days |
| `test-results` | TRX test result files | 30 days |

### PR Comments

#### Build and Test
```markdown
## 🧪 Build and Test Results

✅ Build successful
✅ Tests completed

### 📊 Code Coverage

Summary table with line/branch/method coverage

[View detailed coverage report](link)
```

#### PR Validation
```markdown
## 🔍 PR Validation Results

### Build & Test Status
✅ Code formatting: success
✅ Build: success
✅ Unit tests: success
✅ PostgreSQL 16+ requirement: success

### 📊 PR Statistics
- Files changed: 12
- Lines added: +345
- Lines deleted: -67

### ⚠️ Warnings
- None

✅ Ready for review!
```

---

## Local Testing

### Run Locally with Docker

```bash
# Start PostgreSQL
docker run --name pgpac-test \
  -e POSTGRES_PASSWORD=testpassword \
  -p 5432:5432 \
  -d postgres:16

# Set connection string
export ConnectionStrings__TestDatabase="Host=localhost;Port=5432;Database=postgres;Username=postgres;Password=testpassword"

# Run tests with coverage
dotnet test \
  --collect:"XPlat Code Coverage" \
  --results-directory ./coverage

# Generate HTML report
dotnet tool install -g dotnet-reportgenerator-globaltool
reportgenerator \
  -reports:"coverage/**/coverage.cobertura.xml" \
  -targetdir:"coveragereport" \
  -reporttypes:Html
```

### Verify Formatting

```bash
# Check formatting
dotnet format --verify-no-changes

# Auto-fix formatting
dotnet format
```

---

## Troubleshooting

### Workflow Fails: "Connection refused"

**Cause:** PostgreSQL service not ready

**Solution:** Increase health check retries in workflow:
```yaml
options: >-
  --health-retries 10
```

### Coverage Threshold Failed

**Cause:** Code coverage below 70%

**Solution:**
1. Add more tests
2. Review uncovered code paths
3. Consider if threshold is appropriate for feature

### PR Validation: "Breaking changes detected"

**Cause:** Public API modifications detected

**Solution:**
1. Review changes carefully
2. Update version if breaking change is intentional
3. Add migration guide to PR description

### Large PR Warning

**Cause:** More than 50 files changed

**Solution:**
1. Break PR into smaller logical chunks
2. Submit as multiple sequential PRs
3. If unavoidable, explain in PR description

---

## Best Practices

### For Contributors

1. **Run tests locally** before pushing
2. **Check formatting** with `dotnet format`
3. **Keep PRs small** (<50 files when possible)
4. **Add tests** for new features
5. **Review coverage** reports for your changes

### For Maintainers

1. **Monitor coverage trends** - Don't let coverage decrease
2. **Review PR validation** results before merging
3. **Check multi-version tests** for compatibility
4. **Update workflows** as project needs evolve

---

## Future Enhancements

Planned workflow improvements:

- 🚧 **Benchmark tests** - Performance regression detection
- 🚧 **Security scanning** - Dependency vulnerability checks
- 🚧 **Release automation** - Automatic NuGet publishing
- 🚧 **Container builds** - Docker image creation
- 🚧 **Documentation deploy** - Auto-publish docs to GitHub Pages

---

## Workflow Maintenance

### Updating .NET Version

When upgrading to .NET 11 (or later):

1. Update `DOTNET_VERSION` in both workflows
2. Update project files (`TargetFramework`)
3. Test locally first
4. Update this documentation

### Adding New Test Categories

1. Add category to test classes: `[TestCategory("NewCategory")]`
2. Update PR validation to include new category
3. Document in this file

### Modifying Coverage Threshold

To change the 70% threshold:

1. Update `threshold` variable in `build-and-test.yml`
2. Document reason in commit message
3. Announce to team

---

## Support

- **Workflow issues:** Open issue with `ci/cd` label
- **Coverage questions:** See [TESTING_STRATEGY.md](../.github/TESTING_STRATEGY.md)
- **Test failures:** Check test output logs in Actions

---

**Last Updated:** 2026-01-31  
**Workflows Version:** 2.0  
**Next Review:** After Milestone 2
