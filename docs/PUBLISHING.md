# Publishing Workflow Guide

This guide explains how to publish pgPacTool packages using GitHub Actions.

## Overview

The project uses GitHub Actions to automate package publishing to NuGet.org:

- **`publish-preview.yml`** - Publishes preview releases from `preview1` branch
- **Future: `publish-release.yml`** - Will publish stable releases from `main` branch

## Setup Instructions

### 1. Create NuGet API Key

1. Go to https://www.nuget.org/account/apikeys
2. Click **Create**
3. Configure the API key:
   - **Key Name**: `pgPacTool-GitHub-Actions`
   - **Package Owner**: Select your account
   - **Scopes**: Select `Push` and `Push new packages and package versions`
   - **Glob Pattern**: `mbulava.PostgreSql.*, MSBuild.Sdk.PostgreSql*, postgresPacTools*`
   - **Expiration**: Set to 1 year (or longer if preferred)
4. Click **Create**
5. **Copy the API key** (you won't be able to see it again!)

### 2. Add Secret to GitHub Repository

1. Go to https://github.com/mbulava-org/pgPacTool/settings/secrets/actions
2. Click **New repository secret**
3. Name: `NUGET_API_KEY`
4. Value: Paste the API key from step 1
5. Click **Add secret**

### 3. Verify Workflow Permissions

1. Go to https://github.com/mbulava-org/pgPacTool/settings/actions
2. Under **Workflow permissions**, ensure:
   - ✅ **Read and write permissions** is selected
   - ✅ **Allow GitHub Actions to create and approve pull requests** is checked
3. Click **Save** if you made changes

## Publishing a Preview Release

### Method 1: Automatic (Push to preview1 branch)

1. Update version in all `.csproj` files:
   ```xml
   <Version>1.0.0-preview2</Version>
   ```

2. Commit and push to `preview1` branch:
   ```bash
   git add .
   git commit -m "chore: bump version to 1.0.0-preview2"
   git push origin preview1
   ```

3. The workflow will automatically:
   - ✅ Build the solution
   - ✅ Run tests
   - ✅ Pack all 3 NuGet packages
   - ✅ Verify Npgquery embedding
   - ✅ Publish to NuGet.org
   - ✅ Create GitHub release with packages attached

### Method 2: Manual Trigger

1. Go to https://github.com/mbulava-org/pgPacTool/actions/workflows/publish-preview.yml
2. Click **Run workflow**
3. Select branch: `preview1`
4. (Optional) Enter custom version: `1.0.0-preview2`
5. Click **Run workflow**

## Monitoring the Workflow

### View Workflow Run

1. Go to https://github.com/mbulava-org/pgPacTool/actions
2. Click on the workflow run
3. Monitor progress through each job:
   - **Build and Test** - Compiles and tests the code
   - **Pack and Publish** - Creates and publishes packages
   - **Notify Completion** - Summarizes results

### Check Workflow Results

After successful completion:

- **NuGet Packages**: https://www.nuget.org/profiles/mbulava-org
- **GitHub Releases**: https://github.com/mbulava-org/pgPacTool/releases
- **Artifacts**: Download `.nupkg` files from workflow run page

## Package Verification

The workflow automatically verifies:

1. ✅ **Npgquery.dll is embedded** in mbulava.PostgreSql.Dac package
2. ✅ **Native libraries** (pg_query.*) are included for all platforms
3. ✅ **No Npgquery dependency** in package metadata
4. ✅ **All 3 packages** created successfully
5. ✅ **Tests pass** before publishing

## Testing Published Packages

### Test CLI Tool

```bash
# Install globally
dotnet tool install --global postgresPacTools --version 1.0.0-preview1

# Verify installation
pgpac --version

# Test commands
pgpac --help
```

### Test Library Package

```bash
# Create test project
dotnet new console -n TestPgPac
cd TestPgPac

# Add package
dotnet add package mbulava.PostgreSql.Dac --version 1.0.0-preview1

# Build and verify no dependency errors
dotnet build
```

### Test MSBuild SDK

```bash
# Create database project
dotnet new console -n MyDatabase
cd MyDatabase

# Edit .csproj to use SDK
```

```xml
<Project Sdk="MSBuild.Sdk.PostgreSql/1.0.0-preview1">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
  </PropertyGroup>
</Project>
```

```bash
dotnet build
```

## Troubleshooting

### Workflow fails: "NUGET_API_KEY secret not set"

**Solution**: Add the `NUGET_API_KEY` secret (see Setup Instructions above)

### Workflow fails: "dotnet pack failed"

**Causes**:
- Build errors in solution
- Missing dependencies
- Invalid .csproj configuration

**Solution**: Run locally first:
```bash
.\scripts\Pack-PreviewRelease.ps1 -TestLocally
```

### Package publish fails: "409 Conflict"

**Cause**: Package version already exists on NuGet.org

**Solution**: 
- Increment version number in `.csproj` files
- NuGet.org does not allow overwriting existing versions

### Tests fail in workflow

**Causes**:
- Code issues
- Docker not available (shouldn't happen on ubuntu-latest runners)

**Solution**: 
- Workflow includes Integration tests with Docker: `dotnet test --filter "Category!=LinuxContainer"`
- Only LinuxContainer tests are skipped (used for local Linux compatibility testing)
- Verify tests pass locally: `dotnet test --filter "Category!=LinuxContainer"`
- Check test output in workflow logs for specific failures

### Native libraries not found in package

**Cause**: Build target not copying native libraries

**Solution**:
- Verify `IncludeNpgqueryInPackage` target in `mbulava.PostgreSql.Dac.csproj`
- Check `runtimes/` folder structure in Npgquery project

## Version Numbering

### Preview Releases (`preview1` branch)

Format: `MAJOR.MINOR.PATCH-previewN`

Examples:
- `1.0.0-preview6` - Current preview release
- `1.0.0-preview5` - Prior preview release
- `1.1.0-preview1` - New features preview

### Stable Releases (main branch, future)

Format: `MAJOR.MINOR.PATCH`

Examples:
- `1.0.0` - First stable release
- `1.0.1` - Patch release
- `1.1.0` - Minor version with new features
- `2.0.0` - Major version with breaking changes

## Workflow Configuration

## Runtime Dependency Packaging Rule

- The global tool package and the MSBuild SDK package must carry all runtime-critical managed dependencies required after install/restore.
- Do not rely on downstream machines already having assemblies such as `Google.Protobuf`, `Npgsql`, `Npgquery`, `System.CommandLine`, or `mbulava.PostgreSql.Dac` available globally.
- Package-validation tests should verify both the `.nupkg` contents and the files laid down by `dotnet tool install`.

### Trigger Conditions

The workflow runs when:

1. ✅ Code is pushed to `preview1` branch
2. ✅ Manually triggered via GitHub Actions UI
3. ❌ Skips if only `.md` files or docs changed

### Environment Variables

| Variable | Value | Description |
|----------|-------|-------------|
| `DOTNET_VERSION` | `10.0.x` | .NET SDK version |
| `CONFIGURATION` | `Release` | Build configuration |
| `PACKAGES_DIR` | `./packages` | Output directory for `.nupkg` files |

### Secrets Required

| Secret | Purpose | Where to Get |
|--------|---------|--------------|
| `NUGET_API_KEY` | Publish to NuGet.org | https://www.nuget.org/account/apikeys |
| `GITHUB_TOKEN` | Create releases | Automatically provided by GitHub |

## Future Enhancements

### Stable Release Workflow (main branch)

When ready to publish stable releases:

1. Create `publish-release.yml` based on `publish-preview.yml`
2. Change trigger to `main` branch
3. Remove `prerelease: true` from GitHub release step
4. Set `make_latest: true` for GitHub release
5. Update version format to exclude `-preview` suffix

### Additional Features

- 🔜 Automatic version bumping
- 🔜 Changelog generation from git commits
- 🔜 Docker image publishing
- 🔜 Slack/Discord notifications
- 🔜 Multi-stage deployment (test NuGet feed first)

## Support

For issues with the publishing workflow:

1. Check workflow logs: https://github.com/mbulava-org/pgPacTool/actions
2. Review this guide
3. Open an issue: https://github.com/mbulava-org/pgPacTool/issues

---

*Last Updated*: 2026-03-17  
*Maintained By*: pgPacTool Contributors
