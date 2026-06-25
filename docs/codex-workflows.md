## `/shipit` workflow

When the user says `/shipit`, treat it as a request to complete the full GitHub delivery workflow for the current approved change.

The workflow means:

1. Inspect the current repository state.

   * Check current branch.
   * Check `git status`.
   * Review the diff.
   * Identify changed files.
   * Confirm there are no unrelated or accidental changes.

2. Create or update a GitHub issue.

   * If the user has already given an issue number, use it.
   * Otherwise create a new GitHub issue describing the change.
   * The issue should include:

     * problem / goal
     * implementation scope
     * acceptance criteria
     * test plan

3. Create a feature branch.

   * Branch from the correct base branch, usually `main`.
   * Use a clear branch name, for example:

     * `feature/<issue-number>-short-description`
     * `fix/<issue-number>-short-description`
   * Do not work directly on `main`.

4. Implement or finalize the change.

   * Keep the scope limited to the issue.
   * Do not perform unrelated refactoring.
   * Follow the architecture rules in `AGENTS.md`.
   * Preserve existing behavior unless the issue explicitly changes it.

5. Run validation.

   * Run `dotnet build`.
   * Run `dotnet test`.
   * If frontend code changed, run the relevant frontend build/test command.
   * If some tests cannot be run locally, explain exactly why.

6. Commit the change.

   * Stage only relevant files.
   * Use a clear commit message.
   * Prefer a message linked to the issue, for example:

     * `feat: add optimization run Cosmos document store`
     * `fix: handle completed run SignalR updates`

7. Push the branch to GitHub.

8. Open a draft pull request against `main`.

   * Link the GitHub issue.
   * Include:

     * summary of changes
     * architecture notes if relevant
     * test results
     * risks / follow-up work
   * Keep the PR as Draft unless the user explicitly says it is ready for review.

9. Report back with:

   * issue link
   * branch name
   * PR link
   * build/test results
   * any unresolved risks or manual verification still needed

Important rules:

* Never push directly to `main`.
* Never include secrets in commits.
* Never include unrelated formatting or refactoring.
* Do not delete the old RabbitMQ workflow unless the issue explicitly says so.
* Prefer small, reviewable PRs.
* If the working tree contains unrelated changes, stop and ask how to handle them before committing.
