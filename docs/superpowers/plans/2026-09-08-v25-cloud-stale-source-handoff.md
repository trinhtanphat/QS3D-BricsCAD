# V25 Cloud Stale-Source Handoff Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Make a V25 cloud release that becomes stale before its first persistent release mutation complete as an auditable successful no-op, then let the canonical dispatcher safely hand the same preview ordinal to current protected `main` and dispatch a fresh exact-source release.

**Architecture:** Keep stale-source classification in `release-v25-cloud.yml`, but replace only the release-relevant-drift throw with a successful superseded terminal path before draft-release creation. Keep all retry authority in `dispatch-v25-cloud-after-main-integration.yml`; it derives the effective append-only reservation owner from complete reservation/fence pairs and permits an owner handoff only when a successful canonical upstream release run proves the old owner, ancestry to current protected `main` is valid, and the normal tag/published-release gates still admit dispatch. Any malformed, dangling, mismatched, noncanonical, or non-ancestor state remains fail-closed.

**Tech Stack:** GitHub Actions YAML, PowerShell 7, GitHub REST/CLI calls already used by the workflows, Python 3.12 source/preflight guards.

**Spec:** GitHub Issue #6149 (`V25 cloud release: reconcile stale source without stale publication`).

## Global Constraints

- Never publish a V25 release built from a source SHA when protected `main` has release-relevant changes after that SHA.
- The release workflow must not directly dispatch its own retry.
- Retry authority stays in the canonical post-integration dispatcher and its append-only Issue #1085 reservation ledger.
- Handoff requires authenticated canonical `workflow_run` provenance and descendant protected-main ancestry; ambiguity fails closed.
- Existing tag/version/published-release checks remain authoritative.
- Licensed BricsCAD V25/V26 runtime evidence is not fabricated or promoted by this cloud workflow.

---

### Task 1: Lock the stale-source recovery contract

**Files:**
- Create: `scripts/preflight-v25-cloud-release-stale-source-handoff.py`
- Modify: none

**Interfaces:**
- Consumes: `.github/workflows/release-v25-cloud.yml`, `.github/workflows/dispatch-v25-cloud-after-main-integration.yml`
- Produces: an auto-discovered source guard that fails until the recovery contract exists.

- [x] **Step 1: Write the failing source guard**

Require an auditable `V25_RELEASE_SUPERSEDED` marker before the draft-release body, require the legacy stale release-relevant throw to be absent, and require dispatcher tokens for upstream run/head provenance, effective-owner tracking, default-deny handoff state, canonical Actions-run verification, and handoff admission before durable dispatch mutation.

- [ ] **Step 2: Run the guard and verify RED**

Run through Shared CI's `python scripts/preflight-all.py` on the canonical `agent/**` PR carrier. Expected failure: `preflight-v25-cloud-release-stale-source-handoff.py` reports missing production contract tokens, not branch-admission or lane-reservation failure.

### Task 2: Make stale release completion a safe no-op

**Files:**
- Modify: `.github/workflows/release-v25-cloud.yml`
- Test: `scripts/preflight-v25-cloud-release-stale-source-handoff.py`

**Interfaces:**
- Consumes: existing `SOURCE_SHA`, `RELEASE_TAG`, protected-main API/fetch identity and release-relevant path classification.
- Produces: successful workflow completion only when release-relevant drift is proven before the first persistent release mutation.

- [ ] **Step 1: Replace only the release-relevant drift throw**

When `git diff --quiet` returns `1`, emit `V25_RELEASE_SUPERSEDED source_sha=<old> current_main=<new> release_tag=<tag> run_id=<run>` plus a GitHub Actions notice and terminate the PowerShell publish step successfully before `$body = @{` can be reached. Preserve non-ancestor, API/fetch mismatch, git-diff error, and later protected-main race failures as hard failures.

- [ ] **Step 2: Verify ordering and static contract**

Run `python scripts/preflight-v25-cloud-release-stale-source-handoff.py`. Expected at this intermediate point: release-side assertions pass; dispatcher-side assertions still fail.

