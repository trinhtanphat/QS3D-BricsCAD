# PR CI lifecycle and timing

This document records the owner-approved correction for pull-request CI timing. It supplements `CI_POLICY.md`, `docs/AGENT-WORK-REGISTRATION.md`, and `docs/AGENT-DUPLICATE-PROMPT-RACE-POLICY.md`. Where older wording treats PR creation time as a permanent admission boundary, this correction controls: **timing alone must not invalidate a canonical PR or require a replacement carrier**.

## Safety properties that remain mandatory

- Normal agents do not write directly to `main`.
- `main` remains protected by the repository's required pull-request path.
- The current merge candidate must satisfy required `preflight` and `core` checks and strict freshness before merge.
- Merge uses the current expected head SHA; stale-head merges are rejected.
- One Lane-Key has one canonical owner, branch, and open PR carrier at a time.
- Red CI on the current carrier is diagnosed and fixed on that same carrier unless an explicit supersession is genuinely required.
- Force-push, protection bypass, fake/no-op CI trigger commits, and stale CI reuse remain forbidden.

## Automatic branch CI

Every push to `agent/**` and `integration/**` remains eligible for automatic shared CI. This is early isolated validation and should be allowed to finish when practical before PR handoff, but **its completion timestamp is not a permanent PR-admission identity**.

A PR may already exist while the automatic branch run is queued, running, or completes later. That ordering does not make the PR historical, poisoned, or non-canonical. Do not close/recreate a PR solely to make a branch-CI completion timestamp precede PR creation.

If automatic branch CI is red on the current branch head, fix the concrete failure on the same branch and push the remediation. The open PR remains the canonical review/merge carrier and receives fresh synchronized validation for the new candidate.

## Protected PR checks are the merge gate

The hard merge decision is based on the **current PR merge candidate**, not the historical order in which branch CI and PR creation occurred. Before an authorized merge require:

1. the intended canonical Lane-Key/carrier with no duplicate open carrier;
2. current branch/PR head identity and expected-head protection;
3. current `main` freshness under the protected ruleset;
4. required `preflight` SUCCESS;
5. required `core` SUCCESS;
6. no unresolved blocker that invalidates the candidate.

If `main` moves or the branch head changes, obtain the fresh checks GitHub requires for that new candidate. Keep the same canonical PR unless there is a real ownership/scope reason to supersede it.

## What is no longer a valid close/replacement reason

None of the following, by itself, makes a PR permanently invalid:

- branch CI completed after PR creation;
- branch CI was still queued/running when the PR opened;
- the PR was opened seconds before the branch run reached terminal success;
- a later same-carrier remediation changed the head SHA;
- current `main` moved and the same carrier needs reconciliation.

Those cases are lifecycle updates. Revalidate the current candidate; do not manufacture a replacement PR for timestamp ordering.

## Real reasons a PR can still be blocked or superseded

A PR may still be blocked when there is a real technical failure, stale protected merge candidate, invalid/missing Lane-Key metadata, duplicate active carrier, wrong canonical ownership, unresolved conflict, or another concrete protection/policy violation. A replacement carrier is reserved for genuine explicit supersession, not CI timing cosmetics.
