# Hybrid Native Auto-Merge Coordinator Design

## Context

QS3D currently protects `main` with PR-only changes plus strict required `preflight` and `core` checks. Repository policy intentionally rejects repository-wide blind auto-merge and rejects autonomous merge primitives inside arbitrary workflows. The owner has explicitly authorized one narrow coordinator to automate the queue without weakening those protections.

The previous #5322/#5323 attempt tried to merge directly from a workflow after CI success. `scripts/preflight-repository-professionalism.py` correctly rejected that design because direct PR merge API calls and generic autonomous merge primitives are forbidden. This successor keeps GitHub itself as the actor that performs the final merge.

Repository settings verified on 2026-09-02:

- `allow_auto_merge=true`
- `allow_update_branch=true`
- protected `main` ruleset requires pull requests, strict freshness, `preflight`, and `core`
- no bypass actor is configured

Runtime verification on 2026-09-02 also established that the workflow-scoped `GITHUB_TOKEN`, despite exposing `PullRequests: write`, receives `Resource not accessible by integration` for the GraphQL `enablePullRequestAutoMerge` mutation in this repository. Therefore every coordinator mutation uses the dedicated repository secret `QS3D_AUTOMERGE_TOKEN`; there is no mutation fallback to `GITHUB_TOKEN`.

## Goal

Automatically drain eligible same-repository PRs into `main` while preserving protected-main checks, exact current-candidate validation, explicit opt-out, and non-destructive branch reconciliation.

## Non-goals

- Do not directly call the pull-request merge endpoint.
- Do not force-push, reset, or directly update `main`.
- Do not mutate fork PR branches.
- Do not merge drafts, conflicted PRs, Dependabot PRs, or PRs carrying `no-automerge`.
- Do not bypass, synthesize, or downgrade required `preflight`/`core` checks.
- Do not publish releases or invoke release workflows.
- Do not claim licensed BricsCAD runtime evidence.

## Architecture

Create exactly one automatic workflow: `.github/workflows/hybrid-pr-coordinator.yml`.

The workflow has two independent event paths.

### 1. Reconcile native auto-merge on PR lifecycle events

Trigger on `pull_request` actions:

- `opened`
- `reopened`
- `ready_for_review`
- `converted_to_draft`
- `synchronize`
- `labeled`
- `unlabeled`

The PR job first requires non-empty `QS3D_AUTOMERGE_TOKEN` and fails closed with an explicit configuration error if the credential is unavailable. It then re-fetches the PR and stays within the same-repository/main boundary. The event PR head SHA must still equal the current API PR head SHA before any auto-merge-state mutation.

An eligible PR must be:

- targeting `main`;
- open;
- non-draft;
- hosted in exactly `trinhtanphat/QS3D-BricsCAD`;
- not authored by `dependabot[bot]`;
- free of the `no-automerge` label.

For an eligible PR, the job uses the PAT-backed GitHub GraphQL `enablePullRequestAutoMerge` mutation. For a previously armed PR that becomes draft or receives `no-automerge`, the same job uses `disablePullRequestAutoMerge`. The workflow never calls the direct REST merge endpoint and never performs the final merge itself.

GitHub then performs the actual merge only when the PR's *current* candidate satisfies repository rules. If the head changes, required checks/freshness apply to the new current candidate before GitHub can merge it.

### 2. Refresh remaining PRs after every `main` landing

Trigger on `push` to `main`.

The refresh job requires the same `QS3D_AUTOMERGE_TOKEN` and fails closed when it is absent. It enumerates open PRs targeting `main` and filters out:

- drafts;
- forks;
- Dependabot PRs;
- PRs with `no-automerge`;
- PRs whose mergeable state reports conflict/dirty;
- PRs whose head repository is not the canonical repository.

For each remaining PR it re-fetches current metadata immediately before mutation, ensures native auto-merge is armed through the PAT-backed GraphQL mutation, then calls GitHub's `update-branch` endpoint with the PR's current head SHA as the optimistic-lock value. The coordinator never force-resets or rewrites a branch.

