# GitHub main protection and CI-recovery addendum

This addendum records the GitHub-settings side of the canonical multi-agent protocol in `docs/MAIN-WRITE-AUTHORIZATION.md`, `AGENTS.md`, `docs/AGENT-RUNTIME-CONTRACT.md`, `docs/AGENT-WORK-REGISTRATION.md` and `CI_POLICY.md`.

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

Hard protection is the technical barrier. Repository Markdown remains the behavioral contract for agents and defines which session is authorized to merge. Green checks alone do not authorize unrelated work or a protection bypass.

## Zero-direct-main target

Normal AI agents/chat sessions must treat `main` as read-only for **direct writes of all task content**:

- source;
- tests;
- scripts;
- workflows;
- docs/Markdown;
- claim/handoff/status files;
- chores and release notes.

Normal work uses Issue + dedicated branch + PR. There is no docs/claim direct-main exception.

The active ruleset physically enforces the PR path for `main` in addition to repository policy. Do not use a bypass, direct ref update, force push, or temporary ruleset weakening as a normal agent workflow.

For a normal repository-owner task, the standing same-task merge authorization in `docs/MAIN-WRITE-AUTHORIZATION.md` applies after every current required gate is satisfied unless the owner explicitly opts out. That authorization is PR-only and never permits direct-main writes or unrelated/bulk merges.

## Branch CI sequencing and PR timing

For changes watched by shared CI, preferred sequencing is:

```text
latest main
  -> Issue / lane reservation
  -> agent/<agent-id>/<scope>
  -> implement + validate
  -> commit + push task branch
  -> shared branch CI on exact branch SHA
  -> CI SUCCESS when required by the current admission gate
  -> refresh main / reconcile if needed
  -> open/update canonical PR
  -> protected-main required checks / freshness gate
  -> same-task authorized merge when all gates are satisfied
  -> exact-main release CI when applicable
```

A PR is not a substitute for diagnosing a known red exact-head branch run. If branch CI is red on the current canonical carrier, fix the concrete failure on that same branch and obtain fresh evidence.

However, CI completion timestamp is **not** permanent carrier identity. If the one canonical PR already exists while branch CI is queued/running, completes later, or a later same-carrier remediation changes the head SHA, do not close/recreate the PR or create a replacement branch merely to reorder timestamps. Revalidate the current candidate and follow `docs/PR-CI-LIFECYCLE.md`.

After PR creation, GitHub may run the shared workflow again for the PR merge candidate because the protected-main ruleset requires `preflight` and `core`. This is current merge-candidate/freshness evidence, not a requirement to manufacture duplicate carriers or cosmetic CI runs. If `main` moves or the candidate changes, strict protection requires fresh applicable checks.

For ordinary docs/claim/handoff-only paths intentionally outside the policy/source/build watched set, heavy pre-PR validation may be omitted, but branch + PR remain mandatory and protected-main checks must not be bypassed.

Governance/policy Markdown explicitly classified by `.github/workflows/ci.yml` must run the required policy/source guards, but does not require a Core/V25 build unless another build-relevant path changed. Changed paths, not `docs:`/`md:`/`chore:` prefixes, decide this classification.

## No CI direct-main exception

Being the agent/chat session assigned to dispatch, monitor, diagnose, or repair `release-v25-cloud.yml` does **not** authorize implementation directly on `main`.

When V25 cloud CI is red, use this path:

```text
exact failing run/SHA
  -> reserve non-overlapping repair lane
  -> recovery/<agent>/<scope> or agent/<agent>/<scope>
  -> deterministic regression/guard
  -> exact-head branch CI evidence
  -> canonical PR / integration carrier
  -> protected checks + current-candidate verification
  -> same-task authorized landing under MAIN-WRITE-AUTHORIZATION
  -> fresh current-main V25 cloud CI
  -> repeat from newest relevant failure until green
```

Do not change a fixture/expectation merely to match an unexpected production result without proving the fixture is wrong. Do not re-use a green run from an older tree as evidence for newer `main`.

## Latest-main / latest-CI recovery loop

Treat V25 recovery as a monotonic loop that always converges on the newest `main`, not on a historical failed SHA.

1. After an authorized integration-relevant landing reaches `main`, refresh current `main` HEAD and require a fresh relevant `release-v25-cloud.yml` qualification for that state.
2. Read the newest V25 cloud run together with the newest `main` commit. The newest run is diagnostic evidence; it is final release evidence only when it qualifies the newest relevant source/release tree.
3. If a run is stale because `main` moved, keep stale-dispatch/concurrency guards intact. Do not weaken or bypass them.
4. If the newest run exposes a real source/test/preflight/build/package failure, reproduce or verify that failure against the newest `main` before patching. If still present, fix it on a recovery/agent branch, verify it, revalidate the exact current head, and land only through the protected PR path under the applicable same-task authorization.
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

`ALL MERGED TO MAIN` means the current combined tree has been freshly reviewed for task completion, commit/tree reachability, missing off-main work, accidental reversions, duplicate implementations and semantic/API/test conflicts after an authorized landing.

Branch existence, branch deletion, Issue state or PR UI state alone is not proof. Current tree reachability, active protection state and exact-SHA evidence are authoritative.
