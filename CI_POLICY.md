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

## Automatic Ready PR refresh/auto-merge arming

`.github/workflows/auto-merge-main-prs.yml` is the single owner-approved Ready-PR reconciliation automation for pull requests targeting `main`.

**Draft is a manual hold.** The workflow must not mark Draft PRs Ready and must not arm them for merge. A human or authorized agent lifts the hold by explicitly marking the PR Ready.

For Ready same-repository PRs, the workflow reacts to PR lifecycle events and to every push on `main`. When GitHub reports a PR as `BEHIND`, it calls the official `update-branch` operation using the current `headRefOid` as `expected_head_sha`. That optimistic lock prevents updating a head that changed after inspection. Cross-repository PRs and merge-conflicted PRs are skipped rather than mutated.

After any needed refresh, the workflow idempotently arms GitHub native auto-merge. Repository-wide native auto-merge arming is explicitly enabled for Ready PRs targeting `main`; branch protection and required checks remain authoritative.

The workflow does not perform the final merge. GitHub protected-main rules still require the current candidate to satisfy fresh successful `preflight` and `core`, strict freshness, mergeability and every other effective repository rule before GitHub may merge it. Updating a stale branch changes the candidate, so normal PR CI must validate the new exact head before merge.

`pull_request_target` is used only for trusted base-repository metadata operations, and the `push` handler operates only on `refs/heads/main`. The workflow must never checkout or execute PR head code, force-push, dispatch releases, call a direct pull-request merge endpoint, use `--admin`, or bypass protected-main rules. It may read `headRefOid` only as metadata for the `expected_head_sha` optimistic lock.

The automation uses the repository `GITHUB_TOKEN` with `contents: write` and `pull-requests: write`. `contents: write` exists solely because GitHub's `update-branch` operation requires permission to write the same-repository PR head; direct content commits/ref rewrites remain forbidden. It may call `gh pr merge --auto --merge` only to arm native auto-merge. The `--auto` operation is queue arming, not a direct merge authorization.

The retired `.github/workflows/hybrid-pr-coordinator.yml` must remain absent.

This is a narrow repository-owner authorization for Ready-PR reconciliation and native auto-merge arming, not permission for ordinary agents or arbitrary workflows to directly merge unrelated PRs. Repository-wide blind direct merge remains disabled.

## Exact-main automatic V25 cloud CI

The approved dispatcher is `.github/workflows/dispatch-v25-cloud-after-main-integration.yml`.

Its role is separate from shared PR validation. It may dispatch `release-v25-cloud.yml` only according to its current protected exact-source contract after an integration-relevant landing on `main`.

Ordinary docs-only landings outside the dispatcher's watched integration-relevant paths must not trigger the V25 cloud release path.

Automatic validation authorization does not imply release authorization.

## Manual workflows

Workflows other than shared `ci.yml`, the approved `auto-merge-main-prs.yml` Ready-PR reconciliation automation and the approved main dispatcher remain owner-controlled manual lanes unless a current canonical policy explicitly says otherwise.

Release workflows retain their own confirmation/protection boundaries.

A normal `continue all`, `fix bug`, source change, docs change or CI remediation does not authorize unrelated manual release dispatch/rerun/cancel operations.

### Dependabot generated-PR boundary

GitHub Dependabot may create dependency-update PRs directly from committed Dependabot configuration.

Dependabot itself does **not** receive merge authority, write `main`, bypass checks or publish releases. A Dependabot PR targeting `main` is subject to the same repository-owned Ready/refresh/auto-merge automation as any other PR, and protected-main checks remain authoritative.

Dependabot PRs still require the protected current-candidate checks applicable to `main`.

Repository-wide blind direct merge remains disabled; repository-wide native auto-merge arming is explicitly enabled only through the owner-approved Ready-PR automation.

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
- `.github/workflows/auto-merge-main-prs.yml` is the only workflow authorized to update stale same-repository Ready PRs targeting `main` through `update-branch` and arm GitHub native auto-merge; Draft remains a manual hold;
- the retired `.github/workflows/hybrid-pr-coordinator.yml` must remain absent;
- the approved main dispatcher must remain narrow;
- release workflows must retain their explicit safety/confirmation requirements.

When executable behavior and prose diverge, treat that as a governance defect and reconcile them on a normal task branch/PR.