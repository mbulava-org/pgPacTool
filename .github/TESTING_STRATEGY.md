# pgPacTool - Testing Strategy & Standards

**Project:** PostgreSQL Data-Tier Application Compiler  
**Target Framework:** .NET 10  
**Code Coverage Target:** ?90%  
**Last Updated:** 2026-01-31

---

## ?? Testing Goals

### Primary Objectives
- **90%+ Code Coverage** - Minimum threshold for production code
- **100% Coverage for Critical Paths** - Extraction, compilation, comparison
- **Zero Regression** - All tests must pass before merging
- **Fast Feedback** - Unit tests complete in < 5 seconds
- **Reliable Integration Tests** - No flaky tests

### Quality Metrics
- **Code Coverage:** ?90% line coverage, ?85% branch coverage
- **Test Pass Rate:** 100% (no failing tests in main branch)
- **Test Execution Time:** Unit tests < 5s, Integration tests < 2min
- **Mutation Test Score:** ?80% (using Stryker.NET)

---

## ??? Testing Pyramid

```
                    ?
                   ? ?
                  ? E2E?         ~5% (CLI workflows, full scenarios)
                 ?????????
                ? Integr. ?       ~25% (Database interaction, Testcontainers)
               ?????????????
              ?    Unit     ?     ~70% (Business logic, parsers, comparers)
             ?????????????????
            ?_________________?

Total Tests Target: 500-1000 tests
```

### Distribution Target
- **Unit Tests:** 350-700 tests (~70%)
- **Integration Tests:** 125-250 tests (~25%)
- **End-to-End Tests:** 25-50 tests (~5%)

---

## ?? Test Categories & Requirements

### 1. Unit Tests

**Target:** 70% of all tests, 95%+ code coverage for business logic

**What to Test:**
- ? All public methods
- ? All business logic
- ? All validation logic
- ? All data transformations
- ? All comparison logic
- ? All parsing logic
- ? Edge cases and boundary conditions
- ? Error handling paths

**What NOT to Unit Test:**
- ? Simple getters/setters (auto-properties)
- ? Data models with no logic
- ? Database queries (use integration tests)
- ? External dependencies (mock them)

**Naming Convention:**
```csharp
[TestFixture]
public class PgProjectExtractorTests
{
    [Test]
    public void ExtractViewsAsync_ValidView_ReturnsCorrectViewDefinition()
    {
        // Arrange, Act, Assert
    }
    
    [Test]
    public void ExtractViewsAsync_NullSchema_ThrowsArgumentNullException()
    {
        // Arrange, Act, Assert
    }
}
```

**Pattern:** `{MethodName}_{Scenario}_{ExpectedResult}`

---

### 2. Integration Tests

**Target:** 25% of all tests, focus on database interaction

**What to Test:**
- ? Database extraction (all object types)
- ? SQL script execution
- ? Schema comparison with real databases
- ? Deployment script generation
- ? Package/project reference resolution
- ? Multi-version PostgreSQL compatibility

**Infrastructure:**
- Use **Testcontainers** for PostgreSQL instances
- Use **Docker** for isolated test environments
- Test against **PostgreSQL 16+** only (simplified from multi-version testing)

**Container Setup:**
```csharp
private const string PostgreSqlVersion = "16"; // Only version we support

_container = new PostgreSqlBuilder()
    .WithImage($"postgres:{PostgreSqlVersion}")
    .WithDatabase("testdb")
    .Build();
```

**Naming Convention:**
```csharp
[TestFixture]
public class ViewExtractionIntegrationTests : PostgreSqlTestBase
{
    [Test]
    public async Task ExtractViews_FromRealDatabase_ExtractsAllViews()
    {
        // Arrange: Seed database
        // Act: Extract
        // Assert: Verify
    }
}
```

---

### 3. End-to-End Tests

**Target:** 5% of all tests, focus on complete workflows

