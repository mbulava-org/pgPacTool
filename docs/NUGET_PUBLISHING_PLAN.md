# NuGet Publishing Plan for pgPacTool

**Version:** 1.0.0-preview1  
**Target:** NuGet.org public repository  
**Branch:** feature/msbuild-sdk-integration  
**Status:** 🟡 Preparation Phase  

---

## 📦 Packages to Publish

| Package | Description | Type | Priority |
|---------|-------------|------|----------|
| **mbulava.PostgreSql.Dac** | Core DAC library for schema operations | Library | 1️⃣ First |
| **MSBuild.Sdk.PostgreSql** | MSBuild SDK for database projects | SDK | 2️⃣ Second |
| **postgresPacTools** | CLI tool for database operations | Global Tool | 3️⃣ Third |

---

## Phase 1: Package Preparation

### ✅ Current State

**MSBuild.Sdk.PostgreSql:**
- [x] Package metadata configured
- [x] README.md included
- [x] Version set to 1.0.0-preview1
- [x] Sdk/ folder structure
- [x] Build task implementation

**mbulava.PostgreSql.Dac:**
- [x] Core functionality complete
- [x] All tests passing (201/201)
- [ ] Package metadata needed
- [ ] README needed

**postgresPacTools:**
- [x] CLI implementation complete
- [x] 23 CLI tests passing
- [ ] Global tool configuration needed

### 🔧 Required Changes

#### 1. Add License Files

**Location:** Root of each package project

```sh
# Copy license to all package projects
copy LICENSE src\libs\mbulava.PostgreSql.Dac\LICENSE.txt
copy LICENSE src\sdk\MSBuild.Sdk.PostgreSql\LICENSE.txt
copy LICENSE src\postgresPacTools\LICENSE.txt
```

**Update .csproj files:**
```xml
<PropertyGroup>
  <PackageLicenseFile>LICENSE.txt</PackageLicenseFile>
</PropertyGroup>

<ItemGroup>
  <None Include="LICENSE.txt" Pack="true" PackagePath="\" />
</ItemGroup>
```

#### 2. Configure mbulava.PostgreSql.Dac Package

**File:** `src/libs/mbulava.PostgreSql.Dac/mbulava.PostgreSql.Dac.csproj`

**Add to PropertyGroup:**
```xml
<!-- Package Configuration -->
<IsPackable>true</IsPackable>
<PackageId>mbulava.PostgreSql.Dac</PackageId>
<Version>1.0.0-preview1</Version>
<Title>PostgreSQL Data-Tier Application Library</Title>
<Description>Core library for PostgreSQL database projects. Provides schema extraction, comparison, validation, and deployment capabilities. Build database-as-code with dependency tracking and circular reference detection.</Description>
<Authors>mbulava-org</Authors>
<Company>mbulava-org</Company>
<Copyright>© mbulava-org. All rights reserved.</Copyright>
<PackageTags>postgresql;database;schema;migration;dacpac;pgpac;devops;cicd</PackageTags>
<PackageLicenseFile>LICENSE.txt</PackageLicenseFile>
<PackageProjectUrl>https://github.com/mbulava-org/pgPacTool</PackageProjectUrl>
<RepositoryUrl>https://github.com/mbulava-org/pgPacTool</RepositoryUrl>
<RepositoryType>git</RepositoryType>
<PackageReadmeFile>README.md</PackageReadmeFile>
```

**Create README.md:**
```markdown
# mbulava.PostgreSql.Dac

Core library for PostgreSQL Data-Tier Application (DAC) operations.

## Features

- Schema extraction from live databases
- Project compilation and validation
- Dependency tracking and ordering
- Circular reference detection
- SQL script generation
- .pgpac package format

## Installation

```sh
dotnet add package mbulava.PostgreSql.Dac
```

## Usage

```csharp
using mbulava.PostgreSql.Dac.Extract;
using mbulava.PostgreSql.Dac.Compile;

// Extract schema
var extractor = new PgProjectExtractor(connectionString);
var project = await extractor.ExtractPgProject("mydb");

// Compile and validate
var compiler = new ProjectCompiler();
var result = compiler.Compile(project);
```

See main documentation: https://github.com/mbulava-org/pgPacTool
```

#### 3. Configure postgresPacTools as Global Tool

**File:** `src/postgresPacTools/postgresPacTools.csproj`

