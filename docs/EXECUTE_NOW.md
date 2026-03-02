# 🚀 EXECUTE NOW: Build Native Libraries

**QUICK ACTION REQUIRED**

---

## Step 1: Open This URL

👉 **https://github.com/mbulava-org/pgPacTool/actions/workflows/build-native-libraries.yml**

---

## Step 2: Click "Run workflow"

You'll see a gray button on the right side that says **"Run workflow"**

---

## Step 3: Fill in Parameters

A dropdown will appear with these fields:

```
┌─────────────────────────────────────────┐
│ Run workflow                             │
├─────────────────────────────────────────┤
│ Use workflow from                        │
│ Branch: [main ▼]                         │
│                                          │
│ PostgreSQL versions to build             │
│ [16,17]                                  │
│                                          │
│ Force rebuild even if files exist        │
│ [ ] checked                              │
│                                          │
│ [Run workflow]                           │
└─────────────────────────────────────────┘
```

**Enter:**
- **Branch:** `main` (select from dropdown)
- **PostgreSQL versions:** `16,17` (already default)
- **Force rebuild:** Leave unchecked ☐

---

## Step 4: Click "Run workflow"

Click the green **"Run workflow"** button at the bottom.

---

## Step 5: Monitor Progress

1. The page will refresh and show a new workflow run at the top
2. Click on it to see live progress
3. You'll see 7 jobs running:
   ```
   Build libpg_query (windows-latest, 16)  ⏳
   Build libpg_query (windows-latest, 17)  ⏳
   Build libpg_query (ubuntu-latest, 16)   ⏳
   Build libpg_query (ubuntu-latest, 17)   ⏳
   Build libpg_query (macos-latest, 16)    ⏳
   Build libpg_query (macos-latest, 17)    ⏳
   Collect libraries and create PR         ⏳ (waits for above)
   ```

---

## Step 6: Wait ~30-40 Minutes ☕

The workflow will:
1. Build libraries for all platforms (parallel)
2. Upload artifacts
3. Collect all artifacts
4. Create a Pull Request automatically

---

## Step 7: Review the Pull Request

After workflow completes:

1. **Go to:** https://github.com/mbulava-org/pgPacTool/pulls
2. **Find PR:** "chore: Update native libpg_query libraries..."
3. **Review changes:** Should show 6 new/updated binary files
4. **Check CI:** All checks should be green ✅

---

## Step 8: Merge the PR

1. Click **"Merge pull request"**
2. Confirm the merge
3. Delete the automated branch

---

## Step 9: Pull Changes Locally

```bash
git checkout main
git pull origin main

# Verify files
ls src/libs/Npgquery/Npgquery/runtimes/*/native/
```

---

## ✅ Done!

You now have native libraries for:
- ✅ Windows (x64) - PostgreSQL 16 & 17
- ✅ Linux (x64) - PostgreSQL 16 & 17
- ✅ macOS (arm64) - PostgreSQL 16 & 17

---

## 🔗 Quick Links

**Trigger Workflow:**  
https://github.com/mbulava-org/pgPacTool/actions/workflows/build-native-libraries.yml

**View Actions:**  
https://github.com/mbulava-org/pgPacTool/actions

**Pull Requests:**  
https://github.com/mbulava-org/pgPacTool/pulls

---

## 💡 Pro Tip

**Bookmark this workflow URL:**  
https://github.com/mbulava-org/pgPacTool/actions/workflows/build-native-libraries.yml

You'll need it whenever you add a new PostgreSQL version!

---

**⏰ START NOW** - It takes 30-40 minutes!  
**☕ Grab coffee and come back in 40 minutes**

---

*Created: 2026-03-01*  
*Next: Wait for workflow to complete*
