# GitHub main protection and CI-recovery addendum

This addendum records the GitHub-settings side of the canonical multi-agent protocol in `docs/MAIN-WRITE-AUTHORIZATION.md`, `AGENTS.md`, `docs/AGENT-WORK-REGISTRATION.md` and `CI_POLICY.md`.

## Zero-direct-main target

After the owner-authorized governance policy is merged into `main`, normal AI agents/chat sessions must treat `main` as read-only for **all** task content:

- source;
- tests;
- scripts;
- workflows;
- docs/Markdown;
- claim/handoff/status files;
- chores and release notes.

Normal work uses Issue + dedicated branch + PR. There is no docs/claim exception.

The policy PR itself does not activate this rule until it is explicitly merged by an owner-authorized coordinator. Until activation, current `main` may still show historical direct-to-main coordination/source commits from agents following the older repository policy.

## No CI direct-main exception

Being the agent/chat session assigned to dispatch, monitor, diagnose, or repair `release-v25-cloud.yml` does **not** authorize implementation directly on `main`.

When V25 cloud CI is red, use this path after the new governance policy is active:

```text
exact failing run/SHA
  -> reserve non-overlapping repair lane
  -> recovery/<agent>/<scope> or agent/<agent>/<scope>
  -> deterministic regression/guard
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
4. If the newest run exposes a real source/test/preflight/build/package failure, reproduce or verify that failure against the newest `main` before patching. If still present, fix it on a recovery/agent branch, verify it, hand it to PR/integration review, and land only with explicit owner merge authorization.
5. Repeat until the newest relevant V25 run is green and no newer integration-relevant landing invalidates it.
6. Never create a no-op implementation commit merely to obtain a new SHA.

A release workflow may create its own release-preparation commit as part of an approved release transaction. That workflow-owned commit must not recursively dispatch an infinite chain of release runs by itself. Any independent integration-relevant landing that advances `main` during the run invalidates stale evidence for the newer tree.

## Main branch protection target

Repository policy should be backed by GitHub branch protection/rulesets so authenticated writers cannot accidentally bypass the integration protocol.

Required target behavior:

- protect `main` from force-push and deletion;
- block normal direct pushes;
- require PR-based changes for normal writers, including docs/Markdown/chore/claim changes;
- require appropriate stable status checks when finalized;
- keep administrator/owner bypass narrow and deliberate;
- do not treat bypass as normal agent authorization.

The repository files cannot configure GitHub account/repository rulesets by themselves. Until hard protection is enabled, repository policy is procedural rather than physically enforced.

## Work registration under hard protection

Use a GitHub Issue as the preferred immediately visible task reservation. A Markdown claim, when useful, lives on the task branch/PR and does not need to be merged into `main` before implementation begins.

This replaces the historical claim-only direct-to-main exception once the governance policy is activated.

## Final-state rule

`ALL MERGED TO MAIN` means the current combined tree has been freshly reviewed for task completion, commit/tree reachability, missing off-main work, accidental reversions, duplicate implementations and semantic/API/test conflicts after an explicitly authorized landing.

Branch existence, branch deletion, Issue state or PR UI state alone is not proof. Current tree reachability and exact-SHA evidence are authoritative.
