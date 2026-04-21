# Documentation Index

See [Multi-Version Support](features/multi-version-support/README.md) for multi-version PostgreSQL support documentation.

## Supported Versions
- PostgreSQL 16 (default)
- PostgreSQL 17

Older versions (14, 15) may be added if needed.

## Architecture Guidance

- Prefer small focused services over large multi-responsibility classes.
- Use partial classes only for organization, not as the primary way to manage business logic complexity.
- Target class sizes under 300 lines when practical, with 500 lines as a hard warning threshold.
- Target methods under 30 lines when practical, with 60 lines as a hard warning threshold.
- Keep orchestration thin and move ownership policy, publish context handling, and CLI formatting into dedicated collaborators when behavior grows.