**Add to PropertyGroup:**
```xml
<!-- Global Tool Configuration -->
<PackAsTool>true</PackAsTool>
<ToolCommandName>pgpac</ToolCommandName>
<IsPackable>true</IsPackable>
<Version>1.0.0-preview1</Version>
<Title>PostgreSQL Data-Tier Application Tools</Title>
<Description>Command-line tools for PostgreSQL database lifecycle management. Extract, compile, validate, and deploy database schemas as code. SqlPackage equivalent for PostgreSQL.</Description>
<PackageTags>postgresql;cli;tool;database;sqlpackage;dacpac;pgpac</PackageTags>
<PackageLicenseFile>LICENSE.txt</PackageLicenseFile>
<PackageReadmeFile>README.md</PackageReadmeFile>
```

**Create README.md:**
```markdown
# postgresPacTools

Command-line tools for PostgreSQL database projects.

## Installation

```sh
dotnet tool install --global postgresPacTools
```

## Usage

```sh
# Extract database schema
pgpac extract -scs "Host=localhost;Database=mydb;..." -tf mydb.pgproj.json

# Compile project
pgpac compile -sf mydb.csproj

# Publish to database
pgpac publish -sf mydb.pgpac -tcs "Host=prod;..."

# Generate script
pgpac script -sf mydb.pgpac -tcs "Host=prod;..." -of deploy.sql
```

Full documentation: https://github.com/mbulava-org/pgPacTool
```

#### 4. Create Shared Version File

**File:** `Directory.Build.props` (at solution root)

```xml
<Project>
  <PropertyGroup>
    <!-- Shared version for all packages -->
    <Version>1.0.0-preview1</Version>
    <Authors>mbulava-org</Authors>
    <Company>mbulava-org</Company>
    <Copyright>© mbulava-org. All rights reserved.</Copyright>
    <PackageProjectUrl>https://github.com/mbulava-org/pgPacTool</PackageProjectUrl>
    <RepositoryUrl>https://github.com/mbulava-org/pgPacTool</RepositoryUrl>
    <RepositoryType>git</RepositoryType>
  </PropertyGroup>
</Project>
```

#### 5. Add Package Icons (Optional but Recommended)

**Create icon:** `icon.png` (128x128 px, elephant logo + database)

**Add to each .csproj:**
```xml
<PropertyGroup>
  <PackageIcon>icon.png</PackageIcon>
</PropertyGroup>

<ItemGroup>
  <None Include="..\..\..\..\icon.png" Pack="true" PackagePath="\" />
</ItemGroup>
```

---

## Phase 2: Local Testing

### Step 1: Build Packages

```powershell
# Clean previous builds
dotnet clean -c Release

# Restore dependencies
dotnet restore

# Build all projects
dotnet build -c Release --no-restore

# Pack packages
dotnet pack src/libs/mbulava.PostgreSql.Dac/mbulava.PostgreSql.Dac.csproj -c Release --no-build
dotnet pack src/sdk/MSBuild.Sdk.PostgreSql/MSBuild.Sdk.PostgreSql.csproj -c Release --no-build
dotnet pack src/postgresPacTools/postgresPacTools.csproj -c Release --no-build
```

**Expected Output:**
```
src/libs/mbulava.PostgreSql.Dac/bin/Release/mbulava.PostgreSql.Dac.1.0.0-preview1.nupkg
src/sdk/MSBuild.Sdk.PostgreSql/bin/Release/MSBuild.Sdk.PostgreSql.1.0.0-preview1.nupkg
src/postgresPacTools/bin/Release/postgresPacTools.1.0.0-preview1.nupkg
```

### Step 2: Create Local NuGet Feed

```powershell
# Create local feed directory
$localFeed = "C:\LocalNuGet"
New-Item -ItemType Directory -Path $localFeed -Force

# Add local feed as source
dotnet nuget add source $localFeed --name LocalFeed

# Copy packages to local feed
Copy-Item "src\libs\mbulava.PostgreSql.Dac\bin\Release\*.nupkg" $localFeed
Copy-Item "src\sdk\MSBuild.Sdk.PostgreSql\bin\Release\*.nupkg" $localFeed
Copy-Item "src\postgresPacTools\bin\Release\*.nupkg" $localFeed

# Verify packages
Get-ChildItem $localFeed
```

### Step 3: Test SDK Installation