**What to Test:**
- ? CLI command workflows (extract ? build ? publish)
- ? Complete database migration scenarios
- ? MSBuild integration
- ? NuGet package creation and consumption
- ? Container image creation and execution

**Examples:**
```csharp
[TestFixture]
public class E2EWorkflowTests
{
    [Test]
    public async Task FullWorkflow_ExtractBuildPublish_Success()
    {
        // 1. Extract from source database
        // 2. Build .pgpac
        // 3. Publish to target database
        // 4. Verify all objects created
    }
}
```

---

## ??? Testing Tools & Frameworks

### Core Testing Stack

**Test Framework:**
```xml
<PackageReference Include="NUnit" Version="4.1.0" />
<PackageReference Include="NUnit3TestAdapter" Version="4.5.0" />
```

**Assertion Library:**
```xml
<PackageReference Include="FluentAssertions" Version="6.12.0" />
```

**Mocking:**
```xml
<PackageReference Include="NSubstitute" Version="5.1.0" />
<!-- OR -->
<PackageReference Include="Moq" Version="4.20.70" />
```

**Test Data Builders:**
```xml
<PackageReference Include="Bogus" Version="35.4.0" />
```

**Integration Testing:**
```xml
<PackageReference Include="Testcontainers" Version="3.7.0" />
<PackageReference Include="Testcontainers.PostgreSql" Version="3.7.0" />
```

**Code Coverage:**
```xml
<PackageReference Include="coverlet.collector" Version="6.0.0" />
<PackageReference Include="coverlet.msbuild" Version="6.0.0" />
```

**Mutation Testing:**
```xml
<PackageReference Include="Stryker.NET" Version="4.0.0" />
```

---

## ?? Code Coverage Configuration

### .NET Configuration

**Directory.Build.props:**
```xml
<Project>
  <PropertyGroup>
    <!-- Enable code coverage -->
    <CollectCoverage>true</CollectCoverage>
    <CoverletOutputFormat>cobertura,opencover</CoverletOutputFormat>
    <CoverletOutput>./TestResults/</CoverletOutput>
    <Threshold>90</Threshold>
    <ThresholdType>line,branch</ThresholdType>
    <ThresholdStat>total</ThresholdStat>
    
    <!-- Exclude generated files -->
    <ExcludeByFile>**/obj/**/*,**/bin/**/*</ExcludeByFile>
    
    <!-- Exclude auto-generated code -->
    <ExcludeByAttribute>GeneratedCode,CompilerGenerated</ExcludeByAttribute>
  </PropertyGroup>
</Project>
```

### Coverage Thresholds

| Component | Line Coverage | Branch Coverage |
|-----------|--------------|-----------------|
| **Extraction** | ?95% | ?90% |
| **Compilation** | ?95% | ?90% |
| **Comparison** | ?95% | ?90% |
| **Script Generation** | ?95% | ?90% |
| **CLI** | ?85% | ?80% |
| **Models** | ?75% | N/A |
| **Overall** | **?90%** | **?85%** |

### Exclusions

**What to Exclude from Coverage:**
- Auto-generated code
- Program.cs entry points
- Simple DTOs/POCOs with no logic
- Third-party code
- Test helper classes

**How to Exclude:**
```csharp
[ExcludeFromCodeCoverage]
public class PgProject
{
    public string Name { get; set; }
    // Simple property - no logic to test
}

// Or exclude specific methods
[ExcludeFromCodeCoverage]
private void LogDebugInfo()
{
    // Logging only, not critical path
}
```

---

## ?? Test Organization

### Project Structure

