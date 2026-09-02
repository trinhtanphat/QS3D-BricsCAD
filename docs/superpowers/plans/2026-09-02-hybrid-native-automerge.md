# Hybrid Native Auto-Merge Coordinator Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add one fail-closed repository coordinator that arms/disarms GitHub native auto-merge for eligible same-repository PRs and refreshes remaining PR branches after every `main` landing without direct merge or force-update primitives.

**Architecture:** `.github/workflows/hybrid-pr-coordinator.yml` owns two event paths: PR lifecycle events reconcile native auto-merge state, and `push` to `main` refreshes other eligible PR heads through GitHub's `update-branch` API. All coordinator mutations use `QS3D_AUTOMERGE_TOKEN`; runtime verification showed the workflow-scoped `GITHUB_TOKEN` cannot call `enablePullRequestAutoMerge` in this repository. Existing repository policy scanners gain an exact-path exception for this one coordinator while a new aggregate-discovered preflight guard locks the required safety markers.

**Tech Stack:** GitHub Actions YAML, GitHub GraphQL/REST through `gh api`, Python 3.12 repository preflight scripts, GitHub protected-main rules.

**Spec:** `docs/superpowers/specs/2026-09-02-hybrid-native-automerge-design.md`

## Global Constraints

- `main` remains PR-only; never write/update/force `main` directly.
- Final merge authority remains GitHub protected-main rules with strict `preflight` and `core` checks.
- Only `.github/workflows/hybrid-pr-coordinator.yml` may receive the new autonomous coordinator exception.
- Direct REST PR merge calls, `gh pr merge`, `pull_request_target`, force/reset, and release dispatch remain forbidden.
- Fork PRs, drafts, conflicts, Dependabot PRs, and `no-automerge` PRs are never armed/refreshed by the coordinator.
- Both native auto-merge state mutations and refresh mutations must use `QS3D_AUTOMERGE_TOKEN`; never fall back to `GITHUB_TOKEN`.
- A missing `QS3D_AUTOMERGE_TOKEN` must fail the applicable coordinator job with a nonzero exit; warning-only success is forbidden.
- Repository settings must remain `allow_auto_merge=true` and `allow_update_branch=true`.

---

### Task 1: Add the failing coordinator policy guard

**Files:**
- Create: `scripts/preflight-hybrid-pr-coordinator.py`

**Interfaces:**
- Consumes: repository root and `.github/workflows/hybrid-pr-coordinator.yml`.
- Produces: exit code 0 only when the coordinator satisfies the complete static safety contract; nonzero otherwise. `scripts/preflight-all.py` auto-discovers the file by `preflight-*.py` naming.

- [x] **Step 1: Write the failing guard**

Implement a Python script that reads the coordinator as strict UTF-8 and fails if it is absent. Require literal/regex contracts for the exact workflow name, `pull_request` and `push` triggers, PR actions `opened`, `reopened`, `ready_for_review`, `converted_to_draft`, `synchronize`, `labeled`, `unlabeled`, `main` push scope, repository-wide serialized concurrency, native enable/disable auto-merge GraphQL mutations, current-head API re-fetch, same-repository/base-main/draft/`no-automerge`/Dependabot filters, `update-branch` with current head SHA, and `QS3D_AUTOMERGE_TOKEN`. Reject `pull_request_target`, `gh pr merge`, `/pulls/.../merge`, `git push`, `git reset`, `--force`, mutation using `github.token`, and warning-only success when the PAT is missing.

- [x] **Step 2: Run RED validation**

Initial RED: aggregate preflight failed because `.github/workflows/hybrid-pr-coordinator.yml` did not exist. A later regression RED additionally proved that warning-only handling of missing `QS3D_AUTOMERGE_TOKEN` was incorrect.

- [x] **Step 3: Record the RED evidence**

Initial exact-head RED was recorded at `8b4831181d5873843ae68ac2023c20dccad0fa9f`. The credential regression was reproduced against the warning-only workflow and the guard was tightened before production behavior was changed.

---

### Task 2: Implement the minimal coordinator workflow

**Files:**
- Create: `.github/workflows/hybrid-pr-coordinator.yml`