```powershell
# Create test directory
$testDir = ".\TestSDKInstall"
New-Item -ItemType Directory -Path $testDir -Force
Set-Location $testDir

# Create test database project
@"
<Project Sdk="MSBuild.Sdk.PostgreSql/1.0.0-preview1">

  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <DatabaseName>TestDatabase</DatabaseName>
  </PropertyGroup>

</Project>
"@ | Out-File -FilePath "TestDatabase.csproj" -Encoding UTF8

# Create test SQL files
New-Item -ItemType Directory -Path "Tables" -Force
@"
CREATE TABLE users (
    id SERIAL PRIMARY KEY,
    username VARCHAR(100) NOT NULL
);
"@ | Out-File -FilePath "Tables\users.sql" -Encoding UTF8

# Configure NuGet to use local feed
@"
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <add key="LocalFeed" value="C:\LocalNuGet" />
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
  </packageSources>
</configuration>
"@ | Out-File -FilePath "nuget.config" -Encoding UTF8

# Try to build
dotnet restore
dotnet build

# Verify output
Test-Path "bin\Debug\net10.0\TestDatabase.pgpac"
```

**Expected Output:**
```
✅ Build succeeded
✅ bin/Debug/net10.0/TestDatabase.pgpac created
```

### Step 4: Test Global Tool

```powershell
# Install from local feed
dotnet tool install --global postgresPacTools --version 1.0.0-preview1 --add-source C:\LocalNuGet

# Test CLI
pgpac --version
pgpac --help
pgpac compile --help

# Uninstall after testing
dotnet tool uninstall --global postgresPacTools
```

### Step 5: Test DAC Library

Create test console app:
```powershell
dotnet new console -n TestDacLib
cd TestDacLib
dotnet add package mbulava.PostgreSql.Dac --version 1.0.0-preview1 --source C:\LocalNuGet
```

```csharp
// Program.cs
using mbulava.PostgreSql.Dac.Models;

var project = new PgProject { DatabaseName = "test" };
Console.WriteLine($"Created project: {project.DatabaseName}");
```

```powershell
dotnet run
```

---

## Phase 3: NuGet.org Account Setup

### Step 1: Create NuGet.org Account

1. Navigate to: https://www.nuget.org/
2. Click "Sign in"
3. Choose authentication:
   - Microsoft Account, OR
   - GitHub Account
4. Complete registration
5. Verify email address

### Step 2: Generate API Key

1. Go to: https://www.nuget.org/account/apikeys
2. Click "Create" button
3. Configure API key:
   - **Key Name:** `pgPacTool-Publisher`
   - **Glob Pattern:** `MSBuild.Sdk.PostgreSql,mbulava.PostgreSql.Dac,postgresPacTools`
   - **Select Scopes:**
     - ✅ `Push new packages and package versions`
   - **Select Packages:**
     - ⚪ All packages (if first time)
     - 🔘 Glob pattern (if specific packages)
   - **Expiration:** 365 days (recommended)

4. Click "Create"
5. **IMPORTANT:** Copy the API key immediately (shown only once!)
6. Save securely (password manager, Azure Key Vault, etc.)

### Step 3: Store API Key Locally

```powershell
# Option 1: Store in .NET user secrets (recommended for development)
dotnet user-secrets init
dotnet user-secrets set "NuGet:ApiKey" "YOUR_API_KEY_HERE"

# Option 2: Store in NuGet configuration
dotnet nuget setapikey YOUR_API_KEY_HERE --source https://api.nuget.org/v3/index.json

# Option 3: Use environment variable
$env:NUGET_API_KEY = "YOUR_API_KEY_HERE"
```

---

## Phase 4: Publishing to NuGet.org

### Pre-Flight Checklist

Before publishing, verify:

- [ ] All tests passing (201/201)
- [ ] Package metadata complete
- [ ] License files included
- [ ] README files included
- [ ] Version numbers consistent (1.0.0-preview1)
- [ ] Local testing completed successfully
- [ ] NuGet.org account created
- [ ] API key generated and stored
- [ ] No sensitive data in packages
- [ ] Documentation up to date

### Publishing Order (IMPORTANT!)

**⚠️ Publish in this specific order to avoid dependency issues:**

1. **mbulava.PostgreSql.Dac** (foundation library)
2. Wait for NuGet.org indexing (5-10 minutes)
3. **MSBuild.Sdk.PostgreSql** (depends on DAC)
4. **postgresPacTools** (depends on DAC)

