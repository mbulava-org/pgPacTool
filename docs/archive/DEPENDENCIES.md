# pgPacTool - Visual Dependency Diagram

**Project:** PostgreSQL Data-Tier Application Compiler  
**Last Updated:** 2026-01-31

---

## ?? Critical Path Overview

This diagram shows the dependency relationships between all 25 issues, highlighting the critical path and parallel work opportunities.

---

## ?? Main Dependency Flow (Mermaid)

```mermaid
graph TD
    %% Critical Blocker
    I7[#7 Fix Privilege<br/>Extraction Bug<br/>?? BLOCKER<br/>8 SP]
    
    %% Phase 1: Extraction
    I1[#1 View Extraction<br/>5 SP]
    I2[#2 Function Extraction<br/>8 SP]
    I3[#3 Procedure Extraction<br/>5 SP]
    I4[#4 Trigger Extraction<br/>5 SP]
    I5[#5 Index Extraction<br/>5 SP]
    I6[#6 Constraint Extraction<br/>8 SP]
    I8[#8 Table Model<br/>Enhancement<br/>5 SP]
    I11[#11 Integration Tests<br/>Infrastructure<br/>13 SP]
    
    %% Phase 2: Compilation
    I9[#9 Compiler Reference<br/>Validation<br/>8 SP]
    I10[#10 Circular Dependency<br/>Detection<br/>5 SP]
    
    %% Phase 3: Comparison
    I12[#12 Schema Comparer<br/>13 SP]
    I14[#14 Attribute Comparer<br/>5 SP]
    
    %% Phase 4: Deployment
    I13[#13 Script Generator<br/>13 SP]
    I15[#15 Pre/Post Scripts<br/>8 SP]
    I16[#16 SQLCMD Variables<br/>5 SP]
    I23[#23 CLI Commands<br/>21 SP]
    
    %% Phase 5: Packaging
    I17[#17 Package References<br/>13 SP]
    I18[#18 System DB Refs<br/>8 SP]
    I19[#19 DacPackage<br/>8 SP]
    I20[#20 NuGet Packaging<br/>5 SP]
    
    %% Phase 6: SDK
    I21[#21 MSBuild SDK<br/>21 SP]
    I22[#22 Templates<br/>8 SP]
    
    %% Phase 7: Production
    I24[#24 Container Publishing<br/>8 SP]
    I25[#25 Documentation<br/>21 SP]
    
    %% Dependencies
    I7 --> I1
    I7 --> I2
    I7 --> I3
    I7 --> I4
    I7 --> I5
    I7 --> I6
    
    I1 --> I8
    I2 --> I8
    I3 --> I8
    I4 --> I8
    I5 --> I8
    I6 --> I8
    
    I8 --> I9
    I11 --> I9
    
    I9 --> I10
    I10 --> I12
    
    I12 --> I14
    I12 --> I13
    I12 --> I17
    
    I13 --> I15
    I15 --> I16
    I16 --> I23
    
    I17 --> I18
    I18 --> I19
    I19 --> I20
    
    I20 --> I21
    I21 --> I22
    
    I22 --> I24
    I22 --> I25
    
    %% Styling
    classDef blocker fill:#ff6b6b,stroke:#c92a2a,stroke-width:4px,color:#fff
    classDef extraction fill:#4ecdc4,stroke:#0a9396,stroke-width:2px
    classDef compilation fill:#ffe66d,stroke:#f4a261,stroke-width:2px
    classDef comparison fill:#a8dadc,stroke:#457b9d,stroke-width:2px
    classDef deployment fill:#95e1d3,stroke:#38a3a5,stroke-width:2px
    classDef packaging fill:#f38181,stroke:#aa4465,stroke-width:2px
    classDef sdk fill:#c7b8ea,stroke:#845ec2,stroke-width:2px
    classDef production fill:#ffd93d,stroke:#f6bd60,stroke-width:2px
    
    class I7 blocker
    class I1,I2,I3,I4,I5,I6,I8,I11 extraction
    class I9,I10 compilation
    class I12,I14 comparison
    class I13,I15,I16,I23 deployment
    class I17,I18,I19,I20 packaging
    class I21,I22 sdk
    class I24,I25 production
```