```
pgPacTool/
??? src/
?   ??? libs/
?   ?   ??? mbulava.PostgreSql.Dac/
?   ?       ??? Extract/
?   ?       ??? Compile/
?   ?       ??? Compare/
?   ?       ??? Models/
?   ??? postgresPacTools/
?       ??? CLI/
??? tests/
?   ??? mbulava.PostgreSql.Dac.Tests/          # Unit tests
?   ?   ??? Extract/
?   ?   ?   ??? PgProjectExtractorTests.cs
?   ?   ?   ??? ViewExtractionTests.cs
?   ?   ?   ??? FunctionExtractionTests.cs
?   ?   ??? Compile/
?   ?   ?   ??? ProjectCompilerTests.cs
?   ?   ?   ??? ReferenceValidatorTests.cs
?   ?   ??? Compare/
?   ?   ?   ??? PgSchemaComparerTests.cs
?   ?   ?   ??? PgAttributeComparerTests.cs
?   ?   ??? TestHelpers/
?   ?       ??? TestDataBuilder.cs
?   ?       ??? MockFactory.cs
?   ??? mbulava.PostgreSql.Dac.Integration.Tests/  # Integration tests
?   ?   ??? PostgreSqlTestBase.cs
?   ?   ??? ExtractionIntegrationTests.cs
?   ?   ??? CompilationIntegrationTests.cs
?   ?   ??? Fixtures/
?   ?       ??? BasicDatabase.sql
?   ?       ??? ComplexDatabase.sql
?   ??? pgPacTool.E2E.Tests/                  # End-to-end tests
?       ??? CliWorkflowTests.cs
?       ??? MigrationScenarioTests.cs
```

---

## ?? Testing Standards & Best Practices

### AAA Pattern (Arrange-Act-Assert)

```csharp
[Test]
public void ExtractViews_ValidSchema_ReturnsViews()
{
    // Arrange
    var extractor = new PgProjectExtractor("connection_string");
    var schemaName = "public";
    
    // Act
    var views = await extractor.ExtractViewsAsync(schemaName);
    
    // Assert
    views.Should().NotBeEmpty();
    views.Should().AllSatisfy(v => v.Schema.Should().Be(schemaName));
}
```

### One Assert Per Test (Preferred)

```csharp
// Good - focused test
[Test]
public void ExtractViews_ValidSchema_ReturnsNonEmptyCollection()
{
    var views = await extractor.ExtractViewsAsync("public");
    views.Should().NotBeEmpty();
}

[Test]
public void ExtractViews_ValidSchema_AllViewsHaveCorrectSchema()
{
    var views = await extractor.ExtractViewsAsync("public");
    views.Should().AllSatisfy(v => v.Schema.Should().Be("public"));
}

// Acceptable - related assertions
[Test]
public void ExtractViews_ValidSchema_ReturnsViewsWithRequiredProperties()
{
    var views = await extractor.ExtractViewsAsync("public");
    views.Should().NotBeEmpty();
    views.Should().AllSatisfy(v => 
    {
        v.Name.Should().NotBeNullOrEmpty();
        v.Definition.Should().NotBeNullOrEmpty();
        v.Owner.Should().NotBeNullOrEmpty();
    });
}
```

### Test Data Builders

```csharp
public class PgViewBuilder
{
    private string _name = "test_view";
    private string _schema = "public";
    private string _definition = "SELECT * FROM test_table";
    private bool _isMaterialized = false;
    
    public PgViewBuilder WithName(string name)
    {
        _name = name;
        return this;
    }
    
    public PgViewBuilder AsMaterialized()
    {
        _isMaterialized = true;
        return this;
    }
    
    public PgView Build() => new PgView
    {
        Name = _name,
        Schema = _schema,
        Definition = _definition,
        IsMaterialized = _isMaterialized
    };
}

// Usage
[Test]
public void CompareViews_MaterializedView_DetectedCorrectly()
{
    var view = new PgViewBuilder()
        .WithName("customer_stats")
        .AsMaterialized()
        .Build();
    
    // Test logic
}
```

### Mocking Best Practices

```csharp
[Test]
public async Task ExtractViews_DatabaseConnectionFails_ThrowsException()
{
    // Arrange
    var mockConnection = Substitute.For<IDbConnection>();
    mockConnection.OpenAsync().ThrowsAsync(new InvalidOperationException("Connection failed"));
    
    var extractor = new PgProjectExtractor(mockConnection);
    
    // Act & Assert
    await extractor.Invoking(e => e.ExtractViewsAsync("public"))
        .Should().ThrowAsync<InvalidOperationException>()
        .WithMessage("Connection failed");
}
```

