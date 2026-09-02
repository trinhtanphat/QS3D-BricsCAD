# Hybrid Native Auto-Merge Coordinator Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add one fail-closed repository coordinator that arms GitHub native auto-merge for eligible same-repository PRs and refreshes remaining PR branches after every `main` landing without direct merge or force-update primitives.

**Architecture:** `.github/workflows/hybrid-pr-coordinator.yml` owns two event paths: PR lifecycle events arm native auto-merge before required checks finish, and `push` to `main` refreshes other eligible PR heads through GitHub's `update-branch` API. Existing repository policy scanners gain an exact-path exception for this one coordinator while a new aggregate-discovered preflight guard locks the required safety markers.

**Tech Stack:** GitHub Actions YAML, GitHub GraphQL/REST through `gh api`, Python 3.12 repository preflight scripts, GitHub protected-main rules.

**Spec:** `docs/superpowers/specs/2026-09-02-hybrid-native-automerge-design.md`

## Global Constraints

- `main` remains PR-only; never write/update/force `main` directly.
- Final merge authority remains GitHub protected-main rules with strict `preflight` and `core` checks.
- Only `.github/workflows/hybrid-pr-coordinator.yml` may receive the new autonomous coordinator exception.
- Direct REST PR merge calls, `gh pr merge`, `pull_request_target`, force/reset, and release dispatch remain forbidden.
- Fork PRs, drafts, conflicts, and `no-automerge` PRs are never mutated by the coordinator.
- Refresh mutations must use `QS3D_AUTOMERGE_TOKEN`; never fall back to `GITHUB_TOKEN` for update-branch mutation.
- Repository settings must remain `allow_auto_merge=true` and `allow_update_branch=true`.

---

### Task 1: Add the failing coordinator policy guard

**Files:**
- Create: `scripts/preflight-hybrid-pr-coordinator.py`

**Interfaces:**
- Consumes: repository root and `.github/workflows/hybrid-pr-coordinator.yml`.
- Produces: exit code 0 only when the coordinator satisfies the complete static safety contract; nonzero otherwise. `scripts/preflight-all.py` auto-discovers the file by `preflight-*.py` naming.

- [ ] **Step 1: Write the failing guard**

Implement a Python script that reads the coordinator as strict UTF-8 and fails if it is absent. Require literal/regex contracts for the exact workflow name, `pull_request` and `push` triggers, PR actions `opened`, `reopened`, `ready_for_review`, `synchronize`, `unlabeled`, `main` push scope, repository-wide serialized concurrency, native `enablePullRequestAutoMerge`, current-head API re-fetch, same-repository/base-main/draft/`no-automerge` filters, `update-branch` with current head SHA, and `QS3D_AUTOMERGE_TOKEN`. Reject `pull_request_target`, `gh pr merge`, `/pulls/.../merge`, `git push`, `git reset`, `--force`, and refresh mutation using `github.token`.

- [ ] **Step 2: Run RED validation**

Run through branch CI/aggregate preflight. Expected: `scripts/preflight-hybrid-pr-coordinator.py` fails because `.github/workflows/hybrid-pr-coordinator.yml` does not exist.

- [ ] **Step 3: Record the RED evidence**

Bind the failure to the exact branch head SHA and confirm the failure is specifically the missing coordinator, not a parser/runtime error.

---

### Task 2: Implement the minimal coordinator workflow

**Files:**
- Create: `.github/workflows/hybrid-pr-coordinator.yml`

**Interfaces:**
- Consumes: PR lifecycle event JSON, `github.token` for native auto-merge arming, `secrets.QS3D_AUTOMERGE_TOKEN` for branch refresh.
- Produces: native auto-merge state on eligible PRs; update-branch requests for remaining eligible same-repo PRs after `main` changes.

- [ ] **Step 1: Add exact event and permission surface**

Use workflow name `QS3D Hybrid PR Coordinator`; triggers are only `pull_request` with actions `opened`, `reopened`, `ready_for_review`, `synchronize`, `unlabeled`, plus `push` limited to `main`. Set `permissions` to `contents: read`, `actions: read`, `pull-requests: write`. Set one constant concurrency group and `cancel-in-progress: false`.

- [ ] **Step 2: Implement the arm job**

For `pull_request`, use `GH_TOKEN=${{ github.token }}`. Re-fetch `/repos/${GITHUB_REPOSITORY}/pulls/${PR_NUMBER}` and require `state=open`, `draft=false`, `base.ref=main`, `head.repo.full_name=${GITHUB_REPOSITORY}`, no `no-automerge` label, and API head SHA equal to event head SHA. Resolve the PR GraphQL node ID and call only `enablePullRequestAutoMerge` with `mergeMethod: MERGE`. Treat an already-enabled state as success; any eligibility mismatch exits successfully without mutation.

- [ ] **Step 3: Implement the refresh job**

