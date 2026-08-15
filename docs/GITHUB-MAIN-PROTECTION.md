# GitHub main protection and CI-recovery addendum

This addendum records the GitHub-settings side of the canonical multi-agent protocol in `docs/MAIN-WRITE-AUTHORIZATION.md`, `AGENTS.md`, `docs/AGENT-WORK-REGISTRATION.md` and `CI_POLICY.md`.

## Active hard protection

As verified on 2026-08-15, repository ruleset **`protectedMain`** (ruleset ID **`20890901`**) is active and targets GitHub's default-branch selector `~DEFAULT_BRANCH`, which currently resolves to `main`.

Effective rules on `main` are:

- deletion protection;
- non-fast-forward / force-push protection;
- require a pull request before merging;
- require successful status checks `preflight` and `core`;
- strict required-status freshness enabled, so stale/out-of-date candidates must be refreshed/revalidated before merge;
- bypass list empty.

The GitHub branch API reports `main` as protected, and the effective-rules API reports ruleset `20890901` applying to `main`.

Hard protection is the technical barrier. Repository Markdown remains the behavioral contract for agents and defines who is allowed to perform a merge. Green checks never grant merge authorization by themselves.

## Zero-direct-main target

Normal AI agents/chat sessions must treat `main` as read-only for **all** task content:

- source;
- tests;
- scripts;
- workflows;
- docs/Markdown;
- claim/handoff/status files;
- chores and release notes.

Normal work uses Issue + dedicated branch + PR. There is no docs/claim exception.

The active ruleset now physically enforces the PR path for `main` in addition to the repository policy. Do not use a bypass, direct ref update, force push, or temporary ruleset weakening as a normal agent workflow.

## Mandatory branch CI before PR

For changes watched by shared CI, the task branch must pass its own automatic shared branch-push CI on the exact current branch SHA **before a new PR is opened**.

```text
latest main
  -> Issue / lane reservation
  -> agent/<agent-id>/<scope>
  -> implement + validate
  -> commit + push task branch
  -> shared branch CI on exact branch SHA
  -> CI SUCCESS
  -> refresh main / reconcile if needed
  -> open PR
  -> protected-main required checks / freshness gate
  -> owner-authorized merge only
  -> exact-main release CI when applicable
```

A PR is not the first CI attempt for watched/integration-relevant work. If branch CI is red, fix it on the branch and obtain a fresh green branch run before opening the PR.

After PR creation, GitHub may run the shared workflow again for the PR merge candidate because the protected-main ruleset requires `preflight` and `core`. This is merge-candidate/freshness evidence, not a policy requirement to run two arbitrary identical full CI passes. If `main` moves or the candidate changes, strict protection requires fresh applicable checks.

For ordinary docs/claim/handoff-only paths that are intentionally outside the shared branch-CI watch set, heavy pre-PR branch CI may be omitted, but a branch + PR is still mandatory and the protected-main checks must not be bypassed.

## No CI direct-main exception

Being the agent/chat session assigned to dispatch, monitor, diagnose, or repair `release-v25-cloud.yml` does **not** authorize implementation directly on `main`.

When V25 cloud CI is red, use this path:

```text
exact failing run/SHA
  -> reserve non-overlapping repair lane
  -> recovery/<agent>/<scope> or agent/<agent>/<scope>
  -> deterministic regression/guard
  -> branch CI SUCCESS before PR
  -> PR / integration/<batch-id>
  -> owner-authorized reviewed final landing to main
  -> fresh current-main V25 cloud CI
  -> repeat from newest relevant failure until green
```

Do not change a fixture/expectation merely to match an unexpected production result without proving the fixture is wrong. Do not re-use a green run from an older tree as evidence for newer `main`.

## Latest-main / latest-CI recovery loop

Treat V25 recovery as a monotonic loop that always converges on the newest `main`, not on a historical failed SHA.

1. After an authorized integration-relevant landing reaches `main`, refresh current `main` HEAD and require a fresh relevant `release-v25-cloud.yml` qualification for that state.
2. Read the newest V25 cloud run together with the newest `main` commit. The newest run is diagnostic evidence; it is final release evidence only when it qualifies the newest relevant source/release tree.
3. If a run is stale because `main` moved, keep stale-dispatch/concurrency guards intact. Do not weaken or bypass them.
4. If the newest run exposes a real source/test/preflight/build/package failure, reproduce or verify that failure against the newest `main` before patching. If still present, fix it on a recovery/agent branch, verify it, obtain green branch CI before PR, hand it to PR/integration review, and land only with explicit owner merge authorization.
5. Repeat until the newest relevant V25 run is green and no newer integration-relevant landing invalidates it.
6. Never create a no-op implementation commit merely to obtain a new SHA.

A release workflow may create its own release-preparation commit as part of an approved release transaction. That workflow-owned commit must not recursively dispatch an infinite chain of release runs by itself. Any independent integration-relevant landing that advances `main` during the run invalidates stale evidence for the newer tree.

## Protection contract verification

When a task depends on hard protection being active, verify GitHub's effective branch rules rather than trusting this file alone. The expected effective rule set for `main` includes:

```text
deletion
non_fast_forward
pull_request
required_status_checks: preflight, core
```

Treat any of these as a governance defect:

- the ruleset no longer targets the default branch;
- `main` reports no effective rules or no longer reports protected;
- PR requirement disappears;
- `preflight` or `core` is removed from required checks without an owner-approved replacement;
- force-push/deletion protection disappears;
- an unexpected bypass actor is added;
- workflow changes make required contexts impossible to produce for legitimate PRs.

Do not bypass protection to work around a governance defect. Fix the ruleset/workflow contract deliberately.

## Work registration under hard protection

Use a GitHub Issue as the preferred immediately visible task reservation. A Markdown claim, when useful, lives on the task branch/PR and does not need to be merged into `main` before implementation begins.

## Final-state rule

`ALL MERGED TO MAIN` means the current combined tree has been freshly reviewed for task completion, commit/tree reachability, missing off-main work, accidental reversions, duplicate implementations and semantic/API/test conflicts after an explicitly authorized landing.

Branch existence, branch deletion, Issue state or PR UI state alone is not proof. Current tree reachability, active protection state and exact-SHA evidence are authoritative.
