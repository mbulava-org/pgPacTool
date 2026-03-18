# GitHub Actions Workflows

This directory contains GitHub Actions workflows for automated CI/CD.

## Workflows

### 📦 `publish-preview.yml`

**Purpose**: Publishes preview releases to NuGet.org from the `preview1` branch

**Triggers**:
- Push to `preview1` branch
- Manual workflow dispatch

**Jobs**:
1. **Build and Test** - Compiles solution and runs unit tests
2. **Pack and Publish** - Creates NuGet packages and publishes to NuGet.org
3. **Notify Completion** - Summarizes workflow results

**Secrets Required**:
- `NUGET_API_KEY` - API key for publishing to NuGet.org

**Outputs**:
- ✅ 3 NuGet packages published to NuGet.org
- ✅ GitHub release with packages attached
- ✅ Test results uploaded as artifacts

**Documentation**: See [docs/PUBLISHING.md](../docs/PUBLISHING.md) for detailed setup and usage

---

### 🔜 `publish-release.yml` (Future)

**Purpose**: Will publish stable releases from `main` branch

**Status**: Not yet implemented - will be created when ready for v1.0.0 stable

---

## Setup Instructions

### 1. Add NuGet API Key Secret

1. Generate API key: https://www.nuget.org/account/apikeys
2. Add to repository: https://github.com/mbulava-org/pgPacTool/settings/secrets/actions
3. Name: `NUGET_API_KEY`

### 2. Enable Workflow Permissions

1. Go to: https://github.com/mbulava-org/pgPacTool/settings/actions
2. Select: **Read and write permissions**
3. Enable: **Allow GitHub Actions to create and approve pull requests**

## Quick Reference

### Trigger Preview Release

**Automatic**:
```bash
# Update version in .csproj files, then:
git add .
git commit -m "chore: bump version to 1.0.0-preview2"
git push origin preview1
```

**Manual**:
1. Go to: https://github.com/mbulava-org/pgPacTool/actions/workflows/publish-preview.yml
2. Click: **Run workflow**
3. Select branch: `preview1`
4. Click: **Run workflow**

### Monitor Workflow

- **Workflow Runs**: https://github.com/mbulava-org/pgPacTool/actions
- **Published Packages**: https://www.nuget.org/profiles/mbulava-org
- **Releases**: https://github.com/mbulava-org/pgPacTool/releases

## Troubleshooting

| Issue | Solution |
|-------|----------|
| "NUGET_API_KEY not set" | Add secret to repository settings |
| "Package already exists" | Increment version number in .csproj files |
| Tests fail | Run locally: `dotnet test --filter "Category!=LinuxContainer"` (includes Integration tests) |
| Build fails | Run locally: `.\scripts\Pack-PreviewRelease.ps1 -TestLocally` |

## Related Documentation

- [Publishing Guide](../docs/PUBLISHING.md) - Detailed publishing instructions
- [CHANGELOG.md](../CHANGELOG.md) - Version history and release notes
- [README.md](../README.md) - Main project documentation

---

*For detailed information, see [docs/PUBLISHING.md](../docs/PUBLISHING.md)*
