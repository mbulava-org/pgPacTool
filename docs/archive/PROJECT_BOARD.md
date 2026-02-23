# pgPacTool - GitHub Project Board Structure

**Project:** PostgreSQL Data-Tier Application Compiler  
**Target Framework:** .NET 10  
**Last Updated:** 2026-01-31

---

## Table of Contents

- [Board Configuration](#board-configuration)
- [Custom Fields](#custom-fields)
- [Labels Structure](#labels-structure)
- [Milestones](#milestones)
- [Board Views](#board-views)
- [Automation Workflows](#automation-workflows)
- [Issue Templates](#issue-templates)
- [Quick Start Guide](#quick-start-guide)

---

## Board Configuration

### Project Details

**Name:** `pgPacTool - PostgreSQL DAC Development`

**Description:**
> Development tracker for PostgreSQL Data-Tier Application Compiler (pgPacTool) - A .NET 10 tool providing SQL Server Data Tools (.sqlproj) functionality for PostgreSQL databases.

**Board Type:** Board (Kanban-style)

---

## Column Structure

### Status-Based Board (Recommended)

| Column | Purpose | Automation Trigger |
|--------|---------|-------------------|
| ?? **Backlog** | Issues not yet started, triaged but not scheduled | Auto-add new issues |
| ?? **Ready** | Issues ready to work on, dependencies resolved | Manual move |
| ?? **In Progress** | Currently being worked on | Auto when assigned |
| ?? **In Review** | PR open, awaiting code review | Auto when PR created |
| ? **Done** | Completed and merged | Auto when PR merged |
| ?? **Blocked** | Waiting on dependencies or decisions | Manual with reason |

### Alternative: Phase-Based Board

| Column | Description |
|--------|-------------|
| ?? **Planning** | Issue definition, acceptance criteria refinement |
| ?? **Phase 1: Extraction** | All extraction-related issues (#1-8, #11) |
| ?? **Phase 2: Compilation** | Compiler and validation (#9-10) |
| ?? **Phase 3: Comparison** | Schema comparison (#12-14) |
| ?? **Phase 4: Packaging** | DacPackage and distribution (#15-20) |
| ?? **Phase 5: Deployment** | CLI, publishing, containers (#23-24) |
| ? **Complete** | Finished work |

---

## Custom Fields

### 1. Priority Field (Single Select)

**Values:**
- ?? **P0 - Critical** - MVP blockers, must fix immediately
- ?? **P1 - High** - MVP features, high priority
- ?? **P2 - Medium** - Post-MVP, important but not blocking
- ?? **P3 - Low** - Nice to have, future enhancements

**Usage:** Set based on impact to MVP and user value

---

### 2. Phase Field (Single Select)

**Values:**
- Phase 0: Setup & Infrastructure
- Phase 1: Extraction
- Phase 2: Compilation
- Phase 3: Comparison
- Phase 4: Packaging
- Phase 5: Deployment
- Phase 6: Documentation

**Usage:** Indicates which development phase the issue belongs to

---

### 3. Complexity Field (Single Select)

**Values:**
- **XS** - < 1 day (simple changes, config updates)
- **S** - 1-2 days (small features, bug fixes)
- **M** - 3-5 days (medium features, refactoring)
- **L** - 1-2 weeks (large features, complex logic)
- **XL** - > 2 weeks (epic-level work, major features)

**Usage:** Helps with sprint planning and resource allocation

---

### 4. Story Points Field (Number)

**Values:** 1, 2, 3, 5, 8, 13, 21 (Fibonacci sequence)

**Guidelines:**
- **1 point** - Trivial, < 2 hours
- **2 points** - Simple, half day
- **3 points** - Small, 1 day
- **5 points** - Medium, 2 days
- **8 points** - Large, 3-4 days
- **13 points** - Very large, 1 week
- **21 points** - Epic, 2+ weeks

**Usage:** For velocity tracking and capacity planning

---

### 5. Target Version Field (Single Select)

**Values:**
- **v0.1.0 (MVP)** - Core extraction features
- **v0.2.0** - Compilation and validation
- **v0.3.0** - Schema comparison
- **v0.4.0** - Deployment and publishing
- **v0.5.0** - Packaging and references
- **v1.0.0** - Production ready
- **Backlog** - Not scheduled yet

**Usage:** Release planning and milestone tracking

---

### 6. Component Field (Multiple Select)

**Values:**
- Extraction
- Compilation
- Comparison
- Packaging
- CLI
- SDK
- Testing
- Documentation
- Infrastructure

**Usage:** Helps filter and organize by technical area

---

### 7. .NET Version Field (Text)

**Default Value:** "net10.0"

**Usage:** Track .NET version requirements (usually net10.0)

---

### 8. PostgreSQL Versions Field (Multiple Select)

**Values:**
- All Versions
- PostgreSQL 16
- PostgreSQL 15
- PostgreSQL 14
- PostgreSQL 13
- PostgreSQL 12
- PostgreSQL 11+

**Usage:** Track PostgreSQL version compatibility requirements

---

## Labels Structure

### Priority Labels

```yaml
priority: critical
  color: #b60205 (red)
  description: Critical issue blocking MVP or production

priority: high
  color: #d93f0b (orange)
  description: High priority, MVP feature

priority: medium
  color: #fbca04 (yellow)
  description: Medium priority, post-MVP

priority: low
  color: #0e8a16 (green)
  description: Low priority, nice to have
```

---

### Type Labels

```yaml
type: bug
  color: #d73a4a (red)
  description: Something isn't working

type: enhancement
  color: #a2eeef (light blue)
  description: New feature or request

type: documentation
  color: #0075ca (blue)
  description: Improvements or additions to documentation

type: technical-debt
  color: #f9d0c4 (light pink)
  description: Code quality, refactoring, tech debt

type: test
  color: #c5def5 (light blue)
  description: Testing improvements or additions

type: question
  color: #d876e3 (purple)
  description: Further information is requested
```

---

### Component Labels

```yaml
component: extraction
  color: #5319e7 (purple)
  description: Database extraction functionality

component: compilation
  color: #0052cc (blue)
  description: SQL compilation and validation

component: comparison
  color: #006b75 (teal)
  description: Schema comparison logic

component: packaging
  color: #1d76db (blue)
  description: DacPackage creation and management

component: cli
  color: #0e8a16 (green)
  description: Command-line interface

component: sdk
  color: #5319e7 (purple)
  description: MSBuild SDK integration

component: model
  color: #d4c5f9 (light purple)
  description: Data models and schemas

component: testing
  color: #c5def5 (light blue)
  description: Test infrastructure and test cases
```

---

### Status Labels

```yaml
status: blocked
  color: #d93f0b (orange)
  description: Blocked by dependencies or decisions

status: in-progress
  color: #fbca04 (yellow)
  description: Currently being worked on

status: needs-review
  color: #0075ca (blue)
  description: Awaiting code review

status: ready-to-test
  color: #c2e0c6 (light green)
  description: Ready for testing

status: stale
  color: #ffffff (white)
  description: Inactive for 30+ days
```

---

### Help & Community Labels

```yaml
good first issue
  color: #7057ff (purple)
  description: Good for newcomers

help wanted
  color: #008672 (teal)
  description: Extra attention is needed

wontfix
  color: #ffffff (white)
  description: This will not be worked on

duplicate
  color: #cfd3d7 (gray)
  description: This issue already exists

invalid
  color: #e4e669 (yellow)
  description: This doesn't seem right
```

---

### Area Labels

```yaml
area: database-objects
  color: #c5def5 (light blue)
  description: Tables, views, functions, procedures, etc.

area: privileges
  color: #bfdadc (light cyan)
  description: ACL and privilege extraction

area: dependencies
  color: #d4c5f9 (light purple)
  description: Dependency resolution and references

area: ast-parsing
  color: #e99695 (light red)
  description: AST parsing with Npgquery

area: script-generation
  color: #f9d0c4 (light pink)
  description: SQL script generation
```

---

## Milestones

### Milestone 1: MVP - Core Extraction (v0.1.0)

**Due Date:** 8 weeks from start  
**Description:** Complete extraction of all major database objects

**Goals:**
- ? Extract all object types (tables, views, functions, procedures, triggers, indexes, constraints)
- ? Fix privilege extraction
- ? Basic model completeness
- ? Integration test infrastructure
- ? Basic CLI (extract command)

**Issues:** #1, #2, #3, #4, #5, #6, #7, #8, #11

**Success Metrics:**
- All extraction tests pass
- Can extract Northwind database completely
- Test coverage > 70%

---

### Milestone 2: MVP - Compilation & Validation (v0.2.0)

**Due Date:** 12 weeks from start  
**Description:** Project compilation with validation

**Goals:**
- ? Reference validation
- ? Circular dependency detection
- ? Compiler errors and warnings
- ? Build integration

**Issues:** #9, #10

**Success Metrics:**
- Reference validation catches all missing dependencies
- Circular dependencies detected correctly
- Clear error messages with line numbers

---

### Milestone 3: Schema Comparison (v0.3.0)

**Due Date:** 16 weeks from start  
**Description:** Full schema comparison capability

**Goals:**
- ? Complete schema comparer
- ? Attribute-level comparison
- ? Generate diff reports

**Issues:** #12, #14

**Success Metrics:**
- Can compare any two databases
- All object types compared
- Accurate diff detection

---

### Milestone 4: Deployment & Publishing (v0.4.0)

**Due Date:** 20 weeks from start  
**Description:** Deployment script generation and publishing

**Goals:**
- ? Script generation from diffs
- ? Pre/post deployment scripts
- ? SQLCMD variables
- ? CLI publish command

**Issues:** #13, #15, #16, #23

**Success Metrics:**
- Generated scripts deploy successfully
- No data loss in safe scenarios
- Variables substituted correctly

---

### Milestone 5: Packaging & Distribution (v0.5.0)

**Due Date:** 24 weeks from start  
**Description:** DacPackage and NuGet support

**Goals:**
- ? DacPackage implementation
- ? Package/Project references
- ? NuGet packaging
- ? System database references

**Issues:** #17, #18, #19, #20

**Success Metrics:**
- Can create and load .pgpac files
- Package references work correctly
- NuGet packages can be published

---

### Milestone 6: MSBuild SDK (v1.0.0)

**Due Date:** 28 weeks from start  
**Description:** Full MSBuild SDK integration

**Goals:**
- ? MSBuild.Sdk.PgProj package
- ? Project templates
- ? Item templates
- ? Build integration

**Issues:** #21, #22

**Success Metrics:**
- `dotnet new pgproj` works
- Projects build with MSBuild
- Visual Studio integration works

---

### Milestone 7: Production Ready (v1.0.0)

**Due Date:** 32 weeks from start  
**Description:** Production-ready release

**Goals:**
- ? Container publishing
- ? Comprehensive documentation
- ? All tests passing
- ? Performance optimization

**Issues:** #24, #25

**Success Metrics:**
- All features complete
- Test coverage > 80%
- Documentation complete
- Performance benchmarks met

---

## Board Views

### View 1: By Priority (Default)

**Layout:** Board  
**Group By:** Priority  
**Sort By:** Story Points (descending)  
**Filter:** Status != Done

**Purpose:** See highest priority work first

---

### View 2: By Phase

**Layout:** Board  
**Group By:** Phase  
**Sort By:** Priority, Story Points  
**Filter:** Status != Done

**Purpose:** See work organized by development phase

---

### View 3: MVP Roadmap

**Layout:** Roadmap  
**Filter:** Target Version = "v0.1.0" OR "v0.2.0"  
**Horizontal:** Milestones  
**Vertical:** Component

**Purpose:** Visual timeline of MVP work

---

### View 4: Sprint Board

**Layout:** Board  
**Group By:** Status  
**Filter:** Assignee = @me OR Sprint = "Current"  
**Sort By:** Priority

**Purpose:** Personal work view for current sprint

---

### View 5: Backlog

**Layout:** Table  
**Columns:** Title, Priority, Phase, Story Points, Component, Target Version  
**Filter:** Status = "Backlog"  
**Sort By:** Priority (descending), Story Points (descending)

**Purpose:** Backlog grooming and sprint planning

---

### View 6: Blocked Items

**Layout:** Table  
**Filter:** Status = "Blocked" OR Label contains "status: blocked"  
**Sort By:** Created (oldest first)  
**Highlight:** Show blocking reason

**Purpose:** Track and resolve blockers

---

### View 7: Testing View

**Layout:** Board  
**Group By:** Component  
**Filter:** Label contains "type: test" OR Component = "Testing"  
**Sort By:** Priority

**Purpose:** Track testing work

---

### View 8: By Assignee

**Layout:** Board  
**Group By:** Assignee  
**Filter:** Status = "In Progress" OR "In Review"  
**Sort By:** Priority

**Purpose:** See who's working on what

---

## Automation Workflows

### Workflow 1: Auto-add to Project

```yaml
Name: Auto-add Issues and PRs
Trigger: Issue created OR PR created
Actions:
  - Add to project "pgPacTool Development"
  - Set Status: Backlog
  - Add label: "status: needs-triage" (for issues)
```

---

### Workflow 2: Move to In Progress

```yaml
Name: Mark as In Progress
Trigger: Issue assigned AND Status = "Ready"
Actions:
  - Set Status: In Progress
  - Add label: "status: in-progress"
  - Remove label: "status: ready"
```

---

### Workflow 3: Move to Review

```yaml
Name: Mark as In Review
Trigger: PR created and linked to issue
Actions:
  - Set Status: In Review
  - Add label: "status: needs-review"
  - Remove label: "status: in-progress"
```

---

### Workflow 4: Move to Done

```yaml
Name: Mark as Done
Trigger: PR merged OR Issue closed (as completed)
Actions:
  - Set Status: Done
  - Remove labels: "status: in-progress", "status: needs-review"
  - Close linked issues (if PR)
```

---

### Workflow 5: Mark as Blocked

```yaml
Name: Track Blocked Issues
Trigger: Label "status: blocked" added
Actions:
  - Set Status: Blocked
  - Add comment: "@assignee Please add blocking reason and link to blocking issue"
  - Notify: @team-leads
```

---

### Workflow 6: High Priority Alert

```yaml
Name: Alert on Critical Issues
Trigger: Label "priority: critical" added
Actions:
  - Add to "Critical Issues" view
  - Notify: @team-leads
  - Pin issue to top
```

---

### Workflow 7: Stale Issue Check

```yaml
Name: Mark Stale Issues
Trigger: Issue inactive for 30 days AND Status != "Blocked"
Actions:
  - Add label: "status: stale"
  - Add comment: "This issue has been inactive for 30 days. Please update or close."
```

---

## Issue Templates

### Template 1: Feature Request

**File:** `.github/ISSUE_TEMPLATE/feature_request.yml`

```yaml
name: Feature Request
description: Suggest a new feature for pgPacTool
title: "[FEATURE] "
labels: ["type: enhancement", "status: needs-triage"]
body:
  - type: markdown
    attributes:
      value: |
        ## Feature Request
        Thanks for suggesting a new feature!

  - type: dropdown
    id: priority
    attributes:
      label: Priority
      description: How important is this feature?
      options:
        - P1 - High (MVP)
        - P2 - Medium (Post-MVP)
        - P3 - Low (Nice to have)
    validations:
      required: true

  - type: dropdown
    id: component
    attributes:
      label: Component
      description: Which area does this affect?
      options:
        - Extraction
        - Compilation
        - Comparison
        - Packaging
        - CLI
        - SDK
        - Documentation
        - Other
      multiple: true
    validations:
      required: true

  - type: textarea
    id: description
    attributes:
      label: Description
      description: Clear description of the feature
      placeholder: What problem does this solve?
    validations:
      required: true

  - type: textarea
    id: acceptance-criteria
    attributes:
      label: Acceptance Criteria
      description: What needs to be true for this to be complete?
      placeholder: |
        - [ ] Criterion 1
        - [ ] Criterion 2
        - [ ] Criterion 3
    validations:
      required: true

  - type: textarea
    id: technical-notes
    attributes:
      label: Technical Implementation Notes
      description: Any technical details, SQL queries, code samples
      placeholder: Optional technical details

  - type: dropdown
    id: postgres-versions
    attributes:
      label: PostgreSQL Versions
      description: Which PostgreSQL versions should support this?
      options:
        - All versions
        - PostgreSQL 16
        - PostgreSQL 15
        - PostgreSQL 14
        - PostgreSQL 13
        - PostgreSQL 12
      multiple: true
```

---

### Template 2: Bug Report

**File:** `.github/ISSUE_TEMPLATE/bug_report.yml`

```yaml
name: Bug Report
description: Report a bug in pgPacTool
title: "[BUG] "
labels: ["type: bug", "status: needs-triage"]
body:
  - type: markdown
    attributes:
      value: |
        ## Bug Report
        Thanks for reporting a bug!

  - type: textarea
    id: description
    attributes:
      label: Bug Description
      description: Clear description of the bug
    validations:
      required: true

  - type: textarea
    id: steps
    attributes:
      label: Steps to Reproduce
      description: How can we reproduce this?
      placeholder: |
        1. Step one
        2. Step two
        3. See error
    validations:
      required: true

  - type: textarea
    id: expected
    attributes:
      label: Expected Behavior
      description: What should happen?
    validations:
      required: true

  - type: textarea
    id: actual
    attributes:
      label: Actual Behavior
      description: What actually happens?
    validations:
      required: true

  - type: input
    id: version
    attributes:
      label: pgPacTool Version
      placeholder: "e.g., 0.1.0"
    validations:
      required: true

  - type: input
    id: postgres-version
    attributes:
      label: PostgreSQL Version
      placeholder: "e.g., 16.1"
    validations:
      required: true

  - type: input
    id: dotnet-version
    attributes:
      label: .NET Version
      placeholder: "e.g., 10.0"
    validations:
      required: true

  - type: dropdown
    id: os
    attributes:
      label: Operating System
      options:
        - Windows
        - Linux
        - macOS
    validations:
      required: true

  - type: textarea
    id: logs
    attributes:
      label: Error Logs
      description: Any relevant error messages or stack traces
      render: shell
```

---

### Template 3: Test Case

**File:** `.github/ISSUE_TEMPLATE/test_case.yml`

```yaml
name: Test Case
description: Create a test case for pgPacTool
title: "[TEST] "
labels: ["type: test", "component: testing"]
body:
  - type: dropdown
    id: test-type
    attributes:
      label: Test Type
      options:
        - Unit Test
        - Integration Test
        - End-to-End Test
        - Performance Test
    validations:
      required: true

  - type: textarea
    id: scenario
    attributes:
      label: Test Scenario
      description: What are we testing?
    validations:
      required: true

  - type: textarea
    id: test-data
    attributes:
      label: Test Data Setup
      description: SQL or setup needed
      render: sql

  - type: textarea
    id: assertions
    attributes:
      label: Assertions
      description: What should be verified?
      placeholder: |
        - [ ] Assertion 1
        - [ ] Assertion 2
```

---

## Quick Start Guide

### For Team Members

#### Daily Workflow

1. **Morning Check:**
   - View "Sprint Board" to see your assigned work
   - Move "In Progress" issues forward
   - Update status if blocked

2. **Creating Issues:**
   - Use appropriate issue template
   - Fill all required fields
   - Link to related issues
   - Add to milestone if known

3. **Working on Issues:**
   - Assign yourself
   - Move to "In Progress"
   - Create branch: `feature/#123-short-description`
   - Update with progress notes

4. **Submitting PR:**
   - Link PR: "Closes #123" in description
   - Issue auto-moves to "In Review"
   - Request reviewers

5. **After Merge:**
   - Issue auto-closes
   - Moves to "Done"
   - Updates milestone progress

---

### For Project Managers

#### Sprint Planning

1. **Review Backlog View:**
   - Sort by Priority and Story Points
   - Select issues for sprint
   - Move to "Ready" column

2. **Check Dependencies:**
   - Review "Blocked Items" view
   - Resolve blockers before sprint start

3. **Track Progress:**
   - Use "By Phase" view for roadmap
   - Use "MVP Roadmap" for timeline
   - Monitor milestone completion

---

### Setting Up Locally

#### Create Project Board

1. Go to GitHub Projects
2. Click "New Project"
3. Select "Board" template
4. Name: "pgPacTool Development"

#### Configure Fields

1. Add custom fields from section above
2. Set default values

#### Create Views

1. Add each view configuration
2. Set "By Priority" as default

#### Import Labels

Use GitHub CLI:
```bash
gh label create "priority: critical" --color "b60205" --description "Critical issue"
gh label create "priority: high" --color "d93f0b" --description "High priority"
# ... repeat for all labels
```

Or use the labels.yml file (create script to import)

#### Create Milestones

1. Go to Issues ? Milestones
2. Create each milestone with dates
3. Link issues to milestones

#### Set Up Automation

1. Project ? Settings ? Workflows
2. Enable each automation workflow
3. Test with a dummy issue

---

## Project Metrics

### Velocity Tracking

- **Story Points Completed per Week**
- **Issues Closed per Sprint**
- **Average Cycle Time** (Ready ? Done)
- **Lead Time** (Created ? Done)

### Quality Metrics

- **Test Coverage %**
- **Bug/Feature Ratio**
- **Reopened Issues Count**
- **Time to Resolution** (bugs)

### Progress Metrics

- **% of MVP Complete**
- **% of Each Phase Complete**
- **Milestone Progress**
- **Burndown Chart**

---

## Notes

- **Board is flexible:** Adjust columns, fields, and views as needed
- **Automation helps:** But don't over-automate - some manual curation is good
- **Regular reviews:** Review board structure monthly for improvements
- **Keep it simple:** Don't create too many views or fields - use what's needed

---

**Document Version:** 1.0  
**Last Updated:** 2026-01-31  
**Next Review:** After first sprint
