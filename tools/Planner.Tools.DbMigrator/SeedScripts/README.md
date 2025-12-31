# SeedScripts – Reference Data Seeding

This folder contains **append-only SQL scripts** used to seed and evolve
**reference data** in the Planner database.

These scripts are executed **only** by `Planner.DbMigrator` and are
**never executed at application runtime**.

---

## Purpose

Seed scripts are used for:

- Reference / lookup data (job types, vehicle types, status codes, etc.)
- Controlled corrections to previously seeded reference data
- One-time data adjustments that must be explicit and auditable

Seed scripts are **not** used for:

- Schema changes (use EF Core migrations)
- Transactional or user-generated data
- Environment-specific data

---

## Execution Model

- Scripts are embedded into the `Planner.DbMigrator` assembly at build time.
- Scripts are executed in **lexical filename order**.
- Each script is executed **at most once per database**.
- Execution is tracked in the `__SeedHistory` table using a checksum.

If a script has already been executed with the same checksum, it is skipped.

---

## Naming Convention (Mandatory)

```
NNN_<Category>__<Intent>[_<Qualifier>].sql
```

Examples:

```
000_CreateSeedHistory.sql
010_JobTypes__AddInitial.sql
050_JobTypes__FixSpelling.sql
110_JobTypes__AddOvernight.sql
```

Rules:

- `NNN` is zero-padded and determines execution order.
- Scripts are **append-only**; never rename or modify existing scripts.
- Use clear, intention-revealing names.
- Use `Add`, `Fix`, `Rename`, `Deactivate` verbs.
- Avoid generic names like `Update` or `Modify`.

---

## Script Authoring Rules

All scripts **must**:

- Be **idempotent**
- Be safe to re-run
- Avoid destructive operations

### Allowed
- `INSERT` with `IF NOT EXISTS`
- `UPDATE` with precise `WHERE` clauses
- Soft-deactivation (`IsActive = 0`)

### Forbidden
- `DROP`
- `TRUNCATE`
- `DELETE` (except in non-production scenarios with explicit approval)
- Schema changes

---

## Required Script Header

Every script must begin with a comment header:

```sql
-- Script: 110_JobTypes__AddOvernight.sql
-- Purpose: Add Overnight job type for after-hours routing
-- Safety: Idempotent, no deletes
```

This header is required for auditability and long-term maintainability.

---

## Making Changes

To introduce new reference data or corrections:

1. **Create a new script**
2. Give it the next appropriate sequence number
3. Commit it to source control
4. Deploy `Planner.DbMigrator`
5. Run `Planner.DbMigrator seed`

**Never modify or delete existing scripts.**

---

## Production Safety

- Seed scripts are executed explicitly via CI/CD.
- Application services do not run seed scripts.
- Production environments may enforce additional approval gates.

---

## Related Components

- `Planner.DbMigrator` – Executes seed scripts
- `__SeedHistory` table – Tracks script execution
- EF Core Migrations – Schema evolution only

---

## Summary

Seed scripts provide a **transparent, deterministic, and auditable**
mechanism for managing reference data.

Treat these scripts with the same discipline as EF Core migrations.
