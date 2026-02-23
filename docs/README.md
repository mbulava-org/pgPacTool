# pgPacTool Documentation

Welcome to the pgPacTool documentation! This directory contains all project documentation organized by topic.

---

## 📚 Quick Navigation

### For New Users
- **[User Guide](USER_GUIDE.md)** - Getting started, usage examples, troubleshooting
- **[README](../README.md)** - Project overview and current status

### For Developers
- **[API Reference](API_REFERENCE.md)** - Complete API documentation with examples
- **[Workflows](WORKFLOWS.md)** - CI/CD setup, GitHub Actions, testing

### For Contributors
- **[Milestone 1 Status](milestone-1/)** - Completed features and implementation notes
- **[Archive](archive/)** - Historical implementation documents

---

## 📁 Documentation Structure

```
docs/
├── README.md                 # This file
├── API_REFERENCE.md          # Complete API documentation
├── USER_GUIDE.md             # User-facing documentation
├── WORKFLOWS.md              # CI/CD and GitHub Actions
├── milestone-1/              # Milestone 1 completion docs
│   └── ...                   # Implementation summaries
└── archive/                  # Historical/completed task docs
    └── ...                   # Old planning documents
```

---

## 📖 Documentation Index

### Core Documentation

| Document | Description | Audience |
|----------|-------------|----------|
| [API Reference](API_REFERENCE.md) | Complete API with code examples | Developers |
| [User Guide](USER_GUIDE.md) | Getting started & usage scenarios | All users |
| [Workflows](WORKFLOWS.md) | CI/CD, testing, coverage | Contributors |

### Project Status

| Document | Description |
|----------|-------------|
| [Main README](../README.md) | Project overview, current state |
| [Milestone 1](milestone-1/) | Completed features |

### Historical

| Document | Description |
|----------|-------------|
| [Archive](archive/) | Completed implementation docs |

---

## 🎯 What Documentation Covers

### Current Functionality (Milestone 1 - Complete ✅)

**Extraction:**
- ✅ Database schema extraction
- ✅ Tables (with columns, constraints, indexes, privileges)
- ✅ Views (regular and materialized)
- ✅ Functions and procedures
- ✅ Triggers
- ✅ Sequences
- ✅ User-defined types (domain, enum, composite)
- ✅ Roles and privileges
- ✅ AST parsing for all objects

**Comparison:**
- ✅ Schema comparison (basic)
- ✅ Difference detection

**Infrastructure:**
- ✅ PostgreSQL 16+ support
- ✅ .NET 10 library
- ✅ JSON serialization
- ✅ CI/CD with code coverage

### Future Functionality (Planned)

**Milestone 2 - Compilation & Validation:**
- 🚧 Dependency validation
- 🚧 Circular dependency detection
- 🚧 Build artifacts

**Milestone 3 - Schema Comparison & Scripts:**
- 🚧 Migration script generation
- 🚧 Pre/post deployment scripts
- 🚧 SQLCMD variables

**Milestone 4 - Deployment:**
- 🚧 Deployment automation
- 🚧 Rollback support
- 🚧 Publishing profiles

**Milestone 5 - Packaging:**
- 🚧 NuGet packages
- 🚧 Package references

**Milestone 6 - MSBuild SDK:**
- 🚧 .pgproj file support
- 🚧 MSBuild integration
- 🚧 Project templates

---

## 🚀 Quick Start Paths

### "I want to use pgPacTool"
1. Read [User Guide](USER_GUIDE.md) - Getting Started
2. Check [API Reference](API_REFERENCE.md) - Usage Examples
3. Run your first extraction

### "I want to contribute"
1. Read [Workflows](WORKFLOWS.md) - Development setup
2. Check [API Reference](API_REFERENCE.md) - Architecture
3. Review [Milestone 1](milestone-1/) - Current state

### "I need help"
1. Check [User Guide](USER_GUIDE.md) - Troubleshooting
2. Review [API Reference](API_REFERENCE.md) - Examples
3. Open an issue on GitHub

---

## 📝 Documentation Standards

### Writing Style
- **Clear and concise** - Get to the point
- **Code examples** - Show, don't just tell
- **Up-to-date** - Reflect current functionality
- **Tested** - All examples should work

