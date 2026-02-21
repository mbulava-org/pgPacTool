# Consolidate mbulava-org.Npgquery Library

## 🎯 Objective

Consolidate the `mbulava-org.Npgquery` library directly into the pgPacTool solution instead of maintaining it as a separate NuGet package.

## 📋 Current State

### Current Reference
```xml
<PackageReference Include="mbulava-org.Npgquery" Version="1.0.0.41-beta" />
```

### Solution Structure (Before)
```
pgPacTool/
├── src/
│   ├── libs/
│   │   └── mbulava.PostgreSql.Dac/        # Uses Npgquery as NuGet
│   └── postgresPacTools/
└── tests/
    └── ProjectExtract-Tests/
```

## 🎯 Target State

### Solution Structure (After)
```
pgPacTool/
├── src/
│   ├── libs/
│   │   ├── mbulava.PostgreSql.Dac/        # Uses Npgquery as ProjectReference
│   │   └── Npgquery/                      # ← NEW: Npgquery library source
│   └── postgresPacTools/
└── tests/
    └── ProjectExtract-Tests/
```

## 📝 Implementation Steps

### Phase 1: Clone and Add Npgquery Source

1. **Clone the Npgquery Repository**
   ```bash
   cd src/libs
   git clone https://github.com/mbulava-org/Npgquery.git
   ```

2. **Add Project to Solution**
   ```bash
   cd ../..
   dotnet sln add src/libs/Npgquery/Npgquery.csproj
   ```

3. **Verify Project Structure**
   ```bash
   dotnet sln list
   ```

### Phase 2: Update References

1. **Remove NuGet Package Reference**
   Edit `src/libs/mbulava.PostgreSql.Dac/mbulava.PostgreSql.Dac.csproj`:
   ```xml
   <!-- REMOVE THIS -->
   <PackageReference Include="mbulava-org.Npgquery" Version="1.0.0.41-beta" />
   
   <!-- ADD THIS -->
   <ProjectReference Include="..\Npgquery\Npgquery.csproj" />
   ```

2. **Update Test Project** (if needed)
   Check if `ProjectExtract-Tests.csproj` needs updates

### Phase 3: Update CI/CD

1. **Update Build Pipeline**
   - CI/CD will now build Npgquery from source
   - No need to restore from NuGet for Npgquery
   - Faster builds (no external dependency)

2. **Update .gitignore** (if needed)
   - Ensure Npgquery source is tracked
   - Keep Npgquery's bin/obj folders ignored

### Phase 4: Testing & Validation

1. **Build Solution**
   ```bash
   dotnet clean
   dotnet build
   ```

2. **Run Tests**
   ```bash
   dotnet test
   ```

3. **Verify All Features Work**
   - AST parsing
   - SQL parsing
   - All extraction features

## ✅ Benefits

### For Development
- ✅ Single repository for all code
- ✅ Easier debugging (source code available)
- ✅ Consistent versioning
- ✅ No NuGet package management overhead
- ✅ Direct source code modifications possible

### For CI/CD
- ✅ Faster builds (no external package restore)
- ✅ No dependency on NuGet feed availability
- ✅ Single build output
- ✅ Simplified deployment

### For Maintenance
- ✅ One place to update PostgreSQL parser
- ✅ Easier to track changes
- ✅ Simplified dependency management
- ✅ Better IDE integration

## ⚠️ Considerations

### Repository Structure
- Keep Npgquery as a subfolder in `src/libs/`
- Don't use git submodule (clone directly)
- Npgquery becomes part of main repo

### Version Control
- Npgquery source will be committed to pgPacTool repo
- Future updates to Npgquery require manual sync or automated tooling
- Consider keeping Npgquery as a separate repo and pulling updates when needed

### Build Configuration
- Ensure Npgquery project targets .NET 10
- Ensure all dependencies are compatible
- Update any hardcoded paths

## 📁 Expected File Changes

### Files to Modify
- `src/libs/mbulava.PostgreSql.Dac/mbulava.PostgreSql.Dac.csproj`
- `pgPacTool.sln` or `pgPacTool.slnx`
- `.github/workflows/*.yml` (CI/CD - if exists)

### Files to Add
- `src/libs/Npgquery/` (entire project)

### Files to Remove
- None (NuGet package cache will naturally be removed)

## 🎯 Success Criteria

- [x] Npgquery source code added to solution
- [x] Solution builds successfully
- [x] All tests pass
- [x] No NuGet reference to mbulava-org.Npgquery
- [x] ProjectReference works correctly
- [x] CI/CD updated (if applicable)
- [x] Documentation updated

## 📚 Next Steps After Integration

1. Consider updating Npgquery to latest libpg_query version
2. Add Npgquery-specific tests if needed
3. Document how to sync future Npgquery updates
4. Consider automating Npgquery update process

---

**Branch:** `feature/consolidate-npgquery-library`  
**Status:** 🟡 In Progress