### Step 1: Publish mbulava.PostgreSql.Dac

```powershell
# Navigate to package location
cd src\libs\mbulava.PostgreSql.Dac\bin\Release

# Publish to NuGet.org
dotnet nuget push mbulava.PostgreSql.Dac.1.0.0-preview1.nupkg `
  --source https://api.nuget.org/v3/index.json `
  --api-key $env:NUGET_API_KEY

# OR (if API key stored in config)
dotnet nuget push mbulava.PostgreSql.Dac.1.0.0-preview1.nupkg `
  --source https://api.nuget.org/v3/index.json
```

**Expected Output:**
```
Pushing mbulava.PostgreSql.Dac.1.0.0-preview1.nupkg to 'https://www.nuget.org/api/v2/package'...
  PUT https://www.nuget.org/api/v2/package/
  Created https://www.nuget.org/api/v2/package/ 2034ms
Your package was pushed.
```

**Verify:**
```
https://www.nuget.org/packages/mbulava.PostgreSql.Dac/1.0.0-preview1
```

### Step 2: Wait for Indexing

**NuGet.org requires time to index packages before they can be referenced as dependencies.**

```powershell
# Wait 5-10 minutes
Write-Host "Waiting for NuGet.org to index mbulava.PostgreSql.Dac..."
Start-Sleep -Seconds 300

# Check if available
$response = Invoke-WebRequest -Uri "https://api.nuget.org/v3-flatcontainer/mbulava.postgresql.dac/1.0.0-preview1/mbulava.postgresql.dac.1.0.0-preview1.nupkg"
if ($response.StatusCode -eq 200) {
    Write-Host "✅ Package indexed and ready!"
} else {
    Write-Host "⏳ Still indexing, wait longer..."
}
```

### Step 3: Publish MSBuild.Sdk.PostgreSql

```powershell
cd ..\..\..\..\sdk\MSBuild.Sdk.PostgreSql\bin\Release

dotnet nuget push MSBuild.Sdk.PostgreSql.1.0.0-preview1.nupkg `
  --source https://api.nuget.org/v3/index.json `
  --api-key $env:NUGET_API_KEY
```

**Verify:**
```
https://www.nuget.org/packages/MSBuild.Sdk.PostgreSql/1.0.0-preview1
```

### Step 4: Publish postgresPacTools

```powershell
cd ..\..\..\postgresPacTools\bin\Release

dotnet nuget push postgresPacTools.1.0.0-preview1.nupkg `
  --source https://api.nuget.org/v3/index.json `
  --api-key $env:NUGET_API_KEY
```

**Verify:**
```
https://www.nuget.org/packages/postgresPacTools/1.0.0-preview1
```

### Step 5: Verify All Packages Published

```powershell
# Check all three packages
$packages = @(
    "mbulava.PostgreSql.Dac",
    "MSBuild.Sdk.PostgreSql",
    "postgresPacTools"
)

foreach ($pkg in $packages) {
    $url = "https://www.nuget.org/packages/$pkg/1.0.0-preview1"
    Write-Host "Checking: $url"
    try {
        $response = Invoke-WebRequest -Uri $url -Method Head
        Write-Host "✅ $pkg published successfully!" -ForegroundColor Green
    } catch {
        Write-Host "❌ $pkg not found or still indexing..." -ForegroundColor Red
    }
}
```

---

## Phase 5: Documentation & Announcement

### Step 1: Update Main README.md

Add installation instructions at the top:

````markdown
## 🚀 Quick Start

### Option 1: MSBuild SDK (Recommended)

Create a database project:

```xml
<!-- MyDatabase.csproj -->
<Project Sdk="MSBuild.Sdk.PostgreSql/1.0.0-preview1">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <DatabaseName>MyDatabase</DatabaseName>
  </PropertyGroup>
</Project>
```

Add SQL files and build:

```sh
dotnet build  # Generates MyDatabase.pgpac
```

### Option 2: CLI Tool

```sh
# Install
dotnet tool install --global postgresPacTools

# Use
pgpac extract -scs "Host=localhost;..." -tf mydb.pgproj.json
pgpac compile -sf mydb.csproj
pgpac publish -sf mydb.pgpac -tcs "Host=prod;..."
```

### Option 3: Library

```sh
dotnet add package mbulava.PostgreSql.Dac
```

```csharp
using mbulava.PostgreSql.Dac.Extract;

