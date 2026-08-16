# QS3D hourly controller and worker pool

This document defines the scheduled multi-agent control topology requested for `trinhtanphat/QS3D-BricsCAD`.

The durable coordination ledger is GitHub issue #1910. This policy supplements `AGENTS.md`, `docs/AGENT-WORK-REGISTRATION.md`, `docs/MAIN-WRITE-AUTHORIZATION.md`, and the duplicate-prompt race policy tracked by #1904 / PR #1906. Existing repository ownership, protected-main, CI, and LOCAL_ONLY rules remain authoritative.

## Topology

The active pool contains exactly six hourly schedules:

- `QS3D-CONTROL`: the authoritative coordinator and an execution lane itself.
- `QS3D-WORKER-01`.
- `QS3D-WORKER-02`.
- `QS3D-WORKER-03`.
- `QS3D-WORKER-04`.
- `QS3D-WORKER-05`.

There is one controller and five worker schedules. The controller must also receive and execute its own work package every round; it is not a passive dispatcher.

## Hourly round

At the start of every hourly round, `QS3D-CONTROL` must:

1. Resolve the exact latest `main` SHA from GitHub.
2. Read open issues, PRs, active work claims/reservations, relevant CI state, and the latest #1910 ledger comments.
3. Reconcile the prior round: completed work, stalled work, stale branches, CI failures, merge candidates, and ownership changes.
4. Define a round ID and exact baseline SHA.
5. Allocate one non-overlapping work package to each worker and one to itself.
6. Record every assignment in #1910 before the lane edits overlapping repository scope.
7. During its own lane, perform a real high-priority audit/fix/integration package rather than stopping after dispatch.
8. Before any merge/write decision, refresh current `main` again and reject stale evidence.

Workers run later in the hour, read only the newest valid controller assignment for their lane, and verify that no newer reservation or main change invalidates the assignment before writing.

## Minimum one-hour workload sizing

Each lane assignment must be sized as an estimated **at least 60 minutes of substantive engineering work**.

This is a workload-sizing rule, not an elapsed-time claim. Agents must never fabricate that they spent 60 minutes, and a scheduled invocation is not guaranteed to remain active for a wall-clock hour. The controller must instead allocate enough concrete work that a competent engineering lane would reasonably require at least one hour to complete correctly.

If a single bug or PR is too small, the controller must bundle multiple related, non-overlapping subtasks into the same lane. A bundle can include, for example:

- root-cause analysis plus production fix plus deterministic regression tests;
- source fix plus stale preflight/contract repair plus exact-head CI verification;
- multiple independent small defects within one clearly bounded module;
- PR reconciliation plus conflict repair plus fresh CI plus integration/close-out;
- repository audit plus issue creation/reservation plus one or more verified fixes.

A valid assignment must state enough acceptance criteria that the worker can continue useful work if its first subtask completes early.

## Required assignment fields

Every controller assignment in #1910 must include:

- round ID and timestamp;
- exact `main` baseline SHA;
- lane owner (`QS3D-CONTROL` or worker 01-05);
- issue/PR references or a new bounded discovery scope;
- reserved files, symbols, modules, or explicit exclusions;
- estimated workload rationale showing why the package is >=60 minutes;
- acceptance criteria and regression expectations;
- tests/preflights/build/CI expected;
- branch/PR plan;
- merge authority and protected-main boundary;
- LOCAL_ONLY/runtime boundary when applicable;
- handoff condition and what the next round should do if incomplete.

## Default lane affinities

Affinities guide dispatch but do not override explicit reservations.

- Worker 01: Core correctness, floating-point/numeric edges, parsers, serialization, persistence, deterministic smoke.
- Worker 02: BricsCAD adapter, document affinity, commands, Ribbon/Workspace/Start Center/UI, V25/V26 shared source, source guards.
- Worker 03: Quantity, MEP/clash, BCF, interchange, rebar/domain workflows, cross-feature correctness.
- Worker 04: CI/preflight/build/package/security/dependencies/release tooling and failing-run recovery.
- Worker 05: PR review/reconciliation, stale/conflict repair, integration readiness, issue/branch cleanup, exact-main verification.
- Control: portfolio-wide audit/dispatch plus the highest-priority unowned fix or integration lane.

The controller may rebalance workers every round when current repository state makes another split safer or more valuable.

## Collision and simultaneous-agent handling

Scheduled workers are not exempt from normal multi-agent ownership rules.

- First visible valid reservation owns overlapping scope.
- A clean Git merge does not prove semantic non-overlap.
- Workers must not duplicate an existing active issue/PR/claim merely because the controller's previous baseline is stale.
- If the controller discovers two overlapping implementations, it must choose or record a canonical lane before either merges.
- Reassignment/takeover must be written to #1910 first.
- Branches/PRs that become superseded should be preserved long enough to retain evidence, then closed/cleaned safely after dependencies are verified.

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
9. refresh `main` after integration and verify the resulting lineage;
10. update/close the issue and release the reservation when complete.

Never weaken a correctness/security/release gate merely to turn CI green. Never claim tests, licensed BricsCAD behavior, or runtime evidence that was not actually executed.

## Controller reporting

The controller should use #1910 as the shared state machine between schedules. Each hourly round should leave a concise durable record of:

- exact starting `main`;
- six assignments (controller + five workers);
- prior-round outcomes and blockers;
- commits/PRs/merges/CI evidence that changed the plan;
- scopes that remain reserved into the next round.

Workers should comment only meaningful delivery or blocker evidence: root cause, before/after, files changed, tests, CI/run IDs, commit SHA, PR/merge SHA, remaining risk, and handoff. Do not flood the ledger with no-change heartbeat comments.
