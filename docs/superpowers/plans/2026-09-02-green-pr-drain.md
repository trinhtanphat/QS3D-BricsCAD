# Green PR Drain Automation Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add a fail-closed, owner-authorized repository-wide GitHub Actions coordinator that merges eligible exact-head green same-repository PRs into `main` only after proving they contain current `main`, then refreshes remaining same-repository PR branches against the new base.

**Architecture:** A `workflow_run` workflow listens only to successful pull-request runs of `QS3D Shared Branch and Integration CI`. It re-fetches the triggering PR, preserves draft/Dependabot/`no-automerge`/fork boundaries, verifies exact-head/state/base/source invariants, proves current-main ancestry with the compare API, merges with optimistic locking, and then best-effort updates all other eligible same-repository PR branches using a dedicated fine-grained PAT so synchronize CI is triggered normally. Existing CI governance is extended narrowly so only this named coordinator gains the third automatic-workflow slot and cross-lane merge authority.

**Tech Stack:** GitHub Actions YAML, GitHub REST API through `gh api`, Bash, `jq` on `ubuntu-latest`, Python source-policy preflights.

**Spec:** `docs/superpowers/specs/2026-09-02-green-pr-drain-design.md`

## Global Constraints

- Never bypass required checks or repository rulesets.
- Never merge a stale head SHA.
- Never merge a head that does not contain current `main`.
- Never operate on draft PRs, fork PRs, PRs whose base is not `main`, Dependabot-authored PRs, or PRs labeled `no-automerge`.
- Never force-push/reset/rewrite another PR branch.
- `QS3D_AUTOMERGE_TOKEN` is mandatory for mutations; no `GITHUB_TOKEN` mutation fallback.
- Fine-grained PAT scope: only `trinhtanphat/QS3D-BricsCAD`; Contents: Read and write; Pull requests: Read and write.
- One fixed non-cancelling concurrency group serializes all drain runs.
- Cross-lane merge authority belongs only to `.github/workflows/green-pr-drain.yml`; normal agents retain canonical-carrier ownership boundaries.

---

### Task 1: Add the serialized green-PR drain workflow

**Files:**
- Create: `.github/workflows/green-pr-drain.yml`

- [x] **Step 1: Add workflow trigger and least-privilege job context**

Use `workflow_run.types: [completed]`, `concurrency.group: qs3d-green-pr-drain`, `cancel-in-progress: false`, and one job guarded by successful pull-request CI completion. Keep workflow-level permissions read-only because all mutation API calls authenticate explicitly with `QS3D_AUTOMERGE_TOKEN`.

- [x] **Step 2: Add credential and event admission gates**

Require non-empty `QS3D_AUTOMERGE_TOKEN`; consume `github.event.workflow_run.event`, `.conclusion`, `.head_sha`, and `.pull_requests`; admit only `pull_request/success` with exactly one associated PR.

- [x] **Step 3: Re-fetch and validate the triggering PR**

Require:

```text
state == open
draft == false
user.login != dependabot[bot]
labels excludes no-automerge
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

- [x] **Step 4: Merge with exact-head optimistic locking**

Call:

```text
PUT /repos/{owner}/{repo}/pulls/{number}/merge
merge_method=merge
sha=<workflow_run.head_sha>
```

Require `merged=true`. A rejected merge is a hard failure; do not refresh other PRs because this run did not advance `main`.

- [x] **Step 5: Refresh remaining same-repository PR branches**

List open PRs targeting `main`. For each PR other than the merged PR, require same-repository head and open state, then call:

```text
PUT /repos/{owner}/{repo}/pulls/{number}/update-branch
expected_head_sha=<current head.sha>
```

Handle each refresh independently. Conflicts/no-applicable-update conditions are warnings and never trigger a force update.

- [x] **Step 6: Emit a concise job summary**

Record merged PR number/SHA plus refreshed, skipped, and warning PR numbers in `$GITHUB_STEP_SUMMARY` without printing token material.

---

### Task 2: Register the coordinator in fail-closed repository governance

**Files:**
- Create: `scripts/preflight-green-pr-drain.py`
- Modify: `scripts/preflight-ci-manual-only.py`
- Modify: `CI_POLICY.md`
- Modify: `docs/MAIN-WRITE-AUTHORIZATION.md`
- Modify: `docs/AGENT-WORK-REGISTRATION.md`

- [x] **Step 1: Capture RED evidence from existing policy**

Initial carrier exact-head CI run `33616889360` failed at `CI trigger and publishing policy gate` because existing governance allowed automatic triggers only for `ci.yml` and `dispatch-v25-cloud-after-main-integration.yml`. This proves the policy registration change is required; do not bypass the gate.

- [x] **Step 2: Add a focused source guard**

Add auto-discovered `scripts/preflight-green-pr-drain.py` that pins the exact workflow trigger, successful-PR guard, read-only workflow permissions, dedicated token, exact-head/current-main/same-repository checks, Dependabot/manual boundary, `no-automerge` opt-out, optimistic merge SHA, non-force branch refresh, and aligned governance documentation.

- [x] **Step 3: Register exactly one new automatic workflow**

Extend `scripts/preflight-ci-manual-only.py` with `GREEN_PR_DRAIN = "green-pr-drain.yml"`. Require exactly `workflow_run`, the named shared CI source, completed type, one `merge-and-refresh` job, and the exact success+pull-request job guard. Preserve manual-only enforcement for all other unapproved workflows.

- [x] **Step 4: Record owner authorization and boundaries**

Update `CI_POLICY.md`, `docs/MAIN-WRITE-AUTHORIZATION.md`, and `docs/AGENT-WORK-REGISTRATION.md` so the cross-lane authorization belongs only to the named coordinator. Preserve normal-agent ownership restrictions, protected-main rules, Dependabot manual merge, `no-automerge` opt-out, fork exclusion, and no-force semantics.

---

### Task 3: Validate, bootstrap, and activate

- [ ] **Step 1: Obtain fresh exact-head repository CI**

The carrier PR #5323 must produce fresh `preflight` and `core` success on its current exact head after the governance changes. Diagnose any red gate on the same canonical carrier; never weaken a guard merely to make it green.

- [ ] **Step 2: Reconcile current `main` freshness if needed**

Before bootstrap merge, prove the carrier head contains the then-current `main`. If `main` advanced, update/reconcile the same canonical branch without force/history rewrite and obtain new exact-head CI.

- [ ] **Step 3: Bootstrap through protected PR merge**

Merge PR #5323 through the normal protected PR endpoint with its exact current head SHA. Confirm `.github/workflows/green-pr-drain.yml` exists on default `main` and verify the resulting main SHA.

- [ ] **Step 4: Configure the dedicated repository secret**

Create a fine-grained PAT restricted to `trinhtanphat/QS3D-BricsCAD` with only Contents: Read and write and Pull requests: Read and write, then save it as Actions repository secret `QS3D_AUTOMERGE_TOKEN`. Never paste the token into issue/PR logs or chat.

- [ ] **Step 5: Verify the first live drain cycle**

After the workflow exists on `main` and the secret is configured, the next successful exact-head eligible PR CI run should invoke `QS3D Green PR Drain`. Verify that it either merges the eligible exact head and refreshes other same-repository PRs, or emits a documented safety skip for an intentionally ineligible PR. GitHub branch protection remains authoritative throughout.
