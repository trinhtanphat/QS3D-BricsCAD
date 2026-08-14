# GitHub Actions / CI Policy

This file is the repository-level source of truth for CI ownership, multi-agent integration and final exact-SHA evidence.

**Owner policy — 2026-08-14:** task-scoped, non-destructive CI/verification is part of the normal AI agent/chat-session completion loop. This does **not** grant release/publish authority and does **not** grant permission to write or merge `main`.

Read `docs/AI-SESSION-WORKFLOW.md` and `docs/AGENT-WORK-REGISTRATION.md` together with this file.

## Main remains integration-only

Ordinary implementation/CI agents must not independently land claim/status/source/test/script/workflow/packaging/release changes directly onto `main`.

The phrases `fix bug`, `update code`, `commit push git`, `continue all`, `implement all`, `run CI`, `fix CI`, `loop until success` or equivalent do not authorize `main` writes.

Only explicit owner integration authority permits a `main` merge/write, for example `merge all to main`, `you are the integration coordinator`, or `allow merge PR #... to main`.

CI authorization and integration authorization are separate.

## Task-scoped CI standing authorization

A session that owns a registered lane may run/observe/retry **applicable, non-destructive CI/checks for that lane** on its agent/recovery branch, PR or authorized integration candidate when the repository exposes such checks.

The required loop is:

1. bind the diagnosis to the exact workflow run/check and exact tested SHA;
2. inspect the failing job/step/log and determine root cause against current source;
3. fix on the agent/recovery branch, not on `main`;
4. add/retain deterministic regression coverage when appropriate;
5. commit and push the fix;
6. run/observe a fresh relevant attempt;
7. repeat from the newest relevant failure until every required/applicable lane check is green.

Never weaken tests, source guards, architecture/product contracts, security checks, release-integrity gates or expected behavior merely to get green CI.

This standing authorization does **not** permit:

- publishing a GitHub Release or package;
- supplying release-confirmation inputs merely to create a CI signal;
- dispatching unrelated workflows;
- changing or merging `main`;
- manufacturing licensed/local BricsCAD evidence;
- bypassing required approvals or secrets.

If branch/PR CI does not exist for a docs-only change, or path filters intentionally skip code/release jobs, record that fact rather than manufacturing a release run. Required docs/static/preflight checks still must pass when they exist.

## Existing automatic post-integration V25 cloud exception

GitHub Actions remain manual-only by default except the previously owner-approved automatic dispatcher:

- `.github/workflows/dispatch-v25-cloud-after-main-integration.yml`

That dispatcher may run on an integration-relevant `push` to `main` and may dispatch exactly:

- `.github/workflows/release-v25-cloud.yml`

Its purpose is to validate the single combined `main` landing after an authorized multi-agent integration batch. It is not permission for ordinary agents to merge `main` or publish arbitrary releases.

The dispatcher must remain narrow:

- integration-relevant `push` to `main` only;
- documentation-only changes ignored by path filtering;
- `github-actions[bot]` release-preparation pushes ignored to prevent recursion;
- newest relevant batch wins when adjacent landings overlap;
- only `release-v25-cloud.yml` may be dispatched;
- the release workflow retains exact-source preparation, source guards, Core smoke, BricsCAD V25 compile-reference, packaging and release-integrity gates;
- its existing explicit `confirm_release=RELEASE` contract remains required for the approved automatic path.

All other automatic triggers remain disallowed unless the owner explicitly changes this policy again.

## Manual release workflows remain release-controlled

The following remain release/operator lanes rather than ordinary branch CI:

- `.github/workflows/release-v25.yml`;
- `.github/workflows/release-v25-cloud.yml` when manually invoked;
- `.github/workflows/release-v26.yml`.

Do not use release workflows merely to validate a documentation or ordinary feature branch. Manual release/publish operations require explicit owner release authorization and their own confirmations.

## Canonical multi-agent progression

For ordinary registered work:

```text
CLAIM_ISSUE_OR_PR_VISIBLE
  -> AGENT_BRANCH_IMPLEMENTATION
  -> BRANCH/PR_VALIDATION
  -> CI_GREEN_FOR_LANE
  -> READY_FOR_INTEGRATION
```