**Interfaces:**
- Consumes: PR lifecycle event JSON and `secrets.QS3D_AUTOMERGE_TOKEN` for every coordinator mutation.
- Produces: native auto-merge state on eligible PRs; update-branch requests for remaining eligible same-repo PRs after `main` changes.

- [x] **Step 1: Add exact event and permission surface**

Use workflow name `QS3D Hybrid PR Coordinator`; triggers are only `pull_request` with actions `opened`, `reopened`, `ready_for_review`, `converted_to_draft`, `synchronize`, `labeled`, `unlabeled`, plus `push` limited to `main`. Set `permissions` to `contents: read`, `actions: read`, `pull-requests: write`. Set one constant concurrency group and `cancel-in-progress: false`.

- [x] **Step 2: Implement the PR auto-merge reconciliation job**

For `pull_request`, require non-empty `QS3D_AUTOMERGE_TOKEN` and expose it as `GH_TOKEN`; fail with exit 1 if it is missing. Re-fetch `/repos/${GITHUB_REPOSITORY}/pulls/${PR_NUMBER}` and require `state=open`, `base.ref=main`, `head.repo.full_name=${GITHUB_REPOSITORY}`, and API head SHA equal to event head SHA before mutation. For an eligible non-draft, non-Dependabot PR without `no-automerge`, call only `enablePullRequestAutoMerge` with `mergeMethod: MERGE`. If a previously armed PR becomes draft or receives `no-automerge`, call `disablePullRequestAutoMerge`. Treat an already-correct auto-merge state as success; boundary/stale-event mismatches exit successfully without mutation.

- [x] **Step 3: Implement the refresh job**

For `push` to `main`, require non-empty `QS3D_AUTOMERGE_TOKEN` and expose it as `GH_TOKEN`; fail with exit 1 if it is missing. Enumerate open PRs with base `main`. Re-fetch each candidate immediately before mutation, skip draft/fork/Dependabot/opt-out/dirty PRs, arm native auto-merge when needed, capture the current head SHA, then call `PUT /repos/${GITHUB_REPOSITORY}/pulls/${number}/update-branch` with `expected_head_sha` equal to that captured SHA. Treat conflict/unprocessable responses as safe per-PR skips; unexpected refresh failures fail the job. Never force/reset.

- [x] **Step 4: Run GREEN validation for the focused guard**

The focused guard now requires PAT-backed, fail-closed mutation paths and disarm behavior. Final aggregate validation remains tied to the exact merge candidate.

---

### Task 3: Narrowly authorize the coordinator in executable governance

**Files:**
- Modify: `scripts/preflight-repository-professionalism.py`
- Modify: `scripts/preflight-ci-manual-only.py`

**Interfaces:**
- Consumes: exact coordinator filename and workflow text.
- Produces: repository-wide rejection of autonomous merge primitives everywhere except the exact native-auto-merge markers in the named coordinator; automatic-trigger whitelist extended to this one workflow only.

- [x] **Step 1: Update professionalism scanner with exact exception**

Keep `pull_request_target`, `gh pr merge`, and direct REST merge endpoint rejection global. Permit the native auto-merge GraphQL primitives only when `workflow.name == "hybrid-pr-coordinator.yml"`; reject them in every other workflow. Add required coordinator safety tokens so the exception cannot become a generic waiver.

- [x] **Step 2: Update automatic-workflow policy scanner**

Add `HYBRID_COORDINATOR = "hybrid-pr-coordinator.yml"` to the required owner-approved workflows. Give only this filename a special validation branch requiring exactly `pull_request` + `push`, exact PR action markers, `main` push branch, read/read/write permissions, serialized concurrency, PAT-backed native auto-merge, update-branch optimistic locking, and no `github.token` mutation fallback. Reject release/workflow-dispatch/direct-main/force primitives.

- [ ] **Step 3: Run aggregate source validation on the final exact head**

Require `scripts/preflight-repository-professionalism.py`, `scripts/preflight-ci-manual-only.py`, `scripts/preflight-hybrid-pr-coordinator.py`, and aggregate `scripts/preflight-all.py` to pass in current exact-head CI.

---

