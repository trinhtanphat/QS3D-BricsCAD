# Hybrid Native Auto-Merge Coordinator Design

## Context

QS3D currently protects `main` with PR-only changes plus strict required `preflight` and `core` checks. Repository policy intentionally rejects repository-wide blind auto-merge and rejects autonomous merge primitives inside arbitrary workflows. The owner has explicitly authorized one narrow coordinator to automate the queue without weakening those protections.

The previous #5322/#5323 attempt tried to merge directly from a workflow after CI success. `scripts/preflight-repository-professionalism.py` correctly rejected that design because direct PR merge API calls and generic autonomous merge primitives are forbidden. This successor keeps GitHub itself as the actor that performs the final merge.

Repository settings verified on 2026-09-02:

- `allow_auto_merge=true`
- `allow_update_branch=true`
- protected `main` ruleset requires pull requests, strict freshness, `preflight`, and `core`
- no bypass actor is configured

## Goal

Automatically drain eligible same-repository PRs into `main` while preserving protected-main checks, exact current-candidate validation, explicit opt-out, and non-destructive branch reconciliation.

## Non-goals

- Do not directly call the pull-request merge endpoint.
- Do not force-push, reset, or directly update `main`.
- Do not mutate fork PR branches.
- Do not merge drafts, conflicted PRs, or PRs carrying `no-automerge`.
- Do not bypass, synthesize, or downgrade required `preflight`/`core` checks.
- Do not publish releases or invoke release workflows.
- Do not claim licensed BricsCAD runtime evidence.

## Architecture

Create exactly one automatic workflow: `.github/workflows/hybrid-pr-coordinator.yml`.

The workflow has two independent event paths.

### 1. Arm native auto-merge on PR lifecycle events

Trigger on `pull_request` actions:

- `opened`
- `reopened`
- `ready_for_review`
- `synchronize`
- `unlabeled`

The arm job runs only when all of the following are true:

- base branch is `main`;
- PR is open;
- PR is not draft;
- PR head repository is exactly `trinhtanphat/QS3D-BricsCAD`;
- PR does not have label `no-automerge`;
- event PR head SHA still equals the current API PR head SHA.

The job uses the normal `GITHUB_TOKEN` with `pull-requests: write` and `contents: read`. It invokes GitHub GraphQL `enablePullRequestAutoMerge` for that PR. The mutation only arms GitHub native auto-merge; it does not call the REST merge endpoint and it cannot bypass protected-main requirements.

GitHub then performs the actual merge only when the PR's *current* candidate satisfies repository rules. If the head changes, required checks/freshness apply to the new current candidate before GitHub can merge it.

### 2. Refresh remaining PRs after every `main` landing

Trigger on `push` to `main`.

The refresh job enumerates open PRs targeting `main` and filters out:

- drafts;
- forks;
- PRs with `no-automerge`;
- PRs whose mergeable state reports conflict/dirty;
- PRs whose head repository is not the canonical repository.

For each remaining PR it re-fetches current metadata immediately before mutation and calls GitHub's `update-branch` endpoint with the PR's current head SHA as the optimistic-lock value. The coordinator never force-resets or rewrites a branch.

Refresh mutations use repository secret `QS3D_AUTOMERGE_TOKEN`, not `GITHUB_TOKEN`. The secret must be a fine-grained PAT restricted to `trinhtanphat/QS3D-BricsCAD` with only:

- Contents: Read and write
- Pull requests: Read and write

This external credential is required because events emitted by mutations authenticated with `GITHUB_TOKEN` are not a reliable source of recursive workflow runs. A PAT-backed `update-branch` creates the normal `synchronize` lifecycle, causing shared CI and the auto-merge arm path to evaluate the refreshed candidate.

If `QS3D_AUTOMERGE_TOKEN` is absent, the refresh job fails closed with an explicit configuration error. It must not fall back to mutation with `GITHUB_TOKEN`.

## Concurrency

Use one repository-wide coordinator concurrency group with `cancel-in-progress: false`. This serializes coordinator mutations so a `main` landing cannot race another refresh/arm cycle into destructive assumptions. Individual mutations still re-fetch PR state and use exact current head data before action.

## Governance exception

The repository remains fail-closed for autonomous merge behavior generally. Two existing policy scanners are updated to recognize exactly `.github/workflows/hybrid-pr-coordinator.yml` as a third owner-approved automatic workflow.

### `scripts/preflight-ci-manual-only.py`

The exception must require the coordinator to expose exactly `pull_request` plus `push` triggers with the specified PR actions and `main` push branch. It must reject release/publishing primitives, direct `main` writes, force pushes, or unrelated workflow dispatches.

### `scripts/preflight-repository-professionalism.py`

The global autonomous-merge scan remains in force for every other workflow. The named coordinator alone may contain the `enablePullRequestAutoMerge` GraphQL mutation, and even there direct merge primitives remain forbidden:

- `gh pr merge` remains forbidden;
- REST `/pulls/{number}/merge` remains forbidden;
- `pull_request_target` remains forbidden.

The exception is exact-path and exact-primitive, not a generic token relaxation.

## Executable regression guard

Add `scripts/preflight-hybrid-pr-coordinator.py`. Because `scripts/preflight-all.py` automatically executes every `preflight-*.py`, this guard becomes part of aggregate source validation.

The guard fails unless the coordinator has all required contracts, including:

- exact workflow name/path;
- exact automatic trigger families;
- serialized concurrency;
- no `pull_request_target`;
- no direct merge API or `gh pr merge`;
- native `enablePullRequestAutoMerge` marker;
- same-repository/base-main/draft/opt-out filters;
- exact PR head re-fetch check before arming;
- `update-branch` endpoint with current head SHA;
- `QS3D_AUTOMERGE_TOKEN` for refresh;
- no PAT fallback to `GITHUB_TOKEN` mutation;
- no force/reset/direct-main primitive.

The first commit adds this guard before the coordinator exists, intentionally producing RED evidence. Subsequent implementation makes it GREEN.

## Queue behavior

The expected steady-state loop is:

```text
PR opened/synchronized
  -> coordinator arms native auto-merge
  -> shared CI validates current candidate
  -> GitHub protected-main rules decide eligibility
  -> GitHub merges eligible PR
  -> push main
  -> coordinator refreshes remaining eligible same-repo PRs
  -> synchronize events rerun shared CI and re-arm native auto-merge
  -> next eligible PR lands
```

A red, stale, draft, opted-out, conflicted, or fork PR simply remains open. The coordinator never converts a failing candidate into a passing one and never erases PR work.

## Rollout

This task itself lands through the existing protected PR path. The coordinator only becomes active after its workflow file reaches default `main`.

Before calling the automation operational, verify:

1. exact task PR `preflight` and `core` are successful and fresh;
2. the task PR merges through protected `main` without bypass;
3. `main` contains the coordinator commit;
4. repository settings still show `allow_auto_merge=true` and `allow_update_branch=true`;
5. `QS3D_AUTOMERGE_TOKEN` exists before relying on automatic branch refresh.