### Task 3: Authenticate append-only dispatcher ownership handoff

**Files:**
- Modify: `.github/workflows/dispatch-v25-cloud-after-main-integration.yml`
- Test: `scripts/preflight-v25-cloud-release-stale-source-handoff.py`

**Interfaces:**
- Consumes: `github.event.workflow_run.id`, `github.event.workflow_run.head_sha`, existing Issue #1085 reservation/fence comments, current protected-main SHA, current tag/version gates.
- Produces: a new reservation/fence pair for current protected main only after old effective owner provenance is proven.

- [ ] **Step 1: Bind upstream workflow-run identity**

Expose `UPSTREAM_RELEASE_RUN_ID` and `UPSTREAM_RELEASE_HEAD_SHA` to the dispatcher job. For non-`workflow_run` events these values are empty/default and cannot authorize handoff.

- [ ] **Step 2: Derive the effective owner from complete append-only pairs**

Parse bot-authored reservation and dispatch-fence markers in ledger order. Track the latest complete coherent pair as `latest_reservation_source` / `latest_dispatch_fence_source`; reject malformed duplicate/conflicting/dangling ownership rather than treating historical handoff rows as a permanent conflict.

- [ ] **Step 3: Admit only canonical stale-run handoff**

Initialize `handoff_rebind=0`. If current source differs from the effective owner, require `workflow_run`, require the upstream head SHA to equal that owner, fetch `actions/runs/${UPSTREAM_RELEASE_RUN_ID}`, require canonical workflow path `.github/workflows/release-v25-cloud.yml`, successful completed `workflow_dispatch` provenance on `main`, and require old owner to be an ancestor of current protected main. Log `V25 preview reservation handoff admitted` and set `handoff_rebind=1` only after all checks pass.

- [ ] **Step 4: Append new owner/fence and dispatch normally**

Reuse the existing immediate protected-main rebind/version/tag checks before the first durable reservation/fence side effect. Append a new reservation for the current source when handoff is admitted, append its dispatch fence, then invoke the canonical release workflow with exact current `source_sha`. Do not mutate/delete historical ledger comments.

- [ ] **Step 5: Run source guards**

Run `python scripts/preflight-v25-cloud-release-stale-source-handoff.py` and aggregate `python scripts/preflight-all.py`. Expected: both PASS.

### Task 4: Exact-head CI, review, and merge

**Files:**
- Modify: none unless review/CI finds a concrete defect.

**Interfaces:**
- Consumes: PR exact head, protected `preflight` and `core` contexts, mergeability/current-main state.
- Produces: merged protected-main fix only from a verified current candidate.

- [ ] **Step 1: Inspect exact PR diff**

Confirm the carrier changes only the four Reservation-v2 `Expected-Paths` and that no release/package/runtime invariant was weakened outside the intended stale no-op/handoff path.

- [ ] **Step 2: Obtain exact-head Shared CI**

Require authoritative PR `preflight` and `core` GREEN for the exact candidate SHA. If protected `main` moves, reconcile before merge and require fresh exact-head evidence.

- [ ] **Step 3: Mark ready and re-check mergeability/currentness**

Do not treat draft or stale checks as merge evidence. Confirm the PR is ready, mergeable, and based on current protected main according to repository policy.

- [ ] **Step 4: Merge with expected head SHA**

Merge only with the verified expected PR head SHA. After merge, verify the resulting protected-main SHA and leave licensed runtime status unchanged unless real licensed-host evidence exists.

## Self-Review

- Spec coverage: stale publication is still blocked; recovery is dispatcher-owned; provenance, ancestry, append-only ledger behavior, idempotency, and exact-head merge verification each have an explicit task.
- Placeholder scan: no deferred implementation placeholders are present.
- Interface consistency: release produces a successful canonical `workflow_run`; dispatcher consumes that run ID/head SHA and emits a new reservation/fence pair before dispatching the fresh release.
