# Main write authorization policy

This document is the canonical repository rule for who may change `main` and for the default completion endpoint of normal owner-requested repository tasks.

## Default rule: agents treat `main` as read-only

`origin/main` is read-only for direct task writes. Source, tests, scripts, workflows, docs, Markdown, claims, handoffs, status files and chores must land through a dedicated task branch and protected PR.

## Explicit authorization required

A normal agent must never use a direct ref update, direct contents write, force-push, protection bypass, or equivalent write primitive against `main` for ordinary task work.

Merge authorization is exercised through the task PR after the repository's required checks and merge gates are satisfied.

## Standing owner authorization for the same task PR

For a normal repository-owner request, the owner grants standing authorization to finish the **same owned task PR** through merge when all of these are true:

- implementation/required docs are complete;
- known current-lane failures have been remediated;
- required protected checks are green on the current candidate;
- strict freshness is satisfied;
- the PR is mergeable and collision-clean;
- the expected current head/candidate is still the one being merged;
- the owner has not opted out of merge for that task.

The owning agent should then merge the same task PR through the protected PR path without waiting for a second `merge main` message.

The owner may opt out with wording such as `PR only`, `do not merge main`, `stop before merge`, `đừng merge`, or a clear equivalent.

Standing authorization never permits:

- direct writes to `main`;
- bypassing required checks;
- weakening protection;
- force-pushing;
- merging unrelated PRs;
- bulk-integrating another agent's lanes without the applicable coordinator assignment.

## Owner-authorized repository-wide merge coordinator

The repository owner separately authorizes exactly `.github/workflows/green-pr-drain.yml` as a **repository-wide merge coordinator** for ordinary same-repository PRs targeting `main`.

This named workflow may merge across task lanes only when its machine-enforced contract verifies all of the following immediately before the merge request:

- the upstream shared PR CI completed successfully;
- the successful workflow run belongs to the PR's exact current head SHA;
- the PR is open and non-draft;
- the PR targets `main` and its head belongs to this repository, not a fork;
- current `main` is an ancestor/merge base of the tested head, so strict freshness has been reconciled;
- the PR has no `no-automerge` label;
- the PR is not authored by Dependabot;
- GitHub still accepts the exact expected head SHA through the protected PR merge endpoint.

The workflow may then request non-force `update-branch` reconciliation for the other open same-repository PRs so they re-run CI against the new `main`. A conflict or rejected update must leave that branch unchanged.

This authorization belongs only to the named workflow. **Normal agents** still may not sweep unrelated PRs merely because they are green, may not impersonate the coordinator, and may not bypass any protected-main rule. Converting a PR to draft or applying the `no-automerge` label is an explicit machine-readable opt-out from coordinator merge.

## Normal successful endpoint: `MERGED_MAIN`

For ordinary owner-requested repository work, intermediate states are not the default endpoint:

```text
edited
  != committed
  != pushed
  != branch CI green
  != PR open
  != PR green
  != MERGED_MAIN
```

The normal path is:

```text
implement/fix
  -> validate available evidence
  -> commit + push canonical branch
  -> automatic branch CI / remediation
  -> open/update canonical PR
  -> protected `preflight` + `core` SUCCESS
  -> current + mergeable + collision-clean
  -> merge same task PR, or named green-drain coordinator merge when eligible
  -> refresh/verify resulting main SHA
  -> close/complete task Issue and release reservation
  -> delete merged task branch when practical
  -> MERGED_MAIN
```

Branch-CI completion timestamp is not a permanent PR-admission identity. A correct canonical PR may coexist with branch CI that is queued/running/completes later. Known red branch evidence still must be fixed before merge; protected current-candidate checks are authoritative.

## Merge safety gates

Do not merge when any of the following is true:

- required `preflight` or `core` is pending, failed, cancelled, missing or stale;
- GitHub requires another freshness update/revalidation;
- the PR is not mergeable;
- a collision/ownership rule rejects the carrier;
- intended task content was lost during reconciliation;
- the owner opted out of merge;
- the PR contains unrelated/unreviewed work outside the authorization applicable to the actor performing the merge;
- protected-main state is unexpectedly weakened or bypassed.

When a safely fixable gate fails, fix/reconcile on the same canonical branch and continue. Red CI is a remediation trigger, not a reason to hand routine work back to the owner.

## Main remains PR-only for every content class

There is no docs-only direct-main exception. The same protected path applies to:

- `src/**`;
- `tests/**`;
- `scripts/**`;
- workflows/configuration;
- `docs/**` and `*.md`;
- claims/handoffs/inbox/status files;
- release-note preparation;
- chores.

## Multi-agent batches

Standing same-task authorization does not grant unrelated batch authority to normal agents.

When the owner explicitly assigns a multi-agent integration batch, an authorized coordinator may assemble the named participating lanes on:

```text
integration/<batch-id>
```

The coordinator integrates only the named participating lanes, resolves semantic conflicts deliberately, obtains combined-tree CI when applicable, satisfies protected-main requirements, and merges only within the authorization defined here.

Outside the explicitly named `green-pr-drain.yml` coordinator, do not independently sweep/merge unrelated open PRs merely because they are green.

## Reservation cleanup after merge

After the protected merge succeeds and current `main` is verified:

1. close/complete the task Issue if still open;
2. release the active reservation/ownership state;
3. update any task-specific handoff/inbox state that must remain current;
4. delete the merged task branch when practical.

A merged implementation must not leave its Issue presented as an ACTIVE owner indefinitely.

## Release boundary

`MERGED_MAIN` is the normal endpoint for an ordinary code/docs/chore task. Release/package/publication/runtime evidence is reported as a required endpoint only when the owner request explicitly includes it or it is an actual acceptance blocker.

CI authorization, merge authorization and release authorization are separate concepts. See `CI_POLICY.md`.

## GitHub protection

The repository's protected-main ruleset is expected to require:

- PR-based changes;
- required `preflight` and `core` checks;
- strict required-status freshness;
- deletion protection;
- non-fast-forward/force-push protection;
- no unexpected bypass actor.

Markdown describes intent; effective GitHub rules are the hard enforcement and should be verified when protection state matters.

## Precedence

If another repository document conflicts with this file on direct-main permission, same-task standing merge authorization, the named repository-wide coordinator, or the normal `MERGED_MAIN` endpoint, this file wins unless the repository owner explicitly changes the policy.
