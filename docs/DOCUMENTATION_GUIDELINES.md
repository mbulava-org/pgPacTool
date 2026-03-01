# Documentation Guidelines for Branch-Specific Features

## 📁 Directory Structure

All branch-specific or feature-specific documentation should be organized under `docs/features/`:

```
docs/
├── features/
│   ├── feature-name/
│   │   ├── README.md           # Feature overview and index
│   │   ├── STATUS.md          # Current status
│   │   ├── IMPLEMENTATION.md  # Implementation details
│   │   └── COMPLETE.md        # Completion summary
│   └── another-feature/
│       └── ...
├── architecture/               # Cross-cutting architecture docs
│   ├── DESIGN_PATTERN.md
│   └── ...
└── DOCUMENTATION_GUIDELINES.md # This file
```

## 🎯 When to Create Feature Documentation

Create a feature documentation folder when:

1. **Working on a feature branch** that will be merged later
2. **Implementing significant new functionality** (not just bug fixes)
3. **Creating documentation that spans multiple sessions** or is substantial
4. **Documentation needs to be isolated** from main docs until merge

## 📝 Naming Conventions

### Folder Names
- Use lowercase with hyphens: `feature-name`
- Match branch name pattern when applicable: `feature/AST_BASED_COMPILATION` → `ast-based-compilation`
- Keep concise but descriptive

### File Names
- Use UPPER_CASE_WITH_UNDERSCORES.md for important docs
- Use lowercase-with-hyphens.md for supporting docs
- Standard names:
  - `README.md` - Feature overview and documentation index (required)
  - `STATUS.md` - Current implementation status
  - `COMPLETE.md` - Feature completion summary
  - `IMPLEMENTATION.md` - Technical implementation details

## 🔗 Link Management

### Relative Path Rules

All links should use relative paths. Calculate paths based on document location:

#### From Feature Docs (`docs/features/feature-name/`)

| Target | Relative Path |
|--------|---------------|
| Root | `../../../` |
| Other feature | `../other-feature/` |
| Architecture | `../../architecture/` |
| Tests | `../../../tests/PROJECT/` |

#### From Architecture Docs (`docs/architecture/`)

| Target | Relative Path |
|--------|---------------|
| Feature | `../features/feature-name/` |
| Root | `../../` |
| Tests | `../../tests/PROJECT/` |

#### From Test Docs (`tests/PROJECT/Integration/`)

| Target | Relative Path |
|--------|---------------|
| Feature | `../../../docs/features/feature-name/` |
| Architecture | `../../../docs/architecture/` |
| Root | `../../../` |

### Link Format

Always use descriptive link text:

```markdown
✅ Good:
[AST Builder Patterns](../../architecture/AST_BUILDER_PATTERNS.md)
[Integration Test Guide](../../../tests/.../QUICK_START.md)

❌ Bad:
[Click here](../../architecture/AST_BUILDER_PATTERNS.md)
[Link](../COMPLETE.md)
```

## 📋 Required Contents

### Every Feature Folder Must Have:

#### 1. README.md
- **Purpose**: Entry point and documentation index
- **Contents**:
  - Feature overview (2-3 paragraphs)
  - Link index to all other documents
  - Status dashboard/table
  - Quick links by topic
  - Related documentation links

#### 2. STATUS.md (During Development)
- **Purpose**: Track current progress
- **Contents**:
  - Implementation phase
  - Completed tasks
  - Pending tasks
  - Test results
  - Known issues

#### 3. COMPLETE.md (When Done)
- **Purpose**: Final achievement summary
- **Contents**:
  - What was accomplished
  - Test results (final)
  - Performance metrics
  - Files created/modified
  - Merge checklist
  - Future enhancements

## 🎨 Document Templates

### README.md Template

```markdown
# Feature Name

Brief description of what this feature does.

## Documentation Index

- [Overview](#overview)
- [Status](STATUS.md)
- [Implementation Details](IMPLEMENTATION.md)

## Overview

What problem does this solve?

## Quick Start

How to use this feature.

## Status

Current implementation status.

## Related Documentation

Links to related docs.
```

### STATUS.md Template

```markdown
# Feature Name - Status

**Last Updated**: Date  
**Branch**: branch-name  
**Status**: In Progress / Complete

## Current Phase

What phase are we in?

## Completed ✅

- Task 1
- Task 2

## In Progress 🔄

- Task 3
- Task 4

## Pending ⏸️

- Task 5
- Task 6

## Test Results

Test counts and status.
```

## ✅ Pre-Commit Checklist

Before committing documentation changes:

- [ ] All links tested (click each one)
- [ ] README.md updated with new docs
- [ ] Relative paths calculated correctly
- [ ] No absolute paths or hardcoded URLs
- [ ] Cross-references work both ways
- [ ] Status documents updated
- [ ] Code examples tested (if any)

## 🔄 Maintenance

### When Moving Documents

1. **Update all incoming links** - Search project for references
2. **Update outgoing links** - Recalculate relative paths
3. **Update index documents** - README files with link lists
4. **Test all links** - Click through every link
5. **Commit with clear message** - Explain the reorganization

### When Merging Branch

1. **Review feature docs** - Ensure they're complete
2. **Move to main docs** if applicable
3. **Update cross-references**
4. **Archive or keep** feature folder as historical reference
5. **Update main README** with link to feature docs

## 📦 Example: AST-Based Compilation

### Correct Structure
```
docs/features/ast-based-compilation/
├── README.md                          # Index
├── AST_COMPILATION_COMPLETE.md       # Final summary
├── AST_BASED_COMPILATION_STATUS.md   # Status
└── AST_BASED_COMPILATION.md          # Original plan
```

### Correct Links (from README.md)
```markdown
[Architecture Patterns](../../architecture/AST_BUILDER_PATTERNS.md)
[Test Guide](../../../tests/.../QUICK_START.md)
[Main README](../../../README.md)
```

## 🚫 Common Mistakes

### ❌ Don't Do This
- Absolute paths: `/docs/features/...`
- Root-relative paths: `docs/features/...` (unless in root)
- Hardcoded domains: `https://github.com/.../docs/...`
- Missing index documents
- Broken cross-references
- Dead links after moving files

### ✅ Do This Instead
- Relative paths: `../../architecture/...`
- Test all links before commit
- Update indexes when adding docs
- Fix all cross-references when moving
- Use search to find all references

## 🛠️ Tools and Scripts

### Find All References to a Document
```bash
# From repository root
git grep -n "FILENAME.md"
```

### Validate Links (Manual)
1. Open README.md in VS Code
2. Ctrl+Click each link
3. Verify document opens correctly

### Check for Broken Links
```bash
# Using markdown-link-check (if installed)
npx markdown-link-check docs/**/*.md
```

## 📚 Additional Resources

- [Markdown Guide](https://www.markdownguide.org/)
- [Conventional Commits](https://www.conventionalcommits.org/)
- [Git Best Practices](https://git-scm.com/book/en/v2)

---

**Last Updated**: Documentation reorganization  
**Version**: 1.0  
**Status**: Active guideline
