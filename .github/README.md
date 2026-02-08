# pgPacTool - Project Tracking Quick Reference

**Last Updated:** 2026-01-31

---

## ?? Document Structure

All project tracking documents are in the `.github/` folder:

```
.github/
??? ISSUES.md           # Detailed issue tracker (25 issues)
??? PROJECT_BOARD.md    # GitHub Project board structure
??? ROADMAP.md          # Development roadmap & milestones
??? README.md           # This file - quick reference
```

---

## ?? Quick Links

### For Developers

- **[Start Here: Issues List](ISSUES.md)** - All 25 issues with full details
- **[High Priority Issues](ISSUES.md#high-priority---mvp-issues)** - MVP work (#1-11)
- **[Current Blockers](ISSUES.md#issue-7-fix-privilege-extraction-bug)** - Issue #7 must be fixed first
- **[Testing Setup](ISSUES.md#issue-11-create-integration-tests-with-testcontainers)** - Issue #11 for test infrastructure

### For Project Managers

- **[Roadmap Overview](ROADMAP.md)** - Visual timeline and milestones
- **[Milestone Details](ROADMAP.md#milestone-roadmap)** - 7 milestones with goals
- **[Feature Matrix](ROADMAP.md#feature-matrix)** - Current vs target state
- **[Risk Assessment](ROADMAP.md#risk-assessment)** - Known risks and mitigation

### For Contributors

- **[Good First Issues](ISSUES.md#issue-1-implement-view-extraction-from-postgresql-database)** - Issue #1 is tagged as good first issue
- **[Project Board Setup](PROJECT_BOARD.md#quick-start-guide)** - How to set up tracking
- **[Issue Templates](PROJECT_BOARD.md#issue-templates)** - Templates for creating issues

---

## ?? Project Status at a Glance

### Current State
- **Phase:** Planning / Pre-development
- **Version:** 0.0.1 (current codebase)
- **Target:** v0.1.0 (MVP - Core Extraction)
- **Timeline:** 28-32 weeks to v1.0

### Issue Statistics
- **Total Issues:** 25
- **High Priority (MVP):** 11 issues (44%)
- **Medium Priority:** 7 issues (28%)
- **Low Priority:** 7 issues (28%)
- **Total Story Points:** 213

### Completion Status
- ? **Completed:** 0 issues (0%)
- ?? **In Progress:** 0 issues (0%)
- ?? **Not Started:** 25 issues (100%)
- ?? **Blocked:** 0 issues (0%)

---

## ?? Next Steps

### Immediate Actions (Week 1)

1. **Fix Blocker**
   - [ ] Start Issue #7: Fix Privilege Extraction Bug
   - [ ] This blocks all other extraction work

2. **Set Up Testing**
   - [ ] Start Issue #11: Integration Tests with Testcontainers
   - [ ] Needed to validate extraction work

3. **Plan Sprint 1**
   - [ ] Review Issues #1-6 (object extraction)
   - [ ] Assign team members
   - [ ] Set sprint goals

### First Sprint Goals (Weeks 2-3)

After fixing Issue #7, tackle these in parallel:
- Issue #1: View Extraction
- Issue #2: Function Extraction
- Issue #8: Table Model Enhancement

### First Milestone (Weeks 1-8)

Complete Milestone 1 (v0.1.0):
- All extraction issues (#1-8)
- Integration tests (#11)
- Fix blocker (#7)

---

## ?? Issue Priority Reference

### ?? Critical (Must Fix First)
- **#7** - Fix Privilege Extraction Bug (BLOCKER)

### ?? High Priority (MVP)
- **#1** - View Extraction (Good First Issue)
- **#2** - Function Extraction
- **#3** - Procedure Extraction
- **#4** - Trigger Extraction
- **#5** - Index Extraction
- **#6** - Constraint Extraction
- **#8** - Table Model Enhancement
- **#9** - Compiler Reference Validation
- **#10** - Circular Dependency Detection
- **#11** - Integration Tests Infrastructure

### ?? Medium Priority
- **#12** - Schema Comparison
- **#13** - Script Generation
- **#14** - Attribute Comparison
- **#15** - Pre/Post Deployment Scripts
- **#16** - SQLCMD Variables
- **#17** - Package References
- **#18** - System Database References

### ?? Low Priority
- **#19** - DacPackage Implementation
- **#20** - NuGet Packaging
- **#21** - MSBuild SDK
- **#22** - Templates
- **#23** - CLI Commands
- **#24** - Container Publishing
- **#25** - Documentation

---

## ?? Milestone Timeline

```
Week 1-8    : Milestone 1 (v0.1.0) - Core Extraction
Week 9-12   : Milestone 2 (v0.2.0) - Compilation
Week 13-16  : Milestone 3 (v0.3.0) - Comparison
Week 17-20  : Milestone 4 (v0.4.0) - Deployment
Week 21-24  : Milestone 5 (v0.5.0) - Packaging
Week 25-28  : Milestone 6 (v1.0.0) - MSBuild SDK
Week 29-32  : Milestone 7 (v1.0.0) - Production Ready
```

---

## ?? Dependency Chain

**Critical Path:**

```
#7 (Fix Privileges)
  ?
#1-6 (Extract Objects)
  ?
#8 (Table Model) + #11 (Tests)
  ?
#9 (Compiler)
  ?
#10 (Dependencies)
  ?
#12 (Comparison)
  ?
#13 (Script Gen)
  ?
#15-16 (Deploy Scripts)
  ?
#23 (CLI)
```

**Packaging Path:**

```
#12 (Comparison)
  ?
#17-18 (References)
  ?
#19-20 (Packaging)
  ?
#21-22 (SDK)
  ?
#24-25 (Production)
```

---

## ?? How to Use This Tracker

### For Daily Work

1. **Open ISSUES.md**
2. Find your assigned issue
3. Read acceptance criteria
4. Check dependencies
5. Mark progress with checkboxes

### For Sprint Planning

1. **Open ROADMAP.md**
2. Review current milestone
3. Check feature matrix
4. Select issues from ISSUES.md
5. Assign to team

### For Reporting

1. **Open ROADMAP.md**
2. Check milestone progress
3. Review success metrics
4. Update stakeholders

### When Creating Issues

1. **Open PROJECT_BOARD.md**
2. Find issue templates section
3. Copy template format
4. Fill in details
5. Add to ISSUES.md

---

## ?? Status Indicators

### Issue Status
- ?? Not Started
- ?? In Progress
- ?? Done
- ?? Blocked
- ?? Paused

### Priority
- ?? P0 - Critical
- ?? P1 - High
- ?? P2 - Medium
- ?? P3 - Low

### Completion
- ? Complete
- ? In Progress
- ? Not Started
- ?? Partial/Limited

---

## ?? Find What You Need

### I want to...

**...start contributing**
? Read [ISSUES.md](ISSUES.md) and look for `good first issue` label (Issue #1)

**...understand the project scope**
? Read [ROADMAP.md](ROADMAP.md) overview and feature matrix

**...know what to work on next**
? Check [dependency chain](#dependency-chain) and [next steps](#next-steps)

**...set up the project board**
? Follow [PROJECT_BOARD.md](PROJECT_BOARD.md) quick start guide

**...see the timeline**
? Check [milestone timeline](#milestone-timeline) or [ROADMAP.md](ROADMAP.md)

**...understand an issue**
? Find it in [ISSUES.md](ISSUES.md) and read full details

**...track progress**
? Update checkboxes in [ISSUES.md](ISSUES.md)

**...report a bug**
? Use bug template from [PROJECT_BOARD.md](PROJECT_BOARD.md#template-2-bug-report)

**...request a feature**
? Use feature template from [PROJECT_BOARD.md](PROJECT_BOARD.md#template-1-feature-request)

---

## ?? Support

### Questions?
- Check existing documentation first
- Look for similar issues in ISSUES.md
- Review PROJECT_BOARD.md for processes

### Found an Issue with Tracking Docs?
- Update the relevant markdown file
- Keep documentation in sync with reality
- Review docs monthly

---

## ?? Additional Resources

### In This Repo
- `README.md` - Project overview
- `CONTRIBUTING.md` - Contribution guidelines (to be created)
- `.github/workflows/` - CI/CD configuration (to be created)

### External
- [MSBuild.Sdk.SqlProj](https://github.com/rr-wfm/MSBuild.Sdk.SqlProj) - Reference implementation
- [PostgreSQL Docs](https://www.postgresql.org/docs/)
- [Npgsql Docs](https://www.npgsql.org/)

---

## ?? Document Updates

### How to Update These Docs

1. **When completing an issue:**
   - Update status in ISSUES.md
   - Update progress in ROADMAP.md
   - Update this README stats

2. **When adding an issue:**
   - Add detailed entry in ISSUES.md
   - Update issue statistics
   - Add to appropriate milestone in ROADMAP.md

3. **When milestones change:**
   - Update ROADMAP.md
   - Update timeline in this README
   - Notify team

### Update Frequency
- **ISSUES.md:** Daily (as issues progress)
- **ROADMAP.md:** Weekly (during sprint review)
- **PROJECT_BOARD.md:** Monthly (or as process changes)
- **This README:** Weekly (stats and status)

---

## ? Quick Checklist

### Before Starting Work
- [ ] Read the issue in ISSUES.md
- [ ] Check dependencies
- [ ] Review acceptance criteria
- [ ] Check for blockers

### During Work
- [ ] Update issue status (?? In Progress)
- [ ] Mark completed acceptance criteria
- [ ] Ask questions early

### After Completing Work
- [ ] Update issue status (?? Done)
- [ ] Update ROADMAP.md progress
- [ ] Update this README stats
- [ ] Celebrate! ??

---

**Document Version:** 1.0  
**Maintained By:** Development Team  
**Next Review:** After Sprint 1