### Structure
- **Overview** - What is this about?
- **Quick start** - How do I use it?
- **Details** - How does it work?
- **Examples** - Show me real usage
- **Troubleshooting** - Common issues

### Maintenance
- Update docs with code changes
- Archive outdated documents
- Version control all changes
- Review quarterly

---

## 🔍 Finding What You Need

### By Task

| I want to... | Read this... |
|--------------|--------------|
| Extract a database | [User Guide - Getting Started](USER_GUIDE.md#getting-started) |
| Use the API | [API Reference](API_REFERENCE.md) |
| Compare schemas | [API Reference - Comparison](API_REFERENCE.md#comparison-api) |
| Set up CI/CD | [Workflows](WORKFLOWS.md) |
| Run tests | [Workflows - Testing](WORKFLOWS.md#postgresql-testing) |
| Troubleshoot | [User Guide - Troubleshooting](USER_GUIDE.md#troubleshooting) |

### By Component

| Component | Documentation |
|-----------|---------------|
| PgProjectExtractor | [API Reference - Extraction](API_REFERENCE.md#extraction-api) |
| PgSchemaComparer | [API Reference - Comparison](API_REFERENCE.md#comparison-api) |
| Data Models | [API Reference - Models](API_REFERENCE.md#data-models) |
| GitHub Actions | [Workflows](WORKFLOWS.md) |

### By Role

| Role | Start Here |
|------|-----------|
| **End User** | [User Guide](USER_GUIDE.md) |
| **Developer** | [API Reference](API_REFERENCE.md) |
| **Contributor** | [Workflows](WORKFLOWS.md) |
| **Maintainer** | All documents |

---

## 📚 Additional Resources

### External Documentation
- [PostgreSQL Official Docs](https://www.postgresql.org/docs/)
- [Npgsql Documentation](https://www.npgsql.org/)
- [.NET 10 Documentation](https://docs.microsoft.com/dotnet/)

### Project Links
- [GitHub Repository](https://github.com/mbulava-org/pgPacTool)
- [Issue Tracker](https://github.com/mbulava-org/pgPacTool/issues)
- [Discussions](https://github.com/mbulava-org/pgPacTool/discussions)

### Related Projects
- [MSBuild.Sdk.SqlProj](https://github.com/rr-wfm/MSBuild.Sdk.SqlProj) - SQL Server inspiration
- [pg_query](https://github.com/pganalyze/pg_query) - PostgreSQL parser
- [libpg_query](https://github.com/pganalyze/libpg_query) - Native parsing library

---

## 💡 Tips for Documentation

### For Readers
- Use Ctrl+F to find specific topics
- Check the [User Guide](USER_GUIDE.md) troubleshooting first
- Examples are your friend - try them!

### For Contributors
- Update docs with PRs
- Add examples for new features
- Keep docs in sync with code
- Archive old information

---

## 📞 Getting Help

### Documentation Issues
- Found a typo? Open a PR
- Documentation unclear? Open an issue
- Missing information? Let us know

### Technical Issues
- Check [User Guide - Troubleshooting](USER_GUIDE.md#troubleshooting)
- Search existing issues
- Open a new issue with details

---

## 🎉 Documentation Updates

### Recent Updates
- **2026-01-31**: Complete Milestone 1 documentation
  - API Reference created
  - User Guide created
  - Workflows documented
  - AST types fixed

### Planned Updates
- Milestone 2 documentation (Compilation)
- Migration script generation guide
- Deployment automation guide
- Performance optimization guide

---

## ✅ Documentation Checklist

When adding new features, update:
- [ ] API Reference - New methods/classes
- [ ] User Guide - Usage examples
- [ ] Workflows - If CI/CD changes
- [ ] README.md - Current status
- [ ] This index - New documents

---

**Last Updated:** 2026-01-31  
**Documentation Version:** 1.0  
**Project Version:** 0.1.0 (Milestone 1)

---

**Questions?** Open an issue or check the [User Guide](USER_GUIDE.md) troubleshooting section.

**Contributing?** Read the [Workflows](WORKFLOWS.md) guide for development setup.

**Just browsing?** Start with the [User Guide](USER_GUIDE.md)!