---

## ?? Critical Path (Sequential)

The longest dependency chain that determines minimum project duration:

```mermaid
graph LR
    Start([Start]) --> I7[#7 Fix Privileges<br/>8 SP]
    I7 --> I2[#2 Functions<br/>8 SP]
    I2 --> I8[#8 Table Model<br/>5 SP]
    I8 --> I9[#9 Compiler<br/>8 SP]
    I9 --> I10[#10 Dependencies<br/>5 SP]
    I10 --> I12[#12 Comparison<br/>13 SP]
    I12 --> I13[#13 Script Gen<br/>13 SP]
    I13 --> I15[#15 Pre/Post<br/>8 SP]
    I15 --> I16[#16 Variables<br/>5 SP]
    I16 --> I23[#23 CLI<br/>21 SP]
    I23 --> End([v0.4.0])
    
    classDef critical fill:#ff6b6b,stroke:#c92a2a,stroke-width:3px,color:#fff
    class I7,I2,I8,I9,I10,I12,I13,I15,I16,I23 critical
```

**Critical Path Story Points:** 94 SP  
**Estimated Duration:** ~19 weeks (assuming 5 SP/week)

---

## ?? Phase-Based Dependency Tree

```mermaid
graph TB
    subgraph Phase0[Phase 0: Prerequisites]
        I7[#7 Fix Privileges ??]
    end
    
    subgraph Phase1[Phase 1: Extraction - 8 weeks]
        I1[#1 Views]
        I2[#2 Functions]
        I3[#3 Procedures]
        I4[#4 Triggers]
        I5[#5 Indexes]
        I6[#6 Constraints]
        I8[#8 Table Model]
        I11[#11 Tests]
    end
    
    subgraph Phase2[Phase 2: Compilation - 4 weeks]
        I9[#9 Compiler]
        I10[#10 Dependencies]
    end
    
    subgraph Phase3[Phase 3: Comparison - 4 weeks]
        I12[#12 Schema Compare]
        I14[#14 Attribute Compare]
    end
    
    subgraph Phase4[Phase 4: Deployment - 4 weeks]
        I13[#13 Script Gen]
        I15[#15 Pre/Post]
        I16[#16 Variables]
        I23[#23 CLI]
    end
    
    subgraph Phase5[Phase 5: Packaging - 4 weeks]
        I17[#17 References]
        I18[#18 System DBs]
        I19[#19 DacPackage]
        I20[#20 NuGet]
    end
    
    subgraph Phase6[Phase 6: SDK - 4 weeks]
        I21[#21 MSBuild SDK]
        I22[#22 Templates]
    end
    
    subgraph Phase7[Phase 7: Production - 4 weeks]
        I24[#24 Containers]
        I25[#25 Docs]
    end
    
    Phase0 --> Phase1
    Phase1 --> Phase2
    Phase2 --> Phase3
    Phase3 --> Phase4
    Phase3 --> Phase5
    Phase5 --> Phase6
    Phase6 --> Phase7
    
    classDef phase0 fill:#ff6b6b,stroke:#c92a2a
    classDef phase1 fill:#4ecdc4,stroke:#0a9396
    classDef phase2 fill:#ffe66d,stroke:#f4a261
    classDef phase3 fill:#a8dadc,stroke:#457b9d
    classDef phase4 fill:#95e1d3,stroke:#38a3a5
    classDef phase5 fill:#f38181,stroke:#aa4465
    classDef phase6 fill:#c7b8ea,stroke:#845ec2
    classDef phase7 fill:#ffd93d,stroke:#f6bd60
    
    class I7 phase0
    class I1,I2,I3,I4,I5,I6,I8,I11 phase1
    class I9,I10 phase2
    class I12,I14 phase3
    class I13,I15,I16,I23 phase4
    class I17,I18,I19,I20 phase5
    class I21,I22 phase6
    class I24,I25 phase7
```