var extractor = new PgProjectExtractor(connectionString);
var project = await extractor.ExtractPgProject("mydb");
```
````

### Step 2: Create GitHub Release

1. Go to: https://github.com/mbulava-org/pgPacTool/releases/new
2. Configure release:
   - **Tag:** `v1.0.0-preview1`
   - **Target:** `main` (after merging feature branch)
   - **Title:** `pgPacTool v1.0.0-preview1 - Initial Preview Release`
   - **Description:**

```markdown
# 🎉 pgPacTool v1.0.0-preview1

First preview release of PostgreSQL Data-Tier Application Tools!

## 📦 What's Included

- **MSBuild.Sdk.PostgreSql** - Build database projects with MSBuild
- **postgresPacTools** - CLI for extract/publish/script operations  
- **mbulava.PostgreSql.Dac** - Core library for programmatic use

## ✨ Features

- ✅ SQL Server SqlPackage-style workflow for PostgreSQL
- ✅ Convention-based project structure
- ✅ Automatic SQL file discovery
- ✅ Dependency validation
- ✅ Incremental builds
- ✅ CI/CD ready

## 🚀 Installation

### MSBuild SDK
```xml
<Project Sdk="MSBuild.Sdk.PostgreSql/1.0.0-preview1">
  ...
</Project>
```

### CLI Tool
```sh
dotnet tool install --global postgresPacTools
```

## 📚 Documentation

- [MSBuild SDK Guide](docs/SDK_PROJECT_GUIDE.md)
- [CLI Reference](docs/CLI_REFERENCE.md)
- [Getting Started](README.md)

## 🐛 Known Issues

- [ ] Multi-schema support limited (see #1)
- [ ] Pre/post deployment scripts need manual configuration (see docs)

## 📝 Breaking Changes

None (initial release)

## 🙏 Acknowledgments

Inspired by MSBuild.Sdk.SqlProj for SQL Server.

## 💬 Feedback

Please report issues at: https://github.com/mbulava-org/pgPacTool/issues
```

3. **Attach files:**
   - `mbulava.PostgreSql.Dac.1.0.0-preview1.nupkg`
   - `MSBuild.Sdk.PostgreSql.1.0.0-preview1.nupkg`
   - `postgresPacTools.1.0.0-preview1.nupkg`

4. Check: **☑️ This is a pre-release**
5. Click **"Publish release"**

### Step 3: Update CHANGELOG.md

Create `CHANGELOG.md` at repository root:

```markdown
# Changelog

All notable changes to pgPacTool will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.0.0-preview1] - 2024-XX-XX

### Added

- MSBuild SDK for PostgreSQL database projects
- CLI tool with extract, publish, script, compile, deploy-report commands
- Core DAC library with schema operations
- Automatic SQL file discovery
- Dependency validation and ordering
- .pgpac package format
- Incremental build support
- 201 unit and integration tests

### Documentation

- MSBuild SDK guide
- CLI reference
- API documentation
- Quick start guide

[1.0.0-preview1]: https://github.com/mbulava-org/pgPacTool/releases/tag/v1.0.0-preview1
```

### Step 4: Announcements

#### GitHub Discussions

Create discussion: https://github.com/mbulava-org/pgPacTool/discussions

**Title:** "pgPacTool v1.0.0-preview1 Released! 🎉"

**Content:**
```markdown
We're excited to announce the first preview release of pgPacTool!

## What is it?

pgPacTool brings SQL Server-style database project workflow to PostgreSQL. Build your database schema as code with MSBuild, just like SqlPackage/SSDT.

## Quick Example

```xml
<Project Sdk="MSBuild.Sdk.PostgreSql/1.0.0-preview1">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
  </PropertyGroup>
</Project>
```

```sh
dotnet build  # → MyDatabase.pgpac
```

## Installation

```sh
dotnet tool install --global postgresPacTools
```

## We Need Your Feedback!

This is a preview release. Please try it out and let us know:
- What works well?
- What's missing?
- What's confusing?

Report issues: https://github.com/mbulava-org/pgPacTool/issues

## Next Steps

- Multi-schema improvements
- Visual Studio integration
- Azure DevOps tasks
- More examples