### Parameterized Tests

```csharp
[TestCase("12.0", true)]
[TestCase("13.0", true)]
[TestCase("14.0", true)]
[TestCase("15.0", true)]
[TestCase("16.0", true)]
[TestCase("11.0", false)]
[TestCase("10.0", false)]
public void SupportsProcedures_VariousVersions_ReturnsCorrectly(string version, bool expected)
{
    var result = VersionChecker.SupportsProcedures(version);
    result.Should().Be(expected);
}

// Or with TestCaseSource
[Test, TestCaseSource(nameof(PostgreSqlVersions))]
public async Task ExtractSchema_AllSupportedVersions_Succeeds(string version)
{
    await using var container = new PostgreSqlBuilder()
        .WithImage($"postgres:{version}")
        .Build();
    
    await container.StartAsync();
    
    var extractor = new PgProjectExtractor(container.GetConnectionString());
    var result = await extractor.ExtractPgProject("testdb", version);
    
    result.Should().NotBeNull();
}

private static IEnumerable<string> PostgreSqlVersions()
{
    yield return "12";
    yield return "13";
    yield return "14";
    yield return "15";
    yield return "16";
}
```

---

## ?? Running Tests

### Local Development

```bash
# Run all tests
dotnet test

# Run with code coverage
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=opencover

# Run specific test category
dotnet test --filter Category=Unit

# Run tests from specific project
dotnet test tests/mbulava.PostgreSql.Dac.Tests

# Run tests matching pattern
dotnet test --filter FullyQualifiedName~ViewExtraction

# Run with verbose output
dotnet test --logger "console;verbosity=detailed"

# Generate coverage report
dotnet test /p:CollectCoverage=true /p:CoverletOutput=TestResults/ /p:CoverletOutputFormat=cobertura
reportgenerator -reports:TestResults/coverage.cobertura.xml -targetdir:TestResults/html -reporttypes:Html
```

### CI/CD Pipeline

```yaml
# .github/workflows/test.yml
name: Tests

on: [push, pull_request]

jobs:
  test:
    runs-on: ubuntu-latest
    
    steps:
    - uses: actions/checkout@v3
    
    - name: Setup .NET 10
      uses: actions/setup-dotnet@v3
      with:
        dotnet-version: '10.0.x'
    
    - name: Restore dependencies
      run: dotnet restore
    
    - name: Build
      run: dotnet build --no-restore
    
    - name: Run tests with coverage
      run: |
        dotnet test --no-build --verbosity normal \
          /p:CollectCoverage=true \
          /p:CoverletOutputFormat=opencover \
          /p:Threshold=90 \
          /p:ThresholdType=line,branch \
          /p:ThresholdStat=total
    
    - name: Generate coverage report
      run: |
        dotnet tool install -g dotnet-reportgenerator-globaltool
        reportgenerator -reports:TestResults/**/coverage.opencover.xml \
          -targetdir:TestResults/html \
          -reporttypes:Html,Cobertura
    
    - name: Upload coverage to Codecov
      uses: codecov/codecov-action@v3
      with:
        files: TestResults/**/coverage.cobertura.xml
        fail_ci_if_error: true
    
    - name: Check coverage threshold
      run: |
        # Fail if coverage < 90%
        exit_code=$?
        if [ $exit_code -ne 0 ]; then
          echo "Code coverage below 90% threshold"
          exit 1
        fi
```

---

## ?? Issue-Specific Testing Requirements

### Issue #1: View Extraction