---

## ? Parallel Work Opportunities

Issues that can be worked on simultaneously (no dependencies between them):

```mermaid
graph TB
    subgraph Parallel1[After #7: Can work in parallel]
        P1A[#1 Views]
        P1B[#2 Functions]
        P1C[#3 Procedures]
        P1D[#4 Triggers]
        P1E[#5 Indexes]
        P1F[#6 Constraints]
        P1G[#11 Tests]
    end
    
    subgraph Parallel2[After #12: Can work in parallel]
        P2A[#14 Attribute Compare]
        P2B[#13 Script Gen]
        P2C[#17 References]
    end
    
    subgraph Parallel3[After #22: Can work in parallel]
        P3A[#24 Containers]
        P3B[#25 Docs]
    end
    
    Start([Start]) --> I7[#7 Fix Privileges]
    I7 --> Parallel1
    Parallel1 --> I8[#8 Table Model]
    I8 --> I9[#9 Compiler]
    I9 --> I10[#10 Dependencies]
    I10 --> I12[#12 Schema Compare]
    I12 --> Parallel2
    Parallel2 --> I15[#15 Pre/Post]
    I15 --> I16[#16 Variables]
    I16 --> I23[#23 CLI]
    Parallel2 --> I18[#18 System DBs]
    I18 --> I19[#19 DacPackage]
    I19 --> I20[#20 NuGet]
    I20 --> I21[#21 MSBuild SDK]
    I21 --> I22[#22 Templates]
    I22 --> Parallel3
    Parallel3 --> End([v1.0])
    
    classDef parallel fill:#a8dadc,stroke:#457b9d,stroke-width:2px
    classDef sequential fill:#ffe66d,stroke:#f4a261,stroke-width:2px
    
    class P1A,P1B,P1C,P1D,P1E,P1F,P1G,P2A,P2B,P2C,P3A,P3B parallel
    class I7,I8,I9,I10,I12,I15,I16,I18,I19,I20,I21,I22,I23 sequential
```

**Key Parallel Groups:**
1. **Group 1 (7 issues):** After #7, all extraction issues can be done simultaneously
2. **Group 2 (3 issues):** After #12, comparison/deployment/packaging can split
3. **Group 3 (2 issues):** After #22, final polish can be done in parallel

---

## ?? Milestone Dependencies

```mermaid
graph LR
    M1[Milestone 1<br/>v0.1.0<br/>Core Extraction<br/>62 SP]
    M2[Milestone 2<br/>v0.2.0<br/>Compilation<br/>13 SP]
    M3[Milestone 3<br/>v0.3.0<br/>Comparison<br/>18 SP]
    M4[Milestone 4<br/>v0.4.0<br/>Deployment<br/>47 SP]
    M5[Milestone 5<br/>v0.5.0<br/>Packaging<br/>34 SP]
    M6[Milestone 6<br/>v1.0.0<br/>SDK<br/>29 SP]
    M7[Milestone 7<br/>v1.0.0<br/>Production<br/>29 SP]
    
    M1 --> M2
    M2 --> M3
    M3 --> M4
    M3 --> M5
    M5 --> M6
    M6 --> M7
    
    classDef milestone fill:#4ecdc4,stroke:#0a9396,stroke-width:3px
    class M1,M2,M3,M4,M5,M6,M7 milestone
```

---

## ?? Cumulative Story Points by Phase

```mermaid
gantt
    title Story Points Accumulation
    dateFormat YYYY-MM-DD
    section Phase 0
    Fix Privileges (8 SP)           :2026-02-01, 1w
    section Phase 1
    Extraction (62 SP)              :2026-02-08, 7w
    section Phase 2
    Compilation (13 SP)             :2026-03-29, 3w
    section Phase 3
    Comparison (18 SP)              :2026-04-19, 4w
    section Phase 4
    Deployment (47 SP)              :2026-05-17, 5w
    section Phase 5
    Packaging (34 SP)               :2026-06-21, 4w
    section Phase 6
    SDK (29 SP)                     :2026-07-19, 4w
    section Phase 7
    Production (29 SP)              :2026-08-16, 4w
```