If the owner later authorizes final integration:

```text
READY_LANES
  -> INTEGRATION_BRANCH
  -> INTEGRATION_REVIEW
  -> ONE_AUTHORIZED_FINAL_MERGE_TO_MAIN
  -> EXACT_CURRENT_MAIN_RECORDED
  -> AUTO_V25_CLOUD_CI
  -> CI_GREEN
  -> ALL_DONE
```

A session can therefore finish its assigned lane at `READY_FOR_INTEGRATION` when the prompt did not grant `main` authority, provided its scope is fully implemented, no known in-scope defect remains, required/applicable lane validation is green and the repository-side handoff is complete. It must report `MERGED TO MAIN: NO`.

## Integration coordinator responsibilities

Only an explicitly authorized integration coordinator may assemble and land the combined batch.

Before the final `main` landing, the coordinator must:

1. refresh current `origin/main`;
2. enumerate participating claim issues/PRs/branches and exact implementation SHAs;
3. combine every required lane into `integration/<batch-id>` or another explicitly approved candidate;
4. resolve semantic/API/test conflicts deliberately;
5. verify no required implementation remains only off-candidate;
6. run relevant combined-tree preflights/builds/smoke/tests;
7. inspect for accidental reversions and duplicate competing implementations;
8. freeze the batch;
9. perform the explicitly authorized final PR/merge to `main`;
10. refresh `main` and record the exact resulting SHA;
11. observe/fix the exact-current-main final CI through the appropriate authorized recovery path until green.

## Definition of `ALL MERGED TO MAIN`

For a specific owner request, report `ALL MERGED TO MAIN` only when an authorized reviewer has freshly verified:

- every required lane is represented in current `main` or explicitly excluded/superseded;
- no required code exists only on an agent branch, local worktree, stash, draft patch or unmerged PR;
- the combined current tree has no unresolved merge markers, accidental reversions, duplicate competing implementations or known semantic/API/test collisions;
- required combined-tree validation is acceptable;
- the exact current `main` SHA is recorded.

A branch existing/deleted, issue state, PR UI state or green CI for an older SHA is not sufficient proof.

## Exact-SHA and evidence rules

CI evidence proves only the exact tree it tested. A green run for an older SHA does not prove a newer branch, integration candidate or `main`.

The V25 cloud workflow is not licensed local BricsCAD runtime proof. Real `NETLOAD`/DemandLoad, native UI/runtime, private-DWG, signing, clean-machine installer and other `LOCAL_ONLY` evidence remain separate.

V25 and V26 runtime evidence are independent.

## Completion and session-close gate

Every AI/chat session must follow `docs/AI-SESSION-WORKFLOW.md` and report:

- `PROMPT/LANE STATUS: 100% COMPLETE` or `NOT 100% COMPLETE`;
- `SESSION CAN BE CLOSED/DELETED: YES` or `NO`;
- `MERGED TO MAIN: YES` or `NO`;
- exact branch/PR/issue, implementation SHA(s), validation/CI results and blockers.

If required/applicable CI is red, the lane is not complete. Continue diagnose -> fix -> push -> fresh run until green while actionable work remains within the session's tools/permissions/scope.

If a required local/proprietary prerequisite prevents proof, register the blocker precisely and do not claim unavailable evidence as PASS.

## Enforcement

- `scripts/preflight.py` retains broad repository/source policy checks.
- `scripts/preflight-ci-manual-only.py` remains the strict Actions trigger-safety gate and must continue allowing only the single approved automatic post-integration dispatcher while rejecting unauthorized automatic triggers/workflows.
- `scripts/preflight-all.py` should continue discovering the strict CI policy guard with other feature preflights.

Related documentation: `AGENTS.md`, `docs/AI-SESSION-WORKFLOW.md`, `docs/AGENT-WORK-REGISTRATION.md`, `docs/CI.md`, `docs/CI-READINESS.md`, `docs/MANUAL-BUILD-RELEASE.md`, `docs/MANUAL-BUILD-RELEASE-V26.md`, `docs/LOCAL-V25-QUALIFICATION.md`, `docs/LOCAL-V26-QUALIFICATION.md`.