### Task 4: Align repository policy documentation

**Files:**
- Modify: `CI_POLICY.md`
- Modify: `docs/MAIN-WRITE-AUTHORIZATION.md`
- Modify: `docs/AGENT-WORK-REGISTRATION.md`
- Modify: design/plan docs when runtime evidence changes an implementation assumption.

**Interfaces:**
- Consumes: implemented coordinator contract and verified runtime behavior.
- Produces: prose governance that matches executable behavior without granting general agents bulk-merge authority.

- [x] **Step 1: Document the hybrid coordinator in CI policy**

State that repository-wide blind direct auto-merge remains disabled, while the exact named coordinator may arm GitHub native auto-merge for eligible same-repo PRs and refresh branches after `main` landings. Preserve `preflight/core`, strict freshness, no release authority, and `no-automerge` semantics.

- [x] **Step 2: Document owner authorization boundary**

In `MAIN-WRITE-AUTHORIZATION.md`, record the owner's explicit exception for the named coordinator only. Clarify that ordinary agents still may not sweep unrelated PRs and that GitHub protection, not the workflow, performs final merge authorization.

- [x] **Step 3: Document coordinator carrier semantics**

In `AGENT-WORK-REGISTRATION.md`, add the named coordinator as a persistent owner-approved integration mechanism without weakening one-lane/one-carrier rules for ordinary work.

- [x] **Step 4: Align design/plan with runtime PAT requirement**

Record that `GITHUB_TOKEN` returned `Resource not accessible by integration` for `enablePullRequestAutoMerge`, so both coordinator jobs use `QS3D_AUTOMERGE_TOKEN` and missing credentials fail closed.

- [ ] **Step 5: Re-run all focused policy guards on the final exact head**

Expected: professionalism, CI policy, hybrid coordinator guard, and aggregate preflight all pass.

---

### Task 5: Open protected PR and remediate exact-head CI

**Files:**
- No new source file required; PR metadata only.

**Interfaces:**
- Consumes: completed branch head.
- Produces: canonical PR for issue #5337 targeting `main`.

- [x] **Step 1: Open the canonical PR**

PR #5345 is the canonical carrier for Lane-Key `issue-5337`, superseding the closed #5323/#5322 attempt.

- [ ] **Step 2: Observe final exact-head branch/PR CI**

Require current-head `preflight` and `core` success. Diagnose any failure from job logs and fix only the same lane; do not weaken checks.

- [ ] **Step 3: Reconcile with current `main` if strict freshness requires it**

Refresh the same canonical branch non-destructively and re-run current-candidate CI. Do not replace the PR merely because `main` moved.

---

### Task 6: Merge and verify activation

**Files:**
- No source changes unless a verification defect is found.

**Interfaces:**
- Consumes: fresh mergeable PR with protected checks green.
- Produces: coordinator present on current `main`, issue/reservation completed.

- [ ] **Step 1: Merge the same task PR with expected-head guard**

Use the protected PR merge path and expected current head SHA. Do not bypass rulesets.

- [ ] **Step 2: Verify resulting `main`**

Fetch current `main`; confirm it contains the task PR merge and `.github/workflows/hybrid-pr-coordinator.yml`.

- [ ] **Step 3: Verify repository prerequisites**

Confirm `allow_auto_merge=true` and `allow_update_branch=true`. `QS3D_AUTOMERGE_TOKEN` cannot be read or created through the available GitHub connector, so operational verification must treat a missing-secret coordinator run as an external configuration blocker rather than a successful no-op.

- [ ] **Step 4: Verify one live coordinator mutation**

After the PAT exists, require a live coordinator run to perform PAT-backed auto-merge state reconciliation or branch refresh successfully. Do not call the automation fully operational merely because the source/preflight is green.

- [ ] **Step 5: Complete reservation**

Close #5337 as completed after merge verification and release/delete the task branch when practical.

## Self-Review

- Spec coverage: all design requirements map to Tasks 1–6.
- Placeholder scan: no TBD/TODO/future-fill markers remain.
- Type/interface consistency: workflow filename, secret name, opt-out label, trigger actions, PAT requirement, and guard names are consistent across all tasks.