Thanks for your interest! 🐘
```

#### PostgreSQL Community

**PostgreSQL Mailing Lists:**
- pgsql-general@postgresql.org
- pgsql-hackers@postgresql.org (if appropriate)

**Subject:** [ANNOUNCE] pgPacTool - MSBuild SDK for PostgreSQL Database Projects

**Body:**
```
Hello PostgreSQL community,

I'm pleased to announce the first preview release of pgPacTool, a tool that brings SQL Server-style database project workflow to PostgreSQL.

What it does:
- Build database projects with MSBuild/dotnet build
- Extract schemas from live databases  
- Generate deployment scripts
- Validate dependencies and circular references
- CI/CD integration

Similar to SqlPackage/SSDT for SQL Server, but for PostgreSQL.

Quick start:
  dotnet tool install --global postgresPacTools
  pgpac extract -scs "Host=localhost;Database=mydb;..." -tf mydb.pgproj.json

Project page: https://github.com/mbulava-org/pgPacTool
NuGet: https://www.nuget.org/packages/MSBuild.Sdk.PostgreSql

This is a preview release - feedback welcome!

Best regards,
[Your name]
```

#### Reddit: r/PostgreSQL

**Title:** [Tool] pgPacTool - Build PostgreSQL databases with MSBuild (like SSDT for SQL Server)

**Content:**
```markdown
Hi r/PostgreSQL! 👋

I've built a tool that lets you manage PostgreSQL databases using MSBuild, similar to how SQL Server Database Projects work in Visual Studio.

## What does it do?

- **Extract** schema from live database
- **Build** database from .sql files (with `dotnet build`)  
- **Validate** dependencies automatically
- **Deploy** with diff scripts
- **CI/CD** ready

## Example

```xml
<!-- MyDatabase.csproj -->
<Project Sdk="MSBuild.Sdk.PostgreSql/1.0.0-preview1">
  <PropertyGroup>
    <DatabaseName>MyDatabase</DatabaseName>
  </PropertyGroup>
</Project>
```

Put SQL files in folders (Tables/, Views/, etc.) and run:

```sh
dotnet build  # Generates MyDatabase.pgpac
```

## Links

- GitHub: https://github.com/mbulava-org/pgPacTool
- NuGet: https://www.nuget.org/packages/MSBuild.Sdk.PostgreSql
- Docs: [link to docs]

This is a preview release. Feedback appreciated!

---

*Inspired by MSBuild.Sdk.SqlProj for SQL Server*
```

#### Dev.to Blog Post

**Title:** Introducing pgPacTool: Database-as-Code for PostgreSQL

**Tags:** `#postgresql` `#dotnet` `#devops` `#database`

**Content:** (See next section for full blog post template)

#### Twitter/X

```
🎉 Excited to announce pgPacTool v1.0.0-preview1!

Build PostgreSQL databases with MSBuild, just like SQL Server SSDT.

✅ Extract schema  
✅ Build with `dotnet build`
✅ Deploy with validation
✅ CI/CD ready

Try it: dotnet tool install --global postgresPacTools

Docs: https://github.com/mbulava-org/pgPacTool

#PostgreSQL #dotnet #DevOps #DatabaseAsCode
```

---

## Phase 6: CI/CD Automation

### GitHub Actions Workflow

Create `.github/workflows/publish-nuget.yml`:

```yaml
name: Publish to NuGet

on:
  push:
    tags:
      - 'v*'
  workflow_dispatch:
    inputs:
      version:
        description: 'Version to publish (e.g., 1.0.0-preview1)'
        required: true

jobs:
  publish:
    runs-on: ubuntu-latest
    
    steps:
      - name: Checkout code
        uses: actions/checkout@v4
        with:
          fetch-depth: 0  # Full history for versioning
      
      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '10.0.x'
      
      - name: Restore dependencies
        run: dotnet restore
      
      - name: Build
        run: dotnet build -c Release --no-restore
      
      - name: Test
        run: dotnet test -c Release --no-build --verbosity normal
      
      - name: Pack packages
        run: |
          dotnet pack src/libs/mbulava.PostgreSql.Dac/mbulava.PostgreSql.Dac.csproj -c Release --no-build --output ./packages
          dotnet pack src/sdk/MSBuild.Sdk.PostgreSql/MSBuild.Sdk.PostgreSql.csproj -c Release --no-build --output ./packages
          dotnet pack src/postgresPacTools/postgresPacTools.csproj -c Release --no-build --output ./packages
      
      - name: List packages
        run: ls -la ./packages
      
      - name: Publish mbulava.PostgreSql.Dac to NuGet
        run: |
          dotnet nuget push ./packages/mbulava.PostgreSql.Dac.*.nupkg \
            --api-key ${{ secrets.NUGET_API_KEY }} \
            --source https://api.nuget.org/v3/index.json \
            --skip-duplicate
      
      - name: Wait for NuGet indexing
        run: |
          echo "Waiting for mbulava.PostgreSql.Dac to be indexed..."
          sleep 300  # 5 minutes
      
      - name: Publish MSBuild.Sdk.PostgreSql to NuGet
        run: |
          dotnet nuget push ./packages/MSBuild.Sdk.PostgreSql.*.nupkg \
            --api-key ${{ secrets.NUGET_API_KEY }} \
            --source https://api.nuget.org/v3/index.json \
            --skip-duplicate
      
      - name: Publish postgresPacTools to NuGet
        run: |
          dotnet nuget push ./packages/postgresPacTools.*.nupkg \
            --api-key ${{ secrets.NUGET_API_KEY }} \
            --source https://api.nuget.org/v3/index.json \
            --skip-duplicate
      
      - name: Create GitHub Release
        uses: softprops/action-gh-release@v1
        if: startsWith(github.ref, 'refs/tags/')
        with:
          files: ./packages/*.nupkg
          draft: false
          prerelease: true
        env:
          GITHUB_TOKEN: ${{ secrets.GITHUB_TOKEN }}
```

### Setup GitHub Secrets

1. Go to: https://github.com/mbulava-org/pgPacTool/settings/secrets/actions
2. Click "New repository secret"
3. Add:
   - **Name:** `NUGET_API_KEY`
   - **Value:** [Your NuGet.org API key]
4. Click "Add secret"

### Trigger Publication

#### Option 1: Git Tag (Recommended)

```powershell
# Create and push tag
git tag v1.0.0-preview1
git push origin v1.0.0-preview1

# Workflow runs automatically
```

#### Option 2: Manual Trigger

1. Go to: https://github.com/mbulava-org/pgPacTool/actions
2. Select "Publish to NuGet" workflow
3. Click "Run workflow"
4. Enter version: `1.0.0-preview1`
5. Click "Run workflow"

---

## Versioning Strategy

### Preview Releases

| Version | Purpose |
|---------|---------|
| `1.0.0-preview1` | Initial public preview |
| `1.0.0-preview2` | Bug fixes, feedback incorporated |
| `1.0.0-preview3` | More fixes, stabilization |
| `1.0.0-rc1` | Release candidate 1 |
| `1.0.0-rc2` | Release candidate 2 (if needed) |

### Stable Releases

| Version | Type | When |
|---------|------|------|
| `1.0.0` | Major | First stable release |
| `1.0.1` | Patch | Bug fixes only |
| `1.1.0` | Minor | New features (backward compatible) |
| `2.0.0` | Major | Breaking changes |

### Version Update Process

```powershell
# Update version in Directory.Build.props
$newVersion = "1.0.0-preview2"

# Find and replace
(Get-Content Directory.Build.props) -replace '<Version>.*</Version>', "<Version>$newVersion</Version>" | Set-Content Directory.Build.props

# Commit
git add Directory.Build.props
git commit -m "Bump version to $newVersion"
git push

# Tag and publish
git tag "v$newVersion"
git push origin "v$newVersion"
```

---

## Checklist: Ready to Publish?

### Pre-Publish Verification

- [ ] **All tests passing**
  - [ ] 201/201 unit tests passing
  - [ ] Integration tests passing
  - [ ] CLI tests passing

- [ ] **Package configuration complete**
  - [ ] mbulava.PostgreSql.Dac.csproj metadata
  - [ ] MSBuild.Sdk.PostgreSql.csproj metadata  
  - [ ] postgresPacTools.csproj metadata
  - [ ] License files added
  - [ ] README files added
  - [ ] Version numbers consistent

- [ ] **Local testing completed**
  - [ ] Packages build successfully
  - [ ] SDK test project builds
  - [ ] CLI tool installs and runs
  - [ ] DAC library can be referenced

- [ ] **Documentation ready**
  - [ ] Main README.md updated
  - [ ] SDK_PROJECT_GUIDE.md complete
  - [ ] CLI_REFERENCE.md complete
  - [ ] CHANGELOG.md created

