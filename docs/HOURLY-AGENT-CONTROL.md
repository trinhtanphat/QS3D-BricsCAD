# QS3D hourly controller and worker pool

This document defines the scheduled multi-agent control topology for `trinhtanphat/QS3D-BricsCAD`.

The durable coordination ledger is GitHub issue #1910. This policy supplements `AGENTS.md`, `docs/AGENT-WORK-REGISTRATION.md`, `docs/MAIN-WRITE-AUTHORIZATION.md`, and the duplicate-prompt race policy tracked by #1904 / PR #1906. Existing repository ownership, protected-main, CI, and LOCAL_ONLY rules remain authoritative.

## Topology

The active pool contains exactly five hourly schedules:

- `QS3D-CONTROL`: the authoritative coordinator and an execution lane itself (Task 0).
- `QS3D-WORKER-01` (Task 1).
- `QS3D-WORKER-02` (Task 2).
- `QS3D-WORKER-03` (Task 3).
- `QS3D-WORKER-04` (Task 4).

There is one controller and four worker schedules. The controller must also receive and execute its own work package every round; it is not a passive dispatcher.

## Hourly round and dispatch ordering

At the start of every hourly round, `QS3D-CONTROL` must:

1. Resolve the exact latest `main` SHA from GitHub.
2. Read open issues, PRs, active work claims/reservations, relevant CI state, and the latest #1910 ledger comments.
3. Reconcile the prior round: completed work, stalled work, stale branches, CI failures, merge candidates, and ownership changes.
4. Define a round ID and cycle timestamp plus the exact `main` baseline SHA.
5. Allocate exactly five mutually exclusive packages: Task 0 to itself and Task 1-4 to the four workers.
6. Publish the complete Task 0-4 assignment in #1910 before any substantive Task 0 coding or any lane edits overlapping repository scope.
7. Only after the complete assignment is visible, execute Task 0 immediately as a real engineering package rather than stopping after dispatch.
8. Before any merge/write decision, refresh current `main` again and reject stale evidence.
9. Before closeout, verify that all five packages remain substantial and valid against current `main`; replace any package made obsolete by main drift with a new non-overlapping >=60-minute package and record the replacement in #1910.

The assignment record must contain both the cycle timestamp and exact baseline SHA so workers can distinguish it from all previous cycles. Workers read only the newest valid controller assignment for their lane, then verify that no newer reservation or main change invalidates the package before writing.

## Minimum one-hour workload sizing

Each of Task 0 through Task 4 must independently be sized as an estimated **at least 60 minutes of substantive engineering work**, preferably 60-120 minutes when the repository has enough safe scope. The minimum is per task, not combined across the pool.

This is a workload-sizing rule, not an elapsed-time claim. Agents must never fabricate that they spent 60 minutes, and a scheduled invocation is not guaranteed to remain active for a wall-clock hour. The controller must instead allocate enough concrete related work that a competent engineering lane would reasonably require at least one hour to complete correctly.

If one defect is too small, bundle multiple related, non-overlapping subtasks in the same lane. A valid package may combine root-cause analysis, production fixes, deterministic regression coverage, source guards, build/test validation, PR reconciliation, stale-base repair, and integration evidence, provided every subtask stays within the lane's explicit ownership boundary. Never pad a package with filler or unrelated work merely to satisfy the sizing rule.

## Required package fields

Every Task 0-4 assignment in #1910 must include:

- round ID and cycle timestamp;
- exact `main` baseline SHA;
- owner lane and task number (`QS3D-CONTROL`/Task 0 or worker 01-04/Task 1-4);
- issue/PR references or a new bounded discovery scope;
- owned component/files plus explicit exclusions or collision boundaries;
- one primary objective;
- two to four concrete related sub-objectives or fallback items that stay in the same lane;
- estimated workload rationale showing why that individual package is >=60 minutes;
- acceptance criteria and regression expectations;
- tests/preflights/build/CI required;
- branch/PR plan and canonical integration surface;
- merge authority and protected-main boundary;
- LOCAL_ONLY/runtime boundary when applicable;
- handoff condition and what the next round should do if incomplete.

## Package continuation rule

A lane does not stop merely because its first defect/sub-item is fixed, one test turns green, or one commit is pushed. After each completed sub-item, the lane continues immediately to the next justified sub-objective or fallback inside the same reserved package until the substantive package is exhausted or an external blocker prevents further safe progress.

If one sub-item becomes blocked, the lane should work another non-overlapping sub-item inside the same package when safe. A blocker is not permission to jump into another lane's files, duplicate an active reservation, or invent filler. Every additional commit must remain coherent with the package objective and ownership boundary.

## Main-drift and stale-package replacement

Main can advance while a cycle is executing. Before publishing, integrating, or closing a lane, refresh current `main` and reconcile safely without force-push or overwriting others.

