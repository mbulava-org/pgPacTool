# Quick Start: Test Windows Build Locally

## The Easiest Way

1. **Press Windows Key** and type: `developer command prompt`

2. **Click on**: `Developer Command Prompt for VS 2022`
   (or `x64 Native Tools Command Prompt for VS 2022`)

3. **Run these commands**:
   ```batch
   cd C:\Users\mbula\source\repos\mbulava-org\pgPacTool
   scripts\test-windows-build.bat 17
   ```

4. **Watch for the success message**:
   ```
   SUCCESS! The build logic works correctly.
   ```

## What the Test Does

The test will:
1. ✓ Clone/update libpg_query repository (if needed)
2. ✓ Build the static library with nmake
3. ✓ Extract object files from pg_query.lib
4. ✓ Link them into pg_query.dll
5. ✓ Verify the DLL has exports
6. ✓ Show you the result

## Expected Output

You should see something like:
```
Testing Windows DLL Build for PostgreSQL 17
Building static library with nmake...
[... compilation output ...]
Library contains 65 object files
Extracting object files...
Extracted 65 object files
Linking DLL...
SUCCESS! Built pg_query.dll
[File size: ~1.5 MB]

Checking exports...
pg_query_parse
pg_query_normalize
pg_query_scan
[... more exports ...]

SUCCESS! The build logic works correctly.
You can safely commit and push your changes.
```

## Troubleshooting

### "nmake is not recognized"
- You're not in a Developer Command Prompt
- Open it from the Start menu (see step 1 above)

### "Visual Studio not found"
- Install Visual Studio 2022 with "Desktop development with C++" workload
- Or use Visual Studio 2019 (change prompt name in step 2)

### Build takes a long time
- First build downloads PostgreSQL source (~20 MB)
- Subsequent builds are faster

## Can't Find Developer Command Prompt?

Try these locations:
- Start Menu → Visual Studio 2022 → Developer Command Prompt
- Start Menu → Visual Studio 2022 → x64 Native Tools Command Prompt
- `C:\Program Files\Microsoft Visual Studio\2022\Community\Common7\Tools\LaunchDevCmd.bat`
- `C:\Program Files\Microsoft Visual Studio\2022\Professional\Common7\Tools\LaunchDevCmd.bat`

## Alternative: Skip Local Test

If you're confident in the logic (I am!), you can skip the local test and just:
```bash
git add .
git commit -m "fix: Windows DLL build - extract from library archive"
git push
```

The GitHub Actions runner will test it for you, and we have good diagnostics now.
