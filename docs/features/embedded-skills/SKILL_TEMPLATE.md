# Embedded Skill Template

Use this template for every new repository skill under `.github/skills/<skill-name>/SKILL.md`.

## Recommended Folder Shape

```text
.github/
  skills/
    <skill-name>/
      README.md
      SKILL.md
```

## Skill Naming Rules

- Use lowercase kebab-case
- Keep the name domain-specific and durable
- Prefer names that describe responsibility, not an implementation detail
- Examples:
  - `compare-and-deploy-expert`
  - `postgresql-catalog-extraction-expert`
  - `managed-postgresql-platforms-expert`

## Standard Sections

### 1. Purpose
- One short paragraph explaining what the skill is for

### 2. When To Use This Skill
- Concrete triggers
- File/project types that should activate the skill
- Operational scenarios where the skill is mandatory

### 3. Repository Defaults
- Repo-specific assumptions
- Supported versions/platforms
- Product defaults and naming conventions

### 4. Core Operating Rules
- Durable rules agents must follow
- Prefer numbered rules
- Keep them implementation-aware but not overly verbose

### 5. Version-Gated or Platform-Gated Quick Rules
- Short bullets for syntax/platform differences
- Include cloud-provider constraints where relevant

### 6. Expected Implementation Behavior
Break down by activity:
- model changes
- compare changes
- extraction changes
- script/publish changes
- docs changes
- test changes

### 7. Required Documentation Updates
- Which docs must be updated when behavior changes

### 8. Required Test Mindset
- What kinds of tests must accompany changes

### 9. Do Not Do
- Anti-patterns and forbidden shortcuts

### 10. Skill References
- Links to source-of-truth docs, relevant code, and related skills

## README.md for Skill Folder

Each skill folder should include a short `README.md` with:
- purpose
- covered scenarios
- primary references
- neighboring skills

## Authoring Rules

- Keep the skill concise enough to be used repeatedly
- Put deep detail in docs, not all in the skill
- Prefer durable rules over temporary implementation notes
- Avoid duplicating large reference tables if a source doc already exists
- Reference tests and code surfaces explicitly when possible

## Required Companion Updates

When adding a skill, also consider updating:
- `.github/copilot-instructions.md`
- `docs/README.md`
- `README.md` if the skill is user-significant
- `docs/features/embedded-skills/EXECUTION_ROADMAP.md`
- related version-reference documents
