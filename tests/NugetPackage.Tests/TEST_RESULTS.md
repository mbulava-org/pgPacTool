# NuGet Package Validation Test Results

## Tests Created

### ✅ Passing Tests (3/5)

1. **DacLibraryPackage_ShouldContainAllRequiredFiles** ✅
   - Validates package structure
   - Confirms Npgquery.dll is included in lib/net10.0/
   - Confirms native libraries are included for all platforms
   - Confirms correct NuGet dependencies (Google.Protobuf, Npgsql)
   - Confirms Npgquery is NOT listed as a NuGet dependency

2. **GlobalToolPackage_ShouldContainAllRequiredFiles** ✅
   - Validates global tool package structure
   - Confirms all dependencies are included in tools directory
   - Confirms DotnetTool package type is set
   - Confirms tool settings are correct

3. **GlobalTool_CanBeInstalledAndExecuted** ✅
   - Successfully installs the global tool
   - Successfully executes pgpac --version
   - Successfully executes pgpac --help

### ❌ Failing Tests (2/5) - REAL BUGS FOUND!

4. **DacLibraryPackage_CanBeConsumedSuccessfully** ❌
   - **Status**: FAILS - Cannot add package to new project
   - **Error**: `NU1101: Unable to find package Npgquery`
   - **Root Cause**: When mbulava.PostgreSql.Dac.dll is loaded, it has assembly references to Npgquery.dll. Even though Npgquery.dll is included in the package, NuGet's dependency resolution looks for a Npgquery NuGet package that doesn't exist.
   
5. **DacLibraryPackage_NativeLibrariesLoadCorrectly** ❌
   - **Status**: FAILS - Same issue as #4
   - **Error**: Cannot even add the package due to Npgquery dependency issue

## Root Cause Analysis

The mbulava.PostgreSql.Dac package has a **runtime dependency** on Npgquery.dll that cannot be resolved:

1. ✅ **Package Structure is Correct**: Npgquery.dll IS included in the package
2. ✅ **NuGet Dependencies are Correct**: Npgquery is NOT listed as a NuGet dependency (due to PrivateAssets=all)
3. ❌ **Assembly References Create Problem**: The compiled mbulava.PostgreSql.Dac.dll has assembly metadata that references Npgquery, and NuGet tries to resolve this as a package

## Solutions

### Option 1: Publish Npgquery as a NuGet Package (Recommended)
Make Npgquery packable and publish it to nuget.org or include it in the local feed for testing.

**Changes needed**:
```xml
<!-- src/libs/Npgquery/Npgquery/Npgquery.csproj -->
<PropertyGroup>
  <IsPackable>true</IsPackable>  <!-- Change from false -->
  <PackageId>Npgquery</PackageId>
</PropertyGroup>
```

Then in mbulava.PostgreSql.Dac.csproj, remove the PrivateAssets and let it be a normal dependency:
```xml
<ItemGroup>
  <ProjectReference Include="..\Npgquery\Npgquery\Npgquery.csproj" />
  <!-- Remove PrivateAssets=all -->
</ItemGroup>
```

### Option 2: ILRepack/IL Merge (Complex)
Merge Npgquery.dll into mbulava.PostgreSql.Dac.dll using ILRepack so there's only one assembly.

**Pros**: Single DLL, no dependency issues
**Cons**: Complex build process, potential issues with native library loading

### Option 3: Document the Limitation (Not Recommended)
Document that the package can only be used via project reference, not via NuGet package reference.

**Pros**: No changes needed
**Cons**: Defeats the purpose of publishing to NuGet

## Recommendation

**Implement Option 1**: Publish Npgquery as a separate NuGet package. This is the standard .NET approach and will:
- Allow mbulava.PostgreSql.Dac to be consumed as a NuGet package
- Allow postgresPacTools global tool to work correctly
- Follow .NET packaging best practices
- Make it easier for users to consume the libraries

## Test Coverage Summary

These tests successfully validate:
- ✅ Package structure and contents
- ✅ Native library inclusion for all platforms (Windows, Linux, macOS)
- ✅ Global tool installation and execution
- ✅ NuGet metadata correctness
- ❌ **Package consumability (IDENTIFIED BUG)**
- ❌ **Native library loading in consumer projects (BLOCKED BY BUG)**

## Next Steps

1. Decide on solution approach (Option 1 recommended)
2. Implement the solution
3. Re-run tests to verify all 5 tests pass
4. Add these tests to CI/CD pipeline to prevent future regressions

---

*Generated on: ${new Date().toISOString()}*
*Test Project: tests/NugetPackage.Tests/*
