# GitHub main protection and CI-recovery addendum

This addendum records the GitHub-settings side of the canonical multi-agent protocol in `AGENTS.md`, `docs/MAIN-WRITE-AUTHORIZATION.md`, `docs/AGENT-WORK-REGISTRATION.md` and `CI_POLICY.md`.

## Zero direct-main exception for normal agents

Being the agent/chat session assigned to implement, document, claim, hand off, dispatch, monitor, diagnose, or repair work does **not** authorize any direct write or merge to `main`.

This applies equally to:

- source/tests/scripts/workflows;
- docs and Markdown;
- claim/handoff/status files;
- README/policy changes;
- chores;
- CI-recovery patches.

Normal agents must use an Issue plus a dedicated branch/PR and stop before merge. Only an explicitly owner-authorized integration/merge coordinator may change `main`, and only for the named PR/batch/task.

## CI recovery remains branch-first

When V25 cloud CI is red, use this path:

```text
exact failing run/SHA
  -> verify failure against newest relevant main
  -> register non-overlapping repair lane
  -> recovery/<agent>/<scope> or agent/<agent>/<scope>
  -> deterministic regression/guard
  -> PR / integration/<batch-id>
  -> owner-authorized final landing to main
  -> fresh current-main V25 cloud CI
  -> repeat from newest relevant failure until green
```

Do not change a fixture/expectation merely to match an unexpected production result without proving the fixture is wrong. Do not re-use a green run from an older tree as evidence for newer `main`.

## Latest-main / latest-CI recovery loop

Treat V25 recovery as a monotonic loop that converges on the newest authorized `main`, not on a historical failed SHA.

1. After an authorized integration-relevant update reaches `main`, refresh current `main` HEAD and require fresh `release-v25-cloud.yml` qualification for that newest state.
2. Read the newest V25 cloud run together with the newest relevant `main` commit. The run is final release evidence only when it qualifies the newest relevant release tree.
3. If a run is stale because `main` moved, keep stale-dispatch/concurrency guards intact. Do not weaken or bypass them.
4. If the newest run exposes a real source/test/preflight/build/package failure, reproduce or verify it against the newest relevant `main`. If still present, register a repair lane, fix branch-first, validate, open/update PR, integrate only with explicit owner authorization, then qualify the new current state.
5. Repeat until the newest relevant V25 run is green and no newer integration-relevant landing has invalidated it.
6. Never create a no-op implementation commit merely to obtain a new SHA.

Compact form:

```text
latest main HEAD
  -> latest relevant V25 run
  -> SUCCESS on current release tree? -> done
  -> stale because main moved? -> qualify current HEAD
  -> real failure? -> diagnose on current HEAD
  -> issue -> agent/recovery branch -> verify -> PR/integration
  -> owner-authorized main landing
  -> fresh V25 run
  -> repeat until green
```

A release workflow may create its own workflow-owned `chore(release): prepare ...` commit as part of the release transaction if that behavior is explicitly defined by the release workflow. That bot-owned transaction is not standing permission for human/AI agents to push chores directly to `main`, and it must not recursively create an infinite dispatch chain.

## Main branch protection target

Repository policy should be backed by GitHub branch protection/rulesets. The intended target is:

- protect `main` from force-push and deletion;
- require PR-based changes for normal writers;
- block normal direct pushes, including docs/Markdown/chore/claim-only pushes;
- require appropriate stable status checks when available;
- keep administrator/owner bypass narrow and deliberate;
- do not treat bypass as permission for ordinary agents.

The repository files cannot configure GitHub account/repository rulesets by themselves. Until hard protection is enabled, agents must still follow the repository policy contract.

## Issue/claim publication under hard protection

Use a GitHub Issue as the immediately visible reservation whenever practical. If a Markdown claim is useful for repository history, create/update it on the same task branch/PR.

**Do not publish claim-only Markdown directly to `main`.** There is no coordination exception to the normal-agent read-only-main rule.

Implementation may begin once the lane is visibly registered (for example by the Issue) and the non-overlapping task branch has been created from the latest valid baseline; the Markdown claim itself does not need to be reachable from `main` first.

## Documentation-only changes and CI

Documentation/Markdown/chore work still uses branch/PR and owner-authorized merge rules.

The V25 automatic dispatcher is separately path-filtered. An authorized merge that changes only ordinary documentation paths outside the watched set must not trigger V25 cloud release CI.

Changed paths are authoritative. A commit labelled `docs:` or `chore:` that also changes watched source/tests/scripts/build/workflow paths is integration-relevant despite its prefix.

## Verification checklist for hard protection

Close the repository governance issue only after GitHub read-back proves:

- `main` is protected or ruleset-enforced;
- force-push and deletion are blocked;
- normal direct pushes are rejected;
- the intended PR requirement is active;
- owner/admin bypass is narrow and deliberate;
- docs-only PR merges outside watched paths do not trigger the V25 automatic release dispatcher.

## Final-state rule

`ALL MERGED TO MAIN` means an authorized integration reviewer has freshly verified the current combined tree for task completion, commit/tree reachability, missing off-main work, accidental reversions, duplicate implementations and semantic/API/test conflicts, and has recorded the exact current `main` SHA.
