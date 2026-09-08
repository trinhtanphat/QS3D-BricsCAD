# Required Status Bridge Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Make protected-main ruleset contexts `preflight` and `core` consume the already-verified exact-head GitHub Actions results instead of remaining permanently `expected`.

**Architecture:** Add one isolated pull-request workflow. It waits for the current PR number's exact-head `preflight` and `core` check-runs, mirrors only their terminal result into legacy commit-status contexts, and fails closed on missing, non-success, or timeout conditions. The existing `.github/workflows/ci.yml` and the protected-main ruleset remain unchanged.

**Tech Stack:** GitHub Actions YAML, Python 3 standard library, GitHub Checks/Statuses REST APIs.

**Spec:** GitHub Issue #6113.

## Global Constraints

- Reservation-Protocol: v2.
- Do not mutate `.github/workflows/ci.yml`; Issue #6069 owns that path.
- Do not weaken `protectedMain`, `preflight`, or `core` semantics.
- Mirror only checks tied to the same PR number and exact head SHA.
- Same-repository pull requests only; fork write escalation is forbidden.
- Missing, pending beyond timeout, cancelled, skipped, or failed required checks must not publish success.

---

### Task 1: Add fail-closed required-status bridge

**Files:**
- Create: `.github/workflows/required-status-bridge.yml`
- Test: protected pull-request workflow run for the exact bridge head

**Interfaces:**
- Consumes: GitHub check-runs named `preflight` and `core`, current PR number, current PR head SHA.
- Produces: legacy commit-status contexts `preflight` and `core` on the same exact head SHA.

- [ ] **Step 1: Confirm the RED condition**

Run through GitHub REST for the current blocked PR head:

```text
GET /repos/trinhtanphat/QS3D-BricsCAD/commits/<head>/check-runs
GET /repos/trinhtanphat/QS3D-BricsCAD/commits/<head>/status
```

Expected before the fix: `preflight` and `core` check-runs are SUCCESS while combined commit status has `statuses=[]`, and merge returns `2 of 2 required status checks are expected`.

- [ ] **Step 2: Implement the minimal bridge workflow**

Create a same-repository `pull_request` workflow with `checks: read` and `statuses: write`. Poll exact-head check-runs, filter each context to the current PR number, and publish `pending` followed by terminal `success`/`failure` to the matching legacy context.

- [ ] **Step 3: Verify fail-closed behavior in source review**

Confirm the workflow:

```text
never uses pull_request_target
never publishes success for missing/in-progress/non-success checks
never accepts a check-run from a different PR that reuses the same head SHA
never mutates the protected-main ruleset
```

- [ ] **Step 4: Verify GREEN on the bridge PR**

Expected:

```text
Shared CI exact-head preflight = SUCCESS
Shared CI exact-head core = SUCCESS
legacy status preflight = success
legacy status core = success
protected-main merge no longer reports required checks as expected
```

- [ ] **Step 5: Merge and revalidate blocked PRs**

After the bridge PR merges, update #6102 and #6112 to include current `main`, rerun their exact-head required checks, confirm mirrored commit statuses are success, then merge each only while current and GREEN.