The secret must be a fine-grained PAT restricted to `trinhtanphat/QS3D-BricsCAD` with only:

- Contents: Read and write
- Pull requests: Read and write

The external credential is required both because `enablePullRequestAutoMerge` is not accessible to the repository workflow token in the observed runtime and because PAT-backed branch updates can emit the normal `synchronize` lifecycle needed for shared CI and coordinator reevaluation.

If `QS3D_AUTOMERGE_TOKEN` is absent, either mutation path fails closed with an explicit configuration error. The workflow must not silently report successful coordination and must not fall back to mutation with `GITHUB_TOKEN`.

## Concurrency

Use one repository-wide coordinator concurrency group with `cancel-in-progress: false`. This serializes coordinator mutations so a `main` landing cannot race another refresh/arm cycle into destructive assumptions. Individual mutations still re-fetch PR state and use exact current head data before action.

## Governance exception

The repository remains fail-closed for autonomous merge behavior generally. Two existing policy scanners are updated to recognize exactly `.github/workflows/hybrid-pr-coordinator.yml` as a third owner-approved automatic workflow.

### `scripts/preflight-ci-manual-only.py`

The exception must require the coordinator to expose exactly `pull_request` plus `push` triggers with the specified PR actions and `main` push branch. It must require PAT-backed mutations and reject release/publishing primitives, direct `main` writes, force pushes, or unrelated workflow dispatches.

### `scripts/preflight-repository-professionalism.py`

The global autonomous-merge scan remains in force for every other workflow. The named coordinator alone may contain the `enablePullRequestAutoMerge` and `disablePullRequestAutoMerge` GraphQL mutations, and even there direct merge primitives remain forbidden:

- `gh pr merge` remains forbidden;
- REST `/pulls/{number}/merge` remains forbidden;
- `pull_request_target` remains forbidden.

The exception is exact-path and exact-primitive, not a generic token relaxation.

## Executable regression guard

Add `scripts/preflight-hybrid-pr-coordinator.py`. Because `scripts/preflight-all.py` automatically executes every `preflight-*.py`, this guard becomes part of aggregate source validation.

The guard fails unless the coordinator has all required contracts, including:

- exact workflow name/path;
- exact automatic trigger families, including draft/label transitions needed to disarm;
- serialized concurrency;
- no `pull_request_target`;
- no direct merge API or `gh pr merge`;
- native `enablePullRequestAutoMerge` and `disablePullRequestAutoMerge` markers;
- same-repository/base-main/draft/opt-out/Dependabot filters;
- exact PR head re-fetch check before arming/disarming;
- PAT-backed GraphQL mutation rather than `github.token`;
- `update-branch` endpoint with current head SHA;
- `QS3D_AUTOMERGE_TOKEN` for refresh;
- explicit nonzero failure when the PAT is missing;
- no PAT fallback to `GITHUB_TOKEN` mutation;
- no force/reset/direct-main primitive.

The first commit adds this guard before the coordinator exists, intentionally producing RED evidence. Subsequent implementation makes it GREEN. A later regression cycle additionally proved that warning-only handling of a missing PAT was incorrect; the guard now requires both mutation paths to fail closed.

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

If an armed PR becomes draft or gains `no-automerge`, its PR lifecycle event causes the coordinator to disarm native auto-merge. A red, stale, draft, opted-out, conflicted, Dependabot, or fork PR otherwise remains open. The coordinator never converts a failing candidate into a passing one and never erases PR work.

## Rollout

This task itself lands through the existing protected PR path. The coordinator only becomes active after its workflow file reaches default `main`.

Before calling the automation operational, verify:

1. exact task PR `preflight` and `core` are successful and fresh;
2. the task PR merges through protected `main` without bypass;
3. `main` contains the coordinator commit;
4. repository settings still show `allow_auto_merge=true` and `allow_update_branch=true`;
5. `QS3D_AUTOMERGE_TOKEN` exists before relying on any automatic arm/disarm or branch-refresh behavior;
6. a live coordinator run performs a PAT-backed mutation successfully instead of taking the missing-secret failure path.
