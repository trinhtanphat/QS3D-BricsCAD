# Issue #72 remote handoff — collision-safe current-main candidate (2026-08-21)

Supporting handoff only; this is not a second queue. The canonical owner/local queue remains `docs/LOCAL-AGENT-INBOX.md`.

## Supersession update after local continuation

This document preserves the collision audit at remote merge SHA
`d36989144ba1454bd0b5efd58d9a0ab834176edb`; its `UNQUALIFIED` warning applies
to that historical combined SHA. The canonical local carrier later merged
`main@805bc1dc63c007b562dc5d65529c76b25a7336ab` and froze exact source/runtime
candidate `9130c70582f4f67b83d4d63c6a59d3a30f7c0ed8`. That candidate passed the
official `962/962` source gate set, zero-warning Core/V25 builds, Core smoke,
offline WPF, licensed V25 NETLOAD/Ribbon/Palette, curved structural Millimeter
and Meter probes, and the affected Curtain P04/P10 native reviews. Canonical
sanitized evidence is recorded by commit
`7f6fd97bff232e64adc70c225f9b0b67557fa45c`; later docs/merge commits must not
be relabeled as the tested binary SHA. A final fetch observed later
`main@1a8f60fa5c41ac7c52e9e0ca042f4db72d9bd5cf` drift only in the unrelated
measurement boundary smoke and intentionally did not reopen the frozen runtime
candidate. Broad LOCAL-002/LOCAL-003 and the full customer/private-DWG matrix
remain pending as recorded in the canonical inbox and Issue #72 claim.

## Canonical carrier

- Lane-Key: `issue-72`
- Issue: #72
- Branch: `agent/codex/issue72-customer-pilot-20260820`
- PR: #3402
- Owner/session: `codex-root-20260820`

## Current-main reconciliation

- Prior carrier head: `2bc320053bed45c636c9d0edff3a5c02996a0bd4`
- Current `main`: `68e2083ff67c866bff81b2dfd2379a393481cf61`
- Main drift since the carrier merge-base affects only:
  - `src/QS3D.Core/Features/AddCreateStateMachine.cs`
  - `src/QS3D.Core/Features/FloatingToolWindowPolicy.cs`
  - `tests/QS3D.Core.SmokeTests/AddCreateStateMachineSmoke.cs`
  - `tests/QS3D.Core.SmokeTests/FloatingToolWindowPolicySmoke.cs`
- None of those paths collide with the existing #72 carrier changes.
- The authoritative exact handoff SHA is the branch head/commit containing this document and is posted to Issue #72 after push.
- IMPORTANT: the new combined SHA is `UNQUALIFIED` for licensed BricsCAD V25. Prior exact-SHA runtime evidence is historical and MUST NOT be carried forward.

## P0 collision disposition

- LOCAL-001: PASS (existing queue state).
- LOCAL-002: no separate active PR carrier surfaced; continue under #72. Source/preflight is already on the carrier; licensed runtime remains local-only.
- LOCAL-003: no separate active PR carrier surfaced; continue under #72; do not open a duplicate carrier.
- LOCAL-004: only the existing #72 base/local-matrix slice is in scope here. Beam STRETCH dependent P04 remains owned by PR #3387 / Lane-Key `issue-3383` and is excluded.
- LOCAL-013: excluded. Issue #1494 is ACTIVE for `local003`; do not claim or mutate that lane.

## P1/P2 disposition

No speculative remote reimplementation was added. Source-safe waves already marked `REMOTE_DONE` remain as-is; native/interactive rows stay local per `docs/LOCAL-AGENT-INBOX.md`.

## Local operator handoff

1. Sync the canonical branch and pin the exact SHA posted on Issue #72 after this commit.
2. Build/test this exact source candidate.
3. Run only lane-owned runtime rows in licensed BricsCAD V25.
4. Post fresh evidence to the canonical carrier; do not reuse old exact-SHA PASS evidence.
5. Respect collisions: PR #3387 / `issue-3383` and Issue #1494 remain excluded.

No main merge is requested here. No manual Actions dispatch is requested.