If main drift makes a worker package obsolete before that worker can safely execute it, the controller must not leave the worker with stale instructions. It must select another real, non-overlapping package estimated >=60 minutes in the same worker lane (or an explicitly rebalanced lane when safer), record the replacement with the new baseline in #1910, and clearly mark the superseded package. A package that merely became smaller than the minimum must be expanded with justified same-component regression/hardening work or replaced; it must not be padded.

## Default lane affinities

Affinities match the five active schedules and guide dispatch without overriding explicit reservations.

- Worker 01 / Task 1: Core correctness and user-visible bug fixes.
- Worker 02 / Task 2: deterministic tests, reproductions, regression coverage, and tightly related defects.
- Worker 03 / Task 3: build, CI, configuration, dependencies, packaging, and developer-workflow reliability.
- Worker 04 / Task 4: safe refactoring, performance, resource handling, robustness, and maintainability.
- Control / Task 0: portfolio audit + dispatch + one highest-priority unowned fix, integration, or governance-correctness package.

The controller may rebalance the exact module boundaries each round when current repository state makes another split safer or more valuable, but it must still produce exactly Task 0-4 and keep the five scopes mutually exclusive.

## Collision and simultaneous-agent handling

Scheduled workers are not exempt from normal multi-agent ownership rules.

- First visible valid reservation owns overlapping scope.
- A clean Git merge does not prove semantic non-overlap.
- Workers must not duplicate an existing active issue/PR/claim merely because the controller's previous baseline is stale.
- If the controller discovers overlapping implementations, it must choose or record a canonical lane before either merges.
- Reassignment/takeover must be written to #1910 first.
- Branches/PRs that become superseded should be preserved long enough to retain evidence, then closed/cleaned safely after dependencies are verified.

## Write authority and `main` boundary

Once a lane accepts the newest valid non-overlapping assignment addressed to its own identity, that assignment explicitly authorizes the lane to perform the repository work inside the reserved scope without another owner confirmation. The lane may reproduce/fix bugs, edit/add/delete/refactor code/config/docs/tests, add deterministic regressions/source guards, run or inspect allowed validation, fix failures caused by its own changes, commit, push its dedicated task branch, and open/update its PR.

Scheduled lanes must not stop at readiness review or analysis when the accepted package contains valid implementation work they can perform remotely. This execution authority is branch-scoped and does not grant direct `main` write permission.

`main` is integration-only for the scheduled pool:

- Never push commits directly to `main`.
- Never write the default branch through the GitHub Contents API.
- Never update the `main` ref directly, force-update it, or use an equivalent bypass.
- Never infer merge authorization merely from a green branch, open PR, mergeability, completed implementation, or generic `fix bug` / `update code` / `commit` / `push git` wording.
- Integration into `main` must occur only through the repository-authorized PR/merge path and only when `docs/MAIN-WRITE-AUTHORIZATION.md`, active ownership, branch protection, current-head freshness, review state, and required CI/evidence all permit that named integration.
- If merge is not authorized, leave the branch/PR verified and hand it off; do not bypass the boundary.

A lane may reconcile/rebase its own task branch when repository policy and ownership permit. It must never overwrite another lane's branch or take over another active scope without a recorded reassignment.

## Engineering and Git rules

Workers should use a dedicated branch plus PR by default. A normal round should, when evidence supports it:

1. reproduce or verify a concrete defect/gap;
2. reserve a bounded scope;
3. fix production code/config/docs/tests as required;
4. add or update deterministic regression coverage;
5. run the relevant preflights/build/tests/CI available to the lane;
6. commit and push real changes;
7. open/update the PR with exact-head evidence;
8. merge only when repository policy, branch protection, current-head evidence, and the assignment's merge authority all allow it;
9. refresh `main` after integration and verify resulting lineage;
10. update/close the issue and release the reservation when complete.

"Push git" means push to the repository-prescribed canonical working branch/PR workflow, not direct protected-main push. Never force-push or overwrite another lane's work. Never weaken a correctness/security/release gate merely to turn CI green. Never claim tests, licensed BricsCAD behavior, or runtime evidence that was not actually executed.

## Controller reporting

The controller should use #1910 as the shared state machine between schedules. Each hourly round should leave a concise durable record of:

- exact starting `main` and cycle timestamp;
- exactly five assignments (Task 0-4: controller + four workers);
- prior-round outcomes and blockers;
- commits/PRs/merges/CI evidence that changed the plan;
- any replacement package created because current `main` invalidated the original assignment;
- scopes that remain reserved into the next round.

Workers should comment only meaningful delivery or blocker evidence: root cause, before/after, files changed, tests, CI/run IDs, commit SHA, PR/merge SHA, remaining risk, and handoff. Do not flood the ledger with no-change heartbeat comments.