- [ ] **NuGet.org setup**
  - [ ] Account created
  - [ ] Email verified
  - [ ] API key generated
  - [ ] API key stored securely

- [ ] **GitHub setup**
  - [ ] Release notes drafted
  - [ ] NUGET_API_KEY secret added
  - [ ] CI/CD workflow committed

### Post-Publish Verification

- [ ] **Packages available on NuGet.org**
  - [ ] mbulava.PostgreSql.Dac visible
  - [ ] MSBuild.Sdk.PostgreSql visible
  - [ ] postgresPacTools visible

- [ ] **Installation works**
  - [ ] SDK installs from NuGet.org
  - [ ] CLI tool installs globally
  - [ ] DAC library can be added to projects

- [ ] **Documentation published**
  - [ ] GitHub release created
  - [ ] CHANGELOG updated
  - [ ] README shows installation

- [ ] **Announcements made**
  - [ ] GitHub Discussions post
  - [ ] PostgreSQL mailing list
  - [ ] Reddit r/PostgreSQL
  - [ ] Twitter/X
  - [ ] Dev.to article

---

## Troubleshooting

### Common Issues

#### "Package already exists"

**Problem:** Trying to push a version that's already published.

**Solution:**
```powershell
# Increment version
$newVersion = "1.0.0-preview2"
# Update Directory.Build.props
# Rebuild and push
```

#### "Dependency not found"

**Problem:** MSBuild.Sdk.PostgreSql can't find mbulava.PostgreSql.Dac

**Solution:**
- Wait 10-15 minutes for NuGet indexing
- Check package is actually published: `https://www.nuget.org/packages/mbulava.PostgreSql.Dac`
- Clear NuGet cache: `dotnet nuget locals all --clear`

#### "Build failed - SDK not found"

**Problem:** Test project can't find MSBuild.Sdk.PostgreSql

**Solution:**
```xml
<!-- Add to test project -->
<PropertyGroup>
  <RestoreSources>
    https://api.nuget.org/v3/index.json;
    C:\LocalNuGet;
  </RestoreSources>
</PropertyGroup>
```

#### "401 Unauthorized"

**Problem:** API key invalid or expired

**Solution:**
- Generate new API key on NuGet.org
- Update stored key: `dotnet nuget setapikey NEW_KEY --source https://api.nuget.org/v3/index.json`

---

## Timeline Estimate

| Phase | Tasks | Estimated Time |
|-------|-------|----------------|
| **1. Preparation** | Add metadata, licenses, READMEs | 2-3 hours |
| **2. Local Testing** | Build, pack, test locally | 1-2 hours |
| **3. Account Setup** | NuGet.org account, API key | 15-30 minutes |
| **4. Publishing** | Push packages, wait for indexing | 30-60 minutes |
| **5. Documentation** | Update docs, create release | 2-3 hours |
| **6. CI/CD** | GitHub Actions workflow | 1-2 hours |
| **7. Announcements** | Posts, articles, outreach | 2-4 hours |

**Total:** 9-15 hours for first publication

**Subsequent updates:** 30 minutes to 2 hours (automated)

---

## Success Metrics

### Week 1

- [ ] 100+ NuGet downloads combined
- [ ] 10+ GitHub stars
- [ ] 5+ GitHub issues/questions

### Month 1

- [ ] 500+ NuGet downloads combined
- [ ] 50+ GitHub stars
- [ ] 10+ active users

### Quarter 1

- [ ] 2,000+ NuGet downloads
- [ ] 100+ GitHub stars
- [ ] Stable 1.0.0 release
- [ ] Community contributions

---

## Next Version Planning

### v1.0.0-preview2 Planned Features

- Multi-schema support improvements
- Pre/post deployment auto-discovery
- Better error messages
- Performance optimizations
- More examples

### Feedback Collection

- GitHub Issues for bugs
- GitHub Discussions for features
- Community survey after 1 month

---

## Contact & Support

**Questions:** Open GitHub Discussion  
**Bugs:** Open GitHub Issue  
**Email:** [your-email]@[domain]  
**Twitter:** @[your-handle]

---

**Last Updated:** 2024-[DATE]  
**Next Review:** After v1.0.0-preview1 publication  
**Status:** 🟢 Ready for Implementation
