# Green PR Drain Automation Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add a fail-closed GitHub Actions integration lane that merges exact-head green PRs into `main` only after proving they contain current `main`, then refreshes remaining same-repository PR branches against the new base.

**Architecture:** A `workflow_run` workflow listens only to successful pull-request runs of `QS3D Shared Branch and Integration CI`. It re-fetches the triggering PR, verifies exact-head/state/base/source invariants, proves current-main ancestry with the compare API, merges with optimistic locking, and then best-effort updates all other eligible same-repository PR branches using a dedicated fine-grained PAT so synchronize CI is triggered normally.

**Tech Stack:** GitHub Actions YAML, GitHub REST API through `gh api`, Bash, `jq` on `ubuntu-latest`.

**Spec:** `docs/superpowers/specs/2026-09-02-green-pr-drain-design.md`

## Global Constraints

- Never bypass required checks or repository rulesets.
- Never merge a stale head SHA.
- Never merge a head that does not contain current `main`.
- Never operate on draft PRs, fork PRs, or PRs whose base is not `main`.
- Never force-push/reset/rewrite another PR branch.
- `QS3D_AUTOMERGE_TOKEN` is mandatory for mutations; no `GITHUB_TOKEN` mutation fallback.
- Fine-grained PAT scope: only `trinhtanphat/QS3D-BricsCAD`; Contents: Read and write; Pull requests: Read and write.
- One fixed non-cancelling concurrency group serializes all drain runs.

---

### Task 1: Add the serialized green-PR drain workflow

**Files:**
- Create: `.github/workflows/green-pr-drain.yml`

**Interfaces:**
- Consumes: completed `workflow_run` payloads from `QS3D Shared Branch and Integration CI`; secret `QS3D_AUTOMERGE_TOKEN`.
- Produces: guarded merge into `main`; best-effort `update-branch` calls for remaining open same-repository PRs.

- [ ] **Step 1: Add workflow trigger and least-privilege job context**

Create a workflow with `workflow_run.types: [completed]`, `concurrency.group: qs3d-green-pr-drain`, `cancel-in-progress: false`, and one job guarded by successful pull-request CI completion. Keep the workflow-level token read-only because all mutation API calls authenticate explicitly with `QS3D_AUTOMERGE_TOKEN`.

- [ ] **Step 2: Add credential and event admission gates**

In Bash, require non-empty `QS3D_AUTOMERGE_TOKEN`; read `github.event.workflow_run.event`, `.conclusion`, `.head_sha`, and `.pull_requests`; reject anything except `pull_request/success` with exactly one associated PR.

- [ ] **Step 3: Re-fetch and validate the triggering PR**

Fetch `/repos/$GITHUB_REPOSITORY/pulls/$PR_NUMBER` and require:

```text
state == open
draft == false
base.ref == main
head.repo.full_name == GITHUB_REPOSITORY
head.sha == workflow_run.head_sha
```

Then fetch current `main` and compare `main_sha...head_sha`; require:

```text
merge_base_commit.sha == main_sha
status in {ahead, identical}
```

Any mismatch exits successfully as an ineligible/stale/outdated run.

- [ ] **Step 4: Merge with exact-head optimistic locking**

Call:

```text
PUT /repos/{owner}/{repo}/pulls/{number}/merge
merge_method=merge
sha=<workflow_run.head_sha>
```

Require the API response to report `merged=true`. A rejected merge is a hard failure; do not refresh other PRs because this run did not advance `main`.

- [ ] **Step 5: Refresh remaining eligible PR branches**

List open PRs targeting `main`. For each PR other than the merged PR, require same-repository head and open state, then call:

```text
PUT /repos/{owner}/{repo}/pulls/{number}/update-branch
expected_head_sha=<current head.sha>
```

Handle each refresh independently. `422` conflict/no-applicable-update conditions are warnings and never trigger a force update.

- [ ] **Step 6: Emit a concise job summary**

Record merged PR number/SHA plus refreshed and skipped PR numbers in `$GITHUB_STEP_SUMMARY` without printing token material.

- [ ] **Step 7: Validate through repository CI**

Open the carrier PR from the reservation branch to `main`. Expected checks: repository preflight and core required contexts run on the exact carrier head. The new `workflow_run` automation itself cannot orchestrate until this carrier is present on default `main`, so this one PR is bootstrapped through existing rules.

- [ ] **Step 8: Bootstrap and verify**

After the carrier PR is green, merge it through existing rules with its exact head SHA. Confirm `.github/workflows/green-pr-drain.yml` exists on `main`. Once `QS3D_AUTOMERGE_TOKEN` is configured, the next successful exact-head PR CI run should invoke the drain workflow; verify it either merges the eligible PR or logs a safety skip for an intentionally ineligible PR.