**Cumulative Story Points:**
- After Phase 0: 8 SP (4%)
- After Phase 1: 70 SP (33%)
- After Phase 2: 83 SP (39%)
- After Phase 3: 101 SP (47%)
- After Phase 4: 148 SP (69%)
- After Phase 5: 182 SP (85%)
- After Phase 6: 211 SP (99%)
- After Phase 7: 240 SP (100%) ?

---

## ?? Detailed Issue Dependencies Table

| Issue | Depends On | Blocks | Can Work With (Parallel) |
|-------|------------|--------|--------------------------|
| **#7** | None | #1, #2, #3, #4, #5, #6 | #11 |
| **#1** | #7 | #8 | #2, #3, #4, #5, #6, #11 |
| **#2** | #7 | #8 | #1, #3, #4, #5, #6, #11 |
| **#3** | #7 | #8 | #1, #2, #4, #5, #6, #11 |
| **#4** | #7 | #8 | #1, #2, #3, #5, #6, #11 |
| **#5** | #7 | #8 | #1, #2, #3, #4, #6, #11 |
| **#6** | #7 | #8 | #1, #2, #3, #4, #5, #11 |
| **#8** | #1-6 | #9 | None |
| **#11** | #7 | #9 | #1-6 |
| **#9** | #8, #11 | #10 | None |
| **#10** | #9 | #12 | None |
| **#12** | #10 | #13, #14, #17 | None |
| **#13** | #12 | #15 | #14, #17 |
| **#14** | #12 | None | #13, #17 |
| **#15** | #13 | #16 | #17, #18 |
| **#16** | #15 | #23 | #17, #18 |
| **#17** | #12 | #18 | #13, #14, #15, #16 |
| **#18** | #17 | #19 | #15, #16 |
| **#19** | #18 | #20 | None |
| **#20** | #19 | #21 | None |
| **#21** | #20 | #22 | None |
| **#22** | #21 | #24, #25 | None |
| **#23** | #16 | None | None |
| **#24** | #22 | None | #25 |
| **#25** | #22 | None | #24 |

---

## ?? Blocker Analysis

### Current Blockers (Red)
- **Issue #7** - Blocks 6 extraction issues
  - Impact: Cannot start any extraction work
  - Priority: Fix immediately
  - Estimated: 2-3 days

### Near-term Risks (Yellow)
- **Issue #8** - Aggregates all extraction work
  - Blocks compilation phase
  - Requires all extraction issues complete
  
- **Issue #12** - Gateway to multiple paths
  - Blocks deployment, packaging, and comparison tracks
  - Consider starting early if possible

### Optimization Opportunities (Green)
- **Group 1:** Issues #1-6, #11 (7 issues) can be distributed across 7 developers
- **Group 2:** Issues #13, #14, #17 can be worked simultaneously after #12
- **Group 3:** Issues #24, #25 can be worked simultaneously at the end

---

## ?? Resource Allocation Recommendations

### Team of 3 Developers

**Week 1:**
- Dev 1: Issue #7 (blocker)
- Dev 2: Issue #11 (test infrastructure)
- Dev 3: Planning and setup

**Weeks 2-8:**
- Dev 1: Issues #1, #4 (views, triggers)
- Dev 2: Issues #2, #5 (functions, indexes)
- Dev 3: Issues #3, #6, #8 (procedures, constraints, model)

**Weeks 9-12:**
- Dev 1: Issue #9 (compiler)
- Dev 2: Issue #10 (dependencies)
- Dev 3: Support and testing

**Weeks 13+:**
- Follow critical path with parallel work as available

