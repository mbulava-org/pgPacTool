# PostgreSQL Upgrade Guide

**pgPacTool requires PostgreSQL 16 or higher**

This guide helps you upgrade from PostgreSQL 15 or earlier to PostgreSQL 16+.

---

## ?? Table of Contents

- [Why Upgrade?](#why-upgrade)
- [Before You Begin](#before-you-begin)
- [Upgrade Methods](#upgrade-methods)
  - [Method 1: pg_dump/restore (Recommended)](#method-1-pg_dumprestore-recommended)
  - [Method 2: pg_upgrade (In-place)](#method-2-pg_upgrade-in-place)
  - [Method 3: Logical Replication](#method-3-logical-replication-minimal-downtime)
- [Post-Upgrade Steps](#post-upgrade-steps)
- [Troubleshooting](#troubleshooting)
- [Rollback Plan](#rollback-plan)

---

## ?? Why Upgrade?

pgPacTool requires PostgreSQL 16+ for several reasons:

? **Modern Features** - Access to latest PostgreSQL capabilities  
? **Better Performance** - PostgreSQL 16 performance improvements  
? **Simplified Tooling** - No version-specific code branches  
? **Long Support** - PostgreSQL 16 LTS until November 2028  
? **Security** - Latest security patches and improvements  

---

## ?? Before You Begin

### Check Your Current Version

```sql
-- Connect to your PostgreSQL database
psql -U postgres

-- Check version
SELECT version();
-- Example output: PostgreSQL 15.3 on x86_64-pc-linux-gnu...
```

Or via command line:
```bash
psql --version
# Example output: psql (PostgreSQL) 15.3
```

### Prerequisites Checklist

- [ ] **Backup completed** (see below)
- [ ] **Sufficient disk space** (2x current database size minimum)
- [ ] **Maintenance window scheduled** (if production)
- [ ] **Tested on non-production first** (highly recommended)
- [ ] **Read PostgreSQL 16 release notes** for breaking changes
- [ ] **Verified application compatibility** with PostgreSQL 16

### Create a Full Backup

**CRITICAL: Always backup before upgrading!**

```bash
# Backup entire database cluster
pg_dumpall -U postgres > all_databases_backup.sql

# Backup specific database
pg_dump -U postgres -d mydb -F c -f mydb_backup.dump

# Backup with custom format (allows selective restore)
pg_dump -U postgres -d mydb --format=custom --file=mydb.dump

# Verify backup file exists and has content
ls -lh mydb_backup.dump
```

**Store backups safely:**
- Copy to a different server or storage
- Verify backups are readable
- Test restoration on a separate server if possible

---

## ?? Upgrade Methods

### Method 1: pg_dump/restore (Recommended)

**Best for:** Most situations, guaranteed compatibility  
**Downtime:** Highest (minutes to hours depending on size)  
**Risk:** Lowest (clean slate)  
**Difficulty:** Easy

#### Step 1: Backup Your Data

```bash
# Dump all databases
pg_dumpall -U postgres > all_databases.sql

# Or dump specific database
pg_dump -U postgres -d mydb > mydb.sql
```

#### Step 2: Install PostgreSQL 16

**Ubuntu/Debian:**
```bash
# Add PostgreSQL repository
sudo sh -c 'echo "deb http://apt.postgresql.org/pub/repos/apt $(lsb_release -cs)-pgdg main" > /etc/apt/sources.list.d/pgdg.list'
wget --quiet -O - https://www.postgresql.org/media/keys/ACCC4CF8.asc | sudo apt-key add -

# Update and install
sudo apt update
sudo apt install postgresql-16
```

**RedHat/CentOS:**
```bash
# Add repository
sudo dnf install -y https://download.postgresql.org/pub/repos/yum/reporpms/EL-8-x86_64/pgdg-redhat-repo-latest.noarch.rpm

# Install
sudo dnf install -y postgresql16-server

# Initialize
sudo /usr/pgsql-16/bin/postgresql-16-setup initdb
```

**Windows:**
- Download installer from: https://www.postgresql.org/download/windows/
- Run installer (select PostgreSQL 16)
- Follow installation wizard

**macOS (Homebrew):**
```bash
brew install postgresql@16
brew services start postgresql@16
```

**Docker:**
```bash
docker run --name postgres16 \
  -e POSTGRES_PASSWORD=mypassword \
  -p 5432:5432 \
  -d postgres:16
```

#### Step 3: Stop Old PostgreSQL (if running)

```bash
# Ubuntu/Debian
sudo systemctl stop postgresql@15-main

# RedHat/CentOS
sudo systemctl stop postgresql-15

# macOS
brew services stop postgresql@15

# Docker
docker stop old-postgres
```

#### Step 4: Restore Data to PostgreSQL 16

```bash
# Restore all databases
psql -U postgres -f all_databases.sql

# Or restore specific database
createdb -U postgres mydb
psql -U postgres -d mydb < mydb.sql

# For custom format dumps
pg_restore -U postgres -d mydb mydb.dump
```

#### Step 5: Verify Data

```sql
-- Connect to database
psql -U postgres -d mydb

-- Check tables exist
\dt

-- Check row counts
SELECT count(*) FROM your_table;

-- Run application smoke tests
```

#### Step 6: Update Connection Strings

Update your application configuration to point to PostgreSQL 16:

```bash
# Old
Host=localhost;Port=5433;Database=mydb  # PostgreSQL 15 port

# New
Host=localhost;Port=5432;Database=mydb  # PostgreSQL 16 port
```

---

### Method 2: pg_upgrade (In-place)

**Best for:** Large databases, minimal downtime requirements  
**Downtime:** Medium (minutes)  
**Risk:** Medium  
**Difficulty:** Moderate

#### Step 1: Stop Applications

Ensure no applications are connected to the database.

#### Step 2: Stop PostgreSQL Services

```bash
sudo systemctl stop postgresql@15-main
```

#### Step 3: Run pg_upgrade

```bash
# As postgres user
sudo su - postgres

# Run upgrade check first
/usr/lib/postgresql/16/bin/pg_upgrade \
  --old-datadir=/var/lib/postgresql/15/main \
  --new-datadir=/var/lib/postgresql/16/main \
  --old-bindir=/usr/lib/postgresql/15/bin \
  --new-bindir=/usr/lib/postgresql/16/bin \
  --check

# If check passes, run actual upgrade
/usr/lib/postgresql/16/bin/pg_upgrade \
  --old-datadir=/var/lib/postgresql/15/main \
  --new-datadir=/var/lib/postgresql/16/main \
  --old-bindir=/usr/lib/postgresql/15/bin \
  --new-bindir=/usr/lib/postgresql/16/bin \
  --link  # Use hard links (faster, less disk space)
```

**Options:**
- `--check` - Dry run to check for issues
- `--link` - Use hard links (faster, less space, but can't rollback)
- `--clone` - Use reflinks on supported filesystems
- Without `--link` - Copy files (slower, more space, can rollback)

#### Step 4: Start PostgreSQL 16

```bash
sudo systemctl start postgresql@16-main
```

#### Step 5: Run Analyze

```bash
# As postgres user
/usr/lib/postgresql/16/bin/vacuumdb --all --analyze-in-stages
```

#### Step 6: Update Statistics

```bash
./analyze_new_cluster.sh  # Generated by pg_upgrade
```

---

### Method 3: Logical Replication (Minimal Downtime)

**Best for:** Production systems requiring minimal downtime  
**Downtime:** Low (seconds to minutes)  
**Risk:** Medium-High  
**Difficulty:** Advanced

#### Prerequisites

- PostgreSQL 15 (or 10+) as source
- PostgreSQL 16 as target
- Both running simultaneously

#### Step 1: Set up Logical Replication on Source

```sql
-- On PostgreSQL 15 source
-- Edit postgresql.conf
wal_level = logical
max_replication_slots = 5
max_wal_senders = 5

-- Restart PostgreSQL 15
```

#### Step 2: Create Publication on Source

```sql
-- On PostgreSQL 15
CREATE PUBLICATION my_publication FOR ALL TABLES;
```

#### Step 3: Create Subscription on Target

```sql
-- On PostgreSQL 16
CREATE SUBSCRIPTION my_subscription
CONNECTION 'host=old_server port=5432 dbname=mydb user=postgres password=xxx'
PUBLICATION my_publication;
```

#### Step 4: Monitor Replication

```sql
-- On target (PostgreSQL 16)
SELECT * FROM pg_stat_subscription;

-- Wait until all data is synced
SELECT count(*) FROM your_tables;
```

#### Step 5: Cutover

```sql
-- 1. Stop applications
-- 2. Wait for replication to catch up
-- 3. Drop subscription
DROP SUBSCRIPTION my_subscription;

-- 4. Point applications to PostgreSQL 16
-- 5. Start applications
```

---

## ? Post-Upgrade Steps

### 1. Verify Everything Works

```sql
-- Check version
SELECT version();
-- Should show: PostgreSQL 16.x

-- Check all databases exist
\l

-- Check extensions
SELECT * FROM pg_extension;

-- Check users/roles
\du

-- Run application smoke tests
```

### 2. Update Statistics

```bash
# Analyze all databases
vacuumdb --all --analyze-only -U postgres
```

### 3. Remove Old PostgreSQL (Optional)

**IMPORTANT: Only after confirming everything works!**

```bash
# Ubuntu/Debian
sudo apt remove postgresql-15
sudo apt autoremove

# Keep data directory as backup for a while
# Don't delete until 100% confident
```

### 4. Update Monitoring & Backups

- Update backup scripts to use PostgreSQL 16 binaries
- Update monitoring to point to new instance
- Update documentation

### 5. Test pgPacTool

```bash
# Test connection
pgpac extract --connection "Host=localhost;Database=mydb;Username=postgres"

# Should succeed without version errors
```

---

## ?? Troubleshooting

### Error: "could not connect to database"

**Cause:** PostgreSQL 16 not started or wrong port

**Solution:**
```bash
# Check if running
sudo systemctl status postgresql@16-main

# Check port
sudo netstat -tlnp | grep postgres

# Check pg_hba.conf for authentication rules
sudo nano /etc/postgresql/16/main/pg_hba.conf
```

### Error: "role does not exist"

**Cause:** User roles not restored

**Solution:**
```bash
# Restore global objects (roles, tablespaces)
psql -U postgres -f roles_backup.sql
```

### Error: "extension not found"

**Cause:** Extensions need to be created in new database

**Solution:**
```sql
-- Install missing extension
CREATE EXTENSION IF NOT EXISTS postgis;
CREATE EXTENSION IF NOT EXISTS pg_trgm;
```

### Performance Issues After Upgrade

**Cause:** Statistics not updated

**Solution:**
```bash
# Run ANALYZE
vacuumdb --all --analyze-in-stages -U postgres

# Or per database
psql -d mydb -c "ANALYZE;"
```

### Out of Disk Space During Upgrade

**Cause:** Insufficient space for pg_upgrade

**Solution:**
- Use `--link` option (if not already)
- Free up space before upgrading
- Use logical replication method instead

---

## ?? Rollback Plan

### If Using pg_dump/restore:

**Easy rollback:**
1. Stop PostgreSQL 16
2. Start PostgreSQL 15
3. Point applications back to PostgreSQL 15

### If Using pg_upgrade with --link:

**Cannot rollback!** (files are hard-linked)

**Options:**
- Restore from backup
- Keep PostgreSQL 15 installation until confident

### If Using pg_upgrade without --link:

**Can rollback:**
1. Stop PostgreSQL 16
2. Restore old data directory
3. Start PostgreSQL 15

---

## ?? Additional Resources

### Official Documentation
- [PostgreSQL 16 Release Notes](https://www.postgresql.org/docs/16/release-16.html)
- [pg_upgrade Documentation](https://www.postgresql.org/docs/16/pgupgrade.html)
- [pg_dump Documentation](https://www.postgresql.org/docs/16/app-pgdump.html)

### Community Resources
- [PostgreSQL Mailing Lists](https://www.postgresql.org/list/)
- [PostgreSQL Slack](https://postgres-slack.herokuapp.com/)
- [Stack Overflow PostgreSQL Tag](https://stackoverflow.com/questions/tagged/postgresql)

### pgPacTool Documentation
- [Project Scope](.github/SCOPE.md)
- [Main README](README.md)
- [Issue Tracker](.github/ISSUES.md)

---

## ?? Need Help?

If you encounter issues during your upgrade:

1. **Check PostgreSQL Logs:**
   ```bash
   sudo tail -f /var/log/postgresql/postgresql-16-main.log
   ```

2. **Ask the Community:**
   - PostgreSQL Slack: https://postgres-slack.herokuapp.com/
   - Stack Overflow: https://stackoverflow.com/questions/tagged/postgresql

3. **Report pgPacTool Issues:**
   - GitHub Issues: https://github.com/mbulava-org/pgPacTool/issues

---

## ? Upgrade Checklist

Use this checklist during your upgrade:

- [ ] Read this entire guide
- [ ] Check current PostgreSQL version
- [ ] Create full backup
- [ ] Verify backup is valid
- [ ] Test upgrade on non-production first
- [ ] Schedule maintenance window (if production)
- [ ] Install PostgreSQL 16
- [ ] Perform upgrade (method of choice)
- [ ] Verify data integrity
- [ ] Update connection strings
- [ ] Run ANALYZE
- [ ] Test applications
- [ ] Test pgPacTool
- [ ] Update monitoring
- [ ] Update backups
- [ ] Document upgrade process
- [ ] Keep old version for [X] days as backup

---

**Good luck with your upgrade!** ??

**Questions?** Open an issue: https://github.com/mbulava-org/pgPacTool/issues

---

**Document Version:** 1.0  
**Last Updated:** 2026-01-31  
**For:** pgPacTool PostgreSQL 16+ requirement
