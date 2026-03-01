# Documentation Quick Reference

## 📁 Where to Put Documentation

| Type | Location | Example |
|------|----------|---------|
| **Feature Docs** | `docs/features/feature-name/` | `docs/features/ast-based-compilation/` |
| **Architecture** | `docs/architecture/` | `docs/architecture/AST_BUILDER_PATTERNS.md` |
| **API Reference** | `docs/` | `docs/API_REFERENCE.md` |
| **Test Guides** | `tests/PROJECT/Integration/` | `tests/.../SampleDatabases/README.md` |

## 🔗 Quick Link Calculator

### From Feature Docs (`docs/features/feature-name/`)

```markdown
[Architecture Doc](../../architecture/FILE.md)
[Test Doc](../../../tests/PROJECT/PATH/FILE.md)
[Root Doc](../../../FILE.md)
[Other Feature](../other-feature/FILE.md)
```

### From Architecture (`docs/architecture/`)

```markdown
[Feature Doc](../features/feature-name/FILE.md)
[Test Doc](../../tests/PROJECT/PATH/FILE.md)
[Root Doc](../../FILE.md)
```

### From Tests (`tests/PROJECT/Integration/`)

```markdown
[Feature Doc](../../../docs/features/feature-name/FILE.md)
[Architecture Doc](../../../docs/architecture/FILE.md)
[Root Doc](../../../FILE.md)
```

## ✅ Pre-Commit Checklist

- [ ] Documents in correct folder
- [ ] All links use relative paths
- [ ] Links tested (click each one)
- [ ] README.md updated
- [ ] No absolute paths

## 📋 Required Files for Features

Every feature folder needs:

1. **README.md** - Index and overview
2. **STATUS.md** - Current progress (during development)
3. **COMPLETE.md** - Final summary (when done)

## 🎨 File Naming

- Important docs: `UPPER_CASE_WITH_UNDERSCORES.md`
- Supporting docs: `lowercase-with-hyphens.md`
- Index: `README.md`

## 🔍 Find All References

```bash
# Search for document references
git grep -n "FILENAME.md"

# Search in specific directory
git grep -n "FILENAME.md" docs/
```

---

**See**: [Full Documentation Guidelines](DOCUMENTATION_GUIDELINES.md) for complete details