**Unit Tests Required:**
- [ ] `ExtractViewsAsync_ValidSchema_ReturnsViews`
- [ ] `ExtractViewsAsync_EmptySchema_ReturnsEmptyList`
- [ ] `ExtractViewsAsync_NullSchema_ThrowsArgumentNullException`
- [ ] `ExtractViewsAsync_MaterializedView_SetsFlagCorrectly`
- [ ] `ExtractViewsAsync_ViewWithDependencies_TracksDependencies`
- [ ] `ParseViewAst_ValidViewDefinition_ParsesCorrectly`
- [ ] `ParseViewAst_InvalidSql_ThrowsException`

**Integration Tests Required:**
- [ ] `ExtractViews_FromPostgreSQL16_ExtractsAllViews`
- [ ] `ExtractViews_WithComplexJoins_ExtractsCorrectly`
- [ ] `ExtractViews_MaterializedView_ExtractsWithAllProperties`

**Coverage Target:** ?95%

---

### Issue #2: Function Extraction

**Unit Tests Required:**
- [ ] `ExtractFunctionsAsync_ValidSchema_ReturnsFunctions`
- [ ] `ExtractFunctionsAsync_OverloadedFunctions_ReturnsAllOverloads`
- [ ] `ExtractFunctionsAsync_DifferentLanguages_ExtractsAllLanguages`
- [ ] `ExtractFunctionsAsync_VolatilitySettings_ExtractsCorrectly`
- [ ] `ExtractFunctionsAsync_SecurityDefiner_FlagSetCorrectly`
- [ ] `ParseFunctionMetadata_CostAndRows_ParsedCorrectly`

**Integration Tests Required:**
- [ ] `ExtractFunctions_SQLFunction_ExtractsCorrectly`
- [ ] `ExtractFunctions_PlPgSqlFunction_ExtractsCorrectly`
- [ ] `ExtractFunctions_OverloadedFunctions_DistinguishesCorrectly`

**Coverage Target:** ?95%

---

### Issue #7: Fix Privilege Extraction (CRITICAL)

**Unit Tests Required:**
- [ ] `ParseAcl_ValidAcl_ReturnsPrivileges`
- [ ] `ParseAcl_NullAcl_ReturnsEmptyList`
- [ ] `ParseAcl_EmptyArray_ReturnsEmptyList`
- [ ] `ParseAcl_PublicGrant_ParsesCorrectly`
- [ ] `ParseAcl_GrantOption_DetectedCorrectly`
- [ ] `MapPrivilegeCode_AllCodes_MapsCorrectly`
- [ ] `MapPrivilegeCode_UpperCase_DetectsGrantOption`
- [ ] `ExtractPrivileges_ForSchema_WorksCorrectly`
- [ ] `ExtractPrivileges_ForTable_WorksCorrectly`
- [ ] `ExtractPrivileges_ForFunction_WorksCorrectly`

**Integration Tests Required:**
- [ ] `ExtractPrivileges_SchemaWithGrants_ExtractsCorrectly`
- [ ] `ExtractPrivileges_TableWithColumnPrivileges_ExtractsCorrectly`
- [ ] `ExtractPrivileges_MultipleGrantees_ExtractsAllGrants`

**Coverage Target:** 100% (critical path)

---

### Issue #9: Compiler Reference Validation

**Unit Tests Required:**
- [ ] `BuildCatalog_ValidSqlFiles_BuildsCorrectly`
- [ ] `ExtractReferences_TableReference_Detected`
- [ ] `ExtractReferences_FunctionCall_Detected`
- [ ] `ExtractReferences_SequenceReference_Detected`
- [ ] `ReferenceExists_InCatalog_ReturnsTrue`
- [ ] `ReferenceExists_NotInCatalog_ReturnsFalse`
- [ ] `ReferenceExists_InPackageReference_ReturnsTrue`
- [ ] `Validate_MissingReference_GeneratesError`
- [ ] `Validate_AllReferencesValid_NoErrors`

**Integration Tests Required:**
- [ ] `Validate_CompleteProject_AllReferencesResolved`
- [ ] `Validate_MissingTable_ReportsError`
- [ ] `Validate_WithPackageReferences_ResolvesCorrectly`

**Coverage Target:** ?95%

