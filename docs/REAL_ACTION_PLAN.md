# REAL Action Plan: Diagnose and Fix Windows Build

## The Problem

The Windows builds are failing with:
```
unresolved external symbol pg_query_is_utility_stmt
unresolved external symbol pg_query_split  
unresolved external symbol pg_query_summary
```

Even though:
- ✓ We extract 69 object files from pg_query.lib
- ✓ pg_query_split.obj is in the list
- ✓ The object files are being linked

**This means these functions don't exist in the object files, or they're named differently.**

## Root Cause Hypothesis

These functions might:
1. **Not exist in PostgreSQL 16** (but exist in 17)
2. **Have different names** on Windows vs Linux
3. **Be conditional** and not compiled on Windows
4. **Require additional source files** that aren't being built

## How to Diagnose LOCALLY

### Step 1: Run the Diagnostic Script

1. Open **Developer Command Prompt for VS 2022**
2. Run:
   ```batch
   cd C:\Users\mbula\source\repos\mbulava-org\pgPacTool
   scripts\diagnose-windows-build.bat 16
   ```

This will show you:
- What's actually in pg_query.lib
- If those .obj files exist
- What symbols are in the .obj files
- If the source files exist
- What the Makefile says

### Step 2: Compare PG 16 vs 17

The PG 17 build had only 1 unresolved symbol (`pg_query_split`), but PG 16 has 3. This suggests these features were added in different PostgreSQL versions.

Run the diagnostic for both:
```batch
scripts\diagnose-windows-build.bat 16 > pg16-diag.txt
scripts\diagnose-windows-build.bat 17 > pg17-diag.txt
```

Then compare them.

## Likely Solution

Based on the pattern, I suspect **these functions don't exist in all PostgreSQL versions**. We should:

1. **Check which functions actually exist** in each version
2. **Make the DEF file version-specific**
3. **Only export functions that actually exist**

## Immediate Next Step

**RUN THE DIAGNOSTIC** so we can see exactly what's in the library and what's missing. Once you run it and share the output, I can give you the exact fix.

## Alternative: Check the Successful Linux Build

The Linux builds succeed. Let's see what they're doing differently:

```powershell
# Check what the Linux build actually produced
Get-Content "build-logs\2_Build libpg_query (ubuntu-latest, 16).txt" | Select-String -Pattern "ar: creating|libpg_query.a" -Context 5
```

This might show us that Linux isn't even trying to include these functions in version 16.

## Bottom Line

**Stop guessing. Run the diagnostic.** It will tell us exactly what's wrong, and then we can fix it once and for all.

I apologize for being overconfident before. Let's get real data and fix this properly.