### Team of 5+ Developers

**Week 1:**
- Dev 1: Issue #7 (blocker)
- Dev 2: Issue #11 (test infrastructure)
- Devs 3-5: Setup, planning, documentation

**Weeks 2-4:**
- Dev 1: Issue #1 (views)
- Dev 2: Issue #2 (functions)
- Dev 3: Issue #3 (procedures)
- Dev 4: Issue #4 (triggers)
- Dev 5: Issue #5 (indexes)

**Weeks 5-6:**
- Dev 1: Issue #6 (constraints)
- Dev 2: Issue #8 (table model)
- Devs 3-5: Testing and refinement

**Weeks 7+:**
- Split on critical path vs. packaging path

---

## ?? Color Legend

### By Priority
- ?? **Red** - Critical/Blocker (P0)
- ?? **Orange** - High Priority/MVP (P1)
- ?? **Yellow** - Medium Priority (P2)
- ?? **Green** - Low Priority (P3)

### By Phase
- ?? **Red** - Phase 0: Prerequisites (Blocker)
- ?? **Cyan** - Phase 1: Extraction
- ?? **Yellow** - Phase 2: Compilation
- ?? **Light Blue** - Phase 3: Comparison
- ?? **Green** - Phase 4: Deployment
- ?? **Pink** - Phase 5: Packaging
- ?? **Purple** - Phase 6: SDK
- ?? **Gold** - Phase 7: Production

### By Status
- ?? **Red** - Not Started
- ?? **Yellow** - In Progress
- ?? **Green** - Complete
- ?? **Gray** - Blocked

---

## ?? Using These Diagrams

### For Planning
1. **Identify Critical Path:** Focus on red issues in critical path diagram
2. **Find Parallel Work:** Use parallel opportunities diagram to assign work
3. **Check Dependencies:** Ensure upstream work is complete before starting

### For Daily Standup
1. **Show Current Work:** Point to issues in progress on main diagram
2. **Identify Blockers:** Highlight blocked issues
3. **Plan Next Work:** Show next available issues

### For Sprint Planning
1. **Select from Phase:** Choose issues within current phase
2. **Check Dependencies:** Verify all dependencies are met
3. **Assign Parallel Work:** Distribute independent issues across team

### For Reporting
1. **Show Progress:** Use cumulative story points diagram
2. **Highlight Milestones:** Point to completed milestones
3. **Forecast Completion:** Use phase timeline to estimate

---

## ?? Keeping Diagrams Updated

### When an Issue Completes
- Update status in all diagrams
- Check what issues are now unblocked
- Highlight new available work

### When Dependencies Change
- Update dependency arrows
- Recalculate critical path
- Notify team of changes

### When Adding New Issues
- Add to appropriate phase
- Connect dependencies
- Update story point totals

---

## ?? Exporting Diagrams

### To Generate Visual Images

**Using Mermaid CLI:**
```bash
# Install mermaid-cli
npm install -g @mermaid-js/mermaid-cli

# Generate PNG
mmdc -i DEPENDENCIES.md -o dependency-diagram.png

# Generate SVG
mmdc -i DEPENDENCIES.md -o dependency-diagram.svg
```

**Using Online Tools:**
- [Mermaid Live Editor](https://mermaid.live/)
- [GitHub Markdown](https://github.com) - Renders mermaid automatically

**Using VS Code:**
- Install "Markdown Preview Mermaid Support" extension
- Preview this file to see diagrams

---

## ?? Related Documents

- **[ISSUES.md](ISSUES.md)** - Detailed issue descriptions
- **[ROADMAP.md](ROADMAP.md)** - Timeline and milestones
- **[PROJECT_BOARD.md](PROJECT_BOARD.md)** - Board structure
- **[README.md](README.md)** - Quick reference

---

**Document Version:** 1.0  
**Last Updated:** 2026-01-31  
**Diagram Format:** Mermaid 10.x  
**Next Review:** After completing Milestone 1