For `push` to `main`, require non-empty `QS3D_AUTOMERGE_TOKEN` and expose it as `GH_TOKEN`. Enumerate open PRs with base `main`. Re-fetch each candidate immediately before mutation, skip draft/fork/opt-out/dirty PRs, capture the current head SHA, then call `PUT /repos/${GITHUB_REPOSITORY}/pulls/${number}/update-branch` with `expected_head_sha` equal to that captured SHA. Treat 202/200 as refresh accepted; treat conflict/unprocessable responses as skip/fail-closed per PR without force/reset.

- [ ] **Step 4: Run GREEN validation for the focused guard**

Run aggregate preflight on the new head. Expected: `scripts/preflight-hybrid-pr-coordinator.py` advances past the missing-workflow failure; remaining failures, if any, should come only from existing policy scanners that have not yet been updated.

---

### Task 3: Narrowly authorize the coordinator in executable governance

**Files:**
- Modify: `scripts/preflight-repository-professionalism.py`
- Modify: `scripts/preflight-ci-manual-only.py`

**Interfaces:**
- Consumes: exact coordinator filename and workflow text.
- Produces: repository-wide rejection of autonomous merge primitives everywhere except the exact native-auto-merge marker in the named coordinator; automatic-trigger whitelist extended to this one workflow only.

- [ ] **Step 1: Update professionalism scanner with exact exception**

Keep `pull_request_target`, `gh pr merge`, and direct REST merge endpoint rejection global. Permit the lowercase token `enablepullrequestautomerge` only when `workflow.name == "hybrid-pr-coordinator.yml"`; reject it in every other workflow. Add required coordinator safety tokens to ensure the exception cannot become a generic waiver.

- [ ] **Step 2: Update automatic-workflow policy scanner**

Add `HYBRID_COORDINATOR = "hybrid-pr-coordinator.yml"` to the required owner-approved workflows. Give only this filename a special validation branch requiring exactly `pull_request` + `push`, exact PR action markers, `main` push branch, read/read/write permissions, serialized concurrency, `enablePullRequestAutoMerge`, `QS3D_AUTOMERGE_TOKEN`, and update-branch optimistic locking. Reject release/workflow-dispatch/direct-main/force primitives.

- [ ] **Step 3: Run aggregate source validation**

Run `python scripts/preflight-repository-professionalism.py`, `python scripts/preflight-ci-manual-only.py`, `python scripts/preflight-hybrid-pr-coordinator.py`, then `python scripts/preflight-all.py`. Expected: all pass on the exact head.

---

### Task 4: Align repository policy documentation

**Files:**
- Modify: `CI_POLICY.md`
- Modify: `docs/MAIN-WRITE-AUTHORIZATION.md`
- Modify: `docs/AGENT-WORK-REGISTRATION.md`

**Interfaces:**
- Consumes: implemented coordinator contract.
- Produces: prose governance that matches executable behavior without granting general agents bulk-merge authority.

- [ ] **Step 1: Document the hybrid coordinator in CI policy**

State that repository-wide blind direct auto-merge remains disabled, while the exact named coordinator may arm GitHub native auto-merge for eligible same-repo PRs and refresh branches after `main` landings. Preserve `preflight/core`, strict freshness, no release authority, and `no-automerge` semantics.

- [ ] **Step 2: Document owner authorization boundary**

In `MAIN-WRITE-AUTHORIZATION.md`, record the owner's explicit exception for the named coordinator only. Clarify that ordinary agents still may not sweep unrelated PRs and that GitHub protection, not the workflow, performs final merge authorization.

- [ ] **Step 3: Document coordinator carrier semantics**

In `AGENT-WORK-REGISTRATION.md`, add the named coordinator as a persistent owner-approved integration mechanism without weakening one-lane/one-carrier rules for ordinary work.

- [ ] **Step 4: Re-run all focused policy guards**

Expected: professionalism, CI policy, hybrid coordinator guard, and aggregate preflight all pass.

---

### Task 5: Open protected PR and remediate exact-head CI

**Files:**
- No new source file required; PR metadata only.

**Interfaces:**
- Consumes: completed branch head.
- Produces: canonical PR for issue #5337 targeting `main`.

- [ ] **Step 1: Open the canonical PR**

Include Lane-Key `issue-5337`, baseline SHA, canonical branch, `Supersedes #5323`, safety summary, validation evidence, and `Runtime: NOT_APPLICABLE`.

- [ ] **Step 2: Observe exact-head branch/PR CI**

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

Confirm `allow_auto_merge=true` and `allow_update_branch=true`. Report `QS3D_AUTOMERGE_TOKEN` as the only external configuration blocker if its presence cannot be verified through available GitHub tooling.

- [ ] **Step 4: Complete reservation**

Close #5337 as completed after merge verification and remove/delete the task branch when practical.

## Self-Review

- Spec coverage: all design requirements map to Tasks 1–6.
- Placeholder scan: no TBD/TODO/future-fill markers remain.
- Type/interface consistency: workflow filename, secret name, opt-out label, trigger actions, and guard names are consistent across all tasks.
