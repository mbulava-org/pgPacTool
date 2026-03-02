# Multi-Version PostgreSQL Support

This folder contains documentation for the **Multi-Version PostgreSQL Support** feature implemented in `feature/multi-postgres-version-support` branch.

## 📋 Quick Navigation

### Getting Started
- **[Quick Reference](QUICK_REFERENCE.md)** - Common tasks and commands
- **[Next Steps](NEXT_STEPS.md)** - How to build native libraries and get started

### Implementation
- **[Implementation Status](PG16_PG17_IMPLEMENTATION_STATUS.md)** - Current status and remaining work
- **[Implementation Complete](IMPLEMENTATION_COMPLETE.md)** - What was built and how it works
- **[Final Summary](FINAL_SUMMARY.md)** - Complete overview of the feature

### Architecture & Design
- **[Multi-Version Design](MULTI_VERSION_DESIGN.md)** - Architecture and design decisions
- **[Version Compatibility Strategy](VERSION_COMPATIBILITY_STRATEGY.md)** - How to handle version differences

### Build & Automation
- **[Native Library Automation](NATIVE_LIBRARY_AUTOMATION.md)** - Complete build automation guide
- **[Version Compatibility Critical](VERSION_COMPATIBILITY_CRITICAL.md)** - Critical requirements checklist

### Progress Tracking
- **[Progress Report](PROGRESS_REPORT.md)** - Development timeline and milestones

---

## 🎯 Feature Overview

**Support for multiple PostgreSQL versions** (currently 16 and 17) with:
- Dynamic native library loading
- Version-specific parsing
- Automated build pipeline
- Comprehensive version compatibility handling

**Supported Versions**:
- ✅ PostgreSQL 16
- ✅ PostgreSQL 17
- ⏳ Older versions (14, 15) may be added in the future if needed

## 🚀 Quick Start

```csharp
// Use PostgreSQL 16 (default)
using var parser = new Parser();

// Or specify PostgreSQL 17
using var parser17 = new Parser(PostgreSqlVersion.Postgres17);

// Check available versions
var versions = NativeLibraryLoader.GetAvailableVersions();
```

## 📚 Documentation Organization

This feature documentation is organized in a dedicated folder to:
- Keep feature-specific implementation details together
- Track development progress and decisions
- Provide comprehensive guides for future contributors
- Maintain history of breaking changes and compatibility strategies

For general project documentation, see the main [README](../../../README.md).

---

**Branch**: `feature/multi-postgres-version-support`
**Status**: Implementation Complete - Awaiting Native Library Builds
**Last Updated**: Current Session
