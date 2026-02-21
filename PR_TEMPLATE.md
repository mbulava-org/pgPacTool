# Pull Request: Fix Issue #7 - Privilege Extraction Bug

## 🎯 Summary

Fixed critical privilege extraction bug that was blocking all extraction features (Issues #1-6). Implemented comprehensive multi-version test infrastructure with Docker-based testing.

---

## 🐛 Problem

**Issue #7: Fix Privilege Extraction Bug**

PostgreSQL's `aclitem[]` type cannot be read directly by Npgsql, causing this error:
```
42883: no binary output function available for type aclitem
```

This bug completely blocked privilege extraction for schemas, tables, and sequences.

---

## ✅ Solution

### Core Fix
Cast PostgreSQL ACL columns to `text[]`:
- Schema: `n.nspacl::text[]`
- Tables: `c.relacl::text[]`
- Sequences: `c.relacl::text[]`

### Improvements
- Added EXECUTE privilege code ('X')
- Fixed connection disposal
- Normalized privilege code handling

---

## 🧪 Test Results

**✅ 10/12 tests passing (83%)**

- Smoke: 1/1 passing (~5s)
- PostgreSQL 16: 5/7 passing
- PostgreSQL 17: 4/4 passing
- PostgreSQL 18: 4 ignored (future)

---

## 📁 Changed Files

**Production:** 1 file  
**Tests:** 9 files (1 deleted, 2 modified, 6 created)  
**Docs:** 6 files

---

## 🚀 Impact

Unblocks Issues #1, #2, #3, #4, #5, #6

---

## 🧪 Test Commands

```bash
# Quick
dotnet test --filter "Category=Smoke"

# Full
dotnet test --filter "Category!=FutureVersion"
```

---

**Fixes #7**