---

## ?? Coverage Monitoring

### Tools & Reports

**Coverage Report Tools:**
```bash
# Install ReportGenerator
dotnet tool install -g dotnet-reportgenerator-globaltool

# Generate HTML report
reportgenerator \
  -reports:TestResults/**/coverage.opencover.xml \
  -targetdir:TestResults/html \
  -reporttypes:Html

# Open report
open TestResults/html/index.html  # macOS
start TestResults/html/index.html  # Windows
xdg-open TestResults/html/index.html  # Linux
```

**Continuous Monitoring:**
- **Codecov.io** - Cloud-based coverage tracking
- **Coveralls** - Alternative to Codecov
- **SonarQube** - Comprehensive code quality

### Coverage Badge

Add to README.md:
```markdown
[![codecov](https://codecov.io/gh/mbulava-org/pgPacTool/branch/main/graph/badge.svg)](https://codecov.io/gh/mbulava-org/pgPacTool)
```

---

## ?? Mutation Testing

**Purpose:** Verify that tests actually catch bugs

**Configuration:**
```json
{
  "stryker-config": {
    "mutate": [
      "src/**/*.cs",
      "!src/**/Program.cs",
      "!src/**/obj/**/*.cs"
    ],
    "test-runner": "dotnet",
    "threshold-high": 80,
    "threshold-low": 60,
    "threshold-break": 50
  }
}
```

**Running Mutation Tests:**
```bash
# Install Stryker
dotnet tool install -g dotnet-stryker

# Run mutation testing
dotnet stryker

# Generate report
dotnet stryker --reporter html
```

**Target:** ?80% mutation score

---

## ? Definition of Done - Testing Checklist

### For Each Issue

- [ ] **Unit tests written** for all new code
- [ ] **Integration tests written** for database interactions
- [ ] **Code coverage ?90%** verified
- [ ] **All tests pass** locally
- [ ] **Test names follow convention** (Method_Scenario_Expected)
- [ ] **No flaky tests** (run 10 times, all pass)
- [ ] **Arrange-Act-Assert** pattern used
- [ ] **Test documentation** added where needed
- [ ] **Edge cases covered** (null, empty, boundary)
- [ ] **Error paths tested** (exceptions, failures)

### For Pull Requests

- [ ] **CI tests pass** on all commits
- [ ] **Coverage report reviewed** (no decrease)
- [ ] **New code has tests** (enforce in PR template)
- [ ] **Mutation score acceptable** (if applicable)
- [ ] **Test performance acceptable** (< 5s unit, < 2min integration)

---

## ?? Resources

### Documentation
- [NUnit Documentation](https://docs.nunit.org/)
- [FluentAssertions Documentation](https://fluentassertions.com/)
- [Testcontainers for .NET](https://dotnet.testcontainers.org/)
- [Coverlet Documentation](https://github.com/coverlet-coverage/coverlet)
- [Stryker.NET Documentation](https://stryker-mutator.io/docs/stryker-net/introduction/)

### Books
- "The Art of Unit Testing" by Roy Osherove
- "xUnit Test Patterns" by Gerard Meszaros
- "Working Effectively with Legacy Code" by Michael Feathers

### Courses
- Pluralsight: "Testing .NET Code with xUnit.net"
- Udemy: "Unit Testing for C# Developers"

---

## ?? Continuous Improvement

### Weekly Review
- [ ] Review coverage reports
- [ ] Identify untested code
- [ ] Add missing tests
- [ ] Remove redundant tests

### Monthly Review
- [ ] Review test execution time
- [ ] Optimize slow tests
- [ ] Update test infrastructure
- [ ] Review mutation test results

### Quarterly Review
- [ ] Update testing standards
- [ ] Evaluate new testing tools
- [ ] Team training on testing practices
- [ ] Refactor test code

---

**Document Version:** 1.0  
**Last Updated:** 2026-01-31  
**Next Review:** After Milestone 1 completion  
**Maintained By:** Development Team
