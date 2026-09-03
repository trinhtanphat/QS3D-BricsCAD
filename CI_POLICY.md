# GitHub Actions / CI Policy

This file is the repository-level source of truth for GitHub Actions behavior. `docs/MAIN-WRITE-AUTHORIZATION.md` is authoritative for who may merge the same task PR to `main`.

## CI purpose

CI validates candidates; it does not grant merge authority and it does not publish ordinary task branches.

QS3D uses three evidence stages:

1. **automatic branch/PR validation** through `.github/workflows/ci.yml`;
2. combined integration validation for `integration/<batch-id>` when an authorized multi-agent batch exists;
3. **exact-main release** validation/publishing through the approved post-main dispatcher and release workflows.

A green result qualifies only the exact SHA/candidate it tested.

## Automatic branch CI and canonical PR lifecycle

Every push to `agent/**` and `integration/**` is eligible for automatic shared validation. Every protected PR gets the stable required contexts.

Branch CI is early exact-head evidence. **its completion timestamp is not a permanent PR-admission identity**.

A canonical PR may already exist while its matching branch run is queued/running/completes later. Do not close/recreate a correct PR merely to make timestamps ordered.

A known red branch run must still be diagnosed and fixed on the same canonical carrier. Never use a PR to hide a known current-head failure.

The hard merge gate is the protected current PR candidate: required `preflight` and `core` must be terminal `SUCCESS`, strict freshness must be satisfied, ownership/collision checks must pass and the PR must be mergeable.

## Shared automatic branch/PR CI

`.github/workflows/ci.yml` is the owner-approved automatic non-publishing validation workflow.

It may run on:

- every push to `agent/**`;
- every push to `integration/**`;
- **every** pull request targeting `main`;
- every pull request targeting `integration/**`;
- manual `workflow_dispatch` for authorized recovery/testing.

The workflow remains read-only/non-publishing. Validation checkouts use `persist-credentials: false`.

It must not tag, publish, release, sign, mutate Issues, merge PRs, dispatch unrelated publishing workflows or write repository contents.

### Validation tiers

The shared workflow classifies changed paths internally while preserving stable `preflight` and `core` contexts.

1. **repository-metadata tier** — ordinary docs/repository metadata receive lightweight policy/professionalism validation and a lightweight `core` success.
2. **policy/source-guard tier** — canonical governance/source-guard inputs receive source/policy validation without a redundant Core/V25 build when no build-relevant input changed.
3. **full build tier** — production/build-relevant inputs run source guards plus Core build/smoke and applicable V25 compile validation.

Current build-relevant surfaces include source/tests/scripts/workflows/build roots and `samples/generated/**` according to the executable classifier.

Changed paths are authoritative. Commit prefixes such as `docs:` or `chore:` do not override path classification.

## Branch CI versus PR CI

Preferred low-churn usage is:

```text
implement
  -> commit + push branch
  -> automatic branch CI starts
  -> inspect/remediate known red branch evidence
  -> open/update canonical PR when ready
  -> protected current-candidate preflight + core
  -> refresh/reconcile if strict freshness requires it
  -> merge when current/green/mergeable and authorized
```

Branch CI provides early isolated feedback. PR CI validates the current merge candidate against its target. They are complementary, not competing carrier identities.

Do not require PR recreation because branch CI completed after PR creation. Do require fresh evidence whenever the actual candidate changes.

## Red CI remediation

For a failure on the current owned carrier:

1. bind the failure to the exact tested SHA/candidate;
2. inspect the failing job/step/log evidence available;
3. fix the root cause on the same branch;
4. add/strengthen regression coverage when appropriate;
5. commit + push;
6. observe fresh automatic validation;
7. repeat while another safe same-lane remediation exists.

Do not weaken correctness/security/release guards merely to make a candidate green.

## Protected main

The expected GitHub ruleset contract for `main` is:

- require a PR;
- require stable checks `preflight` and `core`;
- strict required-status freshness;
- block deletion;
- block non-fast-forward/force-push;
- no unexpected bypass actor.

When a claim about hard protection matters, verify the effective GitHub ruleset rather than relying on Markdown alone.

## Multi-agent integration

An explicitly authorized multi-agent coordinator may assemble the named batch on:

```text
integration/<batch-id>
```

The coordinator validates the exact combined tree and does not silently drop participating work. Branch CI from individual lanes is not combined-tree CI.

The combined candidate must satisfy the applicable protected checks/freshness before authorized merge.

## Hybrid native auto-merge coordinator

`.github/workflows/hybrid-pr-coordinator.yml` is the single owner-approved queue coordinator. It may arm **GitHub native auto-merge** for an eligible open, non-draft, same-repository PR targeting `main`, unless the PR carries `no-automerge`.

The coordinator does not perform the final merge itself. GitHub protected-main rules remain authoritative: the current candidate still needs fresh successful `preflight` and `core`, strict freshness, mergeability and every other effective repository rule before GitHub can merge it.

After a landing on `main`, the same workflow may refresh remaining eligible same-repository PR branches through GitHub's `update-branch` operation using the current PR head SHA as an optimistic lock. Forks, drafts, conflicts and `no-automerge` PRs are skipped. Force-push, reset, direct writes to `main`, direct PR merge endpoints and `gh pr merge` remain forbidden.

The refresh path uses `QS3D_AUTOMERGE_TOKEN`, a repository-scoped fine-grained credential, so accepted branch updates emit normal PR synchronization/CI events. Missing credentials fail closed; the workflow must not silently fall back to mutating branches with `GITHUB_TOKEN`.

This is a narrow coordinator authorization, not permission for ordinary agents or arbitrary workflows to bulk merge unrelated PRs. Repository-wide blind auto-merge remains intentionally disabled.

## Exact-main automatic V25 cloud CI

The approved dispatcher is `.github/workflows/dispatch-v25-cloud-after-main-integration.yml`.

Its role is separate from shared PR validation. It may dispatch `release-v25-cloud.yml` only according to its current protected exact-source contract after an integration-relevant landing on `main`.

Ordinary docs-only landings outside the dispatcher's watched integration-relevant paths must not trigger the V25 cloud release path.

Automatic validation authorization does not imply release authorization.

## Manual workflows

Workflows other than shared `ci.yml`, the approved hybrid PR coordinator and the approved main dispatcher remain owner-controlled manual lanes unless a current canonical policy explicitly says otherwise.

Release workflows retain their own confirmation/protection boundaries.

A normal `continue all`, `fix bug`, source change, docs change or CI remediation does not authorize unrelated manual release dispatch/rerun/cancel operations.

### Dependabot generated-PR boundary

GitHub Dependabot may create dependency-update PRs directly from committed Dependabot configuration.

This generated-PR boundary does **not** authorize Dependabot to merge, write `main`, enable autonomous protected-main merge, bypass checks or publish releases.

Dependabot PRs still require the protected current-candidate checks applicable to `main`.

Repository-wide blind auto-merge remains intentionally disabled.

## LOCAL_ONLY evidence

Hosted/static CI does not prove licensed BricsCAD runtime, private-DWG behavior, native Windows UI behavior, signing credentials or other environment-gated evidence.

Only compatible execution tied to an exact SHA may produce `LOCAL_PASS`.

## Completion terminology

For ordinary owner task work, `MERGED_MAIN` is the normal successful repository endpoint under `docs/MAIN-WRITE-AUTHORIZATION.md`.

`ALL MERGED TO MAIN` is a broader batch/integration verification phrase only; do not use it merely because one branch is green or one PR merged.

Release/publication status is separate unless explicitly part of the current task.

## Enforcement

The executable workflow and preflight scripts remain the machine enforcement. This Markdown describes semantics and must stay aligned with them.

In particular:

- `.github/workflows/ci.yml` must preserve stable `preflight` and `core` contexts;
- automatic branch pushes must remain available for exact-head evidence;
- PR path filters must not suppress required protected contexts;
- shared validation must remain non-publishing/read-only;
- `.github/workflows/hybrid-pr-coordinator.yml` is the only workflow authorized to arm native PR auto-merge and refresh eligible PR branches;
- the approved main dispatcher must remain narrow;
- release workflows must retain their explicit safety/confirmation requirements.

When executable behavior and prose diverge, treat that as a governance defect and reconcile them on a normal task branch/PR.