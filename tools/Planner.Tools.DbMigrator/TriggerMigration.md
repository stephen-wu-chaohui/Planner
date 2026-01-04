# Database Migration Trigger
This file exists to provide a path-based trigger for the GitHub Action `db-migrator-dev.yml`.

## Last Migration Intent
- **Migration Name:** AddUserRole
- **Date Created:** 2026-01-02
- **Reason:** Ensuring the `Role` column is added to the `Users` table in the Dev environment.

## Instructions
To manually force a re-run of the migration tool via a git push, update the timestamp below:
- **Last Triggered:** 2026-01-04 07:36:00
- 