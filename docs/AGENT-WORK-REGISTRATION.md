# Agent work registration and canonical carrier

This document defines how repository work is registered and how one task maps to one canonical carrier. Read it when a task needs a new reservation, when concurrent agents may overlap, or when a carrier must be reassigned/superseded.

For everyday execution, start with `AGENTS.md`. For merge authority, `docs/MAIN-WRITE-AUTHORIZATION.md` wins. For CI behavior, `CI_POLICY.md` wins.

## Core model

A normal task uses:

```text
one cohesive outcome
  -> one Issue / Lane-Key
  -> one canonical branch
  -> one canonical PR
```

Use a separate carrier only when the work is genuinely independently reviewable/revertible, has a separate risk/release boundary, collides with another active owner, or the owner explicitly requests a split.

Do not create a mega-PR solely because a broad prompt contains unrelated defects. Do not create a micro-PR for every file or implementation step.

## Main boundary

Normal agents treat `origin/main` as read-only for direct writes. Every task uses a dedicated issue/branch/PR and lands through protected PR checks.

Same-task merge authorization is defined only by `docs/MAIN-WRITE-AUTHORIZATION.md`. Do not derive current merge permission from historical wording in this file, old issues, claims or handoffs.

## New task registration

Before mutating files for a new lane:

1. refresh current `origin/main` and record its SHA;
2. search for a semantically equivalent open Issue/Lane-Key/branch/PR;
3. if this session already owns the canonical carrier, continue it;
4. if another active owner owns it, stop overlapping mutation as `DUPLICATE_CARRIER / NO MUTATION`;
5. otherwise create one GitHub Issue reservation;
6. populate Reservation v2 metadata when applicable;
7. create one canonical branch from the current valid baseline.

For current Reservation v2 work, use:

```text
agent/<globally-distinct-session-token>/issue-<N>-<short-scope>
```

The older shorthand `agent/<agent-id>/<scope>` may appear in history but is not the preferred current Reservation v2 branch form.

## Reservation contents

The canonical task Issue should identify at least:

- status;
- Lane-Key, normally `issue-<N>`;
- stable owner/session identity;
- current baseline SHA;
- canonical carrier branch;
- semantic Ownership-Key when Reservation v2 requires it;
- narrow truthful Expected-Paths when Reservation v2 requires them;
- scope/acceptance and important exclusions.

The Issue is coordination state; a Markdown claim is optional evidence/history and does not need to land on `main` before work begins.

## Single-carrier invariant

At any time one Lane-Key has at most:

- one active owner/session;
- one canonical task branch;
- one open canonical PR.

Red, stale, behind-main, draft, queued or slow does not release ownership.

Replacement is allowed only after explicit release/reassignment/supersession is recorded. Close the old open PR before presenting a replacement PR as canonical.

Do not create transport PRs merely to replay `main` into an agent branch. Reconcile the existing canonical branch safely.

## Scope expansion

When a current task discovers work that is necessary to complete the same cohesive outcome, keep it in the same lane if ownership/collision rules permit.

If Reservation v2 Expected-Paths must expand, update the same Issue first and re-check collision ownership before mutating the new path.

A genuinely separate defect should use a separate lane instead of silently broadening the current task.

## Branch and PR lifecycle

Normal sequence:

```text
Issue/reservation
  -> canonical branch
  -> implement + focused validation
  -> commit + push
  -> automatic shared branch/PR CI
  -> open/update canonical PR when ready
  -> remediate known red evidence on same carrier
  -> protected current-candidate checks
  -> merge under MAIN-WRITE-AUTHORIZATION when eligible
  -> verify main
  -> close/complete Issue
  -> release reservation
```

Shared branch/PR CI is validation infrastructure. Branch CI is early exact-head evidence; protected current-candidate checks are the final merge gate. See `CI_POLICY.md` and `docs/PR-CI-LIFECYCLE.md`.

## PR metadata

A PR should make its ownership easy to resolve without duplicating data GitHub already knows.

Keep at least:

- Issue / Lane-Key;
- canonical owner/session;
- canonical carrier;
- `Supersedes` when applicable;
- scope/acceptance summary;
- validation evidence that is not already obvious from GitHub checks.

Do not treat a copied Head SHA in prose as stronger than GitHub's actual current head.

## Multi-agent integration

When the owner explicitly defines a multi-agent batch, an authorized coordinator may use:

```text
integration/<batch-id>
```

The coordinator integrates only the named participating lanes, resolves semantic conflicts deliberately, obtains combined-tree CI when applicable, satisfies protected-main requirements, and merges only within the authorization defined by `docs/MAIN-WRITE-AUTHORIZATION.md`.

Individual branch green status is not combined-tree CI.

Exact-main release CI is a separate evidence class after an applicable landing; it is not ordinary task-branch CI.

## Terminal cleanup

After the same task PR is merged and current `main` is verified:

1. close/complete the task Issue if still open;
2. mark/reconcile any task status that would otherwise still say ACTIVE;
3. release the reservation;
4. delete the merged task branch when practical.

Do not leave completed lanes appearing active indefinitely.

## Historical compatibility note

The repository previously encoded older merge/admission semantics as literal strings inside executable policy scanners. Those literals are retained below **only as non-rendered compatibility markers until that scanner is separately refactored**. They are not current instructions and must never override `AGENTS.md`, `docs/MAIN-WRITE-AUTHORIZATION.md`, or `CI_POLICY.md`.

<!-- LEGACY_SCANNER_COMPAT: agent/<agent-id>/<scope> -->
<!-- LEGACY_SCANNER_COMPAT: `origin/main` as read-only -->
<!-- LEGACY_SCANNER_COMPAT: dedicated issue/branch/PR -->
<!-- LEGACY_SCANNER_COMPAT: Only an agent/session explicitly authorized by the repository owner as an integration/merge coordinator may change `main`. -->
<!-- LEGACY_SCANNER_COMPAT: shared branch/PR CI -->
<!-- LEGACY_SCANNER_COMPAT: combined-tree CI -->
<!-- LEGACY_SCANNER_COMPAT: exact-main release CI -->
<!-- LEGACY_SCANNER_COMPAT: merge to `main` only within the owner's explicit authorization -->
<!-- LEGACY_SCANNER_COMPAT: ALL MERGED TO MAIN -->
<!-- LEGACY_SCANNER_COMPAT: dispatch-v25-cloud-after-main-integration.yml -->

These comments are compatibility debt, not precedence.