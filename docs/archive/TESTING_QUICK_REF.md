# Testing Quick Reference Card

**90%+ Code Coverage Required** | .NET 10 | NUnit

---

## ? Quick Commands

```bash
# Run all tests
dotnet test

# Run with coverage
dotnet test /p:CollectCoverage=true

# Run specific category
dotnet test --filter Category=Unit

# Run with coverage report
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=opencover
reportgenerator -reports:TestResults/**/coverage.opencover.xml -targetdir:TestResults/html
```

---

## ?? Coverage Targets

| Component | Line | Branch |
|-----------|------|--------|
| Extraction | ?95% | ?90% |
| Compilation | ?95% | ?90% |
| Comparison | ?95% | ?90% |
| **Overall** | **?90%** | **?85%** |

---

## ?? Test Naming

**Pattern:** `{Method}_{Scenario}_{Expected}`

```csharp
ExtractViews_ValidSchema_ReturnsViews()
ExtractViews_NullSchema_ThrowsException()
```

---

## ?? AAA Pattern

```csharp
[Test]
public void Method_Scenario_Expected()
{
    // Arrange - Set up test data
    var sut = new SystemUnderTest();
    
    // Act - Execute the method
    var result = sut.DoSomething();
    
    // Assert - Verify result
    result.Should().Be(expected);
}
```

---

## ?? Required Packages

```xml
<PackageReference Include="NUnit" Version="4.1.0" />
<PackageReference Include="FluentAssertions" Version="6.12.0" />
<PackageReference Include="NSubstitute" Version="5.1.0" />
<PackageReference Include="Testcontainers.PostgreSql" Version="3.7.0" />
<PackageReference Include="coverlet.collector" Version="6.0.0" />
```

---

## ? Test Checklist

- [ ] Unit tests written (70% of tests)
- [ ] Integration tests written (25% of tests)
- [ ] Code coverage ?90%
- [ ] All tests pass
- [ ] AAA pattern used
- [ ] Test names descriptive
- [ ] Edge cases covered
- [ ] Error paths tested
- [ ] No flaky tests
- [ ] Tests run in < 5s (unit)

---

**Full Guide:** `.github/TESTING_STRATEGY.md`
