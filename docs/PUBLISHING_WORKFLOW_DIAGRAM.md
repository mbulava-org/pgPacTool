```mermaid
# Automated Publishing Workflow

## Current Setup (preview1 branch)

┌─────────────────────────────────────────────────────────────────┐
│                                                                 │
│  Developer pushes to preview1 branch                            │
│  OR manually triggers workflow                                  │
│                                                                 │
└─────────────┬───────────────────────────────────────────────────┘
              │
              │ GitHub Actions: publish-preview.yml
              │
              ▼
┌─────────────────────────────────────────────────────────────────┐
│                                                                 │
│  Job 1: Build and Test                                          │
│  ════════════════════════                                       │
│                                                                 │
│  ✅ Checkout code                                               │
│  ✅ Setup .NET 10                                               │
│  ✅ Restore dependencies                                        │
│  ✅ Build Release configuration                                 │
│  ✅ Run tests with Docker (skip LinuxContainer tests)          │
│  ✅ Upload test results                                         │
│                                                                 │
└─────────────┬───────────────────────────────────────────────────┘
              │
              │ Tests passed ✅
              │
              ▼
┌─────────────────────────────────────────────────────────────────┐
│                                                                 │
│  Job 2: Pack and Publish                                        │
│  ═════════════════════════                                      │
│                                                                 │
│  ✅ Extract version from .csproj                                │
│  ✅ Build solution                                              │
│                                                                 │
│  📦 Pack mbulava.PostgreSql.Dac                                 │
│     └─ Embed Npgquery.dll                                       │
│     └─ Include native libraries                                 │
│                                                                 │
│  📦 Pack MSBuild.Sdk.PostgreSql                                 │
│     └─ Include SDK files                                        │
│                                                                 │
│  📦 Pack postgresPacTools                                       │
│     └─ CLI global tool                                          │
│                                                                 │
│  🔍 Verify packages:                                            │
│     ✅ Npgquery.dll embedded                                    │
│     ✅ Native libraries included                                │
│     ✅ No Npgquery dependency                                   │
│                                                                 │
│  📤 Publish to NuGet.org                                        │
│     └─ Use NUGET_API_KEY secret                                 │
│                                                                 │
│  🏷️  Create GitHub Release                                      │
│     └─ Tag: v1.0.0-preview1                                     │
│     └─ Mark as pre-release                                      │
│     └─ Attach .nupkg files                                      │
│     └─ Auto-generate release notes                             │
│                                                                 │
└─────────────┬───────────────────────────────────────────────────┘
              │
              │ Published ✅
              │
              ▼
┌─────────────────────────────────────────────────────────────────┐
│                                                                 │
│  Packages Available:                                            │
│  ═══════════════════                                            │
│                                                                 │
│  🌐 NuGet.org:                                                  │
│     • mbulava.PostgreSql.Dac                                    │
│     • MSBuild.Sdk.PostgreSql                                    │
│     • postgresPacTools                                          │
│                                                                 │
│  🏷️  GitHub Release:                                            │
│     • Tagged release with .nupkg files                          │
│     • Installation instructions                                 │
│                                                                 │
│  📥 Users can install:                                          │
│     dotnet tool install --global postgresPacTools               │
│     dotnet add package mbulava.PostgreSql.Dac                   │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘

## Future: Stable Releases (main branch)

┌─────────────────────────────────────────────────────────────────┐
│                                                                 │
│  Merge to main branch (stable release)                          │
│                                                                 │
└─────────────┬───────────────────────────────────────────────────┘
              │
              │ GitHub Actions: publish-release.yml (future)
              │
              ▼
┌─────────────────────────────────────────────────────────────────┐
│                                                                 │
│  Same workflow as preview, but:                                 │
│  • Version without -preview suffix                              │
│  • NOT marked as pre-release                                    │
│  • Set as "latest" release                                      │
│  • Production-ready                                             │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘

## Secrets Required

┌───────────────────┬──────────────────────────────────────────────┐
│ Secret            │ Purpose                                      │
├───────────────────┼──────────────────────────────────────────────┤
│ NUGET_API_KEY     │ Publish to NuGet.org                         │
│                   │ (Must be added by repository admin)          │
├───────────────────┼──────────────────────────────────────────────┤
│ GITHUB_TOKEN      │ Create releases                              │
│                   │ (Automatically provided)                     │
└───────────────────┴──────────────────────────────────────────────┘

## Workflow Permissions

┌────────────────────────────────────────────────────────────────┐
│                                                                │
│ Required repository settings:                                  │
│ ✅ Read and write permissions                                  │
│ ✅ Allow GitHub Actions to create and approve pull requests    │
│                                                                │
└────────────────────────────────────────────────────────────────┘
```